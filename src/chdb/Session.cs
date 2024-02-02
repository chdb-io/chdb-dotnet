namespace ChDb;

public record Session
{
    public string? Format { get; init; }
    public string? DataPath { get; init; }
    public string? UdfPath { get; init; }
    public string? LogLevel { get; init; }

    public LocalResult? Execute(string query, string? format = null)
    {
        var argv = new[] {
            "clickhouse",
            "--multiquery",
            $"--query={query}",
            $"--output-format={format ?? Format ?? "TabSeparated"}",  //$"--path={DataPath}",
            $"--path={DataPath}",
            // $"--udf-path={UdfPath}",
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