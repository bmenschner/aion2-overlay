using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Aion2Overlay.App.Interop;
using Aion2Overlay.Core;
using System.Diagnostics;

namespace Aion2Overlay.App.Services;

public sealed record CapturedFrame(int Width, int Height, byte[] Pixels, DateTimeOffset CapturedAt,
    CaptureGeometry Geometry = default, long Sequence = 0);

public sealed class WindowCaptureService : IAsyncDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private GraphicsCaptureItem? item;
    private IDirect3DDevice? device;
    private Direct3D11CaptureFramePool? pool;
    private GraphicsCaptureSession? session;
    private Task? worker;
    private int ended;
    private nint targetWindow;
    public int ReceivedFrames { get; private set; }
    public string LastContentSize { get; private set; } = "none";

    // Awaiting the consumer bounds the queue to one frame, even when the UI is slow.
    public Func<CapturedFrame, Task>? FrameReady { get; set; }
    public event Action<string>? Ended;
    public static bool IsSupported => GraphicsCaptureSession.IsSupported();

    public void Start(nint window)
    {
        if (worker != null || cancellation.IsCancellationRequested)
            throw new InvalidOperationException("Für eine neue Aufnahme eine neue Sitzung anlegen.");
        if (!IsSupported) throw new NotSupportedException("Windows-Fensteraufnahme ist nicht verfügbar.");
        if (!NativeWindows.IsWindow(window)) throw new InvalidOperationException("Das ausgewählte Fenster wurde geschlossen.");
        targetWindow = window;
        item = CaptureInterop.CaptureItem(window);
        item.Closed += ItemClosed;
        device = CaptureInterop.CreateDevice();
        pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device,
            DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, item.Size);
        session = pool.CreateCaptureSession(item);
        session.IsCursorCaptureEnabled = false;
        session.StartCapture();
        worker = Task.Run(CaptureLoopAsync);
    }

    private void ItemClosed(GraphicsCaptureItem sender, object args)
    {
        cancellation.Cancel();
        NotifyEnded("Das Aufnahmefenster wurde geschlossen.");
    }

    private async Task CaptureLoopAsync()
    {
        var currentSize = item!.Size;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                CapturedFrame? output = null;
                var nextSize = currentSize;
                using (var frame = pool!.TryGetNextFrame())
                {
                    if (frame != null && frame.ContentSize.Width > 0 && frame.ContentSize.Height > 0)
                    {
                        ReceivedFrames++;
                        nextSize = frame.ContentSize;
                        LastContentSize = $"{nextSize.Width}x{nextSize.Height}";
                        if (nextSize.Width == currentSize.Width && nextSize.Height == currentSize.Height)
                        {
                            var before = NativeWindows.FrameBounds(targetWindow);
                            var clientBefore = NativeWindows.ClientBounds(targetWindow);
                            var qpcNow = TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
                            var time = DateTimeOffset.UtcNow + (frame.SystemRelativeTime - qpcNow);
                            using var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(
                                frame.Surface, BitmapAlphaMode.Ignore);
                            using var converted = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
                            var length = checked((uint)(converted.PixelWidth * converted.PixelHeight * 4));
                            var buffer = new Windows.Storage.Streams.Buffer(length);
                            converted.CopyToBuffer(buffer);
                            CryptographicBuffer.CopyToByteArray(buffer, out var pixels);
                            var after = NativeWindows.FrameBounds(targetWindow);
                            var clientAfter = NativeWindows.ClientBounds(targetWindow);
                            var geometry = before == after && clientBefore == clientAfter
                                ? new CaptureGeometry(new(converted.PixelWidth, converted.PixelHeight), after, clientAfter) : default;
                            output = new(converted.PixelWidth, converted.PixelHeight, pixels, time, geometry, ReceivedFrames);
                        }
                    }
                }

                // The old frame is disposed before the pool is resized.
                if (nextSize.Width != currentSize.Width || nextSize.Height != currentSize.Height)
                {
                    session!.Dispose();
                    pool.Dispose();
                    pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device!,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, nextSize);
                    // Restart requests a full frame even for a static window that stops repainting.
                    session = pool.CreateCaptureSession(item!);
                    session.IsCursorCaptureEnabled = false;
                    session.StartCapture();
                    currentSize = nextSize;
                }

                if (output != null && !cancellation.IsCancellationRequested && FrameReady != null)
                    await FrameReady(output);
                await Task.Delay(200, cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception)
        {
            NotifyEnded($"Aufnahme beendet: {exception.Message}");
        }
    }

    private void NotifyEnded(string message)
    {
        if (Interlocked.Exchange(ref ended, 1) == 0) Ended?.Invoke(message);
    }

    public async ValueTask DisposeAsync()
    {
        cancellation.Cancel();
        if (item != null) item.Closed -= ItemClosed;
        if (worker != null) await worker;
        session?.Dispose();
        pool?.Dispose();
        device?.Dispose();
        session = null;
        pool = null;
        device = null;
        item = null;
        // CancellationTokenSource stays valid for any already-dispatched Closed callback.
    }
}
