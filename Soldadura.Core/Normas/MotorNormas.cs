using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;

namespace Soldadura.Core.Normas;

/// <summary>
/// Evalúa el resultado de un análisis contra una norma declarativa y emite veredictos
/// pasa/revisar/no-pasa por regla y un veredicto global.
/// </summary>
public static class MotorNormas
{
    /// <param name="muestras">
    /// Opcional. Si se pasan, se calcula la muestra individual más comprometida respecto a la
    /// tolerancia (normalización por-muestra, no por σ).
    /// </param>
    public static ResultadoNormas Evaluar(
        Norma norma,
        NivelNorma nivel,
        GeometriaObjetivo geometria,
        ResultadoAnalisis analisis,
        CalidadMedicion calidadGlobal,
        IReadOnlyList<Muestra>? muestras = null)
    {
        ArgumentNullException.ThrowIfNull(norma);
        ArgumentNullException.ThrowIfNull(geometria);
        ArgumentNullException.ThrowIfNull(analisis);

        var resultados = new List<ResultadoRegla>();

        foreach (var regla in norma.Reglas.Where(r => r.Nivel == nivel))
        {
            if (!TryMedida(regla.Medida, analisis, out double valor))
                continue; // medida no disponible para este estudio (p. ej. descentrado en lineal)

            double comparand;
            if (regla.Referencia == ReferenciaLimite.DesviacionDeObjetivo)
            {
                if (!TryObjetivo(regla.Medida, geometria, out double objetivo))
                    continue; // objetivo no definido (p. ej. AnchoObjetivo nulo): regla no evaluable
                comparand = Math.Abs(valor - objetivo);
            }
            else
            {
                comparand = valor;
            }

            double limite = regla.Limite.Evaluar(geometria.Espesor);
            var (veredicto, holgura) = Clasificar(regla, comparand, limite);

            // Calidad indicativa: no se concede aprobación firme (sección 7).
            if (calidadGlobal == CalidadMedicion.Indicativa && veredicto == Veredicto.Pasa)
                veredicto = Veredicto.Revisar;

            resultados.Add(new ResultadoRegla(regla, comparand, limite, veredicto, holgura));
        }

        var global = resultados.Count == 0
            ? Veredicto.Pasa
            : resultados.Max(r => r.Veredicto);

        var muestraCritica = muestras is null
            ? null
            : MuestraMasComprometida(norma, nivel, geometria, muestras);

        return new ResultadoNormas(
            norma.Id, norma.Edicion, norma.Verificada, global, resultados, muestraCritica);
    }

    /// <summary>
    /// Recorre las muestras y devuelve la de mayor severidad (|valor − objetivo| / límite) entre
    /// las reglas por-muestra (Profundidad / AnchoCordon con referencia a objetivo).
    /// </summary>
    private static MuestraCritica? MuestraMasComprometida(
        Norma norma, NivelNorma nivel, GeometriaObjetivo geometria, IReadOnlyList<Muestra> muestras)
    {
        MuestraCritica? mejor = null;

        foreach (var regla in norma.Reglas.Where(r => r.Nivel == nivel
            && r.Referencia == ReferenciaLimite.DesviacionDeObjetivo
            && (r.Medida == MedidaEvaluada.Profundidad || r.Medida == MedidaEvaluada.AnchoCordon)))
        {
            if (!TryObjetivo(regla.Medida, geometria, out double objetivo))
                continue;
            double limite = regla.Limite.Evaluar(geometria.Espesor);
            if (limite == 0) continue;

            foreach (var m in muestras)
            {
                double valor = regla.Medida == MedidaEvaluada.Profundidad ? m.Profundidad : m.AnchoCordon;
                var candidata = new MuestraCritica(
                    m.NumeroMuestra, m.AnguloOPosicion, regla.Medida, valor, objetivo, limite);
                if (mejor is null || candidata.Severidad > mejor.Severidad)
                    mejor = candidata;
            }
        }

        return mejor;
    }

    private static (Veredicto, double) Clasificar(ReglaNorma regla, double comparand, double limite)
    {
        double margen = regla.MargenRevision;
        if (regla.TipoLimite == TipoLimite.Maximo)
        {
            double holgura = limite - comparand;
            if (comparand <= limite - margen) return (Veredicto.Pasa, holgura);
            if (comparand <= limite + margen) return (Veredicto.Revisar, holgura);
            return (Veredicto.NoPasa, holgura);
        }
        else
        {
            double holgura = comparand - limite;
            if (comparand >= limite + margen) return (Veredicto.Pasa, holgura);
            if (comparand >= limite - margen) return (Veredicto.Revisar, holgura);
            return (Veredicto.NoPasa, holgura);
        }
    }

    private static bool TryMedida(MedidaEvaluada medida, ResultadoAnalisis a, out double valor)
    {
        switch (medida)
        {
            case MedidaEvaluada.Profundidad:
                valor = a.Axial.EstadisticaProfundidad.Media;
                return true;
            case MedidaEvaluada.ProfundidadMinima:
                valor = a.Axial.EstadisticaProfundidad.Min;
                return true;
            case MedidaEvaluada.ProfundidadMaxima:
                valor = a.Axial.EstadisticaProfundidad.Max;
                return true;
            case MedidaEvaluada.AnchoCordon:
                if (a.Radial is not null) { valor = a.Radial.EstadisticaAncho.Media; return true; }
                if (a.Lineal is not null) { valor = a.Lineal.EstadisticaAncho.Media; return true; }
                break;
            case MedidaEvaluada.Descentrado:
                if (a.Radial is not null) { valor = a.Radial.Ajuste.AmplitudDescentrado; return true; }
                break;
            case MedidaEvaluada.Runout:
                if (a.Radial is not null) { valor = a.Radial.Runout; return true; }
                break;
            case MedidaEvaluada.ExcesoCordon:
                // Peor corte (más fiel a ISO): cualquier muestra que supere el límite reprueba.
                valor = a.Axial.EstadisticaExceso.Max;
                return true;
        }
        valor = 0;
        return false;
    }

    private static bool TryObjetivo(MedidaEvaluada medida, GeometriaObjetivo g, out double objetivo)
    {
        switch (medida)
        {
            case MedidaEvaluada.Profundidad:
            case MedidaEvaluada.ProfundidadMinima:
            case MedidaEvaluada.ProfundidadMaxima:
                objetivo = g.ProfundidadObjetivo;
                return true;
            case MedidaEvaluada.AnchoCordon:
                if (g.AnchoObjetivo is double a) { objetivo = a; return true; }
                break;
            case MedidaEvaluada.Descentrado:
            case MedidaEvaluada.Runout:
                // El objetivo es cero (perfectamente centrado): la desviación coincide con el valor.
                objetivo = 0;
                return true;
        }
        objetivo = 0;
        return false;
    }
}
