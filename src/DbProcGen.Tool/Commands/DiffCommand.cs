using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Commands;

/// <summary>
///     Placeholder command that will show differences between current specs and generated artifacts.
/// </summary>
public sealed class DiffCommand : ICommand
{
    private readonly IConsoleWriter _console;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiffCommand" /> class.
    /// </summary>
    /// <param name="console">the console writer for output</param>
    public DiffCommand(IConsoleWriter console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc />
    public string Name => "diff";

    /// <inheritdoc />
    public string Description => "Show differences between current specs and generated artifacts (placeholder)";

    /// <inheritdoc />
    public int Execute(string[] args)
    {
        _console.WriteLine("diff: placeholder command");
        _console.WriteLine("Future: show differences between specs and generated SQL artifacts");
        return 0;
    }
}
