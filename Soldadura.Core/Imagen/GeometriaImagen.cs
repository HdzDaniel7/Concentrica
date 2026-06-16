namespace Soldadura.Core.Imagen;

/// <summary>Geometría 2D en píxeles usada por la medición en pantalla.</summary>
public static class GeometriaImagen
{
    /// <summary>Distancia euclidiana entre dos puntos (px).</summary>
    public static double Distancia(Punto2D a, Punto2D b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Distancia perpendicular del punto <paramref name="p"/> a la recta que pasa por
    /// <paramref name="rectaA"/> y <paramref name="rectaB"/> (px). Es la distancia al datum.
    /// Si la recta degenera a un punto, devuelve la distancia a ese punto.
    /// </summary>
    public static double DistanciaPerpendicular(Punto2D p, Punto2D rectaA, Punto2D rectaB)
    {
        double dx = rectaB.X - rectaA.X;
        double dy = rectaB.Y - rectaA.Y;
        double largo = Math.Sqrt(dx * dx + dy * dy);
        if (largo < 1e-12)
            return Distancia(p, rectaA);

        // |(B−A) × (P−A)| / |B−A|
        double cruz = dx * (p.Y - rectaA.Y) - dy * (p.X - rectaA.X);
        return Math.Abs(cruz) / largo;
    }
}
