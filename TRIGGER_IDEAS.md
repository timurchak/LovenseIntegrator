# Trigger ideas for Lovense Integrator

600 event ideas in 40 categories, 30 composite recipes and proposals for a flexible rule builder.

Date: 2026-10-04. This is a standalone brainstorming catalog, not a request for immediate implementation. Third-party products are possible integration directions; their API access, permissions, platform restrictions and specific events have not all been verified. Translation preserves the original IDs, difficulty ratings and implementation-status snapshot. For newer keyboard/mouse modes and BLE recovery work, see [session state](docs/SESSION_STATE.md).

## Reading the catalog

An event could trigger a user-selected impulse, pattern, parameter change, profile switch or stop. Continuous values such as speed, volume, distance and percentage could map to effect parameters through configurable curves. Intensity and duration need not rise together. Many of these output options are proposals, not existing commands.

Source labels: **Local** means OS events/computation, sometimes requiring permission; **Adapter** means a supported API, extension, plugin, export, log or webhook whose availability must be verified; **Recognition** analyzes selected audio, screen, text or video and may be wrong; **Sensor** means external hardware/phone telemetry; **Derived** combines other events and usually needs no new integration.

## Implementation difficulty: P0–P3

P denotes **implementation difficulty**, not product priority, urgency or `Rule.Priority`. P0 is easiest, P3 hardest. Estimates assume current local primitives; they do not confirm third-party API availability.

| Level | Meaning | Typical work |
| --- | --- | --- |
| P0 | Existing foundation or minimal extension | Existing event, simple configuration or internal signal, without a new adapter or complex state |
| P1 | Small standalone feature | Local OS input, counter, history, timer, simple math or own-command wrapper with clear verification |
| P2 | Complete module or integration | First app/device adapter, browser extension, protocol/auth, audio/screen capture, fixed OCR or scenario-engine extension |
| P3 | High complexity and uncertainty | Robust speech/object/motion recognition, tight multi-source timing, platform restrictions or a research stage |

Each trigger estimate includes acquiring/detecting that event for one selected app, game, device or site with basic configuration/testing, assuming an existing output action. A complete visual editor and new effect types are rated separately. Recipe estimates include all conditions and the described effect, so they may exceed the trigger's level.

External P2 assumes an allowed machine interface, log or export can actually supply the data. Without one, recognition may raise it to P3 or make the scenario unavailable. Supporting every named service is outside that estimate. A composite includes the hardest source plus coordination: simple AND of two available states is not automatically P3, but correlating fast game, audio and remote chat events may be.

Playful ideas are rated by their mechanics. Shared adapters lower the incremental cost of later events. Do not add ratings as person-days or promise schedules without checking interfaces. P0 does not imply complete, tested implementation.

### Trigger totals in the original audit

| Level | Total | Done | Partial | Missing |
| --- | ---: | ---: | ---: | ---: |
| P0 | 12 | 10 | 2 | 0 |
| P1 | 130 | 3 | 12 | 115 |
| P2 | 403 | 0 | 3 | 400 |
| P3 | 55 | 0 | 0 | 55 |
| Total | 600 | 13 | 17 | 570 |

### Category totals and difficulty drivers

| Category | P0 | P1 | P2 | P3 | Done | Partial | Missing | Main work |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| KEY | 6 | 9 | 0 | 0 | 9 | 1 | 5 | Most inputs exist; statistics, release duration and rhythm require limited state. |
| PTR | 2 | 11 | 2 | 0 | 2 | 2 | 11 | Buttons/wheel exist; coordinates extend input, gestures require a recognizer. |
| PAD | 0 | 8 | 7 | 0 | 0 | 0 | 15 | Basic gamepads/key pedals are local modules; MIDI, sensors and special devices need adapters. |
| WIN | 1 | 13 | 1 | 0 | 1 | 1 | 13 | Window events/history dominate; virtual desktops need compatibility research. |
| SYS | 0 | 12 | 3 | 0 | 0 | 0 | 15 | Ordinary metrics are local; GPU, temperature and fan data depend on providers. |
| FILE | 0 | 9 | 6 | 0 | 0 | 0 | 15 | Folders, logs, timers and ping are simpler; third-party progress and multi-protocol input need adapters. |
| WEB | 0 | 0 | 15 | 0 | 0 | 0 | 15 | Initial extension, desktop connection, permissions and tab lifecycle. |
| DOM | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One-site extension, selectors, dynamic pages and navigation recovery. |
| NOTE | 0 | 0 | 12 | 3 | 0 | 0 | 15 | Notifications/email require access; complete call lifecycle and voicemail are especially platform-dependent. |
| CHAT | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One service, bot/account connection, permissions, event IDs and reconnect. |
| SOC | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One accessible service, authorization, polling limits and previous-state storage. |
| LIVE | 0 | 0 | 15 | 0 | 0 | 0 | 15 | OBS or one streaming/support service; verify events and authorization separately. |
| VID | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One player, available subtitles or fixed screen region, with timed events. |
| MUS | 0 | 0 | 12 | 3 | 0 | 0 | 15 | Metadata needs an adapter; robust beat/BPM/energy detection exceeds simple thresholds. |
| AUD | 0 | 0 | 10 | 5 | 0 | 0 | 15 | Capture/basic features need a module; sound classification needs tuning/error checks. |
| VOX | 0 | 0 | 1 | 14 | 0 | 0 | 15 | ASR adds delay/errors; searching available text is simpler than speech or semantic classification. |
| VIS | 0 | 0 | 10 | 5 | 0 | 0 | 15 | Pixels/templates/fixed OCR need a module; objects, gestures and presence require recognition. |
| GAME | 0 | 1 | 14 | 0 | 0 | 0 | 15 | Process launch is local; semantic events need one game's adapter. |
| FPS | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One game's telemetry; verify latency and duplicates for fast events. |
| RPG | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One game's quests/dialogue/items; arbitrary UI classification is not included. |
| MMO | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One MMO and permitted event export; game API restrictions remain to be verified. |
| SIM | 0 | 0 | 14 | 1 | 0 | 0 | 15 | One adapter/mod; chess evaluation adds an analysis engine and its lifecycle. |
| RACE | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One simulator's telemetry, units, update rates and smoothing. |
| RHY | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One game with available hit ratings, combos and level state. |
| DEV | 0 | 7 | 8 | 0 | 0 | 0 | 15 | Own-command wrappers are simple; IDE, detailed tests and external services require integration. |
| AI | 0 | 0 | 14 | 1 | 0 | 0 | 15 | One tool with logs/events is P2; coordinating independent agent groups is P3. |
| TASK | 0 | 3 | 12 | 0 | 0 | 0 | 15 | Work timers are simpler; tasks, calendars and learning need app adapters. |
| DATA | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One sheet/dashboard; value changes, formulas and refresh must be verified in that app. |
| ART | 0 | 1 | 14 | 0 | 0 | 0 | 15 | One creative tool; pen pressure can be local, other events need an adapter. |
| HOME | 0 | 0 | 15 | 0 | 0 | 0 | 15 | One connected hub; include physical hardware and reconnect checks. |
| PHONE | 0 | 0 | 12 | 3 | 0 | 0 | 15 | Companion/automation; background motion adds OS restrictions and calibration. |
| BODY | 0 | 1 | 8 | 6 | 0 | 0 | 15 | Existing device telemetry is P2; movement recognition/synchronization is P3; breathing timer is local. |
| WORLD | 0 | 2 | 13 | 0 | 0 | 0 | 15 | Sunrise/sunset can be local; other sources need polling and duplicate filtering. |
| TIME | 1 | 14 | 0 | 0 | 1 | 3 | 11 | Interval timer exists; calendars, history, missing events and resets extend local state. |
| MATH | 0 | 14 | 1 | 0 | 0 | 0 | 15 | Counters/randomness are local; independent-stream coincidence needs time coordination. |
| MIX | 0 | 2 | 7 | 6 | 0 | 0 | 15 | Several sources plus history; latency, recognition and synchronization can raise complexity to P3. |
| TOY | 2 | 8 | 5 | 0 | 0 | 10 | 5 | Internal signals exist; battery/acknowledgments depend on transport, queues/conflicts on dispatch. |
| ODD | 0 | 7 | 8 | 0 | 0 | 0 | 15 | Humor does not set difficulty: coordinates/numbers are simple, text/sites need adapters. |
| COSMIC | 0 | 2 | 5 | 8 | 0 | 0 | 15 | Often multiple independent sources/detectors; moon/dice and sunset/windows can be local. |
| QUEST | 0 | 6 | 9 | 0 | 0 | 0 | 15 | Scenario state machine; local-input quests are simpler, external apps retain adapter costs. |

## Implementation status

The original audit checked source on 2026-10-04: engine, Windows input, UI event catalog/editor, executor, transports and tests. It was static inspection, without launching the app/devices or rerunning tests. Development can change the result. The audit included BLE effects from 100 ms and the seventh Keyboard feedback recipe. Newer verification is recorded separately in [session state](docs/SESSION_STATE.md).

**Done** means a UI-selectable trigger reaches event handling and a basic action, subject to stated limits. **Partial** means some behavior or an internal service exists but the entire idea is not configurable. **Missing** means the claimed trigger was not found; a similar key press or research plan does not count.

The 21 engine/UI event types cover 13 ideas fully and 17 partially in this snapshot; 570 remain missing. Counts differ because an idea may combine events or require extra output behavior. KEY-04 includes double and triple press; KEY-02 also needs hold-duration-to-pattern mapping.

| Group | UI EventKind values | Count |
| --- | --- | ---: |
| Keyboard | KeyDown, KeyUp, KeyHeld, KeyReleasedAfterHold, DoublePress, TriplePress, Chord, Sequence, PressCount, CleanTypingStreak, TypingRateAbove, TypingRateBelow | 12 |
| Mouse | MouseDown, MouseUp, MouseDoubleClick, MouseHeld, MouseWheel | 5 |
| Applications | ForegroundChanged | 1 |
| Time/activity | InputIdle, ActivityResumed, Timer | 3 |
| Total | Each has a RuleEngine branch | 21 |

Rules expose foreground-process filtering, cooldown, priority and target toy. Actions are Vibrate, Pulse, RateMapped and Stop. RateMapped computes intensity once per trigger; the UI offers it for the two typing-speed events, not continuous rhythm/intensity control. Duration is 0.1–300 seconds. Short effects run through BLE; LocalApiTransport rejects ≤1 second before sending.

### Mapping implemented ideas to source

