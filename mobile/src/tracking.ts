import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
  type IRetryPolicy,
} from '@microsoft/signalr';

import { API_URL, TRACKING_HUB } from './config';
import type { BusPosition, ConnectionStatus } from './types';

export interface TrackingHandlers {
  /** A bus moved (live update). */
  onBusMoved: (position: BusPosition) => void;
  /** The route's current positions, after every (re)join. */
  onSnapshot: (positions: BusPosition[]) => void;
  onStatus: (status: ConnectionStatus) => void;
}

// Retry quickly at first, then every 10 s for as long as the app is open. The default
// policy gives up after four tries, which would leave the map stuck after a long outage.
const keepRetrying: IRetryPolicy = {
  nextRetryDelayInMilliseconds: ({ previousRetryCount }) => [0, 2000, 5000][previousRetryCount] ?? 10_000,
};

const RESTART_DELAY_MS = 5000;

/**
 * Connects to the tracking hub and follows one route. Reconnects on its own after a network
 * drop and joins the route again, because SignalR groups do not survive a reconnection.
 * Call the returned function to disconnect.
 */
export function followRoute(routeId: string, handlers: TrackingHandlers): () => void {
  const connection: HubConnection = new HubConnectionBuilder()
    .withUrl(`${API_URL}${TRACKING_HUB.path}`)
    .withAutomaticReconnect(keepRetrying)
    .configureLogging(LogLevel.Warning)
    .build();

  let stopped = false;
  let restartTimer: ReturnType<typeof setTimeout> | undefined;

  const join = async () => {
    const positions = await connection.invoke<BusPosition[]>(TRACKING_HUB.joinRoute, routeId);
    handlers.onSnapshot(positions);
  };

  // The first connection is not covered by automatic reconnect: retry it by hand.
  const start = async () => {
    if (stopped) {
      return;
    }
    handlers.onStatus('connecting');
    try {
      await connection.start();
      await join();
      handlers.onStatus('connected');
    } catch {
      if (stopped) {
        return;
      }
      handlers.onStatus('disconnected');
      restartTimer = setTimeout(start, RESTART_DELAY_MS);
    }
  };

  // A handler that returns a value is treated as a client result, so never return one.
  connection.on(TRACKING_HUB.busMoved, (position: BusPosition) => {
    handlers.onBusMoved(position);
  });
  connection.onreconnecting(() => handlers.onStatus('reconnecting'));
  connection.onreconnected(async () => {
    try {
      await join();
      handlers.onStatus('connected');
    } catch {
      handlers.onStatus('reconnecting');
    }
  });
  connection.onclose(() => {
    if (!stopped) {
      handlers.onStatus('disconnected');
      restartTimer = setTimeout(start, RESTART_DELAY_MS);
    }
  });

  void start();

  return () => {
    stopped = true;
    clearTimeout(restartTimer);
    if (connection.state !== HubConnectionState.Disconnected) {
      void connection.stop();
    }
  };
}
