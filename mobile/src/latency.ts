// End-to-end latency as the app sees it: now - recordedAt of each busMoved.
// It mixes the phone's clock with the simulator's (or the driver phone's), so it is only
// meaningful when both clocks are synchronised, as they are with automatic time on.

export interface LatencyStats {
  count: number;
  p50: number;
  p95: number;
  max: number;
}

/** Nearest-rank percentile of already sorted values. */
function percentile(sorted: readonly number[], p: number): number {
  const rank = Math.max(1, Math.ceil((p / 100) * sorted.length));
  return sorted[rank - 1];
}

/** p50, p95 and max in milliseconds, or null when there are no samples. */
export function latencyStats(samplesMs: readonly number[]): LatencyStats | null {
  if (samplesMs.length === 0) {
    return null;
  }
  const sorted = [...samplesMs].sort((a, b) => a - b);
  return {
    count: sorted.length,
    p50: percentile(sorted, 50),
    p95: percentile(sorted, 95),
    max: sorted[sorted.length - 1],
  };
}

export function formatLatencyStats(stats: LatencyStats): string {
  return `latency to app over ${stats.count} positions: p50 ${stats.p50} ms, p95 ${stats.p95} ms, max ${stats.max} ms`;
}
