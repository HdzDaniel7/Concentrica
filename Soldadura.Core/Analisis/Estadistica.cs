namespace Soldadura.Core.Analisis;

/// <summary>Utilidades estadísticas puras usadas por el motor de análisis.</summary>
public static class Estadistica
{
    /// <summary>Calcula media, desviación muestral, min y max de una serie.</summary>
    public static EstadisticaSerie Resumir(IReadOnlyList<double> valores)
    {
        if (valores.Count == 0)
            return new EstadisticaSerie(0, 0, 0, 0);

        double media = valores.Average();
        double sigma = 0.0;
        if (valores.Count > 1)
        {
            double sumaCuadrados = valores.Sum(v => (v - media) * (v - media));
            sigma = Math.Sqrt(sumaCuadrados / (valores.Count - 1));
        }
        return new EstadisticaSerie(media, sigma, valores.Min(), valores.Max());
    }

    /// <summary>
    /// Regresión lineal por mínimos cuadrados: devuelve (pendiente, ordenada) de y = pendiente·x + ordenada.
    /// </summary>
    public static (double Pendiente, double Ordenada) RegresionLineal(
        IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count != y.Count)
            throw new ArgumentException("x e y deben tener la misma longitud.");
        if (x.Count < 2)
            return (0.0, y.Count == 1 ? y[0] : 0.0);

        double mediaX = x.Average();
        double mediaY = y.Average();
        double sxx = 0.0, sxy = 0.0;
        for (int i = 0; i < x.Count; i++)
        {
            double dx = x[i] - mediaX;
            sxx += dx * dx;
            sxy += dx * (y[i] - mediaY);
        }
        if (sxx == 0.0)
            return (0.0, mediaY);

        double pendiente = sxy / sxx;
        double ordenada = mediaY - pendiente * mediaX;
        return (pendiente, ordenada);
    }
}
