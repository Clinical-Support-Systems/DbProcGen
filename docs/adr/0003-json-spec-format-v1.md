# ADR 0003: Use JSON as the v1 declarative spec format

- Status: Accepted
- Date: 2026-03-25
- Deciders: Platform / Database Architecture
- Tags: dsl, spec, json, v1

## Context

We need a declarative source format for logical stored procedure definitions.

The format must:
- be easy to parse in .NET
- support validation and good diagnostics
- be easy to diff in pull requests
- be deterministic for generation
- avoid unnecessary parser complexity in v1

## Decision

We will use **JSON** as the v1 source format for procedure specs.

Example file naming:
- `*.dbproc.json`

Example directory:
- `specs/<domain>/<logical-name>.dbproc.json`

## Rationale

JSON is a good v1 choice because it is:
- simple to parse
- familiar to .NET teams
- deterministic
- easy to validate with structured rules
- easy to snapshot-test
- less ambiguous than YAML for an initial DSL

We value implementation speed and reliability over authoring ergonomics in v1.

## Consequences

### Positive
- The parser and validator stay simple.
- Specs are easy to test and normalize.
- Generated outputs can be traced cleanly back to source specs.

### Negative
- JSON is more verbose than YAML.
- Comments are awkward unless we support a companion convention.
- Some authors may find raw JSON less pleasant to edit by hand.

## Rules for v1

A spec must be able to define:
- logical procedure name
- target schema
- public procedure name
- parameters
- result contract
- specialization axes
- routing rules
- optional reusable fragments

A spec must not try to become:
- a full SQL parser
- a generic programming language
- a complete optimizer model

## Deferred work

We may later add:
- YAML support
- a richer fluent API
- code-generated C# builders
- schema/version migration tooling for specs

## Alternatives considered

### 1. YAML
Rejected for v1 because it increases parser/authoring ambiguity.

### 2. C# fluent definitions only
Rejected for v1 because it raises the barrier to authoring and ties the DSL too closely to implementation details.

### 3. Raw SQL templates only
Rejected for v1 because it does not provide enough structured metadata for specialization and validation.