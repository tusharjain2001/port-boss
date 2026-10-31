# PORT BOSS — Design Critic Review

Reviewer lens: co-op chaos (Overcooked, Moving Out, PEAK, R.E.P.O., Lethal Company) + progression sims (Supermarket Simulator, Stardew, PlateUp!, Factorio-lite).
Input: `friend-plan.md`. Constraints: online co-op 1–4, comedy chaos + cozy loop, ~$5, playable wishlist-able Steam demo in 3–4 months, built by AI agents, judged on virality / originality / value.

**Verdict up front: GO WITH CHANGES.** The loop is right, the progression fantasy is right, the setting is fresh (nobody owns "port chaos"). But the plan as written is a *management sim with a co-op line bolted on*. The fun is not yet on the screen second by second. Below is where it is, where it isn't, and what to change.

---

## 0. The one-sentence problem

> "By the end, you're standing in your office watching your port work without you. That's the payoff."

That is the payoff of a solo tycoon game. In a 4-player online chaos game it is the **failure state**: three friends with nothing to do. Every system below is judged against one rule:

**Players must have their hands full for the whole session. Machines raise the ceiling of what they can handle; they never empty their hands.**

PlateUp! is the proof: you automate the burger station so you can serve twice the customers, you never sit down. Overcooked never lets you sit either. Supermarket Simulator's "hire cashier" is the exact moment players stop playing it — look at its playtime curve.

---

## 1. Moment-to-moment fun

### 1.1 The first 10 minutes (as written)

| Time | What happens (plan) | Fun? | Why |
|---|---|---|---|
| 0:00–0:30 | You stand on a broken pier, $500 | Neutral | Needs a promise: visible rival port across the bay, a boat horn in the distance |
| 0:30–2:00 | Fishing boat arrives, you carry fish crates one at a time to a buyer | **Chore** | "1 box at a time, walk, drop, walk back" is Supermarket Simulator's shelf restocking: the part everyone mods out |
| 2:00–5:00 | Sell, get money | Neutral | Money is only fun if the number *does* something within 60 seconds |
| 5:00–8:00 | Buy hand trolley | Small high | The first upgrade is the hook; it must come at ~minute 4, not minute 10 |
| 8:00–10:00 | Second boat | ? | No tension yet unless the boat leaves without you |

As written the first 10 minutes is a walking sim. Fish sits on the deck; nothing moves unless you move; there is no clock, no failure, no joke.

### 1.2 The first 10 minutes (fixed)

Second-by-second version. This is the vertical slice.

**0:00** — Spawn on the pier. Across the bay: a huge gleaming port with a neon sign (the rival). Your pier has three missing planks and a seagull on the sign. A boat horn. A tiny fishing boat chugs in, rocks against the pier, the captain yells "Five minutes, I've got a tide to catch!" A timer appears over the boat.

**0:15** — You jump onto the boat deck. Fish crates are loose physics objects. You grab one (hold E). You grab a second — it stacks on top of the first. Third. The stack starts to wobble (Overcooked plate stack / PEAK backpack feel). Fourth and the stack sways hard; you have to walk slow.

**0:30** — The gangplank between boat and pier is a loose board. The boat rocks with the waves, so the plank tilts. You walk across with a 4-stack; the top crate slides off and splashes into the water. It floats. You can fish it out later with the net on the pier (5 seconds) or leave it (lost money, the seagull gets it).

**0:45** — You drop crates at the **buyer's table** at the end of the pier. The buyer weighs each crate, a cash register "ka-ching" and a +$12 pops. First dopamine hit in 45 seconds.

**1:30** — Timer at 2:00. Six crates left on deck. You start throwing crates (Moving Out) from the deck to the pier instead of walking. Some land, one bursts: fish everywhere, the pier is now slippery (you slide if you sprint on it).

**3:00** — Boat leaves. Any crates still aboard: gone, and the captain gives you 1 star ("slow"). The board shows $148 earned. Hand trolley costs $120. You buy it. Now you can push 5 crates at once, but the trolley has physics: push it too fast down the sloping pier and it rolls into the sea with the crates on it.

