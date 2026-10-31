# Port Boss M1: boat + fishing (how to test)

Double-click `PortBoss.exe`. You start on Walt's pier at golden hour; Biscuit is moored at the end.

## Controls (keyboard + mouse; a pad works too)
| Do | Keys | Pad |
|---|---|---|
| Walk / look | WASD / mouse (Ctrl = walk slowly, Shift = sprint on land) | left / right stick |
| Jump | Space | A |
| Hotbar | 1–8 or the wheel (slot 1 = Walt's rod) | LB / RB |
| Take / leave the tiller | E at the motor | X |
| Throttle notches (reverse · stop · 1/3 · 2/3 · full) | W / S at the tiller (W casts off) | RT / LT |
| Steer | A / D | left stick |
| Moor at the pier end | F (shown when close and slow) | |
| Drop / raise the anchor | E at the bow | X |
| Cast | hold LMB, release (the ring on the water shows where; gold = sweet spot) | hold RT |
| Strike on the bite | LMB or Space | RT or A |
| Stop the bar (3 stops) | Space or LMB | A or RT |
| Reel in early | hold RMB | hold B |
| Keep / let go (reveal) | E / Q | A / X |
| Climb aboard from the water | E at the stern ladder | X |
| Pause / quit | Esc, then Q | Menu |
| Debug | F3 overlay (bite timer, window ms, stop errors) · F5 next rod tier (1–5) · F6 next bait | |

## Manan's checklist (gdd fishing §8, F1–F6)
- [ ] Take Biscuit out past the pier (A to steer away from the pier first), drive, come back and moor (F).
- [ ] Walk the deck while she rocks: no sliding, no jitter, calm camera.
- [ ] Drop the anchor and fish 15 minutes. Do you want to keep going?
- [ ] 1 draws the rod in about a third of a second; walking, the tip lags and sways with the boat.
- [ ] Casts: short power lands near (~6 m), full lands far (~16 m); the ring turns gold at the top.
- [ ] Aim at the boat or the land: the ring turns red, and the bobber still lands in the water.
- [ ] Count 10 bites: each comes 4–10 s after the bobber settles (F3 shows the timer).
- [ ] Nibbles are small taps (tik); the bite is a big BLOOP with the bobber yanked under.
- [ ] Strike early: "Too soon!" on the LEFT, bait kept. Wait too long: "It swam off.", bait kept.
- [ ] The bar: stop 1's green is in the middle; 3 hits = Prized, 0 hits still lands a Common.
- [ ] Miss on purpose: nothing shakes, you don't stumble, the fish lands one grade lower.
- [ ] F5 to rod 2 and fish with F6 = Sardine: a big conger asks "Something HEAVY!" (Let it go is calm; Fight it snaps only on the 2nd miss).
- [ ] The 6th fish of one species and grade starts a new stack; a full hotbar offers drop-on-deck / let go.
- [ ] "NEW species" shows on the left the first time.
- [ ] Jump off the boat and climb back up the stern ladder (E).
- [ ] Anything that felt slow or ugly: every timing is a number we can tune.

Not in M1 (by design): shops, phone, money, clock, time of day, weather, co-op, rot.
