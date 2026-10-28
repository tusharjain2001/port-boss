# HANDOFF (portboss-w1, overnight 28→29 Oct)

Branch `panoptes/w-portboss-w1` (Kufu pushes). Log of every step: `LOG.md`. Brief: `Desktop/Port Boss/plan/overnight-brief.md` (+ UPDATEs). Latest task: t-6ccc7f36 (report_done with that id, ~10:45).

## Rules in force
- PC is free (t-6ccc7f36) but keep runs windowless where possible (`-batchmode`; shots render the camera to a texture).
- Art direction: stylised cp2 look (build `20261029-0003`). No PBR/HDRI/realistic characters. Every visual change gets a same-camera A/B vs cp2 (`docs/ART-PLAN.md`).
- Per-item message to Kufu with shots; report_done only at the end.

## Done and proven (latest Desktop build `builds/20261029-0211`)
1. Urgent fixes: chunky stylised dockhand (head, facing), cp2 look (LDR, no PBR/HDRI), A/B shots → `proof/overnight/item1-2/`.
2. Wooden dock crates (`CrateLook.cs`), lidded empties by the shed → `item1-2/`.
3. Night music (Manan's track from sunset), P11 settings (5 tabs + functional test) → `item3-5/`.
4. P10 swimming (fall in, tow, ladder, open water) → `item3-5/`.
5. P3 shouts/countdown, P4 open sea, P5 gulls, P8 title screen → `item3-5/`.
Regression fixes: boats spawned at origin swept crates into the sky; black-square particles; night-card overlap; camera into player's back; PlayerPrefs in field initializer; bot auto-sell in OnGUI (batchmode).

## Still to do
- Final: 30–60 s capture (`scratchpad/capture.sh`), 3–5 screenshots, vault notes, report_done (t-6ccc7f36).
- Open questions for Kufu: water colour vs cp2 (now bluer), storm wind blows an idle player off the quay, Steam lobby co-op untested tonight (Steam not running; LAN co-op passes).

Scratchpad scripts: `ubuild.sh` (headless build), `release.sh`, `cc.sh` (dotnet compile check), `regress.sh` (full), `reg2.sh` (quick re-check), `pair.py` (A/B side-by-side).
