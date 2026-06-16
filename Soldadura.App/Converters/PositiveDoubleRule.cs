using System.Globalization;
using System.Windows.Controls;

namespace Soldadura.App.Converters;

/// <summary>
/// Regla de validación WPF: acepta solo cadenas que representen un número estrictamente positivo
/// (> 0), admitiendo coma o punto como separador decimal. Se aplica a campos de tolerancias y
/// objetivos en los que un valor ≤ 0 no tiene sentido físico.
/// </summary>
public sealed class PositiveDoubleRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        if (value is not string s) return new ValidationResult(false, "Valor inválido");

        string norm = s.Trim().Replace(',', '.');
        if (norm.Length == 0)
            return new ValidationResult(false, "Campo requerido (debe ser > 0)");

        if (!double.TryParse(norm, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            return new ValidationResult(false, "Debe ser un número (ej: 0.20)");

        if (d <= 0)
            return new ValidationResult(false, "Debe ser mayor que 0");

        return ValidationResult.ValidResult;
    }
}
