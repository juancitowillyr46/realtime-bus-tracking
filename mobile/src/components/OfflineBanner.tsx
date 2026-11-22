import Ionicons from '@expo/vector-icons/Ionicons';
import { StyleSheet, Text, View } from 'react-native';

import { colors, shadow } from '../theme';

export function OfflineBanner() {
  return (
    <View style={styles.banner} accessibilityRole="alert" accessibilityLiveRegion="polite">
      <Ionicons name="cloud-offline-outline" size={20} color={colors.warningIcon} />
      <View style={styles.text}>
        <Text style={styles.title}>Sin conexión con el servidor</Text>
        <Text style={styles.body}>Mostramos la última posición conocida. Reintentando…</Text>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
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
  text: { flex: 1, gap: 2 },
  title: { fontSize: 14, fontWeight: '600', color: colors.surface },
  body: { fontSize: 13, color: colors.bannerText },
});
