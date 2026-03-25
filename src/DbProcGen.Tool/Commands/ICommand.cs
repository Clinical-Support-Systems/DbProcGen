namespace DbProcGen.Tool.Commands;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    int Execute(string[] args);
}
