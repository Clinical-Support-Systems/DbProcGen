namespace DbProcGen.Tool.Commands;

/// <summary>
///     A CLI subcommand that can be dispatched by the <see cref="CommandDispatcher" />.
/// </summary>
public interface ICommand
{
    /// <summary>
    ///     The command name used on the command line (e.g. `"validate"`, `"generate"`).
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     A short description displayed in help output.
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     Executes the command with the given arguments.
    /// </summary>
    /// <param name="args">the command-line arguments following the command name</param>
    /// <returns>`0` on success; a non-zero exit code on failure.</returns>
    int Execute(string[] args);
}
