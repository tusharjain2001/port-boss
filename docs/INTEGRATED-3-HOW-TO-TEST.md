# Port Boss INTEGRATED-3: how to test

Build: `C:\Users\asus\Desktop\Port Boss\builds\20261106-1146-INTEGRATED-3\PortBoss.exe`
Commit: `panoptes/w-pb-integrate3 code @ 5e72dab`

This build combines everything so far: M1 to M4 (already on `main`), the M3 and M4 quality passes, M5 (shops,
upgrades, wardrobe) and M6 (workers). Work through the sections in order. Tick each box, and write a note next to
anything that felt wrong, slow or ugly, even if it "works".

Time: about 3 hours solo (sections 1 to 9), then about 1 hour with Tushar (section 10). You can split it over days:
Save & quit keeps your world.

---

## 0. Read first

**The clock.** 1 real second = 1 game minute. A game day is 24 real minutes. A new game starts on Day 1 (a Monday)
at 7:00 AM. There is no clock on the screen: the time is on the phone (Tab).

**Test shortcuts** (they all work in this release build; there is no debug-only switch in the code).
F10, F11 and the F7 tester only work for the host (or when you play solo).

| Key | What it does in THIS build |
|---|---|
| F3 | Debug line on or off: day, time, light, weather, bite timer |
| F5 | Next rod tier (1 to 5, then back to 1). Press once = the Fibreglass rod |
| F6 | Next bait |
| **F7** | Next story act and +$5,000 in the bank ("Act 2 · +$5,000"); after Act 4 it wraps to Act 1. A joiner's F7 only gets "F7 is the host's" |
| **Shift+F7** | "Every fish is HEAVY" on or off ("Debug" toast). Not during a cast |
| F8 | Fall over (the ragdoll) |
| F9 | Switch to the next crew body (in co-op: the next free one). Not at the tiller or while fishing |
| F10 | Clock +1 hour. **Shift+F10**: +6 hours |
| F11 | Hold the next weather (Clear, Fair, Breezy, Fog, Rain, Storm), once more: "Weather: the forecast's again" |

**F7 tip:** press F7 **twice** at the start of the M5/M6 sections: Act 3 and +$10,000. (In older builds F7 also
flipped heavy fish; in this build that is Shift+F7 only.)

**Warning:** F10 ages your fish too. Sell or store fish before big time jumps, unless you are testing rot.

