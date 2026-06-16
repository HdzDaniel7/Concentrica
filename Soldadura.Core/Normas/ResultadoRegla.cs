namespace Soldadura.Core.Normas;

/// <summary>Resultado de evaluar una <see cref="ReglaNorma"/> contra el análisis de un estudio.</summary>
public sealed class ResultadoRegla
{
    public ResultadoRegla(
        ReglaNorma regla, double valorEvaluado, double limite, Veredicto veredicto, double holgura)
    {
        Regla = regla;
        ValorEvaluado = valorEvaluado;
        Limite = limite;
        Veredicto = veredicto;
        Holgura = holgura;
    }

    public ReglaNorma Regla { get; }

    /// <summary>Valor que se comparó (medida cruda o desviación respecto al objetivo, mm).</summary>
    public double ValorEvaluado { get; }

    /// <summary>Límite calculado para el espesor del estudio (mm).</summary>
    public double Limite { get; }

    public Veredicto Veredicto { get; }

    /// <summary>
    /// Holgura con signo respecto al límite (mm): positiva = del lado seguro.
    /// Maximo ⇒ límite − valor; Minimo ⇒ valor − límite.
    /// </summary>
    public double Holgura { get; }

    /// <summary>
    /// Severidad normalizada = valor / límite. &gt;1 indica violación; sirve para ordenar
    /// qué criterio está más comprometido (normalización a tolerancia a nivel de regla).
    /// </summary>
    public double Severidad => Limite != 0 ? ValorEvaluado / Limite : double.NaN;
}
