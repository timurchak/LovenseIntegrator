# AI instructions: Lovense Integrator mouse assignments

Create one valid UTF-8 JSON file for Import in Mouse mode. Return a file or one JSON block. Do not add JSON comments or invent fields/features.

All categories use the same effect editor. Its Copy saved effect action makes an independent copy of Action, Intensity, DurationSeconds, PulseMs and ToyId only. JSON still stores these fields in each rule: do not add effect references or library IDs. The UI's exact duration field is milliseconds; DurationSeconds in JSON remains seconds.

Root fields: `Format: "LovenseIntegrator.Mouse"`, `Version: 1`, non-empty `Rules`. This is neither a full profile nor a keyboard export. Field names are case-sensitive. Unknown/duplicate fields, invalid values and duplicate IDs reject the entire import.

## Assignment fields

| Field | Requirement |
| --- | --- |
| Id | Unique nonzero UUID. Preserve exported IDs when editing; generate a new one for an independent assignment. |
| Name | Clear, non-empty string. |
| Enabled | Boolean true/false. |
| MouseLayer | Always true. |
| Event | Always `"MouseDown"`: with MouseLayer this contains both buttons **and** wheel directions. Do not change it to MouseWheel. |
| Keys | Comma-separated selected button/direction codes, or `""` for the whole mouse. |
| ExcludedKeys | Comma-separated exclusions only when Keys is empty; otherwise `""`. |
| Action | `"Vibrate"`, `"Pulse"` or `"Stop"`. WheelContinuous=true requires Vibrate. |
| Intensity | Integer 0–20, usually 5. Do not exceed the user's request. |
| DurationSeconds | Number 0.1–300 seconds; 100 ms = 0.1. Applies to buttons and single wheel effects. |
| CooldownMs | Integer 0–3600000 ms. Use 0 for the fastest response. Shared by the assignment, not per button. |
| PulseMs | Integer 150–10000 ms. Keep 250 for Vibrate/Stop. |
| WheelContinuous | Boolean. false: ordinary effect per wheel event. true: each event renews vibration; buttons still produce a single effect. |
| WheelIdleMs | Integer 100–1000 ms, usually 150. While scrolling ends this long after the last matching wheel event. |
| Process | Process name without a path, such as `"notepad"`; `""` means any application. Ask for an unknown name from the application list rather than guessing. |
| WindowTitleContains | Case-insensitive active-title substring; `""` means any title. Not regex. |
| ToyId | `""` for all connected toys, or an exact exported Id. Do not infer an Id from the model name. |
| Priority | Integer, usually 0. Higher priority wins. |

Optional fields: `KeyboardLayer: false`, `Threshold: 800` (positive finite number), `WindowMs: 2000` (integer 100–60000). These do not control mouse mode; preserve exported values. All other fields are required. Numbers must not be strings; use a decimal point and no nulls. Even Stop requires valid time values.

## Codes and behavior

Buttons: `Left`, `Right`, `Middle`, `X1`, `X2`. Wheel: `Up`, `Down`. Use exact codes, no duplicate entries. At least one input must remain selected. Separate assignments can give Up and Down different effects.

Matching order: higher Priority → Process specificity → WindowTitleContains specificity → smaller Keys group → stable Id. Choose the winner before cooldown; do not fall back to general feedback during its pause. Exclusions affect only their assignment. Layers override matching ordinary press/wheel rules; other event types may still compete.

With WheelContinuous=true, every wheel event renews WheelIdleMs regardless of CooldownMs. Buttons use cooldown normally. There is no wheel-stopped event: silence between Windows messages ends the effect. Motor/Bluetooth timing is not exact. Slow scrolling with gaps longer than WheelIdleMs will produce interruptions; increase it if requested.

Without WheelContinuous, each event starts a bounded DurationSeconds effect. Rapid events may merge because a new command replaces the old one. One Windows event can contain multiple wheel notches; do not promise one pulse per physical notch. A button press triggers once until release.

Live feedback and While scrolling require direct Bluetooth. REST supports ordinary effects **longer than 1 second** only. Renewal at the same intensity extends the local timer without another positive BLE command. Stop, a different effect and disconnection cancel the previous timer. A window change that prevents matching stops renewal; the last timer then ends the effect.

Import adds new IDs and updates matching mouse IDs while preserving other assignments. Conflicting IDs from keyboard/other events are rejected. The previous profile is saved to `profile.json.before-mouse-import.bak`; rules stay paused. Export uses saved values, not drafts. Test effect sends one bounded impulse, not indefinite scrolling.

## Example: button feedback and continuous wheel

```json
{
  "Format": "LovenseIntegrator.Mouse",
  "Version": 1,
  "Rules": [
    {
      "Id": "79ab5b8e-96da-4a36-ac83-433b38676120",
      "Name": "Five buttons — fast feedback",
      "Enabled": true,
      "MouseLayer": true,
      "Event": "MouseDown",
      "Keys": "Left,Right,Middle,X1,X2",
      "ExcludedKeys": "",
      "Action": "Vibrate",
      "Intensity": 5,
      "DurationSeconds": 0.1,
      "CooldownMs": 0,
      "PulseMs": 250,
      "WheelContinuous": false,
      "WheelIdleMs": 150,
      "Process": "",
      "WindowTitleContains": "",
      "ToyId": "",
      "Priority": 0
    },
    {
      "Id": "3bc47a25-fcd6-4d23-95b0-949bc08053ea",
      "Name": "Wheel — vibration while scrolling",
      "Enabled": true,
      "MouseLayer": true,
      "Event": "MouseDown",
      "Keys": "Up,Down",
      "ExcludedKeys": "",
      "Action": "Vibrate",
      "Intensity": 5,
      "DurationSeconds": 0.1,
      "CooldownMs": 0,
      "PulseMs": 250,
      "WheelContinuous": true,
      "WheelIdleMs": 150,
      "Process": "",
      "WindowTitleContains": "",
      "ToyId": "",
      "Priority": 0
    }
  ]
}
```

Verify JSON, time units, ranges and unique IDs before returning it. Do not modify assignments outside the user's request. Generate new UUIDs for new independent assignments.
