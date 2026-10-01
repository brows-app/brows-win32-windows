using Brows.Threading;
using Brows.Win32.PlatformInvoke;
using Domore.Logs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

/// <summary>
/// Retrieves file and Windows stock icons as frozen WPF bitmap sources.
/// </summary>
public sealed class Win32IconService : Win32BaseService {
    private static readonly ILog Log = Logging.For(typeof(Win32IconService));

    private readonly SemaphoreSlim ExtensionLock = new(1, 1);
    private readonly Dictionary<string, Task<BitmapSource>> ExtensionTasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, BitmapSource> ExtensionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim StockLock = new(1, 1);
    private readonly Dictionary<Win32StockIcon, Task<BitmapSource>> StockTasks = [];
    private readonly ConcurrentDictionary<Win32StockIcon, BitmapSource> StockCache = [];

    private readonly bool ThreadPoolOwned;
    private readonly STAThreadPool ThreadPool;

    private async Task<BitmapSource> Attempt<TArg>(TArg arg,
                                                   Func<TArg, CancellationToken, Task<BitmapSource>> task,
                                                   CancellationToken cancellationToken) {
        if (task is null) {
            throw new ArgumentNullException(nameof(task));
        }
        var attempt = 1;
        for (; ; ) {
            try {
                return await task(arg, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                if (attempt++ >= Attempts) {
                    if (Log.Warn()) {
                        Log.Warn(ex);
                    }
                    return null;
                }
                if (Log.Debug()) {
                    Log.Debug(ex);
                }
                await Task.Delay(AttemptDelay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task<BitmapSource> GetPathIconAttempt(string path, CancellationToken cancellationToken) {
        const SHGFI uFlags = SHGFI.ICON | SHGFI.SMALLICON;
        return GetIconAttempt(path, default, uFlags, cancellationToken);
    }

    private Task<BitmapSource> GetFileTypeIconAttempt(string extension, CancellationToken cancellationToken) {
        const FILE_ATTRIBUTE dwFileAttributes = FILE_ATTRIBUTE.NORMAL;
        const SHGFI uFlags = SHGFI.USEFILEATTRIBUTES | SHGFI.ICON | SHGFI.SMALLICON;
        return GetIconAttempt(extension, dwFileAttributes, uFlags, cancellationToken);
    }

    private Task<BitmapSource> GetIconAttempt(string path,
                                              FILE_ATTRIBUTE dwFileAttributes,
                                              SHGFI uFlags,
                                              CancellationToken cancellationToken) {
        return ThreadPool.Work(nameof(GetIconAttempt), cancellationToken: cancellationToken, work: () => {
            var pszPath = path;
            var psfi = new SHFILEINFOW();
            var cbFileInfo = SHFILEINFOW.Size;
            try {
                var returnValue = shell32.SHGetFileInfoW(pszPath, dwFileAttributes, ref psfi, cbFileInfo, uFlags);
                if (returnValue == IntPtr.Zero) {
                    throw new Win32Exception($"{nameof(shell32.SHGetFileInfoW)} error");
                }
                var source = Imaging.CreateBitmapSourceFromHIcon(psfi.hIcon,
                                                                 Int32Rect.Empty,
                                                                 BitmapSizeOptions.FromEmptyOptions());
                if (source.CanFreeze) {
                    source.Freeze();
                }
                return source;
            }
            finally {
                var hIcon = psfi.hIcon;
                if (hIcon != default) {
                    var success = user32.DestroyIcon(hIcon);
                    if (success == false) {
                        if (Log.Error()) {
                            Log.Error(new Win32Exception(Marshal.GetLastWin32Error()));
                        }
                    }
                }
            }
        });
    }

    private static Task<uint> GetPathAttributes(string path, CancellationToken cancellationToken) {
        return Task.Run(() => kernel32.GetFileAttributesW(path), cancellationToken);
    }

    private Task<BitmapSource> GetStockIconAttempt(Win32StockIcon stockIcon, CancellationToken cancellationToken) {
        const SHGSI uFlags = SHGSI.ICON | SHGSI.SMALLICON;
        return ThreadPool.Work(nameof(GetStockIconAttempt), cancellationToken: cancellationToken, work: () => {
            var siid = (SHSTOCKICONID)(uint)stockIcon;
            var
            psii = new SHSTOCKICONINFO();
            psii.cbSize = SHSTOCKICONINFO.Size;
            try {
                var
                hr = shell32.SHGetStockIconInfo(siid, uFlags, ref psii);
                hr.ThrowOnError();
                var source = Imaging.CreateBitmapSourceFromHIcon(psii.hIcon,
                                                                 Int32Rect.Empty,
                                                                 BitmapSizeOptions.FromEmptyOptions());
                if (source.CanFreeze) {
                    source.Freeze();
                }
                return source;
            }
            finally {
                var hIcon = psii.hIcon;
                if (hIcon != default) {
                    var success = user32.DestroyIcon(hIcon);
                    if (success == false) {
                        if (Log.Error()) {
                            Log.Error(new Win32Exception(Marshal.GetLastWin32Error()));
                        }
                    }
                }
            }
        });
    }

    private static async Task<TResult> WaitForTask<TResult>(Task<TResult> task,
                                                            CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            cancellationToken.ThrowIfCancellationRequested();
        }
        if (!cancellationToken.CanBeCanceled || task.IsCompleted) {
            return await task.ConfigureAwait(false);
        }
        var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => canceled.TrySetResult(true))) {
            if (task != await Task.WhenAny(task, canceled.Task).ConfigureAwait(false)) {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        return await task.ConfigureAwait(false);
    }

    private static async Task<BitmapSource> Get<TKey>(TKey key,
                                                      ConcurrentDictionary<TKey, BitmapSource> cache,
                                                      Dictionary<TKey, Task<BitmapSource>> tasks,
                                                      SemaphoreSlim locker,
                                                      Func<CancellationToken, Task<BitmapSource>> factory,
                                                      CancellationToken cancellationToken) {
        if (cache is null) {
            throw new ArgumentNullException(nameof(cache));
        }
        if (tasks is null) {
            throw new ArgumentNullException(nameof(tasks));
        }
        if (locker is null) {
            throw new ArgumentNullException(nameof(locker));
        }
        if (factory is null) {
            throw new ArgumentNullException(nameof(factory));
        }
        for (; ; ) {
            if (cache.TryGetValue(key, out var value)) {
                return value;
            }
            Task<BitmapSource> task;
            var taskOwner = false;
            await locker.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (cache.TryGetValue(key, out value)) {
                    return value;
                }
                if (tasks.TryGetValue(key, out task) == false) {
                    tasks.Add(key, task = factory(cancellationToken));
                    taskOwner = true;
                }
            }
            finally {
                locker.Release();
            }
            try {
                value = taskOwner
                    ? await task.ConfigureAwait(false)
                    : await WaitForTask(task, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                if (taskOwner) {
                    await removeTask(key, tasks, locker, task).ConfigureAwait(false);
                }
                throw;
            }
            catch (OperationCanceledException) when (taskOwner == false && task.IsCanceled) {
                await removeTask(key, tasks, locker, task).ConfigureAwait(false);
                continue;
            }
            catch (Exception ex) {
                if (Log.Debug()) {
                    Log.Debug(ex);
                }
                await removeTask(key, tasks, locker, task).ConfigureAwait(false);
                return null;
            }
            await locker.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try {
                if (cache.TryGetValue(key, out var cached)) {
                    value = cached;
                }
                else if (value is not null) {
                    cache[key] = value;
                }
                if (tasks.TryGetValue(key, out var current) && ReferenceEquals(current, task)) {
                    tasks.Remove(key);
                }
            }
            finally {
                locker.Release();
            }
            return value;
        }
        static async Task removeTask(TKey key,
                                     Dictionary<TKey, Task<BitmapSource>> tasks,
                                     SemaphoreSlim locker,
                                     Task<BitmapSource> task) {
            await locker.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try {
                if (tasks.TryGetValue(key, out var current) && ReferenceEquals(current, task)) {
                    tasks.Remove(key);
                }
            }
            finally {
                locker.Release();
            }
        }
    }

    private Task<BitmapSource> GetStockIconSource(Win32StockIcon stockIcon, CancellationToken cancellationToken) {
        return Get(
            key: stockIcon,
            cache: StockCache,
            tasks: StockTasks,
            locker: StockLock,
            factory: t => Attempt(stockIcon, GetStockIconAttempt, t),
            cancellationToken: cancellationToken);
    }

    private protected sealed override void DisposeCore() {
        void @try(Action action) {
            try {
                action?.Invoke();
            }
            catch (Exception ex) {
                if (Log.Warn()) {
                    Log.Warn(ex);
                }
            }
        }
        @try(() => {
            if (ThreadPoolOwned) {
                ThreadPool.Empty();
            }
        });
        @try(StockCache.Clear);
        @try(StockTasks.Clear);
        @try(StockLock.Dispose);
        @try(ExtensionCache.Clear);
        @try(ExtensionTasks.Clear);
        @try(ExtensionLock.Dispose);
    }

    internal const int AttemptDelayDefault = 100;
    internal const int AttemptsDefault = 5;

    /// <summary>
    /// Gets or sets the maximum number of attempts for an icon retrieval.
    /// </summary>
    /// <remarks>The default is 5 attempts.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is less than one.</exception>
    public int Attempts {
        get;
        set {
            if (value < 1) {
                throw new ArgumentOutOfRangeException(nameof(Attempts), value,
                    "The number of attempts must be at least one.");
            }
            field = value;
        }
    } = AttemptsDefault;

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between failed attempts.
    /// </summary>
    /// <remarks>The default delay is 100 milliseconds.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    public int AttemptDelay {
        get;
        set {
            if (value < 0) {
                throw new ArgumentOutOfRangeException(nameof(AttemptDelay), value,
                    "The attempt delay cannot be negative.");
            }
            field = value;
        }
    } = AttemptDelayDefault;

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32IconService"/> class.
    /// </summary>
    /// <param name="threadPool">
    /// The STA thread pool to use. If <see langword="null"/>, the service creates and owns a pool.
    /// </param>
    public Win32IconService(STAThreadPool threadPool = null) {
        ThreadPool = threadPool ?? new(nameof(Win32IconService)) {
            TryWorkDelay = 10,
            WorkerCountMax = Environment.ProcessorCount,
            WorkerCountMin = 1,
        };
        ThreadPoolOwned = threadPool is null;
    }

    /// <summary>
    /// Gets the icon associated with an existing file or folder, or the generic icon for a file type.
    /// </summary>
    /// <param name="path">
    /// The file or folder path whose icon is requested. Existing paths use their path-specific icon; paths that
    /// do not exist use the generic icon associated with their extension.
    /// </param>
    /// <param name="attributes">
    /// The known file attributes for <paramref name="path"/>. Supply values obtained for that path to avoid an
    /// extra attribute query; if <see langword="null"/>, the service retrieves them from Windows.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the icon, or <see langword="null"/> if the Shell cannot retrieve it
    /// after the configured attempts.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/> or empty.</exception>
    public async Task<BitmapSource> GetIconSource(string path,
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
            if (fileAttributes != kernel32.INVALID_FILE_ATTRIBUTES) {
                var source = await Attempt(path, GetPathIconAttempt, cancellationToken).ConfigureAwait(false);
                if (source is not null) {
                    return source;
                }
                if ((fileAttributes & (uint)FILE_ATTRIBUTE.DIRECTORY) != 0) {
                    return await GetStockIconSource(Win32StockIcon.Folder, cancellationToken).ConfigureAwait(false);
                }
            }

            var ext = Path.GetExtension(path)?.Trim() ?? "";
            if (ext == "") {
                return await GetStockIconSource(Win32StockIcon.DocNoAssoc, cancellationToken).ConfigureAwait(false);
            }
            var key = ext;
            return await Get(
                key: key,
                cache: ExtensionCache,
                tasks: ExtensionTasks,
                locker: ExtensionLock,
                factory: t => Attempt(key, GetFileTypeIconAttempt, t),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        finally {
            EndOperation();
        }
    }

    /// <summary>
    /// Gets a Windows stock icon.
    /// </summary>
    /// <param name="stockIcon">The stock icon to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the request.</param>
    /// <returns>
    /// A task that completes with the icon, or <see langword="null"/> if the Shell cannot retrieve it
    /// after the configured attempts.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The service has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The request was canceled.</exception>
    public async Task<BitmapSource> GetIconSource(Win32StockIcon stockIcon,
                                                  CancellationToken cancellationToken = default) {
        BeginOperation();
        try {
            if (cancellationToken.IsCancellationRequested) {
                cancellationToken.ThrowIfCancellationRequested();
            }
            return await GetStockIconSource(stockIcon, cancellationToken).ConfigureAwait(false);
        }
        finally {
            EndOperation();
        }
    }
}
