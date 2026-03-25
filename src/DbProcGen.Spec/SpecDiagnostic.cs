namespace DbProcGen.Spec;

public enum SpecDiagnosticSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed record SpecDiagnostic(
    string Code,
    string Path,
    SpecDiagnosticSeverity Severity,
    string Message)
{
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
