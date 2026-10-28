# HANDOFF (portboss-w1, overnight 28→29 Oct)

Branch `panoptes/w-portboss-w1` (Kufu pushes). Log of every step: `LOG.md`. Brief: `Desktop/Port Boss/plan/overnight-brief.md` (+ UPDATE 23:59 / 00:10 / 00:30 / 00:40 / 00:45).

## Rules in force right now
- **PC busy (Manan gaming): no Unity, no game windows, nothing that can pop up.** Only hidden light tools (git, `dotnet` compile check). Wait for Kufu's "PC is free".
- **Art direction: stylised cp2 look** (build `20261029-0003`). No PBR/HDRI/realistic characters. No visual change ships without a same-camera A/B vs cp2. `docs/ART-PLAN.md` awaits Kufu + Manan.
- Hold per-item messages: ONE summary to Kufu when the PC is free. report_done only at the end (~10:45) with the newest build.

## Proven in builds (before the pause)
Day-1 loop, wobble (P1), buyer panel (P2), trolley, night shop, day 2, Steam-lobby co-op (same PC), title scene (P8), water shader + clouds + lighthouse + open sea (P4/P6 parts), gulls flying (P5), menu boat.
Last Desktop build: `Desktop/Port Boss/builds/20261029-0003` (cp2, pre-polish). Dev build: `Builds/dev` in the worktree (Manan ran this one: it had the headless Quaternius character, since removed).

## Written + compile-checked, NOT yet run (needs the regression run)
P3 shouts/captains · P7 chunky primitive dockhand (procedural anims) · P9 music (night track in, from sunset) · P10 swimming (ladders, tow, slipway) · P11 settings page (5 tabs, live apply, rebinding, gamepad) · backlog 4 rival speedboat + wake · 5 contract board · 6 second berth + day schedules · 7 knockdown + crate damage · 8 pier repair + sagging end · 9 forklift + pallets · 10 storm + tie-downs · 11 gull raids + end-of-day verdict · revert of PBR/HDRI/LDR grading.

## When the PC is free, in order
1. `bash <scratchpad>/regress.sh <scratchpad>/reg1`: builds `Builds/dev` and runs menu/settings/solo/wobble/trolley/swim/pier/knock/contract/day2/night/storm/forklift/chars, A/B vs cp2 (`ab-old` vs `ab-new`, same spawn camera), and a 2-instance co-op run. Read the summary + look at the shots.
2. Fix what breaks. Back out any visual that looks worse than cp2 in the A/B.
3. Release build → `Desktop/Port Boss/builds/<stamp>/` + `docs/HOW-TO-PLAY.md`.
4. ONE summary message to Kufu (shots: A/B pair, character front/side, swim, storm, forklift, settings, night card with contract board).
5. Final: 3–5 screenshots + a 30–60 s capture of a bot playthrough (frames → ffmpeg), report_done ~10:45.

Scratchpad scripts: `ubuild.sh` (headless build), `release.sh`, `cc.sh` (dotnet compile check, no Unity), `regress.sh`.
