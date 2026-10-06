using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Aion2Overlay.Core;
using Aion2Overlay.Imaging;
using Microsoft.Win32;

namespace Aion2Overlay.App;

public partial class AutomaticRegistrationWindow : Window
{
    private BitmapSource captureBitmap;
    private DateTimeOffset capturedAt;
    private readonly Func<(BitmapSource Image, DateTimeOffset Timestamp)>? freshCapture;
    private readonly OpenCvMapRegistrationService matcher = new();
    private readonly List<Task> jobs = [];
    private readonly bool rememberReference;
    private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aion2Overlay", "last-reference.json");
    private BitmapSource? referenceBitmap;
    private ReferenceImageInfo? reference;
    private RegistrationResult? registration;
    private BitmapSource? overlayBitmap;
    private CancellationTokenSource? cancellation;
    private Task? finishing;
    private int generation;
    private bool closed;

    public AutomaticRegistrationWindow(BitmapSource capture, DateTimeOffset timestamp, bool rememberReference = true,
        Func<(BitmapSource Image, DateTimeOffset Timestamp)>? freshCapture = null)
    {
        InitializeComponent();
        captureBitmap = capture;
        capturedAt = timestamp;
        this.freshCapture = freshCapture;
        this.rememberReference = rememberReference;
        CaptureImage.Source = capture;
        UpdateCaptureTime();
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        Closed += (_, _) => { closed = true; generation++; cancellation?.Cancel(); _ = FinishAsync(); };
        if (rememberReference) Loaded += (_, _) => RestoreReference();
    }

    private void RestoreReference()
    {
        try
        {
            if (!File.Exists(settingsPath) || new FileInfo(settingsPath).Length > 8192) return;
            var saved = JsonSerializer.Deserialize<RememberedReference>(File.ReadAllText(settingsPath));
            if (saved == null || !File.Exists(saved.Path)) return;
            LoadReference(saved.Path, saved.Sha256);
        }
        catch (Exception e) { StatusText.Text = $"Letzte Referenz nicht verwendbar. Bitte erneut öffnen. {e.Message}"; }
    }

