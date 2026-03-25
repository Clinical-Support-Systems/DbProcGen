# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| .NET architecture and generator boundaries | Fry | CLI-first architecture, project boundaries, contracts, integration strategy |
| SQL generation and procedure emission | Hermes | Spec parsing rules, wrapper/worker SQL emission, deterministic naming/order |
| SQL Database Project / DACPAC workflow | Bender | Generated/ layout, sqlproj inclusion, stale artifact cleanup, CI deployment wiring |
| Testing and quality gates | Leela | Unit/integration tests, determinism checks, stale-output CI checks, contract tests |
| Documentation and ADR discipline | Amy | ADR consistency, docs updates, rationale capture, migration notes |
| Code review and acceptance gate | Fry | Cross-cutting review, architecture fitness, ADR compliance |
| Session logging | Scribe | Automatic - never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Fry |
| `squad:fry` | Own .NET architecture / lead decisions | Fry |
| `squad:hermes` | Own SQL generation implementation work | Hermes |
| `squad:bender` | Own SQLProj/DACPAC and deployment workflow work | Bender |
| `squad:leela` | Own test and quality gate work | Leela |
| `squad:amy` | Own docs/ADR updates and narrative consistency | Amy |

## Rules

1. ADRs in `docs/adr/0001`..`0006` are binding unless the user explicitly changes them.
2. Keep wrapper procedure contract stable; worker procedures remain implementation details.
3. Keep generated SQL deterministic and committed; CI should detect drift.
4. SQL Database Project remains deployment source of truth.
5. Prefer parallel fan-out for implementation, testing, and docs where dependencies allow.