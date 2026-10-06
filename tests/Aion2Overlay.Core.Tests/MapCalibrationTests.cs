using System.Text.Json;
using Aion2Overlay.Core;
using Xunit;

namespace Aion2Overlay.Core.Tests;

public class MapCalibrationTests
{
    private static readonly ImageSize Capture = new(1920, 1080);
    // Synthetic rotation/shear/translation, unrelated to any Aion map.
    private static readonly AffineTransform Known = new(900, 200, 100, -120, 650, 200);
    private static LandmarkPair Pair(double x, double y) => new(new(x, y), Known.Map(new(x, y)));
    private static LandmarkPair[] Anchors => [Pair(0.1, 0.1), Pair(0.9, 0.15), Pair(0.2, 0.9)];
    private static LandmarkPair[] Checks => [Pair(0.4, 0.4), Pair(0.7, 0.6)];

    [Fact]
    public void RecoversRotationShearAndTranslationAtIndependentPoint()
    {
        var result = MapCalibration.Fit(Anchors, Capture).Map(new(0.73, 0.26));
        Assert.InRange(result.DistanceTo(Known.Map(new(0.73, 0.26))), 0, 1e-9);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(0.50001)]
    public void RejectsCollinearAndNearlyCollinearReference(double thirdY)
    {
        LandmarkPair[] anchors = [Pair(0.1, 0.1), Pair(0.9, 0.9), Pair(0.5, thirdY)];
        Assert.Throws<ArgumentException>(() => MapCalibration.Fit(anchors, Capture));
    }

    [Fact]
    public void RejectsDegenerateTargetAndNonFiniteReference()
    {
        var anchors = Anchors;
        anchors[2] = anchors[2] with { Capture = anchors[1].Capture };
        Assert.Throws<ArgumentException>(() => MapCalibration.Fit(anchors, Capture));
        anchors = Anchors;
        anchors[0] = anchors[0] with { Reference = new(double.NaN, 0.1) };
        Assert.Throws<ArgumentException>(() => MapCalibration.Fit(anchors, Capture));
    }

    [Fact]
    public void CheckPointsAreIndependentAndNotUsedForFit()
    {
        var checks = Checks;
        checks[0] = checks[0] with { Capture = new(checks[0].Capture.X + 12, checks[0].Capture.Y) };
        var result = MapCalibration.Evaluate(Anchors, checks, Capture);
        Assert.False(result.Passed);
        Assert.InRange(result.Errors[0], 11.999, 12.001);
        Assert.InRange(result.Transform.Map(new(0.4, 0.4)).DistanceTo(Known.Map(new(0.4, 0.4))), 0, 1e-9);
        checks[0] = Anchors[0];
        Assert.Throws<ArgumentException>(() => MapCalibration.Evaluate(Anchors, checks, Capture));
        Assert.Throws<ArgumentException>(() => MapCalibration.Evaluate(Anchors, [checks[1], checks[1]], Capture));
    }

    [Theory]
    [InlineData(1080, 7.9, true)]
    [InlineData(1080, 8.1, false)]
    [InlineData(2160, 15.9, true)]
    [InlineData(2160, 16.1, false)]
    public void ScalesToleranceWithCaptureHeight(int height, double error, bool passed)
    {
        var checks = Checks;
        checks[1] = checks[1] with { Capture = new(checks[1].Capture.X, checks[1].Capture.Y + error) };
        Assert.Equal(passed, MapCalibration.Evaluate(Anchors, checks, new(1920, height)).Passed);
    }

    [Theory]
    [InlineData(800, 600, 1920, 1080)]
    [InlineData(400, 900, 800, 1200)]
    public void LetterboxingAndResizePreserveCoordinates(int width, int height, int imageWidth, int imageHeight)
    {
        var viewport = ImageViewport.Fit(width, height, new(imageWidth, imageHeight));
        var expected = new MapPoint(0.25, 0.7);
        Assert.True(viewport.TryNormalize(viewport.Display(expected), out var actual));
        Assert.InRange(actual.DistanceTo(expected), 0, 1e-12);
        Assert.False(viewport.TryNormalize(new(viewport.Left - 1, viewport.Top), out _));
        var resized = ImageViewport.Fit(width * 2, height / 2.0, new(imageWidth, imageHeight));
        Assert.True(resized.TryNormalize(resized.Display(actual), out var afterResize));
        Assert.InRange(afterResize.DistanceTo(expected), 0, 1e-12);
    }

    [Fact]
    public void ProfileRoundtripRetainsEvidenceAndRejectsMissingMetadataOrFailedChecks()
    {
        var metadata = new CalibrationMetadata("synthetic-map", "synthetic-build", "Synthetic fixed view");
        var reference = new ReferenceImageInfo(new string('a', 64), new(1000, 1000));
        var capturedAt = new DateTimeOffset(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
        var profile = CalibrationProfile.Create(metadata, reference, Capture, capturedAt, Anchors, Checks, capturedAt.AddMinutes(1));
        var json = JsonSerializer.Serialize(profile);
        var restored = JsonSerializer.Deserialize<CalibrationProfile>(json)!;
        Assert.Equal("EuropeGlobal", restored.Region);
        Assert.Equal(profile.Validation.Transform, restored.Validation.Transform);
        Assert.Equal(Anchors, restored.Anchors);
        Assert.Equal(Checks, restored.Checks);
        Assert.Equal(reference, restored.Reference);
        Assert.Throws<ArgumentException>(() => CalibrationProfile.Create(metadata with { GameBuild = "" }, reference, Capture, capturedAt, Anchors, Checks, capturedAt));
        var badChecks = Checks;
        badChecks[0] = badChecks[0] with { Capture = new(1500, 800) };
        Assert.Throws<ArgumentException>(() => CalibrationProfile.Create(metadata, reference, Capture, capturedAt, Anchors, badChecks, capturedAt));
    }
}
