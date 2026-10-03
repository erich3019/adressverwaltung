'use client';

// providers.tsx – Client-Komponente, die den SessionProvider von NextAuth kapselt.
// Nur dieser Wrapper muss 'use client' haben; layout.tsx bleibt ein Server-Component.
import { SessionProvider } from 'next-auth/react';
import type { Session } from 'next-auth';

interface ProvidersProps {
  children: React.ReactNode;
  session?: Session | null;
}

export default function Providers({ children, session }: ProvidersProps) {
  return <SessionProvider session={session}>{children}</SessionProvider>;
}
