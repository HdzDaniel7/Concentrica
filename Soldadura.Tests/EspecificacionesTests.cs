using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;

namespace Soldadura.Tests;

public class EspecificacionesTests
{
    [Fact]
    public void Construir_OmiteToleranciasNulas()
    {
        var spec = new Especificaciones
        {
            ProfundidadMinima = 0.8,
            ProfundidadMaxima = null, // no evaluada
            DescentradoMaximo = null,
            RunoutMaximo = 0.3,
            ToleranciaAncho = null
        };

        var norma = ReglasDeEspecificaciones.Construir(spec);

        Assert.Equal(2, norma.Reglas.Count);
        Assert.Contains(norma.Reglas, r => r.Medida == MedidaEvaluada.ProfundidadMinima);
        Assert.Contains(norma.Reglas, r => r.Medida == MedidaEvaluada.Runout);
        Assert.DoesNotContain(norma.Reglas, r => r.Medida == MedidaEvaluada.Descentrado);
        Assert.DoesNotContain(norma.Reglas, r => r.Medida == MedidaEvaluada.ProfundidadMaxima);
        Assert.True(norma.Verificada); // definida por el usuario
    }

    [Fact]
    public void Construir_AmbosLimitesPenetracion_GeneraDosReglas()
    {
        var spec = new Especificaciones { ProfundidadMinima = 0.8, ProfundidadMaxima = 1.4 };

        var norma = ReglasDeEspecificaciones.Construir(spec);

        Assert.Contains(norma.Reglas, r => r.Medida == MedidaEvaluada.ProfundidadMinima
                                        && r.TipoLimite == TipoLimite.Minimo);
        Assert.Contains(norma.Reglas, r => r.Medida == MedidaEvaluada.ProfundidadMaxima
                                        && r.TipoLimite == TipoLimite.Maximo);
    }

    [Fact]
    public void Evaluar_DescentradoDentroDeToleranciaDefinidaPorUsuario_Pasa()
    {
        // Descentrado real 0.10; el usuario fija tolerancia 0.20 → pasa.
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.10 * Math.Cos(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = 0.8, DescentradoMaximo = 0.20, RunoutMaximo = 0.50, MargenRevision = 0.03
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.Equal(Veredicto.Pasa, r.VeredictoGlobal);
    }

    [Fact]
    public void Evaluar_DescentradoFueraDeToleranciaMasEstricta_NoPasa()
    {
        // Mismo descentrado 0.10; ahora el usuario fija tolerancia estricta 0.05 → no pasa.
        var estudio = DatosSinteticos.EstudioCircular(
            8, g => 5 + 0.10 * Math.Cos(DatosSinteticos.Grados(g)));
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = 0.8, DescentradoMaximo = 0.05, RunoutMaximo = 0.50, MargenRevision = 0.0
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(MedidaEvaluada.Descentrado, r.ReglaMasCritica!.Regla.Medida);
    }

    [Fact]
    public void Evaluar_ExcesoCordon_RepruebaSiUnSoloCorteSupera()
    {
        // 8 cortes bien centrados; el exceso medio es bajo (0.05) pero un corte tiene 0.30.
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0);
        foreach (var m in estudio.Muestras) m.ExcesoCordon = 0.05;
        estudio.Muestras[3].ExcesoCordon = 0.30; // peor corte

        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = null, DescentradoMaximo = null, RunoutMaximo = null,
            ExcesoCordonMaximo = 0.20, MargenRevision = 0.0
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        // La media (0.08) pasaría, pero el peor corte (0.30) reprueba.
        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(MedidaEvaluada.ExcesoCordon, r.ReglaMasCritica!.Regla.Medida);
    }

    [Fact]
    public void ProfundidadMinima_CorteSuperficialBajoElPiso_NoPasa()
    {
        // 8 cortes con profundidad 1.0; uno tiene 0.70. Piso = 0.8 → ese corte falla.
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        estudio.Muestras[3].Profundidad = 0.70;

        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = 0.80,
            ProfundidadMaxima = null,
            DescentradoMaximo = null,
            RunoutMaximo = null,
            MargenRevision = 0.0
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(MedidaEvaluada.ProfundidadMinima, r.ReglaMasCritica!.Regla.Medida);
    }

    [Fact]
    public void ProfundidadMaxima_CorteProundoSobreElTecho_NoPasa()
    {
        // 8 cortes con profundidad 1.0; uno tiene 1.60. Techo = 1.40 → ese corte falla.
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        estudio.Muestras[5].Profundidad = 1.60;

        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = null,
            ProfundidadMaxima = 1.40,
            DescentradoMaximo = null,
            RunoutMaximo = null,
            MargenRevision = 0.0
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.Equal(Veredicto.NoPasa, r.VeredictoGlobal);
        Assert.Equal(MedidaEvaluada.ProfundidadMaxima, r.ReglaMasCritica!.Regla.Medida);
    }

    [Fact]
    public void ProfundidadMinima_SinMaximo_PasaSiCorteMinEncima()
    {
        // Todos los cortes >= 1.0; piso = 0.8 → pasa (no hay techo).
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);

        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var spec = new Especificaciones
        {
            ProfundidadMinima = 0.80,
            ProfundidadMaxima = null,
            DescentradoMaximo = null,
            RunoutMaximo = null,
            MargenRevision = 0.0
        };
        var norma = ReglasDeEspecificaciones.Construir(spec);
        var r = MotorNormas.Evaluar(norma, spec.Nivel, perfil.GeometriaObjetivo, analisis,
            CalidadMedicion.Metrologica, estudio.Muestras);

        Assert.Equal(Veredicto.Pasa, r.VeredictoGlobal);
    }
}
