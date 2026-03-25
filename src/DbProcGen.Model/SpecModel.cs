namespace DbProcGen.Model;

/// <summary>
///     A complete stored procedure specialization spec defining the public procedure,
///     parameters, result contract, specialization axes, routing rules, and SQL fragments.
/// </summary>
/// <param name="Version">the spec format version (e.g. `"1.0"`)</param>
/// <param name="LogicalName">the logical name identifying this procedure family</param>
/// <param name="Schema">the SQL Server schema for generated procedures</param>
/// <param name="PublicProcedure">the name of the public wrapper procedure</param>
/// <param name="Parameters">the declared procedure parameters</param>
/// <param name="ResultContract">the expected result set column contract</param>
/// <param name="SpecializationAxes">the axes along which worker procedures are specialized</param>
/// <param name="RoutingRules">the rules governing dispatch from wrapper to worker procedures</param>
/// <param name="Fragments">reusable SQL fragments referenced by workers</param>
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

/// <summary>
///     A declared stored procedure parameter with name, SQL type, and requiredness.
/// </summary>
/// <param name="Name">the parameter name (without `@` prefix)</param>
/// <param name="SqlType">the SQL Server data type (e.g. `"nvarchar(200)"`)</param>
/// <param name="Required">`true` if the parameter is required; optional parameters default to `NULL`</param>
public sealed record DbProcParameterSpec(
    string Name,
    string SqlType,
    bool Required);

/// <summary>
///     The result set contract defining the columns all worker procedures must return.
/// </summary>
/// <param name="Columns">the columns in the result set</param>
public sealed record DbProcResultContractSpec(
    IReadOnlyList<DbProcResultColumnSpec> Columns);

/// <summary>
///     A single column in the result set contract.
/// </summary>
/// <param name="Name">the column name</param>
/// <param name="SqlType">the SQL Server data type</param>
/// <param name="Nullable">`true` if the column permits `NULL` values</param>
public sealed record DbProcResultColumnSpec(
    string Name,
    string SqlType,
    bool Nullable);

/// <summary>
///     A specialization axis mapping a named dimension to a procedure parameter and its valid values.
/// </summary>
/// <param name="Name">the axis name (e.g. `"FilterType"`)</param>
/// <param name="Parameter">the parameter name this axis binds to</param>
/// <param name="Values">the set of valid values for this axis</param>
public sealed record DbProcSpecializationAxisSpec(
    string Name,
    string Parameter,
    IReadOnlyList<string> Values);

/// <summary>
///     The routing rules that govern dispatch from the wrapper to worker procedures.
/// </summary>
/// <param name="Routes">the ordered list of route definitions</param>
/// <param name="DefaultRoute">legacy field parsed for compatibility; v1 validation rejects fallback default routes</param>
public sealed record DbProcRoutingRulesSpec(
    IReadOnlyList<DbProcRouteSpec> Routes,
    string? DefaultRoute);

/// <summary>
///     A single routing rule mapping a set of axis conditions to a worker procedure suffix.
/// </summary>
/// <param name="Name">the route name</param>
/// <param name="When">the conditions that must all match for this route to activate</param>
/// <param name="WorkerSuffix">the suffix appended to the public procedure name to form the worker name</param>
/// <param name="SqlBody">optional explicit SQL body for the generated worker procedure</param>
public sealed record DbProcRouteSpec(
    string Name,
    IReadOnlyList<DbProcRouteConditionSpec> When,
    string WorkerSuffix,
    string? SqlBody = null);

/// <summary>
///     A single condition within a route, matching an axis to a specific value.
/// </summary>
/// <param name="Axis">the specialization axis name</param>
/// <param name="MatchValue">the value the axis must equal</param>
public sealed record DbProcRouteConditionSpec(
    string Axis,
    string MatchValue);

/// <summary>
///     A reusable SQL fragment that can be referenced by worker procedure templates.
/// </summary>
/// <param name="Name">the fragment identifier</param>
/// <param name="Kind">the fragment kind (e.g. `"where-clause"`, `"select-list"`)</param>
/// <param name="Content">the raw SQL content</param>
public sealed record DbProcFragmentSpec(
    string Name,
    string Kind,
    string Content);
