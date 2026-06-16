namespace Soldadura.Core.Modelo;

/// <summary>
/// Configuración del muestreo de un perfil: cuántas muestras, cómo se reparten
/// y si hay marca física de 0°.
/// </summary>
public sealed class ConfigMuestreo
{
    public ModoMuestreo Modo { get; set; } = ModoMuestreo.SimetricoPorAngulo;

    /// <summary>Cantidad de muestras (default 4).</summary>
    public int NumeroMuestras { get; set; } = 4;

    /// <summary>Paso angular en grados (modo SimetricoPorAngulo). Si null, se usa 360 / NumeroMuestras.</summary>
    public double? PasoGrados { get; set; }

    /// <summary>Paso de distancia (modo SimetricoPorDistancia o soldadura lineal).</summary>
    public double? PasoDistancia { get; set; }

    /// <summary>Posiciones (ángulos o distancias) cuando el modo es Personalizado.</summary>
    public List<double> PosicionesPersonalizadas { get; set; } = new();

    /// <summary>¿Existe marca física de 0° para amarrar la dirección a los ejes del robot?</summary>
    public bool TieneMarcaCero { get; set; }

    /// <summary>
    /// Máximo armónico que se puede resolver con confianza ≈ (NumeroMuestras − 1) / 2.
    /// 4 muestras → 1 (solo descentrado); 8 muestras → 3.
    /// </summary>
    public int ArmonicoMaximoResoluble => Math.Max(0, (NumeroMuestras - 1) / 2);
}
