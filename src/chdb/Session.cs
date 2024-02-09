namespace ChDb;

public record Session : IDisposable
{
    /// <summary>
    /// Output format for queries if not explicitely specified. Default is TabSeparated
    /// </summary>
    public string? Format { get; init; }
    /// <summary>
    /// Path to the ClickHouse data directory. If not set, a temporary directory will be used.
    /// </summary>
    public string? DataPath { get; set; } = Path.Combine(Path.GetTempPath(), "chdb_");
    /// <summary>
    /// Query Log Level.
    /// </summary>
    public string? LogLevel { get; init; }
    /// <summary>
    /// Whether to delete the data directory on dispose. Default is true.
    /// </summary>
    public bool IsTemp { get; init; } = true;

    public void Dispose()
    {
        if (!IsTemp && DataPath?.EndsWith("chdb_") == true && Directory.Exists(DataPath))
            Directory.Delete(DataPath, true);
    }

    /// <summary>
    /// Execute a query and return the result.
    /// </summary>
    /// <param name="query">SQL query</param>
    /// <param name="format">Output format, optional.</param>
    /// <returns>Query result</returns>
    public LocalResult? Query(string query, string? format = null)
    {
        if (IsTemp && DataPath is null)
        {
            DataPath = Path.Combine(Path.GetTempPath(), "chdb_");
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
        return ChDb.Execute(argv);
    }
}