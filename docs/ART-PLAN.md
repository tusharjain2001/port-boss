# Port Boss: art plan (draft for Kufu + Manan, 29 Oct 2026)

**Target:** the cp1/cp2 look (build `20261029-0003`), polished. Stylised, warm, clean, toy-*adjacent* but not a kids' game: chunky readable shapes, flat colour, golden-hour light. **No photoreal**: no scanned/PBR textures, no HDRI photo skies, no realistic humans. "Oomph" = light, shadow, colour, motion and life, not surface detail.

Reference shots of the look Manan liked: `Desktop/Port Boss/proof/overnight/cp1-pier-boat.png`, `cp2-main-menu.png`, `cp2-carrying-stack.png`.

## 1. One material approach, used everywhere
- **Flat colour, URP Lit, no textures.** Smoothness 0.10–0.20 (wood, cloth, concrete), 0.45 (painted metal, boat hull), 0.85 (water only). Metallic 0.
- **Bevelled boxes instead of hard cubes** (the biggest "cheap" tell today). One procedural bevelled-cube mesh, bevel ≈ 4 cm or 8% of the smallest side, cached per size. Used by planks, crates, buildings, boat parts. Cylinders get 16+ segments.
- **Colour variation instead of texture:** every repeated piece (plank, crate, container) gets a small tint jitter (±4% value, ±2% hue) so nothing looks copy-pasted.
- **No outlines.** They'd push it toward cartoon; Manan liked cp2 without them.
- **Two-tone details** carry the "material": a dark band on posts at the waterline (wet), a lighter top edge on planks (wear), a rust stripe on the hull.

## 2. Palette (hex)
| Use | Colour |
|---|---|
| Pier planks (base / light / dark jitter) | `#A27B55` / `#B38B62` / `#8A6A4A` |
| Posts, stringers, dark wood | `#5B4330`, wet band `#3E2E22` |
| Quay concrete | `#A8A196` (top), `#8E887E` (faces) |
| Sand / beach | `#C2AE88` |
| Grass headlands | `#7B8550`, hills `#6F7A4A`–`#8A8758` |
| Rock | `#6C665E`–`#8A8278` |
| Sea shallow → deep | `#3A8C8C` → `#123A52`, foam `#F2F1EA` |
| Boat hull / stripe / cabin / roof | `#2E4A63` / `#E8DCC4` / `#E8DCC4` / `#B5402F` |
| Fish crate wood / slats / fish fresh / fish spoiled | `#B88A5A` / `#7A5638` / `#A9C3CF` / `#6B7A3A` |
| Buyer awning | `#B5402F` + `#E8DCC4` stripes |
| Player colours (beanie, jumper) | `#D8892B`, `#3F86C4`, `#5AA34A`, `#C4473F` |
| Hi-vis vest | `#F2C230` with `#E8E8E0` stripes |
| UI gold / ink | `#F9C962` / `#0D1421` |

## 3. Light and post
- **Key light:** sun from behind-left of the default camera, elevation 42° → 9° over the day, azimuth 20° → 35°. Colour `#FFE6C2` → `#FF9E61` at sunset. Intensity 1.7 → 1.2. Soft shadows, strength 0.8, distance 110 m.
- **Ambient:** trilight. Sky `#9FADC7`, equator `#9E8F7A`, ground `#473D33`. Night: blue `#38476E` ambient, sun below the horizon.
- **Sky:** Unity procedural sky (warm horizon), sun disc small, plus stylised puffy cloud clusters. No photo HDRI.
- **Fog:** gentle warm haze (exp², density 0.0021 at 1200 m view) so the rival port reads as "across the bay".
- **Post (LDR grading, as cp2):** ACES tonemap, bloom 0.45 (threshold 1.05), saturation +8, contrast +8, white balance +12 (warm), vignette 0.22. SSAO subtle (intensity 0.6, radius 0.4), only if the A/B against cp2 says it helps.

## 4. Proportions
- **Crate (fish):** 0.55 m cube, bevelled, 2 slat bands per side with dark gaps, a lip at the top, fish + ice visible from above, two rope handles. Reads as a wooden dock crate from 6 m. (The Kenney "survival-kit box" looks like cardboard: not used. Our crates are code-built.)
- **Pier:** 6 m wide, planks 0.30 × 0.12 m with 4 cm gaps, slight random height (±1 cm) and yaw (±0.6°) jitter for a hand-built feel, missing planks as now. Posts every 3 m with the wet band. Rope coil and bollards on the boat side.
- **Character (stylised dockworker, OPEN ITEM, see §6):** about 1.8 m. Head ≈ 1/4 of height (chunky). Big mitts and boots, short thick limbs. Jumper in the player colour, navy overalls, **hi-vis vest**, knit beanie with pompom. Readable from the chase cam at 6 m: silhouette plus beanie colour tells players apart.
- **Boat:** 4.6 × 12 m, chunky wheelhouse, rounded bow (bevelled wedge, not a rotated cube), rust stripe, tyre fenders on the pier side.

## 5. Every visible object: keep / redo
| Object | Now | Verdict |
|---|---|---|
| Pier planks, holes, posts, stringers | flat boxes (cp2) | **Keep**; add bevel, tint jitter, wet band on posts, rope coils |
| Quay / shore / slipway | flat boxes | **Keep**; bevel the kerb edge, two-tone top/faces |
| Buyer stall (table, awning, buyer figure) | primitives | **Keep** table + awning; **redo** buyer figure in the same style as the player character |
| Shed | boxes | **Keep**; add a door frame, window, corrugated roof as stripes |
| Warehouses on the quay | boxes | **Keep**; add roof overhang, doors |
| Fish crate | code-built slatted box | **Redo** per §4 (bevel, lip, handles, ice) |
| Boat | boxes + rotated-cube bow | **Redo bow**, add fenders and a net pile; keep the rest |
| Gangplank | plank + cleats | **Keep** |
| Trolley | primitive flat-bed | **Keep**; bevel, rubber wheels as cylinders |
| Water | stylised shader (gradient, foam, ripples) | **Keep, A/B vs cp2**; foam slightly stronger at posts/hull |
| Sky + clouds | procedural + puffy spheres | **Keep, A/B vs cp2** |
| Headland hills, rocks | squashed spheres / boxes | **Keep**; a few trees as cone+sphere for life |
| Lighthouse | cylinders | **Keep** |
| Rival port (cranes, containers, ships) | boxes + Kenney ships | **Keep** the boxes; **A/B** the Kenney ships (they may read too toy-like next to the boxes) |
| Distant islands + passing ships | spheres + Kenney ships | **Keep, A/B** |
| Gulls (own-made model v1) | flapping, landing | **Keep**; check v2 model once rendered |
| Player character | chunky primitive dockhand (beanie, overalls, boots, mitts) | **Interim**: add hi-vis vest; final source = open item |
| HUD, title, menus | IMGUI + Alfa Slab title | **Keep**; style pass later |

## 6. Open decisions for Manan
1. **Character source:** (a) own-made in code (as now, cheapest, consistent), (b) own-made in Blender by the team (stylised rigged dockworker, ~1 day), or (c) a paid stylised pack (needs his approval per purchase). Recommendation: (a) for the demo, (b) when the style bible is locked.
2. **Bevelled-box mesh pass:** recommended first visual task after agreement (one code change lifts every object).
3. **Kenney ships at the rival port and on the horizon:** keep or replace with code-built ships in the palette above (decide on the A/B shot).

## Process (agreed rule)
No visual change ships without a same-camera before/after against cp2 (`20261029-0003`). Anything that looks darker, muddier or more "realistic" is backed out.
