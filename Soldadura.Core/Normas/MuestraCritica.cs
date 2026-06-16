namespace Soldadura.Core.Normas;

/// <summary>
/// La muestra cuya medida está más comprometida respecto a la tolerancia de la norma.
/// Sustituye, cuando hay norma cargada, a la normalización por σ de <c>ResultadoAnalisis.PuntoMasSensible</c>.
/// </summary>
public sealed class MuestraCritica
{
    public MuestraCritica(
        int numeroMuestra, double anguloOPosicion, MedidaEvaluada medida,
        double valor, double objetivo, double limite)
    {
        NumeroMuestra = numeroMuestra;
        AnguloOPosicion = anguloOPosicion;
        Medida = medida;
        Valor = valor;
        Objetivo = objetivo;
        Limite = limite;
    }

    public int NumeroMuestra { get; }

    public double AnguloOPosicion { get; }

    public MedidaEvaluada Medida { get; }

    /// <summary>Valor medido en esa muestra (mm).</summary>
    public double Valor { get; }

    /// <summary>Objetivo de la medida (mm).</summary>
    public double Objetivo { get; }

    /// <summary>Tolerancia aplicable (límite de la regla para el espesor del estudio, mm).</summary>
    public double Limite { get; }

    /// <summary>Desviación absoluta respecto al objetivo (mm).</summary>
    public double Desviacion => Math.Abs(Valor - Objetivo);

    /// <summary>
    /// Severidad normalizada a la tolerancia = |Valor − Objetivo| / Límite.
    /// ≥ 1 indica que esa muestra viola la tolerancia.
    /// </summary>
    public double Severidad => Limite != 0 ? Desviacion / Limite : double.NaN;
}
