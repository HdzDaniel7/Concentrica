using System.Windows.Media;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.ComponentModel;
using Soldadura.Core.Analisis;
using Soldadura.Core.Modelo;

namespace Soldadura.App.Visor3D;

/// <summary>
/// Construye la escena 3D de concentricidad a partir de los resultados del análisis.
/// Solo opera en soldadura circular (Radial != null); muestra mensaje si es lineal.
/// </summary>
public sealed partial class Visor3DViewModel : ObservableObject
{
    // Umbrales de severidad normalizada (|valor − objetivo| / tolerancia).
    private const double UmbralAmbar = 0.75;
    private const double UmbralRojo  = 1.00;

    // Proporciones geométricas relativas al radio del anillo.
    private const double RadioBarra            = 0.035;
    private const double RadioCentroIdeal      = 0.06;
    private const double RadioCentroReal       = 0.06;
    private const double FraccionGrossorAnillo = 0.012;
    private const int    DivCirculo            = 8;
    private const int    DivEsfera             = 12;
    private const int    PuntosAnillo          = 60;

    // Perfil del nugget de soldadura (sólido de revolución).
    private const int    DivPerfil             = 20;
    private const double FraccionCuello        = 0.5;
    private const double FraccionAlturaCuello  = 0.85;

    // Radio de referencia de la escena (se actualiza en Actualizar para escalar las etiquetas).
    public double RadioRef { get; private set; } = 5.0;

    /// <summary>Escena 3D lista para bindear como Content de un ModelVisual3D.</summary>
    public Model3DGroup Modelo { get; } = new();

    /// <summary>
    /// La vista (VistaWeld3D) asigna aquí un delegado que renderiza el viewport a PNG.
    /// Devuelve null si no hay escena o el control no está visible.
    /// </summary>
    public Func<byte[]?>? ObtenerSnapshot { get; set; }

    /// <summary>Etiqueta 2D anclada a un punto 3D; el control la proyecta a pantalla.</summary>
    public record LabelInfo(string Texto, double X, double Y, double Z);

    /// <summary>Etiquetas; el control las reposiciona al mover la cámara.</summary>
    public IReadOnlyList<LabelInfo> Labels { get; private set; } = [];

    [ObservableProperty] private string _mensajeEstado = "Ejecuta un análisis para ver la vista 3D.";

    public bool HayDatos   => string.IsNullOrEmpty(MensajeEstado) && Modelo.Children.Count > 0;
    public bool HayMensaje => !string.IsNullOrEmpty(MensajeEstado);

    partial void OnMensajeEstadoChanged(string value)
    {
        OnPropertyChanged(nameof(HayDatos));
        OnPropertyChanged(nameof(HayMensaje));
    }

    /// <summary>Reconstruye la escena completa. Llamar desde hilo de UI.</summary>
    public void Actualizar(
        IReadOnlyList<Muestra> muestras,
        ResultadoAnalisis analisis,
        GeometriaObjetivo objetivo,
        Especificaciones especificaciones)
    {
        Modelo.Children.Clear();

        if (muestras.Count == 0)
        {
            FijarLabels([]);
            MensajeEstado = "Sin muestras para visualizar.";
            return;
        }
        if (analisis.Radial is null)
        {
            FijarLabels([]);
            MensajeEstado = "Vista 3D de concentricidad: requiere análisis radial (soldadura circular con datum externo).";
            return;
        }

        MensajeEstado = "";

        var ajuste  = analisis.Radial.Ajuste;
        var recomen = analisis.Recomendacion;
        double radioRef = objetivo.RadioObjetivo ?? ajuste.RadioMedio;
        RadioRef = radioRef > 0 ? radioRef : 5.0;

        AgregarLuces();
        AgregarPlanosReferencia(RadioRef, objetivo.ProfundidadObjetivo);
        AgregarAnilloReferencia(RadioRef);
        AgregarCurvaAjustada(ajuste);
        AgregarBarrasMuestras(muestras, objetivo, especificaciones);
        AgregarCentros(ajuste);
        AgregarFlechaAjuste(ajuste, recomen);

        // RadioRef debe estar actualizado ANTES de FijarLabels para que el control pueda leer
        // el valor correcto al recibir el evento PropertyChanged(Labels).
        FijarLabels(ConstruirLabels(muestras, ajuste));
    }

