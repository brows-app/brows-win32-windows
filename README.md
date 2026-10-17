# Brows.Win32.Windows

`Brows.Win32.Windows` is a Windows-only .NET library for retrieving file and folder previews through the Windows Shell. It provides asynchronous services for Shell icons, thumbnails, and effective icon overlays, returning WPF `BitmapSource` objects.

## Features

- Retrieve thumbnails through `IShellItemImageFactory`, with cache-only and thumbnail-generation fallbacks.
- Retrieve path-specific, file-type, and Windows stock icons.
- Retrieve the effective Shell overlay icon for a file or folder path.
- Run Shell COM work on STA worker threads so callers can await the operations from a WPF UI.
- Return frozen `BitmapSource` instances when possible so they can be used across threads.

## Requirements

- Windows, with WPF available.
- The repository targets `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`.
- The repository's `global.json` selects the .NET 10 SDK with feature-band roll-forward for builds.

## Install

After the package has been published to a NuGet feed, add it to a project with:

```sh
dotnet add package Brows.Win32.Windows
```

To consume the source from this repository, add a project reference:

```xml
<ProjectReference Include="path/to/source/Brows.Win32.Windows/Brows.Win32.Windows.csproj" />
```

`Brows.Win32.Windows.Composition` is a companion NuGet package for applications using `Brows.Composition`. It exports `IWin32WindowsServices`, which forwards icon, overlay, and thumbnail requests to the services in this library. An optional `Win32WindowsServicesVariable` supplies a caller-owned STA thread pool; otherwise the export creates a shared pool when first used. See the [Composition package README](source/Brows.Win32.Windows.Composition/README.md) for lifecycle details.

## Usage

Each service constructor requires a non-null `Brows.Threading.ISTAThreadPool`. A null pool throws `ArgumentNullException`. The implementation package `Brows.Win32.STAThreadPool` is a dependency, so you can construct an `STAThreadPool` directly. Keep the pool and services for the lifetime of the component using them to reuse workers and service caches.

The services expose `Task`-based APIs. Await them instead of synchronously blocking a WPF dispatcher with `.Wait()` or `.Result`. This example uses a pool for one batch and disposes the services before emptying it:

```csharp
using Brows.Threading;
using Brows.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

public async Task<BitmapSource[]> LoadPreviewsAsync(
    string path,
    CancellationToken cancellationToken) {

    ISTAThreadPool threadPool = new STAThreadPool("previews");
    try {
        using var iconService = new Win32IconService(threadPool);
        using var thumbnailService = new Win32ThumbnailService(threadPool);
        using var overlayService = new Win32OverlayService(threadPool);

        var icon = await iconService.GetIconSource(
            path, cancellationToken: cancellationToken);
        var thumbnail = await thumbnailService.GetThumbnailSource(
            path, width: 320, height: 240, cancellationToken: cancellationToken);
        var overlay = await overlayService.GetOverlayIconSource(
            path, cancellationToken: cancellationToken);

        return new[] { icon, thumbnail, overlay };
    }
    finally {
        threadPool.Empty();
    }
}
```

The returned images can be assigned to WPF `Image.Source` properties; an icon or overlay result can be null. `Work` and `Empty` are explicit interface implementations on `STAThreadPool`, so call them through an `ISTAThreadPool` reference as above.

`Win32IconService` retrieves path-specific icons for existing files and folders, with file-type and stock-icon fallbacks. It caches stock and file-type icons and makes up to 5 retrieval attempts by default, with 100 milliseconds between attempts. `Attempts` must be at least 1, and `AttemptDelay` must be non-negative. If all attempts and applicable fallbacks fail, the result can be null.

`Win32ThumbnailService.GetThumbnailSource` accepts a file or folder path and positive width and height. It first tries a cached thumbnail, then requests a thumbnail, and finally asks the Shell for a resized thumbnail or icon. Shell errors may be reported as exceptions.

`Win32OverlayService.GetOverlayIconSource` asks `SHGetFileInfoW` for the overlay index selected by the Shell, then retrieves that overlay image from the system image list. It returns null when the Shell reports no overlay or when the path's attributes cannot be retrieved, including for a missing path. Shell lookup and image-list errors may be reported as exceptions.

## Cancellation and disposal

Pass a `CancellationToken` to any service to cancel work that has not started and to observe cancellation at supported checkpoints. A token cannot interrupt a Shell COM call that is already running.

`Win32IconService`, `Win32OverlayService`, and `Win32ThumbnailService` implement `IDisposable`. Disposal prevents new operations and waits synchronously for active requests before releasing the service's resources. Complete outstanding requests before disposal when possible. If disposing while requests are active from a WPF UI, use an asynchronous shutdown path, for example `await Task.Run(() => service.Dispose())`, and keep the dispatcher alive until shutdown completes.

The caller owns the pool supplied to each service. Disposing a service does not empty that pool. Empty it only after all services using it have been disposed and other pool users have finished. If you use the composition export, its kill operation handles service disposal and empties a pool created by the export; a pool supplied through configuration remains caller-owned.

## Build and sample

Build the solution on Windows:

```sh
dotnet build brows-win32-windows.slnx
```

Run the WPF sample app, selecting one of its target frameworks:

```sh
dotnet run --project samples/Brows.Win32.Windows.Sample/Brows.Win32.Windows.Sample.csproj --framework net10.0-windows
```

The sample lets you enter or browse to a path and view the icon, thumbnail, and overlay returned by the Shell. It uses the composition export with an application-owned pool and waits for preview requests before shutting down.

## License

This project is licensed under the [MIT License](LICENSE).
