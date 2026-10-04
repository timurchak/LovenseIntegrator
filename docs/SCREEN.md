# Screen mode and WowScreenEvents

Screen mode reads a small color block in the foreground WoW Retail client and turns it into named input events. It does not send input to WoW, read process memory, OCR the desktop, or let the addon specify toy commands. Select an event in Screen, then configure an effect and target in the shared assignment editor; save the assignment. Signal settings are inside the trigger section, while Save/Test/Delete remain in the fixed footer. The existing engine/executor applies priority, cooldown, observation mode, Stop and command cancellation.

## Setup

1. Install the optional, separately distributed `WowScreenEvents` addon; it is not included in the application installer. Reload WoW. `/wse show` displays the block in the top-left corner.
2. In Screen, expand **Signal setup and recent events**. Use X=0, Y=0 and Cell=4 by default. Coordinates are physical pixels relative to the WoW **client** area, so moving the window or changing monitors does not require desktop offsets. The grid is 8×8 cells (default 32×32 pixels). `/wse size 1` produces an actual 8×8-pixel block; larger cells are more robust. `/wse position X Y` changes its client offset; match it in the app.
3. Click Start reading and switch to WoW in windowed/borderless mode. The app shows a nearest-neighbor live preview, connection status and the last 30 semantic events. `/wse test` is preview-only even when rules are enabled.
4. Configure/save an assignment and explicitly Enable rules. The first packet is a baseline; only subsequent events can trigger. Observation mode logs matches without automatic commands. No binding exists until the user saves one or imports a bundle.

The reader polls a bounded rectangle at 20 Hz on a background task, checks foreground identity again after capture and dispatches to WPF. It holds no full desktop image and writes no screenshots. Settings live beside the profile in `screen.json`; capture never starts automatically. Color management/HDR, overlays covering the block, exclusive fullscreen and unsupported capture paths can prevent decoding. A checksum failure is not an event.

After a live session has been established, leaving WoW pauses rules immediately; missing/invalid/frozen signal pauses within approximately one second. Stop invalidates pending samples and commands. Reconnection/reload never reenables rules. Old events while paused or on reader startup are baselined and discarded. An event must be consistent across two samples; heartbeat changes establish liveness. Safety is bounded by the UI dispatcher and Windows scheduling, not a real-time guarantee.

## Events and limitations

The permanent IDs are in [SCREEN_AI_RULES.md](../SCREEN_AI_RULES.md). Damage/healing notifications use non-secret `UNIT_COMBAT` type values. WOUND notifications can include fully absorbed/blocked hits; use health loss when readable actual health reduction is required. Health loss and low health use readable `UNIT_HEALTH` values; health loss carries rounded percentage points (minimum 1), not a damage amount. Low health triggers below 30%, rearming above 35%; initial low health is only baselined. WoW secret values are detected before comparisons/arithmetic and skipped. The connected status reports a restricted-data flag. No combat-log fallback, secret-value extraction, damage-dealt event, spell identity or target/player identity is implemented. Actual availability depends on client/game context.

Events are best-effort, not a lossless combat log: the addon coalesces each frequent event within 250 ms, keeps at most 16 pending distinct notifications and expires queued entries after 750 ms. Each event is visible for at least 250 ms. `/wse status` reports dropped entries. Slow/minimized/covered clients may lose events; the reader never replays a backlog.

## Wire v1

8×8 cells in row-major order, each RGB channel one bit (0 or 255), MSB first. The desktop samples cell centers, rejecting channel values between 73 and 182 inclusive. 64×3 bits = 24 bytes. No colors or codes are exposed as configuration choices for ordinary users.

| Bytes (zero based) | Meaning |
| --- | --- |
| 0–2 | ASCII WSE (87,83,69) |
| 3 | Version 1 |
| 4–7 | Unsigned session counter, big endian; changes on reload/show/world entry |
| 8–9 | Unsigned event sequence, big endian, wraps at 65536 |
| 10 | Event: 0 idle; 1–14 correspond to ScreenEvents.All in order |
| 11 | Flags: bit 0 test, bit 1 restricted data; all other bits zero |
| 12–13 | Event value, unsigned big endian (health percentage points where applicable) |
| 14–15 | Heartbeat, unsigned big endian, advances every 100 ms while rendered |
| 16–21 | Reserved, zero |
| 22–23 | CRC-16/CCITT-FALSE of bytes 0–21, polynomial 0x1021, init 0xFFFF, big endian |

CRC guards accidental corruption, not malicious spoofing. The frame is local visual telemetry, not an authenticated network protocol. Test flags are always ignored for actions. Unknown versions/codes/flags and nonzero reserved bytes are rejected. C# and Lua share golden vectors tested independently.

## Files and verification

`ScreenProtocol.cs` contains the portable catalog, decoder and lifecycle tracker. `ScreenCapture.cs` contains bounded GDI capture; `ScreenModeViewModel` owns preview, liveness and dispatch. `ScreenPresetStore` provides strict v1 mode bundles with atomic merged-profile persistence and `before-screen-import.bak`. Full profiles remain v3; other mode imports preserve Screen rules.

Run `scripts/Build.ps1`, `scripts/Test-Desktop.ps1` and `scripts/Test-UiHarness.ps1`. The explicit `--screen-probe` live capture diagnostic has no profile or toy transport at all; UI tests use isolated profiles. The addon has its own Lua syntax/runtime tests and packaging scripts. See SESSION_STATE for dated verification and remaining limitations.

Capture sets and restores a per-monitor-aware DPI context on its worker thread ([Windows API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setthreaddpiawarenesscontext)). The addon independently converts its parent UI units to physical pixels using the actual screen height, including the user's 53% WoW UI scale. Windows scaling and WoW UI scaling are separate conversions.
