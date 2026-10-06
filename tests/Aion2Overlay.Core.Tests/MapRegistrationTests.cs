using Aion2Overlay.Core;
using Xunit;

namespace Aion2Overlay.Core.Tests;

public class MapRegistrationTests
{
    private static RegistrationQuality Good => new(100, 80, 30, 29, 2, 6, 0.7, 12, 0.9, 0.9, 100);

    [Fact]
    public void IndependentQualityIsRequiredBeyondInlierCount()
    {
        Assert.Null(RegistrationQualityGate.Reject(Good, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { HoldoutP95Pixels = 9 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { HoldoutCount = 0 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { PatchMedian = 0.1 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { PatchMedian = double.NaN }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { Inliers = 101 }, 8));
    }

    [Fact]
    public void SpatialClustersAndInvalidQualityAreRejected()
    {
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { OccupiedCells = 1 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { HullFraction = 0.01 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { HoldoutInliers = 2 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { PatchChecks = 1 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good with { Candidates = 1000 }, 8));
        Assert.NotNull(RegistrationQualityGate.Reject(Good, double.NaN));
    }

    [Fact]
    public void UnsupportedReferenceLocationsCannotBeMapped()
    {
        var result = MakeResult();
        Assert.True(result.TryMap(new(200, 200), out var mapped));
        Assert.Equal(new MapPoint(300, 250), mapped);
        Assert.False(result.TryMap(new(700, 700), out _));
        Assert.False(result.TryMap(new(double.NaN, 100), out _));
        Assert.False((result with { PixelTransform = new(1, 0, double.NaN, 0, 1, 0) }).Passed);
    }

    [Fact]
    public void AutomaticProfileCannotUseManualOrFailedResult()
    {
        var reference = new ReferenceImageInfo(new string('a', 64), new(800, 800));
        var profile = AutomaticRegistrationProfile.Create(reference, MakeResult(), DateTimeOffset.UtcNow);
        Assert.Equal(2, profile.SchemaVersion); Assert.Equal("automatic", profile.Method);
        Assert.False(profile.BuildVerified);
        Assert.Throws<ArgumentException>(() => AutomaticRegistrationProfile.Create(reference,
            MakeResult() with { Quality = Good with { PatchMedian = 0 } }, DateTimeOffset.UtcNow));
        var json = System.Text.Json.JsonSerializer.Serialize(profile);
        var restored = System.Text.Json.JsonSerializer.Deserialize<AutomaticRegistrationProfile>(json)!;
        Assert.True(restored.Registration.Passed);
        Assert.DoesNotContain("Bgra", json); Assert.DoesNotContain("Path", json);
    }

    [Fact]
    public void InvalidRasterLengthIsRejected()
    {
        Assert.Throws<ArgumentException>(() => new RegistrationImage(new(800, 600), new byte[4]).Validate());
        Assert.Throws<ArgumentException>(() => new RegistrationImage(new(int.MaxValue, int.MaxValue), []).Validate());
    }

    private static RegistrationResult MakeResult() => new(new(1, 0, 100, 0, 1, 50), new(800, 800), new(1000, 1000),
        [new(100, 100), new(500, 100), new(500, 500), new(100, 500)], Good);

    [Fact]
    public void CropAndRoundedResizeReturnOriginalPixels()
    {
        var source = new RegistrationImageGeometry(new(3000, 1000), new(400, 100), new(1000, 333));
        var target = new RegistrationImageGeometry(new(1400, 900), new(90, 40), new(700, 450));
        var working = new AffineTransform(1.4, -0.1, 17, 0.1, 1.4, -23);
        var original = RegistrationImageGeometry.ToOriginalTransform(working, source, target);
        foreach (var p in new[] { new MapPoint(900, 300), new MapPoint(2500, 700), new MapPoint(5120, 1440) })
        {
            Assert.True(source.ToOriginal(source.ToWorking(p)).DistanceTo(p) < 1e-8);
            Assert.True(original.Map(p).DistanceTo(target.ToOriginal(working.Map(source.ToWorking(p)))) < 1e-8);
        }
    }
}