**4:00** — Second boat arrives *while* a fruit buyer is walking up the pier wanting 10 crates of fruit (you don't have fruit yet). Simultaneous demands = the Overcooked tension.

**6:00** — A rival speedboat does a doughnut in the bay; its wake rocks your boat, everything loose on the deck slides. First "oh no" that isn't your fault.

**8:00** — Night falls (day cycle, ~8 min day). Boats stop. The upgrade shop opens (Supermarket Sim's calm phase). You buy a second plank for the pier and a cold box. Timelapse: 3 seconds of your pier getting one new board. Day 2 begins.

Where the fun is, concretely: **physics stacking + a rocking boat + a timer + water as the universal punishment.** Every one of those produces a visible, laughable failure that is nobody's fault and everybody's fault.

### 1.3 Carrying boxes by hand: tedious or fun?

Tedious as written. Fun if ALL of these are true:

1. **Stacking**: carry 1 is slow and safe, carry 5 is fast and wobbly. Risk/reward decision every pickup. (PEAK's weight, Overcooked's plate stack, Moving Out's "two-hand big items".)
2. **Throwing**: anything you can carry you can throw. Throwing is faster than walking and is where 80% of the comedy comes from (crate hits friend, friend ragdolls).
3. **The ground is hostile**: rocking boat, tilting plank, missing planks, slippery fish, puddles, a trolley that rolls. The walk is never the same twice.
4. **Water**: drop it → it floats or sinks. Fish-it-out minigame or lose it. Every mistake is visible and recoverable at a cost.
5. **Cargo has personality**: fish crates are slippery and stink (green cloud as they spoil), fruit bruises when thrown (lower price), wood is a 2-person carry, a tyre bounces.
6. **The clock is on the boat, not on the HUD.** The captain shouts the countdown. The boat physically untethers and leaves with your cargo on it.

If the agents build only "pick up / put down / walk", kill the project. If they build 1–6, this alone is a demo.

### 1.4 Minute 60 session (fixed version)

You own: concrete dock, forklift, warehouse, cold room, a trolley. Three players online.

- A cargo ship with 12 pallets and a fruit boat arrive 90 seconds apart. Fruit spoils; pallets are heavy.
- Player A on the forklift. The forklift has real-ish physics: lift too high and turn fast → it tips. Forks stab a pallet wrong → boxes spill. Driving onto the gangplank at speed → forklift in the harbour (recoverable with the dock crane, 60 seconds, everyone laughs).
- Player B hand-carries fruit to the cold room because the forklift is busy. Fruit stacks wobble. Cold room door must be shut or everything inside spoils faster (one player always forgets).
- Player C is on the warehouse end: pulling the right pallets *out* for the truck that is waiting at the gate with its own timer (export side of the loop so storage is not a dead end).
- Minute 64: storm siren. 90 seconds. Loose crates on the pier will blow into the sea. Everyone drops their task and scrambles to tie things down / shove them in the shed. Ship captain refuses to leave in the storm, so the deadline pauses but the fish keeps rotting.
- Minute 68: night. Cash counted: $2,300. Shop: crane ($2,000) or 2nd forklift ($1,200)? Argument. Buy the crane. Timelapse: a crane arm rises over the dock.

Chore risks at minute 60 and fixes:
- Long walks between dock and warehouse → map is small; warehouse is 15 m from the dock, expansions add *more docks*, not more distance.
- Menus → no inventory menus. Storage is physical shelves. Selling is physical (truck, buyer table). The tablet is only the upgrade shop at night and the contract board.
- Waiting for ships → never wait. Ship schedule is driven by "when the dock is free + 20 s". Downtime only exists at night, and night is the shop.

---

## 2. Every system: keep / change / cut

| System | Verdict | Reason / change |
|---|---|---|
| Core loop (arrive → unload → store → sell → upgrade) | **KEEP** | Correct. Add the export side explicitly: trucks/buyer at the gate with their own timer, so "store" has an outflow and the warehouse is a sorting puzzle, not a bank. |
| Hands (1 box) | **CHANGE** | Hands = stack up to 4–5 with wobble + throw. "1 box" is the chore version. |
| Hand trolley | **KEEP** | Physics trolley: rolls, tips, can be ridden down the slope. Cheap, early (minute 4). |
| Forklift | **KEEP** | The star vehicle. Tipping, stabbing pallets wrong, harbour dunk. One forklift for a long time → it's the contested toy (Overcooked's single frying pan). |
| Small crane | **CHANGE** | Player-operated with swinging load (pendulum physics). Hits friends, hits ship, drops cars. Not a "click to unload" button. |
| Container crane | **KEEP (stage 4)** | Same physics, bigger. Fine as late content; out of first pass. |
| Hired workers | **CHANGE** | Workers are *dumb and single-task*: "carry from here to there" by placing two flags. They walk slowly, drop things in the rain, and quit if unpaid. They remove the boring 20 %, not the game. Overcooked has none; PlateUp! has machines not people. Max 2–3 workers ever. |
| Automation ("watch the port work") | **CUT as a payoff** | See §0. Replace with **infrastructure**: conveyor belts, chutes, ramps, roller tracks players place and feed. They jam, overflow, and break in storms (someone has to run and kick it). Automation moves the floor up so bigger ships become handleable, never removes players from the loop. The "boss in the office" becomes a 5-second end-of-day timelapse, not a play state. |
| Docks: pier → concrete → deep-water | **KEEP** | Visible progression, maps to stage. Keep 3. |
| Fish (spoils) | **KEEP** | Best cargo for chaos: slippery, stinks, timer. |
| Fruit (cold) | **KEEP, later** | Adds the "door left open" joke and bruising. Stage 2. |
| Wood (heavy) | **CHANGE** | Make it a **2-person carry** (Moving Out couch). It's the best co-op forcing item in the plan and the plan only uses it as "needs forklift". Both. |
| Cars (drive off) | **KEEP, later** | Pure comedy: 4 players, 4 cars, one narrow ramp, one harbour. Stage 3, strong trailer beat. |
| Containers | **KEEP, later** | Stage 4. Crane only. Money scale jump. |
| Storage: ground → shed → warehouse → cold | **CHANGE** | Keep tiers but storage is physical: shelves, racks, floor space. Boxes outside at night get stolen by seagulls / rained on. No "storage capacity: 50" number. |
| Side businesses (fuel, repair, restaurant) | **CUT for v1, maybe forever** | Each is a different game. Fuel station = one pump, one button, fine as a passive income upgrade later. Restaurant = Overcooked inside Port Boss, no. Repair yard only if it's "your own forklift is broken, fix it under pressure". Scope eaters with no chaos output. |
| Ship deadlines | **KEEP** | The core clock. Must be on the ship, diegetic, shouted by a captain. |
| Spoilage | **KEEP** | Visible: flies, green cloud, buyer holds nose, price ticks down on the crate itself. |
| Market prices | **CHANGE → simplify** | A price board with 2 buyers who want different things today is enough. "Store wood and sell when the price goes up" is a spreadsheet mechanic; nobody in a chaos game plays it. One daily "hot item" bonus does the same job. |
| Reputation stars | **CHANGE → contracts** | Stars are a hidden number. Replace with a **contract board**: pick which ships to accept tomorrow. Bigger ship = more money = more cargo than you can handle. Players choose their own stakes, and overreach is the comedy. Stars become the gate that unlocks bigger contracts. |
| Storm (20-min warning) | **CHANGE → 60–90 s warning** | A 20-minute warning is a sim pace; in a chaos game it is "ignore it for 18 minutes". Short warning, siren, wind physics shoving loose stuff off the pier, rain = slippery. Every 2–3 days. The storm is the trailer. |
| Rival port | **KEEP + make it active** | Not just a money target. The rival's speedboat wake, the rival poaching a ship from your contract board if you got <3 stars, the rival's port visibly growing across the bay. Cheap, recurring antagonist. |
| Port stages (5) | **KEEP** | Good. Demo = stage 1 + the first thing of stage 2. |
| Solo: "do everything, then hire workers" | **CHANGE** | See §3.4. |

---

## 3. Co-op

### 3.1 Do fixed roles (forklift / crane / tablet) work?

No. Two problems:

1. **The tablet role is the role nobody wants.** "Handles orders and sales on a tablet" is the manager in a 4-player game: the friend who is Alt-Tabbed to Discord within 10 minutes. There is no tablet role in Overcooked, Moving Out, PEAK, R.E.P.O. or Lethal Company. Everyone has hands.
2. **Fixed roles kill the scramble.** The best co-op moments are roles *collapsing*: the forklift driver dunks the forklift and now everyone hand-carries.

Fix: **no roles, only scarce tools.** One forklift, one crane, two trolleys, unlimited hands. Roles emerge from who grabbed what, and renegotiate the moment something goes wrong. The "tablet" is one physical contract board on the wall that anyone walks to at night.

### 3.2 Player count

| Players | How it plays | Scaling |
|---|---|---|
| 1 | Pressure game. One ship at a time, longer deadlines, smaller ships. One worker NPC from day 1 ("Uncle Raju" who follows and carries 2 crates, slowly, and falls in sometimes). | Deadlines ×1.6, cargo ×0.6 |
| 2 | Best cozy pairing. One on vehicle, one on hands; swap when it goes wrong. Two-person wood forces cooperation. | Baseline |
| 3 | Chaos starts. Three bodies on a narrow pier; a thrown crate finds a friend. Two ships overlap. | Cargo ×1.4, ships overlap |
| 4 | Full Overcooked. Four cars off one ramp. Someone is always in the water. | Cargo ×1.8, storms more frequent |

Scaling = cargo count + deadline + ship overlap. Never add map size; crowding is the comedy.

### 3.3 What creates shouting

- **The narrow gangplank**: two players, one plank, one is coming the other way with a 5-stack.
- **Throwing**: "CATCH" → didn't catch → fish everywhere.
- **The forklift**: one player drives, one is in the way. Forks lift a friend. Friend ragdolls off the dock.
- **The crane**: swinging a car over the pier where three people are standing.
- **The cold room door**: "WHO LEFT THE DOOR OPEN."
- **Two-person wood**: both players must walk in sync; one sprints, the log pivots, the other goes in the water.
- **Storm**: 90 seconds, everyone yells conflicting plans.
- **Contract night**: "we can't do a 40-pallet ship with one forklift" / "yes we can".
- **Ragdoll on hit**: any player struck by a thrown crate or a vehicle flops for 1.5 s. Cheap, infinite comedy (Gang Beasts, Moving Out).

### 3.4 Solo scaling

Solo must still be good or the $5 Steam reviews say "needs friends". Give the solo player:
- Smaller ships, longer timers (above).
- One free dumb worker from day 1, two more hireable. Workers are the solo player's co-op partners; they are deliberately a bit useless so the player is still the hero.
- The storm and the rival still hit.
Do not make solo "everything then hire workers" — that is the idle-game path again.

### 3.5 Online note that changes design

Physics over the network is the single biggest technical risk in a 3–4 month window. Design around it: **fewer, bigger cargo objects** (crates not fish, pallets not boxes), host-authoritative physics, a hard cap of ~60 live physics objects on the pier, ragdoll = local cosmetic. This is why "containers" and "pallets" scale well and "1,000 loose fish" does not. Tell the engineering critic.

---

## 4. Stakes and chaos: the "oh no" every 3–5 minutes

The plan's stakes are all slow (daily wages, 20-minute storm, weekly prices). A chaos game needs a fast clock and a slow clock.

**Slow clock (cozy):** money → upgrades → rival meter. Day/night. This keeps the "one more day" Stardew pull.

**Fast clock (chaos) — the "oh no" table, roughly one per 3–4 minutes of day time:**

| Event | Frequency | Warning | What you do |
|---|---|---|---|
| Ship horn: second ship arrives while the first isn't done | every day from day 3 | 20 s | Split up |
| Captain countdown "ONE MINUTE" | every ship | diegetic | Start throwing |
| Rival speedboat wake rocks the boat | every 2–3 min | 3 s | Grab what's sliding |
| Fish crate bursts → slippery pier | player-caused | none | Walk, don't sprint |
| Seagull swarm lands on uncovered fish | fish left out >60 s | caw | Shoo / cover |
| Forklift tips / in the harbour | player-caused | none | Crane it out, 60 s, hand-carry meanwhile |
| Storm | every 2–3 days | 90 s siren | Tie down, close doors, shove stuff in the shed |
| Cold room door left open | player-caused | hum stops | Someone runs |
| Truck at the gate with a 2-min timer wants 8 fruit | every day from day 4 | horn | Pull from storage |
| Inspector (later) | weekly | 30 s | Hide the rotten crate |

The rival as a stake: his port grows visibly when you fail a contract (he took it). That's a stake you can *see* without a menu. The loan (end goal B) works as **early** stakes only: "$2,000 due on day 5 or the bank takes the trolley" — one deadline that teaches the game, then it's over.

Plan's "no stakes" rejection is right. The fix is not *more* stakes, it is **faster** ones that are physical and visible.

---

## 5. FIRST PASS — the 3–4 month vertical slice

Goal: a 15–25 minute demo (2–3 in-game days), 1–4 players online, that puts the stack-wobble-throw-water-forklift loop on a Steam page with a trailer.

### 5.1 Must-have

1. One map: wooden pier + small yard + buyer's table + shed. Rival port visible across the bay (static mesh + sign).
2. Day/night: ~8 min day, night = shop + timelapse of the one thing you bought.
3. Character: walk, sprint, jump, grab, **stack up to 5 with wobble**, **throw**, ragdoll on hit. Swim + climb out of water.
4. Boat: arrives, rocks on waves, loose gangplank that tilts, captain with countdown + leaves with cargo still aboard.
5. Cargo: fish crates (spoil visibly, slippery when burst) and generic "goods" crates (safe money). Two types, one hazard.
6. Water: cargo floats/sinks, fish-out with a net at the pier edge.
7. Buyer's table: drop crate → ka-ching. Money number on the pier sign.
8. Shop at night: hand trolley (physics), 2–3 pier repairs (planks → fewer holes), shed (roof: fish spoils slower at night), **forklift as the big ticket on day 3**.
9. Forklift: drive, lift, tip, dunk, crane-out (or respawn at the gate after 60 s if crane is out of scope).
10. One "oh no": the rival speedboat wake, every 2–3 minutes. (Cheapest: a scripted boat path + boat tilt impulse.)
11. Contract board: at night pick tomorrow's boat from 2 options (small safe / big risky).
12. Online 1–4 via Steam (lobby, join by friend list), host-authoritative physics, player scaling (cargo count + timer).
13. End of demo: day 3 ends, "The rival sends you a letter: 'cute pier.' Wishlist to buy his port." → Steam page.

### 5.2 Explicitly leave out

- Crane, containers, cars, cold storage, warehouse, fruit, wood. (Trailer can show forklift + stack chaos; it is enough.)
- Workers, automation, conveyors.
- Side businesses, all three.
- Market prices, reputation stars (contract board covers the gating).
- Storm. Painful to cut, but it is a second weather + wind physics + tie-down system. Put it top of the post-pass list; if the slice ships early, add it before the trailer.
- Any menu that is not the night shop. No inventory screens.
- Cosmetics, character customisation beyond 4 colours.

### 5.3 The test

**Two players, headsets, day 1–2 of the demo, no instructions.** The game works if:
- They laugh out loud at least twice in the first 5 minutes (a crate in the water, a ragdoll, a plank slide).
- At the end of day 1, without prompting, somebody says "again" or "what's the forklift cost".
- Day 2's boat is unloaded faster than day 1's (the loop teaches itself).
- Nobody asks "what do I do now" during the day. (If they do: not enough cargo per player.)

If 1 and 2 fail, the physics layer is wrong, do not add systems to fix it.

### 5.4 Add after the first pass (priority order)

1. **Storm** (90 s siren, wind shove, rain slip, tie-down ropes). Trailer beat #1.
2. **Two-person wood** (forcing co-op item).
3. **Fruit + cold room door**.
4. **Export side: truck at the gate with a timer** (makes storage matter).
5. **Concrete dock + small player crane with pendulum load**.
6. **Cars drive-off** (stage 3, trailer beat #2).
7. **Workers (max 3, dumb, flag-to-flag)** + solo "Uncle".
8. **Infrastructure automation**: roller conveyor, chute, ramp. Jam/break rules.
9. **Rival as active antagonist**: poaches contracts, his port grows/shrinks with a visible meter.
10. **Containers + container crane + deep-water berth** (stage 4–5).
11. Daily "hot item" price bonus (the whole of the market system).
12. Inspector / seagull swarm / other event cards.
13. Fuel pump as a passive income upgrade (the only side business worth keeping).

### 5.5 Add ideas the plan is missing

- **Stack-carry with wobble + throwing.** The missing core verb. Without it there is no moment-to-moment game. (Why: every reference game's fun is in the carry, not the sell.)
- **The rocking boat + loose gangplank.** The level *moves*. Overcooked's moving kitchens are its best levels; a port gives you that for free with waves.
- **Water as the universal failure + fish-out net.** A mistake you can see from across the map and partly recover. (Why: recoverable failures = laughs; unrecoverable = rage-quit.)
- **Day/night with night = shop + 3-second timelapse.** Gives the cozy breath between chaos rounds and makes every upgrade *visible* on the map. Supermarket Sim's "the store grew" feeling, Stardew's "one more day".
- **Contract board (choose your own stakes).** Replaces reputation and solves "no stakes" without a loan timer dominating the whole game: players overreach because the big ship pays 3x.
- **Ragdoll on impact.** Cheapest comedy multiplier in the genre.
- **End-of-day stat card**: "Crates dropped in sea: 7. Friends forklifted: 2. Fish spoiled: 11." Share button. (Why: this is the virality mechanic; Lethal Company's quota screen and PEAK's death recap do the same job.)
- **Named captains with 3 lines each.** "Five minutes, I've got a tide to catch." Characters make clips. Zero mechanics cost.
- **Rival speedboat wake.** Recurring, cheap, blameless chaos tied to the end-goal villain.
- **Second-ship overlap as the main difficulty dial**, not bigger numbers.

---

## 6. The friend's questions

**End goal: A as the goal, B as the first 20 minutes, C as the finale.**
- A (rival) is the right *wall meter*: "Rival's port: $5,000,000" is a number you can show on the Steam page and on the map (his port literally across the bay). Keep it as the spine.
- B (loan) across the whole game is wrong for a cozy co-op: a global "lose everything" timer makes players quit instead of laugh, and nobody wants to be the one who lost the save. But one small loan on day 1 ("$2,000 by day 5 or the bank takes the trolley") is a perfect tutorial stake. Use it once.
- C (giant ship) is not a goal, it is a **mission**: the last contract on the board, the one you need the whole crew for. Make it the ending cinematic of A: you buy the rival's port, the world's biggest ship chooses you, final unload with everyone. Then free play.

**Setting: modern, but run-down.** The progression fantasy *is* hands → trolley → forklift → crane → containers, and three of those five do not exist in a sailing-ship era. The forklift is the single best chaos object in the game; lose it and you lose the trailer. Cars driving off a ship and a 40-foot container swinging over your friend are modern. Give it a 90s/2000s small-harbour look (rust, diesel, a tea stall) so it feels warm, not corporate. Sailing ships/horse carts = cosmetic "heritage pier" DLC later if people love it.

**Sand-digging vs Port Boss:** out of scope here, but on virality alone, "four idiots and a forklift" clips better than digging.

---

## 7. Scores

| Axis | As written | With the changes in this doc |
|---|---|---|
| Fun of the core loop (second-by-second) | 4 — walking sim with a shop | **8** — stack, wobble, throw, rocking boat, forklift |
| Co-op chaos | 4 — fixed roles, tablet role, idle payoff | **8** — scarce tools, narrow plank, 2-person items, ragdoll |
| Stakes | 5 — slow sim stakes, 20-min storms | **7** — diegetic ship clock, contract overreach, storm, rival wake |
| Replay / value for $5 | 6 — 5 stages, fine | **7** — contract choice, scaling by player count, stat card; needs 2+ maps post-launch for an 8 |

**Verdict: GO WITH CHANGES.**

The idea is sound and the setting is open ground. The changes are not scope additions; most of them *remove* systems (side businesses, market, reputation, automation-as-payoff, tablet role) and replace them with one physics layer that does all the work. Build the physics carry + rocking boat + forklift first. If two people laugh in five minutes, everything else in the plan is just content.

### Top 5 changes (in order)
1. Carry = stack with wobble + throw + ragdoll. Not "1 box at a time".
2. Cut "watch the port work" as the payoff. Automation raises the ship size; players never idle. Workers are dumb and capped at 3.
3. No fixed roles, no tablet role. Scarce tools (one forklift) make roles; a wall contract board replaces the tablet and reputation.
4. Fast stakes: diegetic ship countdown, storm with 90-second warning (not 20 min), rival speedboat wake every 2–3 min, second-ship overlap as the difficulty dial.
5. End goal = Rival (A) as the meter, one day-1 loan (B) as tutorial stakes, giant ship (C) as the finale mission. Modern run-down setting.
