# Port Boss full design plan (GDD), 2026-10-31 (Manan's plan GO bcad4228)

## What this is
Manan is REPLACING the current Port Boss with a new direction: a fishing-first, open-sea co-op EXPERIENCE that is also
fully fun SOLO. He wants every aspect planned in real depth ("not even loosely"). He reviews and refines the plan, and
nothing gets built until he is satisfied.

## Read first (all of it)
1. `SOURCE-game-concept-and-manan-notes.txt` (this folder).
   - Part 1 is a concept Tushar's Codex wrote: rival Adrian Voss, Greyhaven, 5 chapters, starter map. It is INPUT, not final.
   - Part 2 (from "now this is what tushar's codex whirred up") is MANAN'S OWN WORDS. Every requirement in it is a MUST.
2. `SOURCE-codex-starter-map.png`. The map is NOT final either: propose better.
3. `C:\Users\asus\Desktop\Port Boss\plan\replan\` contains:
   - market\market.md (Steam research);
   - story\ (earlier premises: reuse ideas freely);
   - chars\ (6-character cast + editor kit);
   - feel\ (why the old animation and character felt wrong, plus a feel spec);
   - artdir\ (6 art directions; top pick Golden Hour + Lantern Dusk evenings).
4. Vault `C:\Users\asus\panoptes-vault\projects\port-boss.md`, from the 2026-10-31 lines down (the new rules).

## Manan's MUST list (from his notes; do not drop any)
**Players and start**
- Solo and co-op (1–4) both fully enjoyable.
- Start: a very small damaged boat at GRANDPA's dock. In his prime grandpa had a bustling port; the rival took over and our port has been abandoned for a while.

**Fishing (a core mechanic)**
- Life-size walkable boats; fish from the boat in the open sea.
- Cast, then a bite after a random 4–10 s, then a minigame: a bar with a green zone, stop the pointer; 3 hits raise rarity and price.
- A bobber shows where the bait is.
- Every animation is polished: cast, bobber, reel.
- Bait is a buyable resource. Each bait type gives different fish and prices (orders/missions later).
- Rot: a fish takes at least 2 in-game days to rot fully.

**Inventory and phone**
- 8-slot hotbar inventory: fishing rod, fish (max stack 5; fish go in the inventory, NOT crates), guns, collectibles.
- The phone is NEVER in the hotbar. It opens on Tab, with a small but useful interface: time of day (top-left), the day's forecast, messages and contracts, and hiring workers.

**First income and progression**
- First income: a small vendor asks permission by phone to moor at our dock and pays us for it. Build from there.
- It is an experience, not a pure simulator: earn, then upgrade the boat, and so on.

**Shops**
- Bait/tackle.
- Surf shop (surfing gear).
- Boat shop: sells several boats plus upgrades and customisation (colours).
- Separate dock upgrade shop: piers, beds, chests, furniture, and upgrades for the run-down office.
- A place to hire workers.
- More shops welcome.

**Workers and the port**
- Hired workers are assigned to our boats (from the phone) and fish for us. This is the FIRST automation.
- Later, the port: build multiple piers; accept ship orders (berth, then unload containers with our cranes).
- We own fishing boats AND shipping boats, and can send ours for shipping consignments (offered by phone).

**Rival**
- The rival notices us a little later (mid-game) and first sends goons to threaten us.
- His port sits on the opposite side of the island, bustling. We can visit by boat but cannot dock there.
- Plan the full rival system now; it gets built later.

**World and time**
- One player sleeping skips the night to 7 AM. It also moves the weather on.
- The night/day/weather systems exist.

**Later**
- Future business lines, e.g. a cartel boss (chill, evil-funny) who has us smuggle. Plan it as a later content line.

**Quality**
- Premium look and feel. The old font was bad: pick new premium fonts.
- Alerts and notifications on the LEFT side.
- The day/time text must look designed, not basic.
- This must be a proper fun game with multiple scenarios, not AI slop.

## Process rules (Manan)
- Build ONE thing at a time, test it, then the next. Quality over quantity. Manan tests every significant build; the first build = boat + fishing only.
- Everyone works in the SAME repo, `C:\Users\asus\Desktop\port-boss`.
- Keep only the latest build on disk.
- Assets are made by us in Blender (free tools). No more asset purchases; the owned Toon Harbor pack may still be used.
- No money until Steam Early Access.

## How to write
- Write in depth: numbers, tables, examples, edge cases, solo vs co-op behaviour for every system, failure cases, and what the player sees and hears.
- Mark each thing MUST (Manan), SUGGESTED (ours), or OPEN QUESTION (for Manan).
- Research is allowed freely (Manan). Cite links and dates, and check facts live.
- Output goes to `C:\Users\asus\Desktop\Port Boss\plan\gdd\<section>\`: a main `<section>.md` plus images, mockups or spreadsheets as needed.
- When done, report_done with the paths. Kufu merges everything into ONE plan page.
- Read the other sections as they appear (`gdd\fishing`, `gdd\world`, `gdd\economy`, `gdd\systems`) and keep names and numbers consistent. If two sections clash, message the other worker.
- No Unity or Blender runs are needed in this pass (planning only). Keep RAM use light: another heavy test was running on the PC.
