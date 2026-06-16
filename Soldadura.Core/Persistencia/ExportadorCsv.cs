using System.Globalization;
using System.Text;
using Soldadura.Core.Modelo;

namespace Soldadura.Core.Persistencia;

/// <summary>
/// Exporta los datos de un estudio a CSV (formato de SALIDA, nunca de entrada — sección 12).
/// Números con punto decimal invariante; campos de texto entrecomillados si lo requieren.
/// Las filas se emiten en el orden de la lista de muestras (= el orden elegido en la captura).
/// </summary>
public static class ExportadorCsv
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private const string Cabecera =
        "NumeroMuestra,Orden,AnguloOPosicion,DistanciaBordeCercano,DistanciaBordeLejano," +
        "AnchoCordon,PosicionCentral,Profundidad,ExcesoCordon,CalidadMedicion,ModoCaptura,ZonaPieza";

    public static string MuestrasACsv(Estudio estudio)
    {
        ArgumentNullException.ThrowIfNull(estudio);

        var sb = new StringBuilder();
        sb.AppendLine(Cabecera);
        foreach (var m in estudio.Muestras)
        {
            string zona = m.ZonaPieza ?? estudio.ZonaPieza ?? "";
            sb.AppendLine(string.Join(",",
                m.NumeroMuestra.ToString(Inv),
                m.Orden.ToString(Inv),
                Num(m.AnguloOPosicion),
                Num(m.DistanciaBordeCercano),
                Num(m.DistanciaBordeLejano),
                Num(m.AnchoCordon),
                Num(m.PosicionCentral),
                Num(m.Profundidad),
                Num(m.ExcesoCordon),
                Texto(m.CalidadMedicion.ToString()),
                Texto(m.ModoCaptura.ToString()),
                Texto(zona)));
        }
        return sb.ToString();
    }

    private static string Num(double v) => v.ToString("0.###", Inv);

    private static string Texto(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
}
