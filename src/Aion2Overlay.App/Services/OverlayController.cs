using System.Windows.Threading;
using Aion2Overlay.App.Interop;
using Aion2Overlay.Core;

namespace Aion2Overlay.App.Services;

public sealed class OverlayController : IDisposable
{
    private readonly WindowTarget target;
    private readonly OverlayWindow window;
    private readonly DispatcherTimer timer;
    private bool visible;
    private bool disposed;
    public bool AlignmentRequested { get; set; } = true;
    public LiveMapView? LiveView { get; set; }
    public event Action<string>? Ended;
    public OverlayWindow Window => window;

    public OverlayController(WindowTarget target)
    {
        this.target = target;
        window = new OverlayWindow();
        window.Show();
        window.SetNativeVisibility(false);
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += Tick;
        timer.Start();
    }

    private void Tick(object? sender, EventArgs args) => Update();

    public void Update()
    {
        if (disposed) return;
        var snapshot = NativeWindows.Snapshot(target.Handle);
        NativeWindows.GetWindowThreadProcessId(target.Handle, out var pid);
        if (!snapshot.Exists || pid != target.ProcessId)
        {
            window.SetNativeVisibility(false);
            timer.Stop();
            Ended?.Invoke("Das ausgewählte Fenster ist nicht mehr verfügbar.");
            return;
        }
        try
        {
            if (snapshot.ClientBounds.HasArea && !snapshot.Minimized) window.Align(snapshot.ClientBounds);
            var geometry = new CaptureGeometry(LiveView?.Geometry.FrameSize ?? default, NativeWindows.FrameBounds(target.Handle), snapshot.ClientBounds);
            var live = LiveView != null && LiveMapPolicy.Fresh(LiveView.CapturedAt, DateTimeOffset.UtcNow) && LiveView.Geometry.Compatible(geometry) ? LiveView : null;
            window.DrawLive(live, AlignmentRequested);
            var display = OverlayPolicy.ShouldDisplay(true, AlignmentRequested || live != null, snapshot);
            if (display != visible)
            {
                window.SetNativeVisibility(display);
                visible = display;
            }
        }
        catch (Exception exception)
        {
            timer.Stop();
            window.SetNativeVisibility(false);
            Ended?.Invoke($"Overlay beendet: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        timer.Tick -= Tick;
        window.Close();
    }
}
