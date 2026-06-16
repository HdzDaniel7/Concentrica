using System.Globalization;
using System.Windows.Data;

namespace Soldadura.App.Converters;

/// <summary>
/// Convierte double? ↔ string aceptando coma o punto como separador decimal. A diferencia de
/// <see cref="DoubleFlexibleConverter"/>, el texto vacío equivale a null (valor "no registrado")
/// y se admiten valores negativos. Pensado para el ajuste aplicado del robot, que puede ir en
/// cualquier dirección o no haberse anotado.
/// </summary>
public sealed class NullableDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double d ? d.ToString(CultureInfo.InvariantCulture) : "";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s) return Binding.DoNothing;
        s = s.Trim().Replace(',', '.');
        if (s.Length == 0) return null; // vacío = no registrado
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
            ? d
            : Binding.DoNothing; // texto a medio escribir o inválido: no piso el valor
    }
}
