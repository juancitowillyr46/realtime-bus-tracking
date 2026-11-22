import { describe, expect, it } from '@jest/globals';

import { applyPosition, applySnapshot, emptyBusState, secondsSince, sortedBuses } from '../busStore';
import type { BusPosition } from '../types';

const t0 = Date.parse('2026-11-22T12:00:00Z');

function position(busId: string, secondsAfterT0: number, latitude = 4.81): BusPosition {
  return {
    eventId: `${busId}-${secondsAfterT0}`,
    busId,
    routeId: 'route-01',
    latitude,
    longitude: -75.7,
    speedKmh: 25,
    heading: 270,
    recordedAt: new Date(t0 + secondsAfterT0 * 1000).toISOString(),
    nextStopId: 'stop-02',
    nextStopName: 'Parada El Roble',
    distanceFromRouteMeters: 12,
  };
}

describe('applyPosition', () => {
  it('adds the first position of a bus', () => {
    const first = position('bus-01', 0);

    expect(applyPosition(emptyBusState, first)).toEqual({ 'bus-01': first });
  });

  it('replaces the position with a newer one', () => {
    const state = applyPosition(emptyBusState, position('bus-01', 0));
    const newer = position('bus-01', 5, 4.82);

    expect(applyPosition(state, newer)['bus-01']).toBe(newer);
  });

  it('ignores an older position and returns the same state object', () => {
    const state = applyPosition(emptyBusState, position('bus-01', 5));

    expect(applyPosition(state, position('bus-01', 0))).toBe(state);
  });

  it('ignores a position recorded at the same time', () => {
    const state = applyPosition(emptyBusState, position('bus-01', 5));

    expect(applyPosition(state, position('bus-01', 5, 4.9))).toBe(state);
  });

  it('keeps each bus separately', () => {
    let state = applyPosition(emptyBusState, position('bus-01', 10));
    // Older than bus-01's position, but bus-02 has none yet.
    state = applyPosition(state, position('bus-02', 0));

    expect(Object.keys(state).sort()).toEqual(['bus-01', 'bus-02']);
  });
});

describe('applySnapshot', () => {
  it('keeps a live position that is newer than the snapshot', () => {
    const live = position('bus-01', 10);
    const state = applyPosition(emptyBusState, live);

    const next = applySnapshot(state, [position('bus-01', 5), position('bus-02', 5)]);

    expect(next['bus-01']).toBe(live);
    expect(next['bus-02']?.recordedAt).toBe(position('bus-02', 5).recordedAt);
  });
});

describe('sortedBuses', () => {
  it('sorts by bus id', () => {
    const state = applySnapshot(emptyBusState, [position('bus-02', 0), position('bus-01', 0)]);

    expect(sortedBuses(state).map((bus) => bus.busId)).toEqual(['bus-01', 'bus-02']);
  });
});

describe('secondsSince', () => {
  it('counts whole seconds since the position was recorded', () => {
    expect(secondsSince(position('bus-01', 0), t0 + 4900)).toBe(4);
  });

  it('is never negative when the phone clock is behind', () => {
    expect(secondsSince(position('bus-01', 10), t0)).toBe(0);
  });
});