    public void Limpiar()
    {
        Modelo.Children.Clear();
        FijarLabels([]);
        MensajeEstado = "Ejecuta un análisis para ver la vista 3D.";
    }

    private void FijarLabels(IReadOnlyList<LabelInfo> labels)
    {
        Labels = labels;
        OnPropertyChanged(nameof(Labels));
    }

    private List<LabelInfo> ConstruirLabels(IReadOnlyList<Muestra> muestras, AjusteArmonico ajuste)
    {
        // Offset Z relativo al radio de la escena para que las etiquetas no queden pegadas.
        double offsetZ = Math.Max(RadioRef * 0.04, 0.05);

        var labels = new List<LabelInfo>(muestras.Count + 2);
        foreach (var m in muestras)
        {
            double theta = m.AnguloOPosicion * Math.PI / 180.0;
            double r = m.PosicionCentral;
            double z = (m.ExcesoCordon > 1e-6 ? m.ExcesoCordon : 0) + offsetZ;
            labels.Add(new LabelInfo($"#{m.NumeroMuestra}", r * Math.Cos(theta), r * Math.Sin(theta), z));
        }
        labels.Add(new LabelInfo("Centro ideal", 0, 0, offsetZ));
        if (ajuste.AmplitudDescentrado > 1e-6)
            labels.Add(new LabelInfo("Centro real", ajuste.DescentradoX, ajuste.DescentradoY, offsetZ));
        return labels;
    }

    // ── Luces ────────────────────────────────────────────────────────────────────

    private void AgregarLuces()
    {
        Modelo.Children.Add(new AmbientLight { Color = Visor3DColores.LuzAmbiente });
        // Luz principal: desde arriba y frente
        Modelo.Children.Add(new DirectionalLight
        {
            Color     = Visor3DColores.LuzDireccional,
            Direction = new Vector3D(-1, -1.5, -2)
        });
        // Luz de relleno secundaria: desde el lado opuesto para dar volumen sin quemar
        Modelo.Children.Add(new DirectionalLight
        {
            Color     = Visor3DColores.LuzRelleno,
            Direction = new Vector3D(1, 0.5, -0.5)
        });
    }

    // ── Planos de referencia ─────────────────────────────────────────────────────

    private void AgregarPlanosReferencia(double radioRef, double profundidadObjetivo)
    {
        double rExt = radioRef * 1.45;
        double rInt = radioRef * 0.60; // hueco más amplio para que se vean los nuggets

        // Plano de superficie (z=0): muy translúcido para no tapar los nuggets.
        var matSup = Visor3DColores.DifusoAlfa(Visor3DColores.Superficie, Visor3DColores.OpacidadSuperficie);
        Agregar(BuildDiscoAnular(rInt, rExt, PuntosAnillo, 0), matSup);

        // Plano de profundidad objetivo (z=-prof): guía sutil de hasta dónde llegar.
        var matObj = Visor3DColores.DifusoAlfa(Visor3DColores.AnilloRef, Visor3DColores.OpacidadObjetivo);
        Agregar(BuildDiscoAnular(rInt, rExt, PuntosAnillo, -profundidadObjetivo), matObj);
    }

    // ── Anillo de referencia ─────────────────────────────────────────────────────

    private void AgregarAnilloReferencia(double radio)
    {
        double tubeR = radio * FraccionGrossorAnillo;
        var path = CirclePath(radio, PuntosAnillo);
        var mesh = BuildTube(path, tubeR, DivCirculo, closed: true);
        Agregar(mesh, Visor3DColores.Difuso(Visor3DColores.AnilloRef));
    }

