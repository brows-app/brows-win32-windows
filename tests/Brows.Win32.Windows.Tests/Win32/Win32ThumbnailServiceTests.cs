using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brows.Threading;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32ThumbnailServiceTests {
    private ISTAThreadPool ThreadPool;

    private static string CreateTemporaryDirectory() {
        var path = Path.Combine(Path.GetTempPath(), $"{nameof(Win32ThumbnailServiceTests)}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreatePng(string directory) {
        const int size = 64;
        const int stride = size * 4;
        var pixels = new byte[stride * size];
        for (var i = 0; i < pixels.Length; i++) {
            pixels[i] = (byte)(i % 251);
        }
        var source = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        var path = Path.Combine(directory, "thumbnail.png");
        using (var stream = File.Create(path)) {
            encoder.Save(stream);
        }
        return path;
    }

    private static string CreateFileWithoutThumbnailHandler(string directory) {
        var path = Path.Combine(directory, "no-thumbnail.brows-win32-test");
        File.WriteAllText(path, "No thumbnail handler is registered for this extension.");
        return path;
    }

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
        var exception = Assert.Throws<ArgumentNullException>(() => new Win32ThumbnailService(null));

        Assert.That(exception.ParamName, Is.EqualTo("threadPool"));
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetThumbnailSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        using var service = new Win32ThumbnailService(ThreadPool);

        var exception = Assert.Throws<ArgumentNullException>(
            () => service.GetThumbnailSource(path, width: 1, height: 1, throwIfNotAvailable: false));

        Assert.That(exception.ParamName, Is.EqualTo("path"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetThumbnailSource_WhenWidthIsNotPositive_ThrowsArgumentOutOfRangeException(int width) {
        using var service = new Win32ThumbnailService(ThreadPool);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetThumbnailSource("unused", width: width, height: 1, throwIfNotAvailable: false));

        Assert.That(exception.ParamName, Is.EqualTo("width"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void GetThumbnailSource_WhenHeightIsNotPositive_ThrowsArgumentOutOfRangeException(int height) {
        using var service = new Win32ThumbnailService(ThreadPool);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.GetThumbnailSource("unused", width: 1, height: height, throwIfNotAvailable: false));

        Assert.That(exception.ParamName, Is.EqualTo("height"));
    }

    [Test]
    public void GetThumbnailSource_WhenTokenIsAlreadyCanceled_CancelsBeforeShellWork() {
        using var service = new Win32ThumbnailService(ThreadPool);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await service.GetThumbnailSource(
                "unused",
                width: 1,
                height: 1,
                throwIfNotAvailable: false,
                cancellationToken: cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Dispose_CanBeCalledMoreThanOnce() {
        var service = new Win32ThumbnailService(ThreadPool);

        Assert.DoesNotThrow(service.Dispose);
        Assert.DoesNotThrow(service.Dispose);
    }

    [Test]
    public void GetThumbnailSource_AfterDispose_ThrowsObjectDisposedException() {
        var service = new Win32ThumbnailService(ThreadPool);
        service.Dispose();

        var exception = Assert.Throws<ObjectDisposedException>(() =>
            service.GetThumbnailSource("example.txt", width: 1, height: 1, throwIfNotAvailable: false));

        Assert.That(exception.ObjectName, Is.EqualTo(nameof(Win32ThumbnailService)));
    }

    [Test]
    public async Task GetThumbnailSource_WhenPathExists_ReturnsFrozenSource() {
        using var service = new Win32ThumbnailService(ThreadPool);
        var path = typeof(Win32ThumbnailServiceTests).Assembly.Location;

        var source = await service.GetThumbnailSource(path, width: 32, height: 32, throwIfNotAvailable: false);

        using (Assert.EnterMultipleScope()) {
            Assert.That(source, Is.Not.Null);
            Assert.That(source.IsFrozen, Is.True);
            Assert.That(source.PixelWidth, Is.GreaterThan(0));
            Assert.That(source.PixelHeight, Is.GreaterThan(0));
        }
    }

    [Test]
    public async Task GetThumbnailSource_WhenThumbnailIsAvailableAndThrowIfNotAvailable_ReturnsFrozenSource() {
        using var service = new Win32ThumbnailService(ThreadPool);
        var directory = CreateTemporaryDirectory();
        try {
            var path = CreatePng(directory);

            var source = await service.GetThumbnailSource(path, width: 32, height: 32, throwIfNotAvailable: true);

            using (Assert.EnterMultipleScope()) {
                Assert.That(source, Is.Not.Null);
                Assert.That(source.IsFrozen, Is.True);
                Assert.That(source.PixelWidth, Is.GreaterThan(0));
                Assert.That(source.PixelHeight, Is.GreaterThan(0));
            }
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task GetThumbnailSource_WhenThumbnailIsNotAvailableAndThrowIfNotAvailable_ThrowsWin32ThumbnailException() {
        using var service = new Win32ThumbnailService(ThreadPool);
        var directory = CreateTemporaryDirectory();
        try {
            var path = CreateFileWithoutThumbnailHandler(directory);

            var exception = await Assert.ThrowsAsync<Win32ThumbnailException>(async () =>
                await service.GetThumbnailSource(path, width: 32, height: 32, throwIfNotAvailable: true));

            Assert.That(exception.InnerException, Is.Not.Null);
            using (Assert.EnterMultipleScope()) {
                Assert.That(exception.HResult, Is.EqualTo(exception.InnerException.HResult));
                Assert.That(exception.Message, Does.Contain(path));
            }
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task GetThumbnailSource_WhenThumbnailIsNotAvailableAndNotThrowIfNotAvailable_ReturnsFrozenIcon() {
        using var service = new Win32ThumbnailService(ThreadPool);
        var directory = CreateTemporaryDirectory();
        try {
            var path = CreateFileWithoutThumbnailHandler(directory);

            var source = await service.GetThumbnailSource(path, width: 32, height: 32, throwIfNotAvailable: false);

            using (Assert.EnterMultipleScope()) {
                Assert.That(source, Is.Not.Null);
                Assert.That(source.IsFrozen, Is.True);
                Assert.That(source.PixelWidth, Is.GreaterThan(0));
                Assert.That(source.PixelHeight, Is.GreaterThan(0));
            }
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task GetThumbnailSource_WhenPathIsMissingAndThrowIfNotAvailable_ThrowsWin32ThumbnailException() {
        using var service = new Win32ThumbnailService(ThreadPool);
        var directory = CreateTemporaryDirectory();
        try {
            var path = Path.Combine(directory, "missing.png");

            var exception = await Assert.ThrowsAsync<Win32ThumbnailException>(async () =>
                await service.GetThumbnailSource(path, width: 32, height: 32, throwIfNotAvailable: true));

            Assert.That(exception.InnerException, Is.Not.Null);
            Assert.That(exception.HResult, Is.EqualTo(exception.InnerException.HResult));
        }
        finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task Dispose_DoesNotEmptySuppliedThreadPool() {
        ISTAThreadPool pool = new STAThreadPool(nameof(Dispose_DoesNotEmptySuppliedThreadPool)) {
            WorkerCountMax = 1,
        };
        try {
            var workerBefore = await pool.Work(nameof(Dispose_DoesNotEmptySuppliedThreadPool),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            using (var service = new Win32ThumbnailService(pool)) {
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
        var service = new Win32ThumbnailService(pool);
        try {
            Assert.That(workerStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);

            var request = service.GetThumbnailSource("unused", width: 1, height: 1, throwIfNotAvailable: false,
                cancellation.Token);
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
