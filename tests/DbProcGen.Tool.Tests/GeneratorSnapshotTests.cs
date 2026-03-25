using DbProcGen.Model;
using DbProcGen.Generator;
using TUnit.Core;

namespace DbProcGen.Tool.Tests;

public sealed class GeneratorSnapshotTests
{
    [Test]
    public async Task WrapperOutput_MatchesSnapshot()
    {
        var spec = CreateTestSpec(
            "UserSearch",
            "dbo",
            [
                new DbProcParameterSpec("SearchTerm", "nvarchar(100)", true),
                new DbProcParameterSpec("IncludeInactive", "bit", true)
            ],
            [
                new DbProcResultColumnSpec("UserId", "int", false),
                new DbProcResultColumnSpec("Email", "nvarchar(256)", false),
                new DbProcResultColumnSpec("IsActive", "bit", false)
            ],
            [
                new DbProcRouteSpec("ActiveUsers", [
                    new DbProcRouteConditionSpec("StatusAxis", "Active")
                ], "active"),
                new DbProcRouteSpec("AllUsers", [
                    new DbProcRouteConditionSpec("StatusAxis", "All")
                ], "all")
            ]);

        var generator = new ArtifactGenerator();
        var tempDir = CreateTempTestDirectory();
        
        try
        {
            var result = generator.Generate([spec], tempDir);

            var wrapperFile = result.GeneratedFiles.First(f => f.EndsWith("dbo_UserSearch.sql"));
            var wrapperContent = File.ReadAllText(wrapperFile);

            // Normalize line endings for cross-platform determinism
            var normalized = NormalizeForSnapshot(wrapperContent);
            
            await Verify(normalized)
                .UseDirectory("Snapshots")
                .UseMethodName("WrapperOutput");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Test]
    public async Task WorkerOutput_MatchesSnapshot()
    {
        var spec = CreateTestSpec(
            "UserSearch",
            "dbo",
            [
                new DbProcParameterSpec("SearchTerm", "nvarchar(100)", true),
                new DbProcParameterSpec("IncludeInactive", "bit", true)
            ],
            [
                new DbProcResultColumnSpec("UserId", "int", false),
                new DbProcResultColumnSpec("Email", "nvarchar(256)", false),
                new DbProcResultColumnSpec("IsActive", "bit", false)
            ],
            [
                new DbProcRouteSpec("ActiveUsers", [
                    new DbProcRouteConditionSpec("StatusAxis", "Active")
                ], "active"),
                new DbProcRouteSpec("AllUsers", [
                    new DbProcRouteConditionSpec("StatusAxis", "All")
                ], "all")
            ]);

        var generator = new ArtifactGenerator();
        var tempDir = CreateTempTestDirectory();
        
        try
        {
            var result = generator.Generate([spec], tempDir);

            var workerFiles = result.GeneratedFiles
                .Where(f => f.Contains("_active.sql") || f.Contains("_all.sql"))
                .OrderBy(f => f)
                .ToList();

            var workers = new Dictionary<string, string>();
            foreach (var workerFile in workerFiles)
            {
                var fileName = Path.GetFileName(workerFile);
                var content = File.ReadAllText(workerFile);
                workers[fileName] = NormalizeForSnapshot(content);
            }

            await Verify(workers)
                .UseDirectory("Snapshots")
                .UseMethodName("WorkerOutput");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Test]
    public async Task GeneratedOutput_IsDeterministic()
    {
        var spec = CreateTestSpec(
            "OrderQuery",
            "dbo",
            [
                new DbProcParameterSpec("CustomerId", "int", true),
                new DbProcParameterSpec("Status", "nvarchar(50)", true)
            ],
            [
                new DbProcResultColumnSpec("OrderId", "int", false),
                new DbProcResultColumnSpec("OrderDate", "datetime", false)
            ],
            [
                new DbProcRouteSpec("PendingOrders", [
                    new DbProcRouteConditionSpec("StatusAxis", "Pending")
                ], "pending"),
                new DbProcRouteSpec("CompletedOrders", [
                    new DbProcRouteConditionSpec("StatusAxis", "Completed")
                ], "completed")
            ]);

        var generator = new ArtifactGenerator();
        
        // Generate multiple times in different directories
        var run1Dir = CreateTempTestDirectory();
        var run2Dir = CreateTempTestDirectory();
        var run3Dir = CreateTempTestDirectory();
        
        try
        {
            var result1 = generator.Generate([spec], run1Dir);
            var result2 = generator.Generate([spec], run2Dir);
            var result3 = generator.Generate([spec], run3Dir);

            // Verify file counts are identical
            await Assert.That(result1.GeneratedFiles.Count).IsEqualTo(result2.GeneratedFiles.Count);
            await Assert.That(result2.GeneratedFiles.Count).IsEqualTo(result3.GeneratedFiles.Count);

            // Verify file names are identical (sorted order)
            var names1 = result1.GeneratedFiles.Select(Path.GetFileName).OrderBy(n => n).ToArray();
            var names2 = result2.GeneratedFiles.Select(Path.GetFileName).OrderBy(n => n).ToArray();
            var names3 = result3.GeneratedFiles.Select(Path.GetFileName).OrderBy(n => n).ToArray();

            await Assert.That(names1).IsEquivalentTo(names2);
            await Assert.That(names2).IsEquivalentTo(names3);

            // Verify content is identical byte-for-byte across all runs
            for (int i = 0; i < result1.GeneratedFiles.Count; i++)
            {
                var content1 = File.ReadAllText(result1.GeneratedFiles[i]);
                var content2 = File.ReadAllText(result2.GeneratedFiles[i]);
                var content3 = File.ReadAllText(result3.GeneratedFiles[i]);

                var fileName = Path.GetFileName(result1.GeneratedFiles[i]);
                await Assert.That(content1).IsEqualTo(content2);
                await Assert.That(content2).IsEqualTo(content3);
            }
        }
        finally
        {
            CleanupTempDirectory(run1Dir);
            CleanupTempDirectory(run2Dir);
            CleanupTempDirectory(run3Dir);
        }
    }

    [Test]
    public async Task MultipleSpecs_ProduceOrderedOutput()
    {
        var specs = new[]
        {
            CreateSimpleSpec("ZebraProc", "zebra_worker"),
            CreateSimpleSpec("AlphaProc", "alpha_worker"),
            CreateSimpleSpec("BravoProc", "bravo_worker"),
            CreateSimpleSpec("CharlieProc", "charlie_worker")
        };

        var generator = new ArtifactGenerator();
        var tempDir = CreateTempTestDirectory();
        
        try
        {
            var result = generator.Generate(specs, tempDir);

            var fileNames = result.GeneratedFiles
                .Select(Path.GetFileName)
                .ToList();

            await Verify(fileNames)
                .UseDirectory("Snapshots")
                .UseMethodName("MultipleSpecs_FileOrder");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Test]
    public async Task GeneratedFiles_ContainAutoGeneratedHeaders()
    {
        var spec = CreateSimpleSpec("HeaderTest", "default");

        var generator = new ArtifactGenerator();
        var tempDir = CreateTempTestDirectory();
        
        try
        {
            var result = generator.Generate([spec], tempDir);

            // Only check SQL files for auto-generated headers (skip manifest)
            var sqlFiles = result.GeneratedFiles.Where(f => f.EndsWith(".sql"));
            foreach (var file in sqlFiles)
            {
                var content = File.ReadAllText(file);
                await Assert.That(content).Contains("<auto-generated>");
                await Assert.That(content).Contains("This file was generated by DbProcGen");
            }
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    [Test]
    public async Task ComplexSpec_MatchesFullSnapshot()
    {
        var spec = CreateTestSpec(
            "GetUsersByFilter",
            "dbo",
            [
                new DbProcParameterSpec("FilterType", "nvarchar(32)", true),
                new DbProcParameterSpec("IsPaged", "bit", true)
            ],
            [
                new DbProcResultColumnSpec("UserId", "int", false),
                new DbProcResultColumnSpec("DisplayName", "nvarchar(200)", false)
            ],
            [
                new DbProcRouteSpec("NamePaged", [
                    new DbProcRouteConditionSpec("FilterTypeAxis", "Name"),
                    new DbProcRouteConditionSpec("PagingAxis", "true")
                ], "name_paged"),
                new DbProcRouteSpec("EmailUnpaged", [
                    new DbProcRouteConditionSpec("FilterTypeAxis", "Email"),
                    new DbProcRouteConditionSpec("PagingAxis", "false")
                ], "email_unpaged")
            ]);

        var generator = new ArtifactGenerator();
        var tempDir = CreateTempTestDirectory();
        
        try
        {
            var result = generator.Generate([spec], tempDir);

            var allFiles = new Dictionary<string, string>();
            foreach (var file in result.GeneratedFiles.OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                var content = File.ReadAllText(file);
                allFiles[fileName] = NormalizeForSnapshot(content);
            }

            await Verify(allFiles)
                .UseDirectory("Snapshots")
                .UseMethodName("ComplexSpec_AllFiles");
        }
        finally
        {
            CleanupTempDirectory(tempDir);
        }
    }

    private static DbProcSpec CreateTestSpec(
        string logicalName,
        string schema,
        DbProcParameterSpec[] parameters,
        DbProcResultColumnSpec[] resultColumns,
        DbProcRouteSpec[] routes)
    {
        return new DbProcSpec(
            "1.0",
            logicalName,
            schema,
            logicalName,
            parameters,
            new DbProcResultContractSpec(resultColumns),
            [],
            new DbProcRoutingRulesSpec(routes, null),
            []);
    }

    private static DbProcSpec CreateSimpleSpec(string logicalName, string workerSuffix)
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
                new DbProcRouteSpec("Default", [], workerSuffix)
            ], null),
            []);
    }

    private static string CreateTempTestDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DbProcGen.Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private static void CleanupTempDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private static string NormalizeForSnapshot(string content)
    {
        // Normalize line endings to LF for cross-platform consistency
        return content.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
