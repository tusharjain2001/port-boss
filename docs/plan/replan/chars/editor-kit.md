# One editable kit: the 6 presets are 6 saved looks

All 6 characters (Bruno, Stretch, Pip, Rani, Old Gus, Sully) are **presets of one modular kit**. The kit has one skeleton, one material and one palette texture. A preset is only a `Look` value, about 20 bytes. Anything a preset wears, any player can wear in any mix.

## 1. Why the current worker feels wrong (what the kit fixes)

The current kit is `art-src/worker/build_worker.py` → `Assets/Art/Worker/Worker.fbx`, read 2026-10-31. Proof shots: `Desktop/Port Boss/proof/refine/character/blender-looks.png` and `merged/look/look-0.png`.

| Problem | Cause | Kit fix |
|---|---|---|
| Looks like a realistic man, but low-res | ~7 heads tall, small head, normal hands | **~4.5 heads tall**: head ×1.5, hands ×1.3, boots ×1.4 |
| Lumpy, wobbly surfaces | Primitives → voxel union → QuadriFlow remesh: no clean edge loops, dents at every join | Hand box-modelled base with clean loops and bevels, one subdivision level applied |
| Everyone looks the same | One body; the variety is face, hair and hat at about 10 cm each, too small to read at play distance | **6 body bases** that change the whole silhouette, plus big headwear shapes |
| Players hard to tell apart | Slot colour is only a *default* shirt colour; players can recolour it away | A fixed **player-colour channel** (armband, back patch, hat stripe, name tag) that no option can change |
| No personality in motion | One gait for all | Per-body gait parameters and idles (section 6) |
| ~14k tris visible (sum of `parts.txt`) | Remesh density spread evenly | **≤ 9k tris**, spent on hands, face and headwear |

## 2. Skeleton: one Mixamo-compatible rig

- **One armature** with Mixamo bone names (`mixamorig:Hips` … `mixamorig:RightToeBase`), 4.5-head proportions and one Unity Humanoid Avatar. All current Mixamo clips (`Desktop/Port Boss/mixamo-incoming`, mapped in `PortBossBuild.MixamoClips`) keep working through Humanoid retargeting. `CharAnim` already measures strides per rig.
- **How to get it free:** do **not** use the Mixamo auto-rigger. It puts joints where the *mesh* shoulders are, and that's exactly how today's worker got shoulder joints 1.67× too wide (pb-feel, `replan/feel/README.md` and `02-proportions-vs-mixamo.png`). Instead, import **Mixamo's own `X Bot.fbx` armature** (`Desktop/Port Boss/mixamo-incoming/X Bot.fbx`) into Blender, scale it uniformly per the spec below, and skin our mesh to it (*Automatic Weights*, then clean up by hand). Every kit part is weighted to that one armature.
- **Bones:** the standard Mixamo set, but with hands as mittens: keep the thumb and one finger chain, drop the other three fingers. Bigger, more readable hands and fewer bones to animate. No extra bones in v1.

### 2b. Skeleton spec: joints stay Mixamo-shaped, the comedy lives in the mesh

**Rule:** the joints are X Bot's joints, scaled **uniformly** so the `Head` joint sits at **82%** of the character's full height. The big head (~22% of height, 4.5 heads tall) is *mesh above the Head joint*. Big hands, big boots, fat, muscle and shoulder pads are *mesh around fixed joints*. No joint moves more than **±5%** of its bone length from the scaled X Bot. Body bases (section 3) change only mesh (blend shapes), uniform root scale, head-bone scale and a posture offset, so **every body type plays the Mixamo clips the same way**.

X Bot measured with pb-feel's `tools/fbxbones.py` on `X Bot.fbx` (cm; HeadTop_End = 182.0, Head joint = 159.9):

