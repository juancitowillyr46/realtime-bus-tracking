import Ionicons from '@expo/vector-icons/Ionicons';
import { Modal, Pressable, StyleSheet, Text, View } from 'react-native';

import { colors } from '../theme';
import type { RouteSummary } from '../types';

interface RoutePickerProps {
  visible: boolean;
  routes: RouteSummary[];
  selectedRouteId: string | null;
  onSelect: (routeId: string) => void;
  onClose: () => void;
}

/** A simple list of routes. Only reachable when there is more than one route. */
export function RoutePicker({ visible, routes, selectedRouteId, onSelect, onClose }: RoutePickerProps) {
  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose} accessibilityLabel="Cerrar">
        <View style={styles.card}>
          <Text style={styles.title} accessibilityRole="header">
            Elige una ruta
          </Text>
          {routes.map((route) => {
            const selected = route.routeId === selectedRouteId;
            return (
              <Pressable
                key={route.routeId}
                onPress={() => onSelect(route.routeId)}
                style={[styles.row, selected && styles.rowSelected]}
                accessibilityRole="button"
                accessibilityState={{ selected }}
              >
                <Text style={styles.rowText}>{route.name}</Text>
                {selected && <Ionicons name="checkmark" size={20} color={colors.route} />}
              </Pressable>
            );
          })}
        </View>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: { flex: 1, backgroundColor: 'rgba(20,24,30,0.4)', justifyContent: 'center', padding: 24 },
  card: { backgroundColor: colors.surface, borderRadius: 18, padding: 16, gap: 8 },
  title: { fontSize: 18, fontWeight: '700', color: colors.text, marginBottom: 4 },
  row: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 12,
    borderRadius: 12,
  },
  rowSelected: { backgroundColor: colors.routeTint },
  rowText: { fontSize: 15, color: colors.text },
});
