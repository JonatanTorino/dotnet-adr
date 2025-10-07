# Capa de Dominio para ADRs

La segunda etapa del plan de integración introduce una capa reutilizable que abstrae el acceso a los registros de decisiones. Esta capa sirve como cimiento para los próximos comandos y encapsula cuatro responsabilidades principales:

- **Descubrimiento** – `FileSystemAdrLocator` resuelve los archivos `.md` que cumplen el patrón `####-*.md` dentro del directorio raíz indicado. La búsqueda puede ampliarse usando `SearchOption` en `AdrRepositoryOptions`.
- **Lectura** – `FileSystemAdrDocumentReader` centraliza la lectura asíncrona de contenido desde disco, aislando al resto de componentes de I/O específico.
- **Parseo** – `MarkdigAdrParser` analiza el Markdown con Markdig/YamlDotNet para obtener secciones, estado, enlaces y metadatos. El parser detecta fechas en front matter YAML o líneas `Date:` en el contenido, e identifica relaciones bidireccionales expresadas mediante vínculos Markdown.
- **Agregación** – `AdrRepository` orquesta las piezas anteriores para exponer operaciones de alto nivel: enumerar registros (`GetAllAsync`) y localizar por número (`GetByIdAsync`).

El modelo `Adr` refleja los datos extraídos, incluyendo número de registro, título, fecha, contenido y metadatos normalizados. La utilería `SafeFileName()` genera slugs compatibles con la convención de `adr-tools`, removiendo diacríticos y caracteres no alfanuméricos antes de construir el nombre `####-slug.md`.

Estos servicios quedan registrados en `ServiceCollectionExtensions.ConfigureDependencies`, lo que permite inyectarlos desde Spectre.Console y reutilizarlos en los nuevos comandos (`adr new`, `adr list`, `adr link`, etc.).
