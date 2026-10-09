// Akzentfarben der Oberfläche. Die Werte entsprechen AccentColors im Backend und
// den Blöcken [data-farbe='…'] in app/globals.css; «blue» ist der Standard.
export const AKZENTFARBEN = [
  { wert: 'blue',   name: 'Blau' },
  { wert: 'green',  name: 'Grün' },
  { wert: 'teal',   name: 'Türkis' },
  { wert: 'violet', name: 'Violett' },
  { wert: 'red',    name: 'Rot' },
  { wert: 'orange', name: 'Orange' },
  { wert: 'slate',  name: 'Grau' },
] as const;

export type Akzentfarbe = (typeof AKZENTFARBEN)[number]['wert'];

export const STANDARDFARBE: Akzentfarbe = 'blue';

// Unbekannte Werte (z.B. von einem neueren Backend) fallen auf den Standard zurück
export function alsAkzentfarbe(wert: unknown): Akzentfarbe {
  return AKZENTFARBEN.find((farbe) => farbe.wert === wert)?.wert ?? STANDARDFARBE;
}
