using System.Text.Json.Serialization;

namespace Soldadura.Core.Modelo;

/// <summary>
/// Una medición en un plano (un corte) de la soldadura.
/// distanciaBordeCercano / distanciaBordeLejano se miden desde el datum del modelo de referencia.
/// </summary>
public sealed class Muestra
{
    /// <summary>Número secuencial de la muestra.</summary>
    public int NumeroMuestra { get; set; }

    /// <summary>Orden dentro del estudio (cuál sigue a cuál); reordenable.</summary>
    public int Orden { get; set; }

    /// <summary>Ángulo en grados (circular) o posición a lo largo del largo (lineal).</summary>
    public double AnguloOPosicion { get; set; }

    /// <summary>Zona física de la pieza. Si es null, hereda la del estudio.</summary>
    public string? ZonaPieza { get; set; }

    /// <summary>Distancia del datum al borde más cercano del cordón (mm).</summary>
    public double DistanciaBordeCercano { get; set; }

    /// <summary>Distancia del datum al borde más lejano del cordón (mm).</summary>
    public double DistanciaBordeLejano { get; set; }

    /// <summary>Profundidad de penetración medida (mm).</summary>
    public double Profundidad { get; set; }

    /// <summary>Exceso de cordón / refuerzo sobre la superficie (mm). 0 si no se mide.</summary>
    public double ExcesoCordon { get; set; }

    public CalidadMedicion CalidadMedicion { get; set; } = CalidadMedicion.Metrologica;

    public ModoCaptura ModoCaptura { get; set; } = ModoCaptura.AnotacionDirecta;

    /// <summary>Ruta relativa de la imagen del microscopio (opcional).</summary>
    public string? RutaImagen { get; set; }

    /// <summary>Escala de la imagen, mm por píxel (usada en MedicionEnPantalla).</summary>
    public double? EscalaMmPorPixel { get; set; }

    /// <summary>Ruta relativa del overlay con las marcas de medición (opcional).</summary>
    public string? RutaOverlay { get; set; }

    /// <summary>Coordenadas en píxeles de las marcas de medición en pantalla (auditable, opcional).</summary>
    public Imagen.MarcasMedicion? MarcasMedicion { get; set; }

    // --- Derivados (no se capturan; se calculan) ---

    /// <summary>Ancho del cordón = borde lejano − borde cercano.</summary>
    [JsonIgnore]
    public double AnchoCordon => DistanciaBordeLejano - DistanciaBordeCercano;

    /// <summary>Posición de la línea central respecto al datum = (cercano + lejano) / 2.</summary>
    [JsonIgnore]
    public double PosicionCentral => (DistanciaBordeCercano + DistanciaBordeLejano) / 2.0;
}
