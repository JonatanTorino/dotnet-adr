# Matriz de Paridad Funcional con `adr-tools`

Este documento ejecuta el Paso 1 del [Plan de Integración](./PlanDeIntegracion.md),
levantando una matriz de funcionalidades entre [`adr-tools`](https://github.com/npryce/adr-tools)
y el estado actual de `dotnet-adr`. La información de referencia se obtuvo
de los scripts ubicados en `src/` dentro de `adr-tools`.

## Resumen Ejecutivo

- `dotnet-adr` cubre parcialmente el flujo de `adr init` y `adr new`,
  pero aún carece de soporte para múltiples reemplazos, enlaces y
  actualización automática de estados.
- Los comandos operativos (`adr list`, `adr link`, `adr upgrade-repository`)
  y los generadores (`adr generate toc`, `adr generate graph`) no tienen
  contraparte en `dotnet-adr`.
- `dotnet-adr` aporta capacidades adicionales de gestión de plantillas
  y paquetes que no existen en `adr-tools`.

## Tabla de paridad

| Categoría | Comando / Opción (`adr-tools`) | Descripción clave | Soporte en `dotnet-adr` | Observaciones |
| --- | --- | --- | --- | --- |
| Inicialización | `adr init [DIRECTORY]` | Crea el directorio de ADR (por defecto `doc/adr`), genera la ADR 0001 desde `init.md` y abre el editor. | Parcial: `adr environment init [PATH]` crea la carpeta y usa plantillas, pero no inicializa contenido estándar ni abre editor. | Falta la ADR inicial con enlaces de origen y la apertura automática del editor. |
| Creación | `adr new TITLE` | Genera ADR numerada a partir de plantilla, establece estado "Accepted" y abre editor. | Parcial: `adr new <TITLE>` genera ADR usando plantillas configurables, pero no abre editor ni asegura estado normalizado. | El título se aplica, pero la fecha usa cultura local (`DateTime.Now.ToShortDateString()`) y no ISO 8601. |
| Creación | `adr new -s SUPERCEDED` (repetible) | Inserta enlace "Supercedes"/"Superceded by" y cambia estado del ADR anterior. | Completo: `adr new` acepta múltiples `-s`, elimina `Accepted` y escribe vínculos recíprocos preservando los saltos de línea. | Se alinea con `_adr_add_link` y `_adr_remove_status`, pendiente automatizar pruebas BDD. |
| Creación | `adr new -l TARGET:LINK:REVERSE` (repetible) | Crea enlaces personalizados entre ADRs (adelante y reverso). | Completo: `adr new` interpreta triples `TARGET:LINK:REVERSE`, resuelve referencias por número/slug y actualiza ambos ADRs con anotaciones simétricas. | Requiere añadir escenarios de SpecFlow para garantizar la compatibilidad continua. |
| Listados | `adr list` | Enumera archivos `NNNN-slug.md` ordenados. | Completo: `adr list` muestra tabla con número, título, estado y ruta relativa. | Integra el repositorio compartido y admite búsqueda recursiva opcional. |
| Enlaces | `adr link SOURCE LINK TARGET REVERSE` | Añade enlaces manuales entre ADR existentes. | Completo: `adr link` aplica relaciones simétricas reutilizando el formateador de estados. | Valida que ambos extremos existan y evita duplicados en `## Status`. |
| Generación | `adr generate` | Punto de entrada para reportes (`toc`, `graph`). | Completo: rama `generate` con subcomandos registrados en Spectre. | Expone ayuda contextual y ejemplos para `toc` y `graph`. |
| Generación | `adr generate toc [-i INTRO] [-o OUTRO] [-p PREFIX]` | Produce TOC Markdown parametrizable. | Completo: genera Markdown ordenado con prefijos y contenido de intro/outro desde archivos. | Soporta rutas relativas al repositorio y lectura segura de archivos externos. |
| Generación | `adr generate graph [-p PREFIX] [-e EXT]` | Genera DOT con enlaces y relaciones. | Completo: emite Graphviz DOT con nodos ordenados, enlaces de estado y prefijo/extensión configurables. | Reutiliza el analizador de estado para poblar aristas y evita duplicados. |
| Mantenimiento | `adr upgrade-repository` | Normaliza formato de fechas en ADR existentes a ISO 8601. | Completo: `adr upgrade repository` actualiza todas las líneas `Date:` al formato ISO-8601. | Opera de forma recursiva y reporta los archivos modificados. |
| Ayuda | `adr help [COMMAND]` | Muestra comandos y ayuda detallada con paginador. | Completo: la configuración de Spectre agrega descripciones y ejemplos para `new`, `list`, `link`, `upgrade repository` y `generate`. | Documentación complementaria disponible en README y [guía de UX](./cli-ux-guide.md). |

## Capacidades exclusivas de `dotnet-adr`

| Categoría | Comando `dotnet-adr` | Descripción |
| --- | --- | --- |
| Plantillas | `adr templates show/list/set` | Gestiona plantillas individuales dentro de un paquete. |
| Paquetes | `adr templates package set/install/update/show` | Opera sobre paquetes de plantillas distribuidos por NuGet. |
| Entorno | `adr environment reset` | Restaura configuración predeterminada del entorno del CLI. |

## Próximos pasos sugeridos

1. Implementar la capa de dominio que abstraiga lectura, estado y enlaces de ADRs.
2. Extender `NewAdrCommand` para manejar colecciones de reemplazos (`-s`) y enlaces (`-l`) con fecha inyectable.
3. Añadir comandos `list`, `link`, `generate toc/graph` y `upgrade repository` sobre la nueva capa, con pruebas BDD/Unitarias.
4. Integrar ayuda contextual y documentación actualizada alineada con los nuevos comandos.
