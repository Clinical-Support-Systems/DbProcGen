# Squad Decisions

## ADR Operating Rules (2026-03-25)

### Architectural Summary
DbProcGen uses **build-time generation** to produce deterministic, version-controlled specialized stored procedures from declarative JSON specs. Generated SQL artifacts are committed to the repo under a SQL Database Project, deployed through normal DACPAC workflows. The generator is CLI-first; Roslyn integration is deferred.

### MUST FOLLOW Rules
- **Source spec format:** JSON only in v1 (`.dbproc.json` files, `specs/<domain>/` layout)
- **Generation timing:** Build-time only; no runtime SQL synthesis as default behavior
- **Artifacts are checked-in:** Generated SQL committed to repo, deterministic, regenerated idempotently
- **Wrapper + Workers:** Emit one stable wrapper (public) and zero-or-more workers (implementation) per logical procedure
- **Storage:** Generated files in dedicated `Generated/` subtree within SQL project
- **Auto-generation header:** All generated files must include an auto-generated marker
- **Stale cleanup:** Generator must remove stale files from previous runs
- **Deployment:** SQL project is source-of-truth; deployment via normal DACPAC/schema-project workflow

### NOT IN V1 Rules
- No YAML, fluent C# builders, or custom migration tooling for specs
- No Roslyn source generators or analyzers (defer to v2)
- No runtime `sp_executesql` as primary specialization mechanism
- No hand-authored and generated SQL mixed in same files
- No procedural code generation outside the generator's control

### Open Questions (within ADR bounds)
- **Routing logic:** Wrapper-to-worker dispatch can be in SQL (stored proc) or .NET (ORM logic); either approach is compatible
- **Worker naming:** Deterministic derivation from specialization axes; exact scheme TBD in spec schema
- **Specialization axes:** Which plan-shaping dimensions are worth specializing (e.g., paging, cardinality, business branches); to be defined per spec family
- **Manifests:** Format and scope of optional generated metadata/reports for ops visibility

## Active Decisions

No additional decisions recorded yet.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
