using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

public sealed class DiffCommand : ICommand
{
    private readonly IConsoleWriter _console;

    public DiffCommand(IConsoleWriter console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    public string Name => "diff";
    public string Description => "Show differences between current specs and generated artifacts (placeholder)";

    public int Execute(string[] args)
    {
        _console.WriteLine("diff: placeholder command");
        _console.WriteLine("Future: show differences between specs and generated SQL artifacts");
        return 0;
    }
}
