using DbProcGen.Tool.Services;

namespace DbProcGen.Tool.Tests;

public sealed class TestConsoleWriter : IConsoleWriter
{
    private readonly List<string> _lines = [];
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    public IReadOnlyList<string> Lines => _lines;
    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Warnings => _warnings;

    public void WriteLine(string message)
    {
        _lines.Add(message);
    }

    public void WriteError(string message)
    {
        _errors.Add(message);
    }

    public void WriteWarning(string message)
    {
        _warnings.Add(message);
    }

    public void Clear()
    {
        _lines.Clear();
        _errors.Clear();
        _warnings.Clear();
    }
}
