using System;
using System.Runtime.InteropServices;

using Brows.Win32.InteropServices.ComTypes;

namespace Brows.Win32.PlatformInvoke;

internal static class shell32 {
    public const int MAX_PATH = 260;
    public const int SHIL_SMALL = 1;

    //[DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true)]
    //public static extern bool ShellExecuteExW([In, Out] ref SHELLEXECUTEINFOW lpExecInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    public static extern IntPtr SHGetFileInfoW(
        [In] string pszPath,
        FILE_ATTRIBUTE dwFileAttributes,
        [In, Out] ref SHFILEINFOW psfi,
        uint cbFileInfo,
        SHGFI uFlags);

    [DllImport("shell32.dll", PreserveSig = true)]
    public static extern HRESULT SHGetImageList(
        int iImageList,
        [In] ref Guid riid,
        [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 1)] out IImageList ppvObj);

    [DllImport("shell32.dll", PreserveSig = true)]
    public static extern HRESULT SHGetStockIconInfo(
        SHSTOCKICONID siid,
        SHGSI uFlags,
        [In, Out] ref SHSTOCKICONINFO psii);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    public static extern HRESULT SHCreateItemFromParsingName(
        [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        [In, Optional] IntPtr pbc,
        [In] ref Guid riid,
        [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out object ppv);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    public static extern IntPtr ExtractIconW(
        [In] IntPtr hInst,
        [In][MarshalAs(UnmanagedType.LPWStr)] string pszExeFileName,
        uint nIconIndex);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    public static extern uint ExtractIconExW(
        [In][MarshalAs(UnmanagedType.LPWStr)] string lpszFile,
        [In] int nIconIndex,
        IntPtr[] phiconLarge,
        IntPtr[] phiconSmall,
        uint nIcons);
}
