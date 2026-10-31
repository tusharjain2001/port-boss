# Port Boss: Economy + Progression (GDD section)

Worker: gdd-economy, for Kufu task t-e8c3db82, 2026-10-31. Plan GO bcad4228.

Tags used everywhere: **MUST** = Manan's own words. **SUGGESTED** = ours, to tune or cut. **OPEN QUESTION** = Manan decides.

All numbers come from one source, `economy_model.py`. It writes `economy.xlsx` (live formulas: change a blue input and every hour and total recalculates), both charts, and every table between `<!--T:...-->` markers in this file. The script ends with self-checks (`python economy_model.py` prints `checks OK`). If you change a number, change it in the script, never by hand here.

Kept consistent with:
- `gdd/fishing` (fish prices, grades ×1.0/1.3/1.7/2.5, the 20 s cycle, freshness bands, bait prices);
- `gdd/world` (Saltmouth Island, Hale's Landing, the shop NPCs, acts and days, the rival Heat system R1–R14, moorers, Teddy Mango);
- `gdd/systems` (24 real min per in-game day, sleep skips to 07:00, one shared company bank, order size ×1.0/1.6/2.1/2.5, hotbar 8 slots with fish stacks of 5).

---

## 0. One-page summary

- **Money:** whole dollars. Start with **$40**, June's free bag of worms, Walt's damaged skiff **Biscuit** and his bamboo rod.
- **First days:** first sale at minute ~8 (Pratt's insulting 60%). **Mari asks to moor on Day 3 at 07:00, or the moment the player's TOTAL EARNED reaches $250, whichever is first** (**MUST first income**, *(Manan 31 Oct)*); she pays from the next morning. In the re-run both a first-timer and a practised player pass $250 earned on **Day 2 at 15:00 (real minute ~30)** (§2.3). First purchase: **Biscuit's engine rebuild, $90** *(Manan 31 Oct)*, at **minute ~9** for both. After that, a purchase every 10–15 minutes.
- **Five stages = gdd/world's five acts.** Solo **~16.8 h** to the Charter vote, 2 players **~13.5 h**, 4 players **~10.8 h**, then Endless.

| Stage | Solo net $/h | Earned by the end |
|---|---|---|
| S1 Fisherman | $1.3k | ~$2.4k |
| S2 Small trader | $3.6k | ~$14k |
| S3 Dock owner | $12.7k | ~$56k |
| S4 Port owner | $70k | ~$400k |
| S5 Rival endgame | $146k | ~$930k |

- **Income mix shifts by stage.** Hand fishing is 85% of S1 income and ~3% of S5. Workers (the **MUST first automation**, Act 2) carry S3. Ship orders and consignments carry S4–S5. Hand play stays worth doing because a job done by a player pays **+30%** over automation, and Trophy/Legend fish, the Derby and charters are player-only.
- **Co-op: one shared company bank, decided.** It matches gdd/systems. No personal wallets. A host setting controls who can spend (default: ask the crew above $5,000 or 25% of the balance), and every dollar is in the phone's Ledger. Prices never scale with players; only order and ship sizes do.
- **Anti-grind:** next goal always on the phone with "need $X more, about N min". Every income purchase pays back within the next stage. A market drops 2% per same-species fish per day, so variety pays. Hotspots (fish signs) thin out as they're fished; there are no zones *(Manan 31 Oct)*. No game over; no offline earnings.
- **Anti-cheese:** the host's save is the only truth. Inventories live in the world save, keyed by player, never carried between worlds. Every day's offers are seeded from (world seed, day), so reloading can't reroll them. Every transaction carries an id, so it can't be applied twice.

**Open questions for Manan (batched, §12):**
1. Boat fish storage (a fish box or cooler with slots, never crates)?
2. Do guns cost money, and what are they for?
3. Is a main path of ~17 h solo / ~13.5 h co-op, then Endless, the right length?
4. Bank loans?
5. Is the cartel's cargo named as drugs? (Shared with gdd/world Q4.)

---

## 1. Research: what the hits do with money and upgrades (checked live 2026-10-31)

| Game | What it does | What we take |
|---|---|---|
| **Dredge** ([wiki: Boat](https://dredge.wiki.gg/wiki/Boat), [StrategyWiki: Fish](https://strategywiki.org/wiki/DREDGE/Fish), [Pro Game Guides](https://progameguides.com/dredge/dredge-all-boat-upgrades-and-how-to-get-them/)) | Two upgrade tracks. **Hull tiers** at the Dry Dock add cargo, rod, net and engine slots (cargo 36 → 41 → 54 → 73 → 88). **Research parts**, found by exploring, unlock rods, engines and nets, which are then bought with cash. Fish start to spoil after 12 in-game hours, sell at 35% while rotting, and become worthless Rot 12 h later. Engine upgrades are the first thing guides tell you to buy. | Upgrades you can **feel** (speed, slots, a bigger green zone). One tier = a new boat you can walk around. Rot as a gentle clock, not a punishment (our 48 h, gdd/fishing). Speed first: our first purchase is Biscuit's engine. |
| **Stardew Valley** ([Multiplayer wiki](https://stardewvalleywiki.com/Multiplayer), [1.4 shared/separate money](https://co-optimus.com/article/16038/next-stardew-valley-update-will-allow-separate-cash-in-co-op.html), [forum on daily income](https://forums.stardewvalley.net/threads/how-much-money-do-you-make-each-day-on-average.8498/latest)) | Income spans orders of magnitude (thousands a day in year 1, 50–60k a day late) with no wall, because new multipliers (kegs, artisan goods) keep arriving. Co-op money is shared by default, and separate wallets were added in 1.4 as an option chosen at farm creation. The "tomorrow" pull ends every day. | Each stage adds a new **multiplier** (workers, then ships, then consignments), not only bigger prices. The night summary always shows the next goal. Shared money by default. |
| **Lethal Company** ([Profit Quota wiki](https://lethal-company.fandom.com/wiki/Profit_Quota), [Configurable Quota docs](https://thunderstore.io/c/lethal-company/p/seechela/Configurable_Quota/v/1.5.0/)) | The quota starts at 130 and rises each cycle by 100 × (1 + n²/16) × a random factor, so it grows faster each time. A clear, faceless target with a deadline drives every run. | Short, visible targets work, but we don't punish. Orders and ship windows give deadlines, and failing one costs that order, never the save (gdd/world: no game over). |
| **Schedule I** ([Prima: levels](https://primagames.com/tips/what-does-each-level-unlock-in-schedule-1), [Dexerto: workers](https://www.dexerto.com/gaming/how-to-hire-assign-workers-in-schedule-1-3171655), [Prima: employees](https://primagames.com/tips/how-to-hire-employees-in-schedule-1)) | Small-time to kingpin. Ranks unlock regions, and dealers have a buy-in. Workers (botanist, chemist, packager, cleaner) are hired at a fixer, paid a daily wage, and need beds. Automation is the mid-game payoff, and the phone is the business hub. | The phone runs the business: Crew app, Contracts, Ledger. Workers have a daily wage and visible, imperfect output. Our cartel line is a separate, riskier economy. Simpler than Schedule I: no beds needed for workers. |
| **Supermarket Simulator** ([Steam discussion: licences and levels](https://steamcommunity.com/app/2670630/discussions/0/4354495356172081436), [Steam: late expansions](https://steamcommunity.com/app/2670630/discussions/0/600771501980966947), [employees guide](https://earlyguides.com/supermarket-simulator)) | Store level gates licences more than cash does, and players say "the level requirement takes longer". Every new product licence raises daily profit. Late expansions get very expensive (300 h players with expansions left). Cashiers and restockers take a flat daily wage. | **Avoid** that late grind wall. Our gates are story goals plus money, never an XP grind. Cap the length (~17 h solo main path), then Endless. A new licence means a new income type in every stage. |
| **TCG Card Shop Simulator** ([progression guide](https://www.whisperofthehouse.com/tcg-card-shop-simulator/progression-guide), [Prima: workers](https://primagames.com/tips/how-to-level-up-your-shop-fast-in-tcg-card-shop-simulator)) | Expansions are bought in sequence and cost more each time (Shop Lot B $5,000). Level plus a purchase unlock licences. Workers hire from shop level ~10, with a fee, and start the next day. The pack-opening gamble is the hook. | Hired staff start **the next morning** (the overnight build/hire beat). Our "gamble" is the minigame grade (Prized ×2.5) and the rare fish reveal, not a loot box. |
| **PowerWash Simulator** ([wiki: Shop](https://powerwashsimulator.wiki.gg/wiki/Shop), [Pro Game Guides](https://progameguides.com/powerwash-simulator/all-power-washers-and-attachments-in-powerwash-simulator/)) | Everything in the shop costs $26,315, and the whole career earns $23,385. Players can't buy everything from the main path, so replays pay 25%. Washers unlock by **stars** plus cash (Urban X U2 at 15 stars, $900). No time pressure. | Total main-path income must cover **all MUST items plus most SUGGESTED ones, but not every cosmetic**, so Endless still has things to buy. Replay income (Endless) pays less per hour than the story (no story bonuses). |
| **Sea of Thieves** ([wiki: Trading Companies](https://seaofthieves.wiki.gg/wiki/Trading_Companies), [Fandom](https://seaofthieves.fandom.com/wiki/Trading_Companies)) | Gold and reputation buy **cosmetics only**: "all customisation options are purely cosmetic". A crew shares a ship and the voyage. | Clothes, boat paint, horns and flags are cosmetic only (Manan: clothes-only edits, art = Ponyo Coast). Cosmetics are the long-tail sink after the finale. |
| **Moonlighter** ([PC Gamer](https://www.pcgamer.com/uk/in-moonlighter-youre-part-adventurer-part-shopkeeper), [pricing write-up](https://willem-icfe-en.beehiiv.com/p/pricing-in-moonlighter), [Steam thread](https://steamcommunity.com/app/606150/discussions/0/1743355067126276812/)) | You set prices and read customer faces. Demand shifts the ceiling. Some players find the trial and error tedious. | We don't make players set prices. Each buyer shows a **clear price** with a demand arrow (saturation). We keep the "read the market" feeling and drop the busywork. |
| **How to Fish** (2026 comp, [GamingOnLinux review](https://gamingonlinux.com/2026/08/how-to-fish-is-another-brilliantly-funny-online-co-op-game), [Niche Gamer: 1M in 2 days](https://nichegamer.com/how-to-fish-attracts-one-million-players-in-two-days/), [beginner guide](https://www.player.one/how-fish-beginner-guide-how-make-money-upgrade-gear-unlock-new-islands-163937)) | Catch, sell, upgrade the rod and gear, and reach new islands. Fish gambling (not real money) is what players get "hopelessly addicted" to. | Short loop: catch → sell → a visible upgrade within minutes. Our optional risk-for-reward is the cartel line and the Derby, never real-money gambling. |
| **Idle/incremental cost maths** ([Game Developer: Math of Idle Games](https://www.gamedeveloper.com/design/the-math-of-idle-games-part-i), [Idle Idol balancing](https://gamedeveloper.com/design/balancing-tips-how-we-managed-math-on-idle-idol)) | Costs grow exponentially (often ×1.07–1.15 per purchase) while output grows slower. Idle Idol's ×1.1 cost overtook reward at about level 35 and forced a rebalance. | We don't use repeating ×1.15 purchases. Each stage is a handful of hand-placed purchases. Stage income grows ×2.7–5.6 per stage (the check enforces under ×6), so there's no cliff. |

**What makes upgrades feel great (rules we keep):**
1. **You see it and feel it at once.** Faster wake and engine pitch, a wider green zone, a new walkable boat, Ingrid's crew building overnight with a timelapse at 07:00.
2. **The next goal is always visible.** It's on the phone home screen and at the shop: "Kestrel: need $1,240 more (~40 min)".
3. **Small every 10–15 min, big every 60–90 min.** S1 has 12 small purchases in ~1.8 h. Each stage ends on a big one (an office tier, a boat, a pier).
4. **New verbs, not just bigger numbers.** Every stage adds one new way to earn (moorers, workers, ships, consignments).
5. **Payback is quick and shown.** The shop card says "pays for itself in about 2 h at your current rate".

**Money curves that avoid grind:** income rises in steps that match the cost of the stage's goal (the chart in §3). No stage takes more than ~5 h solo. Nothing needs repeating the same action past saturation (§10).

---

## 2. Money basics

### 2.1 Assumptions (from the model)
<!--T:assumptions-->
| Assumption | Value | Source / why |
|---|---|---|
| real_min_per_game_day | 24 | gdd/world + gdd/systems: 1 in-game hour = 1 real minute |
| days_per_real_hour | 2.8 | sleep skips the night; matches gdd/world's act day ranges (Day 40 at ~14 h, 2 players) |
| start_cash | 40 | plus June's free bag of worms (gdd/world prologue), Biscuit (damaged), Walt's bamboo rod |
| fish_per_hour_hand | 70 | gdd/fishing: ~20 s cycle, 60-80 landed per real hour incl. travel and selling |
| avg_fish_worms | 16 | gdd/fishing model (15-fish roster, Manan 31 Oct): Walt's bamboo + worms ~$16/fish practised, ~$13 first-timer |
| blended_sell_factor | 0.75 | S1 mix: Pratt pays 60% for about half the catch; Mari, June, Amara and orders pay 100-175% |
| pratt_factor | 0.6 | Old Saltmouth Fish Market (Voss-owned, Gideon Pratt) pays 40% under fair (gdd/world) |
| order_factor_pelican | 1.75 | The Pelican (chef Amara Bell) orders pay 150-200% of base |
| cannery_factor | 0.5 | Sorrel Isle cannery (Act 3): 50% of base, takes any amount, no saturation |
| saturation_step | 0.02 | each same-species fish sold at one buyer in one day: -2% |
| saturation_floor | 0.6 | saturation never drops a price below 60%; resets at 07:00 |
| fuel_price | 1.5 | $ per litre at Doyle's fuel dock; our own pump buys at 0.90 |
| upkeep_rate_per_day | 0.002 | dock + fleet upkeep: 0.2% of built value per day, charged at 07:00 |
<!--/T-->

### 2.2 Who buys fish, and for how much (links gdd/fishing)
Fish base prices, grades and freshness are **gdd/fishing's** (`gdd/fishing/fish-roster.csv`, **15 species: 3 per rod, any fish anywhere** *(Manan 31 Oct)*). Price paid = base × grade (Common ×1.0 / Fine ×1.3 / Rare ×1.7 / Prized ×2.5) × freshness (Fresh 100%, Good 85%, Stale 60%, Old 35%, Rotten unsellable) × **buyer factor** × saturation.

| Buyer | Where | Hours | Factor | Limit | Unlock | Tag |
|---|---|---|---|---|---|---|
| Old Saltmouth Fish Market (Gideon Pratt, Voss-owned) | town, 1 min walk | 05:00–16:00 | **60%** | none (saturation applies) | Day 1 | SUGGESTED (gdd/world: "the only buyer in town") |
| Mari's Tacos | at our dock | 09:00–22:00 | 100% | 10 fish/day, Everyday species only | Day 3, or at $250 total earned *(Manan 31 Oct)* | SUGGESTED |
| The Pelican walk-in (Amara Bell) | Lantern Key, 3.5 min in Biscuit | 10:00–22:00 | 110% | 15 fish/day, Fine or better | after her first order | SUGGESTED |
| The Pelican orders | phone | – | 150–200% | 1–2 orders/day | Day 2 | MUST (orders) |
| Gull's Nest Café (Bao Tran) orders | town | 04:00–15:00 | 130% | small orders, 1/day | Act 2 | SUGGESTED (gdd/world) |
| The Saltmouth Arms hotel orders | town high street | – | 130% | 20–60 fish in 2–3 days | Act 3 | SUGGESTED (new; name to confirm with gdd/world) |
| Sorrel Isle cannery | Sorrel Isle, 8.6 min | 06:00–18:00 weekdays | 50% | **any amount, no saturation** | Act 3 | SUGGESTED (gdd/world) |
| Nell's Curios | post office, town | 10:00–18:00 | fixed | collectibles only, $20–2,000 each; set bonuses | Day 1 | SUGGESTED (gdd/world) |

**Saturation (SUGGESTED, anti-grind):** every fish of the same species sold to the same buyer on the same day lowers that buyer's price for it by 2%, down to a floor of 60%. It resets at 07:00. The sell screen shows a small arrow and "−8% today" on that line. The cannery ignores saturation, which makes it the dump for a big haul.

**What the player sees:** at any buyer, the sell panel lists the hotbar's fish stacks with the price for each, already counting grade, freshness and saturation. "Sell all" asks for confirmation if anything in the hotbar is a Trophy. Ka-ching, then a left-side toast: "+$184 · Pratt (60%)". A small grey line under Pratt's price says "Mari pays 100%" when that's true, so players learn the market by selling, not by reading a manual.

### 2.3 The first days, solo (MUST: "earn, then upgrade"; Mari rule *Manan 31 Oct*)
Clock: Day 1 starts 07:00. 1 in-game hour = 1 real minute. **Mari arrives on Day 3 at 07:00, or the moment the player's TOTAL EARNED reaches $250, whichever is first (MUST, Manan 31 Oct; "total earned" confirmed after the first re-run).** She pays $40 from the next morning and buys up to 10 fish a day at 100% from 09:00. This table is generated by `economy_model.py` (`first_days()`), which plays the same script twice: a first-timer (novice grades, ~$13 a fish on Walt's bamboo rod + worms) and a practised player (~$16), both with the 15-fish roster (gdd/fishing). The script buys, in order, when at least $5 stays in cash (the visit's bait is already paid): **the $90 engine rebuild** *(Manan 31 Oct)*, Ingrid's $100 planks, the $120 hull patch.

<!--T:first_days-->
| Real min | In-game | What happens | First-timer ($13/fish): cash · result | Practised ($16/fish): cash · result |
|---|---|---|---|---|
| 0 | Day 1 07:00 | Wake in Walt's office. Letter, key, journal. June's free bag of worms (10) | **$40** | **$40** |
| 3 | Day 1 10:00–14:00 | Trip 1, the Shallows: 10 casts → 9 fish (sprat, mullet, mackerel) | **$40** | **$40** |
| 8 | Day 1 15:00 | First sale. Pratt (60%): "$6 for a $10 mackerel. Take it or watch it rot." | **$110** · (+$70) | **$126** · (+$86) |
| 9 | Day 1 16:00 | June: 3 packs of worms ($15). Doyle's slipway | **$5** · bought Biscuit engine rebuild $90 | **$21** · bought Biscuit engine rebuild $90 |
| 10 | Day 1 17:00–23:00 | Trip 2 at dusk: 15 casts → ~14 fish, kept overnight (Pratt shut at 16:00) | **$-2** | **$13** |
| 21 | Day 2 07:00 | Sleep, wake Day 2 (bed broken: no Rested buff) | **$-2** | **$13** |
| 22 | Day 2 07:30 | Sell last night's fish (Good, 85%). Amara's first order arrives: 3 mackerel ★★+, ~$90, by Day 3 evening | **$90** · (+$93) | **$128** · (+$114) |
| 23 | Day 2 08:00 | Shops: worms + shrimp ($40) | **$50** · nothing affordable yet | **$88** · nothing affordable yet |
| 24 | Day 2 09:00–15:00 | Trip 3: 16 casts → ~15 fish | **$42** | **$80** |
| 30 | Day 2 15:00 | Sell | **$159** · (+$117) **Mari texts (total earned reached $250):** "Can I moor at your place? $40 a day + tacos." Accept: she ties up at B1 | **$224** · (+$144) **Mari texts (total earned reached $250):** "Can I moor at your place? $40 a day + tacos." Accept: she ties up at B1 |
| 31 | Day 2 16:00 | Shops | **$59** · bought Ingrid's three planks + bench $100 | **$124** · bought Ingrid's three planks + bench $100 |
| 32 | Day 2 17:00–23:00 | Trip 4: ~14 fish, kept | **$52** | **$116** |
| 42 | Day 3 07:00 | Wake Day 3 | **$92** · Mari pays **$40** ("💸 Rent's in, landlord") | **$156** · Mari pays **$40** ("💸 Rent's in, landlord") |
| 44 | Day 3 09:00 | Sell last night's fish | **$229** · (+$137) | **$325** · (+$169) |
| 44 | Day 3 09:30 | Shops: worms + shrimp ($40) | **$69** · bought Biscuit hull patch $120 | **$165** · bought Biscuit hull patch $120 |
| 45 | Day 3 10:00–15:00 | Trip 5: ~13 fish incl. Amara's 3 mackerel | **$62** | **$158** |
| 50 | Day 3 15:00 | Sell | **$163** · (+$101) | **$283** · (+$125) |
| 51 | Day 3 16:00 | Shops | **$163** | **$283** |
| 53 | Day 3 18:00 | Deliver Amara's order (3 mackerel, ~$90) | **$253** | **$373** |
<!--/T-->

**What the re-run shows (Manan 31 Oct):**
- **Mari comes on Day 2 at 15:00 (real minute ~30) for both players**, when total earned passes $250 ($280 first-timer, $344 practised). Her first rent lands on Day 3 at 07:00. (With the first reading, "$250 in cash", both players got her only on Day 3, because buying upgrades kept cash under $250.)
- **The first upgrade is back at minute 9.** The 15-fish roster pays only ~$13–16 a fish on the starter rod, but the engine rebuild is now **$90** *(Manan 31 Oct)* (Fergus: "Parts only. For Walt."), so both players buy it on Day 1 at 16:00 right after Pratt's lowball sale (first-timer left with $5, practised with $21).
- **Stage 1 money:** hand fishing budgeted at **$1,000/h** (was $1,100: ~$700–800/h on the bamboo rod, then ~$1,400–1,900/h once the Fibreglass rod and shrimp arrive); mooring in S1 **~$66/h** (was $120: Mari pays on Days 3 and 4 of the 4-day Act 1; derived from the sim). S1 takes ~2.0 solo hours (was 1.8); the whole game **17.1 solo hours** (2P 13.7, 4P 10.9), inside every target.

> *(Kufu review 31 Oct; superseded in part by the Manan 31 Oct re-run above: the engine is now $90, still bought on Day 1 at 16:00; Mari comes on Day 2 at 15:00)* gdd/world's 60-minute script now follows this table (Mari $40, Pratt $6 per $10 mackerel, the $150 engine bought on Day 1 at 16:00, the $120 hull patch on Day 2). The relaxed minigame (gdd/fishing §3.7) lifts the average grade multiplier from ~×1.8 to ~×1.95 in Stage 1 (about +8% per fish): inside the model's ±10% tolerance, so no table changes now; set `avg_fish_worms` from the F4 playtest log at M4. Rods now also shorten the bite wait (4–10 s → 4–6 s), worth up to ~+12% catches per hour on Old Faithful; the late-game hand-fishing $/h rises a little, which keeps fishing by hand worth doing next to workers.

---

## 3. The progression curve

### 3.1 Stages, goals and money targets (MUST stages: fisherman → small trader → dock owner → port owner → rival endgame)
Each stage is one gdd/world act. A stage ends when its **goal** is done (a story beat plus the key purchase), never on a timer. "Key spend" is what the hours are computed from. Optional extras come out of the same income and add a little time.
<!--T:stages-->
| Stage | gdd/world act | World's 2P hours | Goal to leave it | Key spend | Solo net $/h | Solo hours | Solo in-game days | 2 players | 4 players | Total earned by stage end |
|---|---|---|---|---|---|---|---|---|---|---|
| S1 Fisherman | Prologue + Act 1 The Mooring | 0-1.5 h | Amara's first order delivered; Biscuit fixed | $2,330 | $1,146 | 0.0-2.0 h | 6 | 0.0-1.4 h | 0.0-1.0 h | $2,330 |
| S2 Small trader | Act 2 The Landing Wakes | 1.5-4 h | 3 moorers, Patched Office, first worker, second boat | $11,480 | $3,550 | 2.0-5.3 h | 9 | 1.4-4.0 h | 1.0-2.9 h | $13,810 |
| S3 Dock owner | Act 3 Visitors in the Fog | 4-7 h | P2 pier built, first ship order, goons survived | $42,500 | $12,700 | 5.3-8.6 h | 9 | 4.0-6.7 h | 2.9-5.2 h | $56,310 |
| S4 Port owner | Act 4 Steel and Salt | 7-11 h | P3 deep berth + crane, shipping boat, Cut half dug | $340,000 | $70,500 | 8.6-13.4 h | 14 | 6.7-10.7 h | 5.2-8.4 h | $396,310 |
| S5 Rival endgame | Act 5 The Charter | 11-14 h | Cut open + swing bridge, 4 of 7 allies, the vote | $530,000 | $146,000 | 13.4-17.1 h | 10 | 10.7-13.7 h | 8.4-10.9 h | $926,310 |
<!--/T-->

Key purchases per stage:
<!--T:spend-->
| Stage | Key purchases (the gate spend the hours are computed from) |
|---|---|
| S1 | Biscuit engine rebuild $90; Biscuit hull patch $120; Ingrid's three planks + bench $100; Fibreglass rod $450; Braided line $300; Tackle pouch to 5 baits $250; Landing net $150; Warehouse T1: Patched Warehouse $300; Fix Walt's bed $120; Biscuit paint job $150; Biscuit Engine I $300 |
| S2 | Office T1: Patched Office $2,000; Kestrel launch $5,500; Repair berths B2 + B3 $650; Stove, desk, chair $900; First deckhand hire fee $230; Carbon rod $1,600; Power post for Mari $600 |
| S3 | P2 finger piers (6 berths) $12,000; Office T2: Harbour Office $8,000; Security: 4 lamps, chain moorings, watchman hut $4,500; Puffin trawler $18,000 |
| S4 | P3 deep-water berth $60,000; Harbour crane $90,000; Tern coaster (shipping boat) $120,000; Office T3: Port Authority Building $40,000; Dredge Walt's Cut, part 1 $30,000 |
| S5 | Finish dredging Walt's Cut $80,000; Swing bridge $250,000; Office T4: Hale Tower $150,000; Allies' request chains (materials, gifts) $50,000 |
<!--/T-->

![Money earned over time](money-over-time.png)

*Total money earned (log scale) against real hours played, for solo, 2 and 4 players. Shaded bands are the solo stages. Each line ends at that crew size's Charter vote. The curve rises in steps: every stage adds a new income type, so the line keeps climbing instead of flattening into grind.*

### 3.2 Where the money comes from (solo, net $ per real hour)
<!--T:income-->
| Stage | Hand fishing | Restaurant/hotel orders | Mooring fees | Workers fishing | Services (fuel, bait, ice) | Ship orders | Shipping consignments | Total solo $/h |
|---|---|---|---|---|---|---|---|---|
| S1 Fisherman | $1,000 (87%) | $80 (7%) | $66 (6%) | - | - | - | - | $1,146 |
| S2 Small trader | $1,700 (48%) | $700 (20%) | $450 (13%) | $700 (20%) | - | - | - | $3,550 |
| S3 Dock owner | $2,400 (19%) | $1,600 (13%) | $1,100 (9%) | $3,500 (28%) | $600 (5%) | $3,500 (28%) | - | $12,700 |
| S4 Port owner | $3,500 (5%) | $3,000 (4%) | $2,500 (4%) | $9,000 (13%) | $2,500 (4%) | $30,000 (43%) | $20,000 (28%) | $70,500 |
| S5 Rival endgame | $5,000 (3%) | $6,000 (4%) | $3,500 (2%) | $15,000 (10%) | $5,000 (3%) | $61,500 (42%) | $50,000 (34%) | $146,000 |
<!--/T-->

![Income mix by stage](income-mix.png)

### 3.3 What unlocks when (by act and day; days from gdd/world)
| When | Unlocks (economy) | Tag |
|---|---|---|
| Prologue, Day 1 | Fish Market (Pratt 60%), June's bait + tackle, **Walt's Old Shed (warehouse T0)**, dug worms, Nell's Curios | MUST (bait, warehouse *(Manan 31 Oct)*) |
| Act 1, Days 1–4 | **Mari's mooring request (Day 3, or at $250 total earned, ~Day 2 15:00; MUST first vendor *(Manan 31 Oct)*)**, Doyle repairs + paint, Plank & Post basics (planks, bed, warehouse T1, furniture), contracts app, The Pelican, shrimp, cast net, transient mooring requests | MUST (shops) |
| Act 2, Days 5–12 | Mooring Line job board → **first worker (MUST first automation)**, Kestrel, Office T1, berths B2–B3, Hugo & Fifi, Ola's Ice, sardine + squid, surf shop (Dawn Patrol), Gull's Nest Café orders, Pim | MUST (hiring, surf shop) |
| Act 3, Days 13–20 | **Goons' first visit (MUST)** → rival costs begin. P2 piers, Office T2, **first ship order** (hired mobile crane), Puffin, Deep-sea rod, security items, Old Lowe, Sorrel cannery, the hotel, Sven, the Pickle Sisters, jib crane, cold room, fuel pump | MUST (piers, ship orders) |
| Act 4, Days 21–32 | P3 deep berth, harbour crane, **Tern shipping boat + consignments (MUST)**, Office T3, Walt's Cut part 1, Swordfish (T4), Bulldog tug, Captain Mags, Dr Cole, superyachts, Tamsin Wade, **cartel line opens (optional)** | MUST (cranes, shipping boats) |
| Act 5, Days 33–40+ | Charter-season consignments (×2 pay), finish the Cut + swing bridge, Office T4 Hale Tower, allies' request chains | SUGGESTED |
| Endless | Albatross, Cormorant, Halcyon, gantry, P4, the Voss debt buy-out (ending 2) | SUGGESTED |

### 3.4 Endless (after the vote; gdd/world 4.8)
<!--T:endless-->
| Endless purchase | Price |
|---|---|
| Albatross stern trawler | $150,000 |
| Cormorant feeder | $600,000 |
| Ship-to-shore gantry | $400,000 |
| P4 second deep-water berth | $150,000 |
| Halcyon flagship | $1,800,000 |
| Ending 2 only: buy out Voss's debt | $3,000,000 |
<!--/T-->

Endless keeps money meaningful through bigger ships and berths, cosmetics, the Lantern Festival every 28 days, Derby prizes, and Legend hunts. Income keeps growing ×2–3 with each big purchase. Per hour it pays a little less than the story did, because the Charter-season bonuses end (as PowerWash's replays pay 25%).

### 3.5 Pacing rules the self-check enforces
- Solo total 15–19 h, 2P 12.5–15 h, 4P 10–13.5 h, and 4P < 2P < solo.
- Each stage's solo hours fall within its target window (S1 1.5–2.3 h ... S5 3.0–4.3 h).
- Income per hour grows by more than ×1.5 and less than ×6 from each stage to the next (no cliffs, no flat zones).
- The first purchase (Biscuit's $90 engine *(Manan 31 Oct)*) costs at most 15 min of fishing.
- A skill-1 deckhand on Biscuit earns more than they cost. A skill-2 deckhand earns less per hour than the player's own hand fishing (automation helps but never replaces play).

**Tuning knobs, in order:** the stage key spends (cheapest knob). Then the S4–S5 ship/consignment rates. Never lengthen S1–S2: they are the demo and the first build.

---

## 4. Shops (all MUST shops plus more). NPCs and places are gdd/world's

### 4.0 One shop UI for all (SUGGESTED; look = Ponyo Coast warm paper, owned by gdd/systems)
```
┌──────────────────────────────────────────────────────────────────────────┐
│  DOYLE BOATWORKS              Fergus: "She's a good boat. Don't scratch her." │
│ ┌──────────────┐ ┌──────────────────────────────┐ ┌──────────────────────┐│
│ │ BOATS        │ │                              │ │ KESTREL  · T2 launch ││
│ │ ▸ Kestrel  ● │ │    (3D turntable on the       │ │ Speed   6 → 9 m/s  ▲ ││
│ │   Puffin  🔒 │ │     slipway; WALK ABOARD     │ │ Tank   15 → 50 L   ▲ ││
│ │ UPGRADES     │ │     to test it: E)           │ │ Hold    0 → 6 slots ▲ ││
│ │ PAINT        │ │                              │ │ Bunk    – → 1      ▲ ││
│ │ REPAIRS      │ │                              │ │ Pays back ~3 h       ││
│ │ INSURANCE    │ │                              │ │ $5,500  need $1,240  ││
│ └──────────────┘ └──────────────────────────────┘ │ [ HOLD E TO BUY ]    ││
│  Bank $4,260   ·   Crew rule: ask above $5,000     └──────────────────────┘│
└──────────────────────────────────────────────────────────────────────────┘
```
- **Left:** categories and items. A lock shows the unlock condition on hover ("Act 3").
- **Centre:** a 3D preview. Boats sit life-size on the slipway, and you can **walk aboard before buying**: the shop panel hides while you're on deck.
- **Right:** stat changes in green/red against what you own now, the payback estimate, the price, and "need $X more".
- **Buying:** hold E for 0.6 s. Above the crew limit, the button becomes "Ask the crew" and sends a phone request (§9).
- **Closed shops:** a door sign shows "Back at 07:00". Basic consumables (bait, ice, planks) can be ordered by phone at +$5 and are delivered to our warehouse by 07:00 *(Manan 31 Oct)*. Boats, upgrades and furniture need a visit.
- **Sounds:** a shop bell on entry, a cash-drawer ka-ching on buy, and the NPC's bark (3–5 lines each, from gdd/world's personalities).

### 4.1 Hook, Line & Sinker: bait and tackle (MUST). June Pike, Old Saltmouth, 05:00–19:00 daily
Bait goes in the rod's tackle pouch, never in a hotbar slot (gdd/fishing). **Bait is buyable, and each bait gives different fish and prices (MUST).**
<!--T:bait-->
| Bait | Price | Casts | Unlock | Tag | Note |
|---|---|---|---|---|---|
| June's free bag of worms | free | 10 | prologue (gdd/world) | SUGGESTED | "On the house. For Walt." |
| Dug worms (beach, low tide) | free | 10/day | Act 1 | SUGGESTED | anti-softlock: you can always fish; also R6 counterplay |
| Worms | $5 | 10 | start | MUST (bait is buyable) | favourite of sprat, mullet, mackerel; ~$16/fish on the bamboo rod (gdd/fishing model) |
| Shrimp | $12 | 10 | Act 1 | MUST | favourite of garfish, bream; ~$36/fish on the Fibreglass rod |
| Sardine | $18 | 10 | Act 2 | SUGGESTED | favourite of conger, mahi, blue shark; ~$73/fish on the Carbon rod |
| Squid | $25 | 10 | Act 2 | SUGGESTED | favourite of snapper, grouper, sunfish, swordfish, anglerfish; ~$150/fish on the Deep-sea rod |
| Walt's Mix (best bait) | $40 | 1 | once you own Old Faithful | MUST (Manan 31 Oct) | $40 each at June's; the only bait for Old Brass; big Trophy fish love it; ~$390/fish on Old Faithful |
| Chum bucket | $20 | 3 min | Act 1 | SUGGESTED | more bites from the rarer half of the pool; never changes the 4-10 s bite timer |
<!--/T-->

Tackle:
<!--T:tackle-->
| Item | Price | Unlock | Tag | Effect |
|---|---|---|---|---|
| Walt's bamboo rod | start | start | MUST (rod in hotbar) | green ×1.00, pointer ×1.00, bite 4–10 s. Catches sprat, mullet, mackerel (MUST 3 per rod, Manan 31 Oct) |
| Fibreglass rod | $450 | start (visible goal) | SUGGESTED | green ×1.15, pointer ×0.95, bite 4–9 s. Adds garfish, bream, conger (Manan 31 Oct) |
| Carbon rod | $1,600 | Act 2 | SUGGESTED | green ×1.30, pointer ×0.90, bite 4–8 s. Adds red snapper, mahi-mahi, dusky grouper (Manan 31 Oct) |
| Deep-sea rod | $6,000 | Act 3 | SUGGESTED | green ×1.40, pointer ×0.85, bite 4–7 s. Adds sunfish, blue shark, swordfish (Manan 31 Oct) |
| Old Faithful (Walt's rod, restored) | $15,000 | journal quest, Act 4 | MUST (best rod for the legend, Manan 31 Oct) | green ×1.50, pointer ×0.80, bite 4–6 s. Adds anglerfish, oarfish and Old Brass (with Walt's Mix) |
| Braided line | $300 | start | SUGGESTED | higher weight rating: fewer HEAVY fish (gdd/fishing §6.4) (Kufu review 31 Oct) |
| Tackle pouch: all 5 baits | $250 | start | SUGGESTED | the pouch starts with 3 bait types; this makes it 5 (Manan 31 Oct: 5 baits) |
| Landing net | $150 | start | SUGGESTED | co-op: a friend readies the net: +1 retry, +10% green (gdd/fishing §4.2) (Kufu review 31 Oct) |
| Cast net | $200 | Act 1 | SUGGESTED | catch sprats as free bait (gdd/fishing) |
| Fish guide book | $80 | start | SUGGESTED | shows which fish each bait and rod brings |
| Boat cooler (portable) | $300 | start | OPEN QUESTION | 4 fish slots on any boat; needs Manan's OK (fish never in crates) |
| Fishing hats, vests (cosmetic) | 40-200 | start | SUGGESTED | cosmetic only |
<!--/T-->

**Edge cases:**
- **Bait sold out** (R6 Bait squeeze, gdd/world): that bait line shows "Back Thursday". The counterplay is dug worms, the cast net, or WaltsMix.
- **Broke at June's:** dug worms are always free, 10 a day at low tide, so a player can never be locked out of fishing.

### 4.2 Swell Surf Co.: surf gear (MUST). Kai Rennick, Breaker Point road end, 09:00–18:00
Closed in storms ("Gnarly. Shop's shut. Go home."). Kai texts a swell report at dawn. Surfing is traversal, fun and side income, and never required for progression.
<!--T:surf-->
| Item | Price | Unlock | Tag | Effect |
|---|---|---|---|---|
| Dawn Patrol lesson with Kai | free | Act 2 event (gdd/world) | SUGGESTED | tutorial + free foam board rental |
| Foam board | $350 | Act 2 | MUST (surf gear) | easy waves, stable |
| Bodyboard | $200 | Act 2 | MUST | lie-down; good for co-op races |
| Longboard | $1,200 | Act 2 | MUST | cruisy, nose rides |
| Shortboard | $900 | Kai's trick challenge 1 | MUST | fast, tricks |
| SUP paddleboard | $700 | Act 2 | SUGGESTED | paddle the harbour, fish from it (worms only) |
| Wetsuit | $400 | Act 2 | MUST | swim stamina +50%, cold nights |
| Fins | $120 | Act 2 | SUGGESTED | swim speed +30% (falling off boats happens) |
| Mask + snorkel | $150 | Act 2 | SUGGESTED | see underwater collectibles |
| Wax, grip pads, leash | 10-60 | Act 2 | SUGGESTED | grip + cosmetic |
| Jet ski | $6,000 | Act 3 | SUGGESTED | 18 m/s personal ride, rider + 1 |
| Weekly surf contest entry | $50 | Sundays from Act 2 | SUGGESTED | prizes $500 / $1,500 / $4,000 (rises by act) |
<!--/T-->

### 4.3 Doyle Boatworks: boats, upgrades, colours (MUST). Fergus Doyle, Old Saltmouth harbour + slipway
Shop 07:00–20:00. The fuel dock is self-serve 24 h at $1.50/L. Repairs happen overnight on the slipway (ready 07:00), or instantly at 2× the price.

**Boat roster (MUST: several boats, from a tiny damaged boat to trawlers and cargo ships).** Speeds and tiers T1–T4 match gdd/world's travel times. Every deck is life-size and walkable (MUST).
<!--T:boats-->
| Id | Name | Tier | Role | Length | Walkable deck | Speed m/s | Tank | Burn | Range | Hold | Worker slots | Players aboard | Price | Unlock |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| F1 | **Biscuit** | T1 | fishing | 5 m | open wooden skiff: tiller aft, one bench, bow locker; leaks until patched | 4.5 (damaged) / 6 | 15 L | 0.5 L/min | 30 min | 0 (+4 cooler OQ) | 1 | 2 (3 squeezed) | inherited | start (Walt's) |
| F2 | **Kestrel** | T2 | fishing | 7.5 m | centre console, cuddy cabin with 1 bunk, aft fishing deck, swim ladder | 9 | 50 L | 1.2 L/min | 42 min | 6 | 1 | 4 | $5,500 | start (visible goal) |
| F3 | **Puffin** | T3 | fishing | 12 m | wheelhouse (wheel + chart table), aft work deck with net drum, roof ladder, hold hatch | 11 | 240 L | 3.0 L/min | 80 min | 20 (cold) | 2 | 4 | $18,000 | Act 3 |
| F4 | **Swordfish** | T4 | fishing | 10 m | flybridge (ladder), fighting chair aft, cabin with 2 bunks | 16 | 320 L | 7.0 L/min | 46 min | 6 | 1 | 4 | $45,000 | Act 4 |
| F5 | **Albatross** | T4+ | fishing | 24 m | 2 decks: bridge, galley, 4 bunks, factory deck, stern ramp | 10 | 1,600 L | 10.0 L/min | 160 min | 80 (cold) | 6 | 4 | $150,000 | Endless |
| S1 | **Bulldog** | service | tug | 18 m | wheelhouse, push bow, towing winch aft | 9 | 900 L | 6.0 L/min | 150 min | - | 1 | 4 | $50,000 | Act 4 (optional; else Dot's pilot $800/ship) |
| C1 | **Tern** | cargo | shipping | 35 m | bridge aft, 12-container hold, own deck crane, 3 cabins | 8 | 6,000 L | 18.0 L/min | 333 min | 12 TEU | 3 | 4 | $120,000 | Act 4 (MUST: shipping boat) |
| C2 | **Cormorant** | cargo | shipping | 80 m | bridge tower, walkable deck between container rows, crew mess | 8 | 30,000 L | 45.0 L/min | 667 min | 80 TEU | 6 | 4 | $600,000 | Endless |
| C3 | **Halcyon** | cargo | shipping (flagship) | 140 m | 300 TEU, bridge with balcony wings, walkable engine room | 9 | 90,000 L | 110.0 L/min | 818 min | 300 TEU | 10 | 4 | $1,800,000 | Endless |
<!--/T-->

**Deck layouts (walkable, MUST):**
- **Biscuit (5 m):** one step down from the pier into an open hull. Tiller and outboard at the stern, a bench amidships, a bow locker for the rod rack. Two players fish from either side; a third has to stand in the middle (comedy).
- **Kestrel (7.5 m):** centre console with wheel and throttle, a cuddy cabin with one bunk (sleep at sea → the night skip works aboard), an aft deck for 3 rods, a swim ladder.
- **Puffin (12 m):** wheelhouse with wheel, chart table and radio. Aft work deck with the net drum. A ladder to the wheelhouse roof (lookout). A hatch down to the cold hold.
- **Swordfish (10 m):** a flybridge up a ladder (second wheel), a fighting chair aft for Trophy fish, a cabin with 2 bunks.
- **Albatross (24 m):** two decks (bridge on top, galley + 4 bunks below), a factory deck, a stern ramp.
- **Bulldog tug (18 m):** wheelhouse, push bow, towing winch aft (one player steers, one runs the winch).
- **Tern (35 m):** bridge aft, a 12-container hold, its own deck crane, 3 cabins.
- **Cormorant (80 m) and Halcyon (140 m):** walkable decks between container rows. On Halcyon you can also walk the engine room.

**Upgrades per boat** (price = base × boat class factor):
<!--T:boat_upgrades-->
| Upgrade | Effect | Tag | Biscuit | Kestrel | Puffin | Swordfish | Albatross |
|---|---|---|---|---|---|---|---|
| Engine I | +15% speed | MUST (boat upgrades) | $300 | $600 | $1,500 | $2,400 | $4,500 |
| Engine II | +30% speed (needs Engine I) | MUST | $900 | $1,800 | $4,500 | $7,200 | $13,500 |
| Long-range tank | +50% fuel | SUGGESTED | $250 | $500 | $1,250 | $2,000 | $3,750 |
| Fish box | +50% hold slots (Biscuit: +4) | OPEN QUESTION (boat storage) | $300 | $600 | $1,500 | $2,400 | $4,500 |
| Ice lining | fish in the boat's box rot at half speed | SUGGESTED | $400 | $800 | $2,000 | $3,200 | $6,000 |
| Fish finder | shows fish shadows + spot quality | SUGGESTED | $500 | $1,000 | $2,500 | $4,000 | $7,500 |
| Rod holders | +20% worker catch | SUGGESTED | $250 | $500 | $1,250 | $2,000 | $3,750 |
| Deck lights | night fishing on deck (night fish bite a little more: conger, swordfish, anglerfish) | SUGGESTED | $300 | $600 | $1,500 | $2,400 | $4,500 |
| Hull plating | -50% collision damage | SUGGESTED | $600 | $1,200 | $3,000 | $4,800 | $9,000 |
| Bilge pump | auto-bails leaks | SUGGESTED | $150 | $300 | $750 | $1,200 | $2,250 |
| Locked fuel cap | blocks R11 engine sabotage (gdd/world) | SUGGESTED | $50 | $100 | $250 | $400 | $750 |
| Autopilot | sail to a phone waypoint (Act 3+) | SUGGESTED | $1,500 | $3,000 | $7,500 | $12,000 | $22,500 |
<!--/T-->

**Colour customisation (MUST)** happens in Doyle's paint bay: you pick from a swatch wheel with a live preview on the turntable and pay; **Doyle's crew paints the boat for you** (instant, shown as a short paint-over). The player never holds a brush or edits the boat *(Manan 31 Oct: "automatically… don't give permission to the player to do"; ★ reading, see CHANGES.md)*. Going back to the last paint you owned is free.
<!--T:cosmetics-->
| Cosmetic | Price | Tag |
|---|---|---|
| Hull paint (24 swatches) | $150 | MUST (colours) |
| Trim / accent colour | $80 | MUST |
| Stripe pattern (8) | $120 | SUGGESTED |
| Name decal (first one free) | $50 | SUGGESTED |
| Flag (12 + Walt's) | $60 | SUGGESTED |
| Deck wood stain | $100 | SUGGESTED |
| Horn sound (6) | $200 | SUGGESTED |
| Livery: Walt's Classic | free (journal) | SUGGESTED |
<!--/T-->

**Repairs (SUGGESTED):**
- **Hull HP 0–100%.** Damage: bumping a pier −2–5%, a rock −10–25%, a storm −5–15% per in-game hour at sea, a rival dent −20% (R5).
- **Below 50% HP:** the boat is −20% speed and leaks (bail, or fit a bilge pump).
- **At 0%:** the boat never sinks. It's "swamped": the engine dies, and Dot tows it home. The first tow each act is free; after that the tow costs $50 × class, capped at 15% of cash.
- **Repair cost** at Doyle: $1.50 per % for Biscuit; for other boats, 0.15% of the boat's price per %. Examples: Kestrel ~$8/%, Puffin ~$27/%, Tern ~$180/%.
- **Patch kit** ($40, Hook, Line & Sinker): +20% HP at sea. It's a 6 s action, and in co-op it's a two-player job: one holds, one hammers.

**Buy-back:** Doyle buys any boat at 60% of its price. Biscuit can never be sold ("That's Walt's. Not for sale."), which also stops a broke player selling the only boat.

**Insurance (gdd/world counterplay):** 2% of the insured fleet's value per in-game week. It pays 50% of rival-caused repairs and tows. It shows on the Ledger as a weekly line.

### 4.4 Plank & Post Builders' Yard: dock upgrades (MUST). Ingrid Sundqvist, Old Saltmouth, 07:00–18:00, closed Sundays
**Build rule:** order before 18:00 and Ingrid's crew builds overnight. At 07:00 a 3 s timelapse plays (scaffold → done). Big builds (P2, P3, cranes, Office T2+) take 2–3 nights with visible scaffolding, and you can still walk the site. Furniture is instant and **installs itself in its set spot** (every item has one home in the office or on the dock); there is no placement tool *(Manan 31 Oct, ★ reading)*.
<!--T:dock-->
| Category | Item | Price | Unlock | Effect | Tag |
|---|---|---|---|---|---|
| Piers & berths | Ingrid's offer: fix three planks (+ free bench) | $100 | Act 1 day 3 | first dock upgrade | SUGGESTED |
| Piers & berths | Repair berth B2 | $250 | Act 2 | +1 moorer slot (B1 is usable from day 1) | MUST (moorers) |
| Piers & berths | Repair berth B3 | $400 | Act 2 | +1 moorer slot | MUST |
| Piers & berths | P2 finger piers, Cannery Flats (6 berths) | $12,000 | Act 3 | +6 slots, worker boats | MUST (multiple piers) |
| Piers & berths | P3 deep-water berth | $60,000 | Act 4 | ship orders with containers, Tern | MUST |
| Piers & berths | P4 second deep-water berth | $150,000 | Endless | two big ships at once | SUGGESTED |
| Walt's Cut | Dredge the Cut, part 1 (barge rent + 3 dredge jobs) | $30,000 | Act 4 | Cut half open at high tide | SUGGESTED (gdd/world) |
| Walt's Cut | Finish dredging | $80,000 | Act 5 | Cut open; Voss Harbour 7.3 min in Biscuit (was 9.6), 4.8 in the Kestrel; ships reach P3 directly (Kufu review 31 Oct) | SUGGESTED |
| Walt's Cut | Swing bridge | $250,000 | Act 5 | ships reach our side; the Charter goal | SUGGESTED |
| Mooring amenities | Power post (per berth) | $600 | Act 2 | moorer fee +30%; R4 counterplay | SUGGESTED |
| Mooring amenities | Water tap (per berth) | $350 | Act 2 | moorer fee +20% | SUGGESTED |
| Mooring amenities | Dock lamp | $200 | start | fee +10% nearby; night sabotage -50% (4 lamps) | SUGGESTED |
| Mooring amenities | Chain moorings (per berth) | $300 | Act 3 | blocks R5 cut lines | SUGGESTED |
| Mooring amenities | Life rings + fire buckets | $150 | Act 3 | passes R7 inspection | SUGGESTED |
| Services | Bait vending machine | $1,500 | Act 2 | passive bait sales to visitors | SUGGESTED |
| Services | Ice machine | $1,200 | Act 2 | boat fish boxes rot at half speed (the warehouse has its own cold room) | SUGGESTED |
| Services | Fuel pump | $7,000 | Act 3 | buy at $0.90/L, sell to visitors at $1.60/L | SUGGESTED |
| Cranes | Mobile crane hire (per ship order) | $1,500 | Act 3 | the first ship orders before we own a crane | SUGGESTED (gdd/world) |
| Cranes | Hand winch / davit | $900 | Act 2 | lift Trophy fish and heavy crates | SUGGESTED |
| Cranes | Jib crane | $15,000 | Act 3 | own crane for small ship orders (no hire fee) | SUGGESTED |
| Cranes | Harbour crane | $90,000 | Act 4 | containers | MUST (cranes) |
| Cranes | Ship-to-shore gantry | $400,000 | Endless | big container ships, 2x speed | SUGGESTED |
| Storage | Warehouse T0: Walt's Old Shed | free | Day 1 | 12 slots of 10 fish; fish, tools, collectibles, spare bait | MUST (Manan 31 Oct: warehouse from the start) |
| Storage | Warehouse T1: Patched Warehouse | $300 | Day 1-2 | 24 slots | MUST (expandable, Manan 31 Oct) |
| Storage | Warehouse T2: Cold Room | $2,500 | Act 2 | +12 cold slots: fish rot at 25% | MUST (expandable, Manan 31 Oct) |
| Storage | Warehouse T3: Big Warehouse | $8,000 | Act 3 | 48 slots + 24 cold; forklift lane for workers' catch | MUST (expandable, Manan 31 Oct) |
| Storage | Warehouse T4: Ice House | $30,000 | Act 4 | 96 slots + 48 cold (rot at 10%); hotel bulk orders | MUST (expandable, Manan 31 Oct) |
| Storage | Cargo shed (pallets) | $35,000 | Act 4 | store unloaded cargo, sell later | SUGGESTED |
| Storage | Container yard (40 TEU) | $50,000 | Act 4 | store containers | SUGGESTED |
| Living | Fix Walt's bed (the cot) | $120 | start | sleep with the Rested buff | MUST (beds) |
| Living | Bunk bed | $350 | start | extra co-op sleep spot | MUST |
| Living | Double bed | $700 | Act 2 | comfort +3 | SUGGESTED |
| Living | Hammock | $200 | start | sleep aboard boats with a cabin | SUGGESTED |
| Furniture | Desk / chair / shelves / wardrobe | 80-400 | start | comfort +1 each; wardrobe = free outfit swaps | MUST (furniture) |
| Furniture | Map table | $900 | Act 2 | big map of spots, orders, worker boats | SUGGESTED |
| Furniture | Trophy mount (per fish) | $300 | first Trophy | display Trophy/Legend fish | SUGGESTED |
| Furniture | Collectibles cabinet | $600 | first collectible | display collectibles | SUGGESTED |
| Furniture | Stove | $400 | start | cook fish: Well-fed buff | SUGGESTED |
| Furniture | Balcony (office) | $3,000 | Act 2 | the place to look at your port (gdd/world) | SUGGESTED |
| Furniture | Rugs, plants, lamps, paintings | 30-500 | start | comfort; pure style | SUGGESTED |
| Office | T1 Patched Office | $2,000 | Act 2 | liveable office, phone Crew app full, 3 staff | MUST (office upgrades) |
| Office | T2 Harbour Office | $8,000 | Act 3 | 8 staff, ship orders on the phone, ledger | MUST |
| Office | T3 Port Authority Building | $40,000 | Act 4 | 20 staff, consignments, insurance desk | MUST |
| Office | T4 Hale Tower | $150,000 | Act 5 | 40 staff, Charter campaign room, top-floor view | SUGGESTED |
| Security | Watchman hut (for Old Lowe) | $2,500 | Act 3 | lets you hire a night watchman | SUGGESTED |
| Security | Gate + fence | $3,000 | Act 3 | goons take 2x longer on the dock | SUGGESTED |
| Security | CCTV | $6,000 | Act 4 | auto-evidence photo of night sabotage | SUGGESTED |
<!--/T-->

**Office upgrades (MUST: upgrade the run-down office).** Each tier visibly rebuilds the office:
- **T0, Walt's dusty office with the cot.** Phone basics only.
- **T1, Patched Office.** The Crew app works fully, up to 3 staff.
- **T2, Harbour Office.** 8 staff, ship orders, the Ledger's weekly view.
- **T3, Port Authority Building.** 20 staff, consignments, the insurance desk.
- **T4, Hale Tower.** 40 staff, the Charter campaign room, the top-floor view of the whole port. This is the "watch it run" shot from gdd/world.

Office tiers are the soft gates on staff numbers, so a player can't hire 20 deckhands in Act 2.

### 4.5 The Mooring Line pub: hiring (MUST). Rosie Tull, Old Saltmouth, 11:00–02:00
The job board refreshes at 07:00 with **3 candidates** (gdd/world). Rosie texts one-line reviews. A new hire starts the **next morning** (as TCG Card Shop does). Hiring is in person at the board, or from the phone once the board has been visited once. Assigning staff is always from the phone (MUST).
<!--T:jobs-->
| Job | Unlock | Wage $/day | What they do | Tag |
|---|---|---|---|---|
| Deckhand (fisher) | Act 2 (Mooring Line job board) | 70 + 22.5 x (Fishing + Seamanship) | fishes from an assigned boat (phone) | MUST (first automation) |
| Pim (unique) | Act 2 day ~10 | free week 1, then 115 | skill-1 deckhand; warns of R11 sabotage | SUGGESTED (gdd/world) |
| Dockhand | Act 2 | 120 flat | sells the warehouse catch at the best open buyer; fills simple orders | SUGGESTED |
| Mechanic | Act 3 | 180 flat | repairs docked boats overnight at 50% parts cost | SUGGESTED |
| Night watchman (Old Lowe) | Act 3 + watchman hut | 150 flat | blocks R5 and R11 except 03:00-05:00 (gdd/world) | SUGGESTED |
| Crane operator | Act 4 | 300 flat | unloads ship orders at 70% of player speed | SUGGESTED |
| Tamsin Wade (unique) | Act 4 | 400 | ex-Voss crane operator: crane works at 100% of player speed | SUGGESTED (gdd/world) |
| Nils Berg (unique) | Act 2 | 160 | Fishing 2 / Seamanship 3, never no-shows | SUGGESTED (gdd/world) |
| "Hook" Hennessy (unique) | side quest | 200 | immune to goon intimidation; doubles as a guard | SUGGESTED (gdd/world) |
| Tug skipper | Act 4 + Bulldog | 300 flat | berths ships with the Bulldog | SUGGESTED |
| Ship captain | Act 4 | 1,200 (Tern) / 3,000 (Cormorant) | runs shipping consignments | MUST (shipping) |
<!--/T-->

### 4.6 Hartley's Outfitters: clothes (SUGGESTED new shop). Wen Hartley, Old Saltmouth, 09:00–19:00, closed Sundays
Manan's decision (via gdd/systems, 2026-10-31): the 6 character presets ship, and players edit **clothes only**. Everything here is cosmetic: no stats, ever. Swapping between owned items at any wardrobe is free (gdd/systems).
<!--T:clothes-->
| Slot | Price $ | Examples | Tag |
|---|---|---|---|
| Headwear | 40-250 | beanies, sou'westers, hard hats, flat caps, captain's cap | MUST (clothes-only edits) |
| Top | 60-300 | tees, flannels, striped long-sleeves, Hawaiian shirt | MUST |
| Over | 80-400 | hi-vis vest, oilskin, puffer, captain's jacket | MUST |
| Legs | 60-250 | cargos, bib overalls, shorts + socks | MUST |
| Boots | 80-350 | rubber boots, steel-toes, deck shoes | MUST |
| Gloves | 30-150 | rubber, leather, gauntlets | MUST |
| Colour dye (per item) | 25 | any owned item, 24 swatches; the player colour band is fixed and not for sale | SUGGESTED |
| Story sets | free | Walt's old captain set (journal), Voss-port uniform (ending 2), Teddy Mango's shirt (cartel) | SUGGESTED |
<!--/T-->

### 4.7 More places that take or pay money (SUGGESTED)
- **Doyle's fuel dock:** $1.50/L, self-serve 24 h. Our own fuel pump (Act 3) buys at $0.90 and sells to visiting boats at $1.60.
- **Dot's lighthouse:** pilot service $800 per ship (berths a ship if we have no tug); her formal complaint (free, once per act); free first tow each act.
- **Post office + Nell's Curios:** buys collectibles, with set bonuses (6 Margaret tiles → a story + $1,500). Walt's letters arrive here until Mags' ferry stops at the Landing.
- **Gull's Nest Café (Bao Tran):** food buffs, $6–15. Small fish orders at 130%.
- **Teddy Mango's beach club, Mango Cay (cartel):** sells only for Mango cash (§5.8).
- **Gun counter at Hook, Line & Sinker:** OPEN QUESTION 2. If guns stay non-lethal (gdd/world Q3): flare gun $80 (signal Dot, scare gulls), net gun $1,200 (goon comedy, stops a chasing boat), harpoon gun $3,000 (lands Trophy fish faster, never Legends).

---

## 5. Income sources over the game

### 5.1 Fish sales (MUST core; numbers from gdd/fishing)
- **Hand fishing:** ~70–78 fish per real hour (better rods bite sooner). By rod + its best bait, from gdd/fishing's model (15 fish, practised player, fair price) *(Manan 31 Oct)*: bamboo + worms ~$16/fish, Fibreglass + sardine ~$44, Carbon + squid ~$83, Deep-sea + squid ~$150, Old Faithful + Walt's Mix ~$390. **The rod, not the place, decides what you can catch.** After buyers, saturation, bait and fuel, we budget S1 at **$1,000/h** net (was $1,100), rising to $5,000/h in S5.
- **Capacity:** the hotbar holds 8 slots with fish stacks of 5, split by species and grade, so a trip holds 20–30 fish (gdd/fishing). That's why trips are short and selling happens often, which makes the market and the buyers part of the fun.
- **Trophy and Legend fish** (player-only): $140–220 and $1,500 base. **Old Brass**, the one Legend, needs Old Faithful AND Walt's Mix ($40 each); the first catch is the story beat (mount it or sell it), after that it can bite again, rarely (1% a cast) *(Manan 31 Oct)*. A big, rare spike, never the plan.
- ~~Zone stock~~ **Removed *(Manan 31 Oct)*: no zones, any fish anywhere.** Anti-grind now rests on market saturation (−2% per same species per buyer per day) and hotspots that thin out after 6–10 catches (gdd/fishing §5.3). **The 4–10 s bite timer never changes (MUST).**

### 5.2 Mooring fees (MUST: the first vendor pays to moor; more vendors later)
**Mari Duarte, the first one (MUST):**
- **Day 3 at 07:00, or the moment total earned reaches $250, whichever is first** *(Manan 31 Oct)* (was Day 1, 13:00; in the sim: Day 2, 15:00): she texts. Accept → she arrives within the in-game hour, we tie her line (two-player catch-and-tie in co-op), and she pays **$40 at 07:00 every morning**. Decline → she asks again tomorrow at $45, then at $50, then gives up (gdd/world).
- **Berth:** B1 is usable from Day 1, so no purchase is needed for the first income. That's the point: the first money arrives for free, and it teaches that the dock earns.
- **Fee rules (all moorers):** fee × (1 + 0.3 power post + 0.2 water tap + 0.1 lamp nearby). Mari with a power post pays $52.
- **Fee raise:** every 7 days a moorer may ask to pay less ("rough week") or offer more ("business is good"). The player picks by phone. Being kind adds to that moorer's ally reputation (Charter seats, gdd/world).
- **Paid at 07:00:** a toast on the left ("💸 Rent's in, landlord"), and a line in the Ledger.

**Moorers who follow (cast from gdd/world §6.3):**
<!--T:vendors-->
| Vendor | Boat | Fee/day | Needs | Arrives | Perk | Tag |
|---|---|---|---|---|---|---|
| Mari Duarte | Mari's Tacos | $40 | berth B1 (usable day 1) | Day 3 07:00, or when total earned reaches $250, whichever is first (MUST, Manan 31 Oct); ~Day 2 15:00 in the sim | free tacos; buys up to 10 fish/day at 100%; small fish orders; fee +30% with a power post | MUST (first vendor) |
| Hugo & Fifi Laurent | houseboat Petit Pois | $60 | berth B2 | Act 2 | +1 day of warning on rival actions | SUGGESTED (gdd/world) |
| Ola Lindqvist | Ola's Ice (ice barge) | $50 | berth B3 | Act 2 | fish on the warehouse racks rot 50% slower; ice packs $5 | SUGGESTED (gdd/world) |
| Sven Moss | salvage diver boat | $80 | P2 berth | Act 3 | salvage contracts at the Margaret wreck | SUGGESTED (gdd/world) |
| The Pickle Sisters | floating veg market | $90 | P2 berth + water tap | Act 3 | cheap stamina food $6 | SUGGESTED (gdd/world) |
| Captain Mags | ferry stop | $150 | P2 berth | Act 4 | letters direct; tourist charter contracts $300-1,200 | SUGGESTED (gdd/world) |
| Dr Imani Cole | floating clinic | $40 | P2 berth + power post | Act 4 | stamina regenerates faster at the Landing (pays little: volunteer) | SUGGESTED (gdd/world) |
| Visiting superyachts | random owners | 600/night | P3 berth free that night | Act 4 | tips + cosmetic unlocks | SUGGESTED |
| Transient boats (phone requests) | random | 30-400/night | any free berth | Act 1 day 3+ | accept or decline by phone | SUGGESTED |
<!--/T-->

**Why moorers leave, and how they come back:**
- **R4 berth poaching:** the newest moorer leaves for 2 days and comes back if their berth beats Voss's (power post or lamp).
- **Berth damaged** (storm or rival): they leave until it's repaired.
- **Never in Act 1–2.** Mari never leaves (she's the heart of the dock).

### 5.3 Restaurant and hotel orders (MUST: orders)
An order card on the phone's Contracts app (gdd/world 7.3) shows:
- the buyer's portrait;
- species × count, minimum grade (★ = Fine, ★★ = Rare, ★★★ = Prized) and minimum freshness;
- the deadline, the pay, and the ally reputation it gives.

Accept, then deliver in person to the buyer's door. Or, from Act 2, a Dockhand fills simple orders (Everyday species, no grade rule).

| Buyer | Typical order (solo size) | Pays | Per day | Act |
|---|---|---|---|---|
| Mari | "4 mackerel by 18:00" | $12 each (~120%) | 0–1 | 1 |
| Amara, The Pelican | 3–8 fish of one species, ★★+, Fresh/Good, 1–2 days | 150–200% of base × grade; +15% tip for ★★★ | 1–2 | 1 |
| Bao, Gull's Nest | 5 Everyday fish, any grade, by 15:00 | 130% | 1 | 2 |
| The Saltmouth Arms | 20–60 Everyday/Uncommon fish, ★+, within 2–3 days (cold room helps) | 130% | 1 every 2 days | 3 |
| Sorrel cannery | "200 of anything by Friday" | 55% (5% over its walk-in price) | 1/week | 3 |

- **Example:** Amara's first order is 3 mackerel ★★+ (base $10 × Rare 1.7 = $17 fair) × 175% ≈ $30 each, so **~$90 for 3**. That's 3 times what Pratt pays, which is the lesson.
- **Co-op:** order sizes scale ×1.0/1.6/2.1/2.5 (gdd/systems), and so does the pay. The number of orders per day stays the same.
- **Failure:** a late or missing order lapses with no fine, −1 ally reputation, and the buyer texts their disappointment. Partial delivery is paid pro rata, if at least half is delivered.

### 5.4 Workers fishing for us: the FIRST automation (MUST)
**Flow.** Hire at the Mooring Line board (Act 2, day ~7). Then everything else happens in the phone's **Crew app** (MUST: assign from the phone):
```
┌ CREW ──────────────────────────────┐
│ ● Nils Berg   Fishing ★★☆  Sea ★★★ │
│   "Never misses a shift"   $160/day │
│   ASSIGN ▸ Boat:  Biscuit           │
│            Spot:  Gullrock (2.6 min)│
│            Bait:  Shrimp ×3 (store) │
│            Shift: Day 08–17         │
│            Catch→ Warehouse (cold)  │
│   Expect 18–22 fish · ~$330 · net ~$140 │
│   [ CONFIRM ]                       │
└────────────────────────────────────┘
```
1. **Pick a worker, a boat, a spot, bait and a shift.** (Any fish anywhere *(Manan 31 Oct)*: the spot only sets travel time; the worker's rod and bait set the fish.)
   - The boat must be docked and fuelled. The worker refuels at Doyle's (or our pump) and charges it to the bank.
   - The spot is a map pin showing travel time.
   - Bait comes from the warehouse. Toggle "auto-buy at June's" to have it delivered by 07:00 at +$5.
   - Shifts: Early 05–13, Day 08–17, Late 13–22 (Late needs deck lights; **+50% wage**).
   - The catch goes to the warehouse (cold room if built), or to "sell at best open buyer" (needs a Dockhand), or to fill order #n.
2. **Confirm.** At shift start the worker **walks down the pier and sails out (visible)**. A dot on the phone map, plus texts: "Leaving for Gullrock 🎣", "Got 14, 2 rare, coming home".
3. **Return.** The boat docks, the worker carries the catch to the warehouse, and the Ledger logs the day's net.

**Formulas (SUGGESTED; stats from gdd/world: Fishing ★1–5 and Seamanship ★1–5):**
- Catch per shift = **10 + 5 × Fishing**, capped by the boat's hold (Biscuit with the cooler 20, Kestrel 30, Puffin 100). The Puffin's net drum adds +40 fish per shift.
- Grade: workers don't play the minigame, so price × **(1 + 0.15 × Fishing)**.
- Fail chance per shift = **(14 − 2 × Seamanship)%**.
- Wage = **70 + 22.5 × (Fishing + Seamanship)** per day, paid at 07:00 from the bank. Hire fee = 2 days' wage.
- Workers sell at the blended 75% (through the player or a Dockhand).
- Bait costs ~$1.20 per fish; fuel ~12 L ($18) per shift.

<!--T:workers-->
| Skill | Wage/day | Hire fee | Catch/shift | Grade mult | Fail chance | Net/day: Biscuit + shrimp | = net per real hour | Net/day: Kestrel + squid |
|---|---|---|---|---|---|---|---|---|
| 1 | $115 | $230 | 15 | x1.15 | 12% | $68 | $190 | $238 |
| 2 | $160 | $320 | 20 | x1.30 | 10% | $131 | $368 | $391 |
| 3 | $205 | $410 | 25 | x1.45 | 8% | $129 | $361 | $582 |
| 4 | $250 | $500 | 30 | x1.60 | 6% | $127 | $356 | $813 |
| 5 | $295 | $590 | 35 | x1.75 | 4% | $126 | $353 | $886 |
<!--/T-->

*Rows assume Fishing = Seamanship = skill. The two "Biscuit" columns show the design point: **a better worker needs a better boat**. Skill 2–5 workers all cap at Biscuit's 20 fish. That's the honest reason to buy the Kestrel and the Puffin for your crew.*

**Failures** (rolled at shift start, shown as a text, never silent):

| Failure | Share of fails | Effect | Fix |
|---|---|---|---|
| No-show ("hungover", "sick kid") | 30% | no shift, no wage that day | – |
| Engine trouble | 30% | comes home at half catch; Doyle fix $40 × class | mechanic, Engine I+ halves the odds |
| Snapped line / lost bait | 30% | −25% catch | braided line on the boat |
| Ran aground | 10% | stranded; the player tows it (co-op moment) or Doyle tows for $200 × class; never sinks | autopilot, Seamanship ★4+ |
| Storm (weather, not a fail roll) | – | Seasick workers refuse, others come home early at their catch so far | forecast on the phone |
| R11 sugar in the tank (Act 4, rival) | – | stalls at sea, radios for a tow | locked fuel cap, Old Lowe |

**Growth:**
- Each worker gains +1 star in their weaker stat every 12 shifts, up to their potential (hollow stars on the card).
- Morale drops if they're unpaid, never get a day off in 7, or get sent into storms. It rises with Mari's tacos and a bonus ($50).
- Morale under 30 → a "thinking of quitting" text, a 2-day warning.
- **Unpaid:** a worker works 1 day on an IOU, then quits. Rehiring costs double for 7 days.

**Quirks (gdd/world):** Early riser (leaves 06:00, +1 h), Chatty (−10% catch, sends rumours), Lucky (+5% Rare+ grades), Seasick (refuses storms). Ours: Butterfingers (+5% fail), Old Salt (+1 Fishing at night), Union rep (asks for +10% every 14 days, morale +20 if paid).

**Solo vs co-op:** identical. Workers are the same for any crew size, so in solo they're a bigger share of income. That's why solo is fine without friends.

### 5.5 Ship orders: berth fees + unloading containers with our cranes (MUST)
A phone offer (Act 3+) shows:
- the ship and its line's captain (gdd/world 10.2: Rosa Ferreira, the Okafor Line, Old Tam, Lars Holm);
- ETA, cargo count, berth fee, pay per unit, and the time window;
- from Act 4, "VOSS ALSO BIDDING" (R9).

Accept, and the ship arrives at the ETA.
- **Berthing:** it needs a pilot (Dot, $800) or our Bulldog tug.
- **Unloading:** our crane lifts each unit onto yard tiles (or the warehouse). **Each unit is paid when it touches the yard.**
- **Speed bonus:** finish within half the window → +20%.
- **Window ends:** the ship leaves with whatever is still aboard (unpaid) and the line's trust drops.
- **Damage:** a container dropped from more than 3 m pays 50%. One dropped in the sea is lost, and the line fines the unit's pay.
- **Crane operators:** they unload at 70% of player speed (Tamsin Wade: 100%). A player on the crane earns the **+30% hands-on bonus** on the units they lift.
<!--T:ship_orders-->
| Ship | Stage | Cargo | Berth fee | Per unit | Units (solo) | Window (game h) | Needs | Full pay (solo) |
|---|---|---|---|---|---|---|---|---|
| MV Hilda (small coaster) | S3 | containers | $1,000 | $200 | 12 | 4 | P2 pier + hired mobile crane (gdd/world Act 3) | $3,400 |
| Sunrise Reefer (fruit coaster) | S3 | fruit pallets | $1,500 | $150 | 20 | 4 | jib crane + cold room | $4,500 |
| Feeder ship (visiting) | S4 | containers | $5,000 | $500 | 40 | 6 | P3 berth + harbour crane + tug or Dot's pilot | $25,000 |
| Mainline ship (visiting) | S5 | containers | $10,000 | $400 | 120 | 10 | P3 + harbour crane; gantry in endless | $58,000 |
<!--/T-->

**Order frequency:**
- S3: about 1 ship every 2 days.
- S4: a feeder about every 1.5 days.
- S5: a mainline ship every 3 days, plus feeders.
- **Co-op:** unit counts scale ×1.0/1.6/2.1/2.5; windows stay the same.

### 5.6 Shipping consignments with our own shipping boats (MUST)
A phone offer from a mainland broker reads, for example: "12 containers of machinery to Port Ardent. $1,300 per container. Load by 18:00 tomorrow. Trip 1.5 days."
1. **Load** the Tern at our yard with our crane.
2. **Sail out.** The captain sails to the "Ardent Lane" buoy at the map edge (~4 min). There the boat leaves the map and the phone texts updates ("Rounding the cape", "Delivered, returning"). It comes back at the end time.
3. **Player captain:** the player sails the on-map legs (out and back) for the **+30% captain's bonus** and no captain's wage. At the buoy the player rides the tender back (or Mags' ferry from Act 4).
4. **Risks:** a storm delays it (−20% late fee); 5% chance of cargo damage (−10–40% on one container); R9 outbid. **No ship is ever lost.**
<!--T:consign-->
| Boat | Stage | Capacity | $/TEU | Trip (game days) | Captain/day | Fuel/trip | Net/trip (NPC captain) | Net/trip (player captain +30%) |
|---|---|---|---|---|---|---|---|---|
| Tern | S4 | 12 TEU | $1,300 | 1.5 | $1,200 | $1,800 | $12,000 | $18,480 |
| Tern (Charter season) | S5 | 12 TEU | $2,600 | 1.5 | $1,200 | $1,800 | $27,600 | $38,760 |
| Cormorant | Endless | 80 TEU | $1,500 | 2 | $3,000 | $8,000 | $106,000 | $148,000 |
| Halcyon | Endless | 300 TEU | $1,400 | 3 | $6,000 | $25,000 | $377,000 | $521,000 |
<!--/T-->

### 5.7 Side income (SUGGESTED)
- **Transient moorings:** $30–400 a night by act.
- **Fuel pump margin:** $0.70/L.
- **Bait vending:** ~$40–120 a day.
- **Weekly surf contest:** $500 / $1,500 / $4,000.
- **Saltmouth Fish Derby** (event): $2,000–25,000 by act.
- **Collectibles** at Nell's.
- **Salvage contracts** (Sven): $300–3,000.
- **Tourist charters** (Mags): $300–1,200.
- **Rescue tows** (Dot): $200–1,000 + ally reputation.

All are player-only, so co-op crews and hands-on players earn more.

### 5.8 The cartel: a separate risk/reward economy (MUST as a later line; details SUGGESTED)
Teddy Mango, Mango Cay, from Act 4 day ~28 (gdd/world 10.1). It's optional; refusing costs nothing. It runs on its own currency so it never breaks the legit curve:
- **Mango cash:** a physical **duffel bag** item (1 hotbar slot, no stacking) that must be carried to the office safe. Drop it in the sea and it floats for 20 s, then it's gone.
- **Spending it:** on Teddy's shop only, or **launder** it into the company bank through the moorers' businesses at **75%**, capped each day at **25% of yesterday's legit income**. Mari refuses ("Not my tacos, friend."); the Pickle Sisters and Sven don't ask.

| Tier | Favour (gdd/world) | Pay (Mango cash) | Time | Catch chance | If caught |
|---|---|---|---|---|---|
| 1 Coconuts | night pickup off Mango Cay → buoy drop | $6,000–12,000 | ~15 min | 15% (Captain Keel's searchlight) | package dumped, no pay, Teddy "disappointed", 2-day cooldown |
| 2 Fruit salad | packages hidden in a Pelican fish delivery (take hotbar slots) | $15,000–30,000 | ~20 min | 20% (Constable Okafor's dock check) | fine = 1× the job's pay (capped at 15% of cash), −1 ally reputation |
| 3 The big mango | through Voss Lanes during the gala | $60,000–120,000 | ~25 min | 30% (Voss patrol chase) | package lost, +10 Heat, fine as above |

- **Expected value:** about 1.2–1.5× legit income for the same time, with swings. Each favour costs a little Island Trust (gdd/world), so it's a real trade-off against Ending 1.
- **Teddy's shop (Mango cash only):**
  - silent engine (−50% patrol detection) $20,000 × class factor (scaled like the boat upgrades);
  - hidden fish box (packages don't show in dock checks) $15,000 × class factor;
  - lights-off toggle, free;
  - "Mango Sunset" paint, free after tier 1;
  - gold flare gun, gift;
  - Teddy's ukulele, collectible.
- **Never:** jail, losing a boat, or harm to the player (gdd/world tone guard).
- **Co-op:** any player can take a favour; the duffel is a shared object anyone can carry. The host can switch the cartel line off for the crew (lobby setting).

---

## 6. Costs and sinks

| Sink | Rule | Typical share of gross | When |
|---|---|---|---|
| **Bait** | $5–120 per pouch (gdd/fishing) | 3–6% of hand income | always |
| **Fuel** | $1.50/L (own pump $0.90). Burn per boat in the roster. Biscuit's full tank is $22, the Puffin's $360, a Tern trip $1,800 | 2–4% | always |
| **Repairs** | §4.3 rates; storms and rocks; rival dents | 2–5% | always |
| **Wages** | §5.4 + staff jobs; paid 07:00 | 25–40% of worker gross | Act 2+ |
| **Upkeep** | **0.2% of built value per day** (piers, cranes, office, boats), charged 07:00. At S4 (~$500k built) that's ~$1,000/day | 3–5% | Act 2+ |
| **Moorer amenities** | one-off, raises fees | – | Act 2+ |
| **Insurance** | 2% of insured value per in-game week | 1–2% | Act 3+, optional |
| **Rival pressure** | gdd/world R3–R12 (below) | **3–8% averaged; never more than 15% of cash in one hit** | Act 3+ |
| **Pilot fees** | $800/ship without a tug | 2% of ship income | Act 3–4 |
| **Cosmetics** | clothes, paint, horns, furniture | player choice | always (main sink in Endless) |

**What rival actions cost** (effects are gdd/world's; $ at S3–S4 rates):

| Action | Typical cost to us | Counter (price) |
|---|---|---|
| R3 price war (−20% at main buyers for 3 days) | ~$1,500–8,000 | sell at the cannery / Mari; a rare fish for Amara |
| R4 berth poaching | one moorer's fee × 2 days ($100–300) | power post $600, lamp $200 |
| R5 cut lines | a boat lost for a morning + 20% dent ($300–5,000) | 4 lamps $800 + chain moorings $300/berth + Old Lowe $150/day |
| R6 bait squeeze | dearer substitute bait for 2 days (~$100) | dug worms, cast net, WaltsMix |
| R7 inspection | fine up to 10% of cash | life rings + fire buckets $150; Dot vouches |
| R8 buyer block | one buyer closed for 2 days | the cannery; a loyalty order |
| R9 contract bidding | lost contracts | speed, freshness, allies |
| R11 engine sabotage | tow $200 × class + a lost shift | locked fuel cap $50 × class |
| R12 smear | −10% town prices for 2 days | win the Derby; publish evidence |

Rule (gdd/world): when cash is below 1 day of costs, Voss "loses interest" for 2 days. A struggling player is never kicked while down.

**Broke and softlock rules (SUGGESTED):**
- **Cash floor:** cash can't go below $0, and wages wait (IOU, §5.4).
- **Always able to fish:** dug worms are always free, and Biscuit can never be sold.
- **Out of fuel at sea:** Dot tows you home, free the first time each act, then $50, capped at 15% of cash.
- **Walt's tin:** if cash is under $20 with no bait, no fuel and no fish, Walt mails $100 once per act ("Found this in my sock drawer. Don't tell Dot.").

**Loans (OPEN QUESTION 4):** an Island Savings counter at the post office.
- Borrow up to 50% of one listed big purchase (a boat, pier or crane).
- 10% flat fee, repaid automatically as 20% of each day's income.
- One loan at a time; nothing is ever repossessed.
- Recommended **yes**: it shortens the longest saving gaps (Tern, harbour crane) without lowering prices.

---

## 7. Solo vs co-op scaling (MUST: solo and co-op both fully enjoyable)

| Players | Order and ship unit counts | Prices | Effective income multipliers (hand / orders / ships / consignments) | Hours to the vote |
|---|---|---|---|---|
| 1 | ×1.0 | same | 1 / 1 / 1 / 1 | ~16.8 |
| 2 | ×1.6 | same | 1.45 / 1.35 / 1.3 / 1.15 | ~13.5 |
| 3 | ×2.1 | same | 1.8 / 1.6 / 1.5 / 1.25 | ~11.9 |
| 4 | ×2.5 | same | 2.1 / 1.8 / 1.7 / 1.3 | ~10.8 |

- **Prices never scale with players (SUGGESTED, agreed with gdd/systems).** A Kestrel costs $5,500 for everyone, so friends can compare notes and wikis stay true.
- **Order and ship sizes scale** (gdd/systems), so each extra player has work to do. Effective income grows less than size, because the number of offers per day, the moorers, the workers and the services don't scale.
- **Solo is never punished:**
  - workers and moorers are the same, so in solo they're a bigger share;
  - two-person jobs have a solo path: Mari throws her line to you; the patch kit takes 10 s instead of 6 s; crane and tug jobs have generous windows at ×1.0 units;
  - Dot's pilot can berth any ship.
- **4 players is the fastest but not trivial.** Ship windows and the rival's extra goon per 2 extra players (gdd/world) keep 4 busy.

---

## 8. The co-op economy (decided: one shared company bank)
**Decision (SUGGESTED, matching gdd/systems):** **one shared company bank.** There are no personal wallets.

Why: it's a family port, and everyone builds the same Landing. Personal wallets would split fishing from building (Stardew added separate money only as an option, and players describe shared money as better for big purchases). gdd/systems already designs it this way. If Manan wants personal wallets later, the Ledger already records who earned what, so a split option could be added without a redesign (as Stardew's 1.4 did).

| Rule | Detail |
|---|---|
| **Who can spend** | Host setting (gdd/systems): "Anyone" / "Ask the crew above $X" / "Host only". **Default: ask above $5,000 or 25% of the balance, whichever is lower** |
| **Ask the crew** | The buyer's phone sends a request. Any one other player's "OK" (or the host's) approves. 60 s with no answer approves it if nobody is online to ask. Declined → nothing is spent |
| **Ledger** (phone app) | Every dollar, with who, what, when. A daily summary at the night card: "Pip caught $1,240 · Rani unloaded $3,100 · Wages −$480". Friendly competition stats, not wallets |
| **Personal items** | Rods, clothes and tools are bought from the bank but **owned by the player who bought them**, stored in the world save under that player's id. Boats, buildings and the warehouse belong to the company |
| **Selling company assets** | Boats, piers and cranes: host only, or the host approves |
| **Late joiner** | Joins the host's port as it is. Gets a free starter kit (bamboo rod, 10 worms) not taken from the bank; gets their own items back if they've played this world before |
| **Leaving** | A leaving player's hotbar fish go into the warehouse (not lost, not taken) |
| **Payouts** | All income goes to the bank, including Mango cash laundering (the duffel itself is a shared object) |

---

## 9. Anti-grind rules (SUGGESTED, each one checkable)
1. **Next goal always visible.** The phone home screen and the shop card show "need $X more (~N min at your rate)".
2. **Cadence:** a small purchase every 10–15 min, a big one every 60–90 min solo, no stage longer than ~5 h solo (enforced in `check()`).
3. **Payback:** every income-producing purchase pays for itself before the end of the next stage. The shop card shows it.
   - First deckhand: under 1 h.
   - Kestrel: ~3 h.
   - Puffin + net: ~4 h.
   - Tern: ~5 h.
4. **Variety beats repetition.** Market saturation (−2% per same species per buyer per day) and thinning hotspots make a varied day pay more than one fish on repeat. Orders pay 150–200% for the right fish.
5. **Hands-on pays.** Anything a player does by hand that automation could do pays +30% (crane lifts, captaining). Trophy/Legend fish, the Derby, charters and salvage are player-only.
6. **No game over, no reset, no offline earnings.** Time runs only in session, so nobody has to leave the game running, and nobody idles a farm.
7. **The rival never takes more than 15% of cash in one hit**, and backs off when you're broke (gdd/world).
8. **Endless is optional.** The story's money covers all MUST items and most SUGGESTED ones; cosmetics and mega-ships are for those who want them.

---

## 10. Anti-cheese: save-scum, dupes and exploits (SUGGESTED; save mechanics owned by gdd/systems)

| Exploit | Fix |
|---|---|
| **Save-scum a rare fish or a grade** | Bites roll from a seed per (world seed, day, cast number), so reloading gives the same fish in the same order. Grade comes from timing skill, so there's nothing to reroll |
| **Reroll the job board, orders or ship offers by reloading** | All offers are seeded per (world seed, day). Reloading the morning shows the same three candidates and the same orders |
| **Reload after a bad event** (rival, storm) | Saves happen at 07:00 after sleep (gdd/systems). Rival actions are telegraphed a day ahead, so a reload brings the same event back |
| **Item dupe on disconnect** (co-op) | The host is authoritative for every item. Items are world entities with unique ids. A transfer is one host transaction: if the client drops mid-transfer, the item stays where the host says |
| **Double-sell race** (two players sell one stack) | Each sale is a host transaction with an id; the second one fails ("already sold") |
| **Carry items or money between worlds** | Inventories live in the world save, keyed by player id, never in a character file. Only cosmetics unlocked per account travel |
| **Two hosts with old copies** (forked saves) | Each copy carries the world id + day number. The lobby offers the newest-day copy by default and warns: "Rani's copy is Day 14, Pip's is Day 16". Nothing is carried between them, so forking can't dupe |
| **Hire/fire to farm traits or wages** | Firing costs 1 day's wage (severance). Rehiring the same person within 7 days costs double. The board refreshes only at 07:00, seeded |
| **Evict and re-invite moorers** | No sign-up bonus exists; fees are daily only |
| **Sleep spam** | You can only sleep from 20:00 to 05:00, or when Tired (gdd/systems). Wages, upkeep and rot advance with the skipped night, so skipping isn't free money |
| **Buy-back arbitrage** | Doyle buys at 60%. No shop ever buys above its sell price |
| **Dodge saturation by splitting sales across players** | Saturation is per buyer, not per player |
| **Worker spam on one spot** | Hotspots are shared and thin out after 6–10 catches; the catch is then the plain bite mix (no zones, Manan 31 Oct) |
| **Order filling by rebuying fish** | No shop sells fish. Mari and the cannery never sell back |
| **Ship-order unloading cheese** (double-touch containers) | A unit pays once, when it first lands on a yard tile, by unit id |
| **Co-op griefing** (spend everything) | Host spend rule, Ledger, host-only asset sales; Biscuit can't be sold |
| **Macro/AFK fishing** | Allowed: the minigame needs timing, and workers already automate the boring part. Market saturation caps the gain anyway |

---

## 11. Edge cases and what the player sees
- **Two players buy the last pack of squid at once:** the host decides; the second sees "Sold out", with no charge.
- **The bank can't cover wages at 07:00:** wages are paid in hiring order. Unpaid staff get an IOU toast: "Nils will work today on trust".
- **A worker's boat is still out when its owner (the player) wants it:** the phone shows "Kestrel: at sea with Nils, back ~17:00". Recall costs that shift's catch.
- **Selling a boat a worker is assigned to:** blocked: "Unassign Nils first."
- **Rot during a night skip:** fish age through the skipped hours (gdd/fishing). The night card warns: "3 fish go Stale tonight; sell at Mari's or ice them."
- **A ship order accepted, then the player quits:** the ship waits for the next session (its window freezes with world time).
- **Storm during a consignment:** −20% late fee, never a loss; the phone explains why.
- **Mango cash in the hotbar during a dock check:** it counts as a package (tier 2 risk), shown in the warning.
- **A co-op player wants to buy a $6,000 Jet Ski with the crew limit at $5,000:** an "Ask the crew" request. 60 s with nobody online to ask → approved.

---

## 12. Open questions for Manan (batched)
1. **Boat fish storage.** (Dock storage is now settled, Manan 31 Oct: the expandable warehouse, slot = 10 fish. This question is only about boats.) Your rule is "fish go in the inventory, NOT crates", and we keep it: there are no crates anywhere. But boats with a **fish box** (hold slots like a chest, opened on the boat) and a portable **cooler** would let workers bring back more than 20 fish. That gives a real reason to buy bigger boats for the crew. **Recommended: yes, from the Kestrel; Biscuit only with the $300 cooler.** If no: workers' catch caps at their own 8-slot hotbar (~30 fish), and bigger boats only add speed and comfort.
2. **Guns:** your hotbar mentions guns. What should they cost and do? Recommended (with gdd/world Q3): non-lethal and comic. Flare gun $80, net gun $1,200, harpoon gun $3,000 (faster Trophy fish), sold at June's counter; real guns never.
3. **Length:** the main path is ~17 h solo / ~13.5 h for 2 players, then Endless with mega-ships. Right, or do you want the empire phase longer before the vote?
4. **Loans:** add an Island Savings loan for big purchases? Recommended yes (§6).
5. **The cartel's cargo:** named as drugs or winking ("coconuts")? Shared with gdd/world Q4; it changes the Steam content survey.

## 13. Files
- `economy.md` — this file.
- `economy.xlsx` — sheets: Assumptions, Stages (live formulas: hours, totals, co-op), Curve (money over time per crew size), Workers (formula-driven), plus catalogues for Bait & Tackle, Clothes, Surf shop, Boats, Boat upgrades, Boat cosmetics, Dock shop, Staff jobs, Vendors, Ship orders, Consignments, Endless. Recalculated in Excel with 0 formula errors.
- `money-over-time.png`, `income-mix.png` — charts.
- `economy_model.py` — the one source. Run it to regenerate everything; it ends with `checks OK`.
