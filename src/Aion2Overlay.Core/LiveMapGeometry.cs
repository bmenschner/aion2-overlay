namespace Aion2Overlay.Core;

public readonly record struct CaptureGeometry(ImageSize FrameSize, PixelRect WindowBounds, PixelRect ClientBounds)
{
    public bool IsValid => FrameSize.IsValid && WindowBounds.HasArea && ClientBounds.HasArea &&
        FrameSize.Width == WindowBounds.Width && FrameSize.Height == WindowBounds.Height &&
        ClientBounds.X >= WindowBounds.X && ClientBounds.Y >= WindowBounds.Y &&
        ClientBounds.X + ClientBounds.Width <= WindowBounds.X + WindowBounds.Width &&
        ClientBounds.Y + ClientBounds.Height <= WindowBounds.Y + WindowBounds.Height;

    public bool Compatible(CaptureGeometry other) => IsValid && other.IsValid && FrameSize == other.FrameSize &&
        ClientBounds.Width == other.ClientBounds.Width && ClientBounds.Height == other.ClientBounds.Height &&
        ClientBounds.X - WindowBounds.X == other.ClientBounds.X - other.WindowBounds.X &&
        ClientBounds.Y - WindowBounds.Y == other.ClientBounds.Y - other.WindowBounds.Y;

    public bool TryClientDip(MapPoint framePixel, double dpiX, double dpiY, out MapPoint dip)
    {
        dip = default;
        if (!IsValid || !framePixel.IsFinite || !double.IsFinite(dpiX) || !double.IsFinite(dpiY) || dpiX <= 0 || dpiY <= 0) return false;
        var x = framePixel.X + WindowBounds.X - ClientBounds.X;
        var y = framePixel.Y + WindowBounds.Y - ClientBounds.Y;
        if (x < 0 || y < 0 || x >= ClientBounds.Width || y >= ClientBounds.Height) return false;
        dip = new(x / dpiX, y / dpiY);
        return true;
    }
}

public sealed record LiveFrameSignature(byte[] Samples)
{
    public static LiveFrameSignature Create(RegistrationImage frame)
    {
        frame.Validate();
        var samples = new byte[96 * 36];
        for (var gy = 0; gy < 36; gy++) for (var gx = 0; gx < 96; gx++)
        {
            var x = Math.Clamp((int)((0.13 + (gx + 0.5) / 96 * 0.83) * frame.Size.Width), 0, frame.Size.Width - 1);
            var y = Math.Clamp((int)((0.10 + (gy + 0.5) / 36 * 0.82) * frame.Size.Height), 0, frame.Size.Height - 1);
            var i = (y * frame.Size.Width + x) * 4;
            samples[gy * 96 + gx] = (byte)((frame.Bgra[i] + 2 * frame.Bgra[i + 1] + frame.Bgra[i + 2]) / 4);
        }
        return new(samples);
    }
    public bool SameView(LiveFrameSignature other)
    {
        if (Samples.Length != other.Samples.Length || Samples.Length == 0) return false;
        var changed = 0; long total = 0;
        for (var i = 0; i < Samples.Length; i++) { var delta = Math.Abs(Samples[i] - other.Samples[i]); total += delta; if (delta > 16) changed++; }
        return total / (double)Samples.Length <= 3 && changed / (double)Samples.Length <= 0.04;
    }
}

public static class LiveMapPolicy
{
    public static bool Fresh(DateTimeOffset frameAt, DateTimeOffset now) => frameAt <= now && now - frameAt < TimeSpan.FromSeconds(2);
    public static bool CanApply(int epoch, int currentEpoch, DateTimeOffset frameAt, DateTimeOffset now,
        CaptureGeometry fitGeometry, CaptureGeometry latestGeometry, LiveFrameSignature fitSignature, LiveFrameSignature latestSignature) =>
        epoch == currentEpoch && Fresh(frameAt, now) && fitGeometry.Compatible(latestGeometry) && fitSignature.SameView(latestSignature);
}
