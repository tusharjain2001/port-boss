# Idea shortlist — what Port Boss can borrow

Researched 2026-10-29 (Steam reviews, Reddit, dev talks/interviews — see sources per idea).
Vetted against Manan's rule: no raw brainstorm dump. Everything below either fits the game as it
already is, would produce a good co-op clip/laugh, and is small enough to build tonight — or it's
honestly bigger and parked in the "later" list instead of padded in here.

Full per-game research (praise/complaints/sources) is in the fork outputs this file was built from;
this file keeps only what survived vetting.

---

## Tonight (≤2h each), ranked

### 1. Pre-shift hazard modifier toggle
**What:** before a boat's timer starts, players can vote to toggle an optional modifier (e.g.
"Double Storm," "Greased Deck," "One-Handed Carrying Only") for a pay bonus.
**Why it works:** **Crashout Crew** (Aggro Crab's own co-op forklift-warehouse game, the closest
sibling to Port Boss that exists) ships this exact lever — optional "Safety Violations" that make a
contract wilder — and press coverage calls it the main replay/chaos knob. [Gematsu, CheckpointGaming]
**Fits Port Boss:** we already have a shift clock and a weather system — this is just exposing an
opt-in multiplier on top of systems that exist, no new mechanic.
**Effort:** ~1.5h (a vote UI + a few flags read by existing weather/carry code).

### 2. Unstable/special crate types (live, fragile)
**What:** 1–2 new crate variants beyond the standard wobbly crate — a "live fish crate" that
squirms/slides on its own, a "glass crate" that shatters (and loses the sale) if dropped or bumped
too hard.
**Why it works:** Crashout Crew's absurd cargo variety (lemons, anvils, explosives, live primates
instead of plain crates) is called out in press material as the chaos/comedy driver, on top of the
same "tricky box-balancing" skill tension Port Boss already has. [Gematsu]
**Fits Port Boss:** reuses the existing crate-physics system; just new material/fail-state per type.
**Effort:** ~1.5–2h.

### 3. "Overboard" flail state instead of a hard fail
**What:** a player who falls in the water gets a short comedic flail/swim state and can climb back
up (on their own or pulled by a teammate) instead of a dead screen or long downtime.
**Why it works:** PEAK reviewers specifically complain "not much to do if you get yourself killed" —
dead players get bored and co-op energy drops. [GamingOnLinux, Game Informer] In a game built around
crates (and players) going in the water, this will happen constantly — it has to stay funny, not dead air.
**Fits Port Boss:** direct fix for our own highest-frequency "failure" moment.
**Effort:** ~1.5–2h depending on the current player-state code.

### 4. Storm lookout warning tell
**What:** a few seconds before a big wave/gust hits, give a clear audible+visual tell (bell, horn,
screen flash) so players can brace/catch crates in time.
**Why it works:** Sea of Thieves devs explicitly designed weather as a *mechanical* pressure, not
cosmetic — storms flood the lower deck and reopen patched leaks, and the crew-role split (one person
manning the crow's nest while the captain is blind past the sails) is named as the actual fun part of
the teamwork, not the storm visuals themselves. [news.xbox.com hands-on, pcgamer.com dev interview]
**Fits Port Boss:** turns "random dynamic weather makes the sea violent" from a chaos backdrop into
a reaction-timing co-op moment — directly on-brief.
**Effort:** ~1h (hook a tell into the existing weather-event trigger).

### 5. One moving/tipping hazard tile on the pier
**What:** a single environmental gimmick — a loose plank that tips when stood on, or a barrel that
rolls when bumped — placed somewhere on the dock.
**Why it works:** Overcooked 2's absurd shifting-kitchen gimmicks (rotating floors, hot-air balloons,
rivers) are named as its main novelty driver, on top of an already-low skill floor. [press-start.com.au,
twincitiesgeek.com]
**Fits Port Boss:** cheap variety injection using physics we already have; doesn't touch the core loop.
**Effort:** ~1.5–2h.

