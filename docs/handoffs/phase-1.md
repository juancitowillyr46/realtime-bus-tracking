# Handoff · Fase 1: ingesta, RabbitMQ y simulador

> Lee primero `CLAUDE.md` y `docs/brief.md`. Este documento es el plan **solo de la fase 1**.

## Objetivo

Que un bus simulado recorra una ruta de Pereira, envíe su ubicación a la API cada 5 segundos, la API la publique como `LocationUpdated` en RabbitMQ y una consola la reciba e imprima.

**Listo cuando:** ves los mensajes pasar por el panel de RabbitMQ (`http://localhost:15672`) y llegar a la consola `EventTail`, y todas las pruebas pasan.

## Fuera de alcance en esta fase

No crear nada de esto todavía: SignalR, app móvil (`mobile/`), workers de geocercas e historial, colas durables por consumidor, cola de fallidos (`tracking.dlx`), PostgreSQL/PostGIS, detenciones en paradas, escenarios del simulador (retraso, pérdida de señal, modo carga).

## Antes de empezar (lo hace el desarrollador)

- [ ] Docker Desktop encendido (con WSL 2) y .NET 10 SDK instalado (`dotnet --version`).
- [ ] Dibujar la ruta en [geojson.io](https://geojson.io) y guardarla como `data/routes/route-01.geojson`, con el formato de abajo.

### Formato de una ruta en GeoJSON

Un `FeatureCollection` con **una** línea (`LineString`) para el recorrido y un punto (`Point`) por parada. Las propiedades se agregan en geojson.io con la tabla de propiedades de cada elemento.

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "properties": { "kind": "route", "routeId": "route-01", "name": "Ruta Norte" },
      "geometry": { "type": "LineString", "coordinates": [[-75.6961, 4.8133], [-75.6950, 4.8140]] }
    },
    {
      "type": "Feature",
      "properties": { "kind": "stop", "stopId": "stop-01", "name": "Parada Los Pinos", "order": 1 },
      "geometry": { "type": "Point", "coordinates": [-75.6961, 4.8133] }
    }
  ]
}
```

> **Ojo:** GeoJSON guarda las coordenadas como `[longitud, latitud]`, al revés de como se suelen escribir. En Pereira la longitud es negativa (alrededor de -75.7) y la latitud positiva (alrededor de 4.8). Las coordenadas del ejemplo son solo ilustrativas. Nombres de rutas y paradas: inventados.

## Resultado esperado de la fase

```
realtime-bus-tracking/
├── .github/workflows/ci.yml
├── .editorconfig
├── .gitignore
├── LICENSE
├── README.md
├── CLAUDE.md
├── backend/
│   ├── BusTracking.slnx
│   ├── src/
│   │   ├── BusTracking.Contracts/
│   │   ├── BusTracking.Core/
│   │   ├── BusTracking.Messaging/
│   │   ├── BusTracking.Api/
│   │   └── BusTracking.Simulator/
│   ├── tools/
│   │   └── BusTracking.EventTail/
│   └── tests/
│       ├── BusTracking.Core.Tests/
│       ├── BusTracking.Simulator.Tests/
│       └── BusTracking.Api.IntegrationTests/
├── infra/
│   ├── docker-compose.yml
│   └── .env.example
├── data/routes/route-01.geojson
└── docs/
```

## Pasos

Al final de cada paso: `dotnet build`, `dotnet test` y un commit con el mensaje sugerido.

### Paso 1 · Esqueleto del repositorio

- `.gitignore` (plantilla de .NET con `dotnet new gitignore`, más `.env` y `node_modules/`), `.editorconfig` y `LICENSE` (MIT; **pregunta el nombre** para el copyright).
- Solución `backend/BusTracking.slnx` con los proyectos de la estructura de arriba, todos en `net10.0`, con `Nullable` e `ImplicitUsings` habilitados. Si el SDK genera `.sln` en lugar de `.slnx`, usa el que genere y actualiza `CLAUDE.md`.
  - `Contracts`, `Core`, `Messaging`: class library.
  - `Api`: `web` (Minimal API), puerto fijo `http://localhost:5080` en `launchSettings.json`.
  - `Simulator` y `EventTail`: console con Generic Host (`Host.CreateApplicationBuilder`).
  - Tests: xUnit.
- Referencias: `Core` → `Contracts`; `Messaging` → `Contracts`; `Api` → `Contracts`, `Core`, `Messaging`; `Simulator` → `Contracts`, `Core`; `EventTail` → `Contracts`, `Messaging`.
- `.github/workflows/ci.yml`: en `ubuntu-latest`, .NET `10.0.x`, restore, build, `dotnet format --verify-no-changes` y test.

Commit: `chore: scaffold solution, ci and repository files`

### Paso 2 · Infraestructura con Docker

`infra/docker-compose.yml` con **solo RabbitMQ** en esta fase:

