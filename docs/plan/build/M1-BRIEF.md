# Port Boss NEW game, first build M1 = boat + fishing only (Manan, 31 Oct 2026: "start the first build now")

## Goal
The first playable build of the NEW fishing-first Port Boss: a small damaged skiff at grandpa's dock, open sea, walk the deck, and fish (cast → bobber → bite → minigame → catch → hotbar). Solo. It must FEEL good and LOOK good (Manan judges looks, not "it works"). Nothing else (no shops, no phone, no time-of-day system yet beyond a nice fixed Golden-hour light, no co-op yet).

## Read first
- `C:\Users\asus\Desktop\Port Boss\plan\gdd\MANAN-CHANGES-1031.md`: Manan's latest changes (they override the gdd).
- `gdd\fishing\fishing.md` §3 (the feel: cast, bobber, bite, minigame, catch; relaxing, a miss = lower grade, never a fail) and **§8 build order F1–F6**: those steps + their test checklists ARE this job. F6 = the build Manan tests.
- `gdd\systems\systems.md` §1.1 (hotbar 8 slots, fish stack 5), §5 roadmap (M0 foundations + M1 checklist), §4 tech, UI tokens + fonts (Fraunces + Zen Maru Gothic) and `systems\mockups\` 02-hotbar, 05-minigame, 06-catch.
- `plan\replan\feel\README.md` (why the old character/animation felt wrong + the feel spec) and `plan\replan\artdir\` (art direction = **Ponyo Coast**, Manan's pick #4; light and shadow change with time of day later).
- Repo `HANDOFF.md`, `LOG.md`, `README.md`.
- Vault `C:\Users\asus\panoptes-vault\projects\port-boss.md` (rules from 2026-10-31 down).

## Manan's changes that touch M1
- **Fish: 15 species in total, 3 per rod (5 rods), any fish anywhere (no zones), legendary only with the best rod AND best bait.** The roster is still being discussed with Manan, so make it **pure data** (one ScriptableObject/CSV: species, rod tier, bait set, price, size/weight). For M1 use a placeholder of the starter rod's 3 fish + a debug key to switch rod tier 1–5 so all 15 can be tested. Keep the bite 4–10 s random (MUST), better rods shorten it to 4–6 s and widen the green zone (fishing §6).
- **No HUD clock** (Manan: "not a designed clock, check time on the forecast"). Don't build the clock card.
- Notifications/alerts on the LEFT (MUST).

## How to work
- Repo `C:\Users\asus\Desktop\port-boss` (your worktree branch). The NEW game replaces the old one: build it as new scene(s) + code in the same project; reuse what is genuinely good (Gerstner ocean, buoyancy, walkable deck physics) and drop what felt wrong. Note in LOG.md what you reused.
- One step at a time (F0 boat → F1 … F6). Self-test each step against its checklist (screenshots/clips you look at yourself; judge looks). Keep a log `C:\Users\asus\Desktop\Port Boss\plan\build\m1-log.txt` (time, step, result).
- **Toon Harbor pack is PAID and the repo is PUBLIC:** never commit `Assets/ThirdParty` or anything from the pack, nor a render that shows it. Before every commit `git diff --cached --name-only | grep -ciE 'ThirdParty|Toon Harbor'` must print 0. If your worktree lacks the pack, link it in (junction), never copy it into tracked paths.
- Our own assets: Blender (free) + Mixamo. No purchases, no spend of any kind.
- Never pop up windows on Manan's PC: Unity in `-batchmode` where possible; test runs offscreen/minimised. One Unity instance at a time; keep RAM light.
- Before deleting ANY file or build folder: `tasklist` check and ask Kufu. Old builds in `Desktop\Port Boss\builds\` stay for now (deleting them needs Manan's approval card).
- Put the build in `C:\Users\asus\Desktop\Port Boss\builds\<yyyymmdd-hhmm>-M1\PortBoss.exe`.
- Commit on your branch as you go; when F6 passes your own checklist, merge to main, run the pack check, push, and report.

## Done =
report_done with: the exe path, a short screen recording or 6+ screenshots of cast/bobber/bite/minigame/catch/hotbar, the filled F1–F6 checklists, `-pb-fishtest` log showing bites within 4–10 s, the commit hash on main, and anything that still feels off. Message Kufu at F4 (minigame) with screenshots so we can course-correct early.
