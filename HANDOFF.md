# HANDOFF (portboss-w1, night 28→29 Oct; resume here after any pause)

Branch `panoptes/w-portboss-w1` (Kufu pushes). Every step: `LOG.md`. Open tasks from Kufu: t-6ccc7f36, t-ceaece1c, t-4ae2d153, t-fea759f1,
t-b56d9028, t-70cb50a8, t-71cb1a91, t-155ed2b8, t-d9fc82bc, t-2dd54b2a (report_done each, with proof, at the end).

## Timing (Manan)
Feature freeze **09:00**. 09:00–09:45 fixes + release into `Desktop/Port Boss/builds/<stamp>-MORNING/` (HOW-TO-PLAY.md, 5 screenshots,
60–90 s capture), report_done by **10:00**, then stop. Quality over quantity.

## Rules
- **Toon Harbor Pack is paid + the repo is PUBLIC: never commit pack files.** It lives in `Assets/ThirdParty/ToonHarbor` (gitignored).
  Before every commit: `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor Pack'` must be 0. `Assets/PortBoss/Skin.asset` holds GUIDs only (fine).
- Art direction = the Toon Harbor pack (its demo scene is the reference). Weather is random, no warnings/forecasts.
- Keep the PC quiet: tests in `-batchmode` (shots render the camera); windowed only for IMGUI shots. Never gdigrab (it records whatever window is on top).
- Release builds only for Manan (no Development console).

## Done (latest release `builds/20261029-0440`)
- New harbour map (`HarbourMap.cs`, skin only) + Kufu's map fixes (t-827e3b13): neat container blocks + yard cranes, roads/rail/barriers,
  tank farm, pipes, light towers, parked kit, stains, city skyline + hills, shabby berth; `[PB] map check` (tilt/float pass) logs 0 flagged.
  Water: fresnel ~1 (no refracted-screen smudges), deep-blue base (`GameUI` water setup).
- Menu redo (t-6f6de594, `GameUI.Menu.cs`): title flyovers + idle worker, container logo (off-stage camera → texture), manifest clipboard
  menu/pause, steel panels for Settings/Join, plate buttons, clank/blip sounds, bottom bar. Test: `-pb-uishots dir` (windowed for UI).
- Toon Harbor skin, data-driven (`Skin.cs` slots filled by `PortBossBuild.MakeSkin()` when the pack exists; own art otherwise):
  pack sky/fog/post/water, toon shader for own pieces (`Art.Get` solid-colour textures + pack ramp, `ToonGain`), pack dock workers
  (`Player.BuildWorker` + `CharAnim`: CC0 UAL1 clips via Humanoid, arm IK for carry/throw/flail), pack crates/barrels/pallets/dinghy/buyer,
  rival port (containers, harbour cranes, loaded freighter), horizon freighters, Marlowe's speedboat, tug-palette fishing boat, seabed.
- Random weather: `PortGame.Severity` (host, seeded per session), `Storm.cs` presentation (waves, rain, gusts, gulls hide, sound), F9 debug, `-pb-weather x`.
- Ideas: #10 shared crew tin, #9 next-stage goalpost (HUD + night card), #8 tomorrow recap, #6 splash punch-in (+solo slow-mo), #3 flail on falling in.
- `docs/PORT-STAGES.md` (5 stages → pack prefabs). Proof: `Desktop/Port Boss/proof/overnight/toon-harbor/`.

## Next
Release candidate `builds/20261029-0640` (commit 317fc63) passes the full battery: reg2 (bot, wobble, swim, crates, storm, LAN co-op),
carry 1/3/5, stage 2 (`-pb-stagetest`), forklift, shove (LAN). Since 05:00: menu, B, C, #7 shove, pack forklift/shelter/stall/bollards,
ground tiles, lighter water, crisp logo, first-boat head start, walking dock workers, roads visible, parked trailers, storm sky,
next-boat countdown, buildable stage 2 (Concrete quay $1500), toon gull.
1. 09:00 freeze → MORNING: copy the final build to `builds/<stamp>-MORNING/` + HOW-TO-PLAY + 5 shots + capture
   (windowed `-pb-film <dir>` → `ffmpeg -framerate 30 -i f_%05d.jpg -c:v libx264 -crf 20 -pix_fmt yuv420p`), report_done all tasks.

## Later (not tonight)
- Ideas rejected for tonight: #1 hazard-modifier vote, #2 special crates (live/fragile), #4 storm warning tell (no warnings by design), #5 tipping plank.
- Shortlist "later" list: rope rescue, hired NPC workers, new harbour biomes, clip/upload system.
- Stages 2–5 (PORT-STAGES.md); stage 5 needs LOD/batching for the ~47k-renderer demo layout.
- Steam lobby co-op across two accounts still untested (needs a friend); LAN co-op passes.

## Known issues
- Pack shader logs 3 "duplicate keyword" errors at build: harmless (build succeeds).
- Gull is code-built (the pack has no birds).

## How to build/test (scratchpad scripts, paths in LOG.md)
`ubuild.sh <outdir> [-pbDev]` headless build · `release.sh` stamped Desktop build + HOW-TO-PLAY · `cc.sh` dotnet compile check ·
`reg2.sh` quick batchmode re-check · `pair.py` A/B images. Test hooks: `-pb-solo -pb-bot -pb-shots dir sec -pb-quit s -pb-storm
(weather 0/0.5/1 shots) -pb-weather x -pb-swimtest -pb-crateshots dir -pb-charshots dir -pb-night 18 -pb-settings n -pb-ownart`.
