# Port Boss: handover to Manan (29 Oct 2026, evening)

Hi Manan. After your MORNING build, Tushar played the game five times and asked for changes after each session. This file
covers all five rounds: what changed in the game, what changed in the code, what was tested, and what's still open.
The setup steps and project rules are still the ones in `HANDOVER-TUSHAR.md`; read that if you set up a new PC. The last
section, **"For Manan's Claude"**, is written for your agent.

Other files: `docs/HOW-TO-PLAY.md` (updated for everything below), `LOG.md` (one line per step, rounds 1–5 at the end),
`tools/README.md` (scripts and every `-pb-*` test switch).

---

## 1. Where things stand

- **`main`** now holds all five rounds. They were made on the branch `improve-stage1`, which was merged into `main`
  without a merge commit. The rounds are 5 commits, `3e12d17` to `f134341`, plus this handover.
- **Everything works without the Toon Harbor Pack being in git.** As before, the pack stays out of the repo. Import it
  with section 3 of `HANDOVER-TUSHAR.md`.
- **No release build has been made from these rounds yet.** Tushar played development builds on his PC. Make a release
  with `bash tools/release.sh` before anyone else plays.
- **Tests, on the last build:**
  - `tools/reg2.sh` had 0 exceptions in every run. The solo bot sold 6 crates; LAN co-op sold 9 ($132).
  - Every feature test passes: `-pb-evicttest`, `-pb-storeshots`, `-pb-pushtest`, `-pb-trolleytest`, `-pb-boardtest`,
    `-pb-contracttest`, `-pb-handletest`, `-pb-boattest` and `-pb-trucktest` (stops, queue of 5).
- **Nobody has seen these yet.** The on-screen menus are IMGUI, which doesn't show in batchmode screenshots, and nobody
  has played the last build in a window. Please look at them when you play:
  - the new end-of-day card;
  - the Tab shop;
  - the intro captions;
  - Marlowe's speech bubble;
  - the soak drops on screen;
  - the freshness rings and the Stats screen.

---

## 2. What changed in the game

### The map
- **The yard is twice as big** (x −60..90, z −90..0). It is walled in with a concrete wall and lamps. The back gate has a
  PORT BOSS board and a booth.
- **The fish market is now across the road** outside the back gate, about 115 m from the jetty. It's a red-roofed market
  hall with:
  - a counter, fish on ice, a chalkboard of prices and a FISH MARKET sign;
  - some idle customers.

  It replaces the FRESH FISH stall.
- **The terminals that stood either side of our berth are gone.**
- **Marlowe Port** is now a mega port about 600 m out across the sea, built in `HarbourMap.MarlowePort`. Its freighters
  come and go all day (`RivalTraffic`).
- **Distant land is a generated landscape** (`Scenery.cs`): a headland, beaches, woods, snowy mountains and three islands,
  about 51k triangles and 3.6k trees.
- **New sea, sky and land shaders:**
  - The sea is our own toon shader, `PortBoss/Water`. The pack's ToonWater override was removed because its tiled normal
    map glittered in the distance.
  - Clouds use `PortBoss/ToonCloud` and drift.
  - The landscape uses `PortBoss/ToonLand`.

### The road and the lorries
- **14 lorries** use the road.
- **They brake for anything in their lane:** people, the trolley, the forklift and loose crates.
- **They honk and shout, but never hit anyone.** Lorry knock-downs were removed.
- **At most 5 queue behind a blocker.** The rest wait off the road, so nobody can cause an endless traffic jam.
- **They only stop for things in front of the cab.** Standing beside a passing trailer no longer parks it across the
  crossing.
- **Every peer simulates them.** The host corrects positions twice a second (`PortGame.TrafficRpc`).

### Crates, the crate store and selling
- **Crate store at the foot of the jetty:** shelving racks under a tin roof, built by `Storage.cs`. Fish on the racks
  spoils more slowly, and gulls and wind can't get it.
