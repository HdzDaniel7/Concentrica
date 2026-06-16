using System.Windows;

namespace Soldadura.App;

internal static class ThemeManager
{
    public static void Aplicar(bool oscuro)
    {
        string nombre = oscuro ? "TemaOscuro" : "TemaClaro";
        var uri = new Uri($"pack://application:,,,/Temas/{nombre}.xaml");

        var dicts = Application.Current.Resources.MergedDictionaries;
        var viejo = dicts.FirstOrDefault(d => d.Source?.ToString().Contains("/Temas/") == true);
        if (viejo != null) dicts.Remove(viejo);
        dicts.Add(new ResourceDictionary { Source = uri });
    }
}
