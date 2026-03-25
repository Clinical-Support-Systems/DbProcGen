using DbProcGen.Tool.Commands;
using DbProcGen.Generator;
using DbProcGen.Tool.Services;

namespace DbProcGen.Tool;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICommand> _commands;
    private readonly IConsoleWriter _console;

    public CommandDispatcher(IConsoleWriter console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));

        var specLoader = new SpecLoader();
        var generator = new ArtifactGenerator();

        _commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["validate"] = new ValidateCommand(specLoader, console),
            ["generate"] = new GenerateCommand(specLoader, generator, console),
            ["diff"] = new DiffCommand(console),
            ["doctor"] = new DoctorCommand(console)
        };
    }

    public int Dispatch(string[] args)
    {
        if (args.Length == 0 || string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase) || args[0] == "--help" || args[0] == "-h")
        {
            ShowHelp();
            return 0;
        }

        var commandName = args[0].ToLowerInvariant();
        if (_commands.TryGetValue(commandName, out var command))
        {
            var commandArgs = args.Skip(1).ToArray();
            return command.Execute(commandArgs);
        }

        _console.WriteError($"Unknown command: {commandName}");
        _console.WriteLine("");
        ShowHelp();
        return 1;
    }

    private void ShowHelp()
    {
        _console.WriteLine("DbProcGen CLI v1.0");
        _console.WriteLine("Build-time generator for specialized stored procedures");
        _console.WriteLine("");
        _console.WriteLine("Usage: dbprocgen <command> [options]");
        _console.WriteLine("");
        _console.WriteLine("Commands:");

        foreach (var command in _commands.Values.OrderBy(c => c.Name))
        {
            _console.WriteLine($"  {command.Name,-12} {command.Description}");
        }

        _console.WriteLine("");
        _console.WriteLine("Run 'dbprocgen <command> --help' for command-specific help (future).");
    }
}
