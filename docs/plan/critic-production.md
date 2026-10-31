# PORT BOSS — Production / Tech Critic Review
*2026-10-28. Reviewer: production/tech critic. Reviewed: `friend-plan.md`. Reused and re-verified: Last Boat `research-engine.md`, `research-art.md`, `research-art-v2.md`, `game-design.md`.*

**Verdict in one line:** Buildable to a Steam page + public demo in 16 weeks **only with the cut list in §3**, on the Last Boat stack almost unchanged, for about **$420 total** (floor $100). The one thing that can kill it is networked physics of cargo on vehicles; §1 says how to make that problem mostly disappear by design, and week 1–2 is spent proving it.

---

## 0. Headline scores

| Score | /10 | Why |
|---|---|---|
| Feasibility in 3–4 months (to Steam page + playable demo) | **7** | 7 with the cut list, 4 as written (workers/automation, 5 stages, 3 side businesses, cars, cold storage, reputation, market, storms is a 9-month list) |
| Technical risk (10 = low) | **6** | Networked vehicles + stacked cargo for 4 players is genuinely hard. It becomes a 7–8 if carried cargo is never simulated (see §1.3) and ships are kinematic. Host migration is cut, not solved |
| Cost efficiency | **9** | Hard-surface props are the cheapest thing in 3D; every paid tool has a free fallback; no CCU bills |
| **Verdict** | **GO**, after a 2-week "box test" gate | If the box test fails at 150 ms simulated ping, rescope to "one vehicle per player, cargo is pallets not boxes" before writing anything else |

Compared with Last Boat this is a **cheaper and lower-risk** game to make with AI agents: boxes, containers, forklifts and cranes are hard-surface geometry (agents and AI mesh generators are good at it), no quadruped rigs, no gaits, no fur, no 30 species of AI. The physics comedy is in the same place (things falling over) but the objects are simpler.

---

## 1. Engine and netcode (confirm / change for THIS game)

### 1.1 Stack: keep Unity 6.3 LTS + URP, keep NGO, two changes

