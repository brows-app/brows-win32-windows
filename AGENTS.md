# Agent guidance for brows-win32-windows

## Product context

- The NuGet package built from this solution is consumed by Brows, a Windows File Explorer replacement. Changes to this library affect file and folder previews in everyday browsing, including large directories, slow paths, and rapidly changing Shell state.
- This is a Windows-only WPF library. Its public services return `BitmapSource` objects for icons, effective overlays, and thumbnails. Preserve correct Shell results and responsive UI behavior when optimizing them.
- Treat public APIs, documented behavior, and package contents as consumer-facing contracts. Keep changes compatible with all target frameworks in `Directory.Build.props`: `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`.

## Solution map

- `source/Brows.Win32.Windows/` is the packable library. `Win32IconService`, `Win32OverlayService`, and `Win32ThumbnailService` are under `Win32/`; native declarations and COM wrappers are under `Win32/PlatformInvoke/` and `Win32/InteropServices/`.
- `tests/Brows.Win32.Windows.Tests/` contains NUnit tests, including live Windows Shell and native layout checks.
- `samples/Brows.Win32.Windows.Sample/` is a WPF app for manually checking a path's icon, overlay, and thumbnail.
- `Directory.Packages.props` owns dependency versions. The root and subtree `Directory.Build.props` files own shared build and package settings. Follow `.editorconfig` for formatting.

## Shell, threading, and resource rules

- Keep Shell COM work on the STA worker pool. Do not block a WPF dispatcher while waiting for Shell calls or service disposal. A cancellation token can stop queued work and waiting, but cannot interrupt a native call already in progress.
- Preserve operation tracking around asynchronous service methods. Disposal must prevent new work, wait for active work, and leave a caller-supplied STA pool owned by its caller.
- Release every acquired COM object, `HICON`, and `HBITMAP` on success and failure. Freeze returned bitmap sources when possible so they can cross threads safely.
- For existing paths, ask the Shell about the real item. Generic file-type icons are appropriate for missing paths, but fabricated file attributes can hide custom icons. Let the Shell choose the effective overlay; do not infer it by enumerating overlay handlers.
- Cache only with an explicit freshness and memory policy. Stock and extension icons and overlay images already have service-local caches. Path icons, overlay selection, and thumbnails can change while Brows is running; account for file changes, Shell association or icon changes, and dynamic overlay state before retaining their results. Keep bitmap caches bounded and preserve per-caller cancellation when sharing in-flight work.

## Changes and verification

- Add or update focused tests when changing public behavior, interop layouts, cancellation, concurrency, caching, or disposal. Use temporary paths or stable stock icons where possible; do not require a particular third-party overlay handler or overlay state to exist on the test machine.
- This repository is hosted on GitHub. `.github/workflows/workflow.yml` runs restore, Release build, and tests on `windows-latest` for pushes to `dev` and published releases. Published releases also pack and push the NuGet package. Keep the workflow passing for every supported target framework.
- When validating locally, use Windows and the SDK selected by `global.json`:

  ```powershell
  dotnet restore brows-win32-windows.slnx
  dotnet build brows-win32-windows.slnx --configuration Release --no-restore
  dotnet test brows-win32-windows.slnx --configuration Release --no-build --no-restore
  ```

- For changes to preview appearance, also use the sample app to inspect representative files, folders, shortcuts, and missing paths when practical. Report any manual checks that could not be performed.
- Update XML documentation and `README.md` when changing consumer-visible behavior. Keep the NuGet package metadata and README packaging intact; do not change version or release settings as a side effect of unrelated work.
