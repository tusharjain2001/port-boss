# M5 build plan: Shops, upgrades and the wardrobe (pb-m5, Kufu t-0eef793f / t-fd579d85 / t-735d2e9c)

Spec: systems.md "M5" (scope + Done when + Manan checklist), §1.1.6 warehouse, §1.9 wardrobe, §4.8.2 paint channels;
economy.md §4.0 shop UI, §4.3 Doyle, §4.4 Plank & Post, §4.6 Hartley's, §8 Ask the crew; CHANGES row 3 (no placing).
Rule: never cut a spec part; new ideas go to `Desktop/Port Boss/proof/M5/SUGGESTIONS.md`, never built.

## Work order (tick when committed)
1. [ ] `Yard.cs` (pure C#, compiled by companycheck): the catalog (Doyle / Plank / Hartley items, prices from economy),
   company-owned items + set spots, overnight installs run at 07:00, paint per boat (4 channels), per-player outfit
   (preset + 6 slots + dye), owned clothes, Ask-the-crew requests (limit min($5,000, 25% of balance), any other
   player's OK or the host, 60 s auto-approve when nobody else is online, declined = nothing spent), warehouse tier +
   stacks (slot = 10 fish one species+grade, cold room rot 25%), Biscuit repairs/upgrades state. Save JSON. Tests in
   companycheck: concurrent buys from two players, Ask the crew, overnight installs, warehouse moves, invariants.
2. [ ] Shop UI for all shops (Shop.cs generalised): categories left, item cards, preview pane (live camera on the set
   spot / turntable), stat lines, price + "need $X more", hold E 0.6 s, "Ask the crew", ka-ching + confetti-lite.
3. [ ] Set spots (code-built Ponyo props: beds, lamps, furniture, warehouse T0-T2, office upgrades, berths, three planks)
   + pop-in squash/settle + 07:00 timelapse; set-spot validator (one spot each, no overlaps, navmesh connected,
   no walkway/berth blocked).
4. [ ] Paint: PB_Toon channel tint (uv2 channel id) via MaterialPropertyBlock, Biscuit + Kestrel tagged; Doyle's crew
   walk over and paint with visible progress (game-time driven, so co-op sees the same); paint channel test.
5. [ ] Warehouse T0 next to Walt's office from Day 1, T1/T2 installs overnight; storage UI (grid above hotbar, click /
   Shift-click / Store all fish / Take, pad D-pad + A); cold room slows rot.
6. [ ] Wardrobe: garment channels on the kit (Hat, Top, Over, Legs, Boots, Gloves) + dye; wardrobe screen (crew cards,
   turntable mirror camera, tabs, dyes, Reset to preset, Wear it) at any bed or the office locker; Hartley's shop;
   co-op uniqueness (two players can't both be Pip) + outfits synced.
7. [ ] Kestrel: model, second boat (multi-boat Skiff), walk aboard at Doyle's before buying, co-op.
8. [ ] Town NPC routines v1: shopkeepers at their counters in opening hours, home otherwise.
9. [ ] Biscuit repairs/upgrades with real effects (engine rebuild, hull patch, Engine I/II, bilge pump, deck lights...).
10. [ ] Unity tests (m5test), m4test/m4reg rerun, cooptest/sleeptest with -pb-netsim 150 2, stills, art-director passes,
   60-90 s film, release build + HOW-TO-TEST, proof/M5/README.md + SUGGESTIONS.md, LOG.md lines, vault, report_done x3.
