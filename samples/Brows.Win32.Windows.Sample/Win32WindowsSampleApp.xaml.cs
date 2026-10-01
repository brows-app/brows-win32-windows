using Brows.Composition;
using Brows.Win32;
using System.Windows;

namespace Brows;

sealed partial class Win32WindowsSampleApp : IImportEnvironment {
    protected sealed override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        var imported = await Imports.Init(this, default);
        try {
            var
            window = MainWindow = imported.Find<Win32WindowsSampleWindow>();
            window.ShowDialog();
        }
        finally {
            imported.Kill();
        }
    }

    ImportInfo IImportEnvironment.ImportInfo => ImportInfo.Listed(
        list: () => [
            new Win32WindowsServices(),
            new Win32WindowsSampleWindow(),
        ],
        variables: () => new([
            new Win32WindowsServicesVariable {
                ThreadPool= new(nameof(Win32WindowsSampleApp)) {
                    WorkerCountMax = 4
                }
            }
        ]));
}

