# Port Boss M7 (the demo's first hour): how to test

Build: `Builds\m1\PortBoss.exe`, branch `m7`. It is not on `main` yet: it gets merged after one of you plays it.
The plan, and what was decided for it: `docs/plan/M7-PLAN.md`. Takes about 60 minutes. Start with a **New game**.

## Tester keys (host only in co-op)
| Key | What it does |
|---|---|
| **F10** / **Shift+F10** | +1 hour / +6 hours |
| **Ctrl+F10** | the next morning, 7:00 AM |
| **Ctrl+F7** | +$100,000 |
| **F7** | the next story act and +$5,000 |
| F11 | the next weather (held) |

## What's new
1. **The story moves by itself.**
   - Walt texts, calls (Day 2, 8 AM) and writes (a letter on the desk on Day 1, one in the mailbox on Day 4).
   - His journal teaches casting when you first raise the rod.
   - June, Pratt, Fergus, Ingrid, Dot, Amara, and Hugo & Fifi text too.
   - **Act 2 begins on the evening of Day 4** once you've done Act 1: Biscuit's engine fixed, Mari moored, and Amara's first order delivered. Otherwise it begins on Day 5 at 6 PM.
2. **The first morning.**
   - The game opens in Walt's office by his cot, then shows his letter.
   - Biscuit is half full of rainwater with a cold engine: bail her out (hold E on deck), then **W at the tiller pulls the cord**. The third pull catches; after Doyle's rebuild, the first one does.
   - Until the hull patch, she takes on a little water every day.
3. **Amara's order.**
   - On Day 2 at 11:30 she wants 3 Rare mackerel by 8 PM tomorrow, at $30 each. Press **Deal** in her thread.
   - Sail to **The Pelican on Lantern Key** (the islet west of the bay), **F** to moor at its jetty, walk to her counter, **E** to deliver.
4. **Days 2–4.**
   - Doyle's engine rebuild is done within the in-game hour.
   - June's shrimp is in on Day 2.
   - Catch Mari's line when she ties up (E at her bow).
   - Evening of Day 2 or later, on Walt's pier: something under the pier.
   - Day 3 or later, from 3 PM, out at sea: watch the water.
   - **Day 4, about 7:30 AM: Voss's yacht crosses the bay in the fog.**
   - Act 2: Hugo & Fifi ask to moor.
5. **The demo's end.**
   - Start **`PortBoss demo.bat`** (beside the exe; it runs `PortBoss.exe -pb-demo`).
   - 45 seconds into Act 2, an unknown number texts. Then the "To be continued" card, with **Keep fishing** and **Quit**.
   - The full game (plain `PortBoss.exe`) carries on instead.
6. **Settings** (phone, Settings; or Esc then S):
   - Sound: master, music, effects, sea and gulls, crew voices.
   - Text and motion: text size, captions, camera shake, field of view, boat roll, reduce motion.
   - Fishing: easy fishing (+30% green), needle speed.
   - Controls: sprint toggle.
7. **A controller** shows its own buttons on every prompt (RT to cast, X for E, View for the phone). **Y moors** at the tiller.
8. **Music** has empty slots for Manan's tracks (`docs/MUSIC-SLOTS.md`). The game is silent until they're in.

## Checklist
**The first morning** (New game)
- [ ] The screen fades up in Walt's office by his cot, with gulls. Walt's letter shows; E puts it down. Then his three texts arrive.
- [ ] Biscuit looks half full of water. At the tiller, W pulls the cord: she coughs ("flooded").
- [ ] Hold E on deck to bail until she's under a quarter full. Then three pulls: she catches, and Walt texts "Is that my Biscuit…".
- [ ] Raise the rod: Walt's journal page 1 at the side. Cast: two minutes later Walt says to wave at Dot.
- [ ] Sell at Pratt's: Walt texts about the price, then THE PELICAN.

**Days 1–2**
- [ ] June's shrimp says "In tomorrow" on Day 1; she texts at 6 PM; it's buyable on Day 2.
- [ ] Doyle's engine rebuild ($90) is done within an hour, and Fergus texts.
- [ ] Day 2, 8 AM: Walt calls. Tab, Answer: the call plays as captions. (A missed call leaves a voicemail in his thread.)
- [ ] Day 2, 11:30: Amara's order. Deal. Catch 3 Rare mackerel, sail to Lantern Key, F at the jetty, E at her counter: +$90. That evening Walt texts "AMARA CALLED ME".
- [ ] Mari (Day 3, or at $250 earned): accept, then catch her line at her bow when she ties up.
- [ ] Day 2 after dark, on Walt's pier: the conger's head beside the pier.

**Days 3–4**
- [ ] Day 3 from 3 PM, out at sea: a swordfish leaps.
- [ ] Day 3, 7 PM: Ingrid's planks offer.
- [ ] Day 4, 7 AM: the letter in the mailbox at the pier's head (the red flag is up; E to read it).
- [ ] Day 4, 7:25–7:55: Voss's yacht *Efficiency* crosses the bay in the fog. Dot texts.
- [ ] Day 4 evening (or Day 5, 6 PM): Hugo & Fifi ask. "Lovely, come"; once B2 is repaired (Plank & Post), they come and pay $35 a day.

**The demo**
- [ ] With `PortBoss demo.bat`: the unknown number ("nice dock. be a shame"), then the "To be continued" card. Keep fishing works; Quit quits.

**Settings and the pad**
- [ ] Text size 120% makes everything bigger; Field of view, Camera shake and Captions do what they say.
- [ ] With a controller, the prompts change to its buttons; Y moors.

**Co-op (if you play together)**
- [ ] Walt welcomes your friend by name. His call rings for both of you. The Day 4 letter reaches both of you.

## Known
- **A pull on the cord** is heard and felt (sound, toasts, a lean back) but hard to see from the tiller camera.
- **The opening** starts with you standing by the cot, not lying in it: held cameras in the small office kept ending up inside a wall.
- **Not built:**
  - Mari's string lights and music.
  - Gulls over the fish.
  - Hugo & Fifi's houseboat model.
  - The Pelican as an everyday buyer.
  - Full key rebinding, and the "legible font" option.
- **Steam:** nothing is published. The demo's Steam app and fee are Manan's decision.

## How to report
For each problem, write what you did, what happened and what you expected, with the game day and time (from the phone). A screenshot (Win+Shift+S) helps.
If the game breaks, copy `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Player.log` **before** starting it again.
