# Getting Started (Junior Developer Guide)

This guide is for developers who want to tinker locally and understand how DbProcGen works end-to-end.

If you only do one thing: run `generate`, inspect `database/Generated/`, then run tests.

## What this repo is (in plain terms)

DbProcGen reads JSON specs and generates SQL stored procedures at **build time**.

- You edit specs in `specs/`
- The tool generates SQL into `database/Generated/`
- SQL is built into a DACPAC via `database/DbProcGen.Database.csproj`
- Generated SQL is committed to git for PR review

ADR references:
- Build-time generation: `docs/adr/0001-build-time-generation.md`
- SQL project source-of-truth: `docs/adr/0002-sqlproj-as-source-of-truth.md`
- Wrapper + worker pattern: `docs/adr/0004-wrapper-and-worker-procedures.md`
- Deterministic artifacts: `docs/adr/0005-deterministic-generated-artifacts.md`

## Prerequisites

- .NET SDK from `global.json` (currently .NET 10 preview track)
- Git
- Optional: Docker (only if you want Testcontainers-backed execution tests)

## Repo map you should care about first

- `specs/users/GetUsersByFilter.dbproc.json` — the main concrete sample spec
- `src/DbProcGen.Tool/` — CLI commands (`doctor`, `validate`, `generate`, `diff`)
- `src/DbProcGen.Spec/` — parser + validator
- `src/DbProcGen.Generator/ArtifactGenerator.cs` — SQL generation core
- `database/Schema/` — hand-authored SQL objects
- `database/Generated/` — generated SQL + `generation-manifest.json`
- `tests/` — project-by-project tests

## First 15 minutes: happy path

Run from repo root:

```powershell
dotnet restore
dotnet build DbProcGen.slnx
dotnet run --project src\DbProcGen.Tool -- doctor
dotnet run --project src\DbProcGen.Tool -- validate
dotnet run --project src\DbProcGen.Tool -- generate
dotnet build database\DbProcGen.Database.csproj
dotnet test --solution DbProcGen.slnx
```

What to verify:

1. `doctor` says environment is OK.
2. `validate` succeeds (or shows useful diagnostics).
3. `generate` updates/creates files in `database/Generated/`.
4. SQL project build produces a DACPAC.
5. Tests pass.

## First tinkering task (safe and useful)

Goal: make a tiny spec change and watch generated SQL change deterministically.

1. Open `specs/users/GetUsersByFilter.dbproc.json`.
2. Change route `sqlBody` in a tiny, safe way (for example, adjust ordering in one query).
3. Run:
   ```powershell
   dotnet run --project src\DbProcGen.Tool -- validate
   dotnet run --project src\DbProcGen.Tool -- generate
   ```
4. Inspect changes in:
   - `database/Generated/dbo_GetUsersByFilter*.sql`
   - `database/Generated/generation-manifest.json`
5. Run:
   ```powershell
   dotnet test --project tests\DbProcGen.Tool.Tests
   dotnet build database\DbProcGen.Database.csproj
   ```

## How to reason about failures

- **Spec validation errors (DBPROCxxx):**
  - Start in `src/DbProcGen.Spec/SpecValidator.cs`
  - Check `specs/README.md` for required fields and route rules

- **Generated SQL not as expected:**
  - Check `src/DbProcGen.Generator/ArtifactGenerator.cs`
  - Compare the spec route conditions and `sqlBody` values

- **SQL project build failure:**
  - Verify generated SQL syntax in `database/Generated/`
  - Ensure schema dependencies in `database/Schema/` exist

- **Runtime helper mismatch questions:**
  - Check `database/Generated/generation-manifest.json`
  - Check `src/DbProcGen.Runtime/RuntimeRouteResolver.cs`

## Tests you’ll run most often

```powershell
# Fast spec + generator confidence
dotnet test --project tests\DbProcGen.Spec.Tests
dotnet test --project tests\DbProcGen.Generator.Tests

# CLI realism snapshots/end-to-end behavior checks
dotnet test --project tests\DbProcGen.Tool.Tests

# Runtime helper behavior
dotnet test --project tests\DbProcGen.Runtime.Tests

# SQL project + database-level checks
dotnet test --project tests\DbProcGen.Database.Tests
```

Optional execution-level SQL test (uses Testcontainers + SQL Server container):

```powershell
$env:DBPROCGEN_ENABLE_TESTCONTAINERS_SQL = "true"
dotnet test --project tests\DbProcGen.Database.Tests
```

## Working agreements (important)

- Do not hand-edit files in `database/Generated/`.
- Make changes in specs/generator code, then regenerate.
- Keep outputs deterministic (no timestamps, random IDs, unstable ordering).
- SQL wrapper is the execution authority; runtime helper is advisory diagnostics.

## What is intentionally not “done” yet

This repo is an architectural PoC with one concrete family.

Not v1-complete yet (on purpose):
- broader multi-family coverage
- richer DSL authoring beyond current v1 mechanisms
- Roslyn analyzer/source-generator integration (post-v1)
- richer typed .NET consumer helpers (post-v1)

That means your best contribution path is:
1) improve correctness and clarity of current flow,
2) add behavior-focused tests,
3) expand sample coverage safely.

