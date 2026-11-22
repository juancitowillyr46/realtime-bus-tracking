import { API_URL } from './config';
import type { RouteDetails, RouteSummary } from './types';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, { headers: { Accept: 'application/json' } });
  if (!response.ok) {
    throw new Error(`GET ${path} failed with ${response.status}`);
  }
  return (await response.json()) as T;
}

export function getRoutes(): Promise<RouteSummary[]> {
  return getJson<RouteSummary[]>('/api/routes');
}

export function getRoute(routeId: string): Promise<RouteDetails> {
  return getJson<RouteDetails>(`/api/routes/${encodeURIComponent(routeId)}`);
}
