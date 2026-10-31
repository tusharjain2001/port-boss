# Port Boss: objective, loops and progression

For the recommended premise (A, "Win the Coast", see `story.md`). Numbers are **tuning starts**, not promises: they
are set against today's economy (fresh fish crate $15, trolley $120, store $250, cold store $600, forklift $350,
Concrete quay $1,500) and Manan's timing (full automation only at the finale, ~8–10 h hands-on).

## The objective in one line
**Win the coast's captains away from Marlowe, buy his port, unload the Gigantica, then flip the lever that makes your
port run itself.**

Two meters, both on the map, never only in a menu:
- **Crew tin** (money): pays for stages, tools, wages, and finally the buyout. Already on the HUD.
- **Captain flags** (the rival meter): 10 flag poles on your harbour office roof. Each captain you win raises their
  flag, and their boats dock at your berth instead of his. Marlowe's fireworks get rarer as flags go up.

---

## 1. Core loop (minute to minute)
```
 horn: a boat docks with its captain's countdown
   → grab crates (stack up to 5, wobble) / drive the forklift / swing the crane
   → get them off before the boat leaves (throw, trolley, pallet)
   → store them (rack, cold store, warehouse) or sell straight away (market, truck at the gate)
   → ka-ching on the crew tin
   ↺ meanwhile: Marlowe's wake, gulls, a second boat docks, weather turns, a friend goes in the sea
```
Rule from the design critic, kept: **players' hands are full the whole day.** Machines raise how much the crew can
handle; they don't empty their hands until the finale.

Each minute should hold one decision with a cost: carry 2 safe or 5 wobbly? throw it or walk it? sell fresh now or
rack it for the truck? rescue the floating crate or let the gull have it?

## 2. Session loop (one day, ~9 minutes)
| Phase | Length | What happens |
|---|---|---|
| Morning | 15 s | Gull FM: one news line + who's docking today. The noticeboard shows today's boats (flags) and **Dot's target for the day** (the quota-giver). Letters from overnight sit in the mailbox, optional. |
| Day | ~8 min | 2–4 boats depending on stage, overlapping from day 2. One "oh no" every 3–4 min: Marlowe's wake, a second horn, a gust, a truck at the gate with its own timer. |
| Dusk | 20 s | Last boat leaves; lamps come on. |
| Night card | ~1 min | Money, crates sold/lost, stars per boat → **captain trust changes shown as flag notches**. Tomorrow's boats. Shop (Tab). One line of story. |
| Timelapse | 3 s | Anything you bought builds itself on the map. |

