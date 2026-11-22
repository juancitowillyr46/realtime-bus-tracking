import type { BusPosition, RouteDetails } from '../types';

/** Props shared by LiveMap.tsx (iPhone, Apple Maps) and LiveMap.web.tsx (browser, OpenStreetMap). */
export interface LiveMapProps {
  route: RouteDetails;
  buses: BusPosition[];
  selectedBusId: string | null;
  /** Reconnecting or disconnected: buses are drawn grey at their last known position. */
  offline: boolean;
  onSelectBus: (busId: string) => void;
  /** Space covered by the top bar and the bottom panel, so the route is fitted inside the rest. */
  insets: { top: number; bottom: number };
  /** Changes when the user taps "center": the map fits the whole route again. */
  recenterKey: number;
}
