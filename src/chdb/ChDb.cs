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
/// Opens and closes the chdb connections.
/// libchdb runs one engine per process, on one data path. The connections to that path
/// share it, databases and tables included, but each has a session state of its own: the
/// settings changed with SET and the current database. The engine stops when the last of
/// them closes, and only then can a connection to another path be opened, so opening one
/// closes the connections to the current path first; their owners reconnect, with a fresh
/// session state, on their next query.
/// </summary>
internal static class Connection
{
    internal const string InMemoryPath = ":memory:";

    private static readonly object Lock = new();
    private static readonly List<Handle> Open = new(); // all to the same path
    // keeps the in-memory engine running between stateless queries
    private static Handle? _stateless;
    // what an open connection costs the native side, so that the collector finalizes sessions
    // nobody disposed before their connections pile up (measured at about 140 KiB)
    private const long ConnectionMemoryPressure = 256 * 1024;
    private static readonly TimeSpan ExitWaitForQueries = TimeSpan.FromSeconds(10);

    static Connection()
    {
        // Stop the engine before the process exits. libchdb's own teardown of an engine still
        // open at exit runs while C++ static destructors run, which its background threads
        // (e.g. MergeTree merges) can still be using.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            // A query still running holds the lock: let it finish, but do not keep the process
            // from exiting for longer than that wait; libchdb's own teardown then has to do.
            if (!Monitor.TryEnter(Lock, ExitWaitForQueries))
                return;
            try
            {
                CloseAllLocked();
            }
            finally
            {
                Monitor.Exit(Lock);
            }
        };
    }

    internal sealed class Handle
    {
        internal Handle(string path, nint conn)
        {
            Path = path;
            Conn = conn;
        }

        internal string Path { get; }
        internal nint Conn { get; set; } // chdb_connection*, zero once closed
    }

    /// <summary>
    /// Runs a query on a session's connection, opening it unless it is open to the path.
    /// </summary>
    internal static LocalResult? Query(ref Handle? connection, string? path, string query, string format, string? logLevel)
    {
        path ??= InMemoryPath;
        lock (Lock)
        {
            if (connection is null || connection.Conn == IntPtr.Zero || connection.Path != path)
            {
                if (connection is not null)
                    CloseLocked(connection);
                connection = OpenLocked(path, logLevel);
            }
            return Run(connection, query, format);
        }
    }

    /// <summary>
    /// Runs a stateless query on a connection of its own, so that its SET statements end with it.
    /// </summary>
    internal static LocalResult? Query(string query, string format)
    {
        lock (Lock)
        {
            if (_stateless is null || _stateless.Conn == IntPtr.Zero)
                _stateless = OpenLocked(InMemoryPath, null);
            var connection = OpenLocked(InMemoryPath, null);
            try
            {
                return Run(connection, query, format);
            }
            finally
            {
                CloseLocked(connection);
            }
        }
    }

    internal static void Close(Handle? connection)
    {
        if (connection is null)
            return;
        lock (Lock)
            CloseLocked(connection);
    }

    /// <summary>
    /// Stops the engine if it runs on the path, closing every connection to it.
    /// </summary>
    internal static void CloseAll(string? path)
    {
        lock (Lock)
        {
            if (Open.Count > 0 && Open[0].Path == (path ?? InMemoryPath))
                CloseAllLocked();
        }
    }

    private static LocalResult? Run(Handle connection, string query, string format)
    {
        var resultHandle = NativeMethods.chdb_query(Marshal.ReadIntPtr(connection.Conn), query, format);
        return LocalResult.FromHandle(resultHandle);
    }

    private static Handle OpenLocked(string path, string? logLevel)
    {
        if (Open.Count > 0 && Open[0].Path != path)
            CloseAllLocked();
        var args = new List<string> { "clickhouse" };
        // libchdb runs in memory without --path; --path=:memory: is a directory of that name
        if (path != InMemoryPath)
            args.Add($"--path={path}");
        if (logLevel is not null)
            args.Add($"--log-level={logLevel}");
        var argv = args.ToArray();
        var conn = NativeMethods.chdb_connect(argv.Length, argv);
        if (conn == IntPtr.Zero)
            throw new InvalidOperationException($"Failed to open chdb connection for path '{path}'.");
        var connection = new Handle(path, conn);
        Open.Add(connection);
        GC.AddMemoryPressure(ConnectionMemoryPressure);
        return connection;
    }

    private static void CloseLocked(Handle connection)
    {
        if (connection.Conn == IntPtr.Zero)
            return;
        NativeMethods.chdb_close_conn(connection.Conn);
        connection.Conn = IntPtr.Zero;
        Open.Remove(connection);
        GC.RemoveMemoryPressure(ConnectionMemoryPressure);
    }

    private static void CloseAllLocked()
    {
        while (Open.Count > 0)
            CloseLocked(Open[Open.Count - 1]);
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
    /// Settings changed with SET, by contrast, only last until the query ends.
    /// </remarks>
    public static LocalResult? Query(string query, string? format = null)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));
        return Connection.Query(query, format ?? "TabSeparated");
    }
}
