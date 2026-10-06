using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aion2Overlay.App.Services;

namespace Aion2Overlay.App.Diagnostics;

internal static class UiSmokeTest
{
    public static async Task<int> RunAsync(MainWindow window, string outputBase)
    {
        var results = new List<object>();
        var success = false;
        string? error = null;
        var fullBase = Path.GetFullPath(outputBase);
        Directory.CreateDirectory(Path.GetDirectoryName(fullBase)!);
        try
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var selector = (ComboBox)window.FindName("WindowSelector");
            var samples = new[]
            {
                new WindowTarget(0, 0, "AION 2 · UI-Test"),
                new WindowTarget(0, 0, "Zweites Fenster · UI-Test")
            };
            selector.ItemsSource = samples;
            selector.SelectedIndex = 0;
            window.UpdateLayout();
            await Task.Delay(150);
            await SaveLiveWindow(new WindowInteropHelper(window).Handle, fullBase + "-window.png");

            foreach (var name in new[] { "RefreshButton", "StartButton", "StopButton", "CalibrateButton" })
            {
                var button = (Button)window.FindName(name);
                var initialState = button.IsEnabled;
                foreach (var enabled in new[] { true, false })
                {
                    button.IsEnabled = enabled;
                    button.ApplyTemplate();
                    window.UpdateLayout();
                    var text = button.Template.FindName("Caption", button) as TextBlock
                        ?? throw new InvalidOperationException($"No visible caption found: {name}");
                    var expected = !enabled || name == "StopButton" ? Colors.Black : Colors.White;
                    var correctColor = text.Foreground is SolidColorBrush foreground && foreground.Color == expected;
                    var opaque = text.Opacity == 1 && button.Opacity == 1;
                    results.Add(new { button = name, enabled, correctColor, expected = expected.ToString(), opaque, caption = text.Text });
                    if (!correctColor || !opaque || text.Text != button.Content.ToString())
                        throw new InvalidOperationException($"Unreadable caption: {name}, enabled={enabled}");
                }
                button.IsEnabled = initialState;
            }

            var selectedCaption = TextElements(selector).Single(text => text.Text == samples[0].Label);
            CheckBlack(selectedCaption, "dropdownSelection", results);
            selector.IsDropDownOpen = true;
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(500); // Let the native popup opening animation finish before capture.
            var popup = selector.Template.FindName("PART_Popup", selector) as Popup
                ?? throw new InvalidOperationException("Dropdown popup not found.");
            var child = popup.Child ?? throw new InvalidOperationException("Dropdown popup is empty.");
            foreach (var sample in samples)
                CheckBlack(TextElements(child).Single(text => text.Text == sample.Label), "dropdownItem", results);
            var popupSource = PresentationSource.FromVisual(child) as HwndSource
                ?? throw new InvalidOperationException("Dropdown popup has no native window.");
            await SaveLiveWindow(popupSource.Handle, fullBase + "-dropdown.png");
            selector.IsDropDownOpen = false;

            // Render the hosted visual after templates and disabled states have been applied.
            window.UpdateLayout();
            var source = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            source.Render(window);
            SaveBitmap(source, fullBase + "-hosted.png");
            success = true;
        }
        catch (Exception exception)
        {
            error = exception.ToString();
        }
        finally
        {
            await File.WriteAllTextAsync(fullBase + ".json", JsonSerializer.Serialize(new
            {
                passed = success, title = window.Title, testedAt = DateTimeOffset.UtcNow,
                results, error, scope = "Own live application window; no game capture."
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return success ? 0 : 1;
    }

    private static void SaveCapture(CapturedFrame frame, string path)
    {
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96,
            PixelFormats.Bgra32, null, frame.Pixels, frame.Width * 4);
        SaveBitmap(bitmap, path);
    }

    private static async Task SaveLiveWindow(nint handle, string path)
    {
        var first = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var capture = new WindowCaptureService { FrameReady = frame =>
        {
            if (frame.Pixels.Any(value => value != 0)) first.TrySetResult(frame);
            return Task.CompletedTask;
        } };
        capture.Ended += message => first.TrySetException(new InvalidOperationException(message));
        capture.Start(handle);
        SaveCapture(await first.Task.WaitAsync(TimeSpan.FromSeconds(15)), path);
    }

    private static IEnumerable<TextBlock> TextElements(DependencyObject element)
    {
        if (element is TextBlock text) yield return text;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            foreach (var child in TextElements(VisualTreeHelper.GetChild(element, i))) yield return child;
    }

    private static void CheckBlack(TextBlock text, string state, ICollection<object> results)
    {
        var black = text.Foreground is SolidColorBrush brush && brush.Color == Colors.Black;
        results.Add(new { state, black, caption = text.Text });
        if (!black) throw new InvalidOperationException($"Dropdown caption is not black: {state}");
    }

    private static void SaveBitmap(BitmapSource bitmap, string path)
    {
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }
}
