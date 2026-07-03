# Dependency SCA Summary — F0038 — run 2026-07-01-90a75ace

Ran 2026-07-02 across all three runtimes.

| Runtime | Tool | Result | Raw artifact |
|---|---|---|---|
| Engine (.NET) | `dotnet list package --vulnerable` (all 5 projects) | **Clean** — no vulnerable packages | `engine-sca-dotnet.txt` |
| Neuron (Python) | `pip-audit 2.10.1` (OSV + PyPI advisory DB) — 27 exact deployed versions from the running container | **No known vulnerabilities** | `neuron-sca-pip-audit.txt`, `neuron-audited-requirements.txt` |
| Frontend (React) | `pnpm audit --prod` | 9 advisories (1 low / 2 moderate / 6 high) — **pre-existing, repo-wide, NOT F0038-introduced**; recommend repo-level waiver | `frontend-sca-pnpm.txt` |

F0038 introduced **no new frontend dependencies** (S0007/S0008 added no `package.json` deps),
so the frontend advisories are not attributable to this feature.
