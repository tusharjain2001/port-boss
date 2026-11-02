# Crescent Bay (map B): the island blockout

The final map, built from code at true scale (1 unit = 1 m). This is a blockout to judge the shape and look before any Unity
work. Nothing here is imported into Unity yet.

| File | What |
|---|---|
| `island_shape.py` | The map in numbers (pure numpy): map-B's own control points copied 1:1 from `docs/plan/gdd/world/maps/make_maps.py` `map_b()`, sampled on the exact curves the SVG draws; the coastline distance field; land heights; seabed depths; the sailing-route measure. Run alone: `python art-src/world/island_shape.py` (writes the seabed + prints the measures below) |
| `build_island.py` | Blender, headless: `blender -b -P art-src/world/build_island.py -- [norender] [proof=<dir>]` (~40 s). Builds the terrain + islets, Old Saltmouth (95 houses + the 7 shops at their map pins), Walt's Landing (**M1's real pier + shack FBX, with Biscuit**), Dot's lighthouse, the wreck, the Voss Causeway over Walt's Cut, Voss Harbour as blocks with its breakwater and buoy line, orange marker buoys at P1–P3, rocks, trees and Ponyo cumulus. Renders 7 views. `.blend` + renders go to `art-src/world/out/` (gitignored); renders are copied to `Desktop/Port Boss/proof/map-b/` |
| `proof_sheet.py` | `python art-src/world/proof_sheet.py`: the top-down next to map-B.png, the top-down with map-B's exact coastline drawn over it, and **the check**: the seabed PNG decodes with Pillow and 7 spot heights are right (ocean, Voss quay, ridge, the Cut, the bay, the berth, Gullrock) |
| `seabed/seabed_2048.png` + `.json` | The seabed depth map (below). `seabed_2048_preview.png` = the same in the Ponyo sea bands, for eyes |

Look: Ponyo Coast palette only, flat per-face colours, M1's 2-tone cel ramp with the blue-violet shade, no textures except the
seabed depth that colours the sea.

## Coordinates

- World X = east, Y = north (Blender; Unity Z = north), Z up, metres.
- **World (0,0) is the island's bounding-box centre** (gdd world §8.5), which is map-B point (2840, 2550).
- So map (mx, my) → world (mx − 2840, 2550 − my).
- Everything playable is within ±2.85 km of the origin. At that range a 32-bit float steps in 0.24 mm, so no floating origin is needed.

## Real-world measures (from the heightfield, `island_shape.py`)

| | Blockout (map B as drawn) | gdd says |
|---|---|---|
| Main island | **1.87 km E–W × 2.31 km N–S**, 2.34 km² of land (2.76 km² with the islets and the Voss quay) | systems §4.2 "~2.4 × 1.3 km" (that was map A); world §8.4 "~3 km²" |
| The bay | mouth 1.65 km between the horn tips, 0.96 km from the mouth to Walt's pier; a calm bowl 8–14 m deep | "calm west-facing bay" |
| Saltback Ridge | crest 149 m (140 m + the hand-made relief), 280 m from the outer coast | 140 m |
| Walt's Cut | 465 m long, 24 m wide, silted to 0.5 m, the Voss Causeway (1.7 m road) across the middle | world §2.3 "~730 m" (map A) |
| Sea | map-B's sheet, 5 × 5 km; deep (150 m) toward the sheet edge | 5 × 5 km |

**Sailing times in Biscuit.** Each time is the shortest water path from the end of Walt's pier. The path keeps at least 1.2 m
of water under the keel (16-direction search on a 9.8 m grid, about ±3%). Times are given at the gdd's T1 speed of 6 m/s
(after the $90 engine rebuild) and at M1's real Biscuit, `Skiff.MaxSpeed` 4.5 m/s (the gdd's "damaged" speed).

| From Walt's pier to | gdd route | Water path | 6 m/s | 4.5 m/s (M1 today) | gdd at 6 m/s |
|---|---|---|---|---|---|
| Bay Shallows | 0.30 km | 0.36 km | 1.0 min | 1.3 min | 0.8 |
| Gullrock | 0.40 | 0.44 | 1.2 | 1.6 | 1.0 |
| Lantern Key | 1.70 | 1.51 | 4.2 | 5.6 | 4.6 |
| Wreck of the Margaret | 1.90 | 1.91 | 5.3 | 7.1 | 5.2 |
| Breaker Point surf | 2.70 | 2.20 | 6.1 | 8.1 | 7.5 |
| Sorrel Isle | 4.20 | 3.65 | 10.1 | 13.5 | 11.7 |
| Mango Cay | 4.30 | 3.79 | 10.5 | 14.0 | 12.0 |
| **Voss Harbour, round the south horn** | 4.50 | **3.73** | **10.4** | 13.8 | **12.4** |
| Voss Harbour via Walt's Cut (dredged to 6 m) | 2.90 | 2.73 | 7.6 | 10.1 | 8.0 |

The gdd's routes were hand-drawn loops, so the real shortest paths are 0–20% shorter. The bay trips match. **Voss comes out
2 minutes closer than the gdd's 12.4 min at 6 m/s** (question 2 below). Dredging the Cut still saves 1 km (about 3 minutes). The shortest way to Voss really is round the south horn: walling off the south
makes it 4.71 km round the north (13.1 min at 6 m/s); walling off the north changes nothing.

## The seabed depth map (systems §4.3 "Seabed")

