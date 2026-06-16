using Soldadura.Core.Modelo;

namespace Soldadura.Core.Normas;

/// <summary>
/// Traduce las <see cref="Especificaciones"/> editables del usuario al modelo de reglas que
/// consume <see cref="MotorNormas"/>. Cada tolerancia presente se vuelve una regla de límite
/// máximo absoluto (CoefEspesor = 0); las tolerancias null se omiten.
/// </summary>
public static class ReglasDeEspecificaciones
{
    public static Norma Construir(Especificaciones e)
    {
        ArgumentNullException.ThrowIfNull(e);

        var norma = new Norma
        {
            Id = string.IsNullOrWhiteSpace(e.Nombre) ? "Especificación interna" : e.Nombre,
            Edicion = e.Fuente,
            FechaSello = DateTime.Now,
            Verificada = true // definida por el usuario: es la fuente de verdad elegida
        };

        void Agregar(string defecto, MedidaEvaluada medida, TipoLimite tipo, ReferenciaLimite referencia, double valor)
        {
            norma.Reglas.Add(new ReglaNorma
            {
                Defecto = defecto,
                Medida = medida,
                Nivel = e.Nivel,
                TipoLimite = tipo,
                Referencia = referencia,
                Limite = new LimiteLineal { Offset = valor },
                MargenRevision = e.MargenRevision,
                EtiquetaCaso = "Especificación interna"
            });
        }

        // Penetración: piso absoluto (corte más superficial ≥ mínimo)
        if (e.ProfundidadMinima is double pm)
            Agregar("Penetración insuficiente (corte mínimo)", MedidaEvaluada.ProfundidadMinima,
                    TipoLimite.Minimo, ReferenciaLimite.Absoluto, pm);

        // Penetración: techo absoluto (corte más profundo ≤ máximo)
        if (e.ProfundidadMaxima is double pM)
            Agregar("Penetración excesiva (corte máximo)", MedidaEvaluada.ProfundidadMaxima,
                    TipoLimite.Maximo, ReferenciaLimite.Absoluto, pM);

        if (e.DescentradoMaximo is double dm)
            Agregar("Descentrado", MedidaEvaluada.Descentrado, TipoLimite.Maximo, ReferenciaLimite.Absoluto, dm);
        if (e.RunoutMaximo is double rm)
            Agregar("Runout radial", MedidaEvaluada.Runout, TipoLimite.Maximo, ReferenciaLimite.Absoluto, rm);
        if (e.ToleranciaAncho is double ta)
            Agregar("Desviación de ancho", MedidaEvaluada.AnchoCordon, TipoLimite.Maximo, ReferenciaLimite.DesviacionDeObjetivo, ta);
        if (e.ExcesoCordonMaximo is double ec)
            Agregar("Exceso de cordón", MedidaEvaluada.ExcesoCordon, TipoLimite.Maximo, ReferenciaLimite.Absoluto, ec);

        return norma;
    }
}
