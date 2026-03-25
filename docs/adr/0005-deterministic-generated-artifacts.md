# ADR 0005: Commit deterministic generated artifacts to source control

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: generation, determinism, source-control, reviewability

## Context

Generated SQL can either be:
- created transiently at build time, or
- committed as durable artifacts in the repository

We want strong reviewability and reproducibility.

## Decision

Generated artifacts will be:
- deterministic
- written to fixed repository paths
- committed to source control
- regenerated idempotently from specs

The generator must also remove stale generated files.

## Rationale

Checking in generated artifacts gives us:
- transparent pull-request diffs
- easier DBA review
- build reproducibility
- easier debugging when a generated worker changes
- simpler deployment through normal SQL project workflows

Determinism is required so that running generation twice produces the same output from the same input.

## Consequences

### Positive
- Reviewers can inspect exact SQL changes.
- CI can detect when generation output is stale.
- Generated outputs become part of the repository’s audit trail.

### Negative
- The repo will contain generated files.
- Pull requests may be larger.
- Team discipline is needed to keep generation output current.

## Rules

- All generated files must include an auto-generated header.
- File names must be deterministic.
- Emission order must be deterministic.
- CI should fail if generation changes tracked files unexpectedly.
- Manual edits inside generated folders are not allowed.
- **A generation manifest (e.g., `generation-manifest.json`) must be emitted** listing all generated procedure families, their worker variants, and route conditions. This manifest is required in v1 for:
  - Build verification: CI can compare manifest against specs to detect stale artifacts
  - Operational visibility: DBAs and ops can understand which variants exist and under what conditions
  - Deterministic audit trail: the manifest is deterministic and committed to git
  - The manifest format is defined by the generator; its presence and determinism are mandatory

## Alternatives considered

### 1. Ignore generated files in git
Rejected because it hides important deployment artifacts.

### 2. Commit only specs and regenerate during deployment
Rejected because it reduces visibility and makes review less concrete.