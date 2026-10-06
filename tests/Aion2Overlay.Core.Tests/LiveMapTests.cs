using Aion2Overlay.Core;
using Xunit;

namespace Aion2Overlay.Core.Tests;

public class LiveMapTests
{
    [Fact]
    public void ReturningFromBackgroundRefreshesWithoutManualRestart()
    {
        var policy = new CaptureRefreshPolicy(true, TimeSpan.Zero);
        policy.ObserveFrame(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        Assert.False(policy.ShouldRefresh(false, TimeSpan.FromSeconds(1)));
        Assert.False(policy.ShouldRefresh(false, TimeSpan.FromSeconds(30)));
        Assert.True(policy.ShouldRefresh(true, TimeSpan.FromSeconds(30)));
        policy.Refreshed(TimeSpan.FromSeconds(30));
        Assert.False(policy.ShouldRefresh(true, TimeSpan.FromSeconds(30.2)));
        Assert.False(policy.ShouldRefresh(false, TimeSpan.FromSeconds(30.3)));
        Assert.True(policy.ShouldRefresh(true, TimeSpan.FromSeconds(30.4)));
    }

    [Fact]
    public void StalledForegroundCaptureRetriesAtMostEveryTwoSeconds()
    {
        var policy = new CaptureRefreshPolicy(true, TimeSpan.Zero);
        Assert.False(policy.ShouldRefresh(true, TimeSpan.FromSeconds(1.9)));
        Assert.True(policy.ShouldRefresh(true, TimeSpan.FromSeconds(2)));
        policy.Refreshed(TimeSpan.FromSeconds(2));
        Assert.False(policy.ShouldRefresh(true, TimeSpan.FromSeconds(3.9)));
        Assert.True(policy.ShouldRefresh(true, TimeSpan.FromSeconds(4)));
    }

    [Fact]
    public void RepeatedOldAndFutureCompositorTimesDoNotDelayRecovery()
    {
        var policy = new CaptureRefreshPolicy(true, TimeSpan.Zero);
        policy.ObserveFrame(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        policy.ObserveFrame(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2.9));
        policy.ObserveFrame(TimeSpan.FromSeconds(100), TimeSpan.FromSeconds(2.9));
        Assert.True(policy.ShouldRefresh(true, TimeSpan.FromSeconds(3)));
        policy.Refreshed(TimeSpan.FromSeconds(3));
        policy.ObserveFrame(TimeSpan.FromSeconds(3.8), TimeSpan.FromSeconds(4));
        Assert.False(policy.ShouldRefresh(true, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void WindowFrameIsNotStretchedAcrossClientAndDpiIsAppliedOnce()
    {
        var geometry = new CaptureGeometry(new(1920, 1080), new(-1920, 10, 1920, 1080), new(-1912, 42, 1904, 1040));
        Assert.True(geometry.IsValid);
        Assert.True(geometry.TryClientDip(new(308, 332), 1.5, 1.5, out var dip));
        Assert.Equal(new MapPoint(200, 200), dip);
        Assert.False(geometry.TryClientDip(new(5, 10), 1, 1, out _));
        Assert.False(geometry.TryClientDip(new(1919, 500), 1, 1, out _));
        Assert.False(geometry.TryClientDip(new(500, 500), 0, 1, out _));
    }

    [Fact]
    public void BorderlessUltrawideAndUnknownGeometry()
    {
        var valid = new CaptureGeometry(new(5120, 1440), new(0, 0, 5120, 1440), new(0, 0, 5120, 1440));
        Assert.True(valid.TryClientDip(new(4000, 800), 1, 1, out var point));
        Assert.Equal(new MapPoint(4000, 800), point);
        Assert.False((valid with { FrameSize = new(5119, 1440) }).IsValid);
        Assert.False(default(CaptureGeometry).IsValid);
        Assert.False(valid.TryClientDip(new(double.NaN, 100), 1, 1, out _));
    }

    [Fact]
    public void MovingWindowPreservesMappingButResizeAndFrameOffsetDoNot()
    {
        var original = new CaptureGeometry(new(1000, 650), new(30, 50, 1000, 650), new(38, 82, 984, 610));
        Assert.True(original.Compatible(original with { WindowBounds = new(300, 400, 1000, 650), ClientBounds = new(308, 432, 984, 610) }));
        Assert.False(original.Compatible(original with { ClientBounds = new(38, 83, 984, 609) }));
        Assert.False(original.Compatible(original with { FrameSize = new(2000, 1300) }));
    }

    [Fact]
    public void StaleFutureAndObsoleteJobsCannotBecomeLive()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(LiveMapPolicy.Fresh(now.AddMilliseconds(-1999), now));
        Assert.False(LiveMapPolicy.Fresh(now.AddSeconds(-2), now));
        Assert.False(LiveMapPolicy.Fresh(now.AddMilliseconds(1), now));
        var geometry = new CaptureGeometry(new(100, 100), new(0, 0, 100, 100), new(0, 0, 100, 100));
        var signature = new LiveFrameSignature(new byte[] { 20, 30 });
        Assert.False(LiveMapPolicy.CanApply(1, 2, now, now, geometry, geometry, signature, signature));
        Assert.True(LiveMapPolicy.CanApply(2, 2, now, now, geometry, geometry, signature, signature));
        Assert.False(LiveMapPolicy.CanApply(2, 2, now.AddSeconds(-3), now, geometry, geometry, signature, signature));
    }

    [Fact]
    public void FrameChangeHidesOldFitWhileSmallDynamicRegionIsTolerated()
    {
        var a = new LiveFrameSignature(Enumerable.Repeat((byte)100, 1000).ToArray());
        var small = a.Samples.ToArray(); small[10] = 200;
        var moved = Enumerable.Repeat((byte)130, 1000).ToArray();
        Assert.True(a.SameView(new(small)));
        Assert.False(a.SameView(new(moved)));
        Assert.False(a.SameView(new(Array.Empty<byte>())));
    }
}
