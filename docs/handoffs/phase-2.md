# Handoff · Fase 2: tiempo real en el mapa

> Lee primero `CLAUDE.md` y `docs/brief.md`. Este documento es el plan **solo de la fase 2**.

## Objetivo

Que los buses del simulador aparezcan moviéndose en un mapa en el iPhone (Expo Go) y en el navegador del PC. El camino es: la API publica `LocationUpdated` en RabbitMQ, un consumidor de la API lo lee de su propia cola durable y lo envía por SignalR a la app.

**Listo cuando:** ves moverse los 2 buses de `route-01` en tu iPhone, la app se recupera sola si se cae la red y todas las pruebas pasan.

## Lo que dejó la fase 1

- Funciona de punta a punta: simulador → API → RabbitMQ → EventTail.
- **Latencia observada en EventTail** (simulador → consola): p50 = 4 ms, p95 = 16 ms y máximo 38 ms. El primer mensaje tarda unos 340 ms por el arranque en frío.
- **La ruta `route-01` es provisional** ("Ruta Occidente (provisional)", 5 paradas). Es casi recta de este a oeste.
- `Testcontainers.RabbitMq` se subió a 4.17.0, y `RabbitMqBuilder` ahora recibe la imagen en el constructor.
- `TopologyInitializer` declara el exchange cada vez que se abre un canal, así que no importa el orden en que arranquen los servicios.

## Diseño aprobado

El mock-up de la app está en https://claude.ai/artifact/3nmtjoUaS3Ex59Vdxiv2TT y tiene 3 pantallas:

1. **Mapa en vivo:**
   - arriba, el selector de ruta y el indicador "En vivo";
   - en el mapa, el recorrido, las paradas y los buses;
   - abajo, un panel con la lista de buses: próxima parada, velocidad y "hace N s".
2. **Bus seleccionado:** el bus queda resaltado y se muestran su próxima parada, velocidad, rumbo y última actualización.
3. **Sin conexión:** aparece el aviso "Reconectando", y los buses quedan en gris en su última posición conocida.

**Cambio pedido sobre el mock-up:** usar **íconos** de `@expo/vector-icons`, que viene incluido con Expo:
- un ícono de bus en los marcadores y en la lista, con el número en una etiqueta;
- íconos para la velocidad, el rumbo (una flecha girada según `heading`) y la última actualización;
- íconos para el estado de la conexión.

## Fuera de alcance en esta fase

- Modo conductor y envío del GPS del iPhone (fase 3).
- Geocercas, ETA, alertas y notificaciones (fase 4).
- PostgreSQL, el historial y el CRUD de buses (fase 5).
- Las colas `geofence.location` e `history.location`.
- Escenarios del simulador.
- Backplane de SignalR con Redis.

## Antes de empezar (lo hace el desarrollador)

- [ ] **Instalar Expo Go en el iPhone X desde el App Store.** La versión del App Store llega solo hasta el **SDK 54**; las versiones nuevas (55 en adelante) no están ahí. Por eso el proyecto se fija en el **SDK 54**, que pide iOS 15.1 o superior y funciona en el iPhone X con iOS 16. Si Expo Go dice que el proyecto es incompatible, avisa antes de cambiar de SDK.
- [x] Node.js LTS con nvm-windows: está activa la **v24.21.0** (LTS), con npm 11.19.0. El SDK 54 (React Native 0.81) pide Node 20.19.4 o superior, así que la v20.19.0 que también está instalada no sirve. Si `node --version` muestra otra versión, cámbiala con `nvm use 24.21.0`.
- [ ] El iPhone y el PC en la misma red Wi-Fi.
- [ ] (Opcional, se puede hacer en cualquier momento de la fase) Dibujar la ruta real en geojson.io y reemplazar `data/routes/route-01.geojson`, sin cambiar el `routeId`.

## Resultado esperado de la fase

