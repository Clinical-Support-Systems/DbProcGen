using DbProcGen.Model;

namespace DbProcGen.Spec;

/// <summary>
///     The outcome of parsing a JSON spec, containing the parsed model (if successful) and any parse diagnostics.
/// </summary>
/// <param name="Spec">the parsed spec; `null` if parsing encountered errors</param>
/// <param name="Diagnostics">all diagnostics produced during parsing</param>
public sealed record SpecParseResult(
    DbProcSpec? Spec,
    IReadOnlyList<SpecDiagnostic> Diagnostics)
{
    /// <summary>
    ///     `true` if parsing succeeded and no error-level diagnostics were produced.
    /// </summary>
    public bool IsSuccess =>
        Spec is not null && Diagnostics.All(static d => d.Severity != SpecDiagnosticSeverity.Error);
}
