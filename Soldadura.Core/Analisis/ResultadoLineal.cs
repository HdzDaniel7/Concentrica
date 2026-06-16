namespace Soldadura.Core.Analisis;

/// <summary>
/// Análisis de soldadura lineal: regresión de la posición central a lo largo del largo.
/// Pendiente = deriva; ordenada = desplazamiento medio respecto al datum.
/// </summary>
public sealed class ResultadoLineal
{
    public ResultadoLineal(double pendiente, double ordenada, EstadisticaSerie estadisticaAncho)
    {
        Pendiente = pendiente;
        Ordenada = ordenada;
        EstadisticaAncho = estadisticaAncho;
    }

    /// <summary>Deriva de la línea central por unidad de largo (mm/mm).</summary>
    public double Pendiente { get; }

    /// <summary>Desplazamiento medio de la línea central (ordenada al origen, mm).</summary>
    public double Ordenada { get; }

    /// <summary>Estadística del ancho de cordón a lo largo del largo.</summary>
    public EstadisticaSerie EstadisticaAncho { get; }
}