| Measure | X Bot (cm) | X Bot, % of full height | **Our kit, % of full height** (Head joint at 82%) | Note |
|---|---|---|---|---|
| Head joint height | 159.9 | 87.9% | **82.0%** | Head mesh top ≈ Head joint + 18% of height |
| Shoulder joints (LeftArm ↔ RightArm) apart | 30.3 | 16.6% | **15.5%** | Kufu: ~17%. Keep it at or under 17%; the "broad" look is mesh (VTaper, pads) |
| Shoulder joint height | 144.1 | 79.2% | **73.9%** | |
| Arm, upper + fore (Arm→ForeArm→Hand) | 27.8 + 28.3 = 56.2 | 30.9% | **28.8%** | Hand mesh can reach 13–15% of height (pb-feel) around the same wrist |
| Hip joints (UpLeg ↔ UpLeg) apart | 16.4 | 9.0% | **8.4%** | Today ours are 1.38× wider; that's what makes the stride short and wide |
| Hips joint height | 104.3 | 57.3% | **53.5%** | |
| Leg, thigh + shin (UpLeg→Leg→Foot) | 44.4 + 44.5 = 88.9 | 48.8% | **45.6%** | Kufu: ~49%; the 3-point gap is the uniform scale from the bigger head |
| Ankle height | 8.7 | 4.8% | **4.5%** | Boot mesh goes up to ~12% (big boots) without moving the ankle |
| Foot, ankle → toe base (horizontal) | 10.7 | 5.9% | **5.5%** | Boot mesh may be up to 1.4× longer at the toe |

- **Example:** a 1.75 m base character has Head joint 1.435 m, shoulder joints 27.2 cm apart, legs 79.8 cm and hips 93.6 cm high. Bruno (root 0.92) and Stretch (root 1.14) scale all of it uniformly.
- **What this costs the look:** the Grok concepts draw shorter legs than ~46%. The rig won't do that. Recover the stubby read in the **mesh**: a lower trouser crotch line, high-waisted trousers or overalls, tall big boots, a short neck mesh, and the big head. If Manan wants truly short legs (Gang Beasts style), that means procedural or re-keyed legs instead of mocap (pb-feel, option 2). That's a separate decision.
- **Check before skinning:** run `fbxbones.py` on the exported `Kit.fbx` and compare against this table (±5%). Add it to the build script as an assert.

## 3. Body types: bases from blend shapes plus three transforms

**Recommendation: 6 fixed bases, not free sliders.** A base = blend-shape weights + root scale + head scale + posture offset + gait profile. It's the Sea of Thieves lesson: whole distinct bodies read better than 12 sliders nobody touches. Add one **Height** step selector (5 steps) as the only slider.

| Base | Blend shapes on body (0–100) | Root scale | Head bone scale | Posture (LateUpdate offset) | Preset |
|---|---|---|---|---|---|
| Stocky | Heavy 60, Muscle 60, ShortNeck 100 | 0.92 | 1.05 | chest out 5° | Bruno |
| Tall-lanky | Thin 100, LongNeck 80 | 1.14 | 0.92 | slouch 8°, shoulders fwd | Stretch |
| Short-round | Round 100, ShortNeck 60 | 0.84 | 1.12 | lean back 4° | Pip |
| Broad | Muscle 100, VTaper 100 | 1.04 | 0.98 | shoulders back | Rani |
| Wiry | Thin 70, Sinew 100 | 0.95 | 1.0 | hunch 14°, head fwd | Old Gus |
| Average | — | 1.0 | 1.0 | — | Sully (helmet does the work) |

- **Blend shapes (shape keys)** on the body: Heavy, Thin, Round, Muscle, VTaper, Sinew, ShortNeck, LongNeck, plus Female (chest/hip shaping, for any base). That's 9 shapes.
- **Root scale** changes height without touching the skeleton, so animations, IK and colliders scale with it. `CharAnim` stride measurement must read the scaled foot.
- **Head bone scale** makes the head bigger or smaller. Hair and headwear are skinned or parented to the head bone, so they follow.
- **Posture** = a fixed extra rotation on Spine1/Spine2/Neck/Head added in `CharAnim`'s existing `LateUpdate` after the IK pass.
- **Every garment carries the same body shape keys,** so any top fits any base. In Blender this is free and scriptable: put a **Surface Deform** modifier bound to the body on the garment, set one body shape key to 1, then *Apply as Shape Key* with the same name, and repeat. It goes in the build script as a loop.
- `ponytail:` limb **length** is fixed in v1, since one Avatar means equal bone lengths. If Stretch doesn't read lanky enough with root scale plus Thin, make a second "Long" Avatar (same bone names, longer legs and arms) and fit garments with a LongLimbs shape key. That's about one extra day.

## 4. The modular parts (all skinned to the one armature)