```
realtime-bus-tracking/
├── backend/
│   ├── src/
│   │   ├── BusTracking.Contracts/   # + BusPosition, RouteSummary, RouteDetails
│   │   ├── BusTracking.Core/        # + Routes/ (RouteLoader moved here), route progress, trackers
│   │   ├── BusTracking.Messaging/   # + consumer topology (queues, DLX)
│   │   ├── BusTracking.Api/         # + routes endpoints, SignalR hub, realtime consumer
│   │   └── BusTracking.Simulator/   # uses RouteLoader from Core
│   ├── tools/BusTracking.EventTail/
│   └── tests/                       # + Core and integration tests for the consumer and the hub
├── mobile/                          # Expo app, SDK 54, TypeScript
│   ├── App.tsx
│   ├── src/
│   ├── .env.example
│   └── package.json
└── .github/workflows/ci.yml         # + mobile job
```

## Pasos

Al final de cada paso: compilar, correr las pruebas, `dotnet format` y un commit con el mensaje sugerido.

### Paso 1 · Rutas en `Core` y avance sobre la ruta

La API necesita leer las mismas rutas que el simulador.

- Mover `RouteLoader`, `RouteDefinition` y `RouteStop` de `BusTracking.Simulator` a `BusTracking.Core` (namespace `BusTracking.Core.Routes`), junto con sus pruebas (a `BusTracking.Core.Tests`).
- `RoutesDirectoryLocator` se queda donde está: lo usan el simulador y la API. Si hace falta en los dos, también se mueve a `Core`.
- `RoutePath.DistanceAlong(GeoPoint point)`:
  - devuelve la distancia en metros desde el inicio de la ruta hasta el punto de la línea más cercano a `point`;
  - proyecta el punto sobre cada tramo con una aproximación plana local, suficiente para tramos de pocos cientos de metros.
- `RouteDefinition.NextStop(GeoPoint point)`:
  - devuelve la primera parada cuya distancia sobre la ruta es mayor o igual a la del bus;
  - si el bus ya pasó la última parada, devuelve la primera, porque el simulador reinicia la ruta.

**Pruebas:**
- las de `RouteLoader` siguen pasando en su nueva ubicación;
- `DistanceAlong` de un vértice da su distancia acumulada;
- un punto desplazado a un lado de un tramo se proyecta sobre el tramo;
- un punto antes del inicio da 0, y uno después del final da `TotalLengthMeters`;
- `NextStop` cubre tres casos: antes de la primera parada, entre paradas y después de la última.

Commit: `refactor: move route loading to core and add route progress`

### Paso 2 · Endpoints de rutas

- `GET /api/routes` → `[{ "routeId", "name" }]`.
- `GET /api/routes/{routeId}`:
  - responde `{ routeId, name, path: [{ latitude, longitude }], stops: [{ stopId, name, order, latitude, longitude }] }`;
  - si la ruta no existe, responde `404` con un mensaje en español.
- Las rutas se cargan una vez al arrancar (singleton). La carpeta se configura con `Routes:Directory`; si no se configura, se busca `data/routes` como lo hace el simulador.
- Los contratos `RouteSummary` y `RouteDetails` van en `BusTracking.Contracts`, como `record`.

**Pruebas (integración, con `WebApplicationFactory`):** la lista contiene `route-01`; el detalle trae 5 paradas en orden; una ruta que no existe da `404`.

Commit: `feat: add routes endpoints`

### Paso 3 · Cola durable, ack manual y cola de fallidos

Se agrega a `BusTracking.Messaging` y es la base de todos los consumidores siguientes.

- **`TrackingTopology`** (en `Contracts`) gana dos cosas:
  - las constantes `DeadLetterExchangeName = "tracking.dlx"`, `DeadLetterQueueName = "tracking.dead-letter"` y `RealtimeLocationQueue = "realtime.location"`;
  - el patrón de enlace `location.updated.*`.
