using System.Runtime.InteropServices;

namespace ChDb;

/// <summary>
/// The query result.
/// </summary>
/// <param name="Buf">Result string.</param>
/// <param name="ErrorMessage">Error message if occured.</param>
/// <param name="RowsRead">Number of rows read</param>
/// <param name="BytesRead">Number of bytes read</param>
/// <param name="Elapsed">Query time elapsed, in seconds.</param>
public record LocalResult(string? Buf, string? ErrorMessage, ulong RowsRead, ulong BytesRead, TimeSpan Elapsed)
{
    internal static LocalResult? FromPtr(nint ptr)
    {
        Handle? h = null;
        if (ptr == IntPtr.Zero)
            return null;
        h = Marshal.PtrToStructure<Handle>(ptr);
        if (h == null)
            return null;

        var errorMessage = h.error_message == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.error_message);
        if (errorMessage != null)
            return new LocalResult(null, errorMessage, 0, 0, TimeSpan.Zero);

        var buf = h.buf == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.buf, h.len);
        var elapsed = TimeSpan.FromSeconds(h.elapsed);
        return new LocalResult(buf, errorMessage, h.rows_read, h.bytes_read, elapsed);
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

        public override string ToString() => $"Handle{{\n\tbuf={buf},\n\tlen={len},\n\t_vec={_vec},\n\telapsed={elapsed},\n\trows_read={rows_read},\n\tbytes_read={bytes_read},\n\terror_message={error_message}}}";
    }
}