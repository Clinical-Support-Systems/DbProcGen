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

