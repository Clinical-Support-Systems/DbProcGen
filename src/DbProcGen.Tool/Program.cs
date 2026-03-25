var command = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

switch (command)
{
    case "generate":
        Console.WriteLine("generate: TODO - implement ADR-constrained pipeline");
        return 0;

    case "validate":
        Console.WriteLine("validate: TODO - implement JSON schema + semantic rules");
        return 0;

    case "clean":
        Console.WriteLine("clean: TODO - implement deterministic stale cleanup");
        return 0;

    default:
        Console.WriteLine("DbProcGen CLI (v1 skeleton)");
        Console.WriteLine("Usage: dbprocgen <generate|validate|clean>");
        return 0;
}