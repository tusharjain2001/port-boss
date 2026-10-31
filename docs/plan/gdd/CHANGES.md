# Port Boss GDD: Kufu review, 31 Oct 2026

Kufu read all four sections in full (fishing, world, economy, systems), checked every MUST in `BRIEF.md` and the vault's 2026-10-31 decisions, cross-checked names and numbers, and applied Manan's new notes. Every edit in the four files is marked **"Kufu review 31 Oct"**. Pre-review copies of every touched file are in Kufu's session scratchpad (not in this folder), so any change can be rolled back on request.

## A. Manan's new notes, applied

| # | Change | Where | Why |
|---|---|---|---|
| A1 | **Relaxing fishing pillar** added at the top of fishing: a miss only lowers the grade; the only way to lose a hooked fish is a HEAVY fish you chose to fight; better rods buy calm | fishing header, §1.1 point 4 | Manan: "the fishing should be relaxing". Dredge's team made "not frustrating" the minigame's first goal; Stardew's bar is the genre's best-known complaint |
| A2 | **Wider minigame windows** (~30% wider on every class): Everyday 19% zone / 0.90 speed = 211 ms (was 160); Uncommon 180 (136); Scarce 155 (112); Trophy 120 (90); Legend 93 (73). Grade odds on the starter rod, Everyday: novice 5/26/44/25%, practised 0/7/34/59% | fishing §3.7 | Relaxing; still a skill curve (Legends stay hard: 10% Prized for a practised player on the starter rod) |
| A3 | **A miss no longer stumbles the player or shakes the camera**; the pip turns soft grey, a soft plop, light rumble. The `RodMissStumble` clip became `RodMissFlinch` | fishing §3.7, §3.11; systems M1 checklist | Manan: relaxing. A miss is "one grade lower", not a failure |
| A4 | **Heavy fish = opt-in.** A fish heavier than the line shows a choice card *before* the bar: "Fight it" or "Let it go" (default after 4 s). Only fighting can snap (2nd miss). Settings: "Heavy fish: always let go". Normal fish never snap | fishing §2 R8, §3.7, §6.4, F4 checklist, Q10 | Line snap stays as an optional thrill, never a surprise |
| A5 | **Strike window 0.80 → 1.20 s**; early strike, late strike and reeling in all **keep the bait** ("It swam off.") | fishing §0, §2 R10, §3.5, §3.6, §3.12, F3 checklist | Relaxing; bait is used only per landed fish |
| A6 | **Minigame mockup copy**: "Heavier than your 4 kg line: a 2nd miss snaps it" → "Fighting! Take your time · a miss only lowers the grade"; PNG re-rendered | `systems/mockups/05-minigame.html/.png`, systems §2.6 row 05 | The old copy taught the opposite of the new rule (and used a 4 kg line; the starter line is 8 kg) |
| A7 | **Bait → named fish SETS** with weights: favourite bait ×4, in the set ×2, outside the set ×0.5 (~9 in 10 bites from the set). Sets: Shore Set (worms, 10 fish), Reef & Kelp Set (shrimp, 10), Predator Set (sardine, 10), Big & Night Set (squid, 12), Pelagic Runners (spinner, 8), Night Lights (glow, 3), Deep Drop (jig, 4). Overlaps on purpose (mackerel is in 3 sets). The shop card and Journal show set progress ("4 / 10") | fishing §5.3 (new table), §6.1, Q11 | Manan: "one type of bait for some set of fishes", not one bait per fish. Built from `fish-roster.csv`'s Bait column, so no data changed |
| A8 | **The rod is the headline upgrade**: each rod widens the green zone, slows the pointer AND shortens the bite wait. Bamboo ×1.00/×1.00/4–10 s · Fibreglass $450 ×1.15/×0.95/4–9 s · Carbon $1,600 ×1.30/×0.90/4–8 s · Deep-sea $6,000 ×1.40/×0.85/4–7 s · Old Faithful $15,000 ×1.50/×0.80/4–6 s. Practised Prized chance on Everyday fish: 59 → 75 → 87 → 93 → 97% | fishing §2 R1, §3.5, §3.7, §6.2; economy tackle table (via `economy_model.py`) | Manan: "the minigame timing widens when we buy expensive fishing rods" and "better rods will also increase catching time" (read as: bites come sooner). Bite stays inside the MUST 4–10 s |
| A9 | **Reels now change the strike window only** (pointer speed moved to the rod) | fishing §6.3 | Keeps the rod the one big upgrade; avoids double-stacking |
| A10 | **Map A: Voss Harbour pushed to the far east tip** of a new long east arm, ~2 km over Saltback Ridge, 3.5 km by sea round the north. Travel: 9.6 min in Biscuit (6.4 Kestrel), 7.3 / 4.8 via the reopened Cut (was 6.8 / 2.8). Lantern Glow (F7) moved off the land into the north bay; overlapping labels moved; Walt's Cut now runs west coast → north bay | `world/maps/make_maps.py` (map A only) + `map-A.svg/.png` re-rendered; world §0.2 D5, §2.1, §2.3, §8.1, §8.4; economy Walt's Cut row; systems Q14 | Manan: "push Voss Harbour so it feels a separate bustling world on the opposite side". In the first draft it sat 440 m across the waist |
| A11 | Lighting through the day and the 6 presets with clothes-only edits: **already in systems** (§4.4 six lighting states; §1.9 wardrobe, §4.8.3) and checked; nothing to change | systems | Manan: art #4 with light changing through the day; all 6 presets; clothes only |
| A12 | One thing at a time, M1 = boat + fishing only: **already in systems §5**; M1 checklist gained the "miss on purpose" line | systems §5.2 | MUST |

