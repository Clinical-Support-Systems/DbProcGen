# Database layout

This folder follows ADR 0002 and ADR 0005.

- `Schema/` contains hand-authored SQL objects.
- `Generated/` contains deterministic generated SQL artifacts, committed to source control.

Generation output belongs only under `Generated/`.