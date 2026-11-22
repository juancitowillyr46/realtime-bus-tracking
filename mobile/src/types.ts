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

/** Body of POST /api/locations (LocationReport in the backend). */
export interface LocationReport {
  busId: string;
  routeId: string;
  latitude: number;
  longitude: number;
  speedKmh: number;
  heading: number;
  /** ISO 8601, UTC: when the device took the reading. */
  recordedAt: string;
}

/** An item of GET /api/buses. */
export interface BusStatus {
  busId: string;
  /** Reported in the last 60 s: another phone or the simulator is driving it. */
  inUse: boolean;
  routeId: string | null;
  lastSeenAt: string | null;
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
  /** Null when the route is unknown, has no stops or the bus is off the route. */
  nextStopId: string | null;
  nextStopName: string | null;
  /** Meters from the route's line; more than 150 means off the route. Null when the route is unknown. */
  distanceFromRouteMeters: number | null;
}

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';
