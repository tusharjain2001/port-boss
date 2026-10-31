# Port Boss GDD: FISHING (the core mechanic)

Worker: gdd-fishing (w-gdd-fishing) · task t-cabd819a · 2026-10-31 · planning only.

**Files in this folder**

| File | What |
|---|---|
| `fishing.md` | This section |
| `fish-roster.csv` | The 15 fish *(Manan 31 Oct)*: class, rod tier, favourite time/weather (a nudge), baits, price, size, weight |
| `fishing_model.py` | Numbers check: bite timer, timing windows per rod, grade odds, who bites per rod × bait, $/fish and $/hour. It asserts the MUST rules (bite 4–10 s, rot ≥ 48 h, 15 fish / 3 per rod, legend = best rod AND best bait, no locks) *(Manan 31 Oct)*. Run with `python -I fishing_model.py` |
| `model-output.txt` | Its last output, quoted in §7.6 |

**Tags**
- **MUST**: Manan's own words, from the source notes and the brief.
- **SUGGESTED**: our design.
- **OPEN QUESTION**: Manan decides. All of them are collected in §9.

**Shared canon (agreed by message with the other sections on 2026-10-31)**
- World (gdd\world): Saltmouth Island. Our dock is Hale's Landing on the west coast. Voss Harbour is on the east coast (Adrian Voss). The bait shop is Hook, Line & Sinker (June Pike) in Old Saltmouth. Grandpa is **Walt** (his sunk boat is the Margaret).
- Systems (gdd\systems):
  - Clock: 1 real second = 1 in-game minute, so a day is 24 real minutes.
  - Sleep jumps the clock to 07:00, and rot keeps advancing through the skip.
  - 8 hotbar slots, none locked.
  - A stack holds 5 fish of one species and one grade.
  - Grade names: **Common / Fine / Rare / Prized**.
