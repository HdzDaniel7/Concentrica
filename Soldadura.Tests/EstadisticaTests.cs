using Soldadura.Core.Analisis;

namespace Soldadura.Tests;

public class EstadisticaTests
{
    [Fact]
    public void ResumenCalculaMediaSigmaRango()
    {
        var serie = new double[] { 2, 4, 4, 4, 5, 5, 7, 9 };
        var r = Estadistica.Resumir(serie);

        Assert.Equal(5.0, r.Media, 9);
        // σ muestral (n−1) de esta serie clásica = √(32/7).
        Assert.Equal(Math.Sqrt(32.0 / 7.0), r.Sigma, 9);
        Assert.Equal(2.0, r.Min, 9);
        Assert.Equal(9.0, r.Max, 9);
        Assert.Equal(7.0, r.Rango, 9);
    }

    [Fact]
    public void RegresionRecuperaPendienteYOrdenada()
    {
        // y = 0.1·x + 2
        var x = new double[] { 0, 5, 10, 15, 20 };
        var y = x.Select(v => 0.1 * v + 2.0).ToList();

        var (pendiente, ordenada) = Estadistica.RegresionLineal(x, y);

        Assert.Equal(0.1, pendiente, 9);
        Assert.Equal(2.0, ordenada, 9);
    }

    [Fact]
    public void SerieVaciaNoLanza()
    {
        var r = Estadistica.Resumir(Array.Empty<double>());
        Assert.Equal(0.0, r.Media);
        Assert.Equal(0.0, r.Sigma);
    }
}
