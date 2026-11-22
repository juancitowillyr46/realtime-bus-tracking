import type { LocationReport } from '../types';

/** A reading worse than this (radius in meters) is not sent: the bus would jump on the map. */
export const MAX_ACCURACY_METERS = 30;

/** At most one position every 5 s, the same rhythm as the simulator. */
export const SEND_INTERVAL_MS = 5000;

/** The API rejects speeds above this (LocationReportValidator). */
const MAX_SPEED_KMH = 150;

/** The part of an expo-location reading the driver mode uses. */
export interface GpsReading {
  latitude: number;
  longitude: number;
  /** Radius in meters; null when the device does not know it. */
  accuracy: number | null;
  /** Meters per second; null or negative when unknown. */
  speed: number | null;
  /** Degrees from north, clockwise; null or -1 when unknown (for example when standing still). */
  heading: number | null;
  /** Milliseconds since the epoch, when the device took the reading. */
  timestamp: number;
}

export type ReportResult = { ok: true; report: LocationReport } | { ok: false; reason: 'inaccurate' };

/**
 * Turns a GPS reading into the body of POST /api/locations, or says why it must not be sent.
 * `lastHeading` keeps the arrow pointing the same way while the phone does not know its heading.
 */
export function toLocationReport(
  reading: GpsReading,
  busId: string,
  routeId: string,
  lastHeading: number | null,
): ReportResult {
  if (reading.accuracy === null || reading.accuracy > MAX_ACCURACY_METERS) {
    return { ok: false, reason: 'inaccurate' };
  }

  return {
    ok: true,
    report: {
      busId,
      routeId,
      latitude: reading.latitude,
      longitude: reading.longitude,
      speedKmh: toKmh(reading.speed),
      heading: toHeading(reading.heading, lastHeading),
      recordedAt: new Date(reading.timestamp).toISOString(),
    },
  };
}

export function toKmh(metersPerSecond: number | null): number {
  if (metersPerSecond === null || !Number.isFinite(metersPerSecond) || metersPerSecond < 0) {
    return 0;
  }
  return Math.min(MAX_SPEED_KMH, Math.round(metersPerSecond * 3.6 * 10) / 10);
}

export function toHeading(degrees: number | null, lastHeading: number | null): number {
  if (degrees === null || !Number.isFinite(degrees) || degrees < 0) {
    return lastHeading ?? 0;
  }
  return degrees % 360;
}

/** The reading to send now: the latest one, unless it was already sent. */
export function readingToSend(latest: GpsReading | null, lastSentTimestamp: number | null): GpsReading | null {
  if (!latest) {
    return null;
  }
  return lastSentTimestamp !== null && latest.timestamp <= lastSentTimestamp ? null : latest;
}
