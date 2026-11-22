// User-facing text helpers (Spanish). Pure functions, so they are easy to test.

const compassPoints = ['Norte', 'Noreste', 'Este', 'Sureste', 'Sur', 'Suroeste', 'Oeste', 'Noroeste'] as const;

/** 0° = north, clockwise, as the backend sends it. */
export function headingLabel(headingDegrees: number): string {
  const normalized = ((headingDegrees % 360) + 360) % 360;
  return compassPoints[Math.round(normalized / 45) % 8];
}

/** "bus-01" → "01": the short number shown on markers. Falls back to the whole id. */
export function busNumber(busId: string): string {
  const match = /(\d+)$/.exec(busId);
  return match ? match[1] : busId;
}

/** "bus-01" → "Bus 01". */
export function busTitle(busId: string): string {
  return `Bus ${busNumber(busId)}`;
}

/** "hace 3 s", "hace 2 min", "hace 1 h". */
export function formatAge(seconds: number): string {
  if (seconds < 60) {
    return `hace ${seconds} s`;
  }
  if (seconds < 3600) {
    return `hace ${Math.floor(seconds / 60)} min`;
  }
  return `hace ${Math.floor(seconds / 3600)} h`;
}

/** Drops the "Parada " prefix the route files use, since the UI already says "parada". */
export function stopShortName(name: string): string {
  return name.replace(/^Parada\s+/i, '');
}

/** "850 m", "1,2 km": Spanish decimal comma. */
export function formatDistance(meters: number): string {
  if (meters < 1000) {
    return `${Math.round(meters)} m`;
  }
  return `${(meters / 1000).toFixed(1).replace('.', ',')} km`;
}

/** "± 8 m", or "—" when the device does not know its accuracy. */
export function formatAccuracy(meters: number | null): string {
  return meters === null ? '—' : `± ${Math.round(meters)} m`;
}
