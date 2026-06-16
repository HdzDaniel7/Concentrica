using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;

namespace Soldadura.Tests;

public class MotorNormasTests
{
    /// <summary>Norma mínima con una sola regla, para aislar la lógica de clasificación.</summary>
    private static Norma NormaUnaRegla(ReglaNorma regla) => new()
    {
        Id = "TEST",
        Edicion = "1",
        Verificada = true,
        Reglas = { regla }
    };

    private static ReglaNorma ReglaPenetracion() => new()
    {
        Defecto = "Desviación de penetración",
        Medida = MedidaEvaluada.Profundidad,
        Nivel = NivelNorma.B,
        TipoLimite = TipoLimite.Maximo,
        Referencia = ReferenciaLimite.DesviacionDeObjetivo,
        Limite = new LimiteLineal { Offset = 0.2, CoefEspesor = 0 },
        MargenRevision = 0.05,
        EtiquetaCaso = "Penetración"
    };

    private static (GeometriaObjetivo, ResultadoAnalisis) Caso(
        double profundidad, double profundidadObjetivo, double descentrado = 0.0)
    {
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + descentrado * Math.Cos(DatosSinteticos.Grados(g)), profundidad: profundidad);
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: profundidadObjetivo);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);
        return (perfil.GeometriaObjetivo, analisis);
    }

    [Fact]
    public void Penetracion_DentroDeTolerancia_Pasa()
    {
        var (geo, analisis) = Caso(profundidad: 1.0, profundidadObjetivo: 1.05); // desv 0.05 ≤ 0.2−0.05
        var r = MotorNormas.Evaluar(NormaUnaRegla(ReglaPenetracion()), NivelNorma.B, geo, analisis, CalidadMedicion.Metrologica);

        Assert.Equal(Veredicto.Pasa, r.VeredictoGlobal);
    }

    [Fact]
    public void Penetracion_EnLaBanda_Revisar()
    {
        var (geo, analisis) = Caso(profundidad: 1.0, profundidadObjetivo: 1.2); // desv 0.2 = límite
        var r = MotorNormas.Evaluar(NormaUnaRegla(ReglaPenetracion()), NivelNorma.B, geo, analisis, CalidadMedicion.Metrologica);

        Assert.Equal(Veredicto.Revisar, r.VeredictoGlobal);
    }

    [Fact]
    public void Penetracion_FueraDeTolerancia_NoPasa()
    {
        var (geo, analisis) = Caso(profundidad: 1.0, profundidadObjetivo: 1.5); // desv 0.5 > 0.2+0.05
        var r = MotorNormas.Evaluar(NormaUnaRegla(ReglaPenetracion()), NivelNorma.B, geo, analisis, CalidadMedicion.Metrologica);

        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(0.5, r.Reglas[0].ValorEvaluado, 6);
    }

    [Fact]
    public void CalidadIndicativa_DegradaPasaARevisar()
    {
        var (geo, analisis) = Caso(profundidad: 1.0, profundidadObjetivo: 1.0); // desviación 0 → pasaría
        var r = MotorNormas.Evaluar(NormaUnaRegla(ReglaPenetracion()), NivelNorma.B, geo, analisis, CalidadMedicion.Indicativa);

        Assert.Equal(Veredicto.Revisar, r.VeredictoGlobal);
    }

    [Fact]
    public void LimiteLineal_AplicaClampPorEspesor()
    {
        var lim = new LimiteLineal { Offset = 0, CoefEspesor = 0.1, Min = 0.1, Max = 0.5 };
        Assert.Equal(0.1, lim.Evaluar(0.0), 9);  // 0 → clamp a Min
        Assert.Equal(0.3, lim.Evaluar(3.0), 9);  // 0.3 dentro de rango
        Assert.Equal(0.5, lim.Evaluar(8.0), 9);  // 0.8 → clamp a Max
    }

    [Fact]
    public void ReglaNoAplicable_SeOmite()
    {
        // Estudio lineal: no hay descentrado radial → la regla de descentrado se omite.
        var estudio = new Estudio { IdPieza = "L1", NumeroPuesta = 1 };
        for (int i = 0; i < 6; i++)
            estudio.Muestras.Add(DatosSinteticos.Muestra(i + 1, i * 4.0, 2.0, ancho: 1.0, profundidad: 1.0));
        var perfil = new PerfilSoldadura
        {
            Tipo = TipoSoldadura.Lineal,
            GeometriaObjetivo = new GeometriaObjetivo { ProfundidadObjetivo = 1.0, Espesor = 3.0 }
        };
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var regla = new ReglaNorma
        {
            Defecto = "Descentrado", Medida = MedidaEvaluada.Descentrado, Nivel = NivelNorma.B,
            TipoLimite = TipoLimite.Maximo, Referencia = ReferenciaLimite.Absoluto,
            Limite = new LimiteLineal { Offset = 0.1 }
        };
        var r = MotorNormas.Evaluar(NormaUnaRegla(regla), NivelNorma.B, perfil.GeometriaObjetivo, analisis, CalidadMedicion.Metrologica);

        Assert.Empty(r.Reglas);
        Assert.Equal(Veredicto.Pasa, r.VeredictoGlobal);
    }

    [Fact]
    public void VeredictoGlobal_EsElPeor_YReglaMasCritica()
    {
        var norma = new Norma
        {
            Id = "TEST", Edicion = "1", Verificada = true,
            Reglas =
            {
                ReglaPenetracion(), // pasará
                new ReglaNorma
                {
                    Defecto = "Descentrado", Medida = MedidaEvaluada.Descentrado, Nivel = NivelNorma.B,
                    TipoLimite = TipoLimite.Maximo, Referencia = ReferenciaLimite.Absoluto,
                    Limite = new LimiteLineal { Offset = 0.1 }, MargenRevision = 0.0
                }
            }
        };
        var (geo, analisis) = Caso(profundidad: 1.0, profundidadObjetivo: 1.0, descentrado: 0.4); // descentrado 0.4 ≫ 0.1
        var r = MotorNormas.Evaluar(norma, NivelNorma.B, geo, analisis, CalidadMedicion.Metrologica);

        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(MedidaEvaluada.Descentrado, r.ReglaMasCritica!.Regla.Medida);
    }
}
