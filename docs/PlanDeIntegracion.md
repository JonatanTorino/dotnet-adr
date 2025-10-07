# Plan de Integración

Este documento resume los pasos previstos para alcanzar la paridad funcional con `adr-tools` y las capacidades resultantes para `dotnet-adr`.

## Seguimiento

- [x] Paso 1: [Matriz de paridad con `adr-tools`](./adr-tools-parity-matrix.md).
- [x] Paso 2: Diseñar la capa de dominio para ADRs.
- [x] Paso 3: Ampliar el flujo de `adr new`.
- [ ] Paso 4: Incorporar comandos adicionales con Spectre.
- [ ] Paso 5: Implementar la rama `adr generate`.
- [ ] Paso 6: Actualizar la UX/DX.

## Pasos

1. **Catalogar la paridad funcional**
   - Levantar una matriz de funcionalidades entre los repositorios `adr-tools` y `dotnet-adr`.
   - Revisar los scripts `adr_*` y documentar casos de uso, parámetros y efectos en los archivos Markdown.
   - Definir criterios de aceptación previos a cualquier cambio de código.
2. **Diseñar la capa de dominio para ADRs**
   - Crear servicios para descubrimiento, lectura, parseo de secciones, estados y vínculos.
   - Registrar la capa por medio de inyección de dependencias, reutilizando Markdig/YamlDotNet y extendiendo Adr.
   - Exponer estados y enlaces a través de este núcleo para reutilización en los nuevos comandos.
   - ✅ Repositorio de ADR con descubrimiento en disco, lector y parser Markdig/YamlDotNet registrado vía DI.
   - ✅ Modelo `Adr` extendido con fecha, secciones, estado, enlaces y metadatos listos para ser reutilizados (ver [capa de dominio](./domain-layer.md)).
3. **Ampliar el flujo de `adr new`**
   - Permitir múltiples banderas `-s` y `-l` dentro de `NewAdrCommand`.
   - Actualizar estados y enlaces simétricos y habilitar la inyección de fecha para pruebas.
   - Cubrir el comportamiento con escenarios SpecFlow que igualen los scripts actuales.
   - ✅ `adr new` ahora admite colecciones de `-s`/`-l`, genera enlaces recíprocos, limpia estados "Accepted" y permite fijar la fecha vía `--date` o `ADR_DATE`.
4. **Incorporar comandos adicionales con Spectre**
   - Implementar `adr list`, `adr link` y `adr upgrade repository` sobre el nuevo servicio.
   - Formatear la salida con `AnsiConsole` y validar efectos en disco mediante pruebas BDD y unitarias.
5. **Implementar la rama `adr generate`**
   - Agregar subcomandos `toc` y `graph` que produzcan, respectivamente, Markdown y Graphviz DOT.
   - Permitir la parametrización de prefijos, extensiones e intro/outro, y validar con snapshots de prueba.
6. **Actualizar la UX/DX**
   - Añadir ayuda integrada, documentación en el README y guías de plantillas/variables de entorno.
   - Actualizar pipelines para ejecutar los nuevos escenarios y asegurar retrocompatibilidad con `adr.config.json` y plantillas NuGet.

## Capacidades resultantes

- Creación de ADR con múltiples reemplazos, enlaces cruzados y plantillas administradas.
- Listado enriquecido de registros y rutas objetivo con formato consistente en la terminal.
- Generación de tablas de contenido en Markdown con intro/outro opcionales.
- Exportación de grafos de decisión en DOT con control de prefijos y extensiones.
- Enlazado manual entre ADR existentes y actualización simétrica de estados.
- Actualización masiva del repositorio a los formatos actuales preservando la configuración existente.
