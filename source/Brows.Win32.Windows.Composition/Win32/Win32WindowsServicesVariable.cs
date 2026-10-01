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
    /// Gets or sets the maximum number of attempts to retrieve an icon.
    /// </summary>
    /// <value>
    /// The maximum number of attempts, including the initial attempt, or <see langword="null"/> to use the default
    /// of 5 attempts. The value must be at least 1.
    /// </value>
    public int? IconAttempts { get; set; }

    /// <summary>
    /// Gets or sets the delay, in milliseconds, between failed icon retrieval attempts.
    /// </summary>
    /// <value>
    /// The delay between attempts, or <see langword="null"/> to use the default of 100 milliseconds. The value must
    /// be non-negative.
    /// </value>
    public int? IconAttemptDelay { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Win32WindowsServicesVariable"/> class.
    /// </summary>
    public Win32WindowsServicesVariable() {
    }
}
