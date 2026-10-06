using System.Windows;
using Aion2Overlay.App.Diagnostics;

namespace Aion2Overlay.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--smoke-test"))
        {
            var resultIndex = Array.IndexOf(e.Args, "--result");
            var resultPath = resultIndex >= 0 && resultIndex + 1 < e.Args.Length
                ? e.Args[resultIndex + 1] : "artifacts/smoke-test.json";
            var code = await SmokeTest.RunAsync(resultPath, e.Args.Contains("--visible-test-window"));
            Shutdown(code);
            return;
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
