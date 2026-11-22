import { StatusBar } from 'expo-status-bar';
import { useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

import { secondsSince, sortedBuses } from './src/busStore';
import type { ConnectionStatus } from './src/types';
import { useLiveRoute } from './src/useLiveRoute';

const statusLabels: Record<ConnectionStatus, string> = {
  connecting: 'Conectando…',
  connected: 'En vivo',
  reconnecting: 'Reconectando…',
  disconnected: 'Sin conexión',
};

// Temporary screen for step 6: proves the routes API and SignalR work on the phone.
// Step 7 replaces it with the map from the approved mock-up.
export default function App() {
  const { route, buses, status, error } = useLiveRoute(null);
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  return (
    <View style={styles.screen}>
      <ScrollView contentContainerStyle={styles.content}>
        <Text style={styles.title}>{route?.name ?? 'Cargando ruta…'}</Text>
        <Text style={styles.status}>{statusLabels[status]}</Text>
        {error && <Text style={styles.error}>{error}</Text>}

        {sortedBuses(buses).map((bus) => (
          <View key={bus.busId} style={styles.bus}>
            <Text style={styles.busName}>{bus.busId}</Text>
            <Text style={styles.detail}>
              Rumbo a {bus.nextStopName ?? '—'} · {Math.round(bus.speedKmh)} km/h
            </Text>
            <Text style={styles.detail}>
              {bus.latitude.toFixed(5)}, {bus.longitude.toFixed(5)} · hace {secondsSince(bus, now)} s
            </Text>
          </View>
        ))}
      </ScrollView>
      <StatusBar style="dark" />
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#FFFFFF' },
  content: { padding: 20, paddingTop: 64, gap: 12 },
  title: { fontSize: 22, fontWeight: '700', color: '#1B1D21' },
  status: { fontSize: 15, color: '#1E7B4B', fontWeight: '600' },
  error: { fontSize: 15, color: '#B42318' },
  bus: { padding: 14, borderRadius: 14, borderWidth: 1, borderColor: '#E6E8EC', gap: 4 },
  busName: { fontSize: 16, fontWeight: '600', color: '#1B1D21' },
  detail: { fontSize: 14, color: '#5B5F66' },
});