- **Crate store upgrades in the shop:**

  | Level | What it adds | Crates | Price |
  |---|---|---|---|
  | 1 | two racks (the starting store) | 12 | — |
  | 2 | a third rack | 18 | $250 |
  | 3, the **cold store** | a fourth rack, a chiller unit, icicles, cold mist and a COLD STORE sign | 24 | $600 |

  - In the cold store, stored fish spoils at 0.15× the open-air rate instead of 0.4×.
  - New racks go west, away from the jetty.
  - The level is `PortGame.StoreLevel`. Every peer rebuilds the racks and the dressing when it changes.
- **Crate handling:**
  - **Q** puts the top crate where it belongs: on the rack, into the trolley, onto a pallet, or upright on the ground.
  - Holding **E** or **Q** repeats.
  - Crates glide between places, and the character bends and reaches.
  - HUD prompts and a marker show which crate E will take.
- **Freshness ring** over nearby crates:
  - It drains as the fish goes off and blinks just before the price drops.
  - The buyer's price is in the middle, and red dots under it count knocks.
- **Sell all** is one sale with one popup, e.g. "+$45 for 3 crates".
- **HUD text no longer wraps or gets cut off.** One-line text shrinks to fit (the `OneLine` helper).

### Boats
- **Boats are solid:**
  - The bridge keeps the pack's collision mesh.
  - A hull pushes people out to the side.
  - A boat under way shoves people: swimmers are pushed aside, and anyone on the quay is knocked over.
- **Boarding is smooth.** A bollard and a kerb sat right where the gangplanks land, and you got stuck on them. Both are
  gone, and docked boats no longer push people.
- **No stowaways:** anyone still aboard when a boat casts off is thrown back onto the jetty by the captain. The captain
  turns, shoves and shouts, and you keep your crates. If the boat is already far out, you go over the side into the sea.
  (`Boat.Evict`, `Player.ThrownOffRpc`)
- **Captains take turns.** The contract board is gone, because Tushar found it confusing.
  - Mags comes on days 1–2, then Bjorn every other day.
  - The morning shout says who's coming.
  - The code is `PortGame.CaptainFor(day)`. `NextContractId` and `ChooseContractRpc` were removed.

### The end of the day and the shop
- **New end-of-day card.** It's small and centred:
  1. "Day N done!" and the verdict line.
  2. Three rows that count up: money earned, crates sold and crates lost.
  3. **1–3 stars**, based on how much of the catch reached the market. Spoiled crates count half.
  4. The crew tin bar, and tomorrow's captain.
  5. Three buttons: **Shop**, **Stats** and **Start next day**.

  Clicking the card skips to the end. The old two-panel card with the contract board was removed.
- **Shop (Tab, or Back/View on a gamepad), any time of day.**
  - **What it lists:** trolley $120, bigger crate store $250, cold store $600, forklift $350 (from day 3) and Concrete
    quay $1500.
  - **Buying:** prices are shown, with "need $X more" when you can't afford something. Purchases arrive straight away
    with a message saying where they are.
  - **Market prices:** a line at the bottom shows what the market pays per crate.
  - **Who pays:** anyone in the crew can buy; it comes out of the shared tin.

  The night shop was removed, and the buy functions no longer require night.
- **Stats** (`Stats.cs`):
  - It shows career stats for this run and for all time (saved on that PC).
  - It's reached from the end-of-day card and from the pause menu.
  - It counts days, money, best day, crates sold, spoiled, lost and carried, knocks, swims, distance walked, lorry honks,
    "soaked by Marlowe" and "thrown off a boat".

### Marlowe, the rival
- **Drive-by:**
  - He swings in hard off the end of the jetty, and the spray soaks anyone standing there (drops on screen).
  - He stands in the boat laughing and pointing, with a speech bubble and a cheeky horn tune.
  - He soaks you again as he leaves.
  - Marlowe is the pack's character 03A wearing a captain's hat.
- **Banner plane:** a red plane drones over the yard every ~5 minutes towing one of five insult banners. The banner turns
  to face the camera so it can be read.
