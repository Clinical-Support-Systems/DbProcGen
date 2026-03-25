using System.Text.Json.Serialization;

namespace DbProcGen.Runtime;

public sealed record RuntimeGenerationManifest(
    [property: JsonPropertyName("generatedAt")] string GeneratedAt,
    [property: JsonPropertyName("families")] IReadOnlyList<RuntimeProcedureFamily> Families);

public sealed record RuntimeProcedureFamily(
    [property: JsonPropertyName("logicalName")] string LogicalName,
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("publicProcedure")] string PublicProcedure,
    [property: JsonPropertyName("wrapperFile")] string WrapperFile,
    [property: JsonPropertyName("workers")] IReadOnlyList<RuntimeWorkerVariant> Workers);

public sealed record RuntimeWorkerVariant(
    [property: JsonPropertyName("routeName")] string RouteName,
    [property: JsonPropertyName("workerSuffix")] string WorkerSuffix,
    [property: JsonPropertyName("workerFile")] string WorkerFile,
    [property: JsonPropertyName("conditions")] IReadOnlyList<RuntimeRouteCondition> Conditions);

public sealed record RuntimeRouteCondition(
    [property: JsonPropertyName("axis")] string Axis,
    [property: JsonPropertyName("value")] string Value);
