# Port Boss GDD: WORLD + STORY

Worker gdd-world (task t-c63bf86b), 2026-10-31. Covers the story, the rival system, the cast, the phone as the story channel, the map, a 60-minute script, later content lines and the living world.

Tags: **MUST** = Manan's requirement (from `../BRIEF.md` / his notes). **SUGGESTED** = ours, open to change. **OPEN QUESTION** = only Manan can answer (all batched in §0.3).

Kept consistent with the other sections: one in-game day = 24 real minutes (1 real s = 1 in-game min), active window 07:00–23:00, sleep skips to 07:00 (gdd-systems); the 8 fishing zones and the legend fish come from `../fishing/fish-roster.csv` (gdd-fishing); boat tiers move at 6 / 9 / 11 / 16 m/s (agreed with all three sections).

---

## 0. One-page summary

### 0.1 The story in five sentences
1. **Walt Hale** once ran **Hale's Landing**, the busiest port on **Saltmouth Island**. Freighters reached it through **Walt's Cut**, a deep channel from the west coast to the north bay *(Kufu review 31 Oct)*.
2. Walt's apprentice **Adrian Voss** (Walt took him in at fifteen) came back with mainland money. He built **Voss Harbour** on the far side of the island, bought the island's harbour charter, and laid a low road causeway across Walt's Cut. The Cut silted up, the ships stopped coming, and Hale's Landing died.
3. Walt is 81 now, in a care home on the mainland after a fall, and the Landing has stood empty for three years. He sends you the keys, the leaky skiff **Biscuit**, and his old journal.
4. You fish, and a taco vendor pays to moor at your rotten pier. Moorers, buyers and workers follow, and the Landing wakes up. Voss notices you and sends his men, and the fight for the island's harbour charter begins.
5. It ends in a storm, a choice and a vote. You reopen Walt's Cut, decide what kind of port boss you are, and Walt comes home on the ferry to see it. Then the game goes on endlessly.

### 0.2 Key decisions made in this section
| # | Decision | Tag | Why |
|---|---|---|---|
| D1 | The rival is **Adrian Voss**. **Marlowe** stays on as his fixer | SUGGESTED | See §3.1. "Voss" is one hard syllable that reads on a crane at 500 m and rhymes with the game's title ("Boss vs Voss"). It is also Tushar's Codex name. Marlowe belongs to the old, replaced game; as the fixer he keeps the familiar name and becomes the man who does the dirty work Voss pretends not to see |
| D2 | **Grandpa is alive**, in a mainland care home. He talks to us through texts, calls, letters and his journal, and he comes home in the finale | SUGGESTED / **OPEN Q1** | A living grandpa gives warm, recurring phone contact and a finale payoff (he steps off the ferry and sees the port). A ghost clashes with the tone, and a dead grandpa leaves only one-way letters |
| D3 | The story only ever arrives through the **phone, NPCs, letters, Walt's journal and the changing world**. No cutscene takes control longer than 6 s, and nothing blocks play | MUST (light-frame lesson) | `../../replan/market/market.md` §4: 11 of 13 co-op hits use a light frame, and reviewers called Docked's family-port story intrusive |
| D4 | The rival is a **Heat** system with 6 tiers. Every action is telegraphed, has counterplay and can be recovered from. The goons' threat is the first hostile act | MUST (goons first) + SUGGESTED (system) | §3.3 |
| D5 | **Map B, Crescent Bay** *(Manan 31 Oct)*. Our dock deep inside the calm west-facing bay; Voss Harbour on the outer east coast behind Saltback Ridge; Walt's Cut through the south horn; a 5 × 5 km sea. (Map A was the first draft's pick, not chosen) | **MUST** (map B) | §8.0 |
| D6 | The endgame centres on **reopening Walt's Cut** and the **Harbour Charter vote**, with 3 endings and a dark cartel variant | SUGGESTED | It gives the whole game one visible physical goal on the map |
| D7 | **Moorers**: every vendor who rents a berth brings a small perk (ice, repairs, gossip). This is how "build from there" grows out of the first income | MUST (first vendor) + SUGGESTED (perks) | §6.3 |

### 0.3 Open questions for Manan (batched)
1. **Grandpa: alive (recommended), passed away (letters and journal only), or a ghost?** Alive gives calls plus the finale on the ferry.
2. **Rival name:** Adrian Voss (recommended) or Marlowe as the rival? If Voss, is Marlowe as his fixer OK?
3. **Violence level against the goons:** shove them off the pier, flare gun, net gun (recommended: comedic and non-lethal), or real guns? Manan's hotbar mentions guns. We suggest guns stay non-lethal or comedic until the cartel line, and that even there they shoot at boats and crates, never people (§10.1).
4. **The cartel line:** name the cargo as drugs, as Schedule I does, or keep it winking ("coconuts", "product")? This affects the Steam content survey and the age rating.
5. **Who the co-op friends are in the story:** grandkids/cousins, or "the grandkid's crew"? We recommend the host is Walt's grandkid and the others are "the crew"; Walt adopts every one of them by text.
6. **Seasons:** do we want a 28-day year (4 seasons × 7 days, about 2.8 h per season), or no seasons in EA? We recommend seasons in an EA update, not at launch.
7. ~~Map~~ **Answered *(Manan 31 Oct)*: map B, Crescent Bay**, as first drawn.

---

## 1. Tone and how the story is delivered

**Tone (SUGGESTED):** a warm coastal comedy with a real heart. Think *Stardew's grandpa letter* crossed with *Schedule I's phone calls*: funny people, one sincere thread (Walt and Adrian), and stakes that are never grim. Voss is a petty, polished man with a wound, not a monster. The cartel is "evil-funny" (MUST).

