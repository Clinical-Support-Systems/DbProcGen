# Project Context

- **Owner:** Kori Francis
- **Project:** DbProcGen
- **Stack:** .NET, SQL Server, Azure SQL, SQL Database Project (DACPAC)
- **Created:** 2026-03-25

## Learnings

- ADR 0001-0006 are binding architecture decisions unless explicitly changed by the user.
- The public DB API must remain stable through wrapper procedures; workers are implementation details.
- Added a Visual Studio-friendly `DbProcGen.Dev.slnx` that excludes `database/DbProcGen.Database.sqlproj`, preserving `DbProcGen.slnx` for full CLI/CI builds.
- Implemented thin, testable CLI architecture with command dispatcher pattern (ICommand interface + concrete command handlers).
- Service abstractions (ISpecLoader, IArtifactGenerator, IConsoleWriter) enable clean unit testing and separation of concerns.
- TUnit requires async test methods; all assertions must be awaited.
- Deterministic artifact generation follows ADR 0005: stable file naming, stable ordering by spec LogicalName and route WorkerSuffix, auto-generated headers, stale file cleanup.
- Generated SQL targets `database/Generated/` only per ADR 0002, following `{schema}_{procedureName}.sql` and `{schema}_{procedureName}_{workerSuffix}.sql` naming.
- CLI-first approach per ADR 0006: no Roslyn dependency, commands are `validate`, `generate`, `diff` (placeholder), `doctor` (placeholder).
- **End-to-end proof complete:** GetUsersByFilter spec generates wrapper + 2 workers + manifest, validating all ADRs (0001, 0002, 0004, 0005).
- **Manifest generation:** Generation manifest (JSON) reports all procedure families, workers, route conditions for ops visibility and build verification.
- Manifest uses deterministic "generation-manifest" timestamp to ensure byte-for-byte reproducibility across runs.
- Snapshot tests validate deterministic output using Verify library; all 20 Tool tests passing.
- Documentation updated in README.md and docs/architecture.md showing explicit ADR mapping to generated artifacts.
- ADR audit finding: repository intent aligns with ADRs, but SQL deployability is currently blocked by invalid generated SELECT shape (`column type` in SELECT list) and sqlproj duplicate Build item configuration under SDK-style defaults.

