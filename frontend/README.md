# storionX Migration — Frontend

React 19 + Vite 8 + Tailwind CSS v4 + shadcn/ui dashboard for monitoring EV → storionX migration runs.

## Setup

```bash
pnpm install
cp .env.example .env   # edit VITE_API_URL if your backend runs elsewhere
pnpm dev               # http://localhost:5173
```

## Environment variables

| Variable        | Default                  | Description                  |
|-----------------|--------------------------|------------------------------|
| `VITE_API_URL`  | `http://localhost:8080`  | Base URL of Migration.Api    |

## Generating API types

Types in `src/lib/api-types.ts` are auto-generated from the backend OpenAPI schema.
**Never edit that file by hand.**

```bash
# Backend must be running at VITE_API_URL first
pnpm gen:api
```

All TypeScript types consumed by hooks and pages live in `src/lib/types.ts` as aliases
pointing into the generated `api-types.ts`. Add new aliases there when the backend schema grows.

## Full stack (Docker)

```bash
# from the repo root
docker compose up
```

Frontend → http://localhost:5173  
API → http://localhost:8080  
OpenAPI → http://localhost:8080/openapi/v1.json

## Scripts

| Command           | Description                        |
|-------------------|------------------------------------|
| `pnpm dev`        | Start dev server                   |
| `pnpm build`      | Production build                   |
| `pnpm typecheck`  | TypeScript type check (no emit)    |
| `pnpm gen:api`    | Regenerate types from OpenAPI      |
