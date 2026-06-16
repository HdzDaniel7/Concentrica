using Soldadura.Core.Modelo;
using Soldadura.Core.Persistencia;

namespace Soldadura.Tests;

public class EstudioRepositorioTests : IDisposable
{
    private readonly string _raiz;
    private readonly EstudioRepositorio _repo = new();

    public EstudioRepositorioTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "sold-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raiz);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, recursive: true);
    }

    private static Estudio EstudioEjemplo(string idPieza = "P-100", int puesta = 1)
    {
        var e = new Estudio
        {
            IdPieza = idPieza,
            NumeroPuesta = puesta,
            Fecha = new DateTime(2026, 6, 14),
            ZonaPieza = "Brida A",
            Objetivo = new GeometriaObjetivo { ProfundidadObjetivo = 1.0, Espesor = 3.0 },
            Especificaciones = new Especificaciones { Nombre = "Plano 42", DescentradoMaximo = 0.15 }
        };
        e.Muestras.Add(new Muestra
        {
            NumeroMuestra = 1, AnguloOPosicion = 0,
            DistanciaBordeCercano = 4.5, DistanciaBordeLejano = 5.5, Profundidad = 1.2
        });
        return e;
    }

    [Fact]
    public void GuardarYCargar_RoundTrip()
    {
        var original = EstudioEjemplo();
        string carpeta = _repo.Guardar(_raiz, original);

        Assert.True(File.Exists(Path.Combine(carpeta, EstudioRepositorio.ArchivoDatos)));
        Assert.True(Directory.Exists(Path.Combine(carpeta, EstudioRepositorio.CarpetaImagenes)));
        Assert.True(Directory.Exists(Path.Combine(carpeta, EstudioRepositorio.CarpetaOverlays)));

        var cargado = _repo.Cargar(carpeta);
        Assert.Equal(original.IdPieza, cargado.IdPieza);
        Assert.Equal(original.NumeroPuesta, cargado.NumeroPuesta);
        Assert.Equal("Brida A", cargado.ZonaPieza);
        Assert.Single(cargado.Muestras);
        Assert.Equal(1.0, cargado.Muestras[0].AnchoCordon, 9);   // derivado se recalcula al cargar
        Assert.Equal(5.0, cargado.Muestras[0].PosicionCentral, 9);
        Assert.NotNull(cargado.Objetivo);
        Assert.Equal(3.0, cargado.Objetivo!.Espesor, 9);
        Assert.Equal("Plano 42", cargado.Especificaciones.Nombre);
        Assert.Equal(0.15, cargado.Especificaciones.DescentradoMaximo);
    }

    [Fact]
    public void GuardarYCargar_AjusteAplicado_RoundTrip()
    {
        var original = EstudioEjemplo();
        original.AjusteAplicado = new AjusteAplicado { X = -0.12, Z = 0.30 }; // Y queda null

        string carpeta = _repo.Guardar(_raiz, original);
        var cargado = _repo.Cargar(carpeta);

        Assert.NotNull(cargado.AjusteAplicado);
        Assert.Equal(-0.12, cargado.AjusteAplicado!.X);
        Assert.Null(cargado.AjusteAplicado.Y);
        Assert.Equal(0.30, cargado.AjusteAplicado.Z);
    }

    [Fact]
    public void Cargar_SinAjusteAplicado_QuedaNull()
    {
        string carpeta = _repo.Guardar(_raiz, EstudioEjemplo());
        var cargado = _repo.Cargar(carpeta);
        Assert.Null(cargado.AjusteAplicado);
    }

    [Fact]
    public void RutaEstudio_SigueElEsquemaDeCarpetas()
    {
        var e = EstudioEjemplo("P-100", 2);
        string ruta = _repo.RutaEstudio(_raiz, e);

        Assert.EndsWith(Path.Combine("P-100", "Puesta2_2026-06-14"), ruta);
    }

    [Fact]
    public void Sanear_QuitaCaracteresInvalidos()
    {
        string s = EstudioRepositorio.Sanear("P/100:A*?");
        Assert.DoesNotContain('/', s);
        Assert.DoesNotContain(':', s);
        Assert.DoesNotContain('*', s);
    }

    [Fact]
    public void Listar_DevuelveResumenesOrdenadosPorFecha()
    {
        var viejo = EstudioEjemplo("P-1", 1);
        viejo.Fecha = new DateTime(2026, 1, 1);
        var nuevo = EstudioEjemplo("P-2", 1);
        nuevo.Fecha = new DateTime(2026, 6, 1);
        _repo.Guardar(_raiz, viejo);
        _repo.Guardar(_raiz, nuevo);

        var lista = _repo.Listar(_raiz);

        Assert.Equal(2, lista.Count);
        Assert.Equal("P-2", lista[0].IdPieza); // más reciente primero
        Assert.Equal(1, lista[0].NumeroMuestras);
        Assert.Equal("Brida A", lista[0].ZonaPieza);
    }

    [Fact]
    public void Listar_OmiteCarpetasSinDatosJsonValido()
    {
        _repo.Guardar(_raiz, EstudioEjemplo());
        // Carpeta basura con datos.json inválido.
        string basura = Path.Combine(_raiz, "P-X", "PuestaMala");
        Directory.CreateDirectory(basura);
        File.WriteAllText(Path.Combine(basura, EstudioRepositorio.ArchivoDatos), "{ no es valido");

        var lista = _repo.Listar(_raiz);

        Assert.Single(lista);
    }

    [Fact]
    public void Guardar_CopiaImagenAbsolutaYActualizaRutaRelativa()
    {
        string imgOrigen = Path.Combine(_raiz, "captura.png");
        File.WriteAllBytes(imgOrigen, [0x89, 0x50, 0x4E, 0x47]); // cabecera PNG mínima

        var estudio = EstudioEjemplo();
        estudio.Muestras[0].RutaImagen = imgOrigen;

        string carpeta = _repo.Guardar(_raiz, estudio);

        string rutaRelativaEsperada = Path.Combine(EstudioRepositorio.CarpetaImagenes, "captura.png");
        Assert.Equal(rutaRelativaEsperada, estudio.Muestras[0].RutaImagen);
        Assert.True(File.Exists(Path.Combine(carpeta, rutaRelativaEsperada)));

        var cargado = _repo.Cargar(carpeta);
        Assert.Equal(rutaRelativaEsperada, cargado.Muestras[0].RutaImagen);
    }
}
