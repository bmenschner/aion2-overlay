using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Aion2Overlay.App.Interop;
using Aion2Overlay.Core;

namespace Aion2Overlay.App;

public partial class OverlayWindow : Window
{
    public nint Handle { get; private set; }

    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            Handle = new WindowInteropHelper(this).Handle;
            NativeWindows.ApplyOverlayStyles(Handle);
            HwndSource.FromHwnd(Handle).AddHook(WindowMessage);
        };
    }

    private static nint WindowMessage(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == 0x0084) // WM_NCHITTEST: no interactive regions in this prototype.
        {
            handled = true;
            return new nint(-1); // HTTRANSPARENT
        }
        if (message == 0x0021) // WM_MOUSEACTIVATE
        {
            handled = true;
            return new nint(3); // MA_NOACTIVATE
        }
        return 0;
    }

    public void Align(PixelRect bounds)
    {
        if (!bounds.HasArea) return;
        if (!NativeWindows.SetWindowPos(Handle, NativeWindows.Topmost,
            bounds.X, bounds.Y, bounds.Width, bounds.Height, NativeWindows.NoActivation))
            throw new Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
    }

    public void SetNativeVisibility(bool show) => NativeWindows.ShowWindow(Handle,
        show ? NativeWindows.ShowNoActivate : NativeWindows.HideWindow);
}
