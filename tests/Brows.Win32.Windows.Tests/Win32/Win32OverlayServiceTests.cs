using Brows.Threading;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32OverlayServiceTests {
    private ISTAThreadPool ThreadPool;

    [SetUp]
    public void SetUp() {
        ThreadPool = new STAThreadPool(TestContext.CurrentContext.Test.Name);
    }

    [TearDown]
    public void TearDown() {
        ThreadPool.Empty();
    }

    [Test]
    public void Constructor_WhenThreadPoolIsNull_ThrowsArgumentNullException() {
        var exception = Assert.Throws<ArgumentNullException>(() => new Win32OverlayService(null));

        Assert.That(exception.ParamName, Is.EqualTo("threadPool"));
    }

    [Test]
    public void Dispose_CanBeCalledMoreThanOnce() {
        var service = new Win32OverlayService(ThreadPool);

        Assert.DoesNotThrow(service.Dispose);
        Assert.DoesNotThrow(service.Dispose);
    }

    [Test]
    public async Task GetOverlayIconSource_AfterDispose_ThrowsObjectDisposedException() {
        var service = new Win32OverlayService(ThreadPool);
        service.Dispose();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await service.GetOverlayIconSource("unused"));

        Assert.That(exception.ObjectName, Is.EqualTo(nameof(Win32OverlayService)));
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GetOverlayIconSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        using var service = new Win32OverlayService(ThreadPool);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await service.GetOverlayIconSource(path));

        Assert.That(exception.ParamName, Is.EqualTo("path"));
    }

    [Test]
    public void GetOverlayIconSource_WhenTokenIsAlreadyCanceled_CancelsBeforePathOrShellWork() {
        using var service = new Win32OverlayService(ThreadPool);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await service.GetOverlayIconSource("unused", cancellationToken: cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task Dispose_DoesNotEmptySuppliedThreadPool() {
        ISTAThreadPool pool = new STAThreadPool(nameof(Dispose_DoesNotEmptySuppliedThreadPool)) {
            WorkerCountMax = 1,
        };
        try {
            var workerBefore = await pool.Work(nameof(Dispose_DoesNotEmptySuppliedThreadPool),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            using (var service = new Win32OverlayService(pool)) {
            }

            var workerAfter = await pool.Work(nameof(Dispose_DoesNotEmptySuppliedThreadPool),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            Assert.That(workerAfter, Is.EqualTo(workerBefore));
        }
        finally {
            pool.Empty();
        }
    }

    [Test]
    public async Task GetOverlayIconSource_WhenPathDoesNotExist_ReturnsNull() {
        using var service = new Win32OverlayService(ThreadPool);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var source = await service.GetOverlayIconSource(path);

        Assert.That(source, Is.Null);
    }

    [Test]
    public async Task Dispose_WaitsForAnActiveRequest() {
        ISTAThreadPool pool = new STAThreadPool(nameof(Dispose_WaitsForAnActiveRequest)) {
            WorkerCountMax = 1,
        };
        using var workerStarted = new ManualResetEventSlim();
        using var releaseWorker = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();

        var blocker = pool.Work(nameof(Dispose_WaitsForAnActiveRequest), () => {
            workerStarted.Set();
            releaseWorker.Wait();
        }, CancellationToken.None);
        var service = new Win32OverlayService(pool);
        try {
            Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var request = service.GetOverlayIconSource(
                "unused",
                attributes: FileAttributes.Normal,
                cancellationToken: cancellation.Token);
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
