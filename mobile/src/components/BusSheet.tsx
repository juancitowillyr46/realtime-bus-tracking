import Ionicons from '@expo/vector-icons/Ionicons';
import type { ComponentProps } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { secondsSince } from '../busStore';
import { busNumber, busTitle, formatAge, headingLabel, stopShortName } from '../format';
import { colors } from '../theme';
import type { BusPosition, RouteDetails } from '../types';
import { useNow } from '../useNow';

/** Height of the panel, so the map can keep the route visible above it. */
export const SHEET_HEIGHT = 330;

interface BusSheetProps {
  route: RouteDetails;
  buses: BusPosition[];
  selected: BusPosition | null;
  offline: boolean;
  onSelectBus: (busId: string) => void;
}

export function BusSheet({ route, buses, selected, offline, onSelectBus }: BusSheetProps) {
  const now = useNow();

  return (
    <View style={styles.sheet}>
      <View style={styles.handle} />
      {selected ? (
        <BusDetail bus={selected} routeName={route.name} offline={offline} now={now} />
      ) : (
        <BusList route={route} buses={buses} offline={offline} now={now} onSelectBus={onSelectBus} />
      )}
    </View>
  );
}

function BusList({
  route,
  buses,
  offline,
  now,
  onSelectBus,
}: {
  route: RouteDetails;
  buses: BusPosition[];
  offline: boolean;
  now: number;
  onSelectBus: (busId: string) => void;
}) {
  const title =
    buses.length === 0 ? 'Sin buses en ruta' : buses.length === 1 ? '1 bus en ruta' : `${buses.length} buses en ruta`;

  return (
    <>
      <View style={styles.header}>
        <Text style={styles.title} accessibilityRole="header">
          {title}
        </Text>
        <Text style={styles.muted}>{route.stops.length} paradas</Text>
      </View>

      <ScrollView contentContainerStyle={styles.list}>
        {buses.length === 0 && (
          <Text style={styles.muted}>Cuando un bus empiece su recorrido, aparecerá aquí y en el mapa.</Text>
        )}
        {buses.map((bus) => (
          <Pressable
            key={bus.busId}
            onPress={() => onSelectBus(bus.busId)}
            style={({ pressed }) => [styles.row, pressed && styles.rowPressed]}
            accessibilityRole="button"
            accessibilityLabel={`${busTitle(bus.busId)}, ver detalle`}
          >
            <BusBadge busId={bus.busId} offline={offline} size={40} />
            <View style={styles.rowText}>
              <Text style={styles.rowTitle}>{busTitle(bus.busId)}</Text>
              <Text style={styles.muted} numberOfLines={1}>
                {bus.nextStopName ? `Rumbo a ${stopShortName(bus.nextStopName)}` : 'Sin próxima parada'} ·{' '}
                {Math.round(bus.speedKmh)} km/h
              </Text>
            </View>
            <Text style={[styles.age, offline && styles.ageStale]}>{formatAge(secondsSince(bus, now))}</Text>
          </Pressable>
        ))}
        {offline && buses.length > 0 && (
          <Text style={styles.footnote}>
            Los buses en gris muestran su última posición conocida. Se actualizan solos al volver la conexión.
          </Text>
        )}
      </ScrollView>

      {!offline && <Legend />}
    </>
  );
}

function BusDetail({
  bus,
  routeName,
  offline,
  now,
}: {
  bus: BusPosition;
  routeName: string;
  offline: boolean;
  now: number;
}) {
  return (
    <View style={styles.detail}>
      <View style={styles.detailHeader}>
        <BusBadge busId={bus.busId} offline={offline} size={48} />
        <View>
          <Text style={styles.title} accessibilityRole="header">
            {busTitle(bus.busId)}
          </Text>
          <Text style={styles.muted}>{routeName}</Text>
        </View>
      </View>

      <View style={styles.nextStop}>
        <Ionicons name="location-outline" size={22} color={colors.routeDark} />
        <View>
          <Text style={styles.nextStopCaption}>Próxima parada</Text>
          <Text style={styles.nextStopName}>{bus.nextStopName ? stopShortName(bus.nextStopName) : '—'}</Text>
        </View>
      </View>

      <View style={styles.stats}>
        <Stat icon="speedometer-outline" label="Velocidad" value={`${Math.round(bus.speedKmh)} km/h`} />
        <Stat icon="compass-outline" label="Rumbo" value={headingLabel(bus.heading)} />
        <Stat
          icon="time-outline"
          label="Actualizado"
          value={formatAge(secondsSince(bus, now))}
          warning={offline}
        />
      </View>

      <Text style={styles.footnote}>
        {offline ? 'Sin conexión: es la última posición conocida.' : 'La posición se actualiza cada 5 segundos.'}
      </Text>
    </View>
  );
}