| Idea | Status | Existing behavior and remaining work | Source |
| --- | --- | --- | --- |
| KEY-01 | Done | Physical KeyDown; autorepeat filtered | [input][input-src], [core][core-src] |
| KEY-03 | Done | KeyHeld threshold; once until release | [core][core-src] |
| KEY-04 | Done | DoublePress and TriplePress; triple interval is first-to-third | [core][core-src], [UI][ui-src] |
| KEY-05 | Done | Chord requires all selected keys held | [core][core-src] |
| KEY-06 | Done | Sequence order/time window; unrelated input breaks it | [core][core-src] |
| KEY-07 | Done | Physical presses in a sliding window up to 60 s; cooldown limits repeats after threshold | [core][core-src] |
| KEY-08 | Done | Speed threshold crossing/rearm; below waits for initial full window | [core][core-src] |
| KEY-10 | Done | Typing streak; Backspace/Delete reset, modifiers/arrows excluded | [core][core-src], [UI][ui-src] |
| KEY-15 | Done | First KeyDown/MouseDown/MouseWheel after idle; movement excluded | [core][core-src], [input][input-src] |
| PTR-01 | Done | Left, Right, Middle, X1, X2 MouseDown | [input][input-src], [UI][ui-src] |
| PTR-04 | Done | Wheel Up, Down or either | [input][input-src], [UI][ui-src] |
| WIN-01 | Done | New foreground process filter, polled at 50 ms; same-process windows not separate events | [executor][vm-src], [core][core-src] |
| TIME-02 | Done | Periodic timer while enabled; cooldown applies, restart resets time | [core][core-src], [executor][vm-src] |
| KEY-02 | Partial | Release-after-hold measures duration; no automatic duration-to-pattern choice | [core][core-src], [editor][editor-src] |
| PTR-02 | Partial | MouseUp and MouseHeld exist; no drag coordinates or release-after-drag condition | [input][input-src], [core][core-src] |
| PTR-03 | Partial | Double click exists; mouse triple click does not | [core][core-src], [UI][ui-src] |
| TIME-08 | Partial | InputIdle observes local keys/buttons/wheel, not absence of arbitrary A | [core][core-src] |
| TIME-10 | Partial | Local input resumes; not the first event of an arbitrary external source | [core][core-src] |
| TIME-14 | Partial | Cooldown starts at rule firing, not effect completion; no completion trigger | [core][core-src], [BLE][ble-src] |
| TOY-01 | Partial | Discovery list exists; no first-this-session trigger/history | [executor][vm-src], [BLE][ble-src] |
| TOY-02 | Partial | Connection state exists in BLE/API; not a selectable trigger | [BLE][ble-src], [API][api-src] |
| TOY-03 | Partial | Toy.Battery exists; no battery-threshold trigger | [BLE][ble-src], [API][api-src] |
| TOY-04 | Partial | API replies and BLE send/reject notices do not prove physical completion or feed rules | [API][api-src], [BLE][ble-src] |
| TOY-05 | Partial | FinishAsync ends effects locally; no pattern-finished event | [BLE][ble-src] |
| TOY-08 | Partial | Profile import pauses rules; no named-profile switch action/trigger | [executor][vm-src] |
| TOY-09 | Partial | Enable/pause button is an internal action, not a user-rule input | [executor][vm-src] |
| TOY-11 | Partial | BLE disconnection cancels its task; errors logged; no generic uncertain-state trigger | [BLE][ble-src], [executor][vm-src] |
| TOY-12 | Partial | Button/Ctrl+Alt+F12 stop everything; pressing Stop is not a scenario trigger | [window][window-src], [input][input-src], [executor][vm-src] |
| TOY-14 | Partial | One winner by priority; no conflict event, compatibility model or mixing policy | [core][core-src], [executor][vm-src] |
| WIN-02 | Partial | Foreground change filters the new process; no specific-app departure condition | [executor][vm-src], [core][core-src] |

BLE/Local API are outgoing toy transports, not incoming HTTP/webhook/MQTT event adapters. The process list selects a foreground filter, not process-start/exit events. Title substring filtering is not a title-change event, and time away from a specific application is not tracked. Keyboard double/triple presses exist; mouse supports double only, and holding a button is not drag detection.

Observation logs matching rules, not every input or rejection reason, and does not record/replay event traces. Back to work means input after inactivity, not return to an IDE after time away. Typing rhythm maps intensity at trigger time, not pulse rate continuously. Discord is explicitly [cancelled][discord-plan]; other external chat, browser, game, audio, video and sensor adapters are also absent.

[Tests][tests-src] cover release duration, triple press, clean streak, activity resumed, mouse hold/double click, short BLE effects/cancellation and REST rejection. Their presence is not a fresh execution result.

[core-src]: src/LovenseIntegrator.Core/Rules.cs
[input-src]: src/LovenseIntegrator.Desktop/Services/WindowsInput.cs
[ui-src]: src/LovenseIntegrator.Desktop/ViewModels/RulePresentation.cs
[editor-src]: src/LovenseIntegrator.Desktop/ViewModels/RuleEditor.cs
[vm-src]: src/LovenseIntegrator.Desktop/ViewModels/MainViewModel.cs
[window-src]: src/LovenseIntegrator.Desktop/MainWindow.xaml.cs
[ble-src]: src/LovenseIntegrator.Desktop/Transports/BleTransport.cs
[api-src]: src/LovenseIntegrator.Desktop/Transports/LocalApiTransport.cs
[tests-src]: tests/LovenseIntegrator.Tests/Program.cs
[discord-plan]: DISCORD_INTEGRATION.md


## 01. Keyboard — KEY

Source: Local. Semantic text is better obtained from an explicitly selected editor adapter; these ideas do not require storing all keystrokes.

- **KEY-01** **[P0]** **[Done]** — A selected key is pressed: separate effects for Space, Enter, Escape and others.
- **KEY-02** **[P1]** **[Partial]** — A held key is released; hold duration selects the pattern.
- **KEY-03** **[P0]** **[Done]** — A key remains held beyond a threshold.
- **KEY-04** **[P1]** **[Done]** — A double or triple press occurs within a time window.
- **KEY-05** **[P0]** **[Done]** — Selected keys are held simultaneously.
- **KEY-06** **[P0]** **[Done]** — A key sequence is entered, including a custom secret code.
- **KEY-07** **[P0]** **[Done]** — N physical presses are reached in a minute.
- **KEY-08** **[P0]** **[Done]** — Typing speed crosses an upper or lower threshold.
- **KEY-09** **[P1]** **[Missing]** — Typing speed changes sharply relative to its own average.
- **KEY-10** **[P1]** **[Done]** — A streak of N presses without Backspace or Delete is reached.
- **KEY-11** **[P1]** **[Missing]** — Backspace exceeds a selected share of recent presses.
- **KEY-12** **[P1]** **[Missing]** — The keyboard layout changes.
- **KEY-13** **[P1]** **[Missing]** — Caps Lock remains enabled longer than a threshold.
- **KEY-14** **[P1]** **[Missing]** — Alternating two keys matches a chosen rhythm.
- **KEY-15** **[P1]** **[Done]** — The first press arrives after a long period without input.

## 02. Mouse and touchpad — PTR

Source: Local; touchpad gestures depend on events exposed by the device.

- **PTR-01** **[P0]** **[Done]** — A particular mouse button is pressed, including side buttons.
- **PTR-02** **[P1]** **[Partial]** — A button is released after a long drag.
- **PTR-03** **[P1]** **[Partial]** — A double or triple click occurs within a selected interval.
- **PTR-04** **[P0]** **[Done]** — The wheel scrolls up or down.
- **PTR-05** **[P1]** **[Missing]** — N scroll steps accumulate in one second.
- **PTR-06** **[P1]** **[Missing]** — Scrolling direction reverses several times in a row.
- **PTR-07** **[P1]** **[Missing]** — The pointer travels a selected distance within a time window.
- **PTR-08** **[P1]** **[Missing]** — Pointer speed exceeds a threshold.
- **PTR-09** **[P1]** **[Missing]** — The pointer accelerates sharply or stops.
- **PTR-10** **[P1]** **[Missing]** — The pointer enters a selected screen region.
- **PTR-11** **[P1]** **[Missing]** — The pointer stays over a region longer than N seconds.
- **PTR-12** **[P1]** **[Missing]** — The pointer crosses between monitors.
- **PTR-13** **[P2]** **[Missing]** — The mouse draws a circle, zigzag or figure eight.
- **PTR-14** **[P2]** **[Missing]** — A selected touchpad gesture occurs, if exposed by the driver.
- **PTR-15** **[P1]** **[Missing]** — The user clicks repeatedly within a small region.

## 03. Gamepad and alternative input — PAD

Source: Local or a device adapter.

- **PAD-01** **[P1]** **[Missing]** — A selected gamepad button is pressed.
- **PAD-02** **[P1]** **[Missing]** — An analog trigger crosses a selected pressure level.
- **PAD-03** **[P1]** **[Missing]** — A stick moves beyond a selected dead zone.
- **PAD-04** **[P1]** **[Missing]** — A stick completes a full rotation.
- **PAD-05** **[P1]** **[Missing]** — Both sticks point in opposite directions.
- **PAD-06** **[P1]** **[Missing]** — Input matches a fighting-game combination.
- **PAD-07** **[P1]** **[Missing]** — A USB foot pedal is pressed.
- **PAD-08** **[P1]** **[Missing]** — A hardware dial changes its value by N steps.
- **PAD-09** **[P2]** **[Missing]** — A MIDI key is pressed at a selected velocity.
- **PAD-10** **[P2]** **[Missing]** — A MIDI sustain pedal is held beyond a threshold.
- **PAD-11** **[P2]** **[Missing]** — A specially assigned Stream Deck button is pressed.
- **PAD-12** **[P2]** **[Missing]** — A steering wheel crosses a selected rotation angle.
- **PAD-13** **[P2]** **[Missing]** — A simulator pedal exceeds a selected pressure percentage.
- **PAD-14** **[P2]** **[Missing]** — A controller motion sensor detects a tilt or shake.
- **PAD-15** **[P2]** **[Missing]** — A selected NFC tag, RFID card or QR code is scanned.

## 04. Windows and desktop — WIN

Source: Local; window content requires an available interface or a selected recognition region.

- **WIN-01** **[P0]** **[Done]** — A specific application becomes active.
- **WIN-02** **[P1]** **[Partial]** — The user leaves a selected application.
- **WIN-03** **[P1]** **[Missing]** — A selected word appears in the active window title.
- **WIN-04** **[P1]** **[Missing]** — A selected application opens a new window.
- **WIN-05** **[P1]** **[Missing]** — The last application window closes.
- **WIN-06** **[P1]** **[Missing]** — A window enters full-screen mode.
- **WIN-07** **[P1]** **[Missing]** — A window is minimized or restored.
- **WIN-08** **[P1]** **[Missing]** — A window moves to another monitor.
- **WIN-09** **[P2]** **[Missing]** — The virtual desktop changes.
- **WIN-10** **[P1]** **[Missing]** — More than N application switches occur in one minute.
- **WIN-11** **[P1]** **[Missing]** — One application remains active continuously for N minutes.
- **WIN-12** **[P1]** **[Missing]** — The number of open windows changes.
- **WIN-13** **[P1]** **[Missing]** — The user session locks or unlocks.
- **WIN-14** **[P1]** **[Missing]** — Display resolution, scaling or orientation changes.
- **WIN-15** **[P1]** **[Missing]** — The user returns to a window not visited for several hours.

## 05. System and hardware — SYS

Source: Local; hardware metrics depend on the device and telemetry provider.

- **SYS-01** **[P1]** **[Missing]** — CPU load crosses a threshold and remains there for N seconds.
- **SYS-02** **[P2]** **[Missing]** — GPU load reaches a selected level.
- **SYS-03** **[P1]** **[Missing]** — Free RAM falls below a threshold.
- **SYS-04** **[P1]** **[Missing]** — Disk activity rises sharply above its baseline.
- **SYS-05** **[P2]** **[Missing]** — A selected temperature sensor enters a chosen range.
- **SYS-06** **[P2]** **[Missing]** — Fan speed changes by a selected amount.
- **SYS-07** **[P1]** **[Missing]** — Battery charge reaches a selected percentage.
- **SYS-08** **[P1]** **[Missing]** — External power connects or disconnects.
- **SYS-09** **[P1]** **[Missing]** — A particular USB device appears.
- **SYS-10** **[P1]** **[Missing]** — Headphones connect or disconnect.
- **SYS-11** **[P1]** **[Missing]** — The default audio output device changes.
- **SYS-12** **[P1]** **[Missing]** — The computer wakes from sleep.
- **SYS-13** **[P1]** **[Missing]** — A selected process starts or exits.
- **SYS-14** **[P1]** **[Missing]** — A background job finishes with a selected result code.
- **SYS-15** **[P1]** **[Missing]** — Computer uptime reaches a round-number milestone.

