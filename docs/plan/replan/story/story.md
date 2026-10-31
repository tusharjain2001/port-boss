# Port Boss: three story premises

Written 2026-10-31 by pb-story for the re-plan. Fixed by Manan's earlier decisions, so every premise keeps them:
modern, stylised, run-down harbour that grows; ships physically leave on a clock; stack/wobble/throw, ragdoll, water;
**rival buyout + giant-ship finale, then endless**; the "watch it run" automation payoff arrives late (8–10 h);
**weather is random and never warns** (so no story beat can schedule a storm); co-op 1–4, light and funny.

## What the game has today (so we know what "no story" means)
- A 44 s intro (`Intro.cs`): "This whole coast belongs to one man: MARLOWE… That little pier was Grandpa's."
- Marlowe's drive-by soaking, 5 banner-plane insults, fireworks over Marlowe Port 600 m across the sea.
- Captains Mags (steady) and Bjorn (fast, +50 % crates) take turns and shout countdowns.
- The harbour master fishes you out when your stamina runs out (she has no name or face).

That's a setting and a villain. There is **no arc**: nothing changes between day 2 and day 20 except money, no NPC
wants anything, and nothing tells you what you're winning. The fix below gives the story a spine you can **see on the
map** (who docks where, what the town looks like) rather than read in cutscenes.

## How story is delivered (shared by all three, no cutscene budget)
Rule from the market report (`../market/market.md` §4): **a frame, not a plot.** 11 of 13 co-op hits use only a
quota-giver or a one-line premise. Every channel below can be ignored and the game still makes sense: no dialogue
trees, no forced reading, nothing that stops play.
| Channel | What it is | Cost |
|---|---|---|
| **Captain shouts** | Speech bubbles + gibberish voice blips (Animal Crossing style, synthesised, free). Already in for Mags/Bjorn. | exists, S to extend |
| **Harbour radio** | A small radio on the shelter wall + HUD ticker. A DJ reads news between songs, and reacts live to what just happened ("crate number four, into the sea"). Text + blips; real voice only if Manan OKs it later. | M |
| **Letters & notes** | A mailbox at the gate. Letters arrive overnight (rival offers, captain thank-yous, bills). Read on the night card. | S |
| **The noticeboard** | Physical board by the shed: today's ships, tomorrow's ships, pinned jobs. Replaces any menu. | S |
| **The map changes** | Boats with their owner's flag dock at *your* berth instead of his. Shops across the road reopen. His port's fireworks stop. Your sign gets new letters. | M per stage |
| **Banner plane** | His insults, plus one new banner per act. | exists, S |
| **Night card verdict line** | One line of story per day ("Mags says the boys at the café are talking about you.") | exists, S |

---

## Premise A: "Win the Coast" (take the captains back from Marlowe)

**Pitch.** Marlowe owns every working berth on the coast of Saltmouth, and every captain docks at Marlowe Port
because there's nowhere else to go. Except Grandpa's shabby fish pier, which he left to you and your mates. Marlowe
pays captains peanuts and treats them like dirt, so they're one good shift away from switching. Every ship you
unload well wins a captain's trust; every ship that leaves with cargo still aboard sends them back to him. Win
enough captains and Marlowe's port starts to go quiet, his fireworks fizzle, his bank calls, and the coast's
biggest ship ever, the *MV Gigantica*, has to choose between you. Unload her, buy Marlowe out, and flip the big lever
that makes the whole port run itself. The story is the **coast-share flag count**: a row of captain flags on your
harbour office, filling up one flag at a time.

**Cast**
| Name | Role |
|---|---|
| **Marlowe** | Rival tycoon. Gold-braided captain's hat, speedboat, banner plane, fireworks for every ship. Loud, petty, never actually evil. |
| **Captain Mags** | Grandpa's old friend, the only captain who still docks with you on day 1. Steady boats, warm, believes in you. (exists) |
| **Captain Bjorn** | Fast, grumpy, huge catches, 90 seconds and not one more. The first captain you have to *win*. (exists) |
| **Dot** | Harbour master and the voice of the radio-room: fishes you out of the sea, reads the tide, dry as a biscuit. Gives the harbour master a face. |
| **Pim** | Marlowe's nervous assistant. Fired at the end of Act 2 for losing Bjorn to you; becomes your first hired worker: loyal, useless, lovable. |
| **Gully** | DJ on Gull FM, the coast's one radio station. Gossip, weather-free "news", and live commentary on your disasters. |

Later captains (one line each, each brings a cargo type): **Rosa** (car carrier), **Lars** (container feeder),
**the Okafor sisters** (fruit reefer), **Old Tam** (timber barge).

