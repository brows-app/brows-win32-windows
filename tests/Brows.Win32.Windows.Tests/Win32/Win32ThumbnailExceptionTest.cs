using System;
using System.Runtime.InteropServices;

namespace Brows.Win32;

[TestFixture]
internal sealed class Win32ThumbnailExceptionTest {
    [Test]
    public void Constructor_WithInnerException_CopiesInnerHResult() {
        const int noStorageProviderThumbnailHandler = unchecked((int)0x8004B207);
        var innerException = new COMException("inner", noStorageProviderThumbnailHandler);

        var exception = new Win32ThumbnailException("message", innerException);

        using (Assert.EnterMultipleScope()) {
            Assert.That(exception.Message, Is.EqualTo("message"));
            Assert.That(exception.InnerException, Is.SameAs(innerException));
            Assert.That(exception.HResult, Is.EqualTo(noStorageProviderThumbnailHandler));
        }
    }

    [Test]
    public void Constructor_WithNullInnerException_KeepsDefaultHResult() {
        var exception = new Win32ThumbnailException("message", null);

        using (Assert.EnterMultipleScope()) {
            Assert.That(exception.InnerException, Is.Null);
            Assert.That(exception.HResult, Is.EqualTo(new Exception().HResult));
        }
    }
}