## 06. Files, downloads and local network — FILE

Source: Local or an adapter for a download client/network device.

- **FILE-01** **[P1]** **[Missing]** — A new file appears in a selected folder.
- **FILE-02** **[P1]** **[Missing]** — A file with a selected extension changes.
- **FILE-03** **[P1]** **[Missing]** — File size exceeds a threshold.
- **FILE-04** **[P1]** **[Missing]** — A file is renamed to match a pattern.
- **FILE-05** **[P1]** **[Missing]** — A monitored log receives a line matching a regular expression.
- **FILE-06** **[P2]** **[Missing]** — A file download completes.
- **FILE-07** **[P2]** **[Missing]** — A download reaches 25%, 50%, 75% or another milestone.
- **FILE-08** **[P2]** **[Missing]** — Download speed falls below its own average.
- **FILE-09** **[P2]** **[Missing]** — A selected folder finishes synchronizing.
- **FILE-10** **[P2]** **[Missing]** — A backup completes successfully.
- **FILE-11** **[P1]** **[Missing]** — Recycle Bin size reaches a selected value.
- **FILE-12** **[P1]** **[Missing]** — The computer connects to a selected Wi-Fi network.
- **FILE-13** **[P1]** **[Missing]** — Latency to the user's own server crosses a threshold.
- **FILE-14** **[P1]** **[Missing]** — Internet connectivity returns after an outage.
- **FILE-15** **[P2]** **[Missing]** — A local service sends a custom event over HTTP, WebSocket, MQTT or OSC.

## 07. Browser application — WEB

Source: Browser adapter, usually an extension; the user selects domains and tabs.

- **WEB-01** **[P2]** **[Missing]** — A selected domain opens.
- **WEB-02** **[P2]** **[Missing]** — The URL matches a user-defined pattern.
- **WEB-03** **[P2]** **[Missing]** — A new tab is created.
- **WEB-04** **[P2]** **[Missing]** — A tab for a specific site closes.
- **WEB-05** **[P2]** **[Missing]** — The tab count crosses a threshold.
- **WEB-06** **[P2]** **[Missing]** — The user switches between two tabs N times.
- **WEB-07** **[P2]** **[Missing]** — A page loads successfully after a long wait.
- **WEB-08** **[P2]** **[Missing]** — Navigation fails, if that status is exposed to the extension.
- **WEB-09** **[P2]** **[Missing]** — A bookmark is added.
- **WEB-10** **[P2]** **[Missing]** — A bookmark opens after a selected interval.
- **WEB-11** **[P2]** **[Missing]** — A tab starts or stops playing audio.
- **WEB-12** **[P2]** **[Missing]** — A tab is muted or unmuted.
- **WEB-13** **[P2]** **[Missing]** — Page zoom changes.
- **WEB-14** **[P2]** **[Missing]** — A page is reloaded N times in a minute.
- **WEB-15** **[P2]** **[Missing]** — Time spent in a selected tab exceeds a threshold.

## 08. Website content — DOM

Source: Page extension/adapter or recognition, limited to selected sites and elements.

- **DOM-01** **[P2]** **[Missing]** — An element matching a selected selector appears on the page.
- **DOM-02** **[P2]** **[Missing]** — The text of a specific element changes.
- **DOM-03** **[P2]** **[Missing]** — A number in a selected element crosses a threshold.
- **DOM-04** **[P2]** **[Missing]** — A particular website button is clicked.
- **DOM-05** **[P2]** **[Missing]** — The user submits a selected form.
- **DOM-06** **[P2]** **[Missing]** — A page indicator reports successful completion.
- **DOM-07** **[P2]** **[Missing]** — A validation error message appears.
- **DOM-08** **[P2]** **[Missing]** — Scrolling reaches the end of an article.
- **DOM-09** **[P2]** **[Missing]** — Scroll position indicates N percent of a long page has been read.
- **DOM-10** **[P2]** **[Missing]** — A page countdown reaches zero.
- **DOM-11** **[P2]** **[Missing]** — A tracked product comes back in stock.
- **DOM-12** **[P2]** **[Missing]** — A selected price changes from its previous value.
- **DOM-13** **[P2]** **[Missing]** — A new item appears in the user's task queue.
- **DOM-14** **[P2]** **[Missing]** — An unread counter on a selected site increases.
- **DOM-15** **[P2]** **[Missing]** — A lesson, quiz or exercise on a learning site completes.

## 09. Notifications, email and calls — NOTE

Source: Authorized notification, email or telephony adapter; content availability depends on the application.

- **NOTE-01** **[P2]** **[Missing]** — A notification arrives from a particular application.
- **NOTE-02** **[P2]** **[Missing]** — A notification contains a selected phrase.
- **NOTE-03** **[P2]** **[Missing]** — N notifications arrive within a minute.
- **NOTE-04** **[P2]** **[Missing]** — The first notification arrives after a long quiet period.
- **NOTE-05** **[P2]** **[Missing]** — An email arrives from a selected contact.
- **NOTE-06** **[P2]** **[Missing]** — An email matches a particular filter or label.
- **NOTE-07** **[P2]** **[Missing]** — An email subject contains a keyword.
- **NOTE-08** **[P2]** **[Missing]** — The unread email count reaches zero.
- **NOTE-09** **[P2]** **[Missing]** — The user sends an email from a selected account.
- **NOTE-10** **[P3]** **[Missing]** — A call starts, is answered or ends, with separate effects for each state.
- **NOTE-11** **[P3]** **[Missing]** — A missed call appears.
- **NOTE-12** **[P3]** **[Missing]** — A new voicemail arrives.
- **NOTE-13** **[P2]** **[Missing]** — A calendar reminder fires N minutes before an event.
- **NOTE-14** **[P2]** **[Missing]** — Do Not Disturb ends.
- **NOTE-15** **[P2]** **[Missing]** — Notifications accumulated during a pause are summarized in one signal.

## 10. Messaging and communities — CHAT

Source: Adapter for Discord, Telegram, Slack, Teams or another selected service; verify events and bot access separately. Discord is currently cancelled.

- **CHAT-01** **[P2]** **[Missing]** — A message is posted in a selected channel.
- **CHAT-02** **[P2]** **[Missing]** — The user is explicitly mentioned in a message.
- **CHAT-03** **[P2]** **[Missing]** — A selected reaction is added to the user's own message.
- **CHAT-04** **[P2]** **[Missing]** — A message reaches N reactions.
- **CHAT-05** **[P2]** **[Missing]** — A message contains a particular emoji.
- **CHAT-06** **[P2]** **[Missing]** — A special bot command is sent in an allowed channel.
- **CHAT-07** **[P2]** **[Missing]** — A poll option linked to a particular pattern is selected.
- **CHAT-08** **[P2]** **[Missing]** — A poll ends and its winning option is determined.
- **CHAT-09** **[P2]** **[Missing]** — A selected participant joins a voice channel.
- **CHAT-10** **[P2]** **[Missing]** — The user joins a voice channel.
- **CHAT-11** **[P2]** **[Missing]** — The voice-channel participant count changes.
- **CHAT-12** **[P2]** **[Missing]** — The user's microphone switches between mute and unmute.
- **CHAT-13** **[P2]** **[Missing]** — The first reply appears in a selected thread.
- **CHAT-14** **[P2]** **[Missing]** — Several people send the same emoji within a short period.
- **CHAT-15** **[P2]** **[Missing]** — An allowed contact sends a one-time event through an explicitly enabled shared session.

## 11. Social platforms and publishing — SOC

Source: Selected platform adapter, notifications or a user-provided export.

- **SOC-01** **[P2]** **[Missing]** — The user's post receives a new like.
- **SOC-02** **[P2]** **[Missing]** — A comment appears under the user's post.
- **SOC-03** **[P2]** **[Missing]** — The user's post is shared.
- **SOC-04** **[P2]** **[Missing]** — A new follower arrives.
- **SOC-05** **[P2]** **[Missing]** — Follower count reaches a selected milestone.
- **SOC-06** **[P2]** **[Missing]** — The user is tagged in a post.
- **SOC-07** **[P2]** **[Missing]** — The user's scheduled post is published.
- **SOC-08** **[P2]** **[Missing]** — A post first reaches N views.
- **SOC-09** **[P2]** **[Missing]** — The rate of incoming reactions exceeds a threshold.
- **SOC-10** **[P2]** **[Missing]** — An old post receives a reaction after a long quiet period.
- **SOC-11** **[P2]** **[Missing]** — The user's comment receives a reply.
- **SOC-12** **[P2]** **[Missing]** — A selected author publishes a new post.
- **SOC-13** **[P2]** **[Missing]** — An authorized feed contains a selected hashtag.
- **SOC-14** **[P2]** **[Missing]** — Time spent reading a feed exceeds the user's limit.
- **SOC-15** **[P2]** **[Missing]** — A streak of scheduled personal posts is reached.

## 12. Streaming and OBS — LIVE

Source: OBS, streaming-platform or creator-support adapter; verify specific event types.

- **LIVE-01** **[P2]** **[Missing]** — A stream starts.
- **LIVE-02** **[P2]** **[Missing]** — A stream ends.
- **LIVE-03** **[P2]** **[Missing]** — The OBS scene changes.
- **LIVE-04** **[P2]** **[Missing]** — A selected OBS source becomes visible.
- **LIVE-05** **[P2]** **[Missing]** — Recording starts, stops or pauses.
- **LIVE-06** **[P2]** **[Missing]** — A new viewer follows the channel, if the platform exposes the event.
- **LIVE-07** **[P2]** **[Missing]** — A paid subscription or renewal arrives.
- **LIVE-08** **[P2]** **[Missing]** — N subscriptions are gifted in one event.
- **LIVE-09** **[P2]** **[Missing]** — Creator support arrives; its amount selects a range of patterns.
- **LIVE-10** **[P2]** **[Missing]** — A viewer redeems a purpose-built channel reward.
- **LIVE-11** **[P2]** **[Missing]** — A raid or similar audience transfer begins.
- **LIVE-12** **[P2]** **[Missing]** — Viewer count crosses a selected milestone.
- **LIVE-13** **[P2]** **[Missing]** — Chat message rate exceeds a threshold.
- **LIVE-14** **[P2]** **[Missing]** — A collective stream goal reaches 100%.
- **LIVE-15** **[P2]** **[Missing]** — Viewers finish voting for the next allowed effect.

## 13. Video, films and players — VID

Source: Player/browser adapter, metadata, subtitles or recognition. Semantic detections are approximate.

- **VID-01** **[P2]** **[Missing]** — A video starts or resumes.
- **VID-02** **[P2]** **[Missing]** — A video is paused.
- **VID-03** **[P2]** **[Missing]** — Playback reaches a selected timecode.
- **VID-04** **[P2]** **[Missing]** — A new video chapter begins.
- **VID-05** **[P2]** **[Missing]** — The user seeks backwards by more than N seconds.
- **VID-06** **[P2]** **[Missing]** — Playback speed changes.
- **VID-07** **[P2]** **[Missing]** — Full-screen mode is enabled.
- **VID-08** **[P2]** **[Missing]** — Buffering lasts longer than a threshold.
- **VID-09** **[P2]** **[Missing]** — The video ends.
- **VID-10** **[P2]** **[Missing]** — The next playlist episode starts.
- **VID-11** **[P2]** **[Missing]** — A selected phrase appears in subtitles.
- **VID-12** **[P2]** **[Missing]** — Subtitles mention a selected character's name.
- **VID-13** **[P2]** **[Missing]** — The image abruptly changes average brightness or color palette.
- **VID-14** **[P2]** **[Missing]** — A detector finds an editing cut.
- **VID-15** **[P2]** **[Missing]** — A manually annotated event track reaches its next marker.

