using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aion2Overlay.App.Services;

namespace Aion2Overlay.App;

public partial class MainWindow : Window
{
    private WindowCaptureService? capture;
    private OverlayController? overlay;
    private readonly DispatcherTimer freshnessTimer;
    private DateTimeOffset? lastFrame;
    private string frameSize = "";
    private bool busy;
    private bool closeReady;
    private bool closeRequested;
    private bool shutdownQueued;
    private AutomaticRegistrationWindow? calibrationWindow;
    private LiveMapController? live;
    private CapturedFrame? lastCapturedFrame;
    private Task liveFinishing = Task.CompletedTask;

    public MainWindow()
    {
        InitializeComponent();
        freshnessTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        freshnessTimer.Tick += (_, _) => UpdateFreshness();
        freshnessTimer.Start();
        Loaded += (_, _) => RefreshWindows();
        Closing += OnClosing;
    }

    private void RefreshWindows()
    {
        var old = (WindowSelector.SelectedItem as WindowTarget)?.Handle;
        var windows = WindowCatalog.List();
        WindowSelector.ItemsSource = windows;
        WindowSelector.SelectedItem = windows.FirstOrDefault(x => x.Handle == old);
        // Listing Aion first is useful; recording still requires an explicit choice.
        if (windows.Count == 0) StatusText.Text = "Keine geeigneten Fenster gefunden. Spielfenster öffnen und aktualisieren.";
    }

    private void RefreshClick(object sender, RoutedEventArgs e) => RefreshWindows();

    private async void StartClick(object sender, RoutedEventArgs e)
    {
        if (busy || capture != null) return;
        if (WindowSelector.SelectedItem is not WindowTarget target)
        {
            StatusText.Text = "Bitte zuerst ein Fenster auswählen.";
            return;
        }
        busy = true;
        SetControls();
        StatusText.Text = "Aufnahme wird gestartet …";
        try
        {
            lastFrame = null;
            Preview.Source = null;
            EmptyPreview.Visibility = Visibility.Visible;
            FrameInfo.Text = "Warte auf erstes Bild …";
            capture = new WindowCaptureService();
            capture.FrameReady = frame => Dispatcher.InvokeAsync(() => PresentFrame(frame)).Task;
            capture.Ended += OnSessionEnded;
            capture.Start(target.Handle);
            overlay = new OverlayController(target) { AlignmentRequested = AlignmentCheck.IsChecked == true };
            overlay.Ended += OnSessionEnded;
            overlay.Update();
            StatusText.Text = $"Aufnahme aktiv: {target.Title}. Zum Spielfenster wechseln, um den Rahmen zu sehen.";
        }
        catch (Exception exception)
        {
            await StopSessionAsync($"Start fehlgeschlagen: {exception.Message}");
        }
        finally
        {
            FinishOperation();
        }
    }

    private void PresentFrame(CapturedFrame frame)
    {
        if (capture == null) return;
        var image = BitmapSource.Create(frame.Width, frame.Height, 96, 96,
            PixelFormats.Bgra32, null, frame.Pixels, checked(frame.Width * 4));
        image.Freeze();
        Preview.Source = image;
        EmptyPreview.Visibility = Visibility.Collapsed;
        lastFrame = frame.CapturedAt;
        lastCapturedFrame = frame;
        live?.Offer(frame);
        frameSize = $"{frame.Width} × {frame.Height} px";
        UpdateFreshness();
        SetControls();
    }

    private void UpdateFreshness()
    {
        if (capture == null) return;
        if (lastFrame == null)
        {
            FrameInfo.Text = "Warte auf erstes Bild · noch keine gültige Aufnahme";
            return;
        }
        var age = DateTimeOffset.UtcNow - lastFrame.Value;
        FrameInfo.Text = age.TotalSeconds >= 2
            ? $"{frameSize} · VERALTET ({age.TotalSeconds:F0} s)"
            : $"{frameSize} · aktuell";
        CalibrateButton.IsEnabled = !busy && age.TotalSeconds < 2 && calibrationWindow == null;
    }

