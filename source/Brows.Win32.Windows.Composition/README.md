# Brows.Win32.Windows.Composition

This package makes the Windows Shell preview services available to Brows through `Brows.Composition` as the `IWin32WindowsServices` export. It depends on `Brows.Win32.Windows` and is intended for Windows WPF applications. It targets `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`.

The export provides asynchronous file and stock icons, effective overlays, and thumbnails. Add this assembly to the assemblies discovered by your Brows import environment, then import `IWin32WindowsServices`. Await its operations and pass a `CancellationToken` when cancellation is needed. Bitmap sources are frozen when WPF permits it; icon and overlay results can be null.

An optional `Win32WindowsServicesVariable` supplies a caller-owned `STAThreadPool`; without one, the export creates a shared pool when the services are first used. Apply configuration before the first service request. `IconAttempts` defaults to 5 and must be at least 1; `IconAttemptDelay` defaults to 100 milliseconds and must be non-negative. A null retry setting leaves the current value unchanged. Configuration after services have been created is rejected with `InvalidOperationException`.

The import environment's kill operation disposes the services and empties only a pool created by the export. A configured pool remains caller-owned: empty it after killing the import environment and finishing any other work using that pool. Kill is idempotent, and later preview requests are rejected with `InvalidOperationException`. A request that races the kill operation — accepted by the export while the services are being disposed, or begun just after disposal — can instead be rejected with `ObjectDisposedException`.

Complete outstanding preview requests before shutting down the environment when possible. Service disposal waits synchronously for active work. If shutdown must wait for requests from a WPF UI, run the kill operation off the UI thread and keep the dispatcher alive until it completes. Shell work can block during a native call even when a request has been canceled.
