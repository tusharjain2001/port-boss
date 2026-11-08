# M8: surfing and fishing gear (plan, 8 Nov 2026)

**Branch:** `m8` (from `main` @ `4e4a209`, after M7 was played and merged). **Status:** all 8 steps built (8 Nov). **Waiting for Manan's hand test (★)**: Tushar said "manan will test once m8 will be fully completed". Each step is tested headless and committed on `m8`. The branch is pushed to GitHub, and it goes to `main` only after Tushar or Manan plays it (HANDOVER rule 10).

**Scope** (systems §5.2 M8):
- Swell Surf Co. and surfing (systems §1.7, world §10.3, economy §4.2);
- fishing F10–F11 (fishing §6.3–6.6, §4.2): tackle, rod holders, the landing net, trolling.
- **The gear guns are not built.** Manan hasn't answered Q1 (HANDOVER §9: are guns OK as tools that never hurt people, and at what prices?). They wait for his answer.

## Tushar's decisions (8 Nov)
| Question | Answer |
|---|---|
| M7 | Played by Tushar and Manan; merged into `main` and pushed. |
| What to build | M8, starting with surfing and the fishing gear; no guns until Manan decides. |
| How | The same as M7: branch `m8`, each step tested headless and committed, the branch pushed, merged after a hand test. Unity may be closed and reopened for Blender exports. |

