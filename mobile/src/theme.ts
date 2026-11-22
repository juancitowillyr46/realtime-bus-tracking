// Colors from the approved mock-up (docs/handoffs/phase-2.md, "Diseño aprobado").
export const colors = {
  text: '#1B1D21',
  textMuted: '#5B5F66',
  surface: '#FFFFFF',
  border: '#E6E8EC',
  handle: '#D5D8DD',
  mapGround: '#EEF0EB',
  route: '#1F5FBF',
  routeDark: '#174A94',
  routeTint: '#EDF3FC',
  bus: '#F2A900',
  busOffline: '#C9CCD1',
  busOfflineText: '#3A3E45',
  live: '#1E7B4B',
  liveDot: '#1E9E5A',
  warning: '#8A5000',
  warningTint: '#FFF1DC',
  warningIcon: '#FFC24D',
  bannerBg: '#1B1D21',
  bannerText: '#D5D8DD',
  error: '#B42318',
} as const;

export const shadow = {
  shadowColor: '#14181E',
  shadowOpacity: 0.12,
  shadowRadius: 10,
  shadowOffset: { width: 0, height: 2 },
  elevation: 4,
} as const;
