# Port Boss LOG

One line per step: time | step | result

- 2026-10-28 23:05 | merged origin/main @7090a98 into panoptes/w-portboss-w1 | ok
- 2026-10-28 23:11 | plan changed to overnight-brief.md (playable by 11:00) | building day-1 slice, NGO host-auth, batchmode-driven
- 2026-10-28 23:15 | Steam transport (Facepunch → NGO 2.13) written by subagent | compiles (dotnet check), untested over real Steam yet
- 2026-10-28 23:35 | batchmode Setup+Build (materials, 5 network prefabs, Port scene, Win64 dev build) | build ok, prefab hashes non-zero
- 2026-10-28 23:38 | headless solo bot run | boat arrives/docks/rocks, gangplank, grab/stack/drop/throw/topple, sells at table: 8 crates $96 in 90 s
- 2026-10-28 23:46 | windowed UI run (test mode, no cursor lock) | HUD money, captain shouts, +$12 popups, end-of-day card, trolley bought, Day 2 starts
- 2026-10-28 23:50 | trolley test (headless) | 3 crates load into bed, pushed 5.4 m in 3 s, let go on slipway -> rolls into sea, spills, "trolley lost"
- 2026-10-28 23:52 | co-op: host (LAN mode) + client via Steam lobby search/join on app 480, same PC | client found + joined lobby 109775242660978856, NGO connected, client bot grabbed/sold crates; host money rose. PASS
- 2026-10-28 23:55 | co-op over Steam relay, same account both sides | fails "Invalid Connection" (Steam can't P2P to self) -> needs a 2nd account to verify; now fails gracefully
- 2026-10-29 00:00 | CC0 sounds in (Resources/Audio), horn synth retuned 70/140 Hz, night = sun below horizon, hills/rocks, walk anim, buyer label | ok
- 2026-10-29 00:04 | release build -> Desktop/Port Boss/builds/20261029-0003 + HOW-TO-PLAY.md | smoke test 0 errors, menu + play screenshots
- 2026-10-29 00:10 | (note) times above corrected to the real clock; from here every line uses `date` | -
- 2026-10-29 00:10 | Kenney + own-made gull FBX imported to Assets/Art (muted kit materials) | build ok, sizes logged
- 2026-10-29 00:14 | P1 wobble: balance model (inertia x height, instability, counter-steer saves), Steady! + creak, bending stack visual; tuning table docs/wobble.md | build ok
- 2026-10-29 00:14 | P2 buyer panel: E at buyer opens panel (carried + parked trolley crates, grade/freshness/offer, Sell, Sell all, Close); table selling removed; offers 15/12/4 | bot sold 3x$15 via panel, screenshot p2-buyer-panel.png
- 2026-10-29 00:15 | P1 wobble test in build (5 crates): walk 0.32, sprint start 0.69 (Steady!, saved), sprint reverse 1.00 -> top 2 topple | proof p1-wobble-test.log, p1-steady-teeter.png
- 2026-10-29 00:18 | P1 visual: stack bends/tilts strongly + side swing scaled by lean, per-crate stagger; Steady text moved low | p1-visible-lean.png
- 2026-10-29 00:25 | Unity paused (Manan gaming): code-only. P3/P4/P5/P6 written; dotnet compile check of all scripts passes; URP asset tuned (shadows, MSAA, HDR grading, SSAO). Not yet run in Unity (queue in HANDOFF.md) | compile ok
- 2026-10-29 00:28 | P7 code (CharAnim playables + Player hookup), P8 front door (live title scene w/ bobbing boat + gulls, Alfa Slab title (OFL), hover/click sounds, Settings: music/sound/mouse/invert/window/resolution, version tag), P9 music slots menu/day/rush/night/storm from StreamingAssets/Music (silent until Manan's files), crossfade, rush = last 30 s | dotnet compile ok, not yet run
- 2026-10-29 00:49 | ART DIRECTION RESET (Manan): removed PBR textures, HDRI, Quaternius realistic characters from scene/build; colour grading back to LDR like cp2; SSAO 0.6/0.4; posts plain. Keep stylised additions pending A/B vs cp2 | compile ok (Unity paused)
- 2026-10-29 00:52 | P11 settings page coded: 5 tabs (graphics presets Low/Med/High/Ultra auto-picked + render scale, shadows, AA, SSAO, post, water, view dist, vsync, fps cap, FOV; display res/window mode/monitor/brightness; audio master/music/sfx/ambience/mute-unfocused; controls sens/invert/pad sens/key rebinding; gameplay shake/shout size/HUD/hint), live apply, PlayerPrefs; gamepad input; camera shake | compile ok, not yet run (no Unity: Manan gaming)
- 2026-10-29 00:55 | P10 swimming coded: surface swim (head+shoulders out, bob, never sinks), slower, splash sfx+particles, stack drops & floats, tow one floating crate (server force), 5 ladders (pier sides, quay front) E to climb, slipway walk-out (hysteresis vs mean sea level), camera kept above water, swim pose on all peers, HUD hints, bot climbs out | compile ok, not yet run
- 2026-10-29 00:56 | docs/ART-PLAN.md written (look target, palette hex, light/post, one material approach = flat colour + bevelled boxes, proportions, keep/redo per object, open decisions); HOW-TO-PLAY updated (buyer panel, swimming, settings, gamepad) | for Kufu + Manan review
- 2026-10-29 00:58 | Backlog 4: rival speedboat (Marlowe) does doughnuts off the pier every ~170 s (time-deterministic, no sync), foam wake + engine drone, taunts; host wake: docked boat rolls, loose crates slide, floating crates heave | compile ok, not yet run
- 2026-10-29 00:59 | Backlog 5 contract board: night card shows board (also pinned on the shed wall): Captain Mags (steady, 2:00, normal) vs Captain Bjorn (+50% crates, +25% price, 1:30); host books; drives cargo, countdown, prices, captain name in shouts. Backlog 11 part: end-of-day verdict line | compile ok, not yet run
- 2026-10-29 01:00 | Backlog 6: second berth (left of pier, own gangplank, boats turn round when leaving); day schedules = difficulty dial (day1 3 boats one berth, day2 4 boats overlapping, day3+ 4 earlier); horn shout names the left side | compile ok, not yet run
- 2026-10-29 01:01 | Backlog 7: crate hitting a player >6 m/s knocks them over (drop stack, shove, tip-over anim 1.2 s, toast/shake); hard landings >7.5 m/s damage crates (max 3: slats break, -25% price each, shown in buyer panel) | compile ok, not yet run
- 2026-10-29 01:02 | Backlog 8: pier split into base + hinged old end (kinematic rb) that sags up to 9 deg when overloaded (host weighs crates/players; cap 10, 20 repaired), creaks, buyer warns; night shop 'Repair the pier' $80 fills the holes + braces | compile ok, not yet run
