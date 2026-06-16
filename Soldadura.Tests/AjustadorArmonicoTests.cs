using Soldadura.Core.Analisis;

namespace Soldadura.Tests;

public class AjustadorArmonicoTests
{
    private const double Tol = 1e-6;

    [Fact]
    public void RecuperaDescentradoPuro()
    {
        // r(θ) = 5 + 0.3 cos θ − 0.2 sin θ
        int n = 8;
        var angulos = new List<double>();
        var valores = new List<double>();
        for (int i = 0; i < n; i++)
        {
            double g = 360.0 * i / n;
            angulos.Add(g);
            double th = DatosSinteticos.Grados(g);
            valores.Add(5 + 0.3 * Math.Cos(th) - 0.2 * Math.Sin(th));
        }

        var ajuste = AjustadorArmonico.Ajustar(angulos, valores, 3);

        Assert.Equal(5.0, ajuste.RadioMedio, 6);
        Assert.Equal(0.3, ajuste.DescentradoX, 6);
        Assert.Equal(-0.2, ajuste.DescentradoY, 6);
        Assert.True(ajuste.Ovalidad < Tol, $"Ovalidad debería ser ~0, fue {ajuste.Ovalidad}");
    }

    [Fact]
    public void RecuperaOvalidadSinDescentrado()
    {
        // r(θ) = 4 + 0.5 cos 2θ
        int n = 8;
        var angulos = new List<double>();
        var valores = new List<double>();
        for (int i = 0; i < n; i++)
        {
            double g = 360.0 * i / n;
            angulos.Add(g);
            valores.Add(4 + 0.5 * Math.Cos(2 * DatosSinteticos.Grados(g)));
        }

        var ajuste = AjustadorArmonico.Ajustar(angulos, valores, 3);

        Assert.True(ajuste.AmplitudDescentrado < Tol, $"Descentrado debería ser ~0, fue {ajuste.AmplitudDescentrado}");
        Assert.Equal(0.5, ajuste.Ovalidad, 6);
    }

    [Fact]
    public void RecortaKCuandoFaltanMuestras()
    {
        // 4 muestras → solo 1er armónico resoluble (2·1+1 = 3 ≤ 4; 2·2+1 = 5 > 4).
        var angulos = new List<double> { 0, 90, 180, 270 };
        var valores = new List<double> { 5.3, 5.2, 4.7, 4.8 };

        var ajuste = AjustadorArmonico.Ajustar(angulos, valores, 3);

        Assert.Single(ajuste.Armonicos);
        Assert.Equal(1, ajuste.Armonicos[0].Orden);
    }

    [Fact]
    public void AjustaMuestreoNoUniformeConHueco()
    {
        // Mismas amplitudes pero ángulos irregulares (falta una posición).
        var angulos = new List<double> { 0, 30, 75, 140, 210, 300 };
        var valores = angulos
            .Select(g => 5 + 0.3 * Math.Cos(DatosSinteticos.Grados(g)) - 0.2 * Math.Sin(DatosSinteticos.Grados(g)))
            .ToList();

        var ajuste = AjustadorArmonico.Ajustar(angulos, valores, 1);

        Assert.Equal(0.3, ajuste.DescentradoX, 6);
        Assert.Equal(-0.2, ajuste.DescentradoY, 6);
    }
}