## B. Contradictions fixed (one canon)

| # | Clash | Canon now | Files |
|---|---|---|---|
| B1 | Mari's mooring fee: world $20/day (and $25 on decline) vs economy $40 | **$40/day** (decline: $50) | world §4.2, §7.2, §9.1 |
| B2 | Pratt pays 80% (fishing) vs 60% (economy, world's "40% under fair"); world's "$4 for a $10 mackerel" | **60%**, "$6 for a $10 mackerel"; Pratt's price list Sprat $4 · Mullet $5 · Mackerel $6 · Pollock $5 | fishing header, §7.1, §7.5; world §4.1, §7.2 |
| B3 | Pratt's hours: fishing 05:00–19:00 vs economy 05:00–16:00 | **05:00–16:00** | fishing §7.1 |
| B4 | Biscuit engine: world $60, quoted Day 2, done Day 3 vs economy $150 bought Day 1 at minute ~9 | **$150 on Day 1 at 16:00, ready in 1 in-game hour**; Day 2 Fergus offers the $120 hull patch; "Biscuit repaired" beat became "Hull patched: no more bailing" | world §4.2, §7.2 Fergus thread, §9.1 rows 10:00, 10:30, 19:00, 30:10 |
| B5 | Amara's first order: world $14 each / $42 vs economy ~$30 each / ~$90 | **$30 each, ~$90** | world §7.2, §9.1 |
| B6 | End of hour 1 cash: world $150–250 vs economy $200–400 | **$200–400** | world §9.1 |
| B7 | Co-op prices: world "costs scale ×1.0/1.3/1.6/1.9" vs economy "prices never scale; order sizes do" | **Prices never scale**; orders ×1.0/1.6/2.1/2.5 | world §9.2 |
| B8 | Rods: fishing had 6 rods at other prices (Walt's Old Rod, Bamboo Spinner $150, Fibreglass $600, Carbon Pro $2,000, Deepwater $4,500, Walt's Rod Restored $800) vs economy's 5 | **Economy's 5 rods and prices**, with fishing's new stats (A8) | fishing §6.2, §5.5, §6.5b |
| B9 | Walt's text times: "07:40 after the first cast" and "09:15 after the first sale" vs the script's 09:30 cast and 14:30 sale | **09:35** and **14:35** | world §7.2 |
| B10 | Weather word "Cloudy" in the fish roster vs canon Clear/Fair/Breezy/Fog/Rain/Storm | **"Cloudy" = Fair or Breezy** (the importer maps it; CSV unchanged) | fishing §5.1 |
| B11 | Fishing's "In" column M1–M4 vs the systems roadmap M0–M12 | It is a **content wave**: M1 = systems M1; M2 = the demo (systems M4–M7); M3 = EA zones; M4 = EA Trophy pass | fishing §5.2 |
| B12 | Tackle pouch: fishing "6 types × 99" vs economy "+2 types → 4" | **Starts at 2 types, +2 per upgrade ($250), max 6** | fishing §6.1 |
| B13 | Landing net: fishing $80 vs economy $150; economy effect "no fish lost at the reveal splash" (no such loss exists) | **$150**; effect = fishing's co-op net (+1 retry, +10% green) | fishing §4.2; economy tackle table |
| B14 | Economy model comment "engine rebuild $250" | $150 | `economy_model.py` |
| B15 | Braided line effect "snap chance down" (snaps are no longer random) | "higher weight rating: fewer HEAVY fish" | economy tackle table |

`economy_model.py` was re-run after its text edits: `checks OK: {1: 16.8, 2: 13.5, 4: 10.8} hours; stage solo $/h [1300, 3550, 12700, 70500, 146000]`. Only descriptive text changed; all numbers are the same.

## C. Gaps found, NOT changed (flagged for the build or for Manan)

1. **Grade mix vs economy.** The relaxed bar lifts Stage 1's average grade from ~×1.8 to ~×1.95 (+8% per fish). Inside economy's ±10% tolerance; `avg_fish_worms` to be set from the F4 playtest log at M4 (noted in economy §2.3).
2. **`fishing_model.py` prints the old windows** (160/136/112/90/73 ms). Re-run it with the new class table at F4 (noted in fishing §3.7).
3. **Bite time windows vs lighting states**: fishing uses Dawn 05–08 / Day 08–17 / Dusk 17–20 / Night 20–05; systems' light uses Dawn 05:30–07:30 / Day / Golden 17:30–19:30 / Dusk 19:30–21 / Night 21–05:30. They are different things (fish behaviour vs light) but players will read "dusk fish" by the sky. Suggest aligning fish Dusk to 17:30–21:00 at F9.
4. **Deep jig**: economy says 40 reusable casts; fishing says $120 per 10, used up. Decide at M8 (not in the demo).
5. **Walt's Mix**: economy "crafted from shrimp + sardine, Act 2"; fishing "not sold, June makes 3 per letter step". Decide at F13.
6. **Chum**: economy sells a $20 chum bucket; fishing makes chum free from rotten fish. Both can exist; economy's table should mention the free one.
7. **Shop hours**: Hook, Line & Sinker 05:00–19:00 (economy) vs "shops 07:00–20:00" (world §11.1). Harmless; pick one at M4.
8. **Lines, reels and hooks** (fishing §6.3–6.5) have prices that economy's tackle table doesn't list. Add them to `economy_model.py` at M8.
9. **Map A** routes still cross the "Hale's Landing" label in the diagram; it's a diagram, not art, but the final in-game map needs its own layout pass.
10. **Storage** (dock ice chest / boat fish box) is still OPEN (systems Q2); workers (M6) need the answer.
11. **Demo date** (systems Q7): the June 2027 Next Fest leaves ~8 weeks of public demo against the market report's 2–4 months.

## D. Research used (all checked 2026-10-31)

- Dredge, Joel Mason on the minigame: "for it to not be frustrating, so we never wanted to make it too hard or twitchy": [premortem.games](https://premortem.games/2023/03/30/unique-blend-of-genres-in-haunting-fishing-adventure-dredge-by-black-salt-games)
- Stardew Valley's bobber bar complaints and the Full Fishing Bar mod: [Stardew forums](https://forums.stardewvalley.net/goto/post?id=19141), [CurseForge mod](https://www.curseforge.com/stardewvalley/mods/full-fishing-bar)
- Cast n Chill (low friction, idle mode, gentle progression; OpenCritic top-critic average 87): [OpenCritic](https://opencritic.com/game/18729/view/reviews), [Shacknews](https://www.shacknews.com/article/144913/cast-n-chill-review-score)
- WEBFISHING (social, chill hub; fishing is one of several small activities): [Wikipedia](https://en.wikipedia.org/wiki/Webfishing)
- How to Fish (co-op comedy, Overwhelmingly Positive, soloable campaign): [GamingOnLinux](https://gamingonlinux.com/2026/08/how-to-fish-is-another-brilliantly-funny-online-co-op-game)
- Co-op physics scope traps: LEGO Voyagers added online co-op late and said "starting that conversation earlier would have saved a lot of pain" ([Unity blog](https://unity.com/blog/light-brick-studio-lego-voyagers)); Worlds Adrift shut down as "no longer commercially viable" after networking/server tech problems ([PCGamesInsider](https://www.pcgamesinsider.biz/news/69078/bossas-ambitious-spatialos-powered-worlds-adrift-is-shutting-down-in-july/)). Our plan already does co-op at M2, right after the first fishing build, and uses peer-hosted NGO, no servers.

## Files touched
- `fishing/fishing.md` (36 marks)
- `world/world.md` (26 marks), `world/maps/make_maps.py` (map A function), `world/maps/map-A.svg`, `world/maps/map-A.png`
- `economy/economy.md` (9 marks), `economy/economy_model.py` (descriptive strings only), regenerated `economy.xlsx` and charts
- `systems/systems.md` (3 marks), `systems/mockups/05-minigame.html`, `systems/mockups/05-minigame.png`


---

# E. Manan's plan changes, 31 Oct 2026 (evening)

Source: `MANAN-CHANGES-1031.md` (his words + Kufu's reading). Worker pb-plan-changes applied them on 31 Oct. Every edit in the four section files is marked **"Manan 31 Oct"**. Pre-change copies of every touched file are in the worker's scratchpad (`C:\Users\asus\AppData\Local\Temp\claude\C--Users-asus-Desktop-Port-Boss-plan\1d4c3648-ba46-4179-8f3e-15f8940e181a\scratchpad\before-1031\`, plus `fish-roster-39-before-1031.csv`), not in this folder.

## E1. The 9 changes, applied

| # | Manan's change | What changed | Where |
|---|---|---|---|
| 1 | Discuss inventory and fish types | One-page proposal written; **Manan approved all 5 picks** (via Kufu): warehouse slot = 10 fish; Walt's Mix sold at June's once you own Old Faithful, $40 each; Old Brass first catch = story moment, then it can bite again rarely; time/weather = ×1.5 nudge, never a lock; the 5-rod ladder as proposed. The 15 fish are final for the build | `DISCUSS-inventory-and-fish.md` |
| 2 | Warehouse from the start, expandable | **Walt's warehouse at our dock from Day 1**, 5 tiers: Old Shed (free, 12 slots) → Patched $300 (24) → Cold Room $2,500 (+12 cold, rot 25%) → Big Warehouse $8,000 (48 + 24) → Ice House $30,000 (96 + 48, rot 10%). A slot holds 10 fish. It replaces the wooden/iron/ice chests, the insulated satchel and the boat live well. Workers' catch goes there | systems §1.1.6 (rewritten), §0, H9, M5, M6, Q2; economy dock-shop Storage rows, S1 spend (Wooden chest $150 → Warehouse T1 $300), workers; fishing §5.6, §6.6, §7.4, Q1 |
| 3 ★ | Boat and office customisation happen automatically | Reading applied: **you choose and pay; it installs itself in its set spot** (Doyle's crew paints the boat; furniture and warehouse tiers appear in their one home). No placement tool, no paint/edit mode | economy §4.3 paint bay, §4.4 build rule; systems M5 scope/tests/checklist; world §4.3 |
| 4 | No designed clock; time on the forecast | **HUD clock card removed.** Notifications move up to the top-left (x 40, y 32). Time, day and forecast live on the phone (Home + Forecast). HUD mockup edited and re-rendered | systems §0, §1.2.1, §2.3 type scale, §2.4, §2.5 (diagram + table + HUD modes), §2.6, §2.7, M1, M3, Q9; `systems/mockups/01-hud.html` + `.png` |
| 5 | Legendary fish need the best rod AND best bait | Old Brass bites only with **Old Faithful + Walt's Mix** (5% a cast the first time, ×1.5 at dusk, ×1.5 in rain; 1% a cast after the first catch). Asserted in the model | fishing R13, §5.3, §5.5, §6.1, §6.2, F13; CSV; `fishing_model.py` |
| 6 | 15 fish, 3 per rod | **New roster: 15 fish** from the old 39, same prices: Bamboo (sprat, mullet, mackerel) · Fibreglass (garfish, bream, conger) · Carbon (snapper, mahi, grouper) · Deep-sea (sunfish, blue shark, swordfish) · Old Faithful (anglerfish, oarfish, Old Brass). **5 baits**, one new per rod: worms, shrimp, sardine, squid, Walt's Mix. Spinner, glow lure and deep jig cut. Bait pouch starts with 3 types, one $250 upgrade holds all 5 | `fishing/fish-roster.csv` (rewritten); fishing R11, §5.2, §5.3, §6.1, §6.2, §6.6, §7.5, §7.6, F5/F6/F9/F12/F13; economy bait + tackle tables, §5.1; systems hotbar, Journal, demo/EA table |
| 7 | Map B | **Map B is the chosen map.** (This pass first redrew it in a new outline from a misreading of Manan's note; reverted to the original Crescent Bay in §F.) Map A is kept as "first draft's pick, not chosen" | `world/maps/make_maps.py` (`map_b()`), `map-B.svg/.png`; world §0.2 D5, Q7, §2.3 note, §8 (new §8.0), §8.2, §13 |
| 8 | Any fish anywhere, no sections | **Fishing zones removed.** The rod is the hard gate, bait steers, time/weather nudge ×1.5. Places stay (buyers, wreck, surf, hotspots). Economy's "zone stock" anti-grind replaced by saturation + thinning hotspots | fishing R12, §5.1 (rewritten), §5.3, §6.7, §7.4; economy §0, §5.1, §5.4, §9, §10, §11; systems Crew/Map apps, M6, demo table; world §4, §7, §8, §9, §11 |
| 9 | Mari on Day 3 or at $250 | **Mari texts on Day 3 at 07:00, or the moment cash reaches $250, whichever is first.** `economy_model.py` gained a first-days cash sim (`first_days()`, two players: first-timer $13/fish, practised $16/fish) that writes the §2.3 table; S1 mooring $/h now follows Mari's arrival; new asserts (Mari by Day 3; engine by minute 35) | economy §0, §2.2, §2.3 (generated), §3.3, §5.2, vendors table; world §4.2, §7.2 Mari thread, §9.1/§9.2 script |

## E2. Model re-runs

- `fishing/fishing_model.py` (rewritten for 15 fish / 5 rods / no zones; asserts: bite 4–10 s on every rod, rot ≥ 48 h, 15 fish, 3 per rod, 1 legend only with Old Faithful + Walt's Mix, every fish can bite at every time × weather × bait): **OK: all MUST asserts passed**. Output in `fishing/model-output.txt`. It also prints the class windows for every rod (fixes gap C2 above: the old model printed the pre-review windows).
- `economy/economy_model.py`: **checks OK: {1: 17.2, 2: 13.8, 4: 10.9} hours; stage solo $/h [1118, 3550, 12700, 70500, 146000]** (was 16.8 / 13.5 / 10.8; S1 $1,300 → $1,118). Regenerated `economy.xlsx`, `money-over-time.png`, `income-mix.png` and every table block in `economy.md`.

## E3. ★ Two readings to confirm with Manan (flagged, not decided)

1. **"For boat and office customisation… automatically, don't give permission to the player."** Applied reading: upgrades and paint install themselves in set places; no editor, no placing. Other reading: in co-op, friends don't need the host's permission to customise. If Manan meant the second, revert the M5 "set spot" wording and keep the old placement grid.
2. ~~Map shape~~: **settled in §F** (Manan: "option B was okay"; map B stays the Crescent Bay).

## E4. New issues the re-runs found (flagged, NOT changed)

1. **The first upgrade comes later.** On the bamboo rod only sprat ($6), mullet ($8) and mackerel ($10) bite: about $13–16 a fish, not ~$23. With no 100% buyer before Mari, the $150 engine is now bought at **real minute 23 (practised) / 31 (first-timer)**, not minute 9. Cheap fixes for Manan/Kufu to pick: (a) Doyle's engine rebuild $150 → **$90** ("parts only, for Walt"); (b) June buys Day 1's catch at 100% ("Walt's account"); (c) accept it, because a Day-2 engine gives Day 1 a clear goal.
2. **The "$250" path almost never fires first.** Both simulated players reach Day 3 before $250 in cash, because buying the engine keeps cash under $250 (the first-timer peaks at $249). If Manan means "has *earned* $250 in total", both players get Mari on Day 2 at ~15:00. One-line change in `first_days()`.
3. ~~Voss distance on the redrawn map~~: superseded by §F (Crescent Bay: Voss 12.4 min round the south horn, far on purpose; 8.0 via the Cut).
4. ~~Lantern Key distance~~: superseded by §F (Crescent Bay: 4.6 min).
5. **Silverbill is cut** with the other 3 legends, but it was the Act 4 rival beat (Voss's fake marlin). The rival section needs another proof of Voss's lie (world §4.5 marked).
6. **Crab and lobster pots are parked**: "15 fish" reads as the whole catch list, so shellfish would be extra. Manan to say if pots come back (fishing §6.7).
7. **Not re-rendered:** `systems/mockups/03-phone.png` (its Map screen still shows map A and the old fishing zones) and `world/maps/map-A/C` (left as alternatives). Redraw the phone Map at M4 from map B.
8. **Hand-fishing income fell in Stage 1** ($1,100 → $1,000/h budget), so S1 takes ~2.1 solo hours (was 1.8). Still inside every target; noted for the F4 playtest calibration.

## E5. Files touched (this pass)
- `DISCUSS-inventory-and-fish.md` (new; approved)
- `fishing/fishing.md` (38 marks), `fishing/fish-roster.csv` (rewritten, 15 fish), `fishing/fishing_model.py` (rewritten), `fishing/model-output.txt`
- `economy/economy.md` (34 marks + generated tables), `economy/economy_model.py` (first-days sim, data rows), `economy/economy.xlsx`, `economy/money-over-time.png`, `economy/income-mix.png`
- `systems/systems.md` (25 marks), `systems/mockups/01-hud.html`, `systems/mockups/01-hud.png`
- `world/world.md` (33 marks), `world/maps/make_maps.py` (map B), `world/maps/map-B.svg`, `world/maps/map-B.png` (map A and C files were re-rendered by the script unchanged)
- `CHANGES.md` (this section)


---

# F. Map clarified + Kufu's picks taken (Manan, 31 Oct, later)

Manan: "option A was looking like a [rude shape]; option B was okay." He did not want a reshaped map: **map B stays the original Crescent Bay.** He also took Kufu's picks on the §E4 issues. Edits marked "Manan 31 Oct".

## F1. What changed

| # | Change | Where |
|---|---|---|
| F1 | **Map B redrawn back to the Crescent Bay**: a crescent island around a calm bay that opens west. Hale's Landing is deep inside the bay with Old Saltmouth behind it; Voss Harbour is on the outer east coast behind Saltback Ridge. Kept from §E: the "Any fish, anywhere" label (no zones), dock + P1–P3 expansion on the bay shore, Dot's lighthouse (north horn tip), Wreck of the Margaret (off the north horn), Gullrock (in the bay), Lantern Key, Sorrel Isle, Mango Cay, surf breaks (Breaker Point on the south horn, a north-horn point break, Voss Wall). Title "B · Crescent Bay (CHOSEN)". The earlier reshaped outline and its code are gone | `world/maps/make_maps.py` (`map_b()`), `map-B.svg`, `map-B.png`; world §0.2 D5, Q7, §2.3 note, §8.0 (rewritten), §8.2, §8.4, §13 |
| F2 | **Walt's Cut moved** to a silted channel **through the narrow south horn**, from the bay just south of town to the outer south coast. Dredged, it takes Voss from 12.4 to **8.0 min** in Biscuit and lets ships reach the bay without rounding the horn, so the endgame goal survives | map B; world §8.0, §8.4 |
| F3 | **Doyle's engine rebuild: $150 → $90** ("Parts only. For Walt.") | `economy_model.py` (S1 spend, first-days wishlist), economy §0, §2.3, §3.5; world §4.2, §7.2 Fergus line, §9.1, §9.2 |
| F4 | **Mari: Day 3 at 07:00, or when TOTAL EARNED reaches $250**, whichever is first (was "cash reaches $250"). The sim now tracks total earned; S1 mooring $/h is derived from her simulated arrival day | `economy_model.py` (`first_days()`, `MARI_EARNED`), economy §0, §2.2, §2.3, §3.3, §5.2, vendors table; world §4.2, §7.2 thread, §9.1, §9.2; fishing §7.5 |
| F5 | **Silverbill's rival beat replaced by "Walt's Hook"** (SUGGESTED): the marlin in Voss's office ("Caught by A. Voss, 1981") still carries Walt's brass hook stamped W.H. · MARGARET. From Act 3, sail past at night and take an evidence photo with the phone Camera; with Walt's letter 10 it goes to Gull FM ("The fish Voss only borrowed"): reputation up (Charter allies +1), Heat up a tier. No new fish needed | world §4.5 beat 5; fishing §5.5 note |
| F6 | **Crab and lobster pots stay out**; **customisation = auto-install, no editor** is confirmed (the ★ reading in §E3.1 now stands) | no further edits (already applied) |
| F7 | "Hook" map wording removed from world.md, CHANGES.md §E, DISCUSS page and the map script | as listed |

## F2. New key numbers (models re-run)

- **First engine:** bought on **Day 1 at 16:00, real minute 9**, by both the first-timer ($5 left) and the practised player ($21 left). The sim keeps $5 after an upgrade (the visit's bait is already paid). New assert: first upgrade by minute 10.
- **Mari's arrival:** **Day 2 at 15:00, real minute ~30**, for both players (total earned $280 first-timer / $344 practised); first rent Day 3 07:00. Assert: never later than Day 3 07:00.
- **End of the simulated first days** (Day 3 18:00, minute 53): $253 first-timer, $373 practised.
- `economy_model.py`: **checks OK: {1: 17.1, 2: 13.7, 4: 10.9} hours; stage solo $/h [1146, 3550, 12700, 70500, 146000]** (S1 mooring ~$66/h; S1 ~2.0 solo h). Regenerated `economy.xlsx`, charts and every table block.
- `fishing_model.py`: **OK: all MUST asserts passed** (unchanged roster).
- **Map B travel table** (from our dock; T1 Biscuit 6 m/s / T2 9 / T3 11 / T4 16, min):

| To | Distance | T1 | T2 | T3 | T4 |
|---|---|---|---|---|---|
| Bay Shallows | 0.3 km | 0.8 | 0.5 | 0.4 | 0.3 |
| Gullrock (in the bay) | 0.4 km | 1.0 | 0.7 | 0.6 | 0.4 |
| Lantern Key (The Pelican) | 1.7 km | 4.6 | 3.1 | 2.5 | 1.7 |
| Wreck of the Margaret | 1.9 km | 5.2 | 3.4 | 2.8 | 1.9 |
| Breaker Point surf (by boat) | 2.7 km | 7.5 | 5.0 | 4.1 | 2.8 |
| Sorrel Isle | 4.2 km | 11.7 | 7.8 | 6.4 | 4.4 |
| Mango Cay | 4.3 km | 12.0 | 8.0 | 6.5 | 4.5 |
| Voss Harbour, round the south horn | 4.5 km | 12.4 | 8.3 | 6.8 | 4.7 |
| Voss Harbour via Walt's Cut (dredged) | 2.9 km | 8.0 | 5.3 | 4.3 | 3.0 |

Lantern Key is 1.7 km (4.6 min), not the first draft's 1.4 km: the route now bends round Gullrock instead of crossing the reef.

## F3. Still open (unchanged)
- `systems/mockups/03-phone.png`'s Map screen still shows map A and the old zones; redraw from map B at M4.
- The rest of §C and §E4 items 5–8 as written (Walt's Hook is a suggestion for the writer to refine).
