using MathNet.Numerics.LinearAlgebra;

namespace Soldadura.Core.Analisis;

/// <summary>
/// Ajuste de Fourier por mínimos cuadrados de una serie angular muestreada (uniforme o no,
/// con huecos). Modelo: valor(θ) = r0 + Σ_{k=1..K} [A_k·cos(kθ) + B_k·sin(kθ)].
/// </summary>
public static class AjustadorArmonico
{
    /// <summary>
    /// Ajusta hasta <paramref name="armonicoMaximo"/> armónicos a los pares (ángulo, valor).
    /// Los ángulos se reciben en GRADOS. K se recorta para no superar la resolución de los datos.
    /// </summary>
    public static AjusteArmonico Ajustar(
        IReadOnlyList<double> angulosGrados,
        IReadOnlyList<double> valores,
        int armonicoMaximo)
    {
        if (angulosGrados.Count != valores.Count)
            throw new ArgumentException("ángulos y valores deben tener la misma longitud.");
        int n = valores.Count;
        if (n == 0)
            return new AjusteArmonico(0, Array.Empty<Armonico>());

        // Para resolver K armónicos se requieren ≥ 2K+1 datos. Se recorta K en consecuencia.
        int k = Math.Max(0, armonicoMaximo);
        k = Math.Min(k, (n - 1) / 2);

        if (k == 0)
            return new AjusteArmonico(valores.Average(), Array.Empty<Armonico>());

        int columnas = 1 + 2 * k; // [1, cos(θ), sin(θ), cos(2θ), sin(2θ), …]
        var diseño = Matrix<double>.Build.Dense(n, columnas);
        var y = Vector<double>.Build.Dense(n);

        for (int i = 0; i < n; i++)
        {
            double theta = angulosGrados[i] * Math.PI / 180.0;
            diseño[i, 0] = 1.0;
            for (int orden = 1; orden <= k; orden++)
            {
                diseño[i, 2 * orden - 1] = Math.Cos(orden * theta);
                diseño[i, 2 * orden] = Math.Sin(orden * theta);
            }
            y[i] = valores[i];
        }

        // Solución por mínimos cuadrados (QR), robusta para muestreo no uniforme.
        Vector<double> coef = diseño.Solve(y);

        var armonicos = new List<Armonico>(k);
        for (int orden = 1; orden <= k; orden++)
            armonicos.Add(new Armonico(orden, coef[2 * orden - 1], coef[2 * orden]));

        return new AjusteArmonico(coef[0], armonicos);
    }
}
