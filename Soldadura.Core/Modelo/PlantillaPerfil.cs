namespace Soldadura.Core.Modelo;

/// <summary>
/// Plantilla reutilizable de perfil: agrupa el tipo de soldadura, la geometría objetivo y las
/// tolerancias de aceptación bajo un nombre identificable. Se persiste como JSON independiente
/// del estudio para importar/exportar criterios por pieza, modelo o revisión.
/// </summary>
public sealed class PlantillaPerfil
{
    public string Nombre { get; set; } = "";

    /// <summary>Descripción libre: pieza, revisión de plano, línea de producción, etc.</summary>
    public string Descripcion { get; set; } = "";

    public TipoSoldadura Tipo { get; set; } = TipoSoldadura.Circular;

    /// <summary>
    /// Modelo de referencia con el que se toman las medidas crudas de cada muestra (sección 4).
    /// De momento documenta el criterio del perfil; el diagrama correspondiente se muestra en la UI.
    /// </summary>
    public ModeloReferencia ModeloReferencia { get; set; } = ModeloReferencia.DatumPlanoExterno;

    public GeometriaObjetivo GeometriaObjetivo { get; set; } = new();

    public Especificaciones Especificaciones { get; set; } = new();

    /// <summary>Catálogo de zonas físicas predefinidas para este perfil.</summary>
    public List<string> ZonasCatalogo { get; set; } = [];

    /// <summary>
    /// Coeficiente foco↔Z aprendido (mm profundidad / mm ajuste Z). null = solo dirección disponible.
    /// </summary>
    public double? CoefFocoZ { get; set; }

    // ── Ejes del robot ──────────────────────────────────────────────────────────
    /// <summary>Nombre en el robot del eje de ajuste lateral principal (recomendación X).</summary>
    public string NombreEjeX { get; set; } = "X";

    /// <summary>Nombre en el robot del eje de ajuste lateral perpendicular (recomendación Y).</summary>
    public string NombreEjeY { get; set; } = "Y";

    /// <summary>Nombre en el robot del eje de ajuste de foco/penetración (recomendación Z).</summary>
    public string NombreEjeZ { get; set; } = "Z";

    /// <summary>
    /// Ángulo en grados (horario, visto desde arriba) del eje X del robot respecto a la
    /// dirección de la marca de 0° de la pieza. Permite rotar el vector de ajuste X/Y para
    /// que corresponda con el sistema de coordenadas real del robot.
    /// </summary>
    public double AnguloEjesGrados { get; set; } = 0;
}