- **`TopologyInitializer.DeclareConsumerQueueAsync(channel, queueName, bindingPattern)`:**
  - declara la cola **durable de tipo quorum** (`x-queue-type: quorum`) con `x-dead-letter-exchange: tracking.dlx` y `x-delivery-limit: 3`;
  - la enlaza a `tracking.events`;
  - declara también `tracking.dlx` (tipo `fanout`, durable) y la cola `tracking.dead-letter` enlazada a él.
  - En RabbitMQ 4 las colas quorum cuentan las entregas solas: un mensaje que se rechaza con `requeue: true` más de 3 veces pasa a `tracking.dead-letter`, sin escribir lógica de reintentos.
- **Una clase base `RabbitMqConsumer<T> : BackgroundService`.** Es la única abstracción de la fase y se justifica porque la reutilizan los consumidores de las fases 4 y 5.
  - Abre su canal, declara la topología y fija `BasicQos(prefetchCount: 10)`.
  - Consume con **ack manual**.
  - Deserializa con `TrackingJson` y llama a `HandleAsync(T message, CancellationToken)`.
  - Si el handler termina bien, hace `BasicAck`.
  - Si el handler lanza una excepción, hace `BasicNack(requeue: true)` y registra el error.
  - Si el JSON es inválido, hace `BasicNack(requeue: false)`, así que va directo a la cola de fallidos.
- **Idempotencia.** `RecentEventIds` en `Core` es un conjunto con capacidad limitada (por ejemplo 10 000 ids, los más viejos se descartan) y `TryAdd(Guid)`. Un `eventId` repetido se reconoce con ack y no se procesa. En la fase 5 esto se hará con la base de datos.

**Pruebas:**
- Unitarias de `RecentEventIds`: un id repetido devuelve `false`, y al pasar la capacidad se descartan los más viejos.
- Integración con Testcontainers:
  - un mensaje que siempre falla termina en `tracking.dead-letter` después de 3 intentos;
  - un JSON inválido va directo a la cola de fallidos;
  - el mismo `eventId` dos veces se procesa una sola vez.

Commit: `feat: add durable consumer base with manual ack and dead letter queue`

### Paso 4 · Hub de SignalR y consumidor de tiempo real

**El contrato `BusPosition`** (en `Contracts`) lleva estos campos: `eventId`, `busId`, `routeId`, `latitude`, `longitude`, `speedKmh`, `heading`, `recordedAt`, `nextStopId` y `nextStopName`.

**`BusPositionTracker`** (en `Core`):
- guarda la última posición de cada bus;
- `TryUpdate(position)` devuelve `false` si llega un `recordedAt` anterior o igual al último guardado de ese bus;
- `GetByRoute(routeId)` devuelve las posiciones actuales de una ruta.

**El hub `TrackingHub`** se publica en `/hubs/tracking`:
- `JoinRoute(routeId)`: agrega la conexión al grupo `route:{routeId}` y le devuelve las posiciones actuales de esa ruta, para que la app pinte los buses enseguida sin esperar al siguiente reporte;
- `LeaveRoute(routeId)`.

**`RealtimeLocationConsumer`** hereda de `RabbitMqConsumer<LocationUpdated>` y usa la cola `realtime.location`. Con cada mensaje:
1. calcula la próxima parada con la ruta (paso 1);
2. arma un `BusPosition`;
3. si `BusPositionTracker.TryUpdate` lo acepta, lo envía al grupo de la ruta con el evento `busMoved`; si no, solo hace ack, porque es una posición vieja.

**CORS:** se permite `http://localhost:8081` (Expo web) solo en Development.

**Pruebas:**
- Unitarias de `BusPositionTracker`: una posición más nueva se acepta, una más vieja o igual se rechaza, y cada bus se maneja aparte.
- Integración con `Microsoft.AspNetCore.SignalR.Client` (paquete NuGet nuevo, solo en el proyecto de pruebas):
  - un cliente llama a `JoinRoute("route-01")`, se hace `POST /api/locations` y el cliente recibe `busMoved` con el mismo `eventId` y la `nextStopName` correcta;
  - un cliente unido a otra ruta no recibe nada.

