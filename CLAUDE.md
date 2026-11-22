# CLAUDE.md

Guía permanente para Claude Code en este repositorio. Se lee en cada sesión. El plan de trabajo de cada fase está en `docs/handoffs/`.

## Qué es este proyecto

Prueba de concepto (POC), no un producto: buses escolares simulados envían su ubicación, el backend la distribuye por RabbitMQ a varios consumidores independientes y una app en React Native (Expo) muestra los buses en un mapa en tiempo real. Contexto completo: `docs/brief.md`.

## Cómo trabajar

- Trabaja **solo la fase indicada** en el handoff activo. No crees carpetas, proyectos ni abstracciones de fases futuras.
- Sigue los pasos del handoff en orden. Al terminar cada paso: compila, corre las pruebas y propón un commit.
- Si algo del handoff es ambiguo o choca con lo que encuentras en el código, **pregunta antes de decidir**.
- Prefiere código simple y explícito sobre patrones genéricos. Nada de repositorios genéricos, mediadores ni capas sin uso.
- No agregues paquetes NuGet o npm que el handoff no mencione sin preguntar primero.

## Convenciones

- **Todo lo técnico en inglés:** nombres de proyectos, carpetas, clases, métodos, variables, eventos, colas, rutas HTTP, commits y comentarios en el código.
- **Textos que ve el usuario final en español** (mensajes de la app, errores devueltos por la API pensados para mostrarse).
- C#: PascalCase para tipos y miembros públicos, camelCase para locales, `_camelCase` para campos privados. `record` para contratos y DTOs. Nullable habilitado.
- JSON: camelCase. Fechas en UTC con `DateTimeOffset` (ISO 8601).
- Columnas de base de datos (desde la fase 5): snake_case.
- Commits en inglés con formato Conventional Commits: `feat:`, `fix:`, `test:`, `docs:`, `chore:`, `refactor:`.

## Estructura del monorepo

```
realtime-bus-tracking/
├── backend/        # .NET 10 (BusTracking.slnx)
│   ├── src/
│   ├── tools/
│   └── tests/
├── mobile/         # Expo app (desde la fase 2)
├── infra/          # docker-compose.yml
├── data/routes/    # rutas y paradas en GeoJSON
└── docs/           # brief y handoffs
```

## Mensajería (resumen)

- Exchange `tracking.events`, tipo `topic`, durable.
- Claves de enrutamiento: `location.updated.{routeId}`, `stop.approaching.{routeId}`, `stop.arrived.{routeId}`.
- Cada mensaje lleva `MessageId = eventId`, `Type` = nombre del evento, `ContentType = application/json` y es persistente.
- Consumidores (desde la fase 2): una cola durable por consumidor, ack manual, `prefetch` limitado, idempotencia por `eventId`, dead letter exchange `tracking.dlx`.
- Cliente: `RabbitMQ.Client` (oficial, API asíncrona). No usar MassTransit.

## Comandos

```bash
docker compose -f infra/docker-compose.yml up -d     # infraestructura
dotnet build backend/BusTracking.slnx
dotnet test backend/BusTracking.slnx
dotnet format backend/BusTracking.slnx --verify-no-changes
```

Panel de RabbitMQ: http://localhost:15672 (usuario y clave de desarrollo en `infra/.env.example`).

## Repositorio público: reglas

- **Nunca** subir secretos, tokens, claves ni archivos `.env`. Solo `.env.example` con valores de desarrollo.
- **Solo datos inventados:** buses, conductores, rutas y paradas ficticios. Nada de datos reales de niños ni de personas.
- No subir carpetas `bin/`, `obj/`, `node_modules/` ni artefactos de compilación.

## Pruebas

- xUnit. Pruebas unitarias para la lógica de `BusTracking.Core` y la validación de la API.
- Pruebas de integración con Testcontainers (RabbitMQ y, desde la fase 5, PostgreSQL). Requieren Docker encendido.
- Una funcionalidad no está terminada si no tiene pruebas y si `dotnet format` no pasa.
