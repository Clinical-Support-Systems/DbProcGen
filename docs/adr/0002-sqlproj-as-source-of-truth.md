# ADR 0002: Use a SQL Database Project as the deployment source of truth

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: sqlproj, dacpac, deployment, schema-as-code

## Context

The project will generate stored procedures and related database objects.

We need a deployment model that:
- supports normal code review
- works in CI/CD
- keeps generated artifacts visible and testable
- does not hide core database logic in opaque runtime steps
- is compatible with Azure SQL deployment practices

## Decision

We will treat the **SQL Database Project** as the deployment source of truth for generated database objects.

Generated SQL files will be written into the repository as normal `.sql` files and included in the SQL project.

The SQL project will build the deployable artifact used by CI/CD.

## Rationale

A SQL project gives us:
- file-based schema ownership
- static validation during build
- compatibility with schema review in pull requests
- a clean boundary between generation and deployment
- a predictable place for hand-authored and generated SQL to coexist

This keeps generation as a build concern and deployment as a database project concern.

## Consequences

### Positive
- Generated procedures are first-class schema artifacts.
- DBAs and developers can review exact emitted SQL before deployment.
- Deployment remains aligned with DACPAC/schema-project workflows.
- The generator can remain focused on emission, not deployment.

### Negative
- Generated file layout must be managed carefully.
- The build must ensure stale generated artifacts are removed.
- Developers must understand the distinction between spec files and generated SQL files.

## Rules

- Generated stored procedures must be emitted as checked-in `.sql` files.
- Generated SQL must live in a dedicated `Generated/` subtree.
- Hand-authored SQL must remain clearly separated from generated SQL.
- Post-deployment scripts are not the primary location for generated procedures.

## Alternatives considered

### 1. Deploy generated SQL directly from the generator
Rejected because it couples generation and deployment too tightly.

### 2. Keep generated SQL only in memory or temporary output
Rejected because it reduces visibility and reviewability.

### 3. Emit only post-deployment scripts
Rejected because it makes generated schema harder to validate and reason about.