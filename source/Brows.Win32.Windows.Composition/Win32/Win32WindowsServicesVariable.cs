using Brows.Composition;
using Brows.Threading;

namespace Brows.Win32;

/// <summary>
/// Supplies configuration to the <see cref="IWin32WindowsServices"/> composition export.
/// </summary>
/// <remarks>
/// Supply this configuration before making the first service request. Leave <see cref="ThreadPool"/> unset to
/// let the export create and own its STA thread pool when first used.
/// </remarks>
public sealed class Win32WindowsServicesVariable : IExportVariable {
    /// <summary>
    /// Gets or sets the STA thread pool used for Windows Shell work.
    /// </summary>
    /// <value>
    /// A caller-owned pool to use, or <see langword="null"/> to let the export create and own a pool on first use.
    /// A supplied pool remains caller-owned and is not emptied when the export is killed.
    /// </value>
    public STAThreadPool ThreadPool { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowsServicesVariable"/> class.
    /// </summary>
    public Win32WindowsServicesVariable() {
    }
}
