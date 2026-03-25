using System.Diagnostics;

namespace DbProcGen.Database.Tests;

public class DatabaseIntegrationPlaceholderTests
{
    [Test]
    public async Task PlaceholderIntegration_SqlProjectBuild_Succeeds()
    {
        var repoRoot = FindRepoRoot();
        var sqlProjPath = Path.Combine(repoRoot, "database", "DbProcGen.Database.sqlproj");

        var result = RunProcess(
            "dotnet",
            $"build \"{sqlProjPath}\" --nologo",
            repoRoot);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Output).Contains("Build succeeded.");
    }

    [Test]
    public async Task PlaceholderIntegration_WrapperContractShape_IsPresent()
    {
        var repoRoot = FindRepoRoot();
        var generatedDir = Path.Combine(repoRoot, "database", "Generated");

        var wrapperPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter.sql");
        var pagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_name_paged.sql");
        var unpagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_email_unpaged.sql");

        var wrapper = await File.ReadAllTextAsync(wrapperPath);
        var pagedWorker = await File.ReadAllTextAsync(pagedWorkerPath);
        var unpagedWorker = await File.ReadAllTextAsync(unpagedWorkerPath);

        await Assert.That(wrapper).Contains("CREATE PROCEDURE [dbo].[GetUsersByFilter]");
        await Assert.That(wrapper).Contains("@FilterType nvarchar(32)");
        await Assert.That(wrapper).Contains("@IsPaged bit");
        await Assert.That(wrapper).Contains("@FilterValue nvarchar(512)");
        await Assert.That(wrapper).Contains("EXEC [dbo].[GetUsersByFilter_name_paged]");
        await Assert.That(wrapper).Contains("EXEC [dbo].[GetUsersByFilter_email_unpaged]");

        await Assert.That(pagedWorker).Contains("AS [UserId]");
        await Assert.That(pagedWorker).Contains("AS [DisplayName]");
        await Assert.That(unpagedWorker).Contains("AS [UserId]");
        await Assert.That(unpagedWorker).Contains("AS [DisplayName]");
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "DbProcGen.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private static (int ExitCode, string Output) RunProcess(string fileName, string arguments, string workingDirectory)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, standardOutput + standardError);
    }
}
