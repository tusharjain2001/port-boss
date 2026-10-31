# Port Boss: feature list, tiered

For the recommended premise A (`story.md`) and the loops in `loops.md`. Status checked against the repo
`C:\Users\asus\Desktop\port-boss` main @ a6fe3d4 (code + `docs/HOW-TO-PLAY.md` + `HANDOVER-MANAN.md`) on 2026-10-31.

- **Status:** **EXISTS** = in the build today · **PARTIAL** = some of it exists · **NEW** = not built.
- **Effort** (agent work, incl. tests and Manan's look check): **S** ≤ 2 days · **M** 3–7 days · **L** 2+ weeks.
- **Why:** **fun** (minute-to-minute), **clip** (makes a 10-second shareable moment), **retention** (brings a group back).
- Rule of thumb: nothing is built in a tier until the tier above it is solid.

---

## Tier 1: DEMO / Next Fest
Goal: a 45–60 min demo (stages 1–2, acts 1–2) that ends on a hook. Most of it already exists; the new work is the
**story spine** (trust, flags, radio, letters), **save/continue** and **proximity voice**.
The market report also wants the demo's first day to stand alone and end on a ship leaving; this demo does.

| Feature | What | Why | Effort | Depends on | Status |
|---|---|---|---|---|---|
| Stack carry (5) + wobble | Carry up to 5, sways on start/stop/turn, topples when sprint-turning | fun | – | – | EXISTS |
| Throw | Throw the top crate; flying crates knock people over | fun, clip | – | carry | EXISTS |
| Knock-over | Hit by a crate or boat → tip over, lie, get up | fun, clip | – | – | EXISTS (animated, not ragdoll) |
| Swim, stamina, ladder, fish-out | Swim ~25 s, climb ladders, harbour master fishes you out | fun | – | – | EXISTS |
| Hand trolley | Held push, stops at walls, rolls into the sea off the slipway | fun, clip | – | – | EXISTS |
| Forklift + pallets | From day 3, $350 | fun, clip | – | – | EXISTS |
| Crate store, bigger store, cold store | Racks at the jetty foot, slower spoiling | fun | – | – | EXISTS |
| Freshness ring + spoiling | Price drains on the crate, green fish + flies | fun | – | – | EXISTS |
| Fish market across the road + lorries | Sell at the counter; lorries brake and honk | fun | – | – | EXISTS |
| Boats leave on a clock | Captain countdown; leaves with cargo; throws stowaways off | fun, clip | – | – | EXISTS |
| Overlapping boats | 2nd berth from day 2 | fun | – | – | EXISTS |
| Captains Mags + Bjorn | Take turns, shout countdowns | fun | – | – | EXISTS |
| Marlowe: drive-by soak, banner plane, fireworks | Recurring rival chaos | clip | – | – | EXISTS |
| Random weather, tie-downs | Calm → storm, no warning; F ties a crate down | fun, clip | – | – | EXISTS |
| Gulls | Steal from unwatched crates | fun | – | – | EXISTS |
| Shove | Push a mate, they drop their stack | fun, clip | – | – | EXISTS |
| Splash punch-in | Camera beat on a big splash, "wipeout" on 3 | clip | – | – | EXISTS |
| Old pier end sags | Overload tilts it; crates slide | clip | – | – | EXISTS (`PierSag`) |
| Night card, stars, Stats, Tab shop | Day summary + career stats | retention | – | – | EXISTS |
| Stage 2 Concrete quay | $1,500 build: shed, forklift yard, +30 % catch | retention | – | – | EXISTS |
| Intro flythrough | 44 s, skippable | story | S to rewrite | story pick | EXISTS (rewrite lines for the acts) |
| Character editor, settings, title menu | – | – | – | – | EXISTS (look being redone by the art workers) |
| Co-op: Steam lobby (app 480) + LAN | 1–4 players | – | S (test) | – | PARTIAL: Steam across two accounts never tested |
| **Save / continue a port** | Autosave at every night card; Continue on the title menu; **every crew member keeps a copy of the shared port**, so anyone can host it next time (host-only saves are a top complaint, market §3.6) | retention (an 8–10 h game can't live without it) | M | – | **NEW** |
| **Proximity voice** | Steam Voice through Facepunch (free; Dissonance is paid, so it's blocked until EA). Every big friendslop hit has it (market §3.1) | clip | M | Steam co-op | **NEW** |
| **Catch window** | A thrown crate can be caught by a mate facing it: a clear "caught" sound, and a big splash when it's missed. Makes the bucket brigade | fun, clip | S | throw | NEW |
| **Dot's daily target** | Chalked on the noticeboard each morning (the quota-giver, market §4). Hit it for a bonus and a captain notch | fun, story | S | noticeboard, trust | NEW |
| **2-player balance check** | Every stage beatable by 2 (Crashout Crew's "can't beat later levels with two" complaint) | retention | S | scaling | NEW |
| **Captain trust + flag poles** | 3 notches per captain on the night card; flags on the office roof; lost captains sail to Marlowe | retention, story | M | night card, captains | **NEW** |
| **Dot, the harbour master** | Give the fish-out voice a name, a face and a hut; she runs the radio room | story | S | – | NEW |
| **Gull FM radio** | Wall radio + ticker: morning news, who's docking, live reactions to splashes, wipeouts, boats leaving full | clip, story | M | event hooks (splash/wipeout exist) | NEW |
| **Mailbox letters** | Overnight letters on the night card: Marlowe's $1.50 offer, thank-yous, bills | story | S | night card | NEW |
| **Noticeboard** | Physical board: today's and tomorrow's boats by flag | fun, story | S | captains | NEW |
| **Old Tam + timber (2-person carry)** | 3rd captain; logs need two players or the forklift; sync or it pivots | fun, clip | M | carry netcode | NEW |
| **Ragdoll on hit** | Replace the tip-over with a short local ragdoll (cosmetic, client-side) | clip | M | knock-over | NEW |
| **Forklift dunk + tow** | Forklift floats 4 s, sinks; Dot tows it back, $100 | clip | S | forklift | NEW |
| **Set piece 1: the pier gives way** | When the quay is bought, the pier end drops with everything on it | clip | S | `PierSag`, stage build | NEW |
| **Café across the road reopens** | First "town comes back" state at stage 2 | story, retention | S | stage 2 | NEW |
| **Solo scaling** | Cargo ×0.6, time ×1.5, overlap later | fun | S | – | NEW (verify what exists) |
| **Shareable day card** | One key saves the night card as a PNG with the funny stats | clip | S | Stats | NEW |
| **Demo ending** | Act 2 ends: Pim at your gate with a box of his stuff, the crane silhouette, "Wishlist to find out what Marlowe does next" | retention (wishlists) | S | Pim model | NEW |

## Tier 2: EARLY ACCESS launch
Goal: stages 1–4, acts 1–4, ~6–7 h of play, workers and the first automated line. The finale follows in EA update 1
(pull it into launch if the date allows: the buyout is the store-page promise).

| Feature | What | Why | Effort | Depends on | Status |
|---|---|---|---|---|---|
| Stage 3 Crane + warehouse | Build at $8,000; map grows into the yard | retention | L | stage system | NEW (HUD goal only) |
| Jib crane, swinging hook | Player-operated, pendulum load; a spotter helps | fun, clip | L | networked physics test | NEW |
| Warehouse | Physical shelves, no capacity number | fun | M | stage 3 | NEW |
| Gate trucks with a timer | Trucks wait at the gate for specific crates; the export side | fun | M | warehouse | PARTIAL (lorries exist as traffic only) |
| Barrels | Roll when dropped; crane magnet | fun, clip | S | crane | NEW |
| Cars: drive them off | Rosa's carrier; one narrow ramp | clip | M | vehicle code (forklift) | NEW |
| Fruit + cold-store door | Okafor sisters; door left open = spoils | fun | S | cold store | PARTIAL (cold store exists) |
| Workers (max 3) | Flag-to-flag carriers, daily wage, quit if unpaid, fall in sometimes | fun | L | NPC walking (dock workers walk today) | NEW |
| Pim as worker #1 + set piece 2 | His first job: forklift off the quay | clip, story | S | workers | NEW |
| Stage 4 Container berth | Build at $30,000 | retention | L | stage 3 | NEW |
| Harbour crane + containers | Containers crane-only; stacks can topple | fun, clip | L | jib crane | NEW |
| First automated line | Chute → belt → store → truck bay for one cargo; jams, gull naps on it | fun, retention | L | stage 4, warehouse, trucks | NEW |
| Captain roster to 10 | Lars, Rosa, Okafors, Tam + 2–3 more; one line each, own boat colours | story | M | trust system | NEW |
| Marlowe escalation | Bribery letter, one loyalty-test day, bigger wakes per act | story | S | trust, letters | NEW |
| Rejoin / join in progress | Drop and come back to the host's port (a top complaint against Crashout Crew) | retention | M | save | NEW |
| Achievements (~15) | Funny ones ("Fishing for compliments": 50 crates fished out) | retention | S | Steam app | NEW |
| Town states at stages 3–4 | More shops reopen across the road | story | M | – | NEW |

## Tier 3: EA updates
| Update | Feature | What | Why | Effort | Depends on | Status |
|---|---|---|---|---|---|---|
| U1 | Stage 5 Mega port | The Toon Harbor demo-scale harbour, ~47k renderers: needs batching/LOD | retention | L | stage 4 | NEW |
| U1 | Gantry crane + freighter berth | Biggest machine | fun, clip | L | harbour crane | NEW |
| U1 | Buyout fund + Marlowe's bank letter | Jar on the office board; 7-flag gate | story | S | flags | NEW |
| U1 | **Gigantica finale** (set piece 3) | One long day, all machines, bow wave floods the quay | clip, story | L | stage 5 | NEW |
| U1 | **The lever: full automation** | All lines run, camera rise, fireworks over your port, credits | story (the payoff) | M | lines | NEW |
| U2 | Endless: odd jobs | Piano, circus, eggs, the yacht; machines can't do them | retention | M (+S each) | finale | NEW |
| U2 | Hands-on switch | Turn any line off | retention | S | lines | NEW |
| U2 | Cosmetic shop | Port paint, outfits, horns, a Marlowe statue | retention (money sink) | M | character editor | PARTIAL (editor exists) |
| U2 | Marlowe's comeback schemes | From a dinghy | story, clip | S | – | NEW |
| U3 | Second berth: refit Marlowe's old port | New map from the pack, same systems | retention | L | stage 5 perf | NEW |
| any | Daily modifiers | Opt-in "greased deck", "double gulls" for a pay bonus (ideas shortlist #1) | retention | S | – | NEW |
| any | Special crates | Live fish crate squirms, glass crate shatters (shortlist #2) | fun, clip | S each | – | NEW |
| any | Hard mode: the loan | $2,000 by day 5 or lose the trolley | retention | S | save | NEW |
| monthly | Optional mystery beats | One per update: a ticking crate in a manifest, a radio voice at night, a shape under the pier. Found, never forced (market §4) | retention | S each | radio, letters | NEW |
| any | Photo mode | Free camera, hide HUD | clip | M | – | NEW |

EA rules from the market report: a **monthly** visible update, a public roadmap **without dates**, **no DLC during EA**.

## Tier 4: 1.0
| Feature | What | Why | Effort | Depends on | Status |
|---|---|---|---|---|---|
| Pacing pass to 8–10 h | Re-tune prices, cargo, flag gate from EA data | retention | M | all stages | NEW |
| Stage 5 performance | 60 fps at High on the mega port | – | L | stage 5 | NEW |
| Localisation | EN + Simplified Chinese (+ others by EA data) | reach | M | text frozen | NEW |
| Steam Deck / full gamepad | Verified, every screen | reach | M | – | PARTIAL (gamepad bindings exist) |
| Accessibility | Subtitles for blips, colour-blind rings, hold/toggle options | reach | M | – | NEW |
| More captains, odd jobs, letters | Content from EA feedback | retention | M | – | NEW |
| Spoken radio/captain lines | Only if Manan OKs voice (question 2 in README) | clip | M | Manan | NEW |

---

## The most clip-able physics moments (ranked)
| # | Moment | Status |
|---|---|---|
| 1 | **Boat leaves with a friend aboard**: captain throws them over the side as the gap opens | EXISTS (`Boat.Evict`) |
| 2 | **5-stack on a rocking gangplank**: top crates slide off one by one, splash, punch-in | EXISTS (stack + splash beat) |
| 3 | **Marlowe's drive-by**: a wall of spray, then he laughs and points | EXISTS |
| 4 | **The pier end gives way** with crates, trolley and people on it | PARTIAL (sag exists, full drop NEW) |
| 5 | **Forklift off the quay**, floating 4 seconds with its driver, then sinking slowly with bubbles | NEW (forklift exists) |
| 6 | **Crane swing**: a car or barrel on a pendulum hook sweeps three friends into the harbour | NEW |
| 7 | **Two-person log pivots**: one sprints, the other is swung into the sea | NEW |
| 8 | **Untied crates in a storm gust** go off the quay like dominoes | EXISTS (weather + gusts) |
| 9 | **Four cars, one ramp**: Rosa's carrier unloads in a race | NEW |
| 10 | **Bucket brigade, missed**: boat → friend → quay, the last catch fumbles, splash, a knock on the ring | PARTIAL (throw exists; catch window NEW) |

Market rule for all of them (`../market/market.md` §6): readable in under 10 s, from one camera, **without the HUD**,
with a friend's voice on top. #1, #2, #5, #8 and #10 are the market report's five.

Gull FM's live line on each of these ("…and the forklift is swimming") is what turns a funny moment into a clip with
a caption.
