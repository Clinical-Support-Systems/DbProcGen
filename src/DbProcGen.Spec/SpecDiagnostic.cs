namespace DbProcGen.Spec;

/// <summary>
///     Severity levels for spec diagnostics.
/// </summary>
public enum SpecDiagnosticSeverity
{
    /// <summary>
    ///     Informational message that does not prevent generation.
    /// </summary>
    Info = 0,

    /// <summary>
    ///     Warning that may indicate a potential issue but does not block generation.
    /// </summary>
    Warning = 1,

    /// <summary>
    ///     Error that prevents generation from proceeding.
    /// </summary>
    Error = 2
}

/// <summary>
///     A diagnostic message produced during spec parsing or validation, carrying a code, JSON path, severity, and message.
/// </summary>
/// <param name="Code">the diagnostic code (e.g. `"DBPROC001"`, `"DBPROC100"`)</param>
/// <param name="Path">the JSON path where the issue was detected (e.g. `"$.version"`)</param>
/// <param name="Severity">the severity level</param>
/// <param name="Message">the human-readable diagnostic message</param>
public sealed record SpecDiagnostic(
    string Code,
    string Path,
    SpecDiagnosticSeverity Severity,
    string Message)
{
    /// <summary>
    ///     A comparer that orders diagnostics deterministically by path, then severity (descending), then code, then message.
    /// </summary>
    public static IComparer<SpecDiagnostic> DeterministicComparer { get; } = new DeterministicSpecDiagnosticComparer();

    private sealed class DeterministicSpecDiagnosticComparer : IComparer<SpecDiagnostic>
    {
        public int Compare(SpecDiagnostic? x, SpecDiagnostic? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var pathCompare = StringComparer.Ordinal.Compare(x.Path, y.Path);
            if (pathCompare != 0)
            {
                return pathCompare;
            }

            var severityCompare = y.Severity.CompareTo(x.Severity);
            if (severityCompare != 0)
            {
                return severityCompare;
            }

            var codeCompare = StringComparer.Ordinal.Compare(x.Code, y.Code);
            if (codeCompare != 0)
            {
                return codeCompare;
            }

            return StringComparer.Ordinal.Compare(x.Message, y.Message);
        }
    }
}