    // ── Curva ajustada (1er armónico) ─────────────────────────────────────────────

    private void AgregarCurvaAjustada(AjusteArmonico ajuste)
    {
        if (ajuste.AmplitudDescentrado < 1e-6) return;

        double radioMedio = ajuste.RadioMedio;
        double tubeR = radioMedio * FraccionGrossorAnillo * 0.7;
        var path = new List<Point3D>(PuntosAnillo + 1);
        for (int i = 0; i <= PuntosAnillo; i++)
        {
            double t = 2 * Math.PI * i / PuntosAnillo;
            double r = ajuste.Evaluar(t);
            path.Add(new Point3D(r * Math.Cos(t), r * Math.Sin(t), 0));
        }
        var mesh = BuildTube(path, tubeR, DivCirculo, closed: false);
        Agregar(mesh, Visor3DColores.Difuso(Visor3DColores.CurvaAjustada));
    }

    // ── Barras de muestras ───────────────────────────────────────────────────────

    private void AgregarBarrasMuestras(
        IReadOnlyList<Muestra> muestras,
        GeometriaObjetivo objetivo,
        Especificaciones spec)
    {
        foreach (var m in muestras)
        {
            double theta = m.AnguloOPosicion * Math.PI / 180.0;
            double r = m.PosicionCentral;
            var centro = new Point3D(r * Math.Cos(theta), r * Math.Sin(theta), 0);

            double profundidad = Math.Max(m.Profundidad, 1e-4);
            double exceso = Math.Max(m.ExcesoCordon, 0);
            double ancho = m.AnchoCordon > 1e-6 ? m.AnchoCordon : RadioBarra * 2;

            // Nugget de penetración: sólido de revolución coloreado por semáforo.
            Color colorPen = ColorVeredicto(m, objetivo, spec);
            Agregar(BuildPerfilSoldadura(ancho, profundidad, centro, DivPerfil),
                    Visor3DColores.DifusoEmisor(colorPen));

            // Cúpula de corona: casquete que se apoya en z=0, material brillante.
            if (exceso > 1e-6)
            {
                Color colorCor = ColorVeredictoExceso(m, spec);
                Agregar(BuildCupulaCorona(ancho, exceso, centro, DivPerfil),
                        Visor3DColores.DifusoEmisorFuerte(colorCor));
            }
        }
    }

    private static Color ColorVeredicto(Muestra m, GeometriaObjetivo obj, Especificaciones spec)
    {
        double? minP = spec.ProfundidadMinima;
        double? maxP = spec.ProfundidadMaxima;

        if (!minP.HasValue && !maxP.HasValue)
            return Visor3DColores.Pasa; // sin especificación de penetración → neutro

        double prof  = m.Profundidad;
        double marg  = Math.Max(spec.MargenRevision, 0.01);

        // NoPasa: fuera de cualquier límite por más de un margen
        if (minP is double min && prof < min - marg) return Visor3DColores.NoPasa;
        if (maxP is double max && prof > max + marg) return Visor3DColores.NoPasa;

        // Revisar: dentro del margen de algún límite
        if (minP is double minR && prof < minR + marg) return Visor3DColores.Revisar;
        if (maxP is double maxR && prof > maxR - marg) return Visor3DColores.Revisar;

        return Visor3DColores.Pasa;
    }

    private static Color ColorVeredictoExceso(Muestra m, Especificaciones spec)
    {
        if (spec.ExcesoCordonMaximo is not double maxExc)
            return Visor3DColores.CoronaNeutral;

        double sev = m.ExcesoCordon / maxExc;
        if (sev >= UmbralRojo)  return Visor3DColores.NoPasa;
        if (sev >= UmbralAmbar) return Visor3DColores.Revisar;
        return Visor3DColores.Pasa;
    }

