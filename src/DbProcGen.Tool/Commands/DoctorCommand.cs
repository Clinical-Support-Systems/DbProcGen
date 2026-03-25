using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

/// <summary>
///     Checks the working directory for required directories and files (specs, database, sqlproj).
/// </summary>
public sealed class DoctorCommand : ICommand
{
    private readonly IConsoleWriter _console;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DoctorCommand" /> class.
    /// </summary>
    /// <param name="console">the console writer for output</param>
    public DoctorCommand(IConsoleWriter console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public string Name => "doctor";

    /// <inheritdoc />
    public string Description => "Check environment configuration and prerequisites";

    /// <inheritdoc />
    public int Execute(string[] args)
    {
        _console.WriteLine("Checking environment...");
        _console.WriteLine("");

        var allOk = true;
        var currentDirectory = Environment.CurrentDirectory;

        allOk &= CheckDirectory("specs", currentDirectory);
        allOk &= CheckDirectory("database", currentDirectory);
        allOk &= CheckFile("database\\DbProcGen.Database.csproj", currentDirectory);

        var generatedDir = Path.Combine(currentDirectory, "database", "Generated");
        if (Directory.Exists(generatedDir))
        {
            _console.WriteLine("✓ database\\Generated\\ exists");
        }
        else
        {
            _console.WriteWarning("⚠ database\\Generated\\ does not exist (will be created on first generate)");
        }

        _console.WriteLine("");
        _console.WriteLine(allOk ? "Environment OK" : "Environment has issues");

        return allOk ? 0 : 1;
    }

    private bool CheckDirectory(string relativePath, string baseDirectory)
    {
        var fullPath = Path.Combine(baseDirectory, relativePath);
        if (Directory.Exists(fullPath))
        {
            _console.WriteLine($"✓ {relativePath}\\ exists");
            return true;
        }

        _console.WriteError($"✗ {relativePath}\\ not found");
        return false;
    }

    private bool CheckFile(string relativePath, string baseDirectory)
    {
        var fullPath = Path.Combine(baseDirectory, relativePath);
        if (File.Exists(fullPath))
        {
            _console.WriteLine($"✓ {relativePath} exists");
            return true;
        }

        _console.WriteError($"✗ {relativePath} not found");
        return false;
    }
}

