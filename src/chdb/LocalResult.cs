using System.Runtime.InteropServices;

namespace ChDb;

public record LocalResult(string? Buf, string? ErrorMessage, ulong RowsRead, ulong BytesRead, TimeSpan Elapsed)
{
    internal static LocalResult? FromPtr(nint ptr)
    {
        Handle? h = null;
        try
        {
            if (ptr == IntPtr.Zero)
                return null;
            h = Marshal.PtrToStructure<Handle>(ptr);
            if (h == null)
                return null;
            var buf = h.buf == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.buf, h.len);
            var errorMessage = h.error_message == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(h.error_message);
            //var elapsed = FromSecondsSafe(h.elapsed);
            var elapsed = TimeSpan.FromSeconds(h.elapsed);
            return new LocalResult(buf, errorMessage, h.rows_read, h.bytes_read, elapsed);
        }
        catch (OverflowException ex)
        {
            throw new OverflowException($"duration {h!.elapsed} is too long. wtf, linux bug? \n{h}", ex);
        }
    }

    private static TimeSpan FromSecondsSafe(double seconds)
    {
        try
        {
            return TimeSpan.FromSeconds(seconds);
        }
        catch (OverflowException ex)
        {
            throw new OverflowException($"duration {seconds} is too long. wtf, linux bug?", ex);
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

        public override string ToString() => $"Handle{{buf={buf}, len={len}, _vec={_vec}, elapsed={elapsed}, rows_read={rows_read}, bytes_read={bytes_read}, error_message={error_message}}}";
    }
}