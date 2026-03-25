using DbProcGen.Generator;
using DbProcGen.Model;
using DbProcGen.Spec;
using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

/// <summary>
///     Loads, validates, and generates SQL artifacts from all `.dbproc.json` specs in the `specs/` directory.
/// </summary>
public sealed class GenerateCommand : ICommand
{
    private readonly IConsoleWriter _console;
    private readonly IArtifactGenerator _generator;
    private readonly ISpecLoader _specLoader;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenerateCommand" /> class.
    /// </summary>
    /// <param name="specLoader">the loader used to read and parse spec files</param>
    /// <param name="generator">the artifact generator</param>
    /// <param name="console">the console writer for output</param>
    public GenerateCommand(ISpecLoader specLoader, IArtifactGenerator generator, IConsoleWriter console)
    {
        _specLoader = specLoader ?? throw new ArgumentNullException(nameof(specLoader));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public string Name => "generate";

    /// <inheritdoc />
    public string Description => "Generate SQL artifacts from specs";

    /// <inheritdoc />
    public int Execute(string[] args)
    {
        var specsDirectory = Path.Combine(Environment.CurrentDirectory, "specs");
        if (!Directory.Exists(specsDirectory))
        {
            _console.WriteError($"Specs directory not found: {specsDirectory}");
            return 1;
        }

        var outputDirectory = Path.Combine(Environment.CurrentDirectory, "database", "Generated");
        Directory.CreateDirectory(outputDirectory);

        var specFiles = Directory.GetFiles(specsDirectory, "*.dbproc.json", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        if (specFiles.Length == 0)
        {
            _console.WriteLine("No spec files found.");
            return 0;
        }

        _console.WriteLine($"Loading and validating {specFiles.Length} spec file(s)...");

        var validSpecs = new List<(string FilePath, DbProcSpec Spec)>();
        var hasErrors = false;

        foreach (var specFile in specFiles)
        {
            var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, specFile);
            var document = _specLoader.LoadSpec(specFile);

            if (!document.IsValid)
            {
                hasErrors = true;
                _console.WriteError($"✗ {relativePath}: validation failed");

                var errors = document.Diagnostics.Where(d => d.Severity == SpecDiagnosticSeverity.Error);
                foreach (var error in errors)
                {
                    _console.WriteError($"  [{error.Code}] {error.Path}: {error.Message}");
                }
            }
            else
            {
                _console.WriteLine($"✓ {relativePath}");
                validSpecs.Add((specFile, document.Spec!));
            }
        }

        if (hasErrors)
        {
            _console.WriteError("\nGeneration aborted due to validation errors.");
            return 1;
        }

        _console.WriteLine(
            $"\nGenerating artifacts to {Path.GetRelativePath(Environment.CurrentDirectory, outputDirectory)}...");

        var result = _generator.Generate(validSpecs.Select(s => s.Spec).ToArray(), outputDirectory);

        _console.WriteLine($"\nGenerated {result.GeneratedFiles.Count} file(s):");
        foreach (var file in result.GeneratedFiles.OrderBy(f => f, StringComparer.Ordinal))
        {
            _console.WriteLine($"  {Path.GetRelativePath(Environment.CurrentDirectory, file)}");
        }

        if (result.ManifestFile != null)
        {
            _console.WriteLine(
                $"\nManifest: {Path.GetRelativePath(Environment.CurrentDirectory, result.ManifestFile)}");
        }

        if (result.DeletedFiles.Count > 0)
        {
            _console.WriteLine($"\nCleaned up {result.DeletedFiles.Count} stale file(s):");
            foreach (var file in result.DeletedFiles.OrderBy(f => f, StringComparer.Ordinal))
            {
                _console.WriteLine($"  {Path.GetRelativePath(Environment.CurrentDirectory, file)}");
            }
        }

        return 0;
    }
}
