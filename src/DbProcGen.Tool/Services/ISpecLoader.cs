using DbProcGen.Spec;

namespace DbProcGen.Tool.Services;

/// <summary>
///     Loads and parses a `.dbproc.json` spec file from disk.
/// </summary>
public interface ISpecLoader
{
    /// <summary>
    ///     Loads, parses, and validates the spec file at the given path.
    /// </summary>
    /// <param name="filePath">the absolute path to the `.dbproc.json` file</param>
    /// <returns>A <see cref="SpecDocument" /> with the parsed spec and any diagnostics.</returns>
    SpecDocument LoadSpec(string filePath);
}
