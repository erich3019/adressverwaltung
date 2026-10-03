import type { Metadata } from 'next';
import './globals.css';
import Providers from './providers';
import NavBar from '@/components/NavBar';

export const metadata: Metadata = {
  title: 'Adressverwaltung',
  description: 'Tutorial-Applikation: Next.js + OData + PostgreSQL',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="de">
      <body className="bg-gray-100 min-h-screen">
        {/*
          Providers kapselt den SessionProvider von NextAuth.
          Da SessionProvider 'use client' ist, bleibt dieses Layout ein Server-Component.
        */}
        <Providers>
          <NavBar />
          {children}
        </Providers>
      </body>
    </html>
  );
}
