using System;
using System.Runtime.InteropServices;

namespace Brows.Win32.PlatformInvoke;

[TestFixture]
public sealed class NativeLayoutTests {
    [Test]
    public void Size_MatchesNativeSizeStructure() {
        Assert.That(Marshal.SizeOf<SIZE>(), Is.EqualTo(8));
    }

    [Test]
    public void ShellFileInfoW_MatchesNativeLayout() {
        var expected = IntPtr.Size == 8 ? 696 : 692;

        using (Assert.EnterMultipleScope()) {
            Assert.That(Marshal.SizeOf<SHFILEINFOW>(), Is.EqualTo(expected));
            Assert.That(SHFILEINFOW.Size, Is.EqualTo((uint)expected));
        }
    }

    [Test]
    public void ShellStockIconInfo_MatchesNativeLayout() {
        var expected = IntPtr.Size == 8 ? 544 : 536;

        using (Assert.EnterMultipleScope()) {
            Assert.That(Marshal.SizeOf<SHSTOCKICONINFO>(), Is.EqualTo(expected));
            Assert.That(SHSTOCKICONINFO.Size, Is.EqualTo((uint)expected));
        }
    }
}
