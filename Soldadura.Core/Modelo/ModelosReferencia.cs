namespace Soldadura.Core.Modelo;

/// <summary>
/// Metadatos de cada modelo de referencia (sección 4) que afectan al análisis. Los cuatro modelos
/// con datum (plano externo, radial, dos features, contorno) comparten la misma aritmética para
/// derivar AnchoCordon / PosicionCentral desde las dos distancias crudas; el único que cambia el
/// comportamiento del cálculo es SoloCordon, que no tiene referencia externa.
/// </summary>
public static class ModelosReferencia
{
    /// <summary>
    /// true si el modelo aporta un datum externo desde el que la posición de la línea central tiene
    /// sentido (permite evaluar centrado/descentrado/runout). false solo para SoloCordon, donde se
    /// mide únicamente la geometría del nugget (ancho/profundidad) sin referencia externa.
    /// </summary>
    public static bool TieneDatumExterno(ModeloReferencia modelo) =>
        modelo != ModeloReferencia.SoloCordon;
}
