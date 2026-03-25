using DbProcGen.Model;

namespace DbProcGen.Spec;

public sealed record SpecParseResult(
    DbProcSpec? Spec,
    IReadOnlyList<SpecDiagnostic> Diagnostics)
{
    public bool IsSuccess => Spec is not null && Diagnostics.All(static d => d.Severity != SpecDiagnosticSeverity.Error);
}