## 14. Music and library — MUS

Source: Player adapter, local metadata or audio analysis; not every service exposes musical features.

- **MUS-01** **[P2]** **[Missing]** — A new track starts.
- **MUS-02** **[P2]** **[Missing]** — A track by a selected artist starts playing.
- **MUS-03** **[P2]** **[Missing]** — The track title contains a selected word.
- **MUS-04** **[P2]** **[Missing]** — A song from a selected playlist starts playing.
- **MUS-05** **[P2]** **[Missing]** — A track is added to favorites.
- **MUS-06** **[P2]** **[Missing]** — The user skips a track.
- **MUS-07** **[P2]** **[Missing]** — One track plays several times in a row.
- **MUS-08** **[P2]** **[Missing]** — A pre-annotated chorus begins.
- **MUS-09** **[P3]** **[Missing]** — A beat detector identifies the next beat.
- **MUS-10** **[P3]** **[Missing]** — Track tempo enters a selected BPM range.
- **MUS-11** **[P3]** **[Missing]** — Energy rises sharply after a quiet section.
- **MUS-12** **[P2]** **[Missing]** — Bass becomes stronger relative to midrange frequencies.
- **MUS-13** **[P2]** **[Missing]** — An unfamiliar playlist track plays for the first time this session.
- **MUS-14** **[P2]** **[Missing]** — Total listening time reaches N minutes.
- **MUS-15** **[P2]** **[Missing]** — The album's last track ends without any tracks skipped.

## 15. Sound and acoustic events — AUD

Source: Recognition of a selected audio stream or microphone; each detector needs confidence and repeat filtering.

- **AUD-01** **[P2]** **[Missing]** — Volume crosses a threshold.
- **AUD-02** **[P2]** **[Missing]** — Volume remains below a threshold for N seconds.
- **AUD-03** **[P2]** **[Missing]** — Sound returns after a long silence.
- **AUD-04** **[P2]** **[Missing]** — A short percussive sound is detected.
- **AUD-05** **[P2]** **[Missing]** — Two claps are detected within a selected interval.
- **AUD-06** **[P2]** **[Missing]** — A whistle is detected.
- **AUD-07** **[P2]** **[Missing]** — A selected frequency or narrow spectral band becomes dominant.
- **AUD-08** **[P2]** **[Missing]** — Sound moves from the left channel to the right.
- **AUD-09** **[P2]** **[Missing]** — Repeated sounds match a selected rhythm.
- **AUD-10** **[P3]** **[Missing]** — Laughter is recognized.
- **AUD-11** **[P3]** **[Missing]** — A dog bark is recognized.
- **AUD-12** **[P3]** **[Missing]** — A meow is recognized.
- **AUD-13** **[P3]** **[Missing]** — A doorbell is recognized.
- **AUD-14** **[P3]** **[Missing]** — An acoustic signature matches a selected game sound.
- **AUD-15** **[P2]** **[Missing]** — A selected application produces audio while the others remain silent.

## 16. Speech and text meaning — VOX

Source: Local/authorized speech recognition or a text adapter. Semantic classification is a hypothesis, not established fact.

- **VOX-01** **[P3]** **[Missing]** — A selected code word is spoken.
- **VOX-02** **[P3]** **[Missing]** — A command to start a particular pattern is spoken.
- **VOX-03** **[P3]** **[Missing]** — A profile-switching command is spoken.
- **VOX-04** **[P3]** **[Missing]** — A selected word is repeated N times in a minute.
- **VOX-05** **[P3]** **[Missing]** — Speech rate exceeds a selected number of words per minute.
- **VOX-06** **[P3]** **[Missing]** — A long pause occurs between utterances.
- **VOX-07** **[P3]** **[Missing]** — Recognition identifies an utterance ending in a question.
- **VOX-08** **[P3]** **[Missing]** — A selected filler word is detected in speech.
- **VOX-09** **[P3]** **[Missing]** — A spoken number falls within a selected range.
- **VOX-10** **[P3]** **[Missing]** — A rhyming pair of words is spoken.
- **VOX-11** **[P3]** **[Missing]** — A switch to another language is recognized.
- **VOX-12** **[P3]** **[Missing]** — Reading aloud reaches a preselected sentence.
- **VOX-13** **[P2]** **[Missing]** — Authorized text contains a word from a user-defined dictionary.
- **VOX-14** **[P3]** **[Missing]** — A classifier labels an utterance as a joke with sufficient confidence.
- **VOX-15** **[P3]** **[Missing]** — A forbidden word is spoken during an explicitly enabled word game.

## 17. Screen, images and camera — VIS

Source: Selected screen region or explicitly enabled camera. Most ideas need simple features without identifying people.

- **VIS-01** **[P2]** **[Missing]** — A pixel or small region changes to a selected color.
- **VIS-02** **[P2]** **[Missing]** — The selected color occupies more than a threshold share of the screen.
- **VIS-03** **[P2]** **[Missing]** — A predefined image template is found.
- **VIS-04** **[P2]** **[Missing]** — A template disappears from the selected region.
- **VIS-05** **[P2]** **[Missing]** — OCR detects a selected word.
- **VIS-06** **[P2]** **[Missing]** — OCR reads a number that crosses a threshold.
- **VIS-07** **[P2]** **[Missing]** — A progress bar visually reaches a selected percentage.
- **VIS-08** **[P2]** **[Missing]** — The difference between adjacent frames exceeds a threshold.
- **VIS-09** **[P2]** **[Missing]** — The image remains almost still for N seconds.
- **VIS-10** **[P3]** **[Missing]** — A mug, cat or another selected object class appears in frame.
- **VIS-11** **[P3]** **[Missing]** — An object crosses a virtual line in the frame.
- **VIS-12** **[P3]** **[Missing]** — The user makes a preselected hand gesture.
- **VIS-13** **[P3]** **[Missing]** — The user nods or shakes their head.
- **VIS-14** **[P2]** **[Missing]** — A prepared visual marker appears in frame.
- **VIS-15** **[P3]** **[Missing]** — A detector switches between person-at-desk and empty-desk states.

## 18. General game events — GAME

Source: Official/permitted game adapter, mod, telemetry export, log or HUD recognition. Availability varies by game.

- **GAME-01** **[P1]** **[Missing]** — A gaming session starts.
- **GAME-02** **[P2]** **[Missing]** — A new match, round or run starts.
- **GAME-03** **[P2]** **[Missing]** — A round ends in victory.
- **GAME-04** **[P2]** **[Missing]** — A round ends in defeat.
- **GAME-05** **[P2]** **[Missing]** — The character dies.
- **GAME-06** **[P2]** **[Missing]** — The character respawns.
- **GAME-07** **[P2]** **[Missing]** — An achievement is earned.
- **GAME-08** **[P2]** **[Missing]** — A personal best is set.
- **GAME-09** **[P2]** **[Missing]** — The next level starts.
- **GAME-10** **[P2]** **[Missing]** — A checkpoint is reached.
- **GAME-11** **[P2]** **[Missing]** — A rare item is collected.
- **GAME-12** **[P2]** **[Missing]** — A new map region is unlocked.
- **GAME-13** **[P2]** **[Missing]** — The game is paused or resumed.
- **GAME-14** **[P2]** **[Missing]** — Difficulty is changed manually.
- **GAME-15** **[P2]** **[Missing]** — A streak of N wins or losses is reached.

## 19. Shooters and action — FPS

Source: A specific game adapter or recognition of available game indicators.

- **FPS-01** **[P2]** **[Missing]** — A shot is fired.
- **FPS-02** **[P2]** **[Missing]** — Reloading starts or finishes.
- **FPS-03** **[P2]** **[Missing]** — The magazine becomes empty.
- **FPS-04** **[P2]** **[Missing]** — A hit on a target is confirmed.
- **FPS-05** **[P2]** **[Missing]** — A headshot is confirmed.
- **FPS-06** **[P2]** **[Missing]** — An opponent is eliminated.
- **FPS-07** **[P2]** **[Missing]** — An elimination assist is awarded.
- **FPS-08** **[P2]** **[Missing]** — The character takes damage.
- **FPS-09** **[P2]** **[Missing]** — Armor breaks.
- **FPS-10** **[P2]** **[Missing]** — Health falls below a selected percentage.
- **FPS-11** **[P2]** **[Missing]** — The player restores health or armor.
- **FPS-12** **[P2]** **[Missing]** — The player performs a perfect parry or dodge.
- **FPS-13** **[P2]** **[Missing]** — Planting/defusing an objective starts or finishes.
- **FPS-14** **[P2]** **[Missing]** — The player becomes the team's last survivor.
- **FPS-15** **[P2]** **[Missing]** — Several successful actions occur within a short window.

## 20. RPGs, roguelikes and adventures — RPG

Source: Game adapter, permitted mod, log or recognition.

- **RPG-01** **[P2]** **[Missing]** — The character gains a level.
- **RPG-02** **[P2]** **[Missing]** — Experience reaches a selected fraction of the next level.
- **RPG-03** **[P2]** **[Missing]** — A new quest is received.
- **RPG-04** **[P2]** **[Missing]** — A quest is completed.
- **RPG-05** **[P2]** **[Missing]** — A selected dialogue decision is made.
- **RPG-06** **[P2]** **[Missing]** — A skill check succeeds.
- **RPG-07** **[P2]** **[Missing]** — A virtual die rolls a critical value.
- **RPG-08** **[P2]** **[Missing]** — An item of selected rarity is obtained.
- **RPG-09** **[P2]** **[Missing]** — A resource such as mana, stamina or energy is exhausted.
- **RPG-10** **[P2]** **[Missing]** — A particular status effect is applied.
- **RPG-11** **[P2]** **[Missing]** — A status effect is removed.
- **RPG-12** **[P2]** **[Missing]** — A new boss phase begins.
- **RPG-13** **[P2]** **[Missing]** — A chest or secret room is opened.
- **RPG-14** **[P2]** **[Missing]** — An upgrade is selected for the current run.
- **RPG-15** **[P2]** **[Missing]** — A resource from the previous failed run is lost or recovered.

## 21. MMOs and cooperative play — MMO

Source: Permitted addon/adapter or log; check every event against the chosen game's available APIs, including WoW.

- **MMO-01** **[P2]** **[Missing]** — A group reaches the required size.
- **MMO-02** **[P2]** **[Missing]** — All participants complete a ready check.
- **MMO-03** **[P2]** **[Missing]** — A pre-combat countdown starts.
- **MMO-04** **[P2]** **[Missing]** — A selected ability is used.
- **MMO-05** **[P2]** **[Missing]** — An important ability becomes ready again.
- **MMO-06** **[P2]** **[Missing]** — A selected temporary bonus or proc activates.
- **MMO-07** **[P2]** **[Missing]** — An enemy spell is interrupted.
- **MMO-08** **[P2]** **[Missing]** — A selected negative effect is removed from an ally.
- **MMO-09** **[P2]** **[Missing]** — A selected encounter debuff is applied.
- **MMO-10** **[P2]** **[Missing]** — An ally is resurrected during combat.
- **MMO-11** **[P2]** **[Missing]** — The entire group dies.
- **MMO-12** **[P2]** **[Missing]** — A dungeon is completed within its timer.
- **MMO-13** **[P2]** **[Missing]** — Reputation level increases.
- **MMO-14** **[P2]** **[Missing]** — The user's listing sells at the in-game auction house.
- **MMO-15** **[P2]** **[Missing]** — A rare fish is caught or a rare item is crafted.