- Imagen `rabbitmq:4-management`, puertos `5672` y `15672`, volumen con nombre para los datos.
- Usuario y clave desde variables `RABBITMQ_DEFAULT_USER` y `RABBITMQ_DEFAULT_PASS`, leídas de `infra/.env` (no se sube). Subir `infra/.env.example` con `RABBITMQ_DEFAULT_USER=bus` y `RABBITMQ_DEFAULT_PASS=change-me`.
- Healthcheck con `rabbitmq-diagnostics -q ping`.

Verificación manual: `docker compose -f infra/docker-compose.yml up -d` y entrar al panel en `http://localhost:15672`.

Commit: `chore: add rabbitmq docker compose`

### Paso 3 · Contratos y lógica geográfica

**`BusTracking.Contracts`**

```csharp
public sealed record LocationUpdated(
    Guid EventId,
    string BusId,
    string RouteId,
    double Latitude,
    double Longitude,
    double SpeedKmh,
    double Heading,
    DateTimeOffset RecordedAt);
```

- `LocationReport`: el cuerpo que recibe la API (los mismos campos sin `EventId`).
- `TrackingTopology`: constantes `ExchangeName = "tracking.events"` y el método `LocationUpdatedRoutingKey(string routeId)` → `location.updated.{routeId}`.
- `TrackingJson`: las `JsonSerializerOptions` compartidas (camelCase).

**`BusTracking.Core`**

- `GeoPoint(double Latitude, double Longitude)`.
- `GeoMath.DistanceMeters(a, b)` (haversine, radio 6 371 000 m) y `GeoMath.BearingDegrees(a, b)` (0 a 360, 0 = norte).
- `RoutePath`: se construye con la lista de puntos de la línea, precalcula las distancias acumuladas y expone `TotalLengthMeters` y `PositionAt(double distanceMeters)`, que devuelve el punto interpolado y el rumbo del tramo. Si la distancia pasa del final, devuelve el último punto.
- `LocationReportValidator.Validate(LocationReport, DateTimeOffset now)`: devuelve la lista de errores. Reglas: `busId` y `routeId` obligatorios; latitud entre -90 y 90; longitud entre -180 y 180; velocidad entre 0 y 150; rumbo entre 0 y 360; `recordedAt` no más de 1 minuto en el futuro ni más de 1 hora en el pasado. Mensajes en español.

**Pruebas (`BusTracking.Core.Tests`)**

- Distancia entre `(0, 0)` y `(1, 0)` ≈ 111 195 m (tolerancia de 1 m).
- `RoutePath.PositionAt(0)` devuelve el primer punto; `PositionAt(TotalLengthMeters)` el último; la mitad de un tramo de dos puntos queda en el punto medio; una distancia mayor al total devuelve el último punto.
- Rumbo de un tramo hacia el norte ≈ 0 y hacia el este ≈ 90.
- Un caso válido y un caso inválido por cada regla del validador.

Commit: `feat: add contracts, geo math and location validation`

### Paso 4 · Mensajería

**`BusTracking.Messaging`**, con `RabbitMQ.Client` (versión 7, API asíncrona; revisa la documentación de la versión instalada, porque la API cambió bastante respecto a la 6):

- `RabbitMqOptions`: `HostName`, `Port`, `UserName`, `Password`, `VirtualHost`, enlazado a la sección `RabbitMq` de la configuración.
- Una conexión compartida por proceso (singleton), creada de forma asíncrona y con recuperación automática habilitada.
- `TopologyInitializer`: declara el exchange `tracking.events` (`topic`, durable). Se ejecuta al arrancar la API.
- `IEventPublisher` / `RabbitMqEventPublisher`: `PublishAsync<T>(T message, string routingKey, Guid messageId)`. Serializa con `TrackingJson`, mensaje persistente, `MessageId`, `Type` = nombre del tipo, `ContentType = application/json`, `Timestamp`. Con **publisher confirms** activados: si el broker no confirma, se lanza una excepción.
- Una extensión `AddRabbitMqMessaging(IConfiguration)` para registrar todo.

Configuración: `appsettings.json` lleva `HostName`, `Port`, `UserName`, `VirtualHost`; la clave **no** va en el archivo, se pone con User Secrets:

```bash
dotnet user-secrets set "RabbitMq:Password" "<la clave de infra/.env>" --project backend/src/BusTracking.Api
```

Commit: `feat: add rabbitmq connection, topology and publisher`

### Paso 5 · API de ingesta

- `POST /api/locations` recibe un `LocationReport`, lo valida con `LocationReportValidator` y:
  - si es inválido: `400` con `ValidationProblem` (errores en español);
  - si es válido: crea un `LocationUpdated` con un `EventId` nuevo, lo publica con la clave `location.updated.{routeId}` y responde `202 Accepted` con `{ "eventId": "..." }`;
  - si RabbitMQ no está disponible: `503` y log del error.
- `GET /health`: responde `200` si la conexión con RabbitMQ está abierta.
- Logs estructurados con `busId`, `routeId` y `eventId`.

Commit: `feat: add location ingest endpoint`

### Paso 6 · Consola EventTail

Herramienta de depuración que imprime todo lo que pasa por el exchange:

