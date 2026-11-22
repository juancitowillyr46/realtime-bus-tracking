import Ionicons from '@expo/vector-icons/Ionicons';
import { Linking, Platform, Pressable, StyleSheet, Text, View } from 'react-native';

import { TOP_OFFSET } from '../screens/layout';
import { colors } from '../theme';

/** Screen 4 of the mock-up: the location permission was denied. */
export function PermissionNeeded({ onBack, onParentMode }: { onBack: () => void; onParentMode: () => void }) {
  return (
    <View style={styles.screen}>
      <Pressable onPress={onBack} style={styles.back} accessibilityRole="button" accessibilityLabel="Volver">
        <Ionicons name="chevron-back" size={22} color={colors.text} />
      </Pressable>

      <View style={styles.center}>
        <View style={styles.iconCircle}>
          <Ionicons name="location-outline" size={40} color={colors.warning} />
        </View>
        <Text style={styles.title} accessibilityRole="header">
          Necesitamos tu ubicación
        </Text>
        <Text style={styles.body}>
          Sin permiso de ubicación no podemos mostrar el bus en el mapa. Solo la usamos mientras la app está abierta y
          en modo conductor.
        </Text>

        {Platform.OS !== 'web' && (
          <View style={styles.steps}>
            <Text style={styles.stepsTitle}>Cómo activarla en el iPhone</Text>
            <Text style={styles.step}>1. Abre Ajustes.</Text>
            <Text style={styles.step}>2. Entra a Expo Go → Ubicación.</Text>
            <Text style={styles.step}>3. Elige «Mientras se usa la app».</Text>
          </View>
        )}
      </View>

      {Platform.OS !== 'web' && (
        <Pressable
          onPress={() => void Linking.openSettings()}
          style={({ pressed }) => [styles.primary, pressed && styles.pressed]}
          accessibilityRole="button"
        >
          <Ionicons name="settings-outline" size={20} color={colors.surface} />
          <Text style={styles.primaryText}>Abrir Ajustes</Text>
        </Pressable>
      )}
      <Pressable onPress={onParentMode} style={styles.secondary} accessibilityRole="button">
        <Text style={styles.secondaryText}>Volver al modo padre</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: '#F6F7F5',
    paddingTop: TOP_OFFSET,
    paddingHorizontal: 24,
    paddingBottom: 28,
    gap: 12,
  },
  back: {
    width: 48,
    height: 48,
    borderRadius: 14,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  center: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 18 },
  iconCircle: {
    width: 88,
    height: 88,
    borderRadius: 44,
    backgroundColor: colors.warningTint,
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: { fontSize: 24, fontWeight: '700', color: colors.text, textAlign: 'center' },
  body: { fontSize: 15, lineHeight: 22, color: colors.textMuted, textAlign: 'center', maxWidth: 320 },
  steps: {
    alignSelf: 'stretch',
    gap: 6,
    padding: 16,
    borderRadius: 14,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  stepsTitle: { fontSize: 13, fontWeight: '600', color: '#3A3E45', marginBottom: 2 },
  step: { fontSize: 14, lineHeight: 20, color: '#3A3E45' },
  primary: {
    minHeight: 56,
    borderRadius: 16,
    backgroundColor: colors.route,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
  },
  primaryText: { fontSize: 17, fontWeight: '700', color: colors.surface },
  pressed: { opacity: 0.85 },
  secondary: { minHeight: 48, alignItems: 'center', justifyContent: 'center' },
  secondaryText: { fontSize: 15, fontWeight: '600', color: colors.route },
});