- **Fireworks** go off over Marlowe Port every time one of his freighters docks. They use the `PortBoss/Spark` shader,
  which is additive and not fogged, so they read from 600 m away.

### Story intro
- **A 44-second camera flight opens every new game.** It's skippable with Space, Esc, Enter, a click, or Start/A on a
  pad. In order:
  1. It shows Marlowe Port and his fireworks.
  2. It closes in on his sign while he says his line.
  3. It sweeps back to Grandpa's old pier.
  4. It explains the job and the build-up path: Concrete quay > Crane + warehouse > Container berth > Mega port.
  5. It ends on the PORT BOSS title.
- **The host's day clock waits** until the intro ends (`Intro.cs`).

### Players
- **Swimming:**
  - The body sits right in the water: head and shoulders out, a stroke when moving, treading water when still.
  - Going into the water is a dunk and bob back up. Players only flail when out of stamina.
- **Breath is now called stamina.** It's about 25 s, less when sprinting. When it runs out, the harbour master fishes
  you out onto a ladder.
- **The jump** is higher and snappier: hold for about 1.2 m, tap for about 0.6 m. It has coyote time and an input buffer,
  and new jump and land sounds.
- **Running** uses gaits matched to the clips' measured speeds:

  | Gait | Speed |
  |---|---|
  | walk | 1.1 m/s |
  | jog | 3.8 m/s |
  | sprint | 6.2 m/s |

  The `Sprint_Loop` clip was added, and the body leans and banks into turns.
- **The carry pose** holds the stack at the belly, hands under the bottom crate, leaning back.

### Trolley
- **New look:** a blue platform cart with a plank deck, tube rails, a push bar with a black grip, and fat tyres.
- **Pushing was reworked.** It used to be dragged by forces: it lagged, swung round and ran into you.
  - Now it's **held**: press F and you step in behind the handle.
  - The trolley sits right in front of your hands on every peer (`Trolley.HeldPose`).
  - You can only move or turn where it fits. It stops at walls, crates, people and the water's edge (`Trolley.Fits`).
  - If you let go on the slipway, it still rolls into the sea.

### Look and sound
- **Font:** **Fredoka** (Bold for titles, numbers and buttons; SemiBold for text) everywhere, including signs and the logo.
  It's OFL-licensed, in `Assets/Art/Fonts/` with its licence. Alfa Slab One and the default Arial are gone.
- **Weather:** the rain emitter was fixed (drops were falling under the sea), and the rain, wind and thunder sounds were
  replaced with synthesized ones.

---

## 3. What changed in the code

**New files**
- **`Game/Storage.cs`:** crate store slots and levels.
- **`Game/Stats.cs`:** career stats, saved in PlayerPrefs under `pb.stat.*`.
- **`Game/Scenery.cs`:** the landscape mesh and clouds.
- **`Game/Rival.cs`:** `BannerPlane` and `Fireworks`.
- **`Game/Intro.cs`:** the flythrough. Its captions are drawn by `GameUI.DrawIntro`.
- **Shaders in `PortBoss/Shaders/`:** `ToonCloud`, `ToonLand` and `Spark`. `Water` was rewritten.

**Big changes in existing files**
- **`GameUI.cs`:**
  - new screens: the night card, `DrawShop`, `DrawStats`, `DrawIntro` and the freshness rings;
  - the HUD helper `OneLine`;
  - Tab handling.
- **`PortGame.cs`:**
  - captains take turns;
  - `StoreLevel` and `BuyStoreRpc`;
  - the shop buy functions work any time and send a toast;
  - `TrafficRpc`;
  - the intro holds the clock.
- **`Player.cs`:** stamina, `BoatPush`, the dunk, gaits and lean, held trolley pushing (`TrolleyClear`, `PushChanged`),
  `ThrownOffRpc` and `Fly`.
- **`Boat.cs`:** the solid bridge, `Velocity`, `Evict` and the captain's throw animation.
- **`HarbourMap.cs`:**
  - the market across the road;
  - `RoadTraffic` (lorries);
  - `BuildCrateStore`, which can be rebuilt;
  - `MarlowePort` and `MarloweSign`.
