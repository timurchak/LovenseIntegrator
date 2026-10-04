using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace LovenseIntegrator.Desktop.Services;

// Release packages do not redistribute the vendor DLL. Fetch the pinned SDK directly from Lovense.
internal static class BleSdkInstaller
{
    internal const string FileName = "LovenseBLE_Lib.dll";
    internal const string Hash = "74424f9047078d0e02735dc9f7106897161e401de502d9f18597a3d9d3afa9cb";
    internal const string DownloadUrl = "https://developer.lovense.com/LovenseBLE_Lib.dll";
    internal static string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LovenseIntegrator", "native", Hash, FileName);
    internal static async Task<string> EnsureAsync(CancellationToken ct, Action<string>? message = null)
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, FileName);
        if (await IsValidAsync(bundled, ct)) return bundled;
        if (await IsValidAsync(CachePath, ct)) return CachePath;
        message?.Invoke("Downloading the Bluetooth SDK from Lovense (first connection only)…");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        ct = timeout.Token;
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        Directory.CreateDirectory(Path.GetDirectoryName(CachePath)!);
        var temporary = CachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                await using var input = await response.Content.ReadAsStreamAsync(ct);
                var buffer = new byte[81920];
                var total = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, ct)) > 0)
                {
                    total += count;
                    if (total > 32 * 1024 * 1024) throw new IOException("Bluetooth SDK download exceeds the expected size.");
                    await output.WriteAsync(buffer.AsMemory(0, count), ct);
                }
            }
            if (!await IsValidAsync(temporary, ct)) throw new IOException("Bluetooth SDK checksum mismatch. The vendor file may have changed; install a newer app release.");
            File.Move(temporary, CachePath, true);
            return CachePath;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static async Task<bool> IsValidAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path)) return false;
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct)).Equals(Hash, StringComparison.OrdinalIgnoreCase);
    }
}
