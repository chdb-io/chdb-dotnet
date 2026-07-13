using System.Runtime.InteropServices;

namespace ChDb;

internal static class NativeMethods
{
    private const string __DllName = "libchdb.so";

    [DllImport(__DllName, EntryPoint = "chdb_connect", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern nint chdb_connect(int argc, string[] argv);

    [DllImport(__DllName, EntryPoint = "chdb_close_conn", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void chdb_close_conn(nint conn);

    [DllImport(__DllName, EntryPoint = "chdb_query", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern nint chdb_query(nint conn, [MarshalAs(UnmanagedType.LPUTF8Str)] string query, [MarshalAs(UnmanagedType.LPUTF8Str)] string format);

    [DllImport(__DllName, EntryPoint = "chdb_destroy_query_result", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void chdb_destroy_query_result(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_buffer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern nint chdb_result_buffer(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_length", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern nuint chdb_result_length(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_elapsed", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern double chdb_result_elapsed(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_rows_read", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern ulong chdb_result_rows_read(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_bytes_read", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern ulong chdb_result_bytes_read(nint result);

    [DllImport(__DllName, EntryPoint = "chdb_result_error", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern nint chdb_result_error(nint result);
}

/// <summary>
/// Manages the single chdb connection allowed per process.
/// libchdb permits only one active connection at a time, so the connection is
/// shared and transparently reopened when a query targets a different data path.
/// </summary>
internal static class Connection
{
    internal const string InMemoryPath = ":memory:";

    private static readonly object Lock = new();
    private static nint _conn; // chdb_connection*
    private static string? _path;

    internal static LocalResult? Query(string? path, string query, string format, string? logLevel = null)
    {
        lock (Lock)
        {
            Connect(path ?? InMemoryPath, logLevel);
            var resultHandle = NativeMethods.chdb_query(Marshal.ReadIntPtr(_conn), query, format);
            return LocalResult.FromHandle(resultHandle);
        }
    }

    internal static void Close(string? path)
    {
        lock (Lock)
        {
            if (_conn != IntPtr.Zero && _path == (path ?? InMemoryPath))
                CloseCurrent();
        }
    }

    private static void CloseCurrent()
    {
        NativeMethods.chdb_close_conn(_conn);
        _conn = IntPtr.Zero;
        _path = null;
    }

    private static void Connect(string path, string? logLevel)
    {
        if (_conn != IntPtr.Zero && _path == path)
            return;
        if (_conn != IntPtr.Zero)
            CloseCurrent();
        var argv = logLevel is null
            ? new[] { "clickhouse", $"--path={path}" }
            : new[] { "clickhouse", $"--path={path}", $"--log-level={logLevel}" };
        var conn = NativeMethods.chdb_connect(argv.Length, argv);
        if (conn == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to open chdb connection for path '{path}'.");
        _conn = conn;
        _path = path;
    }
}

/// <summary>
/// Entry point for stateless chdb queries.
/// </summary>
public static class ChDb
{
    /// <summary>
    /// Execute a stateless query and return the result.
    /// </summary>
    /// <param name="query">SQL query</param>
    /// <param name="format">Optional output format from supported by clickhouse. Default is TabSeparated.</param>
    /// <returns>Query result</returns>
    /// <remarks>
    /// Stateless queries are useful for simple queries that do not require a session.
    /// Use <see cref="Session"/> if you need to create databases or tables.
    /// Set <see cref="Session.DataPath"/> to a non-temporary directory to keep the data between sessions.
    /// Note: since libchdb v3 the engine runs for the lifetime of the process, so in-memory objects
    /// (e.g. Memory engine tables) created by previous queries stay visible to later queries.
    /// </remarks>
    public static LocalResult? Query(string query, string? format = null)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));
        return Connection.Query(null, query, format ?? "TabSeparated");
    }
}