    private void OpenReferenceClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Lokale Referenzkarte auswählen", Filter = "Kartenbilder|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try { LoadReference(dialog.FileName); }
        catch (Exception exception) { StatusText.Text = $"Referenz konnte nicht geöffnet werden: {exception.Message}"; }
    }

    private void LoadReference(string path, string? expectedHash = null)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 64 * 1024 * 1024) throw new InvalidDataException("Referenz darf höchstens 64 MiB groß sein.");
        var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (expectedHash != null && hash != expectedHash) throw new InvalidDataException("Die Referenzdatei wurde verändert. Neu auswählen und neu abgleichen.");
        stream.Position = 0;
        var bitmap = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        if (bitmap.PixelWidth > 8192 || bitmap.PixelHeight > 8192 || (long)bitmap.PixelWidth * bitmap.PixelHeight > 40_000_000)
            throw new InvalidDataException("Bild ist zu groß: maximal 8192 Pixel je Achse und 40 Millionen Pixel.");
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[checked(converted.PixelWidth * converted.PixelHeight * 4)];
        converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
        var normalized = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
        normalized.Freeze();
        UseReference(normalized, new(hash, new(normalized.PixelWidth, normalized.PixelHeight)));
        ReferenceLabel.Text = $"1 · {Path.GetFileName(path)} · {normalized.PixelWidth} × {normalized.PixelHeight} px";
        if (rememberReference)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(new RememberedReference(path, hash)));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { QualityText.Text = "Referenz geöffnet; Merken für den nächsten Start nicht möglich."; }
        }
    }

    internal void UseReference(BitmapSource bitmap, ReferenceImageInfo info)
    {
        generation++; cancellation?.Cancel();
        referenceBitmap = bitmap; reference = info;
        ReferenceImage.Source = bitmap; ReferenceEmpty.Visibility = Visibility.Collapsed;
        ClearResult();
        StatusText.Text = "Bereit. Klicke auf „Automatisch abgleichen“.";
        SetBusy(false);
    }

    private async void MatchClick(object sender, RoutedEventArgs e) => await BeginMatchAsync();

    internal async Task BeginMatchAsync()
    {
        if (referenceBitmap == null || reference == null || closed || MatchButton.IsEnabled == false) return;
        if (freshCapture != null)
        {
            try
            {
                var frame = freshCapture();
                if (DateTimeOffset.UtcNow - frame.Timestamp >= TimeSpan.FromSeconds(2)) throw new InvalidOperationException("Aufnahme ist veraltet.");
                captureBitmap = frame.Image; capturedAt = frame.Timestamp; UpdateCaptureTime();
            }
            catch (Exception exception) { ClearResult(); StatusText.Text = $"Keine frische Aufnahme: {exception.Message} Aufnahme prüfen und erneut versuchen."; return; }
        }
        cancellation?.Cancel();
        var tokenSource = new CancellationTokenSource();
        cancellation = tokenSource;
        var epoch = ++generation;
        var image = referenceBitmap;
        var info = reference;
        ClearResult(); SetBusy(true);
        StatusText.Text = "Gemeinsame Gelände- und Wegmerkmale werden gesucht …";
        var job = RunAsync();
        jobs.RemoveAll(task => task.IsCompleted);
        jobs.Add(job);
        await job;

        async Task RunAsync()
        {
            try
            {
                var token = tokenSource.Token;
                var input = await Task.Run(() => (ToRaster(image), ToRaster(captureBitmap)), token);
                var outcome = await matcher.RegisterAsync(input.Item1, input.Item2, info.Sha256, token);
                if (closed || epoch != generation || token.IsCancellationRequested) return;
                StatusText.Text = outcome.Message;
                if (outcome.Passed)
                {
                    registration = outcome.Result;
                    overlayBitmap = RenderOverlay();
                    var q = registration!.Quality;
                    QualityText.Text = $"{q.Inliers}/{q.Candidates} passende Merkmale · unabhängiger Fehler (95 %): {q.HoldoutP95Pixels:F1} px / Grenze {registration.Tolerance:F1} px · {q.PatchChecks} Geländeprüfungen · {q.DurationMs / 1000:F1} s";
                    SaveButton.IsEnabled = true; OverlayCheck.IsEnabled = true;
                    UpdatePreview();
                }
            }
            catch (OperationCanceledException) { if (!closed && epoch == generation) StatusText.Text = "Abgleich abgebrochen."; }
            catch (Exception exception) { if (!closed && epoch == generation) StatusText.Text = $"Abgleich fehlgeschlagen: {exception.Message}"; }
            finally
            {
                tokenSource.Dispose();
                if (ReferenceEquals(cancellation, tokenSource)) cancellation = null;
                if (!closed && epoch == generation) SetBusy(false);
            }
        }
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        generation++; cancellation?.Cancel(); ClearResult(); SetBusy(false);
        StatusText.Text = "Abgleich abgebrochen. Kein gültiges Ergebnis.";
    }

    private void SetBusy(bool busy)
    {
        MatchButton.IsEnabled = !busy && reference != null;
        CancelButton.IsEnabled = busy;
        LoadButton.IsEnabled = !busy;
    }

    private void ClearResult()
    {
        registration = null; overlayBitmap = null;
        SaveButton.IsEnabled = OverlayCheck.IsEnabled = false;
        QualityText.Text = ""; CaptureImage.Source = captureBitmap;
        CaptureLabel.Text = "2 · EINGEFRORENE AUFNAHME";
    }

    private void UpdateCaptureTime() => CaptureTimeText.Text = $"Eingefrorene Aufnahme: {capturedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss} · {captureBitmap.PixelWidth} × {captureBitmap.PixelHeight} px · kein Live-Abgleich";

    private void OverlayChanged(object sender, RoutedEventArgs e) => UpdatePreview();
    private void UpdatePreview()
    {
        if (CaptureImage == null) return;
        CaptureImage.Source = OverlayCheck.IsChecked == true && overlayBitmap != null ? overlayBitmap : captureBitmap;
        CaptureLabel.Text = overlayBitmap != null && OverlayCheck.IsChecked == true ? "2 · GEPRÜFTE ÜBERLAGERUNG" : "2 · EINGEFRORENE AUFNAHME";
    }

    private BitmapSource RenderOverlay()
    {
        var result = registration!;
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawImage(captureBitmap, new Rect(0, 0, captureBitmap.PixelWidth, captureBitmap.PixelHeight));
            var polygon = new StreamGeometry();
            using (var context = polygon.Open())
            {
                var points = result.SupportReference.Select(result.PixelTransform.Map).Select(p => new Point(p.X, p.Y)).ToArray();
                context.BeginFigure(points[0], true, true); context.PolyLineTo(points.Skip(1).ToArray(), true, false);
            }
            polygon.Freeze();
            drawing.PushClip(polygon); drawing.PushOpacity(0.45);
            var t = result.PixelTransform;
            drawing.PushTransform(new MatrixTransform(t.A, t.D, t.B, t.E, t.C, t.F));
            drawing.DrawImage(referenceBitmap, new Rect(0, 0, result.ReferenceSize.Width, result.ReferenceSize.Height));
            drawing.Pop(); drawing.Pop(); drawing.Pop();
            drawing.DrawGeometry(null, new Pen(Brushes.LimeGreen, Math.Max(2, captureBitmap.PixelHeight / 360.0)), polygon);
        }
        var bitmap = new RenderTargetBitmap(captureBitmap.PixelWidth, captureBitmap.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }

    internal AutomaticRegistrationProfile BuildProfile() => AutomaticRegistrationProfile.Create(reference ?? throw new InvalidOperationException("Referenz fehlt."),
        registration ?? throw new InvalidOperationException("Abgleich fehlt."), capturedAt, MapIdInput.Text.Trim(), BuildInput.Text.Trim(), ViewInput.Text.Trim());

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        if (registration?.Passed != true) return;
        var dialog = new SaveFileDialog { Title = "Automatische Zuordnung speichern", Filter = "Zuordnungsprofil|*.json", DefaultExt = ".json", FileName = "aion2-zuordnung.json", AddExtension = true, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try { File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(BuildProfile(), new JsonSerializerOptions { WriteIndented = true })); StatusText.Text = "Zuordnung gespeichert. Beim nächsten Einsatz neu abgleichen."; }
        catch (Exception exception) { StatusText.Text = $"Speichern fehlgeschlagen: {exception.Message}"; }
    }

    private void LoadProfileClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Automatisches Profil öffnen", Filter = "Zuordnungsprofil|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(dialog.FileName).Length > 1024 * 1024) throw new InvalidDataException("Profil ist zu groß.");
            UseProfile(File.ReadAllText(dialog.FileName));
        }
        catch (Exception exception) { StatusText.Text = $"Profil nicht verwendbar: {exception.Message}"; }
    }

    internal void UseProfile(string json)
    {
        generation++; cancellation?.Cancel(); ClearResult(); SetBusy(false);
        var profile = JsonSerializer.Deserialize<AutomaticRegistrationProfile>(json);
        if (profile == null || profile.SchemaVersion != 2 || profile.Method != "automatic" || profile.Region != "EuropeGlobal")
            throw new InvalidDataException("Nur automatische Schema-2-Profile sind verwendbar.");
        if (reference == null || profile.Reference?.Sha256 != reference.Sha256 || profile.Reference.Size != reference.Size)
            throw new InvalidDataException("Zuerst dieselbe Referenzkarte öffnen. Der Bildhash muss übereinstimmen.");
        MapIdInput.Text = profile.MapId; BuildInput.Text = profile.GameBuild; ViewInput.Text = profile.ViewDescription;
        StatusText.Text = "Profilangaben geladen. Für dieses Aufnahmebild erneut „Automatisch abgleichen“ wählen.";
        // Never install the saved transformation as a current result.
    }

    internal static RegistrationImage ToRaster(BitmapSource bitmap)
    {
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[checked(converted.PixelWidth * converted.PixelHeight * 4)];
        converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
        return new(new(converted.PixelWidth, converted.PixelHeight), pixels);
    }

    internal Task FinishAsync() => finishing ??= FinishCoreAsync();
    private async Task FinishCoreAsync()
    {
        try { await Task.WhenAll(jobs); }
        finally { await matcher.DisposeAsync(); }
    }
    private sealed record RememberedReference(string Path, string Sha256);
}
