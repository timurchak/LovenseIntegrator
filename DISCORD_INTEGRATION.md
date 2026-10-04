# Discord integration research

**Status: cancelled by the user on 2026-10-04.** Windows system notifications and adding a visible bot to a server do not meet the user's requirements. The options below are retained research, not an implementation plan to execute without a new request. No Discord connections or events are implemented.

The original research checked official documentation on 2026-10-04. Recheck access requirements before revisiting an option.

## Windows notifications

For “Discord notification → action,” a local Windows notification source was the simplest proposed first step. `UserNotificationListener` exposes system toast notifications, their source and available text without a Discord bot or RPC access. It requires `userNotificationListener` capability in the package manifest and user permission through Windows. The current build is an unpackaged WPF EXE, so package identity/packaging and API behavior on this Windows installation would need a spike first. This path was not physically verified. [Microsoft notification listener](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/notification-listener), [capability manifest](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-uap3-capability-manual).

Proposed editor: Notifications → Discord, with optional title-contains/text-contains filters. A sender/channel can only be matched against fields actually present; notifications do not guarantee structured Discord IDs or full message content. Select the source by application identity, using its name only as a label. Browser notifications need separate verification because the browser may be reported as the source.

On enable, establish a baseline of existing notifications and trigger only for new ones. Compare notification-center changes by Id, distinguish additions/removals, suppress duplicates and bursts, and expose permission denial/revocation. Process only selected applications; do not store notification contents in logs or exports. Reuse observation, cooldown and Stop.

Only notifications actually delivered to Windows can be observed. Discord/channel settings, app state and notification suppression affect delivery. An in-app banner, sound and speaking event are different sources. Before implementing filters, inspect one real toast in observation mode and its available fields. [Discord desktop notifications](https://discord.com/blog/how-to-manage-your-discord-desktop-notifications).

A first spike would request access from a packaged app, observe one real Discord toast and emit one event without an action. Replay/UI checks would cover baseline notifications, duplicates, removals, empty text, denied access and source persistence. The user rejected this option because they do not want to switch Discord to Windows notifications.

## Server events through a bot

A separate bot account connected to Gateway from the C# application could receive richer events. The bot must be added to the chosen server and is visible there. Ordinary OAuth2 login with `identify` alone does not provide the user's message/voice event stream. [Gateway](https://docs.discord.com/developers/events/gateway), [OAuth2](https://docs.discord.com/developers/topics/oauth2).

| Editor event | Discord data | Example local rule |
| --- | --- | --- |
| New message | `MESSAGE_CREATE` | Selected sender → impulse |
| Mentioned | `MESSAGE_CREATE`, mentions | Selected Discord ID mentioned → pulse |
| Reaction added/removed | `MESSAGE_REACTION_ADD` / `REMOVE` | Selected emoji on selected message → effect |
| Started typing | `TYPING_START` | Typing in selected channel → impulse |
| Joined/left/moved voice channel | `VOICE_STATE_UPDATE`, compare `channel_id` | Joined selected channel → effect |
| Mic/deafen changed | `VOICE_STATE_UPDATE`, compare flags | `self_mute` changed → effect |
| Camera/stream changed | `VOICE_STATE_UPDATE`, `self_video` / `self_stream` | Stream started → effect |

See [Gateway events](https://docs.discord.com/developers/events/gateway-events), [message resource](https://docs.discord.com/developers/resources/message) and [voice state](https://docs.discord.com/developers/resources/voice#voice-state-object). Stream flags may be optional: a missing field is not an “off” transition. Typing does not measure individual keys or typing speed. Mute/deaf state does not identify speech or silence.

These events need the corresponding `GUILDS`, `GUILD_MESSAGES`, `GUILD_MESSAGE_REACTIONS`, `GUILD_MESSAGE_TYPING` and `GUILD_VOICE_STATES` intents plus channel access. Metadata can be sufficient initially. Text/regex matching adds `MESSAGE_CONTENT`; online/idle/dnd presence adds `GUILD_PRESENCES`, both with privileged-intent requirements. [Gateway intents](https://docs.discord.com/developers/events/gateway#gateway-intents).

A bot sees servers/channels available to it, not the ordinary user's private conversations. DMs addressed to the bot itself are a separate case. Message deletion does not always include the author; filtering it by author would require a bounded ID/author cache rather than guessing.

The proposed setup page would show connection state and selectable servers/channels. A personal bot requires Developer Portal setup and permission to add it to a server. A public product would need a separate assessment of a shared bot/backend and OAuth2 account linking. Filter by stable IDs, not display names. The user rejected the server-bot requirement, so none of this is implemented.

## Architecture if revisited

Start in observation mode, showing matches and readable descriptions before automatic actions are enabled. Intensity, duration, action and toy are always selected locally. A Discord event must not carry an arbitrary toy command. Exclude the bot's own messages and other bots by default to avoid feedback loops.

A Discord provider would normalize incoming payloads with kind, receive time, event Id, guild/channel/user/message IDs and emoji. It would use the existing executor, cooldown, priority, observation and Stop rather than calling BLE/REST directly.

Current InputEvent is local-input oriented and Rule has no Discord filters. Add an external-event context and migrate profile fields; do not place Discord IDs in Keys or Process. Append enum values or introduce explicit stable identifiers without renumbering existing values.

Initial `GUILD_CREATE` would establish voice-state baselines without triggering rules. Reconnect should cancel relevant actions, show connection state, rebuild baselines and discard stale events. Resume can replay missed events, so event-ID deduplication and freshness limits are necessary. [Reconnect and Resume](https://docs.discord.com/developers/events/gateway#resuming).

Keep tokens outside exported profiles using Windows Credential Manager or DPAPI. Do not log tokens, complete payloads or message text. Disconnect and Stop must invalidate pending commands just as local-rule pause does.

## Speech, calls and local client access

RPC documents `SPEAKING_START` / `SPEAKING_STOP`, channel selection, voice settings and notifications. However, `rpc`, `rpc.voice.read` and `rpc.notifications.read` access is restricted to approved partners, with additional test-access limitations. Discord being installed does not establish access; this would require separate research after access is confirmed. [RPC](https://docs.discord.com/developers/topics/rpc), [OAuth2 scopes](https://docs.discord.com/developers/topics/oauth2).

A bot in a voice channel is another possible voice-integration route but requires a voice connection and verification of available events/encryption. `VOICE_STATE_UPDATE` alone does not provide speaking duration. Local microphone/output volume is an independent Audio source, without speaker identification or a guarantee that it comes from Discord.

An ordinary incoming channel webhook sends messages into Discord; it is not a subscription to messages/reactions. This proposed event set requires Gateway. [Webhook resource](https://docs.discord.com/developers/resources/webhook).

Use supported bot/OAuth2 APIs if this is revisited. Automating an ordinary account using an extracted user token is prohibited by Discord. [Official self-bot guidance](https://support.discord.com/hc/en-us/articles/115002192352-Automated-User-Accounts-Self-Bots).

## Checks before any future release

Replay synthetic payloads: initial state must not trigger; a repeated event Id is handled once; other servers/channels/users fail filters; missing fields do not invent transitions. Cover heartbeat/reconnect/resume, freshness, unknown IDs, inaccessible channels, bad tokens, revoked permissions, Stop during processing and secret-free exports.

UI harness: filters follow the selected event; IDs survive refresh and channel renames; unavailable entities are explicit. Physical checks would use a separate test server after bot setup, starting in observation. No Discord connection, messages or command registration was performed during this project work.
