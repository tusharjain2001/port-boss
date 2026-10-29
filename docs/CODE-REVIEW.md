# Port Boss: code review (refine pass, 2026-10-30)

Scope: every script in `Assets/Scripts` (about 10,200 lines) at `main` 4cf4ed8. Paths are relative to `Assets/Scripts/`
and line numbers are from 4cf4ed8.
Severity levels:
- **bug**: wrong behaviour today.
- **risk**: breaks under a reachable condition (co-op timing, a locale, a missing asset).
- **quality**: costs time, memory or readability, with no wrong behaviour.

Status:
- **FIXED**: fixed on branch `panoptes/w-pb-review`.
- **LEFT**: not fixed; the reason is given.

Two areas are being rewritten by other workers, so their findings are listed here and not fixed:
- the character model, skin and animation (`CharAnim.cs`, the carry pose, swim/jump/ladder animation, `Player.AnimateWorker`);
- the water and lighting shaders.

## Top 10

| # | Where | Sev | What goes wrong | Fix | Status |
|---|---|---|---|---|---|
| 1 | `Game/Forklift.cs:194` | bug | A client who disconnects while driving takes the forklift with them. NGO despawns objects owned by a leaving client unless `DontDestroyWithOwner` is set, and `HasForklift` stays true, so the crew can never buy another. | Server sets `DontDestroyWithOwner`; the host frees the seat when the driver is no longer connected. | FIXED |
| 2 | `Game/Trolley.cs:146`, `Game/Pallet.cs:75` | bug | The next crate goes into slot `load.Count`. After one crate is taken from the middle (grabbed or sold), that slot is still occupied, so two crates overlap in the trolley or on the pallet. | Put the crate into the first free slot. | FIXED |
| 3 | `Game/Player.cs:954` | bug | `GrabRpc` takes the crate off the pallet's books before the distance check. A rejected grab (lag, more than 3.6 m away) leaves a crate that sits on the pallet but that the pallet no longer counts. It then won't spill, and the next crate is put in its place. | Do the distance check first. | FIXED |
| 4 | `Net/FacepunchTransport.cs:246,257` | bug | The server never calls `Close()` on a Steam connection that dropped, or on one whose `Accept()` failed. Facepunch requires that call, so every dropped client leaks a connection handle until the host quits. | Call `connection.Close()` in both places. | FIXED |
| 5 | `Game/Settings.cs:86` | bug | "Reset to defaults" runs `PlayerPrefs.DeleteAll()`, which also wipes the all-time career stats (`pb.stat.*`). | Keep the stat keys across the reset. | FIXED |
| 6 | `Game/GameUI.cs:216` + `Game/Player.cs:859` | bug | The same E press opens the buyer (in `Player.Update`) and closes it (in `GameUI.Update`). Depending on which script runs first, the panel either closes in the frame it opened or reopens in the frame it closed. | A frame guard on open and close. | FIXED |
| 7 | `Game/GameUI.cs:353` | bug | Pausing solo during the 1.4 s splash slow-mo: when the punch ends it restores `timeScale` to 1, so the game keeps running behind the pause menu. | Don't restore while paused. | FIXED |
| 8 | `Game/GameUI.cs:425` | bug | Hover sounds are keyed on the button label. With two buttons labelled the same ("Buy", "Sell", "‹  On  ›"), the "clank" sound plays on every repaint. | Key on the button rect. | FIXED |
| 9 | `Game/PortGame.cs:216,377` | bug | `DayVerdict()` reads `worstToday`, which only the host tracks. Clients never get the storm verdict, so the host's and the client's end-of-day cards differ. | Sync it as a `NetworkVariable`. | FIXED |
| 10 | `Game/AutoRun.cs:25` | bug | Any `-pb-` switch turns on test mode, including look and transport switches such as `-pb-force-steam` and `-pb-ownart`. Test mode auto-sells at the buyer, skips the intro and never locks the cursor, so a real co-op test with `-pb-force-steam` doesn't play like the game. | Leave the look and transport switches out. | FIXED |

## Bugs

