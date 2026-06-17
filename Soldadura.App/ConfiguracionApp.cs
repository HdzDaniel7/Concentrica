using System.IO;
using System.Text.Json;

namespace Soldadura.App;

/// <summary>
/// Configuración persistida entre sesiones en %LOCALAPPDATA%\Concentrica\config.json.
/// Todo lo que se guarda aquí debe ser información de entorno del usuario (rutas, preferencias de UI),
/// nunca datos de estudio (esos van en el historial de carpetas).
/// </summary>
internal sealed class ConfiguracionApp
{
    public string RaizHistorial { get; set; } = "";

    /// <summary>true = tema oscuro; false = tema claro (default).</summary>
    public bool TemaOscuro { get; set; } = false;

    private static readonly string _rutaArchivo = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Concentrica", "config.json");

    public static ConfiguracionApp Cargar()
    {
        try
        {
            if (File.Exists(_rutaArchivo))
            {
                var json = File.ReadAllText(_rutaArchivo);
                return JsonSerializer.Deserialize<ConfiguracionApp>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    public void Guardar()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_rutaArchivo)!);
            File.WriteAllText(_rutaArchivo, JsonSerializer.Serialize(this,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
