namespace Aion2Overlay.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public bool HasArea => Width > 0 && Height > 0;
}

public readonly record struct WindowSnapshot(
    PixelRect ClientBounds, bool Exists, bool Visible, bool Minimized, bool Foreground);

public static class OverlayPolicy
{
    public static bool ShouldDisplay(bool captureActive, bool alignmentRequested, WindowSnapshot target) =>
        captureActive && alignmentRequested && target.Exists && target.Visible &&
        !target.Minimized && target.Foreground && target.ClientBounds.HasArea;
}
