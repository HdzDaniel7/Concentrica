using Soldadura.Core.Modelo;
using Soldadura.Core.Persistencia;

namespace Soldadura.Tests;

public class PerfilRepositorioTests : IDisposable
{
    private readonly string _raiz;
    private readonly PerfilRepositorio _repo = new();

    public PerfilRepositorioTests()
    {
        _raiz = Path.Combine(Path.GetTempPath(), "sold-perfil-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_raiz);
    }

    public void Dispose()
    {
        if (Directory.Exists(_raiz)) Directory.Delete(_raiz, recursive: true);
    }

    private static PlantillaPerfil PerfilEjemplo(string nombre = "Brida A rev1") => new()
    {
        Nombre = nombre,
        Descripcion = "Soldadura láser, cara interna",
        Tipo = TipoSoldadura.Circular,
        ModeloReferencia = ModeloReferencia.RadialDesdeCentro,
        GeometriaObjetivo = new GeometriaObjetivo
        {
            ProfundidadObjetivo = 1.2,
            Espesor = 3.0,
            AnchoObjetivo = 1.5
        },
        Especificaciones = new Especificaciones
        {
            Nombre = "Plano 12345 rev B",
            ProfundidadMinima = 0.85,
            ProfundidadMaxima = 1.40,
            DescentradoMaximo = 0.18,
            RunoutMaximo = 0.25,
            MargenRevision = 0.02
        },
        ZonasCatalogo = ["Brida A", "Brida B"],
        CoefFocoZ = 0.75
    };

    [Fact]
    public void GuardarCargar_RoundTrip_PreservaTodosLosCampos()
    {
        var original = PerfilEjemplo();
        _repo.Guardar(_raiz, original);

        var cargado = _repo.Cargar(_raiz, original.Nombre);

        Assert.Equal(original.Nombre, cargado.Nombre);
        Assert.Equal(original.Descripcion, cargado.Descripcion);
        Assert.Equal(original.Tipo, cargado.Tipo);
        Assert.Equal(ModeloReferencia.RadialDesdeCentro, cargado.ModeloReferencia);
        Assert.Equal(original.GeometriaObjetivo.ProfundidadObjetivo, cargado.GeometriaObjetivo.ProfundidadObjetivo);
        Assert.Equal(original.GeometriaObjetivo.AnchoObjetivo, cargado.GeometriaObjetivo.AnchoObjetivo);
        Assert.Equal(original.Especificaciones.ProfundidadMinima, cargado.Especificaciones.ProfundidadMinima);
        Assert.Equal(original.Especificaciones.ProfundidadMaxima, cargado.Especificaciones.ProfundidadMaxima);
        Assert.Equal(original.Especificaciones.DescentradoMaximo, cargado.Especificaciones.DescentradoMaximo);
        Assert.Equal(original.Especificaciones.RunoutMaximo, cargado.Especificaciones.RunoutMaximo);
        Assert.Equal(original.CoefFocoZ, cargado.CoefFocoZ);
        Assert.Equal(original.ZonasCatalogo, cargado.ZonasCatalogo);
    }

    [Fact]
    public void Guardar_CreaCarpetaPerfilesSoldadura()
    {
        _repo.Guardar(_raiz, PerfilEjemplo());

        string carpeta = Path.Combine(_raiz, PerfilRepositorio.NombreCarpeta);
        Assert.True(Directory.Exists(carpeta));
    }

    [Fact]
    public void RutaArchivo_UsaNombreSaneado()
    {
        // Nombre con caracteres inválidos → el slug saneado reemplaza con '_'
        string nombre = "Perfil:A/B";
        string ruta = PerfilRepositorio.RutaArchivo(_raiz, nombre);
        string nombreArchivo = Path.GetFileNameWithoutExtension(ruta);

        Assert.DoesNotContain(':', nombreArchivo);
        Assert.DoesNotContain('/', nombreArchivo);
        Assert.EndsWith(".json", ruta);
    }

    [Fact]
    public void Listar_DevuelveNombresOrdenadosAlfa()
    {
        _repo.Guardar(_raiz, PerfilEjemplo("Zebra"));
        _repo.Guardar(_raiz, PerfilEjemplo("Alpha"));
        _repo.Guardar(_raiz, PerfilEjemplo("Medio"));

        var lista = _repo.Listar(_raiz);

        Assert.Equal(["Alpha", "Medio", "Zebra"], lista);
    }

    [Fact]
    public void Listar_SinCarpeta_DevuelveVacio()
    {
        var lista = _repo.Listar(_raiz); // carpeta perfiles-soldadura aún no existe
        Assert.Empty(lista);
    }

    [Fact]
    public void Eliminar_BorraArchivo_YaNoAparece()
    {
        var perfil = PerfilEjemplo("Temporal");
        _repo.Guardar(_raiz, perfil);
        Assert.Single(_repo.Listar(_raiz));

        _repo.Eliminar(_raiz, perfil.Nombre);

        Assert.Empty(_repo.Listar(_raiz));
    }

    [Fact]
    public void Eliminar_ArchivoInexistente_NoLanza()
    {
        // No debe lanzar aunque el archivo no exista
        var ex = Record.Exception(() => _repo.Eliminar(_raiz, "NoExiste"));
        Assert.Null(ex);
    }

    [Fact]
    public void Guardar_SobreescribePerfilExistente()
    {
        var perfil = PerfilEjemplo("Mismo nombre");
        _repo.Guardar(_raiz, perfil);

        perfil.GeometriaObjetivo.ProfundidadObjetivo = 2.5;
        _repo.Guardar(_raiz, perfil);

        var cargado = _repo.Cargar(_raiz, perfil.Nombre);
        Assert.Equal(2.5, cargado.GeometriaObjetivo.ProfundidadObjetivo);
        Assert.Single(_repo.Listar(_raiz)); // no duplica
    }
}
