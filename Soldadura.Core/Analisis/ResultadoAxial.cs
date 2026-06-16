namespace Soldadura.Core.Analisis;

/// <summary>
/// Análisis del eje axial (profundidad de penetración): estadística y, si la geometría es
/// circular, primer armónico de la profundidad (cabeceo / runout axial).
/// </summary>
public sealed class ResultadoAxial
{
    public ResultadoAxial(
        EstadisticaSerie estadisticaProfundidad, Armonico? cabeceo, EstadisticaSerie estadisticaExceso)
    {
        EstadisticaProfundidad = estadisticaProfundidad;
        Cabeceo = cabeceo;
        EstadisticaExceso = estadisticaExceso;
    }

    /// <summary>Estadística de la profundidad medida.</summary>
    public EstadisticaSerie EstadisticaProfundidad { get; }

    /// <summary>
    /// Estadística del exceso de cordón / refuerzo sobre la superficie. La norma lo evalúa contra
    /// el máximo (peor corte), no contra la media.
    /// </summary>
    public EstadisticaSerie EstadisticaExceso { get; }

    /// <summary>
    /// Primer armónico de la profundidad a lo largo del ángulo = cabeceo/runout axial.
    /// null en soldadura lineal o cuando no hay resolución para resolverlo.
    /// </summary>
    public Armonico? Cabeceo { get; }
}
