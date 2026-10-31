# Port Boss game-feel references

Research date: 2026-10-31. Read-only web research, no purchases and no sign-ins.

**Labels used below**
- **[dev]**: a developer said it (interview, talk, store page).
- **[mod-config]**: a default value hard-coded in a public mod or tool.
- **[decompile]**: game code that has been decompiled and mirrored publicly on GitHub.
- **[derived]**: my own arithmetic from code values. Not measured in game.
- **[estimate]**: a judgement from watching or reading. Not a measured value.
- **[unverified]**: secondary or community claim that I could not confirm.

Port Boss today, for comparison: run 4.6 m/s, sprint 7.4 m/s, about 0.15 s to accelerate or stop, 720°/s turn, orbit camera 5.6 m behind at FOV 62, near-rigid follow, Mixamo clips, no ragdoll.

---

## Lethal Company (Zeekerss, Unity)

**Why it feels good**
- It is first-person, so your own body barely matters. The other players' bodies matter a lot: their jank, their ragdolls and proximity voice are the comedy. Death spawns a dead-body object (`SpawnDeadBody` exists in the player code [decompile]).
- Weight is physical. Carried loot divides both top speed and acceleration (see the numbers below). You can feel a heavy item.

**Numbers** (vanilla `PlayerControllerB`)
- Walk: `movementSpeed` = **4.6** [mod-config: [Imperium `ImpConstants.cs`](https://github.com/giosuel/imperium/blob/main/Imperium/src/Core/ImpConstants.cs); many mods reset to `4.6f`, e.g. [LethalProgressionPlus](https://github.com/h-nny/LethalProgressionPlus)]. The field's initializer in code is 0.5, so the prefab overrides it. Our run speed is exactly LC's walk speed.
- Sprint: `sprintMultiplier` eases toward **2.25** with `Mathf.Lerp(..., Time.deltaTime * 1f)` and falls back to 1 at rate 10 [decompile: [Timo576/Lethal_company_data `PlayerControllerB.cs`](https://github.com/Timo576/Lethal_company_data/blob/main/Assembly-CSharp/GameNetcodeStuff/PlayerControllerB.cs), last commit 2024-02-20]. [MikesTweaks](https://thunderstore.io/c/lethal-company/p/MikeMediaStudios/MikesTweaks/v/1.5.0/) confirms "max sprint 2.25" and "decrease 10" as the pre-mod values [mod-config].
  - What that means [derived]: sprint tops out near 10.3 m/s but builds slowly, about 1.8× after 1 s and about 2.1× after 2 s. It drops back in about 0.2 s. Sprint is a run-up, not a switch.
- Acceleration: `walkForce = MoveTowards(..., rate * dt)` [decompile].
  - Walking: rate 10 / carryWeight, so 0 to full input in about 0.1 s.
  - Sprinting: rate 5 / (1.5 × carryWeight), so about 0.3 s.
  - Airborne: rate 1.33, so about 0.75 s, which means almost no air control.
  - These figures are [derived] from the rates.
- Carry weight: `num3 = movementSpeed / carryWeight` (base 1) [decompile].
- Jump [decompile]:
  - A **0.15 s anticipation** delay first: the `Jumping` animation starts, then the impulse is applied.
  - `jumpForce` = **13** [mod-config: Imperium `DefaultJumpForce = 13f`].
  - Gravity is 38 u/s², and fall speed is capped at −150.
  - [derived]: apex after about 0.34 s and about 2.2 m high, if 1 unit = 1 m. Not checked in game, so [unverified].
- Landing [decompile]:
  - If fall speed is below −9, a landing is registered.
  - Below −16 it plays a "hit ground hard" sound. Otherwise it plays a "soft" one.
- Camera FOV [decompile]:
  - Base 66, sprint 68, eased with `Lerp(..., 6 * dt)`.
  - The sprint FOV kick is small (2°) and smoothed.
  - Imperium also lists `DefaultFOV = 66` [mod-config].
- Climbing: `climbSpeed` field initializer is 4 [decompile; the prefab value is unverified].
- Animation: there is an `animationSpeed` Animator float, and the player sends it over RPC [decompile]. Clip playback is driven per state.

**Relevant to us**
- A slow sprint ramp.
- Weight that divides acceleration as well as speed.
- An anticipation delay before the jump.
- Two landing tiers.
- A small, smoothed FOV kick.

## R.E.P.O. (semiwork, Unity, released 2025-02-26)

**Why it feels good**
- The robots have floppy, "flappy" heads. Co-CEO Walter Tischkewitz [dev]: "We just played around with that and we realized this is pretty funny" ([GamesRadar+ via Yahoo, 2025-04-23](https://tech.yahoo.com/gaming/articles/lets-just-fail-quickly-repo-142853084.html)).
- Everything is carried with a springy physics grab beam. Valuables swing, collide and break, and that is the core comedy. Players can also deliberately tumble, and a "Tumble-Grab" was added later ([dlcompare, unverified date](https://www.dlcompare.fr/actualites-gaming/decouvrez-la-saisie-par-frottement-dans-la-mise-a-jour-r-e-p-o-62557)).

**Numbers** [mod-config: [imperium-repo `ImpConstants.cs`](https://github.com/giosuel/imperium-repo/blob/main/Imperium/src/Core/ImpConstants.cs), file last changed 2025-07-26]
- `DefaultMovementSpeed = 2`.
- Sprint: Imperium sets `SprintSpeed = value * 2.5`, so the base is 5 ([PlayerSettings.cs](https://github.com/giosuel/imperium-repo/blob/main/Imperium/src/Core/Settings/PlayerSettings.cs)). [RepoAdminMenu](https://github.com/proferabg/RepoAdminMenu) agrees: `SprintSpeed = 5f + upgrades`.
- Units are game units. The player is a physics body, so speeds are not directly comparable [unverified].
- `DefaultJumpForce = 17`. This is probably a Rigidbody impulse, so it is not comparable to LC [unverified].
- `DefaultSlideTime = 0.5`.
- `DefaultFOV = 70`.
- **Physics grabber**:
  - spring constant 0.9, damping 0.5
  - force 4, max force 4
  - min / max hold distance 1 / 2.5
  - range 4, release distance 8

  This is a soft PD spring, and it is why held objects lag, swing and overshoot.

**Relevant to us**
- Move held crates with a soft spring and damper toward a hold point, not by parenting them.
- Wobbly heads as cheap secondary motion.
- A voluntary tumble or ragdoll as a comedy button.

## Human: Fall Flat (No Brakes Games / Tomas Sakalauskas, Unity, 2016)

**Why it feels good**
- It is fully procedural and physics-driven. Sakalauskas [dev]: "I had to use procedural animation… there was no way anyone would agree to motion capture this for me." The reporter adds: "something about the way you move that's just inherently funny" ([bit-tech preview, 2016-07-14](https://www.bit-tech.net/previews/gaming/human-fall-flat-preview/1)).
- Each trigger raises one arm. Arms reach toward where the camera looks, and anything the hand touches sticks to it ([bit-tech 2016-07-14](https://www.bit-tech.net/previews/gaming/human-fall-flat-preview/1); [Xbox Wire, 2016-03-17](https://news.xbox.com/en-us/2016/03/17/gdc-2016-human-fall-flat-xbox-one/)).
- "Every movement you make comes with comical swaying and tumbling" ([Xbox Wire 2016-03-17](https://news.xbox.com/en-us/2016/03/17/gdc-2016-human-fall-flat-xbox-one/)).
- Bob is a featureless, blobby humanoid, so the motion reads clearly with no facial animation.
- Playtesting showed his son ignored the puzzles to play with the physics, so the puzzles were loosened ([Wikipedia](https://en.wikipedia.org/wiki/Human:_Fall_Flat)).

**Numbers**: I found no public data-mined speeds or joint values.

**Relevant to us**
- Arms driven by intent: "grab" as a physical reach.
- Imprecise stops and turns are acceptable when the game's goal is comedy.
- The camera stays on a soft orbit around a body that is never rigid.

## Gang Beasts (Boneloaf, Unity, 2014 to present)

**Why it feels good**
- Physics replaced animation [dev]: "because we don't have an animator, we can't make smooth animations… let's try it with physics."
- **The physics shaped the character design** [dev]:
  - "They have short legs, because there's an invisible ball between them."
  - "They don't have feet because the feet were getting caught on things."
  - Both quotes: [MCV/DEVELOP, 2021-04-08](https://mcvuk.com/?p=215072).
- The punch was the first feel-good mechanic. A taunt and the comeback ("coming back after your opponent thinks he's been victorious") are central [dev] ([bit-tech interview, 2014-05-06](https://bit-tech.net/reviews/gaming/pc/interview-gang-beasts/1/)).
- Double Fine's description: "silly procedural animation and ragdoll physics" ([Double Fine](https://www.doublefine.com/news/double-fine-presents-gang-beasts)).

**Numbers**: none public.

**Relevant to us**
- Short-limbed characters suit physics locomotion. A ball or capsule rolls the body along, and legs and arms are mostly secondary motion.
- Our short-limbed worker on full-human Mixamo clips is the opposite approach. That is the likely cause of the "feels wrong" complaint [estimate].

## Totally Accurate Battle Simulator (Landfall, Unity)

**How the units move** [dev] ([Game Developer, 2018-10-15](https://gamedeveloper.com/design/how-landfall-games-finds-the-fun-in-physics-engines)):
- Units are ragdolls. Torque on the legs follows **animation curves**, which gives a basic walk cycle.
- A **step handler** decides when one step ends and the next begins.
- A **standing handler** measures the head-to-floor distance and pushes the torso and head up. It uses a curve: x = ground distance, y = force.
- A **balance script** pushes the feet back under the centre of mass.
- This is Landfall's third animation system. It is component-based ("each of these are their own scripts that you can pick and choose from").

**Why it's funny**
- The wonkiness is deliberate. When players don't take it seriously, the studio can do more.
- For Totally Accurate Battlegrounds (first person), what you animate is also what others see. The head bobbing and goofy movement became part of the charm (same article).

**Relevant to us**
- The "standing force curve plus balance" recipe is the simplest active-ragdoll recipe and it is documented.
- We could apply it only to the upper body or the carried stack.

## Overcooked 1 / 2 / All You Can Eat (Ghost Town Games / Team17, Unity)

**Why it feels good**
- The controls are deliberately minimal [dev]: "1 button to pick up, 1 button to interact and a thumbstick to move."
- All chefs are identical, with no per-chef stats.
- Contents are shown as icons ([Game Developer Deep Dive, 2016-08-26](https://www.gamedeveloper.com/design/game-design-deep-dive-building-truly-cooperative-play-in-i-overcooked-i-)).
- Camera [dev]: levels were kept compact because the team "didn't want levels to get too sprawling and for the camera to pull all the way back" ([MCV Develop post-mortem, 2017-06-26](https://mcvuk.com/the-develop-post-mortem-overcooked/)). It is a single shared, fixed, tilted camera (a `MultiplayerCamera` script is listed in the execution order in [this OC2 modding doc](https://github.com/Shyngyskhan-333/overcooked2-eight-players)). The tilt angle and FOV are [unverified].
- Tuning was fragile: a change to move speed or chop speed meant re-tuning every score ([Road to the IGF](https://gamedeveloper.com/design/road-to-the-igf-ghost-town-games-i-overcooked-i-)).

**Numbers**
- Overcooked 2 chef `RunSpeed = 6` (game units per second) for all four chefs in Story 1-1 [decompile/TAS research: [overcooked-supercharged ANIMATOR-REWIND-PARITY.md](https://github.com/hpmv/overcooked-supercharged/blob/master/docs/ANIMATOR-REWIND-PARITY.md), 2026-09].
- The Animator locomotion parameter is `Clamp01(xzSpeed / RunSpeed)`, i.e. a 0–1 speed blend (same doc).
- A dash exists with speed, duration, cooldown and turn-speed fields. I did not find the real values. The TAS test fixtures (12 or 18 u/s, 0.25 s, 0.8 s) are made-up test data, so I have **not** used them.

**Relevant to us**
- Run the camera at a fixed angle, shared by everyone, framing the whole play space for co-op readability, instead of an orbit camera 5.6 m behind one player.
- Drive the locomotion blend from a normalised 0–1 speed.
- Use one fast dash instead of a sprint.

## Crashout Crew (Aggro Crab, released 2026-05-28)

**What it is**: 1–4 player online co-op. Everyone drives a forklift, with "boost and drift capabilities". Boxes are physics objects ("a physics-based nightmare!!"). An "Easy-Grab" makes stacking easy ([Steam page](https://store.steampowered.com/app/3583210/Crashout_Crew)).

**Why it's funny**
- A stress meter fills when you hit walls or other players. At the limit you "crash out" and drive erratically, which can chain into other players' crashouts ([Wikipedia summarising PC Gamer, 2025-10-14](https://en.wikipedia.org/wiki/Crashout_Crew); [Stack Up review](https://www.stackup.org/post/review-crashout-crew)).
- "All it takes is one good bump, and you'll send your cargo flying" (Stack Up).
- Stack Up found keyboard controls "stiff and awkward" and recommends a controller ([Stack Up](https://www.stackup.org/post/review-crashout-crew)).

**Numbers and camera**: none found. Camera type is [unverified]. I have not watched footage, so there is no estimate.

**Relevant to us**
- The genre is closest to ours. Cargo physics carries the comedy.
- Grabbing is made easy so that losing the stack is the drama.
- An emotional "loss of control" state, visible to everyone, is a comedy multiplier.

## Sea of Thieves (Rare, Unreal)

**What I found**
- First person was chosen for immersion. Third person was planned only "selectively" ([DualShockers, 2016-07-18](https://www.dualshockers.com/rares-sea-of-thieves-get-more-info-on-first-and-third-person-player-interaction-and-more/)). As with LC, the character feel is mostly how other pirates look.
- The stylised look comes from "a few simple rules" applied to every visual ([GDC 2018 "Visual Adventures on Sea of Thieves", Ryan Stevenson](https://www.youtube.com/watch?v=BzppoQTG3m0); [80.lv write-up, 2018-04-09](https://80.lv/articles/gdc18-visual-adventures-on-sea-of-thieves/)).

**Not found**: no public animation or locomotion talk and no data-mined walk, sprint or swim speeds. Claims about exaggerated third-person animation are [unverified]. The "Visual Adventures" talk is the best primary source to watch.

**Relevant to us**: swimming and ladders are core verbs in both games. There is no public reference to borrow from here.

## PowerWash Simulator (FuturLab, Unity)

**Why it feels good**
- Instant, obvious progress. "The one word we hear more than any other in our feedback is 'Satisfying'" [dev], Dan Chequer ([Kotaku, 2021-07-19](https://kotaku.com/powerwash-sim-devs-on-making-cleaning-fun-advanced-cro-1847319502)).
- No timers or antagonists. "keep everything else out of the way of the player" ([Epic Store interview, PWS2](https://store.epicgames.com/news/powerwash-simulator-2-futurlab-interview-nick-mccarthy-sequel-add-leave-out)).
- Realistic and satisfying water physics balanced as "the very core" ([FuturLab](https://www.futurlab.co.uk/news/world-intellectual-property-day-how-did-powerwash-simulator-come-to-be)).

**Numbers**: none found. It is first person, so there is no character to compare with ours.

**Relevant to us**: immediate feedback on every action, such as a crate landing, a crate stacking or a delivery.

---

## General game-feel sources

- **Steve Swink, *Game Feel*** (2008). Defines feel as "real-time control of virtual objects in a simulated space, with interactions emphasised by polish". Its building blocks are input, response, context, polish, metaphor and rules ([Wikipedia](https://en.wikipedia.org/wiki/Game_feel); [survey, arXiv 2011.09201](https://arxiv.org/pdf/2011.09201)).
- **Jonasson & Purho, "Juice It or Lose It"** (GDC Europe 2012). A dull game becomes satisfying through tweening, squash, particles, shake and sound, demonstrated live, with source code ([GDC Vault](https://www.gdcvault.com/play/1016487/juice-it-or-lose)). Counterpoint: Folmer Kelly, "Don't Juice It or Lose It" (2014) ([GDC Vault](https://gdcvault.com/play/1020861/Don-t-Juice-It-or)).
- **David Rosen, "An Indie Approach to Procedural Animation"** (GDC 2014 Animation Bootcamp). Fluid, interactive animation from very few keyframes ([GDC Vault](https://gdcvault.com/play/1020583/Animation-Bootcamp-An-Indie-Approach); [YouTube](https://www.youtube.com/watch?v=LNidsMesxSE); [Game Developer, 2014-05-05](https://gamedeveloper.com/design/video-an-indie-approach-to-procedural-animation)). Community summaries say:
  - about 4 poses per gait cycle (pass and reach, left and right)
  - spring-like interpolation between poses
  - "responsiveness in physics first, then animation on top"

  Sources: [TAMU reimplementation](https://people.engr.tamu.edu/sueda/courses/CSC474/2016W/demos/ismithgr/index.html); [Unity forum, 2014-05](https://discussions.unity.com/t/an-indie-approach-to-procedural-animation-gdc-video-talk/538228). The "13 keyframes total" figure is [unverified].
- **GMTK, "Why Does Celeste Feel So Good to Play?"** ([YouTube](https://www.youtube.com/watch?v=yorTG9at90g)). Covers coyote time and jump buffering, confirmed by Matt Thorson ([GameSpot, 2020-03](https://www.gamespot.com/articles/celeste-dev-explains-how-they-made-their-game-feel/1100-6474775/); [Maddy Thorson "Celeste & Forgiveness"](https://maddymakesgames.com/articles/celeste_and_forgiveness/index.html)).
- **Toyful Games, "Making A Physics Based Character Controller In Unity (for Very Very Valet)"** ([YouTube](https://youtube.com/watch?v=qdskE8PJy6Q)). Describes a floating capsule: spring and damper ride height plus an upright torque. Free MIT re-creation with squash and stretch, oscillators and dust: [joebinns/stylised-character-controller](https://github.com/joebinns/stylised-character-controller) (pushed 2024-05; [itch page](https://joebinns.itch.io/stylised-character-controller)).
- **Unity Animation Rigging** (free package). Includes Damped Transform, Multi-Aim, Two Bone IK and Override Transform constraints ([docs, Constraint Components](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0/manual/ConstraintComponents.html)). It is the free substitute for most of Final IK. Paid reference: RootMotion PuppetMaster, an active ragdoll with Active, Kinematic and Disabled modes ([Asset Store](https://assetstore.unity.com/packages/tools/physics/puppetmaster-48977/reviews)).
- **Open-source active ragdolls (Unity)**
  - [sergioabreu-g/active-ragdolls](https://github.com/sergioabreu-g/active-ragdolls) (Apache-2.0, 2021): a hidden animated copy, with each ConfigurableJoint angular drive matching its bone's rotation.
  - [ashleve/ActiveRagdoll](https://github.com/ashleve/ActiveRagdoll) (MIT, 2023): PID-driven.

---

## Takeaways for Port Boss

| # | Technique | Games that do it | Free way in Unity | Effort |
|---|---|---|---|---|
| 1 | Hold crates on a soft spring and damper toward a hold point (lag, swing, overshoot) instead of rigid parenting | R.E.P.O. (spring 0.9, damping 0.5), Crashout Crew, HFF | PD force on the crate Rigidbody, or a ConfigurableJoint with low drive values | S–M |
| 2 | Secondary motion: wobbly head, hat, arms, stack sway | R.E.P.O. "flappy heads", Gang Beasts | Animation Rigging Damped Transform; joebinns oscillators | S |
| 3 | Procedural lean into acceleration and turns, squash and stretch on jump and land | Gang Beasts / HFF (emergent), joebinns controller | Rotate a visual-root bone from velocity delta; scale pulse on land | S |
| 4 | Real ragdoll for knock-down and falling into the harbour, then a get-up | Gang Beasts, HFF, TABS, R.E.P.O. tumble, LC dead bodies | Unity Ragdoll Wizard; blend back with a get-up clip | M |
| 5 | Partial active ragdoll (animated legs, physical upper body and arms) | HFF, Gang Beasts, TABS | sergioabreu-g/active-ragdolls (Apache-2.0) or ashleve (MIT); TABS "standing force + balance" recipe | L |
| 6 | Locomotion built for short limbs: a ball or floating capsule rolls the body, legs are mostly cosmetic | Gang Beasts ("invisible ball between legs"), Very Very Valet | joebinns stylised-character-controller (MIT) | M–L |
| 7 | Replace full-human Mixamo walk/run with a few stylised poses plus spring interpolation, or clips made for the proportions | Overgrowth (Rosen) | Animator with 4-pose cycle, or blend tree on a normalised 0–1 speed like OC2 | M |
| 8 | Match playback rate to ground speed so feet don't slide | LC (`animationSpeed` param), OC2 (`xzSpeed/RunSpeed`) | `animator.SetFloat("speed", v / clipSpeed)` | S |
| 9 | Sprint ramps up over about 1–2 s; carried weight divides speed **and** acceleration | LC (×2.25 at lerp 1/s; walkForce rate 10/carryWeight) | Scale accel and top speed by stack mass | S |
| 10 | Jump anticipation, low air control, coyote time and jump buffer | LC (0.15 s pre-jump, 1.33/s air control), Celeste | Timers in the controller | S |
| 11 | Landing feedback: tiered sound, dust puff, small shake | LC (soft/hard at −9 / −16), joebinns dust | Particle System + Cinemachine Impulse | S |
| 12 | Co-op camera: shared, fixed-angle, frames everyone, not a rigid 5.6 m orbit; add damping | Overcooked (single compact view), TABS (free camera) | Cinemachine Target Group + Framing Transposer with damping | S–M |
| 13 | Small smoothed FOV kick on sprint | LC (66 to 68, lerp 6/s) | Lerp `fieldOfView` | S |
| 14 | Juice on throws and impacts: hit-stop, shake, squash | "Juice It or Lose It" | `Time.timeScale` dip for 50–80 ms [estimate]; Cinemachine Impulse | S |
| 15 | Readability: identical chefs plus colour, simple silhouettes, icons over held items | Overcooked (identical chefs, icon UI), HFF (featureless Bob), Gang Beasts | Art and UI work | M |
| 16 | Shared "loss of control" comedy state | Crashout Crew (stress → crash out), R.E.P.O. tumble | State-machine flag that switches to ragdoll or erratic input | M |
