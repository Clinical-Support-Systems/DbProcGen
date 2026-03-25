namespace DbProcGen.Runtime;

public sealed record WorkerRoute(string Schema, string ProcedureName, string WorkerSuffix)
{
    public string FullyQualifiedWorkerName => $"[{Schema}].[{ProcedureName}_{WorkerSuffix}]";
}
