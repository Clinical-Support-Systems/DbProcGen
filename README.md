# DbProcGen

**Build-time generation of specialized stored procedures from declarative specs while maintaining a stable public SQL API.**

## Problem Statement

SQL Server stored procedures often serve multiple distinct usage patterns with different plan-shaping needs:

- A search procedure that handles both paging and non-paging queries
- A lookup procedure that needs different access patterns for different object types
- Complex procedures where parameter combinations affect cardinality dramatically

Historically, teams either:
- Hand-maintained multiple specialized procedures (error-prone, costly to sync)
- Used runtime dynamic SQL rewrites (harder to review, harder to debug, risky)
- Forced one procedure to handle all patterns (poor execution plans, parameter sniffing issues)

**DbProcGen solves this by generating specialized stored procedures at build time from a single declarative source**, keeping the public SQL API stable while the implementation branches only where it matters most.

## Scope: v1

This project focuses on:

- **Spec format:** JSON-based declarative procedure definitions (`*.dbproc.json`)
- **Generation:** CLI-first tool to read specs and emit deterministic SQL artifacts
- **Deployment:** SQL Database Project (`.sqlproj`) as the source of truth
- **Output:** Wrapper procedures (stable public API) + specialized worker procedures (implementation variants)
- **Validation:** Build-time checks to ensure generated SQL is deterministic and consistent

Out of scope for v1:
- Roslyn integration (optional for later)
- YAML spec format (v2+ candidate)
- Runtime SQL synthesis as a default behavior
- Generic parameter combination explosion

## Repository Layout

```text
src/
  DbProcGen.Tool/         # CLI entry point: generate, validate, clean
  DbProcGen.Spec/         # Spec model, parser, validator
  DbProcGen.Generator/    # Core generation pipeline
  DbProcGen.Model/        # Shared domain types
  DbProcGen.Runtime/      # Optional runtime-facing routing helpers

tests/
  DbProcGen.Spec.Tests/       # Spec parsing and validation tests
  DbProcGen.Generator.Tests/  # Generator logic tests
  DbProcGen.Runtime.Tests/    # Runtime helper tests
  DbProcGen.Database.Tests/   # Database integration tests

database/
  DbProcGen.Database.sqlproj  # SQL project (build target for deployment)
  Schema/                     # Hand-authored schema (tables, views, etc.)
  Generated/                  # Generated SQL only (deterministic, checked-in)

specs/
  <domain>/<logical-name>.dbproc.json  # Declarative procedure specs

docs/
  adr/                        # Accepted architectural decisions (binding for v1)
  architecture.md             # End-to-end flow and design rationale
```

## Workflow: Spec → Generated SQL → Deployment

```
1. Author spec
   └─ specs/domain/procedure.dbproc.json

2. Run generate
   └─ dotnet run --project src/DbProcGen.Tool -- generate
   └─ Reads all specs, validates, emits SQL files

3. Generated SQL appears in repo
   └─ database/Generated/domain_procedure_wrapper.sql
   └─ database/Generated/domain_procedure_worker_variant1.sql (if specialization needed)
   └─ etc.

4. Build SQL project
   └─ dotnet build database/DbProcGen.Database.sqlproj
   └─ Includes Schema/ + Generated/ in the compiled DACPAC

5. Review & commit
   └─ Code review captures exact SQL changes
   └─ Committed artifacts are deterministic, reproducible

6. Deploy
   └─ Standard DACPAC deployment to SQL Server / Azure SQL
```

## SQL File Organization

**Separation rule:** do not mix hand-authored and generated SQL in the same files.

- `database/Schema/` — hand-written SQL only (create tables, indexes, views, schemas)
- `database/Generated/` — auto-generated SQL only (wrapper and worker procedures)

Generated files are deterministic and regenerated idempotently from specs. Do not edit generated files manually.

## Build and Test Commands

```bash
# Restore dependencies
dotnet restore

# Build .NET projects
dotnet build DbProcGen.slnx

# Generate SQL from specs (placeholder; implementation TBD)
dotnet run --project src/DbProcGen.Tool -- generate

# Run tests
dotnet test tests/DbProcGen.Spec.Tests
dotnet test tests/DbProcGen.Generator.Tests
dotnet test tests/DbProcGen.Runtime.Tests
dotnet test tests/DbProcGen.Database.Tests

# Build SQL project (compiles Schema/ + Generated/ into DACPAC)
dotnet build database/DbProcGen.Database.sqlproj
```

## Specs

Procedure definitions live in `specs/` as JSON files (`*.dbproc.json`). See [specs/README.md](specs/README.md) for layout and format details.

## Architectural Decisions (ADRs)

These ADRs are binding constraints for v1. For detailed context and rationale, see:

- **[ADR 0001](docs/adr/0001-build-time-generation.md)** — Build-time generation (not runtime dynamic SQL)
- **[ADR 0002](docs/adr/0002-sqlproj-as-source-of-truth.md)** — SQL Database Project as deployment source of truth
- **[ADR 0003](docs/adr/0003-json-spec-format-v1.md)** — JSON for v1 specs (YAML deferred)
- **[ADR 0004](docs/adr/0004-wrapper-and-worker-procedures.md)** — Wrapper + worker procedure pattern
- **[ADR 0005](docs/adr/0005-deterministic-generated-artifacts.md)** — Commit deterministic artifacts to git
- **[ADR 0006](docs/adr/0006-cli-first-roslyn-optional.md)** — CLI-first; Roslyn integration deferred

## Status: Skeleton

This repository currently contains placeholder projects and stub files to establish ADR-constrained structure.

**Intentionally undecided (marked for future refinement):**
- Exact spec schema beyond ADR minimums (parameter types, result-set metadata, serialization rules)
- Wrapper-to-worker routing implementation (SQL branches vs. .NET routing or both)
- Worker procedure naming serialization and collision avoidance

For end-to-end architecture and design principles, see [docs/architecture.md](docs/architecture.md).