import Ionicons from '@expo/vector-icons/Ionicons';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { colors } from '../theme';

/** Full-screen spinner, or the error message when loading failed. */
export function LoadingState({ error, message = 'Cargando ruta…' }: { error: string | null; message?: string }) {
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
          <Text style={styles.message}>{message}</Text>
        </>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 12,
    padding: 32,
    backgroundColor: colors.surface,
  },
  message: { fontSize: 15, color: colors.textMuted, textAlign: 'center' },
});
