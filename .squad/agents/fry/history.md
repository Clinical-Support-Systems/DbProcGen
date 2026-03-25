# Project Context

- **Owner:** Kori Francis
- **Project:** DbProcGen
- **Stack:** .NET, SQL Server, Azure SQL, SQL Database Project (DACPAC)
- **Created:** 2026-03-25

## Learnings

- ADR 0001-0006 are binding architecture decisions unless explicitly changed by the user.
- The public DB API must remain stable through wrapper procedures; workers are implementation details.