    // ── Centros ──────────────────────────────────────────────────────────────────

    private void AgregarCentros(AjusteArmonico ajuste)
    {
        Agregar(BuildSphere(new Point3D(0, 0, 0), RadioCentroIdeal, DivEsfera),
                Visor3DColores.DifusoEmisor(Visor3DColores.CentroIdeal));

        double cx = ajuste.DescentradoX;
        double cy = ajuste.DescentradoY;
        if (cx * cx + cy * cy < 1e-12) return;

        Agregar(BuildSphere(new Point3D(cx, cy, 0), RadioCentroReal, DivEsfera),
                Visor3DColores.DifusoEmisor(Visor3DColores.CentroReal));
    }

    // ── Flecha de ajuste ─────────────────────────────────────────────────────────

    private void AgregarFlechaAjuste(AjusteArmonico ajuste, Recomendacion recomen)
    {
        if (recomen.SoloMecanico) return;

        double cx = ajuste.DescentradoX;
        double cy = ajuste.DescentradoY;
        double amp = Math.Sqrt(cx * cx + cy * cy);
        if (amp < 1e-6) return;

        double headLen  = Math.Max(amp * 0.25, RadioBarra * 3);
        double shaftLen = amp - headLen;
        double shaftR   = RadioBarra * 0.7;
        double headR    = RadioBarra * 2.0;

        double dx = -cx / amp;
        double dy = -cy / amp;

        var p0 = new Point3D(cx, cy, 0);
        var p1 = new Point3D(cx + dx * shaftLen, cy + dy * shaftLen, 0);
        var p2 = new Point3D(0, 0, 0);

        var mat = Visor3DColores.DifusoEmisor(Visor3DColores.VectorAjuste);
        Agregar(BuildCylinder(p0, p1, shaftR, DivCirculo), mat);
        Agregar(BuildCone(p1, p2, headR, DivCirculo), mat);
    }

    // ── Helper ───────────────────────────────────────────────────────────────────

    private void Agregar(MeshGeometry3D mesh, Material material)
    {
        mesh.Freeze();
        Modelo.Children.Add(new GeometryModel3D(mesh, material) { BackMaterial = material });
    }

    // ── Generadores de malla WPF ─────────────────────────────────────────────────

    private static List<Point3D> CirclePath(double radio, int n)
    {
        var pts = new List<Point3D>(n + 1);
        for (int i = 0; i <= n; i++)
        {
            double t = 2 * Math.PI * i / n;
            pts.Add(new Point3D(radio * Math.Cos(t), radio * Math.Sin(t), 0));
        }
        return pts;
    }

    /// <summary>
    /// Sólido de revolución (nugget de soldadura): tapa plana en z=0, estrechamiento al cuello
    /// y punta redondeada hasta z=−profundidad. Normales suaves calculadas por vértice.
    /// </summary>
    private static MeshGeometry3D BuildPerfilSoldadura(double anchoCordon, double profundidad, Point3D centro, int div)
    {
        double rTop    = anchoCordon / 2;
        double rCuello = rTop * FraccionCuello;
        double zCuello = -profundidad * FraccionAlturaCuello;
        double hPunta  = profundidad - profundidad * FraccionAlturaCuello;

        var perfil = new List<(double r, double z)>
        {
            (0, 0),
            (rTop, 0),
        };

        const int segCuerpo = 3;
        for (int i = 1; i <= segCuerpo; i++)
        {
            double t = (double)i / segCuerpo;
            perfil.Add((rTop + (rCuello - rTop) * t, zCuello * t));
        }

        const int segPunta = 6;
        for (int i = 1; i <= segPunta; i++)
        {
            double a = Math.PI / 2 * i / segPunta;
            perfil.Add((rCuello * Math.Cos(a), zCuello - hPunta * Math.Sin(a)));
        }

        return BuildRevolucion(perfil, centro, div);
    }

