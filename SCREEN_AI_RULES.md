# Screen event assignments

Generate a JSON bundle with Format `LovenseIntegrator.Screen`, Version `1`, and a nonempty Rules array. This is not a full profile or a keyboard/mouse bundle. Import merges by stable Id; preserve an Id when editing, generate a new UUID for an independent assignment. Never include real device IDs in examples.

Every rule must include: Id, Name, Enabled, Event (`ScreenEvent`), ScreenEventId, Action (`Vibrate`, `Pulse` or `Stop`), Intensity (0–20), DurationSeconds (0.1–300 seconds), PulseMs (150–10000 milliseconds), CooldownMs (100–3600000 milliseconds), Priority (integer; higher wins), ToyId (empty = all connected), Process (`Wow` or empty) and WindowTitleContains (empty = any title). Set Keys and ExcludedKeys empty, KeyboardLayer/MouseLayer/WheelContinuous false if included. Do not use RateMapped. Unknown and duplicate fields are rejected.

Supported ScreenEventId values:

| ID | Meaning |
| --- | --- |
| wow.combat.enter | Player entered combat |
| wow.combat.leave | Player left combat |
| wow.player.damage | Player received a damage notification |
| wow.player.heal | Player received a healing notification |
| wow.player.health_loss | Player health decreased (not damage dealt) |
| wow.player.low_health | Player crossed below 30% health; rearms above 35% |
| wow.player.dead | Player died |
| wow.player.resurrected | Player returned to life (not ghost release) |
| wow.encounter.start | Boss encounter started |
| wow.encounter.win | Boss defeated |
| wow.encounter.wipe | Boss attempt failed |
| wow.quest.complete | Quest turned in |
| wow.level.up | Level gained |

Combat notification and health events are conditional on WoW exposing non-secret values. Never claim complete combat-log access or damage-dealt support. No toy commands or intensities are transmitted by the addon. `wow.test` is reserved for preview-only diagnostics and cannot be assigned. A rule receives one semantic event, not a raw color. Each input selects at most one rule by priority and stable Id. Cooldown suppresses repeated matches; effects replace earlier effects.

Reading is opt-in. Rules remain paused at launch, import, connection refresh and signal loss. Start reading, switch to WoW, inspect events, then explicitly enable rules (observation mode can suppress automatic commands). On foreground loss or stale heartbeat the active Screen rules pause; recovery does not resume them. Full profiles retain version 3 and readers still accept v1/v2/v3. Screen capture coordinates are local machine settings, not bundle data.

Use the example `examples/screen-assignments.json` as a complete minimal structure. Keep user names in their requested language; never translate IDs or JSON field names.
