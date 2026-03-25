# Project Context

- **Owner:** Kori Francis
- **Project:** DbProcGen
- **Stack:** .NET, SQL Server, Azure SQL, SQL Database Project (DACPAC)
- **Created:** 2026-03-25

## Learnings

- Documentation must treat ADR 0001-0006 as constraints, not optional guidance.
- Keep spec, generation, and deployment responsibilities clearly separated in docs.
- ADR operating rules distill six decisions into eight concrete MUST-FOLLOW rules, three NOT-IN-V1 boundaries, and four open questions. This frame helps implementation teams stay aligned without reinventing constraints.
- **Realistic examples matter:** First e2e proof must showcase concrete routing, meaningful specialization differences (paged vs unpaged), and explicit schema dependencies. Abstract placeholders lose reviewer confidence and fail to demonstrate value.
- **Schema separation clarity:** Docs must explicitly reference hand-authored `Schema/` objects alongside generated `Generated/` artifacts to ground specs in reality and enforce ADR 0002 boundaries.

## Work: ADR Clarifications (2026-03-25)

**Context:** Implementation discovered three ambiguities in ADR wording that diverged from current constraints:

1. **ADR 0004 routing locus** — The ADR left routing location open ("SQL or .NET"), but implementation requires routing to be generated and visible in SQL for reviewability in PRs.
2. **SQL project compilability** — The ADR didn't specify that generated SQL must be compilable even with placeholders; implementation uses `WHERE 1 = 0` stubs, which are valid but needed explicit ADR grounding.
3. **Manifest optionality** — The ADR mentioned "optional manifests" but implementation requires them for build verification and ops visibility; clarified as mandatory in v1.

**Changes made:**
- ADR 0004: Added v1 clarification that routing must be generated SQL (explicit alternatives section)
- ADR 0002: Added compilability rule requiring valid SQL at all times, even with placeholders
- ADR 0005: Added manifest as mandatory with required use cases
- Squad decisions.md: Updated open questions to reflect resolved routing and manifest requirements
- docs/architecture.md: Added v1 routing and compilability sections with examples

**Pattern:** When implementation reveals ADR ambiguity, clarify the ADR with explicit rules and rationale, preserve the original decision text, and label new constraints as "v1 binding" or "implementation clarification" to distinguish from amendments to core intent.

## Work: E2E Realism Update (2026-03-25)

**Context:** Kori Francis requested first e2e example be updated to read as realistic and explicitly highlight concrete routing, meaningful worker differences, and schema reliance.

**Changes made:**
- Replaced placeholder SQL with working implementations:
  - **Wrapper:** Now includes real `IF/ELSE` routing logic branching on FilterType and IsPaged parameters
  - **name_paged worker:** Implements paginated name search using `OFFSET/FETCH` with dynamic page calculation
  - **email_unpaged worker:** Implements unpaged email lookup with direct equality match—no paging overhead
- Updated spec to include paging parameters (PageSize, PageNumber) required for realistic routing
- Enhanced docs/architecture.md "End-to-End Proof" section to explicitly call out:
  - Concrete wrapper routing syntax with parameter conditions
  - Meaningful specialization (paged vs unpaged query shapes)
  - Hand-authored `database/Schema/Tables/Users.sql` dependency
  - ADR alignment table showing evidence for each binding constraint
- Updated database/Generated/README.md with design highlights and schema references
- Updated README.md manifest example and proof narrative to emphasize realism and worker differences

**Pattern:** Realistic examples anchor understanding for reviewers and future teams. Lead with concrete routing logic, explicit schema dependencies, and performance-meaningful specialization differences. Update all narrative layers (README, architecture.md, Generated/README.md) consistently to reinforce the message.