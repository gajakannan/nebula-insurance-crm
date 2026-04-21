import { readdir } from 'node:fs/promises'
import path from 'node:path'
import { spawn } from 'node:child_process'

const [, , needle] = process.argv

if (!needle) {
  console.error('Usage: node ./scripts/run-vitest-by-pattern.mjs <needle>')
  process.exit(1)
}

const projectRoot = process.cwd()
const searchRoot = path.join(projectRoot, 'src')

async function collectMatchingTests(directory) {
  const entries = await readdir(directory, { withFileTypes: true })
  const matches = []

  for (const entry of entries) {
    const fullPath = path.join(directory, entry.name)
    if (entry.isDirectory()) {
      matches.push(...await collectMatchingTests(fullPath))
      continue
    }

    if (entry.isFile() && entry.name.includes(needle) && /\.(ts|tsx)$/.test(entry.name)) {
      matches.push(path.relative(projectRoot, fullPath))
    }
  }

  return matches.sort()
}

const files = await collectMatchingTests(searchRoot)

if (files.length === 0) {
  console.error(`No test files found containing "${needle}" under src/`)
  process.exit(1)
}

const child = spawn('pnpm', ['exec', 'vitest', 'run', ...files], {
  cwd: projectRoot,
  stdio: 'inherit',
  shell: process.platform === 'win32',
})

child.on('exit', (code) => {
  process.exit(code ?? 1)
})
