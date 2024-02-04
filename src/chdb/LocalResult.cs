using System.Runtime.InteropServices;

namespace ChDb;

public record LocalResult(string? Buf, string? ErrorMessage, ulong RowsRead, ulong BytesRead, TimeSpan Elapsed)
{
    internal LocalResult(Handle h) : this(
        h.buf == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.buf, h.len),
        h.error_message == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.error_message),
        h.rows_read,
        h.bytes_read,
        TimeSpan.FromSeconds(h.elapsed))
    {
    }

    private TimeSpan FromSecondsSafe(double seconds)
    {
        // if (seconds is < 0 or double.NaN)
        //     return TimeSpan.Zero;
        // if (double.IsInfinity(seconds))
        //     return TimeSpan.MaxValue;
        try
        {
            return TimeSpan.FromSeconds(seconds);
        }
        catch (OverflowException)
        {
            Console.Error.WriteLine($$"Overflow: {seconds}"); // TODO linux-x64 bug?
            return TimeSpan.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class Handle
    {
        internal nint buf;
        internal int len;
        internal nint _vec; // std::vector<char> *, for freeing
        internal double elapsed;
        internal ulong rows_read;
        internal ulong bytes_read;
        internal nint error_message;
    }
}