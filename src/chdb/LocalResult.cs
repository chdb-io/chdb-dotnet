using System.Runtime.InteropServices;

namespace ChDb;

/// <summary>
/// The query result.
/// </summary>
public record LocalResult
{
    public byte[]? Buf { get; }
    public string? ErrorMessage { get; }
    /// <summary>
    /// By text formats contains a result text.
    /// </summary>
    public string? Text => Buf == null ? null : System.Text.Encoding.UTF8.GetString(Buf);
    public ulong RowsRead { get; }
    public ulong BytesRead { get; }
    public TimeSpan Elapsed { get; }

    /// <param name="Buf">Result buffer.</param>
    /// <param name="ErrorMessage">Error message if occured.</param>
    /// <param name="RowsRead">Number of rows read</param>
    /// <param name="BytesRead">Number of bytes read</param>
    /// <param name="Elapsed">Query time elapsed, in seconds.</param>
    public LocalResult(byte[]? Buf, string? ErrorMessage, ulong RowsRead, ulong BytesRead, TimeSpan Elapsed)
    {
        this.Buf = Buf;
        this.ErrorMessage = ErrorMessage;
        this.RowsRead = RowsRead;
        this.BytesRead = BytesRead;
        this.Elapsed = Elapsed;
    }

    internal static LocalResult? FromPtr(nint ptr)
    {
        if (ptr == IntPtr.Zero)
            return null;
        var h = Marshal.PtrToStructure<Handle>(ptr);
        if (h == null)
            return null;

        var errorMessage = h.error_message == IntPtr.Zero ? null : MarshalPtrToStringUTF8(h.error_message);
        if (errorMessage != null)
            return new LocalResult(null, errorMessage, 0, 0, TimeSpan.Zero);

        var elapsed = TimeSpan.FromSeconds(h.elapsed);

        var buf = h.buf == IntPtr.Zero ? null : new byte[h.len];
        if (buf != null)
            Marshal.Copy(h.buf, buf, 0, h.len);
        return new LocalResult(buf, errorMessage, h.rows_read, h.bytes_read, elapsed);
    }

    private static string MarshalPtrToStringUTF8(nint ptr)
    {
        unsafe
        {
            var str = (byte*)ptr;
            var length = 0;
            for (var i = str; *i != 0; i++, length++) ;
            var clrString = System.Text.Encoding.UTF8.GetString(str, length);
            return clrString;
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

        public override string ToString() => $"Handle{{\n\tbuf={buf},\n\tlen={len},\n\t_vec={_vec},\n\telapsed={elapsed},\n\trows_read={rows_read},\n\tbytes_read={bytes_read},\n\terror_message={error_message}}}";
    }
}