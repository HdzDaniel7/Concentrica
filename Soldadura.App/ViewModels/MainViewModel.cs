using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Soldadura.App.Controls;
using Soldadura.App.Visor3D;
using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;
using Soldadura.Core.Persistencia;

namespace Soldadura.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly EstudioRepositorio _repo = new();
    private readonly PerfilRepositorio _perfilRepo = new();
    private readonly ConfiguracionApp _config;
    private DiagramaEjesWindow? _ventanaEjes;
    private ModelosReferenciaWindow? _ventanaModelos;
    private string? _carpetaActual;

    public MainViewModel()
    {
        _config   = ConfiguracionApp.Cargar();
        Visor     = new VisorImagenViewModel(this);
        Visor3D   = new Visor3DViewModel();
        Tendencia = new TendenciaViewModel(this);

        // Restaurar tema
        ThemeManager.Aplicar(_config.TemaOscuro);
        _temaOscuro = _config.TemaOscuro;

        if (!string.IsNullOrWhiteSpace(_config.RaizHistorial))
        {
            RaizHistorial = _config.RaizHistorial;
            RefrescarHistorial();
            RefrescarPerfiles();
        }
    }

    public VisorImagenViewModel Visor { get; }
    public Visor3DViewModel Visor3D { get; }
    public TendenciaViewModel Tendencia { get; }

    // --- Identidad del estudio ---
    [ObservableProperty] private string _idPieza = "";
    [ObservableProperty] private int _numeroPuesta = 1;
    [ObservableProperty] private DateTime _fecha = DateTime.Today;
    [ObservableProperty] private string _zonaPieza = "";

    // --- Geometría objetivo ---
    [ObservableProperty] private TipoSoldadura _tipo = TipoSoldadura.Circular;
    [ObservableProperty] private ModeloReferencia _modeloReferencia = ModeloReferencia.DatumPlanoExterno;
    [ObservableProperty] private double _profundidadObjetivo = 1.0;
    [ObservableProperty] private double _espesor = 3.0;
    [ObservableProperty] private bool _tieneMarcaCero = true;
    [ObservableProperty] private bool _evaluarAncho;
    [ObservableProperty] private double _anchoObjetivo = 1.0;

    // --- Especificaciones ---
    [ObservableProperty] private string _especNombre = "Especificación interna";
    [ObservableProperty] private string _especFuente = "";
    [ObservableProperty] private double _profundidadMinima = 0.80;
    [ObservableProperty] private bool _evaluarProfundidadMax;
    [ObservableProperty] private double _profundidadMaxima = 1.50;
    [ObservableProperty] private double _descentradoMaximo = 0.20;
    [ObservableProperty] private double _runoutMaximo = 0.30;
    [ObservableProperty] private double _toleranciaAncho = 0.20;
    [ObservableProperty] private bool _evaluarExceso;
    [ObservableProperty] private double _excesoCordonMaximo = 0.20;
    [ObservableProperty] private double _margenRevision = 0.03;

    // --- Ejes del robot ---
    [ObservableProperty] private string _nombreEjeX = "X";
    [ObservableProperty] private string _nombreEjeY = "Y";
    [ObservableProperty] private string _nombreEjeZ = "Z";
    [ObservableProperty] private double _anguloEjesGrados = 0;

    // --- Ajuste aplicado (robot) y coeficiente foco↔Z ---
    // El ajuste aplicado se guarda con el estudio (trazabilidad y aprendizaje); CoefFocoZ vive en
    // el perfil/plantilla y, una vez aprendido, hace que la recomendación en Z dé una magnitud.
    [ObservableProperty] private double? _ajusteXAplicado;
    [ObservableProperty] private double? _ajusteYAplicado;
    [ObservableProperty] private double? _ajusteZAplicado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CoefFocoZTexto))]
    private double? _coefFocoZ;

    public string CoefFocoZTexto => CoefFocoZ is double c
        ? $"CoefFocoZ del perfil: {c:0.000} mm/mm (la recomendación en Z dará magnitud)."
        : "CoefFocoZ del perfil: sin aprender (la recomendación en Z es solo direccional).";

    // --- Tema ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TexToTema))]
    private bool _temaOscuro;

    public string TexToTema => TemaOscuro ? "☀ Claro" : "🌙 Oscuro";

    // --- Perfiles guardables ---
    [ObservableProperty] private string? _perfilSeleccionado;
    [ObservableProperty] private string _nombrePerfil = "";

    // --- Historial ---
    [ObservableProperty] private string _raizHistorial = "";
    [ObservableProperty] private ResumenEstudio? _historialSeleccionado;

    // --- Salidas ---
    [ObservableProperty] private string _resultado = "Captura muestras y pulsa Analizar.";
    [ObservableProperty] private string _estado = "";
    [ObservableProperty] private MuestraViewModel? _muestraSeleccionada;

    // --- Badge de veredicto ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayVeredicto))]
    private string _veredictoTexto = "";

    [ObservableProperty] private Brush _veredictoFondo = Brushes.Transparent;
    [ObservableProperty] private Brush _veredictoTextColor = Brushes.Transparent;

    public bool HayVeredicto => !string.IsNullOrEmpty(VeredictoTexto);

    public ObservableCollection<MuestraViewModel> Muestras { get; } = new();
    public ObservableCollection<ResumenEstudio> Historial { get; } = new();
    public ObservableCollection<string> PerfilesDisponibles { get; } = new();

    // ── Muestras ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AgregarMuestra()
    {
        int n = Muestras.Count + 1;
        Muestras.Add(new MuestraViewModel
        {
            NumeroMuestra = n,
            AnguloOPosicion = Tipo == TipoSoldadura.Circular ? 360.0 * (n - 1) / Math.Max(1, n) : 0
        });
    }

    [RelayCommand]
    private void QuitarMuestra()
    {
        if (MuestraSeleccionada is null) return;
        Muestras.Remove(MuestraSeleccionada);
        Renumerar();
    }

    [RelayCommand]
    private void OrdenarPorNumero() => OrdenarMuestras(m => m.NumeroMuestra);

    [RelayCommand]
    private void OrdenarPorAngulo() => OrdenarMuestras(m => m.AnguloOPosicion);

    private void OrdenarMuestras(Func<MuestraViewModel, double> clave)
    {
        if (Muestras.Count < 2) return;
        var seleccion = MuestraSeleccionada;
        var ordenadas = Muestras.OrderBy(clave).ToList();
        Muestras.Clear();
        foreach (var m in ordenadas) Muestras.Add(m);
        MuestraSeleccionada = seleccion;
    }

    // ── Ejemplos ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void CargarEjemplo()
    {
        PrepararEjemploBase();
        Estado = "Ejemplo cargado (debería dar PASA). Análisis ejecutado.";
        Analizar();
    }

    [RelayCommand]
    private void CargarEjemploNoPasa()
    {
        PrepararEjemploBase();
        Muestras[0].DistanciaBordeCercano = 1.90;
        Muestras[0].DistanciaBordeLejano = 2.70;
        Estado = "Ejemplo cargado (debería dar NO PASA). Análisis ejecutado.";
        Analizar();
    }

    private void PrepararEjemploBase()
    {
        Tipo = TipoSoldadura.Circular;
        ModeloReferencia = ModeloReferencia.DatumPlanoExterno;
        IdPieza = "DEMO-1";
        NumeroPuesta = 1;
        Fecha = DateTime.Today;
        ZonaPieza = "Brida A";

        ProfundidadObjetivo = 2.6;
        Espesor = 5.0;
        TieneMarcaCero = true;
        EvaluarAncho = false;

        EspecNombre = "Especificación interna";
        EspecFuente = "Ejemplo";
        ProfundidadMinima = 2.6;
        EvaluarProfundidadMax = false;
        DescentradoMaximo = 0.20;
        RunoutMaximo = 0.30;
        MargenRevision = 0.03;

        AjusteXAplicado = null;
        AjusteYAplicado = null;
        AjusteZAplicado = null;

        Muestras.Clear();
        AgregarMuestraEjemplo(1, 0,   1.53, 2.39, 3.05);
        AgregarMuestraEjemplo(2, 90,  1.29, 2.29, 3.00);
        AgregarMuestraEjemplo(3, 180, 1.54, 2.34, 3.02);
        AgregarMuestraEjemplo(4, 270, 1.42, 2.20, 2.94);
    }

    private void AgregarMuestraEjemplo(int n, double angulo, double cercano, double lejano, double profundidad)
    {
        Muestras.Add(new MuestraViewModel
        {
            NumeroMuestra = n, AnguloOPosicion = angulo,
            DistanciaBordeCercano = cercano, DistanciaBordeLejano = lejano,
            Profundidad = profundidad, CalidadMedicion = CalidadMedicion.Metrologica
        });
    }

    // ── Perfiles guardables ───────────────────────────────────────────────────────

    [RelayCommand]
    private void GuardarPerfil()
    {
        if (string.IsNullOrWhiteSpace(RaizHistorial)) { Estado = "Elige primero la carpeta raíz."; return; }
        string nombre = NombrePerfil.Trim();
        if (nombre.Length == 0) { Estado = "Escribe un nombre para la plantilla."; return; }

        var plantilla = new PlantillaPerfil
        {
            Nombre = nombre,
            Tipo = Tipo,
            ModeloReferencia = this.ModeloReferencia,
            GeometriaObjetivo = ConstruirObjetivo(),
            Especificaciones = ConstruirEspecificaciones(),
            CoefFocoZ = CoefFocoZ,
            NombreEjeX = NombreEjeX,
            NombreEjeY = NombreEjeY,
            NombreEjeZ = NombreEjeZ,
            AnguloEjesGrados = AnguloEjesGrados
        };
        _perfilRepo.Guardar(RaizHistorial, plantilla);
        RefrescarPerfiles();
        PerfilSeleccionado = nombre;
        Estado = $"Plantilla «{nombre}» guardada.";
    }

    [RelayCommand]
    private void CargarPerfil()
    {
        if (PerfilSeleccionado is null) { Estado = "Selecciona una plantilla en la lista."; return; }
        try
        {
            var p = _perfilRepo.Cargar(RaizHistorial, PerfilSeleccionado);

            Tipo = p.Tipo;
            ModeloReferencia = p.ModeloReferencia;
            ProfundidadObjetivo = p.GeometriaObjetivo.ProfundidadObjetivo;
            Espesor = p.GeometriaObjetivo.Espesor;
            EvaluarAncho = p.GeometriaObjetivo.AnchoObjetivo.HasValue;
            if (p.GeometriaObjetivo.AnchoObjetivo is double ao) AnchoObjetivo = ao;

            var s = p.Especificaciones;
            EspecNombre = s.Nombre;
            EspecFuente = s.Fuente;
            if (s.ProfundidadMinima is double pm) ProfundidadMinima = pm;
            EvaluarProfundidadMax = s.ProfundidadMaxima.HasValue;
            if (s.ProfundidadMaxima is double pM) ProfundidadMaxima = pM;
            if (s.DescentradoMaximo is double dm) DescentradoMaximo = dm;
            if (s.RunoutMaximo is double rm) RunoutMaximo = rm;
            if (s.ToleranciaAncho is double ta) { EvaluarAncho = true; ToleranciaAncho = ta; }
            EvaluarExceso = s.ExcesoCordonMaximo.HasValue;
            if (s.ExcesoCordonMaximo is double ec) ExcesoCordonMaximo = ec;
            MargenRevision = s.MargenRevision;

            NombreEjeX = p.NombreEjeX;
            NombreEjeY = p.NombreEjeY;
            NombreEjeZ = p.NombreEjeZ;
            AnguloEjesGrados = p.AnguloEjesGrados;
            CoefFocoZ = p.CoefFocoZ;

            NombrePerfil = p.Nombre;
            Estado = $"Plantilla «{p.Nombre}» cargada.";
        }
        catch (Exception ex) { Estado = $"Error al cargar la plantilla: {ex.Message}"; }
    }

    [RelayCommand]
    private void EliminarPerfil()
    {
        if (PerfilSeleccionado is null) return;
        var res = MessageBox.Show($"¿Eliminar la plantilla «{PerfilSeleccionado}»?",
            "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;
        _perfilRepo.Eliminar(RaizHistorial, PerfilSeleccionado);
        RefrescarPerfiles();
        Estado = "Plantilla eliminada.";
    }

    private void RefrescarPerfiles()
    {
        PerfilesDisponibles.Clear();
        if (string.IsNullOrWhiteSpace(RaizHistorial)) return;
        foreach (var n in _perfilRepo.Listar(RaizHistorial)) PerfilesDisponibles.Add(n);
    }

    // ── Tema ─────────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void CambiarTema()
    {
        TemaOscuro = !TemaOscuro;
        ThemeManager.Aplicar(TemaOscuro);
        _config.TemaOscuro = TemaOscuro;
        _config.Guardar();
    }

    // ── Diagrama de ejes ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void VerDiagramaEjes()
    {
        if (_ventanaEjes is { IsVisible: true })
        {
            _ventanaEjes.Activate();
            return;
        }
        _ventanaEjes = new DiagramaEjesWindow { DataContext = this };
        _ventanaEjes.Show();
    }

    [RelayCommand]
    private void VerModelosReferencia()
    {
        if (_ventanaModelos is { IsVisible: true })
        {
            _ventanaModelos.Activate();
            return;
        }
        _ventanaModelos = new ModelosReferenciaWindow();
        _ventanaModelos.Show();
    }

    // ── Analizar ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Analizar()
    {
        if (Muestras.Count == 0) { Resultado = "No hay muestras que analizar."; return; }

        var geo = ConstruirObjetivo();
        var spec = ConstruirEspecificaciones();
        var perfil = ConstruirPerfil(geo);
        var estudio = ConstruirEstudio(geo, spec);

        var analisis = MotorAnalisis.Analizar(estudio, perfil);
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var veredicto = MotorNormas.Evaluar(norma, spec.Nivel, geo, analisis, analisis.CalidadGlobal, estudio.Muestras);

        Resultado = FormatearResultado(analisis, veredicto, NombreEjeX, NombreEjeY, NombreEjeZ, AnguloEjesGrados);
        ActualizarBadge(veredicto.VeredictoGlobal);
        Visor3D.Actualizar(estudio.Muestras, analisis, geo, spec);
    }

    // ── Guardar / Historial ───────────────────────────────────────────────────────

    [RelayCommand]
    private void ElegirRaiz()
    {
        var dlg = new OpenFolderDialog { Title = "Elige la carpeta raíz del historial" };
        if (dlg.ShowDialog() == true)
        {
            RaizHistorial = dlg.FolderName;
            _config.RaizHistorial = dlg.FolderName;
            _config.Guardar();
            RefrescarHistorial();
            RefrescarPerfiles();
        }
    }

    [RelayCommand]
    private void Guardar()
    {
        if (string.IsNullOrWhiteSpace(RaizHistorial)) { Estado = "Elige primero la carpeta raíz."; return; }
        if (string.IsNullOrWhiteSpace(IdPieza)) { Estado = "Falta el Id de pieza."; return; }

        var estudio = ConstruirEstudio(ConstruirObjetivo(), ConstruirEspecificaciones());
        string carpeta = _repo.Guardar(RaizHistorial, estudio);
        _carpetaActual = carpeta;

        bool actualizarJson = false;
        foreach (var m in estudio.Muestras)
        {
            if (m.RutaImagen is null || m.MarcasMedicion is null) continue;
            string rutaImagen = Path.Combine(carpeta, m.RutaImagen);
            if (!File.Exists(rutaImagen)) continue;
            string nombre = Path.GetFileNameWithoutExtension(rutaImagen) + "_overlay.png";
            string rutaOverlay = Path.Combine(carpeta, EstudioRepositorio.CarpetaOverlays, nombre);
            try
            {
                OverlayGenerator.Generar(rutaImagen, m.MarcasMedicion, rutaOverlay);
                m.RutaOverlay = Path.Combine(EstudioRepositorio.CarpetaOverlays, nombre);
                actualizarJson = true;
            }
            catch { }
        }
        if (actualizarJson) _repo.ActualizarDatos(carpeta, estudio);

        Estado = $"Guardado en {carpeta}";
        RefrescarHistorial();
    }

    // ── Exportar ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ExportarCsv()
    {
        if (Muestras.Count == 0) { Estado = "No hay muestras que exportar."; return; }
        var dlg = new SaveFileDialog { Title = "Exportar muestras a CSV", Filter = "CSV (*.csv)|*.csv", FileName = SugerirNombreExport("csv") };
        if (dlg.ShowDialog() != true) return;
        var estudio = ConstruirEstudio(ConstruirObjetivo(), ConstruirEspecificaciones());
        File.WriteAllText(dlg.FileName, ExportadorCsv.MuestrasACsv(estudio), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Estado = $"CSV exportado: {dlg.FileName}";
    }

    [RelayCommand]
    private void ExportarJson()
    {
        if (Muestras.Count == 0) { Estado = "No hay muestras que exportar."; return; }
        var dlg = new SaveFileDialog { Title = "Exportar estudio a JSON", Filter = "JSON (*.json)|*.json", FileName = SugerirNombreExport("json") };
        if (dlg.ShowDialog() != true) return;
        var estudio = ConstruirEstudio(ConstruirObjetivo(), ConstruirEspecificaciones());
        File.WriteAllText(dlg.FileName, EstudioJson.Guardar(estudio), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        Estado = $"JSON exportado: {dlg.FileName}";
    }

    [RelayCommand]
    private void ExportarPdf()
    {
        if (Muestras.Count == 0) { Estado = "No hay muestras que reportar."; return; }
        var dlg = new SaveFileDialog { Title = "Generar reporte PDF", Filter = "PDF (*.pdf)|*.pdf", FileName = SugerirNombreExport("pdf") };
        if (dlg.ShowDialog() != true) return;

        var geo = ConstruirObjetivo();
        var spec = ConstruirEspecificaciones();
        var perfil = ConstruirPerfil(geo);
        var estudio = ConstruirEstudio(geo, spec);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var veredicto = MotorNormas.Evaluar(norma, spec.Nivel, geo, analisis, analisis.CalidadGlobal, estudio.Muestras);

        byte[]? render3d = Visor3D.ObtenerSnapshot?.Invoke();
        ReportePdf.Generar(dlg.FileName, estudio, analisis, veredicto, Tipo == TipoSoldadura.Circular,
            _carpetaActual, render3d);
        Estado = $"PDF generado: {dlg.FileName}";
    }

    private string SugerirNombreExport(string ext)
    {
        string pieza = string.IsNullOrWhiteSpace(IdPieza) ? "estudio" : IdPieza;
        return $"{EstudioRepositorio.Sanear(pieza)}_Puesta{NumeroPuesta}_{Fecha:yyyy-MM-dd}.{ext}";
    }

    [RelayCommand]
    private void RefrescarHistorial()
    {
        Historial.Clear();
        if (string.IsNullOrWhiteSpace(RaizHistorial)) return;
        foreach (var r in _repo.Listar(RaizHistorial)) Historial.Add(r);
    }

    [RelayCommand]
    private void AbrirEstudio()
    {
        if (HistorialSeleccionado is null) return;
        var estudio = _repo.Cargar(HistorialSeleccionado.Carpeta);

        IdPieza = estudio.IdPieza;
        NumeroPuesta = estudio.NumeroPuesta;
        Fecha = estudio.Fecha;
        ZonaPieza = estudio.ZonaPieza ?? "";

        if (estudio.Objetivo is { } geo)
        {
            ProfundidadObjetivo = geo.ProfundidadObjetivo;
            Espesor = geo.Espesor;
            EvaluarAncho = geo.AnchoObjetivo.HasValue;
            if (geo.AnchoObjetivo is double ao) AnchoObjetivo = ao;
        }

        var spec = estudio.Especificaciones;
        EspecNombre = spec.Nombre;
        EspecFuente = spec.Fuente;
        if (spec.ProfundidadMinima is double pm) ProfundidadMinima = pm;
        EvaluarProfundidadMax = spec.ProfundidadMaxima.HasValue;
        if (spec.ProfundidadMaxima is double pM) ProfundidadMaxima = pM;
        if (spec.DescentradoMaximo is double dm) DescentradoMaximo = dm;
        if (spec.RunoutMaximo is double rm) RunoutMaximo = rm;
        if (spec.ToleranciaAncho is double ta) ToleranciaAncho = ta;
        EvaluarExceso = spec.ExcesoCordonMaximo.HasValue;
        if (spec.ExcesoCordonMaximo is double ec) ExcesoCordonMaximo = ec;
        MargenRevision = spec.MargenRevision;

        AjusteXAplicado = estudio.AjusteAplicado?.X;
        AjusteYAplicado = estudio.AjusteAplicado?.Y;
        AjusteZAplicado = estudio.AjusteAplicado?.Z;

        _carpetaActual = HistorialSeleccionado.Carpeta;
        Muestras.Clear();
        foreach (var m in estudio.Muestras) Muestras.Add(MuestraViewModel.Desde(m));
        Estado = $"Abierto: {estudio.IdPieza} / puesta {estudio.NumeroPuesta}";
    }

    // ── Aprendizaje del coeficiente foco↔Z ─────────────────────────────────────────

    [RelayCommand]
    private void AprenderCoefFocoZ()
    {
        if (string.IsNullOrWhiteSpace(RaizHistorial) || string.IsNullOrWhiteSpace(IdPieza))
        {
            Estado = "Establece la carpeta raíz y el Id de pieza antes de aprender el coeficiente.";
            return;
        }

        var resumenes = _repo.Listar(RaizHistorial).Where(r => r.IdPieza == IdPieza).ToList();
        var serie = new List<(int puesta, double? ajusteZ, double profundidadMedia)>();
        foreach (var res in resumenes)
        {
            try
            {
                var estudio = _repo.Cargar(res.Carpeta);
                if (estudio.Muestras.Count == 0) continue;
                double profMedia = estudio.Muestras.Average(m => m.Profundidad);
                serie.Add((estudio.NumeroPuesta, estudio.AjusteAplicado?.Z, profMedia));
            }
            catch { /* estudios corruptos: omitir */ }
        }

        var resultado = MotorAprendizajeFoco.Aprender(serie);
        if (resultado.CoefFocoZ is double c)
        {
            CoefFocoZ = c;
            Estado = $"{resultado.Mensaje} Aplicado al perfil; guarda la plantilla para conservarlo.";
        }
        else
        {
            Estado = resultado.Mensaje;
        }
    }

    // ── Helpers internos ─────────────────────────────────────────────────────────

    private void Renumerar()
    {
        for (int i = 0; i < Muestras.Count; i++) Muestras[i].NumeroMuestra = i + 1;
    }

    private GeometriaObjetivo ConstruirObjetivo() => new()
    {
        ProfundidadObjetivo = ProfundidadObjetivo,
        Espesor = Espesor,
        AnchoObjetivo = EvaluarAncho ? AnchoObjetivo : null
    };

    private Especificaciones ConstruirEspecificaciones() => new()
    {
        Nombre = EspecNombre, Fuente = EspecFuente, Nivel = NivelNorma.B,
        ProfundidadMinima = ProfundidadMinima,
        ProfundidadMaxima = EvaluarProfundidadMax ? ProfundidadMaxima : null,
        DescentradoMaximo = DescentradoMaximo, RunoutMaximo = RunoutMaximo,
        ToleranciaAncho = EvaluarAncho ? ToleranciaAncho : null,
        ExcesoCordonMaximo = EvaluarExceso ? ExcesoCordonMaximo : null,
        MargenRevision = MargenRevision
    };

    private PerfilSoldadura ConstruirPerfil(GeometriaObjetivo geo) => new()
    {
        Nombre = "Captura", Tipo = Tipo, ModeloReferencia = this.ModeloReferencia,
        GeometriaObjetivo = geo, Nivel = NivelNorma.B, CoefFocoZ = CoefFocoZ,
        ConfigMuestreo = new ConfigMuestreo { NumeroMuestras = Muestras.Count, TieneMarcaCero = TieneMarcaCero }
    };

    private AjusteAplicado? ConstruirAjusteAplicado()
    {
        var ajuste = new AjusteAplicado { X = AjusteXAplicado, Y = AjusteYAplicado, Z = AjusteZAplicado };
        return ajuste.Vacio ? null : ajuste;
    }

    private Estudio ConstruirEstudio(GeometriaObjetivo geo, Especificaciones spec)
    {
        var estudio = new Estudio
        {
            IdPieza = IdPieza, NumeroPuesta = NumeroPuesta, Fecha = Fecha,
            ZonaPieza = string.IsNullOrWhiteSpace(ZonaPieza) ? null : ZonaPieza,
            PerfilNombre = "Captura", Objetivo = geo, Especificaciones = spec,
            AjusteAplicado = ConstruirAjusteAplicado()
        };
        int orden = 1;
        foreach (var vm in Muestras) { var m = vm.AModelo(); m.Orden = orden++; estudio.Muestras.Add(m); }
        return estudio;
    }

    private void ActualizarBadge(Veredicto veredicto)
    {
        (VeredictoTexto, VeredictoFondo, VeredictoTextColor) = veredicto switch
        {
            Veredicto.Pasa    => ("✔  PASA",    new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)), Brushes.White),
            Veredicto.Revisar => ("⚠  REVISAR", new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)), Brushes.White),
            _                 => ("✖  NO PASA", new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B)), Brushes.White)
        };
    }

    private static string FormatearResultado(ResultadoAnalisis a, ResultadoNormas v,
        string ejeX, string ejeY, string ejeZ, double anguloGrados)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"VEREDICTO: {v.VeredictoGlobal}");
        sb.AppendLine($"Especificación: {v.NormaId}{(string.IsNullOrWhiteSpace(v.Edicion) ? "" : $" ({v.Edicion})")}");
        sb.AppendLine();

        sb.AppendLine("— Recomendación de ajuste —");

        bool hayCentrado = a.Radial is not null || a.Lineal is not null;
        if (hayCentrado)
        {
            // Rotar el vector X/Y al sistema de coordenadas del robot si hay ángulo configurado
            double ax = a.Recomendacion.AjusteX, ay = a.Recomendacion.AjusteY;
            if (Math.Abs(anguloGrados) > 0.01)
            {
                double rad = anguloGrados * Math.PI / 180;
                double cos = Math.Cos(rad), sin = Math.Sin(rad);
                (ax, ay) = (ax * cos + ay * sin, -ax * sin + ay * cos);
            }

            sb.AppendLine($"{ejeX} = {ax:+0.000;-0.000} mm");
            sb.AppendLine($"{ejeY} = {ay:+0.000;-0.000} mm");
        }
        else
        {
            sb.AppendLine("Centrado: no aplica (modelo sin datum externo).");
        }
        sb.AppendLine(a.Recomendacion.AjusteZ is double z
            ? $"{ejeZ} = {z:+0.000;-0.000} mm ({a.Recomendacion.DireccionZ})"
            : $"{ejeZ}: {a.Recomendacion.DireccionZ} (sin CoefFocoZ, solo dirección)");
        if (a.Recomendacion.SoloMecanico)
            sb.AppendLine("→ Predomina error mecánico / calidad insuficiente: no aplicar ajuste fino.");
        sb.AppendLine();

        if (a.Radial is not null)
        {
            sb.AppendLine("— Radial —");
            sb.AppendLine($"Descentrado: {a.Radial.Ajuste.AmplitudDescentrado:0.000} mm   Ovalidad: {a.Radial.Ajuste.Ovalidad:0.000} mm");
            sb.AppendLine($"Runout (TIR): {a.Radial.Runout:0.000} mm   Fracción corregible: {a.Radial.FraccionCorregible:P0}");
            sb.AppendLine();
        }

        sb.AppendLine("— Profundidad —");
        sb.AppendLine($"Media: {a.Axial.EstadisticaProfundidad.Media:0.000} ± {a.Axial.EstadisticaProfundidad.Sigma:0.000} mm");
        if (a.Axial.EstadisticaExceso.Max > 0)
            sb.AppendLine($"Exceso de cordón: peor corte {a.Axial.EstadisticaExceso.Max:0.000} mm (media {a.Axial.EstadisticaExceso.Media:0.000})");
        sb.AppendLine();

        if (v.MuestraMasCritica is { } mc)
            sb.AppendLine($"Muestra más crítica: #{mc.NumeroMuestra} ({mc.Medida}), severidad {mc.Severidad:0.00}× tolerancia");

        if (a.Avisos.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("— Avisos —");
            foreach (var aviso in a.Avisos) sb.AppendLine("• " + aviso);
        }

        return sb.ToString();
    }
}
