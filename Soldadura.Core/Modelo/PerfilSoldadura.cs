namespace Soldadura.Core.Modelo;

/// <summary>
/// Plantilla de soldadura a revisar: define geometría objetivo, norma, modelo de
/// referencia y cómo se capturan las muestras. Se selecciona un perfil y luego se
/// llenan los datos de un estudio contra él.
/// </summary>
public sealed class PerfilSoldadura
{
    public string Nombre { get; set; } = "";

    public TipoSoldadura Tipo { get; set; } = TipoSoldadura.Circular;

    public ModeloReferencia ModeloReferencia { get; set; } = ModeloReferencia.DatumPlanoExterno;

    public GeometriaObjetivo GeometriaObjetivo { get; set; } = new();

    /// <summary>Identificador de la norma (p. ej. "ISO 13919-1:2019").</summary>
    public string Norma { get; set; } = "ISO 13919-1:2019";

    public NivelNorma Nivel { get; set; } = NivelNorma.B;

    public ConfigMuestreo ConfigMuestreo { get; set; } = new();

    /// <summary>
    /// Catálogo de zonas físicas de la pieza para la etiqueta ZonaPieza.
    /// Se permite además texto libre al capturar.
    /// </summary>
    public List<string> ZonasCatalogo { get; set; } = new();

    /// <summary>
    /// Coeficiente foco↔Z aprendido (mm de profundidad por mm de Z) para este perfil.
    /// null mientras no haya datos suficientes; entonces la recomendación en Z es solo direccional.
    /// </summary>
    public double? CoefFocoZ { get; set; }
}
