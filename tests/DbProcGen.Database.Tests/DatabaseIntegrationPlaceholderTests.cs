using System.Diagnostics;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace DbProcGen.Database.Tests;

public class DatabaseIntegrationPlaceholderTests
{
    [Test]
    public async Task PlaceholderIntegration_SqlProjectBuild_Succeeds()
    {
        var repoRoot = FindRepoRoot();
        var sqlProjPath = Path.Combine(repoRoot, "database", "DbProcGen.Database.csproj");

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
        await Assert.That(wrapper).Contains("THROW 50001, 'No matching route for generated wrapper procedure.', 1;");

        await Assert.That(pagedWorker).Contains("AS [UserId]");
        await Assert.That(pagedWorker).Contains("AS [DisplayName]");
        await Assert.That(unpagedWorker).Contains("AS [UserId]");
        await Assert.That(unpagedWorker).Contains("AS [DisplayName]");
    }

    [Test]
    public async Task PlaceholderIntegration_WorkerBodies_AreSpecDriven_NotPlaceholder()
    {
        var repoRoot = FindRepoRoot();
        var generatedDir = Path.Combine(repoRoot, "database", "Generated");

        var pagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_name_paged.sql");
        var unpagedWorkerPath = Path.Combine(generatedDir, "dbo_GetUsersByFilter_email_unpaged.sql");
        var pagedWorker = await File.ReadAllTextAsync(pagedWorkerPath);
        var unpagedWorker = await File.ReadAllTextAsync(unpagedWorkerPath);

        await Assert.That(pagedWorker.Contains("WHERE 1 = 0", StringComparison.Ordinal)).IsFalse();
        await Assert.That(unpagedWorker.Contains("WHERE 1 = 0", StringComparison.Ordinal)).IsFalse();
        await Assert.That(pagedWorker).Contains("OFFSET @Offset ROWS");
        await Assert.That(unpagedWorker).Contains("u.[Email] = @FilterValue");
    }

    [Test]
    public async Task ExecutionIntegration_RuntimeRouteAndUnmatched_AreEnforcedBySqlServer()
    {
        var enableExecutionIntegration = string.Equals(
            Environment.GetEnvironmentVariable("DBPROCGEN_ENABLE_TESTCONTAINERS_SQL"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!enableExecutionIntegration)
        {
            return;
        }

        await using var container = await StartSqlContainerAsync();
        await using var connection = new SqlConnection(container.GetConnectionString());
        await connection.OpenAsync();

        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = """
                IF OBJECT_ID('[dbo].[Users]', 'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Users]
                    (
                        [UserId] BIGINT NOT NULL PRIMARY KEY,
                        [UserName] NVARCHAR(256) NOT NULL,
                        [Email] NVARCHAR(512) NULL
                    );
                END;
                """;
            await setup.ExecuteNonQueryAsync();
        }

        await using (var clear = connection.CreateCommand())
        {
            clear.CommandText = "DELETE FROM [dbo].[Users];";
            await clear.ExecuteNonQueryAsync();
        }

        await using (var seed = connection.CreateCommand())
        {
            seed.CommandText = """
                INSERT INTO [dbo].[Users] ([UserId], [UserName], [Email])
                VALUES (1, N'Alice', N'alice@example.com'),
                       (2, N'Bob', N'bob@example.com');
                """;
            await seed.ExecuteNonQueryAsync();
        }

        var repoRoot = FindRepoRoot();
        var generatedDir = Path.Combine(repoRoot, "database", "Generated");
        await ExecuteGeneratedSqlProcedureAsync(connection, Path.Combine(generatedDir, "dbo_GetUsersByFilter_name_paged.sql"));
        await ExecuteGeneratedSqlProcedureAsync(connection, Path.Combine(generatedDir, "dbo_GetUsersByFilter_email_unpaged.sql"));
        await ExecuteGeneratedSqlProcedureAsync(connection, Path.Combine(generatedDir, "dbo_GetUsersByFilter.sql"));

        await using (var matched = connection.CreateCommand())
        {
            matched.CommandText = """
                EXEC [dbo].[GetUsersByFilter]
                    @FilterType = N'Email',
                    @IsPaged = 0,
                    @FilterValue = N'alice@example.com',
                    @PageSize = NULL,
                    @PageNumber = NULL;
                """;
            await using var reader = await matched.ExecuteReaderAsync();
            await Assert.That(await reader.ReadAsync()).IsTrue();
            await Assert.That(reader.GetInt32(0)).IsEqualTo(1);
            await Assert.That(reader.GetString(1)).IsEqualTo("alice@example.com");
        }

        await Assert.That(async () =>
            {
                await using var unmatched = connection.CreateCommand();
                unmatched.CommandText = """
                    EXEC [dbo].[GetUsersByFilter]
                        @FilterType = N'Email',
                        @IsPaged = 1,
                        @FilterValue = N'alice@example.com',
                        @PageSize = 10,
                        @PageNumber = 1;
                    """;
                await unmatched.ExecuteNonQueryAsync();
            })
            .Throws<SqlException>();
    }

    private static async Task<MsSqlContainer> StartSqlContainerAsync()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithCleanUp(true)
            .Build();

        await container.StartAsync();
        return container;
    }

    private static async Task ExecuteGeneratedSqlProcedureAsync(SqlConnection connection, string path)
    {
        var sql = await File.ReadAllTextAsync(path);
        var normalized = sql.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
        var body = normalized.Replace("\nGO\n", "\n", StringComparison.Ordinal);

        await using var command = connection.CreateCommand();
        command.CommandText = body;
        await command.ExecuteNonQueryAsync();
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