| Where | What goes wrong | Fix | Status |
|---|---|---|---|
| `Game/Boat.cs:287-298` | Two stowaways thrown in the same check start two `Heave` coroutines. The second one saves the captain's already-turned rotation and "restores" it, so the captain is left facing the first stowaway for good. | Save the captain's resting rotation once and restore that. | FIXED |
| `Game/HarbourMap.cs:921` (RivalTraffic) | `Water.T` jumps from `Time.time` to server time when a session starts. A freighter can skip straight into its docked window, which fires `Docked` (Marlowe's brag and fireworks) for no reason. Speedboat already guards against this jump. | Ignore dockings in a frame where the clock jumps by more than 2 s. | FIXED |
| `Game/HarbourMap.cs:823` (RoadTraffic.Sync) | `TrafficRpc` is unreliable and `Sync` doesn't check a packet's age. A late, older packet pulls the lorries backwards, and they snap if the gap is over 8 m. | Drop packets older than the last one applied. | FIXED |
| `Game/HarbourMap.cs:290,500-508` | Every crate-store rebuild (each upgrade, and every new session) makes a new 1024 px MSAA sign RenderTexture and never frees the old one. The mipmapped path calls `Release()` without `Destroy`, which leaks the source RT for the MARLOWE sign and the 5 banners. | Destroy the old sign texture on rebuild, and destroy the source RT. | FIXED |
| `Game/AutoRun.cs:1044` | `-pb-shop` writes `g.Money` without the null and `IsServer` check the line above has. | Add the guard. | FIXED |
| `Game/AutoRun.cs:607-621` | `-pb-contracttest` only logs values and can't fail, although the handover says it "checks that day 3 is Bjorn's". Its comment describes the removed booking flow. | Log PASS/FAIL and fix the comment. | FIXED |
| `Game/AutoRun.cs:1010,1036` | `TrolleyTest` spawns the trolley from the prefab and skips `SpawnTrolley`, so its `hasTrolley=` log is always False. | Log whether the trolley is still spawned. | FIXED |
| `Game/AutoRun.cs:350` | `PushTest` reads `Trolley.I` without a null check (`HandleTest` has one). | Fail with a log line instead of throwing. | FIXED |
| `Game/GameUI.cs:1044` | The shop says the forklift drives "with W/S, lift with E". The lift is Shift/Q, and E gets out. | Correct the text. | FIXED |
| `Game/GameUI.Settings.cs:72` + `GameUI.cs:217` | Esc in Pause > Settings unpauses but leaves `settingsFromPause` set, so the next Esc opens Settings instead of the pause menu. | Clear it in `SetPaused(false)`. | FIXED |
| `Game/Player.cs:777` | The stack's walking bob uses `walkPhase`, which only the old primitive dockhand advances. With the pack worker (the shipped look) it is always 0, so walking never rocks the stack. Fixing it changes how hard balancing is. | Manan decides whether the stack should bob; then drive `walkPhase` from the gait. | LEFT (gameplay change) |
| `Game/Settings.cs:18-26` | On first launch `Preset` shows `AutoPreset()` (Low on small GPUs), but every setting defaults to High and `ApplyPreset` never runs. The menu says "Low" while the game renders at High. | Run `ApplyPreset(AutoPreset())` on the first run. | LEFT (changes what low-end PCs render; Manan's call) |
| `Game/GameUI.Settings.cs:153` | With the Custom preset, the cycle row clamps to index 3 and shows "Ultra". | Label it from `PresetNames`. | LEFT (minor; the settings UI may be redone with the character editor) |
| `Game/GameUI.cs:447` | `Session.Status` is only drawn on the Join screen, so "Disconnected from host." and "Couldn't host…" are never seen. | Draw it on the menu too. | LEFT (new UI: needs a look in a window) |
| `Game/GameUI.Menu.cs:65-68` | If the pack is in but `cont1` is missing, `Logo()` leaks one GameObject on every OnGUI event. | Check `Skin.Get("cont1")` before creating the root. | FIXED |

## Risks

| Where | What can go wrong | Fix | Status |
|---|---|---|---|
| `Game/HarbourMap.cs:429`, `World.cs:294`, `HarbourMap.cs:307` | Shop items now arrive any time of day, not just at night. The stage-2 shed, the new racks and the chiller colliders can appear on top of a player, a crate or the trolley and trap them. | Design call: push bodies out, or build only at night. | LEFT (behaviour) |
| `Game/HarbourMap.cs:770-780` | Clients decide "held" for lorries themselves, from player positions that lag. A client can hide a lorry the host is still driving, then snap it back. | Let only the host decide `held`. | LEFT (traffic determinism; needs a co-op playtest to check) |
| `Game/GameUI.cs:913` | A client who joins during the night gets credit in Stats for the crew's whole day. | Count it only if this peer saw the Day phase. | LEFT (minor) |
| `Game/GameUI.cs:232` | A client who joins mid-day sits through the 44 s intro while the host's day clock runs. | Show the intro to joiners only at `DayT == 0`. | LEFT (design call) |
| `Game/Gulls.cs:137` vs `PortGame.cs:241` | Each peer times the gull's "fly off with a fish" moment itself (6 s plus the flight), while the host applies the penalty at a fixed 7 s. | One shared constant. | LEFT (cosmetic) |
| `Game/AutoRun.cs:37-58` | `-pb-quit`, `-pb-timescale`, `-pb-shots` and `-pb-night` parse with the current culture, so on a comma-decimal locale "0.5" breaks. | Parse with `CultureInfo.InvariantCulture`. | FIXED |
| `Game/AutoRun.cs` (14 places, e.g. 181) | Tests hand control back with `BotInput = () => new In()`, so in a windowed run the human can't move after a test ends. | Set it to `null`. | LEFT (only affects windowed test runs; the visuals worker is editing AutoRun) |
| `Editor/PortBossBuild.cs:53-54` | The `Land` and `Spark` materials have no fallback. If `Shader.Find` returns null, `Setup()` throws and builds no scene. | Fall back like Water does. | LEFT (shaders: other worker's area) |
| `Editor/PortBossBuild.cs:349` | Prefab hashes are set by calling `NetworkObject.OnValidate` through reflection. If NGO renames it, the hashes stay 0 and host/client prefabs mismatch, with no error. | Log an error if the method is missing. | FIXED |
| `Net/FacepunchTransport.cs:152` | A failing send logs a warning for every packet, every frame, which floods the log while a connection dies. | Log once per client and result. | FIXED |
| `Net/Net.cs:34` | `_ = JoinLobby(l)` throws away the task, so a failure in a friend-invite join is never seen. | Log failures. | FIXED |
| `Game/GameUI.cs:104` | `GetComponent<…>() ?? AddComponent` uses `??` on a Unity object, which never adds the component in the Editor. | Use `TryGetComponent`. | FIXED |

## Quality: performance (per frame)

| Where | Cost | Fix | Status |
|---|---|---|---|
| `Game/Crate.cs:223-230` | Every crate, every frame: `Art.Hex("#9a9a88")` parses a string, and `ch.name` allocates a string for each child of the visual (about 20 per crate, 30+ crates). | A static colour, and the slats cached in `Awake`. | FIXED |
| `Game/Trolley.cs:180`, `Game/Pallet.cs:87` | `Physics.OverlapBox` allocates an array on every physics tick. | `OverlapBoxNonAlloc` into a shared buffer. | FIXED |
| `Game/GameUI.cs` (OnGUI, ~40 places) | A `new GUIStyle` per draw call, `OneLine` allocates a style and content each call, and gameplay queries run per OnGUI event: `SellableCrates()` (a new List and a sort), `FindCrate()` twice, `BuyerLine`. OnGUI runs at least twice a frame. | Cache the styles, and compute the hint and buyer list once in `Update`. | LEFT (big diff in a file the character-editor worker will touch; do it after the merge) |
| `Game/Settings.cs`, `Game/Stats.cs` | Every Settings getter is a PlayerPrefs read; `KeyFor` concatenates `"key."+id` about 8 times a frame; `Stats.Walk` writes PlayerPrefs every metre walked. | Cache in static fields. | LEFT (small cost; changes save timing) |
| `Game/GameUI.Menu.cs:93-103` | The logo camera renders a 2048 px, 8x MSAA RT every frame for the whole session, gameplay included. | Enable it only on the menu. | FIXED |
| `Game/GameUI.Settings.cs:106` | Dragging a slider runs `ApplySettings` and `PlayerPrefs.Save()` (a disk write) on every OnGUI event. The comment says "cheap". | Save on Back only (Back already saves). | LEFT (low value) |
| `Game/Water.cs:77-85` | The CPU rewrites 58k vertices and recalculates normals every frame. It is the biggest per-frame cost in the scenery code. | Displace in the shader. | LEFT (water: other worker's area) |
| `Game/Player.cs:388`, `Pallet.cs:69`, `Trolley.cs:138` | `foreach (Transform c in transform)` allocates an enumerator every frame per player and per call. | Index with `GetChild`. | LEFT (tiny) |
| `Game/Intro.cs:16` | The `Keys` property allocates a 10-entry array every `LateUpdate` while the intro plays. | Build it once. | LEFT (only during the 44 s intro) |
| `Game/HarbourMap.cs:48-67` | Every start, release builds included: ~1000 `RaycastAll` calls plus a log line per flagged prop (the map check). | Only in debug builds. | LEFT (start-up time only) |
| `Game/AutoRun.cs:1067` | Every 5 s, three `FindObjectsByType` scans skew the fps numbers in test runs. | Run them less often. | LEFT (test-only) |
| `Net/FacepunchTransport.cs:235` | A new `byte[]` per received message. | `ArrayPool`. | LEFT (Steam path untested; not worth the risk now) |
| `Game/AutoRun.cs:1076,1086`, `Editor/ToonHarborTools.cs:33,40` | RenderTextures are released but never destroyed. | `Destroy(rt)`. | FIXED (AutoRun) |

## Quality: dead code (contract board, night shop, older)

| Where | What | Status |
|---|---|---|
| `Game/PortGame.cs:63,87,131` | `ContractId` is a NetworkVariable that always equals `CaptainFor(DayNum)`, left over from the contract board. `Today` now comes from `DayNum`. | FIXED |
| `Game/PortGame.cs:25` | `Tomorrow.first` is never read. | FIXED |
| `Game/GameUI.cs:42` | `lastEarned` is never used. | FIXED |
| `Game/World.cs:415-429,437` | `RivalPortSkinned` can't run, because `RivalPort()` is only called when `!Skin.On`. | FIXED |
| `Game/Skin.cs:15,26,27` | `ToonWater`, `Shed` and `Stall` are never read; `Containers`, `Cranes`, `Freighters` and `Warehouses` were only used by `RivalPortSkinned`. | LEFT (serialized fields: removing them needs `Skin.asset` regenerated, and the character worker is changing the skin) |
| `Game/AutoRun.cs:1044-1049`, `tools/README.md` | `-pb-shop` says "at the night card"; the shop works any time now. | FIXED (comment and README) |
| Comments: `GameUI.cs:11` ("night shop"), `HarbourMap.cs:420` ("bought at night"), `AutoRun.cs:547,607,623`, `HarbourMap.cs:619,656`, `Settings.cs:29` ("0 = unlimited": it's index 4), `GameUI.cs:332` (slow-mo is "solo"; really any host alone) | Comments that are wrong. | FIXED |

## Quality: duplicated numbers that drift

| Where | What | Status |
|---|---|---|
| `Crate.cs:25`, `GameUI.cs:1085`, `HarbourMap.cs:575` | The crate prices 15/12/4 appear three times. The market board is also wrong on Bjorn's days (×1.25). | FIXED: shared `Crate.PriceFresh/Okay/Spoiled` (same values). The Bjorn-day board is LEFT: making it change by day is a visual change |
| `PortGame.cs:166` vs `AutoRun.cs:562` | The cargo formula is copied, and the copy has already drifted (it's missing `nextBoat*3`). | LEFT (test log only) |
| `GameUI.cs:1039-1043`, `PortGame.cs:56` | "6 crates", "18 (now 12)" and "24" are written by hand instead of from `Trolley.Capacity` and `Storage.PerRack`. | FIXED (same text, now built from the constants) |
| `Storage.cs:16`, `World.cs:300`, `HarbourMap.cs:278` | Shelf heights appear in three files. | LEFT |
| `PortGame.cs:222` (InShed), `HarbourMap.cs:471`, `World.cs:309` | The shed footprint appears three times. | LEFT |
| `Boat.cs:19` `DockSeconds = 120` vs `PortGame.Contracts[0].DockSeconds` | The fallback copies Mags' dock time. | LEFT |
| `AutoRun.cs` (bot waypoints, gloat window, quay edges) | Copies of private constants from World, Boat, Speedboat and BannerPlane. | LEFT (test-only; the visuals worker is editing AutoRun) |

## Quality: structure

| Where | What | Status |
|---|---|---|
| `Game/Player.cs` (1158 lines) | Movement, swimming, carrying, the trolley, the forklift, the camera and two character rigs in one class. Split it into partials: `Player.Move`, `Player.Cargo`, `Player.Swim`, `Player.Camera`, `Player.Look`. | LEFT. The animation and character workers are editing `Player.cs` right now, and a split would conflict with every change they make. Do it after their branches merge (it's a pure move). |
| `Game/GameUI.cs` (1122 lines) | Bootstrap, lighting, input, HUD, buyer, night card, shop and stats in one file. Split into `GameUI.Hud`, `.Night`, `.Shop`, `.Lighting` partials. | LEFT, same reason: the character editor will land in GameUI. |
| `Game/HarbourMap.cs` (936 lines) | RoadTraffic (networked, deterministic gameplay), RivalTraffic, Mover and Slew live in the scenery file. Move them to `RoadTraffic.cs` and `HarbourTraffic.cs`. | LEFT (the visuals worker may edit HarbourMap; do it after the merge) |
| `Game/AutoRun.cs` (1231 lines) | About 45 test switches. The "place camera, LookAt, Snap" pattern is repeated ~15 times, `WaitGame` is inlined 4 times, and 11 tests log values without PASS/FAIL. | LEFT (test harness; the visuals worker is editing it) |
| `Rival.cs`, `Gulls.cs` | `Rival.cs` holds `BannerPlane` and `Fireworks`; `Gulls.cs` also holds `Drifter`. | LEFT (rename after the merges) |
| `AutoRun.cs:15`, `PortBossBuild.cs:357`, `ToonHarborTools.cs:24`, `GameUI`, `Net.cs:108`, `Art.cs:76` | Six copies of the command-line `Arg`/`Has` helper. | LEFT |

## Other workers' areas (listed, not fixed)

- **`CharAnim.cs`**: clean, and the graph is destroyed in `OnDestroy`. Note: `Arms()` runs in `CharAnim.LateUpdate` while `Player` sets `Carry`/`Reach` in `Update`. That's fine, but any new IK has to stay in `LateUpdate` after the Animator.
- **`Player.AnimateWorker`** (`Player.cs:138-207`): the knock-over pose is duplicated in `Animate` (the old dockhand).
- **`Player.Animate`** (`Player.cs:255-324`): the old primitive dockhand rig. It's still the fallback when the pack isn't imported, so don't delete it until the new character replaces both.
- **Water and lighting:**
  - `Water.cs:77-85`: CPU wave mesh, see the performance table.
  - `GameUI.cs:127` vs `GameUI.Settings.cs:42-46`: with the skin on, the fog distances are fixed at 300/1200, so the "view distance" setting only moves the far clip, and on Low and Medium the geometry cuts off before the fog ends.
  - `GameUI.Settings.cs:163`: the "Water style" setting does nothing when `Skin.On`.

## Not changed on purpose

- **No file splits:** see Structure. Three other workers are editing `Player.cs`, `GameUI.cs` and `AutoRun.cs` right now.
- **No gameplay or tuning changes:** the stack bob, the first-run preset, mid-day building, lorry `held` on clients and joiner intros are listed above for Manan to decide.

## How the fixes were tested

The Toon Harbor pack isn't on this PC (not in the main repo either), so every build here runs in the own-art fallback
(`[PBBuild] skin: Toon Harbor not in the project, using own art`). The comparison is therefore baseline main
(4cf4ed8, built from the same worktree) against this branch, both in the fallback:

| Check | Baseline main | Branch (last batch, a5dff85) |
|---|---|---|
| `tools/cc.sh` | ok | ok |
| build (`ubuild.sh -pbDev`) | ok, 0 errors | ok, 0 errors |
| reg2 exceptions (all runs) | 0 | 0 |
| reg2 solo bot sales | 0: the bot gets stuck at (-2.5, -23.6) on the way to the market in the fallback map | 0: same spot |
| reg2 LAN co-op | client joined, then stopped getting updates at t≈31 s and timed out at 61 s (no error logged) | host clients=2 to the end, client in step at 146 s |
| `-pb-handletest`, `-pb-pushtest`, `-pb-evicttest` | PASS | PASS |
| `-pb-contracttest` | logs only | PASS (day 3 = Bjorn, 90 s) |
| `-pb-trolleytest`, `-pb-forklifttest`, `-pb-storeshots` | values | same values |

Open observations for whoever has the pack:
- Re-run reg2 with the pack in. The "solo ~6 / co-op ~9 crates" numbers can't be reproduced in the fallback.
- The baseline co-op stall at t≈31 s happened once on main and not on this branch's runs. It may be CPU contention (three
  workers testing at once). Worth one more look if it shows up again.
- `-pb-solo` binds 127.0.0.1:7777 and the LAN host binds 0.0.0.0:7777. Two worktrees testing at the same time make the
  co-op step fail with "address already in use". That's a test-environment collision, not a game bug.
