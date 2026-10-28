# HANDOFF (portboss-w1, overnight)

## Done (proven in builds)
- Day-1 slice: pier, boat (arrive/dock/rock/countdown/leave with cargo), crates (grab/stack 5/drop/throw/topple/float/spoil), trolley, night card + shop, day 2, menu/pause, Steam lobby co-op (same-PC verified; cross-account Steam relay untested).
- P1 wobble (balance model, visible bend, Steady!), P2 buyer panel. Last good build: Desktop/Port Boss/builds/20261029-0003 (pre-P1/P2!).

## Written, compiled with dotnet (scratchpad cc.sh), NOT yet run in Unity (Unity paused for Manan's game)
- P3: captain/buyer personality lines, speech banner with speaker, 90/60/30/10 marks + horn toot, cargo 8/11/14.
- P4: open sea NE (right headland cut at z~120), lighthouse, horizon islands, drifting Kenney ships, Kenney ships at the rival quay, boat path from the open sea (34 s sail).
- P5: Gulls.cs (circle/glide/land on perches/take off near players, flapping wings, calls).
- P6: PortBoss/Water.shader (ripples, fresnel sky, glint, depth tint, contact + crest foam), clouds, exp² fog, skybox tweaks, sky globals, URP asset: shadows 110 m/4096, MSAA 4x, HDR grading, SSAO stronger; wet posts.

## Queue for when Unity is free (in order)
1. ubuild dev; check log for shader errors ("PortBoss/Water shader missing" / shader compile errors).
2. Windowed bot run: screenshots of water/foam/sky/clouds/lighthouse/gulls/rival quay; check gull orientation (beak forward) + wing flap axis.
3. P3 shouts readable (speech banner) in screenshots.
4. Full-day 4x run: boats on the new path don't clip the lighthouse/rocks; cargo ramp ok.
5. Wobble test + buyer test regression; co-op 2-instance regression.
6. Release build -> Desktop/Port Boss/builds/<stamp>/ + HOW-TO-PLAY.md; message kufu per item (P3, P4, P5, P6).

## Next
- P7 character (waiting for assets-incoming/Characters), PBR textures + HDRI (waiting).
- Then backlog from overnight-brief.md (co-op first).
