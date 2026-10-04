# Lovense Integrator: instructions for future sessions

This Windows desktop application controls Lovense toys through configurable event rules. Keep the existing C# / .NET 10 / WPF stack. Source code, UI, documentation and AI instructions are in English. Follow the user's language when replying in chat.

## Start here

1. Read [docs/README.md](docs/README.md) and [docs/SESSION_STATE.md](docs/SESSION_STATE.md).
2. Before changing transports, read [docs/BLUETOOTH.md](docs/BLUETOOTH.md). Before changing rules or imports, read [docs/RULES_AND_PROFILES.md](docs/RULES_AND_PROFILES.md).
3. Commands and verification guidance are in [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md); the code map is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
4. Compare historical results with current code and fresh reports. Previously recorded battery levels, PIDs and connections are not live state.

The user authorized initializing Git and publishing to `git@github.com:timurchak/LovenseIntegrator.git` on 2026-10-04. Inspect actual Git status and remotes before making assumptions. The parent `C:\projects\AGENTS.md` applies to WoW addons and explicitly excludes unrelated projects.

## User decisions

- Windows first; initial devices are Lush and Ferri, using a standard Bluetooth adapter without the official dongle.
- Prioritize clear visual keyboard/mouse modes, multiple/all input selection, exclusions, window-specific effects and responsive feedback.
- Preserve per-mode import/export and embedded AI instructions.
- Screen-region recognition is a later stage and is not implemented.
- Discord was cancelled: Windows system notifications and a visible server bot do not suit the user. Do not resume that integration without a new request.
- `TRIGGER_IDEAS.md` is an idea bank, not a list of implemented features or authorization to implement everything.

## Required invariants

- Preserve `%LOCALAPPDATA%\LovenseIntegrator\profile.json`. Tests use separate profiles under `artifacts/` through `ProfileStore.OverridePath`. Never replace the user's profile with demo defaults or translate their own saved names automatically.
- Rules stay paused at startup, connection refresh, import and transport failure. Do not resume physical actions automatically after recovery.
- Discovery/connection must not start vibration. Diagrams and effect previews do not send commands. Honor hardware-testing authorization already given in the session, but do not expand a connection test into motor activation.
- Preserve Stop, `Ctrl+Alt+F12`, cancellation of previous timers and invalidation of queued commands. Do not accumulate stale effects.
- Normal UI uses `IsolatedBleTransport`; native `BleTransport` runs in a hidden worker. Do not restore `_Quit` to refresh/shutdown of a working BLE session: a native crash was confirmed. Do not replace worker termination with `Environment.Exit` without physical revalidation: that also produced shutdown crashes.
- Refresh preserves a healthy BLE session. New connections are sequential and await callbacks. A second hidden application process is expected, not an extra window.
- Preserve profile versions 1/2/3, stable `Rule.Id`, time units and priority semantics. Do not confuse full profiles with keyboard/mouse import bundles.
- Native callbacks and BLE timers run outside the UI thread: update WPF state through Dispatcher and keep delegates alive throughout SDK use.
- Do not log typed text. Do not publish dumps, real profiles or Bluetooth device IDs in examples.
- Close the user's app through its window or `CloseMainWindow`, then await exit. Do not kill all processes by name: the UI and worker share an EXE. Terminating its own disposable worker is part of the transport implementation.

## Workflow

- Edit source files, not `dist/`, `bin/` or `obj/`. `TreatWarningsAsErrors` is enabled.
- Select checks by change: logic uses `Build.ps1`, UI uses smoke/UI harness, BLE lifecycle uses `Test-BleRecovery.ps1`. Keep physical checks separate from demo tests.
- Before publishing a local build, check for a running EXE, back up the profile and close the window gracefully. Keep rules paused when reopening for the user.
- Do not move root `KEYBOARD_AI_RULES.md`, `MOUSE_AI_RULES.md` or `examples/` without updating `.csproj`: they are embedded/copied into the build.
- Update affected docs, AI instructions and examples alongside changes. Record dates, verified behavior, limitations and next steps in `SESSION_STATE.md`; label hypotheses as such.
- Documentation-only changes need link and source consistency checks, not a device restart or the full UI harness.
- Before a push, inspect staged files. Keep profiles, reports, crash dumps, SDK binaries, local dependencies and build outputs out of Git. Do not force-push or rewrite remote history.
