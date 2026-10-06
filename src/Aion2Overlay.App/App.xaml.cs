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
        var uiTest = e.Args.Contains("--ui-smoke-test");
        if (uiTest)
        {
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 80;
            window.Top = 80;
        }
        window.Show();
        if (uiTest)
        {
            var code = await UiSmokeTest.RunAsync(window, "artifacts/ui-smoke-test");
            Shutdown(code);
        }
    }
}