A good group session = 5–8 days = 45–75 min. **The run saves at every night card**, and every crew member keeps a
copy of the shared port so anyone can host next time (needed; it doesn't exist yet).

## 3. Meta loop (days and weeks)
- **Dot's daily target** (the quota-giver that works in Lethal Company and R.E.P.O.): hit it for a bonus and a
  notch on today's captain; miss it and lose a notch. Never the save.
- **Win captains**: each captain has 3 trust notches. A boat unloaded with 3 stars gives +1; a boat that leaves with
  more than 40 % aboard gives −1. At 3 notches their flag goes up for good. At 0 a captain sails to Marlowe (his
  fireworks go off) and can be won back later with a second-chance job.
- **Build stages**: each stage changes the map and brings new captains, new cargo and a new machine.
- **Fill the buyout fund**: unlocked at 7 flags (Marlowe's bank letter); a jar on the office board fills from
  daily takings you choose to put in.
- **Between sessions**: "tomorrow's boats" and the next upgrade's "need $X more" are always visible, so a group
  quits on a promise, not on a finished task (the Stardew "one more day" pull).

---

## 4. Stages, unlocks, upgrades
Hours are hands-on for 2 players at a normal pace. Days at ~9 min each.

| Stage / act | Hours | Days | Build cost | New cargo | New machine | New captains | Workers / automation |
|---|---|---|---|---|---|---|---|
| 1 Fishing pier | 0–1 | 1–7 | start | fish (spoils) | hands, trolley $120, forklift $350 (day 3) | Mags, Bjorn (trial) | none |
| 2 Concrete quay | 1–2.5 | 8–17 | $1,500 (exists) | timber (2-person carry or forklift), fruit (cold store) | 2nd trolley, pallets, cold store | Old Tam, Okafor sisters | none |
| 3 Crane + warehouse | 2.5–4.5 | 18–31 | $8,000 | barrels, cars (drive off yourself) | jib crane (swinging hook), warehouse, gate trucks | Rosa | **workers from here**: max 3, dumb, flag-to-flag, daily wage. Pim first |
| 4 Container berth | 4.5–7 | 32–48 | $30,000 | containers (crane only) | harbour crane, container stacks, 2nd forklift | Lars, +2 | **automated lines, one at a time** (see §6) |
| 5 Mega port | 7–9 | 49–62 | $90,000 | everything, bigger ships | gantry crane, freighter berth | last 2 | lines for each cargo; buyout fund |
| Finale | ~9 | 1 long day | buyout $250,000 + 7 flags | the Gigantica | every machine at once | – | **full automation: the lever** |
| Endless | 9 + | – | – | odd jobs | – | – | port runs itself; crew does odd jobs |

**Upgrades inside a stage** (about 15 across the run, small list on purpose): trolley ×2, crate store 2/3
(exists), cold store (exists), forklift (exists) and a faster forklift, crane hook (magnet for barrels), warehouse
shelving, gate scale (trucks wait 30 s longer), tie-down posts, lifebuoys (faster rescue), a bigger rack per stage,
better gangplank (wider, less tilt), quay lights (night overtime), harbour office (flags + buyout board).

**Money sinks**
| Sink | When | Why it's there |
|---|---|---|
| Stage builds | each stage | the main goal |
| Tools and upgrades | always | short-term goals between stages |
| Worker wages | from stage 3 | daily cost; unpaid workers quit (a stake) |
| Repairs | always | forklift dunked = $100 tow; broken pier planks; a dropped container's fine |
| Automated lines | from stage 4 | build cost + small daily upkeep |
| Captain second-chance jobs | after losing one | costs a day of risk to win them back |
| Buyout fund | stage 5 | the end-game sink |
| Cosmetics | endless | port paint, crew outfits, horn sounds, Marlowe statue. Keeps money meaningful after the finale |

---

## 5. Win condition, fail states, stakes
**Win:** buy Marlowe's port (fund full + 7 of 10 flags) and finish the Gigantica day. Then the game continues in endless.

**There is no game over.** Co-op groups quit games that wipe shared progress (market critic). Every failure is
visible, funny and recoverable at a cost:

| Fail | What you see | Cost | Status |
|---|---|---|---|
| Boat leaves with cargo aboard | it sails off, captain shouting | lost pay, −1 captain trust | leaving exists; trust NEW |
| Captain's trust hits 0 | their boat sails to Marlowe; his fireworks | lost captain until a second-chance job | NEW |
| Crate in the sea | it floats; tow it to a ladder (exists) or lose it | the crate | exists |
| Crate knocked or dropped hard | red dots on its ring | −25 % per knock | exists |
| Fish spoils | goes green, flies | $4 instead of $15 | exists |
| Player in the sea | swim, stamina, fished out dazed | 10–20 s of your hands | exists |
| Forklift in the sea | floats 4 s, sinks; Dot tows it back next morning, or the crane fishes it out now | $100 or a crane job | NEW |
| Workers unpaid | they put their hats down and walk off | rehire fee | NEW |
| Broke | tin can't go below $0; Dot lends $200 against Grandpa's pier once a week | a debt line on the night card | NEW |
| Weather (random, no warning) | gusts shove untied crates; waves rock boats | lost crates | exists |

**Hard mode (optional, not default):** start with a bank loan, $2,000 by day 5 or the bank takes the trolley.

---

## 6. The "watch it run" payoff (late, as Manan decided)
The critics wanted it cut because it empties co-op hands. Manan kept it, later. This design keeps both:
**automation takes over a cargo line only after the crew has mastered it, and only so the crew can move on to a
bigger ship.**

- **Stage 3, workers (2.5 h+):** max 3. You place two flags; a worker carries crates between them, slowly, drops
  some in the rain, falls in sometimes. They take the dull 20 %, never the game.
- **Stage 4, automated lines, one at a time (4.5 h+):** a line is one cargo route you build physically: chute off
  the berth → roller belt → cold store → truck bay. The first line is fish, because by then the crew is bored of
  fish. Lines **jam** (a crate wedged, a gull asleep on the belt) and gusts knock crates off, so someone runs and
  kicks it. Each new line (timber, fruit, cars) frees hands for the next ship class, which is always more than the
  crew can quite handle.
- **Finale, the lever (~9 h):** after the Gigantica and the buyout, Dot hands you the key to the harbour office. A
  big red lever. Pull it: every line, crane and gantry starts at once, the camera rises out of the office window
  over the whole working port, the fireworks go off over *your* port, credits roll over it. That's the payoff, and
  it's earned.
- **Endless:** the port runs itself and pays daily. The crew's hands go to **odd jobs** that machines can't do (a
  grand piano, a crated circus, a yacht that mustn't get a scratch, a container of eggs), fixing jammed lines, and
  Marlowe's comeback schemes. A "hands-on" switch turns any line off for groups who want the old chaos.

---

## 7. Co-op roles without classes
No classes and no tablet player. **Roles come from scarce tools**, and they collapse the moment something goes wrong.

| Tool | How many | The role it creates |
|---|---|---|
| Hands | unlimited | carrier, thrower, catcher |
| Trolley | 1, then 2 | runner to the market / racks |
| Forklift | 1 until stage 4 | driver (the contested toy) |
| Crane | 1 | operator + a spotter shouting directions |
| Gate trucks | 1 at a time, own timer | gate runner pulling from storage |
| The sky | – | lookout: weather never warns, so someone keeps an eye on it |
| Two-person items | timber, piano, odd jobs | forced pairs (Moving Out's couch) |

What makes groups shout: one gangplank, two players, one carrying a 5-stack; a thrown crate nobody catches; the
forklift lifting a friend; a car swinging over three heads; "who left the cold store open"; "we can't take Lars's
ship with one forklift" / "yes we can".

Fairness: everyone's money goes into one crew tin (exists), and every player who was there gets the stars and
stats. Late joiners join the host's port as it is.

## 8. Solo scaling
Solo must be good on its own, or $6.99 reviews say "needs friends".

| Players | Cargo per boat | Boat time | Overlapping boats | Extras |
|---|---|---|---|---|
| 1 | ×0.6 | ×1.5 | from stage 2 only | **Pim arrives at stage 2** instead of 3 (solo only), as a dumb helper; forklift auto-parks at the berth |
| 2 | ×1.0 (baseline) | ×1.0 | from day 2 | – |
| 3 | ×1.4 | ×1.0 | from day 2, more often | – |
| 4 | ×1.8 | ×0.9 | always | Marlowe visits a bit more often |

Scale by cargo, clock and overlap, **never by map size**: crowding is the comedy. **2 players is the baseline and
every stage must be beatable by 2**: Crashout Crew's reviews say its later levels can't be beaten with two. Marlowe, gulls and weather hit
solo players the same.

## 9. Pacing check against 8–10 h
- 62 days × ~9 min ≈ 9.3 h to the lever, at 2 players and a normal pace. 4 good players get there sooner (~7–8 h),
  a solo player later (~10–11 h) because of smaller boats.
- Tuning knobs: stage build prices, cargo per boat, the 7-flag gate, the buyout price. If playtests run fast, raise
  stage 4–5 prices first; never lengthen stage 1–2 (the demo).
