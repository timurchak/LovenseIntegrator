# Rules, visual modes and data

## Events and time

`EventKind` has 22 values: KeyDown, KeyUp, KeyHeld, DoublePress, Chord, Sequence, PressCount, TypingRateAbove, TypingRateBelow, InputIdle, MouseDown, MouseUp, MouseWheel, ForegroundChanged, Timer, KeyReleasedAfterHold, TriplePress, CleanTypingStreak, ActivityResumed, MouseDoubleClick, MouseHeld and ScreenEvent. ScreenEvent was appended, preserving all previous identities. Its semantic filter lives in `ScreenEventId`, never Keys. See [SCREEN.md](SCREEN.md).

Windows autorepeat is not a new physical press. Holds/combinations trigger once until release. Typing speed uses a sliding window; below-threshold detection waits for the first full window. Backspace/Delete reset CleanTypingStreak; modifiers/arrows do not contribute. InputIdle includes keys, buttons and wheel events, not pointer movement.

All four assignment categories use one editor. Exact effect duration and cooldown fields display milliseconds; duration shortcuts retain their explicit ms/s labels. Trigger thresholds/measurement windows and the pulse slider display seconds. In JSON, `DurationSeconds` is seconds; `WindowMs`, `CooldownMs`, `PulseMs` and `WheelIdleMs` are milliseconds. Simplifying the UI must not change existing field units.

Current ranges: intensity 0–20; duration 0.1–300 s; pulse interval 150–10000 ms; measurement window 100–60000 ms. Cooldown is 100–3600000 ms for ordinary/keyboard rules and 0–3600000 ms for MouseLayer. WheelIdleMs is 100–1000 ms. Full contracts are in `Rule.Validate`, `ToyCommand.Validate` and preset validators.

## Conflicts and actions

Ordinary rules are ordered by priority, then stable Id; one input does not trigger an arbitrary number of concurrent actions. Keyboard/mouse assignments additionally compare Process specificity, WindowTitleContains specificity and smaller Keys groups. A window-specific assignment therefore wins over a general one at equal Priority.

Select the assignment before applying its cooldown. Do not fall back to a weaker general effect while the winner is cooling down. Exclusions apply only to their assignment, not globally to other rules. Process and title-substring filters are case-insensitive.

New actions replace the previous effect on the selected toy; rapid pulses may merge. Empty ToyId means all connected devices. RateMapped computes intensity once at trigger time (`value / threshold * intensity`, capped at 20), not continuously. New effects are skipped while a command is being sent, rather than queued.

## Keyboard

The 104-key diagram uses Windows/WPF Key IDs, not typed text or per-language layout recognition. Main Enter and NumPad Enter share Return. Labels use an English keyboard layout. Selection, exclusions and saved-assignment indicators must remain visually distinct.

`KeyboardLayer=true`, `Event=KeyDown`, Actions Vibrate/Pulse/Stop. Empty Keys means all keys minus ExcludedKeys. Assignments override matching ordinary KeyDown rules; combinations, holds and other events still participate separately. Opening a compatible old KeyDown does not migrate it automatically; saving in the new mode converts it to a layer with the same Id.

The shared editor offers short duration shortcuts, exact duration input (including 120 ms typewriter feedback), and vibration/pulse/Stop choices. The former 2-second minimum was a prototype restriction, not an established Windows BLE limit. Do not restore it. Motor/radio timing has not been measured; a programmed pulse does not guarantee a distinct physical response to every key.

## Mouse and wheel

Five buttons: Left, Right, Middle, X1, X2. Two wheel directions: Up, Down. `MouseLayer=true`, `Event=MouseDown` is the assignment container for both buttons and wheel. Do not change Event to MouseWheel in a new layer export. Old ordinary MouseDown/MouseWheel rules are normalized when saved; their empty Keys must not unexpectedly expand from buttons to wheel or vice versa.

New mouse assignments use 100 ms with zero cooldown. WheelContinuous requires MouseLayer + Vibrate. Each matching wheel event renews the effect for WheelIdleMs (default 150), ignoring cooldown for renewal; selected buttons still use ordinary duration/cooldown. There is no wheel-stopped event: silence between Windows messages determines the stop. One Windows event may represent more than one physical wheel notch.

For renewal, RuleMatch sets RenewalId to Rule.Id. BleTransport cancels/replaces the stop timer without resending a positive level if the owner and intensity are unchanged. Stop, another effect and device disconnection cancel the timer. An old timer callback must never stop a newer effect. Losing the matching window stops renewal; the final timer ends the effect. Test effect sends a bounded impulse.

## Profiles and imports

| Format | Version / purpose |
| --- | --- |
| Profile (`Version`, `ApiUrl`, `Rules`) | Writes v3; reads v1/v2/v3; full import replaces the profile |
| `LovenseIntegrator.Keyboard` | v1, keyboard assignments only; merge by Id |
| `LovenseIntegrator.Mouse` | v1, mouse assignments only; merge by Id |
| `LovenseIntegrator.Screen` | v1, semantic Screen event assignments only; merge by Id |

Profiles live at `%LOCALAPPDATA%\LovenseIntegrator\profile.json`. ProfileStore writes a temporary `.tmp` then replaces the file. Read errors must not silently overwrite a damaged profile with defaults: MainViewModel disables autosave until recovery or explicit import. Translating the application does not rewrite user-authored rule names.

Preset import validates required, unknown and duplicate fields, UUIDs, input codes, ranges and Id conflicts with another event type. Do not assume full ProfileStore has the same strictness; it uses a separate, less restrictive deserializer. Validate the entire preset, prepare/persist the merged profile, then replace live rules. Missing assignments and other events are preserved; importing the same IDs twice does not create duplicates.

Mode import creates `profile.json.before-keyboard-import.bak`, `profile.json.before-mouse-import.bak` or `profile.json.before-screen-import.bak`. Each is a single replaceable backup, not a version history. Export uses saved rules rather than draft edits. Rules pause after import. Presets do not contain ApiUrl.

AI contracts: [keyboard](../KEYBOARD_AI_RULES.md), [mouse](../MOUSE_AI_RULES.md). Desktop.csproj embeds these files; the keyboard button appends the complete current key-code list. [Examples](../examples/keyboard-assignments.json) are checked by the harness. AI must preserve Id when editing an assignment and generate a new one for an independent assignment. Do not copy real Toy.Id values into shared examples.

## UI changes

`Controls/AssignmentEditorView` owns the shared name/enabled fields, trigger content slot, effect controls, target, conditions, preview and fixed Save/Test/Delete footer. Keyboard, Mouse, Screen and Other events supply only trigger content and list/import behavior. `IAssignmentEditorContext` is the common binding contract. Preserve visual selection and simple duration shortcuts. Show fields relevant to the selected event; ordinary setup must not require editing JSON. Diagrams, graphs, preset changes and recipes edit a draft and do not activate motors.

Copy saved effect copies only Action, Intensity, DurationSeconds, PulseMs and ToyId. It does not save or dispatch, and never changes trigger identity, inputs, filters, cooldown, priority or wheel renewal. These are independent copies, not a new linked effect-library format. Incompatible speed and wheel actions are filtered and rejected again at the copy boundary. Devices and manual controls live in separate tabs; Stop remains global.

Check minimum window size, scrolling to settings below the diagram, long labels, ru-RU/en-US numeric input, unavailable selected devices and recording cancellation on focus/rule changes. Global hooks and actual monitor DPI require separate checks; the offscreen harness does not prove those behaviors.
