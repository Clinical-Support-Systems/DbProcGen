# Project Context

- **Owner:** Kori Francis
- **Project:** DbProcGen
- **Stack:** .NET, SQL Server, Azure SQL, SQL Database Project (DACPAC)
- **Created:** 2026-03-25

## Learnings

- SQL Database Project is deployment source of truth.
- Generated SQL lives in a dedicated `Generated/` subtree and stale outputs must be removed.
- End-to-end proof now includes hand-authored query support objects in `database/Schema/` (for example, `Views/UsersForGetUsersByFilter.sql`) to show generated procedures are not standalone artifacts.
- `DbProcGen.Database.sqlproj` wildcard includes (`Schema\**\*.sql` and `Generated\**\*.sql`) cleanly compile both hand-authored and generated assets while keeping them physically separated.
