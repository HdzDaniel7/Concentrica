namespace Soldadura.Core.Persistencia;

/// <summary>
/// Fila del historial: lo mínimo para listar/filtrar estudios sin cargar todas las muestras.
/// </summary>
public sealed class ResumenEstudio
{
    public ResumenEstudio(
        string idPieza, int numeroPuesta, DateTime fecha, string? zonaPieza,
        int numeroMuestras, string carpeta)
    {
        IdPieza = idPieza;
        NumeroPuesta = numeroPuesta;
        Fecha = fecha;
        ZonaPieza = zonaPieza;
        NumeroMuestras = numeroMuestras;
        Carpeta = carpeta;
    }

    public string IdPieza { get; }

    public int NumeroPuesta { get; }

    public DateTime Fecha { get; }

    public string? ZonaPieza { get; }

    public int NumeroMuestras { get; }

    /// <summary>Carpeta del estudio en disco.</summary>
    public string Carpeta { get; }
}
