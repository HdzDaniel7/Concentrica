# Contexto de proyecto — Analizador de concentricidad de soldadura

**Para Claude:** este documento es el estado y la especificación del proyecto. Léelo como contexto.
No re-expliques lo ya decidido; continúa desde la sección "17. Estado actual". Si propones cambios,
preséntalos como ajustes puntuales a la sección correspondiente. Respuestas concisas.
Al cerrar cada sesión, entrega una versión actualizada de las secciones 17 y 18.

## 1. Objetivo
App de escritorio local y AUTÓNOMA que, a partir de mediciones metalográficas de soldaduras (láser; circular o
lineal), evalúa centrado/concentricidad y penetración contra una norma, marca en 3D lo fuera de especificación,
y recomienda el ajuste de robot (X/Y para centrado, Z para enfoque) o avisa cuándo solo queda ajuste mecánico.
La captura y la visualización de imágenes ocurren DENTRO de la app; no depende de Excel ni de otros programas en
tiempo de ejecución (aunque sus salidas se puedan revisar en otros programas).

## 2. Decisiones cerradas
- Plataforma: escritorio local Windows (web descartada por ahora).
- Stack: C# / .NET (LTS vigente) + WPF (MVVM) + Helix Toolkit (3D) + Math.NET Numerics.
- Distribución: publish self-contained, instalador único.
- Arquitectura: núcleo de cálculo separado de UI y de 3D.
- App AUTÓNOMA: todos los datos se capturan dentro del programa. SIN importación de Excel.
- Persistencia: historial en CARPETAS LOCALES elegidas por el usuario; datos por estudio en JSON;
  índice/búsqueda opcional en SQLite.
- Captura flexible: medición en pantalla OPCIONAL; el técnico puede solo anotar medidas hechas en el microscopio.
- Primer caso completo: soldadura láser, ISO 13919-1:2019, nivel B. Otros procesos/normas = costuras a rellenar después.

## 3. Hechos del dominio (no re-derivar)
- Datos desde imagen de microscopio Nikon (escala px→mm calibrada; las imágenes se ven y se pueden medir dentro de la app).
- Muestreo destructivo: un corte = un plano angular.
- Modelo de referencia base (el usado): datum = cara plana externa perpendicular a la dirección de medición.
  distanciaBordeCercano = datum→borde más cercano del cordón; distanciaBordeLejano = datum→borde más lejano.
  anchoCordon = distanciaBordeLejano − distanciaBordeCercano;
  posicionCentral = (distanciaBordeCercano + distanciaBordeLejano)/2.
- Preparación: solo "pulida + atacada con ácido" es metrológica. Sin atacar / mal pulida / sin pulir = no confiable
  para ubicar límites de fusión.
- Proceso real: la pieza gira sobre un motor (fuente de runout); el robot Fanuc queda fijo durante la soldadura.
  Ajustes finos normalmente en Y (perpendicular a la cara); X/Y para centrado; Z para enfoque/penetración.
  El proyecto compensa el error mecánico que se acumula entre ajustes.

## 4. Modelos de referencia (seleccionables por perfil)
1. Datum plano externo (base).  2. Radial desde centro/eje.  3. Dos features / línea base.
4. Contorno de pieza.  5. Solo geometría del cordón.
Cada modelo declara: medidas crudas que pide + fórmulas para anchoCordon / posicionCentral / profundidad / posición.

## 5. Motor de análisis (dos ejes)
- Radial (en el plano) → ajuste X/Y. Ajustar radioLineaCentral(angulo) a serie de Fourier:
  radioMedio + a·cos + b·sin + armónicos superiores.
  1er armónico (a,b) = descentrado (corregible con robot; ajuste ≈ −a, −b → ajusteX / ajusteY).
  2º = ovalidad. 3º+ = vibración/ondulación. Solo el 1ero se corrige con offset; el resto = mecánico.
- Profundidad (axial) → ajusteZ/enfoque. profundidadMedia vs profundidadObjetivo = corrección de foco
  (dirección por defecto; magnitud = coeficiente OPCIONAL aprendido por perfil de datos históricos,
  NO hardcodear: depende de potencia y materiales). 1er armónico de profundidad = cabeceo / runout axial.
- Estadística: media ± σ de profundidad y ancho; runout/TIR; desviación por muestra;
  puntoMasSensible = mayor desviación NORMALIZADA a su tolerancia; opcional Cp/Cpk.
- fraccionCorregible = porción del runout que es 1er armónico. Bajo umbral → recomendar ajuste mecánico.
- Lineal: regresión de posicionCentral a lo largo del largo (pendiente = deriva; ordenada = desplazamiento medio).

## 6. Muestreo
- Default: 4 muestras simétricas con referencia a 0°.
- Configurable: cantidad; simétrico por ángulo / por distancia / posiciones personalizadas. Huecos → interpolación.
- Resolución honesta: para resolver k armónicos se necesitan ≥ 2k+1 muestras. 4 → centrado sí, óvalo no;
  8 → hasta ~3er armónico. El programa avisa el límite según N.
