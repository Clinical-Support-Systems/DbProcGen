using DbProcGen.Model;

namespace DbProcGen.Spec;

/// <summary>
///     The result of parsing and validating a spec, containing the parsed spec (if successful) and all diagnostics.
/// </summary>
/// <param name="Spec">the parsed spec; `null` if parsing failed with errors</param>
/// <param name="Diagnostics">all diagnostics produced during parsing and validation</param>
public sealed record SpecDocument(
    DbProcSpec? Spec,
    IReadOnlyList<SpecDiagnostic> Diagnostics)
{
    /// <summary>
    ///     `true` if the spec parsed successfully and contains no error-level diagnostics.
    /// </summary>
    public bool IsValid => Spec is not null && Diagnostics.All(static d => d.Severity != SpecDiagnosticSeverity.Error);
}

/// <summary>
///     Factory that composes parsing and validation into a single entry point.
/// </summary>
public static class SpecDocumentFactory
{
    /// <summary>
    ///     Parses the JSON spec and validates the resulting model, returning a <see cref="SpecDocument" />
    ///     with merged, deterministically ordered diagnostics.
    /// </summary>
    /// <param name="json">the raw JSON spec content</param>
    /// <returns>A <see cref="SpecDocument" /> containing the parsed spec and all diagnostics.</returns>
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
