using DbProcGen.Spec;

namespace DbProcGen.Tool.Services;

/// <summary>
///     Default <see cref="ISpecLoader" /> implementation that reads spec files from disk
///     and delegates to <see cref="SpecDocumentFactory.ParseAndValidate" />.
/// </summary>
public sealed class SpecLoader : ISpecLoader
{
    /// <inheritdoc />
    public SpecDocument LoadSpec(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return new SpecDocument(null, [
                new SpecDiagnostic("FILE001", "$", SpecDiagnosticSeverity.Error, $"File not found: {filePath}")
            ]);
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return SpecDocumentFactory.ParseAndValidate(json);
        }
        catch (Exception ex)
        {
            return new SpecDocument(null, [
                new SpecDiagnostic("FILE002", "$", SpecDiagnosticSeverity.Error, $"Error reading file: {ex.Message}")
            ]);
        }
    }
}