- Registro angular: modo CON marca física de 0° (da ajusteX, ajusteY exactos vía calibración a ejes Fanuc)
  vs SIN marca (magnitud y separación descentrado/óvalo válidas; dirección no). Recomendado: poner marca de 0°.

## 7. Calidad de medición (por muestra)
- Metrológica (baquelita pulida+atacada, corte perpendicular verificado): permite recomendar ajustes finos de robot.
- Indicativa (montaje rápido/plastilina, posible cabeceo): ensanchar incertidumbre; solo veredicto
  pasa/no pasa/revisar; NO ajuste fino.
- Corrección de inclinación: giro en el plano de la imagen = inofensivo (rotar marco de medición);
  cabeceo fuera de plano = alarga por 1/cos θ y desenfoca (el desenfoque permite estimar θ).
  Mejor práctica: nivelar el montaje o medir una feature de dimensión conocida para sacar θ.

## 8. Motor de normas (declarativo)
- Reglas en JSON/YAML: { tipo soldadura, defecto, nivel B/C/D, fórmula del límite en función del espesor }.
- Cada norma/opción lleva una etiqueta breve de caso de uso visible en la UI.
- VERIFICAR los valores contra la edición oficial antes de producción; guardar sello norma/edición/fecha en cada perfil.
- Guardar siempre dato crudo y dato corregido por separado (trazabilidad).
- Referencias: ISO 13919-1:2019 = láser y haz de electrones en acero/níquel/titanio, examen visual/dimensional,
  nivel B = más exigente. Arco → ISO 5817. NOM-027-STPS-2008 = seguridad/higiene, NO criterios de imperfección.
  En México la aceptación suele venir de NMX de producto o de normas internacionales adoptadas por contrato.

## 9. Visor de imágenes y modos de captura
- modoCaptura (por estudio o por muestra), a elección del técnico:
  · AnotacionDirecta: mide en el microscopio y solo escribe los números (más rápido). NO requiere imagen ni escala.
  · MedicionEnPantalla (OPCIONAL): clic sobre la imagen para marcar datum y bordes → calcula
    distanciaBordeCercano, distanciaBordeLejano y profundidad con la escala.
- Visor: elegir carpeta de imágenes; tira de miniaturas con nombres de archivo; navegación con botones,
  flechas del teclado y clic. Formatos PNG, JPG, TIFF. Lienzo grande con zoom (rueda), pan y ajustar-a-ventana / 1:1.
- Especificaciones de microscopio guardables (perfilMicroscopio + objetivo → escalaMmPorPixel) para no recalibrar;
  también leer metadatos de la imagen cuando existan, o escribir mm/píxel a mano.
- En MedicionEnPantalla: guardar coordenadas en píxeles de las marcas (auditable/reproducible) y un overlay junto a la cruda.
- Cada muestra puede (no debe) ligarse a una imagen (ruta relativa dentro de la carpeta del estudio).

## 10. Gestión de archivos e historial (carpetas, elegidas por el usuario)
- El usuario elige la carpeta raíz del historial, la del estudio y la de imágenes. Sin rutas fijas.
  Puede apuntar a una carpeta existente de imágenes del microscopio, recorrerlas y llenar los datos desde ahí.
- Esquema de carpetas (CERRADO):
    <Raíz>/ <idPieza>/ Puesta<numeroPuesta>_<AAAA-MM-DD>/ { datos.json, imagenes/, overlays/, reporte.pdf }
  · fecha AAAA-MM-DD (ordena sola); numeroPuesta único por pieza → sin colisiones.
  · zonaPieza NO va en el nombre de carpeta: vive en datos.json (nombres cortos y estables).
  · la app genera el nombre automáticamente; nombres de archivo saneados (sin caracteres inválidos).
- Un estudio = una puesta = una carpeta autocontenida. Varias puestas de la misma pieza = carpetas hermanas bajo idPieza.
- Historial = navegar carpetas; panel de estudios pasados, abribles y buscables/filtrables por idPieza / puesta / fecha / zonaPieza
  (el navegador lee datos.json para mostrar zona).
- Compartir = copiar o comprimir la carpeta del estudio.
- Renombrado SEGURO desde la app (archivos, estudios, perfiles): si algo está referenciado, actualizar la
  referencia o avisar antes de romper el vínculo. Autoguardado / borrador para no perder la captura.

