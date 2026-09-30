using System.Runtime.InteropServices;
using HANDLE = System.IntPtr;
using HGDIOBJ = System.IntPtr;
using LPVOID = System.IntPtr;

namespace Brows.Win32.PlatformInvoke;

internal static class gdi32 {
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] HGDIOBJ hObject);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    public static extern int GetObjectW([In] HANDLE h, [In] int c, [Out] LPVOID pv);
}
