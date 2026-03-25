# ADR 0004: Generate stable wrapper procedures and specialized worker procedures

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: api, routing, stored-procedures, compatibility

## Context

Application code should not need to know how many specialized stored procedures exist behind a logical operation.

We need:
- one stable public entry point
- the freedom to emit multiple specialized implementations
- a design that preserves compatibility while allowing targeted optimization

## Decision

For each logical procedure family, we will generate:
- one **public wrapper** procedure with the stable contract
- zero or more **worker** procedures with specialized implementations

The wrapper is the supported database API.
Worker procedures are implementation details.

## Rationale

This pattern preserves a stable external contract while allowing the implementation to branch by plan-shaping axes such as:
- paging vs non-paging
- heavy search vs simple lookup
- mutually exclusive business branches
- distinct object kinds that materially change cardinality or joins

It also lets application code remain simple and avoids proliferating public procedure names.

## Consequences

### Positive
- Application code keeps one stable call surface.
- Specialization remains explicit and controllable.
- We can evolve worker procedures without breaking public callers.
- Behavior and result-set compatibility can be tested at the wrapper boundary.

### Negative
- Wrapper procedures must be carefully kept lightweight.
- Debugging may require tracing wrapper-to-worker routing.
- Naming conventions must be enforced consistently.

## Rules

- The wrapper must preserve the public result contract.
- The wrapper should do orchestration only and avoid heavy query work where possible.
- Worker names must be deterministic and derived from specialization axes.
- Callers outside the repo should not depend on worker procedure names.

## Alternatives considered

### 1. Expose every worker as a public procedure
Rejected because it leaks internal specialization details into application code.

### 2. Route only in .NET and call workers directly
Rejected as the default because it weakens the database-side stable API.

### 3. Keep only one public procedure with internal IF branches
Rejected as the general pattern because it becomes harder to maintain as the number of plan shapes grows.