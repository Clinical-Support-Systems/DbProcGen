using DbProcGen.Model;

namespace DbProcGen.Spec;

public sealed record SpecDocument(
    DbProcSpec? Spec,
    IReadOnlyList<SpecDiagnostic> Diagnostics)
{
    public bool IsValid => Spec is not null && Diagnostics.All(static d => d.Severity != SpecDiagnosticSeverity.Error);
}

public static class SpecDocumentFactory
{
    public static SpecDocument ParseAndValidate(string json)
    {
        var parseResult = SpecParser.Parse(json);
        if (parseResult.Spec is null)
        {
            return new SpecDocument(null, parseResult.Diagnostics);
        }

        var validationDiagnostics = SpecValidator.Validate(parseResult.Spec);
        var diagnostics = parseResult.Diagnostics
            .Concat(validationDiagnostics)
            .OrderBy(static d => d, SpecDiagnostic.DeterministicComparer)
            .ToArray();

        return new SpecDocument(parseResult.Spec, diagnostics);
    }
}
