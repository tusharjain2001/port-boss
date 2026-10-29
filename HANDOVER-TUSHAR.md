# Port Boss: handover to Tushar

Hi Tushar. This file is everything you (and your Claude Code agent) need to pick Port Boss up exactly where we stopped on
29 Oct 2026. Most of it is for you; the last section, **"For Tushar's Claude"**, is written to your agent.

Other files worth knowing: `docs/HOW-TO-PLAY.md` (the game as a player sees it), `docs/PORT-STAGES.md` (the five port
stages), `docs/ART-PLAN.md`, `LOG.md` (one line per step of the overnight build), `HANDOFF.md` (the builder's own notes),
`tools/README.md` (build and test scripts).

---

## 1. Where we left off

**State:** `main` at commit `e6283b4`. The last playtest build ("MORNING", built from `4f61141`) is on Manan's PC; ask him
for a copy if you want to play before you build your own.

**Done and playable** (1–4 players, Unity 6000.5.6f1, URP, Netcode for GameObjects 2.13.3, Facepunch.Steamworks 2.5.2):
- A day at the berth: boats arrive on a clock and leave with whatever is still aboard; you carry and stack fish crates (wobble,
  throw, topple), sell at the fish buyer (press E, prices by freshness), with a night card and night shop (hand trolley, forklift
  + pallets from day 3, pier repair). Captains + a contract board, two berths from day 2, a day-1 JOB line.
- Harbour life: rival speedboat and wake, gull raids, crates knocking players over, crate damage, shove (F), swimming
  (you never sink), ladders.
- **Random weather** from calm to storm, host-seeded per session, no warnings by design.
- **Stage 2** (Concrete quay, $1500 at the night shop) is buildable; stages 3–5 are HUD goals only.
- The **Toon Harbor** look: a big original harbour map built from pack pieces, pack dock workers animated with CC0 clips,
  themed menu (title flyovers, cargo-manifest menu, container logo), 5-tab settings, gamepad support.
- Co-op over **LAN** passes (host + client, both selling, stage 2 syncs). Full scripted test battery green on the MORNING
  build: bot, swim, wobble, storm, LAN co-op, carry 1/3/5, forklift, shove, stage, map check 0 flagged, 0 exceptions.

**Open items**
1. **75 s capture not made.** `tools/capture.sh` makes it (the game films itself with `-pb-film`), but it needs a visible
   game window for ~3 minutes, so it waits for a moment when nobody is using the PC.
2. **Steam co-op across two accounts is untested.** Same-account tests can't use Steam relay ("Invalid Connection": Steam
   can't connect an account to itself), so this needs two PCs with two Steam accounts. You and Manan are the obvious pair.
3. **"Later" list:** idea #1 hazard-modifier vote, #2 special crates (live/fragile), #5 tipping plank; also rope rescue,
   hired NPC workers, new harbour biomes, a clip/upload system. (#4 storm warning was rejected for good: weather never warns.)
4. **Stages 3–5** from `docs/PORT-STAGES.md`: 3 small crane + warehouse, 4 container berth, 5 mega port (the Toon Harbor
   demo harbour). Stage 5 needs static batching/LODs/culling first: the demo layout has ~47,000 renderers.
5. **Music:** only the night track is in (`Assets/StreamingAssets/Music/night.mp3`). The menu, day and rush slots are silent
   until Manan adds the other tracks. They're made with Suno (AI), so they must be disclosed on the Steam page.

**Known issues:** the pack's shader logs 3 "duplicate keyword" errors at build time (harmless, the build succeeds). The gull is
built in code because the pack has no birds. `steam_appid.txt` is 480 (Valve's free test app) until Manan pays for a
Steam app ID. "Port Boss" is still a working title.

**Rules we work by**
- **The Toon Harbor Pack is paid and this repo is PUBLIC: never commit any pack file.** It lives in
  `Assets/ThirdParty/ToonHarbor`, which is gitignored (so is `Assets/Toon Harbor Pack/`). Before every commit:
  `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` must print **0**.
  (We suggest making the repo private: you're the admin, Manan only has push access.)
- Never put a secret, token or password in the repo.
- Builds for players are **release builds only** (no `-pbDev`, no Development console).
- **Art direction = the Toon Harbor pack; its demo scene is the reference.** Stylised, not realistic PBR.
- **Weather is random, with no warnings or forecasts.**
- Tests run headless (`-batchmode`). Open a window only when someone has said it's OK.
- We work on branches and merge to `main` after review.

---

## 2. Setup checklist

Two kinds of step: **(you)** = Tushar by hand (sign-ins, clicking in Unity/Blender); **(Claude)** = your agent from the
command line. Your Claude has no computer use, so it can't click in Unity or Blender; everything it does goes through the
CLI or the two MCP servers.

1. **Unity Hub + editor 6000.5.6f1** *(you)*. Install Unity Hub, sign in, activate a Personal licence (batchmode builds need
   an activated licence too). Hub > Installs > Install Editor > **6000.5.6f1** (if it isn't listed, get it from Unity's
   download archive with "Install with Hub"). Windows Build Support (Mono) comes with the Windows editor; IL2CPP isn't
   needed. You can untick Visual Studio (we use VS Code).
2. **Git + Git LFS** *(Claude)*: `winget install Git.Git`, then `git lfs install`. Unity also needs `git` on the PATH: the
   MCP for Unity package is fetched from a git URL.
3. **Clone** *(Claude)*: `git clone https://github.com/tusharjain2001/port-boss.git` then `git lfs pull`. Check
   `Assets/Plugins/Facepunch.Steamworks/Facepunch.Steamworks.Win64.dll` is a real DLL (~570 KB), not a small LFS text
   pointer.
4. **First open** *(you or Claude)*: Unity Hub > Add > the clone folder, open it with 6000.5.6f1. The first open rebuilds
   `Library/` and downloads packages, which takes a while. Headless alternative, Claude:
   `"/c/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe" -batchmode -quit -projectPath "$PWD" -logFile first-open.log`.
5. **Toon Harbor Pack** *(you; Manan sends you the file)*: see section 3. Without it the game builds and runs in its older own-art look.
6. **Steam** *(you)*: install Steam and sign in. The game runs on test app **480** (`steam_appid.txt`, copied next to the exe
   by the build). Solo and LAN co-op work without Steam; Steam lobbies need the Steam client running.
7. **VS Code** *(Claude)*: `winget install Microsoft.VisualStudioCode`, then
   `code --install-extension ms-dotnettools.csharp`, `code --install-extension ms-dotnettools.csdevkit`,
   `code --install-extension visualstudiotoolsforunity.vstuc`. *(You)*: in Unity, Edit > Preferences > External Tools >
   External Script Editor = Visual Studio Code. (The project already has `com.unity.ide.visualstudio` 2.0.29.)
8. **Build tools for the scripts** *(Claude)*: a .NET SDK for `tools/cc.sh` (we used 9.0: `winget install Microsoft.DotNet.SDK.9`),
   ffmpeg for `tools/capture.sh` (`winget install Gyan.FFmpeg`), Python 3 + `pip install pillow` for `tools/pair.py`.
9. **uv / uvx** *(Claude)*: `winget install astral-sh.uv` (both MCP servers run through `uvx`).
10. **Blender 5.1** *(you install, Claude can too)*: we used **Blender 5.1.0** from blender.org (`winget install BlenderFoundation.Blender`
    gives the latest version; take 5.1 if a newer one misbehaves with the add-on).
11. **Blender MCP add-on 2.1.9**: *(Claude)* `uvx mcp-for-blender==2.1.9 install-addon` copies the add-on (`blender_mcp.py`,
    "MCP for Blender") into Blender's user add-ons folder. *(You)* Blender > Edit > Preferences > Add-ons > search "MCP for
    Blender" > tick it. Or *(Claude)*, headless:
    `"/c/Program Files/Blender Foundation/Blender 5.1/blender.exe" -b --python-expr "import bpy; bpy.ops.preferences.addon_enable(module='blender_mcp'); bpy.ops.wm.save_userpref()"`.
    With the add-on on, Blender starts its MCP socket on port **9876** when it opens (if not: View3D > N sidebar >
    MCP for Blender > start). If an older "Blender MCP" add-on is installed, disable it (it uses the same port).
12. **MCP for Unity 10.3.0**: the package is already in `Packages/manifest.json`. *(You)* in Unity: **Window > MCP for Unity**,
    transport **stdio**, **Start Session** (it listens on port **6400**). Do this each time you want your Claude to drive the editor.
13. **Register both MCP servers with your Claude** *(Claude)*:
    ```
    claude mcp add -s user UnityMCP -- uvx --from mcpforunityserver==10.3.0 mcp-for-unity
    claude mcp add -s user -e DISABLE_TELEMETRY=true -e BLENDER_MCP_DISABLE_TELEMETRY=true blender -- uvx mcp-for-blender==2.1.9
    ```
    (The MCP for Unity window can also register Claude Code for you. Use one or the other, not both.) Restart Claude Code;
    `claude mcp list` should show both connected while Unity (session started) and Blender are open.
14. **Verify** *(Claude, with Unity and Blender open)*: through Unity MCP, create a cube named `MCP_Test_Cube` at (0, 1, 0) in
    a new empty scene. Through Blender MCP, create a sphere named `MCP_Test_Sphere` at (3, 0, 1) and take a viewport
    screenshot. That's the same test we passed on 28 Oct. Then delete both, and don't save the scene or commit anything from it.

---

## 3. The Toon Harbor Pack (SICS Games, Unity Asset Store)

**How you get it:** Manan sends you the file himself, privately: **`Toon Harbor Pack.unitypackage`** (about 135 MB, pack
version 1.0.4).

> **Keep it private**
> - **Never commit it.** The repo is **public**: anything pushed is visible to everyone.
> - **Never upload it anywhere public**: no GitHub release or issue, no public drive or share link, no Discord/forum post.
> - Keep the file **outside the clone folder** (e.g. your Downloads folder), so it can't end up in a commit by accident.
> - Before every commit: `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` must print **0**.

**Where the code expects it:** `Assets/ThirdParty/ToonHarbor` (`PortBossBuild.TH` and `ToonHarborTools.Root`). Unity first
puts the pack in `Assets/Toon Harbor Pack/`. The `.gitignore` covers both folders so neither gets committed, but **only
`Assets/ThirdParty/ToonHarbor` switches the skin on.** If the pack is anywhere else, the build quietly uses the own art. The build
log (`Logs/pb/build.log` with `tools/ubuild.sh`) tells you which: `[PBBuild] skin: Toon Harbor ON, map library N/N` versus
`[PBBuild] skin: Toon Harbor not in the project, using own art`.

**Importing it (in Unity, with this project open)**
1. **Assets > Import Package > Custom Package...**, pick the `Toon Harbor Pack.unitypackage` Manan sent you, then **Import**
   (all). This only creates `Assets/Toon Harbor Pack/` holding two more packages and `_ReadFirst.txt`. The pack itself
   comes in the next step.
2. In the Project window, open `Assets/Toon Harbor Pack/` and double-click **`URP_Toon Harbor Pack_6000.0.32.unitypackage`**,
   then **Import** (all). **Not** the `BuiltIn_` one: this project is URP. Now `Assets/Toon Harbor Pack/` also holds
   `Animations`, `Models`, `Prefabs`, `Profiles`, `Scenes`, `Skybox`...
3. Move it to where the code looks, **inside Unity's Project window**: right-click `Assets` > Create > Folder, name it
   `ThirdParty`; drag `Toon Harbor Pack` into `ThirdParty`; rename it to **`ToonHarbor`**. You end up with
   `Assets/ThirdParty/ToonHarbor/Prefabs/...`. (Don't move it in Explorer: that leaves the `.meta` files behind and breaks the GUIDs.)
4. Refresh the skin: menu **Port Boss > Setup scene + prefabs** (or just build). The Console should show
   `[PBBuild] skin: Toon Harbor ON` and no `skin: missing ...` lines. Colour space is already Linear, as the pack's
   `_ReadFirst.txt` asks.

Headless alternative (your Claude): close Unity, then `bash tools/import-toon-harbor.sh "<path to the .unitypackage>"`.
It does steps 1–3 in batchmode: it pulls the URP package out of the file, imports it and moves it to `Assets/ThirdParty/ToonHarbor`.

**If it landed in the wrong folder**
- Still `Assets/Toon Harbor Pack/` (step 3 skipped): do step 3. The skin stays off until the folder is
  `Assets/ThirdParty/ToonHarbor` exactly (case and no space matter).
- `Assets/ThirdParty/ToonHarbor/Toon Harbor Pack/...` (dragged one level too deep): drag the inner folder's contents up a
  level, or delete `Assets/ThirdParty` and redo from step 1.
- Pink/magenta materials: the `BuiltIn_` package got imported. Delete the pack folder(s) in Unity and redo, importing only `URP_`.
- `ToonHarborTools.MoveToThirdParty` (the script's move step) logs `[TH] move: ...` with an error if
  `Assets/ThirdParty/ToonHarbor` already exists. Delete the old copy first.

**If the skin GUIDs don't match**
`Assets/PortBoss/Skin.asset` points at pack assets by GUID. The GUIDs come from the `.unitypackage` itself, and we checked
that the ones in `Skin.asset` match this exact file (v1.0.4), so a normal import keeps them. Check:
`grep -rl 23e6a78508c649044ba886e55b25d11d Assets/ThirdParty/ToonHarbor --include=*.meta` should print
`.../Models/Materials/Atlas_1A_D.mat.meta` (the toon material `Skin.asset` uses). If it prints nothing:
- Usually this means the pack was imported twice, or was already in the project somewhere else, so Unity gave the second copy
  new GUIDs. Delete **every** copy of the pack (search the Project window for `TH_Freighter`), delete `Assets/ThirdParty`, and
  import once more from step 1.
- Or the folder was moved outside Unity. Same fix: delete and re-import.
- Either way the build still works: every build re-runs `PortBossBuild.MakeSkin()`, which rebuilds `Skin.asset` **by path**
  from `Assets/ThirdParty/ToonHarbor`. Without building, run menu **Port Boss > Setup scene + prefabs**, or batchmode
  `-executeMethod PortBoss.EditorTools.PortBossBuild.Setup`. The only side effect is that `Skin.asset` then shows as changed in
  git: **don't commit it** (`git checkout -- Assets/PortBoss/Skin.asset`), and tell Manan, so both PCs stay on the same GUIDs.

**After importing:** `git status --short` must show nothing under `ThirdParty` or `Toon Harbor` (they're ignored). A build
*without* the pack also rewrites `Skin.asset` (with empty slots). Don't commit that either.

---

## 4. Assets outside the repo

We checked every asset path the code loads (`Assets/Art/...`, `Resources/Audio`, `StreamingAssets/Music`, fonts, the UAL1
animations) against git. **Everything the build needs is tracked, except the Toon Harbor Pack.** Manan's
`assets-incoming` folder has more (Quaternius characters, PBR textures, HDRIs, an own-made trolley and gull v2). None of it is
used: the trolley is built in code, the characters and realistic textures were dropped when the art direction changed, and the
gull in use (`Assets/Art/Own/gull.fbx`) is already in the repo. `Assets/CREDITS.md` now also lists the Quaternius UAL1 clips
(CC0) and notes the pack.

---

## 5. Building, testing, releasing

All in `tools/` (details and every `-pb-*` test hook: `tools/README.md`). Git Bash, from the repo root:

| Do | Command | Result |
|---|---|---|
| Compile check (seconds) | `bash tools/cc.sh` | `Build succeeded` or errors |
| Dev build | `bash tools/ubuild.sh "$PWD/Builds/dev" -pbDev` | `Builds/dev/PortBoss.exe`, log `Logs/pb/build.log` |
| Quick re-test | `bash tools/reg2.sh` | `Logs/pb/reg2/<test>/log.txt` + PNGs |
| Full regression | `bash tools/regress.sh` | `Logs/pb/regress/` |
| Release | `bash tools/release.sh` | `Builds/releases/<stamp>/` + HOW-TO-PLAY.md |
| Playtest package | `bash tools/morning.sh` | `Builds/releases/<stamp>-MORNING/` + screenshots + test logs |
| 75 s capture | `WINDOW=1 bash tools/capture.sh <exe> capture.mp4` | opens a game window |

The Unity path, output folders and log folder are variables at the top (`tools/env.sh`); override them from the environment.
Close the Unity editor before any batchmode run.

---

## For Tushar's Claude

You're picking up a Unity 6000.5.6f1 URP co-op game at `main` = `e6283b4`. You have no screen control. Work through the CLI,
the `tools/` scripts, and the `UnityMCP` / `blender` MCP servers (they only answer while Tushar has Unity, with its MCP session
started, and Blender open).

**Hard rules**
- **Never stage, commit or push anything from `Assets/ThirdParty/` or `Assets/Toon Harbor Pack/`** (paid pack, public repo).
  Before every commit run `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` and only commit if it prints 0.
  Never use `git add -f` on ignored paths.
- No secrets, tokens or personal server details in the repo.
- Release builds for anyone playing; `-pbDev` only for your own testing.
- Art direction: the Toon Harbor demo scene. Weather stays random with no warnings.
- Don't open windows unless Tushar has said it's OK: no windowed game runs (`WINDOW=1`), and don't launch the editor or
  Blender UI yourself.

**Setup you do** (section 2): steps 2, 3, 7, 8, 9, 13, the headless parts of 4, 10 and 11, the verification in 14, and
`tools/import-toon-harbor.sh <file>` once Tushar has the pack file from Manan (section 3; he tells you where he saved it). Ask Tushar for 1, 6, 12, and for opening Unity/Blender.
On a fresh clone, run the batchmode first open (step 4) before `tools/cc.sh`: it needs `Library/ScriptAssemblies`.

**Your loop**
1. Read `LOG.md`, `HANDOFF.md`, `docs/PORT-STAGES.md`, then the code in `Assets/Scripts/` (`Game/`, `Net/`, `Editor/`).
   The scene, prefabs, materials and skin are **generated by `Editor/PortBossBuild.cs`**. Change that code, never the
   generated assets by hand.
2. Edit, then `bash tools/cc.sh` (fast compile check).
3. `bash tools/ubuild.sh "$PWD/Builds/dev" -pbDev`. Check `Logs/pb/build.log` for `[PBBuild] build Succeeded` and whether the
   skin is ON.
4. `bash tools/reg2.sh` (or `regress.sh`). Every log should have 0 `Exception`s. Read the `[PB]` lines and look at the PNGs:
   judge how they *look*, not only whether they run.
5. Add a `-pb-*` hook in `Game/AutoRun.cs` for new features so they can be tested headless, like the existing ones.
6. One line per step in `LOG.md` (`time | step | result`), commit on a branch, and let Tushar review before `main`.
7. For a playtest: `bash tools/release.sh`, or `tools/morning.sh` for screenshots and logs too.

**Where things are:** build log `Logs/pb/build.log`; test runs `Logs/pb/<script>/`; builds `Builds/` (gitignored); game
player log `%USERPROFILE%/AppData/LocalLow/Port Boss/Port Boss/Player.log` when no `-logFile` is given.

**Next work, in order unless Tushar says otherwise:** the 75 s capture (needs Tushar's OK for a window), the two-account Steam
co-op test with Manan, then stage 3 (crane + warehouse) per `docs/PORT-STAGES.md`.
