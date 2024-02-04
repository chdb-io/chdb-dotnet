using System.Runtime.InteropServices;

namespace ChDb;

public record LocalResult(string? Buf, string? ErrorMessage, ulong RowsRead, ulong BytesRead, TimeSpan Elapsed)
{
    internal static LocalResult? FromPtr(nint ptr)
    {
        if (ptr == IntPtr.Zero)
            return null;
        var h = Marshal.PtrToStructure<Handle>(ptr);
        if (h == null)
            return null;
        var buf = h.buf == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.buf, h.len);
        var errorMessage = h.error_message == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.error_message);
        var elapsed = FromSecondsSafe(h.elapsed);
        return new LocalResult(buf, errorMessage, h.rows_read, h.bytes_read, elapsed);
    }

    private static TimeSpan FromSecondsSafe(double seconds)
    {
        try
        {
            return TimeSpan.FromSeconds(seconds);
        }
        catch (OverflowException ex)
        {
            Console.Error.WriteLine($"Overflow: {seconds}"); // TODO linux-x64 bug?
            throw new OverflowException("wtf, linux bug?", ex);
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