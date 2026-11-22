import Ionicons from '@expo/vector-icons/Ionicons';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { sortedBuses } from '../busStore';
import { BusSheet, SHEET_HEIGHT } from '../components/BusSheet';
import { LoadingState } from '../components/LoadingState';
import LiveMap from '../components/LiveMap';
import { OfflineBanner } from '../components/OfflineBanner';
import { RoutePicker } from '../components/RoutePicker';
import { TopBar } from '../components/TopBar';
import { colors } from '../theme';
import { useLiveRoute } from '../useLiveRoute';
import { BANNER_HEIGHT, layout, TOP_BAR_HEIGHT, TOP_OFFSET } from './layout';

/** The live map of phase 2: what a parent sees. */
export function ParentScreen({ onDriverMode }: { onDriverMode: () => void }) {
  const [routeId, setRouteId] = useState<string | null>(null);
  const [selectedBusId, setSelectedBusId] = useState<string | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [recenterKey, setRecenterKey] = useState(0);

  const { routes, route, buses, status, error } = useLiveRoute(routeId);
  const offline = status === 'reconnecting' || status === 'disconnected';
  const busList = sortedBuses(buses);
  const selected = busList.find((bus) => bus.busId === selectedBusId) ?? null;

  if (!route) {
    return <LoadingState error={error} />;
  }

  const mapInsets = {
    top: TOP_OFFSET + TOP_BAR_HEIGHT + (offline ? BANNER_HEIGHT : 0),
    bottom: SHEET_HEIGHT,
  };

  return (
    <View style={layout.screen}>
      <LiveMap
        route={route}
        buses={busList}
        selectedBusId={selected?.busId ?? null}
        offline={offline}
        onSelectBus={setSelectedBusId}
        insets={mapInsets}
        recenterKey={recenterKey}
      />

      <View style={layout.top} pointerEvents="box-none">
        <TopBar
          routeName={route.name}
          status={status}
          onBack={selected ? () => setSelectedBusId(null) : null}
          onPickRoute={routes.length > 1 ? () => setPickerOpen(true) : null}
        />
        {offline && <OfflineBanner />}
      </View>

      <Pressable
        onPress={onDriverMode}
        style={[layout.floatingButton, styles.driverButton, { bottom: SHEET_HEIGHT + 16 }]}
        accessibilityRole="button"
        accessibilityLabel="Cambiar a modo conductor"
      >
        <Ionicons name="bus-outline" size={20} color={colors.text} />
        <Text style={styles.driverButtonText}>Modo conductor</Text>
      </Pressable>

      <Pressable
        onPress={() => {
          setSelectedBusId(null);
          setRecenterKey((key) => key + 1);
        }}
        style={[layout.floatingButton, { right: 16, bottom: SHEET_HEIGHT + 16 }]}
        accessibilityRole="button"
        accessibilityLabel="Centrar la ruta"
      >
        <Ionicons name="locate-outline" size={22} color={colors.text} />
      </Pressable>

      <View style={layout.bottom}>
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
    </View>
  );
}

const styles = StyleSheet.create({
  driverButton: { left: 16, flexDirection: 'row', gap: 8, paddingHorizontal: 14 },
  driverButtonText: { fontSize: 14, fontWeight: '600', color: colors.text },
});
