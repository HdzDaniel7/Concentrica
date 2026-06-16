using Soldadura.Core.Modelo;

namespace Soldadura.Core.Imagen;

/// <summary>
/// Marcas en píxeles de una medición sobre la imagen (auditable/reproducible) y su conversión a mm
/// con la escala. El datum es la recta DatumA–DatumB (cara plana externa); las distancias de los
/// bordes son perpendiculares a esa recta. La superficie de referencia es otra recta
/// SuperficieA–SuperficieB: la profundidad es la distancia perpendicular del Fondo de fusión a esa
/// recta (penetración, bajo la superficie) y el exceso de cordón es la distancia perpendicular de la
/// Corona (cresta del cordón) a esa misma recta (refuerzo, sobre la superficie).
/// </summary>
public sealed class MedicionEnPantalla
{
    public Punto2D DatumA { get; set; }
    public Punto2D DatumB { get; set; }
    public Punto2D BordeCercano { get; set; }
    public Punto2D BordeLejano { get; set; }

    /// <summary>Primer punto de la recta de superficie de referencia (opcional).</summary>
    public Punto2D? SuperficieA { get; set; }

    /// <summary>Segundo punto de la recta de superficie de referencia (opcional).</summary>
    public Punto2D? SuperficieB { get; set; }

    /// <summary>Fondo de fusión, para la penetración bajo la superficie (opcional).</summary>
    public Punto2D? Fondo { get; set; }

    /// <summary>Cresta del cordón sobre la superficie, para el exceso/refuerzo (opcional).</summary>
    public Punto2D? Corona { get; set; }

    /// <summary>Escala de la imagen (mm por píxel).</summary>
    public double EscalaMmPorPixel { get; set; }

    public double DistanciaBordeCercanoMm =>
        GeometriaImagen.DistanciaPerpendicular(BordeCercano, DatumA, DatumB) * EscalaMmPorPixel;

    public double DistanciaBordeLejanoMm =>
        GeometriaImagen.DistanciaPerpendicular(BordeLejano, DatumA, DatumB) * EscalaMmPorPixel;

    /// <summary>Profundidad en mm = perpendicular del Fondo a la recta de superficie; null si faltan marcas.</summary>
    public double? ProfundidadMm =>
        SuperficieA is { } a && SuperficieB is { } b && Fondo is { } f
            ? GeometriaImagen.DistanciaPerpendicular(f, a, b) * EscalaMmPorPixel
            : null;

    /// <summary>Exceso de cordón en mm = perpendicular de la Corona a la recta de superficie; null si faltan marcas.</summary>
    public double? ExcesoCordonMm =>
        SuperficieA is { } a && SuperficieB is { } b && Corona is { } c
            ? GeometriaImagen.DistanciaPerpendicular(c, a, b) * EscalaMmPorPixel
            : null;

    /// <summary>Vuelca las medidas calculadas sobre una muestra (modo MedicionEnPantalla) y
    /// guarda las coordenadas en píxeles en la muestra (auditable).</summary>
    public void AplicarA(Muestra muestra)
    {
        ArgumentNullException.ThrowIfNull(muestra);
        muestra.ModoCaptura = ModoCaptura.MedicionEnPantalla;
        muestra.EscalaMmPorPixel = EscalaMmPorPixel;
        muestra.DistanciaBordeCercano = DistanciaBordeCercanoMm;
        muestra.DistanciaBordeLejano = DistanciaBordeLejanoMm;
        if (ProfundidadMm is double p)
            muestra.Profundidad = p;
        if (ExcesoCordonMm is double e)
            muestra.ExcesoCordon = e;
        muestra.MarcasMedicion = AMarcas();
    }

    /// <summary>Empaqueta las marcas en píxeles (con su escala) para persistirlas.</summary>
    public MarcasMedicion AMarcas() => new()
    {
        DatumA = DatumA,
        DatumB = DatumB,
        BordeCercano = BordeCercano,
        BordeLejano = BordeLejano,
        SuperficieA = SuperficieA,
        SuperficieB = SuperficieB,
        Fondo = Fondo,
        Corona = Corona,
        EscalaMmPorPixel = EscalaMmPorPixel
    };
}
