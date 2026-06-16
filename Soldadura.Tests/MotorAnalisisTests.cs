using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;

namespace Soldadura.Tests;

public class MotorAnalisisTests
{
    [Fact]
    public void DescentradoCircular_RecomiendaOffsetXYOpuesto()
    {
        // posicionCentral(θ) = 5 + 0.3 cos θ − 0.2 sin θ
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)) - 0.2 * Math.Sin(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.NotNull(r.Radial);
        Assert.Equal(0.3, r.Radial!.Ajuste.DescentradoX, 6);
        Assert.Equal(-0.2, r.Radial.Ajuste.DescentradoY, 6);
        // ajuste ≈ −descentrado
        Assert.Equal(-0.3, r.Recomendacion.AjusteX, 6);
        Assert.Equal(0.2, r.Recomendacion.AjusteY, 6);
        Assert.False(r.Recomendacion.SoloMecanico);
    }

    [Fact]
    public void OvalidadPura_SeReportaSinDescentrado()
    {
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 4 + 0.5 * Math.Cos(2 * DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular();

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.True(r.Radial!.Ajuste.AmplitudDescentrado < 1e-6);
        Assert.Equal(0.5, r.Radial.Ajuste.Ovalidad, 6);
        // Toda la energía está en el 2º armónico ⇒ no corregible con offset.
        Assert.True(r.Radial.FraccionCorregible < MotorAnalisis.UmbralCorregible);
        Assert.True(r.Recomendacion.SoloMecanico);
    }

    [Fact]
    public void FraccionCorregible_MezclaDescentradoYTercerArmonico()
    {
        // 0.2 cos θ (corregible) + 0.4 cos 3θ (mecánico) → fracción = 0.04 / (0.04+0.16) = 0.2
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.2 * Math.Cos(DatosSinteticos.Grados(g)) + 0.4 * Math.Cos(3 * DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular();

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Equal(0.2, r.Radial!.FraccionCorregible, 6);
        Assert.True(r.Recomendacion.SoloMecanico);
    }

    [Fact]
    public void Z_SinCoef_SoloDireccion()
    {
        // profundidad media 1.0, objetivo 1.5 → falta penetración
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.5);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Equal(DireccionZ.AumentarPenetracion, r.Recomendacion.DireccionZ);
        Assert.Null(r.Recomendacion.AjusteZ);
    }

    [Fact]
    public void Z_ConCoef_CalculaMagnitud()
    {
        // diff = 1.5 − 1.0 = 0.5; coef = 2 mm prof / mm Z → ajusteZ = 0.25
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.5, coefFocoZ: 2.0);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Equal(DireccionZ.AumentarPenetracion, r.Recomendacion.DireccionZ);
        Assert.Equal(0.25, r.Recomendacion.AjusteZ!.Value, 9);
    }

    [Fact]
    public void CalidadIndicativa_BloqueaAjusteFino()
    {
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)));
        estudio.Muestras[0].CalidadMedicion = CalidadMedicion.Indicativa;
        var perfil = DatosSinteticos.PerfilCircular();

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Equal(CalidadMedicion.Indicativa, r.CalidadGlobal);
        Assert.True(r.Recomendacion.SoloMecanico);
    }

    [Fact]
    public void PocasMuestras_AvisaLimiteDeResolucion()
    {
        var estudio = DatosSinteticos.EstudioCircular(
            4, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(numMuestras: 4);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Equal(1, r.ArmonicoMaximoResoluble);
        Assert.Contains(r.Avisos, a => a.Contains("ovalidad", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SinMarcaCero_AvisaDireccionNoValida()
    {
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(marcaCero: false);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Contains(r.Avisos, a => a.Contains("marca de 0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PuntoMasSensible_DetectaMuestraDesviada()
    {
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        // Inyecta una desviación grande de profundidad en la muestra 3.
        estudio.Muestras[2].Profundidad = 2.0;
        var perfil = DatosSinteticos.PerfilCircular();

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.NotNull(r.PuntoMasSensible);
        Assert.Equal(3, r.PuntoMasSensible!.NumeroMuestra);
        Assert.Equal("Profundidad", r.PuntoMasSensible.Medida);
    }

    [Fact]
    public void SoloCordon_SinDatum_NoEvaluaCentrado()
    {
        // Mismo descentrado que el primer test, pero con modelo «solo cordón»: el análisis radial
        // no debe ejecutarse porque no hay datum externo.
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)) - 0.2 * Math.Sin(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(modelo: ModeloReferencia.SoloCordon);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Null(r.Radial);
        Assert.Null(r.Lineal);
        // La penetración (axial) sí se evalúa.
        Assert.Equal(1.0, r.Axial.EstadisticaProfundidad.Media, 9);
        // Sin centrado, no hay recomendación X/Y.
        Assert.Equal(0.0, r.Recomendacion.AjusteX, 9);
        Assert.Equal(0.0, r.Recomendacion.AjusteY, 9);
        Assert.Contains(r.Avisos, a => a.Contains("solo geometría del cordón", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SoloCordon_SiguePermitiendoRecomendacionZ()
    {
        // El eje Z (penetración) no depende del datum: debe seguir funcionando.
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        var perfil = DatosSinteticos.PerfilCircular(
            profundidadObjetivo: 1.5, coefFocoZ: 2.0, modelo: ModeloReferencia.SoloCordon);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.Null(r.Radial);
        Assert.Equal(DireccionZ.AumentarPenetracion, r.Recomendacion.DireccionZ);
        Assert.Equal(0.25, r.Recomendacion.AjusteZ!.Value, 9);
    }

    [Fact]
    public void ModeloConDatum_MantieneAnalisisRadial()
    {
        // Cualquier modelo con datum externo (p. ej. radial desde el eje) conserva el análisis radial.
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(modelo: ModeloReferencia.RadialDesdeCentro);

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.NotNull(r.Radial);
        Assert.Equal(0.3, r.Radial!.Ajuste.DescentradoX, 6);
    }

    [Fact]
    public void Lineal_RegresaDerivaYDesplazamiento()
    {
        var estudio = new Estudio { IdPieza = "L1", NumeroPuesta = 1 };
        for (int i = 0; i < 6; i++)
        {
            double x = i * 4.0; // posición a lo largo del largo
            double pos = 0.1 * x + 2.0;
            estudio.Muestras.Add(DatosSinteticos.Muestra(i + 1, x, pos, ancho: 1.0, profundidad: 1.0));
        }
        var perfil = new PerfilSoldadura
        {
            Tipo = TipoSoldadura.Lineal,
            GeometriaObjetivo = new GeometriaObjetivo { ProfundidadObjetivo = 1.0, Espesor = 3.0 }
        };

        var r = MotorAnalisis.Analizar(estudio, perfil);

        Assert.NotNull(r.Lineal);
        Assert.Null(r.Radial);
        Assert.Equal(0.1, r.Lineal!.Pendiente, 9);
        Assert.Equal(2.0, r.Lineal.Ordenada, 9);
        // La recomendación Y debe corregir el desplazamiento medio (-ordenada).
        Assert.Equal(-2.0, r.Recomendacion.AjusteY, 9);
        Assert.Equal(0.0, r.Recomendacion.AjusteX, 9);
    }
}
