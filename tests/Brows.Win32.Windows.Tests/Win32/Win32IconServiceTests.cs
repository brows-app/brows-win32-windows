using Brows.Threading;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32IconServiceTests {
    [Test]
    public void Dispose_CanBeCalledMoreThanOnce() {
        var service = new Win32IconService();

        Assert.DoesNotThrow(service.Dispose);
        Assert.DoesNotThrow(service.Dispose);
    }

    [Test]
    public void GetIconSource_AfterDispose_ThrowsObjectDisposedException() {
        var service = new Win32IconService();
        service.Dispose();

        var pathException = Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await service.GetIconSource("example.txt", cancellationToken: CancellationToken.None));
        var stockException = Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await service.GetIconSource(Win32StockIcon.Folder, CancellationToken.None));

        using (Assert.EnterMultipleScope()) {
            Assert.That(pathException.ObjectName, Is.EqualTo(nameof(Win32IconService)));
            Assert.That(stockException.ObjectName, Is.EqualTo(nameof(Win32IconService)));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetIconSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        using var service = new Win32IconService();

        var exception = Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.GetIconSource(path));

        Assert.That(exception.ParamName, Is.EqualTo("path"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Attempts_WhenValueIsLessThanOne_ThrowsArgumentOutOfRangeException(int value) {
        using var service = new Win32IconService();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => service.Attempts = value);

        using (Assert.EnterMultipleScope()) {
            Assert.That(exception.ParamName, Is.EqualTo(nameof(service.Attempts)));
            Assert.That(service.Attempts, Is.EqualTo(5));
        }
    }

    [Test]
    public void AttemptDelay_WhenValueIsNegative_ThrowsArgumentOutOfRangeException() {
        using var service = new Win32IconService();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => service.AttemptDelay = -1);

        using (Assert.EnterMultipleScope()) {
            Assert.That(exception.ParamName, Is.EqualTo(nameof(service.AttemptDelay)));
            Assert.That(service.AttemptDelay, Is.EqualTo(50));
        }
    }

    [Test]
    public async Task GetIconSource_WhenStockIconIsCachedAndTokenIsCanceled_ThrowsOperationCanceledException() {
        using var service = new Win32IconService();
        var source = await service.GetIconSource(Win32StockIcon.Folder);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(source, Is.Not.Null);
        Assert.That(
            async () => await service.GetIconSource(Win32StockIcon.Folder, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task GetIconSource_WhenRetrievingStockIcon_ReturnsFrozenCachedSource() {
        using var service = new Win32IconService();

        var first = await service.GetIconSource(Win32StockIcon.Folder);
        var second = await service.GetIconSource(Win32StockIcon.Folder);

        using (Assert.EnterMultipleScope()) {
            Assert.That(first, Is.Not.Null);
            Assert.That(first.IsFrozen, Is.True);
            Assert.That(first.PixelWidth, Is.GreaterThan(0));
            Assert.That(first.PixelHeight, Is.GreaterThan(0));
            Assert.That(second, Is.SameAs(first));
        }
    }

    [Test]
    public async Task GetIconSource_WhenPathExists_ReturnsFrozenSource() {
        using var service = new Win32IconService();
        var path = typeof(Win32IconServiceTests).Assembly.Location;

        var source = await service.GetIconSource(path, File.GetAttributes(path));

        using (Assert.EnterMultipleScope()) {
            Assert.That(source, Is.Not.Null);
            Assert.That(source.IsFrozen, Is.True);
            Assert.That(source.PixelWidth, Is.GreaterThan(0));
            Assert.That(source.PixelHeight, Is.GreaterThan(0));
        }
    }

    [Test]
    [NonParallelizable]
    public async Task GetIconSource_FileTypeCacheUsesOrdinalIgnoreCaseKeys() {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        try {
            using var service = new Win32IconService();
            var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ICO");

            var upper = await service.GetIconSource(missingPath);
            var lower = await service.GetIconSource(Path.ChangeExtension(missingPath, ".ico"));

            using (Assert.EnterMultipleScope()) {
                Assert.That(upper, Is.Not.Null);
                Assert.That(lower, Is.SameAs(upper));
            }
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Test]
    public async Task Dispose_DoesNotEmptyBorrowedThreadPool() {
        var pool = new STAThreadPool(nameof(Dispose_DoesNotEmptyBorrowedThreadPool)) {
            WorkerCountMax = 1,
        };
        try {
            var workerBefore = await pool.Work(nameof(Dispose_DoesNotEmptyBorrowedThreadPool),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            using (var service = new Win32IconService(pool)) {
            }

            var workerAfter = await pool.Work(nameof(Dispose_DoesNotEmptyBorrowedThreadPool),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            Assert.That(workerAfter, Is.EqualTo(workerBefore));
        }
        finally {
            pool.Empty();
        }
    }

    [Test]
    public async Task Dispose_WaitsForAnActiveRequest() {
        var pool = new STAThreadPool(nameof(Dispose_WaitsForAnActiveRequest)) {
            WorkerCountMax = 1,
        };
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();

        var blocker = pool.Work(nameof(Dispose_WaitsForAnActiveRequest), () => {
            workerStarted.Set();
            releaseWorker.Wait();
        }, CancellationToken.None);
        var service = new Win32IconService(pool);
        try {
            Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var request = service.GetIconSource(Win32StockIcon.Folder, cancellation.Token);
            using var disposeStarted = new ManualResetEventSlim();
            var disposal = Task.Run(() => {
                disposeStarted.Set();
                service.Dispose();
            });

            Assert.That(disposeStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(disposal.Wait(TimeSpan.FromMilliseconds(100)), Is.False);

            cancellation.Cancel();
            releaseWorker.Set();
            await blocker;
            try {
                await request;
            }
            catch (OperationCanceledException) {
            }
            await disposal;
        }
        finally {
            cancellation.Cancel();
            releaseWorker.Set();
            service.Dispose();
            pool.Empty();
        }
    }

    [Test]
    public async Task GetIconSource_WhenSharedRequestWaiterIsCanceled_CancelsOnlyThatWaiter() {
        var pool = new STAThreadPool(nameof(GetIconSource_WhenSharedRequestWaiterIsCanceled_CancelsOnlyThatWaiter)) {
            WorkerCountMax = 1,
        };
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        var blocker = pool.Work(nameof(GetIconSource_WhenSharedRequestWaiterIsCanceled_CancelsOnlyThatWaiter), () => {
            workerStarted.Set();
            releaseWorker.Wait();
        }, CancellationToken.None);
        using var service = new Win32IconService(pool);
        Task<System.Windows.Media.Imaging.BitmapSource> owner = null;
        try {
            Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            owner = service.GetIconSource(Win32StockIcon.Folder);
            var waiter = service.GetIconSource(Win32StockIcon.Folder, cancellation.Token);

            cancellation.Cancel();

            var completed = await Task.WhenAny(waiter, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.That(completed, Is.SameAs(waiter));
            Assert.That(async () => await waiter, Throws.InstanceOf<OperationCanceledException>());

            releaseWorker.Set();
            Assert.That(await owner, Is.Not.Null);
        }
        finally {
            releaseWorker.Set();
            await blocker;
            if (owner is not null) {
                try {
                    await owner;
                }
                catch {
                }
            }
            pool.Empty();
        }
    }
}
