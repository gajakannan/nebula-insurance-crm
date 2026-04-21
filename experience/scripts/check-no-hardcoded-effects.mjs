import fs from 'node:fs'
import path from 'node:path'

const ROOT = process.cwd()
const SRC_DIR = path.join(ROOT, 'src')

// Block arbitrary Tailwind shadow/drop-shadow classes with literal colors in app code.
// Use centralized fx-* effect utilities or CSS vars instead.
const HARDCODED_EFFECT_RE =
  /\b(?:drop-shadow|shadow)-\[[^\]]*(?:rgba?\(|hsla?\(|#[0-9a-fA-F]{3,8})/g

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
    HARDCODED_EFFECT_RE.lastIndex = 0

    let match
    while ((match = HARDCODED_EFFECT_RE.exec(line)) !== null) {
      violations.push({
        line: lineIndex + 1,
        column: match.index + 1,
        token: match[0],
      })
    }
  }

  return violations
}

if (!fs.existsSync(SRC_DIR)) {
  console.error('Missing src directory. Run this script from experience/.')
  process.exit(1)
}

const violationsByFile = []

for (const file of walk(SRC_DIR)) {
  const relativePath = path.relative(ROOT, file).replaceAll(path.sep, '/')
  const violations = findViolations(file)
  if (violations.length > 0) {
    violationsByFile.push({ relativePath, violations })
  }
}

if (violationsByFile.length > 0) {
  console.error('Effects guard failed: hardcoded shadow/drop-shadow color literals found.')
  console.error('Use centralized fx-* effect utilities or CSS vars in index.css instead of arbitrary color shadows in TSX.')
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

console.log('Effects guard passed: no hardcoded shadow/drop-shadow color literals found.')
