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

    /// <summary>
    /// Copy a <c>chdb_result</c> into a managed result. The caller still owns and destroys <paramref name="result"/>.
    /// </summary>
    internal static LocalResult? FromResult(nint result)
    {
        if (result == IntPtr.Zero)
            return null;

        var error = NativeMethods.chdb_result_error(result);
        var errorMessage = error == IntPtr.Zero ? null : MarshalPtrToStringUTF8(error);
        if (errorMessage != null)
            return new LocalResult(null, errorMessage, 0, 0, TimeSpan.Zero);

        var elapsed = TimeSpan.FromSeconds(NativeMethods.chdb_result_elapsed(result));

        var data = NativeMethods.chdb_result_buffer(result);
        var buf = data == IntPtr.Zero ? null : new byte[checked((int)NativeMethods.chdb_result_length(result))];
        if (buf != null)
            Marshal.Copy(data, buf, 0, buf.Length);
        return new LocalResult(buf, errorMessage, NativeMethods.chdb_result_rows_read(result), NativeMethods.chdb_result_bytes_read(result), elapsed);
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