    /// <summary>Casquete semielipsoide de la corona de exceso de material (abierto en z=0).</summary>
    private static MeshGeometry3D BuildCupulaCorona(double anchoCordon, double exceso, Point3D centro, int div)
    {
        double rTop = anchoCordon / 2;
        const int seg = 8;
        var perfil = new List<(double r, double z)>(seg + 1);
        for (int i = 0; i <= seg; i++)
        {
            double a = Math.PI / 2 * i / seg;
            perfil.Add((rTop * Math.Sin(a), exceso * Math.Cos(a)));
        }
        return BuildRevolucion(perfil, centro, div);
    }

    /// <summary>
    /// Revoluciona un perfil (r, z) alrededor del eje Z local centrado en 'centro'.
    /// Calcula normales suaves por vértice promediando los segmentos adyacentes del perfil:
    ///   N_2D = (−dz, dr) normalizado → outward en el plano meridional.
    ///   N_3D = (N2D.r·cosθ, N2D.r·sinθ, N2D.z).
    /// </summary>
    private static MeshGeometry3D BuildRevolucion(
        IReadOnlyList<(double r, double z)> perfil, Point3D centro, int div)
    {
        int n = perfil.Count;
        if (n < 2) return new MeshGeometry3D();

        // ── Normales 2D por vértice (promedio de segmentos adyacentes) ────────────
        var n2d = new (double nr, double nz)[n];
        for (int i = 0; i < n; i++)
        {
            double nrAcc = 0, nzAcc = 0;
            int cnt = 0;
            // Segmento DESPUÉS del vértice i
            if (i < n - 1)
            {
                double dr = perfil[i + 1].r - perfil[i].r;
                double dz = perfil[i + 1].z - perfil[i].z;
                double len = Math.Sqrt(dr * dr + dz * dz);
                if (len > 1e-12) { nrAcc += -dz / len; nzAcc += dr / len; cnt++; }
            }
            // Segmento ANTES del vértice i
            if (i > 0)
            {
                double dr = perfil[i].r - perfil[i - 1].r;
                double dz = perfil[i].z - perfil[i - 1].z;
                double len = Math.Sqrt(dr * dr + dz * dz);
                if (len > 1e-12) { nrAcc += -dz / len; nzAcc += dr / len; cnt++; }
            }
            if (cnt > 0) { nrAcc /= cnt; nzAcc /= cnt; }
            else { nrAcc = 1; nzAcc = 0; }
            double nlen = Math.Sqrt(nrAcc * nrAcc + nzAcc * nzAcc);
            if (nlen > 1e-12) { nrAcc /= nlen; nzAcc /= nlen; }
            n2d[i] = (nrAcc, nzAcc);
        }

        // ── Vértices, normales e índices ─────────────────────────────────────────
        var pos     = new Point3DCollection(n * div);
        var normals = new Vector3DCollection(n * div);
        var idx     = new Int32Collection((n - 1) * div * 6);

        for (int i = 0; i < n; i++)
        {
            var (r, z)   = perfil[i];
            var (nr, nz) = n2d[i];
            for (int j = 0; j < div; j++)
            {
                double a = 2 * Math.PI * j / div;
                double cos = Math.Cos(a), sin = Math.Sin(a);
                pos.Add(new Point3D(centro.X + r * cos, centro.Y + r * sin, centro.Z + z));
                normals.Add(new Vector3D(nr * cos, nr * sin, nz));
            }
        }

        for (int i = 0; i < n - 1; i++)
        {
            int row0 = i * div, row1 = (i + 1) * div;
            for (int j = 0; j < div; j++)
            {
                int j1 = (j + 1) % div;
                int a = row0 + j, b = row0 + j1, c = row1 + j1, d = row1 + j;
                idx.Add(a); idx.Add(b); idx.Add(c);
                idx.Add(a); idx.Add(c); idx.Add(d);
            }
        }

        return new MeshGeometry3D { Positions = pos, Normals = normals, TriangleIndices = idx };
    }

