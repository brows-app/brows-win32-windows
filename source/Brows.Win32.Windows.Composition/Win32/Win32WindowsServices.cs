using Brows.Composition;
using Brows.Threading;
using Domore.Logs;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

internal sealed class Win32WindowsServices : IWin32WindowsServices,
                                             IExportAndVary<Win32WindowsServicesVariable>,
                                             IExportAndKill {
    private static readonly ILog Log = Logging.For(typeof(Win32WindowsServices));

    private readonly Lazy<ServiceWrapper> LazyServices;
    private readonly
#if NET9_0_OR_GREATER
        Lock
#else
        object
#endif
        Locker = new();

    private bool Killed;
    private bool ThreadPoolOwned { get; set; }
    private STAThreadPool ThreadPool { get; set; }
    private int IconAttempts { get; set; } = Win32IconService.AttemptsDefault;
    private int IconAttemptDelay { get; set; } = Win32IconService.AttemptDelayDefault;

    private ServiceWrapper Services {
        get {
            lock (Locker) {
                if (Killed) {
                    throw new InvalidOperationException("The Win32 Windows services have already been killed.");
                }
                return LazyServices.Value;
            }
        }
    }

    public Win32WindowsServices() {
        LazyServices = new(() => {
            if (ThreadPool is null) {
                ThreadPool = new STAThreadPool(nameof(Win32WindowsServices)) {
                    IdleTime = TimeSpan.FromMinutes(2),
                    TryWorkDelay = 10,
                    WorkerCountMax = Environment.ProcessorCount,
                    WorkerCountMin = 1,
                };
                ThreadPoolOwned = true;
            }
            if (Log.Info()) {
                Log.Info($"Creating Win32 Windows services with thread pool: {ThreadPool.Name}");
            }
            return new(ThreadPool, iconAttempts: IconAttempts, iconAttemptDelay: IconAttemptDelay);
        });
    }

    Task<BitmapSource> IWin32WindowsServices.GetIconSource(string path,
                                                           FileAttributes? attributes,
                                                           CancellationToken cancellationToken) {
        return Services.Icon.GetIconSource(path, attributes, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetIconSource(Win32StockIcon stockIcon,
                                                           CancellationToken cancellationToken) {
        return Services.Icon.GetIconSource(stockIcon, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetOverlayIconSource(string path,
                                                                  FileAttributes? attributes,
                                                                  CancellationToken cancellationToken) {
        return Services.Overlay.GetOverlayIconSource(path, attributes, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetThumbnailSource(string path,
                                                                int width,
                                                                int height,
                                                                CancellationToken cancellationToken) {
        return Services.Thumbnail.GetThumbnailSource(path, width, height, cancellationToken);
    }

    Task IExportAndVary<Win32WindowsServicesVariable>.Vary(Win32WindowsServicesVariable variable,
                                                           CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        lock (Locker) {
            if (Killed) {
                throw new InvalidOperationException("The Win32 Windows services have already been killed.");
            }
            if (LazyServices.IsValueCreated) {
                throw new InvalidOperationException("The Win32 Windows services have already been created.");
            }
            ThreadPool = variable?.ThreadPool;
            ThreadPoolOwned = false;
            IconAttemptDelay = variable?.IconAttemptDelay ?? IconAttemptDelay;
            IconAttempts = variable?.IconAttemps ?? IconAttempts;
        }
        return Task.CompletedTask;
    }

    void IExportAndKill.Kill() {
        ServiceWrapper services;
        STAThreadPool threadPool;
        bool threadPoolOwned;
        lock (Locker) {
            if (Killed) {
                return;
            }
            Killed = true;
            services = LazyServices.IsValueCreated ? LazyServices.Value : null;
            threadPool = ThreadPool;
            threadPoolOwned = ThreadPoolOwned;
        }
        try {
            services?.Dispose();
        }
        catch (Exception ex) {
            if (Log.Error()) {
                Log.Error(ex);
            }
        }
        try {
            if (threadPoolOwned) {
                threadPool.Empty();
            }
        }
        catch (Exception ex) {
            if (Log.Error()) {
                Log.Error(ex);
            }
        }
    }

    private sealed class ServiceWrapper : IDisposable {
        public Win32IconService Icon { get; }
        public Win32OverlayService Overlay { get; }
        public Win32ThumbnailService Thumbnail { get; }

        public STAThreadPool ThreadPool { get; }

        public ServiceWrapper(STAThreadPool threadPool, int iconAttempts, int iconAttemptDelay) {
            ThreadPool = threadPool;
            Icon = new(ThreadPool) {
                AttemptDelay = iconAttemptDelay,
                Attempts = iconAttempts,
            };
            Overlay = new(ThreadPool) {
            };
            Thumbnail = new(ThreadPool) {
            };
        }

        public void Dispose() {
            try {
                Icon.Dispose();
            }
            catch (Exception ex) {
                if (Log.Error()) {
                    Log.Error(ex);
                }
            }
            try {
                Overlay.Dispose();
            }
            catch (Exception ex) {
                if (Log.Error()) {
                    Log.Error(ex);
                }
            }
            try {
                Thumbnail.Dispose();
            }
            catch (Exception ex) {
                if (Log.Error()) {
                    Log.Error(ex);
                }
            }
        }
    }
}