- **File:** `seabed/seabed_2048.png`, 2048 × 2048, 16-bit greyscale, linear, 2.44 m per pixel.
- **Covers exactly map-B's 5 × 5 km sheet:** world X −2840 … 2160, Y −2450 … 2550.
- **Orientation:** row 0 is the north edge, column 0 the west edge.
- **Value:** `height_m = value / 65535 × 320 − 160`. Steps are 4.9 mm. Sea level is 0, and `depth = −height`. Land is
  positive, so the same file is also the island heightmap.
- **Variants:** Walt's Cut is in its silted state (0.5 m, causeway in). `heightfield(cut_open=True)` gives the dredged
  variant for the endgame.
- **Regenerating:** both scripts regenerate it. The JSON beside it repeats all of this.

**How the game will read it** (M3/M4, not built tonight):

1. **Import:** sRGB off, no compression (R16), clamp, bilinear, no mipmaps for the CPU copy.
2. **World → UV:** `u = (x + 2840) / 5000`, `v = (z + 2450) / 5000`, where z is Unity north. Unity's row 0 is the bottom
   (south), which matches this v.
3. **GPU (water shader):** sample the same UV for the depth bands: foam under 0.4 m, then 0.6 / 2 / 5 / 12 / 30 / 70 m,
   shallow #46D6C8 → deep #2160B8, as in the renders.
4. **CPU:** read one `NativeArray<ushort>` (`GetPixelData`) for anchoring depth, the fishing 0.5 m rule and the AI sea grid
   (downsample it the way `route_lengths()` does).
5. **Not made:** systems' extra 1024² detail tile. At 2.44 m per pixel the main map already has that resolution. Add the
   tile if the shore foam needs to be sharper.

## Ambiguous points: my pick, for Manan

The picture won wherever the picture said something (rule 42). Where it said nothing, I picked:

1. **Island size.** Map B as drawn is 1.87 × 2.31 km, taller than wide. The systems doc's "2.4 × 1.3 km" was map A's, and
   world's "~3 km²" is a little high (2.34 km² main, 2.76 km² in all). *Pick: the picture.* The systems row needs updating.
2. **Voss distance.** It is 10.4 min at 6 m/s by the real shortest path (13.8 min at M1's 4.5 m/s), against the gdd's
   12.4 min, which came from a wide drawn loop. *Pick: keep Voss where the map puts it.* It still feels far (behind the
   ridge, more than 10 minutes away). If you want exactly 12.4 min, move it ~0.4 km further east/north-east.
3. **Hale's / Walt's Landing.** The map draws a 130 m orange pier; M1's real pier is 32 m. *Pick: M1's pier and shack*
   (imported as they are), at the map's spot on the town shore, pointing west into the bay.
4. **The town blob.** The map paints Old Saltmouth's tan blob out over the water past the coastline. *Pick: it is land*, a
   small promontory, and shops 1, 2 and 4 stand on it as drawn.
5. **Beaches vs rocks** (the map doesn't say). *Pick:*
   - **sand:** Breaker Point (the south horn's outer coast), the bay's inner shores of both horns, two coves beside the
     town, Lantern Key's east end, Mango Cay;
   - **rock cliffs:** everything else (the outer coast under the ridge, the horn tips, Gullrock, Sorrel).
6. **Heights** (only the ridge's 140 m is given). *Pick:*
   - north horn 95 → 24 m at the lighthouse;
   - south horn 85 → 15 m, with a 9 m saddle where the Cut crosses;
   - town hillside up to ~20 m;
   - islets: Lantern Key 30 m, Sorrel 18 m, Gullrock 9 m, Mango Cay 6 m.
7. **Depths** (none are given). *Pick:*
   - the bay bowl 8–14 m; the Bay Shallows bank 1.6 m;
   - Gullrock's reef flat ~1 m (the Widowmaker break); the north-horn point reef 2.2 m;
   - the berth at Walt's pier ≥ 2.6 m; the Voss basin 14 m;
   - the open sea down to 150 m;
   - every coast gets a ~10 m shelf so the foam line shows.
8. **Walt's Cut.** A 24 m channel silted to 0.5 m, with the low Voss Causeway (the road that silted it, world §0.1) across
   the middle. It is 465 m long, against "~730 m", which was map A's.
9. **The Wreck of the Margaret.** The map has only a pin. *Pick:* on a 5 m shoal, with her bow, wheelhouse and leaning mast
   above water, so she is a landmark and a photo spot.
10. **Swell Surf Co.'s pin** sits on the Cut's bank, so it was moved 45 m west onto dry ground.
11. **★ Scale feel (please look at `hero_bay_entrance.png`).** At true scale, from the bay mouth the 140 m ridge rises only
    5–6° over the horizon, so the island reads low and long. Inside the bay (the town, the landing, the deck view) it is a
    proper green bowl. *Pick: keep 140 m.* The alternative: raise the ridge to ~220 m and the horns ×1.5 for a grander Ponyo
    silhouette. It's one number each in `island_shape.py`.
12. **Unity axes, later.** M1's FBX settings put Blender −Y at Unity +Z, which would turn the island 180°. At import, export
    the island with `axis_forward='Z'` (or give the root a 180° yaw), so that north = Unity +Z, as the seabed JSON assumes.
13. **Voss Harbour** is grey blocks only: 6 red gantry cranes, 2 freighters, container stacks, sheds and the VOSS tower. The
    real harbour comes at M9. Any render made with the Toon Harbor pack must stay out of git (rule 1).
14. **P1–P3** are marked with orange buoys only. Nothing is built there.
