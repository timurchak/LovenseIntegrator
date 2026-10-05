# Session state — 2026-10-05

## User-facing README — 2026-10-05

Reworked the root README as a download-first product overview: direct Windows installer link, real interface screenshots, concise feature descriptions, four-step setup and essential compatibility/Stop notes. Preserved the previous detailed documentation in [USER_GUIDE.md](USER_GUIDE.md), repaired its relative links and removed stale test counts. The documentation index links both entry points.

The existing offscreen UI harness now emits four additional curated demo captures (`readme-*.png`), committed as `docs/images/{keyboard,mouse,screen,effect}.png`. It uses its isolated demo profile and restores its preceding in-memory profile after capture. Normal UI, transports, rules and packaging behavior are unchanged. Screenshot refresh instructions are in DEVELOPMENT.md.

Verification: fresh Desktop Release build passed with zero warnings/errors; English/en-US UI harness passed 3364 assertions on 2026-10-05. All four screenshot assets were visually inspected. The direct latest-installer URL returned HTTP 200. Local documentation link and Git whitespace checks passed. Only demo devices were used; no physical connection/motor testing or local application publication was performed. Next step: refresh these images when the interface changes, using the documented harness command.

## Goal and decisions

The user requested release 0.4.0 on 2026-10-04 after reviewing the interface refactor. Local release verification passed 245 logic checks, 9 demo worker recovery checks and the install/upgrade harness (custom folder, occupied-folder rejection, corrupt-update rejection, update, registered-folder reuse and profile preservation). The installer/package excludes the vendor DLL. The unchanged interface source already passed WPF smoke and 13456 UI assertions in this session; the release workflow repeats the complete matrix. Release v0.4.0 is published from commit 441bd36c4fefb41585c3f8785807ff3bfef6d793. GitHub Windows release run 37226531338 passed every stage, including the complete UI matrix and installer upgrade checks; Windows checks run 37226529522 also passed. The stable release is marked latest, the Setup URL returned HTTP 200, and releases.win.json contains 0.4.0 with a package size matching the uploaded full package. Publication was verified on 2026-10-04. Release: https://github.com/timurchak/LovenseIntegrator/releases/tag/v0.4.0.

The 2026-10-04 interface refactor is implemented in 0.4.0. Keyboard, Mouse, Screen and Other events share `Controls/AssignmentEditorView` through `IAssignmentEditorContext`: common name/enabled fields, trigger slot, action cards, exact duration/cooldown inputs, target, conditions, preview and fixed Save/Test/Delete footer. Each category retains its own trigger selection and import contract. The permanent connection sidebar is removed; Devices and Manual control are separate tabs with synchronized selected-device state. Stop remains available across tabs.

Copy saved effect reuses Action, Intensity, DurationSeconds, PulseMs and ToyId across categories while preserving the destination trigger, identity, name, filters, cooldown, priority and wheel renewal. Copies are independent drafts, not linked library objects; unsupported speed/wheel combinations are excluded. Profiles remain v3 with v1/v2/v3 reads. No engine, transport lifecycle, SDK, installer or addon changes were needed.

Fresh refactor verification: Release build passed **245 logic checks**, no warnings/errors; WPF smoke passed. Final English/Russian × en-US/ru-RU matrix passed **3364 checks per combination, 13456 total**, including 40 shared-editor/reuse/navigation checks per run. The four final harness processes used separate profiles/artifact directories concurrently. English/Russian minimum-size renders were inspected, including common controls, Screen and separate device/manual tabs. Local documentation links, localization keys and Git whitespace checks passed. Existing import examples and embedded AI guides passed the harness; JSON contracts are unchanged. Tests used demo devices only; no physical motor commands or BLE lifecycle revalidation were performed.

The self-contained local build at `dist/win-x64/LovenseIntegrator.exe` was republished and opened in normal demo mode with rules paused. No user app was running before publication. The real profile was backed up under ignored artifacts. Existing startup discovery persistence added the default ScreenEventId field to the four older saved rules. PowerShell comparison confirmed the same four IDs, names and every pre-existing parameter; after confirming these were the only changes, the original backup bytes were restored and SHA-256 matched. Future explicit saves/refreshes may serialize that default field again. Release preparation for 0.4.0 followed at the user's request. The original profile was backed up again and its bytes remained unchanged throughout release packaging and isolated installer tests.

