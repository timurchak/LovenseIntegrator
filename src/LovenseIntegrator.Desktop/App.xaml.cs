using LovenseIntegrator.Desktop.Localization;
using System.Windows;
using System.IO;
using System.Text.Json;
using LovenseIntegrator.Desktop.Services;
using LovenseIntegrator.Desktop.Transports;

namespace LovenseIntegrator.Desktop;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        // A worker must never apply an update or relaunch as a visible application.
        var diagnostic = args.Any(a => a is "--ble-worker" or "--ble-scan" or "--ble-check" or "--ble-recovery-harness" or "--ui-harness" or "--smoke" or "--update-harness" or "--screen-probe");
        var hook = args.Any(a => a.StartsWith("--veloapp-", StringComparison.Ordinal));
        using var instance = !diagnostic && !hook ? new Mutex(false, @"Local\LovenseIntegrator.UI." + Environment.UserName) : null;
        var owned = false;
        try
        {
            if (instance is not null)
            {
                try { owned = instance.WaitOne(0); } catch (AbandonedMutexException) { owned = true; }
                if (!owned) { MessageBox.Show(L.T("Lovense Integrator is already running."), "Lovense Integrator"); return; }
            }
            Velopack.VelopackApp.Build().SetAutoApplyOnStartup(!diagnostic && !hook).Run();
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        finally { if (owned) instance!.ReleaseMutex(); }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--screen-probe"))
        {
            try { Shutdown(await ScreenProbe.RunAsync() ? 0 : 1); }
            catch (Exception ex) { Directory.CreateDirectory("artifacts/screen-probe"); File.WriteAllText("artifacts/screen-probe/failure.txt", ex.ToString()); Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--update-harness"))
        {
            try { await UpdateHarness.RunAsync(e.Args); Shutdown(0); }
            catch { Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--ble-worker"))
        {
            await BleWorkerHost.RunAsync(e.Args[Array.IndexOf(e.Args, "--ble-worker") + 1], e.Args.Contains("--fake-ble"));
            Shutdown(); return;
        }
        if (e.Args.Contains("--ble-recovery-harness"))
        {
            try { await BleRecoveryHarness.RunAsync(Path.GetFullPath("artifacts/ble-recovery")); Shutdown(0); }
            catch (Exception ex) { Directory.CreateDirectory("artifacts/ble-recovery"); File.WriteAllText("artifacts/ble-recovery/failure.txt", ex.ToString()); Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--ui-harness"))
        {
            var languageIndex = Array.IndexOf(e.Args, "--language");
            L.SetLanguage(languageIndex >= 0 ? e.Args[languageIndex + 1] : "en");
            var cultureIndex = Array.IndexOf(e.Args, "--culture");
            var culture = System.Globalization.CultureInfo.GetCultureInfo(cultureIndex >= 0 ? e.Args[cultureIndex + 1] : "en-US");
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            var directory = Path.GetFullPath(Path.Combine("artifacts", "ui-harness", L.Language, culture.Name));
            Directory.CreateDirectory(directory);
            ProfileStore.OverridePath = Path.Combine(directory, "profile.json");
            ProfileStore.Save(ProfileStore.Defaults());
            try
            {
                var window = new MainWindow { Language = System.Windows.Markup.XmlLanguage.GetLanguage(culture.Name) };
                await window.RunHarnessAsync(directory);
                Shutdown(0);
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "failure.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--ble-scan"))
        {
            Directory.CreateDirectory("artifacts");
            try
            {
                await using var ble = new IsolatedBleTransport();
                var messages = new System.Collections.Concurrent.ConcurrentQueue<string>();
                ble.Message += messages.Enqueue;
                var rounds = new List<object>();
                IReadOnlyList<LovenseIntegrator.Core.Toy> toys = [];
                for (var i = 0; i < (e.Args.Contains("--repeat-ble-scan") ? 3 : 1); i++)
                {
                    toys = await ble.DiscoverAsync(CancellationToken.None);
                    rounds.Add(new { Round = i + 1, Worker = ble.WorkerId, Toys = toys });
                }
                await ble.DisposeAsync();
                File.WriteAllText("artifacts/ble-scan.json", JsonSerializer.Serialize(new { Toys = toys, Rounds = rounds, Messages = messages.ToArray() }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
                Shutdown(0);
            }
            catch (Exception ex) { File.WriteAllText("artifacts/ble-scan.json", JsonSerializer.Serialize(new { Error = ex.ToString() })); Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--ble-check"))
        {
            Directory.CreateDirectory("artifacts");
            try
            {
                await using var ble = new BleTransport();
                var state = await ble.CheckAvailabilityAsync(CancellationToken.None);
                File.WriteAllText("artifacts/ble-check.txt", $"BLE status: {state} (0=enabled, 1=disabled, 2=unsupported). No scan or toy commands performed.");
                Shutdown(state == 0 ? 0 : 1);
            }
            catch (Exception ex) { File.WriteAllText("artifacts/ble-check.txt", ex.ToString()); Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--smoke"))
        {
            var directory = Path.GetFullPath("artifacts"); Directory.CreateDirectory(directory);
            ProfileStore.OverridePath = Path.Combine(directory, "smoke-profile.json");
            ProfileStore.Save(ProfileStore.Defaults(), ProfileStore.OverridePath);
            try
            {
                var window = new MainWindow();
                await window.RunSmokeAsync(directory);
                File.WriteAllText(Path.Combine(directory, "ui-smoke.txt"), "PASS: catalog, recipes, dynamic fields, seconds conversion, sliders, waveform, legacy profile preservation, validation, targeting, application refresh, key recording, observation, demo actions, normal/minimum layouts.");
                Shutdown(0);
            }
            catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "ui-smoke.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        L.SetLanguage(LanguagePreferences.Read(Path.Combine(Path.GetDirectoryName(ProfileStore.PathName)!, "settings.json")));
        MainWindow = new MainWindow(e.Args.Contains("--bluetooth") ? 1 : 0); MainWindow.Show();
    }
}
