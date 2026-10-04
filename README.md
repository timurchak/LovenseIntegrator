# Lovense Integrator

A Windows desktop prototype for controlling Lovense toys and triggering effects from keyboard, mouse and other local events. Built with **C# / .NET 10 and WPF**, with a separate rule engine. Initial device targets: **Lush and Ferri**.

For development and future sessions, start with [AGENTS.md](AGENTS.md) and [project documentation](docs/README.md): architecture, build/test commands, rule formats and Bluetooth crash findings.

## Run

Download **LovenseIntegratorApp-win-Setup.exe** from [GitHub Releases](https://github.com/timurchak/LovenseIntegrator/releases/latest) and run it. The wizard lets you choose a folder, review the changes, then follow installation progress. Windows 10/11 x64 is required. Installation is per user, includes .NET and adds Start menu/desktop shortcuts. Launching the app from the final page is optional. The current installer is unsigned; Windows may show a SmartScreen prompt.

The **Updates** tab checks stable GitHub releases, downloads updates in the background and offers **Restart to update**. Automatic checks run 10 seconds after startup and every six hours; they can be disabled. Downloaded updates apply on the next launch. Restart stops effects and disconnects devices first; rules stay paused afterward. Profiles remain in `%LOCALAPPDATA%\LovenseIntegrator`, separate from the installation folder (default `%LOCALAPPDATA%\LovenseIntegratorApp`). To move an existing installation, uninstall it through Windows Settings first, then run the wizard and choose the new folder. Uninstall preserves profiles.

The self-contained local build is `dist/win-x64/LovenseIntegrator.exe`; no installed .NET runtime is required. A normal launch starts in demo mode with rules paused. Choose **English** or **Русский** in **Settings → App language**, click **Save language**, then restart when convenient. English is the default. Rule names, physical key codes and JSON contracts remain unchanged. AI instructions and low-level diagnostic messages remain in English.

1. Select **Bluetooth (direct connection)**.
2. Turn on the toy and release its connection from your phone or another application.
3. Click **Connect / refresh**. The first Bluetooth connection downloads the verified SDK directly from Lovense and requires internet access. Later connections use the cached file. Scanning takes about four seconds, followed by connection. Refresh stops current effects and pauses rules.
4. Select a connected toy. Use manual controls to test a short effect at a low intensity when ready.
5. Configure and save rules, then click **Enable rules**.

**STOP ALL** and global **Ctrl+Alt+F12** send Stop to all toys and pause rules. Closing the app also sends Stop. If the hotkey is occupied, the app reports it and the button remains available.

The official USB dongle was not needed in the tested setup: Lush and Ferri connected simultaneously through a standard Windows BLE adapter on 2026-10-04. The user confirmed manual control and rules on Lush. Ferri connection/battery reporting is verified; physical vibration still needs a separate check. This does not guarantee compatibility with every adapter.

Refresh preserves a healthy BLE session and connects new devices sequentially. The vendor DLL runs in a hidden worker process. If that process fails, the window stays open, rules pause and devices are marked disconnected; refresh creates a new session. Diagnostics are written to `%LOCALAPPDATA%\LovenseIntegrator\bluetooth-errors.log`. [Bluetooth findings](docs/BLUETOOTH.md) explain the confirmed native Quit crash and the shutdown workaround.

An alternative transport is **Lovense Remote (Local API)**. The documented PC endpoint used here is `https://127-0-0-1.lovense.club:30010/command`. For a phone, supply its reachable HTTPS endpoint on the same LAN. This is the API of Lovense Remote, not the toy itself. TLS validation stays enabled. Cloud authorization, automatic phone endpoint discovery and automatic BLE-to-REST fallback are not implemented.

## Available features

- Manual vibration, intensity 0–20, duration 0.1–300 seconds over BLE, pulse and Stop. REST effects must last more than one second.
- Official `LovenseBLE_Lib.dll` discovery, connection and battery queries; automatic download and SHA-256 verification of the pinned SDK.
- Local API GetToys, Function and Pattern, with endpoint/device selection and error handling.
- Demo Lush/Ferri without physical commands.
- Visual **Keyboard** and **Mouse** modes with grouped selections, exclusions, presets, per-application/title conditions, import/export and embedded AI instructions.
- **Other events** editor: when → where → action, searchable catalog, key recording, effect cards, sliders, graph and a readable rule description.
- 21 event types and seven recipes: holds, double presses, typing rhythm, no-correction streak, activity resumed, interval timer and keyboard feedback. Recipes create drafts that must be saved.
- **Observe without actions** logs automatic matches. Manual controls and **Test action** still send explicit commands.
- JSON profiles at `%LOCALAPPDATA%\LovenseIntegrator\profile.json`, import/export and version validation.
- Last 100 trigger/error messages in memory. Typed text is not stored. Global hooks run only while rules are enabled, do not block input and ignore injected input.

## Keyboard assignments

Select keys by clicking without Ctrl, or use All keys, Typing keys, Letters, WASD, Arrows or NumPad. After All keys, clicking a key makes it an exclusion. In ordinary selection, clicking again deselects it. Purple means selected, pink means excluded, and a dot marks an enabled saved assignment.

Choose **Feedback · 150 ms**, **Typewriter · 120 ms** or **Pulse**, then adjust intensity, duration, cooldown and toy. This mode uses milliseconds. Settings sit below the keyboard; scroll down at smaller window sizes. Optionally choose an application and a case-insensitive window-title substring. Save the assignment, then enable rules. Diagrams and previews do not send commands; **Test effect** does.

Example: general feedback on all keys except Esc/Ctrl, plus stronger WASD feedback in a game. Matching assignments compare Priority, application specificity, title specificity and smaller key groups, then stable Id. During the winner's cooldown, presses are skipped without falling back to a weaker assignment.

Assignments override ordinary key-down rules for covered keys. Holds, combinations and other events remain separate. Main Enter and NumPad Enter share one code. The diagram uses Windows key codes, not reconstructed text.

Compatible old key-down rules can be opened without changing behavior; saving converts them to the new mode with the same Id. Other rules remain in Other events. Full profiles v1/v2/v3 load; saving uses v3.

## Mouse assignments

The diagram has left/right buttons, wheel click, X1/X2 and separate Up/Down wheel zones. **Whole mouse**, **Five buttons** and **Wheel ↑ ↓** select groups; clicking again deselects or adds an exclusion when the whole mouse is selected.

- **Click · 100 ms**: one bounded effect per button press or wheel event, with zero cooldown. New events replace the previous effect; rapid impulses can merge.
- **While scrolling**: matching wheel events renew vibration. Default stop timeout is 150 ms without events, adjustable from 100–1000 ms. Cooldown does not block wheel renewal; selected buttons still produce a single effect.
- **Pulse**: a regular bounded effect. Disable continuous wheel vibration to use ordinary vibration, pulse or Stop on wheel events.

Create separate assignments for different Up/Down effects. Groups, exclusions, application/title filters, toy selection and priority behave as in keyboard mode. Old button/wheel rules retain their input scope when converted.

Live feedback requires **direct Bluetooth**. Repeated wheel events at the same intensity renew a local timer without resending the positive BLE level. Stop, another effect and disconnection cancel the old timer. If the window stops matching, renewal stops and the last timer ends the effect. Windows wheel messages may contain more than one physical notch. Timing depends on radio and motor behavior: 100/150 ms are software settings, not measured hardware latency.

## Mode import/export and AI instructions

Each mode has Import, Export and AI instructions. Export includes all assignments or one selected saved assignment; unsaved draft changes are excluded. Bundles contain assignments only, without connection settings or unrelated events.

Import adds new IDs and updates matching IDs of the same mode. Missing assignments and other events remain untouched; importing twice is idempotent. Unknown/duplicate fields, invalid codes/ranges or an Id conflict with a different event type reject the entire import. Rules pause after import. The previous profile is copied to `profile.json.before-keyboard-import.bak` or `profile.json.before-mouse-import.bak`, replacing that mode's previous backup.

Give the embedded instructions to an AI with your desired behavior; include an existing export when editing so its Id is preserved. Standalone references and ready-to-import examples:

- [Keyboard AI instructions](KEYBOARD_AI_RULES.md), [keyboard example](examples/keyboard-assignments.json): `LovenseIntegrator.Keyboard` v1.
- [Mouse AI instructions](MOUSE_AI_RULES.md), [mouse example](examples/mouse-assignments.json): `LovenseIntegrator.Mouse` v1.

Examples also ship beside the standalone executable. Full-profile import/export remains in Other events and uses a different format.

## Other events and rule behavior

| Event | Keys | Threshold | Window |
| --- | --- | --- | --- |
| Key press / release | Key code, or empty for any | Unused | Unused |
| Key held | Key or empty | Hold duration, ms | Unused |
| Double / triple press | Key or empty | First-to-last interval, ms | At least threshold |
| Combination | `LeftCtrl+Space` | Unused | Unused |
| Sequence | `A,B,C` | Unused | Whole sequence, ms |
| Press count | Key or empty | Physical press count | Measurement window, ms |
| Typing speed above / below | Unused | Presses per second | Sliding window, ms |
| No input | Unused | Idle duration, ms | Unused |
| Mouse button press / release | Left, Right, Middle, X1, X2 or empty | Unused | Unused |
| Mouse wheel | Up, Down or empty | Unused | Unused |
| Application changed | Unused | Unused | Unused |
| Timer | Unused | Interval, ms | Unused |
| Release after hold | Key or empty | Minimum hold, ms | Unused |
| Typing without corrections | Unused | Consecutive typing presses | Unused |
| Activity resumed | Unused | Minimum break, ms | Unused |
| Mouse double click | Button or empty | Click interval, ms | At least threshold |
| Mouse held | Button or empty | Minimum hold, ms | Unused |

This table describes JSON units; the ordinary editor converts relevant timing fields to seconds. **Record** captures a key; for a combination, press together and release all keys (at least two). For a sequence, press in order then Enter, up to 12 keys. Esc cancels; changing focus/rule cancels too. Advanced fields accept WPF key names with distinct left/right modifiers, case-insensitively.

Holds/combinations trigger once until release. Windows autorepeat does not count toward typing speed. Speed rules trigger on threshold crossing; below waits for a full initial window. **Intensity from typing speed** computes `speed / threshold × intensity`, capped at 20, once per trigger rather than continuously. No input includes keyboard, buttons and wheel, not mouse movement.

No-correction streaks count letters, digits, typing keys, Space, Tab and Enter; modifiers/arrows do not count. Backspace/Delete reset the streak. Release-after-hold uses the released key's own duration. Double/triple presses are tracked per key/button. The keyboard feedback recipe uses intensity 5/20, 150 ms and 200 ms cooldown (up to five triggers/s). The former two-second minimum was a prototype restriction, not a BLE SDK requirement.

The highest-priority matching rule wins; ties use stable Id. New commands replace the previous toy effect. Events arriving during a send are skipped rather than queued. Stop invalidates pending commands; pause, connection and import reset input history.

## Limits and later stages

This is a prototype. Current physical actions target vibration on Lush/Ferri; other functions/models need a capability table and hardware verification. UI battery/connection snapshots update on discovery/refresh, not by background polling. No tray, autostart, multi-action rules, nested AND/OR conditions or automatic transport fallback yet.

BLE sends levels without durations; the app runs stop timers and pulses. Stop delivery cannot be guaranteed after worker failure or radio loss. REST receives a bounded duration. Better acknowledgment handling, live connection-state synchronization and coordinated recovery remain future work.

Screen recognition is a separate later stage: select a monitor region, preview, color/template detection, then OCR with stability/confidence thresholds and repeat suppression. Screen capture is not running. Other potential sources include pointer movement, richer window/process events, gamepad, audio, MIDI, schedules, files, local webhooks and device events. [TRIGGER_IDEAS.md](TRIGGER_IDEAS.md) catalogs possibilities, not implemented integrations.

Discord was cancelled because Windows notifications and a visible server bot do not suit the user. [Research findings](DISCORD_INTEGRATION.md) remain for reference; no Discord connection or events are implemented.

## Development

```powershell
./scripts/Setup-Sdk.ps1       # Portable SDK if needed
./scripts/Setup-Ble.ps1       # Official DLL with SHA-256 verification
./scripts/Build.ps1 -Publish  # Build, logic checks, self-contained x64 output
./scripts/Test-Desktop.ps1    # WPF smoke and rendering
./scripts/Test-UiHarness.ps1  # ru-RU / en-US input cultures, editor lifecycle
./scripts/Test-BleRecovery.ps1 # Worker crash/reconnect on demo devices
```

The solution contains Core, Desktop and console Tests without third-party test frameworks. The English build passed 176 logic checks, WPF smoke, 6052 UI assertions and 9 worker recovery checks. See [development guidance](docs/DEVELOPMENT.md) and [session state](docs/SESSION_STATE.md) for scope and current results. Offscreen WPF tests do not replace global input, real monitor DPI or hardware tests.

The SDK binary is excluded from Git and release packages. Installed apps obtain it directly from the vendor. See [Releases and updates](docs/RELEASING.md) for packaging, CI, version tags, upgrade tests and signing limitations.

References: [Lovense Windows BLE SDK](https://developer.lovense.com/docs/game-engine-plugins/windows_ble), [Lovense Standard API](https://developer.lovense.com/docs/standard-solutions/standard-api), [WPF on .NET 10](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net100).
