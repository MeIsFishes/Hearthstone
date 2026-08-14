using System.Windows;
using BbxEditor.Wpf.Services;

namespace BbxEditor.Wpf;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--embedding-worker", StringComparer.OrdinalIgnoreCase))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var exitCode = await EmbeddingWorkerHost.RunAsync(e.Args);
            Shutdown(exitCode);
            return;
        }

        new MainWindow().Show();
    }
}