Commit: `feat: add signalr hub and realtime location consumer`

### Paso 5 · Acceso desde el iPhone

- **Un perfil `lan`** en `launchSettings.json` con `http://0.0.0.0:5080`, para que la API escuche en la red local. El perfil `http` sigue igual (`localhost`).
- **El README explica:**
  - cómo ver la IP del PC (`ipconfig`);
  - la regla del Firewall de Windows, que se ejecuta una vez en una terminal de administrador:
    ```powershell
    Set-NetConnectionProfile -InterfaceAlias "Wi-Fi" -NetworkCategory Private
    New-NetFirewallRule -DisplayName "BusTracking API 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
    New-NetFirewallRule -DisplayName "BusTracking Expo 8081" -Direction Inbound -Protocol TCP -LocalPort 8081 -Action Allow -Profile Private
    ```
  - que la red Wi-Fi debe estar marcada como **privada** en Windows. En el PC de desarrollo estaba como pública, y por eso se agregó el `Set-NetConnectionProfile`;
  - que Expo Go también necesita el puerto **8081**, porque descarga el código de la app del servidor de Expo en el PC.
- **Verificación manual:** desde Safari en el iPhone, abrir `http://<ip-del-pc>:5080/health` y ver `healthy`. Si no funciona, el plan B es ngrok, documentado en el README pero sin automatizar.

Commit: `chore: allow lan access to the api`

### Paso 6 · App Expo: base y conexión

- Crear `mobile/` con `npx create-expo-app@latest mobile --template blank-typescript@sdk-54`. Verifica que `package.json` quede en `expo ~54`.
- Paquetes, siempre con `npx expo install` para que coincidan con el SDK: `react-native-maps`, `@microsoft/signalr`, `leaflet`, `react-leaflet` y `@types/leaflet`. `@expo/vector-icons` ya viene con Expo.
- **Configuración:** `EXPO_PUBLIC_API_URL` en `mobile/.env`, que no se sube. Se sube `mobile/.env.example` con `EXPO_PUBLIC_API_URL=http://192.168.1.10:5080`.
- **`src/api.ts`:** las funciones `getRoutes()` y `getRoute(routeId)`, con tipos que reflejan los contratos del backend.
- **`src/tracking.ts`:**
  - la conexión de SignalR usa `withAutomaticReconnect()`;
  - expone el estado `connected | reconnecting | disconnected`;
  - al conectar y al reconectar, vuelve a llamar a `JoinRoute`.
- **`src/busStore.ts`:**
  - es lógica pura, sin React, y guarda la última `BusPosition` de cada bus;
  - **ignora una posición con `recordedAt` anterior** a la que ya tiene. Es la regla del brief y se cumple también en el cliente.

Commit: `feat: scaffold expo app with routes api and signalr connection`

### Paso 7 · App Expo: pantallas del mock-up

- **Mapa en vivo:**
  - `MapView` centrado en la ruta, con `Polyline` para el recorrido, un `Marker` por parada y un `Marker` por bus (ícono de bus, número y flecha según `heading`);
  - el marcador **se anima** entre posiciones para que el bus no salte;
  - en el panel inferior, la lista de buses con su próxima parada, velocidad y "hace N s", que se actualiza cada segundo.
