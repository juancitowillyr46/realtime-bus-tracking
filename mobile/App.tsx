import { StatusBar } from 'expo-status-bar';
import { useState } from 'react';

import type { AppMode } from './src/components/ModeSwitch';
import { DriverScreen } from './src/screens/DriverScreen';
import { ParentScreen } from './src/screens/ParentScreen';

// One app, two modes (docs/brief.md): parents watch the map, a driver's phone acts as a bus.
// A plain state switch instead of a navigation library: there are only two screens.
export default function App() {
  const [mode, setMode] = useState<AppMode>('parent');

  return (
    <>
      {mode === 'parent' ? (
        <ParentScreen onDriverMode={() => setMode('driver')} />
      ) : (
        <DriverScreen onParentMode={() => setMode('parent')} />
      )}
      <StatusBar style="dark" />
    </>
  );
}
