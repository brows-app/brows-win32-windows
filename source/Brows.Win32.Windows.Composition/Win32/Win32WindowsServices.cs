using Brows.Composition;
using Brows.Threading;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Brows.Win32;

internal sealed class Win32WindowsServices : IWin32WindowsServices,
                                             IExportAndVary<Win32WindowsServicesVariable>,
                                             IExportAndKill {
    private readonly Lazy<ServiceWrapper> Services;

    private bool ThreadPoolOwned { get; set; }
    private STAThreadPool ThreadPool { get; set; }

    public Win32WindowsServices() {
        Services = new(() => {
            return new(ThreadPool);
        });
    }

    Task<BitmapSource> IWin32WindowsServices.GetIconSource(string path,
                                                           FileAttributes? attributes,
                                                           CancellationToken cancellationToken) {
        return Services.Value.Icon.GetIconSource(path, attributes, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetIconSource(Win32StockIcon stockIcon,
                                                           CancellationToken cancellationToken) {
        return Services.Value.Icon.GetIconSource(stockIcon, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetOverlayIconSource(string path,
                                                                  FileAttributes? attributes,
                                                                  CancellationToken cancellationToken) {
        return Services.Value.Overlay.GetOverlayIconSource(path, attributes, cancellationToken);
    }

    Task<BitmapSource> IWin32WindowsServices.GetThumbnailSource(string path,
                                                                int width,
                                                                int height,
                                                                CancellationToken cancellationToken) {
        return Services.Value.Thumbnail.GetThumbnailSource(path, width, height, cancellationToken);
    }

    Task IExportAndVary<Win32WindowsServicesVariable>.Vary(Win32WindowsServicesVariable variable,
                                                           CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return Task.FromCanceled(cancellationToken);
        }
        var
        threadPool = variable?.ThreadPool;
        ThreadPool = threadPool ?? new(nameof(Win32WindowsServices)) {
            IdleTime = TimeSpan.FromMinutes(2),
            TryWorkDelay = 10,
            WorkerCountMax = Environment.ProcessorCount,
            WorkerCountMin = 1,
        };
        ThreadPoolOwned = threadPool is null;
        return Task.CompletedTask;
    }

    void IExportAndKill.Kill() {
        try {
            if (Services.IsValueCreated) {
                Services.Value.Dispose();
            }
        }
        finally {
            if (ThreadPoolOwned) {
                ThreadPool.Empty();
            }
        }
    }

    private sealed class ServiceWrapper : IDisposable {
        public Win32IconService Icon { get; }
        public Win32OverlayService Overlay { get; }
        public Win32ThumbnailService Thumbnail { get; }

        public STAThreadPool ThreadPool { get; }

        public ServiceWrapper(STAThreadPool threadPool) {
            ThreadPool = threadPool;
            Icon = new(ThreadPool);
            Overlay = new(ThreadPool);
            Thumbnail = new(ThreadPool);
        }

        public void Dispose() {
            using (Icon) {
                using (Overlay) {
                    using (Thumbnail) {
                    }
                }
            }
        }
    }
}
