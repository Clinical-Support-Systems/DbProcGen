# ADR 0007: Include runtime manifest-based route resolution helper in v1

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: runtime, manifest, routing, dotnet

## Context

v1 already commits deterministic generated SQL plus a manifest (`generation-manifest.json`) that captures wrapper/worker relationships and route conditions.

Without a runtime API, consumers who want preflight checks, diagnostics, or observability in .NET must hand-roll route lookups from the manifest or duplicate routing assumptions.

To make the end-to-end story complete for teams evaluating this proposal, we need a small, explicit runtime helper in v1.

## Decision

v1 includes a lightweight runtime helper in `DbProcGen.Runtime` that:
- loads `generation-manifest.json`
- resolves a logical procedure + axis values to a deterministic worker route
- exposes the resolved worker as structured data (schema, procedure, worker suffix, fully qualified worker name)

This helper is advisory and diagnostics-oriented. It does not replace generated SQL wrapper routing (ADR 0004).

## Rationale

This closes the v1 adoption gap by giving .NET consumers a supported API for:
- validating expected routing behavior from generated artifacts
- surfacing operational diagnostics and support tooling
- integrating route resolution checks into tests without executing SQL

It leverages existing deterministic artifacts instead of introducing new runtime synthesis behavior.

## Consequences

### Positive
- Runtime project is no longer a placeholder in v1.
- The end-to-end example now includes parse/validate/generate/sqlproj/runtime story continuity.
- Consumers can build route-aware tooling directly from committed artifacts.

### Negative
- We must maintain manifest compatibility for runtime consumers.
- Runtime helper tests become a required part of CI.

## Rules

- Runtime helper behavior must derive from generated manifest data, not duplicated hardcoded route tables.
- When no family or route matches, the helper must fail explicitly with actionable errors.
- The helper must remain deterministic and side-effect free.
- SQL wrapper routing remains the authoritative execution path in v1.

## Alternatives considered

### 1. Keep runtime out of v1 entirely
Rejected because it leaves a visible architecture hole for .NET integration and proposal review.

### 2. Route directly in .NET instead of SQL wrappers
Rejected for v1 because ADR 0004 requires generated SQL routing as the deployable, reviewable source.

### 3. Add rich data-access runtime (ADO.NET execution wrappers)
Rejected for v1 because it expands scope too far beyond route-resolution and diagnostics.
