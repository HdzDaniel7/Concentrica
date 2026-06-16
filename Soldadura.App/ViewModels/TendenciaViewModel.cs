using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Persistencia;

namespace Soldadura.App.ViewModels;

/// <summary>
/// VM del panel "Tendencia de runout": carga las puestas guardadas de la pieza actual,
/// calcula la regresión lineal de runout y proyecta cuándo se superará el límite.
/// </summary>
public partial class TendenciaViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly EstudioRepositorio _repo = new();

    public TendenciaViewModel(MainViewModel main) => _main = main;

    [ObservableProperty] private string _estado = "Pulsa 'Analizar tendencia' para calcular la serie de runout.";
    [ObservableProperty] private string _resumen = "";
    [ObservableProperty] private IReadOnlyList<PuntoTendencia> _puntos = [];
    [ObservableProperty] private double? _runoutMaximo;
    [ObservableProperty] private double _pendiente;
    [ObservableProperty] private double _ordenada;
    [ObservableProperty] private int? _puestaCruceLimite;

    [RelayCommand]
    private void AnalizarTendencia()
    {
        string raiz = _main.RaizHistorial;
        string idPieza = _main.IdPieza;

        if (string.IsNullOrWhiteSpace(raiz) || string.IsNullOrWhiteSpace(idPieza))
        {
            Estado = "Establece la carpeta raíz y el Id de pieza antes de analizar la tendencia.";
            return;
        }

        var resumenes = _repo.Listar(raiz).Where(r => r.IdPieza == idPieza).ToList();
        if (resumenes.Count == 0)
        {
            Estado = $"No se encontraron puestas guardadas para la pieza '{idPieza}'.";
            Puntos = [];
            Resumen = "";
            return;
        }

        var serie = new List<(int puesta, DateTime fecha, ResultadoAnalisis r)>();
        foreach (var res in resumenes)
        {
            try
            {
                var estudio = _repo.Cargar(res.Carpeta);
                if (estudio.Muestras.Count == 0) continue;

                var geo = estudio.Objetivo ?? new GeometriaObjetivo { ProfundidadObjetivo = 1, Espesor = 3 };
                var perfil = new PerfilSoldadura
                {
                    Nombre = "Tendencia",
                    Tipo = TipoSoldadura.Circular,
                    GeometriaObjetivo = geo,
                    Nivel = estudio.Especificaciones.Nivel,
                    ConfigMuestreo = new ConfigMuestreo
                    {
                        NumeroMuestras = estudio.Muestras.Count,
                        TieneMarcaCero = true
                    }
                };
                var analisis = MotorAnalisis.Analizar(estudio, perfil);
                serie.Add((estudio.NumeroPuesta, estudio.Fecha, analisis));
            }
            catch { /* omitir estudios corruptos */ }
        }

        double? limMax = _main.RunoutMaximo > 0 ? _main.RunoutMaximo : (double?)null;
        var resultado = MotorTendencia.Analizar(serie, limMax);

        Puntos = resultado.Puntos;
        Pendiente = resultado.PendienteRunout;
        Ordenada = resultado.OrdenadaRunout;
        PuestaCruceLimite = resultado.PuestaCruceLimite;
        RunoutMaximo = limMax;
        Resumen = resultado.Mensaje;
        Estado = $"Serie calculada: {resultado.Puntos.Count} puestas de '{idPieza}'.";
    }
}
