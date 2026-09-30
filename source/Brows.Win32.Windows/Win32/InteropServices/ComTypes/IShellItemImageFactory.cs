using Brows.Win32.PlatformInvoke;
using System;
using System.Runtime.InteropServices;

namespace Brows.Win32.InteropServices.ComTypes;

[Guid(IID.IShellItemImageFactory)]
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemImageFactory {
    [PreserveSig] HRESULT GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
}
