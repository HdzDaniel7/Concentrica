using System.Text.Json;
using System.Text.Json.Serialization;
using Soldadura.Core.Modelo;

namespace Soldadura.Core.Persistencia;

/// <summary>
/// Catálogo de plantillas de perfil persistidas en &lt;Raíz&gt;/perfiles-soldadura/&lt;slug&gt;.json.
/// Una plantilla es independiente de los estudios: es una PLANTILLA de configuración reutilizable.
/// </summary>
public sealed class PerfilRepositorio
{
    public const string NombreCarpeta = "perfiles-soldadura";

    private static readonly JsonSerializerOptions _opciones = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Ruta del archivo JSON de una plantilla dentro de la raíz dada.</summary>
    public static string RutaArchivo(string raiz, string nombre) =>
        Path.Combine(raiz, NombreCarpeta, EstudioRepositorio.Sanear(nombre) + ".json");

    /// <summary>
    /// Guarda (o sobreescribe) la plantilla. El nombre del archivo se deriva del
    /// <see cref="PlantillaPerfil.Nombre"/> saneado; si dos nombres producen el mismo slug
    /// se sobreescriben mutuamente — el técnico debe usar nombres distintos.
    /// </summary>
    public void Guardar(string raiz, PlantillaPerfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        string directorio = Path.Combine(raiz, NombreCarpeta);
        Directory.CreateDirectory(directorio);
        File.WriteAllText(RutaArchivo(raiz, perfil.Nombre),
                          JsonSerializer.Serialize(perfil, _opciones));
    }

    /// <summary>Carga la plantilla cuyo nombre saneado coincide con el parámetro dado.</summary>
    public PlantillaPerfil Cargar(string raiz, string nombre)
    {
        string ruta = RutaArchivo(raiz, nombre);
        var perfil = JsonSerializer.Deserialize<PlantillaPerfil>(File.ReadAllText(ruta), _opciones);
        return perfil ?? throw new InvalidDataException($"El archivo de perfil estaba vacío: {ruta}");
    }

    /// <summary>
    /// Lista los nombres de las plantillas disponibles (campo Nombre del JSON), ordenados
    /// alfabéticamente. Los archivos con JSON inválido se omiten en silencio.
    /// </summary>
    public IReadOnlyList<string> Listar(string raiz)
    {
        string directorio = Path.Combine(raiz, NombreCarpeta);
        if (!Directory.Exists(directorio))
            return [];

        var nombres = new List<string>();
        foreach (string archivo in Directory.EnumerateFiles(directorio, "*.json"))
        {
            try
            {
                var perfil = JsonSerializer.Deserialize<PlantillaPerfil>(
                    File.ReadAllText(archivo), _opciones);
                if (perfil?.Nombre is { Length: > 0 } n)
                    nombres.Add(n);
            }
            catch { }
        }
        nombres.Sort(StringComparer.CurrentCultureIgnoreCase);
        return nombres;
    }

    /// <summary>Elimina la plantilla cuyo nombre saneado coincide. No lanza si no existe.</summary>
    public void Eliminar(string raiz, string nombre)
    {
        string ruta = RutaArchivo(raiz, nombre);
        if (File.Exists(ruta))
            File.Delete(ruta);
    }
}
