# Port Boss: overnight playable (28 Oct 23:10 → 29 Oct ~11:00)

Manan: "I want something playable in the morning, you have 12 hours."
This replaces the W1–W2 week plan for tonight. Design source: `plan/critic-design.md` (first ten minutes) and the plan page https://claude.ai/artifact/F8cfzV7SJaoabL42N4Af5K.

## The deliverable
A Windows build Manan double-clicks: `C:\Users\asus\Desktop\Port Boss\builds\<yyyymmdd-hhmm>\PortBoss.exe` (windowed, 1920x1080 default), plus a 1-page `HOW-TO-PLAY.md` next to it (controls, what to do, known bugs).

### Must have (the "day 1" slice)
- Third-person character: walk, sprint, jump. Camera that doesn't fight you.
- Map: one wooden pier (a few planks missing), a buyer's table, a small shed, open sea with moving water (shader or animated mesh, free/own only), the rival's bigger port visible across the bay.
- A fishing boat arrives (horn), docks rocking on the waves, gangplank tilts. Captain countdown as on-screen shout text ("2 minutes, I've got a tide to catch!"), no HUD timer bar. Boat LEAVES with whatever is still aboard, cargo included.
- Grab a crate; grab more and they stack (up to 5) with wobble that worsens when you sprint/turn; drop; THROW. Crates are physics objects; they fall into the sea and float/drift.
- Fish crates spoil visibly over the day (tint + flies particle); spoiled sell for less.
- Sell at the buyer's table: drop crates on it → money popup (+$12) and ka-ching.
- Money on screen. 2–3 boats per day (~8 min day). End-of-day card: money earned, crates sold, "Crates in the sea: N".
- Night shop: buy the hand trolley ($120): pushable physics trolley that holds crates and rolls into the sea if you let go on the slope. Then Day 2 starts.
- Main menu: Play (solo = host alone), Host co-op, Join (Steam friend invite / lobby on app 480), Quit. Pause menu with Quit.

### Co-op
Built networked from the start with NGO host-authoritative (solo = host with no clients). Two players must be able to play together via a Steam lobby on 480 (test with Multiplayer Play Mode + one real build). Carried crates are parented/kinematic while held (never simulated in a hand), thrown crates are server-simulated.
**Timebox:** if co-op still breaks the core loop at 06:00, cut it to solo-only for this build (keep the code on a branch) and say so in HOW-TO-PLAY.md. A working solo game beats a broken co-op one.

### Nice to have (only after must-haves work in a build)
Rival speedboat wake every few minutes; second boat arriving early; pier repair purchase; ragdoll when hit by a thrown crate; simple sounds for every action.

## Checkpoints (send_message to kufu at each, with a screenshot path; do NOT report_done until the end)
- 01:30 player + pier + boat + grab/stack/throw in editor
- 04:00 full day-1 loop playable in a Windows build (solo)
- 06:00 co-op verdict (works / cut for tonight)
- 08:30 trolley, night shop, day 2, end-of-day card, menu
- 10:00 feature freeze: only fixes; final build; 3–5 screenshots + a 30–60 s capture (OBS or Unity Recorder) of a real playthrough
- 10:45 report_done with: build path, HOW-TO-PLAY.md, screenshots/capture, LOG.md, what's cut