- Economy (gdd\economy): my price bands, grade multipliers, catch rate, freshness bands and bait prices are adopted.
  - Old Saltmouth Fish Market (Voss-owned, buyer Gideon Pratt) pays **60%** of base, open 05:00–16:00 (economy's number wins; this file said 80%). *(Kufu review 31 Oct)*
  - The Pelican restaurant pays 150–200% on its orders.
  - Saturation: −2% per same-species fish sold at one market in one day, floor 60%, reset at 07:00.
  - A worker's grade multiplier is 1.0 + 0.15 × skill.

> **Relaxing first** *(Kufu review 31 Oct)*. Manan: "the fishing should be relaxing". Every rule below follows three tests: (1) a miss never punishes the body or the camera, it only lowers the grade; (2) the only way to lose a hooked fish is a **heavy fish you chose to fight** (§3.7), and it is always warned; (3) better rods make fishing **easier and faster**, so money buys calm, not just numbers. Research behind this: Dredge's team made "not frustrating" the minigame's first goal; Stardew's bar is the most-modded complaint in the genre; Cast n Chill's reviews praise low friction (sources at the end).

---

## 0. The fishing loop in one screen

```
equip rod (slot) ─► hold to charge, aim ─► release: line arcs, bobber lands
   ─► WAIT: bite after random 4–10 s (MUST; better rods: down to 4–6 s), 0–4 fake nibbles meanwhile
   ─► BITE: bobber yanked under ─► press within 1.20 s (the strike)
         early = "Too soon!"   late = "It swam off."   (bait kept either way)
   ─► MINIGAME: a pointer sweeps a bar, 3 stops; each stop in the green = +1 grade (MUST)
         0 hits Common ×1.0 · 1 Fine ×1.3 · 2 Rare ×1.7 · 3 Prized ×2.5
   ─► REEL-IN (1–3.5 s, cinematic) ─► LAND ─► REVEAL card (species, size, weight, grade, price)
   ─► fish flies into the hotbar (stack 5, MUST) ─► recast
```

A full cycle takes **~20 s**: cast 2 s, wait 7 s on average (5 s with Old Faithful), fight ~5 s, reel 2 s, reveal 2 s, stow 1 s. That is ~2.5 landed fish per real minute while fishing. Counting travel and selling, it comes to **60–80 fish per real hour**, and economy agreed this rate.

**Two words that must never be mixed up**
- **Species class** is how rare the *species* is (fixed per species; it follows the rod tier that unlocks it, *(Manan 31 Oct)*): Everyday, Uncommon, Scarce, Trophy, Legend. It drives the base price and how hard the minigame is.
- **Grade** is how well *you* caught it, set by the minigame (Manan's "3 hits raise rarity and price"): Common, Fine, Rare, Prized. It drives the price multiplier, and the UI colours are paper, teal, blue and amber (systems).

The UI calls the grade "rarity", as Manan does. The species class shows on the reveal card only as a small label ("Scarce species").

---

## 1. Research: the best fishing in games (checked live, 2026-10-31)

| Game | What makes it satisfying | What players hate or critics flag | What we take (SUGGESTED) |
|---|---|---|---|
| **WEBFISHING** (2024, 1–12 players, 97% positive per our market table) | Bites are rolled on short timers. Each bait has its own quality table: Normal, then Shining, up to Alpha, 6 star tiers. Rod Power sets how much progress a yank makes, and other stats set reel speed and bite chance. The real draw is social: trickshots and hanging out. [wiki: Fishing](https://webfishing.wiki.gg/wiki/Fishing), [Dot Esports: rod upgrades](https://dotesports.com/indies/news/what-do-all-the-rod-upgrades-do-in-webfishing), [Dot Esports: baits](https://dotesports.com/indies/news/all-bait-and-what-it-does-in-webfishing) | Some lures eat 2–3 baits per cast, an invisible cost (same wiki) | Bait sets the class odds. Skill sets the stars (our grades). Rod stats stay few and readable. Bait use is always shown: 1 bait per landed fish, never hidden |
| **DREDGE** (2023) | Short timing minigames, one shape per fish type: Radial, Pendulum, Diamond, Ball Catcher. You pull as an indicator passes green or yellow bars; misses cost progress. Reviewers call it a peaceful test of timing, not a tug-of-war. [wiki: Minigames](https://dredge.wiki.gg/wiki/Minigames), [StrategyWiki](https://strategywiki.org/wiki/DREDGE/Fishing), [Press Start review](https://press-start.com.au/reviews/ps5-reviews/2023/03/24/dredge-review-a-trophy-catch/), [GameSkinny](https://www.gameskinny.com/i0ll7/dredge-fishing-guide-how-to-catch-fish) | Little on the minigame itself. The horror and the cargo grid carry the tension | **Our minigame is the same idea**: timing, readable, short. From Dredge: harder classes change the *behaviour* of the bar (Trophy: the green zone drifts; Legend: the pointer surges), not just its speed |
| **Stardew Valley** | The catch bar must be held over a darting fish. The bar grows with fishing level and tackle (Cork Bobber, Training Rod). [wiki: Fishing](https://stardewvalleywiki.com/Fishing) (403 to our fetcher; numbers via search snippets) | **The big complaint:** "Fishing is impossible" early on. Fish dart "faster than you can possibly move", and control feels floaty. Mods exist just to make the bar full size or skip the game. [forum thread](https://forums.stardewvalley.net/goto/post?id=19086), [Steam thread](https://steamcommunity.com/app/413150/discussions/0/4035852798876291083), [Full Fishing Bar mod](https://www.curseforge.com/stardewvalley/mods/full-fishing-bar) | **No erratic movement, no floaty control.** Our pointer moves on a fixed, readable path. Gear widens the green zone. An assist slider exists (§3.7). A failed minigame never loses the fish (only the strike and the line can) |
| **How to Fish** (Dazed Games, 20 Aug 2026, 1–4+ co-op, ~95%) | Physics comedy co-op: fling the catch and shoot it. 1M players in two days, ~268–374K peak. [GamingOnLinux](https://gamingonlinux.com/2026/08/how-to-fish-is-another-brilliantly-funny-online-co-op-game), [PC Gamer](https://www.pcgamer.com/games/sim/the-latest-cooperative-craze-is-a-physics-based-fishing-simulator-where-you-can-launch-your-catch-into-the-air-and-no-scope-it-with-a-sniper-rifle/), [Niche Gamer](https://nichegamer.com/how-to-fish-attracts-one-million-players-in-two-days/), [Notebookcheck](https://www.notebookcheck.net/Doom-but-with-gills-Why-this-new-indie-game-is-blowing-up-on-Steam.1376899.0.html) | "Core loop wears thin within a few hours" and "much weaker solo". [GameBrief](https://www.gamebrief.net/blog/how-to-fish-review-2026) | Co-op chaos moments (tangles, a shark bites a friend's catch, the landing net). **Solo needs its own depth**: orders, legends, the rod holder, the business. That is exactly Manan's "fully fun solo" MUST |
| **Cast n Chill** (2025, Overwhelmingly Positive) | No friction: line, lure and rod never break, and missed catches are not punished. An idle mode lets the game fish for you, which Shacknews calls brilliant. [Shacknews 9/10](https://www.shacknews.com/article/144913/cast-n-chill-review-score), [3DNews](https://3dnews.ru/1126062/cast-n-chill-review), [OpenCritic](https://opencritic.com/game/18729/view/reviews) | "Not very deep"; little to do beyond casting and fights | Commons are never punished hard. Our "idle" is the business layer (workers fish for you; MUST) and the rod holder. Depth comes from orders, freshness and legends |
| **Fisch** (Roblox) | Small rod stat set: Lure Speed (bite speed), Luck (rarity), Control (bar size), Resilience. Mutations add a collect-them chase. [Fisch wiki: rods](https://fisch.fandom.com/wiki/Fishing_Rods), [Deltia's: rod progression](https://deltiasgaming.com/roblox-fisch-rod-progression-guide) | Rod numbers swing between updates; guides disagree on stats (same sources) | Our 4 rod stats map almost 1:1: Range, Control, Luck, Line. Values are frozen per EA build, so guides stay true |
| **Animal Crossing: New Horizons** | A fish can nibble up to 4 times, and the 5th is always a bite. The fake-out is the tension, and the hold-up-to-camera reveal is the joy. [Nookipedia: fishing rod](https://nookipedia.com/wiki/Fishing_rod), [Pixel Poppers](https://pixelpoppers.com/2020/04/why-is-fishing-so-hard/) | "Why is fishing so hard?": early pulls on nibbles lose the fish (same article) | **Nibbles as fake bites, 0–4** (§3.5), each clearly smaller than a real bite. **The hold-up reveal pose** with a one-line quip |
| **Sea of Thieves** | Camera locks on the bite. You pull opposite to the fish or the line snaps with a wood-splinter sound. 10 species × 5 colour variants, ~1/3 are trophy size. Baits are dug up in the world. [SoT wiki: Fishing](https://seaofthieves.wiki.gg/wiki/Fishing), [PC Gamer guide](https://pcgamer.com/sea-of-thieves-fishing-guide) | Long, samey fights | Camera framing on the bite. **Fishing from a moving or anchored ship** works for us too. Trophy-size rolls. The line snap as a readable, *heard* failure |
| **Fishing Planet** (sim) | Real drag and tension | Fish lost to "high tension" while the meter barely moved; "very little logic" in fights. [Steam thread](https://steamcommunity.com/app/380600/discussions/0/364039531224785849), [Steam thread 2](https://steamcommunity.com/app/380600/discussions/0/4406291673456245096) | **Never lose a fish to a rule the player can't see.** Every failure has a visible and audible cause |
| **Ultimate Fishing Simulator** (sim) | Bait view, the strike moment | The bite is subtle and hard to read; mechanics are "obtuse" with bare help. [Gaming Nexus](https://www.gamingnexus.com/Article/5772/Ultimate-Fishing-Simulator/), [PlayStation Country](https://www.playstationcountry.com/?p=16529), [Steam thread](https://steamcommunity.com/app/468920/discussions/0/2789318172117446508) | **The bite must be unmistakable** in sight, sound and rumble, and it must differ from a nibble on all three |

### 1.1 What this means for Port Boss (SUGGESTED)

1. **Timing, not tug-of-war.** Manan's bar-and-pointer fits the best-loved pattern (Dredge, and WEBFISHING's stars). It must be crisp. Stardew's "floaty, erratic" is the thing to avoid.
2. **The bite is the drum hit.** A real bite gets three channels: the bobber dives, a deep *bloop*, and a strong rumble. A nibble gets a quiet version of all three. Nobody should ask "was that a bite?" (UFS).
3. **The reveal is the reward.** Hold-up pose, rarity stinger, size and weight, price, and "NEW" or "Record!". It plays every catch and can be skipped after 0.5 s (Animal Crossing, WEBFISHING).
4. **Low punishment, high reward.** No fish can be lost in the minigame except a HEAVY one you chose to fight (Cast n Chill). A miss lowers the grade, nothing else. Stakes live in the grade, in freshness, and in the opt-in heavy fight *(Kufu review 31 Oct)*.
5. **Few stats, frozen numbers** (Fisch): the rod carries Green zone, Pointer speed, Bite wait, Range and Luck; the line carries the weight rating *(Kufu review 31 Oct)*.
6. **Solo depth + co-op comedy** (the How to Fish lesson): orders, legends and the rod holder for solo; tangles, the landing net and "Double!" for co-op.

---

## 2. Core rules (the contract other sections rely on)

| # | Rule | Tag |
|---|---|---|
| R1 | The bite comes after a random wait, **uniform 4.0–10.0 real seconds**, from the moment the bobber settles. No bait, rod, hotspot or upgrade may push it outside 4–10. Upgrades may only narrow it inside that range. **The rod sets the range: starter 4–10 s, best rod 4–6 s** (§6.2, Manan: better rods make catching faster) *(Kufu review 31 Oct)* | **MUST** |
| R2 | A bobber shows where the bait is: a red-and-white float that bobs | **MUST** |
| R3 | The minigame is a bar with a green zone. Stop the pointer **3 times**; every stop in the green raises the grade one step, and the price with it | **MUST** |
| R4 | Fish go in the inventory (hotbar), **never in crates**. Max stack 5 | **MUST** |
| R5 | A fish takes **≥ 2 in-game days (48 in-game h)** to rot fully. Ours: exactly 48 h | **MUST** |
| R6 | Bait is bought, and each bait gives different fish and prices | **MUST** |
| R7 | Cast, bobber and reel animations are polished (§3, every step) | **MUST** |
| R8 | The minigame grades; it never loses the fish. A fish is lost only (a) by a missed strike (it swims off, the bait stays on), or (b) by a line snap on a **HEAVY** fish the player chose to fight after a clear warning (§3.7). A normal fish can never snap *(Kufu review 31 Oct)* | SUGGESTED |
| R9 | The game rolls the species at the bite; the minigame decides the grade afterwards | SUGGESTED |
| R10 | 1 bait is used per landed fish, and **only** per landed fish (or a snapped heavy fish). Early strike, late strike and reeling in all keep the bait *(Kufu review 31 Oct)* | SUGGESTED |
| R11 | **15 fish, 3 per rod (5 rods).** The rod is a hard gate: a fish above your rod never bites; a better rod still catches the earlier fish *(Manan 31 Oct)* | **MUST** |
| R12 | **Any fish can be caught anywhere.** No fishing zones; time and weather only nudge the odds (×1.5), never lock a fish out *(Manan 31 Oct)* | **MUST** |
| R13 | **The legend needs the best rod AND the best bait** (Old Faithful + Walt's Mix), both at once *(Manan 31 Oct)* | **MUST** |

---

## 3. The full flow, step by step

Camera baseline, from the feel spec (`replan\feel\README.md`): third person, 7.5 m behind, 30° pitch, FOV 55, 0.15 s damping. Every fishing camera below is an offset from that baseline and eases back to it within 0.4 s.

### 3.1 Equip the rod

| Item | Spec |
|---|---|
| Input | Hotbar key (1–8) or scroll; on the pad, LB/RB cycles slots. The rod is a normal hotbar item, not a locked slot (systems) |
| Timing | Draw 0.35 s (pull the rod from the back strap, flick it forward), stow 0.30 s. You can walk while drawing; sprinting stows the rod |
| Animation | Upper-body layer: rod held at 35° up, reel at the hip, the off hand loosely on the cork. Idle: the rod tip sways with the boat (procedural, §3.11). Walking: the tip lags 0.1 s behind the hands (spring) |
| Sound | Cloth whip, light graphite *tick* of the reel bail |
| UI | Hotbar slot pulses once. A small bait tag sits by the hotbar (bottom right): bait icon + "Worms × 7". When bait is 0, the tag reads "No bait (junk & sprats only)" in amber |
| Camera | No change |
| Where you can fish | On a boat deck within 1.2 m of a rail and facing outward (±70° from the rail normal); on a pier edge; on a beach to knee depth. Never through cabins or from cabin roofs. **SUGGESTED** |

### 3.2 Aim and charge the cast (distance)

| Item | Spec |
|---|---|
| Input | Hold LMB / RT to charge; release to cast. RMB / B cancels with no cost |
| Charge | The power meter fills 0→100% in **1.10 s** and then ping-pongs back down (holding is not free; skill matters). The sweet spot is **92–100%** |
| Distance | `d = rangeMin + (rangeMax − rangeMin) × power^0.9`. Walt's starter rod: **6–16 m**; best rod: 8–30 m (§6.2). Scatter ±0.4 m at the sweet spot, ±1.2 m elsewhere |
| Clean cast (sweet spot) | the rarer fish of your rod bite ×1.1 on this cast, a quieter splash and a small sparkle on release. **SUGGESTED** |
| Landing marker | A dashed ring on the water (Ø 0.8 m) slides out with the charge. It turns gold in the sweet spot and red if the point is invalid (boat, land, under another player's line within 1.5 m). Releasing on red puts the bobber at the nearest valid point, never a dead cast |
| Animation | Anticipation: the rod swings back over the right shoulder, 0°→ −110° as power grows. The body twists 25° and the weight goes onto the back foot. At 100% the arm trembles slightly (the cue to release) |
| Sound | Rising rod-creak tone tied to power (pitch +6 semitones from 0→100%). A soft gold *ting* when the meter enters the sweet spot |
| Camera | Shoulder cam: eases in 0.25 s to 4.5 m behind, 0.6 m right, 20° pitch, FOV 50. It looks at the landing marker so the target is always on screen |
| UI | A thin curved arc gauge (60° arc) beside the crosshair, filling white→gold. No big bar: the ring on the water is the main read |
| Pad | The right stick aims. Light aim magnetism: the marker drifts ≤ 1 m toward a hotspot centre within 3 m. **SUGGESTED** |

### 3.3 Release: line arc and bobber flight

| Item | Spec |
|---|---|
| Animation | Whip: the forward swing takes 0.18 s, with release at 60% of the swing and a 0.25 s follow-through. The rod tip overshoots, then springs back (2 oscillations, damped) |
| Flight | A parabola to the marker. Time = 0.55 s + 0.03 s/m (0.73 s at 6 m, 1.03 s at 16 m). Apex height = 0.25 × distance, max 6 m |
| Line | Verlet rope, 32 points, screen-constant 1.5 px width, a light rim at dusk. In flight the line pays out from the reel: a visible spool spin and a *zzzzz* zing whose pitch falls with distance. **Line never clips through the boat**: the rope collides with the hull rail and slides over it |
| Bait | A tiny bait mesh on the hook (worm, shrimp, squid strip) flies with the bobber. **SUGGESTED** (cheap and lovely) |
| Sound | Whoosh scaled by power. Line zing |
| Camera | Holds shoulder framing, then eases back 0.4 s to a "wait" framing (§3.5) as the bobber lands |

### 3.4 Bobber lands and bobs (MUST: the bobber)

| Item | Spec |
|---|---|
| Landing | Splash crown: 12 droplets, a foam ring Ø 0.6 m, and an expanding ring decal to Ø 1.4 m over 0.8 s. A *plip* for short casts, a *plunk* for long ones (two layered samples). The bobber dunks 8 cm, pops up, and settles in 0.6 s (2 bounces) |
| Float | Red top, white body, Ø 6 cm, scaled ×2.5 for readability (Manan's "red blob"). It sits upright, riding the wave height and normal (sampled from the sea shader every frame). Idle bob: 2–4 cm at 0.8 Hz, plus wave motion |
| Ripples | A soft ring every 1.6 s (Ø 1.2 m, 1.0 s life) |
| Long-range read | Beyond 15 m from the camera the bobber gets a soft red halo (screen-space size floor 10 px), so it never gets lost in glare. At night the float tip glows (a "night float" property on every bobber) |
| Drift | The bobber drifts with current at 0.05–0.2 m/s (current varies by place) and with the boat if the boat moves. If the boat moves faster than 2 m/s the line drags: the bobber skips, and after 1.5 s "Line dragging" auto-reels (keeps bait). Trolling is a later upgrade (§6.6) |
| Cosmetics | Bobber colours and shapes are bought at Hook, Line & Sinker or found as collectibles. **SUGGESTED** |

### 3.5 The wait, nibbles and fake bites

| Item | Spec |
|---|---|
| Wait | `wait = lo + (hi − lo) × u` (u uniform 0–1), where lo–hi is the **rod's bite range** (§6.2): starter 4–10 s, best 4–6 s. **MUST inside 4–10 s** (§2 R1). A hotspot takes 2 s off the top and chum 1 s, but the top never drops below 5 s *(Kufu review 31 Oct)* |
| Nibbles | Count by class: Everyday 0–1, Uncommon 1–2, Scarce 1–3, Trophy 2–3, Legend 3–4 (Animal Crossing's "up to 4"). Timing: ≥ 1.5 s after the landing, ≥ 0.6 s before the bite, ≥ 0.5 s apart. *An attentive player reads the nibble count as a class hint.* |
| Nibble look | The bobber dips **3–5 cm** for 0.15 s with a small ring (Ø 0.5 m) |
| Nibble sound | A soft wooden *tik* (−12 dB vs the bite) |
| Nibble rumble | 0.15 amplitude, 80 ms, low motor only |
| Player during the wait | Idle loop: weight shifts, reel hand rests. After 6 s a fidget: checks the line, scratches the neck, glances at the sky. Nibbles trigger a head tilt toward the bobber (0.3 s IK look). In co-op, players can walk, emote or chat; walking more than 2 m from the cast point reels in automatically (keeps bait) |
| Camera | A soft "wait" framing: 6.5 m, 25° pitch, biased 30% toward the bobber so the player and float are both on screen. No other motion |
| Striking on a nibble | **"Too soon!"** (left alert, MUST alerts on the left). The rod lifts on an empty line and the bobber skips back. The bait stays on; recast *(Kufu review 31 Oct)* |
| Reel in early | RMB / B (hold 0.3 s): a quick reel-in, bait kept |

### 3.6 The bite and the strike window

| Item | Spec |
|---|---|
| Bite look | The bobber is **yanked fully under (25 cm)** and dragged 0.4 m sideways; a big splash plus a foam boil. The rod tip dips 15° |
| Bite sound | A deep *BLOOP* + splash + the reel ratchet clicking 3× (line pulled) |
| Bite rumble | 0.8 amplitude, 300 ms, both motors |
| Bite camera | A 0.1 s punch-in of −3° FOV, then hold |
| Accessibility toggle | A "bite icon" (white ! above the bobber) and a screen-edge pulse. Off by default |
| Strike window | **1.20 s** base (was 0.80 s) *(Kufu review 31 Oct)*. Reels and the Circle hook add up to +0.35 s (§6.3, §6.5). The rod-holder bell gives 2.0 s (§6.6) |
| Strike input | The same button as the cast (LMB / RT). Any face button also works (accessibility) |
| Strike animation | A hard upward yank, 0.2 s: the rod goes to 70° up, the body leans back, a back-foot step. The rod bends to 60% (§3.11) |
| Strike success sound | Line *zing-tight* + a rising whoosh. The minigame opens 0.25 s later |
| Too late | **"It swam off."** (left alert). The bobber pops up and spins; the bait is still on. Recast *(Kufu review 31 Oct)* |
| Hidden detail | The fish (species, size, weight) is rolled **at the bite**, on the host (§4.4) |

### 3.7 The minigame (MUST: bar, green zone, stop the pointer, 3 hits)

**Shape:** a horizontal bar with a pointer that ping-pongs edge to edge at constant speed (no easing, no darting: the Stardew lesson). There is a green zone on the bar. Press the strike button to stop the pointer. The game takes **3 stops**, and each stop in the green is a hit. Systems owns the visual design; this table is the functional spec.

| Element | Spec |
|---|---|
| Bar | 560 × 28 px at 1080p, centred horizontally, 62% down the screen (above the hotbar, below the bobber framing). Pointer 4 px white with a dark outline. Three result pips above the bar: hit = teal tick, miss = red cross |
| Stop 1 | The green zone is **centred** (Manan: "green column in the middle"). The pointer starts at the left edge moving right |
| Stops 2–3 | The green zone jumps to 50% ± 8–18% (random side), so rhythm alone doesn't work. **SUGGESTED, OPEN QUESTION Q4** (Manan may prefer always centred) |
| Speed-up | Pointer speed ×1.00 / ×1.08 / ×1.16 for stops 1/2/3 (the fish fights harder near the boat) |
| Between stops | 0.45 s freeze showing the result (pointer flashes teal or red), then the next sweep begins |
| Timeout | No press after 3 full passes = a miss for that stop |
| Judging | **At the input's timestamp**, not the frame it arrives on (a 60 fps frame is 16.7 ms, 10–19% of a hard window). Settings → Gameplay → "Timing offset" −100…+100 ms, like rhythm games. This is the calibration knob for real pads and screens |

**Speed and green zone by species class** (starter gear). Window = zone ÷ speed, i.e. how long the pointer sits in the green per pass:

| Class | Green zone (% of bar) | Pointer speed (bar widths/s) | Window, stop 1 | Window, best gear | Behaviour |
|---|---|---|---|---|---|
| Everyday | 19% | 0.90 | 211 ms | 396 ms | Plain |
| Uncommon | 18% | 1.00 | 180 ms | 338 ms | Plain |
| Scarce | 17% | 1.10 | 155 ms | 290 ms | Plain |
| Trophy | 15% | 1.25 | 120 ms | 225 ms | The green zone drifts slowly (0.08 bar/s) during the sweep (Dredge-style) |
| Legend | 13% | 1.40 | 93 ms | 174 ms | The pointer surges: speed ×(1 ± 0.25 sin 2πt/1.3 s). Unique music |

*(Kufu review 31 Oct)*: every window is ~30% wider than the first draft (was 160/136/112/90/73 ms). Best gear = the best rod, Old Faithful: green ×1.50 and pointer ×0.80 (§6.2). **The rod is the one upgrade that changes the bar**; reels only change the strike window now (§6.3). `fishing_model.py` now uses this table and prints every rod's windows (re-run *(Manan 31 Oct)*).

**What the rod ladder feels like** (practised player, Everyday fish → chance of Prized): Bamboo 59% · Fibreglass 75% · Carbon 87% · Deep-sea 93% · Old Faithful 97%. On a Scarce fish: 33% · 48% · 64% · 75% · 84%. On a Legend: 10% · 16% · 24% · 33% · 42%. So every rod is a felt step, and Legends stay a real fight.

**Grade result** (systems owns the names and colours; economy adopted the multipliers):

| Hits | Grade | Colour | Price × | Odds, novice (σ 110 ms) | Odds, practised (σ 70 ms) |
|---|---|---|---|---|---|
| 0 | Common | paper | 1.0 | 5% | 0% |
| 1 | Fine | teal | 1.3 | 26% | 7% |
| 2 | Rare | blue | 1.7 | 44% | 34% |
| 3 | **Prized** | amber | **2.5** | 25% | 59% |

These are Everyday fish on the starter rod with the *(Kufu review 31 Oct)* table (average price ×1.76 novice, ×2.14 practised; the first draft was ×1.56 / ×1.90). For a Scarce fish a practised player lands Prized 33% of the time on the starter rod, so better rods stay worth buying. **Economy impact:** the average fish is worth ~8% more in Stage 1; economy's model assumed ~×1.8 (`avg_fish_worms` = $23). That is inside its ±10% tolerance; re-run `economy_model.py` at M4 with the new mix.

| Event | What happens |
|---|---|
| **A hit** | Pointer flashes teal. A crisp *ding*, with pitch rising per hit (C → E → G, so 3 hits play a major triad). Rumble 0.5 / 60 ms. The rod bends deeper. A spray at the line entry |
| **3 hits** | A small fanfare (the triad resolves to a chord). An amber shimmer runs along the bar. The fish leaps clear of the water (Scarce+) |
| **A miss** | The pip turns soft grey (not a red cross: a miss is not a failure). A soft *plop* and the line goes briefly slack. Light rumble 0.2 / 80 ms. The fish splashes once. **No stumble, no camera shake, no lost fish**: a miss only means one grade lower *(Kufu review 31 Oct)* |
| **Max attempts** | Exactly 3 stops by default. A Barbed hook gives +1 retry (a missed stop is redone once); a Treble hook gives +2. **Hard cap 5 stops.** A retry pip shows as a small hook icon |
| **Heavy fish (opt-in)** *(Kufu review 31 Oct)* | Only when the fish's weight > the line rating (§6.4). **Before the bar opens**, the bite pauses on a choice card: "**Something HEAVY!** Your line is rated 8 kg. [E] Fight it · [Q] Let it go". *Let it go* (also the default after 4 s): the fish swims off, bait kept, a friendly "Get a stronger line at June's" tip. *Fight it*: the bar shows a red "HEAVY" strain tag and the rule in words, "2 misses snap the line". Then the 2nd miss snaps: a *crack-twang*, the line falls slack, fish and bait lost. Retries don't erase misses for snap counting. **Normal fish never snap.** Settings → Gameplay → "Heavy fish: always let go" for a fully calm game |
| **Hits under the snap rule** | If you hit enough before the 2nd miss, the fish is landed with those hits |
| Bait effect | Baits do not change the bar (they change *who* bites). Exception: Walt's Mix (story bait) gives +10% green on Legends |
| Hint text | Left of the bar: "Something small…" (Everyday), "Fighting!" (Uncommon), "Strong one!" (Scarce), "Something heavy…" (Trophy), "…!" (Legend) |
| Music | The music ducks −6 dB during the minigame. Legend fights get their own 20 s cue |
| Assist (Settings) | "Fishing assist": pointer speed 70–100% (default 100%), and an optional rumble tick when the pointer enters the green (off by default). Neither changes prices; we judge pride is enough. **SUGGESTED** |
| Colour-blind | The green zone also has diagonal hatching and bracket caps; hits and misses also differ by icon (tick or cross), not only colour |

### 3.8 Reel and land

| Item | Spec |
|---|---|
| Reel time | `clamp(1.0 + 0.25 × √weight_kg, 1.0, 3.5)` s. Not interactive; it is the payoff |
| Animation | The reel hand cranks (cycle speed scales with fish size). The rod pumps up and down. For Scarce+ the fish breaks the surface once at 60% of the reel (a jump with a droplet trail) |
| Landing: small (< 3 kg) | Swung out of the water on the line, caught in the off hand |
| Landing: medium (3–15 kg) | Lifted with both hands over the rail |
| Landing: big (15–60 kg) | Gaff-and-haul over the rail with a grunt, then it thumps onto the deck |
| Landing: huge (> 60 kg) | A two-step haul that rocks the boat 2° (co-op: a nearby player auto-grabs the tail, a nice moment), then it lands on the deck |
| VFX | Water streams off the fish, a deck wet decal (fades after 20 s), droplets. The fish flops on the deck via vertex animation (no rig, §3.11) |
| Sound | The reel ratchet accelerates. The surface break *splash*. A wet *slap* on the deck. A satisfied exhale from the player |
| Camera | Eases to a 3/4 front close-up at the landing point (3.0 m, FOV 45, 0.5 s ease) |

### 3.9 The reveal

| Item | Spec |
|---|---|
| Pose | **The hold-up**: the player lifts the fish toward the camera (Animal Crossing style). Small fish one-handed, big two-handed, huge kneel beside it. The lionfish is held by the tail; the moray snaps at the camera |
| Timing | 2.0 s, skippable after 0.5 s. A first-of-species catch takes 3.0 s, with "NEW" |
| Card | Systems owns the visuals; the card shows: species name (display font); the grade badge in its colour + tier pips; size in cm and weight in kg (e.g. "64 cm · 2.3 kg"); "≈ $41 at the Fish Market today"; "NEW species", "Personal record!" (≥ previous largest) or "Island record!" (save-wide, co-op shared); the species class as a small label |
| Quip | One short line per species, rotating 3 variants ("A garfish! Green bones, honestly."). Text only for EA. **SUGGESTED** |
| Rarity stinger | Common: a soft pluck. Fine: a chime. Rare: a shimmer. Prized: a brass swell + an amber sparkle burst around the fish. Legend: horn + choir (its own) |
| Camera | Holds the close-up, then eases back to the baseline as the card leaves |
| Notifications | "NEW species" and "Record!" also post to the left alert stack (MUST: alerts on the left) so co-op friends see them |

### 3.10 Into the hotbar (MUST: stack 5)

| Item | Spec |
|---|---|
| Fly-in | The fish icon arcs from the hands to its slot in 0.35 s. The slot pulses and the count ticks up with a soft *thup* |
| Stacking | Same species **and** same grade stack, up to 5 (systems). Each fish keeps its own catch time, size and weight; the stack shows the oldest fish's freshness bar. Selling sells the oldest first |
| Slot choice | Stack onto an existing matching stack, else the first empty slot left to right |
| Hotbar full | The reveal card shows: **[E] Release** (the fish swims away: a nice splash, the bait refund is 0) or **[Q] Drop** (lands on the deck as a pickup for 120 s, so a co-op friend can take it). On the pad: A / X. **SUGGESTED** |
| Held fish | Selecting a fish slot shows it in the hand, so you can show it off, give it, or drop it. **Hold E on a teammate** to offer the selected stack (co-op) |
| Fish in the hand | It is not a weapon. **SUGGESTED** comedy: a big fish can slap an NPC goon later (a rival-section beat), the How to Fish lesson |

### 3.11 Procedural pieces that make it feel polished

| Piece | How |
|---|---|
| Rod bend | Rod mesh with a 6-bone chain. Bend = line tension 0–1 → tip angle 0–70°, distributed tip-heavy (bone weights .05/.08/.12/.18/.25/.32). Spring 8 Hz, damping 0.4. Tension: waiting 0.05, nibble 0.15 pulse, bite 0.6, minigame 0.55 + 0.1 per hit + 0.25 on a miss pulse, reel 0.4–0.8 by weight |
| Line | Verlet rope: slack sag while waiting (gravity + 3 cm per m of sag), straight under tension, pulled toward the water entry point. Collides with the boat rail |
| Fish | One mesh per species, **no skeleton**: a vertex-shader swim/flop (sine along the body's long axis, amplitude by state: hooked thrash 1.0, deck flop 0.6 decaying, held 0.2). Six base body shapes (torpedo, flat, eel, deep-body, long, odd) carry the 39 species via texture and proportion variants, so it stays Blender-cheap (Manan: our own Blender assets). 300–1,500 tris each |
| Player | Mixamo-based clips per the feel worker's rule, plus LateUpdate IK: hands on the cork and reel, look-at on the bobber. Clips needed: `RodIdle`, `RodCastWindup`, `RodCastRelease`, `RodWaitFidget×2`, `RodStrike`, `RodFightLoop` (lean back), `RodMissFlinch` (a small rod dip, no step; *(Kufu review 31 Oct)*), `RodReelLoop`, `LandSmall/Medium/Big/Huge`, `HoldUpSmall/Two-hand/Kneel`, `Stow`. Mixamo has generic stand-ins; the fishing-specific ones need a Blender pass. **OPEN QUESTION Q6** (who animates) |
| Boat | Players stand on a moving deck. Fishing works while drifting or anchored. The bobber is in world space; the line end follows the rod tip |

### 3.12 Failure and edge cases

| Case | Behaviour |
|---|---|
| Cast lands on the boat, land, a buoy or another bobber | Never. The marker shows red, and release snaps to the nearest valid water (§3.2) |
| Water too shallow (< 0.5 m) | The ring shows red; the bobber still lands at the nearest point ≥ 0.5 m |
| Player is hit or knocked down while waiting | Auto reel-in, bait kept |
| Knocked down during the minigame | The fish escapes, bait kept (rare: only goons or rogue waves) *(Kufu review 31 Oct)* |
| Boat sails off at > 2 m/s | Line drag, then auto reel-in after 1.5 s (bait kept) |
| Player opens the phone (Tab) while waiting | Allowed. The wait continues and the bite still shows (the phone UI is small). The strike works with the phone open |
| Phone opened during the minigame | Blocked (Tab ignored) until the reveal |
| Two bobbers within 1.5 m (co-op) | The second cast's marker can't land there; see tangles §4.2 |
| Disconnect mid-fight | The host resolves the fish as escaped, and the bait is refunded |
| Night with no light | The bobber tip glows; the bite splash adds a faint bioluminescent flash (stronger on dark-moon nights) |
| Storm | Bobber bob ×2. The bite stays readable (the splash scales up). Hint text: "Rough water". Trophy/Legend storm fish possible |
| Out of bait | Casting is allowed. Junk 50%, sprats 50% (§5.4) |
| Hotbar full when the bite comes | Not checked at the bite; handled at the reveal (§3.10) |

---

## 4. Solo, co-op, controller

### 4.1 Solo (MUST: fully fun solo)

- Everything in §3 works solo with no "needs a second player" gate.
- **The rod holder (§6.6)** is the solo multitask: one rod in the holder, one in the hands, a bell on a bite, and a 2.0 s window to run and grab it. It feels busy in a good way.
- Solo-only care:
  - The landing net is co-op flavour, never required.
  - Huge fish land without help (the haul just takes 0.6 s longer).
- Pacing: solo players get the same catch rate. No co-op multiplier on prices (economy).

### 4.2 Co-op: 2–4 players fishing from the same boat and spot

| Feature | Spec | Tag |
|---|---|---|
| Positions | Each player fishes from any valid rail spot. The T1 skiff "Biscuit" fits 2 comfortably (bow + stern), 3–4 crowded | SUGGESTED |
| Independent | Each player has their own cast, bite timer, minigame and reveal. One player's minigame never pauses another's | SUGGESTED |
| Shared spot | A hotspot has a shared catch pool (§5.3) that all lines drain. It is shown as fewer birds and fewer boils as it empties | SUGGESTED |
| **Tangles** | If two lines cross within 0.5 m for > 1 s (or bobbers within 1.5 m), the lines tangle: both rods jerk, a comic *boing* sound, "Tangled!" goes to the left alert for both, and both auto-reel in 1.2 s (bait kept). It is a funny clip and a nudge to spread out | SUGGESTED |
| **Landing net** | A tool item (Hook, Line & Sinker, $150, economy; *(Kufu review 31 Oct)*). A friend holding it within 2 m of a fighting angler presses E ("Ready the net"). The angler then gets **+1 retry and +10% green zone**, and on Scarce+ the friend scoops the fish (a net animation and a shared cheer). It helps without carrying anyone | SUGGESTED |
| Ghost bar | A teammate's minigame shows as a small ghost bar above their head (pointer and pips), so friends can watch and react | SUGGESTED |
| **Double!** | Two players hooked at the same moment, or within 3 s of each other: a "Double hook-up!" stinger and alert. Pure flavour | SUGGESTED |
| Huge fish | A nearby friend auto-grabs the tail on the landing (§3.8) | SUGGESTED |
| Ownership | The catcher owns the fish (it goes to their hotbar). Fish can be given (hold E on a teammate) or dropped on the deck. Money on sale goes to the shared business account (economy and systems own that) | SUGGESTED |
| Boat roles | One player drives to the next hotspot while others reel in; anyone can anchor. Nothing is role-locked (the source concept's "flexible roles") | SUGGESTED |
| Sleep | One player sleeping skips the night (MUST, systems). Fish in **every** player's hotbar age through the skip | Systems MUST + ours |

### 4.3 Controller map (KB/M ↔ gamepad)

| Action | KB/M | Gamepad |
|---|---|---|
| Select rod/fish slot | 1–8, scroll | LB / RB cycle; D-pad left/right |
| Aim | Mouse | Right stick (light magnetism toward hotspots) |
| Charge, then cast | Hold LMB, release | Hold RT, release |
| Cancel charge / reel in early | RMB (hold 0.3 s to reel) | B (hold 0.3 s) |
| Strike / stop the pointer | LMB (also Space) | RT (also A / any face button) |
| Bait pouch radial | Hold R | Hold Y |
| Skip reveal | LMB / E | A |
| Release / drop on a full hotbar | E / Q | A / X |
| Give fish to a teammate | Hold E on them | Hold X on them |
| Phone (never in the hotbar) | Tab | View/Back |

Rumble table: nibble 0.15/80 ms · bite 0.8/300 ms · hit 0.5/60 ms · miss 0.6/220 ms · line snap 1.0/400 ms · landing 0.4/120 ms · Prized 3× 0.3/50 ms ripple. DualSense: adaptive-trigger tension on RT during the minigame (resistance = rod tension). **SUGGESTED** if Unity's input stack allows it for free.

### 4.4 Netcode notes (for the build, SUGGESTED)

- **Host decides.** At the bite, the host rolls the species, size and weight.
- **The client judges timing.** The minigame runs on the angler's own machine, judged by local timestamps. It sends {stop timestamps, hits} to the host, and the host checks they are plausible: each stop ≥ 120 ms after the previous one, and no more than 5 stops.
- **Latency never touches the timing.** Others see the ghost bar with about 100 ms of delay, which is fine.
- **Hotspot pools are host-owned**, along with prices and freshness clocks.

---

## 5. Fish

### 5.1 No zones: any fish, anywhere *(Manan 31 Oct)*

**MUST (Manan 31 Oct): "Any fish can be caught anywhere, no sections."** The fishing zones of the first draft (Shallows, Kelp Shelf, Gullrock Reef, the Margaret wreck, the Drop-off, Voss Lanes, Lantern Glow, the Estuary, Open Water) are gone as *fish pools*. The places stay on the map as places (travel, scenery, story, hotspots), but no place decides which fish can bite.

What decides the fish now:
1. **The rod (hard gate).** Each rod adds 3 fish and still catches the earlier ones (MUST "15 fish, 3 per rod"). A fish above your rod never bites.
2. **The bait (soft).** Favourite bait ×4, a liked bait ×2, any other bait ×0.5.
3. **Time and weather (a light nudge, MUST *(Manan 31 Oct)*).** ×1.5 at a fish's favourite time or weather, **never a lock**: every fish your rod allows can bite at any hour in any weather.
4. **The legend (hard gate, MUST *(Manan 31 Oct)*):** only with the best rod (Old Faithful) **and** the best bait (Walt's Mix).

Time windows (in-game clock): **Dawn 05–08, Day 08–17, Dusk 17–20, Night 20–05.** Weather: **Clear, Fair, Breezy, Fog, Rain, Storm**. (The old "Cloudy" roster word is gone; the CSV now says Fair.)

### 5.2 Roster: 15 fish, 3 per rod (MUST, final for the build *(Manan 31 Oct)*)

Manan: "15 fish, 3 per rod"; the ladder below was approved on 31 Oct (the five picks in `gdd/DISCUSS-inventory-and-fish.md`). All 15 come from the first draft's 39, with the same prices. `fish-roster.csv` is the source of truth; this table mirrors it. Prices are base: Fresh, Common grade, mid size.

| # | Rod | Fish | Class | Baits (★ favourite ×4, others ×2) | Nudge ×1.5 | Base $ | Size cm | Weight kg | Flavour |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 1 Walt's Bamboo | Harbour Sprat | Everyday | ★Worms (also a bare hook) | – | 6 | 8–15 | 0.01–0.04 | Tutorial fish; the cast net catches it too |
| 2 | 1 | Grey Mullet | Everyday | ★Worms, Shrimp | Day | 8 | 25–55 | 0.3–2 | Cruises our dock pilings; visible from the pier |
| 3 | 1 | Mackerel | Everyday | ★Worms, Shrimp, Sardine | Dawn, Clear | 10 | 25–42 | 0.2–0.9 | Shoals under diving gulls |
| 4 | 2 Fibreglass | Garfish | Uncommon | ★Shrimp, Sardine | Dusk, Clear | 20 | 50–85 | 0.3–1.2 | Green bones; leaps on the last hit |
| 5 | 2 | Black Bream | Uncommon | ★Shrimp, Squid | Day | 24 | 25–50 | 0.4–2.5 | Steady daytime biter |
| 6 | 2 | Conger Eel | Uncommon | ★Sardine, Squid | Night | 30 | 60–180 | 1.5–25 | Lives under our old pier; big ones are HEAVY on the starter line (opt-in fight, §3.7) |
| 7 | 3 Carbon | Red Snapper | Scarce | ★Squid, Sardine | Dusk | 55 | 35–90 | 1–12 | The Pelican's staple order |
| 8 | 3 | Mahi-mahi | Scarce | ★Sardine, Shrimp | Day, Clear | 65 | 70–140 | 4–20 | Fades gold → grey as it goes stale |
| 9 | 3 | Dusky Grouper | Scarce | ★Squid, Sardine | Day, Fair | 70 | 50–120 | 3–35 | Cousin of Walt's legend |
| 10 | 4 Deep-sea | Ocean Sunfish | Trophy | ★Squid, Shrimp, Walt's Mix | Day, Clear | 140 | 80–250 | 40–300 | Absurd two-handed wobbling reveal |
| 11 | 4 | Blue Shark | Trophy | ★Sardine, Squid, Walt's Mix | Rain | 160 | 150–300 | 30–150 | Circles the boat before it bites |
| 12 | 4 | Swordfish | Trophy | ★Squid, Sardine, Walt's Mix | Night | 180 | 120–300 | 30–250 | Leaps twice during the fight |
| 13 | 5 Old Faithful | Deep Anglerfish | Trophy | ★Squid, Walt's Mix | Night | 200 | 40–100 | 3–25 | Lure light stays lit in the hotbar |
| 14 | 5 | Giant Oarfish | Trophy | ★Walt's Mix, Squid | Storm | 220 | 300–800 | 20–200 | The reveal pans along its length |
| 15 | 5 | **Old Brass** (Legend) | Legend | **Walt's Mix only** | Dusk, Rain | 1,500 | 210–240 | 180–220 | Walt's lifelong golden grouper (§5.5) |

Cut from the first draft *(Manan 31 Oct)*: 24 regular fish (pollock, flounder, herring, wrasse, shad, scad, hake, sea trout, bass, parrotfish, lionfish, bonito, barracuda, monkfish, lanternfish, moray, tuna, halibut, sturgeon, opah, eagle ray, …) and 3 legends (the Lantern Queen, Silverbill, Mudmother). A copy of the 39-fish CSV is kept outside this folder (Kufu's/this worker's scratchpad) in case any come back after EA.

**Content waves:** wave M1 (systems M1, boat + fishing) = rod 1's 3 fish + rod 2's 3 behind a debug switch; the Steam demo = rods 1–2 (6 fish); EA = all 15.

### 5.3 Which fish bites: the roll (MUST rules *(Manan 31 Oct)*; numbers SUGGESTED)

**At the bite, on the host:**

1. **Legend check.** Only if the rod is **Old Faithful AND** the bait is **Walt's Mix** (MUST). Then **5% per cast** is Old Brass on the first hunt (×1.5 at dusk, ×1.5 in rain, so up to ~11% at a rainy dusk). After the first catch (the story beat, §5.5) it can bite again, **rarely: 1% per cast** (MUST *(Manan 31 Oct)*).
2. **Species.** Candidates = every regular fish whose rod tier ≤ your rod. Weight = **bait** (★ ×4, liked ×2, other ×0.5) × **tier** (rod 1 fish ×1.0, rod 2 ×0.7, rod 3 ×0.45, rod 4 ×0.3, rod 5 ×0.2: newer fish are rarer) × **nudge** (×1.5 at its favourite time, ×1.5 in its favourite weather). Nothing is ever ×0, so no fish is locked out by place, hour or weather.
3. **Hotspots** (fish signs: diving gulls, a boil of bait fish): the bite comes 2 s sooner (never under 4 s) and the tier weights of rods 3–5 double inside the 8 m circle. 6–10 catches each, then the signs thin out and a new one appears elsewhere after 3–6 in-game hours. Shared in co-op. They pick *where it's busy*, never *which fish exist*.
4. **Size.** `r = u^1.6` (skewed small). `size = min + (max − min) × r`; `weight = wmin + (wmax − wmin) × r^2.6`. 3% of bites are "trophy size" (r ∈ [0.85, 1]), "Big one!" in the hint text.
5. **Price.** `price = base × (0.8 + 0.4 × r) × grade × freshness × market × saturation × daily`. Economy owns market, saturation and daily.

**Who usually bites** (from `fishing_model.py`, averaged over every time and weather):

| Rod + bait | Top 3 bites |
|---|---|
| Bamboo + Worms | Sprat 44% · Mullet 33% · Mackerel 23% |
| Fibreglass + Shrimp | Bream 30% · Garfish 22% · Mullet 21% |
| Fibreglass + Sardine | Conger 41% · Mackerel 21% · Garfish 15% |
| Carbon + Squid | Snapper 23% · Conger 18% · Bream 18% |
| Old Faithful + Walt's Mix | Oarfish 12% · Sprat 11% · Swordfish 10% (+ Old Brass 5%) |

So bait still means *which crowd*: on the Fibreglass rod, shrimp brings bream and garfish, sardine brings conger. The shop card and the Journal show each bait's fish as silhouettes that fill in as you catch them. **SUGGESTED**

### 5.4 Junk and collectibles (SUGGESTED)

Junk only comes when casting without bait (half of those casts), or as 2% of any cast near the Margaret wreck:
- **Old Boot** ($0, a comic reveal);
- **Kelp Clump** (can be sold as fertiliser for $1);
- **Rusty Tin**;
- **Message in a Bottle**: 12 of **Walt's letters**, the lore drip and the Old Brass quest chain. They are collectibles (MUST: collectibles live in the hotbar) and go to a shelf in the office.

### 5.5 The legend: Old Brass (MUST rules *(Manan 31 Oct)*)

| Legend | Hook to the story | How to get it | Reward |
|---|---|---|---|
| **Old Brass** (giant golden grouper) | Walt's lifelong fish. Decades ago it broke his rod at the Margaret, and the photo of "the one that got away" hangs in the run-down office | Find 4 of Walt's letters → June restores **Walt's rod as Old Faithful** ($15,000) → June starts selling **Walt's Mix, $40 each** (MUST *(Manan 31 Oct)*). Then cast Old Faithful + Walt's Mix anywhere (any fish anywhere); dusk and rain help | **First catch = the story moment** (unique music, the hardest bar, a 4 s reveal; Walt's voice on the phone): **mount it in the office** (decor + "the port remembers Walt" beat) **or sell it** ($1,500 base). **After that it can bite again, rarely (1% a cast), for money** (MUST *(Manan 31 Oct)*) |

Silverbill was the first draft's rival beat (Voss's fake marlin trophy). It is cut with the other legends *(Manan 31 Oct)*; the rival beat now uses **Walt's Hook** instead: an evidence photo of Walt's stamped brass hook in Voss's mounted marlin (gdd/world §4.5).

### 5.6 Freshness and rot (MUST ≥ 2 in-game days)

| Stage | In-game hours since catch | Real minutes awake | Price × | Look and sound |
|---|---|---|---|---|
| **Fresh** | 0–12 | 0–12 | 1.00 | Glossy icon, bright eye |
| **Good** | 12–24 | 12–24 | 0.85 | Icon slightly matte |
| **Stale** | 24–36 | 24–36 | 0.60 | Icon desaturated 40%, a dull-eye overlay; the mahi-mahi fades gold→grey |
| **Old** | 36–48 | 36–48 | 0.35 | Icon green-grey, 1 fly sprite circling the slot; a held fish gives off faint stink lines |
| **Rotten** | ≥ 48 | ≥ 48 | 0: markets refuse it | A brown icon, 3 flies with a buzz when selected. Uses: **chum** (throw over the side: the bite window becomes 4–7 s and the rarer fish of your rod bite ×1.5 for 2 in-game h at that spot), **crab-pot bait** (§6.7), or the $1 compost bin at the dock |

- The clock is in-game time and runs through sleep. Catch at 18:00 and sleep from 22:00 to 07:00: at the market it's 13 h old, still "Good" at 85%. Sleep once more and it's "Stale" by the next noon.
- The freshness bar is on each hotbar slot (systems). The stack shows its oldest fish, and the tooltip lists every fish in it.
- **Storing and slowing rot *(Manan 31 Oct)*: the warehouse.** Walt's warehouse at our dock (from Day 1, expandable; systems §1.1.6) is the one place to store fish outside the hotbar, 10 fish a slot. Open racks rot at the normal speed; its **cold room** (T2+, $2,500) ages fish at 25%, the T4 Ice House at 10%. The live well, insulated satchel and ice chest of the first draft are cut.

---

## 6. Baits and tackle

### 6.1 Baits (MUST: buyable; each gives different fish and prices)

Bait lives in a **tackle pouch** attached to the rod, not in a hotbar slot. You switch with the radial (hold R / Y). There are **5 baits, one new per rod tier** *(Manan 31 Oct)*. The pouch starts with **3 bait types**; one upgrade ("Tackle pouch: all 5", $250, economy) holds all 5; 99 of each. Extra bait waits in the warehouse. **SUGGESTED** (economy adopted it; **OPEN QUESTION Q2** asks Manan to confirm bait isn't a hotbar item).

| Bait | Price | Where | Uses | Best for | Unlock |
|---|---|---|---|---|---|
| **Worms** | $5 / 10 | Hook, Line & Sinker (June Pike, Old Saltmouth). Also Walt's worm bin at our dock gives **5 free per morning** (so a broke player is never soft-locked) | 1 per landed fish | ★ sprat, mullet, mackerel (rod 1) | Start |
| **Shrimp** | $12 / 10 | Hook, Line & Sinker; from M3, the cast net catches live shrimp | 1 | ★ garfish, bream (rod 2); liked by mullet, mackerel, mahi, sunfish | Act 1 |
| **Sardine (cut bait)** | $18 / 10 | Hook, Line & Sinker; the cast net's sprats cut into sardine-equivalent bait | 1 | ★ conger, mahi, blue shark; liked by mackerel, garfish, snapper, grouper, swordfish | Act 2 |
| **Squid strip** | $25 / 10 | Hook, Line & Sinker | 1 | ★ snapper, grouper, sunfish, swordfish, anglerfish; liked by bream, conger, shark, oarfish | Act 2 |
| **Walt's Mix** (the best bait) | **$40 each** (MUST *(Manan 31 Oct)*) | Hook, Line & Sinker, **once you own Old Faithful** | 1 | **The only bait for Old Brass**; ★ oarfish; liked by sunfish, shark, swordfish, anglerfish. Small fish ignore it | Act 4 |
| **Chum** | From rotten fish (free) | – | Thrown, not hooked | Spot buff (§5.6) | M2 |

Orders and missions can ask for a bait-specific fish (e.g. a Pelican order for "Squid-caught snapper" is just "Red Snapper, Rare+"), so bait choice feeds orders (MUST "orders/missions later").

### 6.2 Rods: THE headline upgrade (SUGGESTED; *(Kufu review 31 Oct)*)

Manan: "the minigame timing widens when we buy expensive fishing rods" and "the better rods will also increase catching time" (read as: fish bite sooner). So the rod is the one item that changes **three** things you feel at once: a **wider green zone**, a **slower pointer** and a **shorter bite wait**. Names and prices follow economy (its shop table wins; the first draft here had six rods at other prices).

| Rod | Price | When | Adds these 3 fish *(Manan 31 Oct)* | Green zone × | Pointer speed × | Bite wait | Cast range | Notes |
|---|---|---|---|---|---|---|---|---|
| **Walt's Bamboo Rod** | $0 | Start (in the office) | Sprat, Mullet, Mackerel | ×1.00 | ×1.00 | **4–10 s** | 6–16 m | Taped grip, squeaky reel seat. Never sold ("That's Walt's") |
| **Fibreglass Rod** | $450 | Day 1, a visible goal in June's window | Garfish, Bream, Conger | ×1.15 | ×0.95 | **4–9 s** | 7–20 m | Rod-holder compatible. The first "oh, that's nicer" |
| **Carbon Rod** | $1,600 | Act 2 | Red Snapper, Mahi-mahi, Dusky Grouper | ×1.30 | ×0.90 | **4–8 s** | 8–24 m | Light; a faster cast wind-up (0.9 s) |
| **Deep-sea Rod** | $6,000 | Act 3 | Ocean Sunfish, Blue Shark, Swordfish | ×1.40 | ×0.85 | **4–7 s** | 10–28 m | The first Trophy fish. Belt-harness fight pose |
| **Old Faithful** (Walt's rod, restored) | $15,000 restoration | Journal quest, Act 4 (after Walt's letter 4) | Deep Anglerfish, Giant Oarfish, **Old Brass** (with Walt's Mix) | ×1.50 | ×0.80 | **4–6 s** | 8–30 m | The best rod: MUST for the legend {M}. Gold whipping on the guides; Walt calls when you first cast it |

- **What you see in the shop:** each rod card shows three bars (Green zone, Calm pointer, Quick bites) and a 3-second looping preview of the bar at that rod's speed, so the upgrade is felt before it's bought.
- **The bite range is the MUST:** every rod stays inside 4–10 s (§2 R1). The hotspot/chum bonus stacks on top but never below 5 s for the top of the range.
- **Hours check:** a practised player on the starter rod catches ~60–80 fish an hour. With Old Faithful the mean wait drops from 7.0 s to 5.0 s and the grade rises, so fishing by hand pays ~25–35% more per hour at the same bait, which keeps hand-fishing worth it next to workers.

### 6.3 Reels: strike window only (SUGGESTED; *(Kufu review 31 Oct)*)

The pointer speed moved to the rod (§6.2), so the rod is the headline and reels stay a small, cheap comfort buy.

| Reel | Price | Strike window + |
|---|---|---|
| Rusty Reel (starter, on Walt's rod) | $0 | +0 s (1.20 s) |
| Click Reel | $120 | +0.10 s |
| Smooth Spinning | $500 | +0.15 s |
| Twin-drag | $1,800 | +0.20 s (1.40 s) |

### 6.4 Lines: max fish weight before the snap rule (SUGGESTED)

| Line | Price | Rating |
|---|---|---|
| Frayed mono (starter) | $0 | 8 kg (a big conger is "HEAVY" and teaches the opt-in rule early: you can always let it go) *(Kufu review 31 Oct)* |
| Mono 15 | $40 | 15 kg |
| Braid 40 | $250 | 40 kg |
| Braid 120 | $900 | 120 kg |
| Walt's Line (story; with the restored rod) | – | 300 kg |

### 6.5 Hooks and bobbers (SUGGESTED)

| Hook | Price | Effect |
|---|---|---|
| Bent hook (starter) | $0 | – |
| Circle hook | $40 | Strike window +0.15 s |
| Barbed hook | $250 | +1 retry (a missed stop is redone once) |
| Treble hook | $1,200 | +2 retries. With the base 3 stops this gives the hard cap of 5 |

Windows add up: the best reel + Circle hook gives 1.20 + 0.20 + 0.15 = 1.55 s (the "+0.35 s" in §3.6) *(Kufu review 31 Oct)*. Hooks are one slot, so it's the Circle **or** the Barbed **or** the Treble: a real choice between a forgiving strike and a forgiving fight.

**Bobbers** are cosmetic, plus one functional one: the **Sight float** ($30) is 1.5× bigger with a bright halo, for long casts and accessibility. Colours are unlocked through the shop, orders and Legends.

### 6.5b Equipping tackle

- **At the bait shop or the office workbench** (SUGGESTED): rod, reel, line and hook are set on the rod item.
- **Hotbar tooltip:** shows `Carbon Rod · Smooth reel · Braid 40 · Barbed`.
- **Upgrades:** no mid-cast swapping.

### 6.6 The boat as a fishing platform (SUGGESTED, the boat/world section to match)

| Thing | Spec |
|---|---|
| Where you stand | Any rail spot (§3.1). Each boat model marks "rail volumes" in the prefab. The T1 skiff "Biscuit" has bow, port, starboard and stern. Fishing is never allowed from the helm while steering |
| Anchor | E at the bow cleat, 1.5 s to drop or raise, and the boat holds its spot. Without it the boat drifts 0.1–0.3 m/s with wind and current, and the bobbers drift with it |
| **Rod holders** | Buy at the boat shop: Biscuit 1 (after the "repair rail" upgrade), T2 launch 2, T3 trawler 4. Put a cast rod in a holder (E): it waits by itself and runs the same 4–10 s bite. On a bite **a bell rings**, the holder rod bends, and the window is **2.0 s** to run over and press E. Then the minigame runs as normal. You need a second rod (a second hotbar slot) to fish two lines. Solo's best friend |
| Trolling | With a holder + sardine or squid and the boat moving 2–5 m/s, holder rods troll on the normal timer (the same roll as §5.3; the Spinner lure is cut *(Manan 31 Oct)*). Lovely solo: drive while the lines work. M4 |
| Gulls | Near Gullrock (a place, not a fish zone), a fish dropped on the deck for more than 20 s may be snatched by a gull (comedy, recoverable: the gull drops it 30 m away 10% of the time) |
| ~~Live well~~ | Cut *(Manan 31 Oct)*: the dock warehouse and its cold room are the one rot-slower (§5.6) |

### 6.7 Nets and traps: later upgrades (SUGGESTED)

| Tool | Price | Unlock | How it works | Output (into the hotbar, stack 5) |
|---|---|---|---|---|
| **Cast net** | $180 | M3 | Throw from the boat or pier in water < 8 m: a spin-throw animation, the net blooms on the water, hold E 1.5 s to haul. 10 s cooldown. No minigame | 2–5 bait fish → converted straight into the bait pouch ("Live sprat" = Sardine tier; "Live shrimp" = Shrimp tier). Makes our own bait |
| **Crab and lobster pots** (parked *(Manan 31 Oct)*: "15 fish" is the whole catch list; shellfish would be extra, so pots wait for Manan) | $90 each; max 4, 8 with an upgrade | after EA | Drop from the boat (a buoy in our colour), bait with rotten fish or squid, soak **6 in-game hours**, haul (hold E 2 s, winch animation) | Shore Crab $5, Brown Crab $15, Lobster $45, Spiny Lobster $70. 1–4 per pot. They go into the hotbar like fish (stack 5), with the same freshness rules |
| Pot theft | – | Mid-game | Voss goons may cut buoys: a warning by phone first, then a recoverable loss (the source concept's rule: rival harm stays recoverable). The rival section owns this | – |
| Trawl and longline | – | Workers only | Worker boats fish with nets as automation (MUST: workers fish for us). The player never trawls; the hand-rod stays the star | Economy/systems own |

---

## 7. Selling fish and the economy link

Economy (gdd\economy) owns the final numbers. Below is what fishing assumes and adds.

### 7.1 Where to sell

| Buyer | Pays | Hours | Takes | Notes |
|---|---|---|---|---|
| **Old Saltmouth Fish Market** (Voss-owned, buyer Gideon Pratt) | **60%** of base × grade × freshness × saturation × daily (economy) *(Kufu review 31 Oct)* | **05:00–16:00** (economy) | All fish except Rotten | The first-week sell point; the story sting is "it's Voss's market". Walk up and sell per stack or "Sell all fish" |
| **The Pelican** (restaurant) | 150–200% on orders | 11:00–22:00 | Only its order items; Fresh or Good only | Phone orders (§7.3) |
| **Hale's Landing fish stall** (later) | 100%? | – | – | A dock-shop upgrade so we can sell at home once the port grows. Economy to price. **OPEN QUESTION Q3** (does Manan want home selling?) |
| Voss's buyer barge | 60%, instant, at sea | Day | All | Floats in the Voss Lanes: sell without sailing home, but it feeds the rival. A choice, not a trap. **SUGGESTED**; the rival section decides |

### 7.2 Daily prices (economy owns; fishing proposes)

- **Saturation:** −2% per fish of the same species sold at that market that day, floor 60%, reset at 07:00 (economy).
- **Daily drift:** each species' price moves ±15% per day around its base (a random walk pulled 30% back toward 1.0 each day, clamped to 0.75–1.35). **SUGGESTED**
- **Catch of the Day:** one species per market at ×1.5, shown in the **phone's day forecast** at 07:00 next to the weather (MUST: the phone shows the forecast). A good morning question: "Where are the mackerel today?". **SUGGESTED**
- **Weather link:** in a storm the market pays +10% (fewer boats went out). **SUGGESTED**

### 7.3 Orders and missions (phone contracts, MUST: messages and contracts on the phone)

| Type | Example | Reward | Fail |
|---|---|---|---|
| Restaurant order (The Pelican) | "3 × Red Snapper, Rare or better, Fresh, by 19:00 today" | 150–200% of value + Pelican trust | Not delivered: the order expires, −trust, no money lost |
| Standing weekly | "10 Mackerel every Monday" | Steady income | – |
| Bait-specific | "Squid-caught snapper" (= Red Snapper, Rare+) | ×1.8 | – |
| Story mission | Walt's letters chain → Legends (§5.5) | Rod restoration, Walt's Mix, decor | Cannot fail; waits |
| Town requests | "Kid at the pier wants to see a garfish" (show it, don't sell it) | Small cash + a bobber cosmetic | – |
| First income (MUST) | The vendor asks by phone to moor at our dock and pays us (economy/story) | – | Not fishing; noted so the fishing tutorial ends before that call |

Delivery: the fish must be in your hotbar at the buyer (inventory-only). A delivery checks grade and freshness per fish and takes the best matching fish first.

### 7.4 Workers (MUST: the first automation)

- **Catch:** a worker assigned to one of our boats (from the phone) fishes at the spot you pin (any fish anywhere *(Manan 31 Oct)*) with the rod and bait you give them from the warehouse. They do **not** play the minigame; grade × = 1.0 + 0.15 × skill (economy).
- **Rate:** SUGGESTED 1 fish per 45 real seconds (slower than a good player). The species roll is the same as §5.3.
- **Where the fish go:** into **the warehouse** (its cold room first, when built) *(Manan 31 Oct)*, or to "sell at best open buyer" with a Dockhand (economy §5.4).

### 7.5 How fish feeds the economy *(Manan 31 Oct)*

- **The rod is the ladder.** Each rod opens 3 new, pricier fish and makes the bar easier; the right bait steers to them. From `fishing_model.py` (practised player, Fresh, mid size, best bait per rod, before buyers):

| Rod | Best bait | $/fish | Fish per real hour | $/real hour |
|---|---|---|---|---|
| Walt's Bamboo | Shrimp (Worms ~$16) | ~$18 | 70 | ~$1,160 |
| Fibreglass | Sardine | ~$44 | 72 | ~$2,980 |
| Carbon | Squid | ~$83 | 74 | ~$5,900 |
| Deep-sea | Squid | ~$150 | 76 | ~$11,100 |
| Old Faithful | Walt's Mix | ~$388 | 78 | ~$26,600 |

- **Stage 1** (bamboo rod, worms): ~$13 a fish for a first-timer, ~$16 practised. Economy budgets S1 hand fishing at **$1,000/h net** (Pratt's 60% blended with Mari's and Amara's 100%+); see economy §2.3 for the first days (Mari on Day 3 or at $250 total earned, ~Day 2 15:00; engine $90) (MUST *(Manan 31 Oct)*).
- **Sinks that keep fish money flowing:** bait (small; Walt's Mix is the one costly bait at $40 a cast), tackle (the big one-off costs), freshness decay, saturation (forces variety and the trip to The Pelican), and the hotbar limit (forces sell trips or the warehouse).
- **Fish → business:** money buys rods (new fish), boats (comfort, speed, room for crew) and the port. Workers take over the hand-fishing grind once the port business starts (MUST progression: earn → upgrade → …).

### 7.6 Numbers check (from `fishing_model.py`, see `model-output.txt`; re-run *(Manan 31 Oct)*)

```
15 species, 3 per rod, 1 legend (Old Faithful + Walt's Mix only; 5%/cast first time, 1%/cast after); every fish bites anywhere, any time, any weather
Stop-1 window (ms), Bamboo -> Old Faithful: Everyday 211->396 · Uncommon 180->338 · Scarce 155->290 · Trophy 120->225 · Legend 93->174
Practised, chance of Prized per rod (Bamboo..Old Faithful): Everyday 59/75/87/93/97% · Scarce 33/48/64/75/84% · Legend 10/16/24/33/42%
  Everyday  C:  5%  F: 26%  R: 44%  P: 25%   price x1.76   (novice, starter rod)
  Everyday  C:  0%  F:  7%  R: 34%  P: 59%   price x2.14   (practised, starter rod)
OK: all MUST asserts passed
```

Model assumption: timing error is normal with σ 110 ms (novice) or 70 ms (practised). Tune it from playtests: the F4 debug overlay logs real per-stop errors.

---

## 8. Build order: one thing at a time (MUST), each with Manan's test

M1 = boat + fishing (MUST, Manan's first build). Each step is built, self-tested by us, then handed over; Manan tests the starred ones.

**Tools we build alongside, SUGGESTED:**
- **F3 overlay:** a debug overlay (bite timer, species roll with its weights, window ms, stop errors).
- **`-pb-fishtest N`:** a batch bot that casts N times and logs the bite waits, nibble counts and the grade mix to `Logs/fishtest.csv`. The test checks below read that log.

| Step | What gets built | Manan's test checklist |
|---|---|---|
| **F1** Rod in hand | Rod item in hotbar slot 1, draw/stow, idle sway, walk with the rod. Assumes the boat step is done (walkable deck, from the boat/world section) | ☐ Press 1: the rod comes out in ~⅓ s and looks held, not glued. ☐ Walk the deck: the tip lags a little and sways with the boat. ☐ Sprint: the rod stows. ☐ Scroll away and back: no pop or jitter |
| **F2** Cast + bobber | Charge (1.1 s ping-pong), landing ring, shoulder cam, line arc, bobber land/bob/ripples/drift, cancel, reel-in | ☐ 10 casts at different power: short power lands near (~6 m), full lands far (~16 m). ☐ The ring turns gold at the top of the charge. ☐ Aim at the boat or land: the ring turns red, and release still lands in water. ☐ The bobber is always visible (also at 16 m and at dusk). ☐ The line never cuts through the boat. ☐ RMB reels in |
| **F3** Wait, nibbles, bite, strike | 4–10 s timer, nibbles, the bite (sight, sound, rumble), the strike window, early/late outcomes, left alerts | ☐ 20 casts with a stopwatch: every bite between 4 and 10 s (the overlay shows the timer; `-pb-fishtest 500` log: min ≥ 4.0, max ≤ 10.0). ☐ You can always tell a nibble from a bite with eyes closed (sound) and by the pad alone. ☐ Strike on a nibble: "Too soon!" on the LEFT, bait still on. ☐ Wait too long: "It swam off.", bait still on. ☐ The strike never feels rushed (1.2 s) *(Kufu review 31 Oct)* |
| **F4** ★ The minigame | Bar, pointer, green zone per class, 3 stops, results, grades, heavy/snap, retries (data only), timing offset setting | ☐ Stop 1 green is in the middle. ☐ Hitting feels crisp: the ding rises over 3 hits. ☐ 0 hits still lands a Common fish. ☐ A heavy conger on the starter line: the "Something HEAVY!" card asks first; Let it go is calm; Fight it then snaps only on the 2nd miss. ☐ A normal fish never snaps, and a miss never shakes the camera or trips you *(Kufu review 31 Oct)*. ☐ The pointer never jumps or stutters at 60 fps or 144 fps. ☐ The overlay shows your ms error per stop: does "practised" (σ ~70 ms) feel right? |
| **F5** Catch, reveal, hotbar | Species roll (the bamboo rod's 3 fish + the Fibreglass rod's 3 on a debug switch *(Manan 31 Oct)*), size/weight, reel-in, landing by size, hold-up reveal card, fly-in to the hotbar, stacks of 5 by species+grade, full-hotbar release/drop | ☐ Catch 15 fish: names, sizes and weights vary and look right in the hands. ☐ The reveal is skippable after half a second. ☐ The 6th mackerel of the same grade starts a new stack. ☐ Fill all 8 slots: the next catch offers Release/Drop. ☐ "NEW species" shows on the left the first time |
| **F6** ★ Feel pass (= **M1 for Manan**) | Final animation set, rod bend, all VFX and sounds in §3, haptics, camera moves, the premium UI from systems, fish meshes for 6 species | ☐ Fish 15 minutes solo from the skiff off Hale's Landing: does it feel good to repeat? ☐ Cast, bobber and reel look polished (MUST). ☐ Nothing clips (line, hands, fish). ☐ Pad only, then KB/M only: both feel complete. ☐ Note anything that feels slow: every timing is a number in a config we can tune |
| **F7** Co-op fishing | 2–4 on one boat, independent minigames, tangles, ghost bars, shared hotspot pool, give/drop fish, host-rolled catches | ☐ Two players fish from Biscuit: both get bites, neither waits for the other. ☐ Cast over a friend's line: "Tangled!" and a laugh, no bait lost. ☐ Hand a fish to a friend. ☐ Play with a 150 ms fake lag: the minigame still feels exact |
| **F8** Freshness + selling + bait buying (M2) | Freshness stages and icons, sleep aging, Hook, Line & Sinker bait purchase, the bait pouch radial, the Fish Market sell, saturation, the forecast "Catch of the Day" | ☐ Catch at dusk, sleep, sell in the morning: "Good" at 85%. ☐ Keep a fish 2 in-game days: it rots exactly at 48 h, not before. ☐ Sell 10 mackerel: the price visibly slides. ☐ Buy shrimp: reef fish appear that worms didn't give |
| **F9** Rod gates + nudges *(Manan 31 Oct)* | Rod tiers, bait weights, the ×1.5 time/weather nudge, hotspots with fish signs, all 15 species, the data validator (same asserts as `fishing_model.py`) | ☐ The bamboo rod never gives a garfish. ☐ The Fibreglass rod at night on sardine gives lots of conger, but mullet still turn up. ☐ Any fish your rod allows turns up anywhere, any hour. ☐ A bird hotspot visibly runs dry after ~8 catches and moves |
| **F10** Tackle | Rods, reels, lines, hooks, equip at the workbench, stats in tooltips | ☐ The Fibreglass rod's green zone is visibly wider, the pointer calmer, and bites come sooner. ☐ Old Faithful: bites every 4–6 s. ☐ The Barbed hook gives a second chance on a miss. ☐ The tooltip numbers match what you feel *(Kufu review 31 Oct)* |
| **F11** Rod holder + landing net + trolling | | ☐ Solo: two lines, the bell rings, you make it in 2 s. ☐ Co-op: the friend nets your snapper |
| **F12** Cast net | Cast net → bait (pots parked *(Manan 31 Oct)*) | ☐ The cast net fills the bait pouch |
| **F13** Old Brass + Walt's letters | 12 bottles, rod restoration, Walt's Mix at June's ($40), the one legend *(Manan 31 Oct)* | ☐ Without Old Faithful AND Walt's Mix it never bites. ☐ Old Brass: unique music, the hardest bar, mount or sell. ☐ After that, it can bite again (rarely) |

Rule: no step starts until the previous one passes Manan's or our checklist (MUST). Keep only the latest build on disk (MUST).

---

## 9. OPEN QUESTIONS for Manan

1. ~~Fish storage beyond the hotbar~~ **Answered *(Manan 31 Oct)*:** the expandable warehouse at our dock from Day 1 (slot = 10 fish, cold room slows rot); workers' fish go there too.
2. **Bait in a pouch, not the hotbar.** We put bait in a tackle pouch on the rod, so it doesn't eat one of the 8 slots. OK?
3. **Selling at home.** Should the dock get its own fish stall later? (Early on we must sail to Voss's market or The Pelican.)
4. **The minigame's green column.** Your note says "in the middle". Stop 1 is centred; we move it a bit on stops 2–3 so rhythm alone doesn't win. Keep it always centred instead?
5. **0 hits.** We land the fish as Common (the minigame only raises the grade). Should 0 hits lose the fish instead, for more tension?
6. **Animation source.** Who makes the fishing clips? Mixamo has only generic stand-ins (Manan downloads). The fishing-specific ones (cast, strike, hold-up) would be a Blender pass by us.
7. ~~Old Brass~~ **Answered *(Manan 31 Oct)*:** the first catch is the story moment (the player chooses mount or sell); after that it can bite again, rarely.
8. ~~River mouth~~: moot *(Manan 31 Oct)* (no fishing zones).
9. **Guns while fishing** (MUST: guns are hotbar items). Should anything at sea be shot (e.g. a shark stealing your catch, How to Fish style), or are guns only for the rival/goon content?
10. **Heavy fish opt-in** *(Kufu review 31 Oct)*. A fish heavier than your line asks first (Fight it / Let it go); only fighting can snap. Keep it, or never snap at all?
11. ~~Bait sets~~ **Answered *(Manan 31 Oct)*:** 5 baits, ★ favourite ×4, liked ×2, other ×0.5; the rod is the hard gate; time/weather a ×1.5 nudge, never a lock.

---

## Sources (all accessed 2026-10-31)

**WEBFISHING**
- [wiki: Fishing](https://webfishing.wiki.gg/wiki/Fishing)
- [wiki: Bait](https://webfishing.wiki.gg/wiki/Bait)
- [Dot Esports: rods](https://dotesports.com/indies/news/what-do-all-the-rod-upgrades-do-in-webfishing)
- [Dot Esports: baits](https://dotesports.com/indies/news/all-bait-and-what-it-does-in-webfishing)

**DREDGE**
- [wiki: Minigames](https://dredge.wiki.gg/wiki/Minigames)
- [StrategyWiki](https://strategywiki.org/wiki/DREDGE/Fishing)
- [Press Start review](https://press-start.com.au/reviews/ps5-reviews/2023/03/24/dredge-review-a-trophy-catch/)
- [GameSkinny](https://www.gameskinny.com/i0ll7/dredge-fishing-guide-how-to-catch-fish)

**Stardew Valley**
- [wiki: Fishing](https://stardewvalleywiki.com/Fishing)
- [forum thread "Fishing is impossible"](https://forums.stardewvalley.net/goto/post?id=19086)
- [Steam thread](https://steamcommunity.com/app/413150/discussions/0/4035852798876291083)
- [Full Fishing Bar mod](https://www.curseforge.com/stardewvalley/mods/full-fishing-bar)

**How to Fish**
- [GamingOnLinux](https://gamingonlinux.com/2026/08/how-to-fish-is-another-brilliantly-funny-online-co-op-game)
- [PC Gamer](https://www.pcgamer.com/games/sim/the-latest-cooperative-craze-is-a-physics-based-fishing-simulator-where-you-can-launch-your-catch-into-the-air-and-no-scope-it-with-a-sniper-rifle/)
- [Niche Gamer](https://nichegamer.com/how-to-fish-attracts-one-million-players-in-two-days/)
- [Notebookcheck](https://www.notebookcheck.net/Doom-but-with-gills-Why-this-new-indie-game-is-blowing-up-on-Steam.1376899.0.html)
- [GameBrief review](https://www.gamebrief.net/blog/how-to-fish-review-2026)
- Steam data in `replan\market\market.md`

**Cast n Chill**
- [Shacknews](https://www.shacknews.com/article/144913/cast-n-chill-review-score)
- [3DNews](https://3dnews.ru/1126062/cast-n-chill-review)
- [OpenCritic](https://opencritic.com/game/18729/view/reviews)

**Fisch**
- [Fisch wiki: Fishing Rods](https://fisch.fandom.com/wiki/Fishing_Rods)
- [Deltia's rod progression](https://deltiasgaming.com/roblox-fisch-rod-progression-guide)

**Animal Crossing: New Horizons**
- [Nookipedia: Fishing rod](https://nookipedia.com/wiki/Fishing_rod)
- [Pixel Poppers](https://pixelpoppers.com/2020/04/why-is-fishing-so-hard/)

**Sea of Thieves**
- [wiki: Fishing](https://seaofthieves.wiki.gg/wiki/Fishing)
- [PC Gamer guide](https://pcgamer.com/sea-of-thieves-fishing-guide)

**Fishing Planet**
- [Steam thread 1](https://steamcommunity.com/app/380600/discussions/0/364039531224785849)
- [Steam thread 2](https://steamcommunity.com/app/380600/discussions/0/4406291673456245096)

**Ultimate Fishing Simulator**
- [Gaming Nexus](https://www.gamingnexus.com/Article/5772/Ultimate-Fishing-Simulator/)
- [PlayStation Country](https://www.playstationcountry.com/?p=16529)
- [Steam thread](https://steamcommunity.com/app/468920/discussions/0/2789318172117446508)

**Caveats**
- Community wiki numbers (WEBFISHING, Fisch) can change between game patches.
- The Stardew wiki page returned 403 to our fetcher, so its points come from search snippets and the linked forum threads.

**Kufu review 31 Oct: research for the relaxing pass** (all accessed 2026-10-31)
- Dredge: Joel Mason (Black Salt Games) says the minigame's first goal was "for it to not be frustrating, so we never wanted to make it too hard or twitchy": [premortem.games interview](https://premortem.games/2023/03/30/unique-blend-of-genres-in-haunting-fishing-adventure-dredge-by-black-salt-games).
- Stardew Valley: the bobber bar is the genre's best-known complaint ("Fishing is impossible" threads; a mod exists only to fill the bar): [Stardew forums](https://forums.stardewvalley.net/goto/post?id=19141), [Full Fishing Bar mod](https://www.curseforge.com/stardewvalley/mods/full-fishing-bar). Gear that widens the bar (the Cork Bobber) is the fix players reach for, which is why our rod widens the green zone.
- Cast n Chill (2025): reviews praise low friction, gentle progression and an optional idle mode; top-critic average 87: [OpenCritic](https://opencritic.com/game/18729/view/reviews), [Shacknews review](https://www.shacknews.com/article/144913/cast-n-chill-review-score).
- WEBFISHING (2024): a social, chill hub where fishing is one of several small activities: [Wikipedia](https://en.wikipedia.org/wiki/Webfishing).
- How to Fish (Aug 2026): Overwhelmingly Positive co-op fishing comedy; the whole campaign is soloable: [GamingOnLinux](https://gamingonlinux.com/2026/08/how-to-fish-is-another-brilliantly-funny-online-co-op-game).
