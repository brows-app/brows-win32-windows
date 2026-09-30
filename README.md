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
- The repository's `global.json` selects the .NET 10 SDK feature band for builds.

## Install

After the package has been published to a NuGet feed, add it to a project with:

```sh
dotnet add package Brows.Win32.Windows
```

To consume the source from this repository, add a project reference:

```xml
<ProjectReference Include="path/to/source/Brows.Win32.Windows/Brows.Win32.Windows.csproj" />
```

## Usage

The services expose `Task`-based APIs. Await them instead of synchronously blocking a WPF dispatcher with `.Wait()` or `.Result`.

```csharp
using Brows.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

public async Task LoadPreviewsAsync(
    string path,
    CancellationToken cancellationToken,
    Win32ThumbnailService thumbnailService,
    Win32OverlayService overlayService) {

    BitmapSource thumbnail = await thumbnailService.GetThumbnailSource(
        path,
        width: 320,
        height: 240,
        cancellationToken: cancellationToken);

    BitmapSource overlay = await overlayService.GetOverlayIconSource(
        path,
        token: cancellationToken);

    // Assign the results to WPF Image.Source properties, for example.
}
```

`Win32ThumbnailService.GetThumbnailSource` accepts a file or folder path and positive width and height. It first tries a cached thumbnail, then requests a thumbnail, and finally asks the Shell for a resized image. Shell errors may be reported as exceptions.

`Win32OverlayService.GetOverlayIconSource` asks `SHGetFileInfoW` for the overlay index selected by the Shell, then retrieves that overlay image from the system image list. It returns `null` when the Shell reports no overlay for the path.

## Cancellation and disposal

Pass a `CancellationToken` to any service to cancel work that has not started and to observe cancellation at supported checkpoints. A token cannot interrupt a Shell COM call that is already running.

`Win32IconService`, `Win32OverlayService`, and `Win32ThumbnailService` implement `IDisposable`. Dispose them after outstanding requests have completed. Disposal is synchronous and waits for active work, so from a WPF UI use an asynchronous shutdown path and keep the dispatcher alive while disposal completes. For example:

```csharp
using System.Threading.Tasks;

// After outstanding requests are complete:
await Task.Run(() => {
    iconService.Dispose();
    thumbnailService.Dispose();
    overlayService.Dispose();
});
```

Each service creates an STA worker pool unless one is supplied to its constructor. Disposal empties only a pool created by the service; a supplied pool remains owned by the caller.

## Build and sample

Build the solution on Windows:

```sh
dotnet build brows-win32-windows.slnx
```

Run the WPF sample app:

```sh
dotnet run --project samples/Brows.Win32.Windows.Sample/Brows.Win32.Windows.Sample.csproj
```

The sample lets you enter or browse to a path and view the icon, thumbnail, and overlay returned by the Shell.

## License

This project is licensed under the [MIT License](LICENSE).
