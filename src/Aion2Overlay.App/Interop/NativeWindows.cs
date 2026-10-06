using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Aion2Overlay.Core;

namespace Aion2Overlay.App.Interop;

internal static class NativeWindows
{
    public const int ExtendedStyle = -20;
    public const long Transparent = 0x20, ToolWindow = 0x80, Layered = 0x80000, NoActivate = 0x08000000;
    public static readonly nint Topmost = new(-1);
    public const uint NoActivation = 0x0010, ShowWindowFlag = 0x0040;
    public const int ShowNoActivate = 4, HideWindow = 0;

    public delegate bool EnumWindowCallback(nint window, nint parameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    internal struct Point { public int X, Y; }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumWindows(EnumWindowCallback callback, nint parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(nint window);
    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(nint window, StringBuilder text, int capacity);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint window, out Rect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint window, int index, nint value);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(nint window, int command);

    public static string Title(nint window)
    {
        var text = new StringBuilder(1024);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    public static PixelRect ClientBounds(nint window)
    {
        if (!GetClientRect(window, out var rect)) return default;
        var origin = new Point();
        if (!ClientToScreen(window, ref origin)) return default;
        return new(origin.X, origin.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    public static WindowSnapshot Snapshot(nint window) => new(
        ClientBounds(window), IsWindow(window), IsWindowVisible(window),
        IsIconic(window), GetForegroundWindow() == window);

    public static void ApplyOverlayStyles(nint window)
    {
        var flags = GetWindowLongPtr(window, ExtendedStyle).ToInt64();
        Marshal.SetLastPInvokeError(0);
        var previous = SetWindowLongPtr(window, ExtendedStyle,
            new nint(flags | Transparent | ToolWindow | Layered | NoActivate));
        if (previous == 0 && Marshal.GetLastPInvokeError() != 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }
}
