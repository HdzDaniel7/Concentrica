using Soldadura.Core.Modelo;

namespace Soldadura.Tests;

/// <summary>Ayudantes para construir muestras y estudios sintéticos en las pruebas.</summary>
internal static class DatosSinteticos
{
    /// <summary>
    /// Crea una muestra a partir de la posición central y el ancho deseados
    /// (deriva los bordes cercano/lejano correspondientes).
    /// </summary>
    public static Muestra Muestra(
        int numero, double angulo, double posicionCentral, double ancho, double profundidad,
        CalidadMedicion calidad = CalidadMedicion.Metrologica)
    {
        return new Muestra
        {
            NumeroMuestra = numero,
            Orden = numero,
            AnguloOPosicion = angulo,
            DistanciaBordeCercano = posicionCentral - ancho / 2.0,
            DistanciaBordeLejano = posicionCentral + ancho / 2.0,
            Profundidad = profundidad,
            CalidadMedicion = calidad
        };
    }

    /// <summary>Estudio circular con N muestras equiespaciadas y posición central dada por una función de θ (grados).</summary>
    public static Estudio EstudioCircular(
        int n, Func<double, double> posicionDeAngulo, double ancho = 1.0, double profundidad = 1.0,
        bool marcaCero = true)
    {
        var estudio = new Estudio { IdPieza = "P1", NumeroPuesta = 1 };
        for (int i = 0; i < n; i++)
        {
            double ang = 360.0 * i / n;
            estudio.Muestras.Add(Muestra(i + 1, ang, posicionDeAngulo(ang), ancho, profundidad));
        }
        return estudio;
    }

    public static PerfilSoldadura PerfilCircular(
        double profundidadObjetivo = 1.0, double? coefFocoZ = null, bool marcaCero = true, int numMuestras = 8,
        ModeloReferencia modelo = ModeloReferencia.DatumPlanoExterno)
    {
        return new PerfilSoldadura
        {
            Nombre = "Test",
            Tipo = TipoSoldadura.Circular,
            ModeloReferencia = modelo,
            GeometriaObjetivo = new GeometriaObjetivo { ProfundidadObjetivo = profundidadObjetivo, Espesor = 3.0 },
            ConfigMuestreo = new ConfigMuestreo { NumeroMuestras = numMuestras, TieneMarcaCero = marcaCero },
            CoefFocoZ = coefFocoZ
        };
    }

    public static double Grados(double g) => g * Math.PI / 180.0;
}
