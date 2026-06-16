using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Soldadura.App.Visor3D;

/// <summary>
/// Paleta de colores para la vista 3D. Todos los colores viven aquí; cambiar uno solo afecta
/// a todos los elementos que lo usen. Agrupa los colores por rol semántico.
/// </summary>
internal static class Visor3DColores
{
    // ── Semáforo de veredicto ────────────────────────────────────────────────────
    /// <summary>Muestra dentro de tolerancia (holgura > 25 %).</summary>
    public static readonly Color Pasa       = Color.FromRgb(0x34, 0xC7, 0x59);  // verde
    /// <summary>Muestra en la banda de revisión (holgura 0–25 %).</summary>
    public static readonly Color Revisar    = Color.FromRgb(0xFF, 0xCC, 0x00);  // ámbar
    /// <summary>Muestra fuera de tolerancia.</summary>
    public static readonly Color NoPasa     = Color.FromRgb(0xFF, 0x3B, 0x30);  // rojo

    // ── Geometría de referencia ──────────────────────────────────────────────────
    /// <summary>Anillo objetivo (radio ideal).</summary>
    public static readonly Color AnilloRef  = Color.FromRgb(0x88, 0x88, 0x99);  // gris azulado
    /// <summary>Curva ajustada del 1er armónico (descentrado real).</summary>
    public static readonly Color CurvaAjustada = Color.FromRgb(0x5A, 0xC8, 0xFA); // azul claro

    // ── Centros ──────────────────────────────────────────────────────────────────
    /// <summary>Centro ideal (objetivo = origen de la escena).</summary>
    public static readonly Color CentroIdeal = Color.FromRgb(0x30, 0xB0, 0xFF); // azul
    /// <summary>Centro real de la soldadura medida.</summary>
    public static readonly Color CentroReal  = Color.FromRgb(0xFF, 0x95, 0x00); // naranja

    // ── Vector de ajuste ─────────────────────────────────────────────────────────
    /// <summary>Flecha de ajuste X/Y recomendado al robot.</summary>
    public static readonly Color VectorAjuste = Color.FromRgb(0xFF, 0xF5, 0x00); // amarillo

    // ── Corona (exceso de cordón) ────────────────────────────────────────────────
    /// <summary>Corona dentro de tolerancia (o sin tolerancia definida).</summary>
    public static readonly Color CoronaNeutral = Color.FromRgb(0x88, 0xCC, 0x88);  // verde suave
    // Reutiliza Pasa / Revisar / NoPasa para la corona cuando hay tolerancia definida.

    // ── Planos de referencia semitransparentes ────────────────────────────────────
    /// <summary>Plano de superficie (z=0): muy suave para no tapar los nuggets.</summary>
    public static readonly Color Superficie      = Color.FromRgb(0xCC, 0xCC, 0xDD);  // gris claro
    /// <summary>Opacidad del plano de superficie ~16 % (translúcido para ver los nuggets debajo).</summary>
    public const byte            OpacidadSuperficie = 40;
    /// <summary>Plano de profundidad objetivo (z=-profundidadObjetivo). Opacidad ~20 %.</summary>
    public const byte            OpacidadObjetivo   = 50;

    // ── Luces y fondo ────────────────────────────────────────────────────────────
    public static readonly Color LuzAmbiente    = Color.FromRgb(0x60, 0x60, 0x60);
    public static readonly Color LuzDireccional = Colors.White;
    /// <summary>Luz de relleno secundaria para dar volumen sin quemar.</summary>
    public static readonly Color LuzRelleno     = Color.FromRgb(0x55, 0x60, 0x70);
    public static readonly Color Fondo          = Color.FromRgb(0x1C, 0x1C, 0x2A);

    // ── Fábricas de materiales ───────────────────────────────────────────────────

    /// <summary>Material difuso con opacidad (alfa 0–255).</summary>
    public static Material DifusoAlfa(Color c, byte alfa)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alfa, c.R, c.G, c.B));
        brush.Freeze();
        var mat = new DiffuseMaterial(brush);
        mat.Freeze();
        return mat;
    }

    /// <summary>Material difuso + especular ligero. Opacidad 1 por defecto.</summary>
    public static Material Difuso(Color c, double opacidad = 1.0)
    {
        var brush = new SolidColorBrush(c) { Opacity = opacidad };
        brush.Freeze();
        var mat = new DiffuseMaterial(brush);
        mat.Freeze();
        return mat;
    }

    /// <summary>Material combinado difuso + emisivo (resalta sin depender de la luz).</summary>
    public static Material DifusoEmisor(Color c)
    {
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        var grp = new MaterialGroup();
        grp.Children.Add(new DiffuseMaterial(brush));
        grp.Children.Add(new EmissiveMaterial(new SolidColorBrush(
            Color.FromRgb((byte)(c.R / 3), (byte)(c.G / 3), (byte)(c.B / 3)))));
        grp.Freeze();
        return grp;
    }

    /// <summary>
    /// Material más brillante (emisivo fuerte + reflejo especular moderado) para la cúpula de
    /// corona: visualmente distinguible del cuerpo sin saturar.
    /// </summary>
    public static Material DifusoEmisorFuerte(Color c)
    {
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        var grp = new MaterialGroup();
        grp.Children.Add(new DiffuseMaterial(brush));
        grp.Children.Add(new EmissiveMaterial(new SolidColorBrush(
            Color.FromRgb((byte)(c.R * 2 / 3), (byte)(c.G * 2 / 3), (byte)(c.B * 2 / 3)))));
        grp.Children.Add(new SpecularMaterial(new SolidColorBrush(Colors.White), 25)); // SpecularPower moderado
        grp.Freeze();
        return grp;
    }
}
