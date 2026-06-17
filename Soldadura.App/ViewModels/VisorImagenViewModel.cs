using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Soldadura.Core.Imagen;
using Soldadura.Core.Modelo;
using PerfilMicroscopioModel = Soldadura.Core.Imagen.PerfilMicroscopio;

namespace Soldadura.App.ViewModels;

/// <summary>Marca de medición que se puede fijar con un clic, en orden.</summary>
public enum Herramienta
{
    DatumA,
    DatumB,
    BordeCercano,
    BordeLejano,
    SuperficieA,
    SuperficieB,
    Fondo,
    Corona
}

/// <summary>Color de los marcadores de medición sobre la imagen (elegible por el usuario).</summary>
public enum ColorMarca { Blanco, Verde, Amarillo, Rojo, Azul, Negro }

/// <summary>Mapea cada <see cref="ColorMarca"/> a un color visible para dibujar las marcas.</summary>
public static class ColoresMarca
{
    public static Color Media(ColorMarca c) => c switch
    {
        ColorMarca.Blanco   => Colors.White,
        ColorMarca.Verde    => Color.FromRgb(0x2E, 0xCC, 0x40),
        ColorMarca.Amarillo => Colors.Yellow,
        ColorMarca.Rojo     => Color.FromRgb(0xFF, 0x41, 0x36),
        ColorMarca.Azul     => Color.FromRgb(0x1E, 0x90, 0xFF),
        ColorMarca.Negro    => Colors.Black,
        _                   => Colors.Yellow
    };
}

/// <summary>Un marcador para el overlay: etiqueta + posición en píxeles de la imagen.</summary>
public sealed class MarcaVisual
{
    public required string Etiqueta { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
}

/// <summary>
/// Visor de imágenes del microscopio + MedicionEnPantalla (fase 3, opcional).
/// Elige carpeta, muestra miniaturas, deja marcar datum/bordes/superficie/fondo/corona con clics y
/// crea o actualiza una muestra del estudio con las medidas (y las coordenadas en píxeles).
/// </summary>
public partial class VisorImagenViewModel : ObservableObject
{
    private static readonly string[] Extensiones = { ".png", ".jpg", ".jpeg", ".tif", ".tiff" };

    private readonly MainViewModel _main;
    private MarcasMedicion _marcas = new();

    public VisorImagenViewModel(MainViewModel main)
    {
        _main = main;
        _marcas.EscalaMmPorPixel = EscalaMmPorPixel;
    }

    [ObservableProperty] private string _carpetaImagenes = "";
    [ObservableProperty] private string? _rutaSeleccionada;

    // --- Microscopio / escala (calibración por defecto del ejemplo: 230 px = 1 mm) ---
    [ObservableProperty] private string _perfilMicroscopio = "Nikon";
    [ObservableProperty] private string _objetivo = "";
    [ObservableProperty] private double _escalaMmPorPixel = 1.0 / 230.0;
    [ObservableProperty] private double _calibPixeles = 230;
    [ObservableProperty] private double _calibMm = 1.0;

    /// <summary>Ángulo/posición del corte para la muestra que se cree desde la imagen.</summary>
    [ObservableProperty] private double _angulo;

    [ObservableProperty] private Herramienta _herramienta = Herramienta.DatumA;
    [ObservableProperty] private string _estado = "Elige una carpeta de imágenes.";
    [ObservableProperty] private string _medidasPreview = "";

    /// <summary>Color elegido por el usuario para los marcadores de medición sobre la imagen.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PincelMarca))]
    private ColorMarca _colorMarca = ColorMarca.Amarillo;

    /// <summary>Brocha del color de marca seleccionado (para el overlay en pantalla).</summary>
    public Brush PincelMarca => new SolidColorBrush(ColoresMarca.Media(ColorMarca));

    /// <summary>Color de marca seleccionado (para el overlay PNG que se guarda).</summary>
    public Color ColorMarcaMedia => ColoresMarca.Media(ColorMarca);

    partial void OnColorMarcaChanged(ColorMarca value) => _main.PersistirColorMarca(value.ToString());

    [ObservableProperty] private PerfilMicroscopioModel? _perfilSeleccionado;

    partial void OnPerfilSeleccionadoChanged(PerfilMicroscopioModel? value) => AplicarPerfil(value);

    public ObservableCollection<string> Miniaturas { get; } = new();
    public ObservableCollection<MarcaVisual> Marcas { get; } = new();
    public ObservableCollection<PerfilMicroscopioModel> CatalogoPerfiles { get; } = new();

