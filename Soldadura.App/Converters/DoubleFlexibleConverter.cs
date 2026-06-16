using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Soldadura.App.Converters;

/// <summary>
/// Convierte double ↔ string aceptando coma o punto como separador decimal, sin depender de la
/// cultura de Windows. Así el técnico puede teclear "1.5" o "1,5" (incluida la tecla decimal del
/// teclado numérico en teclados en español). Entrada inválida conserva el valor anterior.
/// </summary>
public sealed class DoubleFlexibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? d.ToString(CultureInfo.InvariantCulture) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s) return Binding.DoNothing;
        s = s.Trim().Replace(',', '.');
        if (s.Length == 0) return 0.0;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            ? d
            : Binding.DoNothing; // texto a medio escribir o inválido: no piso el valor
    }
}
