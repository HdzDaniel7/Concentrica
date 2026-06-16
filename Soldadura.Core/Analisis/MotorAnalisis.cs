using Soldadura.Core.Modelo;

namespace Soldadura.Core.Analisis;

/// <summary>
/// Motor de análisis del núcleo. Toma un estudio y su perfil y produce el resultado completo:
/// descentrado/ovalidad (radial), estadística de profundidad (axial), recomendación X/Y/Z y avisos.
/// Sin dependencias de UI ni de disco.
/// </summary>
public static class MotorAnalisis
{
    /// <summary>Por debajo de esta fracción corregible, predomina el error mecánico.</summary>
    public const double UmbralCorregible = 0.5;

    /// <summary>Diferencias de profundidad menores a esto (mm) se consideran sin cambio en Z.</summary>
    public const double EpsilonZ = 1e-6;

    public static ResultadoAnalisis Analizar(Estudio estudio, PerfilSoldadura perfil)
    {
        ArgumentNullException.ThrowIfNull(estudio);
        ArgumentNullException.ThrowIfNull(perfil);
        if (estudio.Muestras.Count == 0)
            throw new ArgumentException("El estudio no tiene muestras.", nameof(estudio));

        var muestras = estudio.Muestras;
        int n = muestras.Count;
        int kMax = Math.Max(0, (n - 1) / 2);

        var angulos = muestras.Select(m => m.AnguloOPosicion).ToList();
        var profundidades = muestras.Select(m => m.Profundidad).ToList();
        var anchos = muestras.Select(m => m.AnchoCordon).ToList();
        var excesos = muestras.Select(m => m.ExcesoCordon).ToList();
        var posiciones = muestras.Select(m => m.PosicionCentral).ToList();

        var calidadGlobal = muestras.Any(m => m.CalidadMedicion == CalidadMedicion.Indicativa)
            ? CalidadMedicion.Indicativa
            : CalidadMedicion.Metrologica;

        bool circular = perfil.Tipo == TipoSoldadura.Circular;
        // SoloCordon no tiene datum externo: no se puede ubicar la línea central ni, por tanto,
        // evaluar centrado/descentrado/runout (radial) ni desplazamiento lateral (lineal).
        bool tieneDatum = ModelosReferencia.TieneDatumExterno(perfil.ModeloReferencia);
        var avisos = new List<string>();

        // --- Eje axial (siempre) ---
        var estProf = Estadistica.Resumir(profundidades);
        Armonico? cabeceo = null;
        if (circular && kMax >= 1)
            cabeceo = AjustadorArmonico.Ajustar(angulos, profundidades, 1).Armonicos.FirstOrDefault();
        var axial = new ResultadoAxial(estProf, cabeceo, Estadistica.Resumir(excesos));

        // --- Eje radial (circular) o regresión (lineal) ---
        ResultadoRadial? radial = null;
        ResultadoLineal? lineal = null;
        AjusteArmonico? ajusteRadial = null;

        if (tieneDatum && circular)
        {
            ajusteRadial = AjustadorArmonico.Ajustar(angulos, posiciones, kMax);
            double runout = Estadistica.Resumir(posiciones).Rango;
            double energiaTotal = ajusteRadial.EnergiaArmonicos;
            double energia1 = ajusteRadial.AmplitudDescentrado * ajusteRadial.AmplitudDescentrado;
            double fraccion = energiaTotal > 1e-12 ? energia1 / energiaTotal : 1.0;
            radial = new ResultadoRadial(ajusteRadial, runout, fraccion, Estadistica.Resumir(anchos));
        }
        else if (tieneDatum)
        {
            var (pendiente, ordenada) = Estadistica.RegresionLineal(angulos, posiciones);
            lineal = new ResultadoLineal(pendiente, ordenada, Estadistica.Resumir(anchos));
        }

        var recomendacion = ConstruirRecomendacion(perfil, calidadGlobal, radial, lineal, estProf);
        var sensible = PuntoCritico(muestras, ajusteRadial, lineal, estProf);

        AgregarAvisos(avisos, perfil, n, circular, tieneDatum, calidadGlobal);

        return new ResultadoAnalisis
        {
            Radial = radial,
            Axial = axial,
            Lineal = lineal,
            Recomendacion = recomendacion,
            CalidadGlobal = calidadGlobal,
            PuntoMasSensible = sensible,
            ArmonicoMaximoResoluble = kMax,
            Avisos = avisos
        };
    }