    /// <summary>Tubo a lo largo de un path con radio constante.</summary>
    private static MeshGeometry3D BuildTube(List<Point3D> path, double radius, int div, bool closed)
    {
        var pos = new Point3DCollection();
        var idx = new Int32Collection();
        int n = path.Count;
        if (n < 2) return new MeshGeometry3D();

        var rings = new List<List<Point3D>>(n);
        for (int i = 0; i < n; i++)
        {
            Point3D p = path[i];
            Vector3D tang;
            if (i == 0)          tang = path[1] - path[0];
            else if (i == n - 1) tang = path[n - 1] - path[n - 2];
            else                 tang = path[i + 1] - path[i - 1];
            tang.Normalize();

            Vector3D perp  = PerpTo(tang);
            Vector3D bitan = Vector3D.CrossProduct(tang, perp);

            var ring = new List<Point3D>(div);
            for (int j = 0; j < div; j++)
            {
                double a = 2 * Math.PI * j / div;
                Vector3D offset = (Math.Cos(a) * perp + Math.Sin(a) * bitan) * radius;
                ring.Add(p + offset);
            }
            rings.Add(ring);
        }

        for (int i = 0; i < n; i++)
            foreach (var pt in rings[i])
                pos.Add(pt);

        for (int i = 0; i < n - 1; i++)
        {
            int ring0 = i * div, ring1 = (i + 1) * div;
            for (int j = 0; j < div; j++)
            {
                int j1 = (j + 1) % div;
                int a = ring0 + j, b = ring0 + j1, c = ring1 + j1, d = ring1 + j;
                idx.Add(a); idx.Add(b); idx.Add(c);
                idx.Add(a); idx.Add(c); idx.Add(d);
            }
        }

        return new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
    }

    /// <summary>Cilindro de p0 a p1 con radio r.</summary>
    private static MeshGeometry3D BuildCylinder(Point3D p0, Point3D p1, double r, int div)
    {
        var pos = new Point3DCollection();
        var idx = new Int32Collection();

        Vector3D axis = p1 - p0;
        if (axis.LengthSquared < 1e-20) return new MeshGeometry3D();
        Vector3D tang  = axis; tang.Normalize();
        Vector3D perp  = PerpTo(tang);
        Vector3D bitan = Vector3D.CrossProduct(tang, perp);

        for (int end = 0; end < 2; end++)
        {
            Point3D center = end == 0 ? p0 : p1;
            for (int j = 0; j < div; j++)
            {
                double a = 2 * Math.PI * j / div;
                pos.Add(center + (Math.Cos(a) * perp + Math.Sin(a) * bitan) * r);
            }
        }

        for (int j = 0; j < div; j++)
        {
            int j1 = (j + 1) % div;
            idx.Add(j);      idx.Add(j1);       idx.Add(div + j1);
            idx.Add(j);      idx.Add(div + j1); idx.Add(div + j);
        }

        int center0 = pos.Count; pos.Add(p0);
        int center1 = pos.Count; pos.Add(p1);
        for (int j = 0; j < div; j++)
        {
            int j1 = (j + 1) % div;
            idx.Add(center0); idx.Add(j1);      idx.Add(j);
            idx.Add(center1); idx.Add(div + j); idx.Add(div + j1);
        }

        return new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
    }

