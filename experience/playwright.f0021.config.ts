import { defineConfig } from '@playwright/test'
import path from 'node:path'

const evidenceRoot = path.resolve(process.cwd(), '../planning-mds/operations/evidence/runs/2026-07-02-ddeb8492')
const outputDir = process.env.F0021_E2E_OUTPUT_DIR ?? path.join(evidenceRoot, 'artifacts/test-results/playwright')
const jsonOutput = process.env.F0021_E2E_JSON_REPORT ?? path.join(evidenceRoot, 'artifacts/test-results/f0021-communications-results.json')

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  timeout: 60_000,
  outputDir,
  reporter: [
    ['list'],
    ['json', { outputFile: jsonOutput }],
  ],
  use: {
    baseURL: 'http://127.0.0.1:5174',
    channel: 'chrome',
    viewport: { width: 1440, height: 900 },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
})
