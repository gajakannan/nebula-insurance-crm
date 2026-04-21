/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig(() => {
  const apiProxyTarget = process.env.NEBULA_API_PROXY_TARGET?.trim()
    || process.env.VITE_API_PROXY_TARGET?.trim()
    || 'http://localhost:5113'
  const apiProxyPaths = [
    // Keep OIDC callback (`/auth/callback`) on the frontend router.
    // Only logout should hit the API.
    '/auth/logout',
    '/brokers',
    '/contacts',
    '/dashboard',
    '/my',
    '/tasks',
    '/timeline',
    '/accounts',
    '/mgas',
    '/programs',
    '/submissions',
    '/renewals',
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
      proxy: Object.fromEntries(
        apiProxyPaths.map((pathPrefix) => [
          pathPrefix,
          {
            target: apiProxyTarget,
            changeOrigin: true,
          },
        ]),
      ),
      headers: {
        // Development CSP — Vite/React HMR needs inline + eval script allowances.
        // connect-src includes http://localhost:9000 (authentik IdP) because oidc-client-ts
        // calls the OIDC discovery endpoint and token endpoint directly from the browser.
        'Content-Security-Policy': [
          "default-src 'self'",
          "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
          "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com",
          "connect-src 'self' ws://localhost:5173 http://localhost:9000",
          "img-src 'self' data:",
          "font-src 'self' data: https://fonts.gstatic.com",
          "frame-src 'none'",
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