The user requested an application-only **0.3.0** release on 2026-10-04. The addon remains separate and optional: do not modify, commit or package it as part of this release. The interface refactor requested after that release is recorded above; it is not part of the original 0.3.0 release artifacts. The user confirmed combat-entry events appear in Recent game events and that New assignment creates a draft which enters the list after Save assignment; no functional fix was needed for those reports.

0.3.0 release preparation: a fresh Release build passed all 245 logic checks with no warnings/errors; the self-contained installer/package was built successfully. Installer verification passed custom-folder installation, occupied-folder rejection, corrupt-update rejection, successful update, registered-folder reuse and external-profile preservation; the vendor DLL is excluded. These checks used an isolated test package and no physical toys. The release tag triggers the complete Windows CI verification and publication pipeline; inspect GitHub for the final publication state.

The 2026-10-04 Screen request is implemented locally: a dedicated Screen mode plus an independent `C:\projects\WowScreenEvents` Retail addon. User priority is combat/damage events, with semantic event names rather than raw color codes. The user confirmed a 53% WoW UI scale; physical cell sizing now uses parent UI height / physical screen height and recalculates on scale/display changes. Default grid: 8×8 cells, each 4 physical pixels. Current protocol, supported events and limitations are in [SCREEN.md](SCREEN.md).

Fresh Screen verification: 245 logic checks passed, WPF smoke and 9 demo BLE recovery checks passed, and all four language/numeric-culture UI combinations passed 3324 checks each (13296 total). The addon passed 11 Lua 5.1 runtime tests plus TOC/syntax/icon checks. Live Retail 12.1.0 build 69933/interface 120100 at 3840×2160 and UI scale 0.533333 was tested through wowctl and the C# bounded reader: 200/200 valid heartbeat frames, followed by a test-signal run with 198/200 valid frames and one decoded preview-only `wow.test`. The two rejected frames did not produce events. The final self-contained EXE probe passed with 200/200 valid frames and one preview-only test event. Current-session BugGrabber showed zero WowScreenEvents errors; the TGA was rendered and visually checked in game, then its temporary probe was hidden.

The local client accepts UNIT_COMBAT registration but reports UnitHealth("player") as secret even in the observed noncombat context. Health loss/low-health events therefore cannot be claimed available there. Real damage/healing notifications and an actual encounter have not been physically exercised. Lua mock tests cover readable/secret data and ghost/resurrection semantics; this is not evidence of all live combat contexts. No positive physical toy commands were sent. General template matching, OCR, outgoing damage and secret-value extraction are not implemented.

A Windows application for manual Lovense control and event-triggered actions. Initial models: Lush and Ferri (originally spelled “fierry” by the user), using a standard Bluetooth adapter. Stack: C# / .NET 10 / WPF, Windows input hooks, native BLE SDK and a separate rule engine.

The primary workflow is visually selecting keyboard/mouse inputs, assigning feedback to groups, excluding inputs and increasing intensity for selected windows. This is implemented. The original goal of full control is broader than the current prototype: vibration, software pulse, Stop and speed-based intensity exist, but not every feature of every toy model.

The project was translated to English and published to `git@github.com:timurchak/LovenseIntegrator.git` on `main` (initial commit `20a9da7`). UI text, defaults, examples, AI instructions and documentation are English, including all 600 catalog ideas and 30 recipes. JSON contracts and user-authored profile content are preserved.

The previous release request was **0.2.0** with a visible installer wizard and a language selector. The app now offers English/Russian in Settings, saves `settings.json` separately from profiles, and applies the language on the next normal launch. Existing/imported rule names and JSON/key identifiers remain unchanged. Embedded AI guides and low-level diagnostics remain English. See [LOCALIZATION.md](LOCALIZATION.md).

