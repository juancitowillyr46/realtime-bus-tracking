import { Platform, StyleSheet } from 'react-native';

import { colors, shadow } from '../theme';

// Below the iPhone's status bar and notch; the browser has neither. A fixed value keeps the
// app free of extra packages; the iPhone X status bar is 44 pt tall.
export const TOP_OFFSET = Platform.OS === 'web' ? 16 : 52;
export const TOP_BAR_HEIGHT = 48;
export const BANNER_HEIGHT = 72;

/** Overlays shared by the parent and driver screens. */
export const layout = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.mapGround },
  // zIndex: on web, Leaflet's panes use z-index 400-1000 and would cover these overlays.
  // In development Expo Go draws its menu button in the top-right corner; keep clear of it.
  top: {
    position: 'absolute',
    top: TOP_OFFSET,
    left: 16,
    right: __DEV__ && Platform.OS !== 'web' ? 76 : 16,
    gap: 8,
    zIndex: 1100,
  },
  floatingButton: {
    position: 'absolute',
    zIndex: 1100,
    minWidth: 48,
    height: 48,
    borderRadius: 14,
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
    ...shadow,
  },
  // On a wide browser window the panel keeps a phone-like width instead of stretching.
  bottom: { position: 'absolute', left: 0, right: 0, bottom: 0, alignItems: 'center', zIndex: 1100 },
});
