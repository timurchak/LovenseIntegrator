using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Services;

public sealed class WindowsInput : IDisposable
{
    private readonly Native.Hook keyboard;
    private readonly Native.Hook mouse;
    private nint keyboardHandle, mouseHandle;
    public event Action<InputEvent>? Received;
    public WindowsInput() { keyboard = Keyboard; mouse = Mouse; }
    public void Start()
    {
        if (keyboardHandle != 0) return;
        keyboardHandle = Native.SetWindowsHookEx(13, keyboard, Native.GetModuleHandle(null), 0);
        mouseHandle = Native.SetWindowsHookEx(14, mouse, Native.GetModuleHandle(null), 0);
        if (keyboardHandle == 0 || mouseHandle == 0)
        {
            var error = Marshal.GetLastWin32Error(); Stop(); throw new Win32Exception(error);
        }
    }
    private nint Keyboard(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<Native.KeyboardData>(data);
            if ((info.Flags & 0x10) == 0 && message is 0x100 or 0x101 or 0x104 or 0x105)
            {
                var key = KeyInterop.KeyFromVirtualKey((int)info.Vk);
                // Windows low-level messages use generic Shift/Ctrl/Alt codes; expose left/right keys.
                if (info.Vk == 16) key = info.Scan == 0x36 ? Key.RightShift : Key.LeftShift;
                if (info.Vk == 17) key = (info.Flags & 1) != 0 ? Key.RightCtrl : Key.LeftCtrl;
                if (info.Vk == 18) key = (info.Flags & 1) != 0 ? Key.RightAlt : Key.LeftAlt;
                Received?.Invoke(new(message is 0x100 or 0x104 ? EventKind.KeyDown : EventKind.KeyUp,
                    Environment.TickCount64, key == Key.None ? $"VK_{info.Vk}" : key.ToString()));
            }
        }
        return Native.CallNextHookEx(0, code, message, data);
    }
    private nint Mouse(int code, nint message, nint data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<Native.MouseData>(data);
            if ((info.Flags & 1) == 0)
            {
                var mapped = (int)message switch
                {
                    0x201 => (EventKind.MouseDown, "Left"), 0x202 => (EventKind.MouseUp, "Left"),
                    0x204 => (EventKind.MouseDown, "Right"), 0x205 => (EventKind.MouseUp, "Right"),
                    0x207 => (EventKind.MouseDown, "Middle"), 0x208 => (EventKind.MouseUp, "Middle"),
                    0x20B => (EventKind.MouseDown, (info.Data >> 16) == 1 ? "X1" : "X2"),
                    0x20C => (EventKind.MouseUp, (info.Data >> 16) == 1 ? "X1" : "X2"),
                    0x20A => (EventKind.MouseWheel, (short)(info.Data >> 16) > 0 ? "Up" : "Down"),
                    _ => (EventKind.Timer, "")
                };
                if (mapped.Item2.Length > 0) Received?.Invoke(new(mapped.Item1, Environment.TickCount64, mapped.Item2, Value: (short)(info.Data >> 16)));
            }
        }
        return Native.CallNextHookEx(0, code, message, data);
    }
    public static string ForegroundProcess() => ForegroundContext(false).Process;
    public static (string Process, string Title) ForegroundContext(bool includeTitle)
    {
        var window = Native.GetForegroundWindow();
        Native.GetWindowThreadProcessId(window, out var id);
        var title = new StringBuilder(1024);
        if (includeTitle) Native.GetWindowText(window, title, title.Capacity);
        try { using var process = Process.GetProcessById((int)id); return (process.ProcessName, title.ToString()); }
        catch (ArgumentException) { return ("", ""); }
        catch (Win32Exception) { return ("", ""); }
        catch (InvalidOperationException) { return ("", ""); }
    }
    public void Stop()
    {
        if (keyboardHandle != 0) Native.UnhookWindowsHookEx(keyboardHandle);
        if (mouseHandle != 0) Native.UnhookWindowsHookEx(mouseHandle);
        keyboardHandle = mouseHandle = 0;
    }
    public void Dispose() => Stop();
    private static class Native
    {
        public delegate nint Hook(int code, nint message, nint data);
        [StructLayout(LayoutKind.Sequential)] public struct KeyboardData { public uint Vk, Scan, Flags, Time; public nuint Extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MouseData { public int X, Y; public uint Data, Flags, Time; public nuint Extra; }
        [DllImport("user32.dll", SetLastError = true)] public static extern nint SetWindowsHookEx(int id, Hook callback, nint module, uint thread);
        [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] public static extern bool UnhookWindowsHookEx(nint handle);
        [DllImport("user32.dll")] public static extern nint CallNextHookEx(nint handle, int code, nint message, nint data);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern nint GetModuleHandle(string? name);
        [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(nint window, StringBuilder text, int count);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint window, out uint id);
    }
}

public sealed class EmergencyHotkey : IDisposable
{
    private readonly HwndSource source;
    private readonly Action stop;
    private const int Id = 0x4C56;
    public EmergencyHotkey(nint handle, Action stop)
    {
        this.stop = stop; source = HwndSource.FromHwnd(handle)!;
        if (!RegisterHotKey(handle, Id, 0x4003, 0x7B)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Ctrl+Alt+F12 is already used by another application.");
        source.AddHook(WndProc);
    }
    private nint WndProc(nint hwnd, int msg, nint wparam, nint lparam, ref bool handled)
    {
        if (msg == 0x312 && wparam == Id) { handled = true; source.Dispatcher.BeginInvoke(stop, DispatcherPriority.Send); }
        return 0;
    }
    public void Dispose() { UnregisterHotKey(source.Handle, Id); source.RemoveHook(WndProc); }
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterHotKey(nint hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterHotKey(nint hwnd, int id);
}
