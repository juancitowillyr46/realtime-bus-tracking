import Ionicons from '@expo/vector-icons/Ionicons';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { colors } from '../theme';

export type AppMode = 'parent' | 'driver';

const options: { mode: AppMode; label: string; icon: 'map-outline' | 'bus-outline' }[] = [
  { mode: 'parent', label: 'Padre', icon: 'map-outline' },
  { mode: 'driver', label: 'Conductor', icon: 'bus-outline' },
];

/** Segmented control between the parent map and the driver mode. */
export function ModeSwitch({ mode, onChange }: { mode: AppMode; onChange: (mode: AppMode) => void }) {
  return (
    <View style={styles.track} accessibilityRole="tablist" accessibilityLabel="Modo de la app">
      {options.map((option) => {
        const selected = option.mode === mode;
        return (
          <Pressable
            key={option.mode}
            onPress={() => onChange(option.mode)}
            style={[styles.option, selected && styles.optionSelected]}
            accessibilityRole="tab"
            accessibilityState={{ selected }}
          >
            <Ionicons name={option.icon} size={18} color={selected ? colors.text : colors.textMuted} />
            <Text style={[styles.label, selected && styles.labelSelected]}>{option.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  track: { flexDirection: 'row', gap: 4, padding: 4, borderRadius: 14, backgroundColor: '#E8EAE6' },
  option: {
    flex: 1,
    minHeight: 44,
    borderRadius: 11,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
  },
  optionSelected: {
    backgroundColor: colors.surface,
    shadowColor: '#14181E',
    shadowOpacity: 0.12,
    shadowRadius: 4,
    shadowOffset: { width: 0, height: 1 },
    elevation: 2,
  },
  label: { fontSize: 14, fontWeight: '600', color: colors.textMuted },
  labelSelected: { color: colors.text },
});
