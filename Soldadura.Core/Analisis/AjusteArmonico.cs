namespace Soldadura.Core.Analisis;

/// <summary>
/// Resultado del ajuste de Fourier de una serie angular (p. ej. radioLineaCentral(θ)):
/// valor(θ) = RadioMedio + Σ_k [A_k·cos(kθ) + B_k·sin(kθ)].
/// </summary>
public sealed class AjusteArmonico
{
    public AjusteArmonico(double radioMedio, IReadOnlyList<Armonico> armonicos)
    {
        RadioMedio = radioMedio;
        Armonicos = armonicos;
    }

    /// <summary>Término constante r0 (media de la serie).</summary>
    public double RadioMedio { get; }

    /// <summary>Armónicos ordenados por orden creciente (k = 1, 2, …).</summary>
    public IReadOnlyList<Armonico> Armonicos { get; }

    private Armonico? Termino(int orden) => Armonicos.FirstOrDefault(a => a.Orden == orden);

    /// <summary>Componente X del descentrado = A₁ (1er armónico, corregible con robot).</summary>
    public double DescentradoX => Termino(1)?.A ?? 0.0;

    /// <summary>Componente Y del descentrado = B₁.</summary>
    public double DescentradoY => Termino(1)?.B ?? 0.0;

    /// <summary>Amplitud del descentrado = √(A₁² + B₁²).</summary>
    public double AmplitudDescentrado => Termino(1)?.Amplitud ?? 0.0;

    /// <summary>Ovalidad = amplitud del 2º armónico.</summary>
    public double Ovalidad => Termino(2)?.Amplitud ?? 0.0;

    /// <summary>Valor reconstruido por el modelo en el ángulo dado (rad).</summary>
    public double Evaluar(double anguloRad)
    {
        double v = RadioMedio;
        foreach (var arm in Armonicos)
            v += arm.Evaluar(anguloRad);
        return v;
    }

    /// <summary>Suma de cuadrados de las amplitudes (energía sin el término constante).</summary>
    public double EnergiaArmonicos => Armonicos.Sum(a => a.A * a.A + a.B * a.B);
}
