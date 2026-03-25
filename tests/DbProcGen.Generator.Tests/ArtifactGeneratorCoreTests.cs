using DbProcGen.Model;

namespace DbProcGen.Generator.Tests;

public class ArtifactGeneratorCoreTests
{
    [Test]
    public async Task Generate_ProducesManifestAndExpectedFiles()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "DbProcGen.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);

        try
        {
            var generator = new ArtifactGenerator();
            var spec = CreateSpec("TestProc", "default");

            var result = generator.Generate([spec], outputDir);

            await Assert.That(result.ManifestFile).IsNotNull();
            await Assert.That(result.GeneratedFiles.Any(f => f.EndsWith("dbo_TestProc.sql", StringComparison.Ordinal))).IsTrue();
            await Assert.That(result.GeneratedFiles.Any(f => f.EndsWith("dbo_TestProc_default.sql", StringComparison.Ordinal))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    [Test]
    public async Task Generate_IsDeterministicForSameSpec()
    {
        var outputDir1 = Path.Combine(Path.GetTempPath(), "DbProcGen.Generator.Tests", Guid.NewGuid().ToString("N"));
        var outputDir2 = Path.Combine(Path.GetTempPath(), "DbProcGen.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir1);
        Directory.CreateDirectory(outputDir2);

        try
        {
            var generator = new ArtifactGenerator();
            var spec = CreateSpec("DeterministicProc", "worker_a");

            var result1 = generator.Generate([spec], outputDir1);
            var result2 = generator.Generate([spec], outputDir2);

            var wrapper1 = await File.ReadAllTextAsync(result1.GeneratedFiles.First(f => f.EndsWith("dbo_DeterministicProc.sql", StringComparison.Ordinal)));
            var wrapper2 = await File.ReadAllTextAsync(result2.GeneratedFiles.First(f => f.EndsWith("dbo_DeterministicProc.sql", StringComparison.Ordinal)));
            var manifest1 = await File.ReadAllTextAsync(result1.ManifestFile!);
            var manifest2 = await File.ReadAllTextAsync(result2.ManifestFile!);

            await Assert.That(wrapper1).IsEqualTo(wrapper2);
            await Assert.That(manifest1).IsEqualTo(manifest2);
        }
        finally
        {
            if (Directory.Exists(outputDir1))
            {
                Directory.Delete(outputDir1, true);
            }

            if (Directory.Exists(outputDir2))
            {
                Directory.Delete(outputDir2, true);
            }
        }
    }

    private static DbProcSpec CreateSpec(string logicalName, string workerSuffix)
    {
        return new DbProcSpec(
            "1.0",
            logicalName,
            "dbo",
            logicalName,
            [],
            new DbProcResultContractSpec([
                new DbProcResultColumnSpec("Id", "int", false)
            ]),
            [],
            new DbProcRoutingRulesSpec([
                new DbProcRouteSpec("Default", [new DbProcRouteConditionSpec("Axis", "v")], workerSuffix)
            ], null),
            []);
    }
}
