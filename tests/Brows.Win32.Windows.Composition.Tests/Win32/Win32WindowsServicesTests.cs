using Brows.Composition;
using Brows.Threading;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Brows.Win32;

[TestFixture]
public sealed class Win32WindowsServicesTests {
    [Test]
    public void Assembly_ExposesOneDiscoverableWindowsServicesExport() {
        var exportTypes = typeof(IWin32WindowsServices).Assembly.GetTypes()
            .Where(type => type.IsClass &&
                           !type.IsAbstract &&
                           typeof(IWin32WindowsServices).IsAssignableFrom(type))
            .ToArray();

        Assert.That(exportTypes, Has.Length.EqualTo(1));

        var exportType = exportTypes[0];
        using (Assert.EnterMultipleScope()) {
            Assert.That(exportType, Is.EqualTo(typeof(Win32WindowsServices)));
            Assert.That(typeof(IExport).IsAssignableFrom(exportType), Is.True);
            Assert.That(exportType.GetConstructor(Type.EmptyTypes), Is.Not.Null);
            Assert.That(typeof(IExportAndVary<Win32WindowsServicesVariable>).IsAssignableFrom(exportType), Is.True);
            Assert.That(typeof(IExportAndKill).IsAssignableFrom(exportType), Is.True);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GetIconSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        var implementation = new Win32WindowsServices();
        try {
            var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IWin32WindowsServices)implementation).GetIconSource(path));

