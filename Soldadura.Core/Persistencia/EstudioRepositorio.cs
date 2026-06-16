using Soldadura.Core.Modelo;

namespace Soldadura.Core.Persistencia;

/// <summary>
/// Historial en carpetas locales (esquema cerrado, sección 10):
///   &lt;Raíz&gt;/&lt;idPieza&gt;/Puesta&lt;n&gt;_&lt;AAAA-MM-DD&gt;/ { datos.json, imagenes/, overlays/ }
/// La app elige la Raíz; aquí solo se resuelven rutas saneadas y se lee/escribe.
/// </summary>
public sealed class EstudioRepositorio
{
    public const string ArchivoDatos = "datos.json";
    public const string CarpetaImagenes = "imagenes";
    public const string CarpetaOverlays = "overlays";

    /// <summary>Ruta de la carpeta de un estudio dentro de la raíz (no la crea).</summary>
    public string RutaEstudio(string raiz, Estudio estudio)
    {
        ArgumentNullException.ThrowIfNull(estudio);
        return Path.Combine(raiz, Sanear(estudio.IdPieza), Sanear(estudio.NombreCarpetaSugerido));
    }

    /// <summary>
    /// Guarda el estudio en su carpeta (la crea junto a imagenes/ y overlays/) y deja
    /// <see cref="Estudio.Carpeta"/> apuntando a ella. Devuelve la ruta de la carpeta.
    /// Las muestras con <see cref="Muestra.RutaImagen"/> absoluta se copian a imagenes/ y la
    /// propiedad se actualiza a la ruta relativa.
    /// </summary>
    public string Guardar(string raiz, Estudio estudio)
    {
        string carpeta = RutaEstudio(raiz, estudio);
        Directory.CreateDirectory(carpeta);
        Directory.CreateDirectory(Path.Combine(carpeta, CarpetaImagenes));
        Directory.CreateDirectory(Path.Combine(carpeta, CarpetaOverlays));

        CopiarImagenes(carpeta, estudio.Muestras);

        estudio.Carpeta = carpeta;
        File.WriteAllText(Path.Combine(carpeta, ArchivoDatos), EstudioJson.Guardar(estudio));
        return carpeta;
    }

    /// <summary>Sobreescribe solo datos.json (para actualizar RutaOverlay después de generar los PNGs).</summary>
    public void ActualizarDatos(string carpeta, Estudio estudio)
    {
        estudio.Carpeta = carpeta;
        File.WriteAllText(Path.Combine(carpeta, ArchivoDatos), EstudioJson.Guardar(estudio));
    }

    private static void CopiarImagenes(string carpeta, IList<Muestra> muestras)
    {
        string carpetaImg = Path.Combine(carpeta, CarpetaImagenes);
        foreach (var m in muestras)
        {
            if (m.RutaImagen is not { } ruta || !Path.IsPathRooted(ruta) || !File.Exists(ruta))
                continue;
            string nombre = Path.GetFileName(ruta);
            string destino = Path.Combine(carpetaImg, nombre);
            if (!File.Exists(destino))
                File.Copy(ruta, destino);
            m.RutaImagen = Path.Combine(CarpetaImagenes, nombre);
        }
    }

    /// <summary>Carga el estudio cuyo datos.json está en la carpeta dada.</summary>
    public Estudio Cargar(string carpeta)
    {
        string ruta = Path.Combine(carpeta, ArchivoDatos);
        var estudio = EstudioJson.Cargar(File.ReadAllText(ruta));
        estudio.Carpeta = carpeta;
        return estudio;
    }

    /// <summary>
    /// Recorre la raíz y devuelve un resumen de cada estudio encontrado, ordenado por fecha
    /// descendente. Las carpetas sin datos.json válido se omiten.
    /// </summary>
    public IReadOnlyList<ResumenEstudio> Listar(string raiz)
    {
        var resumenes = new List<ResumenEstudio>();
        if (!Directory.Exists(raiz))
            return resumenes;

        foreach (string carpetaPieza in Directory.EnumerateDirectories(raiz))
        foreach (string carpetaEstudio in Directory.EnumerateDirectories(carpetaPieza))
        {
            string ruta = Path.Combine(carpetaEstudio, ArchivoDatos);
            if (!File.Exists(ruta)) continue;
            try
            {
                var e = EstudioJson.Cargar(File.ReadAllText(ruta));
                resumenes.Add(new ResumenEstudio(
                    e.IdPieza, e.NumeroPuesta, e.Fecha, e.ZonaPieza, e.Muestras.Count, carpetaEstudio));
            }
            catch (Exception ex) when (ex is FormatException or System.Text.Json.JsonException)
            {
                // datos.json corrupto: se omite del historial en vez de tumbar el listado.
            }
        }

        return resumenes.OrderByDescending(r => r.Fecha).ThenBy(r => r.IdPieza).ToList();
    }

    /// <summary>Reemplaza caracteres inválidos de nombre de archivo por '_'.</summary>
    public static string Sanear(string nombre)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var chars = nombre.Select(c => invalidos.Contains(c) ? '_' : c).ToArray();
        string limpio = new string(chars).Trim();
        return limpio.Length == 0 ? "_" : limpio;
    }
}
