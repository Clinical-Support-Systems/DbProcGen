# DbProcGen Architecture

This document describes the end-to-end design of DbProcGen, including the flow from specs to deployment, the wrapper/worker pattern, and the rationale for key technical choices.

## End-to-End Flow

```
Declarative Spec (JSON)
        ↓
    Validation
        ↓
   Generation
        ↓
  Deterministic SQL Artifacts
        ↓
   SQL Project Build (DACPAC)
        ↓
  Review in PR (exact SQL visible)
        ↓
  Database Deployment
```

### 1. Declarative Spec (JSON)

**ADR Reference:** [ADR 0003 - JSON spec format v1](adr/0003-json-spec-format-v1.md)

Developers author procedure definitions as JSON files in `specs/<domain>/<logical-name>.dbproc.json`.

Each spec declares:
- Logical procedure name (domain-scoped identifier)
- Target schema
- Public wrapper procedure name
- Parameters (types, nullability, defaults)
- Result contract (columns, types)
- Specialization axes (if any) — e.g., "paging vs. non-paging"
- Routing rules — e.g., "if paging, call worker_paging; else call worker_simple"
- Optional reusable fragments

JSON was chosen for v1 because it is:
- Simple to parse without heavy DSL machinery
- Deterministic for generation
- Easy to validate and test
- Familiar to .NET teams
- Easy to diff in pull requests

YAML is deferred to v2 for ergonomics once the schema stabilizes.

### 2. Validation

The CLI tool (`DbProcGen.Tool`) reads all specs and validates them:
- JSON structure is well-formed
- Mandatory fields are present
- Parameter and result types are valid
- Specialization axes are coherent
- Worker names will not collide
- No circular routing rules

Validation fails the build if any spec is invalid, preventing broken SQL from being generated.

### 3. Generation

**ADR Reference:** [ADR 0001 - Build-time generation](adr/0001-build-time-generation.md)

The generator (`DbProcGen.Generator`) processes validated specs and emits SQL:

1. **Deterministic routing** — For each spec, compute the exact set of workers to generate (based on specialization axes).
2. **Worker generation** — Emit one `.sql` file per worker variant with deterministic names.
3. **Wrapper generation** — Emit one wrapper procedure per spec that routes to the correct worker(s).
4. **Cleanup** — Remove any stale generated files that no longer correspond to current specs.

All output is deterministic: running generation twice on the same specs produces identical SQL files in identical order.

### 4. Deterministic SQL Artifacts

**ADR Reference:** [ADR 0005 - Deterministic generated artifacts](adr/0005-deterministic-generated-artifacts.md)

Generated SQL files are written to `database/Generated/` with:
- Fixed, deterministic names derived from spec names and worker variants
- Auto-generated headers indicating they are auto-generated (do not edit)
- Included in the SQL project (`DbProcGen.Database.sqlproj`)

All generated files are committed to source control. This ensures:
- Pull requests show exact SQL changes for review
- DBA visibility and trust in generated procedures
- Reproducibility: the same spec + codebase always produces the same SQL
- Simpler CI: detection of stale artifacts is straightforward

### 5. SQL Project Build

**ADR Reference:** [ADR 0002 - SQL project as deployment source of truth](adr/0002-sqlproj-as-source-of-truth.md)

The SQL project (`database/DbProcGen.Database.sqlproj`) includes:
- `Schema/` — hand-authored tables, indexes, views
- `Generated/` — auto-generated wrapper and worker procedures

The build compiles these into a DACPAC (Data-tier Application Package) that is the definitive deployable artifact.

This keeps generation and deployment concerns cleanly separated:
- Generation writes `.sql` files
- The project system handles schema validation and DACPAC construction
- Deployment uses standard SQL Server tooling (DACPAC, schema compare, etc.)

### 6. Code Review & PR Visibility

Because generated artifacts are committed to git, reviewers can:
- See exact SQL being deployed
- Validate that worker procedures are correctly generated
- Spot issues in routing logic
- Assess performance implications of the generated SQL

This is stronger than runtime dynamic SQL or transient generation because the final deployed SQL is visible before merge.

### 7. Deployment

