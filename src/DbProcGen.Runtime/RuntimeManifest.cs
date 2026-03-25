using System.Text.Json.Serialization;

namespace DbProcGen.Runtime;

/// <summary>
///     Deserialized generation manifest used at runtime for route resolution.
/// </summary>
/// <param name="GeneratedAt">the manifest identifier or generation label</param>
/// <param name="Families">the procedure families described in the manifest</param>
public sealed record RuntimeGenerationManifest(
    [property: JsonPropertyName("generatedAt")]
    string GeneratedAt,
    [property: JsonPropertyName("families")]
    IReadOnlyList<RuntimeProcedureFamily> Families);

/// <summary>
///     A procedure family entry in the runtime manifest, linking the public procedure to its worker variants.
/// </summary>
/// <param name="LogicalName">the logical name identifying this procedure family</param>
/// <param name="Schema">the SQL Server schema</param>
/// <param name="PublicProcedure">the public wrapper procedure name</param>
/// <param name="WrapperFile">the generated wrapper SQL file name</param>
/// <param name="Workers">the worker variants available for routing</param>
public sealed record RuntimeProcedureFamily(
    [property: JsonPropertyName("logicalName")]
    string LogicalName,
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("publicProcedure")]
    string PublicProcedure,
    [property: JsonPropertyName("wrapperFile")]
    string WrapperFile,
    [property: JsonPropertyName("workers")]
    IReadOnlyList<RuntimeWorkerVariant> Workers);

/// <summary>
///     A worker variant entry in the runtime manifest, describing a specialized worker procedure and its activation
///     conditions.
/// </summary>
/// <param name="RouteName">the route name</param>
/// <param name="WorkerSuffix">the suffix appended to the public procedure name</param>
/// <param name="WorkerFile">the generated worker SQL file name</param>
/// <param name="Conditions">the conditions that activate this worker</param>
public sealed record RuntimeWorkerVariant(
    [property: JsonPropertyName("routeName")]
    string RouteName,
    [property: JsonPropertyName("workerSuffix")]
    string WorkerSuffix,
    [property: JsonPropertyName("workerFile")]
    string WorkerFile,
    [property: JsonPropertyName("conditions")]
    IReadOnlyList<RuntimeRouteCondition> Conditions);

/// <summary>
///     A routing condition in the runtime manifest, binding an axis to a match value.
/// </summary>
/// <param name="Axis">the specialization axis name</param>
/// <param name="Value">the value the axis must equal</param>
public sealed record RuntimeRouteCondition(
    [property: JsonPropertyName("axis")] string Axis,
    [property: JsonPropertyName("value")] string Value);
