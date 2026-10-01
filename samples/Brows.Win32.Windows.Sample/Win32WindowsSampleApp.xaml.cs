using Brows.Composition;
using Brows.Threading;
using Brows.Win32;
using System.Windows;

namespace Brows;

sealed partial class Win32WindowsSampleApp : IImportEnvironment {
    private readonly STAThreadPool ThreadPool = new(nameof(Win32WindowsSampleApp)) {
        WorkerCountMax = 4,
    };

    protected sealed override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        var imported = default(IImport);
        try {
            imported = await Imports.Init(this, default);
            var
            window = MainWindow = imported.Find<Win32WindowsSampleWindow>();
            window.ShowDialog();
        }
        finally {
            try {
                imported?.Kill();
            }
            finally {
                ThreadPool.Empty();
            }
        }
    }

    ImportInfo IImportEnvironment.ImportInfo => ImportInfo.Listed(
        list: () => [
            new Win32WindowsServices(),
            new Win32WindowsSampleWindow(),
        ],
        variables: () => new([
            new Win32WindowsServicesVariable {
                ThreadPool = ThreadPool,
            }
        ]));
}

