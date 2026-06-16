using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Soldadura.App.Converters;

/// <summary>Muestra solo el nombre de archivo de una ruta completa (para la tira de miniaturas).</summary>
public sealed class FileNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s ? Path.GetFileName(s) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