**Command-line switches** (add them to a shortcut's Target, after `PortBoss.exe`). Optional; the menus do the same.

| Switch | What it does |
|---|---|
| `-pb-host` | Host on this network at once, skipping the title |
| `-pb-join 192.168.1.4` | Join that address at once, skipping the title (`127.0.0.1` = this PC) |
| `-pb-name Manan` | Your name over your head and in the crew list |
| `-pb-port 7777` | Another port (both sides must use the same one) |
| `-pb-voice open` / `ptt` / `off` | Open mic, push-to-talk (hold V), or no mic |
| `-pb-netsim 150 2` | Fake lag: 150 ms round trip and 2% lost packets |
| `-pb-weather storm` | Start with that weather held (clear, fair, breezy, fog, rain, storm) |
| `-pb-day 4 -pb-hour 18` | Start the clock on another day and hour |
| `-pb-body Bruno` | Start as that crew body (Pip, Bruno, Stretch, Rani, Gus, Sully) |

**Controls** (keyboard + mouse; pad in brackets)

| Do | Keys |
|---|---|
| Move / look / sprint / walk slowly | WASD / mouse / hold Shift / hold Ctrl (sticks, L-stick click) |
| Jump | Space, hold = higher (A) |
| Interact, take the tiller, anchor, shop counter, ladder | E (X) |
| Hotbar | 1-8 or the mouse wheel (LB / RB) |
| Cast / strike / stop the bar | hold LMB, let go / LMB or Space (RT / A) |
| Reel in early | hold RMB (B) |
| Keep / let go at the reveal | E / Q |
| Drop one fish / toss | tap Q / hold Q, let go (D-pad down) |
| Give a fish / the stack to a crewmate | hold E / Shift + hold E |
| Bait pouch | hold R, point, let go; tap R = next bait (hold Y) |
| Brace (storm, on deck) | hold C (R-stick click) |
| Shove a crewmate | G (D-pad right) |
| Boat: throttle / steer / moor | W, S notches / A, D / F at the pier end (RT, LT / L-stick) |
| Phone | tap Tab; hold Tab = newest unread; Backspace = back; Esc = close (View / B) |
| In a shop | click, or arrows/WASD + hold E or Enter on Buy; Q / Shift+Q = next / previous tab (LB / RB) |
| Wardrobe | click or arrows + E; Tab = next clothes tab; R = reset clothes; drag to spin the turntable |
| Pause / save & quit / invite / leave the crew | Esc / Esc then Q / I / Esc then L |
| Push-to-talk (if set) | hold V |

---

## 1. Start (10 min)
- [ ] 1.1 Open `BUILD.txt` in the build folder. It names the commit at the top of this page.
- [ ] 1.2 Start `PortBoss.exe`. The title card sits over the waiting world. Rows: **Play solo** (or **Continue** +
  **New game** if you have a save; **Your worlds** once you have 2 or more), **Host a crew on Steam**, **Join a friend
  on Steam**, **Host on this network** (shows your address), **Join on this network**. Number keys pick rows, but the
  numbers move when Continue appears: pick by name.
- [ ] 1.3 Without Steam running, the two Steam rows are greyed: "Steam isn't running".
- [ ] 1.4 Pick **New game** (or Play solo). You are on **Walt's Landing**, Day 1, 7:00 AM, **$40** on the bank pill
  (top right), Walt's rod in slot 1, June's 10 worms. Welcome tips come one at a time.
- [ ] 1.5 F3: the debug line says Day 1, about 7:00 AM. F3 again hides it.
- [ ] 1.6 Esc: the pause menu (Esc resume, Q save & quit). Solo, the world stops. Esc closes it.
- [ ] 1.7 Esc, Q. Start again. **Continue** shows your world's name, day, bank and "saved ... ago". Press it: you
  are back where you were. (The full save test is 7.14.)

## 2. Walking, camera, jump, swim, ladders, falls (10 min)
- [ ] 2.1 Walk, sprint (Shift), walk slowly (Ctrl). Starts and stops ease in; she leans into turns. Footsteps, dust
  at a sprint.
- [ ] 2.2 Camera: follows calmly, about 30° down; walls between you and the camera fade (walk behind the shack).
- [ ] 2.3 Jump: tap = a hop, hold = higher. A big drop lands with a thud and a small camera dip.
- [ ] 2.4 Walk off the pier into the sea. You swim, bobbing on the waves. Tab does nothing in the water.
- [ ] 2.5 E at a ladder (the pier's, or Biscuit's stern ladder): you climb straight up. The camera rises, then glides
  in over the deck (no snap, no sliding under the deck).
- [ ] 2.6 **F8**: you go limp, lie about a second, then get up (about 4 s in all). Try it on the pier edge: you can
  fall into the water.
- [ ] 2.7 **F9** a few times: Pip, Bruno, Stretch, Rani, Old Gus, Sully. The rod, camera and jump fit each body.
  Go back to the one you like (the real way is the wardrobe, section 8).
- [ ] 2.8 *Looks:* does it feel like Ponyo Coast (stylised, warm), never realistic? A grey rock beside Walt's cot or
  a hump at the pier root should be **gone** in this build (map B fix).

## 3. The boat: Biscuit (20 min)
- [ ] 3.1 E at the motor: you sit, left hand on the tiller. W casts off (the "moored" toast goes). W/S step through
  reverse, stop, 1/3, 2/3, full. A/D steer. Steer away from the pier with A first.
- [ ] 3.2 Drive out, turn, come back. At the pier end, slow down: **F** moors her.
- [ ] 3.3 Walk the deck while she rocks: no sliding, no jitter, calm camera.
- [ ] 3.4 E at the bow: the anchor drops (a toast). E again raises it. Anchored, she drifts only a little.
- [ ] 3.5 Jump overboard, swim to the stern, E at the ladder: back on deck.
- [ ] 3.6 At night (section 7) the stern lantern is lit.
- [ ] 3.7 **Storm, rogue waves, leak.** Take her out and anchor off the pier. Press F11 until "Weather held: Storm".
  - [ ] Sky goes slate, rain, big waves. She rolls hard (about 15° anchored) but never flips.
  - [ ] Within about 1¼ min: a rising roar, a white foam crest running in from the side, and "Rogue wave! Hold C to
    brace!" 3 s before it hits. Three waves, about 80 s apart.
  - [ ] Hold C for one: you stay up. Don't for the next: you're knocked down. Near the rail you can go over the side
    (about 1 in 4). Climb back with E at the stern ladder.
  - [ ] Stopping on a wet deck takes a little longer (a slight skid).
  - [ ] Within about 2½ min: "She's sprung a leak! Patch the hole, or bail." Water rises inside as a see-through teal
    sheet; she is slower and leans. The hole spurts, with a red ring and a pin with an arrow (it points even off
    screen).
  - [ ] Hold E elsewhere on deck: you bail, the water drops.
  - [ ] Hold E at the hole for 10 s: you kneel and hammer a plank on, a knock each blow, the pin fills. "Hole patched.
    Now bail her dry." Bail her dry: no water shows when she is dry.
  - [ ] Left unpatched she never sinks; full of water the engine coughs and Dot offers a tow (only offered).
  - [ ] F11 once more: "Weather: the forecast's again".
- [ ] 3.8 *Looks:* is the storm exciting and fair? Did you always see and hear the wave coming?

## 4. Fishing (25 min)
- [ ] 4.1 Press 1: the rod comes out in about a third of a second. Walking, the tip lags and sways.
- [ ] 4.2 Hold LMB: the power arc round the distance label, a ring on the water. Short power ~6 m, full ~16 m; the ring
  turns gold at the sweet spot. Aim at the boat or land: the ring turns red, the bobber still lands in water.
- [ ] 4.3 She faces where she casts. The line sags in a curve, the bobber has a halo.
- [ ] 4.4 Count 10 bites (F3 shows the timer): each comes **4 to 10 s** after the bobber settles. Nibbles are small
  taps (tik); the bite is a big BLOOP with the bobber yanked under.
- [ ] 4.5 Strike too early: "Too soon!" on the left, bait kept. Too late: "It swam off.", bait kept.
- [ ] 4.6 **The bar:** one stop, green zone 1/8 of the bar. Hit it = **Rare** (worth 2×), miss = **Basic**. Nothing
  shakes when you miss. Roughly 1 in 4 Rare for a practised player.
- [ ] 4.7 **Heavy fish:** F5 to rod 2, F6 to Sardine, fish until "Something HEAVY!" (or Shift+F7: every fish heavy, see section 0).
  **Let it go** is calm. **Fight it**: one retry; the 2nd miss snaps the line.
- [ ] 4.8 **The reveal:** you hold it up with the rod still in hand, then the rod goes on your back and you hold the
  fish in both hands. E keeps it, Q throws it back over the side. "NEW species" on the left the first time.
- [ ] 4.9 **Hotbar:** the wheel and 1-8 move along it. 5 of one species and grade per stack; the 6th starts a new one.
  A full hotbar offers drop on deck or let go.
- [ ] 4.10 Tap Q: one fish drops at your feet (over the side it swims away). Hold Q: a ring shows where it will land,
  let go to throw. On deck it lies there; E picks it up.
- [ ] 4.11 **Rot:** each fish has a freshness bar and a stage on its name pill: Fresh, Good, Stale, Old, then Rotten
  at 48 h (12 h a stage). Shift+F10 twice with a fish in hand: Fresh becomes Good.
- [ ] 4.12 **Bait pouch:** hold R, point at a bait, let go: the bait chip by the hotbar says e.g. "Worms × 10". Tap R
  = next bait. Squid and Walt's Mix are locked ("Needs the tackle pouch", June sells it for $250).
- [ ] 4.13 **Rods and baits:** shrimp on Walt's bamboo rod: no sprats (mullet and mackerel instead). F5 once
  (Fibreglass) with shrimp: garfish and bream about two bites in three. F5 four more times goes back to bamboo.
- [ ] 4.14 Tab during a bite or fight: nothing opens; an open phone puts itself away.
- [ ] 4.15 *Feel:* 15 minutes of fishing from the anchored boat. Do you want to keep going?

## 5. Money: Pratt's, June's, the bank, Mari (30 min)
Shops are in Old Saltmouth, along the quay from the landing (the phone's Map shows them).
- [ ] 5.1 **Pratt's Fish Market** (Gideon Pratt, open 5 AM to 4 PM). E at the counter: each fish shows its price. Sell
  one, then **Sell all**. A green "Sold" note on the LEFT; the bank pill pulses and counts up.
- [ ] 5.2 Sell 10 of one kind one by one: the price slides 2% each (never below 60% of it). A "Good" fish pays less
  than a Fresh one.
- [ ] 5.3 After 4 PM: "Pratt's Fish Market · closed, back at 5:00 AM".
- [ ] 5.4 **Hook, Line & Sinker** (June Pike, 5 AM to 7 PM): tabs Bait / Tackle / Rods. Buy shrimp ($12 for 10) with
  ×1, ×2, ×5, Max. It comes out of the bank. With too little money June says "Need $X more"; the bank never goes below
  $0. Sardine's line names conger, mahi-mahi, blue shark; Squid's names snapper, grouper, swordfish.
- [ ] 5.5 **Mari:** she texts once the company has **earned $250 in total**, or on **Day 3 at 7 AM**, whichever comes
  first (Shift+F10 eight times gets you to Day 3). Her text pops up on the LEFT with a chat-bubble icon.
- [ ] 5.6 Hold Tab: her thread opens with the mooring offer ($40 a day). Buttons: **Sounds good!**, **Can you pay
  more?**, **Not now**. "Can you pay more?" works once: a laugh and a free lunch, still $40, then it hides.
- [ ] 5.7 Press **Sounds good!** The card says "Accepted · on her way" (inside the card, anchor icon whole). Within
  one game hour her taco boat ties up at the pier end. Contracts shows the deal.
- [ ] 5.8 From 9 AM to 10 PM, E at her boat: she pays **full price** for Everyday fish (sprat, mullet, mackerel), 10
  fish a day. At Pratt's a grey line says "Mari pays $X".
- [ ] 5.9 Next 7 AM: "Rent's in, landlord. +$40" on the LEFT; the bank and the Ledger show it.
- [ ] 5.10 **Mari's "no"** (a second world, New game): press **Not now**: "Okay! I'll ask again tomorrow." Next day
  $45, then $50; after the third no she gives up.

## 6. The phone (15 min)
- [ ] 6.1 Tap Tab: the phone slides in on the right; you take it out; you can still walk. The left side stays clear
  for toasts. Tap Tab again or Esc closes it; Backspace goes back a screen. Esc does **not** open the pause menu.
- [ ] 6.2 **The time is only on the phone** (status bar, and big on Home). Nowhere else on the HUD.
- [ ] 6.3 **Home:** time, Dot's forecast (from 6 PM it shows **Tomorrow**'s blocks, with the swell), what's next,
  what's due at 7 AM (rent, wages), the bank. App grid at the bottom of the screen.
- [ ] 6.4 **Messages:** threads, one-line previews cut at a word with "…". Hold Tab opens the newest unread.
- [ ] 6.5 **Contracts:** Mari's deal and its state.
- [ ] 6.6 **Bank:** today's in and out, what's due at 7 AM, every Ledger line (who, what, when, balance after).
- [ ] 6.7 **Forecast:** Morning, Afternoon, Evening, each with an icon, wind (kn), waves (m) and Dot's "how sure"
  dots on the right. Tomorrow's comes at 6 PM. The morning call is almost always right; when Dot is wrong it is one
  step off.
- [ ] 6.8 **Map:** "Saltmouth Island", your pin, the shops' pins in the right places.
- [ ] 6.9 **Journal:** opens (check what it lists).
- [ ] 6.10 **Settings:** Mouse look, Pad look, Invert look, Bar timing, Voice chat (Open mic / Hold V / Off). Turn
  Invert on, quit, restart: it stays on.
- [ ] 6.11 **Crew:** before Act 2 it says "Opens in Act 2". (Section 9 tests it.)
- [ ] 6.12 **Toasts** are always top-left; a toast that answers an older one replaces it (leak → patched, anchor
  down ↔ up). Stamps show icons (chat bubble, weather), not "…".

## 7. The day: light, sleep, weather (35 min, real time)
Don't use F10 for 7.1 to 7.8: feel a real day. New game or Continue at 7 AM.
- [ ] 7.1 **Six lights:** Day (from 7:30 AM), Golden (5:30 PM, ~10 min in: warm, long shadows), Dusk (7:30 PM:
  lamps on at the pier post, the shack window, Biscuit's stern), Night (9 PM, ~14 min in: blue, never black, the town
  dimmed with warm windows, the HUD a little cooler), Dawn (5:30 AM: peach horizon, pink-lit sea), Storm (from the
  weather). Each change blends over about 30 s.
- [ ] 7.2 Keep a fish caught between 5 and 7 PM for the rot check (7.7).
- [ ] 7.3 **The cot** (Walt's, by the shack): before 8 PM the prompt says "Tap: wardrobe · too early to sleep, back
  after 8 PM". **Tap E** opens the wardrobe (section 8). After 8 PM: "Tap: wardrobe · hold to sleep (skips to 7:00 AM)".
- [ ] 7.4 **Stay up:** 11 PM "Getting tired" toast. 1:30 AM the screen edges darken, you walk about 10% slower.
  2 AM you pass out where you stand: a fade, the night card. You wake there at 7:00 AM, nothing lost, and Walt texts
  "Bed's for sleeping, kid."
- [ ] 7.5 **Night card:** "Day N · ... is over", the day's money, the company bank, what's due at 7 AM, tomorrow's
  forecast with Dot's dots, and **"Saved ✓"** at the end.
- [ ] 7.6 **Morning:** a low ferry horn at 7:00 with the fade-in; a toast with the day and weather.
- [ ] 7.7 **Rot check:** the 5-7 PM fish says **Good** at 7 AM. One caught after 7 PM may still say Fresh (correct).
- [ ] 7.8 **Sleep properly:** after 8 PM, hold E on the cot about a second: "Sleeping…", you lie down (you can't walk
  while lying), fade, night card, you wake **in bed** at 7:00.
- [ ] 7.9 **Pass out at sea:** at the tiller, motor running, F10 to 1 AM, wait for 2 AM. You slump on deck, the
  throttle goes off, Biscuit anchors herself. You wake on the boat.
- [ ] 7.10 **Weather:** F11 through all six (about 30 s each). Breezy: whitecaps. Fog: you can't see far. Rain: grey
  and wet. Storm: slate sky. Each blends in, no snap. F11 again to let the forecast back.
- [ ] 7.11 **A forecast storm:** never on Days 1-2, at most one in 4 days. Dot texts **at least 3 game hours ahead**.
  Shift+F10 through days and check the Forecast until one comes.
- [ ] 7.12 **Rested buff:** buy a bed at Plank & Post (section 8), sleep in it. Morning toast: "Rested · A proper
  night's sleep. No heavy eyes tonight." That night, no dark edges at 1:30 AM.
- [ ] 7.13 *Looks:* is the night beautiful, can you still see where you walk? Did the sleep feel quick and clean?
- [ ] 7.14 **Save test:** write down the bank, your fish (kinds, freshness), bait, Mari's deal, the day and time,
  where Biscuit is and any water in her, where you stand. Esc, Q. Restart, Continue: everything the same, the clock
  carries on. Then close the window with the X instead: it saved too.

## 8. M5: shops, upgrades, the wardrobe (45 min)
Press **F7 twice** first (Act 3, +$10,000). Plank & Post and Hartley's are shut on Sundays (Day 7, 14...).

**Every shop**
- [ ] 8.1 The keeper stands behind the counter in opening hours (walks in at opening, out at closing). Outside them a
  closed sign gives the time it opens again. E at the counter opens the shop; Esc leaves. Walking away or closing time
  closes it ("Closing up. Come back tomorrow.").
- [ ] 8.2 Tabs along the top, cards with prices and stat changes, a live 3D preview on the right, the price in big type,
  the bank before → after. Buying: a till sound, confetti, a bounce on the button, a line from the keeper; a fanfare
  from $1,000.
- [ ] 8.3 Something you can't buy says why on the button ("Need $X more", "Owned", "Act 3", "Needs ...", shut).

**Doyle Boatworks** (Fergus Doyle, the slipway, 7 AM to 8 PM every day)
- [ ] 8.4 Behind Doyle's counter the boatshed doorway is warm planked brown, not a black hole (fixed in this build).
- [ ] 8.5 **Repairs:** engine rebuild ($90, ready at 07:00) and hull patch ($120, the leak stops for good). Rush =
  double price, done at once.
- [ ] 8.6 **Upgrades:** Engine I/II, long-range tank, deck lights, bilge pump, plating, rod holders, fish finder, fuel
  cap, autopilot. Each bought one shows on the boat. Engine I makes her faster; the deck lights light at dusk; the pump
  bails in a storm.
- [ ] 8.7 **Paint:** pick a channel (hull, stripe with 8 patterns, trim, deck stain), pick a colour: Biscuit takes it on
  the spot while you look (only your screen). Buy: Kit and Arlo walk over on land (never over water) and paint her with
  rollers, the colour sweeping across with a progress bar. "The one before" is free. Try bright and dark, day and night.
- [ ] 8.8 **The Kestrel** ($5,500): before you buy her she sits at Doyle's pontoon. Walk aboard: the helm refuses you.
  Buy her: she comes round to the landing and drives. Her paint and upgrades survive a save.

**Plank & Post** (Ingrid Marsh, the timber yard, 7 AM to 6 PM, shut Sunday)
- [ ] 8.9 **Furniture** (chair, desk, shelves, wardrobe, rug, fern, lamps, paintings, stove, trophy mount): appears at
  once in its own spot in the office with a squash-and-settle pop.
- [ ] 8.10 **Beds:** Walt's cot, hammock (the bunk, double bed and map table need the office upgrade first: the shack
  has no room. **Decide: keep this rule?**)
- [ ] 8.11 **Builds** (dock planks + bench, berths B2/B3, 4 dock lamps, power post, water tap, warehouse T1/T2, office
  T1/T2, balcony): "ordered", then built by **07:00** the next morning (sleep or F10) with a short timelapse.
- [ ] 8.12 Nothing bought stands in a walkway or a berth; you can walk round all of it.

**Hartley's Outfitters** (Mabel Hartley, the high street, 9 AM to 7 PM, shut Sunday)
- [ ] 8.13 Hats, tops, over-layers, legs, boots, gloves, and a $25 dye in 24 colours. The mirror view shows the item
  or dye **on you** before you buy (nothing blocks your face).

**The wardrobe** (tap E at any bed, or at the office wardrobe)
- [ ] 8.14 Six crew cards: Pip, Bruno, Stretch, Rani, Old Gus, Sully. Tap once: preview on the turntable (facing you,
  swaying). Tap again: you become them.
- [ ] 8.15 Tabs Hat, Top, Over, Legs, Boots, Gloves (Tab key cycles) from what you own, plus dyes. Faces, bodies and your
  player-colour band never change. R resets the clothes.
- [ ] 8.16 Check: after F9, does the wardrobe still show the body you are?

**The warehouse** (E at its door; prompt "Warehouse · N fish stored")
- [ ] 8.17 Left: your hotbar fish, Store / Store all. Right: racks (10 fish a slot). Click a slot to take one,
  Shift-click for the lot. With T2 the cold room (rots slower). Bottom: the crew bait shelf (section 9).

## 9. M6: workers (35 min)
Workers open in Act 2 (F7 from section 8 is enough). Buy the Kestrel first so Biscuit is free.
- [ ] 9.1 **The Mooring Line** pub (open 11 AM to 2 AM). Rosie's hiring board stands at the door, Rosie beside it.
  Three cards with initials, name and wage. Prompt: "Rosie's hiring board · 3 looking for work". Before 11 AM:
  "Rosie's back at 11 AM".
- [ ] 9.2 E opens the phone on **Crew → Hire at Rosie's**: three faces with Fishing and Sea stars (hollow stars =
  room to grow), a quirk, Rosie's one-line review, the wage.
