using DbProcGen.Spec;

namespace DbProcGen.Spec.Tests;

public class SpecDocumentFactoryTests
{
    [Test]
    public async Task ParseAndValidate_ValidSpec_IsValidWithNoDiagnostics()
    {
        var json = """
                   {
                     "version": "1.0",
                     "logicalName": "GetOrders",
                     "schema": "sales",
                     "publicProcedure": "GetOrders",
                     "parameters": [
                       { "name": "Status", "sqlType": "nvarchar(20)", "required": true },
                       { "name": "IsPaged", "sqlType": "bit", "required": true }
                     ],
                     "resultContract": {
                       "columns": [
                         { "name": "OrderId", "sqlType": "int", "nullable": false },
                         { "name": "Status", "sqlType": "nvarchar(20)", "nullable": false }
                       ]
                     },
                     "specializationAxes": [
                       { "name": "StatusAxis", "parameter": "Status", "values": [ "Open", "Closed" ] },
                       { "name": "PagingAxis", "parameter": "IsPaged", "values": [ "true", "false" ] }
                     ],
                     "routingRules": {
                       "routes": [
                         {
                           "name": "OpenPaged",
                           "workerSuffix": "open_paged",
                           "when": [
                             { "axis": "StatusAxis", "equals": "Open" },
                             { "axis": "PagingAxis", "equals": "true" }
                           ]
                         }
                       ],
                       "defaultRoute": "OpenPaged"
                     }
                   }
                   """;

        var document = SpecDocumentFactory.ParseAndValidate(json);

        await Assert.That(document.IsValid).IsTrue();
        await Assert.That(document.Spec).IsNotNull();
        await Assert.That(document.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task ParseAndValidate_MissingRequiredFields_ReturnsShapeDiagnostics()
    {
        var json = """
                   {
                     "version": "1.0",
                     "schema": "dbo",
                     "parameters": [],
                     "resultContract": { "columns": [] },
                     "specializationAxes": [],
                     "routingRules": { "routes": [] }
                   }
                   """;

        var document = SpecDocumentFactory.ParseAndValidate(json);

        await Assert.That(document.IsValid).IsFalse();
        await Assert.That(document.Spec).IsNull();
        await Assert.That(document.Diagnostics.Select(static d => d.Code))
            .Contains("DBPROC003")
            .And.Contains("DBPROC003");
        await Assert.That(document.Diagnostics.Select(static d => d.Path))
            .Contains("$.logicalName")
            .And.Contains("$.publicProcedure");
    }

    [Test]
    public async Task ParseAndValidate_InvalidIdentifiers_ReturnsNamingDiagnostics()
    {
        var json = """
                   {
                     "version": "1.0",
                     "logicalName": "1BadLogical",
                     "schema": "dbo",
                     "publicProcedure": "Get-Orders",
                     "parameters": [
                       { "name": "Bad-Param", "sqlType": "int", "required": true }
                     ],
                     "resultContract": {
                       "columns": [
                         { "name": "OrderId", "sqlType": "int", "nullable": false }
                       ]
                     },
                     "specializationAxes": [
                       { "name": "Axis-One", "parameter": "Bad-Param", "values": [ "x" ] }
                     ],
                     "routingRules": {
                       "routes": [
                         {
                           "name": "Route_One",
                           "workerSuffix": "worker-1",
                           "when": [ { "axis": "Axis-One", "equals": "x" } ]
                         }
                       ],
                       "defaultRoute": "Route_One"
                     }
                   }
                   """;

        var document = SpecDocumentFactory.ParseAndValidate(json);

        await Assert.That(document.IsValid).IsFalse();
        await Assert.That(document.Diagnostics.Select(static d => d.Code))
            .Contains("DBPROC100");
        await Assert.That(document.Diagnostics.Select(static d => d.Path))
            .Contains("$.logicalName")
            .And.Contains("$.publicProcedure")
            .And.Contains("$.parameters[0].name")
            .And.Contains("$.specializationAxes[0].name")
            .And.Contains("$.routingRules.routes[0].workerSuffix");
    }

    [Test]
    public async Task ParseAndValidate_DuplicateDeclarations_ReturnsDuplicateDiagnostics()
    {
        var json = """
                   {
                     "version": "1.0",
                     "logicalName": "GetOrders",
                     "schema": "dbo",
                     "publicProcedure": "GetOrders",
                     "parameters": [
                       { "name": "Status", "sqlType": "nvarchar(20)", "required": true },
                       { "name": "status", "sqlType": "nvarchar(20)", "required": false }
                     ],
                     "resultContract": {
                       "columns": [
                         { "name": "OrderId", "sqlType": "int", "nullable": false }
                       ]
                     },
                     "specializationAxes": [
                       { "name": "StatusAxis", "parameter": "Status", "values": [ "Open", "Open" ] }
                     ],
                     "routingRules": {
                       "routes": [
                         {
                           "name": "RouteOne",
                           "workerSuffix": "worker_one",
                           "when": [ { "axis": "StatusAxis", "equals": "Open" } ]
                         },
                         {
                           "name": "routeone",
                           "workerSuffix": "worker_one",
                           "when": [ { "axis": "StatusAxis", "equals": "Open" } ]
                         }
                       ]
                     }
                   }
                   """;

        var document = SpecDocumentFactory.ParseAndValidate(json);

        await Assert.That(document.IsValid).IsFalse();
        await Assert.That(document.Diagnostics.Select(static d => d.Code))
            .Contains("DBPROC120")
            .And.Contains("DBPROC142")
            .And.Contains("DBPROC151")
            .And.Contains("DBPROC152");
    }

    [Test]
    public async Task ParseAndValidate_Diagnostics_AreDeterministicallyOrdered()
    {
        var json = """
                   {
                     "version": "2.0",
                     "logicalName": "Bad-Name",
                     "schema": "dbo",
                     "publicProcedure": "bad-name",
                     "parameters": [
                       { "name": "z", "sqlType": "int", "required": true },
                       { "name": "Z", "sqlType": "int", "required": true }
                     ],
                     "resultContract": { "columns": [] },
                     "specializationAxes": [],
                     "routingRules": { "routes": [] }
                   }
                   """;

        var document = SpecDocumentFactory.ParseAndValidate(json);
        var sorted = document.Diagnostics
            .OrderBy(static d => d, SpecDiagnostic.DeterministicComparer)
            .ToArray();

        await Assert.That(document.Diagnostics).IsEquivalentTo(sorted);
        await Assert.That(document.Diagnostics.Select(static d => $"{d.Path}|{d.Code}"))
            .Contains("$.logicalName|DBPROC100")
            .And.Contains("$.publicProcedure|DBPROC100")
            .And.Contains("$.parameters|DBPROC120")
            .And.Contains("$.version|DBPROC101");
    }
}
