using Brows.Win32.InteropServices.ComTypes;
using Brows.Win32.PlatformInvoke;
using System;
using System.Runtime.InteropServices;

namespace Brows.Win32.InteropServices;

internal sealed class ShellItemImageFactoryWrapper : ComObjectWrapper<IShellItemImageFactory> {
    protected sealed override IShellItemImageFactory Factory() {
        var iid = IID.Managed.IShellItemImageFactory;
        var
        hr = shell32.SHCreateItemFromParsingName(Path, IntPtr.Zero, ref iid, out var ppv);
        hr.ThrowOnError();

        var inst = ppv as IShellItemImageFactory;
        if (inst is null) {
            Marshal.FinalReleaseComObject(ppv);
            throw new InvalidCastException();
        }
        return inst;
    }

    public string Path { get; }

    public ShellItemImageFactoryWrapper(string path) {
        Path = path;
    }

    public IntPtr GetImage(SIZE size, SIIGBF flags) {
        return UseComObject(co => {
            var hr = co.GetImage(size, flags, out var bm);
            hr.ThrowOnError();
            return bm;
        });
    }
}
