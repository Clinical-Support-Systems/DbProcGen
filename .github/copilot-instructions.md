# Copilot Instructions for DbProcGen

## Build, test, and CLI commands
Run commands from the repository root.

```bash
dotnet restore
dotnet build DbProcGen.slnx
dotnet test DbProcGen.slnx

# Single test project
dotnet test tests/DbProcGen.Spec.Tests

# Single test
dotnet test tests/DbProcGen.Spec.Tests --filter "ParseAndValidate_ValidSpec_IsValidWithNoDiagnostics"

# SQL project (DACPAC)
dotnet build database/DbProcGen.Database.csproj

# CLI workflow
dotnet run --project src/DbProcGen.Tool -- doctor
dotnet run --project src/DbProcGen.Tool -- validate
dotnet run --project src/DbProcGen.Tool -- generate
dotnet run --project src/DbProcGen.Tool -- diff
```

## High-level architecture
DbProcGen is a build-time SQL stored procedure generator.

End-to-end flow:
1. JSON specs (`specs/**/*.dbproc.json`) are parsed by `SpecParser` (shape checks).
2. Parsed specs are validated by `SpecValidator` (semantic rules).
3. `SpecDocumentFactory.ParseAndValidate` merges parse + validation diagnostics in deterministic order.
4. The generator emits wrapper + worker procedures and a generation manifest into `database/Generated/`.
5. `database/DbProcGen.Database.csproj` compiles hand-authored schema + generated SQL into a DACPAC.

Project roles:
- `DbProcGen.Model`: immutable spec domain records.
- `DbProcGen.Spec`: parser, validator, diagnostics, parse+validate composition.
- `DbProcGen.Generator`: SQL artifact generation pipeline.
- `DbProcGen.Tool`: CLI commands (`doctor`, `validate`, `generate`, `diff`).
- `DbProcGen.Runtime`: runtime helpers.

## Key conventions in this repo
- Use `.dbproc.json` specs under `specs/`; keep hand-authored SQL in `database/Schema/` and generated SQL in `database/Generated/`.
- Generated files are deterministic and committed to git. Do not hand-edit files in `database/Generated/`.
- Determinism is mandatory: stable ordering, stable naming, no timestamps/GUIDs/non-deterministic output.
- Validation diagnostics use DBPROC codes:
  - parse/shape: `DBPROC001-DBPROC005`
  - semantic: `DBPROC100-DBPROC161`
- Tests use **TUnit** and async assertions.
- Use **System.Text.Json** for JSON handling.
- Target stack is .NET 10 + C# preview with nullable enabled and implicit usings.
- Package versions are centrally managed in `Directory.Packages.props` (not per-project).
- The CLI resolves `specs/` and `database/` relative to the current working directory; run it from repo root.

