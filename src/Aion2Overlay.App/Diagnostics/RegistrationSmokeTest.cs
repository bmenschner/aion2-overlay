using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;
using Aion2Overlay.Core;
using Aion2Overlay.Imaging;
using Aion2Overlay.App.Services;
using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace Aion2Overlay.App.Diagnostics;

internal static class RegistrationSmokeTest
{
    public static async Task<int> RunAsync(string resultPath)
    {
        var tests = new List<object>();
        var passed = false; string? error = null;
        await using var service = new OpenCvMapRegistrationService();
        try
        {
            using var reference = Terrain(1600, 900, 421);
            var input = Raster(reference);
            var hash = Hash(input);
            for (var i = 0; i < 30; i++)
            {
                var size = i % 2 == 0 ? new OpenCvSharp.Size(1920, 1080) : new OpenCvSharp.Size(5120, 1440);
                using var source = new Mat(); Cv2.Resize(reference, source, size);
                var scales = new[] { 0.5, 0.7, 1.0, 1.3, 2.0 };
                var scale = scales[i % scales.Length];
                var shiftX = size.Width * (1 - scale) / 2 + size.Width * (i % 3 - 1) * 0.035;
                var shiftY = size.Height * (1 - scale) / 2 + size.Height * (i % 3 - 1) * 0.025;
                using var matrix = Mat.FromPixelData(2, 3, MatType.CV_64FC1, new double[] { scale, 0, shiftX, 0, scale, shiftY });
                using var target = new Mat(); Cv2.WarpAffine(source, target, matrix, size, borderValue: new Scalar(40, 40, 40, 255));
                var sourceInput = Raster(source);
                var outcome = await service.RegisterAsync(sourceInput, Raster(target), Hash(sourceInput), CancellationToken.None);
                double? truthP95 = null; var controls = 0;
                if (outcome.Result is { } result)
                {
                    var errors = new List<double>();
                    for (var y = 1; y < 20; y++) for (var x = 1; x < 30; x++)
                    {
                        var point = new MapPoint(x * size.Width / 30.0, y * size.Height / 20.0);
                        if (!result.TryMap(point, out var mapped)) continue;
                        errors.Add(mapped.DistanceTo(new(scale * point.X + shiftX, scale * point.Y + shiftY)));
                    }
                    controls = errors.Count;
                    if (controls > 0) truthP95 = errors.Order().ElementAt((int)Math.Ceiling(controls * 0.95) - 1);
                }
                var correct = !outcome.Passed || controls >= 20 && truthP95 <= 8.0 * size.Height / 1080;
                tests.Add(new { type = "known-warp", i, size, scale, accepted = outcome.Passed, correct, controls, truthP95, outcome.Message, quality = outcome.Result?.Quality });
                Console.WriteLine($"Warp {i + 1}/30: {outcome.Passed}, Wahrheit P95 {truthP95:F2}");
                if (!correct) throw new InvalidOperationException($"Accepted incorrect warp {i}.");
            }
            for (var i = 0; i < 20; i++)
            {
                using var unrelated = i < 10 ? Terrain(1600, 900, 9000 + i) : new Mat(900, 1600, MatType.CV_8UC4, new Scalar(50, 50, 50, 255));
                // Same UI border; only map terrain differs or is absent.
                Cv2.Rectangle(unrelated, new OpenCvSharp.Rect(0, 0, 1600, 75), new Scalar(15, 15, 15, 255), -1);
                var outcome = await service.RegisterAsync(input, Raster(unrelated), hash, CancellationToken.None);
                tests.Add(new { type = "negative", i, accepted = outcome.Passed, outcome.Message });
                if (outcome.Passed) throw new InvalidOperationException($"False positive on unrelated terrain {i}.");
            }

            using var original = Cv2.ImRead("artifacts/references/Altgard.png", ImreadModes.Unchanged);
            if (!original.Empty())
            {
                var originalInput = Raster(original);
                var identity = await service.RegisterAsync(originalInput, originalInput, Hash(originalInput), CancellationToken.None);
                tests.Add(new { type = "real-reference-identity-only", accepted = identity.Passed, identity.Message, quality = identity.Result?.Quality });
                if (!identity.Passed) throw new InvalidOperationException("Original Altgard reference cannot match its own terrain.");
            }

            var timing = new List<double>();
            using (var wide = new Mat())
            {
                Cv2.Resize(reference, wide, new OpenCvSharp.Size(5120, 1440));
                var wideInput = Raster(wide); var wideHash = Hash(wideInput);
                for (var i = 0; i < 20; i++)
                {
                    var measured = await service.RegisterAsync(wideInput, wideInput, wideHash, CancellationToken.None);
                    if (!measured.Passed) throw new InvalidOperationException("Repeated ultrawide identity match failed.");
                    timing.Add(measured.Result!.Quality.DurationMs);
                }
            }
            var durationP95 = timing.Order().ElementAt(18);
            tests.Add(new { type = "ultrawide-performance", runs = timing.Count, durationP95, coldAndWarm = true });
            if (durationP95 > 5000) throw new InvalidOperationException("Ultrawide P95 duration exceeds five seconds.");

            // Run the actual dialog and actual Match handler; no settings/files are remembered.
            var referenceBitmap = Bitmap(input);
            var window = new AutomaticRegistrationWindow(referenceBitmap, DateTimeOffset.UtcNow, false)
            { ShowActivated = false, ShowInTaskbar = false, Left = 40, Top = 40 };
            window.Show();
            window.UseReference(referenceBitmap, new(hash, input.Size));
            var matchButton = (Button)window.FindName("MatchButton");
            matchButton.ApplyTemplate();
            var caption = (TextBlock)matchButton.Template.FindName("Caption", matchButton);
            if (caption.Text != "Automatisch abgleichen" || caption.Foreground is not SolidColorBrush color || color.Color != Colors.White)
                throw new InvalidOperationException("Automatic button missing or wrong caption color.");
            await window.BeginMatchAsync();
            if (!((Button)window.FindName("SaveButton")).IsEnabled) throw new InvalidOperationException("Automatic dialog cannot save valid identity match.");
            var profile = window.BuildProfile();
            window.UseProfile(JsonSerializer.Serialize(profile));
            if (((Button)window.FindName("SaveButton")).IsEnabled) throw new InvalidOperationException("Reloaded profile reused old transform.");
            try { window.UseProfile("{\"SchemaVersion\":1,\"Method\":\"manual\"}"); throw new InvalidOperationException("Manual profile accepted."); }
            catch (InvalidDataException) { }
            window.UseReference(referenceBitmap, new(hash, input.Size));
            var cancelledJob = window.BeginMatchAsync();
            ((Button)window.FindName("CancelButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await cancelledJob;
            if (((Button)window.FindName("SaveButton")).IsEnabled) throw new InvalidOperationException("Cancelled result enabled save.");
            window.UseReference(referenceBitmap, new(hash, input.Size));
            var obsoleteJob = window.BeginMatchAsync();
            window.UseReference(referenceBitmap, new(hash, input.Size));
            await obsoleteJob;
            if (((Button)window.FindName("SaveButton")).IsEnabled) throw new InvalidOperationException("Reference change installed obsolete result.");
            window.UseReference(referenceBitmap, new(hash, input.Size));
            await window.BeginMatchAsync();
            await window.Dispatcher.InvokeAsync(() => { });
            window.UpdateLayout();
            ((TextBlock)window.FindName("ReferenceLabel")).Text = "1 · SYNTHETISCHE TESTKARTE · kein Spielbild";
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(300); // Ensure the native WGC screenshot contains the completed result.
            var preview = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            preview.Render(window);
            var imagePath = Path.ChangeExtension(resultPath, ".png");
            using (var stream = File.Create(imagePath)) { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(preview)); encoder.Save(stream); }
            await UiSmokeTest.SaveLiveWindow(new WindowInteropHelper(window).Handle, Path.ChangeExtension(resultPath, ".live.png"));
            window.Close(); await window.FinishAsync();
            tests.Add(new { type = "actual-automatic-dialog", profile.SchemaVersion, profile.Method, freshMatchRequiredAfterReload = true, cancellationAndReferenceChangeDiscarded = true, preview = imagePath });

            // Exercise the normal main-window button and its fresh-frame provider with real WGC.
            var main = new MainWindow { ShowActivated = false, ShowInTaskbar = false, Left = 30, Top = 30 };
            Application.Current.MainWindow = main;
            main.Show();
            var targetWindow = new System.Windows.Window { Title = "SYNTHETISCHER KARTENABGLEICH – eigenes Aufnahmefenster", Width = 1000, Height = 650,
                Left = 120, Top = 100, ShowActivated = false, ShowInTaskbar = false,
                Content = new System.Windows.Controls.Image { Source = referenceBitmap, Stretch = Stretch.Uniform } };
            targetWindow.Show();
            await main.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(300);
            var selector = (ComboBox)main.FindName("WindowSelector");
            var targetEntry = new WindowTarget(new WindowInteropHelper(targetWindow).Handle, (uint)Environment.ProcessId, targetWindow.Title);
            selector.ItemsSource = new[] { targetEntry }; selector.SelectedItem = targetEntry;
            ((Button)main.FindName("StartButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => ((Button)main.FindName("CalibrateButton")).IsEnabled && HasPaintedTerrain(main));
            _ = main.Dispatcher.BeginInvoke(new Action(() => ((Button)main.FindName("CalibrateButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
            await WaitUntilAsync(() => Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Any());
            var liveDialog = Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Single();
            liveDialog.UseReference(referenceBitmap, new(hash, input.Size));
            await liveDialog.BeginMatchAsync();
            if (!((Button)liveDialog.FindName("SaveButton")).IsEnabled)
                throw new InvalidOperationException("Normal WGC→main button→automatic match failed: " + ((TextBlock)liveDialog.FindName("StatusText")).Text);
            tests.Add(new { type = "normal-wgc-to-automatic-dialog", accepted = true, quality = liveDialog.BuildProfile().Registration.Quality });
            liveDialog.Close(); await liveDialog.FinishAsync();
            ((Button)main.FindName("StopButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitUntilAsync(() => ((Button)main.FindName("StartButton")).IsEnabled);
            targetWindow.Close();
            var acceptedWarps = tests.Take(30).Count(t => (bool)t.GetType().GetProperty("accepted")!.GetValue(t)!);
            if (acceptedWarps < 27) throw new InvalidOperationException($"Only {acceptedWarps}/30 known warps accepted.");
            passed = true;
        }
        catch (Exception exception) { error = exception.ToString(); }
        var path = Path.GetFullPath(resultPath); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { passed, testedAt = DateTimeOffset.UtcNow, opencv = Cv2.GetVersionString(), tests, error,
            limitations = "Synthetic warps/negatives and real-reference identity probe; no independent real capture pair or Ingame acceptance." }, new JsonSerializerOptions { WriteIndented = true }));
        return passed ? 0 : 1;
    }

    internal static Mat Terrain(int width, int height, int seed)
    {
        var random = new Random(seed);
        var image = new Mat(height, width, MatType.CV_8UC4, new Scalar(70, 110, 140, 255));
        for (var i = 0; i < 1800; i++)
        {
            var center = new Point(random.Next(width), random.Next(height));
            var color = new Scalar(random.Next(20, 120), random.Next(50, 160), random.Next(70, 180), 255);
            Cv2.Circle(image, center, random.Next(2, 22), color, -1);
        }
        for (var i = 0; i < 60; i++) Cv2.Line(image, new Point(random.Next(width), random.Next(height)), new Point(random.Next(width), random.Next(height)), new Scalar(25, 45, 65, 255), random.Next(2, 5));
        Cv2.Rectangle(image, new OpenCvSharp.Rect(0, 0, width, (int)(height * 0.085)), new Scalar(15, 15, 15, 255), -1);
        Cv2.Rectangle(image, new OpenCvSharp.Rect(0, 0, (int)(width * 0.12), height), new Scalar(15, 15, 15, 255), -1);
        return image;
    }
    internal static RegistrationImage Raster(Mat mat)
    {
        using var bgra = new Mat();
        if (mat.Channels() == 4) mat.CopyTo(bgra); else Cv2.CvtColor(mat, bgra, ColorConversionCodes.BGR2BGRA);
        var bytes = new byte[checked(bgra.Width * bgra.Height * 4)]; Marshal.Copy(bgra.Data, bytes, 0, bytes.Length);
        return new(new(bgra.Width, bgra.Height), bytes);
    }
    internal static string Hash(RegistrationImage image) => Convert.ToHexString(SHA256.HashData(image.Bgra)).ToLowerInvariant();
    internal static BitmapSource Bitmap(RegistrationImage image)
    {
        var bitmap = BitmapSource.Create(image.Size.Width, image.Size.Height, 96, 96, PixelFormats.Bgra32, null, image.Bgra, image.Size.Width * 4);
        bitmap.Freeze(); return bitmap;
    }
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        while (!condition()) { if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("WGC dialog test timed out."); await Task.Delay(50); }
    }
    private static bool HasPaintedTerrain(MainWindow main)
    {
        if (((System.Windows.Controls.Image)main.FindName("Preview")).Source is not BitmapSource image) return false;
        var pixel = new byte[4];
        image.CopyPixels(new Int32Rect(image.PixelWidth / 2, image.PixelHeight / 2, 1, 1), pixel, 4, 0);
        return Math.Max(pixel[0], Math.Max(pixel[1], pixel[2])) - Math.Min(pixel[0], Math.Min(pixel[1], pixel[2])) > 15;
    }
}
