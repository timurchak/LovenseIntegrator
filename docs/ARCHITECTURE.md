# Architecture

Installation and updates use Velopack, bootstrapped in `App.Main` before WPF. `UpdateService` owns bindable update state/preferences; `GithubUpdateBackend` talks to stable GitHub Releases. Normal startup applies pending updates, while workers and diagnostics opt out. `MainWindow.OnClosing` stops/disposes transports before a requested update restart. `BleSdkInstaller` obtains and verifies the vendor SDK before launching a real BLE worker. See [RELEASING.md](RELEASING.md) for packaging and lifecycle details.

## Projects and files

| Area | Main source files | Responsibility |
| --- | --- | --- |
| Core | [Rules.cs](../src/LovenseIntegrator.Core/Rules.cs), [Devices.cs](../src/LovenseIntegrator.Core/Devices.cs) | Rule, RuleEngine, InputEvent, ToyCommand, IToyTransport; no WPF dependency |
| Input normalization | [KeyRecording.cs](../src/LovenseIntegrator.Core/KeyRecording.cs), [MouseInputs.cs](../src/LovenseIntegrator.Core/MouseInputs.cs) | Key recording and supported mouse inputs |
| Windows | [WindowsInput.cs](../src/LovenseIntegrator.Desktop/Services/WindowsInput.cs) | Global keyboard/mouse hooks, foreground process/window, emergency hotkey |
| UI coordination | [MainViewModel.cs](../src/LovenseIntegrator.Desktop/ViewModels/MainViewModel.cs) | Profile, rules, pause, transport, dispatch, log, generation/gate |
| Visual modes | [KeyboardModeViewModel.cs](../src/LovenseIntegrator.Desktop/ViewModels/KeyboardModeViewModel.cs), KeyboardModeView, MouseModeView | Shared `InputModeViewModel`, layout and assignments; the original filename remains |
| Shared editor | Controls/AssignmentEditorView.xaml(.cs), IAssignmentEditorContext, RuleEditor, EffectPreview | All categories share effect editing, independent effect reuse, preview and fixed actions; modes inject trigger content |
| Other events | MainWindow, EventPickerWindow, RulePresentation | Event catalog, trigger fields and recipes |
| Shared styles | Controls/Theme.xaml | Application brushes and control styles, merged by the editor resource dictionary |
| Screen | ScreenProtocol, ScreenCapture, ScreenModeViewModel, ScreenModeView, ScreenPresetStore | WoW color packets, bounded pixel capture, liveness, semantic event assignments and mode import |
| Profiles | ProfileStore, KeyboardPresetStore, MousePresetStore | Full profile, strict import bundles, merge by Id |
| BLE IPC | [IsolatedBleTransport.cs](../src/LovenseIntegrator.Desktop/Transports/IsolatedBleTransport.cs) | Parent transport and BleWorkerHost in one file; named pipe |
| Native BLE | [BleTransport.cs](../src/LovenseIntegrator.Desktop/Transports/BleTransport.cs) | SDK callbacks, scan, connections, battery, commands/timers |
| REST / offline | LocalApiTransport, DemoTransport, OfflineTransport | Lovense Remote API, simulated devices, disconnected state |
| Tests | tests/LovenseIntegrator.Tests/Program.cs, MainWindow.UiHarness.cs, BleRecoveryHarness.cs | Logic, WPF integration/rendering, worker recovery |

`Directory.Build.props` enables nullable and implicit usings and treats warnings as errors. Desktop targets `net10.0-windows`, `WinExe`, WPF and x64. Core and console tests target `net10.0`. Console tests link selected transport/store sources with `Compile Link`, without referencing the entire WPF project.

## Event flow

1. WindowsInput captures a physical event with monotonic `Environment.TickCount64` time. Injected input is ignored; normal system input processing is not blocked.
2. MainViewModel dispatches it to the WPF Dispatcher and checks the generation. Pause/Stop invalidate old queued events.
3. RuleEngine updates press/hold history and returns the selected RuleMatch. All engine calls are serialized on Dispatcher; it is not a general-purpose thread-safe service.
4. RuleMatch creates a ToyCommand. Observation logs automatic matches without dispatching; manual controls and test buttons remain explicit commands.
5. MainViewModel uses transportGate and a sending flag. New effects during dispatch are skipped; generation is checked again after acquiring the gate. Stop must not leave a stale command queued.
6. BLE: UI → JSON lines over a named pipe → hidden worker → BleTransport → SDK. REST sends HTTP requests through HttpClient.

Engine history resets on pause, connection and import. Hooks run only while rules are enabled. Typed text is neither reconstructed nor stored; foreground window/process information is used for filtering. The UI log retains the last 100 messages.

## Boundaries and state

- IToyTransport exposes DiscoverAsync, SendAsync and DisposeAsync. UI controls and event providers must not call the SDK directly.
- A normal launch selects DemoTransport unless `--bluetooth` is passed. The selected transport is not restored from the profile; ApiUrl is persisted.
- Switching transports closes the previous instance. Refresh retains a healthy instance; REST is recreated when its URL changes, BLE when its worker fails.
- `Toy.Id` and `Rule.Id` define identity. List indices and device names are not stable binding keys.
- UI/worker messages contain Toy snapshots and commands. There is currently no live Toy-state stream to the UI for every native callback.
- Duration, pulse sequencing and wheel renewal run in BleTransport. Local stop timers must not depend on WPF rendering.

New event sources should normalize data for the engine and common executor, not bypass them to control motors directly. Define external-event data/filter contracts and profile compatibility first; do not store unrelated service IDs in key or process fields.

Screen capture runs on a background periodic task; decoder/tracker output returns to Dispatcher through a revision gate. Preview continues while rules are paused, but enabling rules baselines the current signal. Dedicated `ScreenEventId` fields identify semantic events. Screen inputs cannot disturb keyboard hold latches or satisfy keyboard/time rules. Signal loss after a live session stops active rules; recovery never enables them. See [SCREEN.md](SCREEN.md).