## 22. Strategy, sandbox and simulation — SIM

Source: Game adapter, mod or observable indicators.

- **SIM-01** **[P2]** **[Missing]** — Technology research completes.
- **SIM-02** **[P2]** **[Missing]** — A selected building is constructed.
- **SIM-03** **[P2]** **[Missing]** — The production queue becomes empty.
- **SIM-04** **[P2]** **[Missing]** — A resource reaches its target stockpile.
- **SIM-05** **[P2]** **[Missing]** — A production line stops due to missing raw materials.
- **SIM-06** **[P2]** **[Missing]** — The electricity network runs short of power.
- **SIM-07** **[P2]** **[Missing]** — City population reaches a new milestone.
- **SIM-08** **[P2]** **[Missing]** — An attack on the settlement begins.
- **SIM-09** **[P2]** **[Missing]** — An alliance or peace agreement is concluded.
- **SIM-10** **[P2]** **[Missing]** — A new turn begins.
- **SIM-11** **[P2]** **[Missing]** — A selected block is placed or destroyed in a grid-based game.
- **SIM-12** **[P2]** **[Missing]** — A selected circuit or sensor activates in a Minecraft-like game.
- **SIM-13** **[P2]** **[Missing]** — A crop is harvested or a production cycle finishes.
- **SIM-14** **[P2]** **[Missing]** — Chess game data reports check or checkmate.
- **SIM-15** **[P3]** **[Missing]** — The evaluation of the user's chess position changes beyond a threshold during permitted game analysis.

## 23. Racing, flight and transport — RACE

Source: Game telemetry or simulator adapter; scenarios concern virtual vehicles.

- **RACE-01** **[P2]** **[Missing]** — Speed crosses a selected threshold.
- **RACE-02** **[P2]** **[Missing]** — Engine RPM enters a selected range.
- **RACE-03** **[P2]** **[Missing]** — The gear changes.
- **RACE-04** **[P2]** **[Missing]** — Heavy braking begins.
- **RACE-05** **[P2]** **[Missing]** — The car starts skidding.
- **RACE-06** **[P2]** **[Missing]** — A drift lasts longer than N seconds.
- **RACE-07** **[P2]** **[Missing]** — A collision occurs.
- **RACE-08** **[P2]** **[Missing]** — Wheels touch a curb or rough surface.
- **RACE-09** **[P2]** **[Missing]** — An opponent is overtaken.
- **RACE-10** **[P2]** **[Missing]** — A lap is completed.
- **RACE-11** **[P2]** **[Missing]** — A sector is completed faster than the personal best.
- **RACE-12** **[P2]** **[Missing]** — Virtual vehicle fuel or battery reaches a threshold.
- **RACE-13** **[P2]** **[Missing]** — A simulator aircraft takes off from the runway.
- **RACE-14** **[P2]** **[Missing]** — A simulator aircraft touches down on landing.
- **RACE-15** **[P2]** **[Missing]** — A transport simulator vehicle stops precisely at its assigned point.

## 24. Rhythm games, platformers and puzzles — RHY

Source: Game adapter or recognition of its indicators.

- **RHY-01** **[P2]** **[Missing]** — A note receives a Perfect rating.
- **RHY-02** **[P2]** **[Missing]** — A note is missed.
- **RHY-03** **[P2]** **[Missing]** — A combo reaches a selected length.
- **RHY-04** **[P2]** **[Missing]** — A combo breaks.
- **RHY-05** **[P2]** **[Missing]** — A section is played without mistakes.
- **RHY-06** **[P2]** **[Missing]** — The player enters a bonus mode.
- **RHY-07** **[P2]** **[Missing]** — A jump starts precisely at a platform edge.
- **RHY-08** **[P2]** **[Missing]** — The character lands on a selected surface.
- **RHY-09** **[P2]** **[Missing]** — The character falls outside the level boundaries.
- **RHY-10** **[P2]** **[Missing]** — All items in a room are collected.
- **RHY-11** **[P2]** **[Missing]** — A puzzle row or set is completed.
- **RHY-12** **[P2]** **[Missing]** — Several matches form a cascade.
- **RHY-13** **[P2]** **[Missing]** — A word longer than N letters is found.
- **RHY-14** **[P2]** **[Missing]** — A puzzle is solved without hints.
- **RHY-15** **[P2]** **[Missing]** — Little time remains on the game timer.

## 25. Programming and infrastructure — DEV

Source: IDE adapter, commands with explicit completion events, local logs or the user's infrastructure webhook.

- **DEV-01** **[P2]** **[Missing]** — A file is saved in an editor.
- **DEV-02** **[P1]** **[Missing]** — A project build succeeds.
- **DEV-03** **[P1]** **[Missing]** — A build fails.
- **DEV-04** **[P1]** **[Missing]** — All selected tests pass.
- **DEV-05** **[P1]** **[Missing]** — A new failing test appears.
- **DEV-06** **[P2]** **[Missing]** — A previously failing test passes for the first time.
- **DEV-07** **[P1]** **[Missing]** — The linter stops reporting warnings.
- **DEV-08** **[P1]** **[Missing]** — A local commit is created.
- **DEV-09** **[P2]** **[Missing]** — The current Git branch changes.
- **DEV-10** **[P2]** **[Missing]** — The last merge conflict is resolved.
- **DEV-11** **[P2]** **[Missing]** — The user's pull request is opened, approved or merged, with separate patterns.
- **DEV-12** **[P2]** **[Missing]** — CI finishes with a selected result.
- **DEV-13** **[P2]** **[Missing]** — Deployment of the user's application completes.
- **DEV-14** **[P2]** **[Missing]** — A terminal command finally finishes after running beyond a threshold.
- **DEV-15** **[P1]** **[Missing]** — A selected alert fires or resolves in the user's monitoring system.

## 26. AI tools and agents — AI

Source: Available adapter, local log, webhook or UI-state recognition. These ideas do not promise that Codex or ChatGPT expose these events.

- **AI-01** **[P2]** **[Missing]** — Response generation starts.
- **AI-02** **[P2]** **[Missing]** — Response generation finishes.
- **AI-03** **[P2]** **[Missing]** — An agent requests user action.
- **AI-04** **[P2]** **[Missing]** — An agent completes its current task.
- **AI-05** **[P2]** **[Missing]** — An agent stops with a tool error.
- **AI-06** **[P2]** **[Missing]** — A new artifact file is created.
- **AI-07** **[P2]** **[Missing]** — Image generation finishes.
- **AI-08** **[P2]** **[Missing]** — Audio transcription finishes.
- **AI-09** **[P2]** **[Missing]** — A response exceeds a selected word count or other available counter.
- **AI-10** **[P2]** **[Missing]** — A response contains a selected word or phrase.
- **AI-11** **[P2]** **[Missing]** — A task completes N successful steps in a row.
- **AI-12** **[P2]** **[Missing]** — An agent changes status from working to waiting.
- **AI-13** **[P3]** **[Missing]** — All tasks in a selected group complete.
- **AI-14** **[P2]** **[Missing]** — A long operation has no progress events for N seconds.
- **AI-15** **[P2]** **[Missing]** — A person accepts a proposed change or marks a result helpful, if the application exposes that signal.

## 27. Work, study and personal tasks — TASK

Source: Calendar, task tracker, editor or study-app adapter; some timers are local.

- **TASK-01** **[P2]** **[Missing]** — A task is marked complete.
- **TASK-02** **[P2]** **[Missing]** — The final task for today is closed.
- **TASK-03** **[P2]** **[Missing]** — A card moves to a selected board column.
- **TASK-04** **[P1]** **[Missing]** — A focused-work interval begins.
- **TASK-05** **[P1]** **[Missing]** — A Pomodoro or other work interval finishes.
- **TASK-06** **[P1]** **[Missing]** — A scheduled break ends.
- **TASK-07** **[P2]** **[Missing]** — N minutes remain before a selected meeting.
- **TASK-08** **[P2]** **[Missing]** — A meeting is cancelled or rescheduled.
- **TASK-09** **[P2]** **[Missing]** — N tasks have been closed today.
- **TASK-10** **[P2]** **[Missing]** — A document reaches a selected word count.
- **TASK-11** **[P2]** **[Missing]** — N words are written in one session.
- **TASK-12** **[P2]** **[Missing]** — A daily learning goal is completed.
- **TASK-13** **[P2]** **[Missing]** — A spaced-repetition card is answered correctly.
- **TASK-14** **[P2]** **[Missing]** — A streak of correct answers without hints is reached.
- **TASK-15** **[P2]** **[Missing]** — A study session ends after all planned exercises are completed.

## 28. Spreadsheets, data and dashboards — DATA

Source: Excel/Google Sheets adapter, exported file or a personal data stream.

- **DATA-01** **[P2]** **[Missing]** — A selected cell's value changes.
- **DATA-02** **[P2]** **[Missing]** — A cell crosses a numeric threshold.
- **DATA-03** **[P2]** **[Missing]** — A formula recalculates to a selected value.
- **DATA-04** **[P2]** **[Missing]** — A formula calculation error appears.
- **DATA-05** **[P2]** **[Missing]** — All errors on a selected sheet are resolved.
- **DATA-06** **[P2]** **[Missing]** — A row is added to a table.
- **DATA-07** **[P2]** **[Missing]** — A row receives a selected status.
- **DATA-08** **[P2]** **[Missing]** — A checkbox in a selected cell is checked.
- **DATA-09** **[P2]** **[Missing]** — The sum of a range reaches a target value.
- **DATA-10** **[P2]** **[Missing]** — All required fields are filled in.
- **DATA-11** **[P2]** **[Missing]** — A connected dataset updates.
- **DATA-12** **[P2]** **[Missing]** — A data import completes.
- **DATA-13** **[P2]** **[Missing]** — A quality check finds a duplicate or missing value.
- **DATA-14** **[P2]** **[Missing]** — A metric on the user's dashboard sets a new session high.
- **DATA-15** **[P2]** **[Missing]** — Two independent numeric series cross.

## 29. Creative applications — ART

Source: Plugin/adapter for Blender, Figma, Photoshop, a video editor, DAW or another selected tool.

- **ART-01** **[P2]** **[Missing]** — An image or animation render finishes.
- **ART-02** **[P2]** **[Missing]** — Rendering reaches a selected percentage.
- **ART-03** **[P2]** **[Missing]** — Video export finishes.
- **ART-04** **[P2]** **[Missing]** — A new video-editing marker is placed.
- **ART-05** **[P2]** **[Missing]** — The timeline playhead crosses a marked frame.
- **ART-06** **[P2]** **[Missing]** — A new layer is created.
- **ART-07** **[P2]** **[Missing]** — Several layers are merged.
- **ART-08** **[P2]** **[Missing]** — N undo operations occur in a row.
- **ART-09** **[P1]** **[Missing]** — A brush stroke exceeds a pressure threshold.
- **ART-10** **[P2]** **[Missing]** — The brush or selected object's color enters a selected palette.
- **ART-11** **[P2]** **[Missing]** — A 3D scene's polygon count crosses a milestone.
- **ART-12** **[P2]** **[Missing]** — A physics simulation finishes calculating.
- **ART-13** **[P2]** **[Missing]** — The next bar or musical section begins in a DAW.
- **ART-14** **[P2]** **[Missing]** — A selected audio channel's level reaches a selected mark.
- **ART-15** **[P2]** **[Missing]** — A comment is added or a discussion resolved in the user's design file.

## 30. Smart home and physical buttons — HOME

Source: Explicitly connected home hub, MQTT, sensor or button. These are input signals, not home-control commands.

