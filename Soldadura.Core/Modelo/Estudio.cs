using System.Text.Json.Serialization;

namespace Soldadura.Core.Modelo;

/// <summary>
/// Un estudio = una puesta de soldadura sobre una pieza, con sus muestras.
/// Identidad = IdPieza + NumeroPuesta + Fecha.
/// </summary>
public sealed class Estudio
{
    /// <summary>Identificador de la pieza física (puede repetirse entre estudios).</summary>
    public string IdPieza { get; set; } = "";

    /// <summary>Distingue cada puesta/corrida de soldadura sobre la misma pieza.</summary>
    public int NumeroPuesta { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Now;

    /// <summary>Carpeta del estudio en disco (ruta).</summary>
    public string Carpeta { get; set; } = "";

    /// <summary>Nombre del perfil usado (referencia; los perfiles se guardan aparte).</summary>
    public string PerfilNombre { get; set; } = "";

    /// <summary>Zona física por defecto; las muestras la heredan si no la sobrescriben.</summary>
    public string? ZonaPieza { get; set; }

    /// <summary>Geometría objetivo usada en este estudio (snapshot; trazabilidad).</summary>
    public GeometriaObjetivo? Objetivo { get; set; }

    /// <summary>Tolerancias de aceptación aplicadas en este estudio (snapshot; trazabilidad).</summary>
    public Especificaciones Especificaciones { get; set; } = new();

    public List<Muestra> Muestras { get; set; } = new();

    /// <summary>
    /// Ajuste del robot efectivamente aplicado para esta puesta (snapshot; trazabilidad).
    /// Permite cerrar el lazo: aprender CoefFocoZ comparando Z aplicado vs profundidad obtenida
    /// a lo largo de las puestas. null si el técnico no lo registró.
    /// </summary>
    public AjusteAplicado? AjusteAplicado { get; set; }

    /// <summary>Nombre de carpeta sugerido: Puesta&lt;n&gt;_AAAA-MM-DD.</summary>
    [JsonIgnore]
    public string NombreCarpetaSugerido => $"Puesta{NumeroPuesta}_{Fecha:yyyy-MM-dd}";

    /// <summary>Zona efectiva de una muestra: su override o, si null, la del estudio.</summary>
    public string? ZonaEfectiva(Muestra m) => m.ZonaPieza ?? ZonaPieza;
}
