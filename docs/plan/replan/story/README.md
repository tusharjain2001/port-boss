# Port Boss re-plan: story, objective, features (pb-story, 2026-10-31)

## Executive summary
- **The problem.** Today the game has a setting and a villain (the intro, Marlowe's taunts, two captains) but **no
  arc**. Nothing changes between day 2 and day 20 except money, no character wants anything, and nothing says what
  you're winning.
- **Recommended premise: A, "Win the Coast"**, kept as a light frame, not a plot (the market report shows heavy
  story hurt Docked, the closest setting comp). *Steal the coast from a tycoon, one captain at a time.* Every
  captain on the coast docks at Marlowe Port. Unload their boats well and they switch to you: their **flag goes up
  on your office roof** and their boats sail to *your* berth. Lose their trust and they sail back to him, and he
  sets off fireworks. At 7 of 10 flags Marlowe's bank makes him sell. Buy him out, unload the giant *MV Gigantica*,
  then **pull the lever** that makes your port run itself (the late "watch it run" payoff Manan kept).
  Then endless: the port pays, and the crew does odd jobs machines can't (a piano, a circus, eggs).
- Taken from the other premises: **Gull FM**, a radio DJ who comments live on your disasters (from B), and **the
  town across the road reopening** as you grow (from C).
- **Why A:** most of it is already built (Marlowe, his port across the sea, drive-by, banner plane, fireworks,
  Mags, Bjorn, the harbour master's fish-out, the intro). The rival meter can be **seen on the map** instead of read
  in a menu. Failures carry story weight without a game-over. And it lands exactly on the decided ending (rival
  buyout + giant ship + endless).
- **Timing:** about 9 h to the lever for 2 players (62 days × ~9 min). Workers come at stage 3 (~2.5 h). Automated
  lines come one at a time from stage 4 (~4.5 h). Full automation only at the finale.
- **Biggest gaps in the build today:** there's **no save/continue** for a run (an 8–10 h game needs it), **no
  proximity voice** (every big co-op hit has it), and none of the story spine exists yet (trust, flags, Dot's daily
  target, radio, letters). All three are in the demo tier. Rejoin and shared saves are in the EA launch tier.

## Files
| File | What's in it |
|---|---|
| [story.md](story.md) | 3 premises: pitch, cast, act structure (stage 1 → finale → endless), 3 clip set pieces each, and the recommendation |
| [loops.md](loops.md) | Objective, core / session / meta loops, stages with hours and prices, win and fail states, money sinks, the automation payoff, co-op roles, solo scaling |
| [features.md](features.md) | Every feature tiered Demo → EA launch → EA updates → 1.0, each with why, effort (S/M/L), dependency and **status in today's build**, plus the 10 most clip-able physics moments |

## The three premises in one line each
| | Premise | Rival | Strongest point | Weakest point |
|---|---|---|---|---|
| **A** (recommended) | Win the Coast: win the coast's captains from Marlowe | Marlowe, tycoon across the sea | builds on what exists; the rival meter is visible on the map | "underdog vs tycoon" is familiar |
| B | Port Wars: a port league broadcast on radio, loser gets sold | Rex Gantry, 7-time champion | live commentary = a clip machine; seasons give endless mode its shape | reads as level-based; the cozy "home port" gets weaker |
| C | Lifeline: reopen an island's berth and the town comes back to life | Vivienne Sterling, resort developer | warmest, and the most visible progress | weakest villain; most art (every building needs two states) |

## Where this differs from earlier advice (on purpose)
- **No storm siren.** The design critic wanted a 90 s storm warning. Manan's rule is that weather is random and
  never warns, so no story beat schedules a storm. The "lookout" becomes a co-op role instead.
- **Automation stays, late.** The critics said cut it. Manan kept it for 8–10 h. Here a line only takes over a
  cargo the crew has already mastered, and only so they can take a bigger ship. The lever comes at the finale.
- **No contract board.** It was removed because Tushar found it confusing. The noticeboard only *shows* today's and
  tomorrow's boats. Whether players should *choose* boats is question 2 below.

## 3 questions only Manan can answer
1. **Which premise?** A (recommended), B or C. And do the names stay: Marlowe, Mags, Bjorn, Dot, Pim, Saltmouth,
   *Gigantica*?
2. **Should players choose tomorrow's boats?** For example, two cards on the noticeboard: "small and safe" or
   "big and risky". It gives groups their own stakes, but it's the thing Tushar found confusing as a contract board.
   Or do captains just turn up, as today?
3. **Radio and captain voices:** text bubbles + gibberish blips only (free, no AI content), or spoken lines later?
   Spoken lines would mean AI voice (to be declared on Steam) or a paid voice actor after Early Access.

## How this was made
Read: BRIEF.md, friend-plan.md, critic-design/market/production.md, ideas-shortlist.md, refine-brief.md,
HANDOVER-MANAN.md, docs/HOW-TO-PLAY.md, docs/PORT-STAGES.md, the vault's port-boss.md decisions. "Exists"
statuses were checked in the code at main a6fe3d4 (`Intro.cs` captions, `Rival.cs` banners, `PierSag.cs`,
`Boat.Evict`, the splash punch-in in `GameUI.cs`, and no run save beyond the Stats/character/settings PlayerPrefs).

## Market grounding (pb-market, `../market/market.md`, pulled 31 Oct 2026)
| Market finding | What it changed here |
|---|---|
| 11 of 13 co-op hits use only a frame (a quota-giver or a one-line premise); Docked's "rebuild the family port" story was called intrusive | A is a light frame: Grandpa is one intro line, captains only shout, letters and radio are optional, nothing stops play |
| A quota-giver works (Lethal Company's Company, R.E.P.O.'s Taxman) | Dot chalks a daily target on the noticeboard |
| An optional mystery (PowerWash, Dredge) brings crews back | One mystery beat per monthly EA update |
| Crashout Crew (~31K units, 93 %): "nothing persists", "can't beat later levels with two", no rejoin | Persistent port + captain flags; 2 players is the baseline; rejoin + shared saves at the EA launch |
| Proximity voice is in every big friendslop hit; Dissonance is paid | Steam Voice via Facepunch (free), in the demo tier |
| The late automation ending is proven (Schedule I, TCG Card Shop, Gas Station Sim) | Kept late: workers from stage 3, lines from stage 4, the lever at the finale |
| The 5 clips to design for, readable without the HUD | All five are in `features.md`'s clip list; the bucket-brigade catch window was added for #5 |
| EA: monthly updates, a public roadmap without dates, no DLC during EA | Written into the EA updates tier |
