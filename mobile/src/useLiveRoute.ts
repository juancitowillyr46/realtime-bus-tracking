import { useEffect, useState } from 'react';

import { getRoute, getRoutes } from './api';
import { applyPosition, applySnapshot, emptyBusState, type BusState } from './busStore';
import { followRoute } from './tracking';
import type { ConnectionStatus, RouteDetails, RouteSummary } from './types';

export interface LiveRoute {
  routes: RouteSummary[];
  route: RouteDetails | null;
  buses: BusState;
  status: ConnectionStatus;
  /** Set when the routes could not be loaded; shown to the user. */
  error: string | null;
}

/**
 * Loads the routes, picks `routeId` (or the first route when it is null) and follows its buses live.
 */
export function useLiveRoute(routeId: string | null): LiveRoute {
  const [routes, setRoutes] = useState<RouteSummary[]>([]);
  const [route, setRoute] = useState<RouteDetails | null>(null);
  const [buses, setBuses] = useState<BusState>(emptyBusState);
  const [status, setStatus] = useState<ConnectionStatus>('connecting');
  const [error, setError] = useState<string | null>(null);

  const selectedId = routeId ?? routes[0]?.routeId ?? null;

  useEffect(() => {
    getRoutes()
      .then(setRoutes)
      .catch(() => setError('No se pudo conectar con el servidor. Revisa que la API esté encendida.'));
  }, []);

  useEffect(() => {
    if (!selectedId) {
      return;
    }

    let cancelled = false;
    setRoute(null);
    setBuses(emptyBusState);

    getRoute(selectedId)
      .then((details) => !cancelled && setRoute(details))
      .catch(() => !cancelled && setError('No se pudo cargar la ruta.'));

    const stop = followRoute(selectedId, {
      onBusMoved: (position) => setBuses((state) => applyPosition(state, position)),
      onSnapshot: (positions) => setBuses((state) => applySnapshot(state, positions)),
      onStatus: setStatus,
    });

    return () => {
      cancelled = true;
      stop();
    };
  }, [selectedId]);

  return { routes, route, buses, status, error };
}
