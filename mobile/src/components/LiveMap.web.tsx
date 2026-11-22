// Browser map (npx expo start --web): OpenStreetMap tiles through react-leaflet.
// Tile usage policy: https://operations.osmfoundation.org/policies/tiles/ — fine for a local
// demo with little traffic, and the attribution must stay visible.
import 'leaflet/dist/leaflet.css';

import L from 'leaflet';
import { useEffect, useMemo } from 'react';
import { CircleMarker, MapContainer, Marker, Polyline, TileLayer, Tooltip, useMap } from 'react-leaflet';

import { busNumber, busTitle, stopShortName } from '../format';
import { colors } from '../theme';
import type { BusPosition, RoutePathPoint } from '../types';
import type { LiveMapProps } from './LiveMap.types';
import { useSmoothCoordinate } from './useSmoothCoordinate';

const toLatLng = (point: RoutePathPoint): [number, number] => [point.latitude, point.longitude];

export default function LiveMap({
  route,
  buses,
  selectedBusId,
  offline,
  onSelectBus,
  insets,
  recenterKey,
}: LiveMapProps) {
  const path = useMemo(() => route.path.map(toLatLng), [route]);

  return (
    <MapContainer
      bounds={L.latLngBounds(path)}
      style={{ position: 'absolute', inset: 0, backgroundColor: colors.mapGround }}
      zoomControl={false}
    >
      <TileLayer
        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        maxZoom={19}
      />
      <FitRoute path={path} insets={insets} recenterKey={recenterKey} />

      <Polyline positions={path} pathOptions={{ color: colors.route, weight: 5, lineCap: 'round', lineJoin: 'round' }} />

      {route.stops.map((stop) => (
        <CircleMarker
          key={stop.stopId}
          center={toLatLng(stop)}
          radius={6}
          pathOptions={{ color: colors.route, weight: 3, fillColor: colors.surface, fillOpacity: 1 }}
        >
          <Tooltip direction="bottom" offset={[0, 8]}>
            {stopShortName(stop.name)}
          </Tooltip>
        </CircleMarker>
      ))}

      {buses.map((bus) => (
        <BusMarker
          key={bus.busId}
          bus={bus}
          selected={bus.busId === selectedBusId}
          dimmed={selectedBusId !== null && bus.busId !== selectedBusId}
          offline={offline}
          onPress={() => onSelectBus(bus.busId)}
        />
      ))}
    </MapContainer>
  );
}

/** Fits the route between the top bar and the bottom panel, again whenever "center" is tapped. */
function FitRoute({
  path,
  insets,
  recenterKey,
}: {
  path: [number, number][];
  insets: LiveMapProps['insets'];
  recenterKey: number;
}) {
  const map = useMap();
  useEffect(() => {
    map.fitBounds(L.latLngBounds(path), {
      paddingTopLeft: [40, insets.top + 24],
      paddingBottomRight: [40, insets.bottom + 24],
    });
  }, [map, path, insets.top, insets.bottom, recenterKey]);
  return null;
}

interface BusMarkerProps {
  bus: BusPosition;
  selected: boolean;
  dimmed: boolean;
  offline: boolean;
  onPress: () => void;
}

function BusMarker({ bus, selected, dimmed, offline, onPress }: BusMarkerProps) {
  const coordinate = useSmoothCoordinate(bus);
  const icon = useMemo(
    () => busIcon(busNumber(bus.busId), bus.heading, selected, dimmed, offline),
    [bus.busId, bus.heading, selected, dimmed, offline],
  );

  return (
    <Marker
      position={[coordinate.latitude, coordinate.longitude]}
      icon={icon}
      title={busTitle(bus.busId)}
      zIndexOffset={selected ? 1000 : 0}
      eventHandlers={{ click: onPress }}
    />
  );
}

// Same look as the native marker. The text comes from our own data, but it is escaped anyway
// because it ends up in HTML.
function busIcon(number: string, heading: number, selected: boolean, dimmed: boolean, offline: boolean) {
  const fill = offline ? colors.busOffline : colors.bus;
  const ring = selected ? colors.text : colors.surface;
  const arrow = offline
    ? ''
    : `<div style="position:absolute;inset:0;transform:rotate(${heading}deg)">
         <div style="margin:0 auto;width:0;height:0;border-left:7px solid transparent;border-right:7px solid transparent;border-bottom:10px solid ${colors.text}"></div>
       </div>`;
  const html = `
    <div style="position:relative;width:64px;height:64px;opacity:${dimmed ? 0.45 : 1}">
      ${arrow}
      <div style="position:absolute;left:13px;top:13px;width:32px;height:32px;border-radius:19px;background:${fill};border:3px ${offline ? 'dashed' : 'solid'} ${ring};display:flex;align-items:center;justify-content:center">
        ${BUS_SVG}
      </div>
      <div style="position:absolute;right:2px;bottom:4px;min-width:14px;padding:0 4px;border-radius:8px;background:${offline ? colors.busOfflineText : colors.text};color:#fff;font:700 10px system-ui,sans-serif;text-align:center">${escapeHtml(number)}</div>
    </div>`;
  return L.divIcon({ html, className: '', iconSize: [64, 64], iconAnchor: [32, 32] });
}

// A simple bus pictogram, so the web marker does not depend on an icon font.
const BUS_SVG = `<svg width="18" height="18" viewBox="0 0 24 24" aria-hidden="true"><path fill="${colors.text}" d="M6 3h12a2 2 0 0 1 2 2v11a2 2 0 0 1-1 1.7V20a1 1 0 0 1-1 1h-1a1 1 0 0 1-1-1v-1H8v1a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1v-2.3A2 2 0 0 1 4 16V5a2 2 0 0 1 2-2zm0 3v5h12V6H6zm1.5 7.5a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zm9 0a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3z"/></svg>`;

function escapeHtml(text: string): string {
  return text.replace(/[&<>"']/g, (c) => `&#${c.charCodeAt(0)};`);
}
