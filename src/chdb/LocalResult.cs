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

    internal static LocalResult? FromHandle(nint handle)
    {
        if (handle == IntPtr.Zero)
            return null;
        try
        {
            var errorPtr = NativeMethods.chdb_result_error(handle);
            if (errorPtr != IntPtr.Zero)
                return new LocalResult(null, MarshalPtrToStringUTF8(errorPtr), 0, 0, TimeSpan.Zero);

            var bufPtr = NativeMethods.chdb_result_buffer(handle);
            var len = checked((int)NativeMethods.chdb_result_length(handle));
            var buf = bufPtr == IntPtr.Zero ? null : new byte[len];
            if (buf != null)
                Marshal.Copy(bufPtr, buf, 0, len);

            var elapsed = TimeSpan.FromSeconds(NativeMethods.chdb_result_elapsed(handle));
            var rowsRead = NativeMethods.chdb_result_rows_read(handle);
            var bytesRead = NativeMethods.chdb_result_bytes_read(handle);
            return new LocalResult(buf, null, rowsRead, bytesRead, elapsed);
        }
        finally
        {
            NativeMethods.chdb_destroy_query_result(handle);
        }
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
}
