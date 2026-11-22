# Resumen ejecutivo · Monitoreo de buses escolares en tiempo real

**Estado:** prueba de concepto, fase 3 de 5 completada (noviembre de 2026).

## Qué es

Un prototipo para que los padres vean **en vivo, en el celular, dónde va el bus escolar** de sus hijos y a qué parada se dirige. Es una prueba de concepto: busca demostrar que la idea funciona de punta a punta antes de invertir en un producto.

## Qué funciona hoy

- **Mapa en vivo:** los buses se mueven en el mapa del iPhone y del navegador, y se actualizan cada 5 segundos.
- **El celular del conductor como GPS:** con la app abierta, el iPhone del conductor envía su ubicación real. No hace falta instalar equipos en el bus.
- **Información útil para el padre:** la próxima parada, la velocidad y hace cuánto se actualizó cada bus. Si el bus se sale de su ruta, la app lo indica.
- **Tolerancia a fallas:** si se cae la conexión, la app avisa y se recupera sola. Si una parte del sistema se detiene, ninguna ubicación aceptada se pierde: queda en espera hasta que vuelve.
- **Probado en un iPhone real** en Pereira, con una precisión de GPS de 12 a 14 metros.

## Resultados clave

| Indicador | Resultado |
| --- | --- |
| Tiempo para distribuir una ubicación por el sistema central | 4 ms en promedio y 16 ms en el peor 5 % (prueba local) |
| Frecuencia de actualización | Cada 5 segundos |
| Pruebas automáticas | 134, que se ejecutan en cada cambio |
| Costo de infraestructura | Cero: todo corre en un PC, con herramientas gratuitas |

## Cómo está construido (en una frase)

Los buses envían su ubicación a un servidor, que la reparte a través de un **sistema de mensajería** (RabbitMQ) a módulos independientes. Así, agregar funciones nuevas (alertas, historial) no obliga a cambiar lo que ya funciona, y una falla en un módulo no detiene a los demás.

## Decisiones importantes

- **El celular del conductor en lugar de un GPS dedicado:** abarata y acelera la prueba.
- **Componentes estándar y gratuitos:** RabbitMQ, .NET, React Native y mapas de Apple y de OpenStreetMap.
- **Privacidad desde el diseño:** solo datos ficticios, y no se publicaron videos que muestren la ubicación real de las personas.

## Límites actuales

- El GPS del conductor **solo funciona con la app abierta**. Para que funcione en segundo plano hace falta publicar la app, lo que requiere una cuenta de Apple Developer (con costo anual).
- **No hay usuarios ni contraseñas** todavía: es un prototipo en una red local.
- **No se guarda el historial** de recorridos (llega en la fase 5).

## Próximos pasos

| Fase | Entrega | Valor para el padre |
| --- | --- | --- |
| 4 · Geocercas y alertas | Aviso "el bus se acerca a tu parada" y tiempo estimado de llegada | Saber cuándo salir a la parada |
| 5 · Historial y administración | Recorridos guardados y gestión de buses | Revisar recorridos pasados; administrar la flota |
| 6 · Opcional | GPS en segundo plano y notificaciones push | Uso real sin la app abierta (requiere la cuenta de Apple) |

## Para llevarlo a producción

Habría que sumar:
- **seguridad:** usuarios y cifrado;
- **almacenamiento:** una base de datos para el historial;
- **la app publicada en las tiendas**, para tener GPS en segundo plano y notificaciones;
- **servidores en la nube** con varios equipos, para atender a muchos buses;
- **una política de privacidad** sobre los datos de ubicación.

La base técnica (mensajería con garantías de entrega y tiempo real) ya está demostrada y se puede reutilizar.

---

Detalle técnico: [`docs/technical-overview.md`](technical-overview.md).
