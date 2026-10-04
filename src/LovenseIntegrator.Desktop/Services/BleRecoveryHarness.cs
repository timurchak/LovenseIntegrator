using System.IO;
using System.Text.Json;
using LovenseIntegrator.Desktop.Transports;
using LovenseIntegrator.Desktop.ViewModels;

namespace LovenseIntegrator.Desktop.Services;

internal static class BleRecoveryHarness
{
    public static async Task RunAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        ProfileStore.OverridePath = Path.Combine(directory, "profile.json"); ProfileStore.Save(ProfileStore.Defaults());
        var checks = new List<string>();
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks.Add(name); }
        var workers = new List<IsolatedBleTransport>();
        await using var vm = new MainViewModel(1, (index, _) =>
        {
            if (index != 1) return new DemoTransport();
            var worker = new IsolatedBleTransport(true); workers.Add(worker); return worker;
        });
        var until = Environment.TickCount64 + 10000;
        while (vm.Busy && Environment.TickCount64 < until) await Task.Delay(20);
        Check(!vm.Busy && vm.Toys.Count == 2 && vm.Toys.All(t => t.Connected), "Two devices returned through isolated worker");
        var first = workers[0].WorkerId;
        vm.SelectedToy = vm.Toys.Last(); var selected = vm.SelectedToy.Id;
        await vm.DiscoverAsync(); await vm.DiscoverAsync();
        Check(workers.Count == 1 && workers[0].WorkerId == first, "Repeated refresh reuses worker without native shutdown");
        Check(vm.SelectedToy?.Id == selected, "Refresh preserves selected toy");
        await vm.ManualAsync(false); // The worker uses DemoTransport; no DLL or physical devices.
        Check(vm.Status.Contains("Manual control"), "Commands cross the pipe and return an acknowledgement");
        var profileBeforeCrash = File.ReadAllText(ProfileStore.PathName);
        var crashObserved = false;
        try { await workers[0].CrashForHarnessAsync(); } catch (IOException) { crashObserved = true; }
        await System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        Check(crashObserved && !workers[0].CanReuse && !vm.Running && vm.Toys.All(t => !t.Connected), "Worker crash leaves UI alive, pauses and invalidates device statuses");
        Check(File.ReadAllText(ProfileStore.PathName) == profileBeforeCrash, "Crash preserves saved profile");
        await vm.DiscoverAsync();
        Check(workers.Count == 2 && workers[1].WorkerId != first && vm.Toys.Count == 2 && vm.Toys.All(t => t.Connected), "Reconnect replaces only failed worker and restores two devices");
        vm.TransportIndex = 0; await vm.DiscoverAsync();
        Check(vm.ConnectionName == "Demo mode", "Leaving Bluetooth gracefully closes isolated worker");
        vm.TransportIndex = 1; await vm.DiscoverAsync();
        Check(workers.Count == 3 && vm.Toys.Count == 2 && !vm.Running, "Returning to Bluetooth creates one fresh worker and remains paused");
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new { Result = "PASS", Checks = checks }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
