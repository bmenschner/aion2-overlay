using Aion2Overlay.App.Interop;

namespace Aion2Overlay.App.Services;

public sealed record WindowTarget(nint Handle, uint ProcessId, string Title)
{
    public string Label => $"{Title}  ·  PID {ProcessId}";
}

public static class WindowCatalog
{
    public static IReadOnlyList<WindowTarget> List()
    {
        var result = new List<WindowTarget>();
        NativeWindows.EnumWindows((handle, _) =>
        {
            if (!NativeWindows.IsWindowVisible(handle) || NativeWindows.IsIconic(handle)) return true;
            NativeWindows.GetWindowThreadProcessId(handle, out var pid);
            if (pid == Environment.ProcessId) return true;
            var title = NativeWindows.Title(handle);
            if (!string.IsNullOrWhiteSpace(title) && NativeWindows.ClientBounds(handle).HasArea)
                result.Add(new(handle, pid, title));
            return true;
        }, 0);
        return result.OrderByDescending(x => x.Title.Contains("AION", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Title).ToList();
    }
}
