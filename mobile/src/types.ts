// Mirrors of the backend contracts (BusTracking.Contracts), as they arrive in JSON (camelCase).

export interface RouteSummary {
  routeId: string;
  name: string;
}

export interface RoutePathPoint {
  latitude: number;
  longitude: number;
}

export interface RouteStopDetails {
  stopId: string;
  name: string;
  order: number;
  latitude: number;
  longitude: number;
}

export interface RouteDetails {
  routeId: string;
  name: string;
  path: RoutePathPoint[];
  stops: RouteStopDetails[];
}

export interface BusPosition {
  eventId: string;
  busId: string;
  routeId: string;
  latitude: number;
  longitude: number;
  speedKmh: number;
  heading: number;
  /** ISO 8601, UTC: when the bus recorded this position. */
  recordedAt: string;
  nextStopId: string | null;
  nextStopName: string | null;
}

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
