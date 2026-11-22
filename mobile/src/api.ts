import { API_URL } from './config';
import type { BusStatus, LocationReport, RouteDetails, RouteSummary } from './types';

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

export function getBuses(): Promise<BusStatus[]> {
  return getJson<BusStatus[]>('/api/buses');
}

/** Sends a driver position. Throws when the API rejects it or cannot be reached. */
export async function postLocation(report: LocationReport): Promise<void> {
  const response = await fetch(`${API_URL}/api/locations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(report),
  });
  if (!response.ok) {
    throw new Error(`POST /api/locations failed with ${response.status}`);
  }
}
