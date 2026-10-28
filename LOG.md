# Port Boss LOG

One line per step: time | step | result

- 2026-10-28 23:05 | merged origin/main @7090a98 into panoptes/w-portboss-w1 | ok
- 2026-10-28 23:11 | plan changed to overnight-brief.md (playable by 11:00) | building day-1 slice, NGO host-auth, batchmode-driven
- 2026-10-28 23:15 | Steam transport (Facepunch → NGO 2.13) written by subagent | compiles (dotnet check), untested over real Steam yet
- 2026-10-28 23:40 | batchmode Setup+Build (materials, 5 network prefabs, Port scene, Win64 dev build) | build ok, prefab hashes non-zero
- 2026-10-28 23:42 | headless solo bot run | boat arrives/docks/rocks, gangplank, grab/stack/drop/throw/topple, sells at table: 8 crates $96 in 90 s
- 2026-10-29 00:00 | windowed UI run (test mode, no cursor lock) | HUD money, captain shouts, +$12 popups, end-of-day card, trolley bought, Day 2 starts
- 2026-10-29 00:10 | trolley test (headless) | 3 crates load into bed, pushed 5.4 m in 3 s, let go on slipway -> rolls into sea, spills, "trolley lost"
- 2026-10-29 00:15 | co-op: host (LAN mode) + client via Steam lobby search/join on app 480, same PC | client found + joined lobby 109775242660978856, NGO connected, client bot grabbed/sold crates; host money rose. PASS
- 2026-10-29 00:20 | co-op over Steam relay, same account both sides | fails "Invalid Connection" (Steam can't P2P to self) -> needs a 2nd account to verify; now fails gracefully
- 2026-10-29 00:30 | CC0 sounds in (Resources/Audio), horn synth retuned 70/140 Hz, night = sun below horizon, hills/rocks, walk anim, buyer label | ok
- 2026-10-29 00:35 | release build -> Desktop/Port Boss/builds/20261029-0003 + HOW-TO-PLAY.md | smoke test 0 errors, menu + play screenshots
