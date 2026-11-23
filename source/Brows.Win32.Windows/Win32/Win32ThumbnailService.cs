using Brows.Threading;
using Brows.Win32.InteropServices;
using Brows.Win32.PlatformInvoke;
using Domore.Logs;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

/// <summary>
/// Provides thumbnail or icon images for file and folder paths.
/// </summary>
public sealed class Win32ThumbnailService : Win32BaseService {
    private static readonly ILog Log = Logging.For(typeof(Win32ThumbnailService));

    private readonly ISTAThreadPool ThreadPool;

    private static BitmapSource GetThumbnailSource(string path, int width, int height, bool throwIfNotAvailable) {
        using (var wrapper = new ShellItemImageFactoryWrapper(path)) {
            var sz = new SIZE { cx = width, cy = height };
            var flags = SIIGBF.THUMBNAILONLY | SIIGBF.BIGGERSIZEOK | SIIGBF.INCACHEONLY;
            try {
                return wrapper.GetBitmapSource(sz, flags);
            }
            catch (Exception ex) {
                /*
                 * A thumbnail missing from the cache is routine, so it is logged at the
                 * debug level. The next request asks the Shell to extract a thumbnail.
                 */
                if (Log.Debug()) {
                    Log.Debug($"Cached thumbnail unavailable [{flags}] [0x{ex.HResult:X8}] {path}", ex);
                }
            }
            flags = SIIGBF.THUMBNAILONLY;
            try {
                return wrapper.GetBitmapSource(sz, flags);
            }
            catch (Exception ex) {
                if (Log.Info()) {
                    Log.Info($"Thumbnail unavailable [{flags}] [0x{ex.HResult:X8}] {path}", ex);
                }
                if (throwIfNotAvailable) {
                    throw new Win32ThumbnailException($"The Windows Shell cannot provide a thumbnail for {path}", ex);
                }
            }
            flags = SIIGBF.RESIZETOFIT;
            return wrapper.GetBitmapSource(sz, flags);
        }
    }

    private async Task<BitmapSource> GetThumbnailSourceCore(string path,
                                                            int width,
                                                            int height,
                                                            bool throwIfNotAvailable,
                                                            CancellationToken cancellationToken) {
        try {
            return await ThreadPool.Work(nameof(GetThumbnailSource), cancellationToken: cancellationToken,
                work: () => GetThumbnailSource(path, width, height, throwIfNotAvailable)).ConfigureAwait(false);
        }
        finally {
            EndOperation();
        }
    }

    private protected sealed override void DisposeCore() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ThumbnailService"/> class.
    /// </summary>
    /// <param name="threadPool">
    /// The non-null, caller-owned STA thread pool to use for thumbnail requests.
    /// </param>
    /// <remarks>
    /// Disposing this service does not empty the pool. The caller must empty it after all services using it
    /// have been disposed and other pool users have finished.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="threadPool"/> is <see langword="null"/>.
    /// </exception>
    public Win32ThumbnailService(ISTAThreadPool threadPool) {
        ThreadPool = threadPool ?? throw new ArgumentNullException(nameof(threadPool));
    }

    /// <summary>
    /// Gets a thumbnail or icon image for a file or folder path.
    /// </summary>
    /// <param name="path">The path of the file or folder to get an image for.</param>
    /// <param name="width">The requested image width, in pixels; must be greater than zero.</param>
    /// <param name="height">The requested image height, in pixels; must be greater than zero.</param>
    /// <param name="throwIfNotAvailable">
    /// <see langword="true"/> to throw <see cref="Win32ThumbnailException"/> when the Shell cannot provide a
    /// thumbnail; <see langword="false"/> to fall back to a resized thumbnail or icon.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the thumbnail image, or, when <paramref name="throwIfNotAvailable"/> is
    /// <see langword="false"/>, with the thumbnail or icon image.
    /// </returns>
    /// <remarks>
    /// Tries a cached thumbnail first, then thumbnail generation. If both fail, the request throws
    /// <see cref="Win32ThumbnailException"/> when <paramref name="throwIfNotAvailable"/> is
    /// <see langword="true"/>, and otherwise asks the Shell for a resized thumbnail or icon. The failures of the
    /// thumbnail requests are logged. Bitmap sources are frozen when WPF permits it.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="Win32ThumbnailException">
    /// <paramref name="throwIfNotAvailable"/> is <see langword="true"/> and the Shell cannot provide a thumbnail,
    /// including when it cannot create an item for <paramref name="path"/>. The inner exception is the failure of
    /// the last thumbnail request.
    /// </exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="COMException">
    /// A Windows Shell image request fails.
    /// </exception>
    public Task<BitmapSource> GetThumbnailSource(string path,
                                                 int width,
                                                 int height,
                                                 bool throwIfNotAvailable,
                                                 CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentNullException(nameof(path));
        }
        if (width <= 0) {
            throw new ArgumentOutOfRangeException(nameof(width), actualValue: width,
                message: "Width must be greater than zero (0).");
        }
        if (height <= 0) {
            throw new ArgumentOutOfRangeException(nameof(height), actualValue: height,
                message: "Height must be greater than zero (0).");
        }
        BeginOperation();
        return GetThumbnailSourceCore(path, width, height, throwIfNotAvailable, cancellationToken);
    }
}
