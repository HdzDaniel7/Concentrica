namespace Soldadura.Core.Modelo;

/// <summary>
/// Tolerancias de aceptación definidas por el técnico/empresa (criterio interno del plano o
/// especificación, NO una norma publicada). Cada tolerancia es opcional: null = ese criterio
/// no se evalúa. Se guarda con el estudio para trazabilidad.
/// </summary>
public sealed class Especificaciones
{
    /// <summary>Etiqueta de la especificación (p. ej. "Plano 12345 rev B").</summary>
    public string Nombre { get; set; } = "Especificación interna";

    /// <summary>De dónde sale el criterio (plano, contrato, decisión interna…).</summary>
    public string Fuente { get; set; } = "";

    /// <summary>Etiqueta de nivel (informativa; se conserva la nomenclatura B/C/D).</summary>
    public NivelNorma Nivel { get; set; } = NivelNorma.B;

    /// <summary>
    /// Profundidad mínima requerida en cualquier corte (mm). null = no evaluar.
    /// Se evalúa contra el corte más superficial (mínimo de la serie).
    /// </summary>
    public double? ProfundidadMinima { get; set; }

    /// <summary>
    /// Profundidad máxima permitida en cualquier corte (mm). null = no evaluar.
    /// Se evalúa contra el corte más profundo (máximo de la serie).
    /// </summary>
    public double? ProfundidadMaxima { get; set; }

    /// <summary>Amplitud máxima de descentrado permitida (mm). null = no evaluar.</summary>
    public double? DescentradoMaximo { get; set; } = 0.20;

    /// <summary>Runout / TIR radial máximo permitido (mm). null = no evaluar.</summary>
    public double? RunoutMaximo { get; set; } = 0.30;

    /// <summary>Desviación máxima del ancho de cordón respecto al objetivo (mm). null = no evaluar.</summary>
    public double? ToleranciaAncho { get; set; }

    /// <summary>
    /// Exceso de cordón / refuerzo máximo permitido sobre la superficie (mm). null = no evaluar.
    /// Se evalúa contra el PEOR corte (cualquier muestra que lo supere reprueba), no contra la media.
    /// </summary>
    public double? ExcesoCordonMaximo { get; set; }

    /// <summary>Semiancho de la banda de "Revisar" alrededor de cada límite (mm).</summary>
    public double MargenRevision { get; set; } = 0.03;
}
