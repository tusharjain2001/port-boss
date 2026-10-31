# Port Boss

**New here? Read `HANDOVER.md` first.** It is the one entry point: the game, every rule, the current state and the next steps. The plan is in `docs/plan/`, Manan's own words in `docs/handover/MANAN-PROMPTS.md`. Headless build/test scripts: `tools/` (`tools/README.md`).

Fishing-first open-sea co-op game (1–4 players, also fully fun solo), Steam. Plan: `docs/plan/` (copy of Manan's plan folder).

## Setup
- Unity 6000.5.6f1, URP (Universal 3D template)
- Netcode for GameObjects 2.13.3, Unity Transport, Multiplayer Play Mode 2.0.2, Multiplayer Tools 2.2.12
- Facepunch.Steamworks 2.5.2 in `Assets/Plugins/Facepunch.Steamworks`; `steam_appid.txt` = 480 (Valve's test app) until the real app ID exists
- MCP for Unity 10.3.0 (stdio). In Unity: Window > MCP for Unity > Start Session, then Claude Code's `UnityMCP` server can drive the editor
- Binaries go through Git LFS (see `.gitattributes`)
