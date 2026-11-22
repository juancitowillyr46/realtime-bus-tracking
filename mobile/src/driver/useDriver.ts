import * as Location from 'expo-location';
import { useEffect, useRef, useState } from 'react';

import { postLocation } from '../api';
import { readingToSend, SEND_INTERVAL_MS, toLocationReport, type GpsReading } from './report';

export type DriverStatus =
  | 'idle'
  /** Waiting for the permission or the first GPS reading. */
  | 'starting'
  | 'sending'
  /** The latest reading was too inaccurate to send. */
  | 'weakSignal'
  /** The API could not be reached or rejected the last position. */
  | 'sendFailed'
  | 'permissionDenied';

export interface DriverState {
  status: DriverStatus;
  sentCount: number;
  /** Phone time of the last successful send, in ms. */
  lastSentAt: number | null;
  /** The latest GPS reading, sent or not: its accuracy and speed are shown on screen. */
  lastReading: GpsReading | null;
}

const initialState: DriverState = { status: 'idle', sentCount: 0, lastSentAt: null, lastReading: null };

/**
 * While `active`, reads the phone's GPS (foreground only: Expo Go stops it when the app is in
 * the background or the phone is locked) and sends the latest good reading every 5 s.
 */
export function useDriver(busId: string, routeId: string, active: boolean): DriverState {
  const [state, setState] = useState<DriverState>(initialState);
  const latestReading = useRef<GpsReading | null>(null);

  useEffect(() => {
    if (!active) {
      return;
    }

    let stopped = false;
    let subscription: Location.LocationSubscription | null = null;
    let lastSentTimestamp: number | null = null;
    let lastHeading: number | null = null;
    let sending = false;

    const start = async () => {
      const permission = await Location.requestForegroundPermissionsAsync();
      if (stopped) {
        return;
      }
      if (permission.status !== Location.PermissionStatus.GRANTED) {
        setState((current) => ({ ...current, status: 'permissionDenied' }));
        return;
      }

      subscription = await Location.watchPositionAsync(
        { accuracy: Location.Accuracy.BestForNavigation, timeInterval: 1000, distanceInterval: 0 },
        (location) => {
          const reading: GpsReading = {
            latitude: location.coords.latitude,
            longitude: location.coords.longitude,
            accuracy: location.coords.accuracy,
            speed: location.coords.speed,
            heading: location.coords.heading,
            timestamp: location.timestamp,
          };
          latestReading.current = reading;
          setState((current) => ({ ...current, lastReading: reading }));
        },
      );
      if (stopped) {
        subscription.remove();
      }
    };

    const sendLatest = async () => {
      const reading = readingToSend(latestReading.current, lastSentTimestamp);
      if (!reading || sending) {
        return;
      }

      const result = toLocationReport(reading, busId, routeId, lastHeading);
      if (!result.ok) {
        setState((current) => ({ ...current, status: 'weakSignal' }));
        return;
      }

      sending = true;
      try {
        await postLocation(result.report);
        lastSentTimestamp = reading.timestamp;
        lastHeading = result.report.heading;
        if (!stopped) {
          setState((current) => ({
            ...current,
            status: 'sending',
            sentCount: current.sentCount + 1,
            lastSentAt: Date.now(),
          }));
        }
      } catch {
        // Not queued for later: the next reading replaces it, which is what the map needs.
        if (!stopped) {
          setState((current) => ({ ...current, status: 'sendFailed' }));
        }
      } finally {
        sending = false;
      }
    };

    void start();
    const timer = setInterval(() => void sendLatest(), SEND_INTERVAL_MS);

    return () => {
      stopped = true;
      clearInterval(timer);
      subscription?.remove();
      latestReading.current = null;
      setState(initialState);
    };
  }, [active, busId, routeId]);

  // Until the permission answer or the first send, an active driver is "starting".
  return active && state.status === 'idle' ? { ...state, status: 'starting' } : state;
}
