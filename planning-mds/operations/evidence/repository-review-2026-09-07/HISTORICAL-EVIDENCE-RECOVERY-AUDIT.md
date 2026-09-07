# Historical evidence recovery audit — 2026-09-07

This audit records the local recovery checks performed for R05. It is a
provenance record, not a replacement for the missing artifacts.

## Local sources checked

- All reachable branches, tags, and commits in the CRM repository.
- Dangling Git commits and blobs reported by `git fsck --unreachable`.
- The working tree, `/tmp`, and the local `/home/gajap` workspace for exact
  missing artifact names.
- Git LFS metadata. Git LFS is not installed in this environment, and the
  repository's `.gitattributes` contains no LFS rules for evidence artifacts.
- Repository workflows and history for `actions/upload-artifact`; no retained
  evidence-upload workflow was found for these runs.

## Findings

The six larger failing packages were committed with their control documents but
with zero raw test or coverage artifacts in the repository tree at the commit
that introduced each run:

| Feature | Run | Introducing commit | Raw test/coverage artifacts in Git at introduction |
|---|---|---|---:|
| F0017 | `2026-06-07-771a5ef6` | `3e4e581` | 0 |
| F0008 | `2026-07-03-fd732693` | `3f0b8fe` | 0 |
| F0024 | `2026-07-03-ba011af8` | `5f60937` | 0 |
| F0022 | `2026-07-03-b9f40b31` | `861a9e5` | 0 |
| F0028 | `2026-07-02-736e7854` | `8bda18b` | 0 |
| F0027 | `2026-07-02-b9316621` | `9f8109b` | 0 |

The dangling-object scan found no matching run paths, artifact names, or
contents. The exact missing raw files are therefore not recoverable from this
repository alone.

The four one-error packages retain raw outputs and scoped tracker-validation
outputs. Their remaining failure is the absence of a validator-recognized G8
tracker-sync result in the historical lifecycle log. That event cannot be
reconstructed as though it ran at the old timestamp; it must be recovered from
the original execution record or re-entered with a new run.

## External recovery sources

The remaining legitimate recovery targets are:

1. GitHub Actions artifacts and logs for the introducing commits and their
   associated pull requests (`3e4e581`, `3f0b8fe`, `5f60937`, `861a9e5`,
   `8bda18b`, and `9f8109b`).
2. Any CI artifact store, runner workspace, or object-storage retention used by
   the feature agents outside this repository.
3. The original developer workspaces or backups that produced the run IDs.

For every recovered file, record the external run/job identifier, source URL or
backup reference, SHA-256 checksum, and the exact historical path it satisfies.
Only then may the file be restored into a durable evidence package and
revalidated. A newer test result, even for the same feature, cannot be copied
back into an older run and treated as historical output.

If those external sources do not contain the artifacts, the recovery result is
**unavailable** and the feature must use the re-entry procedure in
[HISTORICAL-EVIDENCE-DISPOSITION.md](HISTORICAL-EVIDENCE-DISPOSITION.md).
