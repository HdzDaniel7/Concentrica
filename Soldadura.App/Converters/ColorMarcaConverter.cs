using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Soldadura.App.ViewModels;

namespace Soldadura.App.Converters;

/// <summary>Convierte un <see cref="ColorMarca"/> en una brocha sólida para el swatch del ComboBox.</summary>
public sealed class ColorMarcaABrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ColorMarca c ? new SolidColorBrush(ColoresMarca.Media(c)) : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