### 6. Instant replay on a big disaster
**What:** when a crate goes overboard or a multi-crate wipeout happens, a 1–2s slow-mo / camera
punch-in on the splash before returning to normal play.
**Why it works:** Content Warning's entire design is built around producing shareable clips from
co-op chaos, and reviewers explicitly credit the multiplayer chaos/comedy for carrying the game.
[purexbox.com] This is the cheapest version of that idea — no recording/upload system, just a beat.
**Fits Port Boss:** makes our existing funniest moment (crate splash) read better on camera for free.
**Effort:** ~1.5h.

### 7. Player shove / stumble
**What:** players can bump/shove each other for a minor knockback.
**Why it works:** Gang Beasts' core finding is it's "funnier with four players than two" specifically
because of physics-driven grapples/shoves as slapstick comedy — the game is thin on depth but the
shoving itself is the praised part. [pcgamer.com, bit-tech.net]
**Fits Port Boss:** reuses existing ragdoll/physics; adds a comedy layer between players, not just
players-vs-crates.
**Effort:** ~1h.

### 8. End-of-shift summary + next-boat/weather preview
**What:** a short screen between shifts: earnings recap + a peek at tomorrow's incoming boat and
weather.
**Why it works:** Stardew Valley's day loop is repeatedly named by critics as *the* addictive
mechanic — "just one more day" — precisely because you're always shown just enough of what's next to
pull you forward. [ballstatedaily.com, electrondance.com]
**Fits Port Boss:** cheap UI on top of data we already track (next boat, next weather roll).
**Effort:** ~1h.

### 9. Visible "next upgrade" progress goalpost
**What:** a sign or progress bar showing how close the harbor is to its next growth milestone
(pier → bigger dock, etc.).
**Why it works:** a long-time Supermarket Simulator player on Steam explicitly says the daily
stocking loop is repetitive on its own, but "saving up to expand the store" is what keeps it fun —
the *visible goal*, not the task, is what's addictive. [Steam discussion, commonsensegamer.com]
**Fits Port Boss:** we already have a pier-to-harbor growth arc; this just makes the next step visible.
**Effort:** ~1h.

### 10. Equal-split payout confirmation
**What:** make sure the end-of-boat payout UI clearly credits all connected co-op players equally,
not just the host.
**Why it works:** PowerWash Simulator's single most-cited co-op complaint is that a joining partner
doesn't progress their own save and earns only a fraction of the money. [mein-mmo.de, gamepressure.com]
**Fits Port Boss:** a correctness/fairness check, cheapest item on the list, prevents a complaint we
know in advance is coming.
**Effort:** <1h.

---

## Later (bigger, don't build tonight)

- **Rope/pull teammate-to-safety mechanic** — PEAK's whole co-op hook (secure each other, pull
  someone back from the edge/water) is a real system, not a toggle. Bigger than crate-throwing physics.
- **Hire NPC dock workers for passive help** — the delegation fantasy Schedule I and Supermarket
  Simulator players explicitly want (auto-stockers / hired dealers) once the player loop gets big.
  Needs its own AI/economy design.
- **Deeper crew-role specialization** (dedicated crane operator / lookout / loader roles rather than
  everyone doing everything) — the fuller version of idea #4 above, Sea of Thieves-style. Worth
  revisiting once the harbor is big enough to need it.
- **New harbor biomes / a second map** — Schedule I's top post-launch complaint is "nothing left but
  repeat the loop" once the story's done; players explicitly ask for a bigger map. Relevant once
  Port Boss's pier→container-harbor arc is actually finished.
- **In-game clip camera/upload system** — the full Content Warning version (not just idea #6's replay
  beat) — record-and-share as its own feature. Real scope, not a tonight add-on.

## Design warnings worth keeping in mind (not builds, just pitfalls to avoid)
- **Docked** (an actual harbor-crane sim) draws a repeated complaint that missions moving only 3–4
  containers feel "unrealistic and monotonous" at small scale — watch for this in Port Boss's early
  wooden-pier stage specifically.
- **Lethal Company / R.E.P.O. / Content Warning** all draw the same complaint once the novelty wears
  off: "same loop every run." Random weather + hazard modifiers (ideas #1 and #4) are our hedge
  against that, not optional polish.
- **Moving Out**'s biggest complaint is physics that feel inconsistent/arbitrary rather than wacky-on-
  purpose — keep crate/player physics rules predictable even when they're silly.
