using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;

namespace Soldadura.Tests;

public class MuestraCriticaTests
{
    private static Norma NormaPenetracion(double offset = 0.2) => new()
    {
        Id = "TEST",
        Edicion = "1",
        Verificada = true,
        Reglas =
        {
            new ReglaNorma
            {
                Defecto = "Desviación de penetración",
                Medida = MedidaEvaluada.Profundidad,
                Nivel = NivelNorma.B,
                TipoLimite = TipoLimite.Maximo,
                Referencia = ReferenciaLimite.DesviacionDeObjetivo,
                Limite = new LimiteLineal { Offset = offset, CoefEspesor = 0 },
                MargenRevision = 0.05
            }
        }
    };

    [Fact]
    public void IdentificaLaMuestraConMayorSeveridad()
    {
        // Objetivo 1.0. La muestra 3 se desvía 0.3; las demás están en objetivo.
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        estudio.Muestras[2].Profundidad = 1.3;
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var r = MotorNormas.Evaluar(
            NormaPenetracion(), NivelNorma.B, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.NotNull(r.MuestraMasCritica);
        Assert.Equal(3, r.MuestraMasCritica!.NumeroMuestra);
        Assert.Equal(MedidaEvaluada.Profundidad, r.MuestraMasCritica.Medida);
        // Severidad = 0.3 / 0.2 = 1.5 (fuera de tolerancia).
        Assert.Equal(1.5, r.MuestraMasCritica.Severidad, 6);
    }

    [Fact]
    public void SinMuestras_NoCalculaMuestraCritica()
    {
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var r = MotorNormas.Evaluar(
            NormaPenetracion(), NivelNorma.B, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica); // sin muestras

        Assert.Null(r.MuestraMasCritica);
    }

    [Fact]
    public void SeveridadMenorAUno_CuandoTodoDentroDeTolerancia()
    {
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        estudio.Muestras[1].Profundidad = 1.1; // desv 0.1, límite 0.2 → severidad 0.5
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var r = MotorNormas.Evaluar(
            NormaPenetracion(), NivelNorma.B, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.NotNull(r.MuestraMasCritica);
        Assert.Equal(2, r.MuestraMasCritica!.NumeroMuestra);
        Assert.True(r.MuestraMasCritica.Severidad < 1.0);
    }
}