Generated procedures are deployed like any other schema artifact:
- DACPAC is deployed to production via standard tools (SQL Server deployment, Azure Data Studio, CI/CD pipeline, etc.)
- Wrapper procedures are the public API; caller code never directly references worker procedures
- Worker procedures are implementation details protected by the wrapper contract

## Wrapper and Worker Procedures

**ADR Reference:** [ADR 0004 - Wrapper and worker procedures](adr/0004-wrapper-and-worker-procedures.md)

For each logical procedure, the system generates two types:

### Wrapper Procedure (Stable Public API)

- **Name:** Derived from the logical name, e.g., `dbo.GetUser`
- **Purpose:** Single, stable entry point for application code
- **Behavior:** Orchestration and routing only; minimal heavy query work
- **Result contract:** Guaranteed to match the spec's declared result shape
- **Commitment:** Breaking changes require a version bump or migration plan

Example:
```sql
CREATE PROCEDURE dbo.GetUser
    @UserId INT,
    @IncludePaging BIT = 0
AS
BEGIN
    IF @IncludePaging = 1
        EXEC [dbo].[GetUser_Worker_Paging] @UserId
    ELSE
        EXEC [dbo].[GetUser_Worker_Simple] @UserId
END
```

### Worker Procedures (Specialized Implementations)

- **Names:** Deterministically derived from specialization axes, e.g., `GetUser_Worker_Paging`, `GetUser_Worker_Simple`
- **Purpose:** Query variants optimized for specific plan shapes
- **Result contract:** Must match the wrapper's result contract
- **Commitment:** Implementation details; caller code must not reference directly

Workers branch along axes such as:
- **Paging vs. non-paging:** Different row-count paths can have radically different optimal plans
- **Search vs. lookup:** Cardinality differences warrant separate implementations
- **Object-type specialization:** Filtering by object type early can unlock better indexes
- **Mutually exclusive business branches:** Different code paths for different entities

Example:
```sql
CREATE PROCEDURE dbo.[GetUser_Worker_Paging]
    @UserId INT
AS
BEGIN
    SELECT TOP 100
        UserId, UserName, Email
    FROM dbo.Users
    WHERE UserId = @UserId
    ORDER BY UserName
END

CREATE PROCEDURE dbo.[GetUser_Worker_Simple]
    @UserId INT
AS
BEGIN
    SELECT
        UserId, UserName, Email
    FROM dbo.Users
    WHERE UserId = @UserId
END
```

### Why This Pattern?

1. **Stable public API:** Application code uses one procedure name and doesn't need to know how many specialized variants exist
2. **Targeted optimization:** Specialization is explicit and intentional, avoiding premature generalization
3. **Testability:** Wrapper behavior and result-set compatibility can be verified independent of worker internals
4. **Evolution:** Specialized workers can be added, modified, or removed without breaking the public contract
5. **Simplicity:** Routing logic (if-then branches or parameter-based dispatch) is straightforward and reviewable

## CLI-First Implementation

**ADR Reference:** [ADR 0006 - CLI-first, Roslyn optional](adr/0006-cli-first-roslyn-optional.md)

v1 uses a **standalone CLI generator** (`DbProcGen.Tool`) that is invoked separately:

```bash
dotnet run --project src/DbProcGen.Tool -- generate
```

This tool:
- Reads all `*.dbproc.json` spec files
- Validates them
- Emits SQL artifacts to `database/Generated/`
- Reports diagnostics and errors

### Rationale for CLI-First

- **Simplicity:** No need to integrate with MSBuild or Roslyn before the core generation model is proven
- **Debuggability:** Easy to run locally, inspect intermediate state, and understand failures
- **Composability:** Can be invoked in CI/CD as a normal build step
- **Operational clarity:** Developers see exactly what the generator is doing

### Roslyn Integration (Future)

For v2+, Roslyn analyzers and source generators may be added to provide:
- Compile-time validation in C# consumers
- Generated typed access helpers (e.g., `dbo.Procedures.GetUser()`)
- Diagnostics when specs and calling code drift
- Improved IDE experience

But these are not required for v1 and do not block delivery of core SQL generation.

## Determinism and Build Reproducibility

**ADR Reference:** [ADR 0005 - Deterministic generated artifacts](adr/0005-deterministic-generated-artifacts.md)

