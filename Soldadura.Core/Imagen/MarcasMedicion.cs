namespace Soldadura.Core.Imagen;

/// <summary>
/// Coordenadas en píxeles de las marcas de medición sobre la imagen (auditable/reproducible),
/// pensado para persistirse en la muestra. Todos los puntos son opcionales para poder guardar una
/// medición a medio marcar; <see cref="AMedicion"/> solo produce una medición cuando están las cuatro
/// marcas obligatorias (datum + bordes). La superficie es una recta (A/B); Fondo da la penetración y
/// Corona el exceso de cordón.
/// </summary>
public sealed class MarcasMedicion
{
    public Punto2D? DatumA { get; set; }
    public Punto2D? DatumB { get; set; }
    public Punto2D? BordeCercano { get; set; }
    public Punto2D? BordeLejano { get; set; }
    public Punto2D? SuperficieA { get; set; }
    public Punto2D? SuperficieB { get; set; }
    public Punto2D? Fondo { get; set; }
    public Punto2D? Corona { get; set; }

    /// <summary>Escala de la imagen (mm por píxel) con la que se tomaron las marcas.</summary>
    public double EscalaMmPorPixel { get; set; }

    /// <summary>true cuando están marcadas las cuatro marcas obligatorias (datum A/B y ambos bordes).</summary>
    public bool TieneBase =>
        DatumA is not null && DatumB is not null && BordeCercano is not null && BordeLejano is not null;

    /// <summary>
    /// Construye una <see cref="MedicionEnPantalla"/> calculable si están las marcas base; si no, null.
    /// </summary>
    public MedicionEnPantalla? AMedicion()
    {
        if (!TieneBase)
            return null;

        return new MedicionEnPantalla
        {
            DatumA = DatumA!.Value,
            DatumB = DatumB!.Value,
            BordeCercano = BordeCercano!.Value,
            BordeLejano = BordeLejano!.Value,
            SuperficieA = SuperficieA,
            SuperficieB = SuperficieB,
            Fondo = Fondo,
            Corona = Corona,
            EscalaMmPorPixel = EscalaMmPorPixel
        };
    }
}
