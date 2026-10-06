using System.Diagnostics;
using Aion2Overlay.Core;
using OpenCvSharp;
using OpenCvSharp.Features2D;

namespace Aion2Overlay.Imaging;

public sealed class OpenCvMapRegistrationService : IMapRegistrationService, IAsyncDisposable
{
    private readonly SemaphoreSlim mutex = new(1, 1);
    private Features? referenceCache;
    private string? cachedHash;
    private bool disposed;

    public async Task<RegistrationOutcome> RegisterAsync(RegistrationImage reference, RegistrationImage capture,
        string referenceHash, CancellationToken cancellationToken)
    {
        reference.Validate(); capture.Validate();
        await mutex.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return await Task.Run(() => Register(reference, capture, referenceHash, cancellationToken), cancellationToken);
        }
        finally { mutex.Release(); }
    }

    private RegistrationOutcome Register(RegistrationImage reference, RegistrationImage capture, string hash, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        token.ThrowIfCancellationRequested();
        if (referenceCache == null || cachedHash != hash || referenceCache.OriginalSize != reference.Size)
        {
            referenceCache?.Dispose(); referenceCache = null;
            referenceCache = Features.Create(reference);
            cachedHash = hash;
        }
        var source = referenceCache;
        using var target = Features.Create(capture);
        token.ThrowIfCancellationRequested();
        if (source.Descriptors.Empty() || target.Descriptors.Empty())
            return Failure("Zu wenig sichtbares Gelände. Öffne denselben Kartenbereich wie auf der Referenz.");
        using var matcher = new BFMatcher(NormTypes.L2);
        var forward = matcher.KnnMatch(source.Descriptors, target.Descriptors, 2);
        var reverse = matcher.KnnMatch(target.Descriptors, source.Descriptors, 2);
        var matches = forward.Where(pair => pair.Length == 2 && pair[0].Distance < 0.72 * pair[1].Distance)
            .Select(pair => pair[0]).Where(match => reverse[match.TrainIdx].Length == 2 &&
                reverse[match.TrainIdx][0].Distance < 0.72 * reverse[match.TrainIdx][1].Distance &&
                reverse[match.TrainIdx][0].TrainIdx == match.QueryIdx).ToArray();
        token.ThrowIfCancellationRequested();
        if (matches.Length < 40) return Failure($"Nur {matches.Length} gemeinsame Merkmale. Zeige mehr gemeinsames Gelände und vergleiche erneut.");

        // Entire spatial cells are held out, rather than fitting and checking the same points.
        int Cell(DMatch match)
        {
            var p = source.Keypoints[match.QueryIdx].Pt;
            return Math.Clamp((int)(p.Y / source.Gray.Height * 3), 0, 2) * 3 + Math.Clamp((int)(p.X / source.Gray.Width * 3), 0, 2);
        }
        var fit = matches.Where(m => Cell(m) % 2 == 0).ToArray();
        var holdout = matches.Where(m => Cell(m) % 2 != 0).ToArray();
        if (fit.Length < 25 || holdout.Length < 10) return Failure("Die gemeinsamen Merkmale sind nicht ausreichend über die Karte verteilt.");
        using var model = Estimate(fit, source, target);
        if (model.Empty()) return Failure("Keine eindeutige Kartenabbildung gefunden.");
        var work = Transform(model);
        var scale = Math.Sqrt(work.A * work.A + work.D * work.D) * source.Scale / target.Scale;
        if (!double.IsFinite(scale) || scale < 0.2 || scale > 5 || Math.Abs(Math.Atan2(work.D, work.A)) > Math.PI / 12)
            return Failure("Zoom oder Kartenrichtung liegen außerhalb der unterstützten Ansicht.");
        var pixelTransform = RegistrationImageGeometry.ToOriginalTransform(work, source.Geometry, target.Geometry);
        var tolerance = 8.0 * capture.Size.Height / 1080;
        double Error(DMatch match) => pixelTransform.Map(source.Geometry.ToOriginal(ToMap(source.Keypoints[match.QueryIdx].Pt)))
            .DistanceTo(target.Geometry.ToOriginal(ToMap(target.Keypoints[match.TrainIdx].Pt)));
        var inliers = matches.Where(m => Error(m) <= tolerance).ToArray();
        var outliers = matches.Where(m => Error(m) > tolerance).ToArray();
        if (outliers.Length >= 25 && outliers.Length >= inliers.Length * 0.8)
        {
            using var alternative = Estimate(outliers, source, target);
            if (!alternative.Empty())
            {
                var other = Transform(alternative);
                var supported = outliers.Count(m => other.Map(ToMap(source.Keypoints[m.QueryIdx].Pt)).DistanceTo(ToMap(target.Keypoints[m.TrainIdx].Pt)) / target.Scale <= tolerance);
                if (supported >= inliers.Length * 0.8) return Failure("Uneindeutig: Mehrere Kartenbereiche passen ähnlich gut. Wähle einen anderen Ausschnitt.");
            }
        }
        var errors = holdout.Select(Error).Order().ToArray();
        var inlierSource = inliers.Select(m => source.Keypoints[m.QueryIdx].Pt).ToArray();
        if (inlierSource.Length < 3) return Failure("Keine belastbaren gemeinsamen Geländemerkmale.");
        var hull = Cv2.ConvexHull(inlierSource);
        // The supporting polygon bounds where a future reference position can be mapped.
        var support = hull.Select(p => source.Geometry.ToOriginal(ToMap(p))).ToArray();
        using var warped = new Mat();
        using var warpedMask = new Mat();
        Cv2.WarpAffine(source.Gray, warped, model, target.Gray.Size());
        Cv2.WarpAffine(source.Mask, warpedMask, model, target.Gray.Size(), InterpolationFlags.Nearest);
        Cv2.BitwiseAnd(warpedMask, target.Mask, warpedMask);
        var overlapPixels = Cv2.CountNonZero(warpedMask);
        var hullAreaTarget = Math.Abs(Cv2.ContourArea(hull)) * (work.A * work.A + work.D * work.D);
        var patches = PatchCorrelations(warped, target.Gray, warpedMask, token);
        var quality = new RegistrationQuality(matches.Length, inliers.Length, holdout.Length,
            errors.Count(e => e <= tolerance), Percentile(errors, 0.95), inliers.Select(Cell).Distinct().Count(),
            overlapPixels > 0 ? Math.Min(1, hullAreaTarget / overlapPixels) : 0,
            patches.Length, patches.Length > 0 ? (double)patches.Count(p => p >= 0.6) / patches.Length : 0,
            patches.Length > 0 ? Percentile(patches.Order().ToArray(), 0.5) : 0, stopwatch.Elapsed.TotalMilliseconds);
        token.ThrowIfCancellationRequested();
        var rejected = RegistrationQualityGate.Reject(quality, tolerance);
        if (rejected != null) return Failure(rejected);
        var result = new RegistrationResult(pixelTransform, reference.Size, capture.Size, support, quality);
        return new("Kartenbilder passen zusammen. Die Überlagerung zeigt den geprüften gemeinsamen Bereich.", result);
    }

    private static Mat Estimate(DMatch[] matches, Features source, Features target)
    {
        using var from = InputArray.Create(matches.Select(m => source.Keypoints[m.QueryIdx].Pt).ToArray());
        using var to = InputArray.Create(matches.Select(m => target.Keypoints[m.TrainIdx].Pt).ToArray());
        using var inliers = new Mat();
        return Cv2.EstimateAffinePartial2D(from, to, inliers, RobustEstimationAlgorithms.RANSAC, 2, 2000, 0.99, 10) ?? new Mat();
    }

    private static AffineTransform Transform(Mat model) => new(model.At<double>(0, 0), model.At<double>(0, 1), model.At<double>(0, 2),
        model.At<double>(1, 0), model.At<double>(1, 1), model.At<double>(1, 2));
    private static MapPoint ToMap(Point2f p) => new(p.X, p.Y);
    private static RegistrationOutcome Failure(string message) => new(message, null);
    private static double Percentile(double[] sorted, double p) => sorted.Length == 0 ? double.PositiveInfinity : sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * p) - 1, 0, sorted.Length - 1)];

    private static double[] PatchCorrelations(Mat aligned, Mat capture, Mat mask, CancellationToken token)
    {
        var correlations = new List<double>();
        const int half = 12;
        for (var gy = 1; gy < 8; gy++)
            for (var gx = 1; gx < 16; gx++)
            {
                token.ThrowIfCancellationRequested();
                var x = gx * capture.Width / 16;
                var y = gy * capture.Height / 8;
                if (x < half || y < half || x + half >= capture.Width || y + half >= capture.Height) continue;
                var rect = new Rect(x - half, y - half, half * 2 + 1, half * 2 + 1);
                using var patchMask = new Mat(mask, rect);
                if (Cv2.CountNonZero(patchMask) < rect.Width * rect.Height * 0.7) continue;
                using var a = new Mat(aligned, rect);
                using var b = new Mat(capture, rect);
                Cv2.MeanStdDev(b, out _, out var deviation, patchMask);
                if (deviation.Val0 < 8) continue;
                using var correlation = new Mat();
                Cv2.MatchTemplate(a, b, correlation, TemplateMatchModes.CCoeffNormed);
                var value = correlation.At<float>(0, 0);
                if (float.IsFinite(value)) correlations.Add(value);
            }
        return correlations.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        await mutex.WaitAsync();
        try { disposed = true; referenceCache?.Dispose(); referenceCache = null; }
        finally { mutex.Release(); }
    }

    private sealed class Features : IDisposable
    {
        public required Mat Gray { get; init; }
        public required Mat Mask { get; init; }
        public required Mat Descriptors { get; init; }
        public required KeyPoint[] Keypoints { get; init; }
        public required double Scale { get; init; }
        public required ImageSize OriginalSize { get; init; }
        public RegistrationImageGeometry Geometry => new(OriginalSize, new(0, 0), new(Gray.Width, Gray.Height));

        public static Features Create(RegistrationImage image)
        {
            using var original = Mat.FromPixelData(image.Size.Height, image.Size.Width, MatType.CV_8UC4, image.Bgra);
            var scale = Math.Min(1, 1600.0 / Math.Max(image.Size.Width, image.Size.Height));
            using var resized = new Mat();
            Cv2.Resize(original, resized, new Size((int)Math.Round(image.Size.Width * scale), (int)Math.Round(image.Size.Height * scale)), 0, 0, InterpolationFlags.Area);
            var gray = new Mat(); var mask = new Mat(); var descriptors = new Mat();
            try
            {
                Cv2.CvtColor(resized, gray, ColorConversionCodes.BGRA2GRAY);
                using var bgr = new Mat(); using var hsv = new Mat(); using var icons = new Mat();
                Cv2.CvtColor(resized, bgr, ColorConversionCodes.BGRA2BGR);
                Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
                Cv2.InRange(hsv, new Scalar(0, 12, 30), new Scalar(180, 220, 235), mask);
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(9, 9));
                Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
                Cv2.InRange(hsv, new Scalar(0, 80, 190), new Scalar(180, 255, 255), icons);
                Cv2.Dilate(icons, icons, kernel);
                mask.SetTo(Scalar.Black, icons);
                using var layout = new Mat(mask.Size(), MatType.CV_8UC1, Scalar.Black);
                var roi = new Rect((int)(mask.Width * 0.13), (int)(mask.Height * 0.1), (int)(mask.Width * 0.83), (int)(mask.Height * 0.82));
                Cv2.Rectangle(layout, roi, Scalar.White, -1);
                Cv2.BitwiseAnd(mask, layout, mask);
                using var sift = SIFT.Create(3500);
                sift.DetectAndCompute(gray, mask, out var keypoints, descriptors);
                return new() { Gray = gray, Mask = mask, Descriptors = descriptors, Keypoints = keypoints, Scale = scale, OriginalSize = image.Size };
            }
            catch { gray.Dispose(); mask.Dispose(); descriptors.Dispose(); throw; }
        }
        public void Dispose() { Gray.Dispose(); Mask.Dispose(); Descriptors.Dispose(); }
    }
}
