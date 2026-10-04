# Stage 1: Dependencies
FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
# npm ci installiert exakt die Versionen aus package-lock.json
RUN npm ci

# Stage 2: Build
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# Backend-URL zeigt auf Docker-Service-Namen "backend"
ENV NEXT_PUBLIC_API_URL=http://localhost:5000
RUN npm run build

# Stage 3: Runtime
FROM node:20-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

# Nicht als root laufen (Benutzer "node" ist im node-Image vorhanden)
USER node

EXPOSE 3000
CMD ["node", "server.js"]
