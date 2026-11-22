import type { LocationReport } from '../types';

/** A reading worse than this (radius in meters) is not sent: the bus would jump on the map. */
export const MAX_ACCURACY_METERS = 30;

/** At most one position every 5 s, the same rhythm as the simulator. */
export const SEND_INTERVAL_MS = 5000;

/**
 * A phone standing still stops delivering new readings. Within this age the last reading is
 * re-sent with the current time, so the bus does not look frozen (or free) while it waits at a
 * stop; older than this, the GPS is treated as lost.
 */
export const MAX_REUSE_AGE_MS = 30_000;

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
  recordedAt: number = reading.timestamp,
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
      recordedAt: new Date(recordedAt).toISOString(),
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

export type SendPlan =
  | { kind: 'send'; reading: GpsReading; recordedAt: number }
  /** No reading yet, or the latest is older than MAX_REUSE_AGE_MS: the GPS is lost. */
  | { kind: 'waiting' };

/**
 * What to send on this 5-second tick, given the latest reading and the recordedAt of the last
 * position sent (both in ms):
 * - a reading newer than the last one sent: send it as it is;
 * - the same reading, still fresh: the phone is standing still, send it again stamped `now`;
 * - no reading, or a stale one: wait for the GPS.
 */
export function planSend(latest: GpsReading | null, lastSentRecordedAt: number | null, now: number): SendPlan {
  if (!latest) {
    return { kind: 'waiting' };
  }
  if (lastSentRecordedAt === null || latest.timestamp > lastSentRecordedAt) {
    return { kind: 'send', reading: latest, recordedAt: latest.timestamp };
  }
  if (now - latest.timestamp <= MAX_REUSE_AGE_MS) {
    return { kind: 'send', reading: latest, recordedAt: now };
  }
  return { kind: 'waiting' };
}