    [RelayCommand]
    private void ElegirCarpeta()
    {
        var dlg = new OpenFolderDialog { Title = "Elige la carpeta de imágenes del microscopio" };
        if (dlg.ShowDialog() != true) return;

        CarpetaImagenes = dlg.FolderName;
        Miniaturas.Clear();
        foreach (string f in Directory.EnumerateFiles(CarpetaImagenes)
                     .Where(f => Extensiones.Contains(Path.GetExtension(f).ToLowerInvariant()))
                     .OrderBy(f => f))
            Miniaturas.Add(f);

        Estado = Miniaturas.Count == 0
            ? "No hay imágenes (PNG/JPG/TIFF) en la carpeta."
            : $"{Miniaturas.Count} imágenes. Selecciona una y marca el datum.";
    }

    partial void OnRutaSeleccionadaChanged(string? value)
    {
        LimpiarMarcas();
        Estado = value is null ? "" : $"Imagen: {Path.GetFileName(value)}";
    }

    partial void OnEscalaMmPorPixelChanged(double value)
    {
        _marcas.EscalaMmPorPixel = value;
        RefrescarPreview();
    }

    [RelayCommand]
    private void Calibrar()
    {
        try
        {
            EscalaMmPorPixel = Core.Imagen.PerfilMicroscopio.CalibrarEscala(CalibPixeles, CalibMm);
            Estado = $"Escala calibrada: {EscalaMmPorPixel:0.######} mm/px.";
        }
        catch (ArgumentOutOfRangeException)
        {
            Estado = "Píxeles medidos debe ser > 0.";
        }
    }

    /// <summary>Fija la marca de la herramienta activa en el píxel dado y avanza a la siguiente.</summary>
    public void FijarPunto(double x, double y)
    {
        var p = new Punto2D(x, y);
        switch (Herramienta)
        {
            case Herramienta.DatumA: _marcas.DatumA = p; Herramienta = Herramienta.DatumB; break;
            case Herramienta.DatumB: _marcas.DatumB = p; Herramienta = Herramienta.BordeCercano; break;
            case Herramienta.BordeCercano: _marcas.BordeCercano = p; Herramienta = Herramienta.BordeLejano; break;
            case Herramienta.BordeLejano: _marcas.BordeLejano = p; Herramienta = Herramienta.SuperficieA; break;
            case Herramienta.SuperficieA: _marcas.SuperficieA = p; Herramienta = Herramienta.SuperficieB; break;
            case Herramienta.SuperficieB: _marcas.SuperficieB = p; Herramienta = Herramienta.Fondo; break;
            case Herramienta.Fondo: _marcas.Fondo = p; Herramienta = Herramienta.Corona; break;
            case Herramienta.Corona: _marcas.Corona = p; break;
        }
        _marcas.EscalaMmPorPixel = EscalaMmPorPixel;
        RefrescarMarcas();
        RefrescarPreview();
    }

    [RelayCommand]
    private void LimpiarMarcas()
    {
        _marcas = new MarcasMedicion { EscalaMmPorPixel = EscalaMmPorPixel };
        Herramienta = Herramienta.DatumA;
        RefrescarMarcas();
        MedidasPreview = "";
    }

    /// <summary>Siempre CREA una muestra nueva (un corte = una imagen) y la deja seleccionada.</summary>
    [RelayCommand(CanExecute = nameof(PuedeVolcar))]
    private void CrearMuestra()
    {
        if (MedicionActual() is not { } med)
        {
            Estado = "Marca datum A/B y ambos bordes antes de crear.";
            return;
        }

        var vm = new MuestraViewModel
        {
            NumeroMuestra = _main.Muestras.Count + 1,
            AnguloOPosicion = Angulo
        };
        Volcar(vm, med);
        _main.Muestras.Add(vm);
        _main.MuestraSeleccionada = vm;
        Estado = $"Muestra #{vm.NumeroMuestra} creada desde la imagen (ángulo {Angulo:0.#}).";
        LimpiarMarcas(); // listo para el siguiente corte sin duplicar por error
    }

    /// <summary>Vuelca la medición sobre la muestra ya seleccionada en la tabla (corrección).</summary>
    [RelayCommand(CanExecute = nameof(PuedeVolcar))]
    private void ActualizarMuestra()
    {
        if (_main.MuestraSeleccionada is not { } vm)
        {
            Estado = "No hay muestra seleccionada en la tabla. Usa 'Crear muestra'.";
            return;
        }
        if (MedicionActual() is not { } med)
        {
            Estado = "Marca datum A/B y ambos bordes antes de actualizar.";
            return;
        }
        Volcar(vm, med);
        Estado = $"Muestra #{vm.NumeroMuestra} actualizada desde la imagen.";
    }

    private MedicionEnPantalla? MedicionActual()
    {
        _marcas.EscalaMmPorPixel = EscalaMmPorPixel;
        return _marcas.AMedicion();
    }