    private async void CalibrateClick(object sender, RoutedEventArgs e)
    {
        if (busy || capture == null || lastFrame == null || DateTimeOffset.UtcNow - lastFrame.Value >= TimeSpan.FromSeconds(2) || Preview.Source is not BitmapSource image)
        {
            StatusText.Text = "Für die Kalibrierung eine Aufnahme starten und auf ein aktuelles Bild warten.";
            return;
        }
        busy = true;
        SetControls();
        try { await StopLiveAsync(); }
        finally { busy = false; }
        if (closeRequested || capture == null) { FinishOperation(); return; }
        var dialog = new AutomaticRegistrationWindow(image, lastFrame.Value, freshCapture: () =>
        {
            if (capture == null || lastFrame == null || Preview.Source is not BitmapSource current || DateTimeOffset.UtcNow - lastFrame.Value >= TimeSpan.FromSeconds(2))
                throw new InvalidOperationException("Keine aktuelle Fensteraufnahme verfügbar.");
            return (current, lastFrame.Value);
        }) { Owner = this };
        calibrationWindow = dialog;
        SetControls();
        try
        {
            dialog.ShowDialog();
            await dialog.FinishAsync();
            if (dialog.LiveSelection is { } setup && capture != null && overlay != null && !closeRequested)
            {
                live = new LiveMapController(setup, view => { if (overlay != null) { overlay.LiveView = view; overlay.Update(); } });
                live.StatusChanged += text => LiveStatus.Text = text;
                LiveCheck.IsEnabled = true; LiveCheck.IsChecked = true;
                LiveStatus.Text = "Live-Zuordnung wird geprüft …";
                if (lastCapturedFrame != null) live.Offer(lastCapturedFrame);
            }
        }
        finally { calibrationWindow = null; SetControls(); if (closeRequested) QueueShutdown(); }
    }

    private async void LiveUnchecked(object sender, RoutedEventArgs e) => await StopLiveAsync();

    private async Task StopLiveAsync()
    {
        var oldLive = live; live = null;
        LiveCheck.IsChecked = false; LiveCheck.IsEnabled = false;
        if (overlay != null) { overlay.LiveView = null; overlay.Update(); }
        LiveStatus.Text = "Live-Zuordnung aus.";
        if (oldLive != null) liveFinishing = oldLive.DisposeAsync().AsTask();
        await liveFinishing;
    }

    private void AlignmentChanged(object sender, RoutedEventArgs e)
    {
        if (overlay == null) return;
        overlay.AlignmentRequested = AlignmentCheck.IsChecked == true;
        overlay.Update();
    }

    private void OnSessionEnded(string message)
    {
        if (Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            if (busy || capture == null) return;
            busy = true;
            SetControls();
            try { await StopSessionAsync(message); }
            finally { FinishOperation(); }
        }));
    }

    private async void StopClick(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        busy = true;
        SetControls();
        try { await StopSessionAsync("Aufnahme gestoppt."); }
        finally { FinishOperation(); }
    }

    private async Task StopSessionAsync(string message)
    {
        var oldCapture = capture;
        capture = null;
        var oldOverlay = overlay;
        overlay = null;
        if (oldOverlay != null) { oldOverlay.LiveView = null; oldOverlay.Window.SetNativeVisibility(false); }
        try
        {
            await StopLiveAsync();
            var oldDialog = calibrationWindow;
            oldDialog?.Close();
            if (oldDialog != null) await oldDialog.FinishAsync();
        }
        finally
        {
            try { oldOverlay?.Dispose(); }
            finally
            {
                if (oldCapture != null)
                {
                    oldCapture.Ended -= OnSessionEnded;
                    await oldCapture.DisposeAsync();
                }
            }
        }
        lastFrame = null;
        lastCapturedFrame = null;
        Preview.Source = null;
        EmptyPreview.Visibility = Visibility.Visible;
        FrameInfo.Text = "Keine aktive Aufnahme";
        StatusText.Text = message;
    }

    private void SetControls()
    {
        var available = !busy && !closeRequested;
        StartButton.IsEnabled = available && capture == null;
        StopButton.IsEnabled = available && capture != null;
        RefreshButton.IsEnabled = available && capture == null;
        WindowSelector.IsEnabled = available && capture == null;
        CalibrateButton.IsEnabled = available && capture != null && calibrationWindow == null && lastFrame != null &&
            DateTimeOffset.UtcNow - lastFrame.Value < TimeSpan.FromSeconds(2);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (closeReady) return;
        e.Cancel = true;
        closeRequested = true;
        freshnessTimer.Stop();
        SetControls();
        StatusText.Text = busy ? "Anwendung schließt nach der laufenden Aktion …" : "Anwendung wird geschlossen …";
        if (!busy) QueueShutdown();
    }

    private void FinishOperation()
    {
        busy = false;
        SetControls();
        if (closeRequested) QueueShutdown();
    }

    private void QueueShutdown()
    {
        if (shutdownQueued) return;
        shutdownQueued = true;
        // Leave the current Closing event before cleanup, even if there is nothing to await.
        Dispatcher.BeginInvoke(new Action(async () =>
        {
            var exitCode = 0;
            busy = true;
            try { await StopSessionAsync("Anwendung wird geschlossen."); }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceError($"Shutdown cleanup failed: {exception}");
                exitCode = 1;
            }
            finally
            {
                closeReady = true;
                Application.Current.Shutdown(exitCode);
            }
        }));
    }
}
