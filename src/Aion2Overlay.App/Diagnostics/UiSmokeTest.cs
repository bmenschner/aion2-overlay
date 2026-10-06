using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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
        WindowCaptureService? capture = null;
        var fullBase = Path.GetFullPath(outputBase);
        Directory.CreateDirectory(Path.GetDirectoryName(fullBase)!);
        try
        {
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var firstFrame = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            capture = new WindowCaptureService { FrameReady = frame =>
            {
                if (frame.Pixels.Any(value => value != 0)) firstFrame.TrySetResult(frame);
                return Task.CompletedTask;
            } };
            capture.Ended += message => firstFrame.TrySetException(new InvalidOperationException(message));
            capture.Start(new WindowInteropHelper(window).Handle);
            var liveFrame = await firstFrame.Task.WaitAsync(TimeSpan.FromSeconds(15));
            SaveCapture(liveFrame, fullBase + "-window.png");

            foreach (var name in new[] { "RefreshButton", "StartButton", "StopButton" })
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
                    var black = text.Foreground is SolidColorBrush foreground && foreground.Color == Colors.Black;
                    var opaque = text.Opacity == 1 && button.Opacity == 1;
                    results.Add(new { button = name, enabled, black, opaque, caption = text.Text });
                    if (!black || !opaque || text.Text != button.Content.ToString())
                        throw new InvalidOperationException($"Unreadable caption: {name}, enabled={enabled}");
                }
                button.IsEnabled = initialState;
            }

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
            if (capture != null) await capture.DisposeAsync();
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

    private static void SaveBitmap(BitmapSource bitmap, string path)
    {
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }
}
