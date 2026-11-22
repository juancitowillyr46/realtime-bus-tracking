// Expo inlines EXPO_PUBLIC_* variables from mobile/.env at bundle time.
const apiUrl = process.env.EXPO_PUBLIC_API_URL;

if (!apiUrl) {
  throw new Error(
    'EXPO_PUBLIC_API_URL is not set. Copy mobile/.env.example to mobile/.env and restart `npx expo start`.',
  );
}

/** Base URL of the BusTracking API, without a trailing slash. */
export const API_URL = apiUrl.replace(/\/+$/, '');

/** Same values as TrackingHubContract in the backend. */
export const TRACKING_HUB = {
  path: '/hubs/tracking',
  joinRoute: 'JoinRoute',
  leaveRoute: 'LeaveRoute',
  busMoved: 'busMoved',
} as const;
