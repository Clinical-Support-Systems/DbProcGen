using DbProcGen.Model;

namespace DbProcGen.Generator;

public interface IArtifactGenerator
{
    GenerationResult Generate(DbProcSpec[] specs, string outputDirectory);
}

public sealed record GenerationResult(
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> DeletedFiles,
    string? ManifestFile);

public sealed record GenerationManifest(
    string GeneratedAt,
    IReadOnlyList<ProcedureFamilyManifest> Families);

public sealed record ProcedureFamilyManifest(
    string LogicalName,
    string Schema,
    string PublicProcedure,
    string WrapperFile,
    IReadOnlyList<WorkerVariantManifest> Workers);

public sealed record WorkerVariantManifest(
    string RouteName,
    string WorkerSuffix,
    string WorkerFile,
    IReadOnlyList<RouteConditionManifest> Conditions);

public sealed record RouteConditionManifest(
    string Axis,
    string Value);
