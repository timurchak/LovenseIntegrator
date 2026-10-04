using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LovenseIntegrator.Core;

namespace LovenseIntegrator.Desktop.Services;

// Explicit diagnostic: bounded live capture only, no VM, profile, transport or input injection.
internal static class ScreenProbe
{
    public static async Task<bool> RunAsync()
    {
        var tracker = new ScreenPacketTracker();
        var events = new List<object>();
        var valid = 0; var captured = 0; var live = false; var restricted = false; string status = "";
        byte[]? latest = null;
        var end = Environment.TickCount64 + 10000;
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (Environment.TickCount64 < end && await timer.WaitForNextTickAsync())
        {
            var capture = await Task.Run(() => ScreenCapture.Read(0, 0, 4));
            status = capture.Status;
            ScreenPacket? packet = null;
            if (capture.Pixels is { } pixels)
            {
                captured++; latest = pixels; packet = ScreenProtocol.DecodeBgra(pixels, 4);
                if (packet is not null) { valid++; restricted |= packet.Restricted; }
            }
            var received = tracker.Accept(packet, Environment.TickCount64); live |= tracker.Healthy;
            if (received is not null) events.Add(new { Event = ScreenEvents.Find(received.EventCode)!.Id, received.IsTest, received.Value });
        }
        var directory = Path.GetFullPath("artifacts/screen-probe"); Directory.CreateDirectory(directory);
        if (latest is not null)
        {
            var bitmap = BitmapSource.Create(32, 32, 96, 96, PixelFormats.Bgr32, null, latest, 128);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, "block.png")); png.Save(stream);
        }
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new { Live = live, ValidFrames = valid, CapturedFrames = captured, Restricted = restricted, Events = events, Status = status }, new JsonSerializerOptions { WriteIndented = true }));
        return live;
    }
}
