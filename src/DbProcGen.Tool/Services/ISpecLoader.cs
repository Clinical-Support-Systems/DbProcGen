using DbProcGen.Spec;

namespace DbProcGen.Tool.Services;

public interface ISpecLoader
{
    SpecDocument LoadSpec(string filePath);
}
