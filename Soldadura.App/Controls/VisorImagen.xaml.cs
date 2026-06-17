using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Soldadura.App.ViewModels;

namespace Soldadura.App.Controls;

/// <summary>
/// Lienzo de imagen con zoom (rueda, centrado en el cursor), pan (arrastre), ajustar-a-ventana y 1:1.
/// Un clic fija la marca de la herramienta activa en coordenadas de píxel; el overlay las dibuja a
/// tamaño constante. La matemática px→mm vive en Soldadura.Core/Imagen; aquí solo la interacción.
/// </summary>
public partial class VisorImagen : UserControl
{
    private const double FactorZoom = 1.15;
    private const double UmbralClicPx = 3;

    private VisorImagenViewModel? _vm;
    private bool _arrastrando;
    private bool _movido;
    private Point _ultimo;
    private Point _inicio;

    public VisorImagen()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.Marcas.CollectionChanged -= OnMarcasChanged;
        }
        _vm = e.NewValue as VisorImagenViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            _vm.Marcas.CollectionChanged += OnMarcasChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisorImagenViewModel.RutaSeleccionada))
            CargarImagen(_vm?.RutaSeleccionada);
        else if (e.PropertyName == nameof(VisorImagenViewModel.ColorMarca))
            RedibujarOverlay();
    }

    private void OnMarcasChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
        RedibujarOverlay();

    private void CargarImagen(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta))
        {
            imagen.Source = null;
            lienzo.Width = lienzo.Height = 0;
            RedibujarOverlay();
            return;
        }

        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad; // no bloquear el archivo
        bmp.UriSource = new Uri(ruta);
        bmp.EndInit();

        imagen.Source = bmp;
        lienzo.Width = bmp.PixelWidth;
        lienzo.Height = bmp.PixelHeight;
        Ajustar();
    }

    // --- Zoom / pan ---

    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (imagen.Source is null) return;
        double factor = e.Delta > 0 ? FactorZoom : 1.0 / FactorZoom;
        Point cursor = e.GetPosition(viewport);

        double wx = (cursor.X - traslado.X) / escala.ScaleX;
        double wy = (cursor.Y - traslado.Y) / escala.ScaleY;

        double s = Math.Clamp(escala.ScaleX * factor, 0.02, 50);
        escala.ScaleX = escala.ScaleY = s;
        traslado.X = cursor.X - wx * s;
        traslado.Y = cursor.Y - wy * s;
        RedibujarOverlay();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (imagen.Source is null) return;
        _arrastrando = true;
        _movido = false;
        _inicio = _ultimo = e.GetPosition(viewport);
        viewport.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_arrastrando) return;
        Point p = e.GetPosition(viewport);
        Vector d = p - _ultimo;
        _ultimo = p;
        if ((p - _inicio).Length > UmbralClicPx) _movido = true;
        traslado.X += d.X;
        traslado.Y += d.Y;
        RedibujarOverlay();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_arrastrando) return;
        _arrastrando = false;
        viewport.ReleaseMouseCapture();

        if (!_movido && _vm is not null)
        {
            Point px = e.GetPosition(lienzo); // coords de píxel (antes del RenderTransform)
            _vm.FijarPunto(px.X, px.Y);
        }
    }

    private void OnAjustar(object sender, RoutedEventArgs e) => Ajustar();

    private void OnUnoAUno(object sender, RoutedEventArgs e)
    {
        if (imagen.Source is null) return;
        escala.ScaleX = escala.ScaleY = 1;
        Centrar();
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (imagen.Source is not null) Ajustar();
    }

    private void Ajustar()
    {
        if (imagen.Source is not BitmapSource bmp) return;
        double dispW = viewport.ActualWidth, dispH = viewport.ActualHeight;
        if (dispW <= 0 || dispH <= 0) return;

        double s = Math.Min(dispW / bmp.PixelWidth, dispH / bmp.PixelHeight);
        escala.ScaleX = escala.ScaleY = s > 0 ? s : 1;
        Centrar();
    }

    private void Centrar()
    {
        if (imagen.Source is not BitmapSource bmp) return;
        traslado.X = (viewport.ActualWidth - bmp.PixelWidth * escala.ScaleX) / 2;
        traslado.Y = (viewport.ActualHeight - bmp.PixelHeight * escala.ScaleY) / 2;
        RedibujarOverlay();
    }

    private void RedibujarOverlay()
    {
        overlay.Children.Clear();
        if (_vm is null) return;
        double s = escala.ScaleX;
        Brush pincel = _vm.PincelMarca;

        foreach (MarcaVisual m in _vm.Marcas)
        {
            double sx = m.X * s + traslado.X;
            double sy = m.Y * s + traslado.Y;

            var punto = new Ellipse
            {
                Width = 10, Height = 10,
                Stroke = pincel, StrokeThickness = 2, Fill = Brushes.Transparent
            };
            Canvas.SetLeft(punto, sx - 5);
            Canvas.SetTop(punto, sy - 5);
            overlay.Children.Add(punto);

            var texto = new TextBlock
            {
                Text = m.Etiqueta, Foreground = pincel, FontSize = 11, FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(texto, sx + 7);
            Canvas.SetTop(texto, sy - 8);
            overlay.Children.Add(texto);
        }
    }
}