## Where the gdd sections disagree, and the pick
- **Boards:** systems §1.7 lists Soft-top / Fish / Longboard, while economy §4.2 lists Foam $350 / Bodyboard $200 / Longboard $1,200 / Shortboard $900 (Kai's trick challenge). **Economy's shop table wins on names and prices** (as fishing §6.2 already does for rods).
  - Systems' feel goes with them: Foam = stable, Shortboard = fast and twitchy, Longboard = slow with an easy pocket.
  - The Bodyboard is ridden lying down.
- **Hooks with one stop:** the gdd's "retries" were written for 3 stops; rule 29 makes it one stop.
  - Barbed: a missed stop is redone once.
  - Treble: twice.
  - A heavy fish already has one retry, and the hook's retries add to it.
- **Lines:** the line weight used to come with the rod (`FishDb.Rod.LineKg`). Now each rod comes strung with that same line, so nothing gets worse for an existing save, and a better line can be bought and fitted.
- **The rod holder** is Doyle's existing "rod holders" upgrade (+20% worker catch). Once fitted, the holders also take a player's rod. To fish two lines you need a second rod: the hand rod and the holder rod are two rods you own.
- **Surf spots:** Breaker Point (south horn, beginner, every day there's swell) and the Widowmaker at Gullrock (big, the day after a storm).
  - The Voss Wall (+1 Heat, his patrol) waits for M9, which builds Heat.
  - The north-horn point break is left out.
- **Surfing earns no money** (world §10.3, economy §4.2): Kai's challenges pay in reputation and cosmetics.

## Steps (one at a time, each tested and committed)
1. **Tackle (F10).** *(built 8 Nov)*
   - June's Tackle tab sells reels, lines, hooks and the Sight float (fishing §6.3–6.5 prices).
   - **Fitting:** at June's counter or the office workbench, choose the rod in your hand and its reel, line and hook. There is no swapping mid-cast.
   - **What each piece does:**
     - reels and the Circle hook widen the strike window;
     - the line sets the weight above which a fish is "HEAVY";
     - the Barbed and Treble hooks add retries;
     - the Sight float is 1.5× bigger with a halo.
   - The hotbar tooltip reads like `Carbon Rod · Smooth reel · Braid 40 · Barbed`. Save and co-op are covered.
   - Tests: companycheck (buy, fit) and `-pb-tackletest` (window, retry, heavy by line, tooltip).
   - **Built as:**
     - `Tackle.cs` (plain C#, in the company): reels and lines only get better, so the best one owned is always on every rod. Fitting a reel or line means buying it.
     - The hook is the one choice: a new hook goes on when bought, and **Use** in June's Tackle tab swaps back to another.
     - Barbed: 2 stops; with a heavy fish, 3.
     - The hotbar card reads `Worms × 99 · Smooth reel · Mono 15 kg · Barbed`.
     - The minigame still shows only the bar (rule 29).
     - The office workbench is left out: fitting happens at June's.
     - Tests: `-pb-tacklecoop` (the rider buys through the host; the host checks a 2-stop fight) and Company.TackleTest.
2. **Rod holder and trolling (F11).** *(built 8 Nov)*
   - With the holders fitted: cast, then E at a holder puts the rod in it, and your other rod comes to hand.
   - The holder rod waits on the same 4–10 s bite. A bell rings and the rod bends, and you have 2.0 s to E it; then the normal fight.
   - **Trolling:** with sardine or squid on the hook and the boat at 2–5 m/s, a holder rod keeps fishing instead of dragging.
   - Co-op: the holder rods show on every screen. Test `-pb-holdertest`.
   - **Built as:**
     - `RodHolders.cs`. Biscuit gets one steel tube with a red lip on her port quarter (it used to be three grey boxes). The Kestrel's three holders are built into her and work once she's yours.
     - A rod in a holder keeps its cast (the bite comes as planned). The next rod you own comes to hand; with only the bamboo the prompt says you need a second rod.
     - The bell rings, the rod bends, and there's a caption. You have 2.0 s; a ringing rod reaches 2.3 m, so **one E at the tiller leaves it and takes the rod**. A missed bell re-baits.
     - **Trolling:** sardine or squid at 2–5 m/s, the line 14 m astern. Any other bait, or a faster boat, drags the line in and the rod rests in the holder.
     - Leaving the tiller eases the throttle to stop (as before), so taking a trolled bite slows the boat while you fight.
     - Co-op: `pb.hold`. The host keeps each holder's roll aside (`Coop.HoldBook`), so a second line in the hands doesn't free it, and a bite taken becomes the hands' roll for the landed-fish check.
     - Test `-pb-holdercoop` covers this.
3. **The landing net (F11).** *(built 8 Nov)*
   - $150 at June's, a hotbar tool.
   - A crewmate with the net within 2 m of a fighting angler presses E ("Ready the net"). The angler gets +1 retry and +10% green, and on a Rare the friend scoops it.
   - Test `-pb-netcoop` host and rider.
   - **Built as:**
     - `LandingNet.cs`. Once bought, the net is carried rather than taking a hotbar slot (a hotbar tool would touch the synced hotbar and the save). It's on June's Tackle tab.
     - E within 2 m of a crewmate's fight: the net shows in both hands on every screen (the character's two-hand grip), angled at the fish.
     - The angler gets +1 stop and a ×1.1 green zone for that fight (`Fishing.NetReady`). If the net arrives during the strike, the fight starts with both.
     - On a Rare the helper scoops it: the net dips and lifts, a splash, a cheer, and "Scooped!" for both.
     - Co-op: `pb.net` and `pb.scoop`.
4. **Swell Surf Co. and Kai.** *(built 8 Nov)*
   - The shop is the building already at the Cut.
   - Kai Rennick keeps the counter. He is a new keeper look: sun-bleached hair, open shirt, shell necklace, barefoot.
   - Open 09:00–18:00, shut in storms ("Gnarly. Shop's shut. Go home.").
   - Shelf: Foam board, Bodyboard, Longboard, wetsuit (+50% swim stamina), wax; the Shortboard after challenge 1.
   - Kai texts a swell report at dawn on surf days. His "Dawn Patrol" invite comes in Act 2 and lends a free foam board.
   - The Forecast app gains a surf line. The boards are Blender models.
   - Test `-pb-surfshoptest`.
   - **Built as:**
     - `Surf.cs` (company): the boards, the wetsuit and Kai's free foamie. `SurfShop.cs`: his counter at the landmark's door with Firm's market counter and a sun-yellow awning, and the hours.
     - A storm shuts the shop (`Stall.Shut`).
     - The surf report is face ≈ 1.6 × the sea's Hs in feet, with the wind for how clean it is. Kai texts it at 06:00 from the morning after his hello, only when there's surf.
     - The Forecast app's surf card.
     - The wetsuit makes swimming stamina last 1.5× as long.
     - **Kai's jetty** (`art-src/m8/build_surf.py` → `PB_Bld_surfjetty.fbx`, 8.8k tris): 38 m over the Cut's silted shallows to a T-head. It has teal-and-yellow trim, an arch with "Swell Surf Co." facing the sea, two boards planted in the sand, a lamp and a ladder.
     - The jetty is placed by a look round from the door: the shore, then the deepest berth, which came out at 1.17 m. F moors there.
     - The four boards are Blender models too (`PB_Prop_board_*.fbx`, about 1.1–1.3k tris each), for step 6.
     - Kai is keeper look 8.
     - Known: the shop building, its leaning boards and the blue roof board are map B's blockout art from before M8.
5. **The surf waves.** *(built 8 Nov)*
   - A breaking wave at Breaker Point, separate from the open sea: a mesh that peels along a reef line, in sets of 3–5 waves every 20–40 s.
   - Height comes from the day's swell: breezy is good, a storm closes it out. The Widowmaker at Gullrock runs big the day after a storm.
   - Judged on looks: frame sheets of a full set, at day and at dusk.
   - Test `-pb-wavetest`.
   - **Built as:**
     - `SurfWaves.cs` and `PB_Water.shader`. The waves are part of the sea itself: the water shader lifts its own surface by up to 4 breaking waves, so they have the sea's colour, depth tint, reflections and crest foam with no seams. `Sea.Height` adds the same lift on the CPU, so boats, swimmers and boards ride what you see.
     - **Breaker Point** is a straight beach break (map B: the shore at z −1160, breaking on the 2.5 m line).
     - **The Widowmaker** is Gullrock's reef ledge (11 m deep, then 5 m), only the day after a storm, at 1.8× and at least 3.5 m.
     - **Each wave:** it rolls in at 5 m/s and grows as it shoals. Breaking at its peak, it peels 5 m/s one way and 2 m/s the other, alternating lefts and rights. The broken stretch is white water, lower than the green face, and runs to the sand.
     - **Look:** a steep glassy face (lighter aqua, light through it), a wider white lip near the curl, and spray.
     - Sets come from the shared sea clock, so co-op sees the same waves.
     - The height comes from Kai's report. Tester key **Ctrl+F11** puts the surf up for the day.
6. **Surfing.** *(built 8 Nov)*
   - The board is a hotbar item (a carry pose).
   - In the water: W paddles and Ctrl duck-dives under white water.
   - **Pop-up:** a timing ring (the fishing bar's look, one stop) where Space stands you up as the wave lifts you.
   - **Riding:** A/D carves, and a glowing pocket band near the curl gives speed. Lean too far and you wipe out (tumble, bubbles, the leash pulls the board back).
   - The bodyboard is ridden lying down. Camera, sounds and spray.
   - Co-op: riders show on every screen.
   - Test `-pb-surftest`, with a film watched before the step counts as done.
   - **Built as:**
     - `Surfer.cs`. The board isn't a hotbar slot: in the water, E gets you on it. Paddling uses the swim stroke lying on the board.
     - **Duck-dive:** Ctrl, or B on a pad.
     - **Catching:** only within 22 m of the break line.
     - **The pop-up bar:** a 1.1 s sweep; green is 34% wide on the foamie, 30% on the longboard, 16% on the shortboard; the bodyboard has none. Easy fishing widens it too.
     - **Riding** is in the wave's frame. Gravity on the face gives speed. The wave's carry fades down the face, so a ~60° line trims mid-face and straight down outruns it. The pocket gives +2.6 m/s².
     - **The end of a ride:**
       - a wipeout: more than 125° round, or the white water;
       - a ride over: off the back, the end of the wave, or the sand.
     - **Poses:** the jump clip's crouch side-on for standing, the swim stroke lying down, the flail for a wipeout.
     - The camera eases along the line, a glowing pocket band shows near the curl, and the prompt line shows the surf keys.
     - Co-op: the pose carries it (State 7: state, board, lean, wave).
7. **Score, Kai's challenges and party waves.** *(built 8 Nov)*
   - Score = ride time × pocket time × style.
   - Kai's three challenges (the first unlocks the Shortboard) pay in reputation and a cosmetic.
   - The best rides go in the Journal.
   - In co-op, 2+ riders on one wave multiply the score; dropping in on a friend knocks you both off.
   - Tests `-pb-surfchallenge` and `-pb-surfcoop`.
   - **Built as:**
     - **Points:** (10 × (time + 2 × pocket time) + 2 per carve) × the board (shortboard 1.3, longboard 1.1, bodyboard 0.8) × 1.2 all the way in × (1 + 0.5 per crewmate on the wave).
     - Your best and your rides are in the kit and the Journal's Surf card.
     - **Kai's challenges** (crew-wide events): 3 s up unlocks the shortboard; 8 s gets his sunburst tint on your boards; 3 s in the pocket makes you "a local". He lists them with his hello and texts as each is done.
     - **Party wave:** anyone on your wave during the ride.
     - **Drop-in:** two riders within 2.5 m on one wave both go down.
     - The challenges are tested inside `-pb-surftest`; co-op in `-pb-surfcoop`.
8. **The M8 build.** *(built 8 Nov)*
   - `docs/M8-HOW-TO-TEST.md`, also copied to `Builds/m1/HOW-TO-TEST.md`.
   - The full regression on the build, then **stop for Tushar's or Manan's hand test (★)**.
   - **Built as:**
     - `Builds\m1\PortBoss.exe` with `HOW-TO-TEST.md` beside it.
     - **Tester key Ctrl+F11** puts the surf up for the day: Breaker Point 1.5 m, the Widowmaker 3.2 m. It is a company event, so co-op shares it, and F11 alone still holds the weather.
     - The crew list says "surfing".
     - **Tests on the final build:**
       - `tools/m8test.sh`, all 13 runs: tackle and its co-op, holders and their co-op, the net in co-op, the surf shop, the waves, surfing, and surfing in co-op.
       - `testerkeys` (with Ctrl+F11), `m3storm` and `swim`.
       - The company checks, run in the editor.
     - The steps 5–7 regression: everything else passed, 0 exceptions.

## Not in M8
- The gear guns (Manan's Q1).
- The Voss Wall wave (M9, with Heat).
- The Saltmouth Swell Classic (an Act 3 event).
- The weekly surf contest, the SUP, the jet ski, fins, mask and snorkel (all SUGGESTED, for later).
