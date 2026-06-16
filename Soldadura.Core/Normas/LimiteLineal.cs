namespace Soldadura.Core.Normas;

/// <summary>
/// Límite declarativo en función del espesor t: limite = clamp(Offset + CoefEspesor·t, Min, Max).
/// Cubre la forma típica de las normas ("0.1·t, mínimo 0.5 mm, máximo 1 mm") sin necesidad de un parser.
/// </summary>
public sealed class LimiteLineal
{
    /// <summary>Término constante del límite (mm).</summary>
    public double Offset { get; set; }

    /// <summary>Coeficiente que multiplica al espesor (adimensional).</summary>
    public double CoefEspesor { get; set; }

    /// <summary>Cota inferior opcional del límite (mm).</summary>
    public double? Min { get; set; }

    /// <summary>Cota superior opcional del límite (mm).</summary>
    public double? Max { get; set; }

    /// <summary>Evalúa el límite para un espesor dado.</summary>
    public double Evaluar(double espesor)
    {
        double v = Offset + CoefEspesor * espesor;
        if (Min is double min && v < min) v = min;
        if (Max is double max && v > max) v = max;
        return v;
    }
}
