using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Aion2Overlay.App.Services;

namespace Aion2Overlay.App.Diagnostics;

// Each invocation runs in a separate diagnostic process and closes only its own HWNDs.
internal static class ShutdownSmokeTest
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    public static async Task RunAsync(MainWindow window, string scenario, string resultPath)
    {
        var application = Application.Current;
        var path = Path.GetFullPath(resultPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var checks = new List<string>();
        string? error = null;
        var passed = false;
        Window? target = null;
        var releaseConsumer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var closingStarted = false;
        Task? captureWorker = null;
        LiveMapController? liveController = null;
        var closeTime = DateTimeOffset.MinValue;
        application.DispatcherUnhandledException += (_, args) =>
        {
            error = args.Exception.ToString();
            args.Handled = true;
            releaseConsumer.TrySetResult();
            application.Shutdown(1);
        };
        application.Exit += (_, args) =>
        {
            if (captureWorker != null)
            {
                if (!captureWorker.IsCompleted) error ??= "Capture worker still running at application exit.";
                else checks.Add("Capture worker completed before application exit");
            }
            if (application.Windows.Count != 0) error ??= "WPF windows remain at application exit.";
            else checks.Add("No WPF windows remain at application exit");
            if (liveController != null)
            {
                if (liveController.IsMatching || liveController.View != null) error ??= "Live worker or mapping remains at application exit.";
                else checks.Add("Live worker completed and mapping cleared before application exit");
            }
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                passed = passed && error == null && args.ApplicationExitCode == 0,
                scenario, testedAt = DateTimeOffset.UtcNow, title = window.Title,
                exitCode = args.ApplicationExitCode, checks, error,
                closeDurationMs = closingStarted ? (DateTimeOffset.UtcNow - closeTime).TotalMilliseconds : (double?)null,
                limitations = "Separate diagnostic process; only own test HWNDs. No user instance or game captured/closed."
            }, new JsonSerializerOptions { WriteIndented = true }));
        };

        try
        {
            if (!new[] { "idle", "capture", "busy", "dialog", "repeat", "live", "live-unchecked" }.Contains(scenario))
                throw new ArgumentException("Unknown shutdown diagnostic scenario.");
            var liveScenario = scenario is "live" or "live-unchecked";
            await window.Dispatcher.InvokeAsync(() => { });
            if (scenario != "idle")
            {
                target = new Window
                {
                    Title = "Aion2Overlay shutdown diagnostic target", Width = liveScenario ? 1000 : 480, Height = liveScenario ? 650 : 300,
                    Left = 100, Top = 100, ShowActivated = false, ShowInTaskbar = false,
                    Content = new TextBlock { Text = "SYNTHETISCHER SCHLIESSTEST – kein Spiel", Margin = new Thickness(20) }
                };
                Aion2Overlay.Core.RegistrationImage? liveReference = null;
                if (liveScenario)
                {
                    using var terrain = RegistrationSmokeTest.Terrain(1600, 900, 421);
                    liveReference = RegistrationSmokeTest.Raster(terrain);
                    target.Content = new System.Windows.Controls.Image { Source = RegistrationSmokeTest.Bitmap(liveReference), Stretch = System.Windows.Media.Stretch.Uniform };
                }
                target.Show();
                var selector = (ComboBox)window.FindName("WindowSelector");
                var sample = new WindowTarget(new WindowInteropHelper(target).Handle,
                    (uint)Environment.ProcessId, target.Title);
                selector.ItemsSource = new[] { sample };
                selector.SelectedItem = sample;
                ((Button)window.FindName("StartButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await WaitUntilAsync(() => ((Button)window.FindName("CalibrateButton")).IsEnabled);
                checks.Add("Real WGC capture on own target received fresh frame");
                var service = (WindowCaptureService)typeof(MainWindow)
                    .GetField("capture", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
                captureWorker = (Task)typeof(WindowCaptureService)
                    .GetField("worker", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(service)!;

                if (liveScenario)
                {
                    await WaitUntilAsync(() =>
                    {
                        var frame = (CapturedFrame?)typeof(MainWindow).GetField("lastCapturedFrame", BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window);
                        if (frame?.Geometry.IsValid != true) return false;
                        var i = (frame.Height/2*frame.Width+frame.Width/2)*4;
                        return Math.Max(frame.Pixels[i], Math.Max(frame.Pixels[i+1],frame.Pixels[i+2]))-Math.Min(frame.Pixels[i],Math.Min(frame.Pixels[i+1],frame.Pixels[i+2])) > 15;
                    });
                    _ = window.Dispatcher.BeginInvoke(new Action(() => ((Button)window.FindName("CalibrateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                    await WaitUntilAsync(() => application.Windows.OfType<AutomaticRegistrationWindow>().Any());
                    var dialog = application.Windows.OfType<AutomaticRegistrationWindow>().Single();
                    dialog.UseReference(RegistrationSmokeTest.Bitmap(liveReference!), new(RegistrationSmokeTest.Hash(liveReference!), liveReference!.Size));
                    await dialog.BeginMatchAsync();
                    if (!((Button)dialog.FindName("LiveButton")).IsEnabled) throw new InvalidOperationException("Live activation unavailable in shutdown test.");
                    ((Button)dialog.FindName("LiveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await WaitUntilAsync(() => (liveController = (LiveMapController?)typeof(MainWindow).GetField("live",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window))?.View != null);
                    await WaitUntilAsync(() => liveController!.IsMatching);
                    checks.Add("Real live mapping active; close during running native matcher");
                    if (scenario == "live-unchecked")
                    {
                        ((CheckBox)window.FindName("LiveCheck")).IsChecked = false;
                        checks.Add("Live checkbox unchecked immediately before Windows close; pending cleanup must be awaited");
                    }
                }

                if (scenario == "dialog")
                {
                    _ = window.Dispatcher.BeginInvoke(new Action(() =>
                        ((Button)window.FindName("CalibrateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                    await WaitUntilAsync(() => application.Windows.OfType<AutomaticRegistrationWindow>().Any());
                    var dialog = application.Windows.OfType<AutomaticRegistrationWindow>().Single();
                    SendWindowsClose(dialog);
                    await WaitUntilAsync(() => !application.Windows.OfType<AutomaticRegistrationWindow>().Any());
                    if (!window.IsVisible || !((Button)window.FindName("StopButton")).IsEnabled)
                        throw new InvalidOperationException("Dialog close ended main window or capture.");
                    checks.Add("Dialog Windows close leaves main window and capture active");
                }
                if (scenario == "busy")
                {
                    // Fault injection: delay the consumer acknowledgement, keeping real Stop pending.
                    var consumerEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    service.FrameReady = _ => { consumerEntered.TrySetResult(); return releaseConsumer.Task; };
                    target.Width += 10;
                    await consumerEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    ((Button)window.FindName("StopButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    if (((Button)window.FindName("StartButton")).IsEnabled || ((Button)window.FindName("StopButton")).IsEnabled)
                        throw new InvalidOperationException("Stop did not remain pending for injected delay.");
                    checks.Add("Stop is pending while capture consumer acknowledgement is delayed");
                }
            }

            window.Closed += (_, _) =>
            {
                passed = closingStarted;
                checks.Add("Main window Closed after Windows close command");
            };
            closingStarted = true;
            closeTime = DateTimeOffset.UtcNow;
            SendWindowsClose(window);
            if (scenario == "repeat")
            {
                SendWindowsClose(window);
                checks.Add("Two Windows close commands queued; cleanup must run without reentrancy failure");
            }
            await Task.Delay(300);
            releaseConsumer.TrySetResult();
            await Task.Delay(TimeSpan.FromSeconds(5));
            error = "Process still running five seconds after one Windows close command.";
            application.Shutdown(1);
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            releaseConsumer.TrySetResult();
            application.Shutdown(1);
        }
    }

    private static void SendWindowsClose(Window window)
    {
        // Same SC_CLOSE command as the Windows title-bar X; never targets another process.
        if (!PostMessage(new WindowInteropHelper(window).Handle, 0x0112, new nint(0xF060), 0))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Diagnostic state not reached.");
            await Task.Delay(50);
        }
    }
}
