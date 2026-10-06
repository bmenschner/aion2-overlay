using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Aion2Overlay.Core;

namespace Aion2Overlay.App.Services;

// Explicit export only. Original pixels; no automatic screenshots or reference paths.
internal static class RegistrationDiagnosticExport
{
    public static void Write(string path, CapturedFrame frame, LiveMapSetup? setup, string status,
        WindowSnapshot? target, int refreshes, int generation, bool analyzed = false, DateTimeOffset? latestCapturedAt = null)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        Image(archive, "aufnahme.png", new(new(frame.Width, frame.Height), frame.Pixels));
        if (setup != null) Image(archive, "referenz.png", setup.Reference);
        using var report = archive.CreateEntry("zustand.json").Open();
        JsonSerializer.Serialize(report, new
        {
            schemaVersion = 1,
            version = typeof(MainWindow).Assembly.GetName().Version?.ToString(),
            exportedAt = DateTimeOffset.UtcNow, frame.CapturedAt, frame.Sequence, frame.CaptureGeneration,
            frame.Width, frame.Height, frame.Geometry, target, status,
            imageIsLastAnalyzedFrame = analyzed, latestCapturedAt,
            captureRefreshes = refreshes, activeCaptureGeneration = generation,
            hasLiveReference = setup != null,
            referenceSize = setup?.Reference.Size,
            scope = "Explicit export of the selected window frame and supplied local reference. No automatic image storage."
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void Image(ZipArchive archive, string name, RegistrationImage image)
    {
        var bitmap = BitmapSource.Create(image.Size.Width, image.Size.Height, 96, 96, PixelFormats.Bgra32, null, image.Bgra, image.Size.Width * 4);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        // WPF encoders require a seekable stream; ZIP entries are forward-only.
        using var encoded = new MemoryStream(); encoder.Save(encoded); encoded.Position = 0;
        using var stream = archive.CreateEntry(name).Open(); encoded.CopyTo(stream);
    }
}
