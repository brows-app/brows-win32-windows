using System.Runtime.InteropServices;

namespace Brows.Win32.PlatformInvoke;

internal static class kernel32 {
    public const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    public static extern uint GetFileAttributesW(
        [In][MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
}
