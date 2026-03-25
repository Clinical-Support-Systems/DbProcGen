namespace DbProcGen.Model;

public sealed record DbProcSpec(
    string Version,
    string LogicalName,
    string Schema,
    string PublicProcedure,
    IReadOnlyList<DbProcParameterSpec> Parameters,
    DbProcResultContractSpec ResultContract,
    IReadOnlyList<DbProcSpecializationAxisSpec> SpecializationAxes,
    DbProcRoutingRulesSpec RoutingRules,
    IReadOnlyList<DbProcFragmentSpec> Fragments);

public sealed record DbProcParameterSpec(
    string Name,
    string SqlType,
    bool Required);

public sealed record DbProcResultContractSpec(
    IReadOnlyList<DbProcResultColumnSpec> Columns);

public sealed record DbProcResultColumnSpec(
    string Name,
    string SqlType,
    bool Nullable);

public sealed record DbProcSpecializationAxisSpec(
    string Name,
    string Parameter,
    IReadOnlyList<string> Values);

public sealed record DbProcRoutingRulesSpec(
    IReadOnlyList<DbProcRouteSpec> Routes,
    string? DefaultRoute);

public sealed record DbProcRouteSpec(
    string Name,
    IReadOnlyList<DbProcRouteConditionSpec> When,
    string WorkerSuffix);

public sealed record DbProcRouteConditionSpec(
    string Axis,
    string MatchValue);

public sealed record DbProcFragmentSpec(
    string Name,
    string Kind,
    string Content);
