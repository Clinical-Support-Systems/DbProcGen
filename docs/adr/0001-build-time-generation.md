# ADR 0001: Use build-time generation instead of runtime dynamic SQL specialization

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: sql-server, azure-sql, stored-procedures, performance, parameter-sniffing

## Context

We have SQL Server stored procedures, deployed to Azure SQL in production, where plan quality varies significantly by parameter values and calling patterns.

Historically, some procedures have become difficult to optimize because:
- a single procedure must serve several distinct usage patterns
- the optimizer may cache a plan that is good for one pattern and poor for another
- hand-maintaining multiple specialized procedures is costly and error-prone
- runtime dynamic SQL rewrites can drift semantically, weaken reviewability, and increase operational risk

We want developers to author one logical procedure definition while allowing the system to emit multiple specialized stored procedures when necessary.

## Decision

We will use **build-time generation** to produce specialized stored procedures from a single declarative source definition.

We will **not** use runtime string-built SQL as the primary specialization mechanism.

The generated output may include:
- one stable public wrapper procedure
- multiple specialized worker procedures
- supporting manifests and metadata
- optional generated .NET routing/access helpers

## Rationale

Build-time generation gives us:
- version-controlled SQL artifacts
- deterministic outputs that can be reviewed in pull requests
- compatibility with normal database CI/CD workflows
- better DBA visibility and trust
- safer behavior than ad hoc runtime SQL generation
- a single authoring surface for developers

This approach also lets us control specialization intentionally around a small number of plan-shaping axes instead of creating an uncontrolled explosion of procedure variants.

## Consequences

### Positive
- Developers author one logical definition instead of many manual procedures.
- Generated procedures are explicit artifacts and can be tested, reviewed, and benchmarked.
- Database deployment remains aligned with existing schema-as-code practices.
- Public calling contracts can remain stable while implementations evolve.

### Negative
- We must build and maintain a custom generator.
- The repo must include generation, validation, and cleanup workflows.
- Some generated SQL will be repetitive by design.
- The initial design must carefully define which axes deserve specialization.

## Non-goals

This decision does not imply:
- generating every possible parameter combination
- replacing the query optimizer
- replacing Query Store, PSP, OPPO, or normal indexing/tuning work
- introducing runtime SQL synthesis as a default behavior

## Alternatives considered

### 1. Keep one procedure and rely only on query tuning
Rejected because some procedure families have genuinely different plan needs across usage patterns.

### 2. Use runtime `sp_executesql` as the main specialization mechanism
Rejected as the default because it is easier to get wrong semantically, harder to review, and less friendly to stable deployment and diagnostics.

### 3. Hand-author multiple worker procedures
Rejected as the primary model because it scales poorly and increases maintenance cost.

## Follow-up decisions

This ADR requires follow-up decisions on:
- where generated SQL lives
- what the source-of-truth format is
- whether routing happens in SQL, .NET, or both
- how generated artifacts are named and versioned