            Assert.That(exception.ParamName, Is.EqualTo("path"));
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GetOverlayIconSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        var implementation = new Win32WindowsServices();
        try {
            var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IWin32WindowsServices)implementation).GetOverlayIconSource(path));

            Assert.That(exception.ParamName, Is.EqualTo("path"));
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [TestCase(null)]
    [TestCase("")]
    public async Task GetThumbnailSource_WhenPathIsNullOrEmpty_ThrowsArgumentNullException(string path) {
        var implementation = new Win32WindowsServices();
        try {
            var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((IWin32WindowsServices)implementation).GetThumbnailSource(path, width: 1, height: 1));

            Assert.That(exception.ParamName, Is.EqualTo("path"));
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task GetThumbnailSource_WhenWidthIsNotPositive_ThrowsArgumentOutOfRangeException(int width) {
        var implementation = new Win32WindowsServices();
        try {
            var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await ((IWin32WindowsServices)implementation).GetThumbnailSource("unused", width, height: 1));

            Assert.That(exception.ParamName, Is.EqualTo("width"));
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task GetThumbnailSource_WhenHeightIsNotPositive_ThrowsArgumentOutOfRangeException(int height) {
        var implementation = new Win32WindowsServices();
        try {
            var exception = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await ((IWin32WindowsServices)implementation).GetThumbnailSource("unused", width: 1, height));

            Assert.That(exception.ParamName, Is.EqualTo("height"));
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [Test]
    public async Task ForwardedOperations_WhenTokenIsCanceled_CancelBeforeShellWork() {
        var implementation = new Win32WindowsServices();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try {
            var services = (IWin32WindowsServices)implementation;

            Assert.That(async () => await services.GetIconSource(
                    "unused.txt", FileAttributes.Normal, cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(async () => await services.GetIconSource(
                    Win32StockIcon.Folder, cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(async () => await services.GetOverlayIconSource(
                    "unused.txt", FileAttributes.Normal, cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(async () => await services.GetThumbnailSource(
                    "unused.txt", width: 1, height: 1, cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());
        }
        finally {
            ((IExportAndKill)implementation).Kill();
        }
    }

    [Test]
    public async Task GetIconSource_UsesExportAndKeepsBorrowedThreadPoolAliveAfterKill() {
        var pool = new STAThreadPool(nameof(GetIconSource_UsesExportAndKeepsBorrowedThreadPoolAliveAfterKill)) {
            WorkerCountMax = 1,
        };
        var implementation = new Win32WindowsServices();
        var vary = (IExportAndVary<Win32WindowsServicesVariable>)implementation;
        var kill = (IExportAndKill)implementation;

        try {
            await vary.Vary(new Win32WindowsServicesVariable { ThreadPool = pool }, CancellationToken.None);
            var workerBefore = await pool.Work(nameof(GetIconSource_UsesExportAndKeepsBorrowedThreadPoolAliveAfterKill),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            var source = await ((IWin32WindowsServices)implementation).GetIconSource(Win32StockIcon.Folder);

            using (Assert.EnterMultipleScope()) {
                Assert.That(source, Is.Not.Null);
                Assert.That(source.IsFrozen, Is.True);
            }

            kill.Kill();
            kill.Kill();

            var workerAfter = await pool.Work(nameof(GetIconSource_UsesExportAndKeepsBorrowedThreadPoolAliveAfterKill),
                () => Thread.CurrentThread.ManagedThreadId, CancellationToken.None);

            Assert.That(workerAfter, Is.EqualTo(workerBefore));
        }
        finally {
            kill.Kill();
            pool.Empty();
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Vary_WhenIconAttemptsIsLessThanOne_ThrowsArgumentOutOfRangeException(int attempts) {
        var implementation = new Win32WindowsServices();
        var vary = (IExportAndVary<Win32WindowsServicesVariable>)implementation;
        var kill = (IExportAndKill)implementation;

        try {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => vary.Vary(
                new Win32WindowsServicesVariable { IconAttempts = attempts }, CancellationToken.None));

            using (Assert.EnterMultipleScope()) {
                Assert.That(exception.ParamName, Is.EqualTo("variable"));
                Assert.That(exception.ActualValue, Is.EqualTo(attempts));
            }
        }
        finally {
            kill.Kill();
        }
    }

    [TestCase(-1)]
    public void Vary_WhenIconAttemptDelayIsNegative_ThrowsArgumentOutOfRangeException(int delay) {
        var implementation = new Win32WindowsServices();
        var vary = (IExportAndVary<Win32WindowsServicesVariable>)implementation;
        var kill = (IExportAndKill)implementation;

        try {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => vary.Vary(
                new Win32WindowsServicesVariable { IconAttemptDelay = delay }, CancellationToken.None));

            using (Assert.EnterMultipleScope()) {
                Assert.That(exception.ParamName, Is.EqualTo("variable"));
                Assert.That(exception.ActualValue, Is.EqualTo(delay));
            }
        }
        finally {
            kill.Kill();
        }
    }

    [Test]
    public void Vary_WhenRetrySettingsAreAtMinimumAllowedValues_Completes() {
        var implementation = new Win32WindowsServices();
        var vary = (IExportAndVary<Win32WindowsServicesVariable>)implementation;
        var kill = (IExportAndKill)implementation;

        try {
            Assert.DoesNotThrow(() => vary.Vary(new Win32WindowsServicesVariable {
                IconAttempts = 1,
                IconAttemptDelay = 0,
            }, CancellationToken.None).GetAwaiter().GetResult());
        }
        finally {
            kill.Kill();
        }
    }

    [Test]
    public async Task Vary_AfterServicesAreCreated_ThrowsInvalidOperationException() {
        var implementation = new Win32WindowsServices();
        var services = (IWin32WindowsServices)implementation;
        var vary = (IExportAndVary<Win32WindowsServicesVariable>)implementation;
        var kill = (IExportAndKill)implementation;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try {
            Assert.That(async () => await services.GetIconSource(Win32StockIcon.Folder, cancellation.Token),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.Throws<InvalidOperationException>(() => vary.Vary(null, CancellationToken.None));
        }
        finally {
            kill.Kill();
        }
    }

    [Test]
    public void Kill_IsIdempotentAndPreventsNewOperations() {
        var implementation = new Win32WindowsServices();
        var services = (IWin32WindowsServices)implementation;
        var kill = (IExportAndKill)implementation;

        kill.Kill();

        Assert.DoesNotThrow(kill.Kill);
        Assert.Throws<InvalidOperationException>(() => services.GetIconSource(Win32StockIcon.Folder));
    }
}
