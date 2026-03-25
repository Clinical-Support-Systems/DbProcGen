namespace DbProcGen.Tool.Services;

public sealed class ConsoleWriter : IConsoleWriter
{
    public void WriteLine(string message)
    {
        Console.WriteLine(message);
    }

    public void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    public void WriteWarning(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}
