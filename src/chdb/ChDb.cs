using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ChDb;

internal static class NativeMethods
{
    private const string __DllName = "libchdb.so";

    [DllImport(__DllName, EntryPoint = "query_stable_v2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr query_stable_v2(int argc, string[] argv);

    [DllImport(__DllName, EntryPoint = "free_result_v2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void free_result_v2(IntPtr result);
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
    /// </remarks>
    public static LocalResult? Query(string query, string? format = null)
    {
        if (query is null)
            throw new ArgumentNullException(nameof(query));
        var argv = new[] {
            "clickhouse",
            "--multiquery",
            $"--query={query}",
            $"--output-format={format ?? "TabSeparated"}"
        };
        return Execute(argv);
    }

    private static string? LookUpArg(string[] argv, string key)
    {
        foreach (var arg in argv)
        {
            var prefix = $"--{key}=";
            if (arg.StartsWith(prefix))
                return arg.Substring(prefix.Length);
        }
        return null;
    }

    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]
    internal static LocalResult? Execute(string[] argv)
    {
        try
        {
            var ptr = NativeMethods.query_stable_v2(argv.Length, argv);
            var res = LocalResult.FromPtr(ptr);
            NativeMethods.free_result_v2(ptr);
            // Marshal.FreeHGlobal(ptr);
            return res;
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
}