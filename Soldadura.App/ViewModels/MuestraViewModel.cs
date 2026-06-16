using CommunityToolkit.Mvvm.ComponentModel;
using Soldadura.Core.Imagen;
using Soldadura.Core.Modelo;

namespace Soldadura.App.ViewModels;

/// <summary>
/// Una fila editable de captura (AnotacionDirecta). Expone los derivados AnchoCordon y
/// PosicionCentral en vivo para que el DataGrid los refresque al teclear.
/// </summary>
public partial class MuestraViewModel : ObservableObject
{
    [ObservableProperty] private int _numeroMuestra;
    [ObservableProperty] private double _anguloOPosicion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnchoCordon))]
    [NotifyPropertyChangedFor(nameof(PosicionCentral))]
    private double _distanciaBordeCercano;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnchoCordon))]
    [NotifyPropertyChangedFor(nameof(PosicionCentral))]
    private double _distanciaBordeLejano;

    [ObservableProperty] private double _profundidad;
    [ObservableProperty] private double _excesoCordon;
    [ObservableProperty] private CalidadMedicion _calidadMedicion = CalidadMedicion.Metrologica;
    [ObservableProperty] private ModoCaptura _modoCaptura = ModoCaptura.AnotacionDirecta;

    // --- Medición en pantalla (opcional) ---
    [ObservableProperty] private string? _rutaImagen;
    [ObservableProperty] private string? _rutaOverlay;
    [ObservableProperty] private double? _escalaMmPorPixel;
    [ObservableProperty] private MarcasMedicion? _marcasMedicion;

    public double AnchoCordon => DistanciaBordeLejano - DistanciaBordeCercano;
    public double PosicionCentral => (DistanciaBordeCercano + DistanciaBordeLejano) / 2.0;

    public Muestra AModelo() => new()
    {
        NumeroMuestra = NumeroMuestra,
        Orden = NumeroMuestra,
        AnguloOPosicion = AnguloOPosicion,
        DistanciaBordeCercano = DistanciaBordeCercano,
        DistanciaBordeLejano = DistanciaBordeLejano,
        Profundidad = Profundidad,
        ExcesoCordon = ExcesoCordon,
        CalidadMedicion = CalidadMedicion,
        ModoCaptura = ModoCaptura,
        RutaImagen = RutaImagen,
        RutaOverlay = RutaOverlay,
        EscalaMmPorPixel = EscalaMmPorPixel,
        MarcasMedicion = MarcasMedicion
    };

    public static MuestraViewModel Desde(Muestra m) => new()
    {
        NumeroMuestra = m.NumeroMuestra,
        AnguloOPosicion = m.AnguloOPosicion,
        DistanciaBordeCercano = m.DistanciaBordeCercano,
        DistanciaBordeLejano = m.DistanciaBordeLejano,
        Profundidad = m.Profundidad,
        ExcesoCordon = m.ExcesoCordon,
        CalidadMedicion = m.CalidadMedicion,
        ModoCaptura = m.ModoCaptura,
        RutaImagen = m.RutaImagen,
        RutaOverlay = m.RutaOverlay,
        EscalaMmPorPixel = m.EscalaMmPorPixel,
        MarcasMedicion = m.MarcasMedicion
    };
}
