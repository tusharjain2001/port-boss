# Port Boss: Tushar's playtest changes (7 Nov): how to test

Build: `Builds\m1\PortBoss.exe`, branch `playtest-7nov` (on top of INTEGRATED-3, `main` @ `ef61963`).
Start a **New game** for each section unless it says otherwise. Takes about 30 minutes.

## Tester keys (in co-op, only the host can use them)
| Key | What it does |
|---|---|
| **F10** | +1 hour |
| **Shift+F10** | +6 hours |
| **Ctrl+F10** | the next morning, 7:00 AM (the next day) |
| **Ctrl+F7** | +$100,000 in the bank (the story act stays the same) |
| **F7** | the next story act and +$5,000. Press it 3 times for Act 4 (everything unlocked); a 4th press wraps back to Act 1 |
| F11 | the next weather (held) |

## What changed
1. **Walt's Landing is a port.** A timber quay runs along the shore on both sides of Walt's pier. Behind it is a fenced, packed-earth port yard with the office and warehouse, two pegged-out building lots and crates. The yard has three ways out:
   - the north opening onto the lane up to June's;
   - the south opening onto the waterfront path to Doyle's;
   - two inland gates (the town gate and the Market gate).
2. **The office is walk-in, and Walt's cot is inside.**
   - The roof fades away while you're indoors, and the camera tilts to a top-down view.
   - Tap E at the cot for the wardrobe, hold E to sleep.
3. **The town opens step by step.**
   - On Day 1, ropes with bunting keep you to two lanes: up to **June's** (Hook, Line & Sinker) and along the new cobbled **Market Lane** to **Pratt's**. Every other way out has a striped barrier with a "Closed for now" board.
   - The ropes come down when **Doyle's opens**: when you have $90 in the bank for the engine rebuild (or have made $50), or on Day 2.
   - **Plank & Post** opens on Day 2.
   - **The pub, Hartley's and the post office** open on Day 5, or in Act 2 if that comes first. Until then they have a "Closed for now" board at the door, and their counter says when they open.
4. **Shop signs.**
   - Every shop has a big board over its door (name and icon in the shop's colour) and a hanging sign beside it.
   - At the head of the pier, a **To the town** board lists every shop with its icon, distance and an arrow pointing the way. Shops that aren't open yet read "closed".
5. **Phone map tabs are All / Shops / Crew** (no Fish tab). The fishing spots show on All and in the key.
6. **Stamina** for sprinting and swimming. A ring appears beside your character while it isn't full:
   - It's green, turns peach under 30%, and pulses red when you're out of breath.
   - A sprint (Shift) lasts 12 s, then you can only run until it's back to about a third. It refills in about 5 s.
   - In the water, fast swimming (Shift) lasts 25 s and plain swimming or treading water 90 s. Run out in the water and Dot pulls you out up the nearest ladder: "Fished you out, kid. Again?"

## Checklist
**The port** (New game)
- [ ] From the boat or the end of the pier, it reads as a port: a long quay on posts, the yard, the fence, the gate.
- [ ] Walk the quay from end to end on both sides of the pier. The fence keeps you in the yard except at its openings and gates.
- [ ] Both lots have stakes, string, little flags and a blank board. Nothing blocks the ways in.

**The office**
- [ ] Walk in through the door. The roof fades away and you see the whole room from above. Walk out and the roof comes back.
- [ ] Walt's cot is inside. Before 8 PM, tapping E opens the wardrobe. After 8 PM (F10 to skip hours), hold E to sleep, and you wake in the cot at 7:00.

**The town, step by step**
- [ ] Day 1: read the To the town board at the pier's head. June's and Pratt's show distances; the rest read "closed".
- [ ] Walk out the north opening up to June's, and out the Market gate along the Market Lane to Pratt's. You can't leave either lane: ropes and "Closed for now" barriers stop you.
- [ ] The south opening (to Doyle's) and the town gate have a barrier.
- [ ] Sell fish at Pratt's until the bank passes $90 (or press **F10** until Day 2). A note says the town is open, the ropes and barriers vanish, and the board updates.
- [ ] Day 1–4: Plank & Post (from Day 2), the pub, Hartley's and the post office have a "Closed for now" board at the door and say when they open. On Day 5 (Shift+F10 to skip ahead) they open.
- [ ] **F7** (the tester key: next act and +$5,000) opens everything at once.

**Signs**
- [ ] From the street, every shop is obvious: big board over the door, hanging sign beside it.

**Map and stamina**
- [ ] Phone (Tab), Map: tabs All, Shops, Crew. The key lists Fishing.
- [ ] Sprint along the quay: the ring empties in about 12 s, turns red, and you can only run. Stand still and it refills.
- [ ] Jump in and swim with Shift: about 25 s. Tread water until it runs out: Dot pulls you out up a ladder.

**Co-op (if you play together)**
- [ ] Both players see the same ropes. When the shared bank passes $90 (or on Day 2), the ropes come down on both screens at once.

## Known
- **Act 2 never arrives by itself yet.** Nothing in the game advances the story act (only F7). That's why the pub, Hartley's and the post office also open on **Day 5**. Hiring at Rosie's board still says "Act 2", so it needs F7 for now.
- **The town opens all at once.** After the ropes come down (Doyle's), you can walk the whole town. The later shops are shut at their own doors, not by more ropes.
- **Pratt's hall:** a rope across the foot of its steps keeps you to the counter on Day 1.
- **The board's words read a little light from far away** (the font's own shader). Up close they're clear.

## How to report
For each problem write: what you did, what happened, and what you expected, plus the game day and time from the phone. A screenshot (Win+Shift+S) helps.

If something broke, copy `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Player.log` **before** starting the game again.
