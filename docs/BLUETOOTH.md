# Bluetooth findings and diagnostics

## Adapter and SDK

The user's machine exposes Generic Bluetooth Radio and Microsoft Bluetooth LE Enumerator. There is no official Lovense dongle. Windows BLE SDK reported availability code 0, and Lush and Ferri connected simultaneously using this standard adapter. This verifies the tested configuration, not every Bluetooth adapter.

Research sources: [official Windows BLE SDK and C# demo](https://developer.lovense.com/docs/game-engine-plugins/windows_ble), with a local demo copy at `.tools/lovense-demo/LovenseBLETools.cs`. Callback ABI was checked against the C# demo because the web summary omits some arguments. The application is x64. BleTransport stores delegates in fields; keep them rooted while the SDK can call back. SDK symbols identify families (`s` = Lush, `x` = Ferri), not device generations.

The tested binary is pinned by [Setup-Ble.ps1](../scripts/Setup-Ble.ps1). Do not substitute an unverified DLL or remove the hash check to make a build pass. Terms for public redistribution of the vendor DLL have not been independently verified.

## Incident: connecting two devices

On 2026-10-04 the user reported a crash while connecting two devices. Application Error and dump analysis identified an SDK fault; they did not establish a one-device adapter limit.

1. UI crash at 16:19:22 local time: `LovenseBLE_Lib.dll`, exception `0xc000027b`, offset `0x2f7f7`; WER also reported `80000013`.
2. The dump stack showed `RaiseFailFastException` → WinRT/vendor DLL → `LovenseBLE_Lib!Quit+0x24e`.
3. The old DiscoverAsync always performed Stop → Dispose (`_Quit`) → new BleTransport. Refresh therefore unloaded the SDK while a device was active.
4. The first isolation attempt called `Environment.Exit(0)` after Stop. Real worker shutdown still produced access violations `0xc0000005` in ntdll/coreclr at 16:29:21 and 16:33:30. The shutdown association was reproduced; the precise teardown/callback race was not proven by the unsymbolized stack.
5. The final worker sends Stop, then calls `TerminateProcess` on **itself** with exit code 0, bypassing DLL/CLR teardown. Repeat scans and shutdown after this change produced no new matching Application Error events in the checked interval.

Do not restore `_Quit` or `Environment.Exit` to the working path as a supposedly cleaner Dispose without physical validation. Vendor guidance to call Quit does not negate the locally reproduced fail-fast. The legacy `--ble-check` still uses direct Dispose and is not a production lifecycle example.

## Current implementation

- MainViewModel retains a healthy IsolatedBleTransport during refresh and restores selected Toy by Id.
- The worker is the same EXE with `--ble-worker`, without a visible window. ProcessStartInfo starts it; when hosted by dotnet, the parent also passes the assembly path.
- IPC uses JSON lines through a randomly named pipe with `CurrentUserOnly`; requests are serialized. Startup timeout is 8 s, discovery 60 s, ordinary commands 4 s.
- Native discovery: Stop, scan for 4 s, stop scanning and await its callback for up to 3 s. Connect each disconnected toy sequentially, awaiting confirmation for up to 6 s; await battery for up to 1 s. Connection callbacks no longer launch battery requests.
- Connected and Battery use ConcurrentDictionary.AddOrUpdate so each update preserves the other field. A timeout must not mark a device connected.
- A worker crash, EOF or IPC failure marks the transport unhealthy. Through Dispatcher, the UI pauses rules, invalidates device statuses and offers reconnect. Saved profiles remain intact. Fail must mark the transport unhealthy before waking pending requests.
- Refresh after a failure creates a new worker. Do not send Stop to an already failed worker before attempting recovery.
- Normal shutdown sends shutdown/Stop and awaits exit. The parent limits the wait and terminates only its own hung worker if necessary.

Direct BleTransport remains for the worker, fake tests and legacy availability diagnostics. Process isolation cannot guarantee physical Stop delivery after a radio/SDK failure. An IPC reply confirms handler completion, not measured motor movement.

## Reproduction and evidence

For a hardware check, release the connection from Lovense Remote/the phone, turn on the toys and run one scan or three refreshes in one process. A connection test must not start positive vibration. Normal refresh sends Stop and pauses rules.

| Artifact | Contents |
| --- | --- |
| `artifacts/ble-crash-stack.txt` | Original Quit fail-fast stack |
| `artifacts/ble-worker-exit-stack.txt` | Later worker-shutdown crash analysis |
| `artifacts/ble-scan.json` | Latest scan; Rounds contains Worker and Toy snapshots |
| `artifacts/connection-status.json` | Latest diagnostic UI snapshot, not a live API |
| `artifacts/ble-recovery/report.json` | Demo recovery harness report |
| `%LOCALAPPDATA%\LovenseIntegrator\bluetooth-errors.log` | Worker failures: PID, time and message |
| `%LOCALAPPDATA%\CrashDumps` | Local dumps when Windows dump collection is configured |

Analysis used the installed `C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe` with `.ecxr; k 18; q`. Symbol-server loading stalled once; local analysis without symbols still yielded useful evidence. cdb is not a build dependency. Do not publish dumps: they can contain user process memory.

For another crash, match event 1000's exact time and PID to the UI/worker. A zero exit code from the parent scan does not prove the child avoided a crash: Windows Application log exposed the first shutdown implementation's fault. Check report freshness and event times to avoid mistaking historical failures for new ones.

If discovery returns nothing, check occupied connections, power and callbacks, then retry. One empty search immediately after worker exit was observed; reopening later found both devices. Its cause remains unknown. Do not redesign the transport or remove model support based on that single result.

## REST alternative

LocalApiTransport calls Lovense Remote, not an HTTP interface on the toy. The saved PC endpoint is `https://127-0-0-1.lovense.club:30010/command`. GetToys, Function and Pattern are supported; HTTP timeout is 3 s and TLS validation remains enabled. HTTPS is accepted, or HTTP only on loopback; URI userinfo is rejected.

The current implementation rejects ordinary effects of ≤1 s and any `RenewalId`; short haptic feedback and continuous wheel renewal require BLE. Never turn a short effect into an unlimited REST command. Automatic fallback, cloud authorization and phone endpoint discovery are not implemented. Research source: [Standard API](https://developer.lovense.com/docs/standard-solutions/standard-api). Recheck current vendor documentation before changing the contract.
