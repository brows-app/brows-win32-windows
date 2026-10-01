using Brows.Threading;
using Brows.Win32.InteropServices.ComTypes;
using Brows.Win32.PlatformInvoke;
using Domore.Logs;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

/// <summary>
/// Provides overlay icons associated with file and folder paths by the Windows Shell.
/// </summary>
public sealed class Win32OverlayService : Win32BaseService {
    private static readonly ILog Log = Logging.For(typeof(Win32OverlayService));

    private readonly ConcurrentDictionary<int, BitmapSource> OverlayCache = [];
    private readonly bool ThreadPoolOwned;
    private readonly STAThreadPool ThreadPool;

    private static Task<uint> GetPathAttributes(string path, CancellationToken token) {
        return Task.Run(() => {
            var attributes = kernel32.GetFileAttributesW(path);
            if (attributes == kernel32.INVALID_FILE_ATTRIBUTES) {
                if (Log.Debug()) {
                    Log.Debug(new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }
            return attributes;
        }, token);
    }

    private Task<BitmapSource> GetOverlayIconAttempt(string path, CancellationToken token) {
        const SHGFI flags = SHGFI.ICON | SHGFI.SMALLICON | SHGFI.OVERLAYINDEX;
        return ThreadPool.Work(nameof(GetOverlayIconAttempt), cancellationToken: token, work: () => {
            var fileInfo = new SHFILEINFOW();
            try {
                var result = shell32.SHGetFileInfoW(path, default, ref fileInfo, SHFILEINFOW.Size, flags);
                if (result == IntPtr.Zero) {
                    throw new Win32Exception($"{nameof(shell32.SHGetFileInfoW)} error");
                }
                var overlayIndex = (int)(unchecked((uint)fileInfo.iIcon) >> 24);
                if (overlayIndex == 0) {
                    return null;
                }
                if (OverlayCache.TryGetValue(overlayIndex, out var cached)) {
                    return cached;
                }
                var source = GetOverlaySource(overlayIndex);
                return source is null
                    ? null
                    : OverlayCache.GetOrAdd(overlayIndex, source);
            }
            finally {
                DestroyIcon(fileInfo.hIcon);
            }
        });
    }

    private static BitmapSource GetOverlaySource(int overlayIndex) {
        var iid = IID.Managed.IImageList;
        var hIcon = default(IntPtr);
        var imageList = default(IImageList);
        try {
            var
            hr = shell32.SHGetImageList(shell32.SHIL_SMALL, ref iid, out imageList);
            hr.ThrowOnError();
            hr = imageList.GetOverlayImage(overlayIndex, out var imageIndex);
            hr.ThrowOnError();
            hr = imageList.GetIcon(imageIndex, flags: 0, out hIcon);
            hr.ThrowOnError();
            var source = Imaging.CreateBitmapSourceFromHIcon(hIcon,
                                                             Int32Rect.Empty,
                                                             BitmapSizeOptions.FromEmptyOptions());
            if (source.CanFreeze) {
                source.Freeze();
            }
            return source;
        }
        finally {
            try {
                DestroyIcon(hIcon);
            }
            finally {
                if (imageList is not null) {
                    Marshal.ReleaseComObject(imageList);
                }
            }
        }
    }

    private static void DestroyIcon(IntPtr hIcon) {
        if (hIcon == default) {
            return;
        }
        var success = user32.DestroyIcon(hIcon);
        if (success == false) {
            if (Log.Error()) {
                Log.Error(new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
    }

    private protected sealed override void DisposeCore() {
        try {
            OverlayCache.Clear();
        }
        catch (Exception ex) {
            if (Log.Warn()) {
                Log.Warn(ex);
            }
        }
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
    /// Initializes a new instance of the <see cref="Win32OverlayService"/> class.
    /// </summary>
    /// <param name="threadPool">
    /// The optional STA thread pool to use for overlay requests. If omitted, the service creates a pool.
    /// </param>
    public Win32OverlayService(STAThreadPool threadPool = null) {
        ThreadPool = threadPool ?? new(nameof(Win32OverlayService)) {
            TryWorkDelay = 10,
            WorkerCountMax = Environment.ProcessorCount,
            WorkerCountMin = 1,
        };
        ThreadPoolOwned = threadPool is null;
    }

    /// <summary>
    /// Gets the overlay icon that the Windows Shell associates with a file or folder path.
    /// </summary>
    /// <param name="path">The path of the file or folder to inspect.</param>
    /// <param name="attributes">
    /// The known file attributes for <paramref name="path"/>. Supply values obtained for that path to avoid an
    /// extra attribute query; if <see langword="null"/>, the service retrieves them from Windows.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the effective overlay icon, or <see langword="null"/> if no overlay is
    /// associated with the path.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public async Task<BitmapSource> GetOverlayIconSource(string path,
                                                         FileAttributes? attributes = null,
                                                         CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(path)) {
            throw new ArgumentNullException(nameof(path));
        }
        BeginOperation();
        try {
            if (cancellationToken.IsCancellationRequested) {
                cancellationToken.ThrowIfCancellationRequested();
            }
            var fileAttributes = attributes.HasValue
                ? unchecked((uint)attributes.Value)
                : await GetPathAttributes(path, cancellationToken).ConfigureAwait(false);
            if (fileAttributes == kernel32.INVALID_FILE_ATTRIBUTES) {
                return null;
            }
            return await GetOverlayIconAttempt(path, cancellationToken).ConfigureAwait(false);
        }
        finally {
            EndOperation();
        }
    }
}
