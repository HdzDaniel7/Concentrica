namespace Soldadura.Core.Imagen;

/// <summary>
/// Especificación guardable de microscopio + objetivo, con la escala de la imagen (mm por píxel)
/// para no recalibrar en cada estudio (sección 9).
/// </summary>
public sealed class PerfilMicroscopio
{
    public string Nombre { get; set; } = "";

    /// <summary>Objetivo usado (p. ej. "5x", "10x").</summary>
    public string Objetivo { get; set; } = "";

    /// <summary>Escala de la imagen: milímetros que representa cada píxel.</summary>
    public double EscalaMmPorPixel { get; set; }

    /// <summary>
    /// Calibra la escala midiendo en la imagen una feature de longitud conocida:
    /// escala = mm reales / píxeles medidos.
    /// </summary>
    public static double CalibrarEscala(double pixelesMedidos, double mmConocidos)
    {
        if (pixelesMedidos <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelesMedidos), "Debe ser > 0.");
        return mmConocidos / pixelesMedidos;
    }
}
