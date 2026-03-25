namespace DbProcGen.Tool.Services;

public interface IConsoleWriter
{
    void WriteLine(string message);
    void WriteError(string message);
    void WriteWarning(string message);
}
