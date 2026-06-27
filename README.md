
# Bespoke Sewing Studio frontend

React, Vite and TypeScript frontend for the Bespoke Sewing Studio website. The current UI was exported from Figma Make and is being stabilised incrementally without a backend connection.

## Prototype/mock mode

The frontend currently runs in mock/prototype mode only. Typed mock data is exposed through the modules in `src/api/`; they do not use `fetch`, Axios, a database, or any external backend. A separate ASP.NET Core Web API will replace these mock implementations in a later phase.

Future API configuration lives in `src/config/appConfig.ts`. `isPrototypeMode` must remain enabled until the real API client is implemented.

## Routing

The frontend uses `React Router` with lazy-loaded page routes.

Production hosting must support SPA fallback to `index.html` for client-side routes. See `DEPLOYMENT_NOTES_RU.md` for deployment notes and server-side examples.

## Commands (Windows PowerShell)

Install dependencies:

```powershell
npm.cmd install
```

Run strict TypeScript checks:

```powershell
npm.cmd run typecheck
```

Create a production build:

```powershell
npm.cmd run build
```

Start the development server:

```powershell
npm.cmd run dev
```

Start the development server on the explicit loopback host:

```powershell
npm.cmd run dev -- --host 127.0.0.1
```

Vite prints the local URL in the terminal (normally `http://localhost:5173`).

## Output

The generated production bundle is written to `dist/` and is not committed.

## Images

Original exported source images are kept in `src/imports/`.

Optimised responsive derivatives live in `src/assets/images/optimized/`.
Current image usage is centralised in `src/data/imageAssets.ts`.

When updating large visual assets:

- keep the original source file in `src/imports/`
- generate new optimised derivatives instead of overwriting the original
- prefer responsive hero variants and smaller card-sized gallery assets
  
