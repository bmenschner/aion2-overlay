namespace Aion2Overlay.Core;

// Monotonic compositor/QPC times; a repeated frame does not extend its freshness.
public sealed class CaptureRefreshPolicy(bool initiallyActive, TimeSpan startedAt)
{
    private bool wasActive = initiallyActive;
    private TimeSpan lastFrame = startedAt;
    private TimeSpan lastRefresh = startedAt;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    public void ObserveFrame(TimeSpan capturedAt, TimeSpan now)
    {
        if (capturedAt <= now && now - capturedAt < Interval && capturedAt > lastFrame)
            lastFrame = capturedAt;
    }

    public bool ShouldRefresh(bool active, TimeSpan now)
    {
        var returning = active && !wasActive;
        wasActive = active;
        return active && (returning || (now - lastFrame >= Interval && now - lastRefresh >= Interval));
    }

    public void Refreshed(TimeSpan now) { lastFrame = lastRefresh = now; }
}
