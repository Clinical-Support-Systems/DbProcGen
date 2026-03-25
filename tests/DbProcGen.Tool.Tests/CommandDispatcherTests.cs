using TUnit.Core;

namespace DbProcGen.Tool.Tests;

public sealed class CommandDispatcherTests
{
    [Test]
    public async Task Dispatch_NoArgs_ShowsHelp_ReturnsZero()
    {
        var console = new TestConsoleWriter();
        var dispatcher = new CommandDispatcher(console);

        var exitCode = dispatcher.Dispatch([]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.Lines).Any(line => line.Contains("DbProcGen CLI"));
        await Assert.That(console.Lines).Any(line => line.Contains("validate"));
    }

    [Test]
    public async Task Dispatch_HelpCommand_ShowsHelp_ReturnsZero()
    {
        var console = new TestConsoleWriter();
        var dispatcher = new CommandDispatcher(console);

        var exitCode = dispatcher.Dispatch(["help"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.Lines).Any(line => line.Contains("DbProcGen CLI"));
    }

    [Test]
    public async Task Dispatch_UnknownCommand_ShowsError_ReturnsOne()
    {
        var console = new TestConsoleWriter();
        var dispatcher = new CommandDispatcher(console);

        var exitCode = dispatcher.Dispatch(["unknown"]);

        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(console.Errors).Any(error => error.Contains("Unknown command"));
    }

    [Test]
    public async Task Dispatch_DiffCommand_ExecutesCommand_ReturnsZero()
    {
        var console = new TestConsoleWriter();
        var dispatcher = new CommandDispatcher(console);

        var exitCode = dispatcher.Dispatch(["diff"]);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(console.Lines).Any(line => line.Contains("placeholder"));
    }

    [Test]
    public async Task Dispatch_DoctorCommand_ExecutesCommand()
    {
        var console = new TestConsoleWriter();
        var dispatcher = new CommandDispatcher(console);

        dispatcher.Dispatch(["doctor"]);

        await Assert.That(console.Lines).Any(line => line.Contains("Checking environment"));
    }
}