**Delivery channels (MUST: the story never blocks play).**
| Channel | What it carries | Rule |
|---|---|---|
| **Phone (Tab)** | Texts, calls, contracts, photos, voice notes from Walt | A call shows as a small toast on the LEFT (MUST: alerts on the left). Answering is optional; a missed call becomes a voicemail transcript. Nothing pauses the game |
| **NPC barks** | A 1–2 line speech bubble when you walk past, with gibberish voice blips | Proximity-triggered. Never stops movement. Each line plays once per day per NPC |
| **Letters** | Walt's handwritten letters, Voss Maritime offers, bills, fan mail | Delivered to the Landing's mailbox at 07:00 by Captain Mags' ferry. Read in-world on a paper card; close with Tab or Esc |
| **Walt's journal** | Fishing tips, old maps, the WaltsMix bait recipe, the backstory | Found in the office on Day 1. Pages unlock as you hit milestones. Lives in the phone's Journal app |
| **The changing world** | The Landing grows, the town reopens, Voss Harbour's lights dim, the Cut reopens | The real progress meter. Every act changes something you can see from the office balcony |
| **The Bench Council** | Three retirees on the town bench who gossip | **They telegraph rival actions** a day early ("Heard Marlowe's lads were buying rope cutters..."). Being a gossip is useful |
| **Short "beats"** | At most 6 s of camera framing (Voss's yacht passes, a crane turns on) | Player input stays live. Never more than one beat per in-game day |

**Co-op rule (SUGGESTED):** all story messages go to a shared **"Hale & Crew" group chat** on every player's phone, so story moments happen together. Personal threads such as hiring and contracts are company-wide too. A late joiner sees the whole thread history.

**Skippability:** every story event has a "fallback trigger". If the player ignores a call for 2 in-game days, the beat happens anyway through another channel (a letter, an NPC bark). The story waits for the player; the player never waits for the story.

---

## 2. Backstory (all SUGGESTED unless marked)

### 2.1 Timeline
| Years before Day 1 | Event |
|---|---|
| 60 | Walt Hale, 21, buys a dead pier on the west coast of Saltmouth Island with his wife **Maggie**. He names their first boat after her: the **Margaret** |
| 45 | Walt and the islanders dig out **Walt's Cut**, an old storm channel from the west coast through to the north bay (~730 m), so freighters from the east-side shipping lanes can reach the west side's deep, sheltered water without rounding the north cape *(Kufu review 31 Oct)*. **Hale's Landing** booms: the cannery, the ice house, 40 boats, a fish market, the Lantern Festival every summer. Old Saltmouth town grows around it |
| 33 | Adrian Voss, a 15-year-old runaway from Port Ardent, stows away on a freighter and is found asleep in Walt's net shed. Walt gives him a bunk, a job and a brass compass. Adrian becomes the best deckhand on the island |
| 26 | Adrian, now 22, wants to modernise: containers, cranes, outside investors. Walt says the Landing is "for the island, not for a bank". They argue. Adrian leaves for the mainland |
| 15 | Adrian comes back with consortium money as **Voss Maritime**. He builds **Voss Harbour** on the far east tip of the island, the deep-water side facing the shipping lanes *(Kufu review 31 Oct)*, and wins the **Saltmouth Harbour Charter** (a 15-year licence to run the island's port services) from a council that wanted his jobs |
| 12 | Voss builds the **Voss Causeway**, a low road bridge over Walt's Cut, "so trucks can cross the island". No ship can pass under it. The Cut silts up. The freighters go to Voss Harbour and the Landing starts to die |
| 9 | **The night of the Margaret.** In a storm, a Voss trawler loses power off Gullrock. Voss's own boats stay in port. Walt takes the Margaret out alone, tows the crew to safety, and loses the Margaret on the way home. She sinks off the north-west; the wreck is now fishing zone F5. Voss never comes to say thank you, and Walt never forgives the silence |
| 8 | Walt sells the cannery to pay his debts. Voss Maritime buys it through a shell company and closes it |
| 3 | Walt, 78, falls on the rotten pier and breaks a hip. His family moves him to the **Seaview Rest Home** in Port Ardent. Hale's Landing is boarded up |
| 0 | **The Harbour Charter expires in one island year.** Walt, who has heard the council will vote on renewal, mails his keys to his grandchild (the player). Day 1 begins |

### 2.2 Why this backstory works for the game
- **It explains every MUST:** grandpa's once-bustling port, the rival taking over, our dock abandoned for a while, the tiny damaged boat (the Margaret's little tender, Biscuit), and the rival's port on the opposite side of the island.
- **It gives the map a goal:** the silted Cut is visible from the dock on Day 1. Reopening it is the last act.
- **It mirrors the finale:** the night of the Margaret, when Walt saved Voss's crew, comes back as the storm choice in Act 5 (§4.7).
- **It leaves Voss room for redemption.** He still wears Walt's brass compass on his watch chain. Players who look closely at him during the gala beat will spot it.

### 2.3 The island at a glance
> *(Manan 31 Oct)*: the chosen map is now **B, Crescent Bay** (§8.0). The names below carry over; the "Side" column is map A's and is superseded by §8.0 (our dock and town deep inside the bay, Voss on the outer east coast).

| Place | Side | What it is | Status on Day 1 |
|---|---|---|---|
| **Hale's Landing** | West coast | Our dock: a rotten pier, the run-down office with a cot, a net shed, the Cannery Flats behind it | Abandoned for 3 years |
| **Old Saltmouth** | West coast, 1 min walk | The old fishing town: shops, the pub, the post office, the Voss-owned fish market | Half the shutters are down |
| **Walt's Cut** | West coast → north bay | A ~730 m channel under the low Voss Causeway, silted to 0.5 m *(Kufu review 31 Oct)* | Only kayaks and a dinghy at high tide get through |
| **Saltback Ridge** | Island spine, 140 m | The hills that hide Voss Harbour from our dock | At night you see its glow over the ridge |
| **Dot's Lighthouse** | North cape | Harbour master's station: weather, rescue, radio | Working |
| **Breaker Point** | South tip | Beach and the beginner surf break; Kai's hut | Quiet |
| **Salt River / Estuary** | (map A only; not on map B) | A brackish river mouth. No fishing zones any more *(Manan 31 Oct)* | – |
| **Voss Harbour** | Far east tip *(Kufu review 31 Oct)* | A container terminal: 6 cranes, freighters, the VOSS tower, the yacht marina | Bustling day and night (MUST) |
| **Gullrock** | Islet, NW | A reef, gulls, the Widowmaker reef break | – |
| **Lantern Key** | Islet, SW | **The Pelican**, chef Amara Bell's restaurant (premium buyer) | Open |
| **Sorrel Isle** | Islet, NE, past the lanes | The old cannery co-op (bulk buyer) | Opens in Act 3 |
| **Mango Cay** | Islet, far SE | Teddy Mango's "beach club" | Opens in Act 3+ (cartel) |
| **Port Ardent** | Mainland, off-map east | Ferry, Walt's care home, where shipping consignments go | Reached by the ferry only (narrative) |

---

## 3. The rival

### 3.1 Name decision: Adrian Voss (SUGGESTED; OPEN Q2)
| Option | For | Against |
|---|---|---|
| **Adrian Voss** (chosen) | One syllable that reads as a logo on cranes, containers and banners. Rhymes with the title ("Port Boss vs Voss"). Already in Tushar's Codex concept, so the team shares it | Slightly generic "tycoon" ring, which the backstory fixes |
| Marlowe | Already in the old build, and Manan knows it | Belongs to the replaced game's tone (banner-plane insults, cartoon villain). Two syllables, softer; weaker as a logo |
| A new name (e.g. Rourke) | Fresh | Throws away both earlier efforts for no gain |

**Marlowe becomes Voss's fixer**, "Head of Community Relations" at Voss Maritime. He runs the goons. Late in the game it turns out the worst sabotage was Marlowe's own idea, which opens the reconciliation ending.

### 3.2 Adrian Voss: profile
- **Age 48.** Tall, lean, silver at the temples. Navy suit with no tie, deck shoes, a gold watch, and **Walt's old brass compass on the watch chain**. Owns a white motor-yacht, the *Efficiency*.
- **Personality:** charming in public, cold in private. Talks in business slogans ("Sentiment doesn't float"). Petty: he names a new crane after every competitor he crushes. Genuinely believes his port saved the island's jobs. He hasn't spoken to Walt in 26 years and still checks on him through the care home's donor list (a late reveal).
- **What he wants:** to renew the Harbour Charter, to buy Hale's Landing so the past is paved over, and never to admit Walt was right.
- **His tell:** he never says Walt's name. He calls him "the old man".
- **How we meet him:** never face to face until Act 3. Before that we see the yacht passing at a distance, his voice on Voss Harbour's PA when we sail by, a billboard in town ("VOSS MARITIME: THE FUTURE DOCKS HERE"), and his signature on the lowball offer letter.

### 3.3 The rival system: "Heat" (MUST: goons first, recoverable; system built later)
**Core idea (SUGGESTED).** Voss Maritime watches a hidden number, **Heat (0–100)**. It rises as we take market share from Voss and falls with time and with some counterplay. Heat sets the **tier**; the tier sets which **rival actions** can fire. The phone's Contacts page shows Voss's face with a small mood icon (bored → amused → annoyed → furious), so Heat is readable without a meter.

**What raises Heat** (numbers are starting values for tuning):
| Event | Heat |
|---|---|
| Sell fish anywhere except the Voss-owned Fish Market (per in-game day with sales over $200) | +1 |
| A moorer signs with us (from Act 2, many moorers used to berth at Voss Harbour) | +3 |
| Each hired worker beyond 2 | +1 |
| A ship order berthed at our port | +4 |
| Win a contract Voss also bid on | +5 |
| Photograph Voss's wrongdoing (evidence) | +2, and +8 if published |
| Fish in Voss Lanes inside his buoy line | +1 per visit |
| Surf the Voss Wall | +1 (and his patrol hoses you) |

**What lowers Heat:** −2 per in-game day when nothing raised it; **counterplay actions** (below) cut it by 5–15; some story choices (accepting a meeting, returning his lost dog in a side event) cut it once.

**Tiers** (the story locks the first two, so Heat can't run ahead of the plot):
| Tier | Heat | Story lock | Voss's stance | What we see |
|---|---|---|---|---|
| 0 Unaware | 0–14 | Prologue to mid-Act 2 (Heat is capped at 14 until the Act 2 trigger) | Doesn't know we exist | Only his port, ships and billboards |
| 1 Curious | 15–29 | Act 2 end ("Pratt reports") | "Who's the kid at the old man's pier?" | Gideon Pratt photographs our dock; a polite buy-out letter |
| 2 Annoyed | 30–49 | Act 3 opens with the **GOONS' FIRST VISIT (MUST)** | Intimidation | Goons, price war, poaching |
| 3 Hostile | 50–69 | Act 3 mid | Interference and sabotage | Cut lines, blocked buyers, inspections |
| 4 War | 70–89 | Act 4 | Open commercial war | Bids for contracts, bait monopoly, patrols in the lanes |
| 5 Charter | 90–100 | Act 5 | Endgame | The Charter campaign, the storm, the vote |

**Rules for every rival action (SUGGESTED, all hard design rules):**
1. **Telegraph first.** Every action has a warning at least 1 in-game day (24 real min) ahead, through the Bench Council, a moorer, Dot or a text. The only exception is the goons' first visit, which *is* the warning.
2. **Recoverable.** No action destroys an owned boat, building or upgrade for good. The worst case is "out of action for up to 1 in-game day" or "costs up to 15% of current cash".
3. **Counterplay always exists** and is shown in the warning ("Lights and a watchman would help").
4. **One active action at a time** in tiers 2–3, at most two in tiers 4–5, with a 2-day cooldown between actions.
5. **No actions while you sleep.** Night actions resolve when you wake, as a discovered scene you can investigate, never as an off-screen loss without a scene. This avoids "I logged in and lost everything".
6. **Solo vs co-op:** the action pool is the same. In co-op, physical actions (goons on the dock, saboteurs at night) bring one extra goon per 2 extra players. In solo, timers are 25% longer and the sabotage night patrol is shorter.
7. **Rubber band:** if cash drops below 1 in-game day of costs, Voss "loses interest" for 2 days (Heat −10). Struggling players are never kicked while down.

**Rival actions table** (input from Codex's §6 table, expanded):
| # | Tier | Action | Warning (≥1 day) | Effect | Counterplay | Recovery |
|---|---|---|---|---|---|---|
| R1 | 1 | **The polite offer**: a letter from Voss Maritime offering $5,000 for the Landing | It *is* the warning | None. Tear it up or keep it (Walt texts: "Frame it. I did.") | – | – |
| R2 | 2 | **The goons' first visit (MUST)**: Big Dario and Little Felix are waiting on our pier one fog morning at 07:00 | It is the warning | They knock one crate into the sea, eat Mari's tacos without paying, leave Marlowe's card: "Sell, or the sea gets rough." No fight. **Optional comedy:** shove Felix off the pier (no Heat, +morale) | Pocket the card: it is the first piece of evidence | – |
| R3 | 2 | **Price war**: the Voss Fish Market pays 50% for 3 days and Voss buys Amara's supply cheaply | Bench Council: "Pratt's been on the phone to The Pelican" | Our main buyers pay −20% for 3 days | Sell to other buyers (Sorrel cannery, moorers), or keep fish (it rots slowly: 48 in-game h); take Amara a rare fish to keep her loyal | Ends after 3 days |
| R4 | 2 | **Berth poaching**: Voss offers our moorers half-price berths | Mari: "They offered me a free month over there..." | One moorer may leave (the newest, for 2 days) | Upgrade that moorer's berth (shore power, a lamp), or win a moorer side-quest | They come back if their berth beats Voss's |
| R5 | 3 | **Night of the cut lines**: goons cut a boat's mooring line at night | Bench Council: "Marlowe's lads bought rope cutters" | On waking, one boat has drifted up to 300 m (pinged on the phone map) and may have a dented hull (−20% speed until repaired, 1 day at Doyle) | Dock lights + a night watchman (Old Lowe, hire) + chain moorings (Plank & Post) block it fully | Fetch the boat: a short co-op moment (one drives, one tows) |
| R6 | 3 | **Bait squeeze**: Voss buys up June's best bait shipment | June: "Ferry's short on squid this week. Someone bought the lot." | One bait type is out of stock for 2 days | Dig worms on the beach at low tide, catch live bait with the cast net, or use WaltsMix (journal recipe) | Restocks after 2 days |
| R7 | 3 | **The inspection**: a Voss-paid harbour inspector fines our "unsafe" pier | Dot: "Inspector's coming Thursday. He's on Voss's Christmas list." | A fine of up to 10% of cash, unless the pier passes | Repair the planks, buy life rings and fire buckets (cheap); Dot can vouch if her rep is ≥ Friendly | Fine paid; the pier is safer |
| R8 | 3 | **Buyer block**: Voss pressures the Fish Market and one restaurant to refuse us | Amara: "I've had a visit. I'm not scared, but my landlord is." | One buyer refuses us for 2 days | Deliver Amara's "loyalty order" (a rare fish), or open a new buyer (Sorrel Isle) | 2 days |
| R9 | 4 | **Contract bidding**: Voss bids on the same ship orders and consignments | Shown on the contract card ("VOSS ALSO BIDDING") | We lose the contract if he outbids us | Bid on speed (faster boats, better freshness) and reputation, not price; allies vouch | Normal |
| R10 | 4 | **Patrols in the lanes**: Voss patrol boats chase us out of the Voss Lanes | Text from Marlowe: "Our lanes. Our fish." | Inside the buoy line, a patrol boat hoses your deck (comedy: fish and players slide) and tows you out | Fast boat (T4) to outrun it, fish the lanes at night, or the Dot complaint (legal) cuts patrols for 3 days | Instant |
| R11 | 4 | **Engine sabotage**: sugar in a worker boat's tank | Pim (who knows Marlowe's habits): "He used to get sugar sent to the office. A lot of sugar." | One worker boat stalls at sea, and the worker radios for a tow | Locked fuel caps (Doyle), a night watchman | Tow it home (co-op), or pay Doyle's tow ($) |
| R12 | 4 | **The smear**: a Voss newspaper ad calls our fish unsafe | The Bench Council reads it aloud | −10% fish prices in town for 2 days | Win the Saltmouth Fish Derby (event), or publish one piece of evidence | 2 days |
| R13 | 5 | **The Charter campaign**: Voss throws a gala at his port (fireworks we can watch from the Cut) and buys council votes | Act 5 story | Council support swings | Allies (§4.7): each ally NPC at Friendly+ is a vote; evidence; Walt's letter | Story |
| R14 | 5 | **The storm gambit**: during the great storm, Voss orders his crews out to finish a shipment and a trawler founders | Dot's storm call (≥3 in-game h ahead, per gdd-systems) | The finale choice (§4.7) | – | – |

**Never (hard rules):** Voss never burns the office, sinks a boat for good, hurts an NPC on screen, or steals hotbar items.

**Counterplay tools, all in-world (SUGGESTED):**
| Tool | Where | Effect |
|---|---|---|
| Dock lamps (×4) | Plank & Post | Night sabotage −50% chance |
| Night watchman: **Old Lowe**, a retired lighthouse keeper | Hired at The Mooring Line | Blocks R5 and R11 when awake (he sleeps 03–05, so the gap is a known risk) |
| Chain moorings | Plank & Post | Blocks R5 fully |
| Locked fuel caps | Doyle Boatworks | Blocks R11 |
| Phone camera evidence | Phone app | Every photo of a goon or a Voss misdeed is stored. Evidence pieces add council votes in Act 5 |
| Allies (reputation with NPCs) | Everywhere | Allies warn earlier (2 days instead of 1), vouch in inspections, refuse Voss's pressure |
| Dot's formal complaint | Lighthouse | Pauses R7 and R10 for 3 days (once per act) |
| Insurance (Fergus) | Doyle Boatworks | Pays 50% of rival-caused repair costs |

### 3.4 Voss's people
| Name | Role | One line |
|---|---|---|
| **Marlowe** (first name never given) | Fixer, Head of Community Relations | Cream linen suit, pencil moustache, always eating pistachios. Says "friend" like a threat |
| **Big Dario** | Goon | Huge, gentle voice, collects snow globes. Apologises while he knocks your crate into the sea |
| **Little Felix** | Goon | Even bigger than Dario. Can't swim, and everyone knows it, so shoving him off the pier is the island's favourite sport |
| **Gideon Pratt** | Voss purchasing agent, runs the Fish Market | Clipboard, thin smile, pays 40% under fair price. The first antagonist we meet on Day 1 |
| **Captain Keel** | Voss Harbour Patrol | Megaphone, a whistle, a hose cannon. Enforces "cannot dock" (MUST) with escalating politeness |
| **Pim** | Voss's nervous assistant | Fired in Act 2 for "losing" a moorer to us. Shows up at our gate with a box. **Our first unique worker** |

---

## 4. Acts: from the first morning to the endgame, then endless

**Pacing assumptions.** Timings assume 2 players and are hands-on real hours. Solo runs about 25% longer (economy: solo ~16.8 h, 2 players ~13.5 h, 4 players ~10.8 h to the vote), because a crew earns more per hour while prices never scale *(Kufu review 31 Oct)*. One in-game day is 24 real min, so 1 real hour is 2.5 in-game days. The economy section owns the exact prices; the money figures below are placeholders for its numbers.

**Act gates are soft.** An act ends on a goal plus a world event. Players can stay in an act as long as they like; nothing times out. The only clock in the story is the **Harbour Charter vote**, set for Day 40 at the earliest. It waits for the player: the vote is called only once the Act 5 goal is met.

### 4.0 Overview
| Act | Name | Days | Real hours (2P) | Goal | Ends with |
|---|---|---|---|---|---|
| P | First Light | 1 | 0–0.3 | Get Biscuit afloat, catch and sell a fish | First sale (Pratt's lowball) |
| 1 | The Mooring | 1–4 | 0.3–1.5 | First income (Mari's mooring), fix Biscuit, first premium buyer | Amara's first order delivered; Walt's first call |
| 2 | The Landing Wakes | 5–12 | 1.5–4 | 3 moorers, repaired office, first worker (automation), second boat | Pratt photographs the Landing: **Voss notices** |
| 3 | Visitors in the Fog | 13–20 | 4–7 | Survive the goons; first real pier; first ship order | Voss and Walt's story revealed; P2 piers built |
| 4 | Steel and Salt | 21–32 | 7–11 | A working port: crane, ship orders, shipping boats, the Sorrel cannery; start dredging the Cut | The Cut half-dug; Voss declares war on the Charter |
| 5 | The Charter | 33–40+ | 11–14 | Win allies, reopen the Cut, survive the great storm, win the vote | Ending choice; Walt comes home |
| ∞ | Endless | 40+ | 14+ | Grow the empire; side lines (cartel, surf, events) | – |

### 4.1 Prologue: First Light (Day 1, 07:00–~13:00 in-game, ~0–18 real min)
- **Goal:** get Biscuit going, catch a fish, sell it.
- **Beats:**
  1. Wake on the cot in Walt's dusty office at 06:55. Sea light through broken blinds; gulls. On the desk: **Walt's letter**, a key on a cork float, the journal. The letter is shown on a paper card, 6 lines; close it to play.
  2. The phone buzzes on the LEFT: Walt's first text, with his voice-to-text typos (§7.2).
  3. Outside: the rotten pier, **Biscuit** half full of rainwater, the silted Cut and the causeway in view, a Voss freighter's horn from beyond the ridge.
  4. Bail Biscuit (a 15 s physical action: grab the bucket, scoop, the water level drops), then pull-start the outboard (three pulls; the third catches).
  5. Fish in the bay (1 min away; any fish anywhere, *(Manan 31 Oct)*). Tutorial fish: Harbour Sprat, mackerel. The bobber, the 4–10 s bite and the 3-hit bar are taught by **Walt's journal page 1**, shown as a small card at the side, never a full-screen tutorial.
  6. Sell at the **Old Saltmouth Fish Market**. Gideon Pratt pays $6 for a $10 mackerel (60%, economy) *(Kufu review 31 Oct)*: "The only buyer in town, friend. Take it or watch it rot." (Rot is ≥2 in-game days, MUST, so the threat is a slow one.)
- **What changes in the world:** Biscuit is afloat; the Landing's one lamp turns on at dusk.
- **Unlocks:** fishing, the phone, the Fish Market, the bait shop (June gives a free bag of worms: "On the house. For Walt.").
- **Rival:** Tier 0. The Voss freighter horn and billboard only.
- **Emotional moment:** Walt's first text arrives the moment Biscuit's engine catches: "Is that my Biscuit I can hear from here? 😄 (I can't. But I can feel it.)"

### 4.2 Act 1: The Mooring (Days 1–4, ~0.3–1.5 h)
- **Goal:** first steady income, Biscuit repaired, a better buyer.
- **Beats:**
  1. **Mari's request (MUST).** **Day 3 at 07:00, or the moment the player's TOTAL EARNED reaches $250, whichever is first** *(Manan 31 Oct)* (was Day 1, ~13:00; in economy's sim: Day 2, 15:00): Mari Duarte texts. Her taco boat is being priced out of Voss Harbour, so can she moor at the Landing? **Accept** gives **$40/day** (economy's number) *(Kufu review 31 Oct)* plus free tacos (a stamina snack); **Decline** means she asks again tomorrow, more desperate, and asks a third time before she gives up. The economy section sets the price.
  2. Mari arrives within the in-game hour and ties up (we tie her line: the first co-op/physical task). From then on she pays daily at 07:00 (a ka-ching toast on the left), and her music, string lights and customers make the Landing feel alive straight away.
  3. **Fergus Doyle** looks at Biscuit: the engine needs a **$90** rebuild *(Manan 31 Oct)* ("Parts only. For Walt."), done in 1 in-game hour, which restores full T1 speed (damaged Biscuit runs at **4.5 m/s** until then). It is the first purchase, on Day 1 at ~16:00 right after the first sale (economy §2.3: minute ~9).
  4. Day 2: Walt's first **phone call** (30 s, optional). He tells you about Amara Bell at The Pelican: "She pays what fish is worth."
  5. Day 2–3: first **contract**: Amara wants 3 mackerel of ★★ or better (from the 3-hit minigame) by the evening of Day 3. Sailing 3.5 min to Lantern Key is the first real trip.
  6. Day 3: Ingrid at Plank & Post offers "fix three planks, I'll throw in a bench": the first dock upgrade.
  7. Day 4, morning: a letter in the mailbox, Walt's first handwritten one, with a photo of the Landing in its prime (§7.4).
- **What changes:** Mari's boat, string lights and music; 3 new planks; Biscuit sounds healthy.
- **Unlocks:** Doyle Boatworks (repairs, paint), Plank & Post (repairs, basic furniture), contracts on the phone, Lantern Key as a buyer, shrimp bait.
- **Rival:** Tier 0. On Day 4 Voss's yacht *Efficiency* motors past the Landing at a distance. A 4 s beat: the camera nudges, Voss on the flybridge doesn't look over.
- **Emotional moment:** Walt's photo letter. The empty pier where you stand, 45 years ago, packed with boats and people, and Walt holding a huge grouper (Old Brass's cousin).
- **Solo vs co-op:** the same. In co-op, Mari's mooring line is a two-player catch-and-tie; in solo she throws it to you.

### 4.3 Act 2: The Landing Wakes (Days 5–12, ~1.5–4 h)
- **Goal:** 3 moorers, a liveable office, the first hired worker, a second boat.
- **Beats:**
  1. Mari spreads the word. Two more moorers ask by phone (§6.3): **Hugo & Fifi** (retired houseboat couple, they gossip, so +1 day of rival warning) and **Ola's Ice** (an ice barge: fish on board rot 50% slower, which links to gdd-fishing's rot rule).
  2. Office restoration through Plank & Post: bed, the warehouse T1, stove, desk, then the balcony (each installs itself in its set spot, no placing *(Manan 31 Oct)*) (the place to view your growing port).
  3. Day ~7: **The Mooring Line pub** job board opens; Rosie Tull says someone "keeps asking for work". Hire a worker, assign them to a boat from the phone (MUST: first automation), and they fish at a spot you pin and bring the catch to the warehouse *(Manan 31 Oct)*.
  4. Day ~8: buy a second boat at Doyle (T2 launch), so the worker gets Biscuit, or the reverse.
  5. Day ~9: journal page "The Margaret": the wreck location is marked and the **Old Brass** legend is introduced (Walt's lifelong fish; it needs Walt's restored rod, Old Faithful, AND Walt's Mix *(Manan 31 Oct)*).
  6. Day ~10: **Pim arrives.** Fired by Voss for losing Hugo & Fifi's berth to us, he turns up at our gate holding a box of desk stuff and a cactus. Hire him for free for the first week. He is useless and lovable, and he knows Marlowe's habits, so later he warns us about sabotage.
  7. Day ~11: Surf shop event: Kai invites us to the "Dawn Patrol" at Breaker Point, the introduction to surfing (§10.3).
  8. **Act end (Day ~12):** Gideon Pratt is seen on the causeway photographing the Landing through a long lens (a 5 s beat). The same day: Heat tier 1, and a polite Voss Maritime letter offering $5,000 for the Landing (R1).
- **What changes:** 3 moored vendors with their own lights and sounds; the office habitable; a worker boat leaving at 07:00 and returning at 17:00; a few shutters in Old Saltmouth open (the café).
- **Unlocks:** hiring, the second boat, office furniture, the ice barge's slower rot, journal legends, the surf shop, the Wreck of the Margaret (a story place), squid bait.
- **Rival:** Tier 0 → 1 at the end.
- **Emotional moment:** Walt on a call when you hire Pim: "Voss threw him out? Then he's one of ours. That's how it used to work round here."

### 4.4 Act 3: Visitors in the Fog (Days 13–20, ~4–7 h)
- **Goal:** survive the intimidation, build the first real pier (P1→P2), take the first ship order.
- **Beats:**
  1. **Day 13, 07:00, forced fog (systems' override). THE GOONS (MUST).** Big Dario and Little Felix sit on our pier, eating Mari's tacos. They stand when we come out. Dario: "Mr Marlowe sends his regards. And this." He hands over a card, then knocks a crate (or a fish basket) into the sea: "Oops. The sea gets rough round here, eh?" They walk off into the fog. Optional: shove Felix in. He can't swim, Dario fishes him out, and they leave dripping (no penalty). The left-side toast: "New contact: UNKNOWN". The text is a photo of our pier, captioned "nice dock. be a shame".
  2. Same day: Walt calls, angry for the first time. Then the journal's sealed page opens: **"Adrian."** The backstory of Walt and Voss comes out over 3 journal pages unlocked across the act.
  3. Days 14–20: the rival system goes live (tiers 2→3). R3 price war, R4 poaching, and R5 cut lines become possible, each telegraphed.
  4. Day ~15: **Sorrel Isle cannery co-op** opens as a bulk buyer (the counter to R3/R8). It sits past Voss Lanes, so it is risky water.
  5. Day ~16: build **P1 → P2** at the Cannery Flats: finger piers, 6 berths.
  6. Day ~18: **the first ship order (port line begins).** A small coaster (12 containers) asks by phone to berth. We rig a hired mobile crane (P2) and unload by hand plus crane. It is the first taste of the port game.
  7. Day ~19: **Voss's first face-to-face.** He comes to the Landing on the *Efficiency*, does not step off, and talks from the deck: "The old man always did collect strays." He offers $15,000 and a job. Refusing raises Heat; "I'll think about it" buys 3 calm days.
  8. **Act end (Day 20):** the night of the first storm (forced). Biscuit's engine dies at sea in the rain, Dot talks us home on the phone, and from the lighthouse beam we see the Margaret wreck's mast break the surface. Walt texts at dawn: "Dot told me. I'm proud of you. And I'm buying you a new engine."
- **What changes:** the Landing has 6 berths, lamps and a watchman's hut; Voss's men are seen around town; the Bench Council has a new favourite topic.
- **Unlocks:** the rival system, evidence photos, P2 piers, ship orders (small coasters), security (watchman, lamps, chain moorings), the Sorrel cannery, T3 trawler.
- **Rival:** Tiers 2–3, goons, the first sabotage.
- **Emotional moment:** journal page "Adrian, age 15". Walt's handwriting: "Found a boy asleep in my nets today. Told him he could stay till spring. —W." Below it, the brass compass taped to the page has been torn off.

### 4.5 Act 4: Steel and Salt (Days 21–32, ~7–11 h)
- **Goal:** a working port: crane, ship orders, our own shipping boats, consignments. Start dredging Walt's Cut.
- **Beats:**
  1. **P3: the deep-water berth and the first fixed crane** at the end of the Cannery Flats (MUST: build multiple piers, berth ships, unload containers with our cranes).
  2. **Shipping boats:** buy a small feeder, and the phone offers consignments (MUST: send our boats on shipping consignments, offered by phone): carry X containers to Port Ardent; the boat leaves the map and returns after N in-game hours (gdd-economy owns the numbers).
  3. **The Walt's Cut project:** Dot reveals that the Charter makes the Cut public water, and the causeway was built under a "temporary" permit that lapsed. If we dredge the Cut and fund a **swing bridge**, ships can reach our side again. A long project: rent a dredger barge, three dredge jobs (co-op friendly: one drives, one runs the dredge arm), then the bridge.
  4. Rival war (tier 4): contract bidding, patrols in the lanes, engine sabotage, the smear.
  5. Day ~26: **Walt's Hook** (replaces Silverbill, cut with the 15-fish roster *(Manan 31 Oct)*; SUGGESTED). Voss's office has a huge marlin mounted, plaque "Caught by A. Voss, 1981". Walt's letter 10 tells the truth: Walt landed that marlin from the Margaret, Voss "helped tie it up" and took the photo and the credit, and the brass hook Walt forged for it, stamped **W.H. · MARGARET**, is still in its jaw. The proof is something we can get without new fish: from Act 3 the phone Camera takes **evidence photos**. Sail past Voss Harbour at night when his office lights are on (the window faces the water), zoom in, and photograph the stamped hook. The photo plus Walt's letter go to Gull FM: the radio runs "The fish Voss only borrowed", our reputation jumps (Charter allies +1 each), and Heat jumps a tier. Voss's reply by text: "Old men tell stories." Co-op: one player steers in the buoy-line patrol window, one takes the photo.
  6. Day ~28: **Teddy Mango** texts for the first time (§10.1). It is optional; ignoring him is fine.
  7. Day ~30: Marlowe overplays his hand. The goons are caught on our dock at night with a jerrycan (an R11 escalation), Old Lowe catches them, and Pim photographs them. Voss calls us himself, for the only time: "That wasn't me. ... It doesn't matter what you believe."
  8. **Act end (Day 32):** the Cut is half dug. The Harbour Charter vote is announced on the noticeboard: Day 40, or once we are ready.
- **What changes:** cranes on our skyline (visible from Voss Harbour too); trucks; container stacks at the Flats; ships passing through the half-open Cut at high tide; Old Saltmouth's high street mostly reopened; Voss Harbour has 1 crane fewer working at night (the lights dim).
- **Unlocks:** P3 berth and crane, shipping boats, consignments, dredging, T4 fast boat, the cartel line (optional).
- **Rival:** tier 4.
- **Emotional moment:** the first container ship in 12 years sails through Walt's Cut at high tide while the town lines the causeway, and Walt, on a video call, can't speak.

### 4.6 Act 5: The Charter (Days 33–40+, ~11–14 h)
- **Goal:** win allies (votes), finish the Cut and the swing bridge, survive the great storm, win the Charter.
- **Beats:**
  1. **Allies:** the council has 7 seats. Each seat is tied to an NPC faction that we win by finishing its request chain: Mari (vendors), June (fishers), Amara (restaurants), Ingrid (trades), Dot (harbour), Rosie (workers), the Bench Council (old Saltmouth). Evidence pieces (≥3) flip one wavering seat. We need 4 of 7.
  2. Voss's Charter gala (R13): fireworks over Voss Harbour, visible from the Cut. We can sail there and watch from the water (cannot dock, MUST). Voss on the balcony, alone, turning the compass over in his hand.
  3. **The great storm (forced weather; Dot calls it ≥3 h ahead).** See §4.7.
  4. The vote: council day at the town hall. It is shown as a short in-world scene: we stand in the room, players can move, NPC barks. Then the result.
- **What changes:** the swing bridge opens; our port runs day and night; Voss Harbour's night glow dims further.
- **Emotional moment:** see §4.7.

### 4.7 The finale: the storm, the choice, the vote (SUGGESTED)
**The great storm (mirrors the night of the Margaret).** Our biggest consignment (the "Charter cargo": if delivered on time it flips one council seat and pays a fortune) is at sea. At the same time Voss has ordered his crews out to finish his own shipment, and a Voss trawler, the *Ardent Star*, founders off Gullrock. **Voss himself** is aboard, because he went to look. Dot radios everyone: "Anyone who can."

| Choice | What you do | Immediate result |
|---|---|---|
| **A. The cargo** | Finish the delivery | +$$, +1 council seat; the Voss crew is rescued by Dot's lifeboat, slowly; Voss, soaked, sees you didn't come |
| **B. The rescue** | Divert to the *Ardent Star* and tow it in through the Cut (only possible if the Cut is open: our side is the nearest safe harbour) | The cargo is late (−1 seat from the delay); Voss is pulled from the water by you; **the same rescue Walt made 9 years ago** |
| **C. Both (co-op or skilled solo)** | Split the fleet: one player or worker boat makes the delivery, another makes the rescue | Possible for crews of 2+ or a solo player with 2 worker boats; it is hard and its success is earned |

**Endings** (picked by the choice plus the player's play; each ends with a short scene of 30 s or less, then endless play):
| Ending | Condition | What it looks like | Endless state |
|---|---|---|---|
| **1. The Open Harbour** (community) | Win the vote with ≥5 allies; and you chose B or C | The Cut stays public and free. Small vendors fill the Landing. Voss keeps his port, smaller. **Walt steps off Mags' ferry** onto a packed pier for the first Lantern Festival in 12 years | Town at max; moorers ×2; festival events |
| **2. The New Boss** (ruthless) | Win the vote with Voss bankrupted: high Heat at the end, you chose A, and you buy out his debt (big cost) | Voss Harbour becomes yours, VOSS letters come off the tower, your name goes up. Rich, cold. Mari moves her taco boat further down the coast. Walt visits and is polite. "It's a fine port, kid." | Two ports; highest income; moorers fewer |
| **3. Hale & Voss** (reconciliation, secret) | Choice B or C; ≥3 evidence pieces proving Marlowe acted alone; Walt's final letter delivered to Voss in person | Voss fires Marlowe. In front of the town he gives Walt back the compass on the pier. The two ports merge, the Cut and the bridge are run jointly, and Voss becomes your grumpy partner who texts you shipping tips | Shared contracts with Voss; Voss as a contact |
| **Dark variant: Teddy's Favour** | Deep in the cartel line and you accept Teddy's offer to "handle" the vote | Voss "takes a long fishing trip". You win, and Teddy owns 20% of your port and calls you "partner" with a heart emoji. Played as comedy, never shown | Cartel contracts permanent; Heat replaced by "Teddy's mood" |
| **Lose the vote** (not a game over) | Under 4 allies | Voss renews the Charter for one more year. The game continues with a re-vote in 20 in-game days and Voss's Heat locked at tier 4. Walt: "We lost a vote. We didn't lose the Landing." | Act 5 replays, softer |

### 4.8 Endless (MUST: "then endless")
- The port keeps running, and new ship classes, contracts and consignments keep coming (gdd-economy).
- **Monthly "island news" beats:** a new moorer, a new legend fish rumour, a festival, a storm season.
- **Voss in endless** depends on the ending: a partner who texts tips (3), a rival on a dinghy who tries comeback schemes (1), or gone, with Marlowe starting his own rival outfit as a new antagonist (2).
- **Side lines** (§10): the cartel, surfing competitions, shipping routes to new mainland ports, legend-fish hunts, and the Lantern Festival every 28 days.

---

## 5. Emotional spine (SUGGESTED)
The story has one sincere thread: **Walt, Adrian and you.** Every act has one quiet moment (§4) that lands without stopping play: a text, a photo, a journal page, a ship in the Cut. The rest is comedy. The rule: **one tear per act, ten laughs per day.**

---

## 6. The cast

**Look rules (from Manan's 2026-10-31 decisions, relayed by gdd-systems):**
- Art direction is **#4 Ponyo Coast** (`../../replan/artdir/README.md` §4).
- Every NPC is built from the same chunky ~4.5-heads-tall kit as the 6 player presets (`../../replan/chars/`).
- No NPC reuses a player preset's name. The presets are Bruno, Stretch, Pip, Rani, Old Gus and Sully.
- Warm colours belong to our side of the island; greys and navy belong to Voss's people.

**Phone behaviour key:**
- **Texts first**: the NPC only texts after we have met them or they have been introduced.
- **Hours**: when they reply. Outside those hours, the reply arrives the next morning.
- **Calls**: whether they ever call.
- **Style**: how they write.

### 6.1 Family
| Name | Look | Personality | Role | Phone behaviour |
|---|---|---|---|---|
| **Walter "Walt" Hale** (81) | Never seen in person until the finale (photos and video calls). Big white beard, navy knitted cap, one gold tooth, a cane carved like a fish. On video calls the care home's floral wallpaper is behind him | Stubborn, funny, sentimental only in letters, proud. Bad with phones: voice-to-text mistakes, one emoji used wrongly forever (🍆 for "big fish" until a nurse corrects him in Act 2) | The heart of the story; gives fishing tips; a quest-giver for legend fish; the finale | Texts at 07:00–21:00 (care home lights out at 21:00; a 22:00 text reads "nurse took my phone"). Calls 1–2 times per act, 20–40 s, optional, with a voicemail transcript if missed. A handwritten letter about every 4 in-game days by the ferry. Voice notes from Act 3. Reacts to big events within 1 in-game hour ("Dot says you caught a ★★★ snapper. DOT SAYS.") |
| **Maggie Hale** (late) | In photos only: red headscarf, laughing | – | The Margaret is named after her; the Lantern Festival was her idea | – |

### 6.2 Old Saltmouth: shopkeepers (MUST shops)
| Name | Shop | Look | Personality | Phone behaviour |
|---|---|---|---|---|
| **June Pike** (63) | **Hook, Line & Sinker**: bait and tackle (MUST) | Short, wiry, yellow oilskin, reading glasses on a chain, a pencil behind her ear | Grumpy-kind; Walt's oldest friend; knows every fish | Texts 06:00–20:00. Restock alerts ("Squid's in. Don't tell the Pelican"). Bait-shortage warnings (R6 telegraph). Sends fishing tips with fish photos taken badly |
| **Kai Rennick** (24) | **Swell Surf Co.** (MUST surf shop) + a beach hut at Breaker Point | Sun-bleached hair, sunburnt nose, open shirt, shell necklace, always barefoot | Laid back, philosophical about waves, terrible at money | Texts at dawn (05:00–09:00) and dusk only. A **swell report** every morning there's surf ("6 ft at Breaker, glassy, come"). Event invites |
| **Fergus Doyle** (55) | **Doyle Boatworks**: boats, upgrades, paint (MUST boat shop) | Big red beard, grease-black overalls, welding goggles on his head, a pipe he never lights | Once Walt's mechanic. Blunt, a perfectionist, loves boats more than people | Texts 08:00–18:00. Quotes, "your boat's ready", a monthly new-model teaser with a photo. Insurance sales (R-counterplay). Never calls: "Phones are for emergencies" |
| **Ingrid Sundqvist** (40) | **Plank & Post Builders' Yard**: piers, beds, the warehouse tiers *(Manan 31 Oct)*, furniture, office upgrades (all installed in their set spots, no placing) (MUST dock upgrade shop) | Tall, pale blonde braid, a carpenter's pencil, a tape measure on her belt, sawdust everywhere | Precise, dry, quietly ambitious for the Landing | Texts 07:00–19:00. Build-complete photos; design suggestions ("That office needs a balcony. Trust me"); delivery times |
| **Rosie Tull** (50) | **The Mooring Line** pub = the hiring board (MUST hire place) | Round, rosy, curly grey hair, an apron with a fish pun on it | The town's mother, the gossip hub, protective of her regulars | Texts 11:00–02:00 (pub hours). Worker candidates with one-line reviews ("Nils: strong, smells, honest"). Pub quiz night invites |
| **Gideon Pratt** (45) | **Old Saltmouth Fish Market** (Voss-owned) | Grey suit, clipboard, thin moustache, a Voss lapel pin | Polite, mean, always "just doing my job" | Texts first on Day 2: "Market prices, daily." His price list arrives at 07:00 every day: useful, and insulting |
| **Nell Abara** (70) | **Post office + Nell's Curios** (SUGGESTED shop): buys collectibles (bottles, shells, coins, old Hale Landing memorabilia) | Tiny, enormous glasses, beads, a cardigan with pins on it | Collector, history buff, knows the old Landing | Texts when a collectible "set" is close: "That's the 4th Margaret tile. Two more and I'll tell you a story." |
| **Bao Tran** (35) | **Gull's Nest Café** (SUGGESTED): food buffs; reopens in Act 2 as a world-change marker | Apron, headband, always tired | Bakes at 04:00, wants the town to wake up | Texts menu specials, and fish orders for the café (small contracts) |

### 6.3 The first moorer and the moorers who follow (MUST + SUGGESTED)
**Mari Duarte (32), the first vendor who pays to moor at our dock (MUST).**
- **Look:** sunflower bandana, denim overalls, gold hoop earrings, a pencil-thin scar on her chin (a "fryer accident"). Her boat **"Mari's Tacos"** is a converted green-and-orange fishing boat with string lights, a hatch counter and a hand-painted sign.
- **Personality:** fast-talking, warm, cheeky, fiercely independent. She left Voss Harbour because berth fees doubled. She becomes our best friend and the voice of the small vendors.
- **Role:**
  - first income (daily mooring fee);
  - first small fish orders (taco fish);
  - a recruiter, who spreads the word to other moorers;
  - the ally chain for the vendor council seat.
- **Phone behaviour:** texts 09:00–22:00, with a lot of emojis and voice notes. She pays at 07:00 daily with a "💸 Rent's in, landlord". She sends mid-afternoon fish orders ("need 4 mackerel by 6, paying 12 each 🌮"). She warns about R4 poaching.

**Moorers who follow** (each pays a daily fee and adds one perk; SUGGESTED, values for gdd-economy):
| Moorer | Arrives | Perk | Personality |
|---|---|---|---|
| **Hugo & Fifi Laurent** (houseboat *Petit Pois*) | Act 2 | +1 day of warning on rival actions (they gossip with the Bench Council) | Retired, bicker lovingly, French |
| **Ola's Ice** (ice barge, Ola Lindqvist) | Act 2 | Fish stored at the Landing rot 50% slower; ice packs on sale | Silent, enormous, brings ice from the mainland |
| **Sven Moss** (salvage diver boat) | Act 3 | Salvage contracts at the Margaret wreck and collectibles | Talks only about the seabed |
| **The Pickle Sisters** (floating veg market) | Act 3 | Cheap stamina food; the town gets busier | Twin sisters who finish each other's sentences |
| **Captain Mags** (ferry) | Act 4 | The ferry now stops at the Landing too: letters arrive directly, and tourists bring charter contracts | Walt's old deckhand, 70, salty, loyal |
| **Dr Imani Cole** (floating clinic) | Act 4 | Stamina regenerates faster at the Landing | A volunteer doctor; brisk |

### 6.4 Town folk and harbour
| Name | Look | Personality | Role | Phone |
|---|---|---|---|---|
| **Dot Achterberg** (58), harbour master and lighthouse keeper | Navy reefer jacket, grey bob, binoculars, a thermos | Dry as a biscuit, deeply kind underneath | **Forecasts** (her face in the phone's forecast widget), storm calls (≥3 in-game h ahead), rescue (fishes you out), the Charter's legal angle, and the harbour council seat | A daily forecast message at 06:30 ("Fog till 10. Then fair. Don't be stupid."). Storm calls are real calls. She replies 24 h a day, because she never sleeps |
| **The Bench Council**: **Arthur, Bernie and Clem** (80s) | Three old men on the harbour bench, caps, a shared thermos | Gossips, contrarians, secretly the town's memory | **They telegraph rival actions** a day early (NPC barks plus a group text once we're invited to their "Bench Chat" in Act 2) | Bench Chat group: 4–6 messages a day, mostly nonsense, always with one useful line |
| **Old Lowe** (74) | Retired lighthouse keeper, pea coat, a lantern | Insomniac | Hireable night watchman (counterplay) | Texts at night only: "All quiet." / "Not quiet." |
| **Constable Ruth Okafor** (38) | Island police, bicycle, a whistle | Underfunded, honest, tired | Evidence goes to her; she can stop the goons for 2 days if handed ≥2 evidence pieces | Texts formally: "Dear Hale Landing. Re: your complaint." |
| **Tiny** (11) | A dock kid in an oversized cap | A worm entrepreneur | Sells worms; finds collectibles; mini side quests | Has no phone and sends notes by bottle (comedy) |
| **Amara Bell** (44), chef at **The Pelican**, Lantern Key | Chef whites, cropped natural hair, gold-rimmed glasses | Exacting, fair, passionate; once worked for Voss's hotel and quit | **Premium buyer**, quality contracts (★★+), the restaurant council seat | Texts 10:00–23:00. Orders with photos of the dish she'll make. Raves when you deliver ★★★ ("This is the one. Bring more.") |
| **Gully** (radio DJ, Gull FM) | Voice only; a gull-shaped mic in the studio photo | Over-excited, gossip in place of news | The island radio in the boat and the office; reacts to big events | Not a contact. Radio only (optional; OPEN for voice, §0.3 of the replan) |

### 6.5 Workers (MUST: hire, assign to boats from the phone)
- **Candidate pool:** the job board refreshes at 07:00 with 3 candidates. Each has a name, a face from the kit, 2 stats (Fishing, Seamanship) and one quirk:
  - *Early riser*: leaves at 06:00;
  - *Chatty*: slower, but adds rumours;
  - *Lucky*: +5% rare fish;
  - *Seasick*: refuses storms.
- Wages and stats are gdd-economy's.
- **Unique workers:**
  - **Pim** (Act 2): useless and lucky; warns about sabotage.
  - **Old Lowe** (night watch).
  - **Nils Berg** (strong, smells, honest; Rosie's first suggestion).
  - **Tamsin Wade** (ex-Voss crane operator, Act 4; unlocks a crane speed bonus).
  - **"Hook" Hennessy** (an ex-con with a heart of gold, joins after you help him with a side quest; immune to goon intimidation).
- **Phone behaviour:** workers report from their boat by text. "Leaving for Gullrock 🎣" at departure, "Got 14, 2 rare, coming home" at return, and "Engine's sick, need a tow" on a breakdown. In storms they text "Staying in port" unless they're the fearless type.

### 6.6 The rival's people (see §3.4) and their phone behaviour
| Name | Phone behaviour |
|---|---|
| **Adrian Voss** | No phone contact until Act 3 (a letter only). From Day 19 he texts rarely, 3–5 times in the whole game, always at 23:00, always short ("Still at it?"). One call in Act 4 |
| **Marlowe** | Texts from Act 3, polite threats with business vocabulary: "Friendly reminder: the market adjusts to those who adjust." |
| **"UNKNOWN"** (the goons) | Photos of our dock with misspelled captions. Dario adds "sorry" afterwards. Felix sends a thumbs-up by mistake |
| **Gideon Pratt** | The daily price list, plus "reconsider our offer" every 5 days |
| **Captain Keel** | Never texts. Megaphone only |

### 6.7 The future cartel boss (MUST: chill, evil-funny)
**Teddy Mango** (real name Teodoro Mangiapane, 50).
- **Look:** a loud mango-print Hawaiian shirt, linen shorts, flip-flops, sunglasses pushed up, a bead bracelet, a ukulele, and a coconut with a straw at all times. Lives on **Mango Cay** in a "beach club" with no visible members.
- **Personality:** extremely relaxed. Meditation talk, kind compliments, then one sentence of quiet horror delivered at the same calm pace. "Breathe in, partner. Breathe out. If the package is late, I'll feed your boat to the reef. Anyway, how's your grandpa?" He is never seen being violent; his menace is all implication and comic understatement.
- **Role:** runs the optional smuggling line (§10.1). He is the "enemy of my enemy": he hates Voss because Voss's patrols ruin his "business". He offers the dark ending variant.
- **Phone behaviour:** his first contact is a coconut washed up on our pier with a phone inside it. He texts only between 11:00 and 16:00 ("Never before my yoga, never after my nap"), always with 🥭🌴🙏, and sends voice notes with waves in the background. His missions arrive as "favours". A failed favour gets a sad, disappointed voice note and a 2-day cool-down, never violence to the player.

---

## 7. The phone as the story channel (MUST: phone on Tab; time, forecast, messages, contracts, hiring)

gdd-systems owns the phone UI. This section supplies its **content**: apps, story threads and the contract and mission types.

### 7.1 Apps the story uses
| App | Story use | Tag |
|---|---|---|
| **Messages** | Every thread in §7.2, plus the "Hale & Crew" group in co-op | MUST |
| **Contracts** | Orders, moorings, ship orders, consignments (§7.3) | MUST |
| **Crew** | Hire and assign workers to boats; workers' status texts | MUST |
| **Forecast** | 3 blocks per day (gdd-systems) with Dot's one-line comment | MUST |
| **Journal** | Walt's journal pages, legend fish notes, the backstory | SUGGESTED |
| **Camera** | Photos; evidence; collectible "sightings" | SUGGESTED |
| **Map** | Hotspots, buyers, our boats' live positions (no zones *(Manan 31 Oct)*) | SUGGESTED |

### 7.2 Example message threads: the first 2 real hours (Days 1–5)
Times are in-game. ★ marks a beat that has a fallback if ignored.

**Thread: Grandpa Walt**
> **Day 1 · 07:02** · Walt: Morning!! Did the key work. The door sticks pull up then push
> **07:02** · Walt: The boat is called Biscuit. She leaks. Bail her first or you will be swiming
> **07:03** · Walt: *swimming. The nurse says I have to proof read
> **09:35** (after the first cast) *(Kufu review 31 Oct)* · Walt: Dot says she can see you from the lighthouse. Wave
> **14:35** (after the first sale) *(Kufu review 31 Oct)* · Walt: Pratt gave you 6 for a mackerel didnt he. He gave me 6 in 1998. Some things never change except the fish
> **14:36** · Walt: Write this down. THE PELICAN. Lantern Key. Amara pays real money. Bring her good fish only
> **Day 2 · 08:00** · ★ *Incoming call: Walt (30 s, optional)*: "You sound tired. Good. Tired is honest. Listen, the ★★★ ones, you feel them in your wrist before the bar goes green. Your grandmother could do three in a row with her eyes closed. ... Go on then. Ring me when you've got something to brag about." *(Missed: voicemail transcript.)*
> **Day 3 · 19:30** (after Amara's order) · Walt: AMARA CALLED ME. She said "your grandkid has hands". Thats the nicest thing shes ever said about a Hale
> **Day 4 · 07:00** · *Letter in the mailbox* (§7.4)
> **Day 5 · 21:05** · Walt: lights out here. Dont stay up. The fish will still be there tomorrow and so will I 🍆 *(Act 2: "the nurse says that one doesnt mean big fish. Ignore it")*

**Thread: Mari Duarte (first moorer: MUST)**. Times moved *(Manan 31 Oct)*: she texts on Day 3 at 07:00, or the moment total earned reaches $250 (here: Day 2, 15:00, as in economy's sim).
> **Day 2 · 15:05** · Mari: hiii is this the Hale dock?? June gave me this number 🙏
> **15:05** · Mari: I'm Mari, I run a little taco boat (Mari's Tacos 🌮 best fish tacos on the island, not biased). Voss Harbour just DOUBLED my berth fee 😤
> **15:06** · Mari: could I tie up at your pier? I'll pay $40 a day + you eat free. I'm tidy I promise
> **[ Accept ] [ Not yet ]**
> *If Accept:* **15:07** · Mari: AAAA thank you!!! coming round the lighthouse, be there in an hour 🚤
> **16:05** · Mari: here! can someone grab my line?? 🙏 *(physical task: catch and tie)*
> **16:09** · Mari: ok this pier is... vintage 😅 love it though
> **Day 3 · 07:00** · Mari: 💸 rent's in, landlord *(+$40 toast, left)* *(Kufu review 31 Oct)*
> **Day 3 · 14:20** · Mari: quick one, need 4 mackerel by 6pm, I'll pay 12 each 🐟 *(contract card attached)*
> **Day 4 · 21:40** · Mari: people keep asking who owns the pier. I say "the Hales are back" and they get weird and smiley 🥹
> *If "Not yet":* **next day · 09:00** · Mari: hey, sorry to bug, any chance? I can do $50 😬 → **the day after · 09:00** · Mari: last try, promise. Voss's guy is being a creep about my late fee 😓 → After a third "not yet" she moors at Breaker Point instead, and the offer returns in Act 2 as a moorer request. This is a soft fail; the game never locks.

**Thread: June Pike**
> **Day 1 · 07:20** (when we walk in) · June: (in person) "You've got his ears. Here. Worms. On the house, for Walt."
> **Day 1 · 18:00** · June: Shrimp in stock tomorrow. Mackerel like shrimp. So does everyone, so come early
> **Day 3 · 07:30** · June: Saw you out at Gullrock. Don't anchor on the north side, the rocks have teeth

**Thread: Gideon Pratt (Voss Fish Market)**
> **Day 2 · 07:00** · Pratt: Good morning. Old Saltmouth Fish Market prices today: Sprat $4 · Mullet $5 · Mackerel $6 · Pollock $5 (60% of fair) *(Kufu review 31 Oct)*. Prices subject to change. Thank you for choosing Voss Maritime.
> **Day 3 · 07:00** · Pratt: [prices] (Mackerel $5 today. Supply is high.)

**Thread: Amara Bell (The Pelican)**
> **Day 2 · 11:30** · Amara: Walt's grandchild? He's been calling me every hour. Fine. Three mackerel, two stars or better, before 20:00 tomorrow. $30 each (economy: ★★ × 175%) *(Kufu review 31 Oct)*. Don't bring me anything soft.
> **Day 3 · 19:10** (on delivery) · Amara: These are good. Not great. Good. Next week: snapper. *(contract chain opens)*

**Thread: Dot (harbour master)**
> **Day 1 · 06:30** · Dot: Forecast. Clear morning. Breezy after 2. Don't go past Gullrock in that bucket.
> **Day 2 · 06:30** · Dot: Fair. Rain 17–22. Fish bite in the rain. People don't. Your choice.
> **Day 4 · 06:30** · Dot: Fog till 10. Foghorn's on. If you hear it twice, you're too close to the rocks.

**Thread: Fergus Doyle**
> **Day 1 · 16:00** · Fergus (in person): Her engine's a crime. $90, parts only, for Walt. An hour, and she'll do full speed.
> **Day 1 · 17:00** · Fergus: She's done. Don't thank me, pay me.
> **Day 2 · 10:00** · Fergus: Hull's still weeping. $120 and you can put the bucket away. *(Kufu review 31 Oct)*

**Thread: Unknown / rival texts (first one is Act 3, shown here for tone)**
> **Day 13 · 07:12** · UNKNOWN: [photo of our pier in fog] nice dock. be a shame
> **Day 13 · 07:12** · UNKNOWN: sorry
> **Day 13 · 07:13** · UNKNOWN: 👍
> **Day 14 · 10:00** · Marlowe: Good morning, friend. A gentle reminder that Voss Maritime's offer stands until Friday. The sea is unpredictable; contracts are not.
> **Day 19 · 23:00** · Voss: Still at it?

**Co-op variant (SUGGESTED):** in co-op, Walt's texts go to the shared "Hale & Crew" group. When a friend joins:
> Walt: Who's this then?? Any friend of my grandkid eats at my table. Don't let them steer
> *(For a 3rd or 4th player: "How many of you ARE there. Dot says the boat's sitting low")*

### 7.3 Contract types (MUST: contracts on the phone; amounts are gdd-economy's)
| Type | From | What | Story hook | Unlocks |
|---|---|---|---|---|
| **Mooring request** | A vendor or boat | Accept a moorer for a daily fee | First income (MUST); every moorer has a perk | Prologue |
| **Fish order** | Mari, Amara, Bao, the cannery | Species × count × min stars × deadline | Builds ally reputation for the Charter seats | Act 1 |
| **Bait challenge** | June | "Catch X using only Y bait" | Teaches bait types (MUST: bait → different fish) | Act 1 |
| **Delivery** | Islanders, moorers | Carry a parcel or crate by boat to an islet | Uses travel, rot and tiers | Act 1 |
| **Legend hunt** | Walt (journal) | Catch the legend with the best rod AND best bait | Old Brass, the one legend *(Manan 31 Oct)* | Act 4 |
| **Charter trip** | Tourists (Mags' ferry) | Take 1–3 NPC tourists fishing or surfing | Living world, comedy | Act 4 |
| **Salvage** | Sven | Dive or hook items at the Margaret wreck | Collectibles and backstory items | Act 3 |
| **Rescue** | Dot | Tow a stranded boat home | Reputation; foreshadows the finale | Act 2+ |
| **Ship berth order** | Shipping lines (phone) | A ship arrives at our pier at time T; berth and unload N containers with our cranes before it leaves | MUST port line | Act 3 |
| **Shipping consignment** | Mainland brokers | Send our shipping boat with cargo to Port Ardent; it returns after N h | MUST | Act 4 |
| **Favour** | Teddy Mango | Smuggling runs (§10.1) | Optional cartel line | Act 4+ |

### 7.4 Mission types (story, beyond contracts; SUGGESTED)
| Mission | Example | Solo vs co-op |
|---|---|---|
| **Physical task** | Bail Biscuit, tie Mari's line, haul a drifted boat | In co-op, split roles: one bails, one starts the engine |
| **Investigation** | After R5, follow the cut rope's fibres to the goons' rowboat under the causeway; photograph it | Same |
| **Escort** | Escort Mari through Voss Lanes during the price war | In co-op, one player escorts in a second boat |
| **Build project** | Rebuild the office; dredge Walt's Cut (3 jobs) | In co-op, one dredges, one drives |
| **Night watch** | Sit up with Old Lowe to catch saboteurs (optional; caught saboteurs = evidence) | Co-op: more eyes, more fun |
| **Letter delivery** | Act 5: carry Walt's final letter to Voss Harbour. You can't dock (MUST), so you hand it over boat to boat while Captain Keel's patrol watches | Same |

**Example letter: Walt, Day 4 (in-world paper card):**
> *Kid,*
> *That's the Landing in '81 in the photo. Forty boats. The cannery whistle at six. Your grandmother selling coffee off the end of the pier like she owned the place (she did).*
> *I'm not asking you to bring that back. I'm asking you to have one good day out there. Then another one.*
> *The Cut was deep enough for a freighter once. Ask Dot about that sometime.*
> *—W.*
> *P.S. Don't let Pratt rob you. He's not clever, he's just the only one there.*

---

## 8. THE MAP (MUST: our dock, town and shops, ~~fishing zones~~ no zones *(Manan 31 Oct)*, the rival port on the OPPOSITE side, islands and buyers, travel times by tier, surf spots, the expansion zone)

All three layouts share the same sea area (5 × 5 km) and the same boat tiers (T1 Biscuit 6 m/s, T2 launch 9, T3 trawler 11, T4 fast boat 16). Travel times were measured along the drawn routes by `maps/make_maps.py`.

### 8.0 THE CHOSEN MAP: B, Crescent Bay *(Manan 31 Oct)*
![Map B: Crescent Bay](maps/map-B.png)

`maps/map-B.svg` · `maps/map-B.png` (drawn by `maps/make_maps.py`, `map_b()`). Manan: "Map B" ("option B was okay"). The original Crescent Bay shape is kept: a crescent island hugging a calm bay that opens to the west.
- **Hale's Landing, our dock, sits deep inside the bay** on its east shore, with **Old Saltmouth** (town and shops) right behind it; our expansion P1 → P3 runs along the bay shore. **Gullrock** reef sits in the middle of the bay.
- **Saltback Ridge** (140 m) runs along the crescent's outer arc and hides Voss Harbour from the bay.
- **Voss Harbour is on the outer east coast (MUST "opposite side")**, a separate, bustling world: **12.4 min in Biscuit round the south horn** (8.3 in the Kestrel). Far on purpose *(Manan 31 Oct)*.
- **Walt's Cut** is a silted channel **through the narrow south horn**, from the bay just south of town to the outer south coast. Once dredged it takes the route to Voss from 12.4 to **8.0 min** (Kestrel 5.3), and lets ships reach our bay without rounding the horn, so the endgame goal (D6) survives.
- **Dot's lighthouse** stands on the north horn's tip; the **Wreck of the Margaret** lies off the north horn to the west; **Breaker Point** (beginner surf) is the south horn's outer beach, with a point break off the north horn and the Voss Wall wave.
- **No fishing zones** *(Manan 31 Oct)*: any fish can be caught anywhere; the map shows places (buyers, wreck, surf, story), not fish pools. The rod and bait pick the fish (gdd/fishing §5).
- **For:** sheltered starter waters, so storms feel safe at home; a beautiful bowl vista from the office. **Watch:** the bay gets crowded once the port grows (the expansion stays on the east shore and P3 by the Cut).

| From our dock to... | Distance | T1 6 m/s | T2 9 | T3 11 | T4 16 |
|---|---|---|---|---|---|
| Town shops (walk) | 0.2 km | 1 min walk | – | – | – |
| Bay Shallows (home water) | 0.3 km | 0.8 min | 0.5 | 0.4 | 0.3 |
| Gullrock (in the bay) | 0.4 km | 1.0 | 0.7 | 0.6 | 0.4 |
| Lantern Key (The Pelican) | 1.7 km | 4.6 | 3.1 | 2.5 | 1.7 |
| Wreck of the Margaret | 1.9 km | 5.2 | 3.4 | 2.8 | 1.9 |
| Breaker Point surf (by boat) | 2.7 km | 7.5 | 5.0 | 4.1 | 2.8 |
| Sorrel Isle (cannery) | 4.2 km | 11.7 | 7.8 | 6.4 | 4.4 |
| Mango Cay (cartel) | 4.3 km | 12.0 | 8.0 | 6.5 | 4.5 |
| **Voss Harbour, round the south horn** | 4.5 km | **12.4** | 8.3 | 6.8 | 4.7 |
| Voss Harbour via Walt's Cut (after dredging) | 2.9 km | 8.0 | 5.3 | 4.3 | 3.0 |

Measured along the drawn routes by `maps/make_maps.py` (31 Oct). The Lantern Key route bends south round Gullrock (1.7 km; the first draft's straight line through the reef was 1.4 km / 4.0 min).

Places named elsewhere in this document for map A (west coast, north bay, Salt River) carry over by name; their sides follow this section.

### 8.1 Option A: Twin Coasts (the first draft's pick; not chosen *(Manan 31 Oct)*)
![Map A](maps/map-A.png)

`maps/map-A.svg` · `maps/map-A.png`
- **Shape** *(Kufu review 31 Oct)*: a round west lobe (2.4 km north–south) with **a long east arm** reaching 2 km out toward the shipping lanes. Our dock and Old Saltmouth are on the **west coast**. **Voss Harbour sits on the far east tip, on the opposite side (MUST)**, ~2 km from our dock over **Saltback Ridge** (140 m), which runs across the root of the arm. On Day 1 you see only its crane tips over the ridge and its sodium glow at night; it is a separate, bustling world (freighters, 6 cranes, the VOSS tower, its own ferry to Port Ardent) that you only reach by a real voyage. **Walt's Cut** runs from the west coast to the north bay under the low Voss Causeway. *Why moved:* in the first draft Voss sat 440 m across the waist, close enough to feel like a neighbour, not a rival world (Manan: "push Voss Harbour so it feels separate"). The first draft's Lantern Glow zone also sat on land; it is now the north bay.
- **Zones** (first draft only; removed *(Manan 31 Oct)*):
  - **F1** Shallows (start);
  - **F8** Saltmouth Estuary (Salt River mouth);
  - **F2** Kelp Shelf;
  - **F3** Gullrock Reef;
  - **F5** Wreck of the Margaret;
  - **F6** Voss Lanes (east);
  - **F7** Lantern Glow bay (north, night);
  - **F4** Drop-off (everything beyond the dotted ring, ~1.85 km out).
- **Buyers:** the Fish Market (town), The Pelican (Lantern Key), Mari and the moorers (our dock), the Sorrel Isle cannery (NE, Act 3), Mango Cay (SE, cartel).
- **Surf:** Breaker Point (south tip, beginner), the Widowmaker at Gullrock (advanced), the Voss Wall breakwater wave (forbidden; patrols chase you off: comedy).
- **Port expansion:** north from the dock onto the **Cannery Flats**:
  - P1 is Walt's pier repaired;
  - P2 adds three finger piers with 6 berths;
  - P3 adds the deep-water berth, a crane and a container yard;
  - after the Cut reopens, ships reach P3 straight from the east lanes.
  The office balcony on the rise behind the dock overlooks it all.

| From our dock to... | Distance | T1 6 m/s | T2 9 | T3 11 | T4 16 |
|---|---|---|---|---|---|
| Town shops (walk) | 0.25 km | 1 min walk | – | – | – |
| F1 Shallows | 0.37 km | 1.0 min | 0.7 | 0.6 | 0.4 |
| F8 Saltmouth Estuary | 0.52 km | 1.5 | 1.0 | 0.8 | 0.5 |
| F3 Gullrock | 0.93 km | 2.6 | 1.7 | 1.4 | 1.0 |
| Lantern Key (The Pelican) | 1.27 km | 3.5 | 2.4 | 1.9 | 1.3 |
| Breaker Point (surf) | 1.42 km | 3.9 | 2.6 | 2.1 | 1.5 |
| F5 Wreck of the Margaret | 1.48 km | 4.1 | 2.7 | 2.2 | 1.5 |
| **Voss Harbour, round the north cape** | 3.47 km | **9.6** | 6.4 | 5.3 | 3.6 |
| Voss Harbour via Walt's Cut (after dredging) | 2.61 km | 7.3 | 4.8 | 4.0 | 2.7 |
| Sorrel Isle (cannery) | 2.82 km | 7.8 | 5.2 | 4.3 | 2.9 |
| Mango Cay (cartel) | 3.54 km | 9.8 | 6.6 | 5.4 | 3.7 |

Travel times re-measured by `maps/make_maps.py` after the Voss move *(Kufu review 31 Oct)*.

**Why A wins:**
1. **"Opposite side" is literal and the story is on the map.** The ridge hides the rival; going to Voss is a real voyage (9.6 min in Biscuit, 6.4 in the Kestrel), so it feels like another world. The Cut is the shortcut you earn: 7.3 min in Biscuit, 4.8 in the Kestrel, and it lets ships reach our deep berth P3 without rounding the north cape *(Kufu review 31 Oct)*.
2. **Starter pacing matches Codex's targets** (close = 30–60 s, premium = 3–4 min) without long boring trips: everything early sits within 4 min.
3. **The smallest streaming footprint of the three**: one compact island and 4 small islets; most cells are open ocean.
4. **The best vista:** from the office balcony you see the Cannery Flats grow, Lantern Key at sunset, and Voss Harbour's glow over the ridge at night.

### 8.2 Option B: Crescent Bay, CHOSEN *(Manan 31 Oct)*
See §8.0. The first draft's against-points: Voss far (12.4 min, now wanted: a separate world) and no Cut goal (now the Cut through the south horn).

### 8.3 Option C: Long Island Chain
![Map C](maps/map-C.png)

`maps/map-C.svg` · `maps/map-C.png`. A long, thin west–east island (3.5 × 0.7 km). Our dock is on the north shore, Voss on the south shore, and a chain of 4 buyer islets lies to the north.
- **For:** the most buyers and islands (variety for orders); the long coastline gives lots of fishing edges.
- **Against:** the island is a big continuous streaming chunk and the most art; Voss is 10.6 min away in T1 round the west tip; there's no visual link between the two ports; and islet hopping can turn into errands.

| To | T1 | T2 | T3 | T4 |
|---|---|---|---|---|
| North Shallows | 0.6 | 0.4 | 0.3 | 0.2 |
| Gullrock | 1.5 | 1.0 | 0.8 | 0.6 |
| Lantern Key | 2.6 | 1.7 | 1.4 | 1.0 |
| Sorrel Isle | 4.7 | 3.1 | 2.6 | 1.8 |
| Voss Harbour | 10.6 | 7.0 | 5.8 | 4.0 |

### 8.4 Comparison (Manan chose **B** *(Manan 31 Oct)*)
| | A Twin Coasts | B Crescent Bay | C Long Island |
|---|---|---|---|
| Rival port opposite, visitable (MUST) | ✅ 9.6 min (Kestrel 6.4), 7.3 via the Cut *(Kufu review 31 Oct)* | ✅ 12.4 min, far on purpose; 8.0 via the Cut *(Manan 31 Oct)* | ⚠️ 10.6 min |
| Starter loop within 4 min | ✅ | ✅ | ✅ |
| A story goal built into the geography | ✅ Walt's Cut | ✅ Walt's Cut through the south horn *(Manan 31 Oct)* | ❌ |
| Streaming cost (land area) | ✅ lowest (~2.2 km²) | ⚠️ ~3 km² | ❌ ~2.5 km² continuous + 4 islets |
| Vista from the office | ✅ port + islets + Voss glow | ✅ the bay bowl | ⚠️ the sea to the north only |
| Art load (Blender, MUST own-made) | ✅ | ⚠️ | ❌ |

### 8.5 World scale and Unity streaming (SUGGESTED, agreed with gdd-systems)
| Item | Value | Why |
|---|---|---|
| Sea area | 5 × 5 km; play bounded at ~2.3 km from the island by the **Shelf Current** (the boat is pushed back gently; Dot texts "Nothing out there but water and the mainland") plus a fog band | Big enough for 7–10 min crossings in T1, small enough to fill |
| Origin | The island's centre is world (0,0). Every playable point is within ±2.5 km | At 2–4 km from the origin a 32-bit float's step is 2⁻¹² m ≈ 0.24 mm (float spacing doubles every power of two), well under the ~1 mm that forum users report as visible jitter, so **no floating origin is needed**. Sources: [Unity forum: jitter ~1500 units in one project](https://discussions.unity.com/t/animators-are-micro-stuttering-when-around-1500-units-from-origin/715331), [Unity forum: large worlds](https://discussions.unity.com/t/floating-point-errors-and-large-scale-worlds/698299) |
| Streaming | Additive scenes on a **512 m grid** (10 × 10 cells, ~70 of them open ocean with no scene). Load radius is 2 cells (~1 km); islets beyond load as impostors | A T4 boat at 16 m/s crosses a cell in 32 s, which leaves plenty of time for async loads |
| Ocean | One camera-following water plane (Ponyo Coast's turquoise water shader), never per-cell | Cheapest option; no seams |
| Voss Harbour | Always loaded as an **HLOD skyline impostor** (cranes, tower, lights) visible over the ridge. The full scene loads within 1 km | It is the "bustling" MUST at every distance |
| Draw distance | Fog at 1.8 km (clear days), 300 m (fog days) | Hides streaming; sells weather |
| Target PC | GTX 1060 / 8 GB RAM class at 1080p, 60 fps (OPEN for gdd-systems' perf section) | "Modest PC" from the brief |
| Island land | ~2.2 km² for A, of which ~0.25 km² is detailed (town, dock, Voss Harbour) and the rest low-detail hills | Fits a small Blender team |

**Art contrast as story (from Manan's Ponyo Coast pick, gdd-systems' suggestion, adopted):** our side is warm and small: plaster houses, tiled roofs, washing lines, string lights, Mari's colours. Voss Harbour is the grey industrial side: Toon Harbor freighters and gantries, sodium lights, smoke. As our port grows, our cranes are painted **Hale red and cream** and our sheds stay timber, so even our industry looks hand-made. Ending 2 (The New Boss) is the only one where our side greys.

### 8.6 "Cannot dock at Voss Harbour" (MUST): how it plays
- A **buoy line** sits 150 m outside his breakwater. Crossing it brings Captain Keel's patrol boat: a megaphone warning ("Private terminal! Turn about!"), then after 10 s a nudge and a tow out, with his hose cannon from Act 4 (comedy). Before Act 3 there is no penalty; from Act 3 each crossing adds +1 Heat.
- **Reasons to go anyway:**
  - the view: freighters, 6 cranes, the VOSS tower, the gala fireworks;
  - surfing the Voss Wall;
  - photographing evidence (Act 3+);
  - Voss staff who quietly buy fish from your boat at sea ("don't tell Pratt") in Act 4;
  - delivering Walt's letter in Act 5 (boat to boat).
- **There is no walkable Voss Harbour interior** (a smaller art cost). It is a stage set seen from the water.

---

## 9. Script: the first 60 real minutes

Clock: 1 real min = 1 in-game hour. Day 1 starts at 06:55. Real minute 0 = 06:55, minute 16 = 23:00, and sleep jumps to Day 2 at 07:00. The script assumes the player sleeps at around 22:00–23:00 and does not count the sleep fade (2 s) as time.

### 9.1 Solo
| Real min | In-game | What happens | Player does | Teaches |
|---|---|---|---|---|
| 0:00 | D1 06:55 | Black → the gulls are heard first → the office, sea light through the blinds | Gets up from the cot | – |
| 0:10 | 06:57 | Walt's letter on the desk, a 6-line card. The key is used at once on the office door; the journal goes into the phone's Journal app (never the hotbar) | Reads or closes the letter | The letter channel |
| 0:30 | 07:02 | The phone buzzes LEFT: Walt's text. The prompt "TAB: phone" shows once | Opens the phone: sees the time, the forecast (Dot's "Clear morning") and messages | The phone |
| 1:00 | 07:05 | Outside: the pier, Biscuit half-sunk, the causeway and the silted Cut in view, a Voss horn beyond the ridge | Walks the pier (one plank wobbles: comedy) | Movement, the world |
| 1:30 | 07:35 | Bailing: grab the bucket, scoop (the water level visibly drops in 6 scoops) | Bails | Physical interaction |
| 2:15 | 08:15 | Pull-start: 3 pulls; the third catches, smoke, and Walt's "Is that my Biscuit" text | Starts the engine | Boat start |
| 2:30 | 08:30 | Biscuit limps at 4.5 m/s (damaged) out into the bay, 1 min away. Dot's lighthouse is visible on the north horn | Drives | Boat control |
| 3:30 | 09:30 | Journal page 1 card (side): "Cast, watch the float, wait for the dunk." The hotbar shows the rod (slot 1) | Casts; the bobber lands, bobs; a bite after 4–10 s | Casting, bobber |
| 4:00 | 10:00 | First minigame: the bar with a green zone, 3 stops. Sprat ★ | Plays | The minigame |
| 4:00–7:00 | 10:00–13:00 | Catches 4–6 fish (sprat, mackerel, mullet); fish stack to 5 per slot (MUST); slots fill up | Fishes; sees ★★ once | Stacks, rarity |
| 6:30 | 12:30 | Gulls dive over a mackerel shoal (a hotspot hint from gdd-fishing) | Moves to the birds | Reading the water |
| 7:00 | 13:00 | Walt texts: "Pratt'll lowball you. Sell a few anyway, the engine won't fix itself." (Mari no longer texts on Day 1 *(Manan 31 Oct)*: Day 3, or at $250 earned) | Reads | Phone texts |
| 7:30 | 13:30 | Heads home; ties up; walks to town (1 min) | – | – |
| 8:30 | 14:30 | Fish Market: Pratt's lowball line. Sells some fish. Walt texts about Pratt and The Pelican | Sells | Selling, the price gap |
| 9:00 | 15:00 | Back at the Landing: empty berth B1 and Walt's old hand-painted sign "BERTHS FOR RENT" | Looks | The dock can earn (sets up Mari) |
| 9:30 | 15:30 | Hook, Line & Sinker: June gives free worms and sells shrimp (locked until tomorrow) | Looks at the baits | Bait economy |
| 10:00 | 16:00 | Doyle Boatworks: Fergus quotes **$90** for Biscuit's engine, "parts only, for Walt" *(Manan 31 Oct)*; after the sale there's ~$95–110, so it's affordable straight away. 1 in-game hour (economy §2.3: the first purchase at minute ~9) | Pays | First upgrade |
| 10:30–14:00 | 16:30–20:00 | Golden hour (Ponyo light) fishing in the bay at full speed (6 m/s from 17:00); first ★★★ attempt | Fishes | Feels the upgrade |
| 14:00 | 20:00 | Dusk: the Landing's lamp turns on; Mari's lights; the Voss glow over the ridge | Keeps the fish for the morning market (rot ≥ 2 days) | Rot timer (MUST) |
| 15:00 | 21:00 | Walt: "lights out here. Dont stay up" | Sleeps in the cot | Sleep → 07:00 (MUST) |
| 15:10 | D2 07:00 | Sells last night's fish at Pratt's (Good, 85%). Dot's forecast: "Rain 17–22" | Sells | Daily rhythm |
| 15:30 | 07:30 | June's shrimp is in. Buys shrimp | Buys bait | Bait types |
| 16:00 | 08:00 | ★ Walt's call (optional, 30 s) | Answers or not | Calls |
| 16:30–21:00 | 08:30–13:00 | Fishing with shrimp at Gullrock (1 min, in the bay) | Fishes | Bait → which fish bite |
| 19:00 | 11:00 | Fergus: "Hull's still weeping. $120" (if cash ≥ $120, a pay prompt) *(Kufu review 31 Oct)* | Pays | Second upgrade |
| 20:00 | 12:00 | Pratt's daily price list text arrives | – | – |
| 21:30 | 13:30 | Amara's contract: 3 mackerel ★★+ by tomorrow 20:00, $30 each *(Kufu review 31 Oct)* | Accepts | Contracts |
| 22:00 | 14:00 | Sells at Pratt's before he closes at 16:00. Total earned passes $250: **Mari's text (MUST)**, LEFT toast *(Manan 31 Oct)*. Accept or Not yet; she arrives within the hour, we catch and tie her line, string lights and music | Sells, accepts, ties the line | First income |
| 22:00–27:00 | 14:00–19:00 | Rain from 17:00 (Dot: "Some fish like the rain") | Fishes | Weather → fishing |
| 27:00 | 19:00 | Ingrid's offer: "3 planks fixed = bench free" | Buys planks | Dock upgrades |
| 28:00 | 20:00 | Night: a conger's head pokes out from under the old pier. June (text): "Not on Walt's bamboo, love. Fibreglass." | Optional | Rods open new fish *(Manan 31 Oct)* |
| 30:00 | 22:00 | Sleeps | – | – |
| 30:10 | D3 07:00 | **Hull patched: no more bailing.** Mari's first rent toast (+$40) | Notices the dry deck | Upgrade reward, daily income |
| 31:00–36:00 | 07:00–12:00 | Hunt for ★★ mackerel (needs 2+ hits) | Fishes | Mastery |
| 36:00 | 12:00 | Sails to Lantern Key (3.5 min): the first long trip; Lantern Key's cliffs and The Pelican's terrace | Sails | Travel |
| 39:30 | 15:30 | Delivers to Amara: "Good. Not great. Good." ~$90 *(Kufu review 31 Oct)*. Snapper teaser | – | The premium buyer |
| 40:00 | 16:00 | On the way back, a swordfish leaps far out past Lantern Key. Dot: "Not on that rod." | – | Future rods |
| 43:00 | 19:00 | Walt: "AMARA CALLED ME" | – | The emotional beat |
| 44:00 | 20:00 | Plank & Post: patch Walt's warehouse ($300, 24 slots of 10 fish) or a first piece of furniture; it installs itself overnight *(Manan 31 Oct)* | Buys | Storage, upgrades |
| 46:00 | 22:00 | Sleeps | – | – |
| 46:10 | D4 07:00 | Mari's rent (+$40). **Walt's letter with the photo (Act 1 emotional moment).** Fog (forced light fog) | Reads | – |
| 47:00 | 07:30 | Voss's yacht *Efficiency* glides past in the fog, a 4 s beat with input live | Watches | The rival exists |
| 48:00–58:00 | 08:00–18:00 | Free play: contracts, worm bait vs shrimp tests, a first look at Breaker Point (Kai's text "come surf sometime") | – | – |
| 58:00 | 18:00 | Hugo & Fifi's moorer request (Act 2 starts) | Accepts | Growth |
| 60:00 | 20:00 | End of hour 1: Biscuit fixed; Mari + 1 moorer request; 1 premium buyer; the warehouse patched; ~$250–370 cash (economy §2.3 re-run *(Manan 31 Oct)*: $253 first-timer, $373 practised at Day 3 18:00) | – | – |

### 9.2 Co-op (2–4 players) differences
| Real min | Co-op version |
|---|---|
| 0:00 | Each player wakes in their own spot: the host on the cot, friends on a sleeping bag, a hammock and the floor ("the crew"). Walt's group text welcomes each friend by name as they join |
| 1:30 | Bailing is faster with 2 buckets. **Role split:** one bails, one tries the pull-start (and fails comically until the water is out) |
| 2:30 | Biscuit holds 2 fishing spots plus the driver; with 3–4 players, the extras fish from the pier into the bay, or ride along and fight over the rod (comedy: there are only 2 rod holders on Biscuit) |
| 4:00 | **Shared catches:** fish go into the catcher's own hotbar (MUST: stack 5 per slot). Players can hand fish over by throwing them (a physics catch: clip moment #5 from market.md) |
| 10:00 | Money is a **shared company account**, so the $90 engine *(Manan 31 Oct)* is bought even sooner. **Prices never scale with players**; contract order sizes do (×1.0 / 1.6 / 2.1 / 2.5 for 1–4, economy) *(Kufu review 31 Oct)* |
| 15:00 | **Sleep (MUST):** any one player sleeping skips to 07:00. The others get a 5 s "Pat is sleeping, the night will pass" toast on the LEFT, with a cancel only if they're mid-minigame (the skip waits for the minigame to end) |
| 16:30 | **Split up:** one player takes Biscuit to Gullrock while the others shop, fish the pier, or run Mari's order on foot |
| 22:00 | (Day 2, at $250 earned *(Manan 31 Oct)*) Mari's text goes to the group chat; **any** player can accept. A vote prompt appears only if two players answer differently within 10 s (the first answer wins after that) |
| 23:00 | Mari's line is a **two-person catch**: she throws it short, so one player catches it while another holds the bollard (a fail drops it in the water and she laughs and throws again) |
| 36:00 | The Lantern Key trip: one drives, one keeps the fish fresh (an ice bucket once Ola's barge arrives; early on, just hold them, since rot is slow) |
| 47:00 | The yacht beat plays on every screen at once; whoever is nearest the water gets the best view |
| 48–60 | Co-op crews typically reach the Act 2 moorer request ~5 min earlier (two income streams) |

---

## 10. Later content lines (how each hooks into the story)

### 10.1 The cartel smuggling path (MUST as a planned later line; SUGGESTED details)
- **Hook:** Day ~28 (Act 4), when we own a T4 fast boat and Heat ≥ 50. A coconut with a phone inside washes onto our pier. Teddy Mango: "Hello, friend. Voss's patrols are bad for my vibes. Yours too, I hear. Let's be friends. Friends do favours. 🥭"
- **Structure:** 3 tiers of favours. Optional; refusing has no penalty except Teddy's sad voice notes.
| Tier | Favour | Gameplay | Risk |
|---|---|---|---|
| 1 "Coconuts" | Pick up a floating package at night off Mango Cay; drop it at a buoy | A night run, lights off (a new boat toggle) | Captain Keel's patrol searchlight sweeps; if caught, the package is dumped and lost, and Teddy is disappointed |
| 2 "Fruit salad" | Hide packages inside a fish delivery to The Pelican's back door | Packages take hotbar slots (they mix with fish stacks: tension) | Constable Okafor's random dock checks; a caught package costs a fine and ally rep |
| 3 "The big mango" | Run a package through Voss Lanes during the Voss gala (patrols busy) | A chase: outrun a Voss patrol boat at night | Big money, big Heat |
- **Payoff:** money, an exclusive boat paint ("Mango Sunset"), Teddy's ukulele as a collectible, and the **dark ending variant** (§4.7).
- **Cost:** each favour lowers **Island Trust** (ally reputation) a little, so going deep in the cartel makes Ending 1 harder.
- **Guns (MUST: guns in the hotbar):** in this line, guns appear as a **flare gun** (signal, scare gulls, light the water) and a **net gun** (stop a pursuing boat's propeller). Teddy gifts a gold-plated flare gun. OPEN Q3: real guns.
- **Tone guard:** Teddy is never seen hurting anyone; the comedy is all in his calm wording. OPEN Q4: drugs named or winking.

### 10.2 Port and shipping (MUST)
- **Story hook:** Act 3's first ship order, then Act 4's crane and shipping boats; Walt's Cut decides the scale. Before the Cut reopens, only small coasters berth (they come round the north cape). After the Cut reopens, feeder ships come through from the east lanes.
- **Ship lines as characters (SUGGESTED):** each shipping line has a captain contact with a personality and a cargo type:
  - **Captain Rosa Ferreira** (cars, fussy about scratches);
  - **the Okafor Line** (fruit: reefer, fast rot, so speed matters);
  - **Old Tam** (timber barge, slow, chatty);
  - **Lars Holm** (containers; a stopwatch man).
  Winning a line away from Voss is a Heat event and a world change: their flag flies on our office roof (a nod to the replan's "Win the Coast" premise A).
- **Consignments (MUST):** our shipping boats leave the map for Port Ardent and other mainland ports; the phone texts updates ("Rounding the cape", "Delivered, returning"). Rival bidding (R9) competes for the same consignments.

### 10.3 Surfing (MUST: surf shop exists)
- **Hook:** Kai's "Dawn Patrol" invite in Act 2. Surfing is a **leisure + reputation + event** line, not an income line.
- **Spots:** Breaker Point (beginner, daily swell report), Gullrock's Widowmaker (advanced, big swell after storms, linked to weather), the Voss Wall (forbidden, at Voss's breakwater: you get hosed, and photos of you surfing it raise Voss's annoyance and the town's love).
- **Story ties:**
  - the **Saltmouth Swell Classic** (event, §10.4);
  - winning gives the "Kai vouches" bonus (the youth vote in the Charter counts as part of the Bench seat);
  - storm days create the best surf (risk vs reward);
  - tourists from Mags' ferry book **surf lessons** (charter contracts).
- **Gear** from Swell Surf Co.: boards (foam → shortboard → gun), wetsuits (colour customisation), wax. Fishing gear and the rod stay in the hotbar; boards are worn on the back (OPEN for gdd-systems: does the board take a hotbar slot?).

### 10.4 Events and seasons (SUGGESTED; OPEN Q6 for seasons)
| Event | When | What | Story hook |
|---|---|---|---|
| **Lantern Festival** | Every 28 days (summer), starting after Act 2 | Lanterns on the water, Mari's food stalls, night fishing contest | Maggie's tradition; Walt texts a photo of the old festival; Ending 1 reopens it in full |
| **Saltmouth Fish Derby** | Once per act from Act 2 | Biggest fish in 2 in-game hours, town-wide | Counters R12 (the smear) |
| **Saltmouth Swell Classic** | After storm days, Act 3+ | Surf contest at Breaker Point | Kai, the youth vote |
| **Storm Week** | Act 5, forced | 3 storm days in a row, then the great storm | The finale |
| **Voss Gala** | Act 5, plus a yearly event in endless | Fireworks over Voss Harbour, visible from the Cut | R13; the compass moment |
| **Regatta** | Endless, every 28 days | Boat race around the island (through the Cut, if open) | Endless rival rematches with Voss's yacht |
| **Full moon** | Every 7 days | Night fish feel livelier (anglerfish, swordfish, conger) | Walt's "full moon, Maggie's moon" text |
| **Seasons** (if adopted) | 4 × 7 days | Spring (shoals), Summer (festival, calm), Autumn (storms, big fish), Winter (fog, rare fish, short days) | Weather and fish tables shift (gdd-fishing and gdd-systems) |

---

## 11. A living world

### 11.1 Day/night beat (times from gdd-systems: sunrise 06:00, sunset ~20:00, shops 07:00–20:00; lighting states owned by gdd-systems)
| In-game time | Real min into the day | World beat | Sound |
|---|---|---|---|
| 05:00–06:00 | –2 to –1 | (only if awake) Bao bakes; Kai at Breaker Point | Waves, one radio |
| 06:00 | – | Sunrise: Ponyo dawn pink, cumulus lit from below | Gulls start |
| 06:30 | – | Dot's forecast text | – |
| **07:00** | 0 | **Wake/sleep anchor.** Mari's rent; Mags' ferry horn and mail; worker boats leave; Pratt's prices; the shops open | Ferry horn, ka-ching |
| 07:00–10:00 | 0–3 | **Morning market:** busiest town, Bench Council seated, best mackerel bite | Market chatter |
| 12:00 | 5 | Noon: shops' lunch rush, worker boats mid-sea; the Voss freighter departs (horn over the ridge) | Freighter horn |
| 14:00–17:00 | 7–10 | Afternoon: wind picks up (gdd-systems' Breezy chance), surf report changes | – |
| 17:00 | 10 | Worker boats return; Mari's dinner rush begins | Music from Mari's boat |
| 18:00–20:00 | 11–13 | **Golden hour** (Ponyo warm light): the most beautiful fishing; tourists on the pier | – |
| 20:00 | 13 | Sunset: shops close; lamps on; beds usable | Lamplighter clinks |
| 21:00 | 14 | Walt's goodnight text; the bay's plankton glows on dark nights; Voss Harbour's sodium glow peaks over the ridge | Night insects |
| 23:00 | 16 | "Getting tired" toast (gdd-systems) | – |
| 23:00–02:00 | 16–19 | Night: night fish; rival night actions are telegraphed for this window; Old Lowe's rounds | Foghorn on fog nights |
| 02:00 | 19 | Pass-out (counts as sleep → 07:00, gdd-systems) | – |

### 11.2 NPC routines (SUGGESTED; each NPC needs 3–5 waypoints only)
| NPC | 07–10 | 10–14 | 14–17 | 17–20 | 20–23 | Night |
|---|---|---|---|---|---|---|
| June | Shop counter | Shop | Pier, fishing (her rod, her chair) | Shop | The Mooring Line, 1 pint | Home |
| Mari | Prep on her boat | Serving | Nap in a hammock (closed sign) | Dinner rush | Serving + music | Sleeps aboard |
| Fergus | Slipway, welding sparks | Workshop | Test-drives a boat round the bay | Workshop | Pub | Home |
| Ingrid | Yard, sawing | Delivering on her cart | Yard | Yard | Home | – |
| Rosie | (closed) | Opens at 11 | Bar | Bar | Bar, quiz night on Day % 7 = 3 | Closes at 02 |
| Dot | Lighthouse gallery with binoculars | Harbour office (town) | Harbour office | Walks the causeway | Lighthouse | Lighthouse (light on) |
| Bench Council | **Bench** | Bench | Café | Bench | Home | – |
| Pratt | Fish Market | Market | Phone calls on the causeway | Market closes | Drives to Voss Harbour | – |
| Tiny | Beach, digging worms | Pier | Fishing with a stick | Home | – | – |
| Kai | Surf (if swell) / hut | Shop | Shop | Breaker Point | Bonfire on the beach (Fri) | – |
| Workers | Leave at 07:00 | At sea | At sea | Return at 17:00 | Pub or bunkhouse | – |
| Goons (Act 3+) | Causeway, leaning | Fish Market | Their rowboat under the causeway (findable) | Voss Harbour | Night actions (if active) | – |

**Rule (SUGGESTED):** routines bend to weather. In rain, the bench is empty and everyone is in the café or pub; in storms, the shops shut early (16:00) and NPCs stand in doorways watching the sea; on fog mornings the Bench Council gossips louder. This is cheap, and it sells a living island.

### 11.3 Weather tied to story and fishing (gdd-systems owns the states and the forecast; gdd-fishing owns the fish effects)
| Weather | Fishing (from gdd-fishing) | Story / world use |
|---|---|---|
| **Clear** | Mackerel, mahi, sunfish like clear days (×1.5) | Default; most story days |
| **Fair** | Normal | – |
| **Breezy** | Choppy; surf is up | Kai's swell texts; harder boat handling for Biscuit |
| **Fog** | No fish effect (it just looks great) | **Forced on Day 4** (Voss's yacht) and **Day 13** (the goons, MUST beat). Foghorn; 300 m draw; best for the cartel's night runs |
| **Rain** | Blue shark and Old Brass like rain (×1.5 nudge, never a lock *(Manan 31 Oct)*) | Bench empty; the café full; Dot: "Fish bite in the rain. People don't." |
| **Storm** | Giant oarfish likes storms (×1.5 nudge *(Manan 31 Oct)*) | **Forced Day 20** (Biscuit dies at sea), **Storm Week + the great storm in Act 5** (the finale). Dot calls ≥3 in-game h ahead. Rescue contracts spawn. The Widowmaker surf is huge the next day |

**Rule (MUST-adjacent):** sleeping moves the weather on (Manan's rule). A forced story weather day overrides the roll only on its day, and gdd-systems' forecast shows it like any other day, so story weather never looks scripted.

### 11.4 The world changes as we grow (the real progress meter)
| Milestone | Our side | Old Saltmouth | Voss Harbour |
|---|---|---|---|
| Prologue | One lamp, a rotten pier | Half the shutters down | 6 cranes, full lights |
| Act 1 end | Mari's string lights, 3 new planks | – | – |
| Act 2 end | 3 moorers, a restored office, a worker boat | The café reopens; washing lines appear | – |
| Act 3 end | 6 berths, lamps, a watchman's hut | 2 more shops reopen; the Bench gets a new bench | The goons' rowboat is seen under the causeway |
| Act 4 end | A red-and-cream crane, a container yard, trucks; the Cut half-dug | High street full; tourists | 1 crane dark at night |
| Act 5 / ending 1 | Full port; festival lanterns; ships through the Cut | The Lantern Festival is back | Smaller, quieter |
| Ending 2 | Grey takes over; the VOSS letters come down and HALE goes up | Busy but colder; Mari gone | Yours |
| Ending 3 | Shared port; Hale red on Voss cranes | Festival + Voss-sponsored stalls | Lights back on; the two ports trade |

---

## 12. Sources (all accessed 2026-10-31)
- Market research and the light-frame lesson: `../../replan/market/market.md` §4 (Docked's intrusive story; 11 of 13 hits use a light frame).
- Earlier premises reused (Marlowe, Mags, Dot, Pim, Gull FM, captain flags, the town reopening): `../../replan/story/story.md`.
- Stardew Valley grandpa framing (a letter at the start, his return to judge the farm in Year 3): [Stardew Valley Wiki: Grandpa's Evaluation](https://stardewvalleywiki.com/Grandpa%27s_Evaluation).
- Schedule I story through the phone (Uncle Nelson's calls, the Benzies' texts and ultimatum): [Destructoid: how to meet the cartel](https://www.destructoid.com/how-to-meet-and-interact-with-the-cartel-in-schedule-1/), [Prima Games: how the cartel works](https://primagames.com/tips/how-does-the-cartel-work-in-schedule-1).
- Dredge story through NPCs and messages in bottles, and a fishing day/night cycle where the night is dangerous and lucrative: [Wikipedia: Dredge](https://en.wikipedia.org/wiki/Dredge_(video_game)), [Dredge wiki: Time](https://dredge.wiki.gg/wiki/Time), [StrategyWiki: DREDGE messages](https://strategywiki.org/wiki/DREDGE/Messages). Reviewers' complaint that Dredge's day cycle is too fast supports our 24-min day.
- Sea of Thieves: world size is tied to encounter frequency, not area: [Wccftech: Rare on map size](https://wccftech.com/sea-of-thieves-map-size-linked-frequency/amp/). Unverified area figures were not used.
- Unity float precision and jitter reports: [Unity Discussions: animators stutter ~1500 units from origin](https://discussions.unity.com/t/animators-are-micro-stuttering-when-around-1500-units-from-origin/715331), [Unity Discussions: floating point errors and large worlds](https://discussions.unity.com/t/floating-point-errors-and-large-scale-worlds/698299). The float step math (2⁻¹² m between 2,048 and 4,096 m) is IEEE-754 single precision: a 23-bit mantissa.
- Codex concept and map (input): `../SOURCE-game-concept-and-manan-notes.txt`, `../SOURCE-codex-starter-map.png`.
- Fish names: `../fishing/fish-roster.csv` (gdd-fishing). Time, weather and the phone: gdd-systems (messages 2026-10-31). Prices: gdd-economy (pending).

## 13. Files in this section
| Path | What |
|---|---|
| `world/world.md` | This document |
| `world/maps/map-A.png` / `.svg` | Option A: Twin Coasts (first draft's pick, not chosen *(Manan 31 Oct)*) |
| `world/maps/map-B.png` / `.svg` | **THE CHOSEN MAP: B, Crescent Bay** *(Manan 31 Oct)* (§8.0) |
| `world/maps/map-C.png` / `.svg` | Option C: Long Island Chain |
| `world/maps/make_maps.py` | Generates the SVGs and prints the measured travel-time table (`python make_maps.py`) |
| `world/maps/render.sh` | SVG → PNG with headless Chrome |