- **HOME-01** **[P2]** **[Missing]** — A wireless button is pressed.
- **HOME-02** **[P2]** **[Missing]** — A button receives a double or long press.
- **HOME-03** **[P2]** **[Missing]** — The lights turn on in a selected room.
- **HOME-04** **[P2]** **[Missing]** — Lamp brightness crosses a selected percentage.
- **HOME-05** **[P2]** **[Missing]** — A lamp changes to a selected color.
- **HOME-06** **[P2]** **[Missing]** — A door sensor switches to open.
- **HOME-07** **[P2]** **[Missing]** — A motion sensor triggers in a selected room.
- **HOME-08** **[P2]** **[Missing]** — A presence sensor triggers after a long period without movement.
- **HOME-09** **[P2]** **[Missing]** — Room temperature enters a selected range.
- **HOME-10** **[P2]** **[Missing]** — Humidity crosses a threshold.
- **HOME-11** **[P2]** **[Missing]** — Room illumination changes sharply.
- **HOME-12** **[P2]** **[Missing]** — A smart plug detects an appliance cycle ending from its power consumption.
- **HOME-13** **[P2]** **[Missing]** — A washing machine reports program completion.
- **HOME-14** **[P2]** **[Missing]** — A robot vacuum returns to its dock.
- **HOME-15** **[P2]** **[Missing]** — A coffee machine or kettle reports completion.

## 31. Phone and personal devices — PHONE

Source: Companion app, authorized phone automation or device adapter. OS restrictions vary.

- **PHONE-01** **[P2]** **[Missing]** — A remote-control button is pressed on the phone.
- **PHONE-02** **[P2]** **[Missing]** — A slider moves on the remote-control screen.
- **PHONE-03** **[P3]** **[Missing]** — The phone is shaken.
- **PHONE-04** **[P3]** **[Missing]** — The phone tilts relative to its initial position.
- **PHONE-05** **[P3]** **[Missing]** — The phone is turned face down.
- **PHONE-06** **[P2]** **[Missing]** — The phone is placed on charge.
- **PHONE-07** **[P2]** **[Missing]** — Phone battery reaches a selected percentage.
- **PHONE-08** **[P2]** **[Missing]** — A purpose-built phone automation runs.
- **PHONE-09** **[P2]** **[Missing]** — The phone scans an NFC tag containing a profile ID.
- **PHONE-10** **[P2]** **[Missing]** — The phone connects to the home Wi-Fi network.
- **PHONE-11** **[P2]** **[Missing]** — A companion app detects a selected nearby Bluetooth beacon.
- **PHONE-12** **[P2]** **[Missing]** — An event-confirmation button is pressed on a watch.
- **PHONE-13** **[P2]** **[Missing]** — A selected gesture is drawn on the phone remote.
- **PHONE-14** **[P2]** **[Missing]** — A phone timer finishes.
- **PHONE-15** **[P2]** **[Missing]** — A paired device returns within communication range after an absence.

## 32. Movement and wearable sensors — BODY

Source: Opt-in sensors or camera. Readings are game inputs without medical interpretation.

- **BODY-01** **[P2]** **[Missing]** — A step counter reaches a selected milestone.
- **BODY-02** **[P2]** **[Missing]** — N steps are recorded within a selected window.
- **BODY-03** **[P2]** **[Missing]** — Step cadence enters a selected range.
- **BODY-04** **[P3]** **[Missing]** — A sensor detects a transition from sitting to standing.
- **BODY-05** **[P3]** **[Missing]** — The user performs a selected stretching gesture in an opt-in game.
- **BODY-06** **[P2]** **[Missing]** — A wearable sensor's tilt angle crosses a threshold.
- **BODY-07** **[P2]** **[Missing]** — Pressure on a separate handheld sensor exceeds a threshold.
- **BODY-08** **[P2]** **[Missing]** — A sequence of taps is performed on a sensor.
- **BODY-09** **[P2]** **[Missing]** — Current heart rate enters a user-defined range, when telemetry is available.
- **BODY-10** **[P1]** **[Missing]** — A manually started breathing-timer session finishes.
- **BODY-11** **[P3]** **[Missing]** — A movement coincides with a selected musical beat.
- **BODY-12** **[P3]** **[Missing]** — A selected interval passes between two hand raises.
- **BODY-13** **[P3]** **[Missing]** — A tracked body point moves between two frame regions.
- **BODY-14** **[P3]** **[Missing]** — A streak of N recognized identical movements is reached.
- **BODY-15** **[P2]** **[Missing]** — A wearable reports completion of a selected activity.

## 33. Weather, environment and open data — WORLD

Source: Selected external service or personal sensor. Polling adds delay; these are ideas, not current world facts.

- **WORLD-01** **[P2]** **[Missing]** — A local sensor or weather source reports rain starting.
- **WORLD-02** **[P2]** **[Missing]** — Precipitation stops.
- **WORLD-03** **[P2]** **[Missing]** — Outdoor temperature crosses a selected mark.
- **WORLD-04** **[P2]** **[Missing]** — Wind speed enters a selected range.
- **WORLD-05** **[P2]** **[Missing]** — Cloud cover crosses above or below a threshold.
- **WORLD-06** **[P1]** **[Missing]** — Calculated sunrise occurs at a selected location.
- **WORLD-07** **[P1]** **[Missing]** — Sunset occurs.
- **WORLD-08** **[P2]** **[Missing]** — Pressure changes by a selected amount within a time window.
- **WORLD-09** **[P2]** **[Missing]** — A selected source changes its air-quality category.
- **WORLD-10** **[P2]** **[Missing]** — A selected sports match reports a scoring event.
- **WORLD-11** **[P2]** **[Missing]** — A selected team's match finishes.
- **WORLD-12** **[P2]** **[Missing]** — A new entry appears in a selected RSS feed.
- **WORLD-13** **[P2]** **[Missing]** — A package changes status in the user's delivery tracking system.
- **WORLD-14** **[P2]** **[Missing]** — A selected route feed reports an approaching vehicle while the user watches from home.
- **WORLD-15** **[P2]** **[Missing]** — A public scientific stream publishes a new measurement of a selected type.

## 34. Time, schedules and missing events — TIME

Source: Local or derived; distinguish calendar and interval timers.

- **TIME-01** **[P1]** **[Missing]** — A particular time of day arrives.
- **TIME-02** **[P0]** **[Done]** — Another interval elapses since the session began.
- **TIME-03** **[P1]** **[Missing]** — A selected weekday arrives.
- **TIME-04** **[P1]** **[Missing]** — A selected date or anniversary arrives.
- **TIME-05** **[P1]** **[Missing]** — Clock hours and minutes match, such as 12:12.
- **TIME-06** **[P1]** **[Missing]** — N seconds remain before a planned event.
- **TIME-07** **[P1]** **[Missing]** — A timer with a bounded random duration finishes.
- **TIME-08** **[P1]** **[Partial]** — Event A has not occurred for a selected duration.
- **TIME-09** **[P1]** **[Missing]** — B does not occur within N seconds after A.
- **TIME-10** **[P1]** **[Partial]** — The first event of a selected type occurs after a pause.
- **TIME-11** **[P1]** **[Missing]** — Every Nth work interval completes.
- **TIME-12** **[P1]** **[Missing]** — Total enabled-profile time reaches a milestone.
- **TIME-13** **[P1]** **[Missing]** — No erroneous action is reported by an application during a selected period.
- **TIME-14** **[P1]** **[Partial]** — A selected interval elapses after the previous effect ends.
- **TIME-15** **[P1]** **[Missing]** — A scheduled activity window opens or closes.

## 35. Counters, mathematics and randomness — MATH

Source: Derived. Random rules need a clear sampling rate and trigger limit.

- **MATH-01** **[P1]** **[Missing]** — Every Nth event of a selected type occurs.
- **MATH-02** **[P1]** **[Missing]** — The event count becomes a prime number.
- **MATH-03** **[P1]** **[Missing]** — The counter reaches the next Fibonacci number.
- **MATH-04** **[P1]** **[Missing]** — The counter becomes a power of two.
- **MATH-05** **[P1]** **[Missing]** — The counter's final digits match a selected combination.
- **MATH-06** **[P1]** **[Missing]** — A virtual die rolls a selected value.
- **MATH-07** **[P1]** **[Missing]** — A probability filter admits an event with chance p.
- **MATH-08** **[P1]** **[Missing]** — After failed rolls, an increased but capped probability produces a success.
- **MATH-09** **[P1]** **[Missing]** — Two randomly selected symbols match.
- **MATH-10** **[P1]** **[Missing]** — A virtual deck draws a selected card without repeats until reshuffling.
- **MATH-11** **[P1]** **[Missing]** — A metric's windowed average crosses a threshold.
- **MATH-12** **[P1]** **[Missing]** — A metric's rate of change exceeds a threshold.
- **MATH-13** **[P1]** **[Missing]** — A value deviates from its local average by more than a selected amount.
- **MATH-14** **[P1]** **[Missing]** — The weighted sum of different events reaches a goal.
- **MATH-15** **[P2]** **[Missing]** — Two event streams coincide within a timing tolerance.

## 36. Cross-application events — MIX

Source: Derived from explicitly enabled adapters.

- **MIX-01** **[P2]** **[Missing]** — Game damage is received while a selected music track plays.
- **MIX-02** **[P1]** **[Missing]** — Tests pass while the IDE remains the active window.
- **MIX-03** **[P2]** **[Missing]** — A selected contact sends a message while an allowed profile is active.
- **MIX-04** **[P2]** **[Missing]** — A website opens after N minutes of work in an editor.
- **MIX-05** **[P3]** **[Missing]** — The player wins immediately after a musical chorus ends.
- **MIX-06** **[P3]** **[Missing]** — Three different applications produce events within a minute.
- **MIX-07** **[P3]** **[Missing]** — A rare game item drops while chat sends a selected reaction.
- **MIX-08** **[P2]** **[Missing]** — The user starts typing quickly immediately after an AI response finishes.
- **MIX-09** **[P3]** **[Missing]** — A render finishes while the user is at their desk.
- **MIX-10** **[P2]** **[Missing]** — A timer fires while a selected application is silent.
- **MIX-11** **[P1]** **[Missing]** — A pedal is pressed while a selected window is open.
- **MIX-12** **[P3]** **[Missing]** — A screen marker appears at the same time as a particular sound.
- **MIX-13** **[P2]** **[Missing]** — A task, study card and work interval finish in any order.
- **MIX-14** **[P3]** **[Missing]** — Two explicitly connected participants press buttons within a shared time window.
- **MIX-15** **[P2]** **[Missing]** — A loss streak ends in victory without a profile switch.

## 37. Device and application state — TOY

Source: Application internals and supported device telemetry. Some events naturally pause, switch profile or stop.

- **TOY-01** **[P1]** **[Partial]** — A device is discovered for the first time this session.
- **TOY-02** **[P1]** **[Partial]** — Connection to a selected device is confirmed.
- **TOY-03** **[P2]** **[Partial]** — Device battery crosses a selected mark, if available.
- **TOY-04** **[P2]** **[Partial]** — Command completion is acknowledged, if the transport provides it.
- **TOY-05** **[P1]** **[Partial]** — A local pattern timer expires.
- **TOY-06** **[P1]** **[Missing]** — N patterns have completed this session.
- **TOY-07** **[P1]** **[Missing]** — A sequence of several effects finishes.
- **TOY-08** **[P1]** **[Partial]** — The user manually switches profiles.
- **TOY-09** **[P0]** **[Partial]** — The user enables or pauses rules.
- **TOY-10** **[P2]** **[Missing]** — An external event source loses connectivity.
- **TOY-11** **[P1]** **[Partial]** — Toy connection state becomes uncertain.
- **TOY-12** **[P0]** **[Partial]** — The user presses the global stop button.
- **TOY-13** **[P1]** **[Missing]** — A user-defined session activity limit is reached.
- **TOY-14** **[P2]** **[Partial]** — Two rules request incompatible effects simultaneously.
- **TOY-15** **[P2]** **[Missing]** — The effect queue empties after an accumulated series.

