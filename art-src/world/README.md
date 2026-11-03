# Crescent Bay (map B): the island blockout

The final map, built from code at true scale (1 unit = 1 m). This is a blockout to judge the shape and look before any Unity
work. Nothing here is imported into Unity yet.

| File | What |
|---|---|
| `island_shape.py` | The map in numbers (pure numpy): map-B's own control points copied 1:1 from `docs/plan/gdd/world/maps/make_maps.py` `map_b()`, sampled on the exact curves the SVG draws; the coastline distance field; land heights; seabed depths; the sailing-route measure. Run alone: `python art-src/world/island_shape.py` (writes the seabed + prints the measures below) |
| `build_island.py` | Blender, headless: `blender -b -P art-src/world/build_island.py -- [norender] [proof=<dir>]` (~40 s). Builds the terrain + islets, Old Saltmouth (~235 houses + 8 landmark shops), Walt's Landing (**M1's real pier + shack FBX, with Biscuit**), Dot's lighthouse, the wreck, the Voss Causeway over Walt's Cut, Voss Harbour as blocks with its breakwater and buoy line, orange marker buoys at P1–P3, rocks, trees and Ponyo cumulus. Renders 11 views. `.blend` + renders go to `art-src/world/out/` (gitignored); renders are copied to `Desktop/Port Boss/proof/map-b/` |
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

| | Blockout (Manan's pick, 3 Nov: ridge 220 m, south horn +400 m) | gdd says |
|---|---|---|
| Main island | **2.06 km E–W × 2.32 km N–S**, 2.46 km² of land (2.90 km² with the islets and the Voss quay). Map B as drawn: 1.87 × 2.31 km, 2.34 km² | systems §4.2 "~2.4 × 1.3 km" (that was map A); world §8.4 "~3 km²" |
| The bay | mouth 1.65 km between the horn tips, 0.96 km from the mouth to Walt's pier; a calm bowl 8–14 m deep | "calm west-facing bay" |
| Saltback Ridge | crest 229 m (220 m + the hand-made relief), 280 m from the outer coast | 140 m (Manan picked 220) |
| Walt's Cut | 465 m long, 24 m wide, silted to 0.5 m, the Voss Causeway (1.7 m road) across the middle | world §2.3 "~730 m" (map A) |
| Sea | map-B's sheet, 5 × 5 km; deep (150 m) toward the sheet edge | 5 × 5 km |
| Triangles | 360k for the whole map at LOD0 (sea and clouds excluded; Island 163k, Trees 76k): `IMPORT-PLAN.md` §6 | ≤ 600k per view |

**Sailing times in Biscuit.** Each time is the shortest water path from the end of Walt's pier. The path keeps at least 1.2 m
of water under the keel (16-direction search on a 9.8 m grid, about ±3%). Times are given at the gdd's T1 speed of 6 m/s
(after the $90 engine rebuild) and at M1's real Biscuit, `Skiff.MaxSpeed` 4.5 m/s (the gdd's "damaged" speed).

| From Walt's pier to | gdd route | Water path | 6 m/s | 4.5 m/s (M1 today) | gdd at 6 m/s |
|---|---|---|---|---|---|
| Bay Shallows | 0.30 km | 0.36 km | 1.0 min | 1.3 min | 0.8 |
| Gullrock | 0.40 | 0.44 | 1.2 | 1.6 | 1.0 |
| Lantern Key | 1.70 | 1.51 | 4.2 | 5.6 | 4.6 |
| Wreck of the Margaret | 1.90 | 1.91 | 5.3 | 7.1 | 5.2 |
| Breaker Point surf | 2.70 | 2.91 | 8.1 | 10.8 | 7.5 |
| Sorrel Isle | 4.20 | 3.65 | 10.1 | 13.5 | 11.7 |
| Mango Cay | 4.30 | 4.53 | 12.6 | 16.8 | 12.0 |
| **Voss Harbour, round the south horn** | 4.50 | **4.48** | **12.4** | 16.6 | **12.4** |
| Voss Harbour via Walt's Cut (dredged to 6 m) | 2.90 | 2.73 | 7.6 | 10.1 | 8.0 |

**Voss is 12.4 min at 6 m/s (T1 Biscuit) and 8.3 min in the Kestrel (9 m/s), the gdd's numbers**, thanks to the stretched
south horn. Breaker Point and Mango Cay grew too (the way round the longer horn). Dredging the Cut saves 1.75 km (about
5 minutes). The shortest way to Voss is round the south horn; walling it off makes it 4.71 km round the north (13.1 min).
Map B as drawn (horn 0) gave Voss 3.73 km = 10.4 min, Breaker Point 2.20 km, Mango Cay 3.79 km.

## The seabed depth map (systems §4.3 "Seabed")

