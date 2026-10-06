using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Aion2Overlay.App.Interop;
using Aion2Overlay.Core;
using Aion2Overlay.App.Services;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

    public void DrawLive(LiveMapView? view, bool alignment)
    {
        var visibility = alignment ? Visibility.Visible : Visibility.Collapsed;
        AlignmentBorder.Visibility = AlignmentLabel.Visibility = AlignmentCross.Visibility = visibility;
        LiveCanvas.Children.Clear();
        if (view == null) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        var geometry = view.Geometry;
        var points = view.Registration.SupportReference.Select(view.Registration.PixelTransform.Map)
            .Select(p => new Point((p.X + geometry.WindowBounds.X - geometry.ClientBounds.X) / dpi.DpiScaleX,
                (p.Y + geometry.WindowBounds.Y - geometry.ClientBounds.Y) / dpi.DpiScaleY));
        LiveCanvas.Children.Add(new Polygon { Points = new PointCollection(points), Stroke = Brushes.LimeGreen, StrokeThickness = 2, Fill = Brushes.Transparent });
        if (!view.Registration.TryMap(view.Anchor, out var framePoint) || !geometry.TryClientDip(framePoint, dpi.DpiScaleX, dpi.DpiScaleY, out var dip)) return;
        var marker = new Ellipse { Width = 14, Height = 14, Stroke = Brushes.LimeGreen, StrokeThickness = 3, Fill = Brushes.Black };
        Canvas.SetLeft(marker, dip.X - 7); Canvas.SetTop(marker, dip.Y - 7); LiveCanvas.Children.Add(marker);
        var label = new TextBlock { Text = "TEST · kein Cube", Foreground = Brushes.LimeGreen, Background = Brushes.Black, Padding = new Thickness(4), FontSize = 12 };
        Canvas.SetLeft(label, dip.X + 12); Canvas.SetTop(label, dip.Y - 10); LiveCanvas.Children.Add(label);
    }
}
