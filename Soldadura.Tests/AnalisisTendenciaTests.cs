using Soldadura.Core.Analisis;

namespace Soldadura.Tests;

/// <summary>Tests de MotorTendencia: regresión de runout histórico, proyección de cruce y mensajes.</summary>
public class AnalisisTendenciaTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static readonly EstadisticaSerie _statVacia = new(0, 0, 0, 0);

    private static readonly Recomendacion _recoVacia =
        new(0, 0, null, DireccionZ.SinCambio, false, "");

    /// <summary>Construye un ResultadoAnalisis radial mínimo con el runout indicado.</summary>
    private static ResultadoAnalisis ConRunout(double runout, double descentrado = 0)
    {
        var ajuste = new AjusteArmonico(5.0,
        [
            new Armonico(1, descentrado, 0),
            new Armonico(2, 0, 0)
        ]);
        var radial = new ResultadoRadial(
            ajuste,
            runout,
            descentrado > 0 ? 0.9 : 0.0,
            _statVacia);

        return new ResultadoAnalisis
        {
            Radial = radial,
            Axial  = new ResultadoAxial(_statVacia, null, _statVacia),
            Recomendacion = _recoVacia
        };
    }

    private static (int, DateTime, ResultadoAnalisis) Punto(int puesta, double runout)
        => (puesta, new DateTime(2025, 1, puesta), ConRunout(runout));

    // ── Tests ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void DatosInsuficientes_MenosDeDosPuntos_MensajeInsuficiente()
    {
        // Solo una puesta → no se puede calcular tendencia.
        var serie = new[] { Punto(1, 0.10) };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: null);

        Assert.Single(res.Puntos);
        Assert.Equal(0, res.PendienteRunout);
        Assert.Null(res.PuestaCruceLimite);
        Assert.Contains("insuficiente", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TendenciaEstable_PendienteNegativa_MensajeEstable()
    {
        // Runout decreciente (bueno): la pendiente es negativa.
        var serie = new[] { Punto(1, 0.20), Punto(2, 0.18), Punto(3, 0.15) };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: 0.30);

        Assert.True(res.PendienteRunout < 0, "Pendiente debe ser negativa.");
        Assert.Null(res.PuestaCruceLimite);
        Assert.Contains("estable", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TendenciaCreciente_SinLimite_MensajeCrecienteSinProyeccion()
    {
        var serie = new[] { Punto(1, 0.10), Punto(2, 0.15), Punto(3, 0.22) };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: null);

        Assert.True(res.PendienteRunout > 0);
        Assert.Null(res.PuestaCruceLimite);
        Assert.Contains("no se definió", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TendenciaCreciente_ConLimite_ProyectaPuestaCruce()
    {
        // Runout sube ~0.05 mm por puesta; límite = 0.50 mm.
        var serie = new[]
        {
            Punto(1, 0.10), Punto(2, 0.15), Punto(3, 0.20),
            Punto(4, 0.25), Punto(5, 0.30)
        };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: 0.50);

        Assert.NotNull(res.PuestaCruceLimite);
        Assert.True(res.PuestaCruceLimite > 5, "La puesta de cruce debe ser posterior a la última medición.");
        Assert.Contains($"puesta {res.PuestaCruceLimite}", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LimiteYaSuperado_CruceAnteriorAUltimaPuesta_MensajeYaSuperado()
    {
        // Serie donde la tendencia ya supera el límite en las puestas existentes.
        var serie = new[] { Punto(1, 0.10), Punto(2, 0.50), Punto(3, 0.90) };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: 0.20);

        // La recta de regresión cruza 0.20 antes de la puesta 3 → ya superado.
        Assert.Null(res.PuestaCruceLimite);
        Assert.Contains("ya se superó", res.Mensaje, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FiltroLineal_PuestasLinearesExcluidas_SoloCirculares()
    {
        // Una entrada sin análisis radial (soldadura lineal → Radial=null) debe excluirse.
        var sinRadial = new ResultadoAnalisis
        {
            Radial = null,
            Axial  = new ResultadoAxial(_statVacia, null, _statVacia),
            Recomendacion = _recoVacia
        };

        var serie = new (int, DateTime, ResultadoAnalisis)[]
        {
            (1, new DateTime(2025,1,1), ConRunout(0.10)),
            (2, new DateTime(2025,1,2), sinRadial),        // lineal → excluida
            (3, new DateTime(2025,1,3), ConRunout(0.15)),
        };
        var res = MotorTendencia.Analizar(serie, runoutMaximo: null);

        // Solo 2 puntos circulares; el lineal se descarta.
        Assert.Equal(2, res.Puntos.Count);
        Assert.All(res.Puntos, p => Assert.True(p.NumeroPuesta != 2));
    }
}
