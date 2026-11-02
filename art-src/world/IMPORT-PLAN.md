# Crescent Bay into Unity: the import plan (plan only, nothing built)

How the Blender blockout (`art-src/world/`) becomes the game world later. It follows the gdd: systems §4.2 (streaming),
§4.3 (water and seabed), §4.7 / §4.8.4 (budgets) and §4.8.6 ("Blender is the level editor"), and world §8.5. Nothing here
runs until Manan approves the map and schedules it. Each step below is its own branch, test and look check (HANDOVER §3
rules 9, 15, 19).

## 0. What exists now and what changes

| Today (M1/M2) | After the import |
|---|---|
| `PB_Landing_Walts.fbx`: pier + shack + **its own little islet (`Land`, `Ground_col`)**, at the world origin | Pier + shack only (`Land` and `Ground_col` dropped), standing on the island's terrain at the map's spot |
| `Game.cs` builds code-made horizon islands and a sandy seabed that slopes away from the shore | Deleted: the real island and the seabed texture replace both |
| `PB_Water` colours by screen-space depth (`SampleSceneDepth`, `_DepthDist`) | Colour bands from the seabed texture; scene depth kept only for contact foam around boats, piles and props |
| `Sea.cs`: one camera-following mesh, Gerstner waves on GPU + CPU | Unchanged (one water mesh, never per cell) |

## 1. Coordinates and axes

- World (0,0) is the island's bounding-box centre (map-B point 2840, 2550). Unity X = east, Z = north, Y = up, metres.
  Everything is within ±2.85 km, so there is no floating origin (world §8.5).
- **North must land on Unity +Z.** M1's FBX settings put Blender −Y at Unity +Z (Biscuit's bow), which would turn the
  island 180°. The island exporter picks its axes so Blender +Y lands on Unity +Z.
  - The first import test checks it: an empty `socket_north` 500 m north of the origin must import at Unity (0, 0, +500).
- Walt's Landing moves from the origin to its map spot.
  - Its root: Blender (`PIER_ROOT.x` − 10, 0), turned −90° about Z (the pier points west into the bay).
  - `Landing.I` and its sockets (`socket_moor`, `socket_spawn`, `socket_pier_ladder_*`) carry over unchanged.
  - Every bot test that reads `Landing.I` keeps working. Before the move, grep `Bot.cs` / `AutoRun.cs` for hard-coded world
    positions.

## 2. Chunks (512 m cells, world §8.5 / systems §4.2)

- The grid is aligned to the world origin: cell (i, j) covers X `[512 i, 512 (i+1))`, Z the same.
  - The island (X −1150 … 1700, Z −1300 … 1300) spans cells i = −3 … 3, j = −3 … 2: 42 cells, about 25 of them with land.
  - The islets are 4 more cells.
  - Open-water cells have no scene at all.
- **Export:** `build_island.py -- export` writes, per cell, `Assets/Game/Art/World/PB_Island_c<i>_<j>.fbx` plus
  `PB_Island_c<i>_<j>.json` (the placement list: prefab id, position, rotation, scale, seed: systems §4.8.6). It is one
  script, so it always rebuilds the same thing.
- **Unity side:** an editor importer (`WorldBuild.cs`, generated like `M1Build.cs`, rule 19) builds one additive scene per
  cell from its FBX + JSON. There are no hand-edited scenes.
- **Always loaded:** the core scene (sea, sky, light, clock, managers), the Hale's Landing cell, and the Voss Harbour
  skyline impostor (~20k tris).
- **Streaming:** load a radius of 2 cells round any player and unload with a 1-cell hysteresis. A T4 boat at 16 m/s crosses
  a cell in 32 s; the budget is a cell async-loaded in under 2 s on the minimum spec.
- **Networked things** (boats, NPCs, dropped fish, the warehouse) never live in cell scenes (systems §4.2).

## 3. LODs

| Piece | LOD0 | LOD1 | LOD2 | How |
|---|---|---|---|---|
| Terrain per cell | 6 m grid (town and landing cells: 3 m) | 12 m | 24 m | Re-sampled from the same heightfield (`island_shape.py`), not decimated, so seams match. Edge vertices are shared, and skirts 2 m down hide cracks |
| Houses (the town) | as built (box + roof + windows, ~60 tris) | roof + box | merged into a town impostor block at > 600 m | Mesh names `_LOD0/1/2` → Unity makes the LODGroup on import |
| Landmarks (8 shops, lighthouse, wreck) | as built | 50% Decimate (systems §4.8.4) | silhouette card | Always visible from the bay: no culling before 1.5 km |
| Trees, rocks | blob (80 tris) | 20 tris | culled | GPU instanced from the placement JSON, no GameObject each |
| Far islets, Voss skyline | | | baked impostor | Lantern Key, Sorrel and Mango Cay beyond 1 km; Voss always (its HLOD, §4.2) |

- Cross-fade on every LODGroup.
- Fog at 1.8 km on clear days and 300 m on fog days hides the far edge (world §8.5).

## 4. Colliders (systems §4.8.2 naming)

- **Terrain:** one `MeshCollider` per cell from LOD1 (12 m). Exceptions:
  - the town and landing cells get a 2 m walk collider (`_colGround`) over the walkable streets, the yard and the
    beaches;
  - the shoreline everywhere gets a 3 m strip, so a boat bumps the real coast and not a 12 m approximation.
- **Houses and landmarks:** `_colBox` per building (walls only, the roof can be skipped). Walt's pier and shack keep M1's
  colliders.
- **Coastal rocks and reef rocks:** convex `_col` (boats bump them). Inland rocks and trees get no collider, except tree
  trunks in the town and along walkable paths (capsules).
