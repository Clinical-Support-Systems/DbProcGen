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

## v1 Routing Implementation

**Clarification (implementation note):**

For v1, wrapper-to-worker routing must be explicit in the generated SQL via `IF ... ELSE` branching or parameterized dispatch logic. This ensures:
- Router logic is visible in generated SQL files (reviewable in PRs)
- No hidden routing state in .NET code
- Database-side audit trail is complete

The specific routing syntax (e.g., `IF @Axis = 'value' EXEC worker_name` vs. dynamic procedure names) is determined by the generator at generation time based on specialization axes. The requirement is **that routing be generated and committed**, not relegated to runtime .NET logic alone.

### Future: Hybrid Routing

v2 may support optional .NET-side routing helpers (e.g., Roslyn-generated typed accessors) that supplement or replace SQL-side routing, but only after v1 routing is proven.

## Alternatives considered

### 1. Expose every worker as a public procedure
Rejected because it leaks internal specialization details into application code.

### 2. Route only in .NET and call workers directly
Rejected as the default because it weakens the database-side stable API and hides routing logic from code review.

### 3. Keep only one public procedure with internal IF branches
Rejected as the general pattern because it becomes harder to maintain as the number of plan shapes grows.

### 4. Implicit routing based on heuristics at query time
Rejected because it couples routing logic to the database engine's optimizer behavior, reducing predictability and reviewability.