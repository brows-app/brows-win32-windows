namespace Brows.Win32;

/// <summary>
/// The exception that is thrown when the Windows Shell cannot provide a thumbnail for a path.
/// </summary>
/// <remarks>
/// <see cref="Exception.InnerException"/> is the failure of the Shell's last thumbnail request, and
/// <see cref="Exception.HResult"/> is copied from it. For example, an HRESULT of <c>0x8004B207</c>
/// (<c>WTS_E_NOSTORAGEPROVIDERTHUMBNAILHANDLER</c>) reports that the item is managed by a cloud storage provider
/// whose thumbnail handler is unavailable.
/// </remarks>
public sealed class Win32ThumbnailException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ThumbnailException"/> class.
    /// </summary>
    public Win32ThumbnailException() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ThumbnailException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public Win32ThumbnailException(string message) : base(message) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ThumbnailException"/> class with a message and the
    /// exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">
    /// The exception that caused this exception. When it is not <see langword="null"/>, its
    /// <see cref="Exception.HResult"/> is copied to this exception.
    /// </param>
    public Win32ThumbnailException(string message, Exception innerException) : base(message, innerException) {
        if (innerException is not null) {
            HResult = innerException.HResult;
        }
    }
}
