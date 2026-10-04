# AI instructions: Lovense Integrator keyboard assignments

Create JSON for import through Lovense Integrator's Keyboard mode. Return one valid UTF-8 JSON file, or one JSON block without comments if attachments are unavailable. Do not invent fields, events or features.

## Format

The root object contains exactly `Format: "LovenseIntegrator.Keyboard"`, `Version: 1` and a non-empty `Rules` array. This is a keyboard bundle, not the application's version 3 full profile. Field names are case-sensitive; unknown and duplicate fields are rejected.

Every rule requires these fields:

| Field | Type and value |
| --- | --- |
| Id | Unique nonzero UUID. Preserve the exported Id when editing; generate a new UUID for an independent assignment. |
| Name | Clear, non-empty name. |
| Enabled | `true` or `false`. |
| Event | Always `"KeyDown"`. |
| KeyboardLayer | Always `true`. |
| Keys | Comma-separated codes: `"W,A,S,D"` means any of these keys, not a combination. `""` means all keys. |
| ExcludedKeys | Comma-separated exclusions, only when `Keys: ""`. Otherwise exactly `""`. |
| Action | Only `"Vibrate"`, `"Pulse"` or `"Stop"`. |
| Intensity | Integer 0–20. Do not exceed the user's requested intensity; use 5 for ordinary feedback. |
| DurationSeconds | Number 0.1–300 in **seconds**: 150 ms = `0.15`. Use a decimal point. |
| CooldownMs | Integer 100–3600000 in **milliseconds**. Ordinary feedback: 200. |
| PulseMs | Integer 150–10000; pulse interval in ms. Keep 250 for Vibrate/Stop. |
| Process | Process name without a path, such as `"notepad"` or `"notepad.exe"`; `""` means any application. Do not guess an unknown process: ask for its name from the application list or use the supplied export. |
| WindowTitleContains | Case-insensitive substring of the active window title; `""` means any title. Not a regular expression. |
| ToyId | `""` means all connected toys, otherwise an exact Id from a supplied export. Never invent an Id or substitute a model name. |
| Priority | Integer, usually 0. Higher values win. |

Optional fields: `Threshold` (positive finite number, default 800) and `WindowMs` (integer 100–60000, default 2000). KeyDown does not use them; preserve them from exports. Current exports may also include MouseLayer=false, WheelContinuous=false and WheelIdleMs=150; preserve those keyboard defaults. All numbers must be JSON numbers, not strings. No field may be null. Even Stop must retain valid duration, intensity and interval values.

## Keys

Use Windows/WPF codes, not layout characters: `A`–`Z`, `D0`–`D9`, `F1`–`F12`, `Space`, `Return`, `Tab`, `Back`, `Escape`, `LeftCtrl`, `RightCtrl`, `LeftShift`, `RightShift`, `LeftAlt`, `RightAlt`, `Left`, `Right`, `Up`, `Down`, `Home`, `End`, `Insert`, `Delete`, `NumPad0`–`NumPad9`, `Add`, `Subtract`, `Multiply`, `Divide`, `Decimal`. Obtain other codes from an export or the full list appended by the AI instructions button. Do not substitute display labels such as Ctrl, Enter or Esc, or localized characters. Main Enter and NumPad Enter share `Return`.

Non-empty Keys must contain at least one supported key. An all-keys assignment cannot exclude the entire layout. Do not repeat keys. Combinations, holds, typing speed and release events require the Other events editor and are unsupported by this bundle.

## Overlapping assignments

First match key, application and title. Then choose: higher Priority → non-empty Process → non-empty WindowTitleContains → smaller Keys group → stable Id order. Do not use Id ordering to tune effects.

For general feedback plus stronger WASD in a game, equal Priority lets the game-specific Process filter win. Outside the game, general feedback applies. Excluding Escape in the general layer does not prevent another assignment from selecting Escape explicitly.

CooldownMs applies to the entire assignment, not each key. Presses during cooldown are skipped without falling back to a general assignment. Windows autorepeat is not a new press. Do not promise a distinct pulse on every rapid key press. A new effect replaces the previous one on that toy; other event types can also compete.

Effects up to and including 1 second require direct Bluetooth. Lovense Remote REST requires duration **greater than 1 second**. Do not promise exact physical motor latency.

## Import and export

Import adds new IDs and updates matching keyboard IDs. Missing assignments and other event types remain intact. Reimporting the same IDs does not create duplicates. An Id conflict with another event type rejects the whole import. Rules stay paused after import until the user enables them. Export contains saved values, not draft edits. Before import, the previous profile is backed up to `profile.json.before-keyboard-import.bak`.

## Example: general feedback and stronger WASD in Notepad

Notepad is only an example. Replace the process as requested. Generate new IDs for new independent assignments.

```json
{
  "Format": "LovenseIntegrator.Keyboard",
  "Version": 1,
  "Rules": [
    {
      "Id": "3056dd48-a313-41d3-862a-77b0b829d52a",
      "Name": "General feedback without Esc and Ctrl",
      "Enabled": true,
      "Event": "KeyDown",
      "KeyboardLayer": true,
      "Keys": "",
      "ExcludedKeys": "Escape,LeftCtrl,RightCtrl",
      "Action": "Vibrate",
      "Intensity": 5,
      "DurationSeconds": 0.15,
      "CooldownMs": 200,
      "PulseMs": 250,
      "Process": "",
      "WindowTitleContains": "",
      "ToyId": "",
      "Priority": 0
    },
    {
      "Id": "657e5a92-533e-4991-83eb-5a305d580d06",
      "Name": "Stronger WASD in Notepad",
      "Enabled": true,
      "Event": "KeyDown",
      "KeyboardLayer": true,
      "Keys": "W,A,S,D",
      "ExcludedKeys": "",
      "Action": "Vibrate",
      "Intensity": 8,
      "DurationSeconds": 0.15,
      "CooldownMs": 200,
      "PulseMs": 250,
      "Process": "notepad",
      "WindowTitleContains": "",
      "ToyId": "",
      "Priority": 0
    }
  ]
}
```

Before returning a file, verify JSON validity, unique IDs, key codes, time units and ranges. Return the keyboard bundle, without ApiUrl, executable instructions, code or invented integrations.