- [ ] 9.3 **Hire** costs two days' wage. They start tomorrow at 7; Rosie texts you. The hired card comes off the
  board. After this first visit you can hire from the phone anywhere.
- [ ] 9.4 Each 7 AM brings three new faces.
- [ ] 9.5 **Assign:** Phone → Crew → Boats. Biscuit's card: "No skipper"; your hand under "Free hands". Assign, then
  › cycles Skipper, Spot, Bait (with shelf count), Top up, Shift (Early 05-13, Day 08-17, Late 13-22 +50%, needs deck
  lights), Back by (at least 3 h out). The card shows the expected catch, the hold and the wage. Confirm.
- [ ] 9.6 A boat you don't own is a locked card ("Second boat · Fergus sells boats at the yard").
- [ ] 9.7 **The shift:** next morning at its start the worker walks down the pier, toots and waves, and sails out,
  steering round the land. Text: "Leaving for Gullrock".
- [ ] 9.8 Follow in the Kestrel: near you Biscuit really drives (rudder, throttle). Far away she is a blue dot on the
  Map (**Track** on her card).
- [ ] 9.9 Mid-shift text with the count. Status pill: Sailing out, Fishing, Coming home, At berth; "N fish · X h left"
  and a progress bar.
- [ ] 9.10 At back-by she comes home and moors; the worker carries a fish crate up the pier; the fish are in the
  warehouse.
