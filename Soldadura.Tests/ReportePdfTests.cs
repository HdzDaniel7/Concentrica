using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;
using Soldadura.Core.Persistencia;

namespace Soldadura.Tests;

public class ReportePdfTests
{
    [Fact]
    public void Generar_ProduceUnPdfValido()
    {
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, ancho: 1.0, profundidad: 1.0);
        estudio.IdPieza = "P-PDF";
        estudio.Objetivo = new GeometriaObjetivo { ProfundidadObjetivo = 1.0, Espesor = 3.0, RadioObjetivo = 5.0 };
        estudio.Especificaciones = new Especificaciones { Nombre = "Plano 42", Fuente = "Prueba" };

        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0, numMuestras: 8);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);
        var norma = ReglasDeEspecificaciones.Construir(estudio.Especificaciones);
        var veredicto = MotorNormas.Evaluar(
            norma, estudio.Especificaciones.Nivel, estudio.Objetivo, analisis, analisis.CalidadGlobal, estudio.Muestras);

        string ruta = Path.Combine(Path.GetTempPath(), $"reporte-{Guid.NewGuid():N}.pdf");
        try
        {
            ReportePdf.Generar(ruta, estudio, analisis, veredicto, esCircular: true);

            Assert.True(File.Exists(ruta));
            byte[] bytes = File.ReadAllBytes(ruta);
            Assert.True(bytes.Length > 1000, $"PDF demasiado pequeño: {bytes.Length} bytes");
            // Cabecera de archivo PDF: "%PDF".
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }
        finally
        {
            if (File.Exists(ruta)) File.Delete(ruta);
        }
    }
}
