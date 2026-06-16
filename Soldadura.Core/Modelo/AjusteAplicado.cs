namespace Soldadura.Core.Modelo;

/// <summary>
/// Ajuste/offset del robot efectivamente aplicado para producir esta puesta (mm), en el marco del
/// robot. Se registra para cerrar el lazo de mejora: comparar lo recomendado contra lo realmente
/// hecho y aprender el coeficiente foco↔Z desde el histórico (regresión de profundidad media vs Z).
/// Cada eje es nullable: null = ese ajuste no se registró para la puesta.
/// </summary>
public sealed class AjusteAplicado
{
    /// <summary>Offset lateral aplicado en el eje X del robot (mm).</summary>
    public double? X { get; set; }

    /// <summary>Offset lateral aplicado en el eje Y del robot (mm).</summary>
    public double? Y { get; set; }

    /// <summary>Offset de foco/penetración aplicado en el eje Z del robot (mm).</summary>
    public double? Z { get; set; }

    /// <summary>true si no se registró ningún ajuste (los tres ejes null).</summary>
    public bool Vacio => X is null && Y is null && Z is null;
}
