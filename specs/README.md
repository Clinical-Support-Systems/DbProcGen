# Procedure specs

Spec files for v1 are JSON (`*.dbproc.json`) per ADR 0003.

Layout convention:

- `specs/<domain>/<logical-name>.dbproc.json`

Required v1 fields:

- `version` (must be `"1.0"`)
- `logicalName`, `schema`, `publicProcedure`
- `parameters[]` with `name`, `sqlType`, optional `required`
- `resultContract.columns[]` with `name`, `sqlType`, optional `nullable`
- `specializationAxes[]` with `name`, `parameter`, `values[]`
- `routingRules.routes[]` with `name`, `workerSuffix`, `when[]` (`axis`, `equals`)
- optional `routingRules.routes[].sqlBody` (explicit worker SQL body text for v1)
- optional `fragments[]` with `name`, `kind`, `content`

Notes:

- `routingRules.defaultRoute` is legacy/parsing-compatible but intentionally rejected by v1 validation.
- Unmatched routes fail explicitly in generated wrappers and in `RuntimeRouteResolver`.