## 38. Absurd digital rituals — ODD

Source: Local, derived or selected-app adapter. Deliberately playful ideas.

- **ODD-01** **[P1]** **[Missing]** — The pointer visits all four screen corners clockwise: a summoning ritual.
- **ODD-02** **[P1]** **[Missing]** — The pointer lands on one preselected pixel: find the needle.
- **ODD-03** **[P2]** **[Missing]** — The user opens and closes the same tab three times: reconsidering the reconsideration.
- **ODD-04** **[P2]** **[Missing]** — The tab count becomes 42: the answer is found.
- **ODD-05** **[P1]** **[Missing]** — The Recycle Bin contains exactly 13 files: digital mysticism.
- **ODD-06** **[P1]** **[Missing]** — A saved filename contains final_final or a user-defined equivalent.
- **ODD-07** **[P2]** **[Missing]** — Three consecutive exclamation marks appear in a document.
- **ODD-08** **[P2]** **[Missing]** — An emoji is inserted that did not occur in the previous N text events.
- **ODD-09** **[P2]** **[Missing]** — The keyboard layout switches back and forth without a character typed between switches.
- **ODD-10** **[P2]** **[Missing]** — The user presses Ctrl+S repeatedly even though the file is already saved.
- **ODD-11** **[P1]** **[Missing]** — The mouse moves without a single click for five minutes: the philosopher pointer.
- **ODD-12** **[P1]** **[Missing]** — Rounded compilation time equals a selected lucky number.
- **ODD-13** **[P1]** **[Missing]** — CPU load and battery charge happen to display the same integer.
- **ODD-14** **[P2]** **[Missing]** — Two consecutively created tasks begin with the same letter.
- **ODD-15** **[P2]** **[Missing]** — A palindrome is typed in a selected editor: never odd or even.

## 39. Absurd coincidences with the world — COSMIC

Source: Derived, sensors or recognition; accuracy is limited by the source.

- **COSMIC-01** **[P3]** **[Missing]** — A cat appears in frame exactly when a build succeeds: cat approved.
- **COSMIC-02** **[P3]** **[Missing]** — The kettle finishes at the same time as a game level.
- **COSMIC-03** **[P3]** **[Missing]** — Rain starts outdoors while the word rain is sung in a track.
- **COSMIC-04** **[P2]** **[Missing]** — A track title contains a color matching a smart lamp.
- **COSMIC-05** **[P2]** **[Missing]** — A robot vacuum docks while the character returns to town.
- **COSMIC-06** **[P2]** **[Missing]** — Air temperature matches the current game level number.
- **COSMIC-07** **[P3]** **[Missing]** — Handheld sensor pressure rises when subtitles say press.
- **COSMIC-08** **[P3]** **[Missing]** — A bird chirps after an all-done message.
- **COSMIC-09** **[P2]** **[Missing]** — A real door opens while a virtual door opens on screen.
- **COSMIC-10** **[P1]** **[Missing]** — A virtual die rolls its maximum during a full moon.
- **COSMIC-11** **[P2]** **[Missing]** — Rounded room temperature equals the number of open tabs.
- **COSMIC-12** **[P3]** **[Missing]** — A mug appears in frame while a track with coffee in its title plays.
- **COSMIC-13** **[P3]** **[Missing]** — The last three digits of daily steps match the game score.
- **COSMIC-14** **[P3]** **[Missing]** — A meow is detected while a cat image is being viewed.
- **COSMIC-15** **[P1]** **[Missing]** — Sunset occurs as the user closes all work windows for the first time today.

## 40. Absurd mini-games and stories — QUEST

Source: Derived; a separate game layer stores scenario state.

- **QUEST-01** **[P1]** **[Missing]** — Treasure hunt: the user activates three secret screen regions in order.
- **QUEST-02** **[P2]** **[Missing]** — Desktop boss: damage from completed tasks reduces virtual boss health to zero.
- **QUEST-03** **[P1]** **[Missing]** — Lazy snail: an indicator reaches the finish while events arrive below a selected rate.
- **QUEST-04** **[P1]** **[Missing]** — Chaos goblin: a secret random condition chosen at session start is fulfilled.
- **QUEST-05** **[P2]** **[Missing]** — Destiny decided: the final digit of render duration matches a preselected digit.
- **QUEST-06** **[P2]** **[Missing]** — Music detective: the user guesses a track with a button before its chorus begins.
- **QUEST-07** **[P1]** **[Missing]** — Anti-spam ninja: a sequence of actions completes without unnecessary repeat presses.
- **QUEST-08** **[P2]** **[Missing]** — Collect the alphabet: a selected text exercise contains every letter in a chosen set.
- **QUEST-09** **[P1]** **[Missing]** — Window collector: the user visits the complete selected set of applications during a session.
- **QUEST-10** **[P2]** **[Missing]** — Diplomat: three peaceful game dialogue decisions are made in a row.
- **QUEST-11** **[P2]** **[Missing]** — Secret courier: an agreed code arrives from an allowed contact.
- **QUEST-12** **[P2]** **[Missing]** — Echo: the user reproduces a displayed rhythm with the mouse within a tolerance.
- **QUEST-13** **[P2]** **[Missing]** — Paradox of order: tasks A, B and C are completed in a randomly assigned order.
- **QUEST-14** **[P1]** **[Missing]** — One chance: the user presses a button in a narrow time window after a cue.
- **QUEST-15** **[P2]** **[Missing]** — Closing credits: all conditions of a selected evening quest are fulfilled.

## Making these ideas flexible

Do not turn 600 ideas into 600 special switches. Most scenarios combine a source, filters, reusable operators and an effect. Start with a simple form and reveal complex relationships in an advanced mode.

### Shared infrastructure difficulty

These estimates concern mechanisms assuming input data already exists; trigger ratings include data acquisition. This table groups UI, effects and debugging proposals and is not part of the 600-trigger count. Status reflects the original static audit.

| Mechanism | Level | Status | Main work / current evidence and gaps |
| --- | --- | --- | --- |
| Existing keys, combinations, sequences and foreground process | P0 | Done | WindowsInput → RuleEngine → FireAsync, selectable in UI |
| Events, states and state transitions | P1 | Partial | InputEvent, held/latched and individual threshold transitions exist; no general source-state model |
| Numeric stream with units, freshness and stop on missing data | P2 | Missing | Stream lifecycle, update rate and transport interaction; no general unit/TTL stream |
| Filters by source Id, sender, tag, number and string | P1 | Partial | Keys, Process, Threshold exist; need arbitrary fields, lists, comparisons and validated regex |
| Schedule, window, profile, repetition and origin filters | P1 | Partial | Foreground filtering and autorepeat suppression exist; no schedule/general source context |
| Recognition confidence and stability filters | P1 | Missing | Threshold and repeated-result confirmation; no detector inputs yet |
| AND, OR, NOT and K-of-N states | P1 | Partial | Event plus active-process filter exists; no configurable general logic or unknown-state semantics |
| Counters, streaks, every-Nth, holds and hysteresis | P1 | Partial | PressCount, CleanTypingStreak and holds exist; no general every-Nth or separate hysteresis thresholds |
| Mixed-event sequences, absence of B after A, delayed checks | P2 | Missing | Concurrent waits, cancellation/context; Sequence currently handles keys only |
| Local-process input coincidence | P1 | Partial | Chords/repeated-press timing exist; no general bounded-buffer temporal join |
| Independent remote clocks and fast streams | P3 | Missing | Latency, time uncertainty, reordering and late events |
| Randomness, no-repeat selection, accumulation and decay | P1 | Missing | Sampling frequency, generator state and resets |
| Average, rate of change and baseline deviation | P1 | Partial | Windowed typing rate exists; no arbitrary metric derivative/baseline handling |
| Fixed-stage state machine | P1 | Missing | Transitions, timeouts and completion for one scenario |
| General arbitrary-stage scenario builder | P2 | Missing | Scenario model, editor, validation and serialization |
| Linear, inverse, stepped, threshold, power and log mapping | P1 | Partial | RateMapped linearly maps Value/Threshold at trigger time; no general mapping editor |
| Normalization, dead zones, smoothing and rate limiting | P1 | Missing | Stateful transforms and configurable parameters |
| Visual arbitrary curve | P2 | Missing | Control points, interpolation, storage and preview; current EffectPreview is read-only |
| Independent sources for intensity and rhythm | P2 | Missing | Coordinated parameter updates without conflicting commands |
| Single action, existing pulse and manual stop | P0 | Done | Vibrate, Pulse and Stop in UI/transports |
| Short single/double/triple impulses, codes, waves, steps and fades | P2 | Partial | BLE single effects from 100 ms and pulse intervals from 150 ms exist; no finite-N/arbitrary-pattern editor |
| Select a pattern from a library | P1 | Missing | Choice parameters/history assuming a player; currently selects ActionKind, not stored custom patterns |
| Annotated track and timeline editor | P2 | Missing | Seek, start at position and cancellation; EffectPreview is not a timeline editor |
| Route events to selected device groups | P2 | Partial | ToyId selects one or all; no arbitrary groups/scenario routing/general partial-failure handling |
| Sequence effects across devices with timing tolerance | P2 | Missing | Multiple queues and coordinated cancellation |
| Precise device/music synchronization | P3 | Missing | Measure latency/jitter; no compensation or external music sync, accuracy is hardware-dependent |
| Existing priority, repeat interval and duration | P0 | Done | Priority, CooldownMs and DurationSeconds in engine/executor/UI |
| Queue, replace, mix and cap the combined result | P2 | Partial | Priority winner and per-toy BLE replacement exist; no general queue/mixing/policy editor |
| Burst aggregation, latest value, TTL and deduplication | P1 | Partial | Cooldown/autorepeat filtering exist; no burst aggregator, TTL or external event-ID deduplication |
| Loop prevention, pending-action cleanup and reconnect | P2 | Partial | generation invalidates stale sends, BLE cancels jobs; no origin graph/scenario queues/automatic source recovery |
| Remote session with allowed senders and expiry | P2 | Missing | Authorization, revocation, replay protection and session state |
| Select text/audio/video sources; store detector results only | P2 | Missing | Permissions/capture lifecycle; detector models are rated separately |
| Recipes and simple rule form | P1 | Done | Seven RulePresentation recipes, editor and profile persistence/import/export |
| Arbitrary nested-condition/scenario UI | P2 | Missing | Structural editing/validation; current editor handles one event with parameters |
| Readable description and why-not explanations | P2 | Partial | RulePresentation.Describe exists; no rejected-condition evaluation trace |
| Demo test of an existing rule | P0 | Done | DemoTransport/ObserveOnly; test button sends action, condition testing needs an event in observation |
| Event viewer and adapter states | P1 | Partial | Log shows matches/transport messages, not all inputs or every adapter's availability |
| Event recording and replay | P2 | Missing | Trace format, virtual time and reproducible random rules |
| General event schema, versions, IDs, units and unknowns | P1 | Partial | InputEvent has Kind, AtMs, Key, Process, Value and window context; no schema version/eventId/unit/confidence/unknown-value type |
| Local file input or wrapper for one own command | P1 | Missing | Clear format and result detection |
| Extensible HTTP/WebSocket/MQTT/OSC input and plugins | P2 | Missing | Adapter contracts, connections, errors and access boundaries; LocalApiTransport is outgoing toy control |

