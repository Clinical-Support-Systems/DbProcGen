using DbProcGen.Spec;
using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

public sealed class ValidateCommand : ICommand
{
    private readonly ISpecLoader _specLoader;
    private readonly IConsoleWriter _console;

    public ValidateCommand(ISpecLoader specLoader, IConsoleWriter console)
    {
        _specLoader = specLoader ?? throw new ArgumentNullException(nameof(specLoader));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public string Name => "validate";
    public string Description => "Validate all spec files";

    public int Execute(string[] args)
    {
        var specsDirectory = Path.Combine(Environment.CurrentDirectory, "specs");
        if (!Directory.Exists(specsDirectory))
        {
            _console.WriteError($"Specs directory not found: {specsDirectory}");
            return 1;
        }

        var specFiles = Directory.GetFiles(specsDirectory, "*.dbproc.json", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        if (specFiles.Length == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        _console.WriteLine($"Validating {specFiles.Length} spec file(s)...");
        _console.WriteLine("");

        var totalErrors = 0;
        var totalWarnings = 0;
        var validCount = 0;
        var invalidCount = 0;

        foreach (var specFile in specFiles)
        {
            var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, specFile);
            var document = _specLoader.LoadSpec(specFile);

            if (document.IsValid)
            {
                validCount++;
                _console.WriteLine($"✓ {relativePath}");
            }
            else
            {
                invalidCount++;
                _console.WriteError($"✗ {relativePath}");
            }

            var errors = document.Diagnostics.Where(d => d.Severity == SpecDiagnosticSeverity.Error).ToArray();
            var warnings = document.Diagnostics.Where(d => d.Severity == SpecDiagnosticSeverity.Warning).ToArray();

            totalErrors += errors.Length;
            totalWarnings += warnings.Length;

            foreach (var diagnostic in errors)
            {
                _console.WriteError($"  [{diagnostic.Code}] {diagnostic.Path}: {diagnostic.Message}");
            }

            foreach (var diagnostic in warnings)
            {
                _console.WriteWarning($"  [{diagnostic.Code}] {diagnostic.Path}: {diagnostic.Message}");
            }

            if (errors.Length > 0 || warnings.Length > 0)
            {
                _console.WriteLine("");
            }
        }

        _console.WriteLine($"Summary: {validCount} valid, {invalidCount} invalid");
        _console.WriteLine($"  Errors: {totalErrors}, Warnings: {totalWarnings}");

        return totalErrors > 0 ? 1 : 0;
    }
}