    private void Volcar(MuestraViewModel vm, MedicionEnPantalla med)
    {
        var m = new Muestra();
        med.AplicarA(m);

        vm.ModoCaptura = ModoCaptura.MedicionEnPantalla;
        vm.EscalaMmPorPixel = EscalaMmPorPixel;
        vm.DistanciaBordeCercano = m.DistanciaBordeCercano;
        vm.DistanciaBordeLejano = m.DistanciaBordeLejano;
        if (med.ProfundidadMm is double) vm.Profundidad = m.Profundidad;
        if (med.ExcesoCordonMm is double) vm.ExcesoCordon = m.ExcesoCordon;
        vm.MarcasMedicion = m.MarcasMedicion;
        if (RutaSeleccionada is not null)
            vm.RutaImagen = RutaSeleccionada; // ruta absoluta; EstudioRepositorio la relativizará al guardar
    }

    private bool PuedeVolcar() => _marcas.TieneBase;

    private void RefrescarMarcas()
    {
        Marcas.Clear();
        Agregar("DatumA", _marcas.DatumA);
        Agregar("DatumB", _marcas.DatumB);
        Agregar("Cercano", _marcas.BordeCercano);
        Agregar("Lejano", _marcas.BordeLejano);
        Agregar("Sup.A", _marcas.SuperficieA);
        Agregar("Sup.B", _marcas.SuperficieB);
        Agregar("Fondo", _marcas.Fondo);
        Agregar("Corona", _marcas.Corona);
        CrearMuestraCommand.NotifyCanExecuteChanged();
        ActualizarMuestraCommand.NotifyCanExecuteChanged();

        void Agregar(string etiqueta, Punto2D? p)
        {
            if (p is { } q) Marcas.Add(new MarcaVisual { Etiqueta = etiqueta, X = q.X, Y = q.Y });
        }
    }

    private void RefrescarPreview()
    {
        _marcas.EscalaMmPorPixel = EscalaMmPorPixel;
        var med = _marcas.AMedicion();
        if (med is null)
        {
            MedidasPreview = "Marca datum A/B y ambos bordes para ver medidas.";
            return;
        }
        string prof = med.ProfundidadMm is double p ? $"{p:0.000} mm" : "(marca Sup.A/B + Fondo)";
        string exc = med.ExcesoCordonMm is double e ? $"{e:0.000} mm" : "(marca Sup.A/B + Corona)";
        MedidasPreview =
            $"Borde cercano: {med.DistanciaBordeCercanoMm:0.000} mm\n" +
            $"Borde lejano:  {med.DistanciaBordeLejanoMm:0.000} mm\n" +
            $"Profundidad:   {prof}\n" +
            $"Exceso cordón: {exc}";
    }

    // --- Catálogo de perfiles de microscopio ---

    [RelayCommand]
    private void GuardarPerfil()
    {
        string raiz = _main.RaizHistorial;
        if (string.IsNullOrWhiteSpace(raiz))
        {
            Estado = "Elige la carpeta raíz del historial antes de guardar el perfil.";
            return;
        }
        var perfil = new PerfilMicroscopioModel
        {
            Nombre = PerfilMicroscopio,
            Objetivo = Objetivo,
            EscalaMmPorPixel = EscalaMmPorPixel
        };
        string ruta = RutaCatalogo(raiz);
        var lista = LeerCatalogo(ruta);
        lista.RemoveAll(p => p.Nombre == perfil.Nombre && p.Objetivo == perfil.Objetivo);
        lista.Add(perfil);
        EscribirCatalogo(ruta, lista);
        RefrescarCatalogoEnUI(lista);
        Estado = $"Perfil '{perfil.Nombre} {perfil.Objetivo}' guardado en el catálogo.";
    }

    [RelayCommand]
    private void RefrescarCatalogo()
    {
        string raiz = _main.RaizHistorial;
        if (string.IsNullOrWhiteSpace(raiz)) return;
        RefrescarCatalogoEnUI(LeerCatalogo(RutaCatalogo(raiz)));
    }

    [RelayCommand]
    private void AplicarPerfil(PerfilMicroscopioModel? perfil)
    {
        if (perfil is null) return;
        PerfilMicroscopio = perfil.Nombre;
        Objetivo = perfil.Objetivo;
        EscalaMmPorPixel = perfil.EscalaMmPorPixel;
        Estado = $"Perfil '{perfil.Nombre} {perfil.Objetivo}': {EscalaMmPorPixel:0.######} mm/px.";
    }

    private static string RutaCatalogo(string raiz) =>
        Path.Combine(raiz, "perfiles-microscopio.json");

    private static List<PerfilMicroscopioModel> LeerCatalogo(string ruta)
    {
        if (!File.Exists(ruta)) return new();
        try { return JsonSerializer.Deserialize<List<PerfilMicroscopioModel>>(File.ReadAllText(ruta)) ?? new(); }
        catch { return new(); }
    }

    private static void EscribirCatalogo(string ruta, List<PerfilMicroscopioModel> lista) =>
        File.WriteAllText(ruta, JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }));

    private void RefrescarCatalogoEnUI(List<PerfilMicroscopioModel> lista)
    {
        CatalogoPerfiles.Clear();
        foreach (var p in lista) CatalogoPerfiles.Add(p);
    }
}
