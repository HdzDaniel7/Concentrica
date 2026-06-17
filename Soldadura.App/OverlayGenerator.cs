using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Soldadura.Core.Imagen;

namespace Soldadura.App;

/// <summary>
/// Genera un PNG con las marcas de medición superpuestas sobre la imagen del microscopio.
/// Dibuja la recta del datum (amarillo, discontinua) y la recta de superficie (verde),
/// ambas extendidas a los bordes de la imagen. Las dimensiones perpendiculares
/// (bordes→datum, fondo/corona→superficie) se dibujan con anotaciones en mm.
/// </summary>
internal static class OverlayGenerator
{
    private static readonly Typeface _fuente = new("Consolas");
    private const double TamTexto = 11;
    private const double PxPorDip = 1.25;

    /// <param name="colorMarcas">
    /// Si se indica, las cruces de los 8 puntos usan ese color (elegido por el usuario);
    /// las rectas y cotas conservan sus colores semánticos. null = colores por tipo.
    /// </param>
    public static void Generar(string rutaImagen, MarcasMedicion marcas, string rutaSalida,
        Color? colorMarcas = null)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.UriSource = new Uri(rutaImagen, UriKind.Absolute);
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.EndInit();
        bmp.Freeze();

        double w = bmp.PixelWidth, h = bmp.PixelHeight;
        double esc = marcas.EscalaMmPorPixel;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(bmp, new Rect(0, 0, w, h));

            // ── Recta de datum extendida (amarillo, discontinua) ──────────────────────
            var penDatum = new Pen(Brushes.Yellow, 2) { DashStyle = DashStyles.Dash };
            if (marcas.DatumA is { } da && marcas.DatumB is { } db)
            {
                if (ExtenderLinea(P(da), P(db), w, h) is (var p1, var p2))
                    dc.DrawLine(penDatum, p1, p2);

                // Cotas de bordes al datum
                DibujarCota(dc, marcas.BordeCercano, P(da), P(db), Brushes.Orange, esc, "BC", w, h);
                DibujarCota(dc, marcas.BordeLejano, P(da), P(db), Brushes.Orange, esc, "BL", w, h);
            }

            // ── Recta de superficie extendida (verde) ────────────────────────────────
            var penSup = new Pen(Brushes.Lime, 2);
            if (marcas.SuperficieA is { } sa && marcas.SuperficieB is { } sb)
            {
                if (ExtenderLinea(P(sa), P(sb), w, h) is (var p1, var p2))
                    dc.DrawLine(penSup, p1, p2);

                // Profundidad (fondo → superficie)
                DibujarCota(dc, marcas.Fondo, P(sa), P(sb), Brushes.Cyan, esc, "P", w, h);

                // Exceso de cordón (corona → superficie)
                DibujarCota(dc, marcas.Corona, P(sa), P(sb), Brushes.Magenta, esc, "E", w, h);
            }