- [ ] 9.11 **Wages at 07:00:** one Ledger line per worker; Home and Bank show what's due.
- [ ] 9.12 **Bait:** at the warehouse, **Leave 10** moves bait from your pouch to the crew shelf. With Top up "June's
  delivers", June sends the rest in whole packs (+$5). With Top up off and an empty shelf, they stay ashore and text.
- [ ] 9.13 **Recall** at sea: home now, nothing in the hold.
- [ ] 9.14 **Fire** ashore: a day's severance plus anything owed. Rehiring them within a week costs double.
- [ ] 9.15 **Trouble** (by chance, over several shifts): a no-show (hungover, sick kid), the engine coughs, a line
  snaps, or **Aground!** with a red **Tow · $200** button (Doyle tows her at once; or the 7 AM tide lifts her free).
- [ ] 9.16 **IOU:** spend the bank down before 7 AM. The worker texts they'll work on trust; the bank stays at $0.
  Unpaid two mornings running, they quit (at sea, they bring the boat home first).

## 10. LAN co-op (with Tushar, 60 min)
**Setup:** the same build on both PCs (copy the whole folder privately, never a public link). Same Wi-Fi or router.
Headphones. Steam running on both for voice (the mic chip bottom left says "Mic (no Steam)" otherwise). The host picks
**Host on this network** and reads out the address under it; Windows may ask about the firewall: **Allow**. The other
picks **Join on this network**, types it, Enter. **One PC only:** run the game twice; Host, and Join with `127.0.0.1`.
Hosting starts a **new Day 1 world**: use F7 (host) again before 10.8.

