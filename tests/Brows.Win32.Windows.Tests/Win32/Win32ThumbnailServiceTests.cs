using System;
using System.Threading;
using System.Threading.Tasks;
using Brows.Threading;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32ThumbnailServiceTests {
    [TestCase(null)]
    [TestCase("")]
    public void GetThumbnailSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        using var service = new Win32ThumbnailService();

        var exception = Assert.Throws<ArgumentNullException>(
            () => service.GetThumbnailSource(path, width: 1, height: 1));

        Assert.That(exception.ParamName, Is.EqualTo("path"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetThumbnailSource_WhenWidthIsNotPositive_ThrowsArgumentOutOfRangeException(int width) {
        using var service = new Win32ThumbnailService();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetThumbnailSource("unused", width: width, height: 1));

        Assert.That(exception.ParamName, Is.EqualTo("width"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetThumbnailSource_WhenHeightIsNotPositive_ThrowsArgumentOutOfRangeException(int height) {
        using var service = new Win32ThumbnailService();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetThumbnailSource("unused", width: 1, height: height));

        Assert.That(exception.ParamName, Is.EqualTo("height"));
    }

    [Test]
    public void GetThumbnailSource_WhenTokenIsAlreadyCanceled_CancelsBeforeShellWork() {
        using var service = new Win32ThumbnailService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await service.GetThumbnailSource(
                "unused",
                width: 1,
                height: 1,
                cancellationToken: cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Dispose_CanBeCalledMoreThanOnce() {
        var service = new Win32ThumbnailService();

        Assert.DoesNotThrow(service.Dispose);
        Assert.DoesNotThrow(service.Dispose);
    }

    [Test]
    public void GetThumbnailSource_AfterDispose_ThrowsObjectDisposedException() {
        var service = new Win32ThumbnailService();
        service.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() =>
            service.GetThumbnailSource("example.txt", width: 1, height: 1));

        Assert.That(exception.ObjectName, Is.EqualTo(nameof(Win32ThumbnailService)));
    }

    [Test]
    public async Task GetThumbnailSource_WhenPathExists_ReturnsFrozenSource() {
        using var service = new Win32ThumbnailService();
        var path = typeof(Win32ThumbnailServiceTests).Assembly.Location;

        var source = await service.GetThumbnailSource(path, width: 32, height: 32);

        using (Assert.EnterMultipleScope()) {
            Assert.That(source, Is.Not.Null);
            Assert.That(source.IsFrozen, Is.True);
            Assert.That(source.PixelWidth, Is.GreaterThan(0));
            Assert.That(source.PixelHeight, Is.GreaterThan(0));
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

            using (var service = new Win32ThumbnailService(pool)) {
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
        var service = new Win32ThumbnailService(pool);
        try {
            Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var request = service.GetThumbnailSource("unused", width: 1, height: 1, cancellation.Token);
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
            Assert.That(async () => await request, Throws.InstanceOf<OperationCanceledException>());
            await disposal;
        }
        finally {
            cancellation.Cancel();
            releaseWorker.Set();
            service.Dispose();
            pool.Empty();
        }
    }
}
