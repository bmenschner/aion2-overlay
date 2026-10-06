using Aion2Overlay.Core;
using Xunit;

namespace Aion2Overlay.Core.Tests;

public class OverlayPolicyTests
{
    private static WindowSnapshot Target => new(new(-1920, 120, 1920, 1080), true, true, false, true);

    [Fact]
    public void TargetOnMonitorLeftOfPrimaryCanBeDisplayed() =>
        Assert.True(OverlayPolicy.ShouldDisplay(true, true, Target));

    [Fact]
    public void SwitchingToAnotherApplicationHidesOverlay() =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, true, Target with { Foreground = false }));

    [Fact]
    public void MinimizedTargetHidesOverlayEvenIfItStillOwnsForeground() =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, true, Target with { Minimized = true }));

    [Fact]
    public void ClosedTargetNeverDisplaysStaleGeometry() =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, true, Target with { Exists = false }));

    [Fact]
    public void HiddenTargetDoesNotDisplayOverlay() =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, true, Target with { Visible = false }));

    [Fact]
    public void StoppingCaptureHidesOverlay() =>
        Assert.False(OverlayPolicy.ShouldDisplay(false, true, Target));

    [Fact]
    public void DisablingAlignmentHidesFrameWhileCaptureCanContinue() =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, false, Target));

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    public void EmptyOrInvalidGeometryCannotProduceOverlay(int width, int height) =>
        Assert.False(OverlayPolicy.ShouldDisplay(true, true,
            Target with { ClientBounds = new(0, 0, width, height) }));
}
