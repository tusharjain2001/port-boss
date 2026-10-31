# market/: Steam market research for the Port Boss re-plan (31 Oct 2026)

**Read `market.md`.** It has the executive summary at the top, then:
1. The numbers table for 38 games
2. How each game is built and how it spread
3. Patterns: breakouts, Early Access, Next Fest, wishlists, price, review killers
4. The story question
5. Positioning and Steam tags
6. Five clip moments
7. Method and caveats

## The answers Kufu's plan page needs
- **Market:** the co-op port niche is still empty. The closest mechanic, Crashout Crew (trucks depart), stalled at ~31K units because its progress resets after every contract. The closest setting, Docked, is single-player, realistic and 73% positive. How to Fish (co-op fishing, $7.99, Aug 2026, 374K peak) shows the sea theme can break out when physics comedy is the hook.
- **Story:** a light frame, not a plot. A quota-setting **Harbourmaster**, the rival **Marlowe Port**, and an optional radio/manifest mystery. No cutscenes. Avoid "save the family port": Docked did that and players found it intrusive.
- **Price:** $6.99 confirmed. Launch at 20% off ($5.59) and consider $9.99 at 1.0.
- **Calendar:** Next Fest **June 2027** (14–21 Jun, register by 25 Apr 2027). Public demo 2–4 months before it. 2,000+ wishlists going into the fest, at least 7,000 at the EA launch, 25,000 as the target.
- **Must-haves before the demo:** proximity voice (none in the build today), drop-in rejoin, shared port progress, a 2-player balance, and a 150 ms netcode test across two real Steam accounts.
- **Positioning:** "The sea doesn't wait": every ship leaves on time, with your cargo or without it.

## Files
| File | What it is |
|---|---|
| `market.md` | The report |
| `raw/master-2026-10-31.csv` | Merged numbers for the first 38 games pulled (the 9 added later are in `steam-pull2`/`raijin2`) |
| `raw/steam-pull-2026-10-31.json`, `raw/steam-pull2-2026-10-31.json` | Steam appdetails, store page, review histogram, SteamSpy, SteamCharts |
| `raw/raijin-2026-10-31.json`, `raw/raijin2-2026-10-31.json` | Raijin unit and wishlist estimates |
| `raw/trackergg-peaks-2026-10-31.csv` | All-time peak CCU (Tracker.gg) |
| `raw/research-hits.md` | 13 hit games: team, story, objective, session, co-op, progression, virality, EA (every bullet sourced) |
| `raw/research-nearmisses.md` | Near-misses and the port niche: review complaints, plus 16 upcoming and recent port/sea games |
| `raw/research-patterns.md` | Early Access, Next Fest, wishlists, pricing, friendslop critiques, killers, story devices |
| `raw/pull.py`, `raw/raijin.py`, `raw/gen_table.py`, `raw/ids*.txt` | Re-run to refresh the numbers: `python -I raw/pull.py raw/ids.txt out.json`, then `python -I raw/gen_table.py` |

SteamDB blocked every fetch (HTTP 403), so peaks come from Tracker.gg and SteamCharts. Section 7 of `market.md` lists every caveat.