| Slot | v1 options (✱ = used by a preset) | Notes |
|---|---|---|
| **Body** | Stocky✱, Lanky✱, Round✱, Broad✱, Wiry✱, Average✱ | Section 3 |
| **Head / face** | Bruno✱, Stretch✱, Pip✱, Rani✱, Gus✱, Sully✱ (seen inside the helmet), + 2 spare | Each head has the expression shape keys: Blink, Happy, Strain, Panic, Smug, Angry. Eyes are big and simple, as on R.E.P.O.'s Semibot. |
| **Hair** | Bald, Buzz, Spiky✱, Puffs✱, Undercut✱, White fringe✱, Quiff, Ponytail | Under any hat, hair swaps to a "rim crop" variant. The current `Worker.Apply` already does this. |
| **Beard** | None, Full✱, Walrus moustache✱, Goatee, Stubble | Per-head fit via a head-shape-key bake |
| **Headwear** | None, Beanie✱, Hard hat (+ ear defenders)✱, Sou'wester✱, Bandana✱, Flat cap✱, Diving helmet✱, Cap | Biggest silhouette slot: every preset differs here. The helmet hides head, hair and beard. |
| **Top** | T-shirt✱, Flannel shirt✱, Striped long-sleeve✱, Sleeveless denim✱, Cable-knit sweater✱, Diving suit✱ | The diving suit is a one-piece and hides Legs |
| **Over** (new) | None, Hi-vis vest✱, Tool belt✱, Suspenders | The hi-vis vest is always fluorescent lime, never a slot colour |
| **Legs** | Cargo trousers✱, Short trousers + socks✱, Bib overalls✱, Chest waders✱ | |
| **Boots** | Steel-toe✱, Oversized✱, Rubber✱, Lace-up✱, Diving boots✱ | Boots ×1.4 scale is part of the style |
| **Gloves** | Bare, Leather✱, Rubber✱, Gauntlet✱, Diver rubber✱ | Gloves are seen all the time while carrying (PowerWash Simulator treats gloves as a cosmetic slot) |
| **Colours** | Skin (8), Hair (8), Hat, Top, Over, Legs, Boots, Gloves from the 12-colour cloth list | As today |
| **Player colour** | **Not a choice:** comes from the player slot (orange / blue / green / red) | Armband, back patch, hard-hat stripe and name tag. Outfit cloth colours are muted so the slot colour always wins. |

## 5. Polycount and draw-call budget

| Part | Tris |
|---|---|
| Head + face (with expression shapes) | 1,800 |
| Hair or headwear (only one shows fully) | 1,200 |
| Top + Over | 2,400 |
| Legs | 1,600 |
| Boots | 900 |
| Hands / gloves (mitten + thumb + 1 finger chain) | 900 |
| **Visible total** | **≤ 8,800** (today ~14,000) |

- 4 players at ≤ 35k tris is trivial next to the 51k-tri landscape already in the scene.
- NPCs (lorry drivers, market customers, Marlowe) use the same kit with an LOD1 at about 4k tris (Blender *Decimate*, free).
- **One material**, `Art.Toon` or whichever direction wins, and **one palette texture per character**. Today it's a 4×4 grid. Grow it to **8×4 = 32 cells** to add Over, PlayerColour, HelmetBrass, Sock and Skin-shadow cells.
- Each character merges its visible parts into one `SkinnedMeshRenderer` at spawn (`Mesh.CombineMeshes` with the same bones; Unity built-in, free). That gives **1 draw call per player**. `ponytail:` skip the merge until the profiler says draw calls matter, since 4–20 renderers per player is fine on PC.

## 6. Personality in motion (same skeleton and clips)

Per body base, a small `Gait` record read by `CharAnim`:

| Base | Stride ×, bounce ×, arm swing × | Idle clip (Mixamo, free; Manan downloads) | Fall reaction |
|---|---|---|---|
| Stocky | 0.8, 0.6, 0.7 (waddle) | "Idle" heavy breathing variant | Flat drop, small bounce |
| Lanky | 1.3, 1.2, 1.5 (flail) | a nervous look-around idle | Arms windmill, then ragdoll |
| Round | 0.7, 1.6, 0.9 (bouncy) | "Happy Idle" | Roll (a ragdoll impulse with spin) |
| Broad | 1.0, 0.8, 1.1 | a hands-on-hips idle | Stagger, catch, then fall |
| Wiry | 0.6 at 1.3× cadence (speed-walk) | "Old Man Idle" | Lands on feet ~50% of the time (a quick recover clip) |
| Average | 1.0, 1.0, 1.0 (diver: 0.85 stride, heavy) | "Idle" | Clank: sinks in water, walks out |

