using System.Reflection;

namespace Soldadura.Core.Normas;

/// <summary>
/// Acceso a los rulesets que vienen embebidos con la app. Hoy solo el provisional de
/// ISO 13919-1:2019, cuyos valores son PLACEHOLDER (Verificada = false): deben cotejarse contra
/// la edición oficial antes de usarse en producción.
/// </summary>
public static class NormasProvisionales
{
    private const string RecursoIso =
        "Soldadura.Core.Normas.Datos.iso-13919-1-2019.provisional.json";

    /// <summary>Carga el ruleset provisional de ISO 13919-1:2019 embebido en el ensamblado.</summary>
    public static Norma Iso13919_1_2019()
    {
        var asm = typeof(NormasProvisionales).Assembly;
        using Stream? stream = asm.GetManifestResourceStream(RecursoIso)
            ?? throw new InvalidOperationException($"No se encontró el recurso embebido '{RecursoIso}'.");
        using var lector = new StreamReader(stream);
        return NormaJson.Cargar(lector.ReadToEnd());
    }
}
