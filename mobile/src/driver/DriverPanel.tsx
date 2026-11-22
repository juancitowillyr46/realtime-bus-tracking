import Ionicons from '@expo/vector-icons/Ionicons';
import type { ComponentProps } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { formatAccuracy, formatAge, stopShortName } from '../format';
import { colors } from '../theme';
import type { BusPosition } from '../types';
import { useNow } from '../useNow';
import { MAX_ACCURACY_METERS, toKmh } from './report';
import type { DriverState } from './useDriver';

/** Height of the panel, so the map keeps the bus visible above it. */
export const DRIVER_PANEL_HEIGHT = 440;

type IconName = ComponentProps<typeof Ionicons>['name'];

const headlines: Record<Exclude<DriverState['status'], 'idle' | 'permissionDenied'>, { title: string; icon: IconName; tone: 'ok' | 'muted' | 'warning' }> = {
  starting: { title: 'Buscando señal GPS…', icon: 'locate-outline', tone: 'muted' },
  sending: { title: 'Enviando tu ubicación', icon: 'radio-outline', tone: 'ok' },
  weakSignal: { title: 'Señal GPS débil', icon: 'warning-outline', tone: 'warning' },
  sendFailed: { title: 'No se pudo enviar', icon: 'cloud-offline-outline', tone: 'warning' },
};

interface DriverPanelProps {
  driver: DriverState;
  /** The driver's own bus as the server echoes it back (next stop), null until the first echo. */
  ownPosition: BusPosition | null;
  startedAt: number;
  onStop: () => void;
}

/** Screens 2 and 3 of the mock-up: status, GPS details, next stop and the stop button. */
export function DriverPanel({ driver, ownPosition, startedAt, onStop }: DriverPanelProps) {
  const now = useNow();
  const status = driver.status === 'idle' || driver.status === 'permissionDenied' ? 'starting' : driver.status;
  const headline = headlines[status];
  const reading = driver.lastReading;
  const weak = status === 'weakSignal';
  const lastSentSeconds = driver.lastSentAt === null ? null : Math.max(0, Math.floor((now - driver.lastSentAt) / 1000));
  const lastSentStale = lastSentSeconds !== null && lastSentSeconds > 15;

  return (
    <View style={styles.panel}>
      <View style={styles.handle} />

      <View style={styles.header} accessibilityLiveRegion="polite">
        <View style={[styles.headerIcon, toneBackground[headline.tone]]}>
          <Ionicons name={headline.icon} size={22} color={toneColor[headline.tone]} />
        </View>
        <View style={styles.headerText}>
          <Text style={styles.title} accessibilityRole="header">
            {headline.title}
          </Text>
          <Text style={styles.muted}>{subtitle(status, startedAt, now)}</Text>
        </View>
      </View>

      <View style={styles.stats}>
        <Stat label="Precisión GPS" icon="locate-outline" value={formatAccuracy(reading?.accuracy ?? null)} warning={weak} />
        <Stat
          label="Último envío"
          icon="time-outline"
          value={lastSentSeconds === null ? '—' : formatAge(lastSentSeconds)}
          warning={lastSentStale}
        />
        <Stat label="Velocidad" icon="speedometer-outline" value={reading ? `${Math.round(toKmh(reading.speed))} km/h` : '—'} />
        <Stat label="Posiciones enviadas" icon="checkmark-done-outline" value={String(driver.sentCount)} />
      </View>

      {weak ? (
        <Text style={styles.tip}>Pasa a un lugar más abierto. Entre edificios altos o bajo techo el GPS pierde precisión.</Text>
      ) : (
        <View style={styles.nextStop}>
          <Ionicons name="location-outline" size={22} color={colors.routeDark} />
          <View>
            <Text style={styles.nextStopCaption}>Próxima parada</Text>
            <Text style={styles.nextStopName}>
              {ownPosition?.nextStopName ? stopShortName(ownPosition.nextStopName) : '—'}
            </Text>
          </View>
        </View>
      )}

      <View style={styles.spacer} />

      <Pressable
        onPress={onStop}
        style={({ pressed }) => [styles.stopButton, pressed && styles.pressed]}
        accessibilityRole="button"
      >
        <Ionicons name="stop" size={18} color={colors.surface} />
        <Text style={styles.stopText}>Detener recorrido</Text>
      </Pressable>
      <Text style={styles.footnote}>Mantén la app abierta: con el teléfono bloqueado el GPS se detiene.</Text>
    </View>
  );
}

function subtitle(status: keyof typeof headlines, startedAt: number, now: number): string {
  switch (status) {
    case 'starting':
      return 'Puede tardar unos segundos al aire libre.';
    case 'weakSignal':
      return `Esta lectura no se envía: su precisión es peor que ${MAX_ACCURACY_METERS} m.`;
    case 'sendFailed':
      return 'Revisa la conexión con el servidor. Se reintenta con la siguiente lectura.';
    default: {
      const minutes = Math.floor((now - startedAt) / 60_000);
      return `Recorrido iniciado hace ${minutes < 1 ? 'menos de 1 min' : `${minutes} min`} · cada 5 s`;
    }
  }
}

function Stat({ label, icon, value, warning = false }: { label: string; icon: IconName; value: string; warning?: boolean }) {
  return (
    <View style={[styles.stat, warning && styles.statWarning]}>
      <View style={styles.statLabel}>
        <Ionicons name={icon} size={14} color={colors.textMuted} />
        <Text style={styles.statLabelText}>{label}</Text>
      </View>
      <Text style={[styles.statValue, warning && styles.warningText]}>{value}</Text>
    </View>
  );
}

const toneColor = { ok: colors.live, muted: colors.textMuted, warning: colors.warning } as const;
const toneBackground = StyleSheet.create({
  ok: { backgroundColor: '#E3F3EA' },
  muted: { backgroundColor: '#EEF0F2' },
  warning: { backgroundColor: colors.warningTint },
});

const styles = StyleSheet.create({
  panel: {
    width: '100%',
    maxWidth: 560,
    height: DRIVER_PANEL_HEIGHT,
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
  header: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  headerIcon: { width: 44, height: 44, borderRadius: 22, alignItems: 'center', justifyContent: 'center' },
  headerText: { flex: 1, gap: 2 },
  title: { fontSize: 19, fontWeight: '700', color: colors.text },
  muted: { fontSize: 13, color: colors.textMuted },
  stats: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  stat: {
    flexBasis: '45%',
    flexGrow: 1,
    gap: 4,
    padding: 12,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
  },
  statWarning: { borderColor: '#F0D9B0', backgroundColor: '#FFFAF1' },
  statLabel: { flexDirection: 'row', alignItems: 'center', gap: 5 },
  statLabelText: { fontSize: 12, color: colors.textMuted },
  statValue: { fontSize: 17, fontWeight: '700', color: colors.text },
  warningText: { color: colors.warning },
  tip: { fontSize: 13, lineHeight: 19, color: colors.textMuted },
  nextStop: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 12,
    paddingHorizontal: 14,
    borderRadius: 14,
    backgroundColor: colors.routeTint,
  },
  nextStopCaption: { fontSize: 12, fontWeight: '500', color: '#3D4A5C' },
  nextStopName: { fontSize: 16, fontWeight: '600', color: colors.routeDark },
  spacer: { flex: 1 },
  stopButton: {
    minHeight: 56,
    borderRadius: 16,
    backgroundColor: colors.error,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 10,
  },
  stopText: { fontSize: 17, fontWeight: '700', color: colors.surface },
  pressed: { opacity: 0.85 },
  footnote: { fontSize: 12, color: colors.textMuted, textAlign: 'center' },
});
