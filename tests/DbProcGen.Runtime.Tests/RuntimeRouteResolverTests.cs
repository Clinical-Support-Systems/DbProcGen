using System.Text.Json;
using DbProcGen.Generator;
using DbProcGen.Model;
using DbProcGen.Runtime;

namespace DbProcGen.Runtime.Tests;

public class RuntimeRouteResolverTests
{
    [Test]
    public async Task Resolve_ReturnsExpectedWorker_ForMatchingAxes()
    {
        var resolver = CreateResolverFromGeneratedManifest();

        var route = resolver.Resolve("GetUsersByFilter", new Dictionary<string, string>
        {
            ["FilterTypeAxis"] = "Name",
            ["PagingAxis"] = "true"
        });

        await Assert.That(route.Schema).IsEqualTo("dbo");
        await Assert.That(route.ProcedureName).IsEqualTo("GetUsersByFilter");
        await Assert.That(route.WorkerSuffix).IsEqualTo("name_paged");
        await Assert.That(route.FullyQualifiedWorkerName).IsEqualTo("[dbo].[GetUsersByFilter_name_paged]");
    }

    [Test]
    public async Task Resolve_Throws_WhenNoRouteMatches()
    {
        var resolver = CreateResolverFromGeneratedManifest();

        await Assert.That(async () =>
                _ = resolver.Resolve("GetUsersByFilter", new Dictionary<string, string>
                {
                    ["FilterTypeAxis"] = "Email",
                    ["PagingAxis"] = "true"
                }))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LoadFromManifestFile_LoadsResolverAndResolvesRoute()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DbProcGen.Runtime.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var generator = new ArtifactGenerator();
            var result = generator.Generate([CreateGetUsersByFilterSpec()], tempDir);

            var resolver = RuntimeRouteResolver.LoadFromManifestFile(result.ManifestFile!);
            var route = resolver.Resolve("GetUsersByFilter", new Dictionary<string, string>
            {
                ["FilterTypeAxis"] = "Email",
                ["PagingAxis"] = "false"
            });

            await Assert.That(route.FullyQualifiedWorkerName).IsEqualTo("[dbo].[GetUsersByFilter_email_unpaged]");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private static RuntimeRouteResolver CreateResolverFromGeneratedManifest()
    {
        var generator = new ArtifactGenerator();
        var outputDir = Path.Combine(Path.GetTempPath(), "DbProcGen.Runtime.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDir);

        var result = generator.Generate([CreateGetUsersByFilterSpec()], outputDir);
        var json = File.ReadAllText(result.ManifestFile!);
        var manifest = JsonSerializer.Deserialize<RuntimeGenerationManifest>(json);

        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        return new RuntimeRouteResolver(manifest!);
    }

    private static DbProcSpec CreateGetUsersByFilterSpec()
    {
        return new DbProcSpec(
            "1.0",
            "GetUsersByFilter",
            "dbo",
            "GetUsersByFilter",
            [
                new DbProcParameterSpec("FilterType", "nvarchar(32)", true),
                new DbProcParameterSpec("IsPaged", "bit", true),
                new DbProcParameterSpec("FilterValue", "nvarchar(512)", false),
                new DbProcParameterSpec("PageSize", "int", false),
                new DbProcParameterSpec("PageNumber", "int", false)
            ],
            new DbProcResultContractSpec([
                new DbProcResultColumnSpec("UserId", "int", false),
                new DbProcResultColumnSpec("DisplayName", "nvarchar(200)", false)
            ]),
            [
                new DbProcSpecializationAxisSpec("FilterTypeAxis", "FilterType", ["Name", "Email"]),
                new DbProcSpecializationAxisSpec("PagingAxis", "IsPaged", ["true", "false"])
            ],
            new DbProcRoutingRulesSpec([
                new DbProcRouteSpec("NamePaged", [
                    new DbProcRouteConditionSpec("FilterTypeAxis", "Name"),
                    new DbProcRouteConditionSpec("PagingAxis", "true")
                ], "name_paged"),
                new DbProcRouteSpec("EmailUnpaged", [
                    new DbProcRouteConditionSpec("FilterTypeAxis", "Email"),
                    new DbProcRouteConditionSpec("PagingAxis", "false")
                ], "email_unpaged")
            ], "NamePaged"),
            []);
    }
}
