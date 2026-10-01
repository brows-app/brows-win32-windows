using Brows.Threading;
using Brows.Win32.InteropServices;
using Brows.Win32.PlatformInvoke;
using Domore.Logs;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

/// <summary>
/// Provides thumbnail or icon images for file and folder paths.
/// </summary>
public sealed class Win32ThumbnailService : Win32BaseService {
    private static readonly ILog Log = Logging.For(typeof(Win32ThumbnailService));

    private readonly bool ThreadPoolOwned;
    private readonly STAThreadPool ThreadPool;

    private static BitmapSource GetThumbnailSource(string path, int width, int height) {
        using (var wrapper = new ShellItemImageFactoryWrapper(path)) {
            var sz = new SIZE { cx = width, cy = height };
            var flags = SIIGBF.THUMBNAILONLY | SIIGBF.BIGGERSIZEOK | SIIGBF.INCACHEONLY;
            try {
                return wrapper.GetBitmapSource(sz, flags);
            }
            catch {
            }
            flags = SIIGBF.THUMBNAILONLY;
            try {
                return wrapper.GetBitmapSource(sz, flags);
            }
            catch {
            }
            flags = SIIGBF.RESIZETOFIT;
            return wrapper.GetBitmapSource(sz, flags);
        }
    }

    private async Task<BitmapSource> GetThumbnailSourceCore(string path,
                                                            int width,
                                                            int height,
                                                            CancellationToken cancellationToken) {
        try {
            return await ThreadPool.Work(nameof(GetThumbnailSource), cancellationToken: cancellationToken,
                work: () => GetThumbnailSource(path, width, height)).ConfigureAwait(false);
        }
        finally {
            EndOperation();
        }
    }

    private protected sealed override void DisposeCore() {
        try {
            if (ThreadPoolOwned) {
                ThreadPool.Empty();
            }
        }
        catch (Exception ex) {
            if (Log.Warn()) {
                Log.Warn(ex);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32ThumbnailService"/> class.
    /// </summary>
    /// <param name="threadPool">
    /// The optional STA thread pool to use for thumbnail requests. If omitted, the service creates a pool.
    /// </param>
    public Win32ThumbnailService(STAThreadPool threadPool = null) {
        ThreadPool = threadPool ?? new(nameof(Win32ThumbnailService)) {
            TryWorkDelay = 10,
            WorkerCountMax = Environment.ProcessorCount,
        };
        ThreadPoolOwned = threadPool is null;
    }

    /// <summary>
    /// Gets a thumbnail or icon image for a file or folder path.
    /// </summary>
    /// <param name="path">The path of the file or folder to get an image for.</param>
    /// <param name="width">The requested image width, in pixels.</param>
    /// <param name="height">The requested image height, in pixels.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>A task that completes with the thumbnail or icon image.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="path"/> is <see langword="null"/> or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    public Task<BitmapSource> GetThumbnailSource(string path,
                                                 int width,
                                                 int height,
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
        return GetThumbnailSourceCore(path, width, height, cancellationToken);
    }
}
