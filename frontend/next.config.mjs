/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'standalone',
  // Kein "X-Powered-By: Next.js" in den Antworten
  poweredByHeader: false,
};

export default nextConfig;
