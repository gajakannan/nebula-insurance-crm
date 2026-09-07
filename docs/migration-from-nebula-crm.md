# Migration provenance

This repository is the Nebula Insurance CRM product split from the former
`nebula-crm` workspace. The split baseline, source revision, and preserved
feature/evidence paths are recorded in [`.split-baseline`](../.split-baseline)
and [`CHANGELOG.md`](../CHANGELOG.md).

After a checkout, run the repository validation commands in the root README,
then apply database migrations through the engine startup or the deployment
runbook. Do not copy credentials or generated coverage into the planning
evidence tree; record their durable artifact paths and the source revision
instead.

For a staged or production deployment, use the environment-specific Compose
overlay (`docker-compose.staging.yml` or `docker-compose.prod.yml`). The deploy
script refuses to promote the development base file by itself.