**Joining and moving**
- [ ] 10.1 The joiner's view stays covered ("Joining the crew…") until in. They start on their own spot on the pier,
  never inside you. Two different crew members, each with their own colour on armband, back patch, name tag and the
  crew list (top right).
- [ ] 10.2 Esc in co-op: the menu opens, the world keeps running. I opens the invite list.
- [ ] 10.3 Walk into each other: you stop or slide round, no shaking; someone standing still is never pushed.
- [ ] 10.4 **G** right in front of a crewmate: they fall and get up (about 4 s); once per 3 s. At Biscuit's rail they
  can go over. Shove someone waiting on a bite: line reels in, bait kept. Shove someone fighting: fish and bait lost.

**The boat and fishing together**
- [ ] 10.5 Both on Biscuit: one drives, the other stands: no sliding or jitter for either. Swap the tiller both ways.
  The host can drop the anchor while the joiner drives.
- [ ] 10.6 Both fish: you see each other's cast, float, bite, fight and hold-up. A **ghost bar** over a fighting
  crewmate's name. Cast across each other: "Tangled!" with a boing, both reel in.
- [ ] 10.7 **Give / toss:** hold E on a crewmate (within 3 m): the fish goes over, both get a toast. Shift + hold E:
  the stack. Toss to a crewmate facing you: "Caught!". Toss to nobody: it lands on deck (first to E gets it) or swims
  away ("It's free!").
