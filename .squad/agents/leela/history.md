# Project Context

- **Owner:** Kori Francis
- **Project:** DbProcGen
- **Stack:** .NET, SQL Server, Azure SQL, SQL Database Project (DACPAC)
- **Created:** 2026-03-25

## Learnings

- CI should fail when generation output drifts from tracked files.
- Wrapper boundary compatibility is the core contract to test.
- **Verify.TUnit snapshot testing** (v31.13.5) requires TUnit v1.21.20+ for API compatibility.
- Snapshot tests successfully capture wrapper/worker SQL output for determinism validation.
- Line ending normalization (CRLF→LF) ensures cross-platform test stability.
- Temporary directory fixtures keep test runs isolated from repo working tree.
- Verify auto-creates `.received.txt` on first run; copy to `.verified.txt` to accept baseline.