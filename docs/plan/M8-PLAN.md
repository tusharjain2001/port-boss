# M8: surfing and fishing gear (plan, 8 Nov 2026)

**Branch:** `m8` (from `main` @ `4e4a209`, after M7 was played and merged). **Status:** being built. Each step is tested headless and committed on `m8`. The branch is pushed to GitHub, and it goes to `main` only after Tushar or Manan plays it (HANDOVER rule 10).

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
3. **The landing net (F11).**
   - $150 at June's, a hotbar tool.
   - A crewmate with the net within 2 m of a fighting angler presses E ("Ready the net"). The angler gets +1 retry and +10% green, and on a Rare the friend scoops it.
   - Test `-pb-netcoop` host and rider.
4. **Swell Surf Co. and Kai.**
   - The shop is the building already at the Cut.
   - Kai Rennick keeps the counter. He is a new keeper look: sun-bleached hair, open shirt, shell necklace, barefoot.
   - Open 09:00–18:00, shut in storms ("Gnarly. Shop's shut. Go home.").
   - Shelf: Foam board, Bodyboard, Longboard, wetsuit (+50% swim stamina), wax; the Shortboard after challenge 1.
   - Kai texts a swell report at dawn on surf days. His "Dawn Patrol" invite comes in Act 2 and lends a free foam board.
   - The Forecast app gains a surf line. The boards are Blender models.
   - Test `-pb-surfshoptest`.
5. **The surf waves.**
   - A breaking wave at Breaker Point, separate from the open sea: a mesh that peels along a reef line, in sets of 3–5 waves every 20–40 s.
   - Height comes from the day's swell: breezy is good, a storm closes it out. The Widowmaker at Gullrock runs big the day after a storm.
   - Judged on looks: frame sheets of a full set, at day and at dusk.
   - Test `-pb-wavetest`.
6. **Surfing.**
   - The board is a hotbar item (a carry pose).
   - In the water: W paddles and Ctrl duck-dives under white water.
   - **Pop-up:** a timing ring (the fishing bar's look, one stop) where Space stands you up as the wave lifts you.
   - **Riding:** A/D carves, and a glowing pocket band near the curl gives speed. Lean too far and you wipe out (tumble, bubbles, the leash pulls the board back).
   - The bodyboard is ridden lying down. Camera, sounds and spray.
   - Co-op: riders show on every screen.
   - Test `-pb-surftest`, with a film watched before the step counts as done.
7. **Score, Kai's challenges and party waves.**
   - Score = ride time × pocket time × style.
   - Kai's three challenges (the first unlocks the Shortboard) pay in reputation and a cosmetic.
   - The best rides go in the Journal.
   - In co-op, 2+ riders on one wave multiply the score; dropping in on a friend knocks you both off.
   - Tests `-pb-surfchallenge` and `-pb-surfcoop`.
8. **The M8 build.**
   - `docs/M8-HOW-TO-TEST.md`, also copied to `Builds/m1/HOW-TO-TEST.md`.
   - The full regression on the build, then **stop for Tushar's or Manan's hand test (★)**.

## Not in M8
- The gear guns (Manan's Q1).
- The Voss Wall wave (M9, with Heat).
- The Saltmouth Swell Classic (an Act 3 event).
- The weekly surf contest, the SUP, the jet ski, fins, mask and snorkel (all SUGGESTED, for later).
