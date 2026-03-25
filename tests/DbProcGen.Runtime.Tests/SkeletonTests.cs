namespace DbProcGen.Runtime.Tests;

public class SkeletonTests
{
    [Test]
    public async Task Placeholder()
    {
        await Assert.That(true).IsTrue();
    }
}