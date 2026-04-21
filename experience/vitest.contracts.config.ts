import { defineConfig } from 'vitest/config'
import path from 'path'

export default defineConfig({
  test: {
    include: ['tests/contracts/**/*.spec.ts'],
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
