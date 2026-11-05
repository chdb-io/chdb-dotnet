namespace ChDb;

/// <summary>
/// A connection of its own to the engine on <see cref="DataPath"/>: sessions with the same path
/// share the databases and tables, but each has its own settings changed with SET and current database.
/// </summary>
public record Session : IDisposable
{
    private Connection.Handle? _connection; // opened by the first query

    public Session()
    {
    }

    /// <summary>
    /// A copy made with <c>with</c> opens a connection of its own, and so gets its own settings and
    /// current database instead of sharing the original's.
    /// </summary>
    protected Session(Session original)
    {
        Format = original.Format;
        DataPath = original.DataPath;
        LogLevel = original.LogLevel;
        IsTemp = original.IsTemp;
    }

    // a session nobody disposed still gives its connection back once it is collected
    ~Session() => Connection.Close(_connection);

    /// <summary>
    /// Output format for queries if not explicitely specified. Default is TabSeparated
    /// </summary>
    public string? Format { get; set; }
    /// <summary>
    /// Path to the ClickHouse data directory. If not set, a temporary directory will be used.
    /// </summary>
    public string? DataPath { get; set; } = Path.Combine(Path.GetTempPath(), "chdb_");
    /// <summary>
    /// Query Log Level.
    /// </summary>
    public string? LogLevel { get; set; }
    /// <summary>
    /// Whether to delete the data directory on dispose. Default is true.
    /// </summary>
    public bool IsTemp { get; set; } = true;

    public void Dispose()
    {
        Connection.Close(_connection);
        _connection = null;
        GC.SuppressFinalize(this);
        if (IsTemp && DataPath?.EndsWith("chdb_") == true && Directory.Exists(DataPath))
        {
            // the engine must stop before its data directory goes, also if other sessions use it
            Connection.CloseAll(DataPath);
            Directory.Delete(DataPath, true);
        }
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

        return Connection.Query(ref _connection, DataPath, query, format ?? Format ?? "TabSeparated", LogLevel);
    }
}