## 11. Identificación: pieza, puesta, muestra
- idPieza: identifica la pieza física; PUEDE repetirse entre estudios.
- numeroPuesta: distingue cada corrida/puesta de soldadura sobre la misma pieza. (idPieza + numeroPuesta + fecha = identidad del estudio.)
- Por muestra: numeroMuestra (secuencial), orden (cuál sigue a cuál; reordenable), anguloOPosicion.
- zonaPieza: etiqueta legible de en qué parte física de la pieza está la soldadura (cara, brida, cordón A/B…),
  independiente del ángulo. Funcionamiento:
  · Catálogo por perfil (desplegable) + texto libre; una zona nueva se puede guardar al catálogo.
  · Se define a nivel ESTUDIO y las muestras la HEREDAN; se puede sobrescribir por muestra si aplica.
  · Uso: filtrar/agrupar el historial por zona, comparar deriva por zona, dar contexto en el reporte.

## 12. Accesibilidad y UX
- Navegación por teclado completa (tab entre campos; flechas entre imágenes/muestras; atajos siguiente/anterior).
- Validación en vivo: marcar valores fuera de rango mientras se escribe.
- Plantillas de captura: el perfil define qué campos pedir; el formulario se genera solo.
- Recordar el último modoCaptura y la última carpeta usada.
- Tema claro/oscuro; opción de fuente grande.
- Exportar reporte a PDF y datos a CSV/JSON (solo SALIDA, nunca como entrada/dependencia).
- Vista de comparación: superponer estudio actual vs previo (misma pieza, otra puesta) para ver deriva.
- Undo/redo en las mediciones sobre imagen.

