namespace Soldadura.Core.Analisis;

/// <summary>Dirección sugerida del ajuste de enfoque en Z.</summary>
public enum DireccionZ
{
    SinCambio,
    /// <summary>Falta penetración: acercar el foco (más profundidad).</summary>
    AumentarPenetracion,
    /// <summary>Sobra penetración: alejar el foco (menos profundidad).</summary>
    ReducirPenetracion
}

/// <summary>
/// Recomendación de ajuste del robot derivada del análisis.
/// X/Y corrigen el descentrado; Z corrige el enfoque/penetración.
/// </summary>
public sealed class Recomendacion
{
    public Recomendacion(
        double ajusteX,
        double ajusteY,
        double? ajusteZ,
        DireccionZ direccionZ,
        bool soloMecanico,
        string mensaje)
    {
        AjusteX = ajusteX;
        AjusteY = ajusteY;
        AjusteZ = ajusteZ;
        DireccionZ = direccionZ;
        SoloMecanico = soloMecanico;
        Mensaje = mensaje;
    }

    /// <summary>Offset sugerido en X = −A₁ (mm). Solo válido con marca de 0°.</summary>
    public double AjusteX { get; }

    /// <summary>Offset sugerido en Y = −B₁ (mm). Solo válido con marca de 0°.</summary>
    public double AjusteY { get; }

    /// <summary>
    /// Magnitud sugerida del ajuste en Z (mm). null cuando no hay CoefFocoZ aprendido:
    /// en ese caso solo se conoce la dirección (<see cref="DireccionZ"/>).
    /// </summary>
    public double? AjusteZ { get; }

    public DireccionZ DireccionZ { get; }

    /// <summary>
    /// true cuando el error no es corregible con offset de robot (predomina ovalidad/vibración,
    /// o la calidad de medición no permite ajuste fino): solo queda ajuste mecánico.
    /// </summary>
    public bool SoloMecanico { get; }

    public string Mensaje { get; }
}