The generator **must be deterministic**: running generation twice on the same specs produces byte-for-byte identical output.

This ensures:

1. **Pull request reviewability:** The diff is stable; reviewers see the exact change proposed
2. **Build reproducibility:** CI can detect when generation output is stale or has drifted
3. **Debugging:** When a worker procedure changes, the diff is minimal and meaningful

Requirements for determinism:

- Specs must be read in a deterministic order (typically alphabetical by file path)
- Worker names must be derived deterministically from specialization axes
- Generated procedure ordering must be consistent
- Generated code must not include timestamps, GUIDs, or other non-deterministic values
- File headers must be consistent across runs

A CI check should verify that committed generated artifacts match the current spec + generator state. If not, the build fails and the developer must regenerate.

## Undecided Details (v1 Scope)

The following are intentionally deferred to allow focused v1 delivery:

- **Exact spec schema:** ADRs define minimums (procedure name, parameters, result type, specialization axes). Specifics like parameter length, result-set column ordering, and nullable defaults are open for refinement as specs are authored.

- **Wrapper-to-worker routing mechanism:** ADRs require routing to work but leave implementation open:
  - SQL IF/ELSE branches in the wrapper
  - .NET-side routing with direct worker calls
  - A hybrid approach
  - This will be finalized as the first real spec is implemented.

- **Worker naming collision avoidance:** ADRs require worker names to be deterministic. The exact serialization of specialization axes (e.g., `Worker_Paging_SortByName` vs. `Worker_Paging` vs. a hash of axes) is open for decision once specialization axes are concrete.

- **Reusable SQL fragments:** Whether specs will support shared template snippets (e.g., common WHERE clause patterns) is deferred pending demand signal.

## Build and Test Integration

The complete workflow is:

```bash
# 1. Write/update a spec
vi specs/domain/procedure.dbproc.json

# 2. Generate SQL
dotnet run --project src/DbProcGen.Tool -- generate

# 3. Build .NET projects
dotnet build DbProcGen.slnx

# 4. Run tests
dotnet test --project tests/DbProcGen.Spec.Tests          # Spec parsing/validation
dotnet test --project tests/DbProcGen.Generator.Tests     # Generation logic
dotnet test --project tests/DbProcGen.Database.Tests      # SQL + stored proc behavior
dotnet test --project tests/DbProcGen.Runtime.Tests       # Optional runtime helpers

# 5. Build SQL project (generates DACPAC)
dotnet build database/DbProcGen.Database.sqlproj

# 6. Commit and open PR
git add specs/ database/Generated/
git commit -m "..."
git push
```

The CI/CD pipeline can be configured to:
- Run generation and assert output is deterministic
- Build all projects and run all tests
- Compile the DACPAC
- Optionally run schema validation tests against a test database

## Key Design Principles

1. **Build-time over runtime:** All specialization decisions are made at build time, producing version-controlled, reviewable artifacts.

2. **One public surface, many implementations:** The wrapper procedure is the stable API; workers are internal optimizations.

3. **Deterministic generation:** Specs → SQL is a pure function; the same input always produces the same output.

4. **Schema-as-code:** Generated SQL lives in version control alongside hand-authored schema; deployment is unified through the SQL project.

5. **CLI simplicity first:** v1 uses a straightforward CLI tool; advanced integration (Roslyn, IDE tooling) follows once the core model is proven.

6. **Reviewability and safety:** All generated SQL is visible in pull requests; DBAs and developers can inspect before deployment.

## References

- [README.md](../README.md) — Quick start and scope
- [ADR 0001](adr/0001-build-time-generation.md) — Why build-time generation
- [ADR 0002](adr/0002-sqlproj-as-source-of-truth.md) — SQL project as deployment source
- [ADR 0003](adr/0003-json-spec-format-v1.md) — JSON spec format
- [ADR 0004](adr/0004-wrapper-and-worker-procedures.md) — Wrapper/worker pattern
- [ADR 0005](adr/0005-deterministic-generated-artifacts.md) — Deterministic artifacts
- [ADR 0006](adr/0006-cli-first-roslyn-optional.md) — CLI-first approach
- [specs/README.md](../specs/README.md) — How to write specs
