# DbProcGen Architecture

This document describes the current end-to-end architecture of DbProcGen as an architectural proof-of-concept with one concrete family (`GetUsersByFilter`), plus the framework surfaces that are already reusable.

## End-to-End Flow

```
Declarative Spec (JSON)
        ↓
    Validation
        ↓
   Generation
        ↓
Deterministic SQL + Manifest Artifacts
        ↓
SQL Project Build (DACPAC) + Runtime Route Resolver
        ↓
  Review in PR (exact SQL visible)
        ↓
  Database Deployment + Runtime Diagnostics
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
- Optional route-level `sqlBody` for explicit v1 worker body text
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
- Identifier and uniqueness rules are enforced for core fields
- Specialization axes and route conditions reference declared members
- Route-level `sqlBody` (when provided) must be non-empty
- `routingRules.defaultRoute` is rejected in v1 (explicit unmatched failure model)

Validation fails the build if any spec is invalid, preventing broken SQL from being generated.

### 3. Generation

**ADR Reference:** [ADR 0001 - Build-time generation](adr/0001-build-time-generation.md)

The generator (`DbProcGen.Generator`) processes validated specs and emits SQL (implementation lives in `src/DbProcGen.Generator/ArtifactGenerator.cs`):

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

#### SQL Compilability Requirement

**Clarification (v1 binding):** All generated SQL must be syntactically valid and compilable by the SQL project build at all times, including when procedures use placeholder implementations. For example:

```sql
-- Placeholder during spec authoring:
SELECT UserId int NOT NULL, DisplayName nvarchar(200) NOT NULL WHERE 1 = 0;
```

This ensures:
- The DACPAC build never fails due to generated SQL stubs
- Specs can be authored and committed incrementally
- Implementation work can proceed on database schema while procedure bodies are filled in
- Reviewers can see the complete generated shape even before implementation

#### Generation Manifest

**Clarification (v1 binding):** The generator must emit a deterministic manifest (e.g., `generation-manifest.json`) listing:
- All generated procedure families
- All worker variants and their route conditions
- Wrapper and worker file paths
- Sufficient detail for build verification and operational visibility

The manifest is required (not optional) and is used by CI to verify that generated artifacts match the current spec state.

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

### 8. Runtime Helper (v1)

**ADR Reference:** [ADR 0007 - runtime manifest routing helper](adr/0007-runtime-manifest-routing-helper-v1.md)

`DbProcGen.Runtime` provides a v1 helper that loads `database/Generated/generation-manifest.json` and resolves a logical procedure + axis values into a deterministic worker route.

This helper is intentionally advisory:
- It supports application diagnostics, preflight checks, and test assertions
- It does not replace SQL wrapper routing in deployment
- It derives behavior from committed generated artifacts

## Wrapper and Worker Procedures

**ADR Reference:** [ADR 0004 - Wrapper and worker procedures](adr/0004-wrapper-and-worker-procedures.md)

For each logical procedure, the system generates two types:

### Wrapper Procedure (Stable Public API)

- **Name:** Derived from the logical name, e.g., `dbo.GetUser`
- **Purpose:** Single, stable entry point for application code
- **Behavior:** Orchestration and routing only; minimal heavy query work
- **Result contract:** Guaranteed to match the spec's declared result shape
- **Commitment:** Breaking changes require a version bump or migration plan
- **Routing:** v1 routing logic is generated and committed to SQL (visible in the wrapper procedure). Routing may use `IF/ELSE` branching or parameterized dispatch, determined at generation time and reviewed in PRs.

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

## Current framework boundaries (v1)

The following boundaries keep v1 small and ADR-aligned:

- **Exact spec schema:** ADRs define minimums (procedure name, parameters, result type, specialization axes). Specifics like parameter length, result-set column ordering, and nullable defaults are open for refinement as specs are authored.

- **Routing authority is already decided:** wrappers route in generated SQL, runtime resolver is advisory only (ADR 0004 + ADR 0007).
- **Unmatched routes fail explicitly:** wrappers `THROW` and runtime resolver throws.
- **Worker-body authoring is intentionally minimal in v1:** route-level `sqlBody` is supported; a richer compositional authoring model is future work.

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

## End-to-End Proof: GetUsersByFilter

The repository includes a complete end-to-end proof for one procedure family that validates all ADR requirements:

### Inputs

**Spec file:** `specs/users/GetUsersByFilter.dbproc.json`
- Declares two specialization axes: FilterType (Name/Email) and Paging (true/false)
- Defines two routes: `name_paged` and `email_unpaged`
- Specifies parameter and result contracts

### Hand-Authored Schema Dependency

**Location:** `database/Schema/Tables/Users.sql`

The generated procedures depend on a pre-existing hand-authored table:
```sql
CREATE TABLE [dbo].[Users]
(
    [UserId] BIGINT NOT NULL PRIMARY KEY,
    [UserName] NVARCHAR(256) NOT NULL,
    [Email] NVARCHAR(512) NULL
);
```

This separation—hand-authored schema in `Schema/`, generated procedures in `Generated/`—is enforced by ADR 0002 and prevents manual edits to generated files.

### Generated Artifacts

**Location:** `database/Generated/`

1. **Wrapper procedure** (`dbo_GetUsersByFilter.sql`):
   - Public SQL API with stable signature (FilterType, IsPaged, FilterValue, PageSize, PageNumber)
   - **Concrete wrapper routing:** Uses `IF/ELSE` branching to dispatch based on FilterType and IsPaged
   - Routes `Name + Paging=1` to `_name_paged` worker
   - Routes `Email + Paging=0` to `_email_unpaged` worker
   - Explicit unmatched-route failure (`THROW`) when no route condition matches

2. **Worker procedures** (specialized implementations):
   - **`dbo_GetUsersByFilter_name_paged.sql`** (NamePaged route):
     - Specialized for **paginated name searches** with `OFFSET/FETCH` for efficient paging
     - Uses wildcard match (`LIKE`) on `UserName` from `dbo.Users`
     - Includes `OFFSET` calculation to handle page boundaries without materializing all rows
   - **`dbo_GetUsersByFilter_email_unpaged.sql`** (EmailUnpaged route):
     - Specialized for **unpaged email lookups** with direct equality match
     - Simple direct-execution `WHERE Email = @FilterValue` on `dbo.Users`
     - No paging parameters or row-limit overhead
   - Each contains route conditions in comments
   - Each matches the wrapper's result contract (UserId, DisplayName)

3. **Generation manifest** (`generation-manifest.json`):
   - JSON document listing all generated families
   - Shows which workers were emitted and why (route conditions)
   - Deterministic format for build verification
    - Enables ops visibility into generated variants

4. **Runtime route resolution helper** (`src/DbProcGen.Runtime/RuntimeRouteResolver.cs`):
   - Loads the generated manifest as the source for runtime route metadata
   - Resolves route pairs such as:
     - `FilterTypeAxis=Name`, `PagingAxis=true` → `GetUsersByFilter_name_paged`
     - `FilterTypeAxis=Email`, `PagingAxis=false` → `GetUsersByFilter_email_unpaged`
   - Provides deterministic, side-effect free route lookup for .NET diagnostics/test tooling

### Hand-Authored SQL in the Proof

The end-to-end proof also includes hand-authored SQL objects under `database/Schema/` that generated procedures are expected to depend on in real deployments:

- `Tables/Users.sql` (base table)
- `Views/UsersForGetUsersByFilter.sql` (projection-aligned query object for wrapper/worker result shape)

These remain hand-maintained deployment assets and are intentionally separate from `database/Generated/` artifacts.

### Key Design Visibility

The example shows:

| Aspect | Evidence | ADR |
|--------|----------|-----|
| **Concrete wrapper routing** | `IF @FilterType = 'Name' AND @IsPaged = 1 EXEC [dbo].[GetUsersByFilter_name_paged]...` in wrapper | ADR 0004 |
| **Meaningful worker differences** | `_name_paged` uses `OFFSET/FETCH` (pagination); `_email_unpaged` uses direct equality without paging | ADR 0001 |
| **Hand-authored schema dependency** | Both workers query `dbo.Users` (hand-authored in `Schema/Tables/Users.sql`) | ADR 0002 |
| **Build-time generation** | `dotnet run --project src/DbProcGen.Tool -- generate` produces SQL at build time | ADR 0001 |
| **SQL project source-of-truth** | All generated SQL in `database/Generated/`, included in `.sqlproj`, committed to git | ADR 0002 |
| **Wrapper + workers** | One wrapper (`GetUsersByFilter`) + two workers with deterministic naming | ADR 0004 |
| **Deterministic artifacts** | Stable file names, alphabetical ordering, auto-generated headers, stale file cleanup, manifest report | ADR 0005 |
| **Runtime helper continuity** | Manifest-driven route resolution in `DbProcGen.Runtime` (`RuntimeRouteResolver`) | ADR 0007 |

### Determinism Properties

- **Stable naming:** `{schema}_{procedureName}.sql` for wrappers, `{schema}_{procedureName}_{workerSuffix}.sql` for workers
- **Stable ordering:** Specs processed alphabetically by LogicalName; routes sorted by WorkerSuffix
- **No timestamps:** Manifest uses constant "generation-manifest" value instead of timestamp
- **Idempotent:** Running generation twice on same specs produces byte-for-byte identical output

### Tests

Comprehensive test coverage validates the proof:

- **ArtifactGeneratorTests:** Wrapper/worker creation, stale file cleanup, deterministic ordering, manifest validation
- **GeneratorSnapshotTests:** Snapshot verification of generated SQL content, file order, determinism across runs
- **RuntimeRouteResolverTests:** Manifest-driven route resolution behavior and failure cases

All tests use TUnit with async assertions.

## References

- [README.md](../README.md) — Quick start and scope
- [ADR 0001](adr/0001-build-time-generation.md) — Why build-time generation
- [ADR 0002](adr/0002-sqlproj-as-source-of-truth.md) — SQL project as deployment source
- [ADR 0003](adr/0003-json-spec-format-v1.md) — JSON spec format
- [ADR 0004](adr/0004-wrapper-and-worker-procedures.md) — Wrapper/worker pattern
- [ADR 0005](adr/0005-deterministic-generated-artifacts.md) — Deterministic artifacts
- [ADR 0006](adr/0006-cli-first-roslyn-optional.md) — CLI-first approach
- [ADR 0007](adr/0007-runtime-manifest-routing-helper-v1.md) — Runtime helper scope and explicit failure semantics
- [specs/README.md](../specs/README.md) — How to write specs

