using System.Text.Json;
using System.Text.Json.Serialization;
using Soldadura.Core.Modelo;

namespace Soldadura.Core.Persistencia;

/// <summary>Serialización de un estudio a/desde JSON (datos.json del estudio).</summary>
public static class EstudioJson
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Guardar(Estudio estudio) =>
        JsonSerializer.Serialize(estudio, Opciones);

    public static Estudio Cargar(string json) =>
        JsonSerializer.Deserialize<Estudio>(json, Opciones)
        ?? throw new FormatException("datos.json inválido o vacío.");
}
