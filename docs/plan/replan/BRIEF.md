# Port Boss re-plan, 2026-10-31 (Manan's plan GO 0afc733b)

## Why
Manan: "there is no story, and the current animations, the whole look and the whole character feel wrong." He wants:
- 6 art directions, each shown WITH OUR Toon Harbor ships, vehicles and props, so he can see which one fits the pack;
- 6 character variants (editable later in the character editor);
- a story, an objective and features.
Then Kufu writes ONE full plan page that Manan and Kufu agree on. This pass is PLANNING: nothing gets merged into the game.

## Read first
- `C:\Users\asus\Desktop\port-boss\HANDOVER-MANAN.md`: the game today (repo `C:\Users\asus\Desktop\port-boss`, main a6fe3d4).
- `C:\Users\asus\Desktop\Port Boss\plan\`: friend-plan.md, critic-market.md, critic-design.md, critic-production.md, refine-brief.md, music-brief.md, art/.
- Vault `C:\Users\asus\panoptes-vault\projects\port-boss.md`: every decision Manan has made so far.
- Current release build: `C:\Users\asus\Desktop\Port Boss\builds\20261030-2346-REFINE\PortBoss.exe`, plus proof shots in `Desktop\Port Boss\proof\refine\merged\`.

## Decisions that stand (Manan)
- Co-op physical port-manager game on Steam, price $6.99, working name "Port Boss".
- Ships physically leave on a clock. Stack/wobble/throw, ragdoll, water. The automation "watch it run" ending stays but comes late (8–10 h).
- Stylised, not realistic. Earlier attempts at realism made it worse.
- Ships, vehicles and props come from the Toon Harbor Pack (paid, already owned). Characters are OUR OWN, never the pack's.

## HARD rules
- **NO MONEY until Steam Early Access.** No paid assets, tools, credits or services. Free, CC0 and own-made only. The Toon Harbor pack is fine. Image generation: use the Grok CLI (Manan's existing subscription, no extra spend), for concept/direction only, never shipped as game art.
- The Toon Harbor pack is PAID and the repo is PUBLIC. Never commit Assets/ThirdParty or Assets/Toon Harbor Pack, or any render that shows it, to the repo. Renders live under `Desktop\Port Boss\plan\replan\` only.
- Memory: the PC is shared with another heavy test today.
  - Before Unity or Blender: free RAM >= 5 GB, and at most 1 Unity instance running for this pass.
  - Close what you open.
  - Avoid stealing focus on Manan's screen (batchmode or minimised where possible).
- Judge visuals on LOOKS, not "it works".
- Web research is allowed freely (Manan). Cite sources with links and dates. Verify numbers on Steam/SteamDB pages, never from memory.

## Output
Everything goes into `C:\Users\asus\Desktop\Port Boss\plan\replan\<worker>\`, with a README.md that summarises it. Images are PNG/JPG, at most 1920 px on the long side. Then report_done with the paths.
Kufu builds the final plan page from your outputs, so be concrete and visual.
