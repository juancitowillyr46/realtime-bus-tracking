import { describe, expect, it } from '@jest/globals';

import { formatLatencyStats, latencyStats } from '../latency';

describe('latencyStats', () => {
  it('is null without samples', () => {
    expect(latencyStats([])).toBeNull();
  });

  it('computes nearest-rank percentiles on unsorted samples', () => {
    const samples = [50, 10, 40, 20, 30, 60, 70, 80, 90, 100];

    expect(latencyStats(samples)).toEqual({ count: 10, p50: 50, p95: 100, max: 100 });
  });

  it('handles a single sample', () => {
    expect(latencyStats([42])).toEqual({ count: 1, p50: 42, p95: 42, max: 42 });
  });

  it('does not reorder the caller array', () => {
    const samples = [3, 1, 2];
    latencyStats(samples);

    expect(samples).toEqual([3, 1, 2]);
  });
});

describe('formatLatencyStats', () => {
  it('reads as one log line', () => {
    expect(formatLatencyStats({ count: 12, p50: 80, p95: 140, max: 210 })).toBe(
      'latency to app over 12 positions: p50 80 ms, p95 140 ms, max 210 ms',
    );
  });
});