The Inno Setup 6.7.3 wizard wraps Velopack 1.2.161 and offers destination/Browse, review, extraction/installation progress and optional launch. Velopack still owns updates and uninstall. Upgrades keep the registered installation folder; changing it requires uninstall first, preserving profiles. Important finding: Inno `CreateAppDir=no` suppresses the directory page even if `DisableDirPage=no`; keep it `yes`. Portable Inno compiler setup avoids global associations/shortcuts. The originally planned 0.1.1 was not published; these changes are included in 0.2.0.

The self-contained x64 app uses a stable GitHub feed, automatic background checks/downloads and restart through the Updates tab. Package ID `LovenseIntegratorApp` deliberately differs from the profile directory. Worker/test processes cannot auto-apply updates. SDK redistribution is avoided: first Bluetooth use fetches the pinned DLL directly from Lovense and caches it. See [RELEASING.md](RELEASING.md) for tags, workflows, lifecycle and verification. Check actual GitHub Actions/releases for publication state rather than inferring it from this document.

0.2.0 local verification: 205 logic checks, WPF smoke and 9 BLE recovery checks passed. The expanded installer test installed into a custom path containing spaces, rejected an occupied folder, rejected a damaged update, updated successfully and reinstalled using the registered custom path. Real user profiles/settings were not used. All four UI combinations passed 3038 assertions each (English/Russian × en-US/ru-RU), **12152 total**. Russian settings, keyboard and mouse renders were visually reviewed at minimum size. Installer UI automation was interrupted by the user's Escape key in the preceding turns; no interactive installation of the production app was performed for 0.2.0.

Release work passed 192 offline logic checks, WPF smoke, 3031 UI assertions in each culture (6062 total) and 9 BLE worker recovery checks. A real installer test installed 0.0.1, rejected a damaged 0.0.2 package, then downloaded/applied the valid update and ran version 0.0.2. Test profiles are external to the install folder. No physical motor commands were used. The installer is unsigned; code signing remains an owner-provided dependency.

The English build passed 176 logic checks, WPF smoke, 3026 UI assertions in each of ru-RU/en-US (6052 total), and 9 BLE worker recovery checks. English keyboard, mouse and minimum-size editor renders were inspected. Smoke input now uses the field's numeric culture instead of a hardcoded decimal comma. Hardware behavior was not retested for this text/layout change.

## Completed features

- Manual controls, demo transport, direct Windows BLE and alternative Lovense Remote Local API.
- 22-event engine (including ScreenEvent with 13 assignable WoW semantic events), graphical catalog, recipes, key/combination/sequence recording, application/title filters and observation without automatic commands.
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
- Screen now implements explicit foreground-WoW region capture, pixel preview, validated color packets and stable event assignments. Arbitrary monitor/region selection, template detection and OCR remain deferred. Capture starts only when requested and never auto-resumes physical actions.
- No tray, autostart, AND/OR condition groups, multiple actions per rule or automatic BLE → REST fallback.
- No background synchronization of battery/connection state into the UI. Native disconnection is handled inside BLE; UI gets snapshots during discovery and a separate worker-failure notification.
- Physical haptic latency is unmeasured. 100/150 ms are software durations, not motor latency guarantees.
- Stop delivery cannot be guaranteed after radio loss or worker failure.

Previous local delivery: `dist/win-x64/LovenseIntegrator.exe` was rebuilt with Screen support before release preparation. The real profile was backed up under ignored artifacts and its bytes remained unchanged during that delivery. WowScreenEvents was deployed in the local Retail AddOns directory; its standalone ZIP contains exactly the TOC, three Lua files and runtime TGA. Its local main repository has no remote. This historical addon delivery is independent of the application-only 0.3.0 release. Generic OCR and additional live combat/hardware validation remain future work.

## Next session

Read actual Git status and verification artifacts, then continue the user's next request. Preserve current modes, profile and BLE lifecycle. For hardware work, read [BLUETOOTH.md](BLUETOOTH.md) and establish any missing device readiness from the current conversation. The idea catalog is a roadmap resource, not a request to implement all entries.