**Acts across the run** (stage = act; hours are hands-on for a 2-player group, see `loops.md`)
| Act | Stage | Story beats | Ends when |
|---|---|---|---|
| 1. *The Pier* (0–1 h) | Fishing pier | Mags only. Marlowe's $1.50 cash offer arrives in the mailbox on day 2. Gully mocks you on air. The old pier end groans under load. | You build the Concrete quay. Set piece 1. |
| 2. *The Quay* (1–2.5 h) | Concrete quay | Bjorn tries you out ("90 seconds, kid"). Win him and his flag goes up on your office; Marlowe's speedboat visits get nastier (bigger wake). Old Tam's timber and the Okafors' fruit arrive. The café across the road reopens. | Bjorn's flag goes up for good → Marlowe fires Pim, Pim turns up at your gate with a box of his stuff. |
| 3. *Crane & Warehouse* (2.5–4.5 h) | Crane + warehouse | Pim is worker #1 (set piece 2). Trucks queue at the gate. Rosa's car carrier. Marlowe tries bribery: a letter offering captains double pay, and one captain wavers (a loyalty test day). | 5 of 10 captains on your side. |
| 4. *The Berth* (4.5–7 h) | Container berth | Lars's feeder ship and containers. First automated line. Gull FM runs "Marlowe Port: is the party over?". His fireworks now come every *other* ship. | 7 of 10 captains. Marlowe's bank letter: he has to sell. |
| 5. *The Big One* (7–9 h) | Mega port | The buyout fund fills on the office board. The *Gigantica* was booked at Marlowe's; the captains vouch for you and she turns towards your berth. | Finale. |
| Finale (~9 h) | – | Gigantica day (set piece 3), then Marlowe signs on your quay. He keeps the speedboat and becomes your harbour tour guide. The fireworks now go off over **your** port. You pull the big lever: every line runs, the camera rises over the harbour, credits over your port working. | – |
| Endless | – | Port runs itself. You take **odd jobs** by hand (a grand piano, a circus, a yacht that must not get a scratch), Marlowe's comeback schemes from a dinghy, and you can reopen his old port as a second berth (EA update). | – |

**Set pieces built for clips**
1. **The old pier gives way.** The night you buy the Concrete quay, the last day's overloaded pier end (`PierSag`
   already tilts it) finally drops: crates, trolley and anyone standing on it slide into the sea in slow motion, then
   the timelapse builds the quay. Hold the shot for 3 s, the splash punch-in already exists.
2. **Pim's first day.** Pim's first job is to bring the forklift over. He drives it gently, carefully, beautifully,
   straight off the end of the quay. Gully: "And Pim is in the harbour. Pim is fine." Teaches that workers are dumb
   and that forklifts float for exactly four seconds.
3. **Gigantica.** A ship as long as the whole quay. Her horn rattles the windows across the road. Her bow wave floods
   the quay apron, every loose crate floats, and all four players work every machine at once on one giant
   countdown while Marlowe watches from his speedboat with binoculars.

---

## Premise B: "Port Wars" (a radio-broadcast port league)

**Pitch.** Every year the ports of the coast compete in the Harbour League, broadcast live on Gull FM. The rule
everyone signed in 1962: at the end of the season, the bottom port is sold to the champion. Your crew's port came
last last year and it has one season to climb. The champion is **Rex Gantry** of Gantry Global, seven titles,
perfect hair, enters every match by helicopter. Each stage is a division; each division ends on a **Match Day**:
one scored ship, both ports on the same clock, two commentators losing their minds over every crate that hits the
water. The final is the *Gigantica*: whoever unloads her better takes the other's port.

**Cast**
| Name | Role |
|---|---|
| **Rex Gantry** | Champion and rival. Helicopter entrances, slow-motion hair, catchphrase "Containerised." |
| **Barry & Bev** | Gull FM commentators. Play-by-play over your actual events; they argue with each other. |
| **Commissioner Haddock** | League official. Announces each division's "rule of the week" (greased deck, one-hand carry, double gulls). |
| **Captain Mags** | The only captain who'll still sail for a bottom-of-the-table port. |
| **Tiff from Clammo** | Sponsor rep. Clammo Energy pays you to do silly things on air ("throw 3 crates on Match Day"). |

**Acts:** Division 4 (pier) → Division 3 (quay) → Division 2 (crane + warehouse) → Division 1 (container berth) →
Premier (mega port) → League Final vs Rex (Gigantica) → endless = defend the title, each new season with new
Commissioner rules and a new challenger port.

**Set pieces**
1. **Instant replay.** A big wipeout triggers Barry: "Let's see that again!" and the game re-plays the last 4 s from
   a TV angle with a scoreboard graphic. A built-in clip machine.
2. **Rex's helicopter.** He lands on *your* quay on Match Day; the downdraft blows every untied crate off the edge.
3. **Match Day crowd.** Fans line the quay wall, cheer a catch and groan at a splash; one fan always falls in.

