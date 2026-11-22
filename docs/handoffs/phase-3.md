# Handoff · Fase 3: modo conductor

> Lee primero `CLAUDE.md` y `docs/brief.md`. Este documento es el plan **solo de la fase 3**.

## Objetivo

Que el iPhone haga de bus: en el **modo conductor**, la app lee el GPS real del teléfono mientras está abierta y envía una posición cada 5 segundos a `POST /api/locations`, igual que el simulador. Desde ahí todo funciona como en la fase 2 (RabbitMQ → SignalR → mapa).

**Listo cuando:** tu propio recorrido por Pereira aparece en el mapa, por ejemplo en el navegador del PC en modo padre, mientras el iPhone está en modo conductor.

## Lo que dejó la fase 2

- La app corre en Expo Go con el **SDK 58**. Expo Go exige iniciar sesión con la misma cuenta de Expo en el PC y en el teléfono.
- La conexión del iPhone al PC funcionó con la **IP local**; no hizo falta ngrok. La IP del PC cambia a veces, así que hay que actualizar `mobile/.env`.
- Desde RabbitMQ 4.3, los consumidores usan `basic.reject` (no `basic.nack`) para que cuente `x-delivery-limit`.
- El mapa del navegador con OpenStreetMap sirve para mostrar el modo padre mientras el iPhone está en modo conductor.
- Quedaron pendientes:
  - probar a mano el aviso "Reconectando";
  - medir la latencia hasta la app;
  - subir de versión las acciones del CI que usan Node 20;
  - mover la píldora "En vivo", que tapa el engranaje de Expo Go.

## Diseño aprobado

El mock-up está en https://claude.ai/artifact/PJi7mtgvWciR9SZquVjhY5 y tiene 4 pantallas:

1. **Empezar recorrido:**
   - un selector **Padre / Conductor**;
   - elegir el bus (los que están en uso aparecen deshabilitados como "En uso");
   - elegir la ruta;
   - un aviso de que la ubicación se usa solo con la app abierta;
   - el botón **Iniciar recorrido**.
2. **Conduciendo:**
   - un mapa con tu bus y un círculo con la precisión del GPS;
   - el estado "Enviando";
   - la precisión, el último envío, la velocidad y las posiciones enviadas;
   - la próxima parada;
   - el botón **Detener recorrido**.
3. **Fuera de ruta y señal débil:**
   - "Estás fuera de la ruta (N m)": se sigue enviando, sin próxima parada;
   - "Señal GPS débil": la lectura no se envía.
4. **Sin permiso de ubicación:** los pasos para activarlo en Ajustes, y un botón para volver al modo padre.

Reglas aprobadas con el mock-up:
- se envía como máximo una posición cada **5 s**;
- una lectura con precisión peor que **30 m** no se envía;
- **fuera de ruta** significa estar a más de **150 m** de la línea de la ruta;
- se prueba **en Pereira** sobre `route-01`.

## Fuera de alcance en esta fase

- GPS en segundo plano y pantalla siempre encendida (fase 6).
- Autenticación del conductor o del endpoint. `POST /api/locations` sigue abierto en la red local; es una POC sin despliegue.
- Geocercas, ETA y alertas (fase 4).
- CRUD de buses y PostgreSQL (fase 5). La flota sale de la configuración.
- Guardar posiciones sin conexión para enviarlas después: si un envío falla, se descarta y se envía la siguiente lectura.

## Antes de empezar (lo hace el desarrollador)

- [ ] En el iPhone, dar permiso de ubicación a Expo Go cuando lo pida ("Mientras se usa la app").
- [ ] Revisar la IP del PC y actualizar `mobile/.env` si cambió.
- [ ] (Opcional) Dibujar en geojson.io la ruta real por donde vas a pasar en Pereira y reemplazar `data/routes/route-01.geojson`, sin cambiar el `routeId`.

## Pasos

Al final de cada paso: build, pruebas (`dotnet test` y `npm test`), `dotnet format`, lint y un commit con el mensaje sugerido.

### Paso 1 · Pendientes de la fase 2

- **CI:** `actions/checkout@v7`, `actions/setup-dotnet@v6` y `actions/setup-node@v7`.
- **Píldora "En vivo":** en desarrollo (`__DEV__`), dejar a la derecha de la barra superior un espacio para el engranaje de Expo Go.
- **Latencia hasta la app:** en desarrollo, la app registra `ahora - recordedAt` de cada `busMoved` y cada 60 s escribe en la consola de Metro el p50 y el p95. Es una función pura con pruebas (`latencyStats`).

Commit: `chore: close phase 2 leftovers`

### Paso 2 · Distancia a la ruta en el backend

- **`RoutePath.DistanceFromLine(GeoPoint point)`:** la distancia en metros desde el punto hasta el tramo más cercano de la línea. Reusa la proyección de `DistanceAlong`.
- **`RouteDefinition`:** la constante `OffRouteThresholdMeters = 150`. `NextStop` devuelve `null` si el punto está fuera de ruta.
- **`BusPosition`** gana `distanceFromRouteMeters` (`double?`, `null` si la ruta no existe). La app usa este campo para el aviso "fuera de ruta".
- **Pruebas:** la distancia de un punto sobre la línea es 0; la de un punto a un lado de un tramo es correcta; la de un punto más allá del final es la distancia hasta el último vértice; `NextStop` es `null` fuera de ruta.

