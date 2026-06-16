using Soldadura.Core.Analisis;

namespace Soldadura.Tests;

/// <summary>
/// Tests de MotorAprendizajeFoco: recuperación del coeficiente foco↔Z por regresión de la
/// profundidad media contra el ajuste Z aplicado, y los casos donde no se puede aprender.
/// </summary>
public class AprendizajeFocoTests
{
    private static (int, double?, double) P(int puesta, double? z, double prof) => (puesta, z, prof);

    [Fact]
    public void RecuperaCoeficienteConocido_RelacionLinealPerfecta()
    {
        // prof = 0.5·Z + 1.0  → la pendiente (CoefFocoZ) debe recuperarse como 0.5 con R²=1.
        const double coef = 0.5, basePrf = 1.0;
        var serie = new (int, double?, double)[]
        {
            P(1, 0.0, basePrf + coef * 0.0),
            P(2, 0.4, basePrf + coef * 0.4),
            P(3, 0.8, basePrf + coef * 0.8),
            P(4, 1.2, basePrf + coef * 1.2),
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.NotNull(res.CoefFocoZ);
        Assert.Equal(coef, res.CoefFocoZ!.Value, 6);
        Assert.NotNull(res.R2);
        Assert.Equal(1.0, res.R2!.Value, 6);
        Assert.Equal(4, res.Puntos.Count);
    }

    [Fact]
    public void RecuperaCoeficienteNegativo()
    {
        // Relación inversa: más Z reduce penetración.
        var serie = new (int, double?, double)[]
        {
            P(1, 0.0, 2.0),
            P(2, 0.5, 1.7),
            P(3, 1.0, 1.4),
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.NotNull(res.CoefFocoZ);
        Assert.Equal(-0.6, res.CoefFocoZ!.Value, 6);
    }

    [Fact]
    public void DatosInsuficientes_MenosDeDosConZ_NoAprende()
    {
        var serie = new (int, double?, double)[]
        {
            P(1, 0.5, 2.0),
            P(2, null, 2.1),   // sin Z registrado → se ignora
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.Null(res.CoefFocoZ);
        Assert.Single(res.Puntos);
        Assert.Contains("≥", res.Mensaje);
    }

    [Fact]
    public void SinVariacionEnZ_NoAprende()
    {
        // Todas las puestas con el mismo Z: no se puede estimar pendiente.
        var serie = new (int, double?, double)[]
        {
            P(1, 0.3, 2.0),
            P(2, 0.3, 2.2),
            P(3, 0.3, 1.9),
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.Null(res.CoefFocoZ);
        Assert.Contains("mismo ajuste Z", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoraPuestasSinZ_YOrdenaPorPuesta()
    {
        var serie = new (int, double?, double)[]
        {
            P(3, 1.0, 1.5),
            P(1, 0.0, 1.0),
            P(2, null, 9.9),   // ignorada
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.Equal(2, res.Puntos.Count);
        Assert.Equal(1, res.Puntos[0].NumeroPuesta);   // ordenado por puesta
        Assert.Equal(3, res.Puntos[1].NumeroPuesta);
        Assert.NotNull(res.CoefFocoZ);
        Assert.Equal(0.5, res.CoefFocoZ!.Value, 6);     // (1.5−1.0)/(1.0−0.0)
    }

    [Fact]
    public void ProfundidadNoRespondeAZ_PendienteCero_NoAprende()
    {
        // Profundidad constante frente a Z variable → pendiente ≈ 0.
        var serie = new (int, double?, double)[]
        {
            P(1, 0.0, 2.0),
            P(2, 0.5, 2.0),
            P(3, 1.0, 2.0),
        };

        var res = MotorAprendizajeFoco.Aprender(serie);

        Assert.Null(res.CoefFocoZ);
        Assert.Contains("no responde", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }
}
