using Soldadura.Core.Modelo;

namespace Soldadura.Core.Analisis;

/// <summary>
/// Resultado completo del análisis de un estudio: ejes radial/axial o lineal, recomendación
/// de ajuste, calidad global, punto más sensible y avisos de resolución.
/// </summary>
public sealed class ResultadoAnalisis
{
    /// <summary>Eje radial (solo soldadura circular).</summary>
    public ResultadoRadial? Radial { get; init; }

    /// <summary>Eje axial / profundidad (siempre).</summary>
    public required ResultadoAxial Axial { get; init; }

    /// <summary>Análisis lineal (solo soldadura lineal).</summary>
    public ResultadoLineal? Lineal { get; init; }

    public required Recomendacion Recomendacion { get; init; }

    /// <summary>
    /// Calidad global = la peor calidad de medición entre las muestras.
    /// Indicativa ⇒ solo veredicto, sin ajuste fino.
    /// </summary>
    public CalidadMedicion CalidadGlobal { get; init; }

    public PuntoSensible? PuntoMasSensible { get; init; }

    /// <summary>Máximo armónico resoluble con el número de muestras disponible.</summary>
    public int ArmonicoMaximoResoluble { get; init; }

    /// <summary>Avisos honestos (límite de resolución, falta de marca de 0°, calidad, etc.).</summary>
    public IReadOnlyList<string> Avisos { get; init; } = Array.Empty<string>();
}
