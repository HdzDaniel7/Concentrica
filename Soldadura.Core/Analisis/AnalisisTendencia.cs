namespace Soldadura.Core.Analisis;

/// <summary>Un punto en la serie histórica: una puesta con su runout medido.</summary>
public record PuntoTendencia(
    int NumeroPuesta,
    DateTime Fecha,
    double Runout,
    double Descentrado,
    double FraccionCorregible);

/// <summary>Resultado del análisis de tendencia: regresión lineal de runout vs puesta.</summary>
public sealed class ResultadoTendencia
{
    public required IReadOnlyList<PuntoTendencia> Puntos { get; init; }

    /// <summary>Pendiente de la regresión runout vs NumeroPuesta (mm/puesta). Positivo = empeorando.</summary>
    public required double PendienteRunout { get; init; }

    /// <summary>Ordenada en el origen de la regresión.</summary>
    public required double OrdenadaRunout { get; init; }

    /// <summary>Primera puesta futura donde la recta supera RunoutMaximo. null si no aplica.</summary>
    public int? PuestaCruceLimite { get; init; }

    /// <summary>Mensaje honesto sobre la tendencia.</summary>
    public required string Mensaje { get; init; }
}

/// <summary>
/// Analiza la tendencia temporal de runout a partir de varias puestas de la misma pieza.
/// Solo incluye puestas con análisis radial (soldadura circular). Sin dependencias de UI.
/// </summary>
public static class MotorTendencia
{
    public static ResultadoTendencia Analizar(
        IReadOnlyList<(int puesta, DateTime fecha, ResultadoAnalisis r)> serie,
        double? runoutMaximo)
    {
        var puntos = serie
            .Where(s => s.r.Radial is not null)
            .OrderBy(s => s.puesta)
            .Select(s => new PuntoTendencia(
                s.puesta,
                s.fecha,
                s.r.Radial!.Runout,
                s.r.Radial.Ajuste.AmplitudDescentrado,
                s.r.Radial.FraccionCorregible))
            .ToList();

        if (puntos.Count < 2)
        {
            return new ResultadoTendencia
            {
                Puntos = puntos,
                PendienteRunout = 0,
                OrdenadaRunout = puntos.Count == 1 ? puntos[0].Runout : 0,
                PuestaCruceLimite = null,
                Mensaje = "Datos insuficientes para calcular tendencia (se necesitan ≥ 2 puestas con análisis radial)."
            };
        }

        var xs = puntos.Select(p => (double)p.NumeroPuesta).ToList();
        var ys = puntos.Select(p => p.Runout).ToList();
        var (pendiente, ordenada) = Estadistica.RegresionLineal(xs, ys);

        int? puestaCruce = null;
        string mensaje;

        if (pendiente <= 0)
        {
            mensaje = "Runout estable o mejorando entre puestas; sin proyección de cruce con el límite.";
        }
        else if (runoutMaximo is not double limMax)
        {
            mensaje = $"Runout con tendencia creciente ({pendiente:+0.000} mm/puesta); no se definió RunoutMaximo para proyectar cruce.";
        }
        else
        {
            double nCruce = (limMax - ordenada) / pendiente;
            int ultimaPuesta = puntos[^1].NumeroPuesta;
            int nCruceEntero = (int)Math.Ceiling(nCruce);

            if (nCruceEntero <= ultimaPuesta)
            {
                mensaje = $"Runout creciente {pendiente:+0.000} mm/puesta; el límite ({limMax:0.000} mm) ya se superó según la tendencia.";
            }
            else
            {
                puestaCruce = nCruceEntero;
                mensaje = $"Runout creciente {pendiente:+0.000} mm/puesta; se proyecta superar el límite ({limMax:0.000} mm) en la puesta {puestaCruce}.";
            }
        }

        return new ResultadoTendencia
        {
            Puntos = puntos,
            PendienteRunout = pendiente,
            OrdenadaRunout = ordenada,
            PuestaCruceLimite = puestaCruce,
            Mensaje = mensaje
        };
    }
}
