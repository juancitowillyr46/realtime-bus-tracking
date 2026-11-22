import { describe, expect, it } from '@jest/globals';

import {
  MAX_REUSE_AGE_MS,
  planSend,
  toHeading,
  toKmh,
  toLocationReport,
  type GpsReading,
} from '../driver/report';

const reading = (overrides: Partial<GpsReading> = {}): GpsReading => ({
  latitude: 4.8136,
  longitude: -75.7068,
  accuracy: 8,
  speed: 6.5,
  heading: 270,
  timestamp: Date.parse('2026-11-22T13:00:00Z'),
  ...overrides,
});

describe('toLocationReport', () => {
  it('builds the body of POST /api/locations', () => {
    const result = toLocationReport(reading(), 'bus-03', 'route-01', null);

    expect(result).toEqual({
      ok: true,
      report: {
        busId: 'bus-03',
        routeId: 'route-01',
        latitude: 4.8136,
        longitude: -75.7068,
        speedKmh: 23.4,
        heading: 270,
        recordedAt: '2026-11-22T13:00:00.000Z',
      },
    });
  });

  it('accepts a reading exactly at the accuracy limit', () => {
    expect(toLocationReport(reading({ accuracy: 30 }), 'bus-03', 'route-01', null).ok).toBe(true);
  });

  it.each([31, 65, null])('rejects a reading with accuracy %p', (accuracy) => {
    expect(toLocationReport(reading({ accuracy }), 'bus-03', 'route-01', null)).toEqual({
      ok: false,
      reason: 'inaccurate',
    });
  });
});

describe('toKmh', () => {
  it('converts meters per second', () => {
    expect(toKmh(10)).toBe(36);
  });

  it.each([null, -1, Number.NaN])('uses 0 for an unknown speed (%p)', (speed) => {
    expect(toKmh(speed)).toBe(0);
  });

  it('caps at the 150 km/h the API accepts', () => {
    expect(toKmh(60)).toBe(150);
  });
});

describe('toHeading', () => {
  it('keeps a known heading', () => {
    expect(toHeading(90, 270)).toBe(90);
  });

  it('wraps 360 to 0', () => {
    expect(toHeading(360, null)).toBe(0);
  });

  it('reuses the last heading when the phone does not know it', () => {
    expect(toHeading(-1, 180)).toBe(180);
    expect(toHeading(null, 180)).toBe(180);
  });

  it('falls back to north without any heading', () => {
    expect(toHeading(-1, null)).toBe(0);
  });
});

describe('toLocationReport with another recordedAt', () => {
  it('stamps the report with the given time, keeping the position of the reading', () => {
    const now = Date.parse('2026-11-22T13:00:20Z');

    const result = toLocationReport(reading(), 'bus-03', 'route-01', null, now);

    expect(result.ok && result.report.recordedAt).toBe('2026-11-22T13:00:20.000Z');
    expect(result.ok && result.report.latitude).toBe(4.8136);
  });
});

describe('planSend', () => {
  const latest = reading();
  const t = latest.timestamp;

  it('waits before the first reading', () => {
    expect(planSend(null, null, t)).toEqual({ kind: 'waiting' });
  });

  it('sends a new reading as it is', () => {
    expect(planSend(latest, null, t + 500)).toEqual({ kind: 'send', reading: latest, recordedAt: t });
    expect(planSend(latest, t - 5000, t + 500)).toEqual({ kind: 'send', reading: latest, recordedAt: t });
  });

  it('re-sends a fresh reading stamped now when the phone stands still', () => {
    const now = t + 5000;

    expect(planSend(latest, t, now)).toEqual({ kind: 'send', reading: latest, recordedAt: now });
  });

  it('keeps re-sending after an earlier re-send, while the reading is fresh', () => {
    const now = t + 10_000;

    expect(planSend(latest, t + 5000, now)).toEqual({ kind: 'send', reading: latest, recordedAt: now });
  });

  it('waits when the latest reading is older than the reuse limit', () => {
    expect(planSend(latest, t, t + MAX_REUSE_AGE_MS + 1)).toEqual({ kind: 'waiting' });
  });
});