## 13. Arquitectura en capas
- Núcleo (C# puro): modelo de datos, geometría, modelos de referencia, motor de análisis, motor de normas. Testeable.
- Visualización 3D: Helix Toolkit; malla construida UNA vez, re-render solo al cambiar datos; panel propio grande y
  acoplable; rojo = fuera de spec; línea guía de concentricidad + vector de ajuste recomendado.
- Visor de imágenes: lienzo zoomable/paneable + tira de miniaturas + herramientas de medición sobre imagen (opcional).
- UI: WPF (MVVM) con paneles acoplables y redimensionables (p. ej. AvalonDock); imagen y 3D grandes, cada uno
  maximizable; formulario de muestra y resultados en panel lateral.
- Persistencia: carpetas locales + JSON por estudio; índice/búsqueda opcional en SQLite. Exportación PDF/CSV/JSON.

## 14. Nomenclatura (nombres de variables; en código C# = PascalCase)
- distanciaBordeCercano (antes dist_1) · distanciaBordeLejano (antes dist_2) · profundidad (antes depths/depth)
- anchoCordon = lejano − cercano · posicionCentral = (cercano + lejano)/2 · radioLineaCentral = posicionCentral en circular
- anguloOPosicion (antes angles / x lineal) · puntosInterpolacion (antes N_SMOOTH)
- radioObjetivo (antes goal) · profundidadObjetivo (antes Dgoal) · espesor (antes t)
- radioMedio (r0) · descentradoX, descentradoY (a,b) · ovalidad (2º armónico) · runout · fraccionCorregible
- ajusteX, ajusteY, ajusteZ · escalaMmPorPixel · perfilMicroscopio · objetivo · modoCaptura

## 15. Modelo de datos (resumen)
- PerfilSoldadura { nombre; tipo (lineal/circular); modeloReferencia; geometríaObjetivo (radioObjetivo/diámetro,
  profundidadObjetivo, anchoObjetivo, espesor); norma+nivel; configMuestreo; camposCaptura; zonasCatalogo[]; coefFocoZ opcional }.
- Estudio { idPieza; numeroPuesta; fecha; carpeta; perfil; zonaPieza (default del estudio); muestras[];
  resultados (armónicos, estadística, veredictos); recomendación (ajusteX, ajusteY, ajusteZ); calidadGlobal }.
- Muestra { numeroMuestra; orden; anguloOPosicion; zonaPieza (hereda del estudio, override opcional); distanciaBordeCercano;
  distanciaBordeLejano; profundidad; calidadMedicion; modoCaptura; imagen (ruta relativa, opcional);
  escalaMmPorPixel; coordenadasPixeles; overlay }.

## 16. Fases
1. Núcleo: modelo de datos + motor de análisis (con datos de prueba).
2. Captura: formulario de muestras (AnotacionDirecta) + identidad pieza/puesta/muestra + historial en carpetas.
3. Visor de imágenes + MedicionEnPantalla (opcional) + specs de microscopio.
4. Motor de normas (cargar ISO 13919-1 verificada).
5. 3D con marcado en rojo + guía de concentricidad.
6. UI integrada (layout acoplable, resumen, comparación).
7. Reporte exportable (PDF) + export CSV/JSON.
8. Histórico y tendencia de runout (predicción de cuándo tocará ajuste mecánico).

## 17. Estado actual
- Entorno: Git, .NET 10 SDK, VS Code + C# Dev Kit, Claude Code. Solución Soldadura.slnx en
  C:\repos\soldadura-analyzer con 3 proyectos: Soldadura.Core (núcleo puro, testeable),
  Soldadura.App (WPF, MVVM con CommunityToolkit.Mvvm + Dirkster.AvalonDock 4.74.1 + HelixToolkit.Wpf),
  Soldadura.Tests (xUnit). Paquetes: MathNet.Numerics, Microsoft.Data.Sqlite, QuestPDF 2026.6.0.
- BUILD: 0 errores, 0 advertencias. 80 pruebas verdes.
### Núcleo (Soldadura.Core)

**Modelo de datos** (Soldadura.Core/Modelo):
- `GeometriaObjetivo` (ProfundidadObjetivo, Espesor, AnchoObjetivo?), `ConfigMuestreo`, `PerfilSoldadura`,
  `Muestra` (AnchoCordon / PosicionCentral derivados; ExcesoCordon; MarcasMedicion), `Estudio`
  (IdPieza + NumeroPuesta + Fecha = identidad; ZonaPieza heredada por muestra; `AjusteAplicado`?).
- `AjusteAplicado` (X/Y/Z nullable): ajuste del robot realmente aplicado para la puesta (trazabilidad
  + insumo del aprendizaje foco↔Z). Se guarda con el Estudio; null por eje = no registrado.
- `Especificaciones` (criterio interno del usuario, se guarda con el estudio y con las plantillas):
  `ProfundidadMinima`? (piso absoluto, corte más superficial ≥ valor) y `ProfundidadMaxima`? (techo
  absoluto, corte más profundo ≤ valor) — reemplazaron `ToleranciaPenetracion` (±) que fue eliminado.
  `DescentradoMaximo`?, `RunoutMaximo`?, `ToleranciaAncho`?, `ExcesoCordonMaximo`?, `MargenRevision`.
- `PlantillaPerfil` (Nombre, Descripcion, Tipo, ModeloReferencia, GeometriaObjetivo, Especificaciones,
  ZonasCatalogo, CoefFocoZ, NombreEjeX/Y/Z, AnguloEjesGrados): plantilla reutilizable indep. del Estudio.
  Los campos de eje permiten nombrar los ejes del robot y rotar el vector de ajuste X/Y al marco
  físico del robot (AnguloEjesGrados = rotación horaria desde la marca 0° hasta el eje X del robot).

**Motor de análisis** (Soldadura.Core/Analisis):
- `AjustadorArmonico`: Fourier por mínimos cuadrados (Math.NET QR); muestreo no uniforme y huecos;
  K recortado a (N−1)/2 (resolución honesta).
- `Estadistica`: media, σ muestral, rango, regresión lineal.
- Resultados: `AjusteArmonico` (DescentradoX/Y=A₁/B₁, Ovalidad=2º armónico), `EstadisticaSerie`
  (Media, Sigma, Min, Max, Rango), `ResultadoRadial` (Runout=TIR, FraccionCorregible),
  `ResultadoAxial` (profundidad+exceso por estadística, Cabeceo=1er armónico axial),
  `ResultadoLineal` (pendiente=deriva, ordenada=desplazamiento), `Recomendacion` (AjusteX, AjusteY,
  AjusteZ±CoefFocoZ, DireccionZ, SoloMecanico), `ResultadoAnalisis`.
- `ModelosReferencia.TieneDatumExterno(modelo)`: false solo para SoloCordon. Los 4 modelos con datum
  comparten la aritmética (cercano,lejano)→(ancho,posición); SoloCordon no tiene referencia externa.
- `MotorAnalisis.Analizar`: bifurca circular/lineal; SoloMecanico si calidad Indicativa o
  FraccionCorregible < 0.5; avisos de resolución / marca 0° / CoefFocoZ / trayecto lineal.
  Si el modelo NO tiene datum externo (SoloCordon): NO calcula radial ni lineal (Radial=Lineal=null),
  solo axial (penetración) + ancho; aviso «solo geometría del cordón»; X/Y no aplican (Z sí).
  MotorNormas ya omite reglas de descentrado/runout/ancho cuando Radial/Lineal son null.
  Circular: AjusteX=−A₁, AjusteY=−B₁. Lineal: AjusteX=0, AjusteY=−ordenada (desplazamiento lateral);
  aviso adicional si |pendiente|>0 (deriva → corrección angular del trayecto).
- `AnalisisTendencia`: `PuntoTendencia`, `ResultadoTendencia`, `MotorTendencia.Analizar(serie,
  runoutMaximo?)` → filtra Radial≠null, ordena por puesta, regresión lineal,
  `PuestaCruceLimite` = ceil((limMax−ordenada)/pendiente) si pendiente>0 y cruce > última puesta.
- `AprendizajeFoco`: `PuntoFoco`, `ResultadoAprendizajeFoco` (CoefFocoZ?, R2?), `MotorAprendizajeFoco
  .Aprender(serie de (puesta, Z aplicado?, profundidadMedia))` → filtra Z≠null, exige ≥2 puntos con
  variación en Z, regresión profundidadMedia vs Z → pendiente = CoefFocoZ (mm/mm) + R²; mensajes
  honestos si faltan datos / sin variación / pendiente≈0. Cierra el lazo: el coeficiente aprendido
  se guarda en el perfil y entonces la recomendación en Z deja de ser solo direccional.

**Motor de normas** (Soldadura.Core/Normas):
- Modelo declarativo: `Norma`, `ReglaNorma` (Defecto, Medida, TipoLimite, Referencia, Limite,
  MargenRevision), `LimiteLineal` (clamp(Offset + CoefEspesor·t, Min, Max)).
- Enums: `Veredicto`, `MedidaEvaluada` (Profundidad, ProfundidadMinima, ProfundidadMaxima,
  AnchoCordon, Descentrado, Runout, ExcesoCordon), `TipoLimite` (Maximo/Minimo), `ReferenciaLimite`.
- `MotorNormas.Evaluar` → `ResultadoNormas` (VeredictoGlobal, ResultadoRegla[], ReglaMasCritica,
  MuestraMasCritica). Calidad Indicativa degrada Pasa→Revisar. Reglas no aplicables se omiten.
- `MuestraCritica` (severidad = |valor−objetivo|/límite) para reglas DesviacionDeObjetivo.
- `ReglasDeEspecificaciones.Construir(Especificaciones)` → `Norma` (Verificada=true):
  - ProfundidadMinima → TipoLimite.Minimo, Absoluto (Min_medido ≥ piso).
  - ProfundidadMaxima → TipoLimite.Maximo, Absoluto (Max_medido ≤ techo).
  - ExcesoCordon → Absoluto contra Max de la serie (cualquier corte que supere = NoPasa).
  - Las demás reglas son TipoLimite.Maximo.
- Ruleset ISO 13919-1:2019 PLACEHOLDER embebido (Verificada=false): solo para tests, no cargable en UI.

**Persistencia** (Soldadura.Core/Persistencia):
- `EstudioRepositorio`: esquema `<Raíz>/<idPieza>/Puesta<n>_<AAAA-MM-DD>/{datos.json,imagenes/,overlays/}`;
  Guardar (copia imágenes absolutas a relativas), Cargar, Listar, ActualizarDatos, Sanear.
- `EstudioJson`: System.Text.Json + enums como texto; `[JsonIgnore]` en derivados.
- `PerfilRepositorio`: CRUD en `<Raíz>/perfiles-soldadura/<slug>.json`; Listar ordenado alfa;
  Eliminar (no lanza si no existe).
- `ExportadorCsv.MuestrasACsv`: punto decimal invariante, zona heredada.
- `ReportePdf` (QuestPDF 2026.6.0 Community): `Generar(ruta, estudio, analisis, veredicto, esCircular,
  carpetaEstudio?, render3d?)`. Parámetros opcionales para embeber imágenes.
  Secciones: 1 identificación · 2 especificaciones · 3 recomendación X/Y/Z · 4 resultados ·
  5 detalle por criterio · 6 muestra crítica · 7 tabla muestras ·
  8 Vista 3D (PNG capturado del HelixViewport3D, si se proporciona) ·
  9 Imágenes de medición (overlays en 2 columnas, solo muestras con RutaOverlay + carpeta provista) ·
  10 avisos. Sección 3: X/Y = "n/a" + nota "Centrado: no aplica" cuando el modelo no tiene datum externo.

**Imagen** (Soldadura.Core/Imagen):
- `MedicionEnPantalla`: 8 marcas (DatumA/B, BordeCercano/Lejano, SuperficieA/B, Fondo, Corona).
  Profundidad = perpendicular de Fondo a la recta Superficie; ExcesoCordon = perpendicular de Corona.
- `MarcasMedicion`: DTO serializable; `AMedicion()`, `TieneBase`.
- `PerfilMicroscopio`: escala mm/px + calibración por feature conocida.

### UI (Soldadura.App)

**Layout**: `DockingManager` (Dirkster.AvalonDock 4.74.1, xmlns `https://github.com/Dirkster99/AvalonDock`).
6 paneles: izquierda (340 px): "Perfil · Estudio · Objetivo · Especificaciones" + "Historial";
centro: documentos "Muestras" / "Imagen · Medición" / "Vista 3D";
derecha (380 px): "Resultados" + "Tendencia de runout".

**Estilos y temas** (App.xaml + Window.Resources + Temas/):
- `Temas/TemaClaro.xaml` y `Temas/TemaOscuro.xaml`: 20 recursos de color cada uno
  (PincelFondoVentana/Panel/Control/Lectura/Cabecera/Alt/Resultado, TextoPrimario/Secundario/
  Desactivado/Lectura/Estado, PincelPrimario, PincelBorde/Foco/Error, PincelFondoBoton/Hover/Pres,
  PincelSeparador, PincelFondoDiagrama/PiezaDiagrama/TextoDiagrama).
- `ThemeManager.Aplicar(bool oscuro)`: intercambia el `MergedDictionary` del tema en runtime;
  el cambio es instantáneo sin reiniciar. Preferencia persiste en `ConfiguracionApp.TemaOscuro`.
- Barra de estado (StatusBar) debajo del DockingManager: título de app a la izquierda + botón toggle
  "🌙 Oscuro" / "☀ Claro" a la derecha (`CambiarTemaCommand`); siempre visible sin tapar el workspace.
- Todos los estilos implícitos (Button, GroupBox, DataGrid, DataGridRow, DataGridColumnHeader,
  ListBox, ComboBox) y keyed ("Lbl", "Campo", "CampoLectura") usan `DynamicResource` → adaptan al tema.
- `PositiveDoubleRule`: `ValidationRule` WPF (acepta coma o punto; rechaza ≤0 o vacío).
- `DoubleFlexibleConverter`: IValueConverter coma/punto bidireccional.
- `NullableDoubleConverter`: como el anterior pero vacío↔null y admite negativos (para el ajuste
  aplicado del robot, que puede ir en cualquier dirección o no haberse registrado).
- `ModeloReferenciaConverter`: nombres legibles del enum `ModeloReferencia` para el ComboBox (presentación).
- `ConfiguracionApp`: persiste `RaizHistorial` y `TemaOscuro` en `%LOCALAPPDATA%\Concentrica\config.json`.

**Panel izquierdo** (`MainWindow.xaml`):
- GroupBox "Perfil de soldadura": ComboBox de plantillas + Cargar/Eliminar; TextBox nombre + Guardar.
- GroupBox "Estudio": IdPieza|Puesta# / Fecha|Zona (DatePicker).
- GroupBox "Objetivo": TipoSoldadura (ComboBox) / ModeloReferencia (ComboBox con nombres legibles) /
  ProfundidadObjetivo|Espesor / CheckBox TieneMarcaCero /
  fila de ejes del robot en 3 columnas (NombreEjeX, NombreEjeY, NombreEjeZ) + botón "Ver diagrama de ejes…"
  que abre `DiagramaEjesWindow` (`VerDiagramaEjesCommand`) + botón "Ver modelos de referencia…"
  que abre `ModelosReferenciaWindow` (`VerModelosReferenciaCommand`).
- GroupBox "Especificaciones": ProfundidadMinima | ProfundidadMaxima (CheckBox) / Descentrado | Runout /
  MargenRevision / CheckBox ancho + AnchoObjetivo|TolAncho / CheckBox exceso + ExcesoCordonMaximo.
- GroupBox "Ajuste aplicado (robot) y foco↔Z": 3 campos Aplicado X/Y/Z (NullableDoubleConverter,
  vacío=no registrado) + texto `CoefFocoZTexto` + botón "Aprender CoefFocoZ del historial"
  (`AprenderCoefFocoZCommand`).
- GroupBox "Historial": ruta raíz + Elegir/Refrescar/Abrir + ListBox (2 líneas por estudio).

**Panel central** (documentos):
- "Muestras": DataGrid AnotacionDirecta (# / Áng/Pos / BordeCercano / BordeLejano / Profundidad /
  Exceso / Calidad / Ancho / PosicionCentral / Modo); barra con Agregar/Quitar/Ejemplos/Ordenar.
- "Imagen · Medición": `VisorImagen` (UserControl) con `VisorImagenViewModel`.
  Zoom (rueda centrada), pan, Ajustar/1:1; overlay Canvas de marcas (tamaño constante);
  tira de miniaturas PNG/JPG/TIFF; calibración px→mm; herramienta 8 puntos en orden;
  DOS botones: "Crear muestra" (nueva fila + limpiar marcas) / "Actualizar seleccionada" (corrección);
  catálogo de perfiles de microscopio (JSON en raíz).
  `OverlayGenerator.Generar()` (Soldadura.App): líneas datum y superficie extendidas al borde de la
  imagen (recorte paramétrico con 4 bordes); cotas perpendiculares con ticks y etiquetas mm para
  BordeCercano/Lejano/Profundidad/ExcesoCordon; cruces en las 8 marcas (DatumA/B + SuperficieA/B incluidas).
- "Vista 3D": `VistaWeld3D` (UserControl) con `HelixViewport3D`.
  Escena: anillo de referencia (radioObjetivo), curva ajustada Fourier, nuggets de revolución por
  muestra (BuildPerfilSoldadura: perfil con cuello y punta elíptica; color por semáforo de penetración
  según ProfundidadMinima/Maxima + MargenRevision), cúpulas de corona semielipsoidales (exceso,
  emisivo brillante), esferas centro ideal/real, flecha ajuste X/Y.
  Normales suaves (promedio meridional); luz principal + relleno (#556070); plano translúcido (16%
  opacidad); etiquetas 2D proyectadas con `Viewport3DHelper`; cámara inicial 3/4 (45° yaw, 35° pitch).
  `Visor3DViewModel.ObtenerSnapshot`: callback `Func<byte[]?>` que el UserControl registra en
  `DataContextChanged` → `RenderABytes()` renderiza a PNG en memoria para el reporte PDF a
  RESOLUCIÓN FIJA (1000×750), independiente del tamaño/visibilidad del panel: compone un
  `Viewport3D` off-screen con un clon del `Modelo` y de la cámara actual + fondo de la vista +
  etiquetas reproyectadas (`ConstruirCanvasEtiquetas`/`CrearEtiqueta` compartido con `ActualizarLabels`).

**Panel derecho**:
- "Resultados": botones Analizar (azul) / Guardar / Exportar CSV·JSON·PDF; badge veredicto coloreado
  (PASA verde / REVISAR naranja / NO PASA rojo); TextBox resultado con recomendación X/Y/Z, estadística,
  muestra crítica, avisos.
- "Tendencia de runout": `TendenciaView` (Canvas WPF-puro: ejes, puntos, regresión azul, límite rojo
  dashed, cruce naranja); `TendenciaViewModel` → `MotorTendencia`.

**MainViewModel** (CommunityToolkit.Mvvm):
- Propiedades: identidad, objetivo, especificaciones (ProfundidadMinima/Maxima/EvaluarMax, Descentrado,
  Runout, Margen, Ancho, Exceso), VeredictoTexto/Fondo/TextColor/HayVeredicto.
- Tema: `TemaOscuro` (bool), `TexToTema` ("🌙 Oscuro"/"☀ Claro"); `CambiarTemaCommand` llama
  `ThemeManager.Aplicar()` y guarda en config.
- Ejes robot: `NombreEjeX/Y/Z` (string), `AnguloEjesGrados` (double); persisten en PlantillaPerfil.
  `VerDiagramaEjesCommand`: abre/reactiva `DiagramaEjesWindow` (no-modal, DataContext=this).
- `FormatearResultado(analisis, veredicto, ejeX, ejeY, ejeZ, anguloGrados)`: rota el vector (AjusteX, AjusteY)
  del marco analítico al marco robot con matriz 2D: `(ax·cos+ay·sin, −ax·sin+ay·cos)`.
- `ModeloReferencia` (enum): se persiste en PlantillaPerfil (Guardar/Cargar) y se pasa a ConstruirPerfil.
  `FormatearResultado` muestra "Centrado: no aplica" (en vez de X/Y) cuando no hay datum externo.
- Foco↔Z: `AjusteXAplicado/YAplicado/ZAplicado` (double?), `CoefFocoZ` (double?) + `CoefFocoZTexto`.
  `ConstruirPerfil` pasa CoefFocoZ al análisis; `ConstruirAjusteAplicado()` arma el AjusteAplicado del
  Estudio (null si vacío); CoefFocoZ persiste en PlantillaPerfil (Guardar/Cargar); AbrirEstudio carga
  el ajuste aplicado del estudio. `AprenderCoefFocoZCommand`: recorre las puestas guardadas de la pieza
  → serie (puesta, Z aplicado, profundidad media) → `MotorAprendizajeFoco`; si aprende, fija CoefFocoZ.
- Comandos: Agregar/Quitar/Ordenar(#/Ang) muestras; CargarEjemplo/NoPasa; Analizar; Guardar;
  ElegirRaiz; RefrescarHistorial; AbrirEstudio; ExportarCsv/Json/Pdf;
  GuardarPerfil/CargarPerfil/EliminarPerfil; CambiarTema; VerDiagramaEjes; VerModelosReferencia;
  AprenderCoefFocoZ.
- Sub-VMs: `Visor` (VisorImagenViewModel), `Visor3D` (Visor3DViewModel), `Tendencia` (TendenciaViewModel).
- `_carpetaActual`: se actualiza en `Guardar` y `AbrirEstudio`; lo usa `ExportarPdf` para resolver
  rutas de overlays y pasarlas a `ReportePdf.Generar`. `ExportarPdf` también llama
  `Visor3D.ObtenerSnapshot?.Invoke()` para capturar el render 3D antes de generar el PDF.
- `ConstruirEspecificaciones()`: `ProfundidadMaxima = EvaluarProfundidadMax ? valor : null`.

**ModelosReferenciaWindow** (`Controls/ModelosReferenciaWindow.xaml+cs`): ventana no-modal (660×500).
- TabControl con 5 tabs: 1·Plano externo / 2·Radial desde eje / 3·Dos features / 4·Contorno de pieza /
  5·Solo geometría del cordón.
- Cada tab: Canvas WPF (540×195) con sección transversal dibujada (piezas gris, cordón azul,
  datum rojo punteado, marcas coloreadas: DatumA/B rojo, SurfA/B+Bord verde, Fondo naranja, Corona violeta)
  + descripción de dónde colocar cada marca y fórmulas derivadas.

**DiagramaEjesWindow** (`Controls/DiagramaEjesWindow.xaml+cs`): ventana no-modal (420×540) con tema oscuro.
- Campos: NombreEjeX/Y/Z (TextBox) + AnguloEjesGrados (Slider −180→180 + display numérico).
- Vista superior (Canvas 280×280): círculo=pieza, punto dorado=láser, punto blanco=marca 0°,
  flecha azul=eje X robot, flecha verde=eje Y robot; `RotateTransform.Angle` enlazado a `AnguloEjesGrados`
  → actualización en vivo al mover el slider.
- Vista lateral (Canvas 280×140): cabezal láser, haz punteado amarillo, flecha naranja Z, cordón azul.
- Leyenda con la convención de ángulo (grados horarios desde 0°).

### Tests (Soldadura.Tests) — 83 pruebas verdes
AjustadorArmonicoTests (recuperación descentrado/ovalidad, resolución, huecos), EstadisticaTests,
MotorAnalisisTests, MotorNormasTests (pasa/revisar/no-pasa, clamp, calidad, veredicto global),
MuestraCriticaTests, EspecificacionesTests (piso/techo independientes, exceso peor corte),
MedicionEnPantallaTests (perpendicular a superficie inclinada, marcas round-trip),
EstudioRepositorioTests (incl. AjusteAplicado round-trip), PerfilRepositorioTests (round-trip, slug, listar, eliminar),
ExportadorCsvTests, ReportePdfTests, AnalisisTendenciaTests,
AprendizajeFocoTests (recupera coef ±, datos insuficientes, sin variación en Z, ignora Z null, pendiente≈0),
MotorAnalisisTests incl. SoloCordon (sin datum no evalúa centrado; Z sigue; modelos con datum mantienen radial).

## 18. Pendientes / preguntas abiertas
- NORMA PUBLICADA: ruleset ISO 13919-1:2019 embebido sigue PLACEHOLDER (Verificada=false). Sin la
  edición oficial a la mano no avanzar. Requiere: (a) transcribir valores/fórmulas oficiales;
  (b) UI para seleccionar norma publicada vs. especificación interna.
- Soldadura lineal: recomendación implementada (AjusteY = −ordenada; aviso si hay deriva significativa).
  Pendiente: validar con el usuario que el eje Y del robot siempre es perpendicular al trayecto lineal,
  o aplicar rotación de ejes equivalente al caso circular.
- Coeficiente foco↔Z: RESUELTO. `MotorAprendizajeFoco` regresa profundidadMedia vs Z aplicado de las
  puestas guardadas de la pieza; el comando `AprenderCoefFocoZ` lo fija en el perfil (guardar plantilla
  para conservarlo). Requiere que el técnico registre el AjusteAplicado.Z por puesta (campo nuevo en UI).
  Pendiente menor: aún se aprende contra IdPieza; podría agruparse también por nombre de perfil/material,
  y exponer R²/puntos en un panel en vez de solo el texto de estado.
- PDF: render 3D y overlays ya se incrustan. RESUELTO: `RenderABytes` ahora renderiza off-screen a
  resolución fija (1000×750) con clon de modelo+cámara, así que ya no depende del tamaño/visibilidad
  del panel al exportar. Licencia QuestPDF Community (gratuita < 1 M USD/año).
- Diagramas de modelos de referencia: IMPLEMENTADO. `Controls/ModelosReferenciaWindow` (no-modal,
  TabControl con 5 tabs) accesible desde botón "Ver modelos de referencia…" en GroupBox Objetivo.
  Cada tab muestra: sección transversal en Canvas WPF + descripción de marcas y fórmulas.
  2a RESUELTO: `ModeloReferencia` añadido a PlantillaPerfil + ComboBox en GroupBox Objetivo
  (con nombres legibles vía ModeloReferenciaConverter); persiste en plantilla y se pasa a ConstruirPerfil.
  2b RESUELTO (con matiz): el análisis del dominio mostró que 4 de los 5 modelos comparten exactamente
  la aritmética (cercano,lejano)→(ancho,posición); el único con comportamiento distinto es SoloCordon
  (sin datum externo → sin centrado). Implementado vía `ModelosReferencia.TieneDatumExterno`:
  SoloCordon desactiva el análisis radial/lineal (solo penetración + ancho), con avisos y "n/a" en X/Y
  en UI y PDF. NO se inventaron fórmulas divergentes para los 4 modelos con datum porque no las hay.
  Pendiente menor opcional: si en el futuro algún modelo necesita campos de captura distintos
  (p. ej. capturar ancho directo en SoloCordon en vez de dos bordes), habría que variar el formulario.