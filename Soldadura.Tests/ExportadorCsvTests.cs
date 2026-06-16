using Soldadura.Core.Modelo;
using Soldadura.Core.Persistencia;

namespace Soldadura.Tests;

public class ExportadorCsvTests
{
    private static Estudio EstudioDosMuestras()
    {
        var e = new Estudio { IdPieza = "P-1", NumeroPuesta = 1, ZonaPieza = "Brida A" };
        e.Muestras.Add(new Muestra
        {
            NumeroMuestra = 1, Orden = 1, AnguloOPosicion = 0,
            DistanciaBordeCercano = 4.5, DistanciaBordeLejano = 5.5, Profundidad = 1.2
        });
        e.Muestras.Add(new Muestra
        {
            NumeroMuestra = 2, Orden = 2, AnguloOPosicion = 180,
            DistanciaBordeCercano = 4.4, DistanciaBordeLejano = 5.4, Profundidad = 1.1
        });
        return e;
    }

    [Fact]
    public void Csv_TieneCabeceraYUnaFilaPorMuestra()
    {
        string csv = ExportadorCsv.MuestrasACsv(EstudioDosMuestras());
        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith("NumeroMuestra,Orden,AnguloOPosicion", lineas[0]);
        Assert.Equal(3, lineas.Length); // cabecera + 2 muestras
    }

    [Fact]
    public void Csv_UsaPuntoDecimalInvariante()
    {
        string csv = ExportadorCsv.MuestrasACsv(EstudioDosMuestras());

        // AnchoCordon = 1.0 y PosicionCentral = 5.0 deben escribirse con punto, no coma.
        Assert.Contains("4.5,5.5,1,5,1.2", csv);
    }

    [Fact]
    public void Csv_RespetaElOrdenDeLaLista()
    {
        var e = EstudioDosMuestras();
        // Invierto el orden de la colección (como tras "Ordenar por ángulo").
        var primera = e.Muestras[0];
        e.Muestras.RemoveAt(0);
        e.Muestras.Add(primera);

        string csv = ExportadorCsv.MuestrasACsv(e);
        var lineas = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith("2,", lineas[1]); // la muestra #2 va primero
        Assert.StartsWith("1,", lineas[2]);
    }

    [Fact]
    public void Csv_HeredaZonaDelEstudioYEntrecomillaSiHayComas()
    {
        var e = EstudioDosMuestras();
        e.Muestras[0].ZonaPieza = "Cordón A, lado izq";

        string csv = ExportadorCsv.MuestrasACsv(e);

        Assert.Contains("\"Cordón A, lado izq\"", csv); // override por muestra, entrecomillado
        Assert.Contains(",Brida A", csv);               // la otra hereda la zona del estudio
    }
}
