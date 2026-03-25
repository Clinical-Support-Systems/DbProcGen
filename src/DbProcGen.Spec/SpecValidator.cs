using System.Text.RegularExpressions;
using DbProcGen.Model;

namespace DbProcGen.Spec;

/// <summary>
///     Validates a parsed <see cref="DbProcSpec" /> against semantic rules, producing diagnostics
///     for constraint violations such as invalid identifiers, missing references, and duplicates.
/// </summary>
/// <remarks>
///     Diagnostic codes emitted by the validator range from `DBPROC100` through `DBPROC161`.
/// </remarks>
public static partial class SpecValidator
{
    private static readonly Regex IdentifierRegex = SqlIdentifierRegex();

    /// <summary>
    ///     Validates the given spec and returns any semantic diagnostics, deterministically ordered.
    /// </summary>
    /// <param name="spec">the parsed spec to validate</param>
    /// <returns>A deterministically ordered list of validation diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="spec" /> is `null`.</exception>
    public static IReadOnlyList<SpecDiagnostic> Validate(DbProcSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var diagnostics = new List<SpecDiagnostic>();

        if (!string.Equals(spec.Version, "1.0", StringComparison.Ordinal))
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC101",
                "$.version",
                SpecDiagnosticSeverity.Error,
                "Only JSON spec version '1.0' is supported in v1."));
        }

        ValidateIdentifier(spec.LogicalName, "$.logicalName", diagnostics);
        ValidateIdentifier(spec.Schema, "$.schema", diagnostics);
        ValidateIdentifier(spec.PublicProcedure, "$.publicProcedure", diagnostics);

        ValidateParameterRules(spec.Parameters, diagnostics);
        ValidateResultContractRules(spec.ResultContract, diagnostics);
        ValidateAxisRules(spec.SpecializationAxes, spec.Parameters, diagnostics);
        ValidateRoutingRules(spec.RoutingRules, spec.SpecializationAxes, diagnostics);
        ValidateFragmentRules(spec.Fragments, diagnostics);

        return diagnostics.OrderBy(static d => d, SpecDiagnostic.DeterministicComparer).ToArray();
    }

    private static void ValidateParameterRules(
        IReadOnlyList<DbProcParameterSpec> parameters,
        List<SpecDiagnostic> diagnostics)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            ValidateIdentifier(parameter.Name, $"$.parameters[{i}].name", diagnostics);

            if (string.IsNullOrWhiteSpace(parameter.SqlType))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC102",
                    $"$.parameters[{i}].sqlType",
                    SpecDiagnosticSeverity.Error,
                    "Parameter sqlType must not be empty."));
            }
        }

        AddDuplicateNameDiagnostics(
            parameters.Select(static p => p.Name),
            "$.parameters",
            "DBPROC120",
            "Parameter names must be unique.",
            diagnostics);
    }

    private static void ValidateResultContractRules(
        DbProcResultContractSpec resultContract,
        List<SpecDiagnostic> diagnostics)
    {
        if (resultContract.Columns.Count == 0)
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC130",
                "$.resultContract.columns",
                SpecDiagnosticSeverity.Error,
                "At least one resultContract column is required."));
        }

        for (var i = 0; i < resultContract.Columns.Count; i++)
        {
            var column = resultContract.Columns[i];
            ValidateIdentifier(column.Name, $"$.resultContract.columns[{i}].name", diagnostics);

            if (string.IsNullOrWhiteSpace(column.SqlType))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC131",
                    $"$.resultContract.columns[{i}].sqlType",
                    SpecDiagnosticSeverity.Error,
                    "Result column sqlType must not be empty."));
            }
        }

        AddDuplicateNameDiagnostics(
            resultContract.Columns.Select(static c => c.Name),
            "$.resultContract.columns",
            "DBPROC132",
            "Result column names must be unique.",
            diagnostics);
    }

    private static void ValidateAxisRules(
        IReadOnlyList<DbProcSpecializationAxisSpec> axes,
        IReadOnlyList<DbProcParameterSpec> parameters,
        List<SpecDiagnostic> diagnostics)
    {
        var parameterNames = parameters.Select(static p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < axes.Count; i++)
        {
            var axis = axes[i];
            ValidateIdentifier(axis.Name, $"$.specializationAxes[{i}].name", diagnostics);
            ValidateIdentifier(axis.Parameter, $"$.specializationAxes[{i}].parameter", diagnostics);

            if (!parameterNames.Contains(axis.Parameter))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC140",
                    $"$.specializationAxes[{i}].parameter",
                    SpecDiagnosticSeverity.Error,
                    $"Axis parameter '{axis.Parameter}' must reference a declared parameter."));
            }

            if (axis.Values.Count == 0)
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC141",
                    $"$.specializationAxes[{i}].values",
                    SpecDiagnosticSeverity.Error,
                    "Axis values must contain at least one item."));
            }

            AddDuplicateNameDiagnostics(
                axis.Values,
                $"$.specializationAxes[{i}].values",
                "DBPROC142",
                "Axis values must be unique.",
                diagnostics);
        }

        AddDuplicateNameDiagnostics(
            axes.Select(static a => a.Name),
            "$.specializationAxes",
            "DBPROC143",
            "Axis names must be unique.",
            diagnostics);
    }

    private static void ValidateRoutingRules(
        DbProcRoutingRulesSpec routingRules,
        IReadOnlyList<DbProcSpecializationAxisSpec> axes,
        List<SpecDiagnostic> diagnostics)
    {
        if (routingRules.Routes.Count == 0)
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC150",
                "$.routingRules.routes",
                SpecDiagnosticSeverity.Error,
                "At least one route is required."));
        }

        var axisNames = axes.Select(static a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var routeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var workerSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < routingRules.Routes.Count; i++)
        {
            var route = routingRules.Routes[i];
            var routePath = $"$.routingRules.routes[{i}]";

            ValidateIdentifier(route.Name, $"{routePath}.name", diagnostics);
            ValidateIdentifier(route.WorkerSuffix, $"{routePath}.workerSuffix", diagnostics);

            if (!routeNames.Add(route.Name))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC151",
                    $"{routePath}.name",
                    SpecDiagnosticSeverity.Error,
                    $"Route name '{route.Name}' is duplicated."));
            }

            if (!workerSuffixes.Add(route.WorkerSuffix))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC152",
                    $"{routePath}.workerSuffix",
                    SpecDiagnosticSeverity.Error,
                    $"Route workerSuffix '{route.WorkerSuffix}' is duplicated."));
            }

            if (route.When.Count == 0)
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC153",
                    $"{routePath}.when",
                    SpecDiagnosticSeverity.Error,
                    "Route must include at least one condition."));
            }

            var routeAxisNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var conditionIndex = 0; conditionIndex < route.When.Count; conditionIndex++)
            {
                var condition = route.When[conditionIndex];
                var conditionPath = $"{routePath}.when[{conditionIndex}]";

                ValidateIdentifier(condition.Axis, $"{conditionPath}.axis", diagnostics);

                if (string.IsNullOrWhiteSpace(condition.MatchValue))
                {
                    diagnostics.Add(new SpecDiagnostic(
                        "DBPROC154",
                        $"{conditionPath}.equals",
                        SpecDiagnosticSeverity.Error,
                        "Route condition equals must not be empty."));
                }

                if (!axisNames.Contains(condition.Axis))
                {
                    diagnostics.Add(new SpecDiagnostic(
                        "DBPROC155",
                        $"{conditionPath}.axis",
                        SpecDiagnosticSeverity.Error,
                        $"Route axis '{condition.Axis}' must reference a declared specialization axis."));
                }

                if (!routeAxisNames.Add(condition.Axis))
                {
                    diagnostics.Add(new SpecDiagnostic(
                        "DBPROC156",
                        $"{conditionPath}.axis",
                        SpecDiagnosticSeverity.Error,
                        $"Route condition axis '{condition.Axis}' is duplicated within route '{route.Name}'."));
                }
            }
        }

        if (routingRules.DefaultRoute is not null && !routeNames.Contains(routingRules.DefaultRoute))
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC157",
                "$.routingRules.defaultRoute",
                SpecDiagnosticSeverity.Error,
                $"Default route '{routingRules.DefaultRoute}' must match a declared route name."));
        }
    }

    private static void ValidateFragmentRules(
        IReadOnlyList<DbProcFragmentSpec> fragments,
        List<SpecDiagnostic> diagnostics)
    {
        for (var i = 0; i < fragments.Count; i++)
        {
            var fragment = fragments[i];
            var fragmentPath = $"$.fragments[{i}]";

            ValidateIdentifier(fragment.Name, $"{fragmentPath}.name", diagnostics);
            ValidateIdentifier(fragment.Kind, $"{fragmentPath}.kind", diagnostics);

            if (string.IsNullOrWhiteSpace(fragment.Content))
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC160",
                    $"{fragmentPath}.content",
                    SpecDiagnosticSeverity.Error,
                    "Fragment content must not be empty."));
            }
        }

        AddDuplicateNameDiagnostics(
            fragments.Select(static f => f.Name),
            "$.fragments",
            "DBPROC161",
            "Fragment names must be unique.",
            diagnostics);
    }

    private static void ValidateIdentifier(string value, string path, List<SpecDiagnostic> diagnostics)
    {
        if (!IdentifierRegex.IsMatch(value))
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC100",
                path,
                SpecDiagnosticSeverity.Error,
                $"Identifier '{value}' is invalid. Use letters, numbers, and underscores; start with a letter or underscore."));
        }
    }

    private static void AddDuplicateNameDiagnostics(
        IEnumerable<string> names,
        string collectionPath,
        string code,
        string message,
        List<SpecDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = names
            .Where(name => !seen.Add(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static n => n, StringComparer.Ordinal);

        foreach (var duplicate in duplicates)
        {
            diagnostics.Add(new SpecDiagnostic(
                code,
                collectionPath,
                SpecDiagnosticSeverity.Error,
                $"{message} Duplicate: '{duplicate}'."));
        }
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SqlIdentifierRegex();
}
