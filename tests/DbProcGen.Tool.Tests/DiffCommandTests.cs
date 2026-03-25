using DbProcGen.Tool.Commands;
using DbProcGen.Tool.Services;
using TUnit.Core;

namespace DbProcGen.Tool.Tests;

public sealed class DiffCommandTests
{
    [Test]
    public async Task Execute_ShowsPlaceholderMessage()
    {
        var console = new TestConsoleWriter();
        var command = new DiffCommand(console);

        var exitCode = command.Execute([]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.Lines).Any(line => line.Contains("placeholder"));
        await Assert.That(console.Lines).Any(line => line.Contains("differences"));
    }

    [Test]
    public async Task Name_ReturnsDiff()
    {
        var console = new TestConsoleWriter();
        var command = new DiffCommand(console);

        await Assert.That(command.Name).IsEqualTo("diff");
    }
}
