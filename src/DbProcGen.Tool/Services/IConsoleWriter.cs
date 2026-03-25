namespace DbProcGen.Tool.Services;

/// <summary>
///     Abstraction for writing console output with support for standard, error, and warning messages.
/// </summary>
public interface IConsoleWriter
{
    /// <summary>
    ///     Writes a standard message to the console.
    /// </summary>
    /// <param name="message">the message to write</param>
    void WriteLine(string message);

    /// <summary>
    ///     Writes an error message to the standard error stream.
    /// </summary>
    /// <param name="message">the error message to write</param>
    void WriteError(string message);

    /// <summary>
    ///     Writes a warning message to the console.
    /// </summary>
    /// <param name="message">the warning message to write</param>
    void WriteWarning(string message);
}
