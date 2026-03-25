using DbProcGen.Model;

namespace DbProcGen.Generator;

/// <summary>
///     Generates SQL artifacts (wrapper + worker stored procedures and manifest) from compiled specs.
/// </summary>
public interface IArtifactGenerator
{
    /// <summary>
    ///     Generates SQL stored procedure files and a generation manifest from the provided specs.
    /// </summary>
    /// <param name="specs">the compiled specs to generate artifacts for</param>
    /// <param name="outputDirectory">the directory to write generated SQL files into</param>
    /// <returns>A <see cref="GenerationResult" /> describing the generated and deleted files.</returns>
    GenerationResult Generate(DbProcSpec[] specs, string outputDirectory);
}

/// <summary>
///     The outcome of a generation run, listing generated files, cleaned-up stale files, and the manifest path.
/// </summary>
/// <param name="GeneratedFiles">the paths of all generated SQL and manifest files</param>
/// <param name="DeletedFiles">the paths of stale files that were removed</param>
/// <param name="ManifestFile">the path to the generation manifest JSON file; `null` if not produced</param>
public sealed record GenerationResult(
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> DeletedFiles,
    string? ManifestFile);

/// <summary>
///     The top-level generation manifest recording all procedure families produced in a generation run.
/// </summary>
/// <param name="GeneratedAt">the manifest identifier or timestamp label</param>
/// <param name="Families">the procedure families included in the manifest</param>
public sealed record GenerationManifest(
    string GeneratedAt,
    IReadOnlyList<ProcedureFamilyManifest> Families);

/// <summary>
///     Manifest entry for a single procedure family (one wrapper + its worker variants).
/// </summary>
/// <param name="LogicalName">the logical name of the procedure family</param>
/// <param name="Schema">the SQL Server schema</param>
/// <param name="PublicProcedure">the public wrapper procedure name</param>
/// <param name="WrapperFile">the generated wrapper SQL file name</param>
/// <param name="Workers">the worker variant entries for this family</param>
public sealed record ProcedureFamilyManifest(
    string LogicalName,
    string Schema,
    string PublicProcedure,
    string WrapperFile,
    IReadOnlyList<WorkerVariantManifest> Workers);

/// <summary>
///     Manifest entry for a single worker variant within a procedure family.
/// </summary>
/// <param name="RouteName">the route name that produces this worker</param>
/// <param name="WorkerSuffix">the suffix appended to the public procedure name</param>
/// <param name="WorkerFile">the generated worker SQL file name</param>
/// <param name="Conditions">the routing conditions that select this worker</param>
public sealed record WorkerVariantManifest(
    string RouteName,
    string WorkerSuffix,
    string WorkerFile,
    IReadOnlyList<RouteConditionManifest> Conditions);

/// <summary>
///     A single routing condition recorded in the generation manifest.
/// </summary>
/// <param name="Axis">the specialization axis name</param>
/// <param name="Value">the match value for this condition</param>
public sealed record RouteConditionManifest(
    string Axis,
    string Value);
