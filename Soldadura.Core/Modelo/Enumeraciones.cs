namespace Soldadura.Core.Modelo;

/// <summary>Geometría de la soldadura.</summary>
public enum TipoSoldadura
{
    Lineal,
    Circular
}

/// <summary>Cómo se toman las distancias de referencia en cada muestra.</summary>
public enum ModeloReferencia
{
    /// <summary>Dos distancias desde una cara plana externa (caso base).</summary>
    DatumPlanoExterno,
    /// <summary>Radios desde un centro/eje conocido.</summary>
    RadialDesdeCentro,
    /// <summary>Medición perpendicular a una línea base entre dos features.</summary>
    DosFeatures,
    /// <summary>El contorno exterior de la pieza como datum.</summary>
    ContornoPieza,
    /// <summary>Solo geometría del cordón (ancho/profundidad), sin datum externo.</summary>
    SoloCordon
}

/// <summary>Nivel de calidad de la norma (B = más exigente).</summary>
public enum NivelNorma { B, C, D }

/// <summary>Confiabilidad de la medición según preparación y montaje.</summary>
public enum CalidadMedicion
{
    /// <summary>Baquelita pulida y atacada, corte perpendicular verificado. Permite ajuste fino.</summary>
    Metrologica,
    /// <summary>Montaje rápido, posible inclinación. Solo veredicto pasa/no pasa/revisar.</summary>
    Indicativa
}

/// <summary>Forma de capturar las medidas de una muestra.</summary>
public enum ModoCaptura
{
    /// <summary>Se mide en el microscopio y solo se escriben los números.</summary>
    AnotacionDirecta,
    /// <summary>Se marca sobre la imagen y el programa calcula con la escala.</summary>
    MedicionEnPantalla
}

/// <summary>Distribución de las muestras alrededor de la soldadura.</summary>
public enum ModoMuestreo
{
    SimetricoPorAngulo,
    SimetricoPorDistancia,
    Personalizado
}
