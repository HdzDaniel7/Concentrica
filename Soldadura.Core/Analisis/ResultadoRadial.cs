namespace Soldadura.Core.Analisis;

/// <summary>
/// Análisis del eje radial (en el plano) de una soldadura circular: ajuste armónico de la
/// posición de la línea central, descentrado (corregible con X/Y), ovalidad y runout.
/// </summary>
public sealed class ResultadoRadial
{
    public ResultadoRadial(
        AjusteArmonico ajuste,
        double runout,
        double fraccionCorregible,
        EstadisticaSerie estadisticaAncho)
    {
        Ajuste = ajuste;
        Runout = runout;
        FraccionCorregible = fraccionCorregible;
        EstadisticaAncho = estadisticaAncho;
    }

    /// <summary>Ajuste de Fourier de la posición central radial.</summary>
    public AjusteArmonico Ajuste { get; }

    /// <summary>Runout / TIR = rango pico a pico de la posición central medida (mm).</summary>
    public double Runout { get; }

    /// <summary>
    /// Fracción de la energía armónica que está en el 1er armónico (descentrado).
    /// Alta ⇒ corregible con offset de robot; baja ⇒ predomina ovalidad/vibración (mecánico).
    /// </summary>
    public double FraccionCorregible { get; }

    /// <summary>Estadística del ancho de cordón.</summary>
    public EstadisticaSerie EstadisticaAncho { get; }
}
