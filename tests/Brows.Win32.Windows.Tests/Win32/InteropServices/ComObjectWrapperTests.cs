using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Brows.Win32.InteropServices;

[TestFixture]
public sealed class ComObjectWrapperTests {
    [Test]
    public async Task ComObject_WhenAccessedConcurrently_IsCreatedOnlyOnce() {
        var wrapper = new TestObjectWrapper();
        try {
            var values = await Task.WhenAll(Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => wrapper.Value)));
            using (Assert.EnterMultipleScope()) {
                Assert.That(wrapper.FactoryCallCount, Is.EqualTo(1));
                Assert.That(values.All(value => ReferenceEquals(value, values[0])), Is.True);
            }
        }
        finally {
            wrapper.Dispose();
        }
    }

    [Test]
    public void ComObject_AfterDispose_ThrowsWithoutCallingFactory() {
        var
        wrapper = new TestObjectWrapper();
        wrapper.Dispose();

        Assert.Throws<ObjectDisposedException>(() => {
            _ = wrapper.Value;
        });
        Assert.That(wrapper.FactoryCallCount, Is.Zero);
    }

    [Test]
    public void Dispose_WhenCalledMoreThanOnce_RunsManagedCleanupOnce() {
        var wrapper = new TestObjectWrapper();
        _ = wrapper.Value;

        wrapper.Dispose();
        wrapper.Dispose();

        Assert.That(wrapper.DisposeCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Dispose_WhenComObjectIsInUse_WaitsForTheCallToFinish() {
        var wrapper = new TestObjectWrapper();
        using var callStarted = new ManualResetEventSlim();
        using var releaseCall = new ManualResetEventSlim();
        using var disposeStarted = new ManualResetEventSlim();
        var call = Task.Run(() => wrapper.Use(_ => {
            callStarted.Set();
            releaseCall.Wait();
        }));
        try {
            Assert.That(callStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            var disposal = Task.Run(() => {
                disposeStarted.Set();
                wrapper.Dispose();
            });

            Assert.That(disposeStarted.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(disposal.Wait(TimeSpan.FromMilliseconds(100)), Is.False);

            releaseCall.Set();
            await call;
            await disposal;
            Assert.Throws<ObjectDisposedException>(() => _ = wrapper.Value);
        }
        finally {
            releaseCall.Set();
            await call;
            wrapper.Dispose();
        }
    }

    private sealed class TestObjectWrapper : ComObjectWrapper<object> {
        private int FactoryCalls;
        private int DisposeCalls;

        public object Value => ComObject;
        public int FactoryCallCount => Volatile.Read(ref FactoryCalls);
        public int DisposeCallCount => Volatile.Read(ref DisposeCalls);

        public void Use(Action<object> action) {
            UseComObject(value => {
                action(value);
                return true;
            });
        }

        protected override object Factory() {
            Interlocked.Increment(ref FactoryCalls);
            return new object();
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                Interlocked.Increment(ref DisposeCalls);
            }
            base.Dispose(disposing);
        }
    }
}
