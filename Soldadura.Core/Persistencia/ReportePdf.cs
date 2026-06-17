using System.Globalization;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;

namespace Soldadura.Core.Persistencia;

/// <summary>
/// Genera el reporte PDF profesional de un estudio (secciones separadas: identificación,
/// veredicto, especificaciones, recomendación, resultados, detalle por criterio, muestras y avisos).
/// Solo salida; usa QuestPDF (licencia Community: gratuita para empresas con ingresos &lt; 1 M USD).
/// </summary>
public static class ReportePdf
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    // Paleta alineada con el semáforo de la vista 3D.
    private static readonly Color Tinta     = Color.FromHex("#1A237E"); // azul títulos
    private static readonly Color GrisSuave = Color.FromHex("#F2F3F7");
    private static readonly Color GrisBorde = Color.FromHex("#C9CDD6");

    public static void Generar(
        string ruta, Estudio estudio, ResultadoAnalisis analisis, ResultadoNormas veredicto, bool esCircular,
        string? carpetaEstudio = null, byte[]? render3d = null) =>
        Construir(estudio, analisis, veredicto, esCircular, carpetaEstudio, render3d).GeneratePdf(ruta);

    /// <summary>
    /// Construye el documento del reporte (sin volcarlo). Útil para generar PDF o imágenes de página.
    /// </summary>
    public static IDocument Construir(
        Estudio estudio, ResultadoAnalisis analisis, ResultadoNormas veredicto, bool esCircular,
        string? carpetaEstudio = null, byte[]? render3d = null)
    {
        ArgumentNullException.ThrowIfNull(estudio);
        ArgumentNullException.ThrowIfNull(analisis);
        ArgumentNullException.ThrowIfNull(veredicto);

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.6f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontSize(9.5f).FontColor(Colors.Grey.Darken4));

                page.Header().Element(c => Encabezado(c, estudio));
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(2);
                    BadgeVeredicto(col, veredicto);
                    Identificacion(col, estudio, analisis, esCircular);
                    EspecificacionesSeccion(col, estudio.Especificaciones);
                    RecomendacionSeccion(col, analisis);
                    Resultados(col, analisis);
                    DetallePorCriterio(col, veredicto);
                    MasCritico(col, veredicto);
                    TablaMuestras(col, estudio);
                    if (render3d is not null)
                        Vista3D(col, render3d);
                    if (carpetaEstudio is not null)
                        ImagenesMedicion(col, estudio, carpetaEstudio);
                    Avisos(col, analisis.Avisos);
                });
                page.Footer().Element(PieDePagina);
            });
        });
    }

    // ── Encabezado / pie ─────────────────────────────────────────────────────────

    private static void Encabezado(IContainer c, Estudio e)
    {
        c.BorderBottom(1.5f).BorderColor(Tinta).PaddingBottom(6).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Reporte de análisis de concentricidad de soldadura")
                    .FontSize(15).Bold().FontColor(Tinta);
                col.Item().Text($"Pieza {e.IdPieza}  ·  Puesta {e.NumeroPuesta}  ·  {e.Fecha:dd/MM/yyyy}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(150).AlignRight().Text(t =>
            {
                t.Span("Generado\n").FontColor(Colors.Grey.Darken1).FontSize(8);
                t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).Bold();
            });
        });
    }

    private static void PieDePagina(IContainer c)
    {
        c.BorderTop(0.8f).BorderColor(GrisBorde).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("Concéntrica — Analizador de concentricidad de soldadura")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(120).AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

    // ── Veredicto ─────────────────────────────────────────────────────────────────

    private static void BadgeVeredicto(ColumnDescriptor col, ResultadoNormas v)
    {
        var (bg, fg, _) = ColoresVeredicto(v.VeredictoGlobal);
        col.Item().PaddingTop(6).Background(bg).Border(1).BorderColor(fg).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("VEREDICTO GLOBAL").FontSize(8).Bold().FontColor(fg).LetterSpacing(0.05f);
                c.Item().Text(TextoVeredicto(v.VeredictoGlobal)).FontSize(20).Bold().FontColor(fg);
            });
            row.ConstantItem(260).AlignRight().AlignMiddle().Column(c =>
            {
                c.Item().AlignRight().Text(t =>
                {
                    t.Span("Especificación: ").FontColor(Colors.Grey.Darken2);
                    t.Span(string.IsNullOrWhiteSpace(v.NormaId) ? "—" : v.NormaId).Bold();
                });
                if (!string.IsNullOrWhiteSpace(v.Edicion))
                    c.Item().AlignRight().Text(v.Edicion).FontColor(Colors.Grey.Darken1).FontSize(8.5f);
                if (!v.Verificada)
                    c.Item().AlignRight().Text("⚠ Valores no cotejados con edición oficial")
                        .FontColor(Color.FromHex("#B8860B")).FontSize(8).Italic();
            });
        });
    }

    // ── Secciones ─────────────────────────────────────────────────────────────────

    private static void Identificacion(ColumnDescriptor col, Estudio e, ResultadoAnalisis a, bool esCircular)
    {
        Seccion(col, "1. Identificación del estudio", c =>
        {
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                FilaInfo(t, "Id de pieza", e.IdPieza);
                FilaInfo(t, "Número de puesta", e.NumeroPuesta.ToString(Inv));
                FilaInfo(t, "Fecha", e.Fecha.ToString("dd/MM/yyyy"));
                FilaInfo(t, "Zona de pieza", string.IsNullOrWhiteSpace(e.ZonaPieza) ? "—" : e.ZonaPieza!);
                FilaInfo(t, "Tipo de soldadura", esCircular ? "Circular" : "Lineal");
                FilaInfo(t, "Calidad global de medición", a.CalidadGlobal.ToString());
                if (e.Objetivo is { } g)
                {
                    FilaInfo(t, "Profundidad objetivo", $"{N(g.ProfundidadObjetivo)} mm");
                    FilaInfo(t, "Espesor (t)", $"{N(g.Espesor)} mm");
                    if (g.RadioObjetivo is double r) FilaInfo(t, "Radio objetivo", $"{N(r)} mm");
                    if (g.AnchoObjetivo is double an) FilaInfo(t, "Ancho objetivo", $"{N(an)} mm");
                }
            });
        });
    }

    private static void EspecificacionesSeccion(ColumnDescriptor col, Especificaciones s)
    {
        Seccion(col, "2. Especificaciones de aceptación (criterio interno)", c =>
        {
            if (!string.IsNullOrWhiteSpace(s.Nombre) || !string.IsNullOrWhiteSpace(s.Fuente))
                c.Item().PaddingBottom(3).Text(t =>
                {
                    t.Span(s.Nombre).Bold();
                    if (!string.IsNullOrWhiteSpace(s.Fuente)) t.Span($"   ·   Fuente: {s.Fuente}").FontColor(Colors.Grey.Darken1);
                });

            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d => { d.RelativeColumn(3); d.RelativeColumn(2); });
                EncabezadoTabla(t, "Criterio", "Límite");
                if (s.ProfundidadMinima is double pm) FilaTabla(t, "Penetración mínima (piso, corte peor)", $"{N(pm)} mm");
                if (s.ProfundidadMaxima is double pM) FilaTabla(t, "Penetración máxima (techo, corte peor)", $"{N(pM)} mm");
                if (s.DescentradoMaximo is double dm) FilaTabla(t, "Descentrado máximo", $"{N(dm)} mm");
                if (s.RunoutMaximo is double rm) FilaTabla(t, "Runout / TIR máximo", $"{N(rm)} mm");
                if (s.ToleranciaAncho is double ta) FilaTabla(t, "Ancho de cordón (desviación máx. ±)", $"{N(ta)} mm");
                if (s.ExcesoCordonMaximo is double ec) FilaTabla(t, "Exceso de cordón (peor corte)", $"{N(ec)} mm");
                FilaTabla(t, "Margen de revisión (±)", $"{N(s.MargenRevision)} mm");
            });
        });
    }

    private static void RecomendacionSeccion(ColumnDescriptor col, ResultadoAnalisis a)
    {
        var r = a.Recomendacion;
        bool hayCentrado = a.Radial is not null || a.Lineal is not null;
        Seccion(col, "3. Recomendación de ajuste del robot", c =>
        {
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); d.RelativeColumn(); });
                t.Cell().Element(CeldaAjuste).Column(x => Ajuste(x, "Ajuste X",
                    hayCentrado ? $"{r.AjusteX:+0.000;-0.000} mm" : "n/a"));
                t.Cell().Element(CeldaAjuste).Column(x => Ajuste(x, "Ajuste Y",
                    hayCentrado ? $"{r.AjusteY:+0.000;-0.000} mm" : "n/a"));
                t.Cell().Element(CeldaAjuste).Column(x => Ajuste(x, "Ajuste Z",
                    r.AjusteZ is double z ? $"{z:+0.000;-0.000} mm" : TextoDireccionZ(r.DireccionZ)));
            });
            if (!hayCentrado)
                c.Item().PaddingTop(4).Text("Centrado: no aplica (modelo sin datum externo).")
                    .FontColor(Tinta).Italic();
            c.Item().PaddingTop(4).Text(t =>
            {
                t.Span("Enfoque Z: ").Bold();
                t.Span(TextoDireccionZ(r.DireccionZ) + (r.AjusteZ is null ? "  (sin CoefFocoZ aprendido: solo dirección)" : ""));
            });
            if (r.SoloMecanico)
                c.Item().PaddingTop(4).Background(Color.FromHex("#FFF8E1")).Border(1)
                    .BorderColor(Color.FromHex("#FFCC00")).Padding(6)
                    .Text("Predomina error mecánico o la calidad de medición no permite ajuste fino: no aplicar offset de robot.")
                    .FontColor(Color.FromHex("#8A6D00"));
        });
    }

    private static void Resultados(ColumnDescriptor col, ResultadoAnalisis a)
    {
        Seccion(col, "4. Resultados del análisis", c =>
        {
            if (a.Radial is { } rad)
            {
                c.Item().Text("Radial (en el plano)").Bold().FontColor(Tinta).FontSize(10);
                c.Item().Table(t =>
                {
                    t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                    FilaInfo(t, "Descentrado (1er armónico)", $"{N(rad.Ajuste.AmplitudDescentrado)} mm");
                    FilaInfo(t, "Ovalidad (2º armónico)", $"{N(rad.Ajuste.Ovalidad)} mm");
                    FilaInfo(t, "Runout / TIR", $"{N(rad.Runout)} mm");
                    FilaInfo(t, "Fracción corregible", rad.FraccionCorregible.ToString("P0", Inv));
                });
            }

            c.Item().PaddingTop(6).Text("Profundidad de penetración (axial)").Bold().FontColor(Tinta).FontSize(10);
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d => { d.RelativeColumn(); d.RelativeColumn(); });
                var p = a.Axial.EstadisticaProfundidad;
                FilaInfo(t, "Media ± σ", $"{N(p.Media)} ± {N(p.Sigma)} mm");
                FilaInfo(t, "Mínimo / máximo", $"{N(p.Min)} / {N(p.Max)} mm");
                if (a.Axial.EstadisticaExceso.Max > 0)
                {
                    var ex = a.Axial.EstadisticaExceso;
                    FilaInfo(t, "Exceso de cordón (peor corte)", $"{N(ex.Max)} mm");
                    FilaInfo(t, "Exceso de cordón (media)", $"{N(ex.Media)} mm");
                }
            });
        });
    }

    private static void DetallePorCriterio(ColumnDescriptor col, ResultadoNormas v)
    {
        if (v.Reglas.Count == 0) return;
        Seccion(col, "5. Detalle por criterio", c =>
        {
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.RelativeColumn(3); d.RelativeColumn(2);
                    d.RelativeColumn(1.4f); d.RelativeColumn(1.4f); d.RelativeColumn(1.6f);
                });
                t.Header(h =>
                {
                    CeldaCabecera(h, "Criterio");
                    CeldaCabecera(h, "Medida");
                    CeldaCabecera(h, "Valor");
                    CeldaCabecera(h, "Límite");
                    CeldaCabecera(h, "Veredicto");
                });
                foreach (var r in v.Reglas)
                {
                    t.Cell().Element(Celda).Text(r.Regla.Defecto);
                    t.Cell().Element(Celda).Text(TextoMedida(r.Regla.Medida));
                    t.Cell().Element(Celda).Text($"{N(r.ValorEvaluado)} mm");
                    t.Cell().Element(Celda).Text($"{N(r.Limite)} mm");
                    var (bg, fg, _) = ColoresVeredicto(r.Veredicto);
                    t.Cell().Background(bg).Padding(4).Text(TextoVeredicto(r.Veredicto)).Bold().FontColor(fg).FontSize(9);
                }
            });
        });
    }

    private static void MasCritico(ColumnDescriptor col, ResultadoNormas v)
    {
        if (v.MuestraMasCritica is null && v.ReglaMasCritica is null) return;
        Seccion(col, "6. Punto más comprometido", c =>
        {
            if (v.MuestraMasCritica is { } mc)
                c.Item().Text(t =>
                {
                    t.Span("Muestra más crítica: ").Bold();
                    t.Span($"#{mc.NumeroMuestra} (ángulo/pos {N1(mc.AnguloOPosicion)}), {TextoMedida(mc.Medida)} = {N(mc.Valor)} mm " +
                           $"(objetivo {N(mc.Objetivo)} mm) → severidad {mc.Severidad:0.00}× tolerancia.");
                });
            if (v.ReglaMasCritica is { } rc)
                c.Item().Text(t =>
                {
                    t.Span("Criterio más comprometido: ").Bold();
                    t.Span($"{rc.Regla.Defecto} → severidad {rc.Severidad:0.00}× límite.");
                });
        });
    }

    private static void TablaMuestras(ColumnDescriptor col, Estudio e)
    {
        if (e.Muestras.Count == 0) return;
        Seccion(col, "7. Muestras medidas", c =>
        {
            c.Item().Table(t =>
            {
                t.ColumnsDefinition(d =>
                {
                    d.ConstantColumn(24);          // #
                    d.RelativeColumn();            // Áng/Pos
                    d.RelativeColumn();            // cercano
                    d.RelativeColumn();            // lejano
                    d.RelativeColumn();            // ancho
                    d.RelativeColumn();            // pos central
                    d.RelativeColumn();            // profundidad
                    d.RelativeColumn();            // exceso
                    d.RelativeColumn(1.4f);        // calidad
                });
                t.Header(h =>
                {
                    CeldaCabecera(h, "#");
                    CeldaCabecera(h, "Áng/Pos");
                    CeldaCabecera(h, "B. cercano");
                    CeldaCabecera(h, "B. lejano");
                    CeldaCabecera(h, "Ancho");
                    CeldaCabecera(h, "Pos. central");
                    CeldaCabecera(h, "Profund.");
                    CeldaCabecera(h, "Exceso");
                    CeldaCabecera(h, "Calidad");
                });
                foreach (var m in e.Muestras)
                {
                    t.Cell().Element(Celda).Text(m.NumeroMuestra.ToString(Inv));
                    t.Cell().Element(Celda).Text(N1(m.AnguloOPosicion));
                    t.Cell().Element(Celda).Text(N(m.DistanciaBordeCercano));
                    t.Cell().Element(Celda).Text(N(m.DistanciaBordeLejano));
                    t.Cell().Element(Celda).Text(N(m.AnchoCordon));
                    t.Cell().Element(Celda).Text(N(m.PosicionCentral));
                    t.Cell().Element(Celda).Text(N(m.Profundidad));
                    t.Cell().Element(Celda).Text(N(m.ExcesoCordon));
                    t.Cell().Element(Celda).Text(m.CalidadMedicion.ToString());
                }
            });
        });
    }

    private static void Vista3D(ColumnDescriptor col, byte[] render3d)
    {
        Seccion(col, "8. Vista 3D de concentricidad", c =>
        {
            c.Item().AlignCenter().MaxWidth(480).Image(render3d);
        });
    }

    private static void ImagenesMedicion(ColumnDescriptor col, Estudio estudio, string carpeta)
    {
        var conOverlay = estudio.Muestras
            .Where(m => m.RutaOverlay is not null)
            .Select(m => (m, ruta: Path.Combine(carpeta, m.RutaOverlay!)))
            .Where(x => File.Exists(x.ruta))
            .ToList();

        if (conOverlay.Count == 0) return;

        Seccion(col, "9. Imágenes de medición", c =>
        {
            // 2 overlays por fila
            for (int i = 0; i < conOverlay.Count; i += 2)
            {
                c.Item().PaddingTop(6).Row(row =>
                {
                    var (m1, r1) = conOverlay[i];
                    row.RelativeItem().Column(img =>
                    {
                        img.Item().AlignCenter().Text($"Muestra #{m1.NumeroMuestra}  ({N1(m1.AnguloOPosicion)}°)")
                            .FontSize(8).FontColor(Colors.Grey.Darken1);
                        img.Item().Image(File.ReadAllBytes(r1)).FitArea();
                    });

                    if (i + 1 < conOverlay.Count)
                    {
                        var (m2, r2) = conOverlay[i + 1];
                        row.ConstantItem(8); // separador
                        row.RelativeItem().Column(img =>
                        {
                            img.Item().AlignCenter().Text($"Muestra #{m2.NumeroMuestra}  ({N1(m2.AnguloOPosicion)}°)")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                            img.Item().Image(File.ReadAllBytes(r2)).FitArea();
                        });
                    }
                    else
                    {
                        row.RelativeItem(); // celda vacía para alinear
                    }
                });
            }
        });
    }

    private static void Avisos(ColumnDescriptor col, IReadOnlyList<string> avisos)
    {
        if (avisos.Count == 0) return;
        Seccion(col, "10. Avisos", c =>
        {
            foreach (var a in avisos)
                c.Item().Row(row =>
                {
                    row.ConstantItem(12).Text("•").FontColor(Tinta);
                    row.RelativeItem().Text(a);
                });
        });
    }

    // ── Helpers de estilo ──────────────────────────────────────────────────────────

    private static void Seccion(ColumnDescriptor col, string titulo, Action<ColumnDescriptor> cuerpo)
    {
        col.Item().PaddingTop(12).Text(titulo).FontSize(12).Bold().FontColor(Tinta);
        col.Item().PaddingBottom(4).LineHorizontal(0.8f).LineColor(GrisBorde);
        col.Item().Column(cuerpo);
    }

    private static void FilaInfo(TableDescriptor t, string etiqueta, string valor)
    {
        t.Cell().PaddingVertical(1.5f).Text(etiqueta).FontColor(Colors.Grey.Darken2);
        t.Cell().PaddingVertical(1.5f).Text(valor).Bold();
    }

    private static void EncabezadoTabla(TableDescriptor t, string a, string b)
    {
        t.Cell().Background(Tinta).Padding(4).Text(a).FontColor(Colors.White).Bold().FontSize(9);
        t.Cell().Background(Tinta).Padding(4).Text(b).FontColor(Colors.White).Bold().FontSize(9);
    }

    private static void FilaTabla(TableDescriptor t, string a, string b)
    {
        t.Cell().Element(Celda).Text(a);
        t.Cell().Element(Celda).Text(b).Bold();
    }

    private static void CeldaCabecera(TableCellDescriptor h, string texto) =>
        h.Cell().Background(Tinta).Padding(4).Text(texto).FontColor(Colors.White).Bold().FontSize(8.5f);

    private static IContainer Celda(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(GrisBorde).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer CeldaAjuste(IContainer c) =>
        c.Border(1).BorderColor(GrisBorde).Background(GrisSuave).Padding(8);

    private static void Ajuste(ColumnDescriptor col, string etiqueta, string valor)
    {
        col.Item().Text(etiqueta).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
        col.Item().Text(valor).FontSize(13).Bold().FontColor(Tinta);
    }

    // ── Textos y colores ────────────────────────────────────────────────────────────

    private static (Color bg, Color fg, Color border) ColoresVeredicto(Veredicto v) => v switch
    {
        Veredicto.Pasa    => (Color.FromHex("#E8F5E9"), Color.FromHex("#2E7D32"), Color.FromHex("#34C759")),
        Veredicto.Revisar => (Color.FromHex("#FFF8E1"), Color.FromHex("#B8860B"), Color.FromHex("#FFCC00")),
        _                 => (Color.FromHex("#FFEBEE"), Color.FromHex("#C62828"), Color.FromHex("#FF3B30")),
    };

    private static string TextoVeredicto(Veredicto v) => v switch
    {
        Veredicto.Pasa => "PASA",
        Veredicto.Revisar => "REVISAR",
        _ => "NO PASA"
    };

    private static string TextoDireccionZ(DireccionZ d) => d switch
    {
        DireccionZ.AumentarPenetracion => "aumentar penetración (acercar foco)",
        DireccionZ.ReducirPenetracion => "reducir penetración (alejar foco)",
        _ => "sin cambio"
    };

    private static string TextoMedida(MedidaEvaluada m) => m switch
    {
        MedidaEvaluada.Profundidad => "Penetración (media)",
        MedidaEvaluada.ProfundidadMinima => "Penetración (mínima)",
        MedidaEvaluada.AnchoCordon => "Ancho de cordón",
        MedidaEvaluada.Descentrado => "Descentrado",
        MedidaEvaluada.Runout => "Runout / TIR",
        MedidaEvaluada.ExcesoCordon => "Exceso de cordón",
        _ => m.ToString()
    };

    private static string N(double v) => v.ToString("0.000", Inv);
    private static string N1(double v) => v.ToString("0.#", Inv);
}
