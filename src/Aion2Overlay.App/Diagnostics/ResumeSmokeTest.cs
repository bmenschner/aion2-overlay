using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Aion2Overlay.App.Services;

namespace Aion2Overlay.App.Diagnostics;

// Real WGC/MainWindow pipeline, controlled focus source; never changes a user window's focus.
internal static class ResumeSmokeTest
{
    public static async Task<int> RunAsync(string path)
    {
        var checks = new List<object>();
        var main = new MainWindow { ShowActivated = false, ShowInTaskbar = false, Left = 40, Top = 40 };
        Application.Current.MainWindow = main;
        Window? target = null;
        var active = 1;
        var passed = false; string? error = null;
        try
        {
            using var terrain = RegistrationSmokeTest.Terrain(1600, 900, 421);
            var reference = RegistrationSmokeTest.Raster(terrain);
            var image = RegistrationSmokeTest.Bitmap(reference);
            target = new Window { Title = "Aion2Overlay resume diagnostic", Width = 1000, Height = 650,
                Left = 100, Top = 100, ShowActivated = false, ShowInTaskbar = false,
                Content = new Image { Source = image, Stretch = Stretch.Uniform } };
            main.Show(); target.Show();
            var selector = (ComboBox)main.FindName("WindowSelector");
            var selected = new WindowTarget(new WindowInteropHelper(target).Handle, (uint)Environment.ProcessId, target.Title);
            selector.ItemsSource = new[] { selected }; selector.SelectedItem = selected;
            GetButton(main, "StartButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntil(() => Frame(main)?.Geometry.IsValid == true && GetButton(main, "CalibrateButton").IsEnabled);
            var capture = Capture(main)!;
            capture.TargetActiveProbe = () => Volatile.Read(ref active) == 1;
            var forward = capture.FrameReady!;
            capture.FrameReady = frame => Volatile.Read(ref active) == 1 ? forward(frame) : Task.CompletedTask;
            await WaitUntil(() => Frame(main) is { } f && HasTerrain(f));
            _ = main.Dispatcher.BeginInvoke(new Action(() => GetButton(main, "CalibrateButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
            await WaitUntil(() => Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Any());
            var dialog = Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Single();
            dialog.UseReference(image, new(RegistrationSmokeTest.Hash(reference), reference.Size));
            await dialog.BeginMatchAsync();
            if (!GetButton(dialog, "LiveButton").IsEnabled) throw new InvalidOperationException("Initial live fit unavailable.");
            GetButton(dialog, "LiveButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntil(() => Live(main)?.View != null);
            var live = Live(main)!;
            for (var cycle = 1; cycle <= 3; cycle++)
            {
                Volatile.Write(ref active, 0);
                await Task.Delay(2600);
                if (live.View != null) throw new InvalidOperationException("Stale mapping remained during background pause.");
                var refreshes = capture.RefreshCount;
                if (cycle == 2) target.Content = new Border { Background = Brushes.DarkSlateGray };
                var returnedAt = DateTimeOffset.UtcNow;
                Volatile.Write(ref active, 1);
                await WaitUntil(() => capture.RefreshCount > refreshes && Frame(main)?.CapturedAt >= returnedAt);
                if (cycle == 2)
                {
                    var before = live.StartedMatches;
                    await WaitUntil(() => live.StartedMatches > before && !live.IsMatching);
                    if (live.View != null) throw new InvalidOperationException("Wrong map accepted on return.");
                    checks.Add(new { type = "changed-content-stays-hidden", cycle, passed = true });
                    target.Content = new Image { Source = image, Stretch = Stretch.Uniform };
                }
                await WaitUntil(() => live.View?.CapturedAt >= returnedAt);
                if (!ReferenceEquals(Capture(main), capture) || !ReferenceEquals(Live(main), live) ||
                    ((CheckBox)main.FindName("LiveCheck")).IsChecked != true)
                    throw new InvalidOperationException("Return replaced capture/live instance or lost activation.");
                checks.Add(new { type = "background-return", cycle, seconds = (DateTimeOffset.UtcNow - returnedAt).TotalSeconds,
                    refreshes = capture.RefreshCount - refreshes, freshFit = true, sameInstances = true });
            }
            Volatile.Write(ref active, 0);
            await Task.Delay(300);
            GetButton(main, "StopButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntil(() => GetButton(main, "StartButton").IsEnabled);
            var stoppedCount = capture.RefreshCount;
            Volatile.Write(ref active, 1);
            await Task.Delay(2300);
            if (Capture(main) != null || Live(main) != null || capture.RefreshCount != stoppedCount)
                throw new InvalidOperationException("Stopped session resumed unexpectedly.");
            checks.Add(new { type = "stop-prevents-resume", passed = true });
            passed = true;
        }
        catch (Exception exception) { error = exception.ToString(); }
        finally
        {
            if (Capture(main) != null)
            {
                GetButton(main, "StopButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await WaitUntil(() => Capture(main) == null && GetButton(main, "StartButton").IsEnabled);
            }
            target?.Close();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { passed, testedAt = DateTimeOffset.UtcNow, checks, error,
            scope = "Only own synthetic WGC target; controlled foreground probe and dropped background deliveries, no real OS Alt-Tab or game test." }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;
    }

    private static bool HasTerrain(CapturedFrame f)
    {
        var i = (f.Height / 2 * f.Width + f.Width / 2) * 4;
        return Math.Max(f.Pixels[i], Math.Max(f.Pixels[i + 1], f.Pixels[i + 2])) - Math.Min(f.Pixels[i], Math.Min(f.Pixels[i + 1], f.Pixels[i + 2])) > 15;
    }
    private static T? Field<T>(MainWindow window, string name) where T : class =>
        (T?)typeof(MainWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window);
    private static CapturedFrame? Frame(MainWindow window) => Field<CapturedFrame>(window, "lastCapturedFrame");
    private static LiveMapController? Live(MainWindow window) => Field<LiveMapController>(window, "live");
    private static WindowCaptureService? Capture(MainWindow window) => Field<WindowCaptureService>(window, "capture");
    private static Button GetButton(Window window, string name) => (Button)window.FindName(name);
    private static async Task WaitUntil(Func<bool> predicate)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(12);
        while (!predicate()) { if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("Resume state not reached."); await Task.Delay(30); }
    }
}
