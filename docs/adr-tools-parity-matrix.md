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
| Creación | `adr new -s SUPERCEDED` (repetible) | Inserta enlace "Supercedes"/"Superceded by" y cambia estado del ADR anterior. | Parcial: `adr new -i <RECORDNUMBER>` agrega texto "Superseded by ADR #### - Título", pero solo admite un ID y no ajusta el estado ni enlaces simétricos. | Necesita admitir múltiples IDs, normalizar estado y crear enlaces bidireccionales. |
| Creación | `adr new -l TARGET:LINK:REVERSE` (repetible) | Crea enlaces personalizados entre ADRs (adelante y reverso). | No soportado. | Requiere modelo de relaciones y actualización simétrica. |
| Listados | `adr list` | Enumera archivos `NNNN-slug.md` ordenados. | No soportado. | Debe implementarse sobre la capa de dominio. |
| Enlaces | `adr link SOURCE LINK TARGET REVERSE` | Añade enlaces manuales entre ADR existentes. | No soportado. | Requiere reutilizar lógica de `_adr_add_link` y validaciones. |
| Generación | `adr generate` | Punto de entrada para reportes (`toc`, `graph`). | No soportado. | Necesario implementar comando raíz y subcomandos. |
| Generación | `adr generate toc [-i INTRO] [-o OUTRO] [-p PREFIX]` | Produce TOC Markdown parametrizable. | No soportado. | Implica recorrer ADRs ordenados y renderizar snapshot Markdown. |
| Generación | `adr generate graph [-p PREFIX] [-e EXT]` | Genera DOT con enlaces y relaciones. | No soportado. | Requiere grafo de decisiones y parametrización de enlaces. |
| Mantenimiento | `adr upgrade-repository` | Normaliza formato de fechas en ADR existentes a ISO 8601. | No soportado. | Debe revisar archivos y aplicar transformaciones controladas. |
| Ayuda | `adr help [COMMAND]` | Muestra comandos y ayuda detallada con paginador. | Parcial: Spectre.Console provee `adr --help` y descripciones de comandos existentes, sin paginador ni ayuda específica por subcomando. | Podría ampliarse con ejemplos y soporte multinivel. |

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
