using System.Text.Json;

namespace DbProcGen.Runtime;

/// <summary>
///     Resolves axis values to the correct worker procedure using a deserialized generation manifest.
/// </summary>
public sealed class RuntimeRouteResolver
{
    private readonly IReadOnlyDictionary<string, RuntimeProcedureFamily> _families;

    /// <summary>
    ///     Initializes a new instance from a deserialized manifest.
    /// </summary>
    /// <param name="manifest">the generation manifest to resolve routes against</param>
    /// <exception cref="ArgumentNullException"><paramref name="manifest" /> is `null`.</exception>
    public RuntimeRouteResolver(RuntimeGenerationManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        _families = manifest.Families.ToDictionary(
            f => f.LogicalName,
            StringComparer.Ordinal);
    }

    /// <summary>
    ///     Loads a manifest JSON file from disk and creates a <see cref="RuntimeRouteResolver" />.
    /// </summary>
    /// <param name="manifestPath">the absolute or relative path to the manifest JSON file</param>
    /// <returns>A <see cref="RuntimeRouteResolver" /> backed by the loaded manifest.</returns>
    /// <exception cref="ArgumentException"><paramref name="manifestPath" /> is `null` or whitespace.</exception>
    /// <exception cref="InvalidOperationException">The manifest file could not be deserialized.</exception>
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

    /// <summary>
    ///     Resolves the worker procedure for the given logical procedure name and axis values.
    /// </summary>
    /// <param name="logicalName">the logical procedure family name</param>
    /// <param name="axisValues">a dictionary mapping axis names to their current values</param>
    /// <returns>The matching <see cref="WorkerRoute" />.</returns>
    /// <exception cref="ArgumentException"><paramref name="logicalName" /> is `null` or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="axisValues" /> is `null`.</exception>
    /// <exception cref="InvalidOperationException">No family or matching worker route was found.</exception>
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
