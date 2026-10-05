# Project documentation

Entry point for future sessions. This knowledge snapshot was updated on 2026-10-05. Current code and fresh verification results take precedence over historical counts.

| Document | Purpose |
| --- | --- |
| [USER_GUIDE.md](USER_GUIDE.md) | Detailed setup, keyboard/mouse workflows, imports and troubleshooting |
| [AGENTS.md](../AGENTS.md) | Persistent project instructions |
| [SESSION_STATE.md](SESSION_STATE.md) | Handoff: completed work, verification and deferred work |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Source map, event flow and responsibilities |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Build, test, publish, launch and diagnostic artifacts |
| [RELEASING.md](RELEASING.md) | Windows installer, GitHub releases, auto-updates and upgrade verification |
| [LOCALIZATION.md](LOCALIZATION.md) | English/Russian UI, preferences, catalog and preservation boundaries |
| [BLUETOOTH.md](BLUETOOTH.md) | Standard adapters, multiple devices, SDK crash findings and limits |
| [RULES_AND_PROFILES.md](RULES_AND_PROFILES.md) | Rules, haptic feedback, data formats, migrations and UI |
| [SCREEN.md](SCREEN.md) | WoW Screen mode, semantic events, color protocol and safety |

Existing detailed references:

- [Application README](../README.md): visual overview, download and quick start.
- [Keyboard AI instructions](../KEYBOARD_AI_RULES.md) and [JSON example](../examples/keyboard-assignments.json).
- [Mouse AI instructions](../MOUSE_AI_RULES.md) and [JSON example](../examples/mouse-assignments.json).
- [Screen AI instructions](../SCREEN_AI_RULES.md) and [JSON example](../examples/screen-assignments.json).
- [Discord research](../DISCORD_INTEGRATION.md): retained findings for a **cancelled** integration.
- [TRIGGER_IDEAS.md](../TRIGGER_IDEAS.md): possible future directions, not an implemented catalog.

Avoid duplicating JSON field tables across documents. AI instructions, validators and examples define the detailed contracts and must be updated together.
