# The cast: 6 dock workers, one kit

Each character is a **preset** of the kit in `editor-kit.md`: one Mixamo-compatible skeleton and one material. Players can mix any part.

Concept sheets were made with the Grok CLI. They are concepts only, never game art.

**Shared style rules:**
- About 4.5 heads tall.
- Big hands and boots.
- Smooth chunky toon shapes and flat matte colours.
- Every one has a **different headwear silhouette**.
- The **player colour** sits on the left armband and a round back patch, never on the outfit.
- Hi-vis is always lime, never a slot colour.

Sheets: `img/turn-<name>.png` (front / side / back) and `img/sheet-<name>.png` (4 expressions + 3 poses: carrying 3 crates, falling, cheering).

| # | Name | Body base | Silhouette in one word | Headwear | Shown player colour |
|---|---|---|---|---|---|
| 1 | **Bruno** | Stocky | Fridge | Rolled charcoal beanie | Orange |
| 2 | **Stretch** | Tall-lanky | Lamppost | White hard hat, set too high, + red ear defenders | Blue |
| 3 | **Pip** | Short-round | Egg | Yellow sou'wester + curly hair puffs | Green |
| 4 | **Rani** | Broad | V / anvil | Mustard bandana | Red |
| 5 | **Old Gus** | Wiry | Question mark (hunched) | Brown tweed flat cap | Purple |
| 6 | **Sully** | Wildcard: the diver | Gumball on legs | Brass diving helmet | Cyan |

Player colours in the game come from the slot (orange, blue, green, red, as in the HUD), not from the preset. The six colours above only show that every outfit leaves room for any slot colour.

---

### 1. Bruno: stocky
- **Who:** quiet strongman. Grumbles, lifts the most, secretly feeds the gulls.
- **Face:** olive-tan skin, thick black beard, heavy brow, small eyes, big round nose.
- **Outfit:** rolled charcoal beanie; lime hi-vis vest with silver stripes over a faded navy tee; bare forearms; tan leather gloves; grey cargo trousers with knee pads; brown steel-toe boots.
- **Motion:** waddles under huge stacks. Falls flat like a dropped wardrobe.
- **Preset:** Body Stocky · Head Bruno · Hair Buzz (under beanie) · Beard Full · Headwear Beanie · Top T-shirt · Over Hi-vis vest · Legs Cargo · Boots Steel-toe · Gloves Leather.

### 2. Stretch: tall-lanky
- **Who:** over-eager apprentice. First to volunteer, first to fall in the sea.
- **Face:** East Asian, spiky black hair, big round glasses, big ears, gap-toothed grin.
- **Outfit:** white hard hat sitting too high; red ear defenders round his neck; rust-brown flannel shirt with rolled sleeves; khaki shorts with rainbow-striped socks; oversized black boots.
- **Motion:** noodle arms and huge strides. Flails like a windmill when he trips.
- **Preset:** Body Lanky · Head Stretch · Hair Spiky · Beard None · Headwear Hard hat (ear defenders) · Top Flannel · Over None · Legs Short trousers + socks · Boots Oversized · Gloves Bare.

### 3. Pip: short-round
- **Who:** relentlessly cheerful fish expert. Hums shanties, knows every buyer by name.
- **Face:** warm medium-brown skin, round rosy cheeks, huge grin, curly black hair in two big puffs.
- **Outfit:** yellow sou'wester; yellow oilskin bib overalls over a red-and-white striped top; orange rubber gloves; navy rubber boots.
- **Motion:** bouncy trot. When knocked over she rolls like a ball and pops back up.
- **Preset:** Body Round · Head Pip · Hair Puffs · Beard None · Headwear Sou'wester · Top Striped long-sleeve · Over None · Legs Bib overalls · Boots Rubber · Gloves Rubber.

### 4. Rani: broad
- **Who:** ex-crane operator and natural boss. Points, shouts orders, usually right.
- **Face:** South Asian, brown skin, confident smirk, strong brows, short black undercut, small gold studs.
- **Outfit:** mustard bandana; sleeveless faded-denim shirt; leather tool belt with a walkie-talkie and spanners; olive cargo trousers; black lace-up boots; yellow gauntlet gloves.
- **Motion:** stomps, points with her whole arm, carries a crate under each arm like handbags.
- **Preset:** Body Broad (+ Female) · Head Rani · Hair Undercut · Beard None · Headwear Bandana · Top Sleeveless denim · Over Tool belt · Legs Cargo · Boots Lace-up · Gloves Gauntlet.

### 5. Old Gus: wiry
- **Who:** has seen everything, panics about nothing. Mysteriously the fastest worker on the quay.
- **Face:** pale, ruddy, weathered; big white walrus moustache; bushy white brows; squint; red nose.
- **Outfit:** brown tweed flat cap; cream cable-knit sweater with elbow patches; dark green oilskin chest waders with braces; brown rubber boots; bare bony hands.
- **Motion:** speed-walks with tiny steps, hands behind his back. Lands every fall like a cat (about half the time).
- **Preset:** Body Wiry · Head Gus · Hair White fringe · Beard Walrus · Headwear Flat cap · Top Cable-knit · Over None · Legs Chest waders · Boots Rubber · Gloves Bare.

### 6. Sully: wildcard, the diver
- **Who:** never takes the helmet off, says little, rescues crates from the seabed. A natural for our water and ragdoll systems: sinks, walks along the bottom, climbs out with a crate.
- **Face:** seen through the front porthole: wide eyes, small moustache.
- **Outfit:** big polished brass diving helmet with portholes; faded tan canvas diving suit; black rubber gloves; brass-and-lead diving boots.
- **Motion:** slow clanking steps on land. In water he sinks instead of swimming (a fun exception to test; can be cosmetic-only if it breaks balance).
- **Preset:** Body Average · Head Sully · Headwear Diving helmet (hides hair and beard) · Top Diving suit (one-piece, hides Legs) · Boots Diving · Gloves Diver rubber.

## Notes from making the sheets
- Grok's `image_edit` copied the reference body too strongly: Rani came out as a Bruno clone twice (kept as `img/raw/turn-rani-v1.png` and `-v2.png`). The fix was a text-only `image_gen` with the shared style block. Lesson for any later cast work: **body shape leads the prompt**, and the style reference must be "style only".
- Grok invents text (Bruno v1 had "CREW" on the armband) and a corner watermark. The final sheets in `img/` have the watermark corner painted out; raw originals are in `img/raw/`.
