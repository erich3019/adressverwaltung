// Header, den lib/api.ts bei jedem Aufruf des Proxys /api/backend mitschickt und
// den der Proxy verlangt (Schutz vor Cross-Site-Request-Forgery, siehe route.ts).
export const API_HEADER = 'x-adressverwaltung';
