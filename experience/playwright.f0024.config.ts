import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './tests/visual',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  timeout: 30_000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: process.env.F0024_SMOKE_BASE_URL ?? 'http://127.0.0.1:5173',
    viewport: { width: 1440, height: 900 },
  },
})