            // ── Cruces en los 8 puntos de marca ──────────────────────────────────────
            // Si el usuario eligió un color de marcas, todas las cruces lo usan; si no, color por tipo.
            Brush? elegido = colorMarcas is Color c ? new SolidColorBrush(c) : null;
            DibujarCruz(dc, marcas.DatumA, elegido ?? Brushes.Yellow);
            DibujarCruz(dc, marcas.DatumB, elegido ?? Brushes.Yellow);
            DibujarCruz(dc, marcas.SuperficieA, elegido ?? Brushes.Lime);
            DibujarCruz(dc, marcas.SuperficieB, elegido ?? Brushes.Lime);
            DibujarCruz(dc, marcas.BordeCercano, elegido ?? Brushes.Orange);
            DibujarCruz(dc, marcas.BordeLejano, elegido ?? Brushes.Orange);
            DibujarCruz(dc, marcas.Fondo, elegido ?? Brushes.Cyan);
            DibujarCruz(dc, marcas.Corona, elegido ?? Brushes.Magenta);
        }

        var rtb = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        Directory.CreateDirectory(Path.GetDirectoryName(rutaSalida)!);
        using var stream = File.Create(rutaSalida);
        encoder.Save(stream);
    }

    // ── Dibuja una cota perpendicular desde un punto de marca hasta una recta ─────────

    private static void DibujarCota(DrawingContext dc, Punto2D? marca, Point lineaA, Point lineaB,
        Brush color, double esc, string prefijo, double imgW, double imgH)
    {
        if (marca is not { } m) return;
        var pm = P(m);
        var pie = PiePerpendicular(pm, lineaA, lineaB);
        dc.DrawLine(new Pen(color, 1.5), pm, pie);
        DibujarTick(dc, pie, lineaA, lineaB, color);
        if (esc > 0)
        {
            double mm = Distancia(pm, pie) * esc;
            string etiqueta = $"{prefijo} {mm:F3} mm";
            DibujarEtiqueta(dc, Lerp(pm, pie, 0.5), etiqueta, color, imgW, imgH);
        }
    }

    // ── Helpers geométricos ──────────────────────────────────────────────────────────

    private static Point P(Punto2D p) => new(p.X, p.Y);

    private static Point PiePerpendicular(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len2 = dx * dx + dy * dy;
        if (len2 < 1e-12) return a;
        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
        return new Point(a.X + t * dx, a.Y + t * dy);
    }

    private static double Distancia(Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static Point Lerp(Point a, Point b, double t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    // Clip de la recta definida por a-b al rectángulo [0,w]×[0,h]
    private static (Point, Point)? ExtenderLinea(Point a, Point b, double w, double h)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return null;

        var ts = new List<double>(4);
        if (Math.Abs(dx) > 1e-9) { ts.Add(-a.X / dx); ts.Add((w - a.X) / dx); }
        if (Math.Abs(dy) > 1e-9) { ts.Add(-a.Y / dy); ts.Add((h - a.Y) / dy); }

        var dentro = ts
            .Select(t => new Point(a.X + t * dx, a.Y + t * dy))
            .Where(p => p.X >= -0.5 && p.X <= w + 0.5 && p.Y >= -0.5 && p.Y <= h + 0.5)
            .OrderBy(p => p.X + p.Y)
            .ToList();

        if (dentro.Count < 2) return null;
        return (dentro[0], dentro[^1]);
    }

    // Tick perpendicular a la recta A-B en el punto p (indica el pie de la perpendicular)
    private static void DibujarTick(DrawingContext dc, Point p, Point a, Point b, Brush color)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-12) return;
        double nx = -dy / len * 6, ny = dx / len * 6;
        dc.DrawLine(new Pen(color, 1.5), new Point(p.X + nx, p.Y + ny), new Point(p.X - nx, p.Y - ny));
    }

    private static void DibujarCruz(DrawingContext dc, Punto2D? p, Brush color)
    {
        if (p is not { } q) return;
        var pen = new Pen(color, 1.5);
        const double R = 6;
        dc.DrawLine(pen, new Point(q.X - R, q.Y), new Point(q.X + R, q.Y));
        dc.DrawLine(pen, new Point(q.X, q.Y - R), new Point(q.X, q.Y + R));
    }

    private static void DibujarEtiqueta(DrawingContext dc, Point pos, string texto,
        Brush color, double imgW, double imgH)
    {
        var ft = new FormattedText(texto, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _fuente, TamTexto, color, PxPorDip);
        double x = Math.Clamp(pos.X - ft.Width / 2, 2, imgW - ft.Width - 2);
        double y = Math.Clamp(pos.Y - ft.Height / 2, 2, imgH - ft.Height - 2);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), null,
            new Rect(x - 2, y - 1, ft.Width + 4, ft.Height + 2));
        dc.DrawText(ft, new Point(x, y));
    }
}
