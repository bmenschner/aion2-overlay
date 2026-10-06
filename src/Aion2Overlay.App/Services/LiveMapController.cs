using System.Windows.Threading;
using Aion2Overlay.Core;
using Aion2Overlay.Imaging;

namespace Aion2Overlay.App.Services;

public sealed record LiveMapSetup(RegistrationImage Reference, string Hash, MapPoint Anchor);
public sealed record LiveMapView(RegistrationResult Registration, CaptureGeometry Geometry, DateTimeOffset CapturedAt, MapPoint Anchor);

public sealed class LiveMapController : IAsyncDisposable
{
    private readonly LiveMapSetup setup;
    private readonly IMapRegistrationService matcher;
    private readonly CancellationTokenSource cancellation = new();
    private readonly DispatcherTimer timer;
    private readonly Action<LiveMapView?> publish;
    private CapturedFrame? latest;
    private LiveFrameSignature? latestSignature;
    private LiveFrameSignature? fittedSignature;
    private LiveMapView? view;
    private Task? worker;
    private DateTimeOffset lastStarted;
    private int epoch;
    private bool disposed;
    private string status = "";
    public event Action<string>? StatusChanged;
    public LiveMapView? View => view;
    public int StartedMatches { get; private set; }
    internal bool IsMatching => worker is { IsCompleted: false };

    public LiveMapController(LiveMapSetup setup, Action<LiveMapView?> publish, IMapRegistrationService? matcher = null)
    {
        this.setup = setup; this.publish = publish;
        this.matcher = matcher ?? new OpenCvMapRegistrationService();
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += Tick; timer.Start();
    }

    public void Offer(CapturedFrame frame)
    {
        if (disposed) return;
        latest = frame;
        latestSignature = LiveFrameSignature.Create(new(new(frame.Width, frame.Height), frame.Pixels));
        if (!frame.Geometry.IsValid || !LiveMapPolicy.Fresh(frame.CapturedAt, DateTimeOffset.UtcNow))
            Clear("Live-Zuordnung wartet auf eine frische Aufnahme mit passender Fenstergeometrie.");
        else if (view != null && (!view.Geometry.Compatible(frame.Geometry) || fittedSignature?.SameView(latestSignature) != true))
            Clear("Ansicht geändert – Live-Zuordnung wird neu geprüft.");
        TryStart();
    }

    private void Tick(object? sender, EventArgs args)
    {
        if (disposed) return;
        if (view != null && !LiveMapPolicy.Fresh(view.CapturedAt, DateTimeOffset.UtcNow)) Clear("Live-Zuordnung veraltet – Anzeige ausgeblendet.");
        TryStart();
    }

    private void TryStart()
    {
        if (disposed || worker is { IsCompleted: false } || latest == null || latestSignature == null ||
            !latest.Geometry.IsValid || !LiveMapPolicy.Fresh(latest.CapturedAt, DateTimeOffset.UtcNow) ||
            DateTimeOffset.UtcNow - lastStarted < TimeSpan.FromMilliseconds(500)) return;
        lastStarted = DateTimeOffset.UtcNow;
        StartedMatches++;
        worker = MatchAsync(latest, latestSignature, epoch);
    }

    private async Task MatchAsync(CapturedFrame frame, LiveFrameSignature signature, int generation)
    {
        try
        {
            var outcome = await matcher.RegisterAsync(setup.Reference, new(new(frame.Width, frame.Height), frame.Pixels), setup.Hash, cancellation.Token);
            if (disposed || generation != epoch || latest == null || latestSignature == null) return;
            if (!outcome.Passed) { Clear($"Live-Zuordnung ausgesetzt: {outcome.Message}"); return; }
            if (!LiveMapPolicy.CanApply(generation, epoch, frame.CapturedAt, DateTimeOffset.UtcNow, frame.Geometry, latest.Geometry, signature, latestSignature))
            { Clear("Aufnahme hat sich geändert – warte auf neuen Abgleich."); return; }
            fittedSignature = signature;
            view = new(outcome.Result!, frame.Geometry, frame.CapturedAt, setup.Anchor);
            publish(view);
            SetStatus($"Live-Zuordnung aktiv · Testpunkt, keine Cubes · {outcome.Result!.Quality.DurationMs / 1000:F1} s pro Abgleich");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) { if (!disposed) Clear($"Live-Zuordnung ausgesetzt: {error.Message}"); }
    }

    private void SetStatus(string message) { if (status == message) return; status = message; StatusChanged?.Invoke(message); }
    private void Clear(string message) { view = null; fittedSignature = null; publish(null); SetStatus(message); }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true; epoch++; timer.Stop(); timer.Tick -= Tick;
        cancellation.Cancel(); publish(null);
        if (worker != null) await worker;
        if (matcher is IAsyncDisposable disposable) await disposable.DisposeAsync();
        cancellation.Dispose(); latest = null; latestSignature = fittedSignature = null; view = null;
    }
}
