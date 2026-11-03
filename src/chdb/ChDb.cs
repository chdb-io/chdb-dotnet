using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ChDb;

internal static class NativeMethods
{
    private const string __DllName = "libchdb.so";

    // chdb_connection * chdb_connect(int argc, char ** argv)
    [DllImport(__DllName, EntryPoint = "chdb_connect", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr chdb_connect(int argc, string[] argv);

    // void chdb_close_conn(chdb_connection * conn)
    [DllImport(__DllName, EntryPoint = "chdb_close_conn", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void chdb_close_conn(IntPtr conn);

    // chdb_result * chdb_query_n(chdb_connection conn, const char * query, size_t query_len, const char * format, size_t format_len)
    [DllImport(__DllName, EntryPoint = "chdb_query_n", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr chdb_query_n(IntPtr conn, byte[] query, UIntPtr query_len, byte[] format, UIntPtr format_len);

    [DllImport(__DllName, EntryPoint = "chdb_destroy_query_result", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void chdb_destroy_query_result(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_buffer", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr chdb_result_buffer(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_length", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern UIntPtr chdb_result_length(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_elapsed", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern double chdb_result_elapsed(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_rows_read", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern ulong chdb_result_rows_read(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_bytes_read", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern ulong chdb_result_bytes_read(IntPtr result);

    [DllImport(__DllName, EntryPoint = "chdb_result_error", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr chdb_result_error(IntPtr result);
}

/// <summary>
/// Entry point for stateless chdb queries.
/// </summary>
public static class ChDb
{
    // libchdb runs one engine per process, bound to one storage path. The connection is
    // shared by all queries and reopened only when a query targets a different data path.
    private static readonly object ConnectionLock = new();
    private static IntPtr _connection; // chdb_connection *
    private static string? _connectionPath;

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
    /// The in-memory engine lives for the whole process, so in-memory objects (e.g. Memory engine tables)
    /// created by a stateless query stay visible to later stateless queries.
    /// A multi-statement query returns the output of every statement, concatenated.
    /// </remarks>
    public static LocalResult? Query(string query, string? format = null)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));
        return Execute(null, query, format ?? "TabSeparated", null);
    }

    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]
    internal static LocalResult? Execute(string? path, string query, string format, string? logLevel)
    {
        try
        {
            lock (ConnectionLock)
            {
                if (!Connect(path, logLevel))
                    return new LocalResult(null, $"Failed to open a chdb connection for path '{path ?? ":memory:"}'", 0, 0, TimeSpan.Zero);
                var queryBytes = Encoding.UTF8.GetBytes(query);
                var formatBytes = Encoding.UTF8.GetBytes(format);
                var result = NativeMethods.chdb_query_n(
                    Marshal.ReadIntPtr(_connection),
                    queryBytes, (UIntPtr)queryBytes.Length,
                    formatBytes, (UIntPtr)formatBytes.Length);
                try
                {
                    return LocalResult.FromResult(result);
                }
                finally
                {
                    NativeMethods.chdb_destroy_query_result(result);
                }
            }
        }
        catch (RuntimeWrappedException e)
        {
            if (e.WrappedException is string s)
                throw new ArgumentException($"Unmanaged string error {s}");
            else
                throw new ArgumentException($"Unmanaged unknown error {e.WrappedException}");
        }
        catch (Exception e)
        {
            throw new Exception("Managed error", e);
        }
    }

    /// <summary>
    /// Close the shared connection if it is open on <paramref name="path"/>, shutting the engine down.
    /// </summary>
    internal static void Close(string? path)
    {
        lock (ConnectionLock)
        {
            if (_connection != IntPtr.Zero && _connectionPath == path)
                CloseCurrent();
        }
    }

    private static bool Connect(string? path, string? logLevel)
    {
        if (_connection != IntPtr.Zero && _connectionPath == path)
            return true;
        if (_connection != IntPtr.Zero)
            CloseCurrent();

        var argv = new List<string> { "clickhouse" };
        if (path != null)
            argv.Add($"--path={path}");
        if (logLevel != null)
            argv.Add($"--log-level={logLevel}");
        var connection = NativeMethods.chdb_connect(argv.Count, argv.ToArray());
        if (connection == IntPtr.Zero)
            return false;
        _connection = connection;
        _connectionPath = path;
        return true;
    }

    private static void CloseCurrent()
    {
        NativeMethods.chdb_close_conn(_connection);
        _connection = IntPtr.Zero;
        _connectionPath = null;
    }
}
