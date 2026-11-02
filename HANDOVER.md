# Port Boss: HANDOVER (the one entry point)

**For Tushar's Claude** (or any new Claude Code session that only has this repo). Written 1 Nov 2026 by Manan's agents
(Kufu, Manan's boss Claude, and its workers). Manan said: *"his Claude should know as much as you do: all the next steps,
all the rules."* So everything we know is here or in the files this page points to. Nothing lives outside the repo
except the paid art pack, the Mixamo clips and old builds (§12).

Read in this order: this file → `docs/plan/gdd/MANAN-CHANGES-1031.md` → `docs/plan/gdd/systems/systems.md` §5 (roadmap)
→ the gdd section for whatever you build next. Manan's own words, dated: `docs/handover/MANAN-PROMPTS.md`.

Contents:
1. What the game is
2. Where everything is, and which doc wins
3. ALL RULES
4. Current state
5. Setting up a new PC
6. Build and test
7. Next steps, in order
8. How Manan works
9. Open questions for Manan
10. Decision log
11. First hour for a new Claude
12. What is NOT in the repo

Glossary:
- **Manan:** the owner, who decides.
- **Tushar:** the developer taking over; the repo is his GitHub.
- **Kufu:** Manan's boss Claude agent.
- **`w-pb-*`, `pb-*`, `w-gdd-*`:** Kufu's worker agents.
- **`t-xxxxxxxx`:** their task ids.
- **"approval card":** Manan's yes/no button in his agent cockpit (Panoptes).
- **★ milestone:** a build Manan tests himself.

---

## 1. What the game is

**Pitch.** A fishing-first, open-sea co-op game (1–4 players, Steam), fully fun SOLO too. It is "an experience, not a pure
simulator" (Manan). You inherit grandpa Walt's dead dock and his leaky little skiff. You fish from life-size walkable boats,
earn, upgrade, hire workers who fish for you, then grow the dock into a port with piers, ship orders and your own shipping
boats. Meanwhile a rich rival, who took over the island's trade, notices you and pushes back. Price $6.99, working title
"Port Boss" (may change before the Steam page).

**This REPLACED the earlier game.** Until 30 Oct this repo held a dock-work game: carrying crates off boats, the Toon Harbor
look, captains, lorries. On 31 Oct Manan replaced it with the fishing-first design. The old code still builds, but it is
legacy (§4).

**Art direction: #4 "Ponyo Coast"** (Manan, 31 Oct):
- Ghibli-ish soft cel shading with painted blue-violet shadows;
- towering cumulus clouds, a luminous turquoise sea with depth bands, contact foam;
- warm paper-grain UI;
- light and shadow that change through the day (6 lighting states).

Stylised, never realistic: Manan said "we are tilting towards realism, that's why we're messing up". Spec:
`docs/plan/replan/artdir/README.md` §4, `docs/plan/gdd/systems/systems.md` §2 (UI) and §4.4 (lighting).

**The 6 characters** (Manan, 31 Oct: all 6 ship as presets; players change CLOTHES only, no face or body edits):

| | Body | Hat | Personality |
|---|---|---|---|
| Bruno | stocky "fridge" | beanie | quiet strongman |
| Stretch | tall, lanky | hard hat + ear defenders | over-eager apprentice |
| **Pip** (built first) | short, round | sou'wester | cheerful fish expert |
| Rani | broad | bandana | ex-crane-op boss |
| Old Gus | wiry | flat cap | unbothered old salt |
| Sully | average + huge helmet | brass diving helmet | silent diver |

How they're built:
- One Mixamo-compatible kit: X Bot joint ratios, chunky ~4.5 heads tall, big hands and boots.
- One material, ≤ 8.8k tris.
- The player colour sits on an armband / back patch / hat stripe that no outfit can hide.
- Spec: `docs/plan/replan/chars/` (README, cast.md, editor-kit.md; concept sheets in `img/`). The concept sheets are Grok
  images, for direction only: in-game models must be flatter, like low-poly toon.

**Core loop** (gdd fishing §0, as changed on 1 Nov):
1. Walk the deck.
2. Cast: hold, release, a landing ring.
3. The bobber settles.
4. A bite comes after a random **4–10 s**.
5. Strike within 1.2 s.
6. **One** timing stop on a bar with a small green zone: hit = **Rare (2× price)**, miss = **Basic**. Bar only on screen.
7. The reveal, then the fish goes into the hotbar: 8 slots, fish stack 5.
8. Sell, buy bait and gear, upgrade the boat.

Fishing must feel RELAXING: a miss only lowers the grade. The only way to lose a hooked fish is a HEAVY fish you choose to
fight; Manan said "KEEP IT" on 1 Nov.

**Fish and gear** (final, approved 31 Oct; `docs/plan/gdd/DISCUSS-inventory-and-fish.md`, `gdd/fishing/fish-roster.csv`):
- **15 fish, 3 per rod, 5 rods.** Each rod adds 3 fish and still catches the earlier ones:
  - Walt's Bamboo: sprat, mullet, mackerel;
  - Fibreglass $450: garfish, black bream, conger;
  - Carbon $1,600: snapper, mahi-mahi, grouper;
  - Deep-sea $6,000: sunfish, blue shark, swordfish;
  - Old Faithful $15,000: anglerfish, oarfish, **Old Brass** (the legend).
- Better rods widen the timing window and bring bites sooner, always inside 4–10 s (down to 4–6 s on Old Faithful).
- **5 baits:** worms, shrimp, sardine, squid, Walt's Mix.
  - Each bait favours a *set* of fish: favourite ×4, liked ×2, other ×0.5.
  - **Any fish can be caught anywhere** (no zones). Time and weather only nudge the odds ×1.5, never lock a fish out.
- **The legend Old Brass bites only with Old Faithful + Walt's Mix** (Walt's Mix: $40 at June's once you own Old Faithful).
  The first catch is a story moment; after that it bites again, rarely.
- Fish rot fully after exactly 48 in-game hours.

**Storage:** the warehouse at our dock from Day 1, in 5 tiers:

| Tier | Name | Price | Slots |
|---|---|---|---|
| 0 | Old Shed | free | 12 |
| 1 | Patched | $300 | 24 |
| 2 | Cold Room | $2,500 | +12 cold |
| 3 | Big Warehouse | $8,000 | 48 + 24 cold |
| 4 | Ice House | $30,000 | 96 + 48 cold |

- A slot holds 10 fish.
- Every upgrade and paint job **installs itself** in its set spot, with no editor or placing.
- No chests and no crates.

**World and story** (`docs/plan/gdd/world/world.md`; **map B "Crescent Bay"** is the chosen map,
`gdd/world/maps/map-B.png`):
- **Saltmouth Island** is a crescent around a calm west-facing bay.
- Our dock, **Hale's Landing**, is deep in the bay, with the town **Old Saltmouth** behind it.
- The rival **Adrian Voss** has **Voss Harbour** on the outer east coast. We can visit by boat but can't dock. **Marlowe**
  is his fixer; the goons are Big Dario and Little Felix.
- Grandpa **Walt Hale** is alive in a mainland care home and talks to us by phone, letters and his journal.
- **Walt's Cut**, a silted channel through the south horn, gets reopened late in the game.
- The first income is **Mari Duarte's** taco boat paying $40/day to moor. She asks on Day 3 at 07:00, or as soon as the
  total earned reaches $250.
- Shopkeepers:
  - June Pike: bait/tackle, Hook, Line & Sinker;
  - Fergus Doyle: Doyle Boatworks;
  - Ingrid Sundqvist: Plank & Post, dock upgrades;
  - Rosie Tull: The Mooring Line pub, hiring;
  - Kai Rennick: Swell Surf Co.;
  - Wen Hartley: Hartley's Outfitters, clothes.
- Pratt's Fish Market pays 60%. Dot is the harbour master.
- Later content: a chill, evil-funny cartel boss, Teddy Mango.
- The rival's pressure is a "Heat" system in 6 tiers. Goons come first, and every action is telegraphed and recoverable.
- The finale is a great storm, a choice and the Harbour Charter vote, then Endless.

**Phone and time** (`gdd/systems/systems.md` §1):
- The phone opens on Tab and is never in the hotbar. It has time + forecast, messages, contracts and hiring.
- **No HUD clock** (Manan): the time is read on the phone.
- 1 real second = 1 in-game minute. One player sleeping skips everyone to 07:00 and rolls the weather on.
- Notifications sit at the top-LEFT.

**Economy** (`gdd/economy/economy.md`):
- Start with $40. The first purchase is the $90 engine rebuild at about real minute 9.
- Solo takes ~17 h to the Charter vote, 2 players ~13.7 h, 4 players ~10.9 h; then Endless.
- One shared company bank. Prices never scale with player count; order sizes do.

**Workers:** hired workers are assigned to our boats from the phone and fish for us. This is the first automation, at M6.

---

## 2. Where everything is, and which doc wins

**Which doc wins on a conflict** (newest Manan word first):
1. This file's §3 rules (they collect every Manan rule with its date).
2. `docs/plan/gdd/MANAN-CHANGES-1031.md`, including the "Manan, 1 Nov" section.
3. `docs/plan/gdd/DISCUSS-inventory-and-fish.md` (approved).
4. `docs/plan/gdd/CHANGES.md`: §F, then §E, then §A–D.
5. `docs/plan/gdd/{fishing,world,economy,systems}/*.md`.
6. `docs/plan/replan/*`.
7. Everything older (historical).

If something is still unclear, it is a question for Manan (§9), not a guess.

**The plan: `docs/plan/`** (index with status per file: `docs/plan/README.md`)

| Path | What | Status |
|---|---|---|
| `docs/plan/gdd/MANAN-CHANGES-1031.md` | Manan's changes of 31 Oct + 1 Nov, verbatim + readings | **wins** |
| `docs/plan/gdd/BRIEF.md` | Manan's MUST list for the new game | current |
| `docs/plan/gdd/DISCUSS-inventory-and-fish.md` | warehouse + 15 fish (approved) | current |
| `docs/plan/gdd/CHANGES.md` | every cross-section fix and Manan change, with where it was applied | current |
| `docs/plan/gdd/fishing/` | fishing flow, F1–F13 build steps + checklists (§8), roster CSV, `fishing_model.py` | current, but 1 Nov overrides the 3-stop/4-grade minigame |
| `docs/plan/gdd/world/` | story, rival, cast, acts, the first 60 minutes script, maps | current (map B) |
| `docs/plan/gdd/economy/` | all prices and pacing; `economy_model.py` regenerates `economy.xlsx` + tables | current |
| `docs/plan/gdd/systems/` | hotbar, phone, time, sleep, weather, boats, UI tokens/fonts/mockups, tech, **roadmap M0–M12 (§5)**, open questions (§6) | current |
| `docs/plan/build/` | M1 brief, M1 log, M1 F0–F6 checklists | current |
| `docs/plan/replan/chars/`, `replan/feel/` | the 6-character kit spec; why the old character felt wrong + the feel spec (speeds, camera, weight) | current spec for the kit |
| `docs/plan/replan/market/market.md` | Steam research: niche empty; needs proximity voice, rejoin, shared progress; Next Fest June 2027 | current input |
| `docs/plan/replan/artdir/` | the 6 art directions (Ponyo Coast = #4); renders left out (paid pack) | current input |
| `docs/plan/replan/story/` | earlier premises | historical (gdd/world wins) |
| `docs/plan/mixamo-list.md` | the Mixamo clips + download settings | current |
| `docs/plan/*.md` at the top + `art/` | the OLD dock-work game: friend's plan, critics, overnight/refine briefs, Grok art prompts | **historical** |
| `docs/handover/MANAN-PROMPTS.md` | everything Manan typed about the game, verbatim, dated (IST) | reference |

**The code**
- **`Assets/Game/`: the NEW game (M1).** Scene `Assets/Scenes/M1.unity`, generated by `Assets/Game/Editor/M1Build.cs`.
  - Scripts in `Assets/Game/Scripts/`: `Angler.cs`, the fishing state machine, the boat, `Bot.cs` (test switches) and more.
  - Data in `Assets/Game/Data/`: the fish roster as data.
  - Shaders in `Assets/Game/Shaders/`: Ponyo toon, outline, Gerstner sea, sky.
  - Fonts in `Assets/Game/Fonts/`: Fraunces + Zen Maru Gothic, OFL.
- **`art-src/m1/`:** Blender Python that builds Biscuit, Walt's Landing and the 15 fish headless. `art-src/worker/` is the
  current placeholder worker.
- **`Assets/Scripts/`: the OLD dock-work game** (`PortBossBuild.cs`, `GameUI`, `Player`, `Boat` …). Legacy, still
  builds. M1 reuses pieces of it through reflection: `CharAnim` (gait blend + two-hand IK), `PortBossBuild.MixamoClips` and
  the worker. Plan: tag it `legacy-dockwork`, then remove what M1/M2 don't use (gdd systems §4.1). Ask Manan first.
- **`tools/`:** build/test scripts (`tools/README.md`); M1 uses `m1build.sh` and `m1test.sh`.
- **`docs/`:**
  - `M1-HOW-TO-TEST.md` (current);
  - `ART-PLAN.md`, `PORT-STAGES.md`, `HOW-TO-PLAY.md`, `CODE-REVIEW.md` (old game, historical);
  - `bevel_check.py`, `wobble*` (old helpers).
- **`LOG.md`:** one line per work step (`time | step | result`). Keep adding to it.
- **Old handovers** `HANDOFF.md`, `HANDOVER-TUSHAR.md`, `HANDOVER-MANAN.md`: historical, about the old game. Their **setup
  steps (HANDOVER-TUSHAR §2–3) are still valid** and §5 below points at them.

**Stack:**
- Unity **6000.5.6f1**, URP;
- Netcode for GameObjects 2.13.3, Unity Transport, Multiplayer Play Mode 2.0.2;
- Facepunch.Steamworks 2.5.2 on Valve's test app **480** (`steam_appid.txt`);
- MCP for Unity 10.3.0;
- Git LFS for binaries (`.gitattributes`: png/jpg/fbx/blend/wav/mp3/ttf…).

M1's HUD is uGUI ("paper" style per the mockups); the plan's UI Toolkit move (systems §2.8) is not done yet.

---

## 3. ALL RULES

**HARD** = Manan's non-negotiable rule: never break it; if a job can't be done without breaking it, stop and ask.
**MUST** = Manan's requirement. **(plan)** = our process rule that Manan accepted with the plan. Source in brackets.
Where Manan said "Manan's PC", read "the PC you run on" (Tushar's): the point is not disturbing the person at it.

### Money, legal, publishing
1. **HARD: the Toon Harbor Pack is PAID (SICS Games, Unity Asset Store, single-entity licence, no redistribution) and this
   repo is PUBLIC.**
   - Never stage, commit or push anything from `Assets/ThirdParty/` or `Assets/Toon Harbor Pack/`, nor any render,
     screenshot or AI image made from the pack. Never `git add -f` an ignored path.
   - Before EVERY commit run `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'`. It must print **0**.
   - Never upload the `.unitypackage` anywhere public.
   - [Manan 29 Oct; repo checked PUBLIC again on 1 Nov 2026]
2. **HARD: no money on Port Boss until Steam Early Access.**
   - No paid assets, tools, plugins, credits or services. Free only: Unity Personal, Blender, Mixamo, CC0 sounds, OFL fonts.
   - The already-bought Toon Harbor pack may still be used.
   - Manan pays the Steam Direct fee ($100) himself, and only after he plays a build and judges it worth it. Until then the
     game stays on test app 480. [Manan 28 + 31 Oct]
3. **HARD: no secrets in the repo** (public). No keys, tokens, passwords, personal server details or `.env` files. Manan
   keeps credentials in his password manager; write *where* a secret lives, never the value. [Manan's standing rule]
4. **HARD: Suno (AI music): never download.** Manan downloads every Suno take himself. Spending Suno credits needs his
   approval. Any AI music in the game must be disclosed on the Steam page. [Manan 22 + 29 Oct]
5. **MUST: no AI-generated assets ship.**
   - Grok/AI images are for concept and direction only, never game art, UI or store art.
   - Store assets are real in-game captures. (The market research also warns of the AI-art backlash.)
   - [Manan 31 Oct re-plan HARD list; market.md; systems §2]
6. **MUST: we make the assets ourselves in Blender; no more asset purchases.** [Manan 31 Oct]
7. **Ask Manan before spending, publishing or deleting:**
   - Steam page, posts, public uploads, making builds public, pushing the plan somewhere public.
   - Deleting files, builds or branches.
   - Anything that touches money.
   - Routine creative and technical calls inside an approved milestone are yours ("make your own calls"). [Manan 24 Oct]
8. **Free web research and extra checks need no permission.** Cite links and dates; verify numbers live, never from
   memory. [Manan 31 Oct]

### Process
9. **MUST: build ONE thing at a time, test it, then the next. Quality over quantity.** A milestone can be cut smaller,
   never rushed. [Manan 31 Oct]
10. **MUST: Manan tests every significant build (every ★ milestone)** before the next one starts. The first build was
    boat + fishing only. [Manan 31 Oct]
11. **MUST: no building beyond what Manan has approved.** He reviews the plan step by step ("I couldn't read the whole
    plan; we'll go step by step"). M1 was approved ("start the first build now"); the roadmap order below is the plan he
    said GO to. New features or scope changes go to him first. [Manan 31 Oct]
12. **MUST: everyone works in the SAME repo** (this one). Work on a short-lived branch, get it reviewed, then merge to `main`.
    `main` always builds. Don't create separate project folders or repos. [Manan 28, 30, 31 Oct]
13. **MUST: keep only the latest build on disk** (one folder). Deleting the older ones needs Manan's OK. [Manan 31 Oct]
14. **Players only get release builds** (no Development console, no `-pbDev`). Every ★ build ships with a `HOW-TO-TEST.md`
    holding the milestone's Manan checklist. [29 Oct; systems §5.1]
15. **Judge on LOOKS, not on "it works".**
    - Compare with the approved look (Ponyo mockups and concept sheets) and reject anything cheap-looking before Manan sees it.
    - **Watch the capture VIDEO (motion) before calling any build ready, not just stills.** Make frame sheets every 0.25 s
      through walk, cast and drive.
    - Check every look or motion caveat yourself before delivery. M1 was handed over from 4 stills and its animations
      weren't clean; Manan had to ask.
    - [Manan 29 Oct "nothing looks good"; Kufu rule 31 Oct]
16. **Don't disturb the person at the PC.**
    - No pop-up windows and no stolen focus. Run Unity in `-batchmode`, test runs headless or offscreen.
    - Windowed runs only after the PC's owner says OK (`WINDOW=1` in `tools/`).
    - Never record with ffmpeg gdigrab: it grabs whatever window is on top. Record in-game frames (`-pb-film`).
    - While Manan is gaming: nothing visible at all. [Manan 29 Oct]
17. **One Unity instance at a time.** Have ≥ 5 GB free RAM before starting Unity or Blender, and close what you open.
    Close the editor before any batchmode run. [briefs 30–31 Oct]
18. **Before deleting any build folder or file:** `tasklist` for a running `PortBoss.exe` from it, then ask. Never run a
    LAN co-op test while someone is playing a build (a solo game listens on UDP 7777). [lessons 30 Oct]
19. **Scenes, prefabs, materials and skins are generated by code** (`M1Build.cs`, `PortBossBuild.cs`): change the code,
    never the generated assets by hand. Don't commit regenerated noise (`Skin.asset`, prefab/scene/ProjectSettings churn).
    [HANDOVER-MANAN]
20. **Every new feature gets a headless test switch** (`-pb-*` in `Bot.cs` / `AutoRun.cs`) so it can be tested in batchmode.
    Every step gets a line in `LOG.md`. [convention]
21. **Present only vetted options** (critics/research first), never raw brainstorms. Keep projects separate (no tie-ins
    with other work). [Manan 28 Oct]

### Look and UI
22. **MUST: stylised, never realistic.** Art = **#4 Ponyo Coast**, with light changing through the day. [Manan 29 + 31 Oct]
23. **MUST: characters are OUR OWN** (never the pack's people). All 6 presets ship; players edit **clothes only**, no
    face/body/other edits. [Manan 30–31 Oct]
24. **MUST: premium UI, no "AI slop".** New premium fonts: Fraunces (SOFT) + Zen Maru Gothic, OFL. Day/time text must look
    designed. **Alerts and notifications on the LEFT** (top-left). [Manan 31 Oct]
25. **MUST: no HUD clock.** The time is read on the phone (Home + Forecast). [Manan 31 Oct]
26. **MUST: the hotbar rod icon looks like a proper fishing rod** (not a toothpick). The **mouse wheel scrolls the
    hotbar**. [Manan 1 Nov]

### Fishing and boat
27. **MUST: fishing feels RELAXING.** A miss only lowers the grade. [Manan 31 Oct]
28. **MUST: bite after a random 4–10 s** after the bobber settles. A bobber shows where the bait is. Cast, bobber and reel
    animations are polished. [Manan's notes, gdd BRIEF]
29. **MUST (1 Nov, overrides the gdd's 3-stop / 4-grade design):**
    - the minigame happens **once per catch: one stop**;
    - the green zone is **1/8 of the M1 width**;
    - **only the bar on screen** (no white info card, chips, dots or hints);
    - **2 rarities only: Basic and Rare; Rare sells for 2× Basic**;
    - **heavy fish stay**: a fish heavier than the line shows a small "Something HEAVY! Fight it / Let it go" prompt before
      the bar, and only fighting can snap the line ("KEEP IT").
    - [Manan 1 Nov]
30. **MUST: during a cast the character's body and head face the cast direction.** [Manan 1 Nov]
31. **MUST: driving the boat: always the LEFT hand on the tiller, right hand free, no clipping through the body.**
    [Manan 1 Nov]
32. **MUST: better (more expensive) rods widen the timing window and bring bites sooner** (inside 4–10 s). [Manan 31 Oct]
33. **MUST: each bait type attracts a SET of fish**, not one bait per fish. [Manan 31 Oct]
34. **MUST: 15 fish, 3 per rod (5 rods); any fish anywhere, no zones.** The legend needs the best rod (Old Faithful) AND the
    best bait (Walt's Mix). The 5 picks: a warehouse slot = 10 fish; Walt's Mix $40 once you own Old Faithful; Old Brass a
    story moment, then rare; time/weather ×1.5 nudge; the rod ladder as listed. [Manan 31 Oct]
35. **MUST: bait is a buyable resource;** each bait gives different fish and prices. **Fish rot: at least 2 in-game days**
    (built as exactly 48 h). [Manan's notes]
36. **MUST: 8-slot hotbar** (rod, fish stack 5, guns, collectibles). Fish go in the inventory, never crates. **The phone is
    never in the hotbar**; it opens on Tab. [Manan's notes]
37. **MUST: life-size walkable boats; fish from the boat in the open sea.** [Manan's notes]

### World, economy, progression
38. **MUST: solo and co-op (1–4) are both fully enjoyable.** [Manan's notes]
39. **MUST: start with a tiny damaged boat at grandpa's dock** (his once-bustling port, abandoned since the rival took
    over). [Manan's notes]
40. **MUST: first income = a small vendor asks by phone to moor at our dock and pays for it.** Mari arrives on **Day 3, or
    when total earned reaches $250**, whichever comes first. The engine rebuild is $90. [Manan 31 Oct]
41. **MUST: the warehouse exists from Day 1 and is expandable** (5 tiers). **Boat and office customisation and upgrades
    install themselves**: no placement editor, no paint mode. [Manan 31 Oct]
42. **MUST: map B, "Crescent Bay", as originally drawn** (not a hook; map A was rejected). Voss stays far. Crab and lobster
    pots stay out. [Manan 31 Oct]
43. **MUST: shops:**
    - bait/tackle;
    - surf shop;
    - boat shop (boats, upgrades, colours);
    - dock upgrade shop (piers, beds, the warehouse, furniture, office upgrades);
    - worker hiring.
    More are welcome. [Manan's notes]
44. **MUST: hired workers are assigned to our boats from the phone and fish for us** (the first automation). Later: build
    piers, take ship orders (unload containers with our cranes), own fishing and shipping boats, shipping consignments by
    phone. [Manan's notes]
45. **MUST: the rival notices us mid-game and first sends goons to threaten.** His bustling port is on the opposite side of
    the island: we can visit by boat but can't dock. Plan it now, build it later. [Manan's notes]
46. **MUST: one player sleeping skips the night to 07:00**, which also moves the weather on. Day, night and weather
    systems exist. [Manan's notes]
47. **MUST (later content line): a chill, evil-funny cartel boss** who has us smuggle. [Manan's notes]
48. The phone shows time (top-left), the day's forecast, messages and contracts, and hiring. [Manan's notes]

### Superseded (don't follow; listed so old docs don't confuse you)
- "Art direction = the Toon Harbor pack / its demo scene" (29 Oct) → Ponyo Coast (31 Oct).
- "Weather is random with no warnings or forecasts" (29 Oct, old game) → the new game has a phone forecast (systems §1.5).
- The dock-work game, crates, captains, contract board, Quaternius/UAL characters, the character editor with face/body edits
  → replaced 31 Oct.
- The 3-stop minigame with 4 grades (Common/Fine/Rare/Prized) → 1 stop, Basic/Rare (1 Nov).
- HUD clock card top-left → no HUD clock (31 Oct).
- Map A "Twin Coasts" and the reshaped "hook" map B → original Crescent Bay (31 Oct).
- "Tushar isn't working on it; we own the repo" (30 Oct) → **Tushar's Claude takes over after M1b** (1 Nov).

---

## 4. Current state

**M1 (boat + fishing), delivered 31 Oct 2026.** Merged on `main` at `392ac00`. Release on Manan's PC:
`Desktop\Port Boss\builds\20261031-2215-M1\PortBoss.exe` (+ HOW-TO-TEST, screenshots, `m1-capture.mp4`, test logs).

What M1 has (details in `docs/M1-HOW-TO-TEST.md` and `docs/plan/build/M1-F6-CHECKLISTS.md`):
- **Biscuit**, the skiff, built in Blender by script (7.7k tris), at **Walt's Landing** (24.8k tris), in the Ponyo look:
  toon + outline shaders, Gerstner sea with a CPU copy for floating, warm 3-stop sky with toon clouds.
- Kinematic float, driving (throttle notches + rudder), anchor at the bow, mooring (F), and push-out hull collision.
- Walkable deck in boat space; jump overboard; swim; climb the stern ladder (E).
- **Fishing F1–F6:**
  - rod draw/stow and back strap;
  - cast charge (1.1 s ping-pong) with a landing ring;
  - line arc and bobber with a halo;
  - wait 4–10 s with nibbles; bite; 1.2 s strike;
  - early/late alerts on the LEFT;
  - minigame (3-stop version, being changed in M1b);
  - heavy-fish prompt; reel; hold-up reveal; hotbar stacks of 5.
- 15 fish modelled. The roster is data. Debug keys: F5 rod tier, F6 bait, F3 overlay.
- Paper UI with Fraunces + Zen Maru Gothic; left alerts; no clock.
- Our placeholder worker in a fisherman outfit, driven by the Mixamo clips.
- Not in M1, by design: shops, phone, money, clock, time of day, weather, co-op, rot.

Proven headless:
- `-pb-fishtest`: 60/60 bites on screen at 4.105–9.938 s; 500 casts at 4.006–9.978 s.
- Grade mix matches the model.
- Heavy fish snap only on the 2nd miss; normal fish never snap.
- `-pb-boattest`: 38.5 m in 12 s, max roll 5.5°, anchor drift 1.26 m / 30 s.
- `-pb-swimtest` PASS.
- 0 exceptions.

**Manan played M1 on 1 Nov** and asked for the changes in rules 26 and 29–31.

**M1b (running when this was written):** worker `pb-m1-anim` on branch `panoptes/w-pb-m1-anim`. Scope:
1. One-stop minigame, green 1/8, bar only, Basic/Rare (Rare = 2×), heavy-fish prompt kept.
2. Left hand on the tiller.
3. The cast faces its direction.
4. A proper rod icon.
5. The mouse wheel scrolls the hotbar.
6. An animation pass (the animations weren't clean).
7. A Ponyo sky/sea pass: towering cumulus, turquoise depth bands, contact foam.

<!-- M1B-RESULT -->
**M1b result (Kufu, 2026-11-01):** M1b is built and checked by hidden runs; Manan has not played it yet.
- Build: `Desktop/Port Boss/builds/20261101-0521-M1b/PortBoss.exe` (Manan's PC; HOW-TO-TEST.md beside it). Proof: `Desktop/Port Boss/proof/M1b/` (66 s capture, frame sheets, test logs).
- Commit: M1b code `8b5cfa0`, merged to `main` together with this handover.
- Passed: animation-clean pass (keyed cast/wait/strike/fight/reel/hold-up/tiller clips, no rod Z-kinks, line is a sag curve); faces the cast direction (worst error 178° → 0° in 8 directions, `-pb-cast8`); seated, LEFT hand on the tiller; painted rod icon; minigame = one stop, green 1/8 width, bar only, Basic/Rare with Rare = ×2; heavy fish Fight it / Let it go, one retry, 2nd miss snaps, normal fish never snap; mouse-wheel hotbar (`-pb-scrolltest` 16/16); Ponyo sky + sea (the white "foam blobs" were the sun's specular, gone); fishtest 500×8 bite timer 4.0–9.96 s, 40 casts at 1× on screen PASS; pack check 0.
- Still open: Manan hasn't played M1b. FLAG for Manan: Rare was only ~15% for a practised player (fish income ~46% below the economy model) and a fought heavy fish snapped ~73%. **Tushar 1 Nov: Rare raised to 25% by slowing the pointer to 0.59× (green stays 1/8), so a fought heavy fish now snaps ~60% (67 of 100 in a forced-heavy run). F7 in game makes every fish heavy, to try it.** Tiller sits chest-high (lower it in the Blender boat; swing limited to ±12°); the rod on his back rises ~0.7 m over his head; fingers don't wrap the grip; arms are IK, less lively than mocap; worker model is still a placeholder.
<!-- /M1B-RESULT -->

**M1 closed (2026-11-01):** Tushar showed M1b (with his play-test fixes on `main` at `479c285`: smooth cast camera, E = straight up a ladder, Rare 25%, F7 every fish heavy) to Manan, who said to move forward. Next: §7 Step 2, the character kit, Pip first (branch `pip-kit`).

**Pip v1 (Tushar's PC, 2026-11-01), waiting for Manan's verdict on looks:**
- Built by `art-src/kit/build_kit.py` (Blender 5.2, headless): `blender -b -P art-src/kit/build_kit.py -- [X Bot.fbx] [norender]`.
  It reads `Assets/ThirdParty/Mixamo/X Bot.fbx` (pass another path if yours is elsewhere), scales X Bot's joints so the Head
  joint is at 82% of her 1.47 m (base 1.75 m x Pip's 0.84) and asserts the editor-kit §2b joint table. Output:
  `Assets/Game/Art/PB_Char_Pip.fbx` (committed) + renders in `art-src/kit/out/` (gitignored).
- One mesh, one material, colour from vertex colours like the boat (no palette texture yet), 8,670 tris. Egg body, big head
  with flat toon face decals (eyes, brows, nose, grin, cheeks), sou'wester, two hair puffs, striped top, yellow overalls
  with straps, buckles and pocket, orange mitts (thumb + index bones only), navy boots. Player colour (slot 0 orange) on
  the left armband and a back patch.
- In the game: `M1Build.Character()` imports her as a Humanoid with the upper-arm Down-Up limit at -25 (default -60),
  so every clip keeps her arms out of her belly. `Angler` uses her when `M1Assets.Pip` is set (else the old worker) with
  `Size` 0.83: the rod hold poses scale down in height and depth, not width.
- Proof (Tushar's PC, `Logs/pb/m1/pip/`): `compare.png` (concept / model / in game), `tour2/tour.mp4` (walk, cast, wait,
  catch, tiller). fish60, swim, cast8, boat, scroll all PASS with her, 0 exceptions. New test switch: `-pb-idleshots <dir>
  -pb-norod` (empty-handed standing + walking close-ups).
- Known: on the cast wind-up her raised mitt touches the hat brim for a moment; the yellow still reads bright through
  the toon shader; one face only (no expression shape keys); no other bodies, no Look v2 / editor yet.
- **Manan approved Pip v1 (2026-11-02, via Tushar): go on to the next step.**

**Feel pass (Step 2 item 2, branch `feel`, Tushar's PC 2026-11-02), waiting for Tushar / Manan to play it:**
- Movement (`Angler`): sprint ramps 4.2 -> 5.8 m/s over 0.6 s; eased starts 0.22 s / stops 0.18 s; a load slows
  starts, stops and turns (10 kg of fish carried = one "crate" of the feel spec, max 5: +0.05 / +0.07 s, -70°/s, -8% speed
  each, no sprint at 5); a 0.15 s skid on a hard reverse; the body leans from acceleration and banks into turns (spring).
- Jump: a 0.08 s crouch, 0.8 m held / 0.45 m tapped (0.6 m on the deck), g 18 up / 35 down, 0.12 s buffer + coyote;
  squash/stretch; landing in 2 tiers (a thud + camera dip over 1 m); arms flail on falls over 0.6 s. Footsteps on the
  heel strikes, dust at a sprint.
- Camera (`CameraRig`): pitch 30 (orbit 10-60), aims 1.2 m up and 0.8 m ahead of a run, follows 0.15 s across / 0.3 s
  up and down from the ground stood on (a jump doesn't lift it), +4° FOV sprinting, +0.35 m a crate. Distances scale with
  the body (the spec's 7.5 m was for the 1.78 m worker: 6.2 m for Pip, 5 m on the deck).
- Walls fade (PB_Toon `_Fade`, a 4x4 dither in the colour pass, dropped from the depth passes so the outline and SSAO
  skip it): anything under 8 m between the lens and her chest (the shack; the boat when she's in the water beside it).
  The pier and the land are single huge models: the camera still pulls in for those.
- Spring jiggle (`Jiggle.cs`): her head nods into starts and stops, the hat (its own `Hat` bone in `build_kit.py` now)
  wobbles on top. Acceleration only, a few degrees.
- Mixamo clips the game drives over everything (`CharAnim.Over`): Jumping Up (take-off + tuck), Falling Into Pool
  (stepping off the pier into the sea), Getting Up, Frisbee Throw. `M1Build.Feel` samples them on Pip at build (how far
  each lifts the hips, where the get-up starts). They're gitignored like every Mixamo clip: `docs/plan/mixamo-list.md`.
- Ragdoll-lite (`Ragdoll.cs`, built at runtime from the humanoid bones): F8 knocks you over, a crash at over 3 m/s throws
  you off the tiller. Limp 1-1.8 s, then up with Getting Up or Standing Up (at random) from where she lies (0.3 s blend; on her back a 0.45 s roll
  onto her front first), or into the water if she lands in it or draped over the side. 50 ms hit-stop.
- Letting a fish go (Q at the reveal) is now a throw: the fish rides her right hand through the wind-up and arcs 4.5 m
  over the nearest side. Fish over 140 cm still just slide back.
- Tests (all PASS, 0 exceptions): `-pb-fadetest <dir>` (behind the shack, swimming by the boat, the open pier),
  `-pb-feeltest <dir>` (every number above measured on the pier), `-pb-knocktest <dir>`
  (forward, back, side, off the tiller, walking off the pier), swim, ladder, boat, camtest, scroll, fish60, cast8, the
  tour (and `-pb-letgo`: every catch thrown back). Films in `Logs/pb/m1/feel/` on Tushar's PC.
- Not done from the spec: jiggle on the belly and hands; the carried-stack spring (no stack in M1).

**Known issues / not yet proven (from M1):**
- **The worker model is a placeholder** (the old code-generated worker in a fisherman outfit). The real cast comes with the
  character kit (§7 step 2).
- Steering off the pier: steer away with A first, or you scrape the piles.
- White foam blobs on the water read as smudges (the M1b sea pass covers this).
- Not yet felt by hand: driving feel on keyboard and pad; "15 minutes of fishing: do you want to keep going?"; "rod looks
  held, not glued"; nibble vs bite by sound or pad alone.
- The final filmed tour on the last M1 build was not re-run (the PC ran low on memory); its proof film is from one fix
  earlier.
- Steam co-op across two real accounts has never been tested (old game; LAN co-op passed). It matters at M2.
- The old dock-work game code is still in `Assets/Scripts/` and still builds (`tools/ubuild.sh`). It needs the Toon Harbor
  pack for its look. M1 does not.
- Mockup `03-phone`'s Map screen still shows map A; mockup `05` still shows a clock and 3 stops.
- `fishing.md` still describes the 3-stop / 4-grade bar; rule 29 wins.

---

## 5. Setting up a new PC (Tushar)

Follow **`HANDOVER-TUSHAR.md` §2 (setup checklist) and §3 (Toon Harbor import)**. They are still correct:
- Unity Hub + 6000.5.6f1;
- Git + Git LFS (`git lfs pull`, and check the Facepunch DLL is real);
- the batchmode first open;
- VS Code;
- .NET SDK, ffmpeg, Python + Pillow, uv;
- Blender 5.1 + MCP for Blender 2.1.9;
- MCP for Unity 10.3.0;
- `claude mcp add` for both.

Differences now:
- **The Toon Harbor pack is optional for M1** (the new game doesn't use it). You need it only to build the old game or for
  Voss Harbour later. Manan sends the `.unitypackage` privately. It goes to `Assets/ThirdParty/ToonHarbor` (gitignored).
- **The Mixamo clips are needed for M1's animations, and they aren't in git** (`Assets/ThirdParty/` is gitignored).
  1. Tushar (a person, with any free Adobe account) downloads them per `docs/plan/mixamo-list.md`. The settings are FBX for
     Unity, Without Skin, 30 fps, In Place where listed.
  2. Also download the **X Bot** character **With Skin** in its T-pose. The build uses the one FBX without "@" in its name
     as the shared rig. Manan's set was 25 required + X Bot + 2 alternates = 28 files.
  3. Put them in `Assets/ThirdParty/Mixamo/`.
  4. Never commit them.

  The build finds clips by name (`PortBossBuild.MixamoClips`). Without them it falls back to the CC0 UAL1 clips that are in
  the repo. That fallback was not checked for M1, so expect worse animation.
- Your Claude has no screen control: Tushar opens Unity and Blender, and clicks the MCP "Start Session".

---

## 6. Build and test (M1, Git Bash, repo root)

| Do | Command |
|---|---|
| Compile check (seconds) | `bash tools/cc.sh` |
| M1 headless setup + Win64 build | `bash tools/m1build.sh "$PWD/Builds/m1"` (add `-pbDev` for a dev build; log `Logs/pb/m1build.log`) |
| Character kit: Pip (Blender CLI, headless) | `"/c/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b -P art-src/kit/build_kit.py -- [path/to/X Bot.fbx] [norender]` → `Assets/Game/Art/PB_Char_Pip.fbx`, then the M1 build picks her up |
| Fishing bot, N casts (bite timer, grade mix) | `bash tools/m1test.sh Builds/m1/PortBoss.exe fish500 -pb-fishtest 500 -pb-timescale 8` (expect all bites in [4.0, 10.0] s; `-pb-sigma 70` = practised player; `-pb-rod n`, `-pb-bait id`, `-pb-fightheavy`) |
| Boat test (drive, roll, anchor hold) | `bash tools/m1test.sh Builds/m1/PortBoss.exe boat -pb-boattest` |
| Swim test (overboard, ladder back) | `bash tools/m1test.sh Builds/m1/PortBoss.exe swim -pb-swimtest` |
| Feel test (run/stop/sprint/skid/jump/load numbers, filmed) | `bash tools/m1test.sh Builds/m1/PortBoss.exe feel -pb-feeltest <dir>` (frames in `<dir>`) |
| Knock-down test (ragdoll + get-up, filmed) | `bash tools/m1test.sh Builds/m1/PortBoss.exe knock -pb-knocktest <dir>` |
| See-through walls test (shack, boat, open pier: shots) | `bash tools/m1test.sh Builds/m1/PortBoss.exe fade -pb-fadetest <dir>` |
| Screenshot tour (cast/bobber/bite/minigame/catch/hotbar) | `bash tools/m1test.sh Builds/m1/PortBoss.exe shots -pb-shots <dir>` |
| Film frames for the capture video | `bash tools/m1test.sh Builds/m1/PortBoss.exe film -pb-film <dir> -pb-filmsecs 60`, then `ffmpeg -framerate 30 -i f_%05d.png -c:v libx264 -crf 20 -pix_fmt yuv420p capture.mp4` |

- Logs go to `Logs/pb/m1/<name>/log.txt`, and every run prints its exception count: it must be 0.
- Paths (Unity exe, project, logs) are in `tools/env.sh`; override them from the environment.
- M1 composites its UI into batchmode screenshots (a two-pass black/white capture), so the HUD shows in `-pb-shots`; look at every PNG yourself.
- In-game debug keys: F3 overlay (bite timer, window ms, stop errors), F5 rod tier, F6 bait, F7 every fish heavy (on/off), F8 fall over.
- The old game's scripts (`ubuild.sh`, `reg2.sh`, `regress.sh`, `release.sh`, `morning.sh`, `capture.sh`) are in
  `tools/README.md`.
- **Release for Manan:**
  1. Make a release build (no `-pbDev`) into `Builds/releases/PortBoss-M<n>-<yyyymmdd>/`, with `HOW-TO-TEST.md` = the
     milestone's checklist.
  2. Add 6+ screenshots and a 60–90 s capture.
  3. **Watch the capture yourself** (rule 15).
  4. Hand it to Manan through Tushar privately. Never a public link without Manan's OK.

---

## 7. Next steps, in order (after M1b)

Start here once Kufu has filled in the M1B-RESULT block in §4 and Manan has played M1b.

**Step 1: close M1.** Manan plays M1b. Fix what he reports, re-test (headless + capture video), and send him the release.
M1 is done when the systems §5.2 M1 checklist and his "15 minutes, do you want to keep going?" both pass.

**Step 2: the character kit, Pip first** (~8 working days for all 6, $0).
- **Spec:** `docs/plan/replan/chars/editor-kit.md` §2b–§8 and `cast.md`, with the turnarounds and sheets in `chars/img/`.
- **Build:**
  - one X Bot-based armature (joints at X Bot ratios, so the Mixamo clips play cleanly), a chunky ~4.5-heads mesh,
    big hands and boots;
  - one material + palette texture, ≤ 8.8k tris;
  - player colour on armband / back patch / hat stripe;
  - built headless by `art-src/kit/build_kit.py` (Blender CLI, like `art-src/m1`).
- **Order:**
  1. **Pip** first, replacing M1's placeholder worker.
  2. Apply the feel spec (`docs/plan/replan/feel/README.md`):
     - run 4.2 m/s, sprint 5.8 m/s ramping;
     - a load slows acceleration;
     - camera ~7.5 m / 30° / FOV 55 / 0.15 s damping;
     - ragdoll-lite + get-up;
     - jump and throw clips with real anticipation.
  3. Manan judges Pip on looks in M1. **(Done: approved 2 Nov. The feel pass, item 2, is on branch `feel`, see §4.)**
  4. Then the other 5. The clothes-only wardrobe comes later (M5).
- **Risk:** modelling quality. The old worker was generated from code and read lumpy. Judge every pass against the concept
  sheets, but flatter, low-poly toon, not Pixar.

**Step 3: M2 ★ co-op on the boat** (~3 weeks; systems §5.2):
- Steam lobby (2–4) + LAN; boat-relative player sync; driver-owned boats;
- fishing F7: independent fights, tangles, giving and tossing fish;
- **proximity voice** (free via Steam/Facepunch);
- drop-in rejoin; player colours;
- the other 5 crew bodies.

Before M2, with Manan's OK: tag `legacy-dockwork`, then remove the old game code M1/M2 don't use (systems §4.1).
- Tests: `-pb-coop` (host + 3 bots, 150 ms), give/toss never dupes, rejoin restores the hotbar.
- Manan's checklist: invite Tushar via Steam; one drives, one fishes; voice fades with distance; "Tangled!"; hand a fish
  over; quit and rejoin; fall overboard → life ring.
- This is also the first real **two-account Steam test**.

**Then, one at a time, each a ★ with its own tests and checklist in systems §5.2:**
- **M3 The day:**
  - full day/night, 6 lighting states;
  - sleep (one sleeper skips to 07:00, every co-op edge case);
  - weather (6 states, phone forecast, Dot calls storms);
  - rot through the night; leaks + bail.
- **M4 Phone + first income + save:**
  - the phone (Home, Messages, Contracts, Bank, Forecast, Map, Settings);
  - **Mari's mooring = first income**;
  - Pratt's market (60%), June's bait shop and the bait pouch;
  - the shared bank; save/continue.
  - Redraw the phone Map screen from map B.
- **M5 Shops, upgrades and the wardrobe:**
  - Doyle Boatworks: repairs, upgrades, paint (Doyle's crew paints it), the Kestrel;
  - Plank & Post: beds, furniture, office, warehouse T0–T2;
  - everything **installs itself**;
  - Hartley's + the clothes-only wardrobe.
- **M6 Workers:** Rosie's hiring board; Crew app (hire, assign to a boat, bait, back-by); the catch goes to the
  warehouse; wages.
- **M7 Demo polish → public Steam demo:**
  - the first 60 minutes script (world §9);
  - settings and accessibility; Steam Deck check; audio;
  - the demo ends on the goons' card.
  - The Steam app ID / fee is Manan's decision.
- **M8** Surf + gear guns (guns as non-lethal tools, if Manan OKs).
- **M9** Rival v1 (Voss Harbour, Heat 0–3, goons first).
- **M10** Port v1 (piers, ship orders, cranes).
- **M11** Shipping + the finale.
- **M12** EA hardening.
- **Post-EA:** the cartel line, seasons, mega-ships.

**Timeline** (systems §5.3, written before M1 started early):
- Demo ~mid-April 2027 for the **June 2027 Next Fest**: register by 25 Apr; fest 14–21 Jun.
- EA ~Oct 2027.
- Open Q7: keep June with M5 trimmed, or move to the October fest. Quality wins if a milestone slips.

**Definition of done for every milestone** (systems §5.1):
- 0 exceptions in every headless scenario; tests green; perf gate;
- UI checked against the mockups;
- 60–90 s film + screenshots, **watched**;
- release build + `HOW-TO-TEST.md`;
- Manan's checklist handed over.
Then STOP and wait for his verdict.

---

## 8. How Manan works

- **He judges looks and feel, not features.**
  - Show him screenshots or a short video, never a feature list.
  - If it looks cheap, it isn't done.
  - He prefers stylised to realistic.
- **Short, plain answers in English** (even when he writes in Hindi).
  - Lead with the result, then what he needs to do.
  - He often types fast in capitals with typos: read the intent.
  - When a line can be read two ways (e.g. "map looking like cock" = map A's shape was rude, not "make it a hook"), write
    your reading, mark it ★, and confirm before building on it.
- **Batch questions.** Ask only what only he can answer, each with your recommended pick first. He often answers "go with
  your picks".
- **Ask before spending, publishing or deleting** (rules 2, 7, 13). Everything else inside an approved milestone: decide,
  say the call in one line, keep going.
- **He tests every ★ milestone himself**, then gives a list of changes (see 1 Nov). Apply them all, then re-test.
- **He downloads from Mixamo and Suno himself.**
  - Give him an exact list (name, card subtitle, settings, target folder), as `docs/plan/mixamo-list.md` does.
  - For Tushar's PC, Tushar does the Mixamo downloads.
- **He reviews the plan step by step**, not all at once. Keep docs short and readable; he can't read huge plans in one go.
- **He wants the game to be fun, not AI slop**, with real scenarios and polish (cast, bobber, reel animations especially).
- **He reaches Tushar's Claude through Tushar.** Report to Tushar with proof (paths, commit hashes, screenshots, test
  output), and keep `LOG.md` and this file's §4 current so the next session can resume.

---

## 9. Open questions still waiting on Manan

Recommended answers first. Sources: systems §6, fishing §9, world §0.3, economy §12, chars README, CHANGES §C/§E4.

| # | Question | Our pick |
|---|---|---|
| 1 | **Guns** (MUST as hotbar items): non-lethal tools only (flare, net, harpoon), never aimed at people, even in the cartel line? Does the harpoon scare off sharks that steal your catch? What do they cost? | Yes, tools only; flare $80, net $1,200, harpoon $3,000 |
| 2 | **Bait in a pouch on the rod** instead of a hotbar slot | Yes |
| 3 | **Boat fish box / cooler** (workers bring back more than their 8 slots) | Yes from the Kestrel; Biscuit only with a $300 cooler |
| 4 | **Sleep "Not yet!":** may other players delay one sleeper's skip once by 15 s? | Yes, host setting, default on |
| 5 | **Passing out at 2 AM:** no penalty? | No penalty |
| 6 | **No death:** knock-downs, falls and drowning never cost items or money? | Yes |
| 7 | **Demo date:** June 2027 Next Fest (tight) or October 2027? | June with M5 trimmed; switch if a milestone slips |
| 8 | **Steam Deck Verified** as an EA goal | Yes |
| 9 | **Co-op characters:** one player per crew member, or duplicates allowed? | One each |
| 10 | **Voice default:** open mic or push-to-talk | Open mic with an easy PTT switch |
| 11 | **"Relaxed deadlines" setting** (contracts ×1.5 time) | Yes, off by default |
| 12 | **Git LFS for Blender sources** on Tushar's GitHub (free 10 GiB storage/bandwidth a month)? | LFS, milestone commits only |
| 13 | **Selling at home:** a fish stall at our own dock later? | Later, as an upgrade |
| 14 | **Where the single green zone sits** (centred, or moved per catch) | M1b decides; centred unless it feels too easy |
| 15 | **Fishing animations:** Mixamo has only generic stand-ins. Cast, strike and hold-up need a Blender pass by us (or IK) | Blender/IK pass |
| 16 | **Grandpa:** alive (texts, calls, comes home in the finale), passed away, or a ghost? | Alive |
| 17 | **Rival name:** Adrian Voss with Marlowe as his fixer? | Yes |
| 18 | **Violence vs the goons:** shove, flare/net (comedic, non-lethal), never real guns | Comedic, non-lethal |
| 19 | **Cartel cargo:** named as drugs, or winking ("coconuts")? Affects the Steam content survey and age rating | Winking |
| 20 | **Co-op friends in the story:** host = Walt's grandkid, others = "the crew"? | Yes |
| 21 | **Seasons:** a 28-day year in an EA update, or none? | In an EA update |
| 22 | **Main length:** ~17 h solo / ~13.7 h for 2 players, then Endless. Right? | Yes |
| 23 | **Bank loans** for big purchases? | Yes |
| 24 | **Pack NPCs:** rebuild every human from our kit? | Yes (the new game uses only our people) |
| 25 | **Make the repo private?** It is public; Manan asked Tushar on 29 Oct; still PUBLIC on 1 Nov | Private, Tushar's call as admin |
| 26 | **Music:** the old game's Suno tracks (only "night" was picked). Does the new game use Suno music (disclosed) or CC0? | Ask when audio comes up (M7) |

Smaller design gaps for the milestone that needs them (CHANGES.md §C, §E4):
- deep jig (reusable vs consumable);
- where Walt's Mix comes from (now: June's, $40);
- chum (bought vs free from rotten fish);
- shop hours;
- prices for lines, reels and hooks;
- aligning fish "dusk" with the lighting dusk.

---

## 10. Decision log (dated; Manan's own words: `docs/handover/MANAN-PROMPTS.md`)

- **28 Oct:**
  - Port Boss replaces the earlier idea "Last Boat" (a port-manager co-op sim from a friend's plan). 3 critics say GO with
    changes.
  - Price $6.99, name "Port Boss" for now; Manan pays the Steam fee only if the build is worth it; app 480 until then.
  - Repo = this one (Tushar's), cloned; no separate folders.
- **29 Oct:**
  - Overnight build of the dock-work game. Manan: "nothing looks good" → judge on looks.
  - Stylised, not realistic. Toon Harbor pack bought ($53, Manan); never commit it (public repo).
  - Weather random. The MORNING build.
  - Handover to Tushar (HANDOVER-TUSHAR.md); Manan sent Tushar the pack privately. Tushar played 5 rounds
    (HANDOVER-MANAN.md).
- **30 Oct:**
  - Refine pass (no new features): our own character + editor, Mixamo animations (Manan downloaded 28 clips), code review,
    visuals sweep.
  - Merged to main a6fe3d4.
- **31 Oct:**
  - **No money until Steam EA** (HARD).
  - Re-plan (art directions, cast, market, story, feel). Then **NEW DIRECTION: fishing-first** (Manan's Game Concept + notes).
  - Rules: one thing at a time; no building until the plan satisfies him; same repo; one build on disk; Blender-made
    assets; premium UI with left notifications.
  - **Art = Ponyo Coast**; the **6 presets, clothes only**.
  - Relaxing fishing; bait sets; better rods widen the window and bite sooner.
  - GDD written (fishing, world, economy, systems) + Kufu review.
  - Manan's evening changes: warehouse; auto-install customisation; no HUD clock; 15 fish × 3 per rod; legend = best rod +
    best bait; no zones; map B; Mari Day 3 or $250. Then the 5 inventory picks approved.
  - Map B = original **Crescent Bay** (A rejected, not a hook); engine $90; Mari at $250 *earned*; pots out.
  - **M1 built and delivered** (392ac00, build 20261031-2215-M1).
- **1 Nov:**
  - Manan played M1: one-stop minigame, green 1/8, bar only, Basic/Rare; left hand on the tiller; cast faces its
    direction; a real rod icon. Then Rare = 2× Basic; mouse wheel scrolls the hotbar; heavy fish KEEP.
  - pb-m1-anim builds M1b + an animation pass + a Ponyo sky/sea pass.
  - **Tushar's Claude takes over after M1b**, with this handover.

---

## 11. First hour for a new Claude

1. Read this file fully, then `docs/plan/gdd/MANAN-CHANGES-1031.md`, `docs/plan/gdd/systems/systems.md` §0 + §5, and
   `docs/M1-HOW-TO-TEST.md`. Skim `docs/handover/MANAN-PROMPTS.md` (last two dates) to hear Manan's voice.
2. `git status`, `git log --oneline -10`, `git lfs pull`. Check the facts against §4: that `main` has M1/M1b and what the
   M1B-RESULT block says.
3. Check your setup (§5): Unity 6000.5.6f1 activated, batchmode first open done, `bash tools/cc.sh` builds. Ask Tushar
   for the Mixamo clips in `Assets/ThirdParty/Mixamo/` (and the pack only if needed).
4. **Run the safety check once** to see it work: `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` → 0.
   Confirm `.gitignore` covers `/Assets/ThirdParty/`.
5. Build M1: `bash tools/m1build.sh "$PWD/Builds/m1"`, then `m1test.sh … -pb-fishtest 100 -pb-timescale 8`,
   `-pb-boattest`, `-pb-swimtest`. Expect 0 exceptions and bites in [4, 10] s.
6. Make a `-pb-shots` tour and a short `-pb-film`, then **look at them** against the mockups
   (`docs/plan/gdd/systems/mockups/*.png`) and the concept sheets. Write down what looks cheap.
7. Read `docs/plan/replan/chars/editor-kit.md` and `replan/feel/README.md`: that is the next job (§7 step 2).
8. Tell Tushar in 5 lines: what state you found, what you'll do first, and any question for Manan (batched, with your
   pick).
9. Add a `LOG.md` line for the session start. Then work one thing at a time, on a branch, with the rules in §3.

---

## 12. What is NOT in the repo

- **Toon Harbor Pack** (paid): Manan sends the `.unitypackage` privately → `Assets/ThirdParty/ToonHarbor/` (gitignored).
  Images made from it stay on Manan's PC; the list is in `docs/plan/README.md`.
- **Mixamo clips:** download them yourself per `docs/plan/mixamo-list.md` → `Assets/ThirdParty/Mixamo/` (gitignored).
- **Builds:** on Manan's PC, `Desktop\Port Boss\builds\`. 21 folders:
  - `preview-20261028-2356`;
  - 16 overnight builds `20261029-*` (incl. `-1054-MORNING`);
  - `20261030-1848-REFINE`, `20261030-2346-REFINE`;
  - `20261031-2215-M1` (the latest delivered);
  - (M1b: see §4).
  The rule is to keep only the newest; deleting the rest waits on Manan's OK.
- **Proof** (screenshots, films, test logs) for every milestone: Manan's PC, `Desktop\Port Boss\proof\` (M1:
  `proof\M1\F4`, `F6`). The checklists are copied to `docs/plan/build/M1-F6-CHECKLISTS.md`.
- **Plan pages** (claude.ai artifacts: full plan, look book, inventory page): private to Manan; their content is in
  `docs/plan/`.
- **Manan's agent memory** (his private notes vault): every Port Boss rule and decision from it is in §3 and §10.
- **Concept/source files Manan referenced** (`Game Concept.docx`, his friend's message): their text is in
  `docs/plan/gdd/SOURCE-game-concept-and-manan-notes.txt` and `docs/plan/friend-plan.md`.
