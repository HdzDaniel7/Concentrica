using System.Globalization;
using System.Windows.Data;
using Soldadura.Core.Modelo;

namespace Soldadura.App.Converters;

/// <summary>
/// Muestra un nombre legible para cada <see cref="ModeloReferencia"/> en el ComboBox, alineado con
/// los títulos de las pestañas de <c>ModelosReferenciaWindow</c>. Solo se usa para presentación
/// (el SelectedItem sigue enlazado al valor del enum).
/// </summary>
public sealed class ModeloReferenciaConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ModeloReferencia m ? Nombre(m) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static string Nombre(ModeloReferencia m) => m switch
    {
        ModeloReferencia.DatumPlanoExterno => "1 · Datum plano externo",
        ModeloReferencia.RadialDesdeCentro => "2 · Radial desde el eje",
        ModeloReferencia.DosFeatures       => "3 · Dos features / línea base",
        ModeloReferencia.ContornoPieza     => "4 · Contorno de la pieza",
        ModeloReferencia.SoloCordon        => "5 · Solo geometría del cordón",
        _                                  => m.ToString()
    };
}