    /// <summary>Cono de p0 (base) a p1 (punta) con radio de base r.</summary>
    private static MeshGeometry3D BuildCone(Point3D p0, Point3D p1, double r, int div)
    {
        var pos = new Point3DCollection();
        var idx = new Int32Collection();

        Vector3D axis = p1 - p0;
        if (axis.LengthSquared < 1e-20) return new MeshGeometry3D();
        Vector3D tang  = axis; tang.Normalize();
        Vector3D perp  = PerpTo(tang);
        Vector3D bitan = Vector3D.CrossProduct(tang, perp);

        for (int j = 0; j < div; j++)
        {
            double a = 2 * Math.PI * j / div;
            pos.Add(p0 + (Math.Cos(a) * perp + Math.Sin(a) * bitan) * r);
        }
        int apex  = pos.Count; pos.Add(p1);
        int base0 = pos.Count; pos.Add(p0);

        for (int j = 0; j < div; j++)
        {
            int j1 = (j + 1) % div;
            idx.Add(j); idx.Add(j1); idx.Add(apex);
            idx.Add(base0); idx.Add(j1); idx.Add(j);
        }

        return new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
    }

    /// <summary>Esfera UV centrada en center con radio r.</summary>
    private static MeshGeometry3D BuildSphere(Point3D center, double r, int div)
    {
        var pos = new Point3DCollection();
        var idx = new Int32Collection();

        int stacks = div;
        int slices  = div * 2;

        pos.Add(center + new Vector3D(0, 0, r));

        for (int i = 1; i < stacks; i++)
        {
            double phi = Math.PI * i / stacks;
            double z   = r * Math.Cos(phi);
            double rxy = r * Math.Sin(phi);
            for (int j = 0; j < slices; j++)
            {
                double theta = 2 * Math.PI * j / slices;
                pos.Add(center + new Vector3D(rxy * Math.Cos(theta), rxy * Math.Sin(theta), z));
            }
        }

        pos.Add(center + new Vector3D(0, 0, -r));
        int southPole = pos.Count - 1;

        for (int j = 0; j < slices; j++)
        {
            int j1 = (j + 1) % slices;
            idx.Add(0); idx.Add(1 + j); idx.Add(1 + j1);
        }

        for (int i = 0; i < stacks - 2; i++)
        {
            int row0 = 1 + i * slices;
            int row1 = row0 + slices;
            for (int j = 0; j < slices; j++)
            {
                int j1 = (j + 1) % slices;
                idx.Add(row0 + j);  idx.Add(row0 + j1); idx.Add(row1 + j1);
                idx.Add(row0 + j);  idx.Add(row1 + j1); idx.Add(row1 + j);
            }
        }

        int lastRow = 1 + (stacks - 2) * slices;
        for (int j = 0; j < slices; j++)
        {
            int j1 = (j + 1) % slices;
            idx.Add(southPole); idx.Add(lastRow + j1); idx.Add(lastRow + j);
        }

        return new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
    }

    /// <summary>Disco anular plano en z=zPlano (corona circular interior-exterior).</summary>
    private static MeshGeometry3D BuildDiscoAnular(double rInt, double rExt, int div, double zPlano)
    {
        var pos = new Point3DCollection();
        var idx = new Int32Collection();

        for (int j = 0; j < div; j++)
        {
            double a = 2 * Math.PI * j / div;
            double cos = Math.Cos(a), sin = Math.Sin(a);
            pos.Add(new Point3D(rInt * cos, rInt * sin, zPlano));
            pos.Add(new Point3D(rExt * cos, rExt * sin, zPlano));
        }

        for (int j = 0; j < div; j++)
        {
            int j1 = (j + 1) % div;
            int i0 = j  * 2, e0 = j  * 2 + 1;
            int i1 = j1 * 2, e1 = j1 * 2 + 1;
            idx.Add(i0); idx.Add(e0); idx.Add(e1);
            idx.Add(i0); idx.Add(e1); idx.Add(i1);
            idx.Add(i0); idx.Add(e1); idx.Add(e0);
            idx.Add(i0); idx.Add(i1); idx.Add(e1);
        }

        return new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
    }

    private static Vector3D PerpTo(Vector3D v)
    {
        Vector3D perp = Math.Abs(v.X) < 0.9 ? new Vector3D(1, 0, 0) : new Vector3D(0, 1, 0);
        return Vector3D.CrossProduct(v, perp);
    }
}
