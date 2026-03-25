namespace DbProcGen.Runtime;

/// <summary>
///     A resolved worker route identifying the schema, procedure, and suffix of the target worker.
/// </summary>
/// <param name="Schema">the SQL Server schema</param>
/// <param name="ProcedureName">the base public procedure name</param>
/// <param name="WorkerSuffix">the suffix identifying the specific worker variant</param>
public sealed record WorkerRoute(string Schema, string ProcedureName, string WorkerSuffix)
{
    /// <summary>
    ///     The bracket-quoted, fully qualified worker procedure name (e.g. `[dbo].[GetUsers_byEmail]`).
    /// </summary>
    public string FullyQualifiedWorkerName => $"[{Schema}].[{ProcedureName}_{WorkerSuffix}]";
}
