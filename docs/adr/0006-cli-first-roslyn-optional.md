# ADR 0006: Use a CLI generator first; treat Roslyn integration as optional

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: cli, roslyn, tooling, dotnet

## Context

We need a practical implementation path for generation.

Possible options include:
- a standalone CLI tool
- an MSBuild-integrated generator
- Roslyn source generators/analyzers
- a combination of the above

We want a path that delivers value quickly without overcomplicating the first version.

## Decision

The first implementation will be **CLI-first**.

A standalone generator will:
- read specs
- validate them
- emit SQL artifacts
- optionally emit manifests and reports

Roslyn analyzers or source generators may be added later to improve developer experience, but they are not required for v1.

## Rationale

CLI-first is the simplest and most operationally reliable approach:
- it works well in local development and CI
- it does not require compiler integration to generate `.sql` files
- it is easier to debug and version
- it keeps the first milestone focused on core value

Roslyn is still valuable later for:
- compile-time validation in C# consumers
- generated typed access helpers
- diagnostics when specs and app code drift

## Consequences

### Positive
- Faster first delivery.
- Fewer moving parts in v1.
- Simpler build and debugging model.

### Negative
- The first version may have a less polished IDE experience.
- Typed consumer helpers may arrive later.

## Alternatives considered

### 1. Roslyn-first
Rejected for v1 because it adds complexity before the core SQL-generation model is proven.

### 2. MSBuild-only custom target
Rejected as the primary abstraction because a CLI is easier to invoke directly and test independently.