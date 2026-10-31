# M1 F1–F6 checklists (gdd fishing §8), self-tested by w-pb-m1, 2026-10-31

How checked: headless bot runs of the release build (`tools/m1test.sh`), logs in `test-logs/`, screenshots + `m1-capture.mp4` here, all looked at. "(Manan)" = needs hands/eyes on a real keyboard/pad; not provable headless.

## F0 Boat
- [x] Biscuit floats on the same waves you see (CPU Gerstner = shader); moored roll 2.9°, pitch 2.0°
- [x] Drive: cast off, 38.5 m in 12 s at full (4.5 m/s), max roll 5.5° (Clear 6° + turn lean)
- [x] Leaves the berth sliding along the piles; bumps stop it on head-on hits (push-out collision)
- [x] Anchor at the bow holds: 1.26 / 1.30 m drift in 30 s (limit 6 m)
- [x] Jump overboard (0.63 s), swim to the stern ladder, E climbs back aboard (swim test PASS)
- [ ] Driving feel on keyboard and pad (Manan)

## F1 Rod in hand
- [x] 1 draws the rod in 0.35 s, hands on cork + reel (two-hand IK), rod on the back strap when stowed
- [x] Tip lags the hands (spring) and sways with the boat's roll
- [x] Sprint (on land) stows the rod; scroll/1–8 switch slots (pulse + name pill)
- [ ] "Looks held, not glued" at full speed (Manan)

## F2 Cast + bobber
- [x] Charge 1.1 s ping-pong, distance 6–16 m on Walt's rod (log: power 0.98 → 15.7 m)
- [x] Ring marker on the water, gold in the sweet spot (92–100%), red when snapped to valid water
- [x] Aim at the boat/land: snaps to the nearest water ≥ 0.5 m deep, never a dead cast
- [x] Bobber visible at 16 m (scales to stay readable + soft red halo)
- [x] Line never through Biscuit (points lifted over the gunwale); clean arc while paying out
- [x] RMB hold 0.3 s reels in, bait kept

## F3 Wait, nibbles, bite, strike
- [x] Bites 4–10 s: 1x run 60/60 seen on screen 4.105–9.938 s; 500-cast timer 4.006–9.978 s
- [x] Nibbles (0–4 by class) = small dip + soft tik + light rumble; bite = yanked under 25 cm, BLOOP + ratchet + big splash + punch-in + strong rumble
- [x] Strike on a nibble: "Too soon!" (LEFT alert), bait kept; too late: "It swam off.", bait kept
- [x] Strike window 1.2 s
- [ ] "Tell nibble from bite with eyes closed / pad only" (Manan)

## F4 ★ Minigame
- [x] Stop 1 green in the middle; stops 2–3 at 50% ± 8–18%; speed ×1.00/1.08/1.16
- [x] Ding rises C → E → G; 3 hits = fanfare; 0 hits lands Common
- [x] A miss: grey pip, soft plop, no shake, no stumble, one grade lower
- [x] Heavy conger (rod 2, 15 kg line): "Something HEAVY!" card first; let go is calm (auto after 4 s); fight snaps only on the 2nd miss (7/7 snaps were 2nd misses; 154 normal misses, 0 snaps)
- [x] Pointer is analytic (no stutter at any fps); judged at the input's timestamp + timing offset
- [x] Grade mix at σ 70 ms over 500 fish: 0/7/37/56 % vs model 0/7/34/59 %
- [x] F3 overlay shows bite timer, window ms, per-stop error

## F5 Catch, reveal, hotbar
- [x] Species rolled from the final 15-fish sheet (data); sizes/weights vary (sprat 9–10 cm, mackerel 25–42 cm…)
- [x] Reel 1–3.5 s by weight, Scarce+ leap; hold-up in both hands; reveal card per mockup 06; skippable after 0.5 s, auto-keeps at 6 s
- [x] Fly-in to the slot; stacks of 5 by species + grade; 6th starts a new stack (hotbar self-test PASS)
- [x] Full hotbar: E drops it on the deck (pickup 120 s), Q lets it go
- [x] "NEW species" on the LEFT the first time

## F6 ★ Feel pass
- [x] Ponyo Coast golden hour: toon + outline, warm 3-stop sky with clouds, luminous sea, islands
- [x] Paper UI per mockups (Fraunces + Zen Maru Gothic), alerts LEFT, no clock
- [x] Sounds for every step (synthesised + CC0), rumble on pad
- [x] Fisherman outfit (beanie, bib overalls) on our worker
- [ ] 15 minutes solo: does it feel good to repeat? KB/M and pad both complete? (Manan)
