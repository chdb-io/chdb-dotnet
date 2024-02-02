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