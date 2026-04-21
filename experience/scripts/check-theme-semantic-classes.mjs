import fs from 'node:fs'
import path from 'node:path'

const ROOT = process.cwd()
const SRC_DIR = path.join(ROOT, 'src')

const RAW_PALETTE_RE =
  /\b(?:text|bg|border|placeholder)-(?:zinc|slate|gray|neutral|stone)-\d{2,3}\b/g

function walk(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true })
  const files = []

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name)
    if (entry.isDirectory()) {
      files.push(...walk(fullPath))
      continue
    }

    if (!/\.(ts|tsx)$/.test(entry.name)) continue
    files.push(fullPath)
  }

  return files
}

function findViolations(filePath) {
  const source = fs.readFileSync(filePath, 'utf8')
  const lines = source.split(/\r?\n/)
  const violations = []

  for (let lineIndex = 0; lineIndex < lines.length; lineIndex += 1) {
    const line = lines[lineIndex]
    RAW_PALETTE_RE.lastIndex = 0

    let match
    while ((match = RAW_PALETTE_RE.exec(line)) !== null) {
      violations.push({
        line: lineIndex + 1,
        column: match.index + 1,
        token: match[0],
        lineText: line.trim(),
      })
    }
  }

  return violations
}

if (!fs.existsSync(SRC_DIR)) {
  console.error('Missing src directory. Run this script from experience/.')
  process.exit(1)
}

const files = walk(SRC_DIR)
const violationsByFile = []

for (const file of files) {
  const relativePath = path.relative(ROOT, file).replaceAll(path.sep, '/')
  const violations = findViolations(file)
  if (violations.length > 0) {
    violationsByFile.push({ relativePath, violations })
  }
}

if (violationsByFile.length > 0) {
  console.error('Theme guard failed: raw palette classes found.')
  console.error('Use semantic theme tokens instead (e.g. text-text-primary, bg-surface-card, border-surface-border).')
  console.error('')

  for (const { relativePath, violations } of violationsByFile) {
    console.error(relativePath)
    for (const violation of violations) {
      console.error(
        `  ${relativePath}:${violation.line}:${violation.column} ${violation.token}`,
      )
    }
  }

  process.exit(1)
}

console.log('Theme guard passed: no raw palette classes found.')
