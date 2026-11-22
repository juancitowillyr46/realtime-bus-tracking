import type { BusPosition } from './types';

/** The latest position of each bus, by busId. Plain data, no React, so it is easy to test. */
export type BusState = Readonly<Record<string, BusPosition>>;

export const emptyBusState: BusState = {};

function recordedAtMs(position: BusPosition): number {
  return Date.parse(position.recordedAt);
}

/**
 * Applies one position. A position recorded at or before the bus's current one is ignored
 * (the brief's rule: the map never moves a bus backwards), and the same state is returned
 * so React skips the re-render.
 */
export function applyPosition(state: BusState, position: BusPosition): BusState {
  const current = state[position.busId];
  if (current && recordedAtMs(position) <= recordedAtMs(current)) {
    return state;
  }
  return { ...state, [position.busId]: position };
}

/** Applies the positions JoinRoute returns, with the same ordering rule as live updates. */
export function applySnapshot(state: BusState, positions: readonly BusPosition[]): BusState {
  return positions.reduce(applyPosition, state);
}

/** The buses sorted by id, for lists and markers. */
export function sortedBuses(state: BusState): BusPosition[] {
  return Object.values(state).sort((a, b) => a.busId.localeCompare(b.busId));
}

/** Whole seconds since the position was recorded, never negative. */
export function secondsSince(position: BusPosition, nowMs: number): number {
  return Math.max(0, Math.floor((nowMs - recordedAtMs(position)) / 1000));
}