- **`World.cs`:** the wall and gate, `BuildRacks` (can be rebuilt), and the gangplank landings cleared.
- **`Speedboat.cs`:** rewritten for the drive-by.
- **`CharAnim.cs`:** gaits, the carry pose, and `Laugh` / `PointAt` for Marlowe.
- **`Sfx.cs`:** synthesized tick, rain, wind, thunder, taunt horn, boom and plane sounds.
- **`Trolley.cs`:** rewritten. The prefab's colliders in `PortBossBuild.cs` changed to match the new model.

**Generated assets:** `PortBossBuild.Setup()` still makes the scene, prefabs, materials and skin.
- New material slots: `Land` and `Spark`. New skin slots: `Sprint` and `Rival`.
- `Skin.asset` is committed with these.
- As before, don't commit the regenerated changes to prefabs, the scene, materials or ProjectSettings.

**New test switches** (all in `AutoRun.cs`, listed in `tools/README.md`):
- `-pb-handletest <dir>`, `-pb-staminatest`, `-pb-trucktest`, `-pb-boattest`, `-pb-boardtest`
- `-pb-intro <dir>`, `-pb-rivalshots <dir>`
- `-pb-evicttest`, `-pb-storeshots <dir>`, `-pb-pushtest <dir>`

`-pb-contracttest` now checks that day 3 is Bjorn's.

---

## 4. Still open

1. **Look at the on-screen menus** listed in section 1 in a real window. They haven't been seen yet.
2. **Make a release build** (`tools/release.sh`) for the next playtest.
3. **Test Steam co-op across two accounts.** It's still untested: same-account tests can't use Steam relay. Use your PC
   and Tushar's.
4. **Make the 75 s capture.** It's still not made, because it needs a visible window (`tools/capture.sh`).
5. **Check that held trolley pushing feels right for a client.** It's built so the client pusher sees no lag, but only
   LAN on one PC has been tested.
6. **Build stage 3** (crane + warehouse) from `docs/PORT-STAGES.md`. This is the next big piece of work.
7. **Known, harmless:** the pack's shader logs 3 "duplicate keyword" errors at build time. The game is still on test
   Steam app 480.

**Rules (unchanged):**
- The Toon Harbor Pack is paid and the repo is public: never commit `Assets/ThirdParty/` or `Assets/Toon Harbor Pack/`.
  Before every commit, `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` must print **0**.
- No secrets in the repo.
- Players get release builds only.
- The art direction is the Toon Harbor demo scene.
- Weather never warns.
- Don't open windows unless someone has said it's OK.
- Work on a branch, then merge after review.

---

## For Manan's Claude

You're picking up `main` after Tushar's five playtest rounds. `HANDOVER-TUSHAR.md` → "For Tushar's Claude" still describes
the loop, the tools and the hard rules. They apply to you unchanged, especially the pack-file check before every commit.

- **What's new:** read `LOG.md` from the 13:52 line onwards for rounds 1–5 in detail.
- **Where to start:** for the systems above, start from the files named in section 3.
- **The scene is generated:** it is built by `Editor/PortBossBuild.cs`. Change that code, not the scene, prefabs or
  materials by hand.
- **IMGUI isn't in batchmode screenshots.** Check UI logic through `[PB]` log lines, and ask Manan to look at the screens.
- **Test changes headless:**
  - Run `bash tools/reg2.sh` (expect 0 exceptions; the solo bot sells about 6 crates, LAN co-op about 9).
  - Run the feature switches in section 3 that touch what you changed.
- **Avoid these:**
  - `RoadTraffic` runs on every peer and is corrected by the host: keep it deterministic.
  - `Storage.SetLevel` rebuilds the racks on every peer: never move the first two racks, or stored crates float.
  - A held trolley is posed in `LateUpdate` on every peer from the pusher's transform: don't add physics forces to it
    while it's held.
