using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Aion2Overlay.App.Interop;
using Aion2Overlay.App.Services;

namespace Aion2Overlay.App.Diagnostics;

internal static class SmokeTest
{
    public static async Task<int> RunAsync(string resultPath, bool visibleTestWindow)
    {
        var checks = new Dictionary<string, object>();
        Window? target = null;
        OverlayController? overlay = null;
        WindowCaptureService? capture = null;
        var exitCode = 1;
        try
        {
            // Visible rendering is opt-in. Off-screen WPF windows may yield empty pixels.
            target = new Window
            {
                Title = "Aion2Overlay capture self-test", Width = 640, Height = 360,
                Left = visibleTestWindow ? 100 : -20000, Top = visibleTestWindow ? 100 : -20000,
                ShowActivated = false, ShowInTaskbar = false,
                WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(18, 90, 130)),
                Content = new TextBlock { Text = "SPEC-001 capture test", Foreground = Brushes.White,
                    Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Top }
            };
            target.Show();
            var handle = new WindowInteropHelper(target).Handle;
            overlay = new OverlayController(new(handle, (uint)Environment.ProcessId, target.Title));
            overlay.Update();
            var required = NativeWindows.Transparent | NativeWindows.NoActivate | NativeWindows.Layered;
            var actual = NativeWindows.GetWindowLongPtr(overlay.Window.Handle, NativeWindows.ExtendedStyle).ToInt64();
            Require((actual & required) == required, "overlayStyles", checks);
            Require(NativeWindows.ClientBounds(overlay.Window.Handle) == NativeWindows.ClientBounds(handle),
                "initialOverlayBounds", checks);
            Require(!NativeWindows.IsWindowVisible(overlay.Window.Handle), "backgroundOverlayHidden", checks);

            var first = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            capture = new WindowCaptureService { FrameReady = frame =>
            {
                // WGC may deliver an initial empty frame before WPF has painted.
                if (!visibleTestWindow || HasTargetColor(frame)) first.TrySetResult(frame);
                return Task.CompletedTask;
            } };
            capture.Ended += message => first.TrySetException(new InvalidOperationException(message));
            capture.Start(handle);
            var firstFrame = await first.Task.WaitAsync(TimeSpan.FromSeconds(15));
            CheckFrame(firstFrame, checks, "firstCapture");
            if (visibleTestWindow) Require(HasTargetColor(firstFrame), "targetColorCaptured", checks);
            else checks["targetColorCaptured"] = "Not tested: off-screen compositor may return empty pixels. Use --visible-test-window.";

            if (visibleTestWindow)
            {
                var resized = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
                capture.Ended += message => resized.TrySetException(new InvalidOperationException(message));
                capture.FrameReady = frame =>
                {
                    if (frame.Width != firstFrame.Width || frame.Height != firstFrame.Height) resized.TrySetResult(frame);
                    return Task.CompletedTask;
                };
                target.Width = 720;
                target.Height = 420;
                await Task.Delay(300);
                overlay.Update();
                checks["resizedTargetBounds"] = NativeWindows.ClientBounds(handle);
                Require(NativeWindows.ClientBounds(overlay.Window.Handle) == NativeWindows.ClientBounds(handle),
                    "resizedOverlayBounds", checks);
                CheckFrame(await resized.Task.WaitAsync(TimeSpan.FromSeconds(15)), checks, "resizedCapture");
            }
            else checks["resizedCapture"] = "Not tested: off-screen compositor may not deliver resize frames. Use --visible-test-window.";

            await capture.DisposeAsync();
            capture = null;
            checks["stop"] = true;
            var restarted = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ended = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            capture = new WindowCaptureService { FrameReady = frame => { restarted.TrySetResult(frame); return Task.CompletedTask; } };
            capture.Ended += message => ended.TrySetResult(message);
            capture.Start(handle);
            CheckFrame(await restarted.Task.WaitAsync(TimeSpan.FromSeconds(15)), checks, "restartCapture");
            target.Close();
            target = null;
            var message = await ended.Task.WaitAsync(TimeSpan.FromSeconds(10));
            checks["closedTargetHandled"] = message;
            overlay.Update();
            Require(!NativeWindows.IsWindowVisible(overlay.Window.Handle), "closedTargetOverlayHidden", checks);
            exitCode = 0;
        }
        catch (Exception exception)
        {
            checks["error"] = exception.ToString();
            if (capture != null)
            {
                checks["receivedFrames"] = capture.ReceivedFrames;
                checks["lastContentSize"] = capture.LastContentSize;
            }
        }
        finally
        {
            overlay?.Dispose();
            if (capture != null) await capture.DisposeAsync();
            target?.Close();
            var fullPath = Path.GetFullPath(resultPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(new
            {
                passed = exitCode == 0, testedAt = DateTimeOffset.UtcNow,
                platform = Environment.OSVersion.VersionString, checks,
                visibleTestWindow,
                limitations = "Ordinary test HWND; Aion 2 and manual click/DPI tests not covered. Off-screen mode does not verify rendered image content."
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return exitCode;
    }

    private static void CheckFrame(CapturedFrame frame, IDictionary<string, object> checks, string key)
    {
        Require(frame.Width > 0 && frame.Height > 0 &&
            frame.Pixels.Length == checked(frame.Width * frame.Height * 4) &&
            (DateTimeOffset.UtcNow - frame.CapturedAt).TotalSeconds < 5, key, checks);
        checks[$"{key}Size"] = $"{frame.Width}x{frame.Height}";
    }

    private static bool HasTargetColor(CapturedFrame frame)
    {
        var center = ((frame.Height / 2) * frame.Width + frame.Width / 2) * 4;
        return Math.Abs(frame.Pixels[center] - 130) < 15 &&
            Math.Abs(frame.Pixels[center + 1] - 90) < 15 && Math.Abs(frame.Pixels[center + 2] - 18) < 15;
    }

    private static void Require(bool condition, string key, IDictionary<string, object> checks)
    {
        checks[key] = condition;
        if (!condition) throw new InvalidOperationException($"Self-test failed: {key}");
    }
}
