using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Aion2Overlay.Core;
using Microsoft.Win32;

namespace Aion2Overlay.App;

public partial class CalibrationWindow : Window
{
    private readonly ImageSize captureSize;
    private readonly DateTimeOffset capturedAt;
    private readonly List<LandmarkPair> pairs = [];
    private ReferenceImageInfo? reference;
    private MapPoint? pendingReference;
    private CalibrationResult? result;
    private double referenceZoom = 1;
    private double captureZoom = 1;

    public CalibrationWindow(BitmapSource captureImage, DateTimeOffset capturedAt)
    {
        InitializeComponent();
        captureSize = new(captureImage.PixelWidth, captureImage.PixelHeight);
        this.capturedAt = capturedAt;
        CaptureImage.Source = captureImage;
        CaptureLabel.Text = $"2 · AUFNAHMEBILD · {captureSize.Width} × {captureSize.Height} px";
        Width = Math.Min(Width, SystemParameters.WorkArea.Width);
        Height = Math.Min(Height, SystemParameters.WorkArea.Height);
        Loaded += (_, _) => UpdateViewSizes();
    }

    private void OpenReferenceClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Lokale Referenzkarte auswählen", Filter = "Kartenbilder|*.png;*.jpg;*.jpeg;*.bmp", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            using var stream = File.OpenRead(dialog.FileName);
            if (stream.Length > 64 * 1024 * 1024) throw new InvalidDataException("Das Referenzbild darf höchstens 64 MiB groß sein.");
            var hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            stream.Position = 0;
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            if (frame.PixelWidth > 8192 || frame.PixelHeight > 8192 || (long)frame.PixelWidth * frame.PixelHeight > 40_000_000)
                throw new InvalidDataException("Das Bild ist zu groß: maximal 8192 Pixel je Achse und 40 Millionen Pixel.");
            var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
            var stride = checked(converted.PixelWidth * 4);
            var pixels = new byte[checked(stride * converted.PixelHeight)];
            converted.CopyPixels(pixels, stride, 0);
            // Uniform must follow pixel aspect ratio, independent of file DPI metadata.
            var bitmap = BitmapSource.Create(frame.PixelWidth, frame.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            bitmap.Freeze();
            UseReference(bitmap, new(hash, new(bitmap.PixelWidth, bitmap.PixelHeight)));
            ReferenceLabel.Text = $"{System.IO.Path.GetFileName(dialog.FileName)} · {bitmap.PixelWidth} × {bitmap.PixelHeight} px · Europa/Global";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException or FileFormatException)
        {
            ResultText.Text = $"Referenzbild konnte nicht geöffnet werden: {exception.Message}";
        }
    }

    internal void UseReference(BitmapSource bitmap, ReferenceImageInfo info)
    {
        reference = info;
        ReferenceImage.Source = bitmap;
        ReferenceEmpty.Visibility = Visibility.Collapsed;
        referenceZoom = captureZoom = 1;
        UpdateViewSizes();
        ReferenceScroll.ScrollToHome();
        CaptureScroll.ScrollToHome();
        ResetPairs();
    }

    private void ReferenceClick(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(ReferenceScroll);
        SelectFromViewport(true, new(point.X, point.Y));
    }

    private void CaptureClick(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(CaptureScroll);
        SelectFromViewport(false, new(point.X, point.Y));
    }

    internal bool SelectFromViewport(bool onReference, MapPoint point)
    {
        var scroll = onReference ? ReferenceScroll : CaptureScroll;
        var host = onReference ? ReferenceHost : CaptureHost;
        var contentPoint = scroll.TranslatePoint(new(point.X, point.Y), host);
        return SelectAt(onReference, new(contentPoint.X, contentPoint.Y));
    }

    // Both actual clicks and the dialog diagnostic pass through the letterbox conversion.
    internal bool SelectAt(bool onReference, MapPoint displayPoint)
    {
        if (reference == null || pairs.Count >= 5) return false;
        var host = onReference ? ReferenceHost : CaptureHost;
        var size = onReference ? reference.Size : captureSize;
        var viewport = ImageViewport.Fit(host.ActualWidth, host.ActualHeight, size);
        if (!viewport.TryNormalize(displayPoint, out var normalized)) return false;
        if (onReference) pendingReference = normalized;
        else
        {
            if (pendingReference == null) return false;
            pairs.Add(new(pendingReference.Value, new(normalized.X * captureSize.Width, normalized.Y * captureSize.Height)));
            pendingReference = null;
        }
        UpdateState();
        return true;
    }

    private void MetadataChanged(object sender, TextChangedEventArgs e)
    {
        if (SaveButton != null) UpdateState();
    }

    private void UndoClick(object sender, RoutedEventArgs e) => UndoPair();
    internal void UndoPair()
    {
        if (pendingReference != null) pendingReference = null;
        else if (pairs.Count > 0) pairs.RemoveAt(pairs.Count - 1);
        UpdateState();
    }

    private void ResetClick(object sender, RoutedEventArgs e) => ResetPairs();
    internal void ResetPairs()
    {
        pairs.Clear();
        pendingReference = null;
        UpdateState();
    }

    private void UpdateState()
    {
        if (CaptureMarks == null) return;
        result = null;
        SaveButton.IsEnabled = false;
        UndoButton.IsEnabled = pendingReference != null || pairs.Count > 0;
        StepText.Text = reference == null ? "Zuerst eine Referenzkarte öffnen."
            : pairs.Count == 5 ? "Alle fünf Paare gesetzt. Kreuz = dein Prüfpunkt · Kreis = Vorhersage."
            : $"{(pairs.Count < 3 ? $"Landmarke {pairs.Count + 1}/3" : $"Prüfpunkt {pairs.Count - 2}/2")} · {(pendingReference == null ? "Zuerst links auf die Referenzkarte klicken." : "Jetzt rechts dieselbe Stelle im Aufnahmebild anklicken.")}";
        ResultText.Text = $"{pairs.Count}/5 Paare gesetzt. Drei weit verteilte Landmarken und zwei weitere Prüfpunkte erforderlich.";
        if (pairs.Count >= 3)
        {
            try
            {
                MapCalibration.Fit(pairs.Take(3).ToArray(), captureSize);
                ResultText.Text = "Abbildung berechnet. Zwei zusätzliche Landmarken prüfen die Genauigkeit.";
                if (pairs.Count == 5)
                {
                    result = MapCalibration.Evaluate(pairs.Take(3).ToArray(), pairs.Skip(3).ToArray(), captureSize);
                    ResultText.Text = $"{(result.Passed ? "Prüfung bestanden" : "Abweichung zu groß")} · Fehler: {result.Errors[0]:F1} / {result.Errors[1]:F1} px · Grenze: {result.Tolerance:F1} px.";
                    if (result.Passed)
                    {
                        if (HasMetadata()) SaveButton.IsEnabled = true;
                        else ResultText.Text += " Karten-ID, Global-Spielbuild und feste Ansicht ergänzen.";
                    }
                }
            }
            catch (ArgumentException exception) { ResultText.Text = exception.Message; }
        }
        DrawPoints();
    }

    private bool HasMetadata() => !string.IsNullOrWhiteSpace(MapIdInput.Text) && !string.IsNullOrWhiteSpace(BuildInput.Text) && !string.IsNullOrWhiteSpace(ViewInput.Text);

    internal CalibrationProfile BuildProfile() => CalibrationProfile.Create(
        new(MapIdInput.Text.Trim(), BuildInput.Text.Trim(), ViewInput.Text.Trim()),
        reference ?? throw new InvalidOperationException("Referenzkarte fehlt."), captureSize, capturedAt,
        pairs.Take(3).ToArray(), pairs.Skip(3).ToArray(), DateTimeOffset.UtcNow);

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = "Geprüfte Kalibrierung speichern", Filter = "Kalibrierungsprofil|*.json", DefaultExt = ".json", FileName = "aion2-kalibrierung.json", AddExtension = true, OverwritePrompt = true };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var profile = BuildProfile();
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            ResultText.Text = $"Kalibrierung gespeichert: {System.IO.Path.GetFileName(dialog.FileName)}. Gilt nur für die angegebene feste Ansicht.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            ResultText.Text = $"Speichern fehlgeschlagen: {exception.Message}";
        }
    }

    private void ImageSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (CaptureMarks != null && ReferenceMarks != null) DrawPoints();
    }

    private void ReferenceZoomIn(object sender, RoutedEventArgs e) => ZoomImage(true, 2);
    private void ReferenceZoomOut(object sender, RoutedEventArgs e) => ZoomImage(true, 0.5);
    private void CaptureZoomIn(object sender, RoutedEventArgs e) => ZoomImage(false, 2);
    private void CaptureZoomOut(object sender, RoutedEventArgs e) => ZoomImage(false, 0.5);
    private void ReferenceWheel(object sender, MouseWheelEventArgs e) => WheelZoom(true, e);
    private void CaptureWheel(object sender, MouseWheelEventArgs e) => WheelZoom(false, e);

    private void WheelZoom(bool onReference, MouseWheelEventArgs e)
    {
        e.Handled = true;
        var host = onReference ? ReferenceHost : CaptureHost;
        var position = e.GetPosition(host);
        ZoomImage(onReference, e.Delta > 0 ? 2 : 0.5, new(position.X, position.Y));
    }

    private void ViewportChanged(object sender, ScrollChangedEventArgs e)
    {
        if (ReferenceHost != null && CaptureHost != null && (e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0)) UpdateViewSizes();
    }

    private void UpdateViewSizes()
    {
        void SizeView(ScrollViewer scroll, Grid host, double zoom)
        {
            if (scroll.ViewportWidth > 0 && scroll.ViewportHeight > 0)
            {
                host.Width = scroll.ViewportWidth * zoom;
                host.Height = scroll.ViewportHeight * zoom;
            }
        }
        SizeView(ReferenceScroll, ReferenceHost, referenceZoom);
        SizeView(CaptureScroll, CaptureHost, captureZoom);
        ReferenceZoomLabel.Text = $"{referenceZoom:0.#}×";
        CaptureZoomLabel.Text = $"{captureZoom:0.#}×";
    }

    internal void ZoomImage(bool onReference, double factor, MapPoint? pointer = null)
    {
        var host = onReference ? ReferenceHost : CaptureHost;
        var scroll = onReference ? ReferenceScroll : CaptureScroll;
        var size = onReference ? reference?.Size : captureSize;
        if (size == null) return;
        var oldView = ImageViewport.Fit(host.ActualWidth, host.ActualHeight, size.Value);
        var anchor = pointer ?? new MapPoint(scroll.HorizontalOffset + scroll.ViewportWidth / 2, scroll.VerticalOffset + scroll.ViewportHeight / 2);
        var screenX = anchor.X - scroll.HorizontalOffset;
        var screenY = anchor.Y - scroll.VerticalOffset;
        if (!oldView.TryNormalize(anchor, out var normalized))
        {
            normalized = new(0.5, 0.5);
            screenX = scroll.ViewportWidth / 2;
            screenY = scroll.ViewportHeight / 2;
        }
        if (onReference) referenceZoom = Math.Clamp(referenceZoom * factor, 1, 16);
        else captureZoom = Math.Clamp(captureZoom * factor, 1, 16);
        UpdateViewSizes();
        UpdateLayout();
        var newView = ImageViewport.Fit(host.ActualWidth, host.ActualHeight, size.Value);
        var position = newView.Display(normalized);
        scroll.ScrollToHorizontalOffset(position.X - screenX);
        scroll.ScrollToVerticalOffset(position.Y - screenY);
    }

    private void DrawPoints()
    {
        ReferenceMarks.Children.Clear();
        CaptureMarks.Children.Clear();
        if (reference == null) return;
        var referenceView = ImageViewport.Fit(ReferenceHost.ActualWidth, ReferenceHost.ActualHeight, reference.Size);
        var captureView = ImageViewport.Fit(CaptureHost.ActualWidth, CaptureHost.ActualHeight, captureSize);
        for (var index = 0; index < pairs.Count; index++)
        {
            var pair = pairs[index];
            var color = index < 3 ? Brushes.Gold : Brushes.Cyan;
            AddMark(ReferenceMarks, referenceView.Display(pair.Reference), (index + 1).ToString(), color, false);
            AddMark(CaptureMarks, captureView.Display(new(pair.Capture.X / captureSize.Width, pair.Capture.Y / captureSize.Height)), (index + 1).ToString(), color, false);
            if (result != null && index >= 3)
            {
                var predicted = result.Transform.Map(pair.Reference);
                AddMark(CaptureMarks, captureView.Display(new(predicted.X / captureSize.Width, predicted.Y / captureSize.Height)), "", result.Errors[index - 3] <= result.Tolerance ? Brushes.LimeGreen : Brushes.OrangeRed, true);
            }
        }
        if (pendingReference is { } pending) AddMark(ReferenceMarks, referenceView.Display(pending), (pairs.Count + 1).ToString(), Brushes.White, false);
    }

    private static void AddMark(Canvas canvas, MapPoint point, string label, Brush color, bool circle)
    {
        if (circle)
        {
            var ellipse = new Ellipse { Width = 18, Height = 18, Stroke = color, StrokeThickness = 2 };
            Canvas.SetLeft(ellipse, point.X - 9);
            Canvas.SetTop(ellipse, point.Y - 9);
            canvas.Children.Add(ellipse);
        }
        else
        {
            canvas.Children.Add(new Line { X1 = point.X - 7, X2 = point.X + 7, Y1 = point.Y, Y2 = point.Y, Stroke = color, StrokeThickness = 2 });
            canvas.Children.Add(new Line { X1 = point.X, X2 = point.X, Y1 = point.Y - 7, Y2 = point.Y + 7, Stroke = color, StrokeThickness = 2 });
            var text = new TextBlock { Text = label, Foreground = color, Background = Brushes.Black, FontWeight = FontWeights.Bold, Padding = new Thickness(2) };
            Canvas.SetLeft(text, point.X + 8);
            Canvas.SetTop(text, point.Y - 18);
            canvas.Children.Add(text);
        }
    }
}
