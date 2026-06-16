using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Soldadura.App.Visor3D;

namespace Soldadura.App.Controls;

public partial class VistaWeld3D : UserControl
{
    private bool _camaraInicializada;

    public VistaWeld3D()
    {
        InitializeComponent();

        SizeChanged += (_, _) => ActualizarLabels();
        if (viewport.Camera is not null)
            viewport.Camera.Changed += (_, _) => ActualizarLabels();

        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is Visor3DViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnVmPropertyChanged;
                oldVm.ObtenerSnapshot = null;
            }
            if (e.NewValue is Visor3DViewModel newVm)
            {
                newVm.PropertyChanged += OnVmPropertyChanged;
                newVm.ObtenerSnapshot = RenderABytes;
                ActualizarLabels();
            }
        };
    }

    // Resolución fija del render para el PDF (4:3), independiente del tamaño del panel.
    private const int RenderAncho = 1000;
    private const int RenderAlto  = 750;

    /// <summary>
    /// Renderiza la escena 3D a PNG en memoria a una resolución FIJA, independiente del tamaño
    /// (o visibilidad) del panel en pantalla. Compone un viewport off-screen con un clon del modelo
    /// y de la cámara actual, el fondo de la vista y las etiquetas reproyectadas. Devuelve null si
    /// no hay escena que renderizar.
    /// </summary>
    private byte[]? RenderABytes()
    {
        if (DataContext is not Visor3DViewModel vm || !vm.HayDatos) return null;
        if (viewport.Camera is null) return null;

        const int w = RenderAncho, h = RenderAlto;

        // Viewport off-screen: mismo modelo (clonado para no compartir árbol visual) y la cámara
        // actual del usuario (clonada). Los parámetros de cámara no dependen del tamaño del panel,
        // así que la vista se conserva aunque el panel esté colapsado.
        var vp = new Viewport3D { Width = w, Height = h, ClipToBounds = true };
        vp.Camera = (Camera)viewport.Camera.Clone();
        vp.Children.Add(new ModelVisual3D { Content = vm.Modelo.Clone() });

        var host = new Grid
        {
            Width = w,
            Height = h,
            Background = new SolidColorBrush(Visor3DColores.Fondo)
        };
        host.Children.Add(vp);

        // Primer layout para que el viewport tenga matriz de proyección válida.
        host.Measure(new Size(w, h));
        host.Arrange(new Rect(0, 0, w, h));
        host.UpdateLayout();

        // Etiquetas: reproyectadas contra el viewport off-screen ya dimensionado.
        var canvas = ConstruirCanvasEtiquetas(vp, vm, w, h);
        if (canvas is not null)
        {
            host.Children.Add(canvas);
            host.Measure(new Size(w, h));
            host.Arrange(new Rect(0, 0, w, h));
            host.UpdateLayout();
        }

        var rt = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rt.Render(host);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rt));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private static Canvas? ConstruirCanvasEtiquetas(Viewport3D vp, Visor3DViewModel vm, double w, double h)
    {
        if (vm.Labels.Count == 0) return null;

        var canvas = new Canvas { Width = w, Height = h, IsHitTestVisible = false };
        foreach (var lbl in vm.Labels)
        {
            var pt2d = Viewport3DHelper.Point3DtoPoint2D(vp, new Point3D(lbl.X, lbl.Y, lbl.Z));
            if (double.IsNaN(pt2d.X) || double.IsNaN(pt2d.Y)) continue;

            var tb = CrearEtiqueta(lbl.Texto);
            Canvas.SetLeft(tb, pt2d.X + 6);
            Canvas.SetTop(tb, pt2d.Y - 8);
            canvas.Children.Add(tb);
        }
        return canvas;
    }

    private static TextBlock CrearEtiqueta(string texto) => new()
    {
        Text       = texto,
        Foreground = Brushes.White,
        FontSize   = 11,
        FontWeight = FontWeights.SemiBold,
        Effect     = new DropShadowEffect { BlurRadius = 3, ShadowDepth = 1, Color = Colors.Black, Opacity = 0.9 }
    };

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Visor3DViewModel.Labels)) return;

        if (!_camaraInicializada && DataContext is Visor3DViewModel vm && vm.Labels.Count > 0)
        {
            EstablecerCamaraInicial(vm.RadioRef);
            _camaraInicializada = true;
        }

        ActualizarLabels();
    }

    /// <summary>
    /// Fija una perspectiva 3/4 ligera al cargar el primer resultado:
    /// la cámara queda a ~3 radios de distancia, elevada ~35° y girada ~45° en yaw.
    /// </summary>
    private void EstablecerCamaraInicial(double radioRef)
    {
        double d = radioRef * 3.0;
        double yaw   = 45.0 * Math.PI / 180.0;
        double pitch = 35.0 * Math.PI / 180.0;

        double x = d * Math.Cos(pitch) * Math.Sin(yaw);
        double y = d * Math.Cos(pitch) * Math.Cos(yaw);
        double z = d * Math.Sin(pitch);

        if (viewport.Camera is PerspectiveCamera cam)
        {
            cam.Position    = new Point3D(x, y, z);
            cam.LookDirection = new Vector3D(-x, -y, -z);
            cam.UpDirection  = new Vector3D(0, 0, 1);
            cam.FieldOfView  = 45;
        }
        else
        {
            // Si HelixViewport3D todavía no creó la cámara, usamos ZoomExtents como fallback.
            viewport.ZoomExtents();
        }
    }

    private void ActualizarLabels()
    {
        labelCanvas.Children.Clear();
        if (DataContext is not Visor3DViewModel vm) return;

        foreach (var lbl in vm.Labels)
        {
            var pt2d = Viewport3DHelper.Point3DtoPoint2D(viewport.Viewport, new Point3D(lbl.X, lbl.Y, lbl.Z));
            if (double.IsNaN(pt2d.X) || double.IsNaN(pt2d.Y)) continue;

            var tb = CrearEtiqueta(lbl.Texto);
            Canvas.SetLeft(tb, pt2d.X + 6);
            Canvas.SetTop(tb, pt2d.Y - 8);
            labelCanvas.Children.Add(tb);
        }
    }
}