- [ ] 10.8 **Voice:** talk close: clear, a ring pulses on the talker's name tag. Walk to the far end of the pier: quieter,
  gone past ~35 m. Muffled under water.
- [ ] 10.9 Swimmer overboard while the other drives away: they float on the waves on both screens, then climb the
  ladder.
- [ ] 10.10 F8: the other sees you go limp and get up.

**The shared day and bank**
- [ ] 10.11 Host F10 to dusk: night on both screens at once; lamps on for both. Both phones show the same time.
- [ ] 10.12 Joiner sells at Pratt's: fish leave their hotbar, money on both bank pills. Joiner buys shrimp: from the
  shared bank, into the joiner's own pouch. Both Ledgers show the same lines, who did what.
- [ ] 10.13 Mari's text reaches both phones; either can accept; the rent toast shows on both.

**Sleep with a crew** (note where each of you wakes; nobody stuck in a wall, under the pier or in the sea)
- [ ] 10.14 You sleep: the other gets "<name> is going to sleep · night skips in 5 s", counting down with 5 dots.
  One skip for both.
- [ ] 10.15 Partner anchored at sea: wakes on the boat. At the tiller moving: wakes at the tiller, boat anchored.
  Fighting a fish: "Waiting for <name>'s fish…" up to 10 s. Swimming: wakes on a ladder. In town: wakes where they stood.