- **File:** `seabed/seabed_2048.png`, 2048 × 2048, 16-bit greyscale, linear, 2.44 m per pixel.
- **Covers exactly map-B's 5 × 5 km sheet:** world X −2840 … 2160, Y −2450 … 2550.
- **Orientation:** row 0 is the north edge, column 0 the west edge.
- **Value:** `height_m = value / 65535 × 400 − 160` (range −160 … +240 m, so the 229 m ridge isn't clipped). Steps are 6.1 mm. Sea level is 0, and `depth = −height`. Land is
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

1. **Island size.** Now 2.06 × 2.32 km (map B as drawn: 1.87 × 2.31). The systems doc's "2.4 × 1.3 km" was map A's, and
   world's "~3 km²" is close (2.46 km² main, 2.90 km² in all). The systems row needs updating.
2. **Voss distance.** *Decided by Manan (3 Nov): the south horn stretched 400 m*, so Voss is 12.4 min at 6 m/s as the gdd
   says (it was 10.4 min as drawn).
3. **Hale's / Walt's Landing.** The map draws a 130 m orange pier; M1's real pier is 32 m. *Pick: M1's pier and shack*
   (imported as they are), at the map's spot on the town shore, pointing west into the bay.
4. **The town blob.** The map paints Old Saltmouth's tan blob out over the water past the coastline. *Pick: it is land*, a
   small promontory, and shops 1, 2 and 4 stand on it as drawn.
5. **Beaches vs rocks** (the map doesn't say). *Pick:*
   - **sand:** Breaker Point (the south horn's outer coast), the bay's inner shores of both horns, two coves beside the
     town, Lantern Key's east end, Mango Cay;
   - **rock cliffs:** everything else (the outer coast under the ridge, the horn tips, Gullrock, Sorrel).
6. **Heights** (map B gives only the ridge: 140 m; Manan picked 220 m). *Pick:*
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
11. **Scale feel.** *Decided by Manan (3 Nov): ridge 220 m.* At 140 m the ridge rose only 5–6° over the horizon from the
    bay mouth; at 220 m the bay face is a green wall over the town. `RIDGE_H` in `island_shape.py`.
12. **Unity axes, later.** M1's FBX settings put Blender −Y at Unity +Z, which would turn the island 180°. At import, export
    the island with `axis_forward='Z'` (or give the root a 180° yaw), so that north = Unity +Z, as the seabed JSON assumes.
13. **Voss Harbour** is grey blocks only: 6 red gantry cranes, 2 freighters, container stacks, sheds and the VOSS tower. The
    real harbour comes at M9. Any render made with the Toon Harbor pack must stay out of git (rule 1).
14. **P1–P3** are marked with orange buoys only. Nothing is built there.

## Variants and the look pass (3 Nov, second pass)

**Two variants for Manan.** Both are knobs on the same scripts. **Manan picked both (3 Nov); they are now the defaults**
(`RIDGE_H, HORN = 220, 400` in `island_shape.py`). Map B as drawn: `-- tag=asdrawn ridge=140 horn=0`. The two sheets below
were made before the pick and stay as its record.

| Knob | Run | Result | Sheet |
|---|---|---|---|
| Saltback Ridge 220 m | `-- tag=ridge220 ridge=220` | crest 229 m; the bay face becomes a ~35–40° green wall over the town | `proof/map-b/ridge-140-vs-220.png` (bay mouth + Biscuit's deck heading home, 140 vs 220) |
| South horn stretched 400 m | `-- tag=voss12 horn=400` | Voss 4.48 km round the south horn = **12.4 min at 6 m/s (T1 Biscuit), 8.3 in the Kestrel (9 m/s)**, the gdd's exact numbers. The Cut stays 2.73 km, so dredging it now saves 1.75 km | `proof/map-b/voss-distance-variant.png` (both top-downs with the real shortest path drawn) |

- More horn doesn't buy more time: past ~600 m the shortest way flips to round the north (4.71 km), so 13.1 min at 6 m/s is
  the most this variant can give.
- **Kufu wrote "12.4 min in the Kestrel at 6 m/s".** 6 m/s is T1 Biscuit; the Kestrel (T2) is 9 m/s. I matched the gdd's
  T1 figure: 12.4 min at 6 m/s, which gives 8.3 min in the Kestrel, the gdd's T2 figure too.

**The look pass:**

- **Landmarks:** all eight are distinct silhouettes at their pins, named in `proof/map-b/town_labelled.png`.
  1. Hook, Line & Sinker: teal shop, a big yellow fish on the roof, an iron hook sign.
  2. Doyle Boatworks: red barrel-roofed boatshed, open doors, a slipway into the bay with a hull on it.
  3. Plank & Post: open timber shed, plank stacks, a log pile, a yellow derrick, the yard office.
  4. The Mooring Line: half-timbered pub, chimney, hanging sign, umbrellas, string lights.
  5. Pratt's Fish Market: long open hall, blue-and-white striped roof, fish counters, a silver fish weathervane.
  6. Swell Surf Co.: yellow beach hut, a fan of surfboards, an umbrella.
  7. Post office + Nell's: red, with a 16 m clock tower.
  8. Hartley's Outfitters: a giant yellow sou'wester on the roof.
- **Mari's taco boat:** moored across the pier from Biscuit (the Day-3+ state).
- **Shop list.** Kufu's list had Hartley's and Mari's instead of map-B's Swell Surf Co. and Post office. *Pick:* all 7 map
  pins stay (rule 42; Swell Surf is rule 43's surf shop). Hartley's is added on the high street, since map B has no pin for
  it; Mari's is her boat.
- **The town:**
  - terrace rows of narrow, bright houses along the real waterline (105);
  - lamps on the quay and dinghies at their moorings;
  - 130 hillside houses, some hipped, with chimneys;
  - lanes instead of a dirt field.
- **From the sea:**
  - Dot's lighthouse is now 26 m of red and white bands on a rock knoll;
  - the Margaret is a 26 m wreck, bow and wheelhouse out of the water, two leaning masts;
  - Gullrock's reef has 70 rocks awash and a two-row surf break on its west edge (the Widowmaker);
  - the north-horn point reef has its own surf ring.
  - New views: `landmarks_reef_lighthouse.png`, `landmarks_wreck.png`.
- **The shoreline:**
  - wet sand at the waterline, dune grass behind the beaches;
  - shingle coves where sand meets rock, boulders on the beaches;
  - banded sea cliffs (three tones);
  - steep inland slopes as mottled scrub, not grey;
  - white foam collars round wet rocks, breaking-surf lines along the beaches.
  - The foam pieces are render stand-ins; in the game, foam is the water shader's job (IMPORT-PLAN §5).
- **How Unity takes it later:** `IMPORT-PLAN.md`.
