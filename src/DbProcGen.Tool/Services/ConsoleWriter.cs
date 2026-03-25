namespace DbProcGen.Tool.Services;

/// <summary>
///     Default <see cref="IConsoleWriter" /> implementation that writes to <see cref="Console" />.
/// </summary>
public sealed class ConsoleWriter : IConsoleWriter
{
    /// <inheritdoc />
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    /// <inheritdoc />
    public void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    /// <inheritdoc />
    public void WriteWarning(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}