- **Selector de ruta:** con una sola ruta muestra el nombre. Con varias, abre una lista simple.
- **Bus seleccionado:** tocar un bus en el mapa o en la lista lo resalta y muestra el detalle. El botón de volver regresa a la lista.
- **Sin conexión:** cuando el estado es `reconnecting` o `disconnected`, aparecen el aviso y el indicador naranja, los buses se ven en gris y el tiempo se marca en naranja.
- **Textos en español**, con botones de al menos 44 px y `accessibilityLabel` en los botones que solo tienen ícono.
- **Un componente de mapa por plataforma**, con las mismas props (ruta, paradas, buses, bus seleccionado y estado de la conexión):
  - `src/components/LiveMap.native.tsx` para el iPhone, con `react-native-maps` (Apple Maps, sin API key);
  - `src/components/LiveMap.web.tsx` para el navegador, con `react-leaflet` y mapas de **OpenStreetMap** (`https://tile.openstreetmap.org/{z}/{x}/{y}.png`). Muestra siempre la atribución "© OpenStreetMap contributors" y respeta la [política de uso](https://operations.osmfoundation.org/policies/tiles/): solo para la demo local y con poco tráfico.
  - El resto de la pantalla (panel, lista y avisos) es igual en las dos plataformas.

Commit: `feat: add live map screen`

### Paso 8 · Pruebas y CI de la app

- **Pruebas de la lógica pura** (`busStore`): una posición vieja se ignora, una más nueva reemplaza a la anterior y cada bus se maneja aparte. Necesitan `jest` y `jest-expo`, que son paquetes nuevos (ver la pregunta abierta 2).
- **En `ci.yml`, un job `mobile`** con `working-directory: mobile`: `npm ci`, `npx tsc --noEmit`, `npx expo lint` y `npm test`.

Commit: `test: add mobile unit tests and ci job`

### Paso 9 · README

- Estado: *Phase 2 of 5*. El diagrama marca como existentes el hub de SignalR, el consumidor de tiempo real y la app.
- Cómo correr la app: `cd mobile && npm install && npx expo start`, escanear el QR con la cámara del iPhone y configurar `EXPO_PUBLIC_API_URL`.
- La latencia de la fase 1 (p50 = 4 ms, p95 = 16 ms), anotada como simulador → EventTail.
- Una captura o un GIF del mapa en el iPhone, si ya existe.

Commit: `docs: update readme for phase 2`

## Checklist de cierre

- [ ] La cola `realtime.location` aparece en el panel de RabbitMQ como durable (quorum), con DLX `tracking.dlx`.
- [ ] Simulador → API → RabbitMQ → hub → app funciona con 2 buses en `route-01`, en el iPhone y en el navegador.
- [ ] Al apagar el Wi-Fi del iPhone, la app muestra "Reconectando". Al volver la red, se recupera sola y los buses siguen moviéndose.
- [ ] Al reiniciar la API, los mensajes esperan en `realtime.location` y se entregan al volver.
- [ ] Un mensaje que falla 3 veces termina en `tracking.dead-letter`.
- [ ] `dotnet test` en verde, incluidas las pruebas de integración del consumidor y del hub.
- [ ] `dotnet format --verify-no-changes` sin cambios y CI en verde, con los jobs `backend` y `mobile`.
- [ ] Ningún secreto, `.env` ni IP personal en el repositorio, solo en `.env.example`.

## Decisiones tomadas antes de empezar

1. ~~**El mapa en el navegador.**~~ **Decidido:** el iPhone usa Apple Maps (`react-native-maps`) y la web usa OpenStreetMap (`react-leaflet`). `react-native-maps` no funciona en Expo web.
2. **Aprobado.** Paquetes nuevos que usa este plan:
   - NuGet `Microsoft.AspNetCore.SignalR.Client`, solo en las pruebas;
   - npm `react-native-maps`, `@microsoft/signalr`, `leaflet`, `react-leaflet`, `@types/leaflet`, `jest` y `jest-expo`.

   SignalR del servidor ya viene en ASP.NET Core.
3. **Aprobado.** La próxima parada se calcula en el servidor (paso 1 y paso 4) y llega en `BusPosition`. Así la app no repite lógica geográfica, y la fase 4 reutiliza el avance sobre la ruta. No incluye ETA.

## Para preparar la fase 3

Al terminar, anota y comparte:

- La latencia observada del simulador a la app (si es posible, con un log en la app de `ahora - recordedAt`).
- Si la conexión del iPhone al PC funcionó con la IP local o hizo falta ngrok.
- Si el mapa de OpenStreetMap en el navegador sirve para mostrar el modo conductor en la fase 3.
- Problemas con el SDK 54 de Expo o con `react-native-maps` en Expo Go.
