namespace Soldadura.Core.Analisis;

/// <summary>
/// Un término armónico de la serie de Fourier: valor(θ) suma … + A·cos(kθ) + B·sin(kθ).
/// k=1 → descentrado; k=2 → ovalidad; k≥3 → vibración/ondulación.
/// </summary>
public sealed class Armonico
{
    public Armonico(int orden, double a, double b)
    {
        Orden = orden;
        A = a;
        B = b;
    }

    /// <summary>Orden del armónico (k ≥ 1).</summary>
    public int Orden { get; }

    /// <summary>Coeficiente del coseno.</summary>
    public double A { get; }

    /// <summary>Coeficiente del seno.</summary>
    public double B { get; }

    /// <summary>Amplitud = √(A² + B²).</summary>
    public double Amplitud => Math.Sqrt(A * A + B * B);

    /// <summary>Fase en radianes = atan2(B, A).</summary>
    public double Fase => Math.Atan2(B, A);

    /// <summary>Contribución de este armónico en el ángulo dado (rad).</summary>
    public double Evaluar(double anguloRad) =>
        A * Math.Cos(Orden * anguloRad) + B * Math.Sin(Orden * anguloRad);
}
