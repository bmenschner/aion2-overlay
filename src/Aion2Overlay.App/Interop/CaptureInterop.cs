using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;

namespace Aion2Overlay.App.Interop;

internal static class CaptureInterop
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateForWindow(nint factory, nint window, ref Guid iid, out nint item);

    [DllImport("combase.dll", CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string text, int length, out nint value);
    [DllImport("combase.dll")]
    private static extern int WindowsDeleteString(nint value);
    [DllImport("combase.dll")]
    private static extern int RoGetActivationFactory(nint name, ref Guid iid, out nint factory);
    [DllImport("d3d11.dll")]
    private static extern int D3D11CreateDevice(nint adapter, int driver, nint software,
        uint flags, nint levels, uint levelCount, uint sdkVersion, out nint device,
        out int featureLevel, out nint context);
    [DllImport("d3d11.dll")]
    private static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgi, out nint device);

    public static GraphicsCaptureItem CaptureItem(nint window)
    {
        const string runtimeClass = "Windows.Graphics.Capture.GraphicsCaptureItem";
        var factoryId = new Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
        var itemId = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        nint name = 0, factory = 0, item = 0;
        try
        {
            Marshal.ThrowExceptionForHR(WindowsCreateString(runtimeClass, runtimeClass.Length, out name));
            Marshal.ThrowExceptionForHR(RoGetActivationFactory(name, ref factoryId, out factory));
            // IUnknown has three entries; CreateForWindow is the first interop method.
            var table = Marshal.ReadIntPtr(factory);
            var create = Marshal.GetDelegateForFunctionPointer<CreateForWindow>(
                Marshal.ReadIntPtr(table, 3 * IntPtr.Size));
            Marshal.ThrowExceptionForHR(create(factory, window, ref itemId, out item));
            return WinRT.MarshalInspectable<GraphicsCaptureItem>.FromAbi(item);
        }
        finally
        {
            if (item != 0) Marshal.Release(item);
            if (factory != 0) Marshal.Release(factory);
            if (name != 0) WindowsDeleteString(name);
        }
    }

    public static IDirect3DDevice CreateDevice()
    {
        nint nativeDevice = 0, context = 0, dxgi = 0, projected = 0;
        try
        {
            var hr = D3D11CreateDevice(0, 1, 0, 0x20, 0, 0, 7,
                out nativeDevice, out _, out context);
            if (hr < 0)
            {
                if (nativeDevice != 0) Marshal.Release(nativeDevice);
                if (context != 0) Marshal.Release(context);
                nativeDevice = context = 0;
                hr = D3D11CreateDevice(0, 5, 0, 0x20, 0, 0, 7,
                    out nativeDevice, out _, out context);
            }
            Marshal.ThrowExceptionForHR(hr);
            var dxgiId = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(nativeDevice, in dxgiId, out dxgi));
            Marshal.ThrowExceptionForHR(CreateDirect3D11DeviceFromDXGIDevice(dxgi, out projected));
            return WinRT.MarshalInterface<IDirect3DDevice>.FromAbi(projected);
        }
        finally
        {
            if (projected != 0) Marshal.Release(projected);
            if (dxgi != 0) Marshal.Release(dxgi);
            if (context != 0) Marshal.Release(context);
            if (nativeDevice != 0) Marshal.Release(nativeDevice);
        }
    }
}
