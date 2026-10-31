# docs/plan: the Port Boss plan (copy, 1 Nov 2026)

This is a copy of Manan's plan folder (`Desktop\Port Boss\plan\` on his PC), taken on 1 Nov 2026 for the handover.
**Start at `../../HANDOVER.md`**, which explains what each file is for and which one wins when two disagree.

**Which doc wins (newest first):**
1. `gdd/MANAN-CHANGES-1031.md` (incl. "Manan, 1 Nov")
2. `gdd/DISCUSS-inventory-and-fish.md` (approved)
3. `gdd/CHANGES.md` §F > §E > §A–D
4. `gdd/{fishing,world,economy,systems}/*.md`
5. `replan/`
6. Everything at the top level: historical.

Edits marked *(Manan 31 Oct)* or *(Kufu review 31 Oct)* inside the gdd files are already applied. Paths inside these docs
point at Manan's PC (`C:\Users\asus\Desktop\Port Boss\...`, `C:\Users\asus\Desktop\port-boss`). Read them as
`docs/plan/...` and as this repo.

## What is here and its status

| Path | What | Status |
|---|---|---|
| `gdd/` | The full design plan (GDD) of the fishing-first game, 31 Oct + 1 Nov | **CURRENT, source of truth** |
| `gdd/MANAN-CHANGES-1031.md` | Manan's own changes on 31 Oct and 1 Nov, verbatim + readings | **CURRENT, wins over everything** |
| `gdd/DISCUSS-inventory-and-fish.md` | Inventory/warehouse + the 15 fish, all 5 picks approved | **CURRENT** |
| `gdd/CHANGES.md` | Kufu's review (A–D), Manan's 31 Oct changes applied (E), map + picks (F) | **CURRENT** (§F wins over §E) |
| `gdd/BRIEF.md` | Manan's MUST list for the new game | **CURRENT** |
| `gdd/SOURCE-*` | Tushar's Codex concept + Manan's own notes (part 2 = MUST) and the Codex starter map | input, not final |
| `gdd/fishing/` | Fishing: flow, 15-fish roster CSV, model (`python fishing_model.py` asserts the MUSTs) | CURRENT, but the 1 Nov minigame (1 stop, 2 rarities) overrides its 3-stop/4-grade design |
| `gdd/world/` | Story, rival Heat system, cast, acts, phone threads, maps A/B/C (B = chosen) | CURRENT |
| `gdd/economy/` | Money, stages, shops, workers, co-op bank; `python economy_model.py` regenerates tables + xlsx | CURRENT |
| `gdd/systems/` | Hotbar, phone, time, sleep, weather, boats, UI (fonts, tokens, mockups), tech, **roadmap M0–M12 (§5)**, open questions (§6) | CURRENT |
| `gdd/systems/mockups/` | 8 UI mockups (HTML sources + PNG). `render.sh` re-renders them with headless Chrome | CURRENT, with 2 stale screens. 05 still shows a clock card and the 3-stop / 4-grade bar. 03's Map screen still shows map A and fishing zones. The 31 Oct and 1 Nov rules win |
| `build/M1-BRIEF.md`, `build/m1-log.txt`, `build/M1-F6-CHECKLISTS.md` | The M1 (boat + fishing) build brief, its log and the self-tested F0–F6 checklists | CURRENT |
| `replan/` | 31 Oct re-plan: art directions (`artdir`), 6-character cast + kit (`chars`), game-feel audit + spec (`feel`), Steam market research (`market`), story premises (`story`) | input to the gdd. `chars/` and `feel/` are still the spec for the character kit; `story/` is superseded by `gdd/world` |
| `mixamo-list.md` | The Mixamo clips to download (names, settings, folder) | CURRENT (the clips are gitignored: download them yourself) |
| `music-brief.md` | Suno music brief (old game) | historical; the Suno rules still apply |
| `friend-plan.md`, `critic-*.md`, `ideas-shortlist.md`, `art-pack-options.md`, `w1-brief.md`, `overnight-brief.md`, `refine-brief.md`, `art/` | The OLD dock-work game (28–30 Oct): friend's plan, 3 critics, overnight + refine briefs, Grok art prompts | **historical**: the 31 Oct direction replaced that game |

## Left out on purpose (local only, on Manan's PC)

**Paid Toon Harbor pack, or images made from it (never commit these):**
- `replan/artdir/renders/**`: 36 Unity renders of the pack: 0-pack-default, plus heroes, gameplay, close-ups and dusk shots for each of the 6 directions.
- `replan/artdir/sheets/**`: 7 sheets (overview-grid + one per direction), built from those renders.
- `replan/artdir/concepts/*-concept.png`: 6 Grok concepts painted from pack renders (the pack's tug + forklift).
- `replan/artdir/concepts/ref/*-ship.png`: 6 in-engine renders of the pack's tug + forklift.
- `replan/chars/img/lineup.png`, `lineup-scale.png`, `raw/lineup.png`: the cast in front of the pack's Tug King.
- `replan/feel/01, 03, 04, 05, 06, 08, 09 *.jpg` and `replan/feel/work/**`: frames from the old game, with pack buildings, containers and cranes behind the player.
- The original `gdd/systems/mockups/01, 03, 04, 05, 06 .png`: their background was the pack-derived Ponyo concept. **They were re-rendered here on a plain background from the same HTML.**

**Other images left out (not pack, just not needed):**
- `art/s1–s6*.png`: Grok concept art of the old game.
- `replan/chars/img/raw/**`: Grok originals and rejected takes; the finals are in `img/`.

**Other files left out:**
- `replan/market/raw/cc.html`, `repo.html`: scraped Steam store pages.
- `_work/` folders, `__pycache__`, `.claude/`.

If `replan/chars/README.md` or `replan/artdir/README.md` links an image that isn't here, it is one of the files above.
