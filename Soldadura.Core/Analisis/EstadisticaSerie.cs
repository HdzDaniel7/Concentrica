namespace Soldadura.Core.Analisis;

/// <summary>Resumen estadístico de una serie de valores (mm).</summary>
public sealed class EstadisticaSerie
{
    public EstadisticaSerie(double media, double sigma, double min, double max)
    {
        Media = media;
        Sigma = sigma;
        Min = min;
        Max = max;
    }

    /// <summary>Media aritmética.</summary>
    public double Media { get; }

    /// <summary>Desviación estándar muestral (n−1). 0 si hay menos de 2 datos.</summary>
    public double Sigma { get; }

    public double Min { get; }

    public double Max { get; }

    /// <summary>Rango pico a pico = Max − Min (usado como runout/TIR).</summary>
    public double Rango => Max - Min;
}
