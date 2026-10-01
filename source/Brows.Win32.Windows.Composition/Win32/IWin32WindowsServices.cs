using Brows.Composition;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

/// <summary>
/// Provides asynchronous access to Windows Shell icons, effective overlay icons, and thumbnail images.
/// </summary>
/// <remarks>
/// The export performs Shell work on an STA thread pool. A pool supplied through
/// <see cref="Win32WindowsServicesVariable.ThreadPool"/> remains owned by its caller; otherwise, the export
/// creates a pool when first used and empties it when the composition environment kills the export. Cancellation
/// can stop queued work or a caller's wait, but cannot interrupt a native Shell call that is already running.
/// Returned bitmap sources are frozen when WPF permits it.
/// </remarks>
public interface IWin32WindowsServices : IExport {
    /// <summary>
    /// Gets the icon associated with a file or folder path.
    /// </summary>
    /// <param name="path">The path whose icon is requested.</param>
    /// <param name="attributes">
    /// Known attributes for <paramref name="path"/>. Supplying the attributes avoids an additional query to
    /// Windows; if <see langword="null"/>, the service retrieves them.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the path-specific icon for an existing path, or the generic icon for its file
    /// type when the path is missing. A stock folder or document icon is used when appropriate; the task can
    /// complete with <see langword="null"/> if Windows cannot provide an icon.
    /// </returns>
    /// <remarks>
    /// Existing paths are queried as the actual Shell item so that path-specific icons are preserved. Missing
    /// paths use the icon associated with their extension, or the stock document icon when there is no extension.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="InvalidOperationException">The composition environment has killed the export.</exception>
    Task<BitmapSource> GetIconSource(
        string path,
        FileAttributes? attributes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Windows stock icon.
    /// </summary>
    /// <param name="stockIcon">The stock icon to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the requested icon, or <see langword="null"/> if Windows cannot retrieve it.
    /// </returns>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="InvalidOperationException">The composition environment has killed the export.</exception>
    Task<BitmapSource> GetIconSource(
        Win32StockIcon stockIcon,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the overlay icon that the Windows Shell associates with a file or folder path.
    /// </summary>
    /// <param name="path">The path whose effective overlay icon is requested.</param>
    /// <param name="attributes">
    /// Known attributes for <paramref name="path"/>. Supplying the attributes avoids an additional query to
    /// Windows; if <see langword="null"/>, the service retrieves them.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the effective overlay icon selected by the Windows Shell, or
    /// <see langword="null"/> if the Shell does not assign an overlay to the path.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">A Windows Shell icon lookup fails.</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">A Windows Shell image list call fails.</exception>
    /// <exception cref="InvalidOperationException">The composition environment has killed the export.</exception>
    Task<BitmapSource> GetOverlayIconSource(
        string path,
        FileAttributes? attributes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a thumbnail or icon image for a file or folder path.
    /// </summary>
    /// <param name="path">The path whose thumbnail or icon image is requested.</param>
    /// <param name="width">The requested image width, in pixels; must be greater than zero.</param>
    /// <param name="height">The requested image height, in pixels; must be greater than zero.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>A task that completes with the thumbnail or icon image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="System.Runtime.InteropServices.COMException">A Windows Shell image request fails.</exception>
    /// <exception cref="InvalidOperationException">The composition environment has killed the export.</exception>
    Task<BitmapSource> GetThumbnailSource(
        string path,
        int width,
        int height,
        CancellationToken cancellationToken = default);
}
