# Port Boss: 6 art directions, shown on our Toon Harbor pack

Worker: pb-artdir. Date: 2026-10-31. Task: t-8adeb5da. This is planning only: nothing here is merged into the game.

**Start with `sheets/overview-grid.jpg`.** It shows all 6 directions side by side. Then open one sheet per direction (`sheets/<id>-sheet.jpg`). Each sheet has the hero, gameplay, close-up and dusk shots, the character concept and the palette.

## What is in this folder
| Path | What |
|---|---|
| `sheets/overview-grid.jpg` | All 6 directions: hero, gameplay, character concept and a palette strip |
| `sheets/<id>-sheet.jpg` | One sheet per direction: hero, gameplay, close-up, dusk, concept, palette with hex codes |
| `renders/<id>/` | Real in-engine renders, 1920×1080: `1-hero`, `2-gameplay`, `3-closeup`, `4-dusk-hero`, `5-dusk-gameplay` |
| `renders/0-pack-default/` | Reference: the pack's own materials, sky, water and volume profile, same scene and cameras |
| `concepts/<id>-concept.png` | Grok CLI concept of OUR dock worker in that style, next to the pack's tug. Concept only. Prompts are in `concepts/run.sh`, refs in `concepts/ref/` |
| `references.md` | 5 reference games or films per direction, with links, verified Steam numbers, awards and technique talks (checked 2026-10-31) |
| `make_sheets.py` | Rebuilds the sheets from the renders and concepts |

## How the renders were made (all free)
- **Scene.** A look-dev harbour built entirely by code, kept separate from the game scene: `Assets/LookDev/` on branch `panoptes/w-pb-artdir` (commit 9de3e36 and later). It uses Toon Harbor prefabs only:
  - quay-wall blocks and tiles;
  - TH_Tugboat_03A at our berth, TH_Boat_Medium_01A and a TH_Freighter_03A_Loaded under a gantry crane;
  - TH_Forklift_01A, a truck with a trailer and container, and a van;
  - crates on pallets and a container stack;
  - bollards, rope, barrels and lamps;
  - sheds, and a breakwater with a lighthouse.
- **The same 3 cameras for every direction:** hero wide (from the water), gameplay 3/4 top-down, and a close-up of the forklift with the tug. A dusk hero and a dusk gameplay shot were added per direction.
- **Per direction:**
  - a lighting rig: sun angle, colour and intensity, hard or soft shadows, trilight ambient, fog, and lamps at dusk;
  - a URP Volume profile, saved to `Assets/LookDev/Generated/Profiles/<id>.asset`;
  - a water material, a sky material, and the pack atlas run through one stylised lit shader with per-direction values: cel steps, shadow tint, rim, specular, paint noise and saturation.
- **Free tools only:**
  - own HLSL shaders: `LD_Stylized`, `LD_Water`, `LD_Sky`, `LD_Outline`;
  - URP's built-in **Full Screen Pass Renderer Feature** for outlines (depth and normal edges);
  - URP **SSAO** and URP post: tonemapping, bloom, colour adjustments, white balance, split toning, lift/gamma/gain, vignette, Bokeh depth of field and film grain.

  No paid assets, and nothing from the Asset Store except the pack itself.
- **Re-render:** `Unity -batchmode -quit -projectPath <worktree> -executeMethod PortBoss.LookDev.LookDevRender.Run [-ldOnly 3-ink-cel] [-ldWidth 1920]`. The pack must be at `Assets/ThirdParty/ToonHarbor`, which is gitignored. `Assets/LookDev/Generated/` is gitignored too. Renders never go into git.
- **The pack in my worktree** is a copy of `Assets/ThirdParty/ToonHarbor`, with its `.meta` files, taken from the w-pb-merge worktree. It is the same v1.0.4 import with the same GUIDs. `git diff --cached` showed 0 pack files in every commit.

---

## The 6 directions

> Every direction keeps our Toon Harbor ships, vehicles and props. The renders change only lighting, materials, water, sky and post.
> "Head:body" is the head height as a fraction of total height. Fonts are free (SIL OFL, Google Fonts).

### 1. Golden Hour Adventure: premium stylised realism  (graphics step-up ★)
**Pitch:** a warm, sunlit "adventure" harbour in the vein of Sea of Thieves: long soft shadows, cool blue shade, haze, and the richest water of the six.

