# Port Boss: game-feel audit (pb-feel, 2026-10-31)

Manan: *"the current animations and the whole character feel wrong."* This page says exactly why, with proof, and gives the
numbers to build to. Planning only: nothing here changes the game.

**What was looked at**
- Release `builds/20261030-2346-REFINE`: its animation films (`proof/refine/merged/anim/`, 24 clips filmed by the game at a
  fixed 30 fps, with the `[PB]` gait log) and its game-camera shots (`proof/refine/merged/test-logs/reg2/`).
- The code that drives the body and camera on `main` a6fe3d4: `Player.cs` (movement, jump, knock-down, camera),
  `CharAnim.cs` (clip blend, IK), `PortBossBuild.MixamoClips` (which Mixamo file goes in which slot),
  `art-src/worker/build_worker.py` (our worker's skeleton).
- Mixamo's `X Bot.fbx` (the body every Mixamo clip is made on), read bone by bone with `tools/fbxbones.py`.
- Reference games: `references.md` (sources with links and dates).
- GIF and MP4 of every clip already exist next to the films: `C:\Users\asus\Desktop\Port Boss\proof\refine\merged\anim\<clip>.gif`
  (e.g. `jog-side.gif` vs `sprint-side.gif` for point 1, `throw.gif`, `jump.gif`, `water-entry-tread.gif`).
- One new film: the knock-down in the real game camera (`work/knock/`, `-pb-knocktest -pb-shots`, batchmode, 0 exceptions),
  made in the one window when the PC had 5.3 GB free.
- **Not filmed (RAM):** the rest of the session the PC had 0.7–4.9 GB free (two Android emulators for another project use
  ~7 GB, then pb-artdir's Unity editor), below the brief's 5 GB bar. Turning and co-op are therefore judged from the code
  (exact numbers, line refs). See *Still to film* at the end.

Files in this folder: numbered images = proof for the diagnosis; `references.md` = the reference games;
`tools/` = the scripts that made the images (`sheet.py` crops/contact sheets, `charts.py`, `fbxbones.py`);
`work/` = raw contact sheets of every clip.

---

## The short answer

The worker is a **wide, short-limbed, small-headed bodybuilder driven by realistic motion-capture clips at speeds those clips
were never made for, with no weight, no anticipation and no physics comedy on top.** Each of those is fixable with free
tools; none of it is one big rewrite.

1. Normal running plays the **Sprint** clip (hunched, fists up) and sprint plays the same clip at **1.65x**: the worker always
   looks panicked, and sprint looks like a fast-forward.
2. The body does not fit the clips: shoulders **1.7x wider apart** than Mixamo's X Bot, arms/legs ~0.9x, so arms swing into the
   chest and the gait reads stiff and scurrying. And it is not a comedy silhouette (head 15% of height, hands 10%; for a comedy read we target a head of 24–30% and big mitts, see fix 2).
3. Actions have **no wind-up and no follow-through**: jump uses Mixamo's *Falling Idle* (a skydiver loop), the throw launches the
   crate 66 ms after the press before the arm moves, and the arm then swings *backwards*. 12 of the 28 Mixamo clips we
   downloaded (Throw, Jumping Up, Picking Up, Getting Thrown, Falling Into Pool...) are not used at all.
4. **No physical comedy body:** no ragdoll; a knock-down tips the whole body back like a plank (always backwards, whatever
   hit you); falling off the quay is an upright run into the water; nothing jiggles.
5. The **camera** is low, close and rigid: the worker is ~20% of the screen, seen from behind, and a carried stack hides him.
6. **No weight:** 0 to full speed in 0.15 s and back, the same with 5 crates as with none; 180-degree turns in 0.25 s.
7. **Thin feedback:** footsteps by distance (not by foot contact), no dust, no landing puff or squash.

---

## Diagnosis, ranked (biggest feel problem first)

Effort: **S** = under a day, **M** = 1–3 days, **L** = a week, for one agent. All fixes use free tools only (Unity built-ins,
Animation Rigging package, Mixamo clips already downloaded, Blender, CC0 sounds).

### 1. Run and sprint both play the Sprint clip; sprint is a 1.65x fast-forward
![run and sprint](01-run-and-sprint-same-clip.jpg)
![speeds](01b-speeds-vs-clips.png)

- **Proof.** Top two rows of `01-run-and-sprint-same-clip.jpg`: the default run (4.6 m/s) and sprint (7.4 m/s) are the same
  hunched, fists-up pose. The game logs the clip speeds on our rig: Standard Walk 1.24 m/s, Standard Run 3.47, Sprint 4.48
  (`proof/refine/merged/anim/log.txt`). Feet slide 0.56 m/s while running (12% of body speed; walking slides 0.10).
- **Cause (speeds + clip choice).** `Player.cs:504` moves at 4.6 / 7.4 m/s: numbers from before the Mixamo swap.
  `CharAnim.GaitWeights` gives a clip full weight once you pass its speed, so at 4.6 m/s the Sprint clip is already at 100%
  and Standard Run is only seen for the ~0.1 s of speeding up. Above the fastest clip the stride phase just runs faster:
  7.4 / 4.48 = 1.65x. With keyboard input the stick is always 100%, so nobody ever sees the walk either.
- **Fix (S).** Move at speeds the clips own (spec below: run 4.2 m/s = Standard Run at 1.2x, sprint 5.8 = Sprint at 1.3x).
  Cap any clip at 1.3x; above that, lengthen the stride procedurally (hips lower, bigger lean) instead of speeding the
  cycle. Add a walk modifier (hold Ctrl / light stick) and an automatic "careful walk" with 4+ crates.

### 2. The body does not fit the clips, and is not a comedy body
![proportions](02-proportions-vs-mixamo.png)

- **Proof.** `02-proportions-vs-mixamo.png`, from the actual skeletons (ours: `build_worker.py` bone positions; X Bot:
  `X Bot.fbx`). As % of height: shoulder joints 28% apart vs 17% (**1.67x**), arm 28% vs 31% (0.92x), leg 43% vs 49% (0.89x),
  hip height 49% vs 54% (0.91x), hand 10% vs 11%, head+neck 21% vs 17% (head alone 15% vs 12%). In the films the sprinting arms pump inside a chest
  that is wider than the clip expects (`work/crop-sprint-side.jpg`, frames 4–8: fists buried in the pecs) and the walk's hands
  hang at the hips with almost no swing (`work/crop-walk-side.jpg`).
- **Your three questions.** *Arm length:* fine (0.92x), the problem is where the arms start, not how long they are.
  *Hand size:* the same as X Bot (10%); fine for the clips, too small for comedy readability. *Pelvis:* hips 0.91x as high but hip
  joints 1.38x wider apart (6.2% vs 4.5% of height), so the legs track wide and short: strides are ~11% shorter and the
  cadence ~12% higher at the same speed. Our in-game skeleton is 1.60 m to the top of the head (scaled 0.89 to fit 1.8 m with the hat).
- **Cause (proportions + rig).** Mixamo clips are motion capture of an average adult with narrow shoulders. Unity's Humanoid
  retarget copies joint *rotations*, so a clip's "fist in front of the sternum" lands inside our barrel chest. The clips are
  also realistic: they carry no squash, no overlap, no exaggeration, so on a toy-like model they look like a man in a costume.
- **Fix.**
  - (S, today's body) A procedural arm offset after the clip: +12–18 degrees of upper-arm abduction scaled by how far the hand is
    inside the torso (`CharAnim.PostPose`, same place as the existing IK); or raise the Humanoid avatar's
    *Arm Down-Up* / *Arm Front-Back* muscle limits for this rig in the importer.
  - (M–L, the new character, with pb-chars) Pick proportions for comedy *and* the clips together: head 24–30% of height,
    hands 13–15%, shoulders ≤ 22% apart (keep the bulk in the belly/back, not the shoulder joints), legs 38–42%. Keep the
    Mixamo-named skeleton so clips still retarget; then re-measure gaits (`[PB] gait` lines) and re-tune the speeds.
  - (L, later) Re-key the 4 locomotion clips in Blender on our rig with exaggerated poses (bigger arm swing, bounce,
    1–2 frame holds), keeping Mixamo only for rare actions.

### 3. No anticipation, no follow-through; wrong clip for jump, no clip for throw
![throw](03-throw-no-windup.jpg)
![jump](04-jump-falling-idle-clip.jpg)

- **Proof.** Throw (`03`): the press is at f8; at f10 (66 ms) the top crate is already flying; the right arm only starts
  moving after the crate has gone and its follow-through ends pointing *behind* the worker (f12–f16). Jump (`04`, side camera
  following the body): the standing jump (f0–f26) is an upright idle-like pose the whole way: no crouch before, no tuck,
  no arms; the running jump (f54–f74) is a hunched ball, then straight back into the sprint with no landing beat.
- **Cause (clip choice + code).** `PortBossBuild.MixamoClips` maps Jump to **"Falling Idle"** (a looping skydiver fall) and
  has no take-off. The throw is not a clip: `CharAnim.PostPose` swings one arm through a fixed angle over 0.45 s
  (`Throw01`), while `Player` releases the crate on the same frame as the press. The landing clip only plays after 0.35 s in
  the air and is cut by running speed (`CharAnim.Tick`). Pick-up/put-down are IK squats (they look fine, `work/crop-pickup.jpg`).
  **Unused downloads** in `Desktop/Port Boss/mixamo-incoming/`: Jumping Up (x2), Throw, Picking Up, Putting Down, Lifting
  Object, Getting Thrown, Falling Into Pool (x2), Jogging With Box, Climbing To Top, Start Climbing Ladder, Laughing,
  Pointing Gesture.
- **Fix (S–M).**
  - Jump: take-off = *Jumping Up* first 0.12 s (crouch) then launch; air = its rising pose, flail arms after 0.4 s;
    land = 2-frame squash + *Falling To Landing* for drops > 0.5 m.
  - Throw: *Throw* clip on an upper-body layer; release the crate at the clip's release frame (~0.25–0.3 s in), not on the
    press. 0.1 s wind-up costs nothing in a comedy game and sells the weight. Hit-stop 40–60 ms on release of a big throw.
  - Use *Getting Thrown* for being flung off a boat, *Falling Into Pool* for going off the quay, *Laughing/Pointing* for
    crew emotes.

### 4. No physical-comedy body: plank knock-down, no ragdoll, no flail, nothing jiggles
![knock](09-knockdown-plank.jpg)
![water](08-fall-into-water-upright.jpg)

- **Proof.** Knock-down (`09`, game camera, a crate thrown at the worker): he goes over flat with straight legs and arms at his
  sides, the Idle pose tipped over like a toy soldier (shots 6–7), then stands up hunched (8). In code,
  `Player.AnimateWorker` rotates the whole model -80 degrees about its own X axis and 8 degrees roll for 1.2 s, **always
  backwards** whatever direction the crate or shove came from, while the clip stays on Idle (`Player.cs:193-201`). Falling off the quay (`08`): the worker runs upright off the
  edge and drops in still in a running pose; the splash and tread are fine.
- **Cause (physics + design).** There is no ragdoll in the project. Every reference comedy game in `references.md` gets its
  laughs from bodies reacting to forces: limbs that flop, heads that lag, falls that go the way you were hit.
- **Fix (M).** "Ragdoll-lite", all free and built into Unity:
  - Build a ragdoll on the worker with Unity's *Ragdoll Wizard* (GameObject > 3D Object > Ragdoll), colliders/joints only.
  - On knock / fling / thrown off a boat / falling off the quay: snapshot the pose, switch the bones to the ragdoll, add the hit
    impulse **in the hit direction** (plus a little spin), stay limp 0.8–1.5 s, then blend 0.3 s from the ragdoll pose into a
    get-up clip (Mixamo *Getting Up* / *Stand Up*: free, one more download with Manan's Mixamo sign-in) facing the way you landed.
  - Network: only the owner simulates; peers get the root + a "knocked" flag and play their own local ragdoll (it is comedy,
    not gameplay, so it need not match exactly).
  - Secondary motion (S): one 20-line spring script on head, hat, belly and hands (damped spring to the animated pose,
    stiffness 120–200, damping 0.5–0.7). No asset needed.
  - Airborne > 0.4 s: arms flail (already exists for "out of stamina", `CharAnim.Flail`; reuse it).

### 5. The camera: low, close, rigid, and the stack hides the worker
![camera](05-game-camera.jpg)

- **Proof.** `05-game-camera.jpg`, the real game camera from the bot run: the worker is ~20% of the screen height, seen from
  behind, the bottom half of the frame is empty ground; carrying, you see the stack more than the man; near sheds fill a third
  of the frame.
- **Cause (camera).** `Player.LateUpdate`: orbit 5.6 m behind (+0.45 m per crate), target 1.7 m up, pitch 18 degrees, FOV 62
  (`GameUI.cs:110`, `Settings.Fov`). Position follows with `1 - exp(-18 dt)` = ~55 ms lag, effectively rigid, and it looks
  straight at the player every frame, so every bob of the body moves the whole world. A wall hit pulls the camera in and up;
  nothing fades. No look-ahead, no sprint FOV kick, no landing dip.
- **Fix (S).** Numbers in the spec: further (7.5 m), higher (30 degrees), narrower (FOV 55), softer (0.15 s spring), looking
  ahead of the run, ignoring the body's bounce, fading walls instead of diving in. One file, `Player.LateUpdate`.

### 6. The carry pose hides the face; the crates are huge on him
![carry](06-carry-hides-character.jpg)

- **Proof.** `06`: front view, one crate already covers the head; 3 and 5 crates turn the worker into legs under a tower.
  Side view: the crate sits in front of the face.
- **Cause (numbers).** Crate 0.55 m (`Crate.Size`), held centred 1.28 m up (`[PB] carry` log) on a body whose head starts at
  1.36 m (in game scale). So the bottom crate's top edge (1.56 m) is above the eyes (~1.46 m).
- **Fix (S).** Carry at the belly: bottom crate centre ~1.0–1.05 m (arms more bent, `CharAnim.CarryAt`), and/or crates 0.45 m.
  With the bigger head from fix 2 the face stays above the first crate. Tilt the head to peek round the stack when walking.

### 7. No weight: instant starts, stops and turns, with or without 5 crates
![curves](07-start-stop-curves.png)

- **Proof.** `07`, from the code: today the worker reaches full speed in 0.15 s and stops in 0.15 s (`Player.cs:505`,
  `MoveTowards` at 30 m/s² on the ground). With 5 crates the top speed drops 35% but the push is the same 30 m/s², so a loaded
  worker starts and stops even *faster* (0.10 s). Turning is 720 degrees/s (`Player.cs:530`): a full reverse in 0.25 s, pivoting
  on the spot, any load. The body only leans by speed (7 degrees at a run, `Player.cs:237`), never into a start or a stop.
- **Cause (speeds + physics).** The stack wobble model (`UpdateSway`) is fed by these accelerations, so the stack feels
  heavy but the man carrying it does not.
- **Fix (S).** Spec below: eased starts/stops whose time grows with the load, turn rate falling with the load, a skid on a hard
  reverse, lean from acceleration (not speed) on a spring.

### 8. Thin feedback on steps and landings
- **Proof (code).** Footstep sounds fire every 1.7 m walked (`Player.cs:449`), not when a foot lands, so at a run they drift
  against the feet; only on the pier height. The jump/land sounds are synthesized blips (`Sfx.cs:53-54`). No dust or puff
  particles exist for steps or landings (`Fx` has only Splash and Spray). No landing camera dip.
- **Fix (S).** Fire steps from the stride phase (`CharAnim` already knows each heel strike), with a small dust puff
  (`ParticleSystem`, 3–5 particles, 0.3 s) at sprint and on landing; land puff + 2-frame squash + 0.1 m camera dip for drops
  over 1 m. CC0 step/land sounds (e.g. Kenney's *Impact Sounds*, CC0) instead of synth.

### 9. Not yet checked: co-op remote players
- Remote players' clip speed comes from their position deltas (smoothed, `CharAnim.Tick`); with network jitter this can
  stutter. Needs a 2-player LAN film (see *Still to film*).

### What already works (keep it)
Walk (upright, feet planted: slide 0.10 m/s), pick-up / put-down squat with hands under the crate (`work/crop-pickup.jpg`),
ladder up/down with hands and feet on the rungs, treading water and the swim stroke, trolley push with both hands on the bar,
the stack wobble model itself, coyote time and jump buffering. The problems above are around these, not in them.

---

## What the reference games do (summary; links in `references.md`)

Full notes, numbers and links (with dates and a label on every claim: dev quote / mod config / decompile / derived /
unverified): **`references.md`**. No public numbers exist for Human: Fall Flat, Gang Beasts, Crashout Crew, Sea of Thieves or
PowerWash; the numbers below come from public mod configs and decompiled code (Lethal Company, R.E.P.O., Overcooked 2).

| Game | Body | Camera | What makes it feel good / funny | What we take |
|---|---|---|---|---|
| **Human: Fall Flat** | full physics, procedural; blobby featureless Bob | soft orbit; arms reach where it looks | "comical swaying and tumbling"; imprecision is the joke | physics only where it's funny (falls, knocks), soft camera |
| **Gang Beasts** | physics instead of an animator; short legs "because there's an invisible ball between them", no feet | shared, pulled back | the body reacts to every force; the comeback after a knock-down | **short-legged bodies suit procedural legs, not mocap**; knock-down + get-up as a beat |
| **TABS** | ragdolls; leg torque follows animation curves + "standing force" + balance script (dev, 2018) | free | deliberate wonk | the documented recipe if we ever go partial active ragdoll |
| **R.E.P.O.** | robots with deliberately floppy heads ("pretty funny", dev 2025); objects held on a soft spring (0.9 / damping 0.5, mod config) | first person, FOV 70 | carried things lag, swing and smash | **spring jiggle on head/hat; springy hold on the carried stack** |
| **Lethal Company** | first person; others see a janky body, ragdoll corpses | FOV 66 → 68 when sprinting (eased) | weight divides speed **and** acceleration; sprint ramps up over ~1–2 s; **0.15 s jump anticipation**; soft/hard landing sounds | **load slows acceleration too; sprint ramps; jump anticipation; 2 landing tiers; small FOV kick** |
| **Overcooked 2** | simple chefs, identical but for colour; locomotion blend on speed 0–1 | one shared fixed tilted camera; levels kept small so it never pulls far back | 1 button pick-up, 1 interact; readability first | **readable silhouettes; blend by normalised speed; higher camera angle** |
| **Crashout Crew** (closest genre) | everyone in a forklift; physics cargo; "Easy-Grab" | none found | grabbing is easy so *losing* the stack is the drama; a visible "crash out" loss-of-control state | **easy pick-up, dramatic losses; a visible tipsy state when the stack teeters** |
| **Sea of Thieves / PowerWash** | first person | first person | instant, obvious feedback on every action ("satisfying") | dust, thunk and pop on every stack, landing and delivery |

**The pattern.** Every comedy reference gets its laughs from bodies and objects *reacting* (springs, ragdolls, physics cargo)
and keeps the controls simple. None of them puts realistic full-human motion capture on a short-limbed toy body, which is
exactly what we do today (point 2). Gang Beasts' developers say outright that the short legs and missing feet came *from* the
physics locomotion. General sources (Swink *Game Feel*, Jonasson & Purho *Juice It or Lose It*, Rosen's GDC 2014 procedural
animation talk, GMTK on Celeste, Toyful's floating-capsule controller) and free code (Unity Animation Rigging; MIT/Apache
active-ragdoll and stylised-controller repos) are linked in `references.md`.

---

## Feel spec (targets to build to)

Built for a **physical-comedy co-op** game: readable from far away, a little slow and heavy so that mistakes are visible and
funny, never sluggish to control. All numbers are for our CharacterController (not an active ragdoll; see the last section).

### Movement

| What | Today | Target | Notes |
|---|---|---|---|
| Walk (light stick / hold Ctrl) | 1.15 m/s (stick 25% only) | **1.4 m/s** | Standard Walk at ~1.1x |
| Run (default) | 4.6 m/s | **4.2 m/s** | Standard Run at 1.2x; −9% keeps the jetty-to-market trip ~27 s |
| Sprint (hold) | 7.4 m/s, instant | **5.8 m/s, ramping up over 0.6 s** (Lethal Company-style run-up), back to run in 0.2 s | Sprint clip at 1.3x; drains stamina only when carrying 3+ |
| Carry speed | −7% per crate | **−8% per crate, and walk-only at 5+** | 5 crates: 2.5 m/s, never sprint |
| Start, 0 → 90% speed | 0.15 s | **0.22 s** no crates, **+0.05 s per crate** (0.45 s at 5) | critically-damped ease, see `07` |
| Stop, 90% → 0 | 0.15 s | **0.18 s** no crates, **+0.07 s per crate** (0.55 s at 5) | the stack keeps going a little: wobble |
| Turn rate | 720°/s always | **600°/s** empty, **−70°/s per crate** (250°/s at 5) | |
| Hard reverse (>120° at >60% speed) | instant pivot | **0.15 s skid**: feet planted, lean back 15°, dust puff, then turn | the comic beat |
| Lean | 7° forward at a run | **forward/back from acceleration**, spring k 60–80, max 12°; bank into turns max 10° | body, not the root |
| Jump height (held / tap) | 1.22 m / 0.65 m | **0.8 m / 0.45 m** | a heavy dock worker, not a platformer hero |
| Jump time to apex / total air | 0.35 s / ~0.6 s | **0.30 s / ~0.51 s** | rise g ≈ 18 (v0 5.3 m/s), fall g ≈ 35 (keep the snappy fall) |
| Jump take-off delay | 0 | **0.08 s crouch** (input is buffered, so it still feels responsive; Lethal Company uses 0.15 s) | anticipation |
| Jump with 3+ crates | allowed, −6% per crate | **a 0.3 m hop that kicks the wobble** | comedy, not traversal |
| Coyote time / jump buffer | 0.12 s / 0.12 s | keep **0.12 s / 0.12 s** | already good |

### Camera (third-person, one per player; each player has their own screen online)

| What | Today | Target |
|---|---|---|
| Distance | 5.6 m (+0.45 per crate) | **7.5 m** (+0.35 per crate, max 9 m) |
| Default pitch | 18° | **30°** (player can orbit 10–60°) |
| FOV (vertical) | 62° | **55°**, +4° while sprinting (0.3 s ease) |
| Look target | 1.7 m up, + side offset when carrying | **1.2 m up** + 0.8 m ahead in the move direction; carrying: aim between the worker and the middle of the stack, no side offset |
| Follow damping | ~0.055 s (`exp(-18 dt)`) | **SmoothDamp, 0.15 s horizontal, 0.30 s vertical** (ignore step bob and jumps; follow height only on landing) |
| Walls | pull in and rise | **fade/dither walls between camera and player**; pull in only for the ground and big hulls |
| Shake | Perlin trauma, ok | keep; add **0.1 m dip on big landings**, **40–60 ms hit-stop** on a big throw or knock; landing sound in 2 tiers (soft < 1 m drop, hard above) |
| Worker size on screen | ~20% of height | **25–30%** with the bigger head; teammates within ~12 m stay in frame |

### Animation blending

| Transition | Target blend |
|---|---|
| Idle ↔ walk/run/sprint | by speed (foot-synced, as now), speed smoothing **0.1 s** |
| Into jump take-off | **0.05 s** |
| Air → land | **0.08 s** in, **0.2 s** out (cut early if running on) |
| Pick-up / put-down / throw (upper-body layer) | **0.10 s** in, **0.20 s** out |
| Carry arms on/off | **0.15 s** |
| Knock / fling → ragdoll | **instant** (pose snapshot) |
| Ragdoll → get-up clip | **0.30 s** from the frozen ragdoll pose |
| Land → swim (falling in) | **0.10 s** |
| On/off ladder | **0.20 s** (as now) |

### What is a clip and what is procedural

| Clip-driven (Mixamo now, re-keyed toon clips later) | Procedural (code, after the clips) |
|---|---|
| Idle, walk, run, sprint | Hand IK on crates, trolley, rungs (exists, keep) |
| Jump take-off, air, landing | Feet planted when squatting / on ladders (exists) |
| Throw (upper body), pick-up, put-down | Lean into acceleration, bank into turns (spring) |
| Get-up (front / back) | Arm offset out of the wide chest (until the new body) |
| Swim stroke, tread, climb, push | Head look-at: nearest teammate / the crate you'll grab |
| Getting thrown, falling into water | Spine counter-lean against the stack sway (the wobble already exists; let the body fight it) |
| Laugh, point, wave (crew emotes) | Spring jiggle on head, hat, belly, hands (Animation Rigging *Damped Transform*, free) |
| | Carried stack held on a soft spring toward the hands (R.E.P.O.-style lag), on top of today's wobble model |
| | Squash/stretch on take-off and landing (scale 0.92/1.06, 2–3 frames) |
| | Ragdoll on knock-down, fling, fall off the quay; flail arms when airborne > 0.4 s |
| | Footsteps, dust, landing puffs from the stride phase |

### Why not a full active ragdoll (Human: Fall Flat / Gang Beasts style)?
Those games build the whole control around a physics body. Port Boss's core is *precise* stacking, racking and selling on a
clock with up to 4 players online; a fully physical body would make every carry a fight and is hard to sync over the network.
The comedy should come from the **stack**, the **water** and the **knock-downs** (ragdoll-lite), while walking and carrying
stay responsive. If the new direction wants more chaos, the step after ragdoll-lite is a "wobbly upper body": physics only on
spine/arms/head (configurable joints driven toward the animated pose), legs and root still kinematic.

---

## Order of work (a suggested pass, ~1.5 weeks for one agent)

1. **S, day 1:** speeds + clip caps (1), weight curves and turn rates (7), camera numbers (5), carry height (6). Re-film with
   `tools/animfilm.sh` and compare.
2. **S–M, days 2–3:** jump take-off/land, throw clip with release on the frame, unused clips wired in (3); steps from the stride
   phase, dust, land puff, squash (8); arm-out offset (2, interim).
3. **M, days 4–6:** ragdoll-lite + get-up + spring jiggle (4); co-op film.
4. **With the new character (pb-chars):** comedy proportions that the clips can live with (2), then re-measure gaits and
   re-tune the speed table.

## Still to film (when the PC has ≥ 5 GB free)
- (done: knock-down, `work/knock/`.)
- `PortBoss.exe -batchmode -pb-solo -pb-bot -pb-shots <dir> 0.15 -pb-quit 45` → camera follow and turning in play.
- LAN co-op (`-pb-host-lan` + `-pb-join-lan 127.0.0.1`, with shots on the client) → remote players' animation. Only when
  nobody is playing a build (a solo game listens on UDP 7777).
