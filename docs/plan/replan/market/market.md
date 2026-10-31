# Port Boss: Steam market research (re-plan, 31 Oct 2026)

Worker: pb-market, for Kufu task t-31c03670. Every number below was pulled live on **31 Oct 2026** from Steam, Tracker.gg, SteamCharts and Raijin. The raw pulls and scripts are in `market/raw/`. This updates `plan/critic-market.md` (28 Oct): the 28 Oct verdict stands, and several numbers have moved since then (Crashout Crew is now ~31K units, not 19K).

---

## Executive summary

1. **The genre is big but hit-or-miss.** Four of Steam's top-10 premium sellers of 2025 by copies were co-op "friendslop" games: R.E.P.O., PEAK, Schedule I and RV There Yet? ([AppMagic via GameDevReports](https://gamedevreports.substack.com/p/appmagic-friendslop-games-in-2025)). But GameDiscoverCo says "there are very few middling friendslop hits" ([source](https://newsletter.gamediscover.co/p/look-we-have-to-talk-about-meccha)), and the average 30-day retention is about 3%. Our games table shows the same split: breakouts sell in the millions, and almost everything else sells under 50K.
2. **The port niche is still empty where we would stand.** No port, harbour or ship game in the table has passed ~85K units. The leaders are both single-player: Ship Graveyard Simulator 2 (~84K units, 74% positive) and Docked (~47K, 73% positive, realistic, $29.99). The sea theme can explode when the hook is physics comedy: **How to Fish** (co-op fishing, $7.99, released 20 Aug 2026) hit a **373,887** all-time peak and ~3.6M estimated units. The two 2026 co-op boat-comedy games that copied the setting without a strong hook went nowhere: Holey Ship (0 reviews) and Water You Doing? (59% positive).
3. **Our closest mechanic comp is a warning.** In **Crashout Crew** (Aggro Crab, the makers of PEAK) you load trucks before they depart, which is our departing-ship clock. It has 93% positive reviews, launched on Game Pass, had a Next Fest demo and an estimated ~95K wishlists, yet it reached only a **2,620 peak and ~31K units**. Players say upgrades "reset after the contract", "nothing new gets unlocked except cosmetics", "cant beat the later levels with only two people", and they asked for endless mode and rejoin. **Port Boss's persistent, growing port answers most of that, but only if it also plays well with 2 players and has rejoin.**
4. **What the breakouts share.** A physical failure you can read in under 10 seconds, proximity voice, a $5–8 price, a fantasy that explains itself on stream, and almost no onboarding. Streamers lit the fire within days (Chained Together on day 1, Liar's Bar on day 2, R.E.P.O. in about a week), then TikTok and X clips of things going wrong spread it.
5. **Story: needed, but light.** 11 of the 13 hits we studied use only a framing device, such as a faceless quota-giver (Lethal Company's "Company", R.E.P.O.'s Taxman) or a one-line premise. **Docked, the direct setting comp, used "rebuild the family port after a hurricane", and reviewers called the story intrusive.** Recommendation: a quota-setting **Harbourmaster**, plus the visible rival **Marlowe Port** across the bay (already in the game), plus an optional mystery told through radio calls and manifests, PowerWash-style. No cutscenes.
6. **$6.99 is right.** It sits in the hit band: Meccha Chameleon $5.99, Liar's Bar $6.99, and PEAK, Content Warning, RV There Yet? and How to Fish at $7.99. GameDiscoverCo's median first-week wishlist conversion falls from 0.15x to 0.10x above $10. Steam caps launch discounts at 40%. Use **20% off at launch ($5.59)**. Price is not the lever: Holey Ship launched at $6.99 → $5.59 too.
7. **Calendar.** Use **Next Fest June 2027 (14–21 Jun, register by 25 Apr 2027)**, not February (register by 10 Jan 2027, only ten weeks away). A game can enter only one Next Fest. Put a public demo out **2–4 months before the fest**: early demos earned ~2.5x more wishlists. Go into the fest with **2,000+ wishlists**, and reach the **EA launch with at least 7,000, aiming for 25,000**.
8. **Early Access content.** The hits launched small, and the loop carried them: Lethal Company had 7 moons, 9 creatures and 8 tools; R.E.P.O. had 4 levels and 29 enemies. Plan **6–12 months of EA with a visible monthly update**. Ship with **drop-in rejoin and shared port progress**: host-only saves and broken online joins are the top killers in the near-miss reviews.
9. **AI disclosure is a live risk.** About a third of the October 2026 Next Fest entries carried AI disclosures. Shipping Store Simulator 2037 drew "AI slop art" reviews. Keep shipped art human-made or from the pack, and disclose any AI use (music if Suno is kept, code) honestly.
10. **Design for five clips** (§6): the ship leaves with a friend aboard, the crate tower falls, a vehicle goes into the harbour, a storm sweeps the deck, and a bucket-brigade throw misses with a splash. Every one must read in a single shot, without the HUD.

---

## 1. Comparable games: the numbers (pulled 31 Oct 2026)

Columns: EA start and 1.0 date, US list price and review totals from Steam ([appdetails API](https://store.steampowered.com/api/appdetails?appids=3583210&cc=us), store page, [review histogram](https://store.steampowered.com/appreviewhistogram/3583210?l=english)). All-time peak CCU is the higher of [Tracker.gg](https://tracker.gg/population/steam/3583210) and [SteamCharts](https://steamcharts.com/app/3583210), because [SteamDB](https://steamdb.info/) blocked every fetch with a 403. Owners use the Boxleiter range (reviews × 30–60). Raijin units are [Raijin.gg](https://raijin.gg/app/3583210/) model estimates. Team and co-op come from the research notes. Swap the app id in any link to check a row. Caveats are in section 7.

**A. The games Kufu named**

| # | Game | EA start | 1.0 / full release | List price | Reviews (all languages) | % positive | All-time peak CCU | Owners: reviews ×30–60 | Raijin est. units | Team at launch | Co-op | Core hook |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | [R.E.P.O.](https://store.steampowered.com/app/3241660/) | Feb 26, 2025 | still in EA | $9.99 | 360,662 | 95.6% | 269,593 | 10.8M–21.6M | 10,086,725 | "small team", Uppsala (n unverified) | 1–6 | Haul fragile loot with physics; damage = lost money; proximity voice |
| 2 | [Lethal Company](https://store.steampowered.com/app/1966720/) | Oct 23, 2023 | still in EA | $9.99 | 438,609 | 96.7% | 239,369 | 13.2M–26.3M | 11,880,676 | solo (Zeekerss) | 1–4 | Scrap runs for "the Company" quota; ship leaves at midnight |
| 3 | [Schedule I](https://store.steampowered.com/app/3164500/) | Mar 24, 2025 | still in EA | $19.99 | 301,583 | 97.5% | 458,410 | 9.0M–18.1M | 6,598,313 | solo core (3 credited) | 1–4 | Cook and sell drugs, small-time to kingpin |
| 4 | [PEAK](https://store.steampowered.com/app/3527290/) | – | Jun 16, 2025 | $7.99 | 320,711 | 93.8% | 170,645 | 9.6M–19.2M | 8,118,916 | 7 (jam: 3 Aggro Crab + 4 Landfall) | 1–4 | Climb a mountain together; stamina + grab physics |
| 5 | [Content Warning](https://store.steampowered.com/app/2881650/) | – | Apr 1, 2024 | $7.99 | 77,898 | 92.7% | 204,352 | 2.3M–4.7M | 3,549,964 | 5 | 2–4 | Film monsters to go viral on SpöökTube; free for 24 h at launch |
| 6 | [PowerWash Simulator](https://store.steampowered.com/app/1290000/) | May 19, 2021 | Jul 14, 2022 | $24.99 | 53,854 | 96.5% | 14,178 | 1.6M–3.2M | 1,165,735 | 11 at EA start | 1–6 (career 2) | Satisfying cleaning, no time pressure |
| 7 | [PowerWash Simulator 2](https://store.steampowered.com/app/2968420/) | – | Oct 23, 2025 | $24.99 | 11,401 | 90.2% | 21,110 | 342K–684K | 382,577 | FuturLab | 1–4 + split screen | Sequel to the above |
| 8 | [Supermarket Simulator](https://store.steampowered.com/app/2670630/) | Feb 20, 2024 | Jun 19, 2025 | $19.99 | 83,124 | 91.6% | 51,353 | 2.5M–5.0M | 1,821,208 | solo dev (own account) | 1 → 1–4 (co-op added later) | Run a shop by hand, grow it |
| 9 | [TCG Card Shop Simulator](https://store.steampowered.com/app/3070070/) | Sep 15, 2024 | Sep 14, 2026 | $19.99 | 49,770 | 95.8% | 53,322 | 1.5M–3.0M | 1,101,153 | solo + freelancers | 1 | Open card packs (the gamble) inside a shop sim |
| 10 | [Gas Station Simulator](https://store.steampowered.com/app/1149620/) | – | Sep 15, 2021 | $19.99 | 25,651 | 86.4% | 12,205 | 770K–1.5M | 548,750 | DRAGO (studio ≈50) | 1 | Restore an abandoned gas station |
| 11 | [Bellwright](https://store.steampowered.com/app/1812450/) | Apr 23, 2024 | still in EA | $29.99 | 20,886 | 78.2% | 15,186 | 627K–1.3M | 464,742 | Donkey Crew (n unverified) | 1–4 | Medieval rebellion + town-building |
| 12 | [Crashout Crew](https://store.steampowered.com/app/3583210/) | – | May 28, 2026 | $7.99 | 1,059 | 93% | 2,620 | 32K–64K | 31,360 | Aggro Crab (n unverified) | 1–4 | Forklift co-op: load trucks before they depart |
| 13 | [Docked](https://store.steampowered.com/app/2487300/) | – | Mar 5, 2026 | $29.99 | 1,257 | 73% | 3,262 | 38K–75K | 47,335 | Saber (n unverified) | 1 | Realistic container-port machinery |

**B. Other 2024–26 co-op breakouts**

| # | Game | EA start | 1.0 / full release | List price | Reviews (all languages) | % positive | All-time peak CCU | Owners: reviews ×30–60 | Raijin est. units | Team at launch | Co-op | Core hook |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 14 | [How to Fish](https://store.steampowered.com/app/4001890/) | – | Aug 20, 2026 | $7.99 | 77,049 | 94.7% | 373,887 | 2.3M–4.6M | 3,590,954 | Dazed Games (n unverified) | 1–4 | Physics fishing with guns/explosives; sell fish, buy upgrades |
| 15 | [RV There Yet?](https://store.steampowered.com/app/3949040/) | – | Oct 21, 2025 | $7.99 | 78,750 | 89.7% | 99,843 | 2.4M–4.7M | 2,191,460 | 4 built it in ≈8 weeks (studio 5–10) | 1–4 | Drive an RV home over awful terrain together |
| 16 | [MECCHA CHAMELEON](https://store.steampowered.com/app/4704690/) | – | Jun 9, 2026 | $5.99 | 82,444 | 86.6% | 339,272 | 2.5M–4.9M | 16,471,636 | solo (n unverified) | online | Hide-and-seek playacting |
| 17 | [Chained Together](https://store.steampowered.com/app/2567870/) | – | Jun 19, 2024 | $4.99 | 58,667 | 89.4% | 92,984 | 1.8M–3.5M | 1,261,706 | unverified | 1–4 | Climb out of Hell chained together |
| 18 | [Liar's Bar](https://store.steampowered.com/app/3097560/) | Oct 2, 2024 | still in EA | $6.99 | 51,988 | 89.2% | 109,419 | 1.6M–3.1M | 2,231,710 | unverified | 4 | Bluffing at a bar; loser plays roulette |
| 19 | [WEBFISHING](https://store.steampowered.com/app/3146520/) | – | Oct 11, 2024 | $4.99 | 61,056 | 97.4% | 23,966 | 1.8M–3.7M | 1,311,152 | 1 credited | 1–12 | Cosy chat-room fishing |
| 20 | [Roadside Research](https://store.steampowered.com/app/3643170/) | Feb 12, 2026 | still in EA | $14.99 | 4,167 | 84.7% | 9,348 | 125K–250K | 299,250 | Cybernetic Walrus (n unverified) | 1–4 | Aliens in disguise run a gas station |
| 21 | [Super Battle Golf](https://store.steampowered.com/app/4069520/) | – | Feb 19, 2026 | $12.99 | 9,818 | 93.1% | 20,395 | 295K–589K | 642,942 | Brimstone (n unverified) | online | Party battle golf |
| 22 | [Supermarket Together](https://store.steampowered.com/app/2709570/) | – | Aug 9, 2024 | Free | 106,675 | 94% | 47,731 | 3.2M–6.4M | 334,117 | unverified | 1–16 | Free co-op supermarket |
| 23 | [Mage Arena](https://store.steampowered.com/app/3716600/) | Jul 24, 2025 | still in EA | $2.99 | 18,571 | 91.1% | 17,126 | 557K–1.1M | 814,200 | solo | PvP to 4v4 | Shout spells into the mic |

**C. Co-op/physics logistics near-misses**

| # | Game | EA start | 1.0 / full release | List price | Reviews (all languages) | % positive | All-time peak CCU | Owners: reviews ×30–60 | Raijin est. units | Team at launch | Co-op | Core hook |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 24 | [Mars First Logistics](https://store.steampowered.com/app/1532200/) | Jun 21, 2023 | Sep 25, 2025 | $19.99 | 3,604 | 94.4% | 2,477 | 108K–216K | 80,676 | "small team" | 1–4 | Build physics rovers, haul awkward cargo on Mars |
| 25 | [Drive Beyond Horizons](https://store.steampowered.com/app/2625420/) | Mar 24, 2025 | still in EA | $24.99 | 18,730 | 76.9% | 14,797 | 562K–1.1M | 413,355 | multi-person studio | 1–4 | Procedural co-op road trip |
| 26 | [Moving Out 2](https://store.steampowered.com/app/1641700/) | – | Aug 15, 2023 | $29.99 | 471 | 72% | 460 | 14K–28K | 16,105 | SMG + DevM (Team17) | 1–4 | Physics house-moving levels |
| 27 | [Totally Reliable Delivery Service](https://store.steampowered.com/app/1011670/) | – | Apr 1, 2021 | $14.99 | 1,721 | 76% | 244 | 52K–103K | 78,621 | We're Five Games | 1–4 | Ragdoll couriers |
| 28 | [Parcel Simulator](https://store.steampowered.com/app/2424010/) | – | Jun 20, 2025 | $17.99 | 2,894 | 92.4% | 3,610 | 87K–174K | 68,170 | Dansan Digital | 1 | Parcel job sim, ≈$50 marketing |
| 29 | [Shipping Store Simulator 2037](https://store.steampowered.com/app/4098030/) | – | Oct 14, 2026 | $19.99 | 747 | 95% | 1,574 | 22K–45K | 13,055 | Playstrom (SayGames) | 1 | Pack parcels by hand, then automate |

**D. Port, ship and harbour games**

| # | Game | EA start | 1.0 / full release | List price | Reviews (all languages) | % positive | All-time peak CCU | Owners: reviews ×30–60 | Raijin est. units | Team at launch | Co-op | Core hook |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 30 | [Ship Graveyard Simulator 2](https://store.steampowered.com/app/2201940/) | – | Aug 16, 2023 | $24.99 | 3,795 | 73.9% | 2,737 | 114K–228K | 83,963 | Games Incubator (PlayWay) | online co-op | Scrap ships with heavy tools |
| 31 | [Seafarer: The Ship Sim](https://store.steampowered.com/app/2582140/) | Oct 7, 2025 | Sep 24, 2026 | $29.99 | 2,110 | 62.7% | 2,718 | 63K–127K | 54,877 | astragon | 1–4 (added Aug 2026) | Realistic ship sim; co-op crew added late |
| 32 | [Water You Doing?](https://store.steampowered.com/app/4429930/) | – | Aug 26, 2026 | $7.99 | 334 | 59% | 975 | 10K–20K | 14,702 | DiSolve Studios | co-op | Chaotic co-op on a leaky boat |
| 33 | [Holey Ship](https://store.steampowered.com/app/4475470/) | – | Aug 25, 2026 | $6.99 | 0 | – | – | – | – | 4 friends | 1–4 | Co-op boat-repair shop comedy |
| 34 | [Dockside Dreams – Fish & Cook Simulator](https://store.steampowered.com/app/3870930/) | – | Nov 21, 2025 | $7.99 | 22 | 59% | – | 660–1,320 | 457 | C51 Games | 1–4 | Dockside fish-and-cook sim |
| 35 | [Container Terminal Simulator](https://store.steampowered.com/app/3806270/) | Sep 25, 2025 | Sep 24, 2026 | $9.99 | 11 | 36% | – | 330–660 | 205 | Tikam Studio | 1 | Rail container terminal sim |
| 36 | [Port Authority](https://store.steampowered.com/app/3672760/) | – | May 14, 2026 | $5.99 | 34 | 97% | – | 1,020–2,040 | 783 | NFold Games | 1 | Harbour traffic control |
| 37 | [Port Shipping Tycoon](https://store.steampowered.com/app/3572770/) | – | Jun 27, 2025 | $4.99 | 5 | 0% | – | 150–300 | 114 | Urban Isotope | 1 | Idle port tycoon |
| 38 | [Forklift Certified](https://store.steampowered.com/app/3364470/) | – | Apr 3, 2026 | $5.99 | 13 | 100% | – | 390–780 | 248 | solo | 1 | Forklift puzzle |

## 2. How each game is built and how it spread

Sources for every cell: `raw/research-hits.md` and `raw/research-nearmisses.md`, where each bullet carries its link (all accessed 2026-10-31). "?" means no live source was found.

| Game | Story (how much) | Objective structure | Session / run | Co-op · voice | Progression | How it went viral (first spark) |
|---|---|---|---|---|---|---|
| R.E.P.O. | **Light frame.** You work for a "mysterious computer intelligence" (the Taxman) and must make money for it. More story is promised for 1.0. | A quota per level; you can't leave until it's met | ? per level | 1–6 · **proximity voice** | Service Station upgrades between levels; cosmetics added in 2026 | Quiet for ~1 week. Then Twitch clips went to TikTok: the "Repo I Love" clip from PresidentLloyd's stream (20 Mar 2025), whose edits got 17.9M and 28.1M views. "Exaggerated physics" did the rest |
| Lethal Company | **Light frame.** Contract workers for "the Company", with journal-entry lore | Profit quota every 3 days; it rises; missing it ends the run | One day ≈ 11 real minutes; **the ship leaves at midnight** | 1–4 · **proximity voice** | Credits buy tools and riskier moons; resets on fail | @imdaukey's TikTok the day after launch (977K plays), then a Forest Keeper TikTok with 10M+ plays in a week; streamers worldwide |
| Schedule I | **Real but light.** Uncle Nelson phones you from jail; a rival cartel blows up your RV | Open-world growth from dealer to kingpin, with an end mission | ? | 1–4 · voice ? (host owns the save) | Hire staff (automation), buy properties | A quiet beta (Dec 2024) and a free co-op demo, then a streamer push; Twitch peak 225K viewers |
| PEAK | **Premise only.** Plane-crash castaways | Run-based climb through 6 biomes; the map rotates daily | A run is under 2 h; a typical summit 2–5 h | 1–4 · **proximity voice** | Badges unlock cosmetics only | Twitch streams; its $5–8 price is credited; a cheeky developer post |
| Content Warning | **Premise only.** Film monsters to go viral on SpöökTube | Views quota within 3 days; failing wipes the run | 3 dives per run | 2–4 · voice | Meta-coins buy hats | **Free for the first 24 h**: 6.2M claims, 50K CCU within 12 h |
| PowerWash Simulator | **Light, optional plot** told in job texts (corrupt mayor, volcano). Developers: it "can happily be ignored" | 38 jobs, no time pressure | ? | career 2, free play 6 | New tools | Free itch.io demo (2020); Game Pass on day one at 1.0 |
| Supermarket Simulator | **None** | Open-ended shop growth | ? | Solo; co-op for 4 added later | Expansion, staff, DLC | 33K wishlists at launch, then streamers (Kuplinov 2M+ views); CCU went from 3K on day 1 to 38K at week 2 |
| TCG Card Shop Simulator | **None** | Open-ended shop growth | ? | Solo | Unlock "tools that automate management" | Reddit, then YouTubers who picked it up unprompted |
| Gas Station Simulator | **Light frame.** Restore an abandoned gas station | Open-ended renovate-and-expand | ? | Solo | Staff, then "leave those details to automation" | Steam festival demo + free prologue; #1 global top seller at launch |
| Bellwright | **Real narrative.** Framed exile leads a rebellion | Liberate regions (has an end goal) | 20+ h is common | 1–4 | Town tech via recruits | Next Fest demo (Jun 2023); 975K Twitch hours |
| Crashout Crew | **Light satire.** "DE NILE SHIPPING", where damage comes out of your pay | **Load the trucks before they depart**; 20+ contracts × 5 shifts | 8–10 h total | 1–4, online only | **Upgrades reset after each contract; cosmetics only** | Announced Oct 2025 with a Next Fest demo (3 contracts); "from the makers of PEAK" press; Game Pass |
| Docked | **Narrative campaign.** Rebuild Port Wake and the family business after a hurricane; **reviewers found it intrusive** | Milestone-gated contracts on a fixed port layout | Campaign ≈ 10–18 h | **Single-player** (co-op is a top request) | Milestones; DLC | Next Fest demo (Oct 2025), console launch; nothing went viral |
| How to Fish | ? (none found) | Fish, sell, upgrade | ? | 1–4 (one source says 8 since patch 1.0.4) | Upgrades | "1M players in two days"; backed by Evil Landfall ([Niche Gamer](https://nichegamer.com/how-to-fish-attracts-one-million-players-in-two-days/)) |
| Chained Together | **Premise only.** Climb out of Hell | A single climb to the top | ? | 1–4 | Cosmetics | DisguisedToast + LilyPichu streamed it on day 1; a 3.1M-view X clip and the "Drunk Ninja" clip |
| Liar's Bar | **Flavour** ("Table Stories") | Session rounds, no meta goal | ? | 4 | Planned ranks/currency | Alanzoka's video on day 2 (1.9M views); a 22M-view neck-stretch bug clip |

**What this says for Port Boss:**
- **The quota-plus-clock structure is the proven co-op loop.** Lethal Company's ship leaving at midnight is our ship leaving on the clock. The difference is that Lethal Company and Content Warning wipe the run, while our port persists. That is closer to the sims (Supermarket, TCG, Schedule I), which keep progress and end in automation. **Port Boss sits between the two hit families, and that is our structural edge.**
- **The late automation ending is market-proven** in Schedule I, TCG Card Shop Simulator, Gas Station Simulator and Shipping Store Simulator 2037. Manan's call to keep it late (8–10 h) matches how they gate it.

## 3. Patterns

### 3.1 What makes this genre break out
- **A clip of something going wrong**, readable without context: R.E.P.O. physics, the "Drunk Ninja" clip, the Liar's Bar neck bug, WEBFISHING trickshots. Docked (realistic) produced none.
- **Proximity voice.** It's confirmed in the three biggest friendslop hits (Lethal Company, R.E.P.O., PEAK). AppMagic's formula is "co-op + proximity chat + unpredictable reactions" ([AppMagic](https://appmagic.rocks/blog/friendslop-steam-games-2025)).
- **Low price.** Friends buy in groups: "the effective spend at least doubles", and a low price "makes players more forgiving" in reviews ([GameDevReports](https://gamedevreports.substack.com/p/appmagic-friendslop-games-in-2025)).
- **Almost no onboarding**, and a "funny fantasy" over polish. Players tolerate jank ([Zukowski, Jul 2026](https://howtomarketagame.com/2026/07/30/is-friendslop-saturated)).
- **Free access as the match.** Content Warning was free for 24 h, Schedule I had a free co-op demo, PowerWash an itch.io demo, and Gas Station Simulator a free prologue.
- **Tiny teams are normal.** Lethal Company, TCG Card Shop and Schedule I were solo; Content Warning had 5 developers and PEAK 7. RV There Yet? was built by 4 people in about 8 weeks ([Automaton](https://automaton-media.com/articles/newsjp/20260625-451820/)).

### 3.2 What an Early Access launch needs
- **Content at the EA launch was small; the loop carried it.** Lethal Company had "7 planets, 9 creatures, 8 tools" and R.E.P.O. "4 levels, 29 enemies". Nobody publishes a minimum content-hours figure (I found no dataset). The real bar is depth that keeps going, not raw hours. Crashout Crew has 8–10 h of contracts, yet a top negative review says "if you've played 3 hours of this game, you have effectively seen everything", because nothing persists.
- **Length.** Zukowski's median EA length is 260 days ([source](https://howtomarketagame.com/2023/09/19/early-access-tips-part-3/)). Newzoo finds 1.0 does best after about 6 months of EA, and 1.0 sells ~0.7x the EA launch ([GameDiscoverCo](https://newsletter.gamediscover.co/p/will-your-steam-10-launch-outpace)). **Plan 6–12 months.**
- **Cadence.** Schedule I promises "substantial content updates on a monthly basis". Lethal Company's update gaps produced "is it abandoned?" threads, and Mage Arena's cancelled roadmap emptied its lobbies. **Commit to a monthly update and a public, undated roadmap**: Valve says not to promise specific future dates ([Steamworks EA](https://partner.steamgames.com/doc/store/earlyaccess)).
- **The EA launch is the real launch.** It uses the game's single Popular Upcoming appearance.

### 3.3 Demo and Next Fest
| Event | Dates | Registration closes | Source |
|---|---|---|---|
| Next Fest Feb 2027 | 22 Feb – 1 Mar 2027 | **10 Jan 2027** | [Steamworks](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/feb_2027) |
| Next Fest June 2027 | 14 – 21 Jun 2027 | **25 Apr 2027** | [Steamworks](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/june_2027) |

- **One Next Fest per game**, and it should be the last one before launch.
- **Wishlists gained depend on wishlists brought in** (r=0.825). Median gain was 322 for games with 0–999 going in and 1,006 for 1K–9.9K. Zukowski's minimum is **2,000+ before the fest** ([benchmarks](https://howtomarketagame.com/2025/03/26/benchmarks-how-many-wishlists-can-i-get-from-steam-next-fest/)).
- **Demos out months early earned ~2.5x more** ([Feb 2026 analysis](https://howtomarketagame.com/2026/04/13/making-sense-of-the-february-2026-steam-next-fest/)). Don't shadow-drop; give the demo to creators first.
- **The demo should be short and sharp**: one berth, one day, two ships, ending on the ship leaving. Roadside Research (co-op gas-station job sim) shows a strong job-sim demo can hold players for a median 1 h 12 m: 903K demo players → 396K units ([GameDiscoverCo](https://newsletter.gamediscover.co/p/how-two-friendslop-hits-got-the-audience)).
- **Next Fest is crowded.** October 2026 had 7,240 entries, about a third of them with AI disclosures. Outside the top 100, the median game had 15 followers the day before ([Zukowski](https://howtomarketagame.com/2026/10/21/how-sloppy-is-the-october-2026-steam-next-fest/)).
- **Production fit.** The 28 Oct production plan had a public demo on 15 Feb 2027 and Next Fest in June 2027. That still matches this data.

### 3.4 Wishlist targets
Conversion ratios: GameDiscoverCo's median first-week sales are 0.15x wishlists, and 0.10x above $10 ([source](https://newsletter.gamediscover.co/p/the-state-of-steam-wishlist-conversions)). Zukowski finds first-month sales per wishlist halved to about 0.22 for 2023–25 releases ([source](https://howtomarketagame.com/2026/10/13/wishlist-inflation-is-real-but-they-are-still-important/)). The units are my arithmetic on those ratios, and gross is at $6.99 before Steam's cut, VAT and refunds.

| Wishlists at EA launch | Week-1 units (×0.10–0.15) | Month-1 units (×0.22) | Month-1 gross at $6.99 | Meaning |
|---|---|---|---|---|
| 2,000 | 200–300 | ~440 | ~$3.1K | Minimum going into Next Fest |
| 7,000 | 700–1,050 | ~1,540 | ~$10.8K | Floor for the EA launch (Zukowski's old minimum) |
| **25,000** | 2,500–3,750 | ~5,500 | ~$38K | **Target.** Clears the Popular Upcoming bar with margin |
| 95,000 | 9,500–14,250 | ~20,900 | ~$146K | Crashout Crew's estimated wishlists; it reached ~31K lifetime units |

### 3.5 Pricing: $6.99 against the field
| Price | Games (all-time peak) |
|---|---|
| $2.99–4.99 | Mage Arena $2.99 (17K), Chained Together $4.99 (93K), WEBFISHING $4.99 (24K) |
| $5.99–6.99 | **Meccha Chameleon $5.99 (339K)**, **Liar's Bar $6.99 (109K)**, Holey Ship $6.99 (–), Port Authority $5.99 (–) |
| $7.99 | PEAK (171K), Content Warning (204K), RV There Yet? (100K), **How to Fish (374K)**, Crashout Crew (2.6K), Water You Doing? (1K) |
| $9.99 | R.E.P.O. (270K), Lethal Company (239K) |
| $12.99–19.99 | Super Battle Golf $12.99 (20K), Roadside Research $14.99 (9K), Schedule I $19.99 (458K), Supermarket Sim $19.99 (51K) |

- **Price doesn't decide the outcome**: the $7.99 row runs from 374K down to 1K peak. But every friendslop breakout is at **≤ $9.99**, and PEAK's developer says "eight bucks is still five bucks" ([Game File](https://www.gamefile.news/p/peak-interview)).
- **$6.99 keeps us under the $10 conversion drop** and leaves room to rise at 1.0 ("indies should always be increasing their price", Zukowski).
- **Discounts.** Steam allows up to 40% for a 7–14 day launch discount, followed by a 30-day cooldown ([Steamworks](https://partner.steamgames.com/doc/marketing/discounts)). **Recommendation: list at $6.99 with 20% off at launch ($5.59)**, $4.89 in seasonal sales, and **raise to $9.99 at 1.0**. Use Valve's regional pricing tool rather than raw exchange rates. Brazil gave over 30% of one Next Fest winner's players.

### 3.6 The review complaints that killed similar games
| Killer | Who it hit | Port Boss rule |
|---|---|---|
| Content ran out, with nothing persistent ("nothing new gets unlocked except cosmetics") | Crashout Crew, Shipping Store Sim 2037, Mars First Logistics | The port grows across sessions; every day adds a visible piece |
| Balanced for 4, broken for 2 ("cant beat the later levels with only two people") | Crashout Crew | Scale ship size and departure timers to the number of players |
| Online co-op that doesn't connect, or desyncs | Moving Out 2, PowerWash Simulator 2, Totally Reliable Delivery Service, Gang Beasts | Test at 150 ms with two real Steam accounts before the demo (still untested) |
| No rejoin after a disconnect | Crashout Crew | Drop-in rejoin |
| Host-only saves | Schedule I (forum threads) | Every crew member keeps the port, or at least a save co-owned with the host |
| "A 10-hour tutorial", minigame filler, a fixed scripted port | Docked | Free play and endless mode, no milestone soft-locks |
| Fighting the physics instead of playing with it | Mars First Logistics, Totally Reliable Delivery Service | Physics is comedy, never precision. No snap-perfect placement (Container Terminal Sim's complaint) |
| DLC or limited-time events before fixes | Docked, Ship Graveyard 2, Drive Beyond Horizons | No DLC during EA |
| Undisclosed AI art | Shipping Store Simulator 2037 | No AI art ships; honest disclosure |
| Abandonment, cancelled roadmap | Mage Arena, Lethal Company gaps | Monthly cadence, honest roadmap |

## 4. Does Port Boss need a story?

**Yes, a frame. No, not a plot.** The evidence:
- **Light frames dominate the hits.** 11 of 13 hits use a quota-giver or a one-line premise. Only Schedule I, Bellwright and PowerWash carry a real plot, and PowerWash's is optional by design.
- **The heavy version failed on our own setting.** Docked's "save the family port after a hurricane" campaign drew complaints that the story was intrusive.
- **Story is something hits add later.** R.E.P.O. lists "more story elements" as a 1.0 goal, not a launch requirement.
- **Co-op and cutscenes don't mix.** Drop-in co-op narrative is "uniquely challenging" ([GDC 2011](https://gdcvault.com/play/1015134/Game-Together-or-Die-Alone)), and co-op players ask to vote-skip cutscenes. That is anecdotal; I found no skip-rate data.

Light-narrative devices that worked, and how each maps onto us:

| Device | Worked in | Port Boss version | Verdict |
|---|---|---|---|
| **Quota boss** | Lethal Company (the Company), R.E.P.O. (the Taxman), Content Warning (views) | **The Harbourmaster** posts the day's tonnage on the board and radios in on the morning shout. Miss it and you lose the berth's next ship, never the save | **Use.** It turns "ships leave" into a target and costs one voice line a day |
| **Rival** | Stardew (Joja), Schedule I (the Benzies) | **Marlowe Port**, the mega port across the bay, already built. Its freighters steal your contracts, its wake swamps your pier, and the finale buys it out | **Use.** It is the visible goal in every trailer shot |
| **Inherited business / town to rebuild** | Stardew (grandfather's farm), Gas Station Sim, Ranch Simulator | "Grandad's jetty" | **Light touch only.** Docked already owns "family port after a hurricane", and its players disliked it. One line of intro text at most |
| **Optional mystery** | PowerWash (texts), Dredge (bottles), Lethal Company (journals) | Odd manifests, a crate that ticks, a radio voice at night, a shape under the pier. Found, never forced | **Use as EA update fuel.** One mystery beat per monthly update gives returning crews a reason to come back |
| **Named characters with lines** | Schedule I (Uncle Nelson on the payphone) | Captains Mags and Bjorn (already in), the fish-market buyer, and the Harbourmaster | **Use.** Short shouts only, no dialogue trees |

**Recommended frame, in one sentence:** *"You and your mates take over the worst berth in the harbour. The Harbourmaster wants tonnage, Marlowe Port wants you gone, and the ships leave on time either way."*

## 5. Positioning

### 5.1 Three positioning statements
1. **"The sea doesn't wait"** (chaos-first, the strongest clip hook). *"A 1–4 player co-op dock-job game where every ship leaves on time, with your cargo or without it, and sometimes with your friend still aboard."* This leads with the departing-ship clip and is the most friendslop-legible. Risk: it reads like Crashout Crew at sea, so the trailer must show the port growing as well.
2. **"From rotten jetty to mega port"** (growth-first). *"Start on one broken berth. Stack, throw and haul your way to the biggest port on the coast, then beat Marlowe Port and watch your empire run."* This sells the 8–10 h arc and the automation ending, which is what Crashout Crew and Holey Ship lacked. Risk: it sounds like the flooded shop-sim genre if the trailer doesn't open on chaos.
3. **"Overcooked at the docks, but the port is yours"** (comparison pitch for creators and press). *"Co-op physics chaos while the ship is in, cosy growth when it sails: a persistent port you and three friends build across sessions."* This is the best for the press kit and creator emails. Risk: comparison pitches cap the game's ceiling.

**Recommendation:** use **#1 as the capsule and trailer opening**, and **#2 as the second half of the trailer and the store description**. Keep #3 for outreach only.

### 5.2 Steam tags (up to 20; the first five carry the most weight)
**Proposed order:** Co-op · Online Co-Op · Physics · Funny · Simulation · Job Simulator · Management · Multiplayer · Casual · Stylized · Colorful · 3D · Third Person · Building · Automation · Economy · Naval · Fishing · Comedy · Early Access (Valve adds this one itself).

Why: the first four match the hits (R.E.P.O. and PEAK lead with Online Co-Op, Physics, Funny and Comedy). "Job Simulator" and "Management" pull in the Supermarket and Loading Dock Simulator audience. "Naval" and "Fishing" put us next to How to Fish and the port games in "More like this". **Avoid "Realistic"**, which is Docked's tag.

## 6. The five most clip-able moments to design for

Rules for all five, taken from the viral clips above: readable in **under 10 seconds**, from **one camera**, **without the HUD**, ideally with a friend's voice over it. The failure must be visible, not shown as a number.

| # | Moment | Why it clips | What the game needs (status) |
|---|---|---|---|
| 1 | **The ship leaves with a friend aboard.** The horn blows, three players sprint down the jetty, and one is still on deck. The captain hurls them overboard, or they sail away | Lethal Company's "ship leaves at midnight" panic, and our signature | Departure clock and stowaway throw already work. Add a rising gangplank, a 5-second horn countdown audible on both ends, and a slow-mo splash (idea #6 is in) |
| 2 | **The crate tower falls.** Someone adds one crate too many to a wobbling stack and it topples onto the deck, onto a friend, into the sea | R.E.P.O.'s "fragile loot" comedy; it reads instantly | Stack/wobble/throw is in. The collapse needs to go far, crates need to bounce and float, and fish should burst out |
| 3 | **A vehicle into the harbour.** The trolley or forklift rolls off the jetty edge with the cargo, and a sheepish voice says "...my bad" | Vehicles in water are a meme staple ("forklift certified") | Trolley-into-sea works. The forklift needs to sink slowly with bubbles, the driver ragdolls out, and fishing it back up (a crane hook or rope) becomes its own clip |
| 4 | **A storm sweeps the deck.** With no warning, waves rise, the boat heels, loose crates slide off in a line, and a gull steals a fish | A physical stake from random weather (Manan's rule: no warning) | Random weather is in. The heel needs to be big enough to slide crates, add one rogue wave, and gulls should grab exposed fish |
| 5 | **Bucket-brigade throw, missed.** Players toss crates boat → friend → quay, the last catch fumbles, splash, and the buyer counts a knock on the freshness ring | Teamwork that fails; the Chained Together "one friend drags the rest down" shape | Throwing is in. It needs a catch window, a clear "caught" sound, and a big visual for a miss. Proximity voice makes it |

**Also capture (trailer, not clips):** the time-lapse from rotten jetty to mega port, Marlowe Port's freighter wake swamping our pier, and the first fully automated line running while the crew "supervises" with their feet up.

**Proximity voice is not yet in the build**, and every major friendslop hit has it. Add it before the demo. (The 28 Oct production plan named Dissonance, which is paid, so it is blocked by the no-money rule until EA. Free options are Steam voice through Facepunch, or Unity Vivox's free tier if its terms allow. To check.)

## 7. Method, sources and caveats

- **Raw data (all pulled 31 Oct 2026):**
  - `raw/steam-pull-2026-10-31.json` and `raw/steam-pull2-2026-10-31.json`: Steam appdetails API, store page, review histogram, SteamSpy and SteamCharts.
  - `raw/raijin-*.json`: Raijin estimates.
  - `raw/trackergg-peaks-2026-10-31.csv`: all-time peaks.
  - `raw/master-2026-10-31.csv`: the merge.
  - Scripts: `raw/pull.py`, `raw/raijin.py`, `raw/gen_table.py` (re-run them to refresh).
- **Agent research notes:**
  - `raw/research-hits.md`: 13 hits, each bullet sourced.
  - `raw/research-nearmisses.md`: near-misses, review quotes, 16 port/sea games.
  - `raw/research-patterns.md`: EA, Next Fest, wishlists, pricing, friendslop, killers, story.
- **SteamDB returned HTTP 403** to every fetch, so peaks come from **Tracker.gg and SteamCharts** (I took the higher of the two). Each tracker misses spikes from before it started tracking: SteamCharts shows 819 for Crashout Crew against Tracker.gg's 2,620, and Tracker.gg shows 2,358 for Gas Station Simulator against SteamCharts' 12,205. Raijin's "peak" is a last-6-months figure and is **not** used as all-time.
- **Review counts** come from Steam's own review histogram, which counts all languages; for small games it is the store page's "All Time" figure. Steam's JSON review endpoint now redirects. SteamSpy and Raijin give different totals for the big games (R.E.P.O.: SteamSpy 186K, Steam histogram 361K, Raijin 430K). Content Warning's free-day reviews make its count understated.
- **Owner estimates.** The Boxleiter range (reviews × 30–60) is a rule of thumb. It undercounts viral, low-review games (Meccha Chameleon: Boxleiter 2.5–4.9M against Raijin's 16.5M and GameDiscoverCo's "10M+") and is meaningless for free games (Supermarket Together). Raijin units are model estimates with "medium" confidence. Neither is Valve data.
- **Prices** are US list prices from Steam's appdetails API (cc=us) on 31 Oct 2026. Some games were on sale that day: Holey Ship $5.59, Gas Station Simulator $8.99, Drive Beyond Horizons $17.49.
- **Not verified:** team headcounts marked "n unverified", session lengths marked "?", follower counts of unreleased games (SteamDB blocked), and the 4-pack effect (no data exists).
