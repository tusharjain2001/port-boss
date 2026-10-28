# Stack wobble tuning

Model (Player.UpdateSway): lean'' = -accel_local * inertia(n) + walk_bob - k(n) * lean - 3.2 * lean'.
inertia(n) = 0.1 * (n-1)^1.2, k(n) = 16 - 2.9 (n-1) - 3.5 (sprinting). Lean 1.0 = topple, 0.55 = "Steady...!" + creak.

Peak lean from `python docs/wobble_sim.py` (start / stop / reverse direction):
| crates | walk | sprint |
|---|---|---|
| 2 | 0.07 / 0.08 / 0.13 | 0.12 / 0.13 / 0.23 |
| 3 | 0.15 / 0.17 / 0.30 | 0.27 / 0.26 / 0.51 |
| 4 | 0.24 / 0.24 / 0.46 | 0.45 / 0.41 / 0.76 |
| 5 | 0.34 / 0.31 / 0.59 | 0.69 / 0.66 / 0.86 |
So 4–5 crates at a sprint live in the teeter zone; a sharp turn on top of that tips it, easing off saves it.
