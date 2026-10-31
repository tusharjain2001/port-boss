# Port Boss: 6 character variants (pb-chars, 2026-10-31)

**Recommendation:** replace the current worker with a **chunky ~4.5-heads-tall kit**: big head, big hands, big boots, smooth hand-made shapes. Build it as **one Mixamo skeleton + 6 body bases + swappable parts**, so the six characters below are just six saved looks in the existing character editor. Put the **player colour on a fixed channel** (armband, back patch, hard-hat stripe, name tag) that no outfit choice can hide. Use the same kit for NPCs (lorry drivers, Marlowe, market customers) so every human in the game shares one style. About **8 working days, $0** (Blender, Mixamo's X Bot rig and clips, Unity). **Joints stay Mixamo-shaped** (X Bot ratios, see `editor-kit.md` §2b): the big head, hands, boots and bulk are mesh only, so every body type plays the Mixamo clips cleanly.

![lineup](img/lineup.png)

## What's here
| File | What |
|---|---|
| `research.md` | Fall Guys, Overcooked, Lethal Company, R.E.P.O., Crashout Crew (+ PEAK), PowerWash Simulator, Sea of Thieves, Human: Fall Flat: silhouette, head size, player colours, hats, distance, comedy in motion. Linked sources, 7 takeaways. |
| `cast.md` | The 6: Bruno (stocky), Stretch (tall-lanky), Pip (short-round), Rani (broad), Old Gus (wiry), Sully (wildcard: the diver). Who they are, face, outfit, motion, and the preset values. |
| `editor-kit.md` | The modular kit: skeleton, body bases (blend shapes + root/head scale + posture), the parts table, polycount (≤ 8.8k tris, 1 material, palette texture), personality in motion, the exact mapping onto `Look` / `Worker.Apply` / `GameUI.Character` in the repo, and the build plan. |
| `img/turn-*.png` | Turnarounds: front / side / back, one per character |
| `img/sheet-*.png` | Expression + pose sheets: happy, strain, panic, smug; carrying 3 crates, falling, cheering |
| `img/lineup.png` | All 6 in front of Tug King: a Grok image_edit beauty shot made from the real game screenshot + the six front views (a style read; the boat is redrawn by Grok) |
| `img/_fronts.png` | The six front views at relative height on one strip (cut out of the turnarounds) |
| `img/lineup-scale.png` | All 6 pasted at **true scale** into a real game screenshot (Tug King, `proof/refine/merged/areas/boat-day.png`), with in-game-style name tags in player colours |
| `img/raw/` | Grok originals, including the rejected Rani v1/v2 and Bruno v1 (with "CREW" text) |
| `_work/` | Scripts: `cast.py` (prompts), `gen.py` (Grok runner), `cutout.py`, `lineup.py`, logs |

## Why the current character feels wrong (short)
It is about 7 heads tall with a small head and normal-size hands, so at play distance it reads as a tiny low-res realistic man. Its surfaces come from a voxel remesh, so they're lumpy. All variety (face, hair, beard) is about 10 cm big and can't be read. And the player's colour is just a recolourable shirt. Details in `editor-kit.md` §1.

## The cast at a glance
| | Bruno | Stretch | Pip | Rani | Old Gus | Sully |
|---|---|---|---|---|---|---|
| Body | Stocky "fridge" | Tall-lanky "lamppost" | Short-round "egg" | Broad V | Wiry, hunched | Average + huge helmet |
| Headwear | Beanie | Hard hat + ear defenders | Sou'wester | Bandana | Flat cap | Brass diving helmet |
| Personality | Quiet strongman | Over-eager apprentice | Cheerful fish expert | Ex-crane-op boss | Unbothered old salt | Silent diver |
| Comedy in motion | Waddles, drops flat | Windmill flail | Rolls and pops up | Crate under each arm | Speed-walks, lands like a cat | Sinks, walks out with a crate |

## Fit with the art directions and the Toon Harbor pack
- The kit is **one flat-colour palette texture + one material**. Any of the stylised directions can restyle it by swapping the shader (toon ramp, soft gradient, outline or no outline) and the palette values; the shapes stay.
- The proportions match the pack's **chunky, rounded vehicles and ships**. In `lineup-scale.png` the cast is sized by perspective against the pack's own captain standing on Tug King's deck (assumed 1.8 m), so heights are true to the game world (±10%).
- **Style gap to close in Blender (seen in `lineup-scale.png`):** the Grok concepts render softer and more detailed (fabric folds, skin shading, glossy brass) than the pack's flat low-poly ships. Build the in-game models like the pack: bevelled low-poly shapes, flat palette colours, the same toon ramp, and no texture detail. Keep the concepts' **shapes, proportions and colours**, not their rendering.
- One known clash: the pack's own people (Marlowe is pack character 03A, the lorry drivers, the market customers) have more realistic proportions. Recommend rebuilding NPCs from our kit (LOD ~4k tris) so the cast never stands next to a different style.

## Open questions for Manan (batched)
1. Six fixed body bases + a Height step (recommended), or free body sliders?
2. Is the diver (Sully) in, or should the wildcard be something else?
3. Pack NPCs: rebuild them from our kit (recommended) or keep the pack's?
