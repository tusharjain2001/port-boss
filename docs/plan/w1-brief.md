# Port Boss: weeks 1–2 brief (builder worker)

Read first: `plan/critic-production.md` (stack, roadmap, risks) and `plan/critic-design.md` (first ten minutes). Full plan: https://claude.ai/artifact/F8cfzV7SJaoabL42N4Af5K

## Decisions from Manan (28 Oct 2026)
- Go on the plan with the six changes. Exception to change 3: the "watch it run" automation ending stays, but is unlocked late (~8–10 h of hands-on play first; automation one cargo line at a time from stage 4). Nothing about automation is built in weeks 1–2.
- No Steam fee yet. Manan pays it himself after playing the first pass, only if he thinks it's worth it. Use Valve's free test app ID 480 (Spacewar) for all Steamworks work.

## Already done by Kufu (28 Oct, main @7090a98)
Base project pushed to main: URP (Universal 3D template), NGO 2.13.3, Unity Transport, Multiplayer Play Mode 2.0.2, Multiplayer Tools 2.2.12, Facepunch.Steamworks 2.5.2 (`steam_appid.txt` = 480), MCP for Unity 10.3.0 (stdio; Claude Code server `UnityMCP`, user scope), Blender MCP (`blender`, user scope, telemetry off), Git LFS, VS Code C#/Unity extensions. Merge main first; skip the setup parts of W1 below. Unity MCP talks to whichever editor has "Start Session" on (port 6400), so open YOUR worktree in Unity and start a session there before using it. Setup log: `Desktop/Port Boss/setup-log.txt`.

## Goal for W1–W2
1. W1: Unity project at `C:\Users\asus\Desktop\port-boss` (cloned from tusharjain2001/port-boss; add Unity .gitignore, Git LFS for binaries). Use the installed editor (6000.5.6f1) with URP. Netcode for GameObjects 2.x + Facepunch.Steamworks on app ID 480. Set up an MCP for Unity server so you can drive the editor. Gate: two instances (Multiplayer Play Mode or a build + editor) join one Steam lobby.
2. W2: the networked box test. Host-authoritative physics, 20–30 physics crates, players walk, grab, stack up to 5 with wobble, throw. A rocking boat deck (kinematic) with crates on it. Run with 4 clients and 150 ms simulated latency (Unity Transport network simulator). Also a car-off-a-ramp test (WheelCollider). Gate: carrying and throwing feel OK at 150 ms, with no rubber-banding a player would notice. Write go/no-go with a short screen recording or screenshots.

## Rules
- No purchases of any kind (assets, subscriptions, fees). If something paid looks necessary, stop and tell Kufu with the free alternative.
- No AI-generated art in the project. Grey boxes and primitives only for now.
- Keep `C:\Users\asus\Desktop\port-boss\LOG.md`: one line per step (time, step, result).
- Commit as you go. Remote: github.com/tusharjain2001/port-boss (Tushar's repo, Manan has push access). Push only your own branch; never push to or rewrite main.
- No popup windows that steal focus on Manan's PC: run builds and tests windowed/minimised or headless where you can.
- Report done with proof: the log, screenshots or a short capture, and the go/no-go note at `C:\Users\asus\Desktop\port-boss\docs\box-test.md`.
