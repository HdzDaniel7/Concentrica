using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;
using Soldadura.Core.Normas;

namespace Soldadura.Tests;

public class NormaJsonTests
{
    [Fact]
    public void RoundTrip_PreservaReglas()
    {
        var original = NormasProvisionales.Iso13919_1_2019();

        string json = NormaJson.Guardar(original);
        var recargada = NormaJson.Cargar(json);

        Assert.Equal(original.Id, recargada.Id);
        Assert.Equal(original.Edicion, recargada.Edicion);
        Assert.Equal(original.Reglas.Count, recargada.Reglas.Count);
        Assert.Equal(original.Reglas[0].Medida, recargada.Reglas[0].Medida);
        Assert.Equal(original.Reglas[0].Limite.CoefEspesor, recargada.Reglas[0].Limite.CoefEspesor, 9);
    }

    [Fact]
    public void ProvisionalIso_EstaMarcadaNoVerificada()
    {
        var norma = NormasProvisionales.Iso13919_1_2019();

        Assert.False(norma.Verificada);
        Assert.Equal("ISO 13919-1", norma.Id);
        Assert.NotEmpty(norma.Reglas);
    }

    [Fact]
    public void ProvisionalIso_EvaluaUnEstudioYPropagaNoVerificada()
    {
        var norma = NormasProvisionales.Iso13919_1_2019();
        var estudio = DatosSinteticos.EstudioCircular(8, _ => 5.0, profundidad: 1.0);
        var perfil = DatosSinteticos.PerfilCircular(profundidadObjetivo: 1.0);
        var analisis = MotorAnalisis.Analizar(estudio, perfil);

        var r = MotorNormas.Evaluar(
            norma, NivelNorma.B, perfil.GeometriaObjetivo, analisis, CalidadMedicion.Metrologica);

        Assert.False(r.Verificada); // el veredicto arrastra que la norma es provisional
        Assert.Equal(Veredicto.Pasa, r.VeredictoGlobal); // estudio perfectamente centrado y en objetivo
    }
}
