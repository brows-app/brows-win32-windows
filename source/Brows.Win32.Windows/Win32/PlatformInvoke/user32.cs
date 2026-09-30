using System;
using System.Runtime.InteropServices;

namespace Brows.Win32.PlatformInvoke;

internal static class user32 {
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true, PreserveSig = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
