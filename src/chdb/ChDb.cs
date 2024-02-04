using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ChDb;

internal static class NativeMethods
{
    const string __DllName = "libchdb.so";

    [DllImport(__DllName, EntryPoint = "query_stable", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr query_stable(int argc, string[] argv);

    [DllImport(__DllName, EntryPoint = "free_result", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void free_result(IntPtr result);

    [DllImport(__DllName, EntryPoint = "query_stable_v2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern IntPtr query_stable_v2(int argc, string[] argv);

    [DllImport(__DllName, EntryPoint = "free_result_v2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    internal static extern void free_result_v2(IntPtr result);
}

public static class ChDb
{
    public static LocalResult? Query(string query, string? format = null)
    {
        ArgumentNullException.ThrowIfNull(query);
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
                return arg[prefix.Length..];
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
            Marshal.FreeHGlobal(ptr);
            return res;
        }
        catch (RuntimeWrappedException e)
        {
            if (e.WrappedException is string s)
                throw new ArgumentException($"Unmanaged error string {s}");
            else
                throw new ArgumentException($"Unmanaged error {e.WrappedException}");
        }
        catch (Exception e)
        {
            throw new Exception("Managed error", e);
        }
    }
}