Commit: `feat: report distance from the route`

### Paso 3 · Flota y buses en uso

- **Configuración `Fleet:BusIds`** en `appsettings.json`: `bus-01` a `bus-05`, todos ficticios.
- **`GET /api/buses`** devuelve `[{ busId, inUse, routeId, lastSeenAt }]`. `inUse` es `true` si el bus reportó en los últimos 60 s, según `BusPositionTracker`. Un bus que reporta pero no está en la flota también aparece.
- **`BusPositionTracker.GetAll()`** para el endpoint, con `TimeProvider` para las pruebas.
- **Pruebas:** unitarias del cálculo de `inUse` y una de integración del endpoint.

Commit: `feat: add fleet endpoint`

### Paso 4 · Lectura del GPS y envío

- **Paquete:** `expo-location` (el brief lo nombra), instalado con `npx expo install`.
- **`src/driver/report.ts`, lógica pura:**
  - `toLocationReport(reading, busId, routeId)` devuelve el reporte o el motivo por el que se descarta (`'inaccurate'`);
  - convierte la velocidad de m/s a km/h (si viene negativa o `null`, usa 0);
  - si el rumbo viene `-1` o `null`, usa el último rumbo conocido o 0;
  - `recordedAt` es la hora de la lectura en ISO 8601 UTC.
- **`src/driver/useDriver.ts`:**
  - pide el permiso;
  - usa `watchPositionAsync` en primer plano;
  - guarda la última lectura;
  - cada 5 s envía la más reciente que sea válida y no se haya enviado, con `postLocation` en `api.ts`;
  - expone el estado: `idle | starting | sending | weakSignal | sendFailed | permissionDenied`, el contador de enviadas, la hora del último envío y la última lectura.
- **Pruebas:** la conversión de velocidad y de rumbo, la lectura imprecisa y la elección de qué lectura enviar.

Commit: `feat: read gps and send driver locations`

### Paso 5 · Pantallas del modo conductor

- **Selector Padre / Conductor** arriba de la app; solo cambia un estado en `App.tsx`, sin navegación.
- **Empezar recorrido:** la lista de buses de `GET /api/buses` (los que están en uso, deshabilitados), la ruta y el botón **Iniciar recorrido**.
- **Conduciendo:**
  - el mapa (`LiveMap`) con la ruta y **solo tu bus**, que se sigue por SignalR como en el modo padre;
  - el panel con el estado, los datos y la próxima parada;
  - el botón **Detener recorrido**;
  - los avisos de **fuera de ruta** (con `distanceFromRouteMeters`) y de **señal débil**.
- **Sin permiso:** la pantalla 4 del mock-up. "Abrir Ajustes" usa `Linking.openSettings()`.
- **Textos en español** y botones de al menos 44 px, igual que en la fase 2.

Commit: `feat: add driver mode screens`

### Paso 6 · README y cierre

- Estado: *Fase 3 de 5*. El diagrama (`docs/media/architecture.svg`) pasa el modo conductor a línea continua.
- Cómo usar el modo conductor y qué limita Expo Go (solo con la app abierta).
- La latencia medida hasta la app, si ya se midió.

Commit: `docs: update readme for phase 3`

## Checklist de cierre

- [ ] Con el iPhone en modo conductor caminando o manejando por Pereira, el recorrido aparece en el mapa del navegador (modo padre).
- [ ] Una lectura con mala precisión muestra "Señal GPS débil" y no se envía.
- [ ] Fuera de la ruta (más de 150 m) aparece el aviso y la app sigue enviando.
- [ ] Sin permiso de ubicación aparece la pantalla 4.
- [ ] El aviso "Reconectando" del modo padre probado a mano (pendiente de la fase 2).
- [ ] Latencia hasta la app (p50 y p95) anotada en el README.
- [ ] `dotnet test`, `dotnet format`, lint, `npm test` y CI en verde.
- [ ] Ningún secreto, `.env` ni IP personal en el repositorio.

## Resultado de la prueba en el iPhone (2026-11-22)

- El modo conductor funcionó en un iPhone con Expo Go: la flota marcó "En uso" los buses del simulador, el GPS dio una precisión de **±12 a 14 m** y las posiciones llegaron de vuelta por SignalR.
- La prueba se hizo lejos de `route-01`, así que apareció "Estás fuera de la ruta (6,3 km)" y no hubo próxima parada, como se diseñó.
- **Corrección hecha después de la prueba:** con el teléfono quieto, iOS deja de entregar lecturas y la app dejaba de enviar ("Último envío hace 12 s"). Ahora reenvía la última lectura con la hora actual mientras tenga menos de 30 s (`planSend` en `driver/report.ts`).
- Por privacidad, el GIF del modo conductor (`docs/media/driver-mode.gif`) oculta el mapa y el aviso de distancia a la ruta: el video mostraba la ubicación real de quien probaba.

## Para preparar la fase 4

Al terminar, anota:

- Qué tan precisas fueron las lecturas del GPS en Pereira y si el umbral de 30 m sirvió.
- Si la ruta provisional coincide con las calles por donde pasaste o hay que redibujarla.
- La latencia observada hasta la app.
