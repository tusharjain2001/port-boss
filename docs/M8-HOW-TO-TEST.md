# Port Boss M8 (surfing and the fishing gear): how to test

Build: `Builds\m1\PortBoss.exe`, from `main` (M8 was merged on 8 Nov, before this test, at Tushar's call).
The plan, and the choices made in it: `docs/plan/M8-PLAN.md`. Allow about 60 minutes. Start with a **New game**.
**The gear guns aren't in.** They wait for Manan's answer on Q1: guns as tools that never hurt anyone, and their prices.

## Tester keys (the host's in co-op)
| Key | What it does |
|---|---|
| **F10** / **Shift+F10** | +1 hour / +6 hours |
| **Ctrl+F10** | the next morning, 7:00 AM |
| **Ctrl+F7** | +$100,000 |
| **F7** | the next story act, and +$5,000 (**press once for Act 2: the surf shop opens**) |
| F11 | the next weather (held) |
| **Ctrl+F11** (new) | **surf's up today**: Breaker Point 1.5 m, the Widowmaker 3.2 m |

**The quickest way to surfing:** F7 once (Act 2), Ctrl+F7 (money), Ctrl+F10 (the next morning: Kai texts at 6:30), Ctrl+F11 (surf).

## What's new
1. **Tackle** at June's (Hook, Line & Sinker, Tackle tab):
   - **Reels** give a longer strike window.
   - **Lines** decide how heavy a fish your line holds before it's "HEAVY".
   - **Hooks:** the Circle gives a longer strike window; the Barbed gives you a second go at a missed stop, and the Treble a third.
   - The **Sight float** is bigger and glows.
   - Reels and lines only get better, so your best one is always on. Choose your hook with **Use**.
   - The rod's name card under the hotbar lists what's on it.
2. **Rod holders and trolling.**
   - Doyle's "rod holders" upgrade puts a steel holder on Biscuit; the Kestrel comes with three.
   - Cast, then **E at the holder**: the rod goes in, and your other rod comes to your hands. (You need two rods: the bamboo counts.)
   - A **bell** means a bite: you have 2 seconds to E it. From the tiller, one E leaves the tiller and takes the rod.
   - **Trolling:** with sardine or squid on the holder rod, drive at ⅔ throttle (2–5 m/s) and the line keeps fishing. Other baits come in and the rod rests.
3. **The landing net** (co-op, $150 at June's): beside a crewmate who's fighting a fish, **E** readies it. They get one more go and a wider green, and if it's Rare you scoop it.
4. **Swell Surf Co.** by the Cut (south of the bay, by boat):
   - **Getting there:** moor at **Kai's jetty** with **F**, then walk to his counter.
   - **Kai's shelf** (from Act 2, 9 AM to 6 PM, shut in storms): **his first foam board is free**, plus the Bodyboard, Longboard, Shortboard (after his challenge 1) and the Wetsuit (+50% stamina in the water).
   - Kai texts a surf report at dawn on surf days, and the Forecast app has a surf line.
5. **The surf.** Waves roll in and break at **Breaker Point**, the long beach on the south horn, in sets. They peel along, the white water runs to the sand, and there's spray. **The Widowmaker** breaks on Gullrock's reef, in the bay, the day after a storm.
6. **Surfing.**
   - **Paddling:** in the water with a board, **E**, then **W** to paddle. **Ctrl** (pad B) ducks you under the white water.
   - **Catching:** paddle in on a wave's green face as it stands up. The **pop-up bar** appears: **Space** in the green and you're up.
   - **Riding:** **A/D** carve along the face. The **pocket**, glowing by the curl, is faster.
   - **Wipeouts:** too far round, or the white water catches you. The leash brings the board back.
   - The bodyboard is ridden lying down and catches anything.
7. **Scores and Kai's challenges.**
   - Every ride scores; your best is in the Journal's Surf card.
   - **Kai's 3 challenges:** stand up for 3 s, ride 8 s, 3 s in the pocket. The rewards are the shortboard, his sunburst on your boards, and being "a local".
   - **In co-op:** two of you on one wave is a **party wave** (more points each). Too close is a **drop-in**: both of you go down.

## Checklist
**Tackle and holders**
- [ ] June's Tackle tab: buy a reel, a line and a hook, then **Use** another hook. The name card under the hotbar changes.
- [ ] The Barbed hook: miss the stop, then hit the second one: it's still Rare.
- [ ] Doyle's rod holders on Biscuit (Ctrl+F7 for money): cast, **E** at the holder, and the bell rings within about 10 s. **E** in time: you fight it.
- [ ] Miss a bell: "it swam off", and it re-baits by itself.
- [ ] Troll: sardine on the holder rod, drive at ⅔. The line trails behind; its bell, then one **E** from the tiller.

**Swell Surf Co.** (F7 once, Ctrl+F10)
- [ ] Kai's texts at 6:30 (four, the last listing his challenges).
- [ ] Sail to the Cut, moor at Kai's jetty (**F**), and walk to his counter: take the free foamie, and buy the wetsuit.
- [ ] The Forecast app's surf line.

**Surfing** (Ctrl+F11, then sail to Breaker Point and anchor outside the waves)
- [ ] Jump in, **E** on the board, paddle out. **Ctrl** ducks under the white water.
- [ ] Waves come in sets and break, peeling along, with the white water running to the sand. Do they look good from the water and from the boat?
- [ ] Catch one: the pop-up bar, **Space** in the green, up. Carve along it, find the glowing pocket, and ride it in. Note the score.
- [ ] Turn too far round: a wipeout, then the board comes back on its leash.
- [ ] The bodyboard (buy it, **Carry** it at Kai's): ridden lying down.
- [ ] Kai texts as you finish each challenge; the Journal's Surf card ticks them off.

**Co-op (if you play together)**
- [ ] Your friend sees you paddling and standing on your board.
- [ ] Both on one wave: "Party wave!". Too close: both of you go down (a drop-in).
- [ ] The landing net on a friend's fight; their holder rod shows in your game.

## Known
- **The surf shop building** (its leaning boards and the blue board on the roof) is map B's blockout art from before M8; only Kai's counter and jetty are new.
- **The board isn't a hotbar item.** It comes out when you get on it in the water, and your rod stays on your back.
- **Surfing earns no money** (the plan's rule); Kai pays in respect.
- **Not in M8:** the gear guns, the Voss Wall wave (M9, with the rival), the surf contest, the SUP, the jet ski.
- **companycheck:** Windows Smart App Control started blocking the money-check program on this PC (8 Nov). The same checks run inside the editor and pass.

## How to report
For each problem, write what you did, what happened and what you expected, with the game's day and time (from the phone). A screenshot (Win+Shift+S) helps.
If the game breaks, copy `%USERPROFILE%\AppData\LocalLow\Port Boss\Port Boss\Player.log` **before** you start it again.
