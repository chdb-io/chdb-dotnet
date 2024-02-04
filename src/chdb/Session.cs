namespace ChDb;

public record Session : IDisposable
{
    public string? Format { get; init; }
    public string? DataPath { get; set; }
    // public string? UdfPath { get; init; }
    public string? LogLevel { get; init; }
    public bool IsTemp { get; init; } = true;

    public void Dispose()
    {
        if (!IsTemp && DataPath?.EndsWith("chdb_") == true && Directory.Exists(DataPath))
            Directory.Delete(DataPath, true);
    }

    public LocalResult? Query(string query, string? format = null)
    {
        if (IsTemp && DataPath is null)
        {
            DataPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chdb_");
        }

        var argv = new[] {
            "clickhouse",
            "--multiquery",
            $"--query={query}",
            $"--output-format={format ?? Format ?? "TabSeparated"}",  //$"--path={DataPath}",
            $"--path={DataPath}",
            // $"--user_scripts_path={UdfPath}", $"--user_defined_executable_functions_config={UdfPath}/*.xml",
            $"--log-level={LogLevel ?? "trace"}",
        };
        try
        {
            return ChDb.Execute(argv);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("chdb error: " + e.Message, e);
            throw;
        }
    }
}