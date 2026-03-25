using DbProcGen.Model;

namespace DbProcGen.Tool.Services;

public interface IArtifactGenerator
{
    GenerationResult Generate(DbProcSpec[] specs, string outputDirectory);
}

public sealed record GenerationResult(
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> DeletedFiles);
