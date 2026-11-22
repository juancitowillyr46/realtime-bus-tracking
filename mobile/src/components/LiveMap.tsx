// iPhone (and Android) map: Apple Maps on iOS through react-native-maps, no API key.
// The browser uses LiveMap.web.tsx instead, because react-native-maps has no web support.
import Ionicons from '@expo/vector-icons/Ionicons';
import { useEffect, useRef } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import MapView, { Marker, Polyline } from 'react-native-maps';

import { busNumber, busTitle, stopShortName } from '../format';
import { colors } from '../theme';
import type { BusPosition } from '../types';
import type { LiveMapProps } from './LiveMap.types';
import { useSmoothCoordinate } from './useSmoothCoordinate';

const SIDE_PADDING = 40;

export default function LiveMap({
  route,
  buses,
  selectedBusId,
  offline,
  onSelectBus,
  insets,
  recenterKey,
}: LiveMapProps) {
  const mapRef = useRef<MapView>(null);

  const fitRoute = (animated: boolean) =>
    mapRef.current?.fitToCoordinates(route.path, {
      edgePadding: { top: insets.top + 24, bottom: insets.bottom + 24, left: SIDE_PADDING, right: SIDE_PADDING },
      animated,
    });

  // Fit the whole route on load, when it changes and when the user taps "center".
  useEffect(() => {
    fitRoute(true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [route.routeId, recenterKey]);

  // Follow the selected bus.
  const selected = buses.find((bus) => bus.busId === selectedBusId);
  const selectedLatitude = selected?.latitude;
  const selectedLongitude = selected?.longitude;
  useEffect(() => {
    if (selectedLatitude !== undefined && selectedLongitude !== undefined) {
      mapRef.current?.animateCamera({ center: { latitude: selectedLatitude, longitude: selectedLongitude } });
    }
  }, [selectedLatitude, selectedLongitude]);

  return (
    <MapView
      ref={mapRef}
      style={StyleSheet.absoluteFill}
      onMapReady={() => fitRoute(false)}
      mapPadding={{ top: insets.top, bottom: insets.bottom, left: 0, right: 0 }}
      showsPointsOfInterests={false}
      showsCompass={false}
      toolbarEnabled={false}
    >
      <Polyline coordinates={route.path} strokeColor={colors.route} strokeWidth={5} lineJoin="round" lineCap="round" />

      {route.stops.map((stop) => (
        <Marker
          key={stop.stopId}
          coordinate={stop}
          title={stopShortName(stop.name)}
          anchor={{ x: 0.5, y: 0.5 }}
          tracksViewChanges={false}
        >
          <View style={styles.stop} />
        </Marker>
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
    </MapView>
  );
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

  return (
    <Marker
      coordinate={coordinate}
      anchor={{ x: 0.5, y: 0.5 }}
      onPress={onPress}
      zIndex={selected ? 2 : 1}
      accessibilityLabel={busTitle(bus.busId)}
    >
      <View style={[styles.busMarker, dimmed && styles.dimmed]}>
        {/* Arrow on the rim pointing where the bus is heading (0° = north). */}
        {!offline && (
          <View style={[styles.headingRing, { transform: [{ rotate: `${bus.heading}deg` }] }]}>
            <View style={styles.headingArrow} />
          </View>
        )}
        <View style={[styles.busCircle, selected && styles.busCircleSelected, offline && styles.busCircleOffline]}>
          <Ionicons name="bus" size={18} color={offline ? colors.busOfflineText : colors.text} />
        </View>
        <View style={[styles.busLabel, offline && styles.busLabelOffline]}>
          <Text style={styles.busLabelText}>{busNumber(bus.busId)}</Text>
        </View>
      </View>
    </Marker>
  );
}

const MARKER = 64;
const CIRCLE = 38;

const styles = StyleSheet.create({
  stop: {
    width: 14,
    height: 14,
    borderRadius: 7,
    backgroundColor: colors.surface,
    borderWidth: 3,
    borderColor: colors.route,
  },
  busMarker: { width: MARKER, height: MARKER, alignItems: 'center', justifyContent: 'center' },
  dimmed: { opacity: 0.45 },
  headingRing: { position: 'absolute', width: MARKER, height: MARKER, alignItems: 'center' },
  headingArrow: {
    width: 0,
    height: 0,
    borderLeftWidth: 7,
    borderRightWidth: 7,
    borderBottomWidth: 10,
    borderLeftColor: 'transparent',
    borderRightColor: 'transparent',
    borderBottomColor: colors.text,
  },
  busCircle: {
    width: CIRCLE,
    height: CIRCLE,
    borderRadius: CIRCLE / 2,
    backgroundColor: colors.bus,
    borderWidth: 3,
    borderColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  busCircleSelected: { borderColor: colors.text },
  busCircleOffline: { backgroundColor: colors.busOffline, borderStyle: 'dashed' },
  busLabel: {
    position: 'absolute',
    right: 2,
    bottom: 4,
    minWidth: 22,
    paddingHorizontal: 4,
    borderRadius: 8,
    backgroundColor: colors.text,
    alignItems: 'center',
  },
  busLabelOffline: { backgroundColor: colors.busOfflineText },
  busLabelText: { color: colors.surface, fontSize: 10, fontWeight: '700' },
});
