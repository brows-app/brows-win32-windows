# Brows.Win32.Windows code review

Reviewed the library, its Win32 and COM declarations, disposal paths, concurrency and cancellation behavior, tests, and sample. Issues are numbered for later reference. All findings are fixed. `dotnet test brows-win32-windows.slnx --no-restore` passes all 48 tests on each target framework. The tests now exercise native structure layouts and live Shell stock icons, path icons, and thumbnails; a manual check on a Start Menu shortcut returned a frozen 20x20 overlay image. Automated testing still cannot require a live overlay because that depends on the machine's installed overlay handlers and file state.

## Findings

1. **[P1, fixed] Path icons were requested as hypothetical normal files, so real file and folder icons were wrong.** [Win32IconService.GetIconSource](source/Brows.Win32.Windows/Win32/Win32IconService.cs) now checks path attributes asynchronously and asks `SHGetFileInfoW` for the actual icon of an existing path without `SHGFI_USEFILEATTRIBUTES`. Directories fall back to the stock folder icon if path-specific retrieval fails. Missing paths with extensions still use the cached generic file-type icon lookup; missing extensionless paths use `DocNoAssoc`. See Microsoft's [SHGetFileInfoW](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow) documentation.

2. **[P1, fixed] Concurrent first overlay requests could abandon activated COM wrappers.** The registry cache and wrapper activation described in the original finding have been removed. Overlay selection now goes through the Shell's effective overlay index, so the service no longer creates or publishes competing lists of overlay handler COM wrappers.

3. **[P2, fixed] Cancellation during an icon lookup was converted to a `null` result.** [The shared `Get` helper](source/Brows.Win32.Windows/Win32/Win32IconService.cs) now catches `OperationCanceledException` when the caller's token is canceled and rethrows. If the canceled caller owns the cache task, the canceled task is removed so subsequent callers can retry; cancellation of a non-owning waiter leaves the shared task intact. Unrelated lookup failures retain the existing `null` fallback.

4. **[P2, fixed] The service finalizer ran managed and COM cleanup on the finalizer thread.** [Win32BaseService.Dispose(bool)](source/Brows.Win32.Windows/Win32/Win32BaseService.cs) calls `DisposeCore()` only when `disposing` is `true`, and the inert service and COM-wrapper finalizers have been removed. Neither hierarchy owns a raw unmanaged resource that can safely be released from a finalizer; COM and pool cleanup remains deterministic through `Dispose()`.

5. **[P2, fixed] Overlay handlers received false file attributes.** The service now uses supplied attributes or retrieves them with `GetFileAttributesW` to reject missing paths, then asks `SHGetFileInfoW` for the Shell-selected overlay using the actual path. It no longer activates handlers or supplies a fabricated attribute mask to them.

6. **[P2, fixed] Overlay selection could return an icon Explorer would never use.** [GetOverlayIconSource](source/Brows.Win32.Windows/Win32/Win32OverlayService.cs) now asks `SHGetFileInfoW` for the effective overlay index and retrieves that image from the Shell's system image list. This delegates conflict resolution and overlay-slot selection to Windows instead of scanning registered handlers. See Microsoft's [`SHGetFileInfoW`](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow), [`SHGetImageList`](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetimagelist), and [`IImageList::GetOverlayImage`](https://learn.microsoft.com/en-us/windows/win32/api/commoncontrols/nf-commoncontrols-iimagelist-getoverlayimage) documentation.

7. **[P2, fixed] An initialized COM wrapper remained accessible after disposal.** [ComObject and UseComObject](source/Brows.Win32.Windows/Win32/InteropServices/ComObjectWrapper.cs) now check disposed state on access. The concrete wrappers run each COM call inside `UseComObject`, which holds the same lock used by `Dispose`; disposal therefore waits for active calls, and later calls throw `ObjectDisposedException` instead of reaching a released RCW.

8. **[P2, fixed] The sample blocked the WPF dispatcher during shutdown.** [Window_Closing](samples/Brows.Win32.Windows.Sample/Win32WindowsSampleWindow.xaml.cs) now cancels pending preview requests and awaits all tracked preview loads, then disposes the services on a worker thread. The close is canceled while this runs and retried after disposal, keeping the dispatcher alive for outstanding WPF continuations.

9. **[P2, fixed] One slow icon lookup blocked unrelated cache misses.** [Win32IconService.Get](source/Brows.Win32.Windows/Win32/Win32IconService.cs) now holds the semaphore only while reading or updating the task map and cache. It awaits the shared per-key task after releasing the lock, allowing lookups for different keys to run concurrently; completion and failure paths reacquire the lock for safe publication and identity-checked task removal.

10. **[P3, fixed] Disposal exceptions named the wrong service.** [Win32BaseService.BeginOperation](source/Brows.Win32.Windows/Win32/Win32BaseService.cs) now passes `GetType().Name` to `ObjectDisposedException`, so each disposed service reports its own runtime type.

11. **[P2, fixed] A canceled caller could remain blocked behind another caller's shared icon lookup and receive its result.** [Win32IconService.Get](source/Brows.Win32.Windows/Win32/Win32IconService.cs) now waits on a shared cache task with each non-owning caller's cancellation token. Canceling a waiter ends only that wait; it does not cancel or evict the task owned by another request. A pre-canceled token is also checked before returning a cached icon.

12. **[P2, fixed] Extension cache keys depended on the current culture.** The old `ToLower()` normalization could turn `.ICO` into `.ıco` under Turkish culture, causing the wrong Shell association lookup and separate cache entries for the same extension. Both extension dictionaries now use `StringComparer.OrdinalIgnoreCase`, and the original extension is passed to the Shell.

13. **[P3, fixed] `Win32StockIcon.MaxIcons` did not match the Windows SDK.** The public and native mirror values were 175. Both now use the SDK's `SIID_MAX_ICONS` value of 181, with an independent literal-value regression test.

14. **[P3, fixed] Icon and overlay path methods accepted null or empty paths.** Both services now reject these inputs with `ArgumentNullException`, matching the thumbnail service and avoiding null native string arguments or misleading generic-icon fallbacks.

15. **[P3, fixed] Invalid retry settings were accepted.** `Win32IconService.Attempts` now requires at least one attempt, and `AttemptDelay` rejects negative delays when assigned instead of failing later during an error retry.

## Verification

- Compared all P/Invoke structures, enum values, interface IDs, and the used `IImageList` vtable slots with the Windows 10.0.26100 SDK headers and Microsoft Win32 documentation.
- Confirmed that every acquired `HICON`, `HBITMAP`, and COM interface has a matching `DestroyIcon`, `DeleteObject`, or COM release in a `finally`/`Dispose` path.
- Built the full solution with compiler warnings treated as errors; only Source Link warnings caused by this checkout having no configured Git remote remain informational.
- Passed 48 tests on `net462`, `net48`, `net8.0-windows`, and `net10.0-windows`.
