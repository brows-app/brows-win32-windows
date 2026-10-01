using Brows.Composition;
using Brows.Threading;

namespace Brows.Win32;

public sealed class Win32WindowsServicesVariable : IExportVariable {
    public STAThreadPool ThreadPool { get; set; }
}
