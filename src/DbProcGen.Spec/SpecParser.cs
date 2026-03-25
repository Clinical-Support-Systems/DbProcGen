using System.Text.Json;
using DbProcGen.Model;

namespace DbProcGen.Spec;

/// <summary>
///     Parses raw JSON into a <see cref="DbProcSpec" /> model, producing diagnostics for structural errors.
/// </summary>
/// <remarks>
///     Diagnostic codes emitted by the parser:
///     | Code      | Meaning                     |
///     |-----------|-----------------------------|
///     | DBPROC001 | Invalid JSON syntax          |
///     | DBPROC002 | Root is not a JSON object    |
///     | DBPROC003 | Required property missing    |
///     | DBPROC004 | Value is empty or whitespace |
///     | DBPROC005 | Unexpected JSON type         |
/// </remarks>
public static class SpecParser
{
    /// <summary>
    ///     Parses the raw JSON spec string into a <see cref="SpecParseResult" />.
    /// </summary>
    /// <param name="json">the raw JSON content to parse</param>
    /// <returns>A <see cref="SpecParseResult" /> containing the parsed spec and any parse diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is `null`.</exception>
    public static SpecParseResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var diagnostics = new List<SpecDiagnostic>();
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Disallow
            });
        }
        catch (JsonException ex)
        {
            diagnostics.Add(new SpecDiagnostic(
                "DBPROC001",
                "$",
                SpecDiagnosticSeverity.Error,
                $"Invalid JSON: {ex.Message}"));

            return new SpecParseResult(null,
                diagnostics.OrderBy(static d => d, SpecDiagnostic.DeterministicComparer).ToArray());
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(new SpecDiagnostic(
                    "DBPROC002",
                    "$",
                    SpecDiagnosticSeverity.Error,
                    "Spec root must be a JSON object."));

                return new SpecParseResult(null,
                    diagnostics.OrderBy(static d => d, SpecDiagnostic.DeterministicComparer).ToArray());
            }

            var root = document.RootElement;

            var version = RequiredString(root, "version", "$.version", diagnostics);
            var logicalName = RequiredString(root, "logicalName", "$.logicalName", diagnostics);
            var schema = RequiredString(root, "schema", "$.schema", diagnostics);
            var publicProcedure = RequiredString(root, "publicProcedure", "$.publicProcedure", diagnostics);

            var parameters = ParseParameters(root, diagnostics);
            var resultContract = ParseResultContract(root, diagnostics);
            var specializationAxes = ParseSpecializationAxes(root, diagnostics);
            var routingRules = ParseRoutingRules(root, diagnostics);
            var fragments = ParseFragments(root, diagnostics);

            if (diagnostics.Any(static d => d.Severity == SpecDiagnosticSeverity.Error))
            {
                return new SpecParseResult(null,
                    diagnostics.OrderBy(static d => d, SpecDiagnostic.DeterministicComparer).ToArray());
            }

            var spec = new DbProcSpec(
                version!,
                logicalName!,
                schema!,
                publicProcedure!,
                parameters,
                resultContract!,
                specializationAxes,
                routingRules!,
                fragments);

            return new SpecParseResult(spec,
                diagnostics.OrderBy(static d => d, SpecDiagnostic.DeterministicComparer).ToArray());
        }
    }

    private static IReadOnlyList<DbProcParameterSpec> ParseParameters(JsonElement root,
        List<SpecDiagnostic> diagnostics)
    {
        var path = "$.parameters";
        if (!root.TryGetProperty("parameters", out var parametersElement))
        {
            diagnostics.Add(MissingRequired(path));
            return [];
        }

        if (parametersElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(path, "array"));
            return [];
        }

        var result = new List<DbProcParameterSpec>();
        for (var i = 0; i < parametersElement.GetArrayLength(); i++)
        {
            var itemPath = $"{path}[{i}]";
            var parameterElement = parametersElement[i];
            if (parameterElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(InvalidType(itemPath, "object"));
                continue;
            }

            var name = RequiredString(parameterElement, "name", $"{itemPath}.name", diagnostics);
            var sqlType = RequiredString(parameterElement, "sqlType", $"{itemPath}.sqlType", diagnostics);
            var required = OptionalBool(parameterElement, "required", $"{itemPath}.required", diagnostics) ?? true;

            if (name is null || sqlType is null)
            {
                continue;
            }

            result.Add(new DbProcParameterSpec(name, sqlType, required));
        }

        return result;
    }

    private static DbProcResultContractSpec? ParseResultContract(JsonElement root, List<SpecDiagnostic> diagnostics)
    {
        var path = "$.resultContract";
        if (!root.TryGetProperty("resultContract", out var contractElement))
        {
            diagnostics.Add(MissingRequired(path));
            return null;
        }

        if (contractElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(InvalidType(path, "object"));
            return null;
        }

        var columnsPath = $"{path}.columns";
        if (!contractElement.TryGetProperty("columns", out var columnsElement))
        {
            diagnostics.Add(MissingRequired(columnsPath));
            return null;
        }

        if (columnsElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(columnsPath, "array"));
            return null;
        }

        var columns = new List<DbProcResultColumnSpec>();
        for (var i = 0; i < columnsElement.GetArrayLength(); i++)
        {
            var itemPath = $"{columnsPath}[{i}]";
            var columnElement = columnsElement[i];
            if (columnElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(InvalidType(itemPath, "object"));
                continue;
            }

            var name = RequiredString(columnElement, "name", $"{itemPath}.name", diagnostics);
            var sqlType = RequiredString(columnElement, "sqlType", $"{itemPath}.sqlType", diagnostics);
            var nullable = OptionalBool(columnElement, "nullable", $"{itemPath}.nullable", diagnostics) ?? true;

            if (name is null || sqlType is null)
            {
                continue;
            }

            columns.Add(new DbProcResultColumnSpec(name, sqlType, nullable));
        }

        return new DbProcResultContractSpec(columns);
    }

    private static IReadOnlyList<DbProcSpecializationAxisSpec> ParseSpecializationAxes(JsonElement root,
        List<SpecDiagnostic> diagnostics)
    {
        var path = "$.specializationAxes";
        if (!root.TryGetProperty("specializationAxes", out var axesElement))
        {
            diagnostics.Add(MissingRequired(path));
            return [];
        }

        if (axesElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(path, "array"));
            return [];
        }

        var result = new List<DbProcSpecializationAxisSpec>();
        for (var i = 0; i < axesElement.GetArrayLength(); i++)
        {
            var itemPath = $"{path}[{i}]";
            var axisElement = axesElement[i];
            if (axisElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(InvalidType(itemPath, "object"));
                continue;
            }

            var name = RequiredString(axisElement, "name", $"{itemPath}.name", diagnostics);
            var parameter = RequiredString(axisElement, "parameter", $"{itemPath}.parameter", diagnostics);
            var values = RequiredStringArray(axisElement, "values", $"{itemPath}.values", diagnostics);

            if (name is null || parameter is null || values is null)
            {
                continue;
            }

            result.Add(new DbProcSpecializationAxisSpec(name, parameter, values));
        }

        return result;
    }

    private static DbProcRoutingRulesSpec? ParseRoutingRules(JsonElement root, List<SpecDiagnostic> diagnostics)
    {
        var path = "$.routingRules";
        if (!root.TryGetProperty("routingRules", out var routingElement))
        {
            diagnostics.Add(MissingRequired(path));
            return null;
        }

        if (routingElement.ValueKind != JsonValueKind.Object)
        {
            diagnostics.Add(InvalidType(path, "object"));
            return null;
        }

        var routesPath = $"{path}.routes";
        if (!routingElement.TryGetProperty("routes", out var routesElement))
        {
            diagnostics.Add(MissingRequired(routesPath));
            return null;
        }

        if (routesElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(routesPath, "array"));
            return null;
        }

        var routes = new List<DbProcRouteSpec>();
        for (var i = 0; i < routesElement.GetArrayLength(); i++)
        {
            var routePath = $"{routesPath}[{i}]";
            var routeElement = routesElement[i];
            if (routeElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(InvalidType(routePath, "object"));
                continue;
            }

            var name = RequiredString(routeElement, "name", $"{routePath}.name", diagnostics);
            var workerSuffix = RequiredString(routeElement, "workerSuffix", $"{routePath}.workerSuffix", diagnostics);

            var whenPath = $"{routePath}.when";
            var conditions = new List<DbProcRouteConditionSpec>();
            if (!routeElement.TryGetProperty("when", out var whenElement))
            {
                diagnostics.Add(MissingRequired(whenPath));
            }
            else if (whenElement.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add(InvalidType(whenPath, "array"));
            }
            else
            {
                for (var conditionIndex = 0; conditionIndex < whenElement.GetArrayLength(); conditionIndex++)
                {
                    var conditionPath = $"{whenPath}[{conditionIndex}]";
                    var conditionElement = whenElement[conditionIndex];
                    if (conditionElement.ValueKind != JsonValueKind.Object)
                    {
                        diagnostics.Add(InvalidType(conditionPath, "object"));
                        continue;
                    }

                    var axis = RequiredString(conditionElement, "axis", $"{conditionPath}.axis", diagnostics);
                    var equals = RequiredString(conditionElement, "equals", $"{conditionPath}.equals", diagnostics);

                    if (axis is null || equals is null)
                    {
                        continue;
                    }

                    conditions.Add(new DbProcRouteConditionSpec(axis, equals));
                }
            }

            if (name is null || workerSuffix is null)
            {
                continue;
            }

            routes.Add(new DbProcRouteSpec(name, conditions, workerSuffix));
        }

        var defaultRoute = OptionalString(routingElement, "defaultRoute", $"{path}.defaultRoute", diagnostics);
        return new DbProcRoutingRulesSpec(routes, defaultRoute);
    }

    private static IReadOnlyList<DbProcFragmentSpec> ParseFragments(JsonElement root, List<SpecDiagnostic> diagnostics)
    {
        var path = "$.fragments";
        if (!root.TryGetProperty("fragments", out var fragmentsElement))
        {
            return [];
        }

        if (fragmentsElement.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(path, "array"));
            return [];
        }

        var result = new List<DbProcFragmentSpec>();
        for (var i = 0; i < fragmentsElement.GetArrayLength(); i++)
        {
            var itemPath = $"{path}[{i}]";
            var fragmentElement = fragmentsElement[i];
            if (fragmentElement.ValueKind != JsonValueKind.Object)
            {
                diagnostics.Add(InvalidType(itemPath, "object"));
                continue;
            }

            var name = RequiredString(fragmentElement, "name", $"{itemPath}.name", diagnostics);
            var kind = RequiredString(fragmentElement, "kind", $"{itemPath}.kind", diagnostics);
            var content = RequiredString(fragmentElement, "content", $"{itemPath}.content", diagnostics);

            if (name is null || kind is null || content is null)
            {
                continue;
            }

            result.Add(new DbProcFragmentSpec(name, kind, content));
        }

        return result;
    }

    private static string? RequiredString(JsonElement parent, string propertyName, string path,
        List<SpecDiagnostic> diagnostics)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            diagnostics.Add(MissingRequired(path));
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(InvalidType(path, "string"));
            return null;
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new SpecDiagnostic("DBPROC004", path, SpecDiagnosticSeverity.Error,
                "Value must not be empty."));
            return null;
        }

        return value;
    }

    private static string[]? RequiredStringArray(JsonElement parent, string propertyName, string path,
        List<SpecDiagnostic> diagnostics)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            diagnostics.Add(MissingRequired(path));
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            diagnostics.Add(InvalidType(path, "array"));
            return null;
        }

        var values = new List<string>();
        for (var i = 0; i < element.GetArrayLength(); i++)
        {
            var itemPath = $"{path}[{i}]";
            var item = element[i];
            if (item.ValueKind != JsonValueKind.String)
            {
                diagnostics.Add(InvalidType(itemPath, "string"));
                continue;
            }

            var value = item.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                diagnostics.Add(new SpecDiagnostic("DBPROC004", itemPath, SpecDiagnosticSeverity.Error,
                    "Value must not be empty."));
                continue;
            }

            values.Add(value);
        }

        return values.ToArray();
    }

    private static bool? OptionalBool(JsonElement parent, string propertyName, string path,
        List<SpecDiagnostic> diagnostics)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.True && element.ValueKind != JsonValueKind.False)
        {
            diagnostics.Add(InvalidType(path, "boolean"));
            return null;
        }

        return element.GetBoolean();
    }

    private static string? OptionalString(JsonElement parent, string propertyName, string path,
        List<SpecDiagnostic> diagnostics)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            diagnostics.Add(InvalidType(path, "string"));
            return null;
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new SpecDiagnostic("DBPROC004", path, SpecDiagnosticSeverity.Error,
                "Value must not be empty."));
            return null;
        }

        return value;
    }

    private static SpecDiagnostic MissingRequired(string path)
    {
        return new SpecDiagnostic("DBPROC003", path, SpecDiagnosticSeverity.Error, "Required property is missing.");
    }

    private static SpecDiagnostic InvalidType(string path, string expectedType)
    {
        return new SpecDiagnostic("DBPROC005", path, SpecDiagnosticSeverity.Error,
            $"Invalid JSON type. Expected {expectedType}.");
    }
}