| Component | Choice | Status 2026-10-28 | Change vs Last Boat? |
|---|---|---|---|
| Engine | **Unity 6.3 LTS**, URP, Forward+ | Live, supported to Dec 2027 ([unity.com/releases/unity-6](https://unity.com/releases/unity-6)). Unity Personal free under $200k/yr | No |
| Netcode | **Netcode for GameObjects 2.x** (2.13.0 shipped July 2026; registry shows 2.11.2+ ([packages.unity.com](https://packages.unity.com/com.unity.netcode.gameobjects))). NGO 1.x is deprecated in the 6.3 editor ([discussions.unity.com](https://discussions.unity.com/t/netcode-for-gameobjects-1-x-deprecation-in-6000-3-editor/1674997)) | Host-authoritative physics | No |
| Steam layer | **Facepunch.Steamworks** + a Facepunch `NetworkTransport` for NGO | The community Facepunch transport is **not verified against NGO 2.x** ([Unity community transports](https://github.com/MidLevel/MLAPI.Transports)); it is ~300 lines and agents can patch or rewrite it in week 1 | **Change: verify in W1, fallback below** |
| Fallback netcode | **FishNet + FishyFacepunch** (free) | FishNet has built-in client prediction and `OfflineRigidbody` for physics ([FishNet docs](https://fish-networking.gitbook.io/docs/manual/guides/components/transports/fishyfacepunch-steam)); its collider prediction only supports primitive shapes, which is fine: all cargo is boxes | Only if the NGO box test fails |
| Voice | **Dissonance** $100/seat (v9.0.8, Oct 2025 build; [asset store](https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078)) | Steam Voice via Facepunch is the $0 fallback | No |
| Water | **Stylized Water 3** $49 (v3.2.5, Feb 2026, Unity 6 URP ([asset store](https://assetstore.unity.com/packages/vfx/shaders/stylized-water-3-287769))) | Crest ($50 sale / $100 list, v4.23.1 Jul 2026 ([asset store](https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-urp-141674))) only if real storm swell is needed | **Change: SW3, not Crest** (harbour water is calm; ships are kinematic, so no buoyancy queries needed) |
| Vehicles | Unity built-in **WheelCollider** for forklift/trucks, ConfigurableJoint for mast and crane arm | $0; NWH Vehicle Physics 2 ($64, v13.5 Feb 2025 ([asset store](https://assetstore.unity.com/packages/tools/physics/nwh-vehicle-physics-2-166252))) is overkill for 15 km/h forklifts | New |
| Rope | Rigidbody chain of 4–6 links + LineRenderer | $0. Obi Rope ($37, v7.1.1 Apr 2026 ([asset store](https://assetstore.unity.com/packages/tools/physics/55579))) is a particle sim; it does not network and costs CPU on 4 clients | New |
| Multiplayer testing | Multiplayer Play Mode 2.0 (main editor + 3 virtual players; needs 6000.3 ([docs](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html))) + NGO network simulator for latency | $0 | No |

**Why not Godot/Unreal:** unchanged from `research-engine.md`. **Why not Photon Fusion:** its physics prediction is the best, but CCU billing on a $5 game that might go viral is the wrong bet. Reconfirmed.

### 1.2 The real difficulty, ranked

| # | Risk | Severity | Why it bites |
|---|---|---|---|
| 1 | **Cargo stacked on a moving networked vehicle** | Critical | If the forklift is owner-driven on a client and the boxes are host-simulated, the boxes lag 1 RTT behind the forks and slide off on the client's screen. This is the exact "smoothing physics for clients was our longest struggle" R.E.P.O. problem, multiplied by stacking |
| 2 | **Many rigidbodies** (hundreds of boxes, 4 players, host CPU) | High | PhysX handles 500 sleeping boxes fine; it does not handle 500 awake boxes being replicated by NetworkTransform at 30 Hz. Bandwidth, not physics, is the limit |
| 3 | **Crane rope + hanging container** | High | Soft-body or long joint chains explode under a heavy load; a networked one explodes differently on each client |
| 4 | **Ships + water + buoyancy** | Medium | Only a risk if ships are physics. Make them kinematic and it disappears |
| 5 | **Host migration** | Medium | NGO has no host migration ([Unity staff](https://discussions.unity.com/t/host-migration-in-multiplayer-game-which-uses-lobby-relay-and-netcode-for-gameobjects/923073)). Distributed authority needs paid UGS. Building it yourself is 3+ weeks |
| 6 | **Steam P2P transport on NGO 2.x** | Medium | Unverified compatibility (see 1.1) |

### 1.3 De-risk by design (what to decide before week 1)

These rules remove most of risk #1–#4 without clever netcode:

1. **Carried cargo is never simulated.** The moment a box/pallet/container is on forks, on a hook, in a trolley or in hands, the host sets it **kinematic and parents it to the carrier** (NGO `NetworkObject` parenting, or a `carriedBy` NetworkVariable and local re-parenting). It moves with the carrier at zero cost. Drop = unparent, set non-kinematic, give it the carrier's velocity. The forklift can still "lose" cargo: a tilt or bump beyond a threshold triggers the drop. Comedy preserved, physics avoided.
2. **Pallet, not boxes.** A pallet is one rigidbody with 8 visual boxes. Boxes only exist as individual rigidbodies when thrown or spilled, and they **merge back into a pallet when stacked on one** (snap zone). Cap: 150 awake dynamic bodies, host-enforced; above the cap the oldest spilled box despawns with a puff.
3. **Vehicles are owner-authoritative** (client-owned NetworkTransform + NetworkRigidbody, interpolated on the others). Cargo on them is parented (rule 1), so remote viewers see the stack move with the vehicle, not behind it. Collisions between two client-owned forklifts are resolved on the host; expect some jank and leave it, it's the joke.
4. **Crane = 1 hook rigidbody on a ConfigurableJoint** with a spring, plus a LineRenderer for the cable. The hanging container is parented (rule 1). Swing is the spring, not a rope. Networked: only the hook transform.
5. **Ships are kinematic**, moved on a spline with a scripted bob and roll. Storm = bigger bob + wind force on loose cargo + a tie-down mini-game. No buoyancy solver. Stylized Water 3 handles the look.
6. **No host migration.** Autosave to the host's disk every 60 s and at every ship departure. Host quits = "Port closed for the day", everyone returns to lobby, the save is intact, anyone can host it next time (save file is shared via Steam P2P on join). This is what most friendslop games do.
7. **Project path without spaces**: `C:\dev\PortBoss` (IvanMurzak Unity-MCP gotcha).

### 1.4 Week 1–2 "box test" gate (must pass before any content)

Scene `Test_Net4.unity`, run with Multiplayer Play Mode + 4 built clients against Steam app 480, NGO network simulator at **150 ms RTT, 2% loss**:
- 4 players, 2 forklifts, 1 hand trolley, 1 crane, 200 boxes on 25 pallets, one kinematic ship moving on a spline.
- Pass criteria: carried stack shows **no visible slide** on any client; a dropped pallet settles within 2 s on all clients within 10 cm of the host position; host frame time < 8 ms physics with everything awake; bandwidth < 60 kB/s per client; one player can drive a car off a ship ramp (cheap to test now, decides whether "cars" survives).
- Also in W1: the Facepunch transport compiles against NGO 2.13 and two real machines connect over Steam relay. If not, 2 days to patch; if still not, switch to FishNet in W2, not later.

---

## 2. Art: a style AI agents can hit that is not a kids game and not an asset flip

### 2.1 Direction: "working port, stylised realism" (matches Manan's painterly-realism pick)

Keep the `research-art-v2.md` recipe (stylised PBR, 70% of the look from lighting/fog/LUT, light Kuwahara pass). Port Boss is a **better** fit for it than Last Boat because:
- Containers, pallets, forklifts, cranes, sheds are boxy hard-surface geometry. Agents can model them in Blender via MCP with primitives + bevel + boolean, and Meshy/Tripo handle hard-surface far better than animals.
- **Rust, grime and painted metal hide texture imperfections.** A slightly rusty 1990s-to-today port looks "real" with 1k textures from Poly Haven (CC0). Clean toy plastic is what makes Synty look like a kids game; dirt is cheap and reads adult.
- Readability for clips: colour-coded containers (the real-world Maersk-blue / Hapag-orange / Evergreen-green pattern, with invented brands) give the saturated accents against a grey-teal harbour without any extra work.

**What NOT to use:**
- **Synty POLYGON**: there is no Synty harbour/port pack anyway (searched; only Construction $24.99 and Pirate $49.99 are adjacent), and the look is the toy style Manan rejected.
- **Untouched asset packs mixed with custom models**: the asset-flip tell. If a pack is bought, every mesh goes through the same Blender post-pass (re-texture with the project material library, bevels, decals) before it enters Unity.
- Photoscan / Sketchfab realism mixed with stylised. Pick one texel density and one value range.

### 2.2 Concrete sources

| Need | Recommendation | Cost | Free alternative |
|---|---|---|---|
| Hero props (forklift, hand trolley, small crane, container crane, 3 ships, 2 trucks, car) | **Custom Blender via MCP**, hard-surface, 5–25k tris, one trim sheet + one 4-material library (painted metal, rust, concrete, wood). Concept turnarounds from Grok image gen | $0 | — |
| Containers, pallets, crates, fish boxes, fruit crates, logs | **Custom** (a container is a box with a decal atlas). Agents can generate 20 brand liveries from one atlas | $0 | — |
| Filler port set (bollards, fences, lamps, sheds, cones, barrels) | **ithappy "Port" pack**, 216 models, Unity/URP, $89 on the Asset Store (v2.1 Apr 2025, [listing](https://assetstore.unity.com/packages/slug/265400)) or $69 on sale on the dev site. Low-poly, so it **must** be re-textured through the pipeline, otherwise it drags the look toward toy | $89 (optional) | Poly Haven CC0 models + Kenney Watercraft Kit (CC0, 45 boats, [OGA](https://opengameart.org/node/60429)) + Quaternius LowPoly Ship Pack (CC0) |
| Workers / players | Mixamo humanoid rig + clips; body from Quaternius Universal Base Characters (CC0) or one Meshy humanoid; 4 player colours via hi-vis vests and hard hats | $0 | — |
| Textures | Poly Haven CC0 PBR (rust, concrete, corrugated metal, wet asphalt) | $0 | — |
| Water | Stylized Water 3 (calm harbour, foam on hulls and quays, rain ripples) | $49 | URP Shader Graph water written by an agent (worse foam, 2 days) |
| Weather | URP fog + rain particles + LUT; storm = darker LUT + wind + rain intensity | $0 | — |
| 2D key art / capsule | Grok (existing sub) for concepts, **final capsule from an in-engine render** with paint-over; no raw AI image on the capsule | $0 | Fiverr capsule $50–150 if the render looks flat |

### 2.3 Consistency rules (the asset-flip killer)

1. **Style bible by end of W2**: one reference board (8 images), one palette PNG, texel density (512 px/m), value range, bevel width, decal atlas.
2. **Golden scene by W3**: the pier, one fishing boat, 20 fish boxes, the hand trolley, storm lighting, the player. Manan signs off on a screenshot. Nothing else is modelled until he does.
3. **One master material set** (4 URP Lit materials + 1 decal shader). Every mesh uses these; no per-asset materials.
4. **Same Blender post-pass script on every mesh**: scale, pivot, bevel, AO bake into albedo, palette quantise, LOD0/1.
5. **Same post stack** on every camera: fog, LUT, light painterly pass (0.2–0.3), vignette. The post stack is 50% of the "not Synty" look.
6. **Reviewer agent** diffs every new asset screenshot against the golden scene before merge.

---

## 3. Scope: is it buildable? Cut list and roadmap

### 3.1 Honest answer
As written, no. The plan lists 5 cargo types, 6 unloading tiers, 5 port stages, 4 storage types, workers with wages, 3 side businesses, deadlines, spoilage, a market board, reputation, storms, 1–4 co-op, and an end goal. That is a 9–12 month game. **The first 16 weeks must ship stages 1–3, tiers 1–4, three cargo types, one storm, and online co-op.** The demo's job is to prove "carrying feels good → forklift feels great → crane feels powerful" and to make clips. The "you are the boss" automation payoff is a 1.0 feature and should appear in the demo only as a locked tease.

### 3.2 Cut list (demo, by W16)

| Keep (demo) | Cut to 1.0 | Cut entirely / replace |
|---|---|---|
| Stages 1–3: fishing pier → trade dock → cargo port | Stages 4–5 (container port, mega port) | Mega port as a separate map |
| Tiers 1–4: hands → hand trolley → forklift → small crane | Tier 5 container crane, tier 6 workers/automation | Automation as full AI pathing: replace with "conveyor belts + a hired worker that walks one fixed route" |
| Cargo: fish (spoils), wood (heavy, forklift), containers (small, crane) | Fruit + cold storage, cars | Cars: keep only if the W2 ramp test is fun for free; otherwise 1.0 |
| Storage: open ground → shed | Warehouse, cold storage | — |
| Ship deadlines, spoilage timer | Reputation stars | Replace with a simple contract board (3 contracts a day) |
| Market: 3 prices that change each morning | Full market with trends | — |
| One scripted storm (the trailer event) | Random storms with 20-minute warnings | — |
| Co-op 1–4 over Steam, voice | Host migration | Autosave + rehost (§1.3) |
| End goal A: rival counter visible ("Rival's port: $5,000,000"), capped at stage 3 for the demo | Buying the rival port, giant ship finale | Side businesses (fuel station, repair yard, restaurant) cut entirely until post-launch |
| Upgrades: 8 | 15 | — |

### 3.3 Steam facts that set the dates (verified 2026-10-28)

- **$100 Steam Direct fee per app**, recoupable (not refundable) at $1,000 adjusted gross revenue ([Steamworks app fee](https://partner.steamgames.com/doc/gettingstarted/appfee)).
- **30-day wait** between paying the fee and being able to release; onboarding paperwork + identity check takes **15–30 days**; page/build review **1–5 days**; Coming Soon page must be public **at least 2 weeks before release** ([Steam Direct](https://partner.steamgames.com/steamdirect), [coming soon](https://partner.steamgames.com/doc/store/coming_soon)). Submit the page for review at least 7 days before you want it live ([Steam launch guide](https://www.steampageanalyzer.com/blog/steam-launch-strategy)).
- A demo needs the **base game's page visible as Coming Soon**; the demo can have an "upcoming demo" listing before it is playable ([Steamworks demos](https://partner.steamgames.com/doc/store/application/demos)).
- **Next Fest Feb 2027**: fest 22 Feb–1 Mar; registration deadline **10 Jan 2027**; all items for review by **1 Feb**; press preview 11 Feb ([Steamworks Feb 2027](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/feb_2027)).
- **Next Fest June 2027**: fest 14–21 Jun; registration **25 Apr 2027**; items for review by 24 May; press preview 3 Jun ([Steamworks June 2027](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/june_2027)).
- **A game may appear in Next Fest only once.** Public store page + public demo required.

**Fest choice:** Feb 2027 is reachable on paper (demo for review by 1 Feb = end of W13) but the page would have only ~5 weeks of wishlists over Christmas. The Last Boat plan reached the same conclusion. **Recommendation: page live W7 (before Valve's holiday slowdown), public demo on the page W16, enter the June 2027 fest** (register by 25 Apr). This keeps the "page + demo in 3–4 months" promise and uses the one Next Fest shot with 5 months of wishlists behind it.

### 3.4 Week-by-week roadmap (W1 = Mon 2 Nov 2026)

| Week | Dates | Deliverable | Gate |
|---|---|---|---|
| **W1** | 2–6 Nov | Repo at `C:\dev\PortBoss`, Unity 6.3 URP project, Git+LFS, MCP servers installed, NGO 2.13 + Facepunch transport compiling, 2 real PCs connected over Steam (app 480). **Pay the $100 fee and start Steamworks onboarding/tax forms this week** (the 15–30-day paperwork + 30-day wait run in parallel) | Two machines in one lobby |
| **W2** | 9–13 Nov | **Box test** (§1.4): forklift, trolley, crane hook, 200 boxes, kinematic ship, 150 ms sim. Car-off-ramp test | **GO/NO-GO on physics approach.** Fail = FishNet switch or rescope to pallets-only |
| **W3** | 16–20 Nov | Style bible + **golden scene** (pier, boat, fish boxes, trolley, storm light, post stack). Player controller + grab/carry/throw | Manan signs off a screenshot |
| **W4** | 23–27 Nov | Stage 1 loop playable solo: ship arrives on spline, unload fish by hand, sell at the hut, spoilage timer, buy hand trolley. First 4-player session | "Carrying and selling feels good" (the plan's own test) |
| **W5** | 30 Nov–4 Dec | Stage 2: forklift (WheelCollider + mast), wood logs, pallets, shed storage, day/night + morning price board | 4-player playtest #2; count clips |
| **W6** | 7–11 Dec | Small crane (hook + spring), small containers, deadlines + penalty, contract board (3/day), upgrade shop (8 upgrades) | Playtest #3 |
| **W7** | 14–18 Dec | **Store page assets**: capsule from in-engine render, 5 screenshots, 2 GIFs, 60 s trailer from real captures, short description, tags, AI disclosure. Submit page for review 14 Dec; **Coming Soon live by ~18 Dec** | Page approved before Valve's holiday slowdown |
| **W8** | 21–25 Dec | The scripted storm: warning, tie-downs, wind on loose cargo, lost-cargo penalty. Rival counter UI. (Light week, holidays) | Storm is the trailer shot |
| **W9** | 28 Dec–1 Jan | Autosave/rehost, lobby, join-in-progress, Dissonance proximity voice, controller support | Playtest #4 at real latency (India ↔ friend abroad) |
| **W10** | 4–8 Jan | Polish pass on feel: forklift tilt-drop, crane swing, box spill sounds, VFX. Audio pass (ElevenLabs SFX + CC0) | Clip count ≥ 3 per session |
| **W11** | 11–15 Jan | Performance: 150-body cap, sleep tuning, LODs, bandwidth; 60-min no-crash soak with 4 players | Host physics < 8 ms |
| **W12** | 18–22 Jan | Demo build: stages 1–3, 45–60 min, ends on the "Rival laughs / container crane locked" tease. Steam Playtest (private) wave 1 | 20+ external players |
| **W13** | 25–29 Jan | Fix wave 1; localisation (EN + Simplified Chinese text); Steam build pipeline (SteamPipe, batchmode) | Crash-free 60 min |
| **W14** | 1–5 Feb | Demo app page submitted (upcoming demo listing), achievements (5), Steam Input | Demo page approved |
| **W15** | 8–12 Feb | Playtest wave 2 (100+), weekly TikTok/Shorts clips have been running since W5 | — |
| **W16** | 15–19 Feb | **Public demo live on the Coming Soon page** (Mon 15 Feb 2027). Streamer 4-key packs wave 1 | **Milestone: page + demo, 16 weeks** |
| W17–24 | Feb–Apr | 1.0 content: stage 4 container crane, workers (fixed-route), fruit + cold storage, cars, random storms, reputation. Register June Next Fest by **25 Apr** | — |
| W25–33 | Apr–Jun | Fest demo refresh (submit by 24 May), **Next Fest 14–21 Jun 2027** | 10k wishlists before fest |
| W34–38 | Jun–Jul | Launch 1.0 after the Summer Sale, ~22 Jul 2027 | — |

Every playtest gate is Manan plus 3 humans. Agents cannot certify fun.

---

## 4. Costs (USD, itemised, with the free alternative)

| Item | Paid pick | Cost | Free alternative | Verdict |
|---|---|---|---|---|
| Steam Direct fee | Required | **$100** (recoupable at $1k AGR) | none | Pay W1 |
| Engine | Unity 6.3 LTS Personal | $0 (Pro $2,200/seat/yr only above $200k/yr) | — | — |
| Netcode + Steam | NGO 2.x + Facepunch.Steamworks | $0 | FishNet + FishyFacepunch $0 | — |
| Voice chat | Dissonance (1 seat) | **$100** | Steam Voice via Facepunch, DIY positional | Buy in W9, not W1 |
| Water | Stylized Water 3 | **$49** | Shader Graph water by agent | Buy in W3 |
| Vehicle physics | Unity WheelCollider | $0 | — (NWH VP2 $64 not needed) | Skip |
| Rope | Joint chain | $0 | — (Obi Rope $37 not needed) | Skip |
| Buoyancy | Kinematic ships | $0 | — (Dynamic Water Physics 2 $39 not needed) | Skip |
| 3D generation | Meshy Pro ~$20/mo × 4 | **$80** | Hand-model in Blender via MCP (hard-surface is tractable); Hunyuan3D self-host | Optional; many props are faster to model than to generate |
| Filler prop pack | ithappy Port pack | $89 (optional) | Poly Haven CC0 + Kenney/Quaternius ship kits | Only if W5 shows the set-dressing backlog is slowing the builder |
| SFX + music | ElevenLabs Creator $22/mo × 4 ([pricing roundup](https://www.nocode.mba/articles/elevenlabs-pricing); indie-games licence terms apply, see `research-art.md`) | **$88** | Sonniss GDC bundles + Kenney audio (CC0) + Kevin MacLeod (CC-BY) | Buy from W10 only |
| Concept art / capsule | Grok (existing sub) | $0 | — | Fiverr paint-over $50–150 if needed |
| Trailer | OBS + DaVinci Resolve | $0 | — | — |
| MCP servers, Blender, VS 2022 Community, Git LFS | — | $0 | — | — |
| Claude Code | existing subscription | $0 extra | — | — |
| **Recommended total** | | **≈ $417** | **Floor: $100** (fee only) | Ceiling with every optional: ≈ $700 |

Note: `research-engine.md` quoted Dissonance at ~$110; the store page now shows $100/seat (cached snapshots disagree, so budget $110).

---

## 5. MCP servers, agent setup, guardrails

### 5.1 Install (all free)
| Tool | Version checked | Install | Use |
|---|---|---|---|
| **CoplayDev MCP for Unity** | v10.3.0 (2026-10-04) ([repo](https://github.com/CoplayDev/unity-mcp)); pin the tag, v10 had breaking changes | Python 3.10+ & `uv`; Package Manager git URL `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0`; Window > MCP for Unity > Configure | Primary builder: scenes, prefabs, scripts, tests, builds |
| **IvanMurzak Unity-MCP** | 4.5k stars ([repo](https://github.com/IvanMurzak/Unity-MCP)) | `npm i -g unity-mcp-cli`; project path **without spaces** | Reviewer: play-mode, console logs, game-view screenshots, `tests-run` |
| **MCP for Blender** (ahujasid) | 32.5k stars ([repo](https://github.com/ahujasid/blender-mcp)) | `winget install astral-sh.uv`; `uvx mcp-for-blender setup`; `DISABLE_TELEMETRY=true`; localhost only | Hard-surface modelling + headless `blender -b --python` batch post-pass |
| Unity CLI | official (`unity mcp` mode) | optional | batchmode builds/tests |
| Multiplayer Play Mode 2.0 | needs 6000.3 | Package Manager | 4-player in-editor |
| Facepunch.Steamworks + Steamworks SDK | — | NuGet/GitHub + SteamPipe | lobbies, relay, uploads |
| Git + Git LFS + UnityYAMLMerge | — | — | scenes as Force Text YAML |

### 5.2 Agent topology

- **1 Builder** (Claude Code + Unity MCP + Blender MCP): owns the task list, works one task per branch, commits with a test scene result attached.
- **Reviewer A, code/netcode**: reads the diff, runs `Unity.exe -batchmode -runTests -testPlatform playmode` and the Multiplayer Play Mode 4-player smoke on `Test_Net4`; blocks merge on bandwidth/physics budgets.
- **Reviewer B, look**: takes `Test_Golden` + new-asset screenshots via IvanMurzak MCP, compares to the style bible; blocks merge on style drift (wrong material set, wrong texel density, raw AI texture).
- **Reviewer C, scope**: once a week, compares the task list to §3.2 and flags anything outside the demo scope.
- **Manan**: style sign-off W3, GO/NO-GO at W2, W4, W8, W12, W16 playtests, and **one approval per purchase**.

### 5.3 Guardrails
1. **One approval per purchase.** Every paid item in §4 is a separate `ask_manan` with price, link and the free alternative. No auto-buy.
2. **Git on every task**; LFS for binaries; `.gitattributes` with UnityYAMLMerge; no commits of `Library/`. Branch per task, squash on merge.
3. **Three permanent test scenes**: `Test_Net4` (physics + bandwidth budgets), `Test_Golden` (look), `Test_Loop` (ship → unload → sell → buy in under 5 min, solo). A PR that breaks any of them does not merge.
4. **No popup windows** on Manan's PC: builds and tests in `-batchmode`, play-mode screenshots via MCP, multi-client tests on built players launched minimised/offscreen.
5. **Budgets as code**: `GameBudgets.cs` constants (max awake bodies 150, max net objects 400, host physics 8 ms, 60 kB/s per client), asserted by `Test_Net4`.
6. **Secrets** (Steamworks credentials, ElevenLabs/Meshy keys) only in 1Password, read via `op` at use time; never in the repo or `.env`.
7. **AI disclosure**: keep a running `ai-content.md` so the Steam Content Survey is honest in W7.

---

## 6. Production answers to the plan's open questions

### Setting: modern vs old era
**Modern (a working, slightly rusty port, 1990s–today).** Cheaper and easier to make look good:
- Modern cargo is boxes: containers, pallets, crates. One trim sheet and one decal atlas cover 80% of the game. Agents model boxes well.
- Modern progression is the plan's own spine: hands → trolley → forklift → crane → automation. An old era caps the fantasy at a hand crane and a horse cart.
- Old era costs: sailing ships need rigging and sail cloth (cloth sim or baked animation), rope everywhere, **horses** (the quadruped rig/gait problem that made Last Boat expensive), barrels that roll (fun, but a worse "stack" than boxes). Period-correct props are also harder to find CC0.
- Rust, grime and wet concrete hide imperfect textures and read "adult". Toy-clean plastic is what reads as a kids game.
- If Manan wants charm, add it through the characters and signage (hand-painted shop signs, a tea stall, a port cat), not the era.

### End goal: which is cheapest?
| Option | Build cost | Fit |
|---|---|---|
| **A) Beat the rival** | Low: one counter on the HUD, a static rival port across the bay (set dressing, no AI), ~20 taunt lines on a notice board, one "buy his port" button at the end | Best for the demo: the counter is visible from minute one, which is what a Steam trailer needs |
| B) Pay back the loan | Lowest: a number and a timer, zero new content | Pressure from day one fights the "cozy" half of the pitch; also punishes 4-player groups that join late. Could be an optional "Hard" mode later |
| C) The giant ship | Medium: one bespoke hero ship model, a deep-water berth stage, one scripted multi-player unloading mission | Best trailer finale, but it depends on the container crane, which is a 1.0 feature |

**Recommendation:** **A for the demo and 1.0**, with **C as the post-goal set-piece** ("the rival's old clients send the biggest ship on the coast") because it reuses every system. Skip B.

### Which one to replace the sand-digging game with
Production-wise this is the better bet than Last Boat: simpler assets, no animal AI, simpler netcode (cargo is parented, not wrestled by four people at once), and a clearer end state.

---

## 7. Scores and verdict

| | Score |
|---|---|
| Feasibility in 3–4 months (page + public demo) | **7/10** with the §3.2 cut list; 4/10 as written |
| Technical risk (10 = low) | **6/10**; 8/10 if the §1.3 rules are adopted and the W2 box test passes |
| Cost efficiency | **9/10** (≈ $417, floor $100) |

**Verdict: GO.** Stack: Unity 6.3 LTS + URP, NGO 2.13 host-authoritative, Facepunch.Steamworks (FishNet fallback), Dissonance, Stylized Water 3, WheelCollider vehicles, joint-spring crane, kinematic ships, no host migration. Modern setting, end goal A. Page live ~18 Dec 2026, public demo 15 Feb 2027, June 2027 Next Fest. Nothing is built past week 2 until the networked box test passes at 150 ms.

---

## Sources
- Steam: [app fee](https://partner.steamgames.com/doc/gettingstarted/appfee) · [Steam Direct (30-day wait, 2-week coming soon, 15–30 day onboarding)](https://partner.steamgames.com/steamdirect) · [coming soon rules](https://partner.steamgames.com/doc/store/coming_soon) · [demos](https://partner.steamgames.com/doc/store/application/demos) · [Next Fest Feb 2027](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/feb_2027) · [Next Fest June 2027](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/june_2027) · [page review timing](https://www.steampageanalyzer.com/blog/steam-launch-strategy)
- Unity: [Unity 6.3 LTS](https://unity.com/releases/unity-6) · [NGO package registry](https://packages.unity.com/com.unity.netcode.gameobjects) · [NGO 1.x deprecation in 6.3](https://discussions.unity.com/t/netcode-for-gameobjects-1-x-deprecation-in-6000-3-editor/1674997) · [NGO physics / NetworkRigidbody](https://mp-docs.dl.it.unity3d.com/netcode/2.0.0/advanced-topics/physics) · [NGO host migration (none)](https://discussions.unity.com/t/host-migration-in-multiplayer-game-which-uses-lobby-relay-and-netcode-for-gameobjects/923073) · [Multiplayer Play Mode 2.0](https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@2.0/manual/index.html) · [community transports (Facepunch)](https://github.com/MidLevel/MLAPI.Transports) · [FishNet FishyFacepunch](https://fish-networking.gitbook.io/docs/manual/guides/components/transports/fishyfacepunch-steam) · [FishNet prediction colliders](https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/prediction/network-collider)
- Assets: [Dissonance](https://assetstore.unity.com/packages/tools/audio/dissonance-voice-chat-70078) · [Stylized Water 3](https://assetstore.unity.com/packages/vfx/shaders/stylized-water-3-287769) · [Crest URP](https://assetstore.unity.com/packages/tools/particles-effects/crest-ocean-system-urp-141674) · [NWH Vehicle Physics 2](https://assetstore.unity.com/packages/tools/physics/nwh-vehicle-physics-2-166252) · [Obi Rope](https://assetstore.unity.com/packages/tools/physics/55579) · [Dynamic Water Physics 2](https://assetstore.unity.com/packages/tools/physics/dynamic-water-physics-2-147990) · [ithappy Port pack (Asset Store)](https://assetstore.unity.com/packages/slug/265400) · [ithappy Port (dev site)](https://ithappystudios.com/environment/port/) · [Synty Construction](https://assetstore.unity.com/packages/package/168036) · [Kenney Watercraft Kit CC0](https://opengameart.org/node/60429) · [Quaternius ship pack CC0](https://opengameart.org/node/82831)
- Tools: [CoplayDev MCP for Unity](https://github.com/CoplayDev/unity-mcp) · [IvanMurzak Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) · [MCP for Blender](https://github.com/ahujasid/blender-mcp) · [ElevenLabs pricing roundup](https://www.nocode.mba/articles/elevenlabs-pricing)
- Prior research reused: `C:\Users\asus\Desktop\Last Boat\plan\research-engine.md`, `research-art.md`, `research-art-v2.md`, `game-design.md`