## Art and audio
- Worker `portboss-assets` drops CC0 models/sounds into `C:\Users\asus\Desktop\Port Boss\assets-incoming\` with `MANIFEST.md` (what, licence, source). Builder imports what it needs into `Assets/Art` and `Assets/Audio`. Until they arrive, primitives.
- No AI-generated images or models. CC0 (Kenney etc.) and what we model ourselves in Blender only. Keep `Assets/CREDITS.md`.
- Look: stylised realism, chunky readable shapes, warm late-afternoon light, NOT cartoon-toy/chibi.

## Rules
- No purchases. No popup windows that steal focus where avoidable (Manan is asleep; the PC is yours, but don't touch other apps).
- Commit often on your branch; push your branch at each checkpoint. Never push to main; Kufu merges.
- Keep LOG.md (time, step, result).

## UPDATE 23:59: no stop point (Manan)
Manan: keep building until the morning, no stop point; use the full 12 hours. The must-haves above are the floor, not the finish. When they work in a build, keep going down this backlog in order, one feature at a time, each one committed and proven in a build before the next. Always keep a working build in `Desktop/Port Boss/builds/<yyyymmdd-hhmm>/` (copy a new one after each feature that works; never leave only a broken one).
1. Co-op over a real Steam lobby (2 players), the 06:00 timebox still applies.
2. Kenney models + CC0 sounds in (pier, crates, fish crate, boat, shed, table, rival port, own-made trolley + gull from `assets-incoming/Models/own-made/` when they land), synthesised ship horn. Muted, warm look.
3. Trolley feel (push, load crates, rolls into the sea on the slope).
4. Rival speedboat: does a doughnut every few minutes; its wake rocks the boat and slides loose crates.
5. Contract board on the shed wall at night: pick tomorrow's ship, small safe vs big risky (more crates, tighter clock, more money).
6. Second boat arriving early / overlapping (difficulty dial by day).
7. Ragdoll/stumble when hit by a thrown crate; crate damage on hard landings (value drops).
8. Pier repair purchase (fill the missing planks); overload the old pier and it tilts.
9. Forklift unlock on day 3 (pallets of 6 crates, tips if you corner fast, can fall in the harbour).
10. The storm: siren, 90 s warning, wind that shoves loose cargo, rain, tie-downs or shed.
11. Gulls that steal fish from crates left out; captain lines with personality; end-of-day stat card with a funny line.
12. Controller support, settings (sensitivity, volume, resolution), simple title screen.
Report: send_message to kufu at each finished backlog item (one line + screenshot path) in addition to the clock checkpoints. report_done only at ~10:45 with the newest working build.

## UPDATE 00:10: Manan played the build. POLISH FIRST, then the backlog
Manan: everything works but all of it needs improving. Order: finish every polish item below (each proven in a build) BEFORE any new backlog feature; only when no polish is left, continue the backlog (co-op first).
His words, turned into tasks:
P1. Carry/stack/throw: works, but the stack should WOBBLE MORE (visible sway that builds with speed and turning, near-topple moments you can save).
P2. Selling: he doesn't like dropping crates on the table. Replace it: walk up to the buyer and press E → a buyer panel opens (mouse cursor on, game keeps running): lists the crates you're carrying (type, freshness, the buyer's offer per crate), actions per crate and "Sell all", plus close. Remove drop-on-table selling. Money popup + ka-ching stay.
P3. Boat & selling loop: make it feel better: tune day length, boat timing, countdown, offers; the captain shouts should be readable and fun.
P4. The sea is closed off: open the bay to a real horizon on at least one side (open sea, distant islands/ships), keep the rival port across the bay.
P5. Gulls: flying gulls circling, landing on posts, gull sounds (own-made gull model coming in assets-incoming/Models/own-made/).
P6. Graphics need more "oomph": not fully real, not fully cartoony (stylised realism). Do: URP post-processing (colour grading/tonemapping, bloom, SSAO, slight vignette), golden-hour lighting with soft shadows, a proper sky (HDRI or gradient + clouds), distance fog, a better water shader (normal-mapped waves, foam where water meets posts and the hull, reflections), PBR textures on wood/rope/metal (CC0 textures + an HDRI are coming in assets-incoming/ from the asset worker), wet-wood look on the pier edge.
P7. The character is a bean: replace with a rigged, animated character (CC0 Quaternius-style pack arriving in assets-incoming/Characters/): idle, walk, run, jump, carry-idle, carry-walk, throw. Camera/movement are fine, keep them.
Movement and camera: OK as they are.

## UPDATE 00:30: two more polish items (Manan)
P8. Home screen: too basic. Rework: a live 3D title scene (camera slowly drifting over the pier at golden hour, boat bobbing, gulls), a proper "PORT BOSS" logo/title treatment (type, not AI art), menu buttons with hover/click sound, Play / Host co-op / Join / Settings / Quit, a small version tag. Should feel like a real game's front door.
P9. Music: will be made with Suno by Manan/Kufu (see plan/music-brief.md). Build the music system now: slots menu/day/rush/night/storm loaded from Assets/Audio/Music/, crossfades, rush kicks in for the last 30 s before a boat leaves, music volume in settings. Leave slots silent until files arrive; NEVER fetch music from anywhere else.

## UPDATE 00:40: swimming (Manan)
P10. Swimming: the player can fall/jump into the sea and SWIM, always staying at the surface (head and shoulders above water, bobbing with the waves): never goes under, never drowns. Slower than walking, splash sound + splash particles on entry. Falling in with a stack drops the stack (crates float). While swimming you can grab ONE floating crate and tow/push it. Getting out: ladders on the pier and the slipway/shore; press E at a ladder to climb out. Camera stays sensible at water level. Works in co-op (host-authoritative like walking).

## UPDATE 00:45: first music + full settings page (Manan)
- Music: Manan's chosen night track is at `C:\Users\asus\Desktop\Port Boss\music-incoming\night.mp3` (Suno, his pick). Put it in the night slot. It plays in the EVENING and at NIGHT (from late afternoon/sunset through the night shop), crossfading in/out, looping cleanly. Other slots stay silent until he picks them. Note in CREDITS/AI-content log: "Music: generated with Suno (AI), selected by the team".
P11. Settings page (from the home screen and the pause menu), saved between sessions, applied live:
  - Graphics: quality preset (Low/Medium/High/Ultra) plus individual: render scale, shadows (off/low/high + distance), anti-aliasing, SSAO, post-processing (bloom/grading), water quality, view distance/fog, VSync, FPS cap (30/60/120/144/unlimited), field of view.
  - Display: resolution, window mode (windowed/borderless/fullscreen), monitor if easy, brightness/gamma.
  - Audio: master, music, sound effects, ambience (sea/gulls), mute when the window is unfocused.
  - Controls: mouse sensitivity, invert Y, key rebinding for the main actions (grab, drop, throw, sprint, jump, interact/E), controller sensitivity if a pad is connected.
  - Gameplay: camera shake on/off, captain-shout text size, HUD on/off, show controls hint.
  - Reset to defaults, Apply/Back. Defaults auto-picked from the PC (High on this PC).
Do it right after the 3 urgent bug fixes (head, walking backwards, graphics regression).

## UPDATE 03:30: NEW MAP DIRECTION (Manan): demo-level look from day 1, bigger, original
Manan: "make it look like the demo harbour from day 1, but we need originality; the current harbour is too small; the water, the sea, the map, everything is too basic."
- Build a NEW, BIGGER original harbour map entirely from Toon Harbor pieces at the demo's density and quality. Don't copy the demo scene's layout; design our own (our own coastline shape, breakwater, channel, berth arrangement, landmarks).
- Concept that gives demo looks on day 1 AND growth: the player takes over a run-down corner berth inside a big, busy working harbour. Day 1 our berth is small and shabby (a short concrete quay, one old crane, a cabin, clutter, puddles, rust), but everything AROUND it is a full demo-quality harbour: cranes working, container stacks, freighters and tugs moving, trucks, lights, pipes, warehouses. Our berth then grows into the plots next to it (stages 2–5 in PORT-STAGES.md), until it's a full terminal like the demo.
- Our own wooden pier and home-made shed/boat are removed. The fishing boat becomes a pack small boat. The rival is a neighbouring terminal in the same harbour (still modest-ish but clearly ahead).
- Sea: the open sea beyond a breakwater (pack tetrapods/breakwater like the demo), with deep demo-blue water, depth colour and foam at the quay walls and the hulls. Big enough that the horizon is open on one side.
- Map scale: walkable area at least 3–4× the current, with ambient life (moving cranes, trucks on routes, tugs pushing ships, gulls) running on simple loops, cheap to run.
- Performance: use LODs/static batching/occlusion; must hold 60 fps on this PC at High.
- Gameplay stays (boats in → crates → buyer → money, weather, co-op). Re-point the spawn points, buyer, ladders and water volumes to the new map.
Priority until the 09:00 freeze: this map + the carry bug. Quality over quantity: nothing else new. If the full map isn't finished by 09:00, the MORNING build ships the best finished part.
