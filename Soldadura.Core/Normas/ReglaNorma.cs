using Soldadura.Core.Modelo;

namespace Soldadura.Core.Normas;

/// <summary>
/// Regla declarativa de aceptación: para un defecto, una medida y un nivel, define el límite
/// (en función del espesor) y el margen de revisión.
/// </summary>
public sealed class ReglaNorma
{
    /// <summary>Nombre del defecto/criterio (p. ej. "Penetración insuficiente").</summary>
    public string Defecto { get; set; } = "";

    /// <summary>Qué cantidad del análisis se evalúa.</summary>
    public MedidaEvaluada Medida { get; set; }

    /// <summary>Nivel de calidad al que aplica la regla.</summary>
    public NivelNorma Nivel { get; set; }

    public TipoLimite TipoLimite { get; set; }

    public ReferenciaLimite Referencia { get; set; }

    public LimiteLineal Limite { get; set; } = new();

    /// <summary>
    /// Semiancho de la banda de incertidumbre alrededor del límite (mm): dentro de ella el
    /// veredicto es Revisar en vez de Pasa/NoPasa. 0 = frontera dura.
    /// </summary>
    public double MargenRevision { get; set; }

    /// <summary>Etiqueta breve de caso de uso visible en la UI.</summary>
    public string EtiquetaCaso { get; set; } = "";
}
