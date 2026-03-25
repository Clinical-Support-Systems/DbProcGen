using DbProcGen.Spec;
using DbProcGen.Tool.Services;
using TUnit.Core;

namespace DbProcGen.Tool.Tests;

public sealed class SpecLoaderTests
{
    [Test]
    public async Task LoadSpec_FileNotFound_ReturnsInvalidDocument()
    {
        var loader = new SpecLoader();

        var document = loader.LoadSpec("nonexistent.dbproc.json");

        await Assert.That(document.IsValid).IsFalse();
        await Assert.That(document.Diagnostics).Any(d => d.Code == "FILE001");
    }

    [Test]
    public async Task LoadSpec_ValidSpec_ReturnsValidDocument()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var validJson = """
                {
                  "version": "1.0",
                  "logicalName": "Test",
                  "schema": "dbo",
                  "publicProcedure": "Test",
                  "parameters": [],
                  "resultContract": { "columns": [{ "name": "Id", "sqlType": "int", "nullable": false }] },
                  "specializationAxes": [],
                  "routingRules": { "routes": [{ "name": "Default", "workerSuffix": "default", "when": [] }] }
                }
                """;
            File.WriteAllText(tempFile, validJson);

            var loader = new SpecLoader();
            var document = loader.LoadSpec(tempFile);

            await Assert.That(document.Spec).IsNotNull();
            await Assert.That(document.Spec!.LogicalName).IsEqualTo("Test");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
