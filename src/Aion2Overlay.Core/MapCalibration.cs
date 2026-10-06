using System.Text.Json.Serialization;

namespace Aion2Overlay.Core;

public readonly record struct MapPoint(double X, double Y)
{
    [JsonIgnore]
    public bool IsFinite => double.IsFinite(X) && double.IsFinite(Y);
    public double DistanceTo(MapPoint other) => Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
}

public readonly record struct ImageSize(int Width, int Height)
{
    [JsonIgnore]
    public bool IsValid => Width > 0 && Height > 0;
    public bool Contains(MapPoint point) => IsValid && point.IsFinite &&
        point.X >= 0 && point.Y >= 0 && point.X <= Width && point.Y <= Height;
}

public readonly record struct LandmarkPair(MapPoint Reference, MapPoint Capture);

public readonly record struct AffineTransform(double A, double B, double C, double D, double E, double F)
{
    public MapPoint Map(MapPoint point) => new(A * point.X + B * point.Y + C, D * point.X + E * point.Y + F);
}

// Shared by click input and drawing: WPF's Uniform image rectangle, in local DIPs.
public readonly record struct ImageViewport(double Left, double Top, double Width, double Height)
{
    public static ImageViewport Fit(double hostWidth, double hostHeight, ImageSize image)
    {
        if (!image.IsValid || !double.IsFinite(hostWidth) || !double.IsFinite(hostHeight) || hostWidth <= 0 || hostHeight <= 0)
            return default;
        var scale = Math.Min(hostWidth / image.Width, hostHeight / image.Height);
        return new((hostWidth - image.Width * scale) / 2, (hostHeight - image.Height * scale) / 2,
            image.Width * scale, image.Height * scale);
    }

    public bool TryNormalize(MapPoint click, out MapPoint normalized)
    {
        normalized = default;
        if (!click.IsFinite || Width <= 0 || Height <= 0 || click.X < Left || click.Y < Top ||
            click.X > Left + Width || click.Y > Top + Height) return false;
        normalized = new((click.X - Left) / Width, (click.Y - Top) / Height);
        return true;
    }

    public MapPoint Display(MapPoint normalized) => new(Left + normalized.X * Width, Top + normalized.Y * Height);
}

public sealed record CalibrationResult(AffineTransform Transform, double[] Errors, double Tolerance)
{
    public bool Passed => Errors.Length == 2 && Errors.All(error => double.IsFinite(error) && error <= Tolerance);
}

public static class MapCalibration
{
    public static AffineTransform Fit(IReadOnlyList<LandmarkPair> anchors, ImageSize capture)
    {
        if (anchors.Count != 3 || !capture.IsValid) throw new ArgumentException("Genau drei Landmarken und eine gültige Aufnahmegröße sind erforderlich.");
        foreach (var pair in anchors) ValidatePair(pair, capture);
        var p = anchors[0].Reference;
        var q = anchors[1].Reference;
        var r = anchors[2].Reference;
        var determinant = Cross(p, q, r);
        var targetArea = Cross(anchors[0].Capture, anchors[1].Capture, anchors[2].Capture) / capture.Width / capture.Height;
        if (Math.Abs(determinant) <= 0.0001 || Math.Abs(targetArea) <= 0.0001)
            throw new ArgumentException("Landmarken liegen zu nah auf einer Linie. Bitte drei weit verteilte Punkte wählen.");

        (double a, double b, double c) Solve(double first, double second, double third)
        {
            var a = ((second - first) * (r.Y - p.Y) - (third - first) * (q.Y - p.Y)) / determinant;
            var b = ((q.X - p.X) * (third - first) - (r.X - p.X) * (second - first)) / determinant;
            return (a, b, first - a * p.X - b * p.Y);
        }
        var x = Solve(anchors[0].Capture.X, anchors[1].Capture.X, anchors[2].Capture.X);
        var y = Solve(anchors[0].Capture.Y, anchors[1].Capture.Y, anchors[2].Capture.Y);
        return new(x.a, x.b, x.c, y.a, y.b, y.c);
    }

    public static CalibrationResult Evaluate(IReadOnlyList<LandmarkPair> anchors, IReadOnlyList<LandmarkPair> checks, ImageSize capture)
    {
        var transform = Fit(anchors, capture);
        if (checks.Count != 2) throw new ArgumentException("Zwei zusätzliche Prüfpunkte sind erforderlich.");
        var previous = anchors.Select(pair => pair.Reference).ToList();
        foreach (var pair in checks)
        {
            ValidatePair(pair, capture);
            if (previous.Any(point => point.DistanceTo(pair.Reference) < 0.01))
                throw new ArgumentException("Prüfpunkte müssen von den bisherigen Landmarken verschieden sein (mindestens 1 % Bildabstand).");
            previous.Add(pair.Reference);
        }
        return new(transform, checks.Select(pair => transform.Map(pair.Reference).DistanceTo(pair.Capture)).ToArray(), 8.0 * capture.Height / 1080);
    }

    private static double Cross(MapPoint p, MapPoint q, MapPoint r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);

    private static void ValidatePair(LandmarkPair pair, ImageSize capture)
    {
        if (!pair.Reference.IsFinite || pair.Reference.X < 0 || pair.Reference.X > 1 || pair.Reference.Y < 0 || pair.Reference.Y > 1 || !capture.Contains(pair.Capture))
            throw new ArgumentException("Eine Landmarke liegt außerhalb des Bildes oder enthält ungültige Koordinaten.");
    }
}

public sealed record ReferenceImageInfo(string Sha256, ImageSize Size);
public sealed record CalibrationMetadata(string MapId, string GameBuild, string ViewDescription);

public sealed record CalibrationProfile(int SchemaVersion, string Region, CalibrationMetadata Metadata,
    ReferenceImageInfo Reference, ImageSize CaptureSize, DateTimeOffset CapturedAt,
    LandmarkPair[] Anchors, LandmarkPair[] Checks, CalibrationResult Validation, DateTimeOffset SavedAt)
{
    public static CalibrationProfile Create(CalibrationMetadata metadata, ReferenceImageInfo reference, ImageSize captureSize,
        DateTimeOffset capturedAt, IReadOnlyList<LandmarkPair> anchors, IReadOnlyList<LandmarkPair> checks, DateTimeOffset savedAt)
    {
        if (string.IsNullOrWhiteSpace(metadata.MapId) || string.IsNullOrWhiteSpace(metadata.GameBuild) || string.IsNullOrWhiteSpace(metadata.ViewDescription))
            throw new ArgumentException("Karten-ID/Gebiet, Global-Spielbuild und feste Ansicht müssen angegeben sein.");
        if (!reference.Size.IsValid || reference.Sha256.Length != 64 || !reference.Sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Referenzgröße oder SHA256 ist ungültig.");
        if (capturedAt == default || savedAt < capturedAt) throw new ArgumentException("Aufnahme- oder Speicherzeitpunkt ist ungültig.");
        var result = MapCalibration.Evaluate(anchors, checks, captureSize);
        if (!result.Passed) throw new ArgumentException("Die Abweichung der Prüfpunkte ist zu groß. Kalibrierung korrigieren.");
        return new(1, "EuropeGlobal", metadata, reference, captureSize, capturedAt, anchors.ToArray(), checks.ToArray(), result, savedAt);
    }
}
