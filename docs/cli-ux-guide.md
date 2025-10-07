# Guía de UX/DX del CLI

La última etapa del [plan de integración](./PlanDeIntegracion.md) se centra en
alinear la experiencia de uso de `dotnet-adr` con los flujos de trabajo más
comunes de `adr-tools` y documentar los puntos de extensión clave.

## Ayuda integrada

La configuración de Spectre.Console expone descripciones y ejemplos para cada
comando principal. Además de `adr --help`, ahora se incluyen ejemplos
contextuales que cubren las opciones más frecuentes:

- `adr new` documenta el uso repetible de `-s/--supersede` y `-l/--link`, así
  como la inyección de fechas vía `--date` o `ADR_DATE`.
- `adr list` y `adr link` comparten la semántica de `-p/--path`, mostrando en
  la ayuda cómo apuntar a un repositorio alternativo.
- `adr generate toc` y `adr generate graph` detallan los modificadores de
  prefijo/extensión e ilustran su uso en la salida de ayuda.
- `adr upgrade repository` explica el alcance de la normalización de fechas y
  cómo ejecutarlo desde automatizaciones.

Estos ejemplos se añaden mediante `config.AddExample(...)` en `Program.cs`, lo
que garantiza que cualquier consumidor (incluyendo pipelines) pueda descubrir
los parámetros admitidos directamente desde la línea de comandos.

## Resolución de espacio de trabajo

`AdrWorkspaceContextFactory` continúa siendo la pieza central para localizar el
repositorio de ADRs. El algoritmo aplica el siguiente orden de precedencia:

1. `-p|--path` recibido en la línea de comandos.
2. `adr.config.json` localizado en la raíz del repositorio.
3. Directorio de trabajo actual.

El archivo de configuración acepta dos claves:

```json
{
    "path": "./docs/adr",
    "templatePath": "./docs/templates/adr.md"
}
```

Ambas rutas se resuelven de forma absoluta a partir de la ubicación del archivo
para mantener compatibilidad con instalaciones existentes y con paquetes de
plantillas distribuidos por NuGet.

## Variables de entorno y automatización

Cuando se ejecuta en pipelines, `dotnet-adr` respeta la variable `ADR_DATE` como
fuente para la fecha por defecto de `adr new`. Esto permite escenarios de
reproceso o generación determinista de ADRs sin modificar las plantillas.

El resto de comandos reutiliza la misma fábrica de espacios de trabajo, por lo
que cualquier automatización que ya hubiera establecido `adr.config.json`
seguirá funcionando sin cambios.

## Recursos adicionales

- [README](../README.md) – Lista exhaustiva de comandos y opciones actualizada.
- [Matriz de paridad](./adr-tools-parity-matrix.md) – Estado de las capacidades
  frente a `adr-tools` tras la actualización de la ayuda integrada.
