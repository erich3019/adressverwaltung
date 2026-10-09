// app/api/backend/[...pfad]/route.ts
// Reicht API-Aufrufe des Browsers an das Backend weiter und hängt dabei den
// Bearer-Token an. Der Token liegt nur im verschlüsselten NextAuth-Cookie und
// verlässt den Server nicht – JavaScript im Browser bekommt ihn nie zu sehen.
//
//   Browser:  /api/backend/odata/Adressen   →   Backend:  /odata/Adressen
import { NextRequest, NextResponse } from 'next/server';
import { getToken } from 'next-auth/jwt';
import { API_HEADER } from '@/lib/apiHeader';

const INTERNAL_API = process.env.INTERNAL_API_URL
  ?? process.env.NEXT_PUBLIC_API_URL
  ?? 'http://localhost:5000';

// Nur diese Bereiche des Backends sind über den Proxy erreichbar (nicht /auth)
const ERLAUBTE_BEREICHE = ['odata', 'settings'];

function fehler(status: number, message: string) {
  return NextResponse.json({ message }, { status, headers: { 'Cache-Control': 'no-store' } });
}

async function weiterleiten(req: NextRequest, { params }: { params: Promise<{ pfad: string[] }> }) {
  const { pfad } = await params;

  const pfadErlaubt =
    ERLAUBTE_BEREICHE.includes(pfad[0]) &&
    pfad.every((teil) => teil !== '.' && teil !== '..' && !/[\\/]/.test(teil));
  if (!pfadErlaubt) return fehler(404, 'Nicht gefunden.');

  // Schutz vor Cross-Site-Request-Forgery: Der Aufruf ist über ein Cookie angemeldet,
  // also darf ihn keine fremde Seite auslösen können. Den eigenen Header kann eine
  // fremde Seite nicht setzen (der Browser verlangt dafür eine CORS-Freigabe, die es
  // nicht gibt), und ein mitgeschickter Origin muss der eigene sein.
  const origin = req.headers.get('origin');
  if (!req.headers.has(API_HEADER) || (origin && new URL(origin).host !== req.headers.get('host'))) {
    return fehler(403, 'Aufruf nicht erlaubt.');
  }

  const token = await getToken({ req });
  if (!token?.accessToken) return fehler(401, 'Nicht angemeldet.');

  const headers: Record<string, string> = { Authorization: `Bearer ${token.accessToken}` };
  const contentType = req.headers.get('content-type');
  if (contentType) headers['Content-Type'] = contentType;

  const mitBody = req.method !== 'GET' && req.method !== 'DELETE';
  const antwort = await fetch(`${INTERNAL_API}/${pfad.join('/')}${req.nextUrl.search}`, {
    method: req.method,
    headers,
    body: mitBody ? await req.text() : undefined,
    cache: 'no-store',
  });

  return new NextResponse(antwort.status === 204 ? null : await antwort.text(), {
    status: antwort.status,
    statusText: antwort.statusText,
    headers: {
      'Content-Type': antwort.headers.get('content-type') ?? 'application/json',
      'Cache-Control': 'no-store',
    },
  });
}

export {
  weiterleiten as GET,
  weiterleiten as POST,
  weiterleiten as PATCH,
  weiterleiten as PUT,
  weiterleiten as DELETE,
};
