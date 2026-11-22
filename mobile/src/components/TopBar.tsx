import Ionicons from '@expo/vector-icons/Ionicons';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors, shadow } from '../theme';
import type { ConnectionStatus } from '../types';

interface TopBarProps {
  routeName: string;
  status: ConnectionStatus;
  /** A bus is selected: show a back button instead of the route selector. */
  onBack: (() => void) | null;
  /** Null when there is only one route to choose from. */
  onPickRoute: (() => void) | null;
}

export function TopBar({ routeName, status, onBack, onPickRoute }: TopBarProps) {
  return (
    <View style={styles.row}>
      {onBack ? (
        <Pressable
          onPress={onBack}
          style={[styles.card, styles.iconButton]}
          accessibilityRole="button"
          accessibilityLabel="Volver a todos los buses"
        >
          <Ionicons name="chevron-back" size={22} color={colors.text} />
        </Pressable>
      ) : (
        <Pressable
          onPress={onPickRoute ?? undefined}
          disabled={!onPickRoute}
          style={[styles.card, styles.routeButton]}
          accessibilityRole="button"
          accessibilityLabel={`Ruta ${routeName}${onPickRoute ? ', cambiar de ruta' : ''}`}
        >
          <View style={styles.routeDot} />
          <View style={styles.routeText}>
            <Text style={styles.routeCaption}>Ruta</Text>
            <Text style={styles.routeName} numberOfLines={1}>
              {routeName}
            </Text>
          </View>
          {onPickRoute && <Ionicons name="chevron-down" size={18} color={colors.textMuted} />}
        </Pressable>
      )}
      <View style={styles.spacer} />
      <StatusPill status={status} />
    </View>
  );
}

function StatusPill({ status }: { status: ConnectionStatus }) {
  if (status === 'connected') {
    return (
      <View style={[styles.card, styles.pill]} accessibilityLabel="Conexión en vivo">
        <View style={styles.liveDot} />
        <Text style={[styles.pillText, { color: colors.live }]}>En vivo</Text>
      </View>
    );
  }

  if (status === 'connecting') {
    return (
      <View style={[styles.card, styles.pill]}>
        <Ionicons name="ellipse" size={8} color={colors.textMuted} />
        <Text style={[styles.pillText, { color: colors.textMuted }]}>Conectando</Text>
      </View>
    );
  }

  return (
    <View style={[styles.card, styles.pill, { backgroundColor: colors.warningTint }]}>
      <Ionicons name="refresh" size={16} color={colors.warning} />
      <Text style={[styles.pillText, { color: colors.warning }]}>Reconectando</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  spacer: { flex: 1 },
  card: { minHeight: 48, borderRadius: 14, backgroundColor: colors.surface, ...shadow },
  iconButton: { width: 48, alignItems: 'center', justifyContent: 'center' },
  routeButton: { flexShrink: 1, flexDirection: 'row', alignItems: 'center', gap: 10, paddingHorizontal: 14 },
  routeDot: { width: 12, height: 12, borderRadius: 6, backgroundColor: colors.route },
  routeText: { flexShrink: 1 },
  routeCaption: { fontSize: 11, fontWeight: '500', color: colors.textMuted },
  routeName: { fontSize: 15, fontWeight: '600', color: colors.text },
  pill: { flexDirection: 'row', alignItems: 'center', gap: 6, paddingHorizontal: 12 },
  liveDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.liveDot },
  pillText: { fontSize: 13, fontWeight: '600' },
});
