import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const backend = 'http://localhost:5656'

// Paths that Vite must serve itself (SPA shell, HMR, local static assets).
const VITE_PREFIXES = ['/src', '/@vite', '/@fs', '/@react', '/node_modules', '/icons/', '/assets/']

export default defineConfig({
  plugins: [react()],

  // Build output goes directly into the ASP.NET Core wwwroot.
  build: {
    outDir: '../src/Jellyking.Host/wwwroot',
    emptyOutDir: true,
  },

  // In development, proxy API calls and any dynamically-configured service
  // base path to the running .NET backend. Vite keeps the SPA shell, HMR,
  // and local static assets.
  server: {
    port: 3000,
    proxy: {
      '/api': { target: backend, changeOrigin: true },

      // Catch-all for service WebUIs configured at runtime (e.g. /sonarr,
      // /radarr, /jellyfin…). Anything that isn't a Vite-reserved path is
      // forwarded to the backend, where YARP routes it to the service.
      '^/.+$': {
        target: backend,
        changeOrigin: true,
        ws: true,
        bypass(req) {
          const url = req.url ?? ''
          if (url === '/' || url.startsWith('/?')) return url
          if (VITE_PREFIXES.some(p => url.startsWith(p))) return url
          // Let Vite handle .ts/.tsx/.css/.svg/.png module requests.
          if (/\.(ts|tsx|css|svg|png|jpg|jpeg|gif|woff2?)$/.test(url)) return url
          return undefined
        },
      },
    },
  },
})
