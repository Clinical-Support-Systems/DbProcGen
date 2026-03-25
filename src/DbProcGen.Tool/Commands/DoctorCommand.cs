using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

public sealed class DoctorCommand : ICommand
{
    private readonly IConsoleWriter _console;

    public DoctorCommand(IConsoleWriter console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public string Name => "doctor";
    public string Description => "Check environment configuration and prerequisites";

    public int Execute(string[] args)
    {
        _console.WriteLine("Checking environment...");
        _console.WriteLine("");

        var allOk = true;
        var currentDirectory = Environment.CurrentDirectory;

        allOk &= CheckDirectory("specs", currentDirectory);
        allOk &= CheckDirectory("database", currentDirectory);
        allOk &= CheckFile("database\\DbProcGen.Database.sqlproj", currentDirectory);

        var generatedDir = Path.Combine(currentDirectory, "database", "Generated");
        if (Directory.Exists(generatedDir))
        {
            _console.WriteLine($"✓ database\\Generated\\ exists");
        }
        else
        {
            _console.WriteWarning($"⚠ database\\Generated\\ does not exist (will be created on first generate)");
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
        else
        {
            _console.WriteError($"✗ {relativePath}\\ not found");
            return false;
        }
    }

    private bool CheckFile(string relativePath, string baseDirectory)
    {
        var fullPath = Path.Combine(baseDirectory, relativePath);
        if (File.Exists(fullPath))
        {
            _console.WriteLine($"✓ {relativePath} exists");
            return true;
        }
        else
        {
            _console.WriteError($"✗ {relativePath} not found");
            return false;
        }
    }
}
