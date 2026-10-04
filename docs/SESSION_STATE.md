# Session state — 2026-10-04

## Goal and decisions

A Windows application for manual Lovense control and event-triggered actions. Initial models: Lush and Ferri (originally spelled “fierry” by the user), using a standard Bluetooth adapter. Stack: C# / .NET 10 / WPF, Windows input hooks, native BLE SDK and a separate rule engine.

The primary workflow is visually selecting keyboard/mouse inputs, assigning feedback to groups, excluding inputs and increasing intensity for selected windows. This is implemented. The original goal of full control is broader than the current prototype: vibration, software pulse, Stop and speed-based intensity exist, but not every feature of every toy model.

The project was translated to English and published to `git@github.com:timurchak/LovenseIntegrator.git` on `main` (initial commit `20a9da7`). UI text, defaults, examples, AI instructions and documentation are English, including all 600 catalog ideas and 30 recipes. JSON contracts and user-authored profile content are preserved.

The latest request is release **0.2.0** with a visible installer wizard and a language selector. The app now offers English/Russian in Settings, saves `settings.json` separately from profiles, and applies the language on the next normal launch. Existing/imported rule names and JSON/key identifiers remain unchanged. Embedded AI guides and low-level diagnostics remain English. See [LOCALIZATION.md](LOCALIZATION.md).

The Inno Setup 6.7.3 wizard wraps Velopack 1.2.161 and offers destination/Browse, review, extraction/installation progress and optional launch. Velopack still owns updates and uninstall. Upgrades keep the registered installation folder; changing it requires uninstall first, preserving profiles. Important finding: Inno `CreateAppDir=no` suppresses the directory page even if `DisableDirPage=no`; keep it `yes`. Portable Inno compiler setup avoids global associations/shortcuts. The originally planned 0.1.1 was not published; these changes are included in 0.2.0.

The self-contained x64 app uses a stable GitHub feed, automatic background checks/downloads and restart through the Updates tab. Package ID `LovenseIntegratorApp` deliberately differs from the profile directory. Worker/test processes cannot auto-apply updates. SDK redistribution is avoided: first Bluetooth use fetches the pinned DLL directly from Lovense and caches it. See [RELEASING.md](RELEASING.md) for tags, workflows, lifecycle and verification. Check actual GitHub Actions/releases for publication state rather than inferring it from this document.

0.2.0 local verification: 205 logic checks, WPF smoke and 9 BLE recovery checks passed. The expanded installer test installed into a custom path containing spaces, rejected an occupied folder, rejected a damaged update, updated successfully and reinstalled using the registered custom path. Real user profiles/settings were not used. All four UI combinations passed 3038 assertions each (English/Russian × en-US/ru-RU), **12152 total**. Russian settings, keyboard and mouse renders were visually reviewed at minimum size. Installer UI automation was interrupted by the user's Escape key in the preceding turns; no interactive installation of the production app was performed for 0.2.0.

Release work passed 192 offline logic checks, WPF smoke, 3031 UI assertions in each culture (6062 total) and 9 BLE worker recovery checks. A real installer test installed 0.0.1, rejected a damaged 0.0.2 package, then downloaded/applied the valid update and ran version 0.0.2. Test profiles are external to the install folder. No physical motor commands were used. The installer is unsigned; code signing remains an owner-provided dependency.

The English build passed 176 logic checks, WPF smoke, 3026 UI assertions in each of ru-RU/en-US (6052 total), and 9 BLE worker recovery checks. English keyboard, mouse and minimum-size editor renders were inspected. Smoke input now uses the field's numeric culture instead of a hardcoded decimal comma. Hardware behavior was not retested for this text/layout change.

## Completed features

- Manual controls, demo transport, direct Windows BLE and alternative Lovense Remote Local API.
- 21-event engine, graphical catalog, recipes, key/combination/sequence recording, application/title filters and observation without automatic commands.
- Keyboard mode: 104-key diagram, groups, exclusions, window-specific assignments, short effects, import/export and embedded AI instructions.
- Mouse mode: five buttons, Up/Down wheel directions, groups/exclusions, single effects and vibration renewal until wheel events stop. Import/export and AI instructions.
- Profile v3 with v1/v2/v3 reads; stable identities and explicit migration of old rules when saving assignments.
- Second-device crash fix: refresh retains a healthy BLE session; connections are sequential. The SDK runs in a separate worker; its failure leaves the UI alive and rules paused.

## Verification before the English migration

| Check | Recorded result |
| --- | --- |
| Release build | No warnings or errors |
| Console logic checks | 176 PASS |
| WPF smoke | PASS |
| UI harness ru-RU / en-US | 3026 + 3026 = 6052 assertions PASS |
| BLE recovery harness | 9 PASS; actual child process, demo devices |
| Physical Lush | User confirmed manual control and rules |
| Physical Lush + Ferri | Connected together; three consecutive searches in one worker retained both |
| Final shutdown fix | No new matching Application Error in the checked interval |
| Local self-contained build | Published/opened; both connected, rules paused, profile matched its backup |

Ferri reported 100% and Lush 86% at the end of that hardware check. These are historical readings. Crash diagnosis did not send positive vibration. Ferri motor operation and simultaneous physical vibration on both devices were not verified by that check.

One immediate launch of the published build found no devices; graceful close/reopen then found both. The cause is unknown. If repeated, collect scan times, callbacks and indicator state rather than claiming a Bluetooth cleanup delay as proven.

## Deferred work and limits

- Discord was cancelled. Neither Windows system notifications nor a visible server bot meet the user's requirements. No Discord connection, events or tokens are implemented.
- Screen-region capture/recognition is later: monitor/region selection, preview, color/template detection, then OCR, with debounce and detector tests. None is running now.
- No tray, autostart, AND/OR condition groups, multiple actions per rule or automatic BLE → REST fallback.
- No background synchronization of battery/connection state into the UI. Native disconnection is handled inside BLE; UI gets snapshots during discovery and a separate worker-failure notification.
- Physical haptic latency is unmeasured. 100/150 ms are software durations, not motor latency guarantees.
- Stop delivery cannot be guaranteed after radio loss or worker failure.

## Next session

Read actual Git status and verification artifacts, then continue the user's next request. Preserve current modes, profile and BLE lifecycle. For hardware work, read [BLUETOOTH.md](BLUETOOTH.md) and establish any missing device readiness from the current conversation. The idea catalog is a roadmap resource, not a request to implement all entries.
