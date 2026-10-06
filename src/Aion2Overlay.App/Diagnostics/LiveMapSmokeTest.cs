using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Aion2Overlay.App.Interop;
using Aion2Overlay.App.Services;
using Aion2Overlay.Core;
using OpenCvSharp;

namespace Aion2Overlay.App.Diagnostics;

internal static class LiveMapSmokeTest
{
    public static async Task<int> RunAsync(string path)
    {
        var checks = new List<object>();
        MainWindow? main = null; System.Windows.Window? target = null; LiveMapController? tracking = null;
        var passed = false; string? error = null;
        try
        {
            using var terrain = RegistrationSmokeTest.Terrain(1600, 900, 421);
            var reference = RegistrationSmokeTest.Raster(terrain);
            var hash = RegistrationSmokeTest.Hash(reference);
            var setup = new LiveMapSetup(reference, hash, new(800, 450));
            tracking = new(setup, _ => { });
            RegistrationResult? first = null;
            foreach (var (scale, dx, dy) in new[] { (1.0, 0.0, 0.0), (0.7, 210.0, 120.0), (1.3, -210.0, -100.0), (1.0, 65.0, -35.0) })
            {
                using var transform = Mat.FromPixelData(2, 3, MatType.CV_64FC1, new double[] { scale, 0, dx, 0, scale, dy });
                using var shifted = new Mat(); Cv2.WarpAffine(terrain, shifted, transform, terrain.Size(), borderValue: new Scalar(40, 40, 40, 255));
                var raster = RegistrationSmokeTest.Raster(shifted);
                tracking.Offer(SyntheticFrame(raster));
                if (first != null && tracking.View != null) throw new InvalidOperationException("View change retained old live mapping.");
                await WaitUntil(() => tracking.View != null);
                var result = tracking.View!.Registration;
                var errors = new List<double>();
                for (var y = 1; y < 15; y++) for (var x = 1; x < 25; x++)
                {
                    var point = new MapPoint(x * 1600 / 25.0, y * 900 / 15.0);
                    if (result.TryMap(point, out var mapped)) errors.Add(mapped.DistanceTo(new(scale * point.X + dx, scale * point.Y + dy)));
                }
                var p95 = errors.Order().ElementAt((int)Math.Ceiling(errors.Count * 0.95) - 1);
                if (errors.Count < 20 || p95 > result.Tolerance) throw new InvalidOperationException("Live mapping exceeds original pixel tolerance.");
                checks.Add(new { type="live-known-warp", scale, dx, dy, controls=errors.Count, truthP95=p95, result.Tolerance });
                first ??= result;
            }
            using var blank = new Mat(900, 1600, MatType.CV_8UC4, new Scalar(40, 40, 40, 255));
            tracking.Offer(SyntheticFrame(RegistrationSmokeTest.Raster(blank)));
            if (tracking.View != null) throw new InvalidOperationException("Closed/blank view retained marker.");
            await Task.Delay(700);
            if (tracking.View != null) throw new InvalidOperationException("Blank view produced valid mapping.");
            checks.Add(new { type="blank-hides-immediately", passed=true });
            tracking.Offer(SyntheticFrame(reference));
            await WaitUntil(() => tracking.View != null);
            await WaitUntil(() => tracking.View == null);
            checks.Add(new { type="no-frames-expires", passed=true });
            await tracking.DisposeAsync(); tracking = null;

            var delayed = new DelayedMatcher(first!);
            var installed = 0;
            tracking = new(setup, view => { if (view != null) installed++; }, delayed);
            tracking.Offer(SyntheticFrame(reference));
            await delayed.Entered.Task;
            for (var i = 0; i < 25; i++) tracking.Offer(SyntheticFrame(reference));
            if (delayed.Calls != 1) throw new InvalidOperationException("Unbounded native match queue.");
            var finish = tracking.DisposeAsync().AsTask(); delayed.Release.TrySetResult(); await finish;
            if (installed != 0 || !delayed.Disposed) throw new InvalidOperationException("Stopped generation installed a late fit or leaked matcher.");
            checks.Add(new { type="bounded-worker-and-stop", offered=26, calls=delayed.Calls, lateResults=installed, disposed=delayed.Disposed });
            tracking = null;

            main = new MainWindow { ShowActivated=false, ShowInTaskbar=false, Left=30, Top=30 };
            Application.Current.MainWindow = main; main.Show();
            var image = new System.Windows.Controls.Image { Source=RegistrationSmokeTest.Bitmap(reference), Stretch=Stretch.Uniform };
            target = new System.Windows.Window { Title="SYNTHETISCHE LIVE-KARTE – kein Spiel", Width=1000, Height=650, Left=110, Top=100,
                ShowActivated=false, ShowInTaskbar=false, Content=image };
            target.Show();
            var handle = new WindowInteropHelper(target).Handle;
            var selector = (ComboBox)main.FindName("WindowSelector");
            selector.ItemsSource = new[] { new WindowTarget(handle, (uint)Environment.ProcessId, target.Title) }; selector.SelectedIndex=0;
            Button(main, "StartButton").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            await WaitUntil(() => Frame(main)?.Geometry.IsValid == true && HasTerrain(Frame(main)!));
            var initialGeometry = Frame(main)!.Geometry;
            checks.Add(new { type="real-wgc-geometry", captureWidth=initialGeometry.FrameSize.Width, captureHeight=initialGeometry.FrameSize.Height,
                window=initialGeometry.WindowBounds.ToString(), client=initialGeometry.ClientBounds.ToString(), timestampAgeMs=(DateTimeOffset.UtcNow-Frame(main)!.CapturedAt).TotalMilliseconds });
            _ = main.Dispatcher.BeginInvoke(new Action(() => Button(main, "CalibrateButton").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent))));
            await WaitUntil(() => Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Any());
            var dialog = Application.Current.Windows.OfType<AutomaticRegistrationWindow>().Single();
            dialog.UseReference(RegistrationSmokeTest.Bitmap(reference), new(hash, reference.Size));
            await dialog.BeginMatchAsync();
            var liveButton = Button(dialog, "LiveButton");
            if (!liveButton.IsEnabled) throw new InvalidOperationException("Dialog did not enable live activation.");
            liveButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            await WaitUntil(() => Live(main)?.View != null);
            var overlay = (OverlayController)typeof(MainWindow).GetField("overlay", BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main)!;
            var view = Live(main)!.View!;
            var required = NativeWindows.Transparent|NativeWindows.NoActivate|NativeWindows.Layered;
            var styles = NativeWindows.GetWindowLongPtr(overlay.Window.Handle, NativeWindows.ExtendedStyle).ToInt64();
            if ((styles & required) != required) throw new InvalidOperationException("Live overlay lost click-through/no-activation styles.");
            target.Activate(); await Task.Delay(100); overlay.Update();
            var foregroundProved = NativeWindows.GetForegroundWindow()==handle;
            if (foregroundProved && !NativeWindows.IsWindowVisible(overlay.Window.Handle)) throw new InvalidOperationException("Live overlay hidden over own foreground target.");
            // OS foreground restrictions can deny Activate for a hidden diagnostic launch.
            // Force only this own overlay briefly to test capture feedback, not normal focus policy.
            if (!foregroundProved) overlay.Window.SetNativeVisibility(true);
            overlay.Window.UpdateLayout();
            var dpi = VisualTreeHelper.GetDpi(overlay.Window);
            if (!view.Registration.TryMap(view.Anchor, out var anchor) || !view.Geometry.TryClientDip(anchor, dpi.DpiScaleX, dpi.DpiScaleY, out var expected))
                throw new InvalidOperationException("Test anchor not projectable.");
            var canvas = (Canvas)overlay.Window.FindName("LiveCanvas");
            var marker = canvas.Children.OfType<System.Windows.Shapes.Ellipse>().Single();
            if (Math.Abs(Canvas.GetLeft(marker)+7-expected.X)>0.01 || Math.Abs(Canvas.GetTop(marker)+7-expected.Y)>0.01)
                throw new InvalidOperationException("Rendered marker is not at projected client DIP.");
            var preview = new RenderTargetBitmap((int)Math.Ceiling(overlay.Window.ActualWidth), (int)Math.Ceiling(overlay.Window.ActualHeight), 96,96,PixelFormats.Pbgra32);
            preview.Render(overlay.Window);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            using(var stream=File.Create(Path.ChangeExtension(path,".overlay.png"))) { var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(preview)); encoder.Save(stream); }
            // Repaint target to get a fresh capture while the independent overlay is shown.
            CapturedFrame? overlayCapture=null;
            await using (var feedbackCapture=new WindowCaptureService { FrameReady=frame=>{ if(HasTerrain(frame)) overlayCapture=frame; return Task.CompletedTask; } })
            {
                feedbackCapture.Start(handle);
                await WaitUntil(()=>overlayCapture!=null);
            }
            var captured=overlayCapture!;
            var sampleX=(int)Math.Round(anchor.X+5); var sampleY=(int)Math.Round(anchor.Y);
            var offset=(sampleY*captured.Width+sampleX)*4;
            if (offset>=0 && offset+3<captured.Pixels.Length && captured.Pixels[offset+1]>180 && captured.Pixels[offset]<100 && captured.Pixels[offset+2]<100)
                throw new InvalidOperationException("Live green marker appeared in target WGC capture.");
            overlay.Window.SetNativeVisibility(false);
            checks.Add(new { type="normal-dialog-to-live", foregroundProved, visibilityForcedOnlyForFeedbackTest=!foregroundProved, correctClientDip=true, markerAbsentFromTargetCapture=true, dpi=dpi.DpiScaleX, anchor=expected.ToString() });
            ((CheckBox)main.FindName("LiveCheck")).IsChecked=false;
            await WaitUntil(()=>Live(main)==null);
            if (overlay.LiveView!=null || canvas.Children.Count!=0) throw new InvalidOperationException("Disable left live marker behind.");
            Button(main,"StopButton").RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            await WaitUntil(()=>Button(main,"StartButton").IsEnabled);
            checks.Add(new {type="disable-and-stop", passed=true});
            target.Close(); target=null;
            passed=true;
        }
        catch(Exception exception) { error=exception.ToString(); }
        finally
        {
            if(tracking!=null) await tracking.DisposeAsync();
            target?.Close();
            // App.OnStartup shuts this own diagnostic process down after report output.
            _ = main;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path,JsonSerializer.Serialize(new {passed,testedAt=DateTimeOffset.UtcNow,checks,error,scope="Only own synthetic windows; no user/game captured or closed."},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }

    private static CapturedFrame SyntheticFrame(RegistrationImage image) => new(image.Size.Width,image.Size.Height,image.Bgra,DateTimeOffset.UtcNow,
        new(image.Size,new(0,0,image.Size.Width,image.Size.Height),new(0,0,image.Size.Width,image.Size.Height)));
    private static System.Windows.Controls.Button Button(System.Windows.Window window,string name)=>(System.Windows.Controls.Button)window.FindName(name);
    private static bool HasTerrain(CapturedFrame frame)
    {
        var i=(frame.Height/2*frame.Width+frame.Width/2)*4;
        return Math.Max(frame.Pixels[i],Math.Max(frame.Pixels[i+1],frame.Pixels[i+2]))-Math.Min(frame.Pixels[i],Math.Min(frame.Pixels[i+1],frame.Pixels[i+2]))>15;
    }
    private static LiveMapController? Live(MainWindow main)=>(LiveMapController?)typeof(MainWindow).GetField("live",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main);
    private static CapturedFrame? Frame(MainWindow main)=>(CapturedFrame?)typeof(MainWindow).GetField("lastCapturedFrame",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main);
    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline=DateTimeOffset.UtcNow.AddSeconds(12);
        while(!condition()) { if(DateTimeOffset.UtcNow>=deadline) throw new TimeoutException("Live diagnostic timed out."); await Task.Delay(30); }
    }
    private sealed class DelayedMatcher(RegistrationResult result):IMapRegistrationService,IAsyncDisposable
    {
        public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls {get;private set;}
        public bool Disposed {get;private set;}
        public async Task<RegistrationOutcome> RegisterAsync(RegistrationImage reference,RegistrationImage capture,string hash,CancellationToken cancellationToken)
        { Calls++; Entered.TrySetResult(); await Release.Task; return new("diagnostic delayed result",result); }
        public ValueTask DisposeAsync() { Disposed=true; return ValueTask.CompletedTask; }
    }
}