**Strengths:** the commentary is the strongest clip engine of the three; seasons give endless mode a natural shape;
Commissioner rules re-use the shortlisted "hazard modifier" idea. **Weaknesses:** it reads as level-based
competition (Crashout Crew's "repetitive after 10 levels" risk); "loser is sold" is harsh; the cozy half of the
game (the port growing as *home*) gets weaker; scoring two ports needs a simulated rival score.

---

## Premise C: "Lifeline" (an island town that comes back to life)

**Pitch.** The ferry stopped calling at Gullhaven Island three years ago. Shops closed, the school shut, the young
left. The only thing still standing is the old cargo berth, and you and your mates have moved over to reopen it.
Every ship you unload brings what the island needs: flour for the bakery, parts for the generator, glass for the
school windows. As the cargo lands, **the town across the road comes back**: lights on, shutters up, people in the
street. Across the bay, resort developer **Vivienne Sterling** is building a glossy yacht marina and wants your berth
to fail so she can buy the island for nothing. The finale: the *Gigantica*, caught by bad weather, needs a deep-water
berth, and only yours is ready. Her shipping line signs with you, Sterling's marina goes bust, and you buy it.

**Cast**
| Name | Role |
|---|---|
| **Vivienne Sterling** | Rival developer. Billboards, a white yacht, legal letters. Polite and ruthless. |
| **Nan Morwenna** | Oldest islander, runs the café, pins the orders on the noticeboard. The heart of the island. |
| **Ferryman Joe** | First captain, back after three years. Brings the first crates. |
| **Kip** | Ten-year-old who runs messages, keeps score, and fishes you out with a boat hook. |
| **Mr Fennick** | Sterling's lawyer. Serves notices on the quay, usually while standing exactly where a crate lands. |
| **Dr Ade** | Island doctor. Medicine crates are fragile and urgent. |

**Acts:** Pier (bakery + café reopen) → Quay (generator: lights come back at dusk) → Crane + warehouse (school,
trucks to the far village) → Container berth (houses rebuilt, Sterling's marina half-empty) → Mega port (festival) →
Gigantica + buy the marina → endless = the island keeps growing, seasonal festivals.

**Set pieces**
1. **Lights on.** The generator parts land; at dusk the whole island switches on street by street. The cozy clip.
2. **The piano.** A two-person, fragile delivery to the pub, across a rocking gangplank. Nan is watching.
3. **Gigantica in the harbour mouth.** She barely fits; tugs nudge her in while the crew clears the quay.

**Strengths:** warmest premise; the "town comes back" is the most visible progress meter; deliveries give each
cargo a reason. **Weaknesses:** weakest villain (paperwork, not wakes); most art per stage (every town building
needs a shut and an open state); bespoke orders pull towards Supermarket-sim chores; less clip energy; and
"rebuild the town" is close to Docked's "rebuild the family port" story, which reviewers called intrusive.

---

## Recommendation: Premise A, with one idea taken from each of the others
**A "Win the Coast"**, kept as a *light frame* (see the market check below), plus **Gull FM's live commentary** (from B) and **the town across the road reopening** (from C).

Why:
1. **It's mostly built already.** Marlowe, Marlowe Port across the sea, the drive-by, banner plane, fireworks, Mags,
   Bjorn, the harbour master's fish-out and the intro all exist. A needs names, a flag board, letters and a radio;
   B and C need new rivals, new framing, and (C) a whole town's worth of art states.
2. **The rival meter is physical.** Captains' flags, which berth their boats sail to, and Marlowe's fireworks
   fading are all on screen. No spreadsheet. This is the "stakes you can see without a menu" the design critic asked for.
3. **Fail states get story weight without game-over.** A ship leaving with cargo costs that captain's trust, and a
   lost captain sails to Marlowe and sets off his fireworks. Painful, funny, recoverable.
4. **It fits the decided ending exactly:** rival buyout + Gigantica + automation lever + endless odd jobs.
5. **One-line Steam pitch:** *"Steal the coast from a tycoon, one captain at a time. Co-op chaos for 1–4 dock workers."*

### Market check (pb-market's `market.md`, 31 Oct 2026) and what it changed here
- **Light frame wins; heavy story hurt the closest setting comp.** Docked's "rebuild the family port after a
  hurricane" campaign was called intrusive. So: Grandpa is **one intro line**, not a theme (premise renamed from
  "Grandpa's Pier"); captains speak in **shouts only**; letters and radio are optional flavour.
- **Quota-giver works** (Lethal Company's Company, R.E.P.O.'s Taxman). Dot, the harbour master, chalks the day's
  target on the noticeboard each morning. Hitting it earns a bonus and a captain notch; missing it costs a notch,
  never the save.
- **Rival works** (Joja, Schedule I's Benzies): Marlowe Port is already the visible goal in every shot.
- **Optional mystery** (PowerWash texts, Dredge bottles) works as **EA update fuel**: a ticking crate in a manifest,
  a radio voice at night, a shape under the pier. One beat per monthly update, found and never forced. Added to
  `features.md`.
- **Trailer order:** open on "the sea doesn't wait" (a ship leaving with a friend aboard); the coast story is the
  second half ("from rotten jetty to mega port"). The story sells the 8–10 h, the chaos sells the clip.

Risk: "underdog vs tycoon" is familiar. The captains, Gully's commentary and Marlowe ending up as your tour guide
are what make it ours.
