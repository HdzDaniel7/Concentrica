namespace Soldadura.Core.Normas;

/// <summary>
/// Una norma cargada de forma declarativa: identidad (sello edición/fecha), si sus valores están
/// verificados contra la edición oficial, y el conjunto de reglas.
/// </summary>
public sealed class Norma
{
    /// <summary>Identificador (p. ej. "ISO 13919-1").</summary>
    public string Id { get; set; } = "";

    /// <summary>Edición (p. ej. "2019").</summary>
    public string Edicion { get; set; } = "";

    /// <summary>Fecha del sello de carga/verificación.</summary>
    public DateTime FechaSello { get; set; }

    /// <summary>
    /// false mientras los valores no se hayan cotejado contra la edición oficial.
    /// El motor lo propaga para que la UI/el reporte adviertan que el veredicto es provisional.
    /// </summary>
    public bool Verificada { get; set; }

    public List<ReglaNorma> Reglas { get; set; } = new();
}
