namespace Soldadura.Core.Normas;

/// <summary>Veredicto global del estudio contra una norma, con el detalle por regla.</summary>
public sealed class ResultadoNormas
{
    public ResultadoNormas(
        string normaId, string edicion, bool verificada,
        Veredicto veredictoGlobal, IReadOnlyList<ResultadoRegla> reglas,
        MuestraCritica? muestraMasCritica = null)
    {
        NormaId = normaId;
        Edicion = edicion;
        Verificada = verificada;
        VeredictoGlobal = veredictoGlobal;
        Reglas = reglas;
        MuestraMasCritica = muestraMasCritica;
    }

    public string NormaId { get; }

    public string Edicion { get; }

    /// <summary>false si la norma usó valores aún no cotejados con la edición oficial.</summary>
    public bool Verificada { get; }

    /// <summary>El peor veredicto entre todas las reglas evaluadas.</summary>
    public Veredicto VeredictoGlobal { get; }

    public IReadOnlyList<ResultadoRegla> Reglas { get; }

    /// <summary>Regla con mayor severidad (la más comprometida). null si no hubo reglas aplicables.</summary>
    public ResultadoRegla? ReglaMasCritica =>
        Reglas.Count == 0 ? null : Reglas.MaxBy(r => r.Severidad);

    /// <summary>
    /// Muestra individual más comprometida respecto a la tolerancia (Profundidad/AnchoCordon).
    /// null si no se pasaron muestras o ninguna medida por-muestra era evaluable.
    /// </summary>
    public MuestraCritica? MuestraMasCritica { get; }
}
