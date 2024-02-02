// See https://aka.ms/new-console-template for more information

using System.Reflection;

void PrintVersion()
{
    var versionString = Assembly.GetEntryAssembly()?
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion ?? "unknown";
    Console.WriteLine($"chdb-tool v{versionString}");
}

void PrintHelp()
{
    PrintVersion();
    Console.WriteLine("-------------");
    Console.WriteLine("\nUsage:");
    Console.WriteLine("  chdb-tool <query> <output-format>");
}

if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    PrintHelp();
    return;
}
if (args[0] == "--version" || args[0] == "-v")
{
    PrintVersion();
    return;
}
try
{
    var result = ChDb.ChDb.Query(args[0], args.Length > 1 && !args[1].StartsWith('-') ? args[1] : "PrettyCompact");
    if (result == null)
        return; // TODO behavior changed in 1.2.1
    Console.WriteLine(result.Buf);
    if (!args.Contains("--quiet") && !args.Contains("-q"))
        Console.WriteLine($"Elapsed: {result.Elapsed} s, read {result.RowsRead} rows, {result.BytesRead} bytes");
    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        Console.Error.WriteLine("Error message: " + result.ErrorMessage);
}
catch (ArgumentException e)
{
    Console.Error.WriteLine(e.Message);
    Environment.Exit(1);
}
