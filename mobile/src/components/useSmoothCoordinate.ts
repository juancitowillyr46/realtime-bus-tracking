import { useEffect, useRef, useState } from 'react';

export interface Coordinate {
  latitude: number;
  longitude: number;
}

const DURATION_MS = 1200;

const easeInOut = (t: number) => (t < 0.5 ? 2 * t * t : 1 - (-2 * t + 2) ** 2 / 2);

/**
 * Glides from the previous coordinate to `target` over ~1 s instead of jumping, so a bus
 * moves smoothly on the map between its 5-second reports. Works on native and web.
 */
export function useSmoothCoordinate(target: Coordinate): Coordinate {
  const [current, setCurrent] = useState(target);
  const currentRef = useRef(target);

  useEffect(() => {
    const from = currentRef.current;
    if (from.latitude === target.latitude && from.longitude === target.longitude) {
      return;
    }

    const start = Date.now();
    let frame = 0;

    const step = () => {
      const t = Math.min(1, (Date.now() - start) / DURATION_MS);
      const k = easeInOut(t);
      const next = {
        latitude: from.latitude + (target.latitude - from.latitude) * k,
        longitude: from.longitude + (target.longitude - from.longitude) * k,
      };
      currentRef.current = next;
      setCurrent(next);
      if (t < 1) {
        frame = requestAnimationFrame(step);
      }
    };

    frame = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frame);
  }, [target.latitude, target.longitude]);

  return current;
}
