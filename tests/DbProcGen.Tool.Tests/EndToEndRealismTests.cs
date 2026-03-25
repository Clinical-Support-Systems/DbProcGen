using System.Text.Json;
using TUnit.Core;

namespace DbProcGen.Tool.Tests;

public sealed class EndToEndRealismTests
{
    [Test]
    public async Task GetUsersByFilter_ProofArtifacts_StayRealisticAndDeterministic()
    {
        var repoRoot = FindRepoRoot();
        var generatedDir = Path.Combine(repoRoot, "database", "Generated");

        var wrapperPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter.sql");
        var pagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_name_paged.sql");
        var unpagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_email_unpaged.sql");
        var manifestPath = Path.Combine(generatedDir, "generation-manifest.json");

        var wrapper = NormalizeForSnapshot(File.ReadAllText(wrapperPath));
        var pagedWorker = NormalizeForSnapshot(File.ReadAllText(pagedWorkerPath));
        var unpagedWorker = NormalizeForSnapshot(File.ReadAllText(unpagedWorkerPath));
        var manifest = NormalizeForSnapshot(File.ReadAllText(manifestPath));

        // 1) Wrapper includes concrete route dispatch to distinct workers.
        await Assert.That(wrapper).Contains("IF @FilterType = 'Name' AND @IsPaged = 1");
        await Assert.That(wrapper).Contains("ELSE IF @FilterType = 'Email' AND @IsPaged = 0");
        await Assert.That(wrapper).Contains("EXEC [dbo].[GetUsersByFilter_name_paged]");
        await Assert.That(wrapper).Contains("EXEC [dbo].[GetUsersByFilter_email_unpaged]");
        await Assert.That(wrapper).Contains("THROW 50001, 'No matching route for generated wrapper procedure.', 1;");

        // 2) Paged and unpaged workers differ materially.
        await Assert.That(pagedWorker != unpagedWorker).IsTrue();
        await Assert.That(pagedWorker).Contains("OFFSET @Offset ROWS");
        await Assert.That(pagedWorker).Contains("FETCH NEXT @PageSize ROWS ONLY");
        await Assert.That(unpagedWorker.Contains("OFFSET @Offset ROWS", StringComparison.Ordinal)).IsFalse();
        await Assert.That(unpagedWorker.Contains("FETCH NEXT @PageSize ROWS ONLY", StringComparison.Ordinal)).IsFalse();
        await Assert.That(pagedWorker).Contains("u.[UserName] LIKE @FilterValue + '%'");
        await Assert.That(unpagedWorker).Contains("u.[Email] = @FilterValue");

        // 3) Generated SQL references expected hand-authored schema objects.
        await Assert.That(pagedWorker).Contains("FROM [dbo].[Users]");
        await Assert.That(unpagedWorker).Contains("FROM [dbo].[Users]");

        // 4) Determinism properties still hold (ordering/naming/manifest stability).
        var familyFileNames = Directory.GetFiles(generatedDir, "dbo_GetUsersByFilter*.sql")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        await Assert.That(string.Join(",", familyFileNames!))
            .IsEqualTo("dbo_GetUsersByFilter.sql,dbo_GetUsersByFilter_email_unpaged.sql,dbo_GetUsersByFilter_name_paged.sql");

        using var manifestDoc = JsonDocument.Parse(manifest);
        var family = manifestDoc.RootElement.GetProperty("families")[0];
        var workerSuffixes = family.GetProperty("workers")
            .EnumerateArray()
            .Select(worker => worker.GetProperty("workerSuffix").GetString())
            .ToArray();

        await Assert.That(manifestDoc.RootElement.GetProperty("generatedAt").GetString()).IsEqualTo("generation-manifest");
        await Assert.That(family.GetProperty("wrapperFile").GetString()).IsEqualTo("dbo_GetUsersByFilter.sql");
        await Assert.That(string.Join(",", workerSuffixes!)).IsEqualTo("email_unpaged,name_paged");

        // 5) Manifest/runtime semantics remain aligned on explicit unmatched-route failure.
        var resolver = DbProcGen.Runtime.RuntimeRouteResolver.LoadFromManifestFile(manifestPath);
        await Assert.That(async () =>
                _ = resolver.Resolve("GetUsersByFilter", new Dictionary<string, string>
                {
                    ["FilterTypeAxis"] = "Email",
                    ["PagingAxis"] = "true"
                }))
            .Throws<InvalidOperationException>();

        await Verify(new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["dbo_GetUsersByFilter.sql"] = wrapper,
                ["dbo_GetUsersByFilter_email_unpaged.sql"] = unpagedWorker,
                ["dbo_GetUsersByFilter_name_paged.sql"] = pagedWorker,
                ["generation-manifest.json"] = manifest
            })
            .UseDirectory("Snapshots")
            .UseMethodName("GetUsersByFilter_RealismProof");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = current.FullName;
            if (File.Exists(Path.Combine(candidate, "DbProcGen.slnx")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static string NormalizeForSnapshot(string content) =>
        content.Replace("\r\n", "\n").Replace("\r", "\n");
}
