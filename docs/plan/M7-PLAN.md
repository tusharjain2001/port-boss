# M7: demo polish → the Steam demo (plan, 8 Nov 2026)

**Branch:** `m7` (from `main` @ `3c48ae6`). **Status:** being built. Nobody can test by hand right now (Tushar, 8 Nov), so every step is tested headless (`tools/m7test.sh`, `tools/companycheck.sh`) and committed on `m7`. The branch is pushed to GitHub but not merged. It goes to `main` only after Tushar or Manan plays it (HANDOVER rule 10).

**Scope** (systems §5.2 M7): the first 60 minutes (gdd world §9) end to end, settings and accessibility, controller glyphs and a Steam Deck check, sound, the demo's ending, the demo build. Publishing anything on Steam stays Manan's (rule 7; the Steam fee is rule 2).

## Tushar's decisions (8 Nov)
| Question | Answer |
|---|---|
| Where the demo ends | **The evening of Day 4, about 1 hour in.** Voss's yacht has passed in the fog, Hugo & Fifi ask to moor (Act 2 begins), then a "to be continued" card with Voss's goons. |
| Music | **A music slot.** Build the player (day / night / storm, fades, a volume slider) with silent placeholders. Manan adds the tracks (no Suno downloads: rule 4). |
| Unity | May be closed and reopened as needed (for Blender exports). |
| Merging | Everything stays on `m7`, pushed as a branch. It is merged after a hand test. |

## Adapting the gdd's script to the game as it is
- "★★ / ★★★" in world §7 and §9 become **Rare** (rule 29: Basic and Rare only).
- Day 1 starts at 07:00 (the script says 06:55). Walt's first texts come at 07:02, as in world §7.2.
- Mari stays as built: Day 3, or $250 earned (rule 40).

## Steps (one at a time, each tested and committed)
1. **The story driver.** *(built 8 Nov)*
   - `Story.cs` (plain C# in the company): a table of beats (a text, a letter, a call, a journal page, an act), each with a time window, so a sleep never makes one fire hours late.
   - Events from the world ("rod", "cast", "sold", "fixed:…"). A crewmate's machine sends them to the host (`pb.ev`).
   - Act 2 begins once Act 1's goal is met (Biscuit's engine rebuilt; Mari moored, or she gave up; Amara's first order delivered) from 6 PM on Day 4, and at 6 PM on Day 5 at the latest.
   - `StoryUi.cs`: Walt's journal pages as a side card; calls that ring (Tab opens the call screen; Answer plays captions; a missed call leaves a voicemail in the thread); letters on a paper card.
   - In the phone: calls and letters sit in each contact's thread, and the Journal app holds Walt's pages and letters. The Crew app opens in Act 2.
   - Walt's, Pratt's and Fergus's first texts (world §7.2). The Carbon rod opens in Act 2 (it never could before).
   - Clock-driven company changes (a beat, Mari's texts, the rent) now reach the crew at once.
2. **The first morning.** *(built 8 Nov)*
   - A new game fades up in Walt's office beside his cot (gulls first), then his letter on the desk (a 6-line card). (Standing, not lying in the cot: held camera shots inside the small office kept landing in a wall.)
   - Biscuit starts half full of rainwater: bail her. Then pull-start the outboard (3 pulls).
   - The mailbox at the office for the Day 4 letter.
   - Walt's "Is that my Biscuit" text when the engine catches.
   - Built as: W at the tiller pulls the cord. Before Doyle's rebuild the third pull catches, after it the first. Flooded (a quarter full) she coughs. She's cold again every morning, and the hull weeps a little until the $120 patch. Real games only (tests: `-pb-coldstart`).
   - Known: the pull is heard (the cord, a cough) and felt (a lean back, toasts), but at the tiller camera's distance the yank isn't really visible yet.
3. **Contracts and Amara.** *(built 8 Nov)*
   - The phone's Contracts app gets real orders.
   - The Pelican on Lantern Key (a Blender model). Amara orders 3 Rare mackerel by 8 PM tomorrow at $30 each, and delivering them meets Act 1's goal.
   - Walt: "AMARA CALLED ME".
   - Built as:
     - `Orders.cs` (company): offer / Deal / Not this time / deliver / late / again the next day at 11:30.
     - `Pelican.cs` + `art-src/m7/build_pelican.py` (`PB_Bld_pelican.fbx`, 13.7k tris): on Lantern Key's east beach, found with `-pb-probe`. The restaurant has a terrace, parasols, the counter and a name arch with a pelican on it, plus a 23 m jetty with its lamp.
     - Visiting berths (`SkiffBerth.cs`): F moors at The Pelican, and a boat moored away is saved as anchored.
     - Amara, in chef whites, keeps the counter (a Works keeper).
     - Her order card is in the thread and in Contracts, and on Home's "next up".
   - Not built (SUGGESTED in economy): The Pelican as an everyday walk-in buyer at 110%.
4. **Days 2–4.**
   - Fergus's $90 rebuild takes one in-game hour (not overnight); his hull offer.
   - June's shrimp from Day 2; Ingrid's "3 planks fixed, a bench free".
   - Mari's line to catch and tie, her string lights and music.
   - Gulls over the fish; the conger at night; a swordfish leaping.
   - Day 4: the fog, Voss's yacht *Efficiency* gliding past (a 4 s beat), Hugo & Fifi's request (Act 2).
5. **The demo's ending.** The "to be continued" card with Voss's goons, and a demo build that stops there (the full build plays on).
6. **Settings and accessibility** (systems §1.12): volume (master, music, SFX, ambience, voice), text size, captions, camera shake and FOV, a tap option for every hold, rebinding, colour-blind-safe shapes. The Esc menu links to them.
7. **Controller.** Pad glyphs on every prompt; a Steam Deck screen-size (1280×800) check.
8. **Sound.** The music player with its silent slots; more CC0 sounds where the game is quiet.
9. **The demo build.** A performance check, store screenshots from the game, `HOW-TO-TEST.md`. Then **stop** for Tushar's or Manan's verdict (★).

## Tests
- `tools/companycheck.sh`: `Company.StoryTest`: beats, lapses, events, letters, calls, pages, acts, the save.
- `tools/m7test.sh`:
  - `-pb-storytest`: solo, with a still of every card.
  - `-pb-storycoop host|rider`: at 150 ms + 2% loss.
- Each step adds its own checks to these, and the rest of the regression runs before each commit.
