using System.Windows;
using Aion2Overlay.App.Diagnostics;

namespace Aion2Overlay.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--live-map-smoke-test"))
        {
            var resultIndex = Array.IndexOf(e.Args, "--result");
            var path = resultIndex >= 0 && resultIndex + 1 < e.Args.Length ? e.Args[resultIndex + 1] : "artifacts/live-map-smoke-test.json";
            Shutdown(await LiveMapSmokeTest.RunAsync(path));
            return;
        }
        if (e.Args.Contains("--registration-smoke-test"))
        {
            var resultIndex = Array.IndexOf(e.Args, "--result");
            var path = resultIndex >= 0 && resultIndex + 1 < e.Args.Length ? e.Args[resultIndex + 1] : "artifacts/registration-smoke-test.json";
            Shutdown(await RegistrationSmokeTest.RunAsync(path));
            return;
        }
        if (e.Args.Contains("--calibration-smoke-test"))
        {
            var ultrawide = e.Args.Contains("--ultrawide-test");
            Shutdown(await CalibrationSmokeTest.RunAsync(ultrawide ? "artifacts/calibration-ultrawide-test" : "artifacts/calibration-smoke-test", ultrawide));
            return;
        }
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
        // Hidden overlay windows must never keep the ordinary application alive.
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        if (e.Args.Contains("--shutdown-smoke-test"))
        {
            window.ShowActivated = false;
            window.ShowInTaskbar = false;
            window.Left = 40;
            window.Top = 40;
            window.Show();
            var scenarioIndex = Array.IndexOf(e.Args, "--scenario");
            var scenario = scenarioIndex >= 0 && scenarioIndex + 1 < e.Args.Length ? e.Args[scenarioIndex + 1] : "idle";
            var resultIndex = Array.IndexOf(e.Args, "--result");
            var result = resultIndex >= 0 && resultIndex + 1 < e.Args.Length ? e.Args[resultIndex + 1] : $"artifacts/shutdown-{scenario}.json";
            await ShutdownSmokeTest.RunAsync(window, scenario, result);
            return;
        }
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
