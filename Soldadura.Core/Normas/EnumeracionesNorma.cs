namespace Soldadura.Core.Normas;

/// <summary>Resultado de evaluar una medida contra una regla o una norma completa.</summary>
public enum Veredicto
{
    /// <summary>Dentro de especificación con margen.</summary>
    Pasa,
    /// <summary>En la banda de incertidumbre del límite: requiere revisión humana.</summary>
    Revisar,
    /// <summary>Fuera de especificación.</summary>
    NoPasa
}

/// <summary>Qué cantidad del análisis evalúa una regla.</summary>
public enum MedidaEvaluada
{
    /// <summary>Profundidad de penetración (media del estudio).</summary>
    Profundidad,
    /// <summary>
    /// Profundidad mínima entre todos los cortes (peor caso de penetración insuficiente).
    /// Se evalúa con TipoLimite.Minimo: el corte más superficial debe superar el piso definido.
    /// </summary>
    ProfundidadMinima,
    /// <summary>
    /// Profundidad máxima entre todos los cortes (peor caso de penetración excesiva).
    /// Se evalúa con TipoLimite.Maximo: el corte más profundo no debe superar el techo definido.
    /// </summary>
    ProfundidadMaxima,
    /// <summary>Ancho de cordón (media del estudio).</summary>
    AnchoCordon,
    /// <summary>Amplitud del descentrado (1er armónico).</summary>
    Descentrado,
    /// <summary>Runout / TIR de la posición central.</summary>
    Runout,
    /// <summary>Exceso de cordón / refuerzo sobre la superficie (peor corte).</summary>
    ExcesoCordon
}

/// <summary>Sentido del límite: la medida debe quedar por debajo o por encima.</summary>
public enum TipoLimite
{
    /// <summary>La medida (o desviación) debe ser ≤ límite.</summary>
    Maximo,
    /// <summary>La medida (o desviación) debe ser ≥ límite.</summary>
    Minimo
}

/// <summary>Sobre qué se aplica el límite.</summary>
public enum ReferenciaLimite
{
    /// <summary>El valor medido tal cual.</summary>
    Absoluto,
    /// <summary>El valor absoluto de la desviación respecto al objetivo de la geometría.</summary>
    DesviacionDeObjetivo
}
