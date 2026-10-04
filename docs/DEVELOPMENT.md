# Development and verification

Run these commands from the repository root in PowerShell. A portable SDK is available at `.tools/dotnet/dotnet.exe`; scripts prefer it and fall back to `dotnet` on PATH. Setup-Sdk pins .NET SDK 10.0.401 and its archive SHA-512. Verify the source before changing either.

## Build

```powershell
# Only when the dependency is missing:
./scripts/Setup-Sdk.ps1
./scripts/Setup-Ble.ps1

# Release build and console checks:
./scripts/Build.ps1

# Also publish a self-contained Windows x64 build:
./scripts/Build.ps1 -Publish
```

The vendor DLL is downloaded to `native/LovenseBLE_Lib.dll` and copied during build/publish when present. Setup-Ble checks SHA-256; do not silently bypass a mismatch after a vendor update. `.tools/`, `native/*.dll`, `bin/`, `obj/`, `dist/` and `artifacts/` are ignored by Git.

The executable is `dist/win-x64/LovenseIntegrator.exe`; it does not require an installed .NET runtime. Edit source, not published output. Root AI instructions are embedded resources and examples are copied beside the executable.

For an installable build use `./scripts/Package.ps1`, then `./scripts/Test-Installer.ps1 -ReleaseDirectory dist/releases/0.1.0` (substitute the current version). See [RELEASING.md](RELEASING.md). Plain development builds do not check for app updates.

## Verification matrix

| Change | Run | Artifact / scope |
| --- | --- | --- |
| RuleEngine, transport logic, formats | `./scripts/Build.ps1` | Console checks; previous baseline: 176 PASS |
| WPF bindings, editor, layout | `./scripts/Test-Desktop.ps1` | `artifacts/ui-smoke.txt`, desktop PNGs |
| Modes, import, catalog, cultures, sizes | `./scripts/Test-UiHarness.ps1` | `artifacts/ui-harness/{ru-RU,en-US}/report.json` and PNGs |
| Worker lifecycle, reconnect, switching | `./scripts/Test-BleRecovery.ps1` | `artifacts/ble-recovery/report.json` |
| Physical BLE scan/connect/refresh | Separate hardware check below | Requires available devices; demo does not prove radio behavior |
| Documentation only | Check links, paths and source contracts | No application/motor restart needed |

The UI harness covers all 441 transitions among 21 event types, dynamic fields, recipes, stable IDs, keyboard capture, import, numeric input and visual keyboard/mouse modes. It renders normal/minimum windows and 100/125/150/200% raster scales. It uses real WPF controls, bindings and routed events offscreen, with demo transport and separate profiles. It does not reproduce global OS input, monitor DPI switching or motor behavior. Both English and Russian interfaces are checked under ru-RU and en-US numeric cultures; reports are in `artifacts/ui-harness/<language>/<culture>/`. See [LOCALIZATION.md](LOCALIZATION.md).

BLE recovery harness starts an actual child process using DemoTransport. It checks two devices, refresh without worker replacement, selection retention, command IPC, forced worker exit, live UI/paused rules, profile preservation, reconnect and transport switching. The baseline is 9 checks. Positive commands in this harness affect demo devices only.

A stale `failure.txt` may remain after an earlier failure: inspect timestamps and the latest exit code. Do not present an old PASS as a new result. Once relevant checks pass, do not repeat the entire suite without a new change or concern.

## Publishing while the application is open

1. Identify the UI by its window/command line. The worker uses the same EXE with `--ble-worker`; two processes are expected.
2. Back up the real profile to a distinct `artifacts/profile-before-<change>.json`; preserve useful earlier backups.
3. Close the UI gracefully and wait for it and its worker to exit. Do not use a blanket `Stop-Process -Name LovenseIntegrator`.
4. Publish. For already tested source, a direct `dotnet publish` is sufficient.
5. Open the new EXE in the intended mode; verify paused rules, connection and profile preservation. Do not activate vibration just to test startup.

```powershell
& ./.tools/dotnet/dotnet.exe publish src/LovenseIntegrator.Desktop -c Release -r win-x64 --self-contained true -o dist/win-x64 --nologo
Start-Process -FilePath "$PWD\dist\win-x64\LovenseIntegrator.exe" -WorkingDirectory $PWD -ArgumentList '--bluetooth','--connection-status','--mouse'
```

## Diagnostic arguments

| Arguments | Behavior |
| --- | --- |
| None | Visible window, demo transport, rules paused |
| `--bluetooth` | BLE window; VM creation starts discovery, rules stay paused |
| `--mouse` | Opens the mouse tab |
| `--connection-status` | Writes VM snapshots to `artifacts/connection-status.json` on Status/Busy changes |
| `--ble-scan` | Real discovery/connection, battery query, Stop and worker shutdown; writes `artifacts/ble-scan.json` |
| `--ble-scan --repeat-ble-scan` | Three searches in one worker; Rounds records its PID and both connections |
| `--ble-check` | Legacy direct availability diagnostic; uses BleTransport and Dispose/_Quit, not the main recovery test |
| `--ble-recovery-harness` | Demo IPC/recovery checks |
| `--smoke` | WPF smoke with a separate profile |
| `--ui-harness --culture ru-RU` | UI harness under the selected numeric culture |
| `--ble-worker <pipe>` | Internal mode; do not launch manually without a parent. `--fake-ble` is for the harness |

Run scan from the root using the built DLL and portable dotnet, with the main application's BLE session closed. Do not run independent SDK owners against the same devices simultaneously.

```powershell
& ./.tools/dotnet/dotnet.exe src/LovenseIntegrator.Desktop/bin/Release/net10.0-windows/LovenseIntegrator.dll --ble-scan --repeat-ble-scan
```

Scan does not start the motor, but it connects to physical devices and sends Stop. Reports include real Toy.Id values; do not publish them as examples. Determine device readiness and hardware-testing authorization from the current session.

## Git publication

The user selected `git@github.com:timurchak/LovenseIntegrator.git` and subsequently requested installers, GitHub releases and auto-updates. Inspect staged files before committing; local profiles, reports, dumps, dependencies and vendor/build binaries must stay excluded. Use ordinary pushes and verify the remote commit afterward. Release packages exclude the vendor SDK binary. Pushing a version tag starts the release workflow; see [RELEASING.md](RELEASING.md).
