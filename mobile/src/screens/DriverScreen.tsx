import Ionicons from '@expo/vector-icons/Ionicons';
import { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';

import { getBuses } from '../api';
import LiveMap from '../components/LiveMap';
import { LoadingState } from '../components/LoadingState';
import { RoutePicker } from '../components/RoutePicker';
import { DRIVER_PANEL_HEIGHT, DriverPanel } from '../driver/DriverPanel';
import { DriverSetup } from '../driver/DriverSetup';
import { PermissionNeeded } from '../driver/PermissionNeeded';
import { useDriver, type DriverStatus } from '../driver/useDriver';
import { busTitle, formatDistance } from '../format';
import { colors, shadow } from '../theme';
import type { BusStatus } from '../types';
import { useLiveRoute } from '../useLiveRoute';
import { BANNER_HEIGHT, layout, TOP_BAR_HEIGHT, TOP_OFFSET } from './layout';

/** Same threshold as RouteDefinition.OffRouteThresholdMeters in the backend. */
const OFF_ROUTE_METERS = 150;

const FLEET_REFRESH_MS = 10_000;

/** The driver mode: the phone becomes a bus (docs/handoffs/phase-3.md). */
export function DriverScreen({ onParentMode }: { onParentMode: () => void }) {
  const [routeId, setRouteId] = useState<string | null>(null);
  const [chosenBusId, setChosenBusId] = useState<string | null>(null);
  const [trip, setTrip] = useState<{ busId: string; startedAt: number } | null>(null);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [fleet, setFleet] = useState<BusStatus[] | null>(null);
  const [fleetError, setFleetError] = useState(false);

  const { routes, route, buses, status, error } = useLiveRoute(routeId);
  const driver = useDriver(trip?.busId ?? '', route?.routeId ?? '', trip !== null && route !== null);

  // Refresh which buses are in use while choosing one (not while driving).
  useEffect(() => {
    if (trip) {
      return;
    }
    let cancelled = false;
    const load = () =>
      getBuses()
        .then((buses) => {
          if (!cancelled) {
            setFleet(buses);
            setFleetError(false);
          }
        })
        .catch(() => !cancelled && setFleetError(true));
    void load();
    const timer = setInterval(() => void load(), FLEET_REFRESH_MS);
    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [trip]);

  if (!route) {
    return <LoadingState error={error} />;
  }

  const freeBuses = (fleet ?? []).filter((bus) => !bus.inUse);
  const selectedBusId = freeBuses.some((bus) => bus.busId === chosenBusId) ? chosenBusId : (freeBuses[0]?.busId ?? null);

  const routePicker = (
    <RoutePicker
      visible={pickerOpen}
      routes={routes}
      selectedRouteId={route.routeId}
      onSelect={(id) => {
        setRouteId(id);
        setPickerOpen(false);
      }}
      onClose={() => setPickerOpen(false)}
    />
  );

  if (!trip) {
    return (
      <>
        <DriverSetup
          route={route}
          fleet={fleet}
          fleetError={fleetError}
          selectedBusId={selectedBusId}
          onSelectBus={setChosenBusId}
          onPickRoute={routes.length > 1 ? () => setPickerOpen(true) : null}
          onStart={() => selectedBusId && setTrip({ busId: selectedBusId, startedAt: Date.now() })}
          onParentMode={onParentMode}
        />
        {routePicker}
      </>
    );
  }

  if (driver.status === 'permissionDenied') {
    return <PermissionNeeded onBack={() => setTrip(null)} onParentMode={onParentMode} />;
  }

  // The driver's own bus as the server echoes it back: the same path parents see.
  const ownPosition = buses[trip.busId] ?? null;
  const offRouteMeters =
    ownPosition?.distanceFromRouteMeters != null && ownPosition.distanceFromRouteMeters > OFF_ROUTE_METERS
      ? ownPosition.distanceFromRouteMeters
      : null;
  const offline = status === 'reconnecting' || status === 'disconnected';

  return (
    <View style={layout.screen}>
      <LiveMap
        route={route}
        buses={ownPosition ? [ownPosition] : []}
        selectedBusId={trip.busId}
        offline={offline}
        onSelectBus={() => undefined}
        insets={{
          top: TOP_OFFSET + TOP_BAR_HEIGHT + (offRouteMeters !== null ? BANNER_HEIGHT : 0),
          bottom: DRIVER_PANEL_HEIGHT,
        }}
        recenterKey={0}
      />

      <View style={layout.top} pointerEvents="box-none">
        <View style={styles.row}>
          <View style={[styles.card, styles.tripCard]}>
            <View style={styles.routeDot} />
            <View style={styles.tripText}>
              <Text style={styles.caption}>Conduces el {busTitle(trip.busId)}</Text>
              <Text style={styles.routeName} numberOfLines={1}>
                {route.name}
              </Text>
            </View>
          </View>
          <DriverPill status={driver.status} />
        </View>

        {offRouteMeters !== null && (
          <View style={styles.banner} accessibilityRole="alert">
            <Ionicons name="map-outline" size={20} color={colors.warningIcon} />
            <View style={styles.bannerText}>
              <Text style={styles.bannerTitle}>Estás fuera de la ruta ({formatDistance(offRouteMeters)})</Text>
              <Text style={styles.bannerBody}>Seguimos enviando tu ubicación, pero sin próxima parada.</Text>
            </View>
          </View>
        )}
      </View>

      <View style={layout.bottom}>
        <DriverPanel
          driver={driver}
          ownPosition={ownPosition}
          startedAt={trip.startedAt}
          onStop={() => setTrip(null)}
        />
      </View>
    </View>
  );
}

function DriverPill({ status }: { status: DriverStatus }) {
  if (status === 'sending') {
    return (
      <View style={[styles.card, styles.pill]}>
        <View style={styles.liveDot} />
        <Text style={[styles.pillText, { color: colors.live }]}>Enviando</Text>
      </View>
    );
  }
  if (status === 'weakSignal' || status === 'sendFailed') {
    return (
      <View style={[styles.card, styles.pill, { backgroundColor: colors.warningTint }]}>
        <Ionicons name="warning-outline" size={16} color={colors.warning} />
        <Text style={[styles.pillText, { color: colors.warning }]}>Revisa</Text>
      </View>
    );
  }
  return (
    <View style={[styles.card, styles.pill]}>
      <Ionicons name="locate-outline" size={16} color={colors.textMuted} />
      <Text style={[styles.pillText, { color: colors.textMuted }]}>GPS</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  card: { minHeight: 48, borderRadius: 14, backgroundColor: colors.surface, ...shadow },
  tripCard: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 14 },
  routeDot: { width: 12, height: 12, borderRadius: 6, backgroundColor: colors.route },
  tripText: { flexShrink: 1 },
  caption: { fontSize: 11, fontWeight: '500', color: colors.textMuted },
  routeName: { fontSize: 15, fontWeight: '600', color: colors.text },
  pill: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingHorizontal: 12 },
  liveDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.liveDot },
  pillText: { fontSize: 13, fontWeight: '600' },
  banner: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 10,
    paddingVertical: 12,
    paddingHorizontal: 14,
    borderRadius: 14,
    backgroundColor: colors.bannerBg,
    ...shadow,
  },
  bannerText: { flex: 1, gap: 2 },
  bannerTitle: { fontSize: 14, fontWeight: '600', color: colors.surface },
  bannerBody: { fontSize: 13, color: colors.bannerText },
});
