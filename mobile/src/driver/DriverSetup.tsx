import Ionicons from '@expo/vector-icons/Ionicons';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { ModeSwitch } from '../components/ModeSwitch';
import { busTitle } from '../format';
import { TOP_OFFSET } from '../screens/layout';
import { colors } from '../theme';
import type { BusStatus, RouteDetails } from '../types';

interface DriverSetupProps {
  route: RouteDetails;
  fleet: BusStatus[] | null;
  fleetError: boolean;
  selectedBusId: string | null;
  onSelectBus: (busId: string) => void;
  /** Null when there is only one route. */
  onPickRoute: (() => void) | null;
  onStart: () => void;
  onParentMode: () => void;
}

/** Screen 1 of the mock-up: choose a bus and a route, then start. */
export function DriverSetup({
  route,
  fleet,
  fleetError,
  selectedBusId,
  onSelectBus,
  onPickRoute,
  onStart,
  onParentMode,
}: DriverSetupProps) {
  const routeLengthKm = (route.path.length > 1 ? routeLengthMeters(route) / 1000 : 0).toFixed(1).replace('.', ',');

  return (
    <View style={styles.screen}>
      <ScrollView contentContainerStyle={styles.content}>
        <ModeSwitch mode="driver" onChange={(mode) => mode === 'parent' && onParentMode()} />

        <View style={styles.intro}>
          <Text style={styles.title} accessibilityRole="header">
            Modo conductor
          </Text>
          <Text style={styles.body}>
            Tu iPhone envía su ubicación como si fuera un bus. Los padres lo ven moverse en el mapa.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionLabel}>¿Qué bus conduces?</Text>
          {fleetError && <Text style={styles.error}>No se pudo cargar la lista de buses.</Text>}
          <View style={styles.busGrid}>
            {(fleet ?? []).map((bus) => {
              const selected = bus.busId === selectedBusId;
              return (
                <Pressable
                  key={bus.busId}
                  disabled={bus.inUse}
                  onPress={() => onSelectBus(bus.busId)}
                  style={[styles.busOption, selected && styles.busOptionSelected, bus.inUse && styles.busOptionInUse]}
                  accessibilityRole="button"
                  accessibilityState={{ selected, disabled: bus.inUse }}
                  accessibilityLabel={`${busTitle(bus.busId)}${bus.inUse ? ', en uso' : ''}`}
                >
                  {selected && (
                    <View style={styles.busIcon}>
                      <Ionicons name="bus" size={16} color={colors.text} />
                    </View>
                  )}
                  <Text style={[styles.busName, bus.inUse && styles.mutedText]}>{busTitle(bus.busId)}</Text>
                  {bus.inUse && <Text style={styles.inUse}>En uso</Text>}
                </Pressable>
              );
            })}
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionLabel}>Ruta</Text>
          <Pressable
            onPress={onPickRoute ?? undefined}
            disabled={!onPickRoute}
            style={styles.routeCard}
            accessibilityRole="button"
            accessibilityLabel={`Ruta ${route.name}${onPickRoute ? ', cambiar de ruta' : ''}`}
          >
            <View style={styles.routeDot} />
            <View style={styles.routeText}>
              <Text style={styles.routeName}>{route.name}</Text>
              <Text style={styles.mutedSmall}>
                {route.stops.length} paradas · {routeLengthKm} km
              </Text>
            </View>
            {onPickRoute && <Ionicons name="chevron-down" size={18} color={colors.textMuted} />}
          </Pressable>
        </View>

        <View style={styles.notice}>
          <Ionicons name="location-outline" size={22} color={colors.routeDark} />
          <Text style={styles.noticeText}>
            Usamos tu ubicación <Text style={styles.bold}>solo mientras la app está abierta</Text>. Si la cierras o
            bloqueas el teléfono, el envío se detiene.
          </Text>
        </View>
      </ScrollView>

      <View style={styles.footer}>
        <Pressable
          onPress={onStart}
          disabled={!selectedBusId}
          style={({ pressed }) => [styles.startButton, !selectedBusId && styles.disabled, pressed && styles.pressed]}
          accessibilityRole="button"
          accessibilityState={{ disabled: !selectedBusId }}
        >
          <Ionicons name="play" size={20} color={colors.text} />
          <Text style={styles.startText}>Iniciar recorrido</Text>
        </Pressable>
      </View>
    </View>
  );
}

function routeLengthMeters(route: RouteDetails): number {
  // Haversine sum, only for the "4,4 km" label; the backend owns the real geometry.
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  let total = 0;
  for (let i = 1; i < route.path.length; i++) {
    const a = route.path[i - 1];
    const b = route.path[i];
    const dLat = toRad(b.latitude - a.latitude);
    const dLon = toRad(b.longitude - a.longitude);
    const h = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(a.latitude)) * Math.cos(toRad(b.latitude)) * Math.sin(dLon / 2) ** 2;
    total += 2 * 6_371_000 * Math.asin(Math.sqrt(h));
  }
  return total;
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#F6F7F5' },
  content: { paddingTop: TOP_OFFSET + 4, paddingHorizontal: 20, paddingBottom: 24, gap: 22, maxWidth: 560, width: '100%', alignSelf: 'center' },
  intro: { gap: 6 },
  title: { fontSize: 26, fontWeight: '700', color: colors.text },
  body: { fontSize: 15, lineHeight: 21, color: colors.textMuted },
  section: { gap: 10 },
  sectionLabel: { fontSize: 13, fontWeight: '600', color: '#3A3E45' },
  error: { fontSize: 13, color: colors.error },
  busGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  busOption: {
    flexBasis: '30%',
    flexGrow: 1,
    minHeight: 76,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 4,
  },
  busOptionSelected: { borderWidth: 2, borderColor: colors.text, backgroundColor: '#FFF4D6' },
  busOptionInUse: { opacity: 0.7 },
  busIcon: {
    width: 28,
    height: 28,
    borderRadius: 14,
    backgroundColor: colors.bus,
    alignItems: 'center',
    justifyContent: 'center',
  },
  busName: { fontSize: 15, fontWeight: '700', color: colors.text },
  mutedText: { color: colors.textMuted },
  inUse: { fontSize: 11, color: colors.textMuted },
  routeCard: {
    minHeight: 60,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
    backgroundColor: colors.surface,
  },
  routeDot: { width: 12, height: 12, borderRadius: 6, backgroundColor: colors.route },
  routeText: { flex: 1, gap: 2 },
  routeName: { fontSize: 15, fontWeight: '600', color: colors.text },
  mutedSmall: { fontSize: 12, color: colors.textMuted },
  notice: { flexDirection: 'row', gap: 12, padding: 14, borderRadius: 14, backgroundColor: colors.routeTint },
  noticeText: { flex: 1, fontSize: 13, lineHeight: 19, color: '#23364F' },
  bold: { fontWeight: '700' },
  footer: { paddingHorizontal: 20, paddingBottom: 28, paddingTop: 8, maxWidth: 560, width: '100%', alignSelf: 'center' },
  startButton: {
    minHeight: 58,
    borderRadius: 16,
    backgroundColor: colors.bus,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
  },
  startText: { fontSize: 17, fontWeight: '700', color: colors.text },
  disabled: { opacity: 0.5 },
  pressed: { opacity: 0.85 },
});
