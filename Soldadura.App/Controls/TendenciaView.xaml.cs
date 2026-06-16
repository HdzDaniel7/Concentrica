using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Soldadura.App.ViewModels;
using Soldadura.Core.Analisis;

namespace Soldadura.App.Controls;

public partial class TendenciaView : UserControl
{
    public TendenciaView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TendenciaViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;
        if (e.NewValue is TendenciaViewModel newVm)
            newVm.PropertyChanged += OnVmPropertyChanged;
        RenderizarGrafico();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TendenciaViewModel.Puntos)
                           or nameof(TendenciaViewModel.Pendiente)
                           or nameof(TendenciaViewModel.RunoutMaximo))
            RenderizarGrafico();
    }

    private void OnGraficoSizeChanged(object sender, SizeChangedEventArgs e) => RenderizarGrafico();

    private void RenderizarGrafico()
    {
        grafico.Children.Clear();

        if (DataContext is not TendenciaViewModel vm || vm.Puntos.Count == 0)
        {
            AgTb("Sin datos de tendencia", 8, 8, Brushes.Gray);
            return;
        }

        double w = grafico.ActualWidth;
        double h = grafico.ActualHeight;
        if (w < 30 || h < 30) return;

        const double mL = 50, mB = 28, mR = 10, mT = 10;
        double cW = w - mL - mR;
        double cH = h - mT - mB;
        if (cW < 10 || cH < 10) return;

        var pts = vm.Puntos;
        int pMin = pts.Min(p => p.NumeroPuesta);
        int pMax = pts.Max(p => p.NumeroPuesta);
        if (pMax == pMin) pMax = pMin + 1;

        double yMaxData = pts.Max(p => p.Runout);
        double yMaxLim = vm.RunoutMaximo is double lim ? lim * 1.15 : 0;
        double yMax = Math.Max(yMaxData * 1.3, Math.Max(yMaxLim, 0.01));

        // Funciones de mapeo a coordenadas canvas
        double xC(double p) => mL + (p - pMin) / (pMax - pMin) * cW;
        double yC(double r) => mT + cH * (1 - r / yMax);

        // Ejes
        AgLine(mL, mT, mL, mT + cH, Brushes.DarkGray, 1);
        AgLine(mL, mT + cH, mL + cW, mT + cH, Brushes.DarkGray, 1);

        // Etiqueta eje Y
        AgTb("mm", 2, mT - 4, Brushes.DimGray, 10);

        // Marcas y etiquetas eje X (puestas)
        for (int p = pMin; p <= pMax; p++)
        {
            double x = xC(p);
            AgLine(x, mT + cH, x, mT + cH + 4, Brushes.DarkGray, 1);
            AgTb(p.ToString(), x - 6, mT + cH + 5, Brushes.DimGray, 10);
        }

        // Marcas eje Y (2 referencias)
        for (int i = 1; i <= 4; i++)
        {
            double val = yMax * i / 4;
            double y = yC(val);
            AgLine(mL - 4, y, mL, y, Brushes.DarkGray, 1);
            AgTb($"{val:0.00}", 2, y - 7, Brushes.DimGray, 9);
        }

        // Línea de límite RunoutMaximo
        if (vm.RunoutMaximo is double limY && limY > 0)
        {
            double yl = yC(limY);
            AgLine(mL, yl, mL + cW, yl, Brushes.Red, 1.5,
                   new DoubleCollection { 5, 3 });
            AgTb($"Lím {limY:0.000}", mL + cW - 70, yl - 14, Brushes.Red, 10);
        }

        // Línea de regresión
        if (pts.Count >= 2 && Math.Abs(vm.Pendiente) > 1e-12)
        {
            double x1 = xC(pMin), y1 = yC(vm.Ordenada + vm.Pendiente * pMin);
            double x2 = xC(pMax), y2 = yC(vm.Ordenada + vm.Pendiente * pMax);
            // Clipear a la zona de la gráfica
            AgLine(x1, Math.Clamp(y1, mT, mT + cH), x2, Math.Clamp(y2, mT, mT + cH),
                   Brushes.CornflowerBlue, 1.5);
        }

        // Puntos de datos
        foreach (var pt in pts)
        {
            double cx = xC(pt.NumeroPuesta);
            double cy = yC(pt.Runout);
            var el = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = Brushes.SteelBlue,
                Stroke = Brushes.MidnightBlue,
                StrokeThickness = 1
            };
            Canvas.SetLeft(el, cx - 4);
            Canvas.SetTop(el, cy - 4);
            grafico.Children.Add(el);
        }

        // Línea vertical en puestaCruce (si existe)
        if (vm.PuestaCruceLimite is int pc && pc >= pMin && pc <= pMax + 10)
        {
            double xCruce = xC(pc);
            if (xCruce <= mL + cW + 1)
            {
                AgLine(xCruce, mT, xCruce, mT + cH, Brushes.OrangeRed, 1,
                       new DoubleCollection { 3, 3 });
                AgTb($"P{pc}", xCruce + 2, mT + 2, Brushes.OrangeRed, 9);
            }
        }
    }

    // ── Helpers de dibujo ──────────────────────────────────────────────────────

    private void AgLine(double x1, double y1, double x2, double y2,
                        Brush stroke, double thick, DoubleCollection? dash = null)
    {
        var ln = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thick };
        if (dash != null) ln.StrokeDashArray = dash;
        grafico.Children.Add(ln);
    }

    private void AgTb(string text, double x, double y, Brush fg, double size = 11)
    {
        var tb = new TextBlock { Text = text, FontSize = size, Foreground = fg };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        grafico.Children.Add(tb);
    }
}