- **Walt's Cut causeway:** a `_colBox`. While it stands, the Cut is closed to boats, as the seabed says (0.5 m silt + the
  road).
- **Voss:** the breakwater and quay edge get `_col`, so the no-dock rule (world §8.6) is physical.
- **The sea floor needs no collider.** Depth comes from the seabed texture (below).

## 5. The seabed texture into `Sea` / `PB_Water`

`art-src/world/seabed/seabed_2048.png` (16-bit, `h = v/65535 × 320 − 160` m, map-B's 5 × 5 km sheet; `seabed_2048.json`
has the numbers).

1. **Import:** linear (sRGB off), no compression (R16), clamp, bilinear, no mipmaps. Keep a readable CPU copy.
2. **Shader (`PB_Water`):** add `_Seabed` and `_SeabedRect` = (−2840, −2450, 5000, 5000).
   - World → UV: `u = (x + 2840) / 5000`, `v = (z + 2450) / 5000`.
   - `depth = Sea.Level − h`.
   - Colour bands by that depth: foam under 0.4 m, then 0.6 / 2 / 5 / 12 / 30 / 70 m, shallow #46D6C8 → deep #2160B8.
     These are the bands in the renders; `_Bands` already exists.
   - Shore foam comes from the same depth.
   - Scene depth stays for the contact foam round piles, hulls and props.
3. **CPU (`Seabed.cs`, new, static):** `Height(x, z)` and `Depth(x, z)` read a `NativeArray<ushort>` (`GetPixelData`) with
   the same bilinear math. Uses:
   - the anchor (holds where the depth is under the rope length);
   - the fishing 0.5 m rule;
   - the AI sea grid: a downsampled walkable-water mask, as `island_shape.route_lengths()` does;
   - the Shelf Current edge;
   - the phone Map (M4).
4. **The dredged Cut** (endgame): a second texture, `seabed_2048_cut_open.png`, from `heightfield(cut_open=True)`. The game
   swaps textures and removes the causeway's collider in one host event, and the dredging story beat plays over it.
5. **Check:** a `-pb-seabedtest` switch samples the 7 spot heights that `proof_sheet.py` asserts (ocean, Voss quay, ridge,
   the Cut, the bay, the berth, Gullrock) and must match within 5 cm.

## 6. Performance budget (systems §4.7, §4.8.4; GTX 1060 class, 1080p, 60 fps)

| Item | Budget | Blockout today | Note |
|---|---|---|---|
| Whole island visible at once (one view) | ≤ 600k tris with LODs | **349k for the whole map at LOD0** (measured in `crescent_bay.blend`, sea and clouds excluded) | Already under budget; LOD1/2 beyond 300 m bring a view to ~150k |
| Terrain | | 154k (island) + 30k (the 4 islets) | 6 m grid; per cell ~4–8k at LOD0 |
| Town (~235 houses + 8 landmarks, lamps, dinghies) | 2–6k per building (§4.8.4) | 25k in all | Well under; room for M5's details. M1's pier + shack: 19k |
| Trees | instanced | 75k (1,500 trees) | One draw call per mesh variant |
| Rocks + foam stand-ins | | 12k (613 rocks); 22k foam | The foam is a render stand-in: in the game it is the water shader's job (§5) |
| Ocean | ≤ 2.5 ms GPU | unchanged | |
| Cell load | < 2 s async | – | Measured by `-pb-islandtest` (below) |

- Per object (LOD0): Island 153,742; Trees 75,140; OldSaltmouth 24,728; Foam 21,568; Pier 13,108; Rocks 12,260; the islets
  30k; Biscuit 7.7k; Shack 5.9k; Voss blocks 3.4k; lighthouse 636; Mari's boat 228; wreck 116.
- The export step will print the triangles per cell and LOD.
- **Test switch:** `-pb-islandtest`.
  - A bot boat sails the README's routes (the bay, Lantern Key, round to Voss) at T1 speed and logs cell loads, frame
    times and the sailing time.
  - It passes if Voss round the south horn takes 10.4 min ± 3% at 6 m/s as built (12.4 min with the stretched-horn
    variant, if Manan picks it), with 0 exceptions and no frame over 33 ms.

## 7. Order of work (each one a reviewed branch)

1. **Exporter + the Hale's Landing cell only.**
   - Do: export the cell, import it, move `Landing` onto it, drop M1's islet and code-made seabed.
   - Test: every existing M1/M2 test (boat, swim, ladder, feel, knock, fish, coop) passes on the new ground.
   - Look: a film of the landing at 30 fps (rule 15).
2. **Seabed into `PB_Water` + `Seabed.cs`.**
   - Test: `-pb-seabedtest`.
   - Look: the bay's depth bands compared with `hero_town.png`.
3. **All cells + streaming.**
   - Test: `-pb-islandtest`, cell load times.
4. **Colliders round the coast, the reef and the causeway.**
   - Test: a bot drives at Gullrock, the reef and the causeway and must bump, not pass through.
5. **LOD, impostors and perf.**
   - Test: the perf flythrough (`-pb-perf`) on the minimum-spec target.
6. **Look pass in the real Ponyo shaders.**
   - Test: the capture video watched, against these renders.

## 8. Open before the import (Manan's picks, from the README and the 3 Nov sheets)

- Ridge 140 m (default) or 220 m: `proof/map-b/ridge-140-vs-220.png`.
- Voss as drawn (10.4 min at 6 m/s) or the stretched south horn (12.4 min, the gdd's number):
  `proof/map-b/voss-distance-variant.png`.
- Where Hartley's stands (map B has no pin for it), and whether Mari's boat shows from Day 1 or only from Day 3, as the
  story says.
