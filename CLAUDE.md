# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

DbProcGen is a build-time SQL Server stored procedure generator. Declarative JSON specs (`.dbproc.json`) define specialization axes and routing rules; the tool generates wrapper + worker stored procedures, compiles them via a `.sqlproj` (DACPAC), and checks generated SQL into git for PR review.

## Build & Test Commands

```bash
# Build entire solution
dotnet build DbProcGen.slnx

# Run all tests (uses Microsoft.Testing.Platform runner)
dotnet test DbProcGen.slnx

# Run a single test project
dotnet test tests/DbProcGen.Spec.Tests

# Run a single test by name
dotnet test tests/DbProcGen.Spec.Tests --filter "ParseAndValidate_ValidSpec_IsValidWithNoDiagnostics"

# Run the CLI tool
dotnet run --project src/DbProcGen.Tool -- generate|validate|clean
```

## Architecture

**Pipeline:** Spec JSON → `SpecParser` (shape) → `SpecValidator` (semantics) → Generator → SQL artifacts → DACPAC

**Projects and their roles:**
- **DbProcGen.Model** — Immutable sealed records (`DbProcSpec` and children) defining the spec domain model
- **DbProcGen.Spec** — `SpecParser` (JSON→model), `SpecValidator` (semantic rules), `SpecDocumentFactory` (composed parse+validate entry point), diagnostic system
- **DbProcGen.Generator** — SQL generation pipeline (`ArtifactGenerator`) emitting wrapper/worker SQL + deterministic generation manifest
- **DbProcGen.Tool** — CLI entry point with `generate`, `validate`, `clean` subcommands (skeleton)
- **DbProcGen.Runtime** — v1 manifest-driven runtime route resolver (`RuntimeRouteResolver`) for diagnostics/preflight checks

**Spec processing flow:** Call `SpecDocumentFactory.ParseAndValidate(json)` which runs `SpecParser.Parse` then `SpecValidator.Validate`, merging diagnostics with deterministic ordering.

**Diagnostic codes:** Parsing errors are DBPROC001–005; validation errors are DBPROC100–161. All diagnostics carry a JSON path, severity, and code.

## Key Design Constraints (from ADRs)

- **Deterministic output** — Same spec must produce byte-identical SQL every time. No timestamps, GUIDs, or non-deterministic ordering in generated output.
- **Committed artifacts** — Generated SQL is checked into `database/Generated/` for review.
- **Wrapper + worker pattern** — One stable public procedure (wrapper) routes to specialized internal workers.
- **CLI-first** — v1 is a standalone CLI tool; Roslyn source generator integration is deferred.
- **JSON specs only** — v1 uses JSON (in `specs/` directory); YAML support deferred.
- **DACPAC deployment** — The `database/DbProcGen.Database.sqlproj` compiles hand-authored schema + generated procedures into a DACPAC.

## Conventions

- .NET 10, C# preview, nullable enabled, implicit usings (set in `Directory.Build.props`)
- Central package management via `Directory.Packages.props` — add version there, not in `.csproj` files
- Tests use **TUnit** (not xUnit/NUnit)
- JSON handling uses **System.Text.Json** exclusively
- Spec files live under `specs/` with `.dbproc.json` extension
- Hand-authored SQL goes in `database/Schema/`; generated SQL goes in `database/Generated/`
- Solution file is the newer `.slnx` format (`DbProcGen.slnx`)

