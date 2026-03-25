using System.Text.Json;

namespace DbProcGen.Runtime;

public sealed class RuntimeRouteResolver
{
    private readonly IReadOnlyDictionary<string, RuntimeProcedureFamily> _families;

    public RuntimeRouteResolver(RuntimeGenerationManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _families = manifest.Families.ToDictionary(
            f => f.LogicalName,
            StringComparer.Ordinal);
    }

    public static RuntimeRouteResolver LoadFromManifestFile(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<RuntimeGenerationManifest>(json);
        if (manifest is null)
        {
            throw new InvalidOperationException($"Unable to deserialize manifest at '{manifestPath}'.");
        }

        return new RuntimeRouteResolver(manifest);
    }

    public WorkerRoute Resolve(string logicalName, IReadOnlyDictionary<string, string> axisValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logicalName);
        ArgumentNullException.ThrowIfNull(axisValues);

        if (!_families.TryGetValue(logicalName, out var family))
        {
            throw new InvalidOperationException($"No generated family found for logical procedure '{logicalName}'.");
        }

        var match = family.Workers.FirstOrDefault(worker =>
            worker.Conditions.All(condition =>
                axisValues.TryGetValue(condition.Axis, out var value) &&
                string.Equals(value, condition.Value, StringComparison.Ordinal)));

        if (match is null)
        {
            var knownAxes = string.Join(", ", axisValues.OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={kvp.Value}"));
            throw new InvalidOperationException(
                $"No worker route matched logical procedure '{logicalName}' for axis values: {knownAxes}.");
        }

        return new WorkerRoute(family.Schema, family.PublicProcedure, match.WorkerSuffix);
    }
}