- **References:**
  - [Sea of Thieves](https://store.steampowered.com/app/1172620/)
  - [Tchia](https://store.steampowered.com/app/1496590/)
  - [Sword of the Sea](https://store.steampowered.com/app/2453160/)
  - [Kena: Bridge of Spirits](https://store.steampowered.com/app/1954200/)
  - [Journey](https://store.steampowered.com/app/638230/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky top | `#2F6DC0` |
  | Horizon haze | `#E9DCC8` |
  | Sun | `#FFDDB0` |
  | Shadow tint | `#8C9BD6` |
  | Water shallow | `#3FD1C2` |
  | Water deep | `#0B3D5C` |
  | Fog | `#EBD5B5` |
  | Highlight | `#F2C38E` |
- **Lighting and post:**
  - Lighting: sun 32° high, 2.4 intensity, warm. Soft shadows on High, 160 m shadow distance.
  - Tonemapping and grade: ACES; exposure +0.3, contrast +12, saturation +8, temperature +8. Split toning: teal shadows `#4E7C99`, warm highlights `#F2C38E`. Bake this to a LUT later.
  - Bloom 0.55 (threshold 1.3, warm tint). Vignette 0.22.
  - SSAO intensity 1.2, radius 0.6. No outline.
- **Water:**
  - clear turquoise-to-navy by depth;
  - refraction of the hull under water, glowing teal close to the hull;
  - strong sky reflection at grazing angles and a sharp sun glint;
  - soft contact foam;
  - gentle Gerstner waves (0.16 m).
- **Sky:** deep blue to warm cream haze, soft cumulus, and a low cloud bank. Dusk turns it into an orange horizon with a blue-violet top.
- **UI and fonts:** confident adventure UI: warm parchment cards with teal accents. Fonts: **Fraunces** for headings, **Barlow** for body.
- **Characters:**
  - Head:body ≈ 1:6.5: stylised but grounded.
  - Thick forearms and big hands, Sea of Thieves style chunky exaggeration.
  - Painted-texture clothes.
- **Animation:** weighty with some overshoot: heavy lifts and real momentum. Ragdolls read as physical, not floppy.
- **Pack fit:**
  - Best of the six. The pack's semi-real proportions and clean atlas colours gain the most from proper light, shadows, AO and water.
  - Clash: the pack's flat atlas has no texture detail, so very close shots can look plain next to painterly characters. Fix: add a subtle paint-noise or detail pass on the atlas.
- **Effort (free only): M.** Everything rendered here already works in URP. Remaining: a time-of-day rig, baking the grade to a LUT, and wave and foam motion.
- **Risks:**
  - Performance of SSAO plus soft shadows plus water at 60 fps on mid-range GPUs. Expect an SSAO quality setting.
  - Leans toward realism, which Manan has rejected before. Keep it stylised through the palette and characters.

### 2. Miniature Harbour: soft matte diorama  (graphics step-up ★)
**Pitch:** the harbour as a beautiful handmade model in the vein of Tiny Glade: airy light, big soft contact shadows, creamy pastels, mint water and a tilt-shift lens.

- **References:**
  - [Tiny Glade](https://store.steampowered.com/app/2198150/)
  - [Townscaper](https://store.steampowered.com/app/1291340/)
  - [Dorfromantik](https://store.steampowered.com/app/1455840/)
  - [Islanders](https://store.steampowered.com/app/1046030/)
  - [Lonely Mountains: Downhill](https://store.steampowered.com/app/711540/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky | `#78B4E8` |
  | Horizon | `#DCEAF0` |
  | Sun | `#FFF3E0` |
  | Shadow tint (lavender) | `#B8BCE0` |
  | Water shallow (mint) | `#A6EBD8` |
  | Water deep | `#52B4C0` |
  | Fog | `#EEE8DF` |
  | Cream tint | `#FFF6E6` |
- **Lighting and post:**
  - Lighting: sun 50° high, 1.6 intensity. Soft shadows at strength 0.7. High, warm ambient light.
  - Tonemapping and grade: Neutral; exposure +0.45, contrast −2. Saturation 0.92 on materials.
  - Bloom 0.25. Vignette 0.32.
  - **Bokeh DoF:** 300 mm, f/1.0, focused on the subject (70 mm f/2.8 on close-ups).
  - **SSAO intensity 3, radius 2:** big soft contact shadows. No outline.
- **Water:** calm "bathtub" mint water, opaque-ish, soft wide contact foam, tiny waves (0.07 m), weak reflections.
- **Sky:** pale powder blue, a few puffs. Dusk is a peach-and-lavender sky.
- **UI and fonts:** soft rounded cards and paper-craft tags. Fonts: **Nunito** or **Quicksand**.
- **Characters:**
  - Head:body ≈ 1:4: toy figurine.
  - Rounded simple limbs, mitten hands.
- **Animation:** gentle and bouncy: small hops and soft squash. Physics chaos reads as toys tumbling.
- **Pack fit:**
  - Good. The pack's simple shapes look like crafted models.
  - Clash: the realistic freighters and gantries undercut the toy-model idea. The tilt-shift needs a high camera and blurs the action at gameplay distance.
- **Effort (free only): S.** Mostly settings: SSAO, DoF and grade.
- **Risks:**
  - Tilt-shift hurts readability in co-op chaos, so it can't be on all the time. Use it in the menu and photo mode only.
  - Low contrast can look washed out on Steam thumbnails.

### 3. Ink & Cel: bold outline cel-shade
**Pitch:** a comic-book harbour in the vein of Hi-Fi Rush and The Wind Waker: thick ink lines, two flat tones, hard shadows, saturated colour and a toon sea.

- **References:**
  - [Hi-Fi Rush](https://store.steampowered.com/app/1817230/)
  - [Sable](https://store.steampowered.com/app/757310/)
  - [The Wind Waker](https://en.wikipedia.org/wiki/The_Legend_of_Zelda:_The_Wind_Waker)
  - [Jet Set Radio](https://store.steampowered.com/app/205950/)
  - [Ōkami HD](https://store.steampowered.com/app/587620/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky | `#2E8BFF` |
  | Horizon | `#BFE8FF` |
  | Sun | `#FFF8EC` |
  | Shadow tint (violet) | `#8C80C0` |
  | Water shallow | `#3FC4E8` |
  | Water deep | `#2462C4` |
  | Ink | `#1A1428` |
  | Accent yellow | `#FFD23C` |
- **Lighting and post:**
  - Lighting: sun 45° high, **hard** shadows. Diffuse ramp in 2 steps (edge 0.02). Toon specular on vehicles.
  - Tonemapping and grade: **None**, to keep the flats flat; contrast +10, saturation +15.
  - Bloom 0.15. No vignette.
  - SSAO 0.5, mostly off.
  - **Outline: yes.** Full-screen depth and normal edges, 2 px, ink `#1A1428`. It runs before transparents, so it is never drawn through the water.
- **Water:** 3 flat colour bands by depth, crisp white contact foam, sharp toon glint, banded fresnel.
- **Sky:** saturated blue, flat-shaded cumulus in two tones, a low cloud bank.
- **UI and fonts:** loud comic panels, thick borders, halftone. Fonts: **Bangers** or **Archivo Black** for headings, **Inter** for body.
- **Characters:**
  - Head:body ≈ 1:4.5.
  - Very big hands and forearms, thin waist. Strong silhouettes.
- **Animation:** snappy, with squash-and-stretch and smear frames, timed to impacts. Matches Hi-Fi Rush's BAFTA-winning animation.
- **Pack fit:**
  - Very good. The outline hides the pack's low-poly edges, and its flat atlas colours are already "cel" colours.
  - Clash: thin rails and cables produce noisy line clusters at a distance. They fade with distance, but the freighter rigging still gets busy.
- **Effort (free only): S–M.** The shader and outline already exist. Remaining: tuning lines per object (rails), hand-picking ramp colours, and toon VFX (splashes, impact stars).
- **Risks:**
  - Line noise on dense props, and shimmer when the camera moves (needs TAA or a thicker line at a distance).
  - A well-known look, so it needs our own colour voice.

### 4. Ponyo Coast: Ghibli-ish soft anime watercolour
**Pitch:** a summer harbour in the vein of Ponyo and Kiki's Delivery Service: soft cel shading with painted blue-violet shade, towering cumulus, luminous turquoise sea, soft glow and paper grain.

- **References:**
  - [Ni no Kuni II](https://store.steampowered.com/app/589360/)
  - [Genshin Impact](https://en.wikipedia.org/wiki/Genshin_Impact) (Liyue harbour)
  - [Kiki's Delivery Service](https://en.wikipedia.org/wiki/Kiki%27s_Delivery_Service)
  - [Ponyo](https://en.wikipedia.org/wiki/Ponyo)
  - [Lost in Play](https://store.steampowered.com/app/1328840/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky | `#3A86E0` |
  | Horizon | `#CFE6F5` |
  | Sun | `#FFF3DC` |
  | Shadow tint (blue-violet) | `#98A6EA` |
  | Water shallow | `#46D6C8` |
  | Water deep | `#2160B8` |
  | Line (warm brown) | `#805C52` |
  | Paper | `#FFFBF2` |
- **Lighting and post:**
  - Lighting: sun 40° high, soft. 1-step ramp with a soft edge (0.12), painted shade tint, paint noise 0.3.
  - Tonemapping and grade: Neutral; exposure +0.2, saturation +10.
  - Bloom 0.45 (scatter 0.8), the "Ghibli glow". Film grain 0.15 for paper. Vignette 0.15.
  - SSAO 0.6.
  - **Outline: yes,** thin (1.2 px), warm brown, 75 % strength.
- **Water:** luminous turquoise-cobalt, soft glint, crisp white foam.
- **Sky:** deep blue with a big cumulus bank on the horizon. At gameplay height it also reads as distant hills.
- **UI and fonts:** hand-drawn, warm, letter-paper cards. Fonts: **Zen Maru Gothic** or **M PLUS Rounded 1c**.
- **Characters:**
  - Head:body ≈ 1:5.
  - Gentle rounded features, medium limbs, simple hands.
- **Animation:** soft and lively, light overshoot: cosy rather than slapstick.
- **Pack fit:**
  - Good in close-ups: the tug and forklift look like anime background props.
  - Clash: the large modern freighters and gantry cranes feel too industrial for a "Ghibli town". It suits the small stage-1 pier better than the stage-5 mega-port.
- **Effort (free only): M.** Remaining: hand-tuned sky art, softer line rules and painted textures on our own models.
- **Risks:**
  - "Ghibli-like" invites comparison; keep it inspired, never imitated.
  - Soft contrast in chaotic gameplay.

### 5. Glossy Toybox: saturated glossy co-op
**Pitch:** the genre's crowd-pleaser in the vein of Astro Bot and It Takes Two: candy colours, glossy plastic highlights, clear-coat rims, bright clean light and candy-aqua water.

- **References:**
  - [Astro Bot](https://www.playstation.com/en-us/games/astro-bot/)
  - [It Takes Two](https://store.steampowered.com/app/1426210/)
  - [Overcooked! 2](https://store.steampowered.com/app/728880/)
  - [Fall Guys](https://store.steampowered.com/app/1097150/)
  - [Moving Out 2](https://store.steampowered.com/app/1641700/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky | `#4FA9FF` |
  | Horizon | `#DDF3FF` |
  | Sun | `#FFFFFF` |
  | Shadow tint (lilac) | `#B4A6E6` |
  | Water shallow | `#4EE6F0` |
  | Water deep | `#1B8FE0` |
  | Accent red | `#FF4F6A` |
  | Accent yellow | `#FFC83C` |
- **Lighting and post:**
  - Lighting: sun 55° high, 1.9 intensity. Soft shadows at strength 0.85.
  - Materials: specular 0.7, power 60, rim 0.45. Saturation 1.35.
  - Tonemapping and grade: ACES; exposure +0.4, contrast +15, saturation +22.
  - Bloom 0.6. Vignette 0.2.
  - SSAO 1.4. No outline.
- **Water:** candy aqua, bright glint, fat white contact foam.
- **Sky:** clean bright blue with fluffy clouds.
- **UI and fonts:** chunky glossy buttons and bouncy icons. Fonts: **Fredoka** or **Baloo 2**.
- **Characters:**
  - Head:body ≈ 1:3.5: chubby.
  - Big round hands, short thick limbs.
- **Animation:** very snappy squash-and-stretch, bouncy idles, exaggerated ragdolls.
- **Pack fit:**
  - Medium. The pack already looks like this, so it adds the least. In the renders it is the closest to the pack default.
  - Clash: glossy specular on concrete and rust reads as wet plastic. It needs per-material gloss masks the atlas doesn't have.
- **Effort (free only): S.** But looking unique needs per-material gloss work (M).
- **Risks:**
  - Generic: it looks like many co-op party games and doesn't stand out on Steam.
  - Gloss on everything gets noisy.

### 6. Lantern Dusk: moody graphic late light  (graphics step-up: atmosphere)
**Pitch:** the harbour at the end of the day in the vein of Firewatch and Dredge: a low orange sun, layered gradient fog that flattens the distance into poster bands, a dark teal sea reflecting the sky, and warm lamps coming on.

- **References:**
  - [Dredge](https://store.steampowered.com/app/1562430/)
  - [Firewatch](https://store.steampowered.com/app/383870/)
  - [Oxenfree](https://store.steampowered.com/app/388880/)
  - [Season: A Letter to the Future](https://store.steampowered.com/app/695330/)
  - [Sable](https://store.steampowered.com/app/757310/)
- **Palette:**

  | Role | Hex |
  |---|---|
  | Sky top | `#2D4A7A` |
  | Horizon | `#F7A86B` |
  | Sun | `#FF9E60` |
  | Shadow tint (purple) | `#6B669E` |
  | Water shallow | `#2E6F73` |
  | Water deep | `#0E2733` |
  | Fog | `#E89A6E` |
  | Lamp | `#FFB060` |
- **Lighting and post:**
  - Lighting: sun 24° high, 2.6 intensity, orange. 3-step banded diffuse, strong back-rim.
  - Fog: linear, 60–420 m (day); 20–200 m at blue hour.
  - Tonemapping and grade: ACES; exposure +0.35, contrast +16. Split toning: teal shadows, orange highlights.
  - Bloom 0.8 (threshold 1.1), for the lamps. Vignette 0.35. Grain 0.2.
  - SSAO 0.8. No outline.
- **Water:** dark teal with 4 bands, strong orange sky reflection, a long glint.
- **Sky:** navy to orange gradient with soft clouds. At blue hour: `#121B36` → `#6E4A6B`, with lamps at 40.
- **UI and fonts:** poster or field-guide style, condensed caps. Fonts: **Oswald** or **Bebas Neue**, with **IBM Plex Mono** for numbers.
- **Characters:**
  - Head:body ≈ 1:6: leaner adults.
  - Simplified graphic shapes, medium hands.
- **Animation:** weighty and slower: tired end-of-shift body language.
- **Pack fit:**
  - Good for mood: the fog hides the pack's plain far geometry.
  - Clash: the pack's bright atlas colours lose their identity in orange light, and its readability for co-op chaos is the lowest.
- **Effort (free only): S** as a lighting preset; **M** if it drives a whole day/night cycle.
- **Risks:**
  - Too dark and too orange for a whole game.
  - The best use is as the **evening phase** of another direction, not as the main look.

---

## Recommendation: the top 2 (judged on looks)
1. **Golden Hour Adventure.** Best looking on the pack.
   - It is the clearest step up in graphics quality: soft shadows, AO, haze, and refractive water that glows around the hull.
   - It flatters the pack's proportions without making it look like a toy.
   - Use **Lantern Dusk as its evening and night phase.** The dusk renders show the two blend naturally.
2. **Ink & Cel.** The most distinctive and readable for co-op chaos.
   - The outlines hide the pack's low-poly edges and make our own simpler characters fit in.
   - It is cheap to build with free tools, and a Steam capsule in this style stands out.

**Runners-up:**
- Ponyo Coast: lovely close-ups, but it is too industrial at port scale.
- Miniature: charming, but the tilt-shift hurts gameplay.
- Glossy Toybox: too close to the pack default and to generic co-op games.

## Not finished yet
- **Two re-renders are pending.** The last look tweaks (lower cloud banks for Ponyo and Ink & Cel, soft glint for Ponyo, hero tilt-shift focused on the tug for Miniature) are committed in 5fd8010 but not rendered. Free RAM stayed under the 5 GB rule (3.6 GB). Re-render with `-ldOnly 2-miniature,3-ink-cel,4-ponyo-coast`, then run `python -I make_sheets.py`.
- **Two concepts were still generating** in Grok when I stopped: `2-miniature` and `4-ponyo-coast`. Re-run `make_sheets.py` once they land. Their sheet slots read "missing" until then.
- **One rule slip:** a single Unity batch run (about 50 s) started with 2.4 GB free, under the 5 GB rule. The render script now refuses to start below 5 GB.
