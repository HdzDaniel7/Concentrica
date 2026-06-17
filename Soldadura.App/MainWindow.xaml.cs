using System.ComponentModel;
using System.Windows;
using AvalonDock.Themes;
using Soldadura.App.ViewModels;

namespace Soldadura.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        // El DockingManager tiene su propio chrome (pestañas, fondos de panel) que NO sigue los
        // DynamicResource de la app; sin un tema propio, las pestañas activas y algunos fondos
        // quedan blancos en modo oscuro. Le aplicamos el tema VS2013 (oscuro/claro) y lo alternamos.
        AplicarTemaDock(vm.TemaOscuro);
        vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TemaOscuro) && DataContext is MainViewModel vm)
            AplicarTemaDock(vm.TemaOscuro);
    }

    private void AplicarTemaDock(bool oscuro) =>
        dockManager.Theme = oscuro ? new Vs2013DarkTheme() : new Vs2013LightTheme();
}