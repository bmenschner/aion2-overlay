using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aion2Overlay.App.Services;
using Aion2Overlay.Core;

namespace Aion2Overlay.App.Diagnostics;

internal static class CalibrationSmokeTest
{
    public static async Task<int> RunAsync(string outputBase)
    {
        var fullBase = Path.GetFullPath(outputBase);
        Directory.CreateDirectory(Path.GetDirectoryName(fullBase)!);
        var checks = new List<string>();
        CalibrationWindow? window = null;
        string? error = null;
        var passed = false;
        try
        {
            var reference = CreateReference();
            var capture = CreateCapture(reference);
            using var bytes = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(reference));
            encoder.Save(bytes);
            var info = new ReferenceImageInfo(Convert.ToHexString(SHA256.HashData(bytes.ToArray())).ToLowerInvariant(), new(reference.PixelWidth, reference.PixelHeight));
            window = new CalibrationWindow(capture, DateTimeOffset.UtcNow)
            {
                ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = 40, Top = 40
            };
            window.Show();
            window.UseReference(reference, info);
            ((TextBlock)window.FindName("ReferenceLabel")).Text = "SYNTHETISCHE REFERENZ · 800 × 600 px · keine Spielkarte";
            SetText("MapIdInput", "SYNTHETISCHE TESTKARTE");
            SetText("BuildInput", "UI-Test, kein Spielbuild");
            SetText("ViewInput", "Synthetisches Bild · feste Ansicht · keine Cube-Daten");
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();

            void Require(bool condition, string name)
            {
                if (!condition) throw new InvalidOperationException(name);
                checks.Add(name);
            }
            void SetText(string name, string text) => ((TextBox)window.FindName(name)).Text = text;
            bool CanSave() => ((Button)window.FindName("SaveButton")).IsEnabled;
            MapPoint Target(MapPoint point) => new(100 + 700 * point.X, 60 + 480 * point.Y);
            void ClickPair(MapPoint point, double errorX = 0)
            {
                var referenceHost = (Grid)window.FindName("ReferenceHost");
                var captureHost = (Grid)window.FindName("CaptureHost");
                var referenceView = ImageViewport.Fit(referenceHost.ActualWidth, referenceHost.ActualHeight, info.Size);
                var captureView = ImageViewport.Fit(captureHost.ActualWidth, captureHost.ActualHeight, new(capture.PixelWidth, capture.PixelHeight));
                var target = Target(point);
                Require(window.SelectAt(true, referenceView.Display(point)), "Reference click accepted");
                Require(window.SelectAt(false, captureView.Display(new((target.X + errorX) / capture.PixelWidth, target.Y / capture.PixelHeight))), "Capture click accepted");
            }

            Require(!window.SelectAt(false, new(100, 100)), "Capture-first click ignored");
            Require(!window.SelectAt(true, new(-1, -1)), "Letterbox/outside click ignored");
            MapPoint[] points = [new(0.12, 0.16), new(0.86, 0.2), new(0.2, 0.82), new(0.55, 0.45), new(0.75, 0.72)];
            foreach (var point in points) ClickPair(point);
            Require(CanSave(), "Complete independent checks enable save");
            var beforeResize = window.BuildProfile();
            window.Width = 960;
            window.Height = 700;
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Require(window.BuildProfile().Validation.Transform == beforeResize.Validation.Transform, "Resize preserves transform");
            window.UndoPair();
            Require(!CanSave(), "Undo removes verification and disables save");
            ClickPair(points[4], 20);
            Require(!CanSave(), "Bad independent check blocks save");
            var rejected = false;
            try { window.BuildProfile(); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Bad profile creation rejected");
            window.UndoPair();
            ClickPair(points[4]);
            SetText("BuildInput", "");
            Require(!CanSave(), "Missing build blocks save");
            SetText("BuildInput", "UI-Test, kein Spielbuild");
            Require(CanSave(), "Restored metadata enables save");
            window.ResetPairs();
            Require(!CanSave(), "Reset clears all pairs");
            ClickPair(points[0]);
            var pendingHost = (Grid)window.FindName("ReferenceHost");
            var pendingView = ImageViewport.Fit(pendingHost.ActualWidth, pendingHost.ActualHeight, info.Size);
            Require(window.SelectAt(true, pendingView.Display(points[1])), "Pending reference accepted");
            window.UndoPair();
            Require(!window.SelectAt(false, new(100, 100)), "Undo clears pending reference");
            ClickPair(points[1]);
            ClickPair(points[2]);
            ClickPair(points[3]);
            ClickPair(points[4]);
            Require(CanSave(), "Undo of pending point preserves earlier pair");
            window.UseReference(reference, info);
            Require(!CanSave(), "Reference replacement clears previous pairs");
            foreach (var point in points) ClickPair(point);
            var profile = window.BuildProfile();
            await File.WriteAllTextAsync(fullBase + "-profile.json", JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
            await Task.Delay(250);
            await SaveOwnWindow(new WindowInteropHelper(window).Handle, fullBase + "-window.png");
            passed = true;
        }
        catch (Exception exception) { error = exception.ToString(); }
        finally
        {
            window?.Close();
            await File.WriteAllTextAsync(fullBase + ".json", JsonSerializer.Serialize(new
            {
                passed, testedAt = DateTimeOffset.UtcNow, checks, error,
                scope = "Synthetic local map and own calibration dialog only; no game capture or map data."
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        return passed ? 0 : 1;
    }

    private static BitmapSource CreateReference()
    {
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(38, 60, 68)), null, new Rect(0, 0, 800, 600));
            var pen = new Pen(new SolidColorBrush(Color.FromRgb(77, 105, 112)), 1);
            for (var x = 0; x < 800; x += 80) draw.DrawLine(pen, new(x, 0), new(x, 600));
            for (var y = 0; y < 600; y += 60) draw.DrawLine(pen, new(0, y), new(800, y));
            var route = new StreamGeometry();
            using (var path = route.Open())
            {
                path.BeginFigure(new(30, 540), false, false);
                path.BezierTo(new(250, 400), new(310, 150), new(760, 80), true, false);
            }
            draw.DrawGeometry(null, new Pen(Brushes.SlateGray, 35), route);
            draw.DrawEllipse(new SolidColorBrush(Color.FromRgb(38, 85, 126)), null, new(490, 410), 180, 90);
            MapPoint[] points = [new(0.12, 0.16), new(0.86, 0.2), new(0.2, 0.82), new(0.55, 0.45), new(0.75, 0.72)];
            foreach (var point in points)
                draw.DrawRectangle(Brushes.LightGray, new Pen(Brushes.White, 2), new Rect(point.X * 800 - 6, point.Y * 600 - 6, 12, 12));
        }
        return Render(visual, 800, 600);
    }

    private static BitmapSource CreateCapture(BitmapSource reference)
    {
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen())
        {
            draw.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 1000, 600));
            draw.DrawImage(reference, new Rect(100, 60, 700, 480));
        }
        return Render(visual, 1000, 600);
    }

    private static BitmapSource Render(Visual visual, int width, int height)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static async Task SaveOwnWindow(nint handle, string path)
    {
        var first = new TaskCompletionSource<CapturedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var capture = new WindowCaptureService { FrameReady = frame =>
        {
            if (frame.Pixels.Any(value => value != 0)) first.TrySetResult(frame);
            return Task.CompletedTask;
        } };
        capture.Ended += message => first.TrySetException(new InvalidOperationException(message));
        capture.Start(handle);
        var frame = await first.Task.WaitAsync(TimeSpan.FromSeconds(15));
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null, frame.Pixels, frame.Width * 4);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        png.Save(stream);
    }
}