Clip names are candidates to check on Mixamo before Manan downloads them. Expressions are driven by game events, not buttons: lifting → Strain, falling → Panic, a sale → Happy, Marlowe's drive-by → Angry, emote key → Smug.

## 7. How it maps onto the existing code (repo `Desktop/port-boss`, read-only, main a6fe3d4)

| Today (`Assets/Scripts/Game/Worker.cs`, `GameUI.Character.cs`, `Player.cs`) | Kit v2 change |
|---|---|
| `Look` struct, 16 bytes, `INetworkSerializeByMemcpy`, all indices, host-clamped in `Clamped()` | **`Look` v2, 20 bytes.** Add `Body`, `Over`, `Legs`, `Boots` (the `Spare` byte becomes `Body`). Keep every field an index, so the host clamp stays the safety check. The existing sync path stays: `Player.LookNet` (NetworkVariable, server write) ← `SetLookRpc` → `Clamped()`. |
| PlayerPrefs `pb.look` = 32 hex chars | New key `pb.look2` = 40 hex. `Load()` reads an old `pb.look` once, maps Face→Head and Top→Top/Over/Legs, then saves v2. |
| `Look.Default(slot)` builds 4 slot defaults | **`Look.Presets[6]`** (Bruno … Sully). Default(slot) = `Presets[slot]`. `Random()` picks across the new arrays. |
| `Worker.Apply`: toggles renderers by name (`Head_N`, `Hat_*` …) and writes a 4×4 palette | Same pattern, keeps the naming convention (`Head_bruno`, `Hat_sou`, `Legs_waders` …). New: set blend-shape weights from the body base on every visible renderer, set root, head and posture values, and write an 8×4 palette including the **slot colour** cell (from `OwnerClientId`, never from `Look`). |
| `GameUI.Character.DrawCharacter`: clipboard with 9 rows (Face, Hair, Beard, Headwear, Top, Vest/Overalls, Trousers, Boots, Gloves) plus Randomise / Back / Save | Add a **strip of 6 preset cards** at the top of the clipboard (portrait + name). One click loads a whole preset, then the player tweaks. The rows become **two tabs**: *Body & face* (Body, Head + skin, Hair + colour, Beard, Height) and *Clothes* (Headwear, Top, Over, Legs, Boots, Gloves, each with a colour). Live 3D preview, drag-to-turn and Randomise stay as they are. |
| `Skin.I.Worker` = one FBX | Still one FBX (`Kit.fbx`): one armature, all parts as children, all shape keys. `Worker.Make` doesn't change. |
| Slot colours orange / blue / green / red used by the HUD | The same 4 colours become the player-colour channel on the model, matching the HUD. Cloth list: drop full-strength orange `#e8752a` and green `#5aa34a` from clothing defaults, or soften them, so they never fake a slot colour. |

## 8. Build plan (all free: Blender 5.1, Mixamo's X Bot rig + clips, Unity)

1. **Base body:** box-model at 4.5 heads with mitten hands and big boots, plus the 9 body shape keys. Skin it to the scaled X Bot armature (§2b) and assert the joint table. (1 day)
2. **6 heads** with 6 expression keys each, and **hair 8 / beard 5**. (2 days)
3. **Clothes:** ~22 garment pieces (the table above). Weights via *Data Transfer* from the body; body shape keys via the Surface Deform loop. All in a headless build script `art-src/kit/build_kit.py`, same CLI pipeline as today. (3 days)
4. **Unity:** Look v2, `Worker.Apply` v2, preset strip and two tabs, gait profiles, expression events. (1.5 days)
5. **Proof:** turntables of all 6 presets next to Tug King in day and dusk light, plus the 4-player co-op slot colours from a high camera at 25 m. (0.5 day)

**About 8 working days.** The risk is modelling quality: the current worker was generated from code and reads lumpy. The base body and the 6 heads need careful hand-modelling passes, judged on looks against these concept sheets. If agent-built models still read cheap after step 1, the free fallback is to start from a CC0 base (Quaternius *Universal Base Characters*, already in `Desktop/Port Boss/assets-incoming/Characters/Quaternius-UniversalBaseCharacters`) reshaped to 4.5 heads.