- [ ] 10.16 Both hold E on the one cot: one skip; the second sleeper steps out at the cot's foot or head end (its far
  side is against the office wall now), on the ground, never on top of the cot.
- [ ] 10.17 Both stay up: at 2 AM both pass out; both wake where they fell.
- [ ] 10.18 Host quits while the joiner lies in the cot during the heads-up: the joiner sleeps alone and wakes in bed
  (not stuck).
- [ ] 10.19 A real storm together: both get the rogue wave warning; the joiner patches; you see them kneel and hammer.

**Shops, wardrobe and workers in co-op**
- [ ] 10.20 Host buys a chair: it pops in on the joiner's screen too. A paint job: both see the lads paint, in step.
- [ ] 10.21 **Ask the crew:** buy something over a quarter of the bank (or over $5,000), like the Kestrel. The other
  gets an "Ask the crew" card on the phone (OK / Not now). OK buys it; Not now spends nothing. Never asked solo.
- [ ] 10.22 Both press Buy on the same item at the same moment: bought once, one Ledger line.
- [ ] 10.23 **Wardrobe:** host picks Pip. On the joiner's wardrobe Pip's card says who has it; they can't pick Pip.
  A new outfit shows on everyone's screen.
- [ ] 10.24 **Workers:** the joiner sees Rosie's board, hires and assigns from their own phone (one Ledger line each),
  leaves bait at the warehouse. Both see Biscuit sail out and come home; both phones get the texts. The joiner can
  Recall and Fire too.

**Leave and rejoin**
- [ ] 10.25 Joiner notes their fish, crew member and spot, then Esc, Q, restarts and rejoins: covered view until back,
  the camera never shows the host first, same fish, crew member, colour and place. Standing on Biscuit while the host
  drives about: they come back on the deck.