    private static Recomendacion ConstruirRecomendacion(
        PerfilSoldadura perfil, CalidadMedicion calidad,
        ResultadoRadial? radial, ResultadoLineal? lineal, EstadisticaSerie estProf)
    {
        double ajusteX = 0, ajusteY = 0;
        bool soloMecanico = calidad == CalidadMedicion.Indicativa;
        var partes = new List<string>();

        if (radial is not null)
        {
            ajusteX = -radial.Ajuste.DescentradoX;
            ajusteY = -radial.Ajuste.DescentradoY;
            if (radial.FraccionCorregible < UmbralCorregible)
            {
                soloMecanico = true;
                partes.Add($"Solo {radial.FraccionCorregible:P0} del runout es descentrado corregible: predomina error mecánico (ovalidad/vibración).");
            }
            else
            {
                partes.Add($"Descentrado corregible con offset de robot (X={ajusteX:+0.000;-0.000}, Y={ajusteY:+0.000;-0.000} mm).");
            }
        }
        else if (lineal is not null)
        {
            // Para lineal: Y corrige el desplazamiento lateral medio; la deriva necesita corrección angular del trayecto.
            ajusteY = -lineal.Ordenada;
            partes.Add($"Desplazamiento lateral medio: Y = {ajusteY:+0.000;-0.000} mm.");
            if (Math.Abs(lineal.Pendiente) > 1e-6)
                partes.Add($"Deriva {lineal.Pendiente:+0.000;-0.000} mm/mm: requiere corrección angular del trayecto (no solo offset).");
        }

        // --- Z (enfoque/penetración) ---
        double diffZ = perfil.GeometriaObjetivo.ProfundidadObjetivo - estProf.Media;
        DireccionZ dirZ;
        double? ajusteZ;
        if (Math.Abs(diffZ) <= EpsilonZ)
        {
            dirZ = DireccionZ.SinCambio;
            ajusteZ = 0.0;
        }
        else
        {
            dirZ = diffZ > 0 ? DireccionZ.AumentarPenetracion : DireccionZ.ReducirPenetracion;
            ajusteZ = perfil.CoefFocoZ is double c && c != 0 ? diffZ / c : null;
            partes.Add(ajusteZ is double az
                ? $"Z: {az:+0.000;-0.000} mm para alcanzar la profundidad objetivo."
                : $"Z: {(dirZ == DireccionZ.AumentarPenetracion ? "aumentar" : "reducir")} penetración (sin CoefFocoZ, solo dirección).");
        }

        if (calidad == CalidadMedicion.Indicativa)
            partes.Insert(0, "Calidad indicativa: solo veredicto, sin ajuste fino.");

        return new Recomendacion(ajusteX, ajusteY, ajusteZ, dirZ, soloMecanico, string.Join(" ", partes));
    }

    private static PuntoSensible? PuntoCritico(
        IReadOnlyList<Muestra> muestras, AjusteArmonico? ajusteRadial,
        ResultadoLineal? lineal, EstadisticaSerie estProf)
    {
        PuntoSensible? mejor = null;
        double mejorAbs = 0;

        void Considerar(Muestra m, string medida, double residual, double sigma)
        {
            if (sigma <= 1e-12) return;
            double norm = residual / sigma;
            if (Math.Abs(norm) > mejorAbs)
            {
                mejorAbs = Math.Abs(norm);
                mejor = new PuntoSensible(m.NumeroMuestra, m.AnguloOPosicion, medida, norm);
            }
        }

        // Residuos de posición central respecto al modelo (radial) o a la recta (lineal).
        var residPos = new List<double>();
        foreach (var m in muestras)
        {
            double esperado = ajusteRadial is not null
                ? ajusteRadial.Evaluar(m.AnguloOPosicion * Math.PI / 180.0)
                : lineal is not null ? lineal.Pendiente * m.AnguloOPosicion + lineal.Ordenada
                : m.PosicionCentral;
            residPos.Add(m.PosicionCentral - esperado);
        }
        double sigmaPos = Estadistica.Resumir(residPos).Sigma;
        for (int i = 0; i < muestras.Count; i++)
            Considerar(muestras[i], "PosicionCentral", residPos[i], sigmaPos);

        foreach (var m in muestras)
            Considerar(m, "Profundidad", m.Profundidad - estProf.Media, estProf.Sigma);

        return mejor;
    }

    private static void AgregarAvisos(
        List<string> avisos, PerfilSoldadura perfil, int n, bool circular, bool tieneDatum, CalidadMedicion calidad)
    {
        if (!tieneDatum)
        {
            avisos.Add("Modelo «solo geometría del cordón»: sin datum externo no se evalúa centrado/descentrado/runout; solo ancho y penetración.");
        }
        else if (circular)
        {
            int k = Math.Max(0, (n - 1) / 2);
            if (k < 2)
                avisos.Add($"Con {n} muestras solo se resuelve el descentrado (1er armónico); la ovalidad necesita ≥ 5 muestras.");
            else
                avisos.Add($"Con {n} muestras se resuelven armónicos hasta el orden {k}.");

            if (!perfil.ConfigMuestreo.TieneMarcaCero)
                avisos.Add("Sin marca de 0°: la magnitud del descentrado es válida, pero la dirección (X/Y a ejes del robot) no.");
        }
        else
        {
            avisos.Add("Soldadura lineal: AjusteY corrige el desplazamiento lateral medio. Si hay deriva significativa, corrija el ángulo del trayecto en el programa del robot.");
        }

        if (calidad == CalidadMedicion.Indicativa)
            avisos.Add("Hay muestras de calidad indicativa: no se recomienda ajuste fino.");

        if (perfil.CoefFocoZ is null)
            avisos.Add("Sin CoefFocoZ aprendido para el perfil: el ajuste en Z es solo direccional.");
    }
}
