using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LovenseIntegrator.Desktop.Services;

public sealed record ScreenCaptureResult(byte[]? Pixels, string Status, string Process = "", string Title = "");

// Capture only the configured physical-pixel rectangle in the foreground WoW client.
// No full-desktop buffers, disk images, input injection or process memory access.
public static class ScreenCapture
{
    public static ScreenCaptureResult Read(int x, int y, int cellSize)
    {
        var previousDpi = SetThreadDpiAwarenessContext(-4); // PER_MONITOR_AWARE_V2, restored for pooled threads.
        try { return ReadPixels(x, y, cellSize); }
        finally { if (previousDpi != 0) SetThreadDpiAwarenessContext(previousDpi); }
    }
    private static ScreenCaptureResult ReadPixels(int x, int y, int cellSize)
    {
        var window = GetForegroundWindow();
        GetWindowThreadProcessId(window, out var pid);
        string name;
        try { using var process = Process.GetProcessById((int)pid); name = process.ProcessName; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { return new(null, "Waiting for World of Warcraft in the foreground."); }
        if (!name.Equals("Wow", StringComparison.OrdinalIgnoreCase) || IsIconic(window)) return new(null, "Waiting for World of Warcraft in the foreground.");
        var side = cellSize * 8;
        if (cellSize is < 1 or > 16 || x < 0 || y < 0 || !GetClientRect(window, out var rect) || x > rect.Right - side || y > rect.Bottom - side)
            return new(null, "The Screen region is outside the game window.");
        var point = new Point { X = x, Y = y };
        if (!ClientToScreen(window, ref point)) return new(null, "Screen capture failed.");
        var title = new System.Text.StringBuilder(512); GetWindowText(window, title, title.Capacity);
        var source = GetDC(0);
        if (source == 0) return new(null, "Screen capture failed.");
        nint destination = 0, bitmap = 0, old = 0;
        try
        {
            destination = CreateCompatibleDC(source);
            var info = new BitmapInfo { Size = 40, Width = side, Height = -side, Planes = 1, BitCount = 32 };
            bitmap = CreateDIBSection(source, ref info, 0, out var bits, 0, 0);
            if (destination == 0 || bitmap == 0 || bits == 0) return new(null, "Screen capture failed.");
            old = SelectObject(destination, bitmap);
            if (old == 0 || old == -1 || !BitBlt(destination, 0, 0, side, side, source, point.X, point.Y, 0x40CC0020)) return new(null, "Screen capture failed.");
            if (!GdiFlush()) return new(null, "Screen capture failed.");
            if (GetForegroundWindow() != window) return new(null, "Waiting for World of Warcraft in the foreground.");
            var bytes = new byte[side * side * 4]; Marshal.Copy(bits, bytes, 0, bytes.Length);
            return new(bytes, "", name, title.ToString());
        }
        finally
        {
            if (old != 0 && old != -1) SelectObject(destination, old);
            if (bitmap != 0) DeleteObject(bitmap);
            if (destination != 0) DeleteDC(destination);
            ReleaseDC(0, source);
        }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, ImageSize; public int XPels, YPels; public uint Colors, Important;
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref Point point);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, System.Text.StringBuilder text, int max);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint obj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint target, int x, int y, int width, int height, nint source, int sx, int sy, uint operation);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
}
