import Ionicons from '@expo/vector-icons/Ionicons';
import { StatusBar } from 'expo-status-bar';
import { useState } from 'react';
import { ActivityIndicator, Platform, Pressable, StyleSheet, Text, View } from 'react-native';

import { sortedBuses } from './src/busStore';
import { BusSheet, SHEET_HEIGHT } from './src/components/BusSheet';
import LiveMap from './src/components/LiveMap';
import { OfflineBanner } from './src/components/OfflineBanner';
import { RoutePicker } from './src/components/RoutePicker';
import { TopBar } from './src/components/TopBar';
import { colors, shadow } from './src/theme';
import { useLiveRoute } from './src/useLiveRoute';

// Below the iPhone's status bar and notch; the browser has neither. A fixed value keeps the
// app free of extra packages; the iPhone X status bar is 44 pt tall.
const TOP_OFFSET = Platform.OS === 'web' ? 16 : 52;
const TOP_BAR_HEIGHT = 48;
const BANNER_HEIGHT = 72;

export default function App() {
  const [routeId, setRouteId] = useState<string | null>(null);
  const [selectedBusId, setSelectedBusId] = useState<string | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [recenterKey, setRecenterKey] = useState(0);

  const { routes, route, buses, status, error } = useLiveRoute(routeId);
  const offline = status === 'reconnecting' || status === 'disconnected';
  const busList = sortedBuses(buses);
  const selected = busList.find((bus) => bus.busId === selectedBusId) ?? null;

  if (!route) {
    return (
      <View style={styles.centered}>
        {error ? (
          <>
            <Ionicons name="cloud-offline-outline" size={40} color={colors.textMuted} />
            <Text style={styles.message}>{error}</Text>
          </>
        ) : (
          <>
            <ActivityIndicator size="large" color={colors.route} />
            <Text style={styles.message}>Cargando ruta…</Text>
          </>
        )}
        <StatusBar style="dark" />
      </View>
    );
  }

  const mapInsets = {
    top: TOP_OFFSET + TOP_BAR_HEIGHT + (offline ? BANNER_HEIGHT : 0),
    bottom: SHEET_HEIGHT,
  };

  return (
    <View style={styles.screen}>
      <LiveMap
        route={route}
        buses={busList}
        selectedBusId={selected?.busId ?? null}
        offline={offline}
        onSelectBus={setSelectedBusId}
        insets={mapInsets}
        recenterKey={recenterKey}
      />

      <View style={[styles.top, { top: TOP_OFFSET }]} pointerEvents="box-none">
        <TopBar
          routeName={route.name}
          status={status}
          onBack={selected ? () => setSelectedBusId(null) : null}
          onPickRoute={routes.length > 1 ? () => setPickerOpen(true) : null}
        />
        {offline && <OfflineBanner />}
      </View>

      <Pressable
        onPress={() => {
          setSelectedBusId(null);
          setRecenterKey((key) => key + 1);
        }}
        style={[styles.recenter, { bottom: SHEET_HEIGHT + 16 }]}
        accessibilityRole="button"
        accessibilityLabel="Centrar la ruta"
      >
        <Ionicons name="locate-outline" size={22} color={colors.text} />
      </Pressable>

      <View style={styles.bottom}>
        <BusSheet
          route={route}
          buses={busList}
          selected={selected}
          offline={offline}
          onSelectBus={setSelectedBusId}
        />
      </View>

      <RoutePicker
        visible={pickerOpen}
        routes={routes}
        selectedRouteId={route.routeId}
        onSelect={(id) => {
          setRouteId(id);
          setSelectedBusId(null);
          setPickerOpen(false);
        }}
        onClose={() => setPickerOpen(false)}
      />

      <StatusBar style="dark" />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.mapGround },
  centered: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 12, padding: 32, backgroundColor: colors.surface },
  message: { fontSize: 15, color: colors.textMuted, textAlign: 'center' },
  // zIndex: on web, Leaflet's panes use z-index 400-1000 and would cover these overlays.
  top: { position: 'absolute', left: 16, right: 16, gap: 8, zIndex: 1100 },
  recenter: {
    position: 'absolute',
    zIndex: 1100,
    right: 16,
    width: 48,
    height: 48,
    borderRadius: 14,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    ...shadow,
  },
  // On a wide browser window the panel keeps a phone-like width instead of stretching.
  bottom: { position: 'absolute', left: 0, right: 0, bottom: 0, alignItems: 'center', zIndex: 1100 },
});
