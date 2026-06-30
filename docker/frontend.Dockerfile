# Frontend multi-stage Dockerfile — Vite dev server (pnpm) + Nginx prod build

# ── Stage: deps ─────────────────────────────────────────────────────────────────
FROM node:22-alpine AS deps
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile

# ── Stage: dev ──────────────────────────────────────────────────────────────────
# Source is bind-mounted at runtime; node_modules come from the deps stage.
FROM node:22-alpine AS dev
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY --from=deps /app/node_modules ./node_modules
COPY . .
EXPOSE 5173
CMD ["pnpm", "dev", "--host"]

# ── Stage: build ────────────────────────────────────────────────────────────────
FROM deps AS builder
COPY . .
RUN pnpm build

# ── Stage: prod ─────────────────────────────────────────────────────────────────
FROM nginx:alpine AS prod
COPY --from=builder /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