Shared mechanisms are implemented once. After a browser extension and counters exist, more tab rules can become small changes even though WEB's original from-scratch rating remains P2.

### 1. Separate events, states and numeric streams

| Type | Example | Use |
| --- | --- | --- |
| One-time event | Build completed | Start one pattern |
| State | IDE active | Gate another event |
| State transition | Health entered below 20% | Trigger once on entry |
| Numeric stream | Typing speed, volume | Smoothly change a parameter |
| Missing event | No message after A | Wait timer cancelled by B |
| Composite | A then B without C | Small state machine with clear reset |

Let a rule choose entry, exit, once-after-hold or periodic-while-true behavior. “Health below 20%” must not accidentally mean triggering every frame.

### 2. Reusable source filters

- Particular app, process, domain, tab, channel, folder, device or game character.
- Allowed sender, contact list, event type or tag.
- Active window, full-screen state, profile and allowed time of day.
- Equal/not equal, above/below, range or set membership.
- Text contains/does not contain, starts with or matches validated regex.
- Manual user actions only; ignore autorepeat/synthetic repeats where the source distinguishes them.
- New events only; separately configure reappearance after disappearance.
- Recognition region, minimum confidence and stability across frames.
- Numeric units, allowed range and maximum value age.

### 3. Composition operators

| Operator | Example |
| --- | --- |
| ALL / AND | Game active AND pedal pressed |
| ANY / OR | Render OR export completed |
| NONE / NOT | No call and not paused |
| Sequence | A → B → C within 10 seconds |
| Unordered collection | All three events arrive within a minute |
| K of N | Any 3 of 5 conditions |
| Temporal coincidence | Audio and marker within 200 ms |
| Window counter | 5 clicks in 2 seconds |
| Every Nth | Every tenth successful test run |
| Streak | 4 wins in a row; loss resets |
| Hold | Condition continuously true for 3 seconds |
| Delayed check | Check B five seconds after A |
| Absence | No B within ten seconds after A |
| Rate of change | Metric grows faster than X/s |
| Baseline deviation | Current speed 40% above five-minute average |
| Hysteresis transition | Enter above 70, rearm only below 60 |
| Probability | Admit 15% of matching events |
| No-repeat selection | Next pattern from those not yet played |
| Accumulation with decay | Events add points, inactivity subtracts |
| Scenario states | Waiting → preparation → active → completion |

Sequences should specify unrelated-event handling: ignore, reset or count an error. Counters need a lifetime: window, session, day or profile.

### 4. Map values to effects

Configure input values and output parameters independently. Typing speed could change pulse frequency, damage could set pulse count, and item rarity could select a stored pattern.

- Linear mapping between two points; inverse mapping within bounds.
- Stepped quiet/medium/active ranges; threshold-to-fixed-effect mapping.
- User curves with control points; logarithmic/power transforms for uneven data.
- Normalization against personal statistics with a lockable baseline.
- Smoothing and slew-rate limits to prevent jitter; neutral dead zone.
- Bounded random variation in intensity, interval or pattern choice.
- Energy accumulator: events add value, a timer gradually reduces it.
- Two-dimensional control: one source for intensity, another for rhythm.
- Route event types to different devices; simultaneous, alternating or wave-like timing.
- Stop stream-driven control automatically when values become stale.

Clamp parameters to device capabilities. Additional actuator channels join this model only after support is verified for that model and transport.

### 5. Effect palette

These are future pattern-editor proposals, not a list of implemented transport commands.

| Effect | Suitable events |
| --- | --- |
| Short single impulse | Click, hit, notification |
| Double/triple impulse | Success, rare item, special contact |
| Smooth ramp-up | Download, resource accumulation |
| Smooth fade-out | Phase ending, declining activity |
| Wave | Musical phrase or long interval |
| Rhythm with adjustable gaps | Typing speed, steps, beats |
| Short pulse code | Distinguish several applications |
| Steps | Combo milestones, process stages |
| Call and response | Two events or two devices |
| Annotated track playback | Video, music, scenario |
| Library selection | Random reward without repeats |
| Pause/stop | End session, leave application |

### 6. Conflicts and frequent events

Each rule needs priority, repeat interval, maximum effect length and restart policy. For simultaneous events, proposed choices are replace, bounded queue, merge, ignore or wait for completion. Merging must cap the combined result, not merely each individual rule.

Useful policies include one signal per burst, summarize ten notifications, latest-value-wins and dropping stale queued events. Event IDs must not replay after reconnect. Integrator's own actions must not create endless self-triggering loops.

Global Stop overrides every scenario and clears deferred starts. Connection recovery must not automatically replay an old queue. Remote-event sessions are explicitly enabled with selected senders and expiry. Text/audio/camera sources should be chosen explicitly and default to storing detector results rather than raw content.

## 30 profile recipes

These combine catalog items, not 30 new sources. Numbers are examples, not mandatory settings. Original audit: **2 Done, 4 Partial, 24 Missing**. A matching application preset name alone does not establish matching behavior.

| # | Level | Status | Name | Condition → response | Existing behavior / gap |
| --- | --- | --- | --- | --- | --- |
| 01 | P1 | Done | Typewriter | Physical keys in a selected editor → rate-limited short impulses | BLE KeyDown + process + ≥100 ms duration + cooldown; feedback preset 150/200 ms. REST rejects ≤1 s |
| 02 | P1 | Partial | Writer's rhythm | Five-second typing average → pattern rate follows tempo, fading on idle | Windowed rate/one-time intensity mapping exist; no continuous frequency/fade |
| 03 | P1 | Done | No corrections | 50 presses without Backspace → special pattern, correction resets | CleanTypingStreak + Pulse; change default 20 to 50; Backspace/Delete reset |
| 04 | P1 | Partial | Back to work | Return to IDE after ten minutes away → one recognizable cue | ActivityResumed + IDE filter measures no-input time, not time away from IDE |
| 05 | P1 | Missing | Green build | Build succeeds after failure → double instead of single pulse | No build source, transition history or conditional effect choice |
| 06 | P2 | Missing | Red turns green | Last failing test fixed → completion pattern | No individual-test adapter/state history |
| 07 | P3 | Missing | Agent finished | AI task completes while user present → signal now, otherwise chosen notification policy | No task-event source or presence detector |
| 08 | P2 | Missing | Render ready | Export finishes after more than a minute → summary pattern | No export-completion source or render-duration tracking |
| 09 | P2 | Missing | Learning streak | Five correct answers → next non-repeating pattern | No answer source or no-repeat library |
| 10 | P2 | Missing | Done for today | Last daily task closes → final pattern and end profile | No task-list/last-task condition |
| 11 | P3 | Missing | Musical pulse | Confident beat detection → synchronized impulses with tempo division | No audio capture, beat detector or output synchronization |
| 12 | P2 | Missing | Bass line | Bass energy above background → smoothed parameter control | No frequency analysis/continuous smoothing |
| 13 | P2 | Missing | Movie markers | Video reaches annotated times → prepared patterns | No player/timecode/pattern track |
| 14 | P2 | Missing | Phrase of the day | Selected subtitle word → once-per-phrase effect | No subtitle source/text filtering |
| 15 | P2 | Missing | Game damage | Damage in active game profile → amount selects short-pattern length | No damage telemetry or duration mapping |
| 16 | P2 | Missing | Perfect parry | Confirmed perfect parry → crisp double pulse | No parry event source |
| 17 | P2 | Missing | Rare trophy | Item of selected rarity → rarity-table pattern | No item events or rarity-pattern table |
| 18 | P2 | Missing | Comeback | Win after three losses → special signal and reset | No game outcomes/win-loss streak |
| 19 | P2 | Missing | Boss phases | New phase → selected reaction scheme | No boss-phase event or scheme-switch action |
| 20 | P2 | Missing | Rally | Speed/surface telemetry → speed sets rhythm, surface adds impulses | No simulator telemetry or stream/event mixing |
| 21 | P2 | Missing | Combo master | Combo reaches 10/25/50 → distinct short patterns | No combo source or milestone effect table |
| 22 | P2 | Missing | Personal postman | Selected contact in allowed hours → personal pulse code | No contact message source or allowed-time schedule |
| 23 | P2 | Missing | Viewers choose | Channel poll ends → winning allowed effect | No poll integration/result mapping |
| 24 | P3 | Missing | Cooperative button | Two participants press within three seconds → shared synchronized pattern | No remote controls, shared session or synchronization |
| 25 | P2 | Missing | Quiet lottery | Each event has 5% chance → random short pattern with daily cap | No probability filter, random library or daily cap |
| 26 | P1 | Partial | Secret code | Key sequence within four seconds → selected profile switch | Sequence/window exist; profile-switch action does not |
| 27 | P3 | Missing | Coffee quest | Coffee cycle and work task complete → one final pattern | No appliance/task sources or composite condition |
| 28 | P3 | Missing | Cat approved | Cat visible and tests green in one window → playful pattern | No cat detector or successful-test temporal join |
| 29 | P2 | Missing | Productivity boss | Tasks reduce virtual health → phase cues and final pattern at zero | No task integration, virtual health or stages |
| 30 | P2 | Partial | Automatic silence | Profile disabled, session locked or source stale → stop effects/clear queue | Manual pause stops output, BLE cancels on disconnect; no Windows-lock detection, external TTL or scenario queue |

## Freedom without an enormous form

Suggested UI levels:

1. **Quick recipe:** choose a scenario, application and pattern.
2. **Ordinary rule:** when an event occurs, if conditions match, perform an effect.
3. **Composite scenario:** multiple sources, sequences, counters and states.
4. **Custom source:** documented local input for a plugin or external automation.

A readable card could say: “When health first falls below 20% in the selected game, play a double pulse; rearm only above 30%.” Alongside it: Test event, Inspect input and Why did this not trigger?

Debugging needs observation without device commands, anonymized test-event recording/replay and availability indicators for every source. Unsupported adapters should clearly be unavailable, not silently appear active.

## Proposed general event schema

Conceptual example for discussion, **not the project's existing API schema**:

```json
{
  "schemaVersion": 1,
  "eventId": "session-42:event-123",
  "source": "game-adapter",
  "type": "player.health.changed",
  "occurredAt": "2026-10-04T18:30:00Z",
  "sessionId": "session-42",
  "subject": "local-player",
  "value": 18,
  "previousValue": 24,
  "unit": "percent",
  "confidence": 1.0,
  "tags": ["game", "health"],
  "data": { "game": "selected-game" }
}
```

Sources publish meaningful events; the central engine decides what to do. Hold durations/timeouts use local monotonic receive time, while calendar time serves schedules/display. Remote/polled sources need allowed latency and late-event policy. Missing data differs from zero.

The same player.health.changed event can signal a drop, select a pattern when entering a range or participate in a larger scenario. New apps then need not rewrite all rule logic.

## Directions to discuss first

This is an order for discussion, not an instruction to change the implementation.

| Direction | Value |
| --- | --- |
| General filters, counters, sequences and missing events | Multiply existing-source possibilities |
| Pattern editor and independent parameter mapping | More variety from the same inputs |
| Local event input with simple sender examples | User scripts and programs |
| File/process/command completion | Practical work scenarios |
| Browser extension | Tabs, sites and selected page elements |
| Audio analysis | Music, volume, rhythm and sound cues |
| One game adapter with rich telemetry | Exercise damage, resources, combos and phases |
| OBS, chat and home hub | Scenarios beyond the computer; Discord remains cancelled unless requested anew |
| OCR/visual detectors | Apps without suitable event APIs, allowing for recognition errors |
| Quests and absurd coincidences | Unusual profiles built from established sources |

The catalog's value comes from how users combine sources over time, apply context and turn events into varied patterns, not merely from the source count.
