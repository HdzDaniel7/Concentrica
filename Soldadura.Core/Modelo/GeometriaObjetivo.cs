using System.Text.Json.Serialization;

namespace Soldadura.Core.Modelo;

/// <summary>
/// Valores objetivo de la soldadura contra los que se evalúa cada estudio (en mm).
/// </summary>
public sealed class GeometriaObjetivo
{
    /// <summary>Radio objetivo del cordón (soldadura circular). null si no aplica.</summary>
    public double? RadioObjetivo { get; set; }

    /// <summary>Profundidad de penetración objetivo.</summary>
    public double ProfundidadObjetivo { get; set; }

    /// <summary>Ancho de cordón objetivo. Opcional.</summary>
    public double? AnchoObjetivo { get; set; }

    /// <summary>Espesor de referencia (t) usado por las fórmulas de la norma.</summary>
    public double Espesor { get; set; }

    /// <summary>Diámetro objetivo = 2 × RadioObjetivo (solo lectura).</summary>
    [JsonIgnore]
    public double? DiametroObjetivo => RadioObjetivo is double r ? r * 2 : null;
}
