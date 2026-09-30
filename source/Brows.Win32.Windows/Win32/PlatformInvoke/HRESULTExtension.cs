using System.Runtime.InteropServices;

namespace Brows.Win32.PlatformInvoke;

internal static class HRESULTExtension {
    public static void ThrowOnError(this HRESULT hresult) {
        var hr = (uint)hresult;
        switch (hr) {
            case 0:
                break;
            default:
                Marshal.ThrowExceptionForHR(unchecked((int)hresult));
                break;
        }
    }
}
