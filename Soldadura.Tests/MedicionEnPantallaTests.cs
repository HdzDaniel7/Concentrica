using Soldadura.Core.Imagen;
using Soldadura.Core.Modelo;

namespace Soldadura.Tests;

public class MedicionEnPantallaTests
{
    [Fact]
    public void CalibrarEscala_DividiendoMmEntrePixeles()
    {
        // 100 px representan 2.0 mm → 0.02 mm/px.
        Assert.Equal(0.02, PerfilMicroscopio.CalibrarEscala(100, 2.0), 9);
    }

    [Fact]
    public void Distancia_Euclidiana()
    {
        Assert.Equal(5.0, GeometriaImagen.Distancia(new Punto2D(0, 0), new Punto2D(3, 4)), 9);
    }

    [Fact]
    public void DistanciaPerpendicular_ALaRectaDatum()
    {
        // Datum horizontal y=0; punto a 50 px por encima.
        double d = GeometriaImagen.DistanciaPerpendicular(
            new Punto2D(5, 50), new Punto2D(0, 0), new Punto2D(10, 0));
        Assert.Equal(50.0, d, 9);
    }

    [Fact]
    public void AplicarA_ConvierteMarcasAMmYDerivados()
    {
        // Datum en y=0; bordes a 50 y 150 px; escala 0.01 mm/px → 0.5 y 1.5 mm.
        // Superficie horizontal en y=150; Fondo a y=200 (50 px bajo) → profundidad 0.5 mm;
        // Corona a y=120 (30 px sobre) → exceso 0.3 mm.
        var med = new MedicionEnPantalla
        {
            DatumA = new Punto2D(0, 0),
            DatumB = new Punto2D(100, 0),
            BordeCercano = new Punto2D(40, 50),
            BordeLejano = new Punto2D(60, 150),
            SuperficieA = new Punto2D(0, 150),
            SuperficieB = new Punto2D(100, 150),
            Fondo = new Punto2D(50, 200),
            Corona = new Punto2D(50, 120),
            EscalaMmPorPixel = 0.01
        };

        var m = new Muestra();
        med.AplicarA(m);

        Assert.Equal(0.5, m.DistanciaBordeCercano, 9);
        Assert.Equal(1.5, m.DistanciaBordeLejano, 9);
        Assert.Equal(0.5, m.Profundidad, 9);          // 50 px × 0.01
        Assert.Equal(0.3, m.ExcesoCordon, 9);         // 30 px × 0.01
        Assert.Equal(1.0, m.AnchoCordon, 9);          // 1.5 − 0.5
        Assert.Equal(1.0, m.PosicionCentral, 9);      // (0.5 + 1.5)/2
        Assert.Equal(ModoCaptura.MedicionEnPantalla, m.ModoCaptura);
    }

    [Fact]
    public void ProfundidadMm_NullSiFaltanMarcas()
    {
        var med = new MedicionEnPantalla
        {
            DatumA = new Punto2D(0, 0), DatumB = new Punto2D(10, 0),
            BordeCercano = new Punto2D(5, 10), BordeLejano = new Punto2D(5, 20),
            EscalaMmPorPixel = 0.01
        };
        Assert.Null(med.ProfundidadMm);   // sin superficie ni fondo
        Assert.Null(med.ExcesoCordonMm);  // sin superficie ni corona
    }

    [Fact]
    public void ExcesoCordonMm_PerpendicularALaSuperficieInclinada()
    {
        // Superficie inclinada 45°; Corona a (0,10) → distancia perpendicular = 10/√2 px.
        var med = new MedicionEnPantalla
        {
            DatumA = new Punto2D(0, 0), DatumB = new Punto2D(100, 0),
            BordeCercano = new Punto2D(5, 10), BordeLejano = new Punto2D(5, 20),
            SuperficieA = new Punto2D(0, 0), SuperficieB = new Punto2D(10, 10),
            Corona = new Punto2D(0, 10),
            EscalaMmPorPixel = 1.0
        };
        Assert.Equal(10.0 / Math.Sqrt(2), med.ExcesoCordonMm!.Value, 9);
    }

    [Fact]
    public void AplicarA_PersisteLasMarcasEnPixeles()
    {
        var med = new MedicionEnPantalla
        {
            DatumA = new Punto2D(0, 0), DatumB = new Punto2D(100, 0),
            BordeCercano = new Punto2D(40, 50), BordeLejano = new Punto2D(60, 150),
            SuperficieA = new Punto2D(0, 150), SuperficieB = new Punto2D(100, 150),
            Fondo = new Punto2D(50, 200), Corona = new Punto2D(50, 120),
            EscalaMmPorPixel = 0.01
        };

        var m = new Muestra();
        med.AplicarA(m);

        Assert.NotNull(m.MarcasMedicion);
        Assert.Equal(new Punto2D(40, 50), m.MarcasMedicion!.BordeCercano);
        Assert.Equal(0.01, m.MarcasMedicion.EscalaMmPorPixel, 9);
    }

    [Fact]
    public void MarcasMedicion_RoundTripConMedicion()
    {
        var original = new MedicionEnPantalla
        {
            DatumA = new Punto2D(0, 0), DatumB = new Punto2D(100, 0),
            BordeCercano = new Punto2D(40, 50), BordeLejano = new Punto2D(60, 150),
            EscalaMmPorPixel = 0.02
        };

        var reconstruida = original.AMarcas().AMedicion();

        Assert.NotNull(reconstruida);
        Assert.Equal(original.DistanciaBordeCercanoMm, reconstruida!.DistanciaBordeCercanoMm, 9);
        Assert.Equal(original.DistanciaBordeLejanoMm, reconstruida.DistanciaBordeLejanoMm, 9);
    }

    [Fact]
    public void MarcasMedicion_SinBaseNoProduceMedicion()
    {
        var marcas = new MarcasMedicion { DatumA = new Punto2D(0, 0), EscalaMmPorPixel = 0.01 };
        Assert.False(marcas.TieneBase);
        Assert.Null(marcas.AMedicion());
    }
}