- [ ] 10.26 Hard drop: Task Manager → End task on the joiner, rejoin within 10 s, then again after 40 s. Both get in.
  The host's game never freezes; the name leaves the crew list after about 8 s and comes back.
- [ ] 10.27 Joiner Esc, L: "You left the crew. Biscuit's all yours." They play on alone.
- [ ] 10.28 Host Esc, Q: the host's game saves first; the joiner carries on solo. The joiner's title then shows the
  world under Your worlds as "a crew copy" (from the last 7 AM). It loads and plays solo.
- [ ] 10.29 *Feel:* is anything laggy? The fishing bar should feel exact (it runs on your own PC).

**Steam (optional):** Host a crew on Steam, press I, **Invite Tushar** (or **Copy the crew code**; it is also in the
pause menu). Tushar starts `PortBoss.exe` **first**, then accepts in Steam (with the game closed Steam starts
"Spacewar"), or picks Join a friend on Steam, then Type a crew code. If it fails, note what happened and use LAN.

---

## Known issues and not yet proven (don't report unless much worse)
**Not proven yet**
- Steam between two real accounts (lobby, invites, relay). Steam may show you as playing "Spacewar".
- A real microphone through Steam (tests only used a fake voice).
- A player who joins mid-session after things were installed (M5) was not tested.
- In co-op, a swimmer who just dropped in sits up to about 16 cm off the waterline for the first second, on their own
  screen too, while the body settles. After that, at most 13 cm on every screen in the tests.

**Found in this build's code**
- Hosting from the title always starts a new Day 1 world; there is no way to host a saved world yet.
- A joiner's "Joining the crew…" cover now lifts after 1.2 to 2.4 s (it was 0.5 to 0.9 s before M5/M6: more to sync).

**By design / not built yet**
- No clock on the HUD. You wake at 7:00, so you see only the end of dawn.
- Saves: 7 AM, Save & quit, closing the window, the host leaving. A crash loses play since then. In co-op only the host
  saves; the joiner gets a copy each 7 AM.
- Upgrades shown on the boat but not yet changing play: long-range tank, plating, rod holders, fish finder, fuel cap,
  autopilot. (Engine I/II, bilge pump, deck lights and repairs work.)
- Dot only offers a tow in a storm. A worker's aground tow ($200) is real.
- No "Not yet!" key to delay someone's sleep. No loose deck items sliding on a rogue wave.
- Deck-tilt sliding barely shows on Biscuit (narrow deck).
- Workers: the catch always goes to the warehouse (no "sell at best buyer", no order board); June's bait and the fuel
  are charged at the shift's start; morale is tracked but not shown; the pub has no interior (the board is at the door).
- The bank runs lower than the economy table (Pratt's 2% slide per fish, about $10 a fish), so Mari often comes on Day 3.

**Looks**
- The Kestrel's deck details are simple; hats fit one head size; dyes are one colour per slot, no patterns; the "over"
  layer is a shell over the top.
- Pratt's stall and June's counter still read a bit block-out. June's shop is a plain tabbed list.
- The top of the sky is a little pale; a soft mid-blue patch in the water by the pier; pale planks; small boat and
  cotton-wool wake while driving; the catch card hides the 3D hold-up (behind it, the angler's look at a big fish she holds up is off by up to
  about 16°: small fish ~6°, 35-50 cm fish 13-16°; the same on INTEGRATED-2); tiller chest-high; one face per character;
  orange (host) and red (4th player) name-tag dots look alike.

**Docs**
- `docs/HOW-TO-PLAY.md` is the old dock game. Ignore its keys.
- The pause menu's help line lists only F3, F5, F6 and hold R; the full list is the table in section 0.

## How to report
For each problem write down:
1. **When:** your PC's clock, and the game day and time from the phone.
2. **Where:** solo or co-op; host or joiner.
3. **What you did** (the keys) and the step number here (like "9.10").
4. **What happened, and what you expected.** Once, or every time?
5. **A screenshot:** Win+Shift+S, drag over the screen, paste into your notes. For movement (light, waves, camera) a
   short phone video is fine.
6. **The log:** paste `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Player.log` into Explorer's address bar.
   **Copy it right after the problem, before you start the game again** (each launch overwrites it; the run before is
   usually kept as `Player-prev.log` beside it). In co-op, get it from both PCs.
7. **Save problems:** also copy your world's folder from
   `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Saves\` (one folder per world).
8. **Looks and feel count.** "This felt slow" or "this looked cheap" is a real report. Every timing and look can be tuned.

Say which build: `panoptes/w-pb-integrate3 code @ 5e72dab`.

Notes: ____________________________________________________________
