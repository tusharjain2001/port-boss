# Port Boss

New here? Start with `HANDOVER-TUSHAR.md` (state, setup, the paid-pack rule). Headless build/test scripts: `tools/` (`tools/README.md`).

Co-op port game (1–4 players, Steam). Plan: `C:/Users/asus/Desktop/Port Boss/plan/`.

## Setup
- Unity 6000.5.6f1, URP (Universal 3D template)
- Netcode for GameObjects 2.13.3, Unity Transport, Multiplayer Play Mode 2.0.2, Multiplayer Tools 2.2.12
- Facepunch.Steamworks 2.5.2 in `Assets/Plugins/Facepunch.Steamworks`; `steam_appid.txt` = 480 (Valve's test app) until the real app ID exists
- MCP for Unity 10.3.0 (stdio). In Unity: Window > MCP for Unity > Start Session, then Claude Code's `UnityMCP` server can drive the editor
- Binaries go through Git LFS (see `.gitattributes`)