- Declara una cola **exclusiva y temporal** (nombre asignado por el servidor) y la enlaza a `tracking.events` con el patrón `#` (todos los eventos). Se puede pasar otro patrón por argumento, por ejemplo `location.updated.route-01`.
- Por cada mensaje imprime: hora, routing key, `busId`, latitud y longitud con 5 decimales, velocidad y **latencia** (`ahora - recordedAt`, en milisegundos).
- Aquí se usa auto-ack porque es solo una herramienta de inspección. Comenta en el código que los consumidores reales (desde la fase 2) usan ack manual.
- Usa los mismos User Secrets que la API (o su propio `UserSecretsId`, documentado en el README).

Verificación manual: con la API y `EventTail` corriendo, enviar un `POST` de prueba (archivo `backend/src/BusTracking.Api/BusTracking.Api.http`) y verlo en la consola.

Commit: `feat: add event tail console`

### Paso 7 · Simulador

- `RouteLoader`: lee los `*.geojson` de la carpeta de rutas y devuelve `RouteDefinition(RouteId, Name, RoutePath Path, IReadOnlyList<RouteStop> Stops)`. Convierte `[longitud, latitud]` a `GeoPoint` correctamente. Error claro si la carpeta no existe, si no hay rutas o si un archivo no tiene exactamente una línea.
- Carpeta de rutas: configurable (`Simulator:RoutesDirectory`); si no se configura, se busca `data/routes` subiendo desde la carpeta del ejecutable hasta encontrarla.
- Configuración en `appsettings.json`:

```json
{
  "Simulator": {
    "ApiBaseUrl": "http://localhost:5080",
    "ReportIntervalSeconds": 5,
    "TimeScale": 1,
    "Buses": [
      { "BusId": "bus-01", "RouteId": "route-01", "SpeedKmh": 25, "StartOffsetMeters": 0 },
      { "BusId": "bus-02", "RouteId": "route-01", "SpeedKmh": 30, "StartOffsetMeters": 1500 }
    ]
  }
}
```

- Cada `ReportIntervalSeconds` (tiempo real), cada bus avanza `velocidad × intervalo × TimeScale` metros sobre su ruta, con una variación aleatoria pequeña de la velocidad (±5 km/h, nunca menor a 5). Al llegar al final, vuelve a empezar la ruta.
- Envía cada posición con `HttpClient` (registrado con `AddHttpClient`) a `POST /api/locations`, con `recordedAt` = hora actual en UTC. Si la API falla, registra el error y sigue con el siguiente ciclo; no se detiene.
- Log por ciclo: bus, distancia recorrida y respuesta de la API.

**Pruebas (`BusTracking.Simulator.Tests`)**

- `RouteLoader` con un GeoJSON de prueba en la carpeta de tests: carga la ruta, invierte bien las coordenadas y ordena las paradas por `order`.
- Archivo sin línea o con dos líneas → error claro.

Verificación manual de punta a punta: RabbitMQ + API + EventTail + Simulator corriendo; los mensajes aparecen en la consola cada 5 segundos y en el panel de RabbitMQ (pestaña *Exchanges* → `tracking.events`, gráfico de mensajes).

Commit: `feat: add route simulator`

### Paso 8 · Prueba de integración

**`BusTracking.Api.IntegrationTests`** con `Testcontainers.RabbitMq` y `WebApplicationFactory<Program>`:

- Levanta un RabbitMQ en contenedor y configura la API para usarlo.
- En la prueba: declara una cola temporal enlazada a `location.updated.*`, hace `POST /api/locations` con un reporte válido y verifica que llega **un** mensaje con el mismo `eventId`, la routing key correcta y las propiedades (`MessageId`, `Type`, persistente).
- Un reporte inválido responde `400` y no publica nada.

Commit: `test: add ingest integration tests with testcontainers`

### Paso 9 · README

README en inglés, corto:

- Qué es el proyecto (2 o 3 líneas) y estado: *Phase 1 of 5*.
- Diagrama de arquitectura en Mermaid (el del brief, marcando que por ahora solo existen simulador, API, RabbitMQ y EventTail).
- Requisitos y cómo correrlo: `docker compose`, User Secrets, `dotnet run` de API, EventTail y Simulator.
- Enlace a `docs/brief.md` y a `docs/handoffs/`.

Commit: `docs: add readme for phase 1`

## Checklist de cierre

- [ ] `docker compose up` levanta RabbitMQ sano y el panel abre.
- [ ] Simulador → API → RabbitMQ → EventTail funciona con 2 buses en `route-01`.
- [ ] La latencia mostrada en EventTail es estable (anótala para el README de la fase 2).
- [ ] `dotnet test` en verde, incluida la prueba de integración.
- [ ] `dotnet format --verify-no-changes` sin cambios y CI en verde en GitHub.
- [ ] Ningún secreto ni `.env` en el repositorio.

## Para preparar la fase 2

Al terminar, anota y comparte:

- Qué fue distinto a este plan (nombres, decisiones, problemas con `RabbitMQ.Client` 7).
- La latencia observada en EventTail.
- Si la ruta dibujada tiene buena forma o hay que ajustarla antes de verla en el mapa.

Con eso se escribe `docs/handoffs/phase-2.md` (SignalR + mapa en Expo Go).
