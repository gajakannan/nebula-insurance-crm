/// <reference types="vitest" />
import { defineConfig, loadEnv, type ProxyOptions } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

function createApiProxyOptions(target: string): ProxyOptions {
  return {
    target,
    changeOrigin: true,
    bypass(req) {
      const accept = req.headers.accept ?? ''
      const authorization = req.headers.authorization
      if (!authorization && accept.includes('text/html')) {
        return '/index.html'
      }
      return undefined
    },
  }
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiProxyTarget = process.env.NEBULA_API_PROXY_TARGET?.trim()
    || env.NEBULA_API_PROXY_TARGET?.trim()
    || process.env.VITE_API_PROXY_TARGET?.trim()

    || env.VITE_API_PROXY_TARGET?.trim()
    || 'http://localhost:5113'
  const neuronProxyTarget = process.env.NEBULA_NEURON_PROXY_TARGET?.trim()
    || env.NEBULA_NEURON_PROXY_TARGET?.trim()
    || process.env.VITE_NEURON_PROXY_TARGET?.trim()
    || env.VITE_NEURON_PROXY_TARGET?.trim()
    || 'http://localhost:8200'
  const apiProxyPaths = [
    // Keep OIDC callback (`/auth/callback`) on the frontend router.
    // Only logout should hit the API.
    '/auth/logout',
    '/brokers',
    '/contacts',
    '/communications',
    '/dashboard',
    '/my',
    '/tasks',
    '/timeline',
    '/accounts',
    '/users',
    '/mgas',
    '/programs',
    '/policies',
    '/submissions',
    '/renewals',
    '/carrier-markets',
    '/search-results',
    '/service-cases',
    '/saved-views',
    '/operational-reports',
    '/broker-insights',
    '/expected-commissions',
    '/commission-schedules',
    '/commission-adjustments',
    '/producer-splits',
    '/revenue-attribution',
    '/documents',
    '/document-templates',
    '/distribution-nodes',
    '/producer-ownership',
    '/territories',
    '/territory-assignments',
    '/internal',
    '/admin',
    '/lob-schemas',
    '/healthz',
  ]

  return {
    plugins: [
    react(),
    tailwindcss(),
    {
      name: 'nebula-auth-mode-guard',
      buildStart() {
        if (
          process.env.VITE_AUTH_MODE === 'dev' &&
          process.env.NODE_ENV === 'production'
        ) {
          throw new Error(
            'FATAL: VITE_AUTH_MODE=dev is not permitted in production builds. Set VITE_AUTH_MODE=oidc.',
          );
        }
      },
    },
  ],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      port: 5173,
proxy: {
        ...Object.fromEntries(
          apiProxyPaths.map((pathPrefix) => [
            pathPrefix,
            createApiProxyOptions(apiProxyTarget),
          ]),
        ),
        '/api': {
          target: apiProxyTarget,
          changeOrigin: true,
          rewrite: (proxyPath) => proxyPath.replace(/^\/api/, ''),
        },
        '/neuron': {
          target: neuronProxyTarget,
          changeOrigin: true,
          rewrite: (proxyPath) => proxyPath.replace(/^\/neuron/, ''),
        },
      },
      headers: {
        // Development CSP — Vite/React HMR needs inline + eval script allowances.
        // connect-src includes http://localhost:9000 (authentik IdP) because oidc-client-ts
        // calls the OIDC discovery endpoint and token endpoint directly from the browser.
        'Content-Security-Policy': [
          "default-src 'self'",
          "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
          "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
          "connect-src 'self' ws://localhost:5173 http://localhost:9000",
          // blob: needed for in-browser document preview (image versions rendered
          // from authenticated blob URLs created via URL.createObjectURL).
          "img-src 'self' data: blob:",
          "font-src 'self' data: https://fonts.gstatic.com",
          // 'self' blob: allows the same-origin PDF iframe preview to render the
          // blob URL we fetch through the auth-bearing api.downloadBlob path.
          "frame-src 'self' blob:",
          "object-src 'none'",
          "base-uri 'self'",
          "form-action 'self'",
        ].join('; '),
        'X-Content-Type-Options': 'nosniff',
        'X-Frame-Options': 'DENY',
        'Referrer-Policy': 'strict-origin-when-cross-origin',
      },
    },
    test: {
      environment: 'jsdom',
      environmentOptions: {
        jsdom: {
          url: 'http://localhost/',
        },
      },
      globals: true,
      setupFiles: ['./src/test-setup.ts'],
      include: ['src/**/*.test.{ts,tsx}'],
      exclude: ['tests/visual/**'],
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
      coverage: {
        provider: 'v8',
        reportsDirectory: './coverage',
        reporter: ['text', 'json-summary', 'json', 'lcov', 'html'],
        include: ['src/**/*.{ts,tsx}'],
        exclude: [
          'src/**/*.d.ts',
          'src/test-setup.ts',
          'src/vite-env.d.ts',
          'src/main.tsx',
        ],
      },
    },
  }
})
