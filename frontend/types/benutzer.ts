/** Rollen wie im Backend (Models/Roles.cs). */
export const ROLLEN = ['User', 'Admin'] as const;
export type Rolle = (typeof ROLLEN)[number];

export function alsRolle(wert: unknown): Rolle {
  return wert === 'Admin' ? 'Admin' : 'User';
}

/**
 * Ein Benutzer, wie ihn /users liefert – ohne Passwort.
 * Anders als Adresse und City kommt er nicht über OData und ohne Audit-Felder.
 */
export interface Benutzer {
  id: number;
  email: string;
  displayName: string;
  role: Rolle;
  /** Ende der Anmeldesperre (ISO 8601, UTC) oder null, wenn nicht gesperrt. */
  lockedUntil: string | null;
  /** true für den angemeldeten Benutzer selbst. */
  isSelf: boolean;
}

/** Was POST /users entgegennimmt. */
export interface BenutzerCreate {
  email: string;
  displayName: string;
  role: Rolle;
  password: string;
}

/** Was PUT /users/id entgegennimmt; ohne password bleibt das Passwort bestehen. */
export interface BenutzerUpdate {
  displayName: string;
  role: Rolle;
  password?: string;
}
