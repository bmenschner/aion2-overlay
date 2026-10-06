using System.Text.Json.Serialization;

namespace Aion2Overlay.Core;

public sealed record RegistrationImage(ImageSize Size, byte[] Bgra)
{
    public void Validate()
    {
        if (!Size.IsValid || Size.Width > 8192 || Size.Height > 8192 || (long)Size.Width * Size.Height > 40_000_000 || Bgra.LongLength != (long)Size.Width * Size.Height * 4)
            throw new ArgumentException("Ungültige Bildgröße oder Pixeldaten.");
    }
}

public readonly record struct RegistrationImageGeometry(ImageSize CropSize, MapPoint CropOrigin, ImageSize WorkingSize)
{
    public MapPoint ToWorking(MapPoint p) => new((p.X - CropOrigin.X + 0.5) * WorkingSize.Width / CropSize.Width - 0.5,
        (p.Y - CropOrigin.Y + 0.5) * WorkingSize.Height / CropSize.Height - 0.5);
    public MapPoint ToOriginal(MapPoint p) => new((p.X + 0.5) * CropSize.Width / WorkingSize.Width - 0.5 + CropOrigin.X,
        (p.Y + 0.5) * CropSize.Height / WorkingSize.Height - 0.5 + CropOrigin.Y);

    public static AffineTransform ToOriginalTransform(AffineTransform working, RegistrationImageGeometry source, RegistrationImageGeometry target)
    {
        if (!source.CropSize.IsValid || !source.WorkingSize.IsValid || !target.CropSize.IsValid || !target.WorkingSize.IsValid || !source.CropOrigin.IsFinite || !target.CropOrigin.IsFinite)
            throw new ArgumentException("Ungültige Arbeitsbild-Geometrie.");
        MapPoint Map(MapPoint p) => target.ToOriginal(working.Map(source.ToWorking(p)));
        var origin = Map(new(0, 0)); var x = Map(new(1, 0)); var y = Map(new(0, 1));
        return new(x.X - origin.X, y.X - origin.X, origin.X, x.Y - origin.Y, y.Y - origin.Y, origin.Y);
    }
}

public sealed record RegistrationQuality(int Candidates, int Inliers, int HoldoutCount, int HoldoutInliers,
    double HoldoutP95Pixels, int OccupiedCells, double HullFraction, int PatchChecks,
    double PatchPassFraction, double PatchMedian, double DurationMs)
{
    [JsonIgnore] public double InlierRatio => Candidates > 0 ? (double)Inliers / Candidates : 0;
}

public static class RegistrationQualityGate
{
    public static string? Reject(RegistrationQuality q, double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance <= 0 || q.Candidates < 40 || q.Inliers < 25 || q.Inliers > q.Candidates || q.InlierRatio < 0.55)
            return "Zu wenig gemeinsame Kartenmerkmale. Verschiebe die Karte zum Referenzausschnitt.";
        if (q.OccupiedCells < 4 || !double.IsFinite(q.HullFraction) || q.HullFraction < 0.25 || q.HullFraction > 1.01)
            return "Die Treffer liegen zu eng beieinander. Ein größerer gemeinsamer Geländeabschnitt ist nötig.";
        if (q.HoldoutCount < 10 || q.HoldoutInliers < Math.Ceiling(q.HoldoutCount * 0.7) || !double.IsFinite(q.HoldoutP95Pixels) || q.HoldoutP95Pixels > tolerance)
            return "Die unabhängige Genauigkeitsprüfung ist fehlgeschlagen. Ansicht oder Referenz passen nicht sicher zusammen.";
        if (q.PatchChecks < 6 || !double.IsFinite(q.PatchMedian) || !double.IsFinite(q.PatchPassFraction) || q.PatchMedian < 0.7 || q.PatchPassFraction < 0.7)
            return "Die Geländeausschnitte stimmen nicht sicher überein. Bitte denselben Kartenbereich anzeigen.";
        return null;
    }
}

public sealed record RegistrationResult(AffineTransform PixelTransform, ImageSize ReferenceSize,
    ImageSize CaptureSize, MapPoint[] SupportReference, RegistrationQuality Quality)
{
    [JsonIgnore] public double Tolerance => 8.0 * CaptureSize.Height / 1080;
    [JsonIgnore] public bool Passed => ReferenceSize.IsValid && CaptureSize.IsValid &&
        SupportReference.Length >= 3 && SupportReference.All(ReferenceSize.Contains) &&
        new[] { PixelTransform.A, PixelTransform.B, PixelTransform.C, PixelTransform.D, PixelTransform.E, PixelTransform.F }.All(double.IsFinite) &&
        PixelTransform.A * PixelTransform.E - PixelTransform.B * PixelTransform.D > 0 &&
        RegistrationQualityGate.Reject(Quality, Tolerance) == null;

    public bool TryMap(MapPoint referencePixel, out MapPoint capturePixel)
    {
        capturePixel = default;
        if (!Passed || !referencePixel.IsFinite || !InsidePolygon(referencePixel, SupportReference)) return false;
        var mapped = PixelTransform.Map(referencePixel);
        if (!CaptureSize.Contains(mapped)) return false;
        capturePixel = mapped;
        return true;
    }

    public static bool InsidePolygon(MapPoint p, IReadOnlyList<MapPoint> polygon)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
}

public sealed record RegistrationOutcome(string Message, RegistrationResult? Result)
{
    [JsonIgnore] public bool Passed => Result?.Passed == true;
}

public interface IMapRegistrationService
{
    Task<RegistrationOutcome> RegisterAsync(RegistrationImage reference, RegistrationImage capture,
        string referenceHash, CancellationToken cancellationToken);
}

public sealed record AutomaticRegistrationProfile(int SchemaVersion, string Method, string Region,
    string MapId, string GameBuild, bool BuildVerified, string ViewDescription,
    ReferenceImageInfo Reference, DateTimeOffset CapturedAt, DateTimeOffset SavedAt,
    string AlgorithmVersion, string MaskVersion, RegistrationResult Registration)
{
    public static AutomaticRegistrationProfile Create(ReferenceImageInfo reference, RegistrationResult registration,
        DateTimeOffset capturedAt, string mapId = "", string gameBuild = "", string view = "")
    {
        if (!registration.Passed || !reference.Size.IsValid || reference.Size != registration.ReferenceSize || reference.Sha256.Length != 64 || !reference.Sha256.All(Uri.IsHexDigit) || capturedAt == default)
            throw new ArgumentException("Kein geprüftes automatisches Ergebnis vorhanden.");
        return new(2, "automatic", "EuropeGlobal", mapId, gameBuild, false, view, reference, capturedAt,
            DateTimeOffset.UtcNow, "sift-similarity-1-opencv4.13", "ingame-terrain-1", registration);
    }
}
