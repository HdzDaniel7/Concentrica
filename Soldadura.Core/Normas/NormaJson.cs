using System.Text.Json;
using System.Text.Json.Serialization;

namespace Soldadura.Core.Normas;

/// <summary>
/// Carga y guarda normas en JSON (formato declarativo elegido; consistente con la persistencia
/// de estudios en JSON). Enums serializados como texto para que el archivo sea legible y editable.
/// </summary>
public static class NormaJson
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Guardar(Norma norma) =>
        JsonSerializer.Serialize(norma, Opciones);

    public static Norma Cargar(string json) =>
        JsonSerializer.Deserialize<Norma>(json, Opciones)
        ?? throw new FormatException("JSON de norma inválido o vacío.");

    public static Norma CargarArchivo(string ruta) =>
        Cargar(File.ReadAllText(ruta));

    public static void GuardarArchivo(Norma norma, string ruta) =>
        File.WriteAllText(ruta, Guardar(norma));
}
