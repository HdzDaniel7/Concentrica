namespace Soldadura.Core.Analisis;

/// <summary>Un punto para aprender el coeficiente foco↔Z: Z aplicado vs profundidad media observada.</summary>
public record PuntoFoco(int NumeroPuesta, double AjusteZAplicado, double ProfundidadMedia);

/// <summary>Resultado del aprendizaje del coeficiente foco↔Z desde el histórico de una pieza/perfil.</summary>
public sealed class ResultadoAprendizajeFoco
{
    public required IReadOnlyList<PuntoFoco> Puntos { get; init; }

    /// <summary>
    /// Coeficiente aprendido (mm de profundidad por mm de ajuste Z): pendiente de la regresión
    /// profundidadMedia vs Z. null si no hay datos suficientes o la profundidad no responde a Z.
    /// </summary>
    public double? CoefFocoZ { get; init; }

    /// <summary>Bondad del ajuste lineal (R², 0..1). null si no se calculó regresión.</summary>
    public double? R2 { get; init; }

    /// <summary>Mensaje honesto sobre la calidad del aprendizaje.</summary>
    public required string Mensaje { get; init; }
}

/// <summary>
/// Aprende el coeficiente foco↔Z de un perfil a partir de varias puestas en las que se registró el
/// ajuste Z aplicado: regresión lineal de la profundidad media contra el Z aplicado. La pendiente es
/// el coeficiente (mm profundidad / mm Z). Sin dependencias de UI ni de disco.
/// </summary>
public static class MotorAprendizajeFoco
{
    /// <summary>Mínimo de puestas con Z registrado para intentar la regresión.</summary>
    public const int MinPuntos = 2;

    /// <summary>Variación mínima (mm) en Z y en pendiente para considerarla significativa.</summary>
    public const double Epsilon = 1e-9;

    /// <param name="serie">Por puesta: (número, Z aplicado o null, profundidad media observada).</param>
    public static ResultadoAprendizajeFoco Aprender(
        IReadOnlyList<(int puesta, double? ajusteZ, double profundidadMedia)> serie)
    {
        ArgumentNullException.ThrowIfNull(serie);

        var puntos = serie
            .Where(s => s.ajusteZ is not null)
            .OrderBy(s => s.puesta)
            .Select(s => new PuntoFoco(s.puesta, s.ajusteZ!.Value, s.profundidadMedia))
            .ToList();

        if (puntos.Count < MinPuntos)
            return Sin(puntos, null,
                $"Se necesitan ≥ {MinPuntos} puestas con ajuste Z registrado para aprender el coeficiente (hay {puntos.Count}).");

        var zs = puntos.Select(p => p.AjusteZAplicado).ToList();
        var ps = puntos.Select(p => p.ProfundidadMedia).ToList();

        if (zs.Max() - zs.Min() < Epsilon)
            return Sin(puntos, null,
                "Todas las puestas tienen el mismo ajuste Z: no hay variación para estimar el coeficiente.");

        var (pendiente, ordenada) = Estadistica.RegresionLineal(zs, ps);
        double r2 = CalcularR2(zs, ps, pendiente, ordenada);

        if (Math.Abs(pendiente) < Epsilon)
            return Sin(puntos, r2,
                "La profundidad no responde al ajuste Z en estos datos (pendiente ≈ 0): no se aprende coeficiente.");

        return new ResultadoAprendizajeFoco
        {
            Puntos = puntos,
            CoefFocoZ = pendiente,
            R2 = r2,
            Mensaje = $"CoefFocoZ ≈ {pendiente:0.000} mm/mm (R² = {r2:0.00}, {puntos.Count} puestas)."
        };
    }

    private static ResultadoAprendizajeFoco Sin(IReadOnlyList<PuntoFoco> puntos, double? r2, string mensaje) =>
        new() { Puntos = puntos, CoefFocoZ = null, R2 = r2, Mensaje = mensaje };

    private static double CalcularR2(IReadOnlyList<double> x, IReadOnlyList<double> y, double m, double b)
    {
        double mediaY = y.Average();
        double ssTot = 0, ssRes = 0;
        for (int i = 0; i < x.Count; i++)
        {
            double pred = m * x[i] + b;
            ssRes += (y[i] - pred) * (y[i] - pred);
            ssTot += (y[i] - mediaY) * (y[i] - mediaY);
        }
        return ssTot < Epsilon ? 1.0 : 1.0 - ssRes / ssTot;
    }
}
