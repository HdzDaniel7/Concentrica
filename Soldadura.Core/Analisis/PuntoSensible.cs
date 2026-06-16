namespace Soldadura.Core.Analisis;

/// <summary>
/// La muestra con mayor desviación normalizada: el punto más crítico del estudio.
/// Por ahora la normalización es por σ de la serie; con el motor de normas (fase 4)
/// pasará a normalizarse contra la tolerancia de cada medida.
/// </summary>
public sealed class PuntoSensible
{
    public PuntoSensible(int numeroMuestra, double anguloOPosicion, string medida, double desviacionNormalizada)
    {
        NumeroMuestra = numeroMuestra;
        AnguloOPosicion = anguloOPosicion;
        Medida = medida;
        DesviacionNormalizada = desviacionNormalizada;
    }

    public int NumeroMuestra { get; }

    public double AnguloOPosicion { get; }

    /// <summary>Qué medida domina la desviación ("PosicionCentral" o "Profundidad").</summary>
    public string Medida { get; }

    /// <summary>Desviación en número de σ respecto al modelo/objetivo.</summary>
    public double DesviacionNormalizada { get; }
}
