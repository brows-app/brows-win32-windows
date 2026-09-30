namespace Brows.Win32.PlatformInvoke;

[TestFixture]
public sealed class HRESULTExtensionTests {
    [TestCase(0x00000000)]
    [TestCase(0x00000001)]
    [TestCase(0x000401A0)]
    public void ThrowOnError_WhenResultIsSuccessful_DoesNotThrow(int result) {
        Assert.DoesNotThrow(() => ((HRESULT)result).ThrowOnError());
    }

    [Test]
    public void ThrowOnError_WhenResultIsFailure_ThrowsComException() {
        var exception = Assert.Throws<System.Runtime.InteropServices.COMException>(
            () => HRESULT.E_FAIL.ThrowOnError());

        Assert.That(exception.HResult, Is.EqualTo(unchecked((int)HRESULT.E_FAIL)));
    }
}
