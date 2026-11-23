# Repository guidance

This file applies to the entire repository. Read the relevant project README and
nearby implementation and tests before making changes.

## Skills

The general conventions for this repository live in agent skills under
`.agents/skills/` and `.claude/skills/`. Use the skill that matches the task, and
follow it where it differs from older code or other guidance.

| Task | Skill |
| --- | --- |
| Writing or editing C#, `.csproj`, `.props`, `.targets`, or `Directory.Packages.props` | `brows-csharp` |
| Restoring, building, and choosing the validation scope | `brows-code-building` |
| Writing and running NUnit tests | `brows-code-testing` |
| Git status, staging, commits, branches, and worktrees | `brows-git` |
| Planning substantial changes under `agents/plans/` | `brows-code-plan` |
| Reviewing code and writing reports under `agents/reviews/` | `brows-code-review` |
| Fixing numbered review issues, including per-issue worktrees | `brows-code-fix` |
| Performance and SIMD analysis | `analyzing-dotnet-performance`, `vectorization` |

## Solution layout

- `brows-win32-windows.slnx` contains two package projects, their two matching
  test projects, and a WPF sample application.
- `source/Brows.Win32.Windows/` contains the Windows Shell preview package.
  `Win32/` holds `Win32IconService`, `Win32OverlayService`,
  `Win32ThumbnailService`, their shared `Win32BaseService`, and `Win32StockIcon`.
  `Win32/PlatformInvoke/` contains native declarations and structures;
  `Win32/InteropServices/` contains COM wrappers and the `ComTypes/` declarations.
  The root `README.md` is this package's README and is included in its NuGet package.
- `source/Brows.Win32.Windows.Composition/` contains the `IWin32WindowsServices`
  export contract, its internal implementation, and
  `Win32WindowsServicesVariable` configuration. Its `README.md` documents the
  companion package and composition lifecycle.
- `tests/Brows.Win32.Windows.Tests/Win32/` contains service tests, with COM
  wrapper tests under `InteropServices/` and native layout and HRESULT tests
  under `PlatformInvoke/`.
- `tests/Brows.Win32.Windows.Composition.Tests/Win32/` tests export discovery,
  forwarded operations, configuration, and shutdown.
- `samples/Brows.Win32.Windows.Sample/` is a WPF application demonstrating icons,
  thumbnails, and overlays through the composition export.
- `Directory.Build.props` and `Directory.Packages.props` hold shared build
  settings and centrally managed package versions. `source/Directory.Build.props`,
  `tests/Directory.Build.props`, and `samples/Directory.Build.props` add settings
  for those project groups. Build artifacts go under `out/`; only the source
  projects are packable. `global.json` selects the SDK, and
  `.github/workflows/workflow.yml` defines Windows CI and release packaging.

Inspect `git status` and existing diffs before editing. Preserve existing work;
do not overwrite or revert unrelated changes. Keep unrelated changes out of
fixes and commits, and stage only the changes belonging to the requested task.

## Preserve existing work

- When working in a Git repository, inspect its status and existing diffs before
  editing. Preserve existing work, avoid reverting unrelated changes, and keep
  changes scoped to the requested task.
- Do not commit generated build or test output, packages, or IDE state unless the
  task specifically requires them.

## Windows Shell preview behavior

- Inject a non-null `Brows.Threading.ISTAThreadPool` into each preview service.
  The caller owns that pool; disposing an individual service must not empty it.
  `STAThreadPool.Work` and `Empty` are explicit interface implementations, so
  call them through an `ISTAThreadPool` reference or cast. Empty a caller-owned
  pool only after its services have been disposed and other pool users have
  finished.
- Keep Shell COM and image retrieval work on the supplied STA pool. Preserve
  freezing of WPF `BitmapSource` results before sharing them across threads.
- Preserve native resource ownership: release owned icon handles with
  `DestroyIcon`, bitmap handles with `DeleteObject`, and COM objects through
  the existing wrappers and cleanup paths. Check native structure layout and
  pointer-size assumptions when changing interop declarations.
- Pair `Win32BaseService.BeginOperation` with `EndOperation` in a `finally`
  block. Disposal prevents new operations and waits synchronously for active
  requests. Avoid blocking the WPF dispatcher when active work needs it.
  Cancellation cannot interrupt a native Shell call already in progress.
- Preserve the icon service's stock and file-type caches, ordinal case-insensitive
  extension keys, retry settings, and cancellation behavior for shared requests.
  Canceling one waiter must not cancel another caller's shared request.
- The composition export lazily creates its services and resolves a pool unless
  a caller supplies one through `Win32WindowsServicesVariable`. Resolution
  prefers a pool provided by an optional imported `ISTAThreadPoolProvider`, then
  a pool created by an optional imported `ISTAThreadPoolFactory`, then direct
  construction. `Kill` empties only pools it created; variable-supplied and
  provider-provided pools remain with their owners. Apply variables before the
  first service request. `Kill` is idempotent, disposes services, and prevents
  later service requests. Document both rejected-request exceptions: requests
  after the kill operation throw `InvalidOperationException`, and a request
  racing the kill operation can be rejected with `ObjectDisposedException` by
  the disposed service.
