# tools/: headless build and test scripts

Bash scripts (Git Bash on Windows). They drive Unity in `-batchmode` and the built game with `-batchmode` + `-pb-*` test
hooks, so an agent with no screen can build, play-test and release the game. **Nothing opens a window unless you set
`WINDOW=1`.**

## Paths (`env.sh`, sourced by every script; override any of them from the environment)

| Variable | Default | What |
|---|---|---|
| `PROJECT` | the repo root (folder above `tools/`) | Unity project |
| `UNITY` | `/c/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe` | the editor (Unity Hub's default install path) |
| `PB_LOGS` | `$PROJECT/Logs/pb` | build log, test logs, temp shots (`Logs/` is gitignored) |
| `RELEASE_DIR` | `$PROJECT/Builds/releases` | stamped release folders (`Builds/` is gitignored) |

Example: `UNITY="/d/Unity/6000.5.6f1/Editor/Unity.exe" RELEASE_DIR=/c/Users/me/Desktop/PortBossBuilds bash tools/release.sh`

**Close the Unity editor for this project first.** A batchmode run can't open a project the editor already has open (it exits at once).

## Scripts

| Script | What it does | Output |
|---|---|---|
| `ubuild.sh <outdir> [-pbDev]` | Runs `PortBossBuild.SetupAndBuild`: regenerates materials, network prefabs, `Skin.asset` and `Scenes/Port.unity` from code, then builds Win64. `-pbDev` = Development build (dev console; never for players). Prints compile errors + `[PBBuild]` lines. | `<outdir>/PortBoss.exe`, log `$PB_LOGS/build.log` |
| `release.sh` | Release build (no dev console) + `docs/HOW-TO-PLAY.md`. | `$RELEASE_DIR/<yyyymmdd-hhmm>/` |
| `morning.sh` | Release + 5 screenshots + test logs, the playtest package. Batchmode by default (camera-only shots: the menu, HUD and night card are IMGUI and only draw in a window). `WINDOW=1` takes those three windowed. `OUTDIR=...` overrides the folder. | `$RELEASE_DIR/<stamp>-MORNING/` (+ `screenshots/`, `test-logs/`) |
| `reg2.sh [outdir] [exe]` | Quick re-check, all batchmode: solo bot, wobble, swim, crate shots, spawn shot, storm, LAN co-op (host + client on 127.0.0.1). Prints exceptions and key `[PB]` lines per run. Default exe `Builds/dev/PortBoss.exe`. | `$PB_LOGS/reg2/<test>/log.txt` + shots |
| `regress.sh [outdir]` | Full regression: dev build into `Builds/dev`, then every scripted test (menu, settings, solo, wobble, trolley, swim, pier, knock, contract, day 2, night, storm, forklift, characters, co-op). `WINDOW=1` runs menu/settings/night/chars windowed; `OLD_EXE=...` adds A/B shots against an older build. Its co-op step joins through a **Steam lobby** (Steam client running + logged in); use `reg2.sh` for LAN. | `$PB_LOGS/regress/` |
| `cc.sh` | Compile check of `Assets/Scripts` with the .NET SDK, no Unity launch (seconds). Needs `Library/` filled by one editor or batchmode open. Uses `cc.csproj`. | prints `Build succeeded` or the errors |
| `capture.sh <exe> <out.mp4>` | 75 s trailer: the game films itself with `-pb-film` (13 s title flyovers, then 62 s of a bot shift at a fixed 30 fps, idle waits cut, weather turns rough near the end), ffmpeg joins the frames. **Needs a visible window: refuses to run without `WINDOW=1`.** Never record the desktop with ffmpeg gdigrab: it grabs whatever window is on top. | `<out.mp4>`, frames in `$PB_LOGS/film/` |
| `import-toon-harbor.sh <pkg>` | Imports the paid Toon Harbor Pack headless from `Toon Harbor Pack.unitypackage` (kept outside the repo, never committed or uploaded): pulls the URP package out of it, `-importPackage`s it, then moves it to `Assets/ThirdParty/ToonHarbor` (`ToonHarborTools.MoveToThirdParty`). | logs `$PB_LOGS/th_import.log`, `th_move.log` |
| `pair.py a.png b.png out.png [la] [lb]` | Side-by-side A/B image with labels (Pillow). | `out.png` |

Typical loop for an agent: edit code, `bash tools/cc.sh`, `bash tools/ubuild.sh "$PWD/Builds/dev" -pbDev`, `bash tools/reg2.sh`,
read the logs, look at the PNGs. Release: `bash tools/release.sh` (or `morning.sh` for a package with screenshots).

Reading a log: `grep -c Exception log.txt` should be 0; `[PB] t=... fps=... money=...` is the once-a-second status line;
`[PB] sold ...` per sale; `[PB] map check: N placed, 0 flagged` (no tilted or floating props); `carry test: PASS`.
The pack shader logs 3 "duplicate keyword" errors at build: harmless.

## Test hooks (`-pb-*` arguments of `PortBoss.exe`, handled in `Assets/Scripts/Game/AutoRun.cs`)

Add `-batchmode` to run without a window (shots still render the camera; IMGUI menus/HUD are not drawn). Add `-logFile <path>`.

| Hook | Does |
|---|---|
| `-pb-solo` | start a solo game straight away (skips the menu) |
| `-pb-host-lan` / `-pb-join-lan <ip>` | host over LAN / join a LAN host directly (co-op without Steam) |
| `-pb-host-steam` / `-pb-join-lobby` | host a Steam lobby / find and join one (Steam client must run; app 480) |
| `-pb-force-steam` | when joining a lobby, connect over Steam relay even if the host is the same account or a LAN host |
| `-pb-bot` | a bot plays the local player (meets boats, carries, sells) |
| `-pb-quit <s>` | quit after s seconds |
| `-pb-timescale <x>` | run game time x times faster |
| `-pb-shots <dir> [interval s]` | screenshot every interval (default 5 s) as `shot_000.png`... |
| `-pb-day <n>` | start on day n (day 3 grants trolley + forklift) |
| `-pb-night <s>` | jump to the night card after s seconds |
| `-pb-weather <0..1>` / `-pb-storm` | force weather severity / storm test (shots at 0, 0.5, 1 as `weather-*.png`) |
| `-pb-wobbletest`, `-pb-trolleytest`, `-pb-swimtest`, `-pb-piertest`, `-pb-knocktest`, `-pb-contracttest`, `-pb-forklifttest`, `-pb-settingstest` | scripted feature tests, results as `[PB]` log lines |
| `-pb-handletest <dir>` | crate handling: rack (Q / hold Q), take back (E / hold E), into the trolley, set down upright: `PASS`/`FAIL` |
| `-pb-staminatest` | swim until out of stamina, fished out onto a ladder |
| `-pb-trucktest` | lorries stop short of a player in the road, queue at most 5, clear after stepping aside |
| `-pb-boattest` / `-pb-boardtest` | boats are solid (swimmer pushed out, bridge blocks on deck) / walking up the gangplank never sticks |
| `-pb-evicttest` | stay aboard as the boat casts off: the captain throws you back onto the jetty (`PASS`/`FAIL`) |
| `-pb-storeshots <dir>` | buys crate store levels 2 and 3 by day, shots of each, crates kept on the racks |
| `-pb-pushtest <dir>` | trolley shots; held pushing stops at a wall and at the quay edge, turns without overlapping (`PASS`/`FAIL`) |
| `-pb-intro <dir>` | play the story intro in a test run, shots along it, checks the day clock waits |
| `-pb-rivalshots <dir>` | Marlowe's drive-by (splash, gloat) and fireworks over his port |
| `-pb-carrytest <dir>` | carries 1/3/5 crates, checks every carried crate is drawn in place and in view: `PASS`/`FAIL` |
| `-pb-stagetest <dir>` | buys stage 2 (Concrete quay) and checks money, shed, bigger cargo |
| `-pb-shovehost` + `-pb-shovetest` | two processes over LAN: the client shoves the carrying host |
| `-pb-shop` | with `-pb-night`: buys the trolley at the night card |
| `-pb-settings <tab>` | open the settings page on tab n (for shots) |
| `-pb-mapshots <dir>` | fixed-camera map shots (`map-demo-angle.png`...) + the map check |
| `-pb-uishots <dir>` | title flyover shots, logo, Join, Settings, Pause (menus drawn only when windowed) |
| `-pb-crateshots <dir>` / `-pb-charshots <dir>` | crate / character close-ups |
| `-pb-film <dir>` | the trailer frames (windowed only, see `capture.sh`) |
| `-pb-ownart` | ignore the Toon Harbor skin even if it is built in |
| `-pb-bevel`, `-pb-oldwater`, `-pb-oldfog`, `-pb-noclouds` | look experiments / A/B switches |

Build-side arguments (Unity `-executeMethod PortBoss.EditorTools.PortBossBuild.SetupAndBuild`): `-pbOut <dir>`, `-pbDev`.
