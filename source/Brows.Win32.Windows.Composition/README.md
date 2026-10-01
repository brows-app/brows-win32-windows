# Brows.Win32.Windows.Composition

This package makes the Windows Shell preview services available to Brows through `Brows.Composition` as the `IWin32WindowsServices` export. It depends on `Brows.Win32.Windows` and is intended for Windows WPF applications.

The export provides asynchronous file and stock icons, effective overlays, and thumbnails. Add this assembly to the assemblies discovered by your Brows import environment, then import `IWin32WindowsServices`. An optional `Win32WindowsServicesVariable` supplies a caller-owned `STAThreadPool`; without one, the export creates a shared pool when the services are first used.

The import environment's kill operation disposes the services and empties a pool created by the export. Complete outstanding preview requests before shutting down the environment. Shell work can block during a native call even when a request has been canceled.