function Stat({
  icon,
  label,
  value,
  warning = false,
}: {
  icon: ComponentProps<typeof Ionicons>['name'];
  label: string;
  value: string;
  warning?: boolean;
}) {
  return (
    <View style={styles.stat}>
      <View style={styles.statLabel}>
        <Ionicons name={icon} size={14} color={colors.textMuted} />
        <Text style={styles.statLabelText}>{label}</Text>
      </View>
      <Text style={[styles.statValue, warning && styles.ageStale]}>{value}</Text>
    </View>
  );
}

function BusBadge({ busId, offline, size }: { busId: string; offline: boolean; size: number }) {
  return (
    <View
      style={[
        styles.badge,
        { width: size, height: size, borderRadius: size / 2 },
        offline && { backgroundColor: colors.busOffline },
      ]}
    >
      <Ionicons name="bus" size={size * 0.42} color={offline ? colors.busOfflineText : colors.text} />
      <Text style={[styles.badgeNumber, offline && { color: colors.busOfflineText }]}>{busNumber(busId)}</Text>
    </View>
  );
}

function Legend() {
  return (
    <View style={styles.legend}>
      <View style={styles.legendItem}>
        <View style={[styles.legendDot, { backgroundColor: colors.bus }]} />
        <Text style={styles.legendText}>Bus</Text>
      </View>
      <View style={styles.legendItem}>
        <View style={styles.legendStop} />
        <Text style={styles.legendText}>Parada</Text>
      </View>
      <View style={styles.legendItem}>
        <View style={styles.legendLine} />
        <Text style={styles.legendText}>Recorrido</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  sheet: {
    width: '100%',
    maxWidth: 560,
    height: SHEET_HEIGHT,
    backgroundColor: colors.surface,
    borderTopLeftRadius: 22,
    borderTopRightRadius: 22,
    paddingTop: 10,
    paddingHorizontal: 20,
    paddingBottom: 24,
    gap: 14,
    shadowColor: '#14181E',
    shadowOpacity: 0.1,
    shadowRadius: 20,
    shadowOffset: { width: 0, height: -4 },
    elevation: 8,
  },
  handle: { alignSelf: 'center', width: 40, height: 5, borderRadius: 3, backgroundColor: colors.handle },
  header: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between' },
  title: { fontSize: 20, fontWeight: '700', color: colors.text },
  muted: { fontSize: 13, color: colors.textMuted },
  list: { gap: 10 },
  row: {
    minHeight: 64,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 10,
    paddingHorizontal: 12,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 14,
  },
  rowPressed: { backgroundColor: colors.routeTint },
  rowText: { flex: 1, gap: 2 },
  rowTitle: { fontSize: 15, fontWeight: '600', color: colors.text },
  age: { fontSize: 12, color: colors.textMuted },
  ageStale: { color: colors.warning, fontWeight: '600' },
  footnote: { fontSize: 12, color: colors.textMuted },
  badge: { backgroundColor: colors.bus, alignItems: 'center', justifyContent: 'center' },
  badgeNumber: { fontSize: 10, fontWeight: '700', color: colors.text, marginTop: -1 },
  detail: { gap: 16 },
  detailHeader: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  nextStop: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    padding: 14,
    borderRadius: 14,
    backgroundColor: colors.routeTint,
  },
  nextStopCaption: { fontSize: 12, fontWeight: '500', color: '#3D4A5C' },
  nextStopName: { fontSize: 16, fontWeight: '600', color: colors.routeDark },
  stats: { flexDirection: 'row', gap: 10 },
  stat: { flex: 1, gap: 4, padding: 12, borderWidth: 1, borderColor: colors.border, borderRadius: 12 },
  statLabel: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  statLabelText: { fontSize: 12, color: colors.textMuted },
  statValue: { fontSize: 16, fontWeight: '700', color: colors.text },
  legend: { flexDirection: 'row', gap: 16 },
  legendItem: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  legendDot: { width: 12, height: 12, borderRadius: 6 },
  legendStop: {
    width: 12,
    height: 12,
    borderRadius: 6,
    borderWidth: 3,
    borderColor: colors.route,
    backgroundColor: colors.surface,
  },
  legendLine: { width: 18, height: 4, borderRadius: 2, backgroundColor: colors.route },
  legendText: { fontSize: 12, color: colors.textMuted },
});
