# Port Boss GDD: SYSTEMS + UI + TECH + ROADMAP

Worker gdd-systems, 2026-10-31, task t-da5ac9e4. This is planning only: nothing is built until Manan is satisfied with the whole plan.

**Tags.** **MUST** = Manan's own words (BRIEF MUST list, his notes, or a decision he has made). **SUGGESTED** = ours. **OPEN** = an OPEN QUESTION for Manan; all of them are batched in §6.

**Canon this section uses** (so all four sections match):

| From | What |
|---|---|
| Manan, 2026-10-31 (via Kufu) | **Art = #4 Ponyo Coast** (soft cel, painted blue-violet shade, towering cumulus, luminous turquoise sea, soft glow, paper grain) with light that changes through the day. **Characters = the 6 presets** (Bruno, Stretch, Pip, Rani, Old Gus, Sully); players edit **clothes only** |
| gdd/world | Saltmouth Island; our dock **Hale's Landing**; the town **Old Saltmouth**; the rival **Adrian Voss**, whose **Voss Harbour** sits on the east side (no docking); Grandpa **Walt**; the cast (Mari, June, Fergus, Ingrid, Rosie, Dot, …); the 5 × 5 km sea; 512 m streaming cells; the Heat rival system |
| gdd/fishing | Rules R1–R10 (bite 4–10 s, 3 stops, every hit = +1 grade: **Common / Fine / Rare / Prized**, price ×1.0 / 1.3 / 1.7 / 2.5); rot = exactly 48 in-game h; F1–F13 build steps; bait sits in a pouch on the rod |
| gdd/economy | Shop names (Hook, Line & Sinker; Doyle Boatworks; Plank & Post; The Mooring Line; Swell Surf Co.; Hartley's Outfitters); boat roster (Biscuit T1 … Halcyon); **one shared company bank**; co-op order multipliers 1.0 / 1.6 / 2.1 / 2.5; all prices |
| This section (agreed with the others) | The clock: **1 real second = 1 in-game minute, so a day = 24 real minutes**. Sunrise 06:00, sunset ~20:00, new day / sleep anchor **07:00**. The 6 weather states: **Clear, Fair, Breezy, Fog, Rain, Storm** |

**Files in this folder**

| Path | What |
|---|---|
| `systems.md` | this document |
| `mockups/00-design-system.png` | fonts, type scale, colour tokens, the 6 lighting states, motion rules |
| `mockups/01-hud.png` | the HUD, daytime: notifications top-LEFT (no clock card *(Manan 31 Oct)*), hotbar, compass, bank |
| `mockups/02-hotbar.png` | hotbar spec: inspect card, freshness per fish, full hotbar, giving, controller |
| `mockups/03-phone.png` | the phone, 4 screens: Home, Messages (Mari's mooring request = the first income), Crew, Map |
| `mockups/04-shop.png` | shop screen (June's Hook, Line & Sinker). The same layout serves every shop |
| `mockups/05-minigame.png` | the fishing minigame bar, at dusk |
| `mockups/06-catch.png` | the catch reveal |
| `mockups/07-wardrobe.png` | the wardrobe: pick 1 of the 6 crew, then change clothes only |
| `mockups/*.html`, `tokens.css`, `icons.js`, `render.sh` | the mockup sources. `tokens.css` is the token sheet that becomes `Tokens.uss` in Unity. `render.sh` re-renders every PNG with headless Chrome |

The mockup backgrounds are the art-direction #4 concept and renders (stand-ins for the real game scene). All UI on top is ours: HTML/CSS, the OFL fonts, and hand-made SVG icons. No icon packs and no AI images in the UI.

---

## 0. One-page summary

1. **Hotbar (MUST):** 8 free slots for the rod, fish (stacks of 5 by species + grade), gear guns, collectibles and tools. The phone is never in it. Bait lives in a pouch on the rod. Nothing you own is ever lost for good: no death, no item loss, and dropped gear washes up in the Lost & Found box. Storage = **the warehouse at our dock, from Day 1, expandable** *(Manan 31 Oct)*.
2. **Phone (MUST, Tab):** Grandpa Walt's old phone. The world keeps running while it's open. Apps: Home (time top-left, forecast), Messages, Contracts, Crew (hire and assign), Map, Bank (+ Ledger), Forecast, Journal, Settings, and Camera from Act 3.
3. **Time:** 24 real min per day. The waking day runs 07:00 → 02:00 (19 real min). Solo pauses only on the Esc menu; co-op never pauses.
4. **Sleep (MUST):** one player sleeping skips everyone to 07:00 and rolls the weather on. There's a 5 s heads-up for the others, and an edge-case table (§1.4) says where everybody wakes.
5. **Weather:** 6 states in 3 forecast blocks per day, seeded per (world, day). Storms are never a surprise (Dot calls them ≥ 3 in-game h ahead). Forecast accuracy is 95 / 85 / 75 % by block.
6. **Boats:** life-size walkable decks. Throttle notches + rudder, an anchor, a kill cord, mooring snap, buoyancy from the same waves the GPU draws, no capsizing and no sinking. Falling off means a swim, the life ring and the stern ladder. A cruise lock lets a solo player troll.
7. **Guns (MUST as items):** **tools, not weapons**: flare gun, net gun, harpoon gun. They never hurt a person. Tone flagged as OPEN.
8. **UI (MUST premium):** Ponyo Coast "letter paper": warm cream cards, a 2 px warm-brown line, blue-violet shadows. **Fraunces** (soft serif, OFL) + **Zen Maru Gothic** (rounded, OFL). Notifications on the **LEFT** (MUST), at the top-left. **No HUD clock** *(Manan 31 Oct)*: the time is read on the phone (Tab), on Home and Forecast.
9. **Solo vs co-op:** everything works solo (the anchor, the cruise lock, rod holders, workers). Co-op adds roles that nobody is locked into. One shared bank, personal hotbars. Drop-in rejoin. Every player keeps a world copy, so anyone can host ("soft host migration"). Free proximity voice through Steam (Facepunch).
10. **Tech:** keep Unity 6 URP + NGO + Facepunch Steam, the tools and the test harness. Throw away the dock-work game (yard, crates, IMGUI, captains). An own Gerstner ocean + CPU buoyancy (free), UI Toolkit, JSON saves. **Blender is the level editor** (placements are exported as JSON). One palette atlas per asset set, tri budgets per asset.
11. **Roadmap (MUST one thing at a time):** M0 foundations → **M1 boat + fishing (Manan's first build)** → M2 co-op → M3 the day (time, sleep, weather, light) → M4 phone + first income + save → M5 shops → M6 workers → M7 demo polish → M8 surf + gear guns → M9 rival → M10 port → M11 shipping + finale → M12 EA. A demo by ~mid-April 2027 is tight for the June 2027 Next Fest (OPEN Q7).

---

## 1. Player systems

### 1.1 The 8-slot hotbar (MUST)

#### 1.1.1 Rules

| # | Rule | Tag |
|---|---|---|
| H1 | 8 slots, shown bottom-centre at all times (minimal-HUD mode keeps it). Keys 1–8, the wheel, or LB/RB | **MUST** (8 slots) |
| H2 | It holds the fishing rod, fish, guns and collectibles. **The phone is never in it** | **MUST** |
| H3 | Fish stack to **5**. A stack = one species + one grade. Each fish keeps its own catch time (rot clock) | **MUST** (5), SUGGESTED (species + grade, agreed with fishing) |
| H4 | Fish go in the hotbar, **never in crates** | **MUST** |
| H5 | No slot is locked to an item. The rod can live in any slot | SUGGESTED (agreed with fishing) |
| H6 | Bait is **not** a hotbar item: it sits in a bait pouch on each rod (hold R / Y), so it doesn't eat a slot | SUGGESTED (fishing OPEN Q2) |
| H7 | The selected item is in your hands. A long item (rod, harpoon) shows on your back when it's not selected; small items don't show | SUGGESTED |
| H8 | **Nothing you own is lost for good.** No death and no item loss on a knock-down or a fall into the sea. Fish only go if you release, sell, give or drop them, or let them rot. Gear dropped overboard floats, then washes up in the Lost & Found box at the office | SUGGESTED (fits world's "Voss never steals hotbar items") |
| H9 | There is no hidden backpack. The hotbar is the whole inventory on your body. Storage = the warehouse (§1.1.6) *(Manan 31 Oct)* | SUGGESTED (Manan's 8-slot design) |

**Item categories**

| Category | Examples | Stack | Notes |
|---|---|---|---|
| Rod | Grandpa's Rod (bamboo), later rods (gdd/fishing §6.2) | 1 | Carries a bait pouch + tackle (line, reel, hook, bobber) |
| Fish | 15 species, 3 per rod (gdd/fishing §5.2) *(Manan 31 Oct)* | 5 | Freshness bar per stack (oldest fish), grade dot |
| Gear guns | Flare gun, net gun, harpoon gun (§1.8) | 1 | Ammo is internal (the count shows on the slot) |
| Collectibles | Shells, sea glass, Walt's message bottles, story items (Marlowe's card) | 10 | Count towards the Journal. Story items can't be dropped |
| Tools | Landing net, cast net, patch kit, binoculars, surfboard | 1 (patch kit 5) | A surfboard in hand changes the walk to a carry pose |
| Cargo (later) | Packages, Mango's duffel (cartel line) | 1 | Heavy: walk-only while held |

#### 1.1.2 Picking up
- **Fish from the catch:** the reveal card (`06-catch.png`) shows where it goes ("Goes to slot 4"). Merge order: (1) a matching stack (same species + grade) with room; (2) the first empty slot from the left. The slot pops (scale 1.08, 120 ms), with a soft "plip" and a "+1" tick.
- **Items in the world** (on a deck, a pier, washed up): look at it within 2.2 m. A prompt shows "E · Pick up · → slot 6". Picking up all of a kind: hold E for 0.4 s.
- **Fish on a deck** slide with the deck's roll (they're physics objects in the boat's local space, §4.6). Gulls may steal one after 20 s at Gullrock Reef (gdd/fishing §6.6).
- **Auto-pickup:** none, except your **own** dropped fish within 1 m when you walk over it (saves fiddling on a rolling deck). It's a setting, on by default.

#### 1.1.3 Dropping, swapping, rearranging

| Action | KB/M | Pad | What happens |
|---|---|---|---|
| Drop one | Q | D-pad ↓ | It drops in front of you; on a boat, onto the deck |
| Drop the stack | Shift+Q | hold D-pad ↓ 0.5 s | The whole slot |
| Drop a Prized fish or a story item | hold Q 0.5 s | hold D-pad ↓ 0.8 s | Accident guard; story items can't be dropped at all |
| Rearrange | I opens **Bag mode**: the hotbar grows ×1.4 and you drag with the mouse | D-pad ↑ opens Bag mode: A picks up, A puts down, B leaves | Swap, merge stacks (same species + grade), split (right-click / Y = take half) |
| Quick swap | Ctrl + 1–8 | – | Swaps the selected slot with slot N |

- **Over the side:** a fish dropped into the sea **swims away**. It's gone, and you hear a happy splash and see "It's free!" in grey on the left. That's the one deliberate way to lose a fish, so the 0.5 s hold guard applies when you're at a rail. Gear and tools float with a bobbing marker for 10 in-game minutes, then wash up in **Lost & Found** at the office (a phone toast tells you).
- **Toss to a mate (SUGGESTED, the market's "bucket brigade" clip):** hold Q with a fish to aim a gentle arc (max 6 m). A crewmate facing it within a 25° cone catches it automatically if they have room ("caught!" ding). A miss lands on the deck, or in the sea, where it swims away. It's a pure comedy and teamwork moment, never required.

#### 1.1.4 The full hotbar
- **On a catch:** the fish waits in your hands for 10 s after the reveal. The prompt reads "Hotbar full · 1–8 swap · Q drop · E let it go" (`02-hotbar.png`, "Full hotbar"). After 10 s it goes back to the sea, with a grey "Let go" toast, so nothing is ever silently destroyed: the player always saw it coming. Fishing §3.12 already handles the bite side (the hotbar isn't checked at the bite).
- **On a pickup:** "Hotbar full" (poppy toast) and the item stays where it is.
- **Selling with a full hotbar:** fine. A sale empties slots.
- **Shops:** a bought hotbar item (net gun, patch kit) goes to a free slot. With no free slot, it goes to the warehouse, and the toast says so.

#### 1.1.5 Knock-downs, falls and passing out (there is no death)

| Event | Items | Where you end up | Tag |
|---|---|---|---|
| Knocked down (a goon shove, a rogue wave, a friend's net gun) | Kept. The held item stays in hand; a rod mid-wait auto-reels (fishing §3.12) | Ragdoll-lite 0.8–1.5 s, get up (feel spec) | SUGGESTED |
| Knocked down during the minigame | The fish escapes, bait lost (fishing §3.12) | – | fishing |
| Overboard / fall off a pier | Kept, wet. Fish don't care | Swim (§1.6.7) | SUGGESTED |
| Out of stamina in the water | Kept | **Dot's fish-out:** a 2 s cartoon tow to the nearest own boat or ladder, then a "Dot: Again?" toast. No fee at T1; tier 2+ costs a small tow fee (economy) | SUGGESTED |
| Pass out at 02:00 | Kept | §1.4 | SUGGESTED |
| Disconnect | Kept in the world save under your SteamID | Rejoin restores slot contents and position (§3.4) | SUGGESTED |

**Why no death:** Manan asked for an *experience* that's fun solo and in co-op. Death and drop-on-death (Valheim style) punish solo players hardest and make clips sour. The tension comes from rot, weather, the rival's telegraphed actions and money, never from losing an hour of fish.

#### 1.1.6 The warehouse: our one storage place *(Manan 31 Oct)*
**MUST (Manan 31 Oct):** a warehouse from the start, and it is expandable. Walt's run-down warehouse stands next to the office at Hale's Landing on Day 1. It replaces the separate chests, the ice chest, the insulated satchel and the boat live well: one place to store, one thing to upgrade. It belongs to the company (economy §8), so anyone in the crew can open it. Upgrades are bought at **Plank & Post** and **install themselves overnight** (Manan 31 Oct: no placing, no editor).

| Tier | Name | Price | When | Slots (10 fish each, MUST) | Cold room (fish rot at 25%) | Looks like |
|---|---|---|---|---|---|---|
| 0 | Walt's Old Shed | free | Day 1 | 12 | – | Sagging roof, one working light, gulls inside |
| 1 | Patched Warehouse | $300 | Day 1–2 | 24 | – | New planks, racks on the walls |
| 2 | Cold Room | $2,500 | Act 2 | 24 | +12 | An insulated back room, frosty door |
| 3 | Big Warehouse | $8,000 | Act 3 | 48 | +24 | Extended shed, a forklift lane for the workers' catch |
| 4 | Ice House | $30,000 | Act 4 | 96 | +48 (rot at 10%) | A proper port store with our sign |

- **A slot holds 10 fish** of one species + grade (MUST, Manan 31 Oct), or one tool / rod / gun, or a collectible stack. Bait overflow (above the pouch's 99) also lands here.
- **Rot:** fish on the open racks rot at normal speed (still ≥ 2 days, MUST). Only the cold room slows it. Ola's Ice (economy) makes the open racks 50% slower.
- **Workers' catch** goes to the warehouse (the cold room first when built), which answers the old storage question for workers.
- **Boat fish box** (Kestrel+, economy Q1) stays a separate OPEN question: it's boat storage, not dock storage.
- **Lost & Found box** stays in the office (gear only, take only).

**Warehouse UI:** walk up to the door and press E. The warehouse grid opens above your hotbar as a paper panel. Click moves one stack; Shift-click moves every stack of that species; "Store all fish" is one button; "Take" works the same way back. On a pad, the cursor jumps between the two grids with the D-pad, and A moves. The world keeps running.

#### 1.1.7 Giving and trading in co-op
- **Give (agreed with fishing §4.2):** look at a crewmate within 3 m and **hold E for 0.4 s** (pad: hold X). One fish (or one item) goes over. **Shift + hold E** gives the whole stack. If they have no room, they get a "Hotbar full" toast and nothing moves.
- **There's no trade window.** All money is the shared company bank, so trading is just giving. That keeps co-op generous and simple.
- **Personal items** (rods, clothes, tools a player bought) belong to the buyer (economy §8). A personal rod can be lent by giving it, and the Ledger records the lender, so it goes back to them if they leave the world.
- Every transfer is one host transaction with an id (economy §10, anti-dupe).

#### 1.1.8 Network and save (summary; details in §4.6 and §4.5)
The host is authoritative. Each player's 8 slots are a synced list of `{itemDefId, count, grade, instanceIds[], catchMinute[]}`. The client UI is optimistic and rolls back if the host rejects. Saved in the world save under the player's SteamID64.

**Solo vs co-op:** identical rules. Co-op only adds giving and tossing.

---

### 1.2 The phone on Tab (MUST)

#### 1.2.1 How it behaves

| Rule | Detail | Tag |
|---|---|---|
| Opens on **Tab** (pad: **View/Back**) | The character takes the phone out (0.35 s anim) and the phone slides up on the **right** of the screen (420 × 860 px at 1080p), so the LEFT notifications stay visible | **MUST** (Tab), SUGGESTED (placement) |
| Never in the hotbar | Correct | **MUST** |
| Time on the phone, top-left | The status bar shows the time top-left on every app; Home shows the big clock + Dot's forecast. **There is no clock on the HUD** *(Manan 31 Oct)*: "not a designed clock… check time on forecast" | **MUST** |
| The world keeps running | Even solo. A phone is not a pause menu (Esc is) | SUGGESTED |
| You can walk with it open | Walk speed only (1.4 m/s), with the mouse on the phone. On a pad the left stick still walks, the D-pad + A/B drive the phone, and the right stick scrolls | SUGGESTED |
| At the helm | Allowed. The boat holds its throttle and heading, A/D still steer, and the phone takes the mouse | SUGGESTED |
| Blocked | During the fishing minigame (fishing §3.12) and while swimming (it's waterproof, the character isn't) | SUGGESTED |
| Close | Tab again, Esc, or B | SUGGESTED |
| Hold Tab 0.4 s | Opens straight to the newest unread item | SUGGESTED |
| Small but useful | 8 apps on one home screen, no sub-menus deeper than 2 levels, max 3 taps to any action | **MUST** ("small but useful"), SUGGESTED (rules) |

**The device:** Walt's old rugged phone in a Ponyo-red rubber case with a cracked top-right corner (`03-phone.png`). The crack is a story beat: Walt dropped it off the pier the day Voss took over. A **phone case** is a cosmetic at Hartley's (SUGGESTED). Carrier name: **Gull Tel**. Battery is decorative: it never runs out (no busywork).

#### 1.2.2 Apps, screen by screen
Mockups: `03-phone.png` shows Home, Messages, Crew and Map. The rest share the same components.

| App | What it shows | Actions | Unlock | Tag |
|---|---|---|---|---|
| **Home** | Big time + "Tuesday · Day 3"; **Dot's forecast** card (3 blocks + "how sure Dot is" dots); the next important card (an unanswered request or the closest deadline); the company bank with tomorrow's 07:00 charges; the app grid with unread badges | Tap a card to jump to it | Start | **MUST** (time, forecast) |
| **Messages** | Threads per contact (Walt, Mari, June, Dot, …, an unknown number = the goons). Co-op adds the **"Hale & Crew"** group, which mirrors key world events. Contacts show Voss with a mood icon (gdd/world Heat) | Read; quick replies (2–3 written choices; never free text); embedded **offer cards** (mooring requests, orders) with Accept / Decline | Start | **MUST** (messages) |
| **Contracts** | Every active and offered deal: moorings, buyer orders, ship orders, consignments (gdd/world §7.3, gdd/economy). Each card: who, what, pay, deadline, status, "Voss also bidding" | Accept, decline, **Track** (pins it to the HUD "Today" list, max 3), Hand in (when at the buyer) | Day 1 (Mari) | **MUST** (contracts) |
| **Crew** | Tab **Boats**: each boat card with its assigned worker, bait, spot, back-by time, live status ("Fishing · 2 h left · 7 fish") and a progress bar. Tab **Hire at Rosie's**: today's candidates (refreshed at 07:00, seeded) with traits and wage | **Assign** a worker to a boat (pick a boat → pick a worker → spot → bait → back-by), recall, fire, hire | Act 2 (economy §5.4) | **MUST** (hire, assign workers) |
| **Map** | The island chart: our boats (live), crewmates (coloured dots), workers' boats, today's hotspots (no fishing zones: any fish anywhere *(Manan 31 Oct)*), shops (open/closed), weather fronts (rain band, storm cell), the Voss buoy line | Pin a waypoint (shows on the HUD compass for everyone); filter All / Fish / Shops / Crew | Start | SUGGESTED (Kufu's task lists the map) |
| **Bank** | The company balance; today's in and out; the **Ledger** (who earned and spent what, economy §8); upcoming 07:00 charges (wages, upkeep); loans if Manan says yes (economy Q4) | Approve or decline "Ask the crew" spend requests (economy §8) | Start | SUGGESTED (task lists bank) |
| **Forecast** | Today: 3 blocks with icon, temperature, wind, waves, and Dot's one-liner. Tomorrow appears from 18:00. Surf report (from Swell Surf Co. unlock). Storm warnings in poppy | – | Start | **MUST** (forecast) |
| **Journal** | Walt's journal pages, the fish collection (15 fish incl. Old Brass *(Manan 31 Oct)*, with best size and grade), bottles found, evidence (Act 3+) | – | Start | SUGGESTED (gdd/world §7.1) |
| **Camera** | Viewfinder; photos are saved to the Journal; evidence photos tagged automatically | Shoot (LMB/RT), zoom | Act 3 (story unlock) | SUGGESTED (gdd/world) |
| **Settings** | The whole settings menu (§1.12 lists the accessibility settings), opened from the phone so there's one place for it. The Esc menu links here too | – | Start | SUGGESTED (task lists settings) |

**Home layout (top to bottom):** status bar (time top-left, Gull Tel, battery) → date line + 64 px clock (the only big clock in the game now *(Manan 31 Oct)*) → forecast card → the "next up" card → bank strip → the 4 × 2 app grid. When Camera unlocks, the grid becomes 4 × 3 (with one free spot for later apps such as Port or Shipping). OPEN: none. **The cartel line** gets its own disguised app later ("Mango's Smoothies").

**Phone notifications:** every message, offer and crew update also arrives as a LEFT toast with "Tab · Open phone" (§2.4). An unread badge stays on the app until it's read.

**Co-op:** each player has their own phone (the same company data). Accepting an offer is shared: the first "Accept" wins, and the others see "Rani accepted". Spend approvals follow the host rule (economy §8).

---

### 1.3 Time and day length

| Item | Value | Tag |
|---|---|---|
| Time scale | **1 real second = 1 in-game minute** (×60). A full day = 24 real min | SUGGESTED (agreed with all sections) |
| Waking day | 07:00 → 02:00 = **19 real min** at most | SUGGESTED |
| Sunrise / sunset | 06:00 / ~20:00. Golden hour 18:00–20:00 | SUGGESTED (world §11.1) |
| Shops | Each shop's own hours (economy §4), typically 07:00–20:00 | economy |
| New game | Starts **Day 1, 07:00** (Prologue "First Light", world §4.1) | SUGGESTED |
| Week | 7 named days (Monday…Sunday), because shops close on Sundays (economy) | SUGGESTED |
| Seasons | None at EA launch (gdd/world Q6) | OPEN (world) |
| Clock display | 12 h with AM/PM by default; a 24 h option | SUGGESTED |
| Pausing | **Solo:** the Esc menu pauses time and the world. **Co-op:** nothing pauses, ever (Esc opens the menu, the world runs) | SUGGESTED |
| Time only runs in session | No offline progress (economy §9.6) | economy |
| Tired | From 23:00, a "Getting tired" toast; at 01:30 a soft vignette and −10% walk; **02:00 pass-out** (§1.4) | SUGGESTED |

**Why 24 minutes:** 48 in-game hours of rot (MUST ≥ 2 days) = 48 real minutes awake. That's long enough to fill a trip and a sale, short enough that rot matters. A fishing trip to the Drop-off (5 min) plus fishing plus return fits one morning. Economy's model checks the money pace against this clock.

**Implementation:** one host-owned `WorldClock` (`double minutesSinceStart`) synced to clients every 5 s plus on every jump (sleep). Clients extrapolate with `NetworkTime`. Every timed system (rot, shops, NPCs, weather blocks, contracts, workers) reads the clock; none keeps its own timer. Debug: `-pb-timescale N` and an F3 overlay.

---

### 1.4 Sleep: one sleeper skips the night (MUST)

**The rule (MUST):** if **one** player sleeps, the night is skipped to **07:00** for everyone, and the weather moves on.

#### 1.4.1 Flow
1. **Beds:** at the start, Walt's old cot in the office (broken: no "Rested" buff, economy §2.3). Later: beds and bunks from Plank & Post, the Kestrel's cuddy bunk, the Swordfish's 2 bunks (sleeping at sea works).
2. **When:** beds are usable **20:00–05:00**, or any time you're Tired (economy §10 uses the same rule). Before 20:00 the prompt reads "Too early. Back after 8 PM."
3. **Hold E 1 s on a bed** → the character lies down (a 1 s anim).
4. **Co-op heads-up:** every other player gets a LEFT toast "Pip is going to sleep · night skips in 5 s" with a 5-segment countdown ring. (Solo: no wait.)
5. **The skip:** a 1.2 s fade to the night card (economy's day summary: income, rot warnings, tomorrow's forecast, overnight events). The world simulates the skipped hours (§1.4.3). Then a fade in at 07:00 with Mari's rent toast and the ferry horn.
6. **Autosave** right after the fade-in, at 07:00 (§4.5).

#### 1.4.2 Co-op edge cases

| Situation at the moment of the skip | What happens | Tag |
|---|---|---|
| Another player is mid-minigame | The skip **waits up to 10 s** for that fight to resolve, then goes. The toast says "Waiting for Rani's fish…" | SUGGESTED |
| Another player is in a shop or warehouse transaction | It completes first (≤ 1 s) | SUGGESTED |
| Another player is within 30 m of a free bed or bunk | They wake in that bed ("you went to bed too") | SUGGESTED |
| Another player is on a boat | They wake on that boat, sitting up on deck with a yawn. The boat is **auto-anchored** where it is (deeper than the anchor reaches: it drifts ≤ 50 m overnight, never into rocks) | SUGGESTED |
| Another player is driving a boat | The same: auto-anchored; they wake at the helm | SUGGESTED |
| Another player is swimming | Moved to the nearest own boat's ladder or a dock ladder; they wake there, damp | SUGGESTED |
| Another player is in town, away from beds | They wake where they stood (no penalty). The Bench Council remarks on it | SUGGESTED |
| Two players sleep at once | One skip | SUGGESTED |
| Someone joins during the night card | They load in at 07:00 | SUGGESTED |
| Someone disconnects during the skip | Their record is saved at 07:00 like everyone's | SUGGESTED |
| The host is the one who leaves right after | Covered by §3.5 (every client has the 07:00 save) | SUGGESTED |
| A player doesn't want the night skipped (night fishing at Lantern Glow!) | **No veto: Manan's rule is one sleeper skips.** SUGGESTED soft option, a host setting "Allow 'Not yet!'": each other player may press **N** once per night to delay the skip by 15 s ("Rani says: not yet!"). Default ON | **MUST** (no veto) + OPEN Q4 (the delay) |
| A story event blocks sleep (the great storm, a ship mid-berthing in the port game) | The bed prompt says why ("Dot needs every hand"). Story-forced only | SUGGESTED |
| Pass-out at 02:00 | **Counts as one player sleeping:** the night skips for everyone. The passed-out player wakes where they fell (on the boat or the pier), with no money or item penalty; Walt texts "Bed's for sleeping, kid." | SUGGESTED (penalty: OPEN Q5) |

#### 1.4.3 What the skip advances (one function, `SimulateSkip(from, to)`, deterministic)

| System | During the skipped hours |
|---|---|
| Fish rot | Every fish ages (hotbars, the warehouse, boat boxes) at its container's rate. Fishing §5.6 example: catch at 18:00, sleep 22:00 → at the 07:00 market it's 13 h old = "Good" |
| Workers | Night shifts (if any) resolve by the economy formula (seeded) |
| Money | 07:00: mooring fees in, wages and upkeep out (economy) |
| Weather | **Moves on (MUST):** the new day's blocks are rolled; a storm night ends by 07:00 unless it's a story storm |
| Boats | Unattended, un-anchored boats are auto-anchored; moored boats stay; rival night actions resolve as a scene you find (gdd/world rule 5: "no off-screen losses") |
| NPCs | Snap to their 07:00 positions (world §11.2) |
| Contracts | Deadlines tick; anything that expired shows on the night card |
| Shops | Restock at 07:00 (seeded) |

**Solo:** identical, without the heads-up.

---

### 1.5 Weather and the forecast

#### 1.5.1 The six states

| State | Wind | Waves (Hs) | Visibility / fog | Light | Boats | Fishing (gdd/fishing owns the fish) | Surf | Tag |
|---|---|---|---|---|---|---|---|---|
| **Clear** | 0–8 kn | 0.2–0.4 m | 1.8 km | full sun | calm | normal | flat | SUGGESTED |
| **Fair** | 5–12 kn | 0.3–0.6 m | 1.8 km | sun + towering cumulus (the Ponyo look) | calm | normal | small | SUGGESTED |
| **Breezy** | 12–20 kn | 0.6–1.0 m | 1.5 km | bright, fast clouds, whitecaps | Biscuit pitches; spray on deck | choppy, bobber bob ×1.5 | **good** | SUGGESTED |
| **Fog** | 0–6 kn | 0.2–0.4 m | 150–300 m | soft white, sun disc only | slow and careful; foghorn | Mudmother at night | flat | SUGGESTED |
| **Rain** | 10–18 kn | 0.5–0.9 m | 600 m | grey-violet, wet sheen | slippery deck (§1.6.5) | pollock and sea trout bite | medium | SUGGESTED |
| **Storm** | 25–40 kn | 1.6–2.5 m | 400 m, lightning | dark slate, poppy alerts | heavy roll, leaks on T1/T2, rogue waves | Scarce fish pushed into the Shallows | huge the day after | SUGGESTED |

#### 1.5.2 How weather is rolled
- A day has **3 forecast blocks**: Morning 07–12, Afternoon 12–17, Evening 17–22, plus a Night block 22–07 that nobody plans around.
- Each block's state comes from a **Markov table** (state → next-block weights). Example from Fair: Fair 45%, Clear 25%, Breezy 20%, Rain 8%, Fog 2%. Storm can only follow Breezy or Rain, and only if the day's "storm budget" allows: at most 1 storm per 4 days outside story weeks.
- **Seeded** by (world seed, day number), so reloading never rerolls the weather (economy §10).
- **Story overrides** (gdd/world §11.3): fog on Day 4 and Day 13 (the goons, MUST beat), a storm on Day 20, Storm Week in Act 5. They show in the forecast like any other day, so the weather never feels scripted.
- **Transitions** take 6–12 in-game minutes (real time 6–12 s) of blending: wave height, wind, clouds, fog density, rain rate. A storm ramps over 30 in-game min with Dot's warning first.

#### 1.5.3 Forecast accuracy

| Forecast | Accuracy (the shown state is right) | When wrong |
|---|---|---|
| Morning block (seen at 07:00) | 95% | One step only (Fair↔Clear, Fair↔Breezy, Rain↔Fog) |
| Afternoon block | 85% | One step only |
| Evening block | 75% | One step only |
| Tomorrow (shown from 18:00) | 70% | One step only |
| **Storms** | **100%.** A storm is always forecast, and Dot texts **≥ 3 in-game h ahead** (world §11.3) | Never a surprise |

- The forecast card shows "How sure Dot is" as 1–4 dots (`03-phone.png`), honest to these numbers.
- **Upgrades:** a weather radio (Plank & Post) adds +5% to each block and shows wind hour by hour; a barometer on the office wall shows the next block's trend.
- The old dock game's rule "weather never warns" is **retired**: the forecast is a MUST now.

#### 1.5.4 Solo vs co-op
Identical. In co-op every player sees the same weather (host-owned, §4.6).

---

### 1.6 Boats: walkable vehicles

Roster, prices and speeds are economy's (§4.3): Biscuit T1 (5 m, 4.5 → 6 m/s), Kestrel T2 (9 m/s), Puffin T3 (11), Swordfish T4 (16), Albatross, and the shipping boats Tern, Cormorant and Halcyon. This section owns how they **work**.

#### 1.6.1 Getting aboard and walking
- Every deck is life-size and walkable (MUST). Step aboard from a pier (a 0.4 m step is automatic) or climb the stern ladder from the water.
- **On board you walk in the boat's own space:** your movement is computed relative to the deck, so a moving, rolling boat carries you naturally with no sliding (the classic "moving platform" done in local space, §4.6). The camera follows you, not the boat's roll (roll follow = 40%, a setting).
- **Feel numbers** come from the feel spec (`replan/feel/README.md`): run 4.2 m/s, walk 1.4, sprint 5.8 with a ramp; eased starts/stops. On a deck: sprint is allowed on T3+ only (Biscuit is too small), and jumps are 0.45 m (no deck-hopping into the sea by accident).

#### 1.6.2 Driving

| Control | KB/M | Pad | Behaviour |
|---|---|---|---|
| Take the helm / tiller | E at the helm | X | Camera blends to the boat cam in 0.4 s |
| Throttle | W / S step through **notches**: Reverse · Stop · ⅓ · ⅔ · Full | RT / LT analog (forward/reverse), snaps to notches when released | The throttle **stays** where you leave it; the engine note changes pitch |
| Steer | A / D | Left stick | Rudder angle ±35°, return to centre 0.6 s. Turn rate falls with speed. Biscuit's tiller is reversed in real life; we keep A = left for sanity |
| Horn | H | LS click | Comedy and signalling. Workers wave back |
| Lights | L | D-pad → | Nav lights and a deck lamp |
| Cruise lock | C | Y | **Locks heading + speed (max 5 m/s)** so you can leave the helm and the boat keeps going. Kestrel+ (or a Biscuit upgrade). The solo trolling enabler (fishing §6.6). SUGGESTED |
| Leave the helm | E / Esc | X / B | Without cruise lock, the throttle eases to Stop over 1.5 s (no runaway boats) |

- **Boat camera:** distance 9 m (Biscuit) → 22 m (Puffin) → 40 m (Tern), pitch 18°, FOV 55° (+6° at full speed), free orbit with the mouse/right stick, recentres 2 s after you stop looking.
- **Kill cord (SUGGESTED, a real-boat detail):** if the driver goes overboard, the engine cuts at once. No ghost boats.
- **Fuel:** a gauge on the helm HUD (bottom-right). Prices and burn are economy's (§4.3). Out of fuel: paddle at 1.5 m/s (Biscuit has oars), or phone Dot for a tow (economy fee). No stranding.

**Helm HUD (bottom-right, only at the helm):** speed in knots (big, Fraunces), a throttle notch strip, fuel, heading, anchor state, cruise-lock state, and a 3-dot "hull" indicator if the boat is leaking.

#### 1.6.3 Anchoring and mooring
- **Anchor (fishing §6.6):** E at the bow cleat, 1.5 s to drop or raise. The boat then holds within a **6 m swing circle**, nose to the wind. Max depth = the anchor rope length: Biscuit 25 m, Kestrel 40 m, Puffin 60 m (an upgrade adds +50%). Too deep: "Too deep to anchor (31 m)", and the boat drifts 0.1–0.3 m/s with wind and current.
- **In a storm** an anchor drags slowly (0.2 m/s), an honest "we should head home" signal.
- **Mooring:** come within 4 m of a free berth below 2 m/s. "E · Moor" plays a 1.5 s assisted settle (the boat glides into place and ropes appear). A moored boat can't be driven until you unmoor (E at the helm: "Cast off"). Our berths are numbered (B1 = Mari's).
- **Workers** moor and unmoor by themselves.

#### 1.6.4 Water, buoyancy and stability (the player's view; the tech is in §4.3)
- Boats ride the **same waves you see**: pitch and roll come from the real wave height under 8–16 sample points.
- **Max angles** (arcade, never capsizing): roll 6° in Clear, 12° in Breezy, 22° in a Storm; pitch half of that. A soft righting spring above 18° guarantees no boat ever flips.
- **Leaks:** Biscuit leaks until the economy's repair (Act 1). In a storm, T1/T2 can **spring a leak** (seeded event, at most 1 per storm): water rises inside (visible), the boat slows down by up to 30% and lists 5°. **Patch kit:** 6 s with two players, 10 s solo (economy §7). **Bailing bucket:** any player; the water drops while you bail. Never sinks: at max water the engine coughs at 30% speed and Dot offers a tow.
- **Collisions:** above 4 m/s, a bump with a camera shake, a wooden thud and a cosmetic scuff. Repairs at Doyle (economy). Knocks over standing players at > 6 m/s ("the boat hit the pier and everyone fell over" = a clip).

#### 1.6.5 Footing on deck
| Condition | Effect |
|---|---|
| Deck tilt > 14° | You slide slowly toward the low side unless you hold a rail (stand next to one: automatic grip) or **brace** (hold C / RS click: crouch, no sliding, slow walk) |
| Rain or storm spray | Deck "wet": stops take 0.15 s longer (a small skid) |
| Rogue wave (Storm only, seeded, telegraphed by a rising roar and a white crest 3 s ahead) | Unbraced players near a rail have a 25% chance of going overboard; everyone unbraced is knocked down (ragdoll-lite). Braced players are safe. Loose deck items slide |
| Boat hit at speed | Unbraced players knocked down (above) |

This is the market report's "storm sweeps the deck" clip, made fair: you always see and hear it coming, and bracing always works.

#### 1.6.6 Co-op roles on board (no role is locked)

| Role | What they do | Solo equivalent |
|---|---|---|
| **Helm** | Drive, watch fuel and rocks, choose spots | You, then anchor or cruise lock |
| **Angler(s)** | Fish from rail spots (Biscuit fits 2, 3 squeezed) | You |
| **Deckhand** | Anchor, life ring, patch leaks, bail, catch tossed fish, landing net | You, slower (patch 10 s) |
| **Spotter** | Binoculars, the fish finder (Kestrel+), pins hotspots on everyone's compass | Fish finder on the helm screen |

Nothing needs two people. Two people make it faster and funnier.

#### 1.6.7 Falling off and swimming
- **Overboard:** ragdoll-lite in the direction of the force (feel spec), a big splash, a 0.5 s dunk, then you surface treading water.
- **Swim:** 1.4 m/s; sprint-swim 2.2 m/s (uses stamina). **Stamina ~25 s** of sprint-swimming; treading water drains slowly (90 s), so you never "drown" while waiting for a friend.
- **Getting back aboard:** every boat has a **stern ladder or swim platform** (a Blender socket rule, §4.8). Climb with E (1.2 s anim).
- **Life ring (SUGGESTED, co-op clip):** every boat carries one. A crewmate presses E at the ring, aims, and throws (6–20 m). The swimmer grabs it automatically and gets reeled to the ladder.
- **Boat moving away:** the kill cord stops it if the driver fell. If another player is driving, they turn back (a clip). Solo: the kill cord saves you.
- **Far from any boat (> 40 m) with low stamina:** Dot's fish-out (§1.1.5).
- **Underwater:** you can't dive in EA. Sully the diver sinks a little more and walks out with a comic "clank" when climbing (cosmetic only, pb-chars).

#### 1.6.8 Workers' boats
Workers drive the real boat models when you're within the load radius (§4.2): AI on a 32 m sea grid with A* (land, shallows < 1.5 m and the Voss buoy line blocked), at ⅔ throttle, with a horn wave. Further out, they're a dot on the phone map and simulated by economy's formula.

---

### 1.7 Surfing (basic plan; MUST: the surf shop exists)
Shop: **Swell Surf Co.** (Kai Rennick, economy §4.2). Spots: **Breaker Point**, **the Voss Wall** (+1 Heat, his patrol hoses you), and **the Widowmaker** the day after a storm (world).

| Piece | Spec | Tag |
|---|---|---|
| Boards | Soft-top (beginner, stable), Fish (fast, twitchy), Longboard (slow, easy pocket). Hotbar item "surfboard" (carry pose) | SUGGESTED |
| Waves | **Separate surf waves**, not the open-sea Gerstner: a spline-driven breaking-wave mesh per spot that peels along a reef line. Sets of 3–5 waves every 20–40 s. Height comes from today's swell (Breezy = good, Storm = closed out, after-storm = huge) | SUGGESTED |
| Paddle out | W paddles (alternating arm anim), Ctrl duck-dives under white water | SUGGESTED |
| Catch the wave | A **pop-up timing ring** (it reuses the fishing bar component, one stop): press Space as the wave lifts the board. Hit = stand up; miss = the wave passes or tumbles you | SUGGESTED |
| Ride | A/D carves along the face. A glowing **pocket band** near the curl gives speed. Lean too far → wipeout (ragdoll-lite + bubbles + board leash pulls the board back) | SUGGESTED |
| Score | Ride time × pocket time × style (later: tricks). Kai's challenges pay in reputation and cosmetics, not money (no new grind) | SUGGESTED |
| Co-op | **Party wave**: 2+ riders on one wave multiply the score; dropping in on a friend knocks you both off (clip) | SUGGESTED |
| Solo | Kai's timed challenges and the surf Journal | SUGGESTED |
| When | Milestone M8 (after the demo) | SUGGESTED |

---

### 1.8 Guns (MUST as hotbar items): tools, not weapons

**Tone flag (OPEN Q1):** Port Boss is a warm Ponyo-Coast fishing story. Real guns pointed at people would clash with that tone, with the shop-and-family cast, and with Steam's content survey (it changes the age rating). gdd/world (Q3) and gdd/economy (Q2) reached the same answer independently: **guns are non-lethal tools**. Systems agrees and specifies them:

| Gun | Price (economy) | What it does | Use |
|---|---|---|---|
| **Flare gun** | $80, 3 flares (refills at June's) | Straight up: lights a 60 m circle for 45 s, and every NPC boat in 1 km comes to look. As a **distress flare** when stranded: a free tow from Dot (once per day). Flares are a signal and never fire flat | Night fishing light, rescue, the storm finale |
| **Net gun** | $1,200 | Fires a weighted net 12 m. On the sea: scoops a **surface school** (bait fish → the bait pouch) or a fish that leapt during a fight. On a person (goon or friend): tangled for 2 s, a comic flop, **no damage** | Bait, comedy, goon "defence" (world: goons are seen off with comedy, never a fight) |
| **Harpoon gun** | $3,000 | Only during a **Trophy / Legend fight**: fire once to skip one stop (the stop counts as a hit). Fishing owns the exact effect ("faster Trophy fish"). Can't be aimed at people (§ rules) | Big-fish fights from the Swordfish's fighting chair |

**Rules for every gear gun (SUGGESTED, hard rules):**
1. Aim with RMB / LT (over-the-shoulder, a reticle appears); fire with LMB / RT.
2. **Nothing ever damages a person.** The flare and harpoon reticles turn grey with "Not at people" when a person is in the cone; the trigger does nothing. The net gun only tangles.
3. No PvP mode, no friendly-fire damage (the net is a comedy tangle and can be turned off by the host).
4. **Sharks (fishing Q9):** if Manan wants "shoot the shark stealing your catch" (How to Fish style), the harpoon gun can scare a shark off (it flees, it doesn't die). OPEN, inside Q1.
5. **The cartel line (later):** even there, guns hit boats, crates and searchlights, never people (world §10.1).

**Why keep them at all:** Manan listed guns as an inventory category, so they're in the hotbar. These three earn their slot by giving the player new actions: a light at night, rescue, bait, comedy, big-fish help.

---

### 1.9 Characters and the wardrobe (Manan's decision, 2026-10-31)
- **All 6 presets ship:** Bruno, Stretch, Pip, Rani, Old Gus, Sully (`replan/chars/cast.md`). A new player picks one; **the face and body come with them**.
- **Clothes only** can change: Hat, Top, Over, Legs, Boots, Gloves (the kit's slots), plus dye per item (Hartley's Outfitters, economy §4.6). No face, body, hair or skin edits. **MUST**.
- **Player colour** (orange, blue, green, red by join slot) is fixed on the left armband and the round back patch. No outfit hides it (pb-chars rule). Name tags use the same colour.
- **Co-op uniqueness (SUGGESTED, OPEN Q10):** one crew member per player at a time, so four friends are four different silhouettes (readability is the whole reason for six body types). The second player who wants Pip is asked to pick another; with Manan's OK we could allow duplicates instead.
- **Wardrobe screen** (`07-wardrobe.png`): crew cards on the left, a turntable in the centre, clothes tabs + dyes on the right; "Reset to <preset>" restores the preset's clothes. Opened at any bed or the office locker. Free to swap between owned items.
- **Story sets** (economy): Walt's captain set, later a Voss-port uniform and Teddy Mango's shirt.
- **NPCs** are built from the same kit (pb-chars), so everyone shares one style.

---

### 1.10 Controls (keyboard + mouse, and controller)

All bindings are rebindable (Unity Input System rebinding UI). Glyphs follow the last device used. On Steam Deck and Steam Controller, Steam Input glyphs are used (Steamworks requirement for Deck Verified: no keyboard glyphs while using a pad; [Steamworks Deck compatibility](https://partner.steamgames.com/doc/steamdeck/compat)).

**On foot**

| Action | KB/M | Pad (Xbox names) |
|---|---|---|
| Move | WASD | Left stick |
| Look / camera | Mouse | Right stick |
| Jump / swim up | Space | A |
| Sprint | Shift (hold) | LS click (toggle) |
| Walk (careful) | Ctrl (hold) | Light stick |
| Brace / crouch | C (hold; toggle option) | RS click (hold; toggle option) |
| Interact / pick up / board / give (hold) | E | X |
| Use the item (cast / fire / strike) | LMB | RT |
| Aim / cancel / reel in (hold) | RMB | LT (aim) · B (cancel) |
| Hotbar 1–8 | 1–8, wheel | LB / RB |
| Drop | Q (Shift+Q stack) | D-pad ↓ (hold = stack) |
| Bag mode (rearrange) | I | D-pad ↑ |
| Bait pouch | hold R | hold Y |
| Phone | Tab (hold = newest unread) | View |
| Emotes (wave, point, laugh, cheer) | hold T + mouse | hold D-pad ← |
| Push-to-talk (only if PTT mode) | V | unbound by default |
| Pause / menu | Esc | Menu |

**At the helm**

| Action | KB/M | Pad |
|---|---|---|
| Throttle notch up / down | W / S | RT / LT (analog) |
| Steer | A / D | Left stick |
| Horn | H | LS click |
| Lights | L | D-pad → |
| Cruise lock | C | Y |
| Leave the helm | E | X |
| Orbit camera | Mouse | Right stick |

**Fishing** (fishing §4.3; repeated here so one table holds everything): charge + cast = hold LMB / RT; strike + stop the pointer = LMB or Space / RT or A; reel in early = hold RMB / hold B; skip the reveal = LMB or E / A; keep or let go = E or Q / A or X.

**Surfing:** paddle W / LS; pop-up Space / A; carve A/D / LS; duck-dive Ctrl / B.

**Rumble** (fishing's table) plus: boat bump 0.6/150 ms; rogue wave 0.9/400 ms; engine idle 0.05 continuous at the helm (setting).

---

### 1.11 Movement feel (the bridge to the feel spec)
`replan/feel/README.md` is the spec for the body and the camera; this section only sets the defaults that change because the game is now on boats:

| Item | Feel spec (dock game) | New game |
|---|---|---|
| Camera on foot | 7.5 m, 30°, FOV 55 | Same on land. **On a deck**: 6 m (small boats feel cramped otherwise), and roll follow 40% |
| Sprint | 5.8 m/s, ramping | Not on Biscuit/Kestrel decks (too small); on land and T3+ decks |
| Carry with load | −8% per crate | Crates are gone. Carrying a surfboard or cargo = walk 3.2 m/s |
| Ragdoll-lite | Knock-downs, flings, falls into the water | Same. Plus rogue waves and boat bumps |
| Footsteps | From the stride phase | Plus deck materials: wood (Biscuit), fibreglass (Kestrel), steel (Puffin+) |

---

### 1.12 Accessibility

| Area | Setting | Default | Tag |
|---|---|---|---|
| Text | UI scale 80–150%. The smallest text (xs 13 px at 1080p) stays ≥ 9 px at 1280×800 even at 80%, which is the Steam Deck minimum ([Steamworks](https://partner.steamgames.com/doc/steamdeck/compat)). Deck default: 120% | 100% | SUGGESTED |
| Font | "Legible font": swaps Zen Maru Gothic for **Atkinson Hyperlegible Next** (OFL) everywhere; Fraunces titles stay | off | SUGGESTED |
| Colour | Grade colours always come with a **shape** (dot / ring / diamond / star). The minigame zone has **stripes and edges**, not just green. Protan / deutan / tritan palette swaps for the UI tokens | – | SUGGESTED |
| Fishing timing | **Timing offset −100…+100 ms** and an **assist slider: pointer speed 70–100%** (gdd/fishing's request). "Easy fishing": the zone +30% wider | 0 ms / 100% / off | SUGGESTED |
| Bite cues | Every bite is visual (bobber dunk + splash), audio, haptic **and** a caption ("[Splash] Bite!") | captions on | SUGGESTED |
| Holds | Every hold action has a toggle option (sprint, brace, charge cast = tap-start / tap-release, give, bag) | hold | SUGGESTED |
| Motion | Camera shake 0–100%; boat roll follow 0–100%; FOV 50–80; head bob off; **Reduce motion** (UI fades only, no overshoot, no sunburst rays); lightning flash limiter | 70 / 40 / 55 / off / off / on | SUGGESTED |
| Audio | Master, music, SFX, ambience, voice; mono audio; per-player voice volume and mute | – | SUGGESTED |
| Subtitles | Barks, radio and the phone call voice (if voiced); size S/M/L; speaker names in player/NPC colour; background opacity | on, M | SUGGESTED |
| Time pressure | "Relaxed deadlines" (contracts ×1.5 time) for anyone, solo or co-op | off | OPEN Q12 |
| Input | Full rebinding; one-hand presets (mouse-only fishing: LMB cast/strike, RMB reel; the wheel selects) | – | SUGGESTED |
| Voice chat | Open mic with voice activity / push-to-talk / off; speaking rings on the crew list | voice activity | OPEN Q11 |

---

## 2. Premium UI/UX (MUST: "the old fonts and day text looked basic")

### 2.1 What was wrong, and the principles now
The old build used IMGUI boxes with Fredoka everywhere, with day/time text drawn as a plain label (`HANDOVER-MANAN.md`). Rounded-everything fonts on flat boxes is the look of a prototype.

**Principles (SUGGESTED):**
1. **One material: letter paper.** Every panel is a cream card (#FFFBF2 + 7% paper grain) with a **2 px warm-brown line** (#805C52), the same colour as the 3D outline in art direction #4. The UI and the world look painted by the same hand.
2. **Shadows are blue-violet, never black.** The Ponyo shade #98A6EA / #5B5E9E, with a 2–3 px "printed" offset in the line colour. This single rule removes most of the cheap look.
3. **Two typefaces with jobs.** A soft serif for things you'd *read aloud* (the time, names, prices, fish), a rounded sans for everything else. Never more than 2 sizes in one card.
4. **Numbers are heroes.** Time, money, weight, price: big Fraunces with tabular figures, units small.
5. **The UI follows the light.** Six lighting states tint the paper slightly (§2.5), so a night HUD doesn't glare.
6. **Motion has weight.** Everything slides in from where it lives (the left column from the left), with a small overshoot. Nothing pops.
7. **Anti-slop checklist** for every screen: no glassmorphism, no neon gradients, no emoji as icons, no generic icon pack, no centred-everything, no text over busy art without a card, no lorem ipsum, every number real or marked illustrative.

### 2.2 Fonts (all free, SIL Open Font License)

| Role | Font | Why this one | Licence / source | Tag |
|---|---|---|---|---|
| **Display**: the clock, titles, names, prices, fish names | **Fraunces** (Undercase Type), variable; we use **SOFT = 100**, optical size 72–144, weight 600 | A "soft" old-style serif: its SOFT axis rounds every terminal, so it reads like a storybook title (Kiki's, Ponyo posters) without being childish. Its numerals are beautiful at 60 px (the clock), and its italic gives fish and quotes personality | OFL 1.1, verified in the repo's `OFL.txt` ([github.com/undercasetype/Fraunces](https://github.com/undercasetype/Fraunces), checked 2026-10-31) | SUGGESTED |
| **UI text**: body, labels, buttons, small caps | **Zen Maru Gothic** (Yoshimichi Ohira), weights 500 / 700 / 900 | The rounded gothic that art direction #4 names. Warm and friendly, with even strokes that stay legible at 13 px, and it pairs with Fraunces' soft terminals. Letterspaced caps at 700 make the labels look designed | OFL 1.1 ([Google Fonts](https://fonts.google.com/specimen/Zen+Maru+Gothic)) | SUGGESTED |
| **Accessibility swap** | **Atkinson Hyperlegible Next** (Braille Institute) | Designed for low vision; replaces Zen Maru Gothic when "Legible font" is on | OFL 1.1 (Google Fonts) | SUGGESTED |
| Retired | Fredoka | Read as "prototype" (Manan) | – | – |
| Considered, not picked | Fraunces + Barlow (Golden Hour's pick), Oswald / Bebas (Lantern Dusk) | Barlow and condensed caps are industrial, which is right for a port sim but wrong for Ponyo Coast's warmth | – | – |

**Unity implementation:**
- Fonts go through Unity's TextCore as SDF font assets. Variable axes aren't read at runtime, so we **instance static cuts** with the free `fonttools varLib.instancer` (Fraunces SOFT=100 at opsz 72 and 144, weights 600/700, plus italic 500) and commit the static TTFs with their `OFL.txt`.
- **Subset Zen Maru Gothic to Latin + Latin Extended + punctuation** (`pyftsubset`). The full font carries thousands of Japanese glyphs that we'd be shipping for nothing (the atlas memory goes from MBs to KBs). Re-subset if we localise to CJK.
- The clock digits use `<mspace>` (fixed advance) so the time never jiggles as it ticks.
- Atlas: 1024² SDF per weight, padding 9, generated in the build script so agents can regenerate it headless.

### 2.3 Type scale, colour tokens, spacing, motion
The full token sheet is `mockups/tokens.css`, shown in `mockups/00-design-system.png`. It becomes `Assets/UI/Tokens.uss` with the same variable names.

**Type scale** (1080p reference, major third ×1.25; scales with the UI-scale setting):

| Token | px | Font | Used for |
|---|---|---|---|
| 4xl | 61 | Fraunces 600 | Catch reveal fish name |
| 3xl | 49 (clock 62) | Fraunces 600 | Phone home clock (no HUD clock *(Manan 31 Oct)*) |
| 2xl | 39 | Fraunces 600 | Screen titles (shop sign, Wardrobe) |
| xl | 31 | Fraunces 600 | Panel titles, money on the bank pill |
| lg | 25 | Fraunces 600 | Card titles, item names |
| md | 20 | Zen Maru 500 | Body text, notifications |
| sm | 16 | Zen Maru 500 | Secondary text, captions |
| xs | 13 | Zen Maru 700, caps, +0.12–0.16 em | Labels ("TUESDAY · DAY 3", "COMPANY") |

**Colour tokens:**

| Token | Hex | Role |
|---|---|---|
| paper / paper-2 / paper-3 | #FFFBF2 / #F8EFDD / #EEDFC4 | Cards, insets, tracks |
| line | #805C52 | Every border, the outline colour |
| ink / ink-soft | #3F2C27 / #7A625A | Text |
| shade / shade-deep | #98A6EA / #5B5E9E | Shadows, night tint |
| sea / sea-deep | #46D6C8 / #2160B8 | Messages, info, water |
| sky | #3A86E0 | Rare grade, links |
| night | #1D2748 | Night panels, selected chip |
| sunflower | #F7C548 | Money, selection, primary buttons, Prized |
| poppy | #E2553F | Alerts, danger, the phone case |
| leaf | #7DBE6A | Success, the minigame green zone |
| peach | #F6A27A | Dusk accents |
| Grades | Common #CDBFA8 (dot) · Fine #46D6C8 (ring) · Rare #3A86E0 (diamond) · Prized #F7C548 (star) | Always colour + shape |

**Spacing:** a 4 px grid (4, 8, 12, 16, 24, 32, 48, 64). **Radius:** 8 / 14 / 20 / 28; nothing sharp. **Elevation:** 2 levels (`--sh-1`, `--sh-2`), each a line-coloured offset plus a violet blur.

**Motion rules:**

| Event | Spec |
|---|---|
| Panel / card in | 220 ms, ease-out-back (overshoot 1.1), slides 24 px from where it lives (notifications from the left) |
| Out | 160 ms ease-in, fade + 8 px |
| Hotbar select | Lift 8 px, 120 ms; the item name fades in above, holds 2 s, fades |
| Money | Counts up over 400 ms; the bank pill pulses once in sunflower |
| Minigame hit | 60 ms hit-stop, the pip pops ×1.08, a rising "ding" per hit (1st, 2nd, 3rd: higher pitch) |
| Catch reveal | The card drops in 280 ms; sunburst rays rotate 6°/s behind it; sparkles twinkle; stamps land with a 3° tilt and a "thunk" |
| Phone | Slides up 260 ms from the bottom-right; the character's phone-out anim is synced |
| Reduce motion | Every motion becomes a 120 ms fade; no rays, no overshoot, no shakes |

### 2.4 Notifications and alerts: on the LEFT (MUST)

| Rule | Spec | Tag |
|---|---|---|
| Position | **Left column**, top-left: x 40 px, from y 32 px, 452 px wide at 1080p (moved up: no clock card *(Manan 31 Oct)*) | **MUST** (left) |
| Look | A paper card with a round coloured "stamp" icon, a small caps sender line, one or two lines of text, and a 4 px life bar at the bottom (`01-hud.png`) | SUGGESTED |
| Types and colours | Message (sea) · Money (sunflower) · Info (paper) · Weather (sea-deep) · Warning (poppy) · Crew (leaf) · Story/Walt (night) | SUGGESTED |
| Duration | 5 s + 0.04 s per character, max 9 s. Warnings 8 s. Timers pause while the phone is open | SUGGESTED |
| Stacking | Newest on top, pushing the others down. **Max 4 visible**; older ones shrink to 97% and fade. A 5th collapses the oldest into "+3 more · Tab" | SUGGESTED |
| Merging | Duplicates within 4 s merge with a counter: "+$18 · +$12 · +$30" becomes one "+$60 · 3 sales" that ticks up | SUGGESTED |
| Priority | Warnings jump the queue. **During the fishing minigame only warnings show**; everything else waits until the reveal ends | SUGGESTED |
| Actions | A toast can carry one hint ("Tab · Open phone", "E · Hand in"). Hold Tab to open its source directly | SUGGESTED |
| History | Every notification is kept in the phone (Messages → "Notifications", 100 newest) | SUGGESTED |
| Sound | A soft per-type sound (a paper flick, a ka-ching, a low bell for warnings). Each type can be muted | SUGGESTED |
| Never | Never in the screen centre (that's for the world); never a modal popup outside menus | SUGGESTED |
| Co-op | Crewmates' big moments ("Rani caught a Prized Grouper!") show as a Crew toast; small stuff (their sales) is merged into a daily line | SUGGESTED |

### 2.5 The HUD layout

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ ┌NOTIFICATIONS (LEFT)────┐        ┌COMPASS RIBBON (at sea)┐      ┌BANK $1,240──────┐ │
│ │ ● Mari: new message    │        │ W · NW | N · NE        │      └─────────────────┘ │
│ │ ● +$18 · 3 Mackerel    │         (pin) Hale's Landing 320 m      ┌TODAY (max 3)──┐ │
│ │ ● Dot: rain at 5 PM    │                                          │☑ Buy worm bait│ │
│ └────────────────────────┘                                          │☐ Fish for June│ │
│   (no clock on the HUD: Manan 31 Oct.                               └───────────────┘ │
│    The time is on the phone, Tab)                                   ┌CREW (co-op)──┐ │
│                                                                     │ ◐ Rani  🔊    │ │
│                                    (the world)                      │ ◐ Pip         │ │
│                                                                     └───────────────┘ │
│                          [prompts:  🖱 Hold to cast   🖱 Aim   Tab Phone]             │
│                              Grandpa's Rod · worm bait ×12                ┌HELM HUD──┐ │
│                   ┌[1][2][3][4][5][6][7][8]┐  ← HOTBAR                    │ 11 kn ⅔  │ │
│                   └────────────────────────┘                              └──────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

| Element | Anchor | When visible | Notes |
|---|---|---|---|
| ~~Clock card~~ | – | **Removed** *(Manan 31 Oct)* | Manan: "Not a designed clock… check time on forecast". The time, day and forecast live on the phone (Home + Forecast, §1.2). The world itself tells the time of day (the 6 lighting states, §2.7); Dot's weather calls and the 23:00 "Getting tired" toast still arrive on the left |
| Notifications | Top-left | When any | §2.4 |
| Compass ribbon | Top-centre | At sea, or when a pin is set | Cardinal letters, a poppy needle, pins (our dock, waypoints, hotspots, crewmates) as paper pills below |
| Bank pill | Top-right | Always (minimal HUD: on change only) | The company balance; it pulses on change |
| Today list | Under the bank | When tracked items exist | Max 3 tracked contracts/goals with progress ("3 / 5") |
| Crew list | Right, under Today | Co-op | Player colour ring, name, a speaking ring when talking, a small icon for "at helm / fishing / in shop" |
| Prompts | Bottom-centre, above the hotbar | Context | Key glyph + verb. Only what's possible right now (max 3) |
| Item name | Above the hotbar | 2 s after a switch | Name + one useful fact |
| Hotbar | Bottom-centre | Always | §1.1 |
| Helm HUD | Bottom-right | At the helm | Speed, throttle notch, fuel, heading, anchor, cruise lock, leak dots |
| Minigame | Lower third, centre | During a fight | The hotbar fades to 40% (`05-minigame.png`) |
| Reticle | Centre | Aim mode only | A soft ring; grey "Not at people" for gear guns |
| World-space tags | Over objects | Near | Name tags (player colour), shop "Back at 07:00" signs, the fish-on-deck prompt, ghost minigame bars over crewmates |

**HUD modes (Settings → HUD):** Full (default) · Minimal (hotbar + toasts; the rest appears on change) · Off (photo mode, F10; Tab still opens the phone).

### 2.6 The mockups (what each shows and the decisions in it)

| File | Screen | Decisions to review |
|---|---|---|
| `00-design-system.png` | Tokens | Fonts, colours, 6 lighting-state tints, motion rules |
| `01-hud.png` | HUD, daytime | No clock card *(Manan 31 Oct)*: 3 notifications at the top-LEFT (a message with "Tab · Open phone", a money toast, Dot's weather call); compass ribbon with pills; the bank pill + Today list; the hotbar with the selected rod lifted, fish stacks with grade dots and freshness bars, a flare gun and a shell; the cast-aim ring on the water |
| `02-hotbar.png` | Hotbar spec | The inspect card lists all 5 fish in a stack with their own freshness; the stack bar shows the worst fish; pick-up merge toast; the full-hotbar ghost slot; giving with Hold E; pad mapping |
| `03-phone.png` | Phone | Home (time top-left, big clock, Dot's 3-block forecast + accuracy dots, Mari's waiting request, bank strip with 07:00 wage, 8 apps); Messages (Mari's mooring request as an offer card, $40/day, Accept/Decline + quick replies); Crew (Biscuit assigned to Pip with bait/back-by/progress, a locked second boat, a free hand at Rosie's); Map (Saltmouth Island, Hale's Landing west, Voss Harbour east with the buoy line, zones, our boats, a rain front) |
| `04-shop.png` | Shop | June's portrait + bark, tabs, a 3 × 2 item grid (owned / stock / "best at dusk" / a locked item with its unlock rule), the detail pane, quantity, total and "bank after", June's order strip with "Hand in", "in your bag" bait counts. The same frame serves every shop (economy §4.0 adds a 3D turntable for boats) |
| `05-minigame.png` | Fishing bar, dusk | The pointer is a little red-and-white bobber riding the bar; the striped green zone with a sweet-spot line; a ghost tick where stop 1 landed; 3 stop pips; the grade ladder lights Fine; the hook retry chip; the hint "Fighting! Take your time · a miss only lowers the grade" (the first draft showed a "2nd miss snaps it" warning on a normal fish; snapping now exists only when you choose to fight a HEAVY fish, gdd/fishing §3.7) *(Kufu review 31 Oct)*; the bobber visible in the water |
| `06-catch.png` | Catch reveal | A painted fish, the grade ribbon with hit pips, the name in Fraunces 66, 4 stats (length, weight, worth now with its multiplier, fresh for), stamps (New in Journal, Personal best), Keep / Let it go, "goes to slot 4 · auto-keeps in 6 s" |
| `07-wardrobe.png` | Wardrobe | Pick 1 of 6 crew (a taken card shows who has it), turntable, clothes tabs, owned / story / shop-locked items, dyes, the fixed player-colour band |

Mockup tech: hand-written HTML/CSS + SVG, rendered at 1920 × 1080 by headless Chrome (`mockups/render.sh`). The fish, icons, phone case and map are drawn in SVG for this plan. In the game, **item icons are rendered from the real 3D models** by an "icon baker" camera with the Ponyo shader and outline (§4.8.5), so icons and world always match.

### 2.7 The UI follows the light (the 6 lighting states)

| State | When | Paper tint | Extra |
|---|---|---|---|
| Dawn | 05:30–07:30 | as is | – |
| Day | 07:30–17:30 | as is | – |
| Golden | 17:30–19:30 | +4% warm | – |
| Dusk | 19:30–21:00 | +6% warm | Soft lamp glow on the phone's Home card |
| Night | 21:00–05:30 | −12% brightness, cooler | A lamp-orange rim on cards; the white of prompts dimmed to 85% |
| Storm | weather | cooler, −6% saturation | Warnings in poppy pulse once |

Done with one USS class on the root (`.light-night` …) switched by the clock, so it costs nothing.

### 2.8 UI technology
- **UI Toolkit** for every screen (HUD, phone, shops, menus). Its USS is CSS-like, so the mockups translate almost line for line. Since Unity 6.2 UI Toolkit also renders **in world space** ([Unity: World Space UI Toolkit in 6.2](https://unity.com/resources/how-to-create-world-space-ui-toolkit)), so name tags, prompts and ghost bars use the same tokens. The project is on 6000.5, so this is available.
- **IMGUI is removed** (it also never showed in batchmode screenshots, `HANDOVER-MANAN.md`). UI Toolkit panels can render into a RenderTexture, which lets headless tests screenshot real UI (§4.9).
- The paper card is a 9-slice sprite (cream, 2 px line, 20 px radius) + a tiled 256 px grain texture. Icons: SVGs drawn in our style, imported with Unity's free Vector Graphics package or rasterised at 2 sizes by the build script.
- **Input glyphs:** our own paper key caps (as in the mockups) for KB/M; Steam Input glyphs on Deck/pads. Kenney's CC0 Input Prompts can fill gaps ([kenney.nl](https://kenney.nl/assets/input-prompts-pixel)), recoloured to our tokens.

---

## 3. Solo vs co-op (MUST: everything fun solo; co-op 1–4)

### 3.1 The system matrix

| System | Solo | Co-op (2–4) | Scales with players? |
|---|---|---|---|
| Fishing | Rod holders, trolling with cruise lock, the same catch rates | Independent fights, tangles, landing net, ghost bars, "Double!" (fishing §4.2) | No (hotspot pools are shared, fishing §5.3) |
| Boats | Anchor + cruise lock make one person enough; patch 10 s | Roles: helm, anglers, deckhand, spotter; patch 6 s; life ring | No |
| Hotbar | 8 slots | 8 slots each; give and toss | Per player |
| Money | One company bank | The same bank; Ledger; spend rule (economy §8) | Order and ship **sizes** ×1.0/1.6/2.1/2.5; **prices never** (economy §7) |
| Contracts | Same offers | Same offers, bigger sizes | Sizes only |
| Workers | Same | Same (in solo they're a bigger share of the income) | No |
| Rival | Timers 25% longer, shorter night patrols (world rule 6) | +1 goon per 2 extra players | Yes, gently |
| Sleep | Instant | One sleeper skips; 5 s heads-up; optional "not yet" | – |
| Weather | Same | Same | No |
| Story | The player is Walt's grandkid | The host is the grandkid; the others are "the crew"; Walt adopts all of them by text (world Q5) | – |

**The solo test (SUGGESTED, applied to every milestone):** a designer plays the milestone solo for 30 minutes and must never hit a "needs a second player" wall, a timer built for two, or a dead stretch longer than 2 minutes with nothing to do.

### 3.2 Shared vs personal progress

| Shared (the world / company) | Personal (per player, stored in the world under their SteamID) | Personal and portable (per Steam account) |
|---|---|---|
| Money, Ledger, boats, piers, buildings, the warehouse, workers, contracts, story flags, Heat, NPC relationships, map discoveries, the fish Journal's "world records" | Hotbar contents, position, owned personal items (rods, clothes, tools they bought), their chosen crew member + outfit, their personal-best fish | Settings, cosmetic unlocks (story outfits once earned), Steam achievements, the career stats screen |

- **Why:** the market report's biggest lesson (Crashout Crew, `replan/market/market.md`): progress that resets or belongs only to the host kills co-op games. Here the port persists and every friend's copy holds it (§3.5).
- **No carrying items between worlds** (economy §10, anti-dupe): inventories live in the world save, not in a character file.

### 3.3 Lobby and joining
- **Steam lobby** (Facepunch) with invite / join-on-friend. Privacy: Friends only (default) / Invite only / Public (OPEN later: public lobbies need moderation).
- **LAN** (Unity Transport) is kept for tests and couch-adjacent play.
- **Drop-in at any time.** The joiner loads the host's snapshot, then spawns at our office door (or on the host's boat if the host is at sea and has a free spot, SUGGESTED). A first-time joiner gets the starter kit (bamboo rod, 10 worms) from outside the bank (economy §8).
- **Max 4.**

### 3.4 Drop-in rejoin (market: "ship with rejoin")
- The Steam lobby stays open while the host plays. A dropped player sees "Reconnect" on the title screen for that lobby for 10 minutes.
- On rejoin, their record (hotbar, position, crew member) is restored by SteamID. If they dropped while at a helm, the boat went to host ownership, cut its throttle and auto-anchored. If they dropped mid-fight, the fish counts as escaped and the bait is refunded (fishing §3.12).
- A leaving player's fish **stay in their hotbar** in the world save. If they leave for good, the host can use "Empty a missing player's hotbar into the warehouse" (economy §8).

### 3.5 Host leaves: "soft host migration"
- **Seamless host migration isn't available in NGO's client-server mode**; Unity's answer is its distributed-authority topology instead ([Unity discussions](https://discussions.unity.com/t/host-migration-in-multiplayer-game-which-uses-lobby-relay-and-netcode-for-gameobjects/923073), [NGO ownership docs](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/basics/ownership.html)). Moving the whole game to distributed authority would be a large, risky rewrite for a 4-player game. **We don't do it.**
- **What we do (SUGGESTED):**
  1. Every client gets a compressed copy of the world save at each autosave (07:00) and a **mini-snapshot every 2 real minutes** (money, boats, hotbars, contracts: < 100 KB).
  2. When the host quits or drops, every client sees "The host left · Continue this port?" with a 3 s countdown. The player with the **newest copy** (ties: lowest join slot) is offered "Host now". The others auto-join the new lobby through Steam (they're friends in the same lobby).
  3. The loss is bounded to ≤ 2 real minutes of world changes, and fish can't be duped (economy §10: world id + day + snapshot counter; the lobby picks the newest).
- **Later, any player can host the port** from their copy, even when the original host is offline (market: "host-only saves are a top complaint"). Forks are flagged by world id + day (economy §10).

### 3.6 Proximity voice (market.md: needed before the demo)
- **Free, built on what we already have:** Facepunch.Steamworks is in the repo. Its `SteamUser.VoiceRecord`, `ReadVoiceData` and `DecompressVoice` record, compress and decode voice ([Facepunch wiki: SteamUser](https://wiki.facepunch.com/steamworks/SteamUser), [ReadVoiceData](https://wiki.facepunch.com/steamworks/SteamUser.ReadVoiceData)). Dissonance is paid and blocked by the no-money rule (market §5). Vivox isn't needed.
- **Pipeline:** read the compressed voice every frame (the wiki says at least 4×/s) → send it as an NGO named message on an unreliable channel to the other players → decompress into a streaming `AudioClip` on an `AudioSource` at the speaker's head.
- **Proximity:** 3D audio with full volume to 3 m, logarithmic rolloff to silence at 35 m. A low-pass filter when the speaker is underwater, inside a cabin or behind the wheelhouse. Wind and rain duck it by up to 3 dB in a storm (shouting in a storm is the fun).
- **Radio:** a **walkie / boat radio** (Kestrel+ helm, or a cheap hotbar walkie at Doyle) makes you heard by everyone with a radio, with a crackle filter. The phone app "Call" does the same for 1:1 (SUGGESTED). That's how co-op works across the island.
- **UI:** a speaking ring on the crew list and over heads; per-player mute and volume; open mic (voice activity) / push-to-talk / off (OPEN Q11).
- **Budget:** a few KB/s per active talker; at most 3 talkers per listener in 4-player co-op. Tested in M2.

### 3.7 Griefing and fairness
- Host spend rule, host-only asset sales, Ledger (economy §8, §10).
- Net-gun tangles can be switched off by the host. There's no damage anywhere.
- Kick from the lobby (host); "vote kick" isn't needed for friends-only play.

---

## 4. Tech plan

### 4.1 What we keep and what we throw away (this REPLACES the current game)
The repo is `C:\Users\asus\Desktop\port-boss` (main @ a6fe3d4: Unity **6000.5.6f1**, URP 17.5, NGO 2.13.3, Unity Transport 6.5, Input System 1.20, Facepunch.Steamworks, Test Framework, Multiplayer Play Mode, AI Navigation; 11.7k lines in `Assets/Scripts`). Read on 2026-10-31.

**Step 1 (M0, SUGGESTED):** tag the old game `legacy/dock-work-final` at a6fe3d4 and push the tag (history is kept forever), then delete the old game code from `main` in one commit. Nothing is lost, and nobody builds on top of the dock-work loop by accident.

| Keep (and adapt) | Why | Throw away | Why |
|---|---|---|---|
| Unity 6 URP project settings, packages (NGO, UTP, Input System, Test Framework, Multiplayer Play Mode, AI Navigation for NPCs on land, Timeline) | Working, current, free | `HarbourMap.cs`, `World.cs`, `Scenery.cs` (the code-built yard and landscape) | The new island is authored in Blender (§4.8.6) |
| `Plugins/Facepunch.Steamworks` + `Net/FacepunchTransport.cs` + the lobby in `Net/Net.cs` | Steam P2P relay, lobbies, **and voice** (§3.6). Two-account Steam co-op is still untested: test it first in M2 | `Crate`, `Pallet`, `Trolley`, `Forklift`, `Storage`, `PierSag`, `CrateLook` | The dock-work loop is gone; fish never go in crates (MUST) |
| `tools/` (env.sh, ubuild.sh, release.sh, animfilm.sh, capture.sh, reg2.sh pattern) | Headless build, release, filming and regression already work | `PortGame.cs` (captain rotation, day loop), `Intro.cs`, `Speedboat.cs`, `Rival.cs` (Marlowe drive-by, banner plane) | The story and loop changed. The banner-plane and fireworks ideas live on in git for Voss later |
| The `AutoRun.cs` **pattern** (`-pb-*` switches running a scenario headless and logging `[PB]` lines) | Agents can test without a window. Rewritten smaller as `TestScenarios` | `GameUI*.cs` (IMGUI) | Replaced by UI Toolkit (§2.8) |
| `CharAnim.cs`: foot-synced gait blend, hand/foot IK, ladder, swim/tread; `Player.cs`: swim, stamina, coyote time, jump buffer | Proven parts (feel audit: "what already works") | `Worker.cs`, `GameUI.Character.cs` (the old character + editor) | The 6-preset kit replaces them (pb-chars) |
| Water learnings (`Water.cs`, `PortBoss/Water` shader) and the **LookDev shaders** on branch `panoptes/w-pb-artdir` (`LD_Stylized`, `LD_Water`, `LD_Sky`, `LD_Outline`) | The Ponyo Coast look was rendered with them (#4 settings in `replan/artdir/README.md`) | `Boat.cs` (scripted captain boats on paths) | New drivable, walkable `Boat` (§1.6) |
| `Settings.cs`, `Stats.cs` (concepts) | Settings persistence, career stats | `Storm.cs` | Replaced by the weather system |
| Mixamo clips in `Desktop/Port Boss/mixamo-incoming` | Free, retarget to the kit rig | `Editor/PortBossBuild.cs` scene generation | Replaced by the Blender placement importer (§4.8.6). Its prefab/material generation ideas are kept |
| The Toon Harbor pack (owned) | **Only on Voss's side**: freighters, gantries (world: "art contrast as story") | Fredoka font | §2.2 |

**Rules that carry over** (from `HANDOVER-TUSHAR.md` / `HANDOVER-MANAN.md`): the repo is **public**, so never commit the pack (`git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` must print 0) and never commit secrets. Players get release builds only. Don't open windows on Manan's PC without an OK.

**New layout (SUGGESTED):**
```
Assets/Game/<Module>/   Core · Net · Player · Boats · Fishing · Inventory · Phone · Time · Weather ·
                        World · Npc · Economy · Rival · Save · Audio · UI · TestScenarios
                        (one assembly definition per module: faster compiles, testable in isolation)
Assets/Art/             Characters · Boats · Fish · Props · Island · Shaders · Fonts · Icons · Palettes
Assets/UI/              Tokens.uss · Components/*.uxml · Screens/*.uxml
Assets/Data/            ScriptableObjects: items, fish (generated from fishing's fish-roster.csv), boats + shops
                        (from economy.xlsx), weather tables, lighting states
art-src/                .blend sources + tools/export_all.py + tools/validate.py
tools/                  build, release, test, perf scripts
```
Game data comes **from the design sheets**: an editor importer reads `gdd/fishing/fish-roster.csv` and `gdd/economy/economy.xlsx` (the `<!--T:...-->` tables) into ScriptableObjects, so a number changed in the plan is one re-import, not a code edit (SUGGESTED).

### 4.2 Open-sea world streaming
Agreed with gdd/world §8.5:
- **Scale:** 5 × 5 km sea, island ~2.4 × 1.3 km; everything within ±2.5 km of the origin, so **no floating origin** (32-bit float error stays sub-millimetre at that range).
- **Cells:** additive scenes on a **512 m grid**. Load radius = 2 cells around **any** player (the host loads the union of all players' radii; each client only loads its own). Unload with a 1-cell hysteresis.
- **What's in a cell scene:** static, non-networked geometry only (rocks, buildings, props, colliders, NPC waypoints). **Everything networked** (boats, NPCs, dropped items, the warehouse) lives in the always-loaded core scene and is spawned by the host, with NGO's object visibility limited by distance (`CheckObjectVisibility`, 600 m for boats, 250 m for NPCs and items). That keeps NGO scene sync out of streaming entirely.
- **Always loaded:** the core scene (ocean, sky, lighting, the WorldClock, managers), our Hale's Landing cell, and the **Voss Harbour HLOD impostor** (skyline + night lights, ~20k tris).
- **Far islets:** impostors (low-poly + baked silhouettes). **Fog** at 1.8 km (clear) to 300 m (fog days) hides the edges (world).
- **Loading budget:** a cell must load async in < 2 s on min spec. T4 at 16 m/s crosses a cell in 32 s (world), so that's comfortable.
- **Edge of the world:** the Shelf Current pushes boats back gently past ~2.3 km, plus a fog band (world).

### 4.3 Water and buoyancy (free, own code)
**Why our own:** Unity's built-in water system is HDRP-only. Crest's open-source repo targets the built-in renderer, and its URP version (Crest 5) is paid ([github.com/wave-harmonic/crest](https://github.com/wave-harmonic/crest), checked 2026-10-31). We already have our own toon water shaders, so we extend those.

| Part | Approach | Tag |
|---|---|---|
| Ocean surface | One **camera-following clipmap mesh**: an inner 128 × 128 grid at 0.5 m spacing (64 m), then 5 rings that double their spacing out to ~2 km. Each ring snaps to its own spacing, so vertices never "swim" | SUGGESTED |
| Waves | **Sum of 5 Gerstner waves** (wavelengths 6–60 m, directions ±40° around the wind) scaled by the weather's Hs (§1.5.1), plus 2 scrolling detail normal maps for ripples. Storms add a 6th, long swell | SUGGESTED |
| Ponyo look | Colour **bands by depth** (shallow #46D6C8 → deep #2160B8, from a seabed depth texture); crisp white contact foam (depth intersection); crest foam above a steepness threshold; soft glint; the warm-brown outline is not drawn on water (the outline pass runs before transparents, artdir) | SUGGESTED |
| Same waves on CPU | The **same 5 Gerstner parameters** run in a Burst job for any point query. Time = `NetworkTime.ServerTime` on every machine, and the shader gets the same time value, so **everyone sees and floats on identical waves** | SUGGESTED |
| Height at (x, z) | Gerstner moves points sideways, so the height at a fixed (x, z) is found with 3 fixed-point iterations (standard trick) | SUGGESTED |
| Buoyancy | Each boat has 8 (Biscuit) / 12 (Kestrel) / 16 (T3+) **buoy points** (Blender empties `buoy_*`). Per point: force = k × submersion depth, clamped, plus vertical damping. Linear + quadratic drag; a low centre of mass; a **righting spring above 18° roll**, so it never capsizes (§1.6.4) | SUGGESTED |
| Bobber, swimmers, floating items | The same sampler, 1 point + a spring | SUGGESTED |
| Seabed | A 2048² depth texture over the 5 km map (2.4 m/px) + a 1024² detail tile around the island, rendered top-down from the Blender island. Feeds: water colour bands, shore foam, anchoring depth, the fishing 0.5 m rule, the AI sea grid | SUGGESTED |
| Wakes | A foam particle trail + a URP decal strip behind boats (no simulation) | SUGGESTED |
| Surf | Separate spline-driven breaking-wave meshes at surf spots (§1.7) | SUGGESTED |
| Performance | Ocean ≤ 2.5 ms GPU at 1080p min spec; buoyancy ≤ 0.3 ms CPU for 12 boats (Burst) | SUGGESTED |

### 4.4 Lighting: Ponyo Coast + the day cycle (Manan's decision)
Base settings come from `replan/artdir/README.md` §4: a lit toon shader with a **1-step ramp, soft edge 0.12**, a painted **blue-violet shade tint (#98A6EA)**, paint noise 0.3; bloom 0.45 (scatter 0.8, the "Ghibli glow"); film grain 0.15 (paper); vignette 0.15; SSAO 0.6; a **thin outline: 1.2 px, warm brown #805C52, 75%** (the full-screen depth/normal pass from `LD_Outline`, before transparents).

**Six lighting states** (a `LightingState` ScriptableObject each; the clock blends between neighbours over 20–40 in-game min; the weather overrides on top):

| State | Time | Sun elevation / colour / intensity | Shadow tint | Sky top → horizon | Water tint | Fog | Bloom | Extras |
|---|---|---|---|---|---|---|---|---|
| **Dawn** | 05:30–07:30 | 6° / #FFC9A8 / 0.9 | #A79BE0 | #8D7FC4 → #FFE1B0 | pink-lit turquoise | light, peach | 0.5 | Cumulus lit from below, mist on the water |
| **Day** | 07:30–17:30 | 35–62° / #FFF3DC / 1.6 | #98A6EA | #3A86E0 → #CFE6F5 | luminous turquoise | 1.8 km | 0.45 | Towering cumulus, cloud shadows drift on the sea |
| **Golden** | 17:30–19:30 | 14° / #FFC77A / 1.8 | #8F8FD6 | #5DA0EA → #F7C548 | warm teal, gold glint | slight warm | 0.55 | Long shadows; the most beautiful fishing (world) |
| **Dusk** | 19:30–21:00 | −2° / – / sky only | #6E6AA8 | #5B5E9E → #F6A27A | violet-teal | peach haze | 0.6 | **Lamps on** (#FFB060), string lights in town |
| **Night** | 21:00–05:30 | moon 30° / #9FB4FF / 0.35 | #3A4580 | #121B36 → #2B3766 | deep blue, moon path | 600 m blue | 0.7 | Warm windows, lantern bloom, Lantern Glow bay bioluminescence, Voss's sodium glow over the ridge. **Never black**: Ponyo nights are blue |
| **Storm** | weather | hidden / ambient #7C8AA3 | #6B7390 | #4A5470 → #A9B3C4 | slate green-blue, white streaks | 400 m | 0.3 | Saturation −25%, rain sheets, lightning flashes (limiter) |

Implementation: one URP Volume per state, weight-blended by a `LightingDirector`. The sun is one directional light (sun, then moon at night), and the sky is our `LD_Sky` gradient with painted cloud cards. The grade is baked to a LUT per state. **Fog days** and **rain** are weather overlays (fog distance, wet sheen) on whichever time state is active.

### 4.5 Save system

| Item | Spec | Tag |
|---|---|---|
| Where | `%USERPROFILE%\AppData\LocalLow\<Company>\PortBoss\Saves\<worldId>\` | SUGGESTED |
| Files | `world.json.gz` (+ 3 rotating backups `.bak1..3`), `meta.json` (name, day, players, playtime, last played), `thumb.png` (a 480 × 270 screenshot at the last autosave), `snapshot.json.gz` (the 2-minute mini-snapshot, §3.5) | SUGGESTED |
| Format | JSON, gzip, a `schemaVersion` integer. **Migrations** are a function per version (`Migrate_3_to_4`), tested against fixture saves in the repo | SUGGESTED |
| Contents | World seed; day + minute; weather blocks; company (bank, Ledger, loans); boats (id, type, upgrades, paint, berth or position + anchor, fuel, damage, fish box); dock build state (bought items by id; each has a fixed spot); the warehouse (contents with catch minutes); workers (traits, assignment, morale); contracts; story flags; Heat; NPC relationships; Journal; Lost & Found; **per player (key = SteamID64):** crew member, outfit, hotbar, position, personal items, personal bests | SUGGESTED |
| When | **Autosave at 07:00 after the night** (economy §10 depends on it); on "Save & quit"; when the host leaves; plus the 2-minute mini-snapshot for crash recovery and soft host migration | SUGGESTED |
| Safety | Write to `.tmp`, fsync, atomic rename; a CRC in the meta file; if the load fails, offer the newest valid `.bak` with its day | SUGGESTED |
| Slots | Unlimited worlds; the title screen shows "Continue" (last played) + a world list with thumbnails | SUGGESTED |
| Co-op copies | At every 07:00 save the host sends the gzip (target < 1 MB) to all clients on a reliable channel in 32 KB chunks; clients store it under the same world id (§3.5) | SUGGESTED |
| Steam Cloud | Steam **Auto-Cloud** on the Saves folder (configured in Steamworks, no code, free) | SUGGESTED |
| Settings | A separate `settings.json` per machine (not in the world) | SUGGESTED |
| Anti-scum | All rolls are seeded (economy §10); saves happen only at 07:00, quit and the snapshot. **The snapshot never restores a mid-fight state** | SUGGESTED |
| Size budget | world.json.gz < 1 MB at the endgame | SUGGESTED |

### 4.6 Networking

**Topology:** a listen-server host (a player) over Steam P2P relay (Facepunch transport); LAN over Unity Transport for tests. 1–4 players, NGO tick **30 Hz**.

| Thing | Authority | Sync | Notes |
|---|---|---|---|
| Clock, weather | Host | Every 5 s + on change; clients extrapolate | §1.3 |
| Waves | Nobody (deterministic) | Shared `NetworkTime` + the weather Hs | §4.3 |
| Player movement | **Owner** (client-authoritative, as today) | Position/rotation at 30 Hz, interpolated | Co-op among friends; no anti-cheat needed |
| Player on a boat | Owner | Sent as **(boatId, local position, local yaw)** while aboard, world position otherwise. The receiver places the avatar in that boat's current transform, so a rolling boat never makes riders jitter | SUGGESTED. NGO parenting replicates too ([NGO: NetworkObject parenting](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/advanced-topics/networkobject-parenting.html)), but forum reports show child sync glitches when the parent moves after parenting, so our own boat-relative sync is safer. Fallback: NGO parenting |
| Boats | **The driver owns the boat** while at the helm (ownership change on helm enter); otherwise the host | NetworkTransform (position, rotation) at 30 Hz with interpolation; buoyancy runs on the owner; others interpolate | The driver feels zero steering lag |
| Boats driven by workers | Host | Same, at 10 Hz beyond 200 m | – |
| Fishing | The host rolls species/size/weight at the bite; the angler's client times the minigame and sends {stop timestamps, hits}; the host checks plausibility (≥ 120 ms apart, ≤ 5 stops) | Ghost bar to others at ~100 ms delay | gdd/fishing §4.4 |
| Hotbar / inventory | Host | A NetworkList per player of slot structs `{ushort itemDef, byte count, byte grade, uint[] ids, int[] catchMinute}`; clients request ops by RPC; optimistic UI with rollback | Every transfer = one host transaction with an id (economy §10) |
| Warehouse | Host | Contents synced to clients within 10 m only | – |
| Dropped items / fish on deck | Host | Pooled NetworkObjects; on a boat they're simulated in the boat's local space by the boat's owner | – |
| NPCs | Host | 5 Hz position + animation state; only within 250 m | Routines are host-side (world §11.2) |
| Money, contracts, workers, Heat | Host | Event-driven RPCs + a full state on join | – |
| Voice | Peer data through the host relay | NGO named messages, unreliable | §3.6 |
| Late join | Host | A snapshot (clock, weather, company, spawned objects), then normal sync | – |

**Bandwidth budget** (4 players, worst case at sea with 6 boats nearby): per client **down ≤ 40 KB/s** (state ~15 + voice ≤ 3 talkers × ~4) and **up ≤ 15 KB/s**. Measured with NGO's Network Profiler (Multiplayer Tools package, already installed).

**Lag test standard:** every co-op milestone is tested at **150 ms RTT + 2% loss** (Unity Transport network simulator, or the free "clumsy" tool on Windows), on two real Steam accounts (market: "test at 150 ms with two real Steam accounts before the demo").

### 4.7 Performance budget

| Target | Spec | Tag |
|---|---|---|
| Min spec | GTX 1060 6 GB / RX 580 class, 4-core i5-8400 class, 8 GB RAM, 1080p **Medium at 60 fps** (world §8.5 agreed) | SUGGESTED |
| Steam Deck | 1280 × 800, **Low at a stable 30–40 fps**; aim for "Deck Verified" (OPEN Q8) | OPEN |
| Frame (16.6 ms) | CPU main ≤ 8 ms (physics 1.5, gameplay 2, NGO 1, UI 1, animation 1.5, audio 0.5); render thread ≤ 6 ms; GPU ≤ 14 ms (ocean 2.5, opaque 4, shadows 2, outline + post 2.5, UI 0.5, transparents/FX 1.5) | SUGGESTED |
| Draw calls | ≤ 1,500 per frame. SRP Batcher on; Unity 6's GPU Resident Drawer for static props; one material per asset family (palette atlas, §4.8.5) makes batching easy | SUGGESTED |
| Triangles on screen | ≤ 1.5 M at Medium (LODs and impostors) | SUGGESTED |
| Shadows | Main light: 2 cascades, 120 m, soft; up to 8 shadowless additional lights (lamps) per view with Forward+ | SUGGESTED |
| Memory | RAM ≤ 3.5 GB; VRAM ≤ 2.5 GB High / ≤ 1.5 GB Low | SUGGESTED |
| Loading | Title ≤ 15 s cold on an HDD; Continue → playing ≤ 20 s; cell stream ≤ 2 s async | SUGGESTED |
| Physics | ≤ 12 active boat rigidbodies; buoyancy in Burst; character controllers for players | SUGGESTED |
| **Perf gate** | `-pb-perf` (§4.9) flies a fixed route at Golden hour with 3 bot players and 6 boats, and logs p50/p95 frame times. A milestone fails if p95 regresses > 10% against the previous milestone on the same machine | SUGGESTED |

### 4.8 Blender asset pipeline (MUST: we make assets in Blender, no purchases)

#### 4.8.1 Style guide (Ponyo Coast)
1. **Silhouette first, chunky and soft.** Bevel every visible edge (2–3 segments, 2–4 cm). No razor edges anywhere a player looks.
2. **Hand-made, never machine-straight.** Planks vary ±1–3° and ±5% length, posts lean, roofs sag a little. A `wobble()` Blender script applies this with a seed, so it's repeatable.
3. **Colour comes from the palette, light from the shader.** No painted shadows, no texture detail, no normal maps. Shapes carry the detail; the toon ramp + blue-violet shade + paper grain + outline make it "painted".
4. **Minimum feature size 4 cm** at gameplay distance (smaller details make outline noise: the artdir risk note).
5. **Our side is warm** (plaster, timber, tile, Hale red and cream on our cranes, world). **Voss's side is industrial** (the Toon Harbor pack: grey, sodium-lit).
6. **No AI-generated textures or models ship** (market §9: AI-disclosure backlash). Grok concepts are reference only.

#### 4.8.2 Conventions
- **Units:** metric, 1 Blender unit = 1 m, real-world sizes (Biscuit 5 m, doors 2.1 m). Characters per the kit (≈1.75 m base).
- **Axes and export:** FBX, Forward −Z, Up Y, Apply Transform, scale 1.0, "Apply scalings: FBX All". Pivots at the base centre (props) or the waterline centre (boats).
- **Naming:** `PB_<Category>_<Name>_<Variant>`, e.g. `PB_Boat_Biscuit_T1`, `PB_Prop_Bollard_A`.
- **Collision:** child meshes ending `_col` → convex MeshCollider; `_colBox` → BoxCollider (the importer converts them). The walkable deck gets its own simple `_colDeck` mesh (smooth, no snag edges; the old game's "stuck on a bollard" lesson).
- **Sockets** (empties the importer turns into named transforms): `socket_helm`, `socket_seat_#`, `socket_anchor`, `socket_ladder`, `socket_lifering`, `socket_rodholder_#`, `rail_volume_#` (fishing rail spots, as box empties), `buoy_#` (buoyancy points), `socket_light_#`, `socket_bed` (bunks), `cam_boat` (the default boat-camera pivot). **The validator fails a boat** without helm, anchor, ladder, life ring, ≥ 8 buoys and ≥ 2 rail volumes.
- **Customisation (MUST: boat colours):** boats are modelled with **4 paint channels** (Hull, Stripe, Cabin, Deck) as their own palette rows; the shader swaps those rows for the chosen colours through a MaterialPropertyBlock. No extra materials, any number of colour combos. Clothes dyes work the same way (one dye channel per garment).
- **Boat names:** painted on the hull with a URP Decal from a font atlas (Fraunces), so the player names their boat (SUGGESTED).

#### 4.8.3 The rig (from `replan/chars` + `replan/feel`)
- **Characters:** one armature = **Mixamo's own X Bot skeleton**, scaled uniformly so the Head joint sits at **82%** of full height; shoulder joints ≤ 17% apart; mitten hands (thumb + one finger chain); joints within ±5% of scaled X Bot; the big head, hands, boots and bulk are mesh only (pb-chars `editor-kit.md` §2b). One Humanoid Avatar, so every Mixamo clip retargets.
- **The 6 presets** = 6 body bases (blend shapes + root/head scale + posture) + clothes. Garments carry the same shape keys (Surface Deform bake loop). **Clothes-only** editing (Manan) means the body/head slots aren't exposed in the UI; the kit stays modular inside.
- **Animation sources:** Mixamo clips for locomotion, jumps, swim, ladder, getting thrown, emotes (free with Manan's sign-in; Manan downloads, mixamo-list.md). **Fishing clips are keyed in Blender on our rig** (cast wind-up/release, strike, reel loop, rod-holder grab, landing-net scoop, hold-up small/medium/huge), because Mixamo has only generic stand-ins (fishing OPEN Q6). Also keyed: helm idle/steer, anchor winch, bailing, patching, life-ring throw, surf paddle/pop-up/ride, phone out/in.
- **Feel spec numbers** (`replan/feel/README.md`) apply: clip speed caps, eased starts, ragdoll-lite with Unity's Ragdoll Wizard, spring jiggle on head/hat/belly.
- **Fish:** one shared 4-bone spine rig + jaw; flops and swims are procedural (sine along the spine), so all 15 species need no hand animation.
- **Boats:** rigid; animated parts (throttle lever, wheel, anchor winch, outboard tilt) are bones or child transforms.

#### 4.8.4 Polycount budget (triangles)

| Asset | LOD0 | LOD1 | LOD2 / impostor | Note |
|---|---|---|---|---|
| Player character (kit) | ≤ 8,800 | 4,000 | 1,500 | pb-chars budget, 1 material |
| NPC (same kit) | ≤ 4,000 | 2,000 | 800 | |
| Fish (common) | 600–1,200 | 300 | – | Legends ≤ 3,000 |
| Biscuit (T1) | 6,000 | 3,000 | 800 | |
| Kestrel (T2) | 12,000 | 5,000 | 1,500 | |
| Puffin / Swordfish (T3–T4) | 20,000–30,000 | 9,000 | 2,500 | |
| Albatross / Tern | 40,000 | 15,000 | 4,000 | Walkable interiors are separate cell content |
| Cormorant / Halcyon | 80,000 | 30,000 | 6,000 | Endless content |
| Building (house, shop) | 2,000–6,000 | 1,000 | impostor | |
| Prop (bollard, crate, lamp) | 50–800 | 50% | culled | |
| A whole island cell visible | ≤ 600,000 | | | With LODs |
| Voss Harbour skyline impostor | 20,000 | | | Always loaded |

LODs come from a Decimate pass in the export script (50% / 20%), with cross-fade, and are hand-fixed only where the silhouette breaks.

#### 4.8.5 Textures and materials
- **One 256 × 256 palette atlas per asset family** (characters, boats, town, nature): 16 × 16 swatches of 16 px, from the Ponyo palette. UVs sit in swatch centres. A validator checks every UV island lies inside one swatch.
- **One shared 512² "paint" detail texture** (brush grain), world-space-tiled in the shader at low strength. That's the painted-paper feel, with no per-asset texturing work.
- No normal maps. Optional AO baked into vertex colour for big props.
- **Icons are baked**: an editor "IconBaker" renders each item prefab at 256 px with the Ponyo shader + outline on a transparent background, so the UI and the world always match (§2.6).

#### 4.8.6 Blender is the level editor (SUGGESTED)
Agents work headless, so the island layout lives in `.blend` files: placed linked props, buildings and boats. `art-src/tools/export_all.py` (`blender -b island.blend -P export_all.py`) exports the FBX models **and a placement JSON per 512 m cell** (prefab id + transform + seed). Unity's importer builds each cell scene from that JSON. One source of truth for the layout, diffable in git, rebuildable by script, and no hand-edited Unity scenes (the old `PortBossBuild` lesson).

#### 4.8.7 Files and git
- `.blend` sources go in `art-src/` via **Git LFS**. GitHub Free includes **10 GiB of LFS storage and 10 GiB of bandwidth per month**, and LFS is disabled until next month if the bandwidth runs out without a payment method ([GitHub docs: Git LFS billing](https://docs.github.com/en/enterprise-cloud@latest/billing/concepts/product-billing/git-lfs)). So: commit .blend files only at milestones, keep `.blend1` ignored (already), and keep the repo's LFS under ~6 GiB. OPEN for Kufu: the repo lives on Tushar's GitHub account (`tusharjain2001/port-boss`), so the quota is his.
- Exported FBX files are committed (normal git; small).
- **Free tools only:** Blender, Krita/GIMP (palettes), fonttools, Mixamo. No purchases (MUST, and no money before EA).

### 4.9 Test infrastructure (shared by every milestone)

| Tool | What | Tag |
|---|---|---|
| **EditMode tests** (Unity Test Framework) | Pure logic: hotbar merge/stack/give, rot stages, clock maths, weather Markov + forecast accuracy over 10,000 seeded days, save round-trip + migrations, contract maths | SUGGESTED |
| **PlayMode tests** | Boat buoyancy at rest (draft and level ±2 cm), max roll per weather state, mooring snap, the swim ladder climb | SUGGESTED |
| **Headless scenarios** (`-pb-*` switches, batchmode) | `-pb-fishtest N` (fishing's bot: N casts → `Logs/fishtest.csv`); `-pb-boattest` (drive a route, anchor, moor); `-pb-sleeptest` (all the §1.4.2 edge cases with 2 bots); `-pb-savetest` (save, reload, compare a hash); `-pb-coop` (host + 3 clients over LAN with a 150 ms simulator); `-pb-perf` (perf flythrough) | SUGGESTED |
| **UI snapshots** | UI Toolkit panels render to a RenderTexture in batchmode → PNG per screen per lighting state → compared against approved images (pixel-diff threshold). Fixes the old "IMGUI never shows in screenshots" gap | SUGGESTED |
| **Films** | `animfilm.sh` films key animations (cast, reel, reveal, ladder) at a fixed 30 fps for the proof folder | keep |
| **Data validators** | Fish roster, shops, boat sockets, Blender exports (tri budgets, sockets, palette UVs) | SUGGESTED |
| **Logs** | Every scenario ends with `[PB] RESULT pass/fail` lines + 0 exceptions, the same contract as `reg2.sh` today | keep |

---

## 5. Process and ROADMAP (MUST: one thing at a time)

### 5.1 The build rules

| Rule | Detail | Tag |
|---|---|---|
| One thing at a time | A milestone starts only when the previous one has passed its automated tests **and** Manan's checklist (for ★ milestones). No parallel features | **MUST** |
| Quality over quantity | A milestone can be cut smaller, never rushed | **MUST** |
| No building until Manan is satisfied with the plan | This document is a plan only | **MUST** |
| Same repo | Everyone works in `C:\Users\asus\Desktop\port-boss`. Short-lived branches per task, merged to `main` by Kufu after review; `main` always builds | **MUST** (same repo), SUGGESTED (branching) |
| Only the latest build on disk | `Desktop\Port Boss\builds\` keeps **one** folder: `PortBoss-M<n>-<yyyymmdd>`. `release.sh` writes the new one; the previous one is deleted **after** Manan approves (the vault rule: deletion needs his approval card). Today 20 old builds are on disk: delete them all except the newest when M0 starts, with his OK | **MUST** |
| A release build per milestone | No dev console; ships with `HOW-TO-TEST.md` = the milestone's Manan checklist | **MUST** (Kufu's task) |
| Assets made in Blender | No purchases; the owned pack only on Voss's side | **MUST** |
| No money until EA | Free tools, CC0 sounds, OFL fonts | **MUST** |
| Definition of done (every milestone) | 0 exceptions in all headless scenarios; all EditMode/PlayMode tests green; the perf gate passes; UI snapshots approved; a 60–90 s film + screenshots in `proof/M<n>/`; release build made; checklist handed to Manan | SUGGESTED |

### 5.2 Milestones

★ = Manan tests it. Estimates are agent-weeks including our own testing and one fix round after Manan's test (rough, see §5.3).

---

**M0 · Foundations** (internal, no Manan build) · ~1 week
- **Scope:** tag + remove the old game (§4.1); new folders and assemblies; input map (both devices); UI Toolkit shell + `Tokens.uss` + fonts (instanced, subset); the Ponyo shader stack v0 (toon lit, outline, sky gradient, Gerstner ocean v0) in an empty sea scene; the WorldClock; the test harness skeleton (`TestScenarios`, EditMode/PlayMode folders, UI snapshot tool); release.sh with the one-build rule; the data importers (fish CSV, economy tables).
- **Done when:** an empty sea renders in the Ponyo look at 60 fps on the dev PC; a test UI card renders headless to PNG; `-pb-smoke` passes with 0 exceptions.
- **Automated tests:** smoke scenario; UI snapshot of the token sheet; importer round-trip (CSV → SO → CSV equal).

---

**M1 · ★ Boat + fishing** (MUST: "the first build just has the boat and fishing sorted") · ~4 weeks
- **Scope:** one sea area (The Shallows, ~1 km², a stub of Hale's Landing pier + Walt's cot); **Biscuit** in Blender (walkable, tiller, anchor, ladder, kill cord, leaks off for now); one crew member (**Pip**, the kit v1) with the feel-spec movement, swim, ladder, ragdoll-lite; **fishing F1–F6** (gdd/fishing §8: rod, cast, bobber, bite 4–10 s, the minigame, the reveal, the hotbar stacks, the feel pass; the **bamboo rod's 3 fish** (sprat, mullet, mackerel) plus a debug switch to the **Fibreglass rod's 3** (garfish, bream, conger) so the rod step can be felt *(Manan 31 Oct: 3 fish per rod)*); the **hotbar** (§1.1, without the warehouse or giving); the **HUD**: no clock *(Manan 31 Oct)*, time runs (F3 debug overlay shows it; no sleep yet, the day loops at 02:00 → 07:00), left notifications, compass, prompts; Clear and Fair weather only; Day and Golden light only. **Solo only.**
- **Done when:** fishing's F6 checklist passes; 15 minutes of fishing feels good to repeat; no clipping in films; pad and KB/M are both complete.
- **Automated tests:** `-pb-fishtest 500` (all bites in [4.0, 10.0] s; grade mix matches fishing's model ±3%); `-pb-boattest` (Biscuit floats level ±2 cm at rest, roll ≤ 6° in Clear, anchor holds within 6 m for 10 in-game min, the ladder works from all sides); hotbar EditMode suite (merge, split, full, drop, let go); UI snapshots of HUD / hotbar / minigame / reveal vs the approved mockups; perf gate baseline.
- **Manan test checklist:**
  - ☐ Take Biscuit out from the pier, drive around, come back and moor. Does driving feel good on keyboard **and** pad?
  - ☐ Walk around the deck while it rocks: no sliding, no jitter, the camera feels calm.
  - ☐ Drop the anchor and fish 15 minutes. Do you want to keep going?
  - ☐ Cast, bobber and reel look polished (MUST).
  - ☐ Count 10 bites: each comes 4–10 s after the bobber settles.
  - ☐ The 3-stop bar: 3 hits = Prized; 0 hits still lands a Common.
  - ☐ Miss on purpose: nothing shakes, you don't stumble, the fish still lands one grade lower. It feels relaxing, not tense *(Kufu review 31 Oct)*.
  - ☐ Fill the hotbar: the 6th mackerel of one grade starts a new stack; a full hotbar offers swap / drop / let go.
  - ☐ Jump off the boat and climb back up the ladder.
  - ☐ The notifications (top-left) and the phone's clock look designed, not basic (MUST; no HUD clock *(Manan 31 Oct)*).
  - ☐ Anything that felt slow or ugly: note it, and every timing is a tunable number.

---

**M2 · ★ Co-op on the boat** · ~3 weeks
- **Scope:** Steam lobby (2–4) + LAN; boat-relative player sync; driver-owned boats; **fishing F7** (independent fights, tangles, ghost bars, giving with Hold E, tossing); **proximity voice** + speaking rings; drop-in and rejoin; the crew list on the HUD; player colours on armbands; the other 5 crew members' bodies (clothes = presets only, no wardrobe yet).
- **Done when:** 2 real Steam accounts on 2 PCs fish together for 30 min with 0 desyncs; a 150 ms + 2% loss test feels exact in the minigame.
- **Automated tests:** `-pb-coop` (host + 3 bot clients, 150 ms sim): bot riders on a moving boat stay within 5 cm of their deck position; ownership hand-offs at the helm; give/toss transactions never dupe (10,000 random transfers, item count conserved); rejoin restores the hotbar; voice loopback test (the packet rate and decode).
- **Manan test checklist:** ☐ Invite Tushar via Steam; both board Biscuit; one drives, one fishes. ☐ Talk while standing close, then walk to the far end of the pier: the voice fades. ☐ Cast over each other's line: "Tangled!" ☐ Hand a fish over. ☐ Toss a fish: catch it, miss it into the sea. ☐ Tushar quits the game and rejoins: his fish are still there. ☐ Fall overboard while the other drives away: the life ring.

---

**M3 · ★ The day** · ~2 weeks
- **Scope:** the full day/night with **all 6 lighting states**; **sleep (MUST)** with every §1.4 edge case; Tired/pass-out; **weather**: all 6 states, transitions, the forecast on the phone, Dot's storm call; footing (wet decks, brace, rogue waves), leaks + patch + bail; **fish rot** (fishing's stages) through the night; the office with Walt's cot.
- **Done when:** a full 3-day solo session and a 2-player session cover every sleep edge case with no stuck players.
- **Automated tests:** `-pb-sleeptest` (2 bots: in minigame, swimming, driving, in town, far from beds → each wakes where §1.4.2 says); weather Markov over 10,000 seeded days (forecast accuracy within ±1% of 95/85/75/70; storms always called ≥ 3 h ahead; ≤ 1 storm per 4 days); rot EditMode (exactly 48 h; ages through the skip); lighting UI snapshots for all 6 states; boats never exceed max roll in a storm (PlayMode).
- **Manan test checklist:** ☐ Watch a full day from the pier: dawn, day, golden, dusk, night. Is it beautiful? ☐ Sleep alone: 07:00, new weather. ☐ In co-op, one of you sleeps while the other is fishing at sea: where do you wake? ☐ Stay up to 2 AM: pass out. ☐ Ride out a storm in Biscuit: brace for the rogue wave, patch the leak. ☐ Catch at dusk, sleep, check the fish: "Good".

---

**M4 · ★ Phone + the first income + selling + save** · ~3 weeks
- **Scope:** the **phone (MUST)**: Home, Messages, Contracts, Bank + Ledger, Forecast, Map, Settings; **Mari's mooring request → $40/day (MUST first income)**; Old Saltmouth's market stub with **Pratt's Fish Market**, Mari as a buyer, and **Hook, Line & Sinker** (bait buying, the bait pouch; fishing F8); the company bank; **save / continue** (§4.5) + co-op copies; the night card.
- **Done when:** the first 30 minutes of economy §2.3 play through solo and in co-op; save/quit/continue round-trips with no loss.
- **Automated tests:** `-pb-savetest` (save → reload → world hash equal; migration fixtures); contract and money EditMode suites (no negative bank, ledger sums); phone UI snapshots; `-pb-firstday` bot (follows the first-30-minutes script and checks the money at each step against economy's model ±10%).
- **Manan test checklist:** ☐ Mari's text arrives; accept it; the next morning $40 arrives with a toast on the LEFT. ☐ Sell at Pratt's (60%) and to Mari (100%): you feel the difference. ☐ Buy shrimp at June's: new fish appear. ☐ Quit and Continue: everything is where you left it. ☐ The phone feels small but useful, and opening it never blocks you.

---

**M5 · ★ Shops, upgrades and the wardrobe** · ~4 weeks
- **Scope:** **Doyle Boatworks** (Biscuit repairs/upgrades, **paint colours (MUST)**, the **Kestrel**, walk aboard before buying); **Plank & Post** (**beds, the warehouse tiers, furniture, office upgrades (MUST)**; everything you buy **installs itself in its set spot** *(Manan 31 Oct)*: no placement mode); **Hartley's Outfitters** + the **wardrobe** (6 presets, clothes only); the shop UI for all; town NPC routines v1; the warehouse T0–T2 *(Manan 31 Oct)*.
- **Done when:** money → upgrade → visible change works for every shop; every bought item appears in its set spot by 07:00 (or at once for paint and furniture), and no set spot blocks a walkway or a berth.
- **Automated tests:** shop transaction suite (concurrent buys from two players, "Ask the crew" rule); set-spot validator (every buyable has one spot; no overlaps, navmesh stays connected); paint channel test (4 channels × N colours render, no extra materials); wardrobe uniqueness in co-op.
- **Manan test checklist:** ☐ Pick Biscuit's colours at Doyle's and see Doyle's crew paint it (you never hold a brush). ☐ Buy the Kestrel, walk aboard first. ☐ Buy a bed, a lamp and the warehouse T1: each appears in its own spot, no placing. ☐ Change Pip's hat and dye it. ☐ Two players can't both be Pip.

---

**M6 · ★ Workers: the first automation (MUST)** · ~2 weeks
- **Scope:** **The Mooring Line** (Rosie's hiring board), the **Crew app**: hire, **assign to a boat**, bait, spot, back-by; workers drive the real boats near you and are simulated far away; the catch goes to the warehouse *(Manan 31 Oct)*; wages at 07:00; the IOU rule.
- **Done when:** a worker shift returns the economy model's catch ±10% over 20 seeded shifts; worker boats never beach.
- **Automated tests:** `-pb-workertest` (20 shifts, a catch distribution check, the A* sea grid never crosses land or the Voss line); wage/IOU EditMode suite.
- **Manan test checklist:** ☐ Hire someone at Rosie's. ☐ Assign them to Biscuit from the phone while you fish from the Kestrel; watch them leave and come back. ☐ The phone's Crew screen tells you everything at a glance.

---

**M7 · ★ Demo polish → the Steam demo** · ~3 weeks
- **Scope:** the first 60 minutes script (gdd/world §9) end to end, with no tutorial screens (learning by doing); settings + accessibility pass (§1.12); controller glyphs + Steam Deck check; perf on the min-spec machine; the audio pass (CC0 + our own); the demo ending hook (Voss's goons' card / the fog morning tease); Steam demo build + store assets (real in-game captures only).
- **Done when:** 5 outside playtesters (friends) finish the demo solo and in co-op; no blocker bugs; perf gate on min spec.
- **Manan test checklist:** ☐ Play the demo start to end solo, then with Tushar. ☐ Would you wishlist it? ☐ Pick 3 moments for the trailer.

---

**After the demo** (each its own ★ milestone, the same structure):

| Milestone | Scope | ~Weeks |
|---|---|---|
| **M8** Surf + gear guns | Swell Surf Co., surfing (§1.7); flare, net and harpoon guns (§1.8, if Manan OKs Q1); fishing F10–F11 (tackle, rod holders, landing net, trolling) | 4 |
| **M9** Rival v1 | Voss Harbour (impostor → full stage set), the buoy line + patrol, Heat tiers 0–3, **the goons' first visit (MUST)**, rival actions R1–R8 with telegraphs, the Camera app + evidence | 4 |
| **M10** Port v1 | Building piers (MUST), ship orders (berth, then **unload containers with our cranes**, MUST), Dot's pilot | 5 |
| **M11** Shipping + the finale | **Shipping boats + consignments by phone (MUST)**, Heat tiers 4–5, the Charter, Storm Week + the great storm, the vote, Endless | 5 |
| **M12** EA hardening | Save migration from demo saves, achievements, localisation-ready strings, crash reporting (free: Unity's own logs + a local report file), store page, launch build | 3 |
| Post-EA | **The cartel line (Teddy Mango)**, seasons (world Q6), events, the mega-ships (economy Endless), monthly updates (market: 6–12 months of EA) | – |

### 5.3 Rough timeline
Assuming the plan is approved and M0 starts **Monday 9 Nov 2026**:

| Milestone | Weeks | Ends about |
|---|---|---|
| M0 Foundations | 1 | 13 Nov 2026 |
| M1 ★ Boat + fishing | 4 | 11 Dec 2026 |
| M2 ★ Co-op | 3 | 8 Jan 2027 (with the holiday week) |
| M3 ★ The day | 2 | 22 Jan 2027 |
| M4 ★ Phone + income + save | 3 | 12 Feb 2027 |
| M5 ★ Shops + wardrobe | 4 | 12 Mar 2027 |
| M6 ★ Workers | 2 | 26 Mar 2027 |
| M7 ★ Demo polish | 3 | **16 Apr 2027: public Steam demo** |
| Next Fest (June 2027) | – | Register by **25 Apr 2027**; fest **14–21 Jun 2027** (market §3.3) |
| M8–M12 | ~21 | ~mid-Sep 2027 → **EA ~Oct 2027** |

**Honest read:** the demo would be public only ~8 weeks before the June fest. The market report wants **2–4 months** ([market.md §3.3]), so the June date is tight and has no slack for a failed Manan test. **OPEN Q7:** (a) keep June 2027 and cut M5 to the boat shop + dock shop only (Hartley's after the fest), which gains ~2 weeks; or (b) aim for the **October 2027 Next Fest** with a demo in ~June, and EA after it. Recommended: **(a)**. Manan's "quality over quantity" rule wins if a milestone slips: then we switch to (b).

### 5.4 What goes into the Steam demo, and into EA

| | **Steam demo** (~60 min, ends on a hook) | **EA launch** |
|---|---|---|
| World | Hale's Landing, Old Saltmouth (shops), the near waters (any fish anywhere *(Manan 31 Oct)*); Voss Harbour visible on the skyline (can't go near) | The whole island and sea, Voss Harbour stage set, Lantern Glow, the Drop-off, the Estuary |
| Boats | Biscuit; the Kestrel viewable and testable at Doyle's (not buyable) | Biscuit → Swordfish, the Tern (shipping), the Bulldog tug |
| Fishing | Rods 1–2 (6 species), 3 baits, grades, rot, the reveal | All 15 species incl. Old Brass, 5 rods, 5 baits *(Manan 31 Oct)*, rod holders, trolling, nets |
| Systems | Hotbar, phone (Home, Messages, Contracts, Bank, Forecast, Map, Settings), day/night, sleep, weather, save | Everything in §1, plus Crew, Journal, Camera |
| Economy | Mari's mooring (first income), selling, bait shop, Biscuit repairs | All shops, workers, piers, ship orders, consignments |
| Story | Prologue + Act 1 (gdd/world), **ends on the fog morning with the goons' card** ("Wishlist to find out what Voss does next") | Acts 1–5, the finale, Endless |
| Co-op | Full 1–4 with voice and rejoin (the market says the demo needs it) | Same, + soft host migration |
| Not in the demo | Workers, surf, guns, the rival's actions, the port | Cartel, seasons, mega-ships (post-EA) |
| Price | Free | $6.99, 20% launch discount (market) |

---

## 6. OPEN QUESTIONS for Manan (batched)

| # | Question | Our recommendation |
|---|---|---|
| Q1 | **Guns:** OK as non-lethal tools (flare, net, harpoon), never aimed at people, even in the cartel line? Should the harpoon scare off sharks stealing your catch? (Same question as world Q3, economy Q2, fishing Q9.) | Yes: tools only; sharks are scared off, never killed |
| Q2 | ~~Dock storage~~ **Answered *(Manan 31 Oct)*: the expandable warehouse from Day 1, slot = 10 fish (§1.1.6).** Still open: a **boat fish box** (Kestrel+, economy Q1)? | Yes, from the Kestrel |
| Q3 | **Bait in a pouch on the rod** instead of a hotbar slot (fishing Q2)? | Yes |
| Q4 | **Sleep "Not yet!":** one sleeper always skips (your rule). May the others delay it once by 15 s (host setting)? | Yes, default on |
| Q5 | **Passing out at 2 AM:** no penalty (wake where you fell, Walt teases you), or a cost? | No penalty |
| Q6 | **No death:** knock-downs, falls and drowning never cost items or money (Dot fishes you out)? | Yes |
| Q7 | **Demo date:** June 2027 Next Fest with a tight demo (public ~16 Apr), or October 2027 with more slack? | June, with M5 trimmed; switch to October if any milestone slips |
| Q8 | **Steam Deck Verified** as a goal for EA? | Yes (UI is already sized for it) |
| Q9 | ~~Clock top-left~~ **Answered *(Manan 31 Oct)*: no HUD clock; the time is on the phone (Home + Forecast).** | – |
| Q10 | **Co-op characters:** one player per crew member (four friends = four different silhouettes), or duplicates allowed? | One each |
| Q11 | **Voice chat default:** open mic (voice activity) or push-to-talk? | Open mic, with an easy PTT switch |
| Q12 | **"Relaxed deadlines" setting** (contracts ×1.5 time) for anyone who wants a calmer game? | Yes, off by default |
| Q13 | **Git LFS for Blender files** on Tushar's GitHub account (10 GiB free each of storage and bandwidth a month): OK, or should the .blend sources live somewhere else? | LFS, milestone commits only |
| Q14 | **Map A moved** *(Kufu review 31 Oct)*: Voss Harbour now sits on the far east tip (9.6 min in Biscuit round the north, 6.4 in the Kestrel; 7.3 / 4.8 via the reopened Cut). Is that far enough to feel like another world, without making visits a chore? | Yes; we can stretch or shrink the east arm in the map script |

---

## 7. Sources (all checked 2026-10-31)
- Plan inputs: `gdd/BRIEF.md`; `gdd/SOURCE-game-concept-and-manan-notes.txt`; `replan/artdir/README.md` (§4 Ponyo Coast); `replan/chars/README.md`, `cast.md`, `editor-kit.md`; `replan/feel/README.md`; `replan/market/market.md`; `replan/story/features.md`; `C:\Users\asus\Desktop\port-boss\HANDOVER-MANAN.md`; the repo's `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt` and `Assets/Scripts`; the vault note `projects/port-boss.md` (2026-10-31 lines).
- The sibling sections: `gdd/fishing/fishing.md`, `gdd/world/world.md`, `gdd/economy/economy.md` (read 2026-10-31; numbers and names follow them).
- Fraunces licence: [github.com/undercasetype/Fraunces](https://github.com/undercasetype/Fraunces) (OFL.txt fetched). Barlow licence checked the same way ([github.com/jpt/barlow](https://github.com/jpt/barlow)), but not chosen. Zen Maru Gothic and Atkinson Hyperlegible Next on [Google Fonts](https://fonts.google.com/) (OFL; the CSS endpoints resolved).
- Crest licence and pipelines: [github.com/wave-harmonic/crest](https://github.com/wave-harmonic/crest).
- Facepunch.Steamworks voice: [SteamUser](https://wiki.facepunch.com/steamworks/SteamUser), [ReadVoiceData](https://wiki.facepunch.com/steamworks/SteamUser.ReadVoiceData), [DecompressVoice](https://wiki.facepunch.com/steamworks/SteamUser.DecompressVoice~edit).
- NGO: [NetworkObject parenting](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/advanced-topics/networkobject-parenting.html), [ownership / distributed authority](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects%402.5/manual/basics/ownership.html), [host-migration discussion](https://discussions.unity.com/t/host-migration-in-multiplayer-game-which-uses-lobby-relay-and-netcode-for-gameobjects/923073), [child sync while the parent moves (forum)](https://discussions.unity.com/t/setting-local-position-of-a-child-on-a-network-object-transform-reports-parents-world-position/1656994).
- UI Toolkit world space (Unity 6.2+): [unity.com](https://unity.com/resources/how-to-create-world-space-ui-toolkit).
- Steam Deck compatibility (text ≥ 9 px at 1280×800, controller glyphs): [Steamworks](https://partner.steamgames.com/doc/steamdeck/compat).
- Kenney Input Prompts (CC0): [kenney.nl](https://kenney.nl/assets/input-prompts-pixel).
- Git LFS quotas: [GitHub docs](https://docs.github.com/en/enterprise-cloud@latest/billing/concepts/product-billing/git-lfs).
- Next Fest dates and demo timing: `replan/market/market.md` §3.3 (Steamworks links inside).
