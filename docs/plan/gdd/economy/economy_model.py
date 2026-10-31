"""Port Boss economy model: one source for economy.xlsx, the charts and the tables in economy.md.

Run:  python economy_model.py
Writes economy.xlsx, money-over-time.png, income-mix.png, and refreshes every
<!--T:name--> ... <!--/T--> block in economy.md. Ends with assert self-checks.
Fish prices, grades, freshness and bait come from gdd/fishing (w-gdd-fishing, 2026-10-31);
clock and places from gdd/world + gdd/systems. Every other number here is ours (SUGGESTED) to tune.
"""
import re
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.comments import Comment

HERE = Path(__file__).parent

# ---------------------------------------------------------------- global assumptions
A = {  # name: (value, note)
    "real_min_per_game_day": (24, "gdd/world + gdd/systems: 1 in-game hour = 1 real minute"),
    "days_per_real_hour": (2.8, "sleep skips the night; matches gdd/world's act day ranges (Day 40 at ~14 h, 2 players)"),
    "start_cash": (40, "plus June's free bag of worms (gdd/world prologue), Biscuit (damaged), Walt's bamboo rod"),
    "fish_per_hour_hand": (70, "gdd/fishing: ~20 s cycle, 60-80 landed per real hour incl. travel and selling"),
    "avg_fish_worms": (16, "gdd/fishing model (15-fish roster, Manan 31 Oct): Walt's bamboo + worms ~$16/fish practised, ~$13 first-timer"),
    "blended_sell_factor": (0.75, "S1 mix: Pratt pays 60% for about half the catch; Mari, June, Amara and orders pay 100-175%"),
    "pratt_factor": (0.60, "Old Saltmouth Fish Market (Voss-owned, Gideon Pratt) pays 40% under fair (gdd/world)"),
    "order_factor_pelican": (1.75, "The Pelican (chef Amara Bell) orders pay 150-200% of base"),
    "cannery_factor": (0.50, "Sorrel Isle cannery (Act 3): 50% of base, takes any amount, no saturation"),
    "saturation_step": (0.02, "each same-species fish sold at one buyer in one day: -2%"),
    "saturation_floor": (0.60, "saturation never drops a price below 60%; resets at 07:00"),
    "fuel_price": (1.50, "$ per litre at Doyle's fuel dock; our own pump buys at 0.90"),
    "upkeep_rate_per_day": (0.002, "dock + fleet upkeep: 0.2% of built value per day, charged at 07:00"),
}

# ---------------------------------------------------------------- stages = gdd/world acts
SOURCES = ["Hand fishing", "Restaurant/hotel orders", "Mooring fees", "Workers fishing",
           "Services (fuel, bait, ice)", "Ship orders", "Shipping consignments"]
# EFFECTIVE income multiplier per source for 1/2/3/4 players. Order and ship sizes scale by gdd/systems'
# 1.0/1.6/2.1/2.5, but the number of offers per day does not, and players share boats, buyers and travel,
# so the hourly gain is lower. Automation (moorers, workers, services) does not scale with players.
COOP = {
    "Hand fishing": [1, 1.45, 1.8, 2.1],
    "Restaurant/hotel orders": [1, 1.35, 1.6, 1.8],
    "Mooring fees": [1, 1, 1, 1],
    "Workers fishing": [1, 1, 1, 1],
    "Services (fuel, bait, ice)": [1, 1, 1, 1],
    "Ship orders": [1, 1.3, 1.5, 1.7],
    "Shipping consignments": [1, 1.15, 1.25, 1.3],
}
# solo net $ per real hour, averaged over the stage, after bait/fuel/wages/upkeep for that source.
# target = solo hours range the check enforces; act = gdd/world act; act2p = world's 2-player hours.
STAGES = [
    dict(key="S1", name="Fisherman", act="Prologue + Act 1 The Mooring", act2p="0-1.5 h", target=(1.5, 2.3),
         rate=[1000, 80, None, 0, 0, 0, 0], gate="Amara's first order delivered; Biscuit fixed",
         spend=[("Biscuit engine rebuild", 90), ("Biscuit hull patch", 120), ("Ingrid's three planks + bench", 100),
                ("Fibreglass rod", 450), ("Braided line", 300), ("Tackle pouch to 5 baits", 250), ("Landing net", 150),
                ("Warehouse T1: Patched Warehouse", 300), ("Fix Walt's bed", 120), ("Biscuit paint job", 150),
                ("Biscuit Engine I", 300)]),
    dict(key="S2", name="Small trader", act="Act 2 The Landing Wakes", act2p="1.5-4 h", target=(2.6, 3.8),
         rate=[1700, 700, 450, 700, 0, 0, 0], gate="3 moorers, Patched Office, first worker, second boat",
         spend=[("Office T1: Patched Office", 2000), ("Kestrel launch", 5500), ("Repair berths B2 + B3", 650),
                ("Stove, desk, chair", 900), ("First deckhand hire fee", 230), ("Carbon rod", 1600),
                ("Power post for Mari", 600)]),
    dict(key="S3", name="Dock owner", act="Act 3 Visitors in the Fog", act2p="4-7 h", target=(2.8, 4.0),
         rate=[2400, 1600, 1100, 3500, 600, 3500, 0], gate="P2 pier built, first ship order, goons survived",
         spend=[("P2 finger piers (6 berths)", 12000), ("Office T2: Harbour Office", 8000),
                ("Security: 4 lamps, chain moorings, watchman hut", 4500), ("Puffin trawler", 18000)]),
    dict(key="S4", name="Port owner", act="Act 4 Steel and Salt", act2p="7-11 h", target=(4.0, 5.5),
         rate=[3500, 3000, 2500, 9000, 2500, 30000, 20000], gate="P3 deep berth + crane, shipping boat, Cut half dug",
         spend=[("P3 deep-water berth", 60000), ("Harbour crane", 90000), ("Tern coaster (shipping boat)", 120000),
                ("Office T3: Port Authority Building", 40000), ("Dredge Walt's Cut, part 1", 30000)]),
    dict(key="S5", name="Rival endgame", act="Act 5 The Charter", act2p="11-14 h", target=(3.0, 4.3),
         rate=[5000, 6000, 3500, 15000, 5000, 61500, 50000], gate="Cut open + swing bridge, 4 of 7 allies, the vote",
         spend=[("Finish dredging Walt's Cut", 80000), ("Swing bridge", 250000), ("Office T4: Hale Tower", 150000),
                ("Allies' request chains (materials, gifts)", 50000)]),
]
TOTAL_TARGET = {1: (15, 19), 2: (12.5, 15), 4: (10, 13.5)}  # hours to the Charter vote (world: ~14 h at 2P)
# S1 mooring $/h from Mari's arrival (Manan 31 Oct): Act 1 = Days 1-4; she pays from the morning after she moors,
# fees in S1 = S1_DAYS - her arrival day (first-timer run of first_days()); plus ~$10/h of transient boats.
S1_DAYS = 4

def stage_rate(st, players):
    return sum(r * COOP[s][players - 1] for r, s in zip(st["rate"], SOURCES))


def stage_spend(st):
    return sum(c for _, c in st["spend"])


def stage_bounds(players):
    t, out = 0.0, []
    for st in STAGES:
        h = stage_spend(st) / stage_rate(st, players)
        out.append((t, t + h, stage_rate(st, players)))
        t += h
    return out


def earned_at(h, bounds):
    return sum(r * max(0.0, min(h, e) - s) for s, e, r in bounds)


# ---------------------------------------------------------------- first days, solo (Manan 31 Oct: Mari on Day 3 or at $250 total earned; engine $90)
MARI_DAY, MARI_EARNED, MARI_FEE, MARI_QUOTA = 3, 250, 40, 10   # MUST (Manan 31 Oct): Day 3 07:00 or TOTAL EARNED >= $250
# Fair value per fish on Walt's bamboo rod + worms (sprat/mullet/mackerel), from gdd/fishing/fishing_model.py:
# practised $16.2; a first-timer (novice grades x1.76 vs x2.14) ~$13.
FIRST_FISH = {"first-timer": 13.0, "practised": 16.0}
# (day, time, real minute, text, kind, arg). kinds: trip (fish caught, worms used), sell (Mari first, then Pratt if open),
# buy (wishlist at a shop visit), order (fixed $), wake (Mari's fee; Day-3 arrival check)
FIRST_SCRIPT = [
    (1, "07:00", 0, "Wake in Walt's office. Letter, key, journal. June's free bag of worms (10)", "start", None),
    (1, "10:00–14:00", 3, "Trip 1, the Shallows: 10 casts → 9 fish (sprat, mullet, mackerel)", "trip", (9, 10)),
    (1, "15:00", 8, "First sale. Pratt (60%): \"$6 for a $10 mackerel. Take it or watch it rot.\"", "sell", "15"),
    (1, "16:00", 9, "June: 3 packs of worms ($15). Doyle's slipway", "buy", 15),
    (1, "17:00–23:00", 10, "Trip 2 at dusk: 15 casts → ~14 fish, kept overnight (Pratt shut at 16:00)", "trip", (14, 15)),
    (2, "07:00", 21, "Sleep, wake Day 2 (bed broken: no Rested buff)", "wake", None),
    (2, "07:30", 22, "Sell last night's fish (Good, 85%). Amara's first order arrives: 3 mackerel ★★+, ~$90, by Day 3 evening", "sell", "07"),
    (2, "08:00", 23, "Shops: worms + shrimp ($40)", "buy", 40),
    (2, "09:00–15:00", 24, "Trip 3: 16 casts → ~15 fish", "trip", (15, 16)),
    (2, "15:00", 30, "Sell", "sell", "15"),
    (2, "16:00", 31, "Shops", "buy", 0),
    (2, "17:00–23:00", 32, "Trip 4: ~14 fish, kept", "trip", (14, 15)),
    (3, "07:00", 42, "Wake Day 3", "wake", None),
    (3, "09:00", 44, "Sell last night's fish", "sell", "09"),
    (3, "09:30", 44, "Shops: worms + shrimp ($40)", "buy", 40),
    (3, "10:00–15:00", 45, "Trip 5: ~13 fish incl. Amara's 3 mackerel", "trip", (13, 14)),
    (3, "15:00", 50, "Sell", "sell", "15"),
    (3, "16:00", 51, "Shops", "buy", 0),
    (3, "18:00", 53, "Deliver Amara's order (3 mackerel, ~$90)", "order", 90),
]
FIRST_WISHLIST = [("Biscuit engine rebuild", 90), ("Ingrid's three planks + bench", 100), ("Biscuit hull patch", 120)]
FIRST_RESERVE = 5  # keep $5 cash after an upgrade (the visit's bait is already paid)


def first_days(fish_value):
    cash, earned, held, mari, mari_left, wish, rows = 40.0, 0.0, [], None, 0, list(FIRST_WISHLIST), []

    def mari_check(day, t):
        nonlocal mari, mari_left
        if mari is None and (earned >= MARI_EARNED or day >= MARI_DAY):
            mari, mari_left = (day, t), MARI_QUOTA
            why = f"total earned reached ${MARI_EARNED}" if earned >= MARI_EARNED and day < MARI_DAY else f"Day {MARI_DAY}"
            return f" **Mari texts ({why}):** \"Can I moor at your place? ${MARI_FEE} a day + tacos.\" Accept: she ties up at B1"
        return ""

    for day, t, rmin, text, kind, arg in FIRST_SCRIPT:
        note = ""
        if kind == "trip":
            n, casts = arg
            cash -= 0.5 * casts if not (day == 1 and t.startswith("10")) else 0  # trip 1 uses June's free worms
            held.append((day, n))
        elif kind == "sell":
            gross = 0.0
            for d, n in held:
                fresh = 1.0 if d == day else 0.85
                to_mari = min(n, mari_left) if mari and int(arg) >= 9 else 0  # Mari's Tacos opens 09:00
                mari_left -= to_mari
                gross += to_mari * fish_value * fresh + (n - to_mari) * fish_value * fresh * A["pratt_factor"][0]
            held, cash, earned = [], cash + gross, earned + gross
            note = f" (+${gross:,.0f})"
        elif kind == "buy":
            cash -= arg
            bought = []
            while wish and (day > 1 or wish[0][0] == "Biscuit engine rebuild") and cash - wish[0][1] >= FIRST_RESERVE:
                name, price = wish.pop(0)
                cash -= price
                bought.append(f"{name} ${price}")
            note = (": bought " + ", ".join(bought)) if bought else (": nothing affordable yet" if wish else "")
        elif kind == "wake":
            if mari and mari[0] < day:
                cash += MARI_FEE
                earned += MARI_FEE
                note = f". Mari pays **${MARI_FEE}** (\"💸 Rent's in, landlord\")"
            mari_left = MARI_QUOTA
        elif kind == "order":
            cash += arg
            earned += arg
        note += mari_check(day, t)
        rows.append((rmin, f"Day {day} {t}", text, note.strip(" .:"), cash))
    return rows, mari


def first_days_table():
    runs = {k: first_days(v) for k, v in FIRST_FISH.items()}
    a, b = runs["first-timer"][0], runs["practised"][0]
    cell = lambda r: f"**{money(int(r[4]))}**" + (f" · {r[3]}" if r[3] else "")  # noqa: E731  (floor: $249.6 shows as $249)
    rows = [(r1[0], r1[1], r1[2], cell(r1), cell(r2)) for r1, r2 in zip(a, b)]
    return md_table(["Real min", "In-game", "What happens", "First-timer ($13/fish): cash · result",
                     "Practised ($16/fish): cash · result"], rows), {k: v[1] for k, v in runs.items()}


_MARI_DAY_S1 = first_days(FIRST_FISH["first-timer"])[1][0]
STAGES[0]["rate"][2] = round(MARI_FEE * max(0, S1_DAYS - _MARI_DAY_S1) / S1_DAYS * A["days_per_real_hour"][0] + 10)


# ---------------------------------------------------------------- shop data (price, unlock, tag, notes)
BAIT = [  # name, price, casts, unlock, tag, note
    ("June's free bag of worms", "free", 10, "prologue (gdd/world)", "SUGGESTED", "\"On the house. For Walt.\""),
    ("Dug worms (beach, low tide)", "free", "10/day", "Act 1", "SUGGESTED", "anti-softlock: you can always fish; also R6 counterplay"),
    ("Worms", 5, 10, "start", "MUST (bait is buyable)", "favourite of sprat, mullet, mackerel; ~$16/fish on the bamboo rod (gdd/fishing model)"),
    ("Shrimp", 12, 10, "Act 1", "MUST", "favourite of garfish, bream; ~$36/fish on the Fibreglass rod"),
    ("Sardine", 18, 10, "Act 2", "SUGGESTED", "favourite of conger, mahi, blue shark; ~$73/fish on the Carbon rod"),
    ("Squid", 25, 10, "Act 2", "SUGGESTED", "favourite of snapper, grouper, sunfish, swordfish, anglerfish; ~$150/fish on the Deep-sea rod"),
    ("Walt's Mix (best bait)", 40, 1, "once you own Old Faithful", "MUST (Manan 31 Oct)", "$40 each at June's; the only bait for Old Brass; big Trophy fish love it; ~$390/fish on Old Faithful"),
    ("Chum bucket", 20, "3 min", "Act 1", "SUGGESTED", "more bites from the rarer half of the pool; never changes the 4-10 s bite timer"),
]
TACKLE = [
    ("Walt's bamboo rod", "start", "start", "MUST (rod in hotbar)", "green ×1.00, pointer ×1.00, bite 4–10 s. Catches sprat, mullet, mackerel (MUST 3 per rod, Manan 31 Oct)"),
    ("Fibreglass rod", 450, "start (visible goal)", "SUGGESTED", "green ×1.15, pointer ×0.95, bite 4–9 s. Adds garfish, bream, conger (Manan 31 Oct)"),
    ("Carbon rod", 1600, "Act 2", "SUGGESTED", "green ×1.30, pointer ×0.90, bite 4–8 s. Adds red snapper, mahi-mahi, dusky grouper (Manan 31 Oct)"),
    ("Deep-sea rod", 6000, "Act 3", "SUGGESTED", "green ×1.40, pointer ×0.85, bite 4–7 s. Adds sunfish, blue shark, swordfish (Manan 31 Oct)"),
    ("Old Faithful (Walt's rod, restored)", 15000, "journal quest, Act 4", "MUST (best rod for the legend, Manan 31 Oct)", "green ×1.50, pointer ×0.80, bite 4–6 s. Adds anglerfish, oarfish and Old Brass (with Walt's Mix)"),
    ("Braided line", 300, "start", "SUGGESTED", "higher weight rating: fewer HEAVY fish (gdd/fishing §6.4) (Kufu review 31 Oct)"),
    ("Tackle pouch: all 5 baits", 250, "start", "SUGGESTED", "the pouch starts with 3 bait types; this makes it 5 (Manan 31 Oct: 5 baits)"),
    ("Landing net", 150, "start", "SUGGESTED", "co-op: a friend readies the net: +1 retry, +10% green (gdd/fishing §4.2) (Kufu review 31 Oct)"),
    ("Cast net", 200, "Act 1", "SUGGESTED", "catch sprats as free bait (gdd/fishing)"),
    ("Fish guide book", 80, "start", "SUGGESTED", "shows which fish each bait and rod brings"),
    ("Boat cooler (portable)", 300, "start", "OPEN QUESTION", "4 fish slots on any boat; needs Manan's OK (fish never in crates)"),
    ("Fishing hats, vests (cosmetic)", "40-200", "start", "SUGGESTED", "cosmetic only"),
]
CLOTHES = [  # slot, price range, examples, tag
    ("Headwear", "40-250", "beanies, sou'westers, hard hats, flat caps, captain's cap", "MUST (clothes-only edits)"),
    ("Top", "60-300", "tees, flannels, striped long-sleeves, Hawaiian shirt", "MUST"),
    ("Over", "80-400", "hi-vis vest, oilskin, puffer, captain's jacket", "MUST"),
    ("Legs", "60-250", "cargos, bib overalls, shorts + socks", "MUST"),
    ("Boots", "80-350", "rubber boots, steel-toes, deck shoes", "MUST"),
    ("Gloves", "30-150", "rubber, leather, gauntlets", "MUST"),
    ("Colour dye (per item)", "25", "any owned item, 24 swatches; the player colour band is fixed and not for sale", "SUGGESTED"),
    ("Story sets", "free", "Walt's old captain set (journal), Voss-port uniform (ending 2), Teddy Mango's shirt (cartel)", "SUGGESTED"),
]
SURF = [
    ("Dawn Patrol lesson with Kai", "free", "Act 2 event (gdd/world)", "SUGGESTED", "tutorial + free foam board rental"),
    ("Foam board", 350, "Act 2", "MUST (surf gear)", "easy waves, stable"),
    ("Bodyboard", 200, "Act 2", "MUST", "lie-down; good for co-op races"),
    ("Longboard", 1200, "Act 2", "MUST", "cruisy, nose rides"),
    ("Shortboard", 900, "Kai's trick challenge 1", "MUST", "fast, tricks"),
    ("SUP paddleboard", 700, "Act 2", "SUGGESTED", "paddle the harbour, fish from it (worms only)"),
    ("Wetsuit", 400, "Act 2", "MUST", "swim stamina +50%, cold nights"),
    ("Fins", 120, "Act 2", "SUGGESTED", "swim speed +30% (falling off boats happens)"),
    ("Mask + snorkel", 150, "Act 2", "SUGGESTED", "see underwater collectibles"),
    ("Wax, grip pads, leash", "10-60", "Act 2", "SUGGESTED", "grip + cosmetic"),
    ("Jet ski", 6000, "Act 3", "SUGGESTED", "18 m/s personal ride, rider + 1"),
    ("Weekly surf contest entry", 50, "Sundays from Act 2", "SUGGESTED", "prizes $500 / $1,500 / $4,000 (rises by act)"),
]
# id, name, tier, role, length, deck, speed, fuel L, L/min, hold slots, worker slots, players, price, unlock
BOATS = [
    ("F1", "Biscuit", "T1", "fishing", "5 m", "open wooden skiff: tiller aft, one bench, bow locker; leaks until patched",
     "4.5 (damaged) / 6", 15, 0.5, "0 (+4 cooler OQ)", 1, "2 (3 squeezed)", 0, "start (Walt's)"),
    ("F2", "Kestrel", "T2", "fishing", "7.5 m", "centre console, cuddy cabin with 1 bunk, aft fishing deck, swim ladder",
     "9", 50, 1.2, "6", 1, "4", 5500, "start (visible goal)"),
    ("F3", "Puffin", "T3", "fishing", "12 m", "wheelhouse (wheel + chart table), aft work deck with net drum, roof ladder, hold hatch",
     "11", 240, 3.0, "20 (cold)", 2, "4", 18000, "Act 3"),
    ("F4", "Swordfish", "T4", "fishing", "10 m", "flybridge (ladder), fighting chair aft, cabin with 2 bunks",
     "16", 320, 7.0, "6", 1, "4", 45000, "Act 4"),
    ("F5", "Albatross", "T4+", "fishing", "24 m", "2 decks: bridge, galley, 4 bunks, factory deck, stern ramp",
     "10", 1600, 10.0, "80 (cold)", 6, "4", 150000, "Endless"),
    ("S1", "Bulldog", "service", "tug", "18 m", "wheelhouse, push bow, towing winch aft",
     "9", 900, 6.0, "-", 1, "4", 50000, "Act 4 (optional; else Dot's pilot $800/ship)"),
    ("C1", "Tern", "cargo", "shipping", "35 m", "bridge aft, 12-container hold, own deck crane, 3 cabins",
     "8", 6000, 18.0, "12 TEU", 3, "4", 120000, "Act 4 (MUST: shipping boat)"),
    ("C2", "Cormorant", "cargo", "shipping", "80 m", "bridge tower, walkable deck between container rows, crew mess",
     "8", 30000, 45.0, "80 TEU", 6, "4", 600000, "Endless"),
    ("C3", "Halcyon", "cargo", "shipping (flagship)", "140 m", "300 TEU, bridge with balcony wings, walkable engine room",
     "9", 90000, 110.0, "300 TEU", 10, "4", 1800000, "Endless"),
]
CLASS_FACTOR = {"Biscuit": 1, "Kestrel": 2, "Puffin": 5, "Swordfish": 8, "Albatross": 15, "Bulldog": 10,
                "Tern": 20, "Cormorant": 40, "Halcyon": 80}
BOAT_UPGRADES = [  # name, base price (x class factor), effect, tag
    ("Engine I", 300, "+15% speed", "MUST (boat upgrades)"),
    ("Engine II", 900, "+30% speed (needs Engine I)", "MUST"),
    ("Long-range tank", 250, "+50% fuel", "SUGGESTED"),
    ("Fish box", 300, "+50% hold slots (Biscuit: +4)", "OPEN QUESTION (boat storage)"),
    ("Ice lining", 400, "fish in the boat's box rot at half speed", "SUGGESTED"),
    ("Fish finder", 500, "shows fish shadows + spot quality", "SUGGESTED"),
    ("Rod holders", 250, "+20% worker catch", "SUGGESTED"),
    ("Deck lights", 300, "night fishing on deck (night fish bite a little more: conger, swordfish, anglerfish)", "SUGGESTED"),
    ("Hull plating", 600, "-50% collision damage", "SUGGESTED"),
    ("Bilge pump", 150, "auto-bails leaks", "SUGGESTED"),
    ("Locked fuel cap", 50, "blocks R11 engine sabotage (gdd/world)", "SUGGESTED"),
    ("Autopilot", 1500, "sail to a phone waypoint (Act 3+)", "SUGGESTED"),
]
COSMETICS = [
    ("Hull paint (24 swatches)", 150, "MUST (colours)"), ("Trim / accent colour", 80, "MUST"),
    ("Stripe pattern (8)", 120, "SUGGESTED"), ("Name decal (first one free)", 50, "SUGGESTED"),
    ("Flag (12 + Walt's)", 60, "SUGGESTED"), ("Deck wood stain", 100, "SUGGESTED"),
    ("Horn sound (6)", 200, "SUGGESTED"), ("Livery: Walt's Classic", "free (journal)", "SUGGESTED"),
]
DOCK = [  # category, item, price, unlock, effect, tag
    ("Piers & berths", "Ingrid's offer: fix three planks (+ free bench)", 100, "Act 1 day 3", "first dock upgrade", "SUGGESTED"),
    ("Piers & berths", "Repair berth B2", 250, "Act 2", "+1 moorer slot (B1 is usable from day 1)", "MUST (moorers)"),
    ("Piers & berths", "Repair berth B3", 400, "Act 2", "+1 moorer slot", "MUST"),
    ("Piers & berths", "P2 finger piers, Cannery Flats (6 berths)", 12000, "Act 3", "+6 slots, worker boats", "MUST (multiple piers)"),
    ("Piers & berths", "P3 deep-water berth", 60000, "Act 4", "ship orders with containers, Tern", "MUST"),
    ("Piers & berths", "P4 second deep-water berth", 150000, "Endless", "two big ships at once", "SUGGESTED"),
    ("Walt's Cut", "Dredge the Cut, part 1 (barge rent + 3 dredge jobs)", 30000, "Act 4", "Cut half open at high tide", "SUGGESTED (gdd/world)"),
    ("Walt's Cut", "Finish dredging", 80000, "Act 5", "Cut open; Voss Harbour 7.3 min in Biscuit (was 9.6), 4.8 in the Kestrel; ships reach P3 directly (Kufu review 31 Oct)", "SUGGESTED"),
    ("Walt's Cut", "Swing bridge", 250000, "Act 5", "ships reach our side; the Charter goal", "SUGGESTED"),
    ("Mooring amenities", "Power post (per berth)", 600, "Act 2", "moorer fee +30%; R4 counterplay", "SUGGESTED"),
    ("Mooring amenities", "Water tap (per berth)", 350, "Act 2", "moorer fee +20%", "SUGGESTED"),
    ("Mooring amenities", "Dock lamp", 200, "start", "fee +10% nearby; night sabotage -50% (4 lamps)", "SUGGESTED"),
    ("Mooring amenities", "Chain moorings (per berth)", 300, "Act 3", "blocks R5 cut lines", "SUGGESTED"),
    ("Mooring amenities", "Life rings + fire buckets", 150, "Act 3", "passes R7 inspection", "SUGGESTED"),
    ("Services", "Bait vending machine", 1500, "Act 2", "passive bait sales to visitors", "SUGGESTED"),
    ("Services", "Ice machine", 1200, "Act 2", "boat fish boxes rot at half speed (the warehouse has its own cold room)", "SUGGESTED"),
    ("Services", "Fuel pump", 7000, "Act 3", "buy at $0.90/L, sell to visitors at $1.60/L", "SUGGESTED"),
    ("Cranes", "Mobile crane hire (per ship order)", 1500, "Act 3", "the first ship orders before we own a crane", "SUGGESTED (gdd/world)"),
    ("Cranes", "Hand winch / davit", 900, "Act 2", "lift Trophy fish and heavy crates", "SUGGESTED"),
    ("Cranes", "Jib crane", 15000, "Act 3", "own crane for small ship orders (no hire fee)", "SUGGESTED"),
    ("Cranes", "Harbour crane", 90000, "Act 4", "containers", "MUST (cranes)"),
    ("Cranes", "Ship-to-shore gantry", 400000, "Endless", "big container ships, 2x speed", "SUGGESTED"),
    ("Storage", "Warehouse T0: Walt's Old Shed", "free", "Day 1", "12 slots of 10 fish; fish, tools, collectibles, spare bait", "MUST (Manan 31 Oct: warehouse from the start)"),
    ("Storage", "Warehouse T1: Patched Warehouse", 300, "Day 1-2", "24 slots", "MUST (expandable, Manan 31 Oct)"),
    ("Storage", "Warehouse T2: Cold Room", 2500, "Act 2", "+12 cold slots: fish rot at 25%", "MUST (expandable, Manan 31 Oct)"),
    ("Storage", "Warehouse T3: Big Warehouse", 8000, "Act 3", "48 slots + 24 cold; forklift lane for workers' catch", "MUST (expandable, Manan 31 Oct)"),
    ("Storage", "Warehouse T4: Ice House", 30000, "Act 4", "96 slots + 48 cold (rot at 10%); hotel bulk orders", "MUST (expandable, Manan 31 Oct)"),
    ("Storage", "Cargo shed (pallets)", 35000, "Act 4", "store unloaded cargo, sell later", "SUGGESTED"),
    ("Storage", "Container yard (40 TEU)", 50000, "Act 4", "store containers", "SUGGESTED"),
    ("Living", "Fix Walt's bed (the cot)", 120, "start", "sleep with the Rested buff", "MUST (beds)"),
    ("Living", "Bunk bed", 350, "start", "extra co-op sleep spot", "MUST"),
    ("Living", "Double bed", 700, "Act 2", "comfort +3", "SUGGESTED"),
    ("Living", "Hammock", 200, "start", "sleep aboard boats with a cabin", "SUGGESTED"),
    ("Furniture", "Desk / chair / shelves / wardrobe", "80-400", "start", "comfort +1 each; wardrobe = free outfit swaps", "MUST (furniture)"),
    ("Furniture", "Map table", 900, "Act 2", "big map of spots, orders, worker boats", "SUGGESTED"),
    ("Furniture", "Trophy mount (per fish)", 300, "first Trophy", "display Trophy/Legend fish", "SUGGESTED"),
    ("Furniture", "Collectibles cabinet", 600, "first collectible", "display collectibles", "SUGGESTED"),
    ("Furniture", "Stove", 400, "start", "cook fish: Well-fed buff", "SUGGESTED"),
    ("Furniture", "Balcony (office)", 3000, "Act 2", "the place to look at your port (gdd/world)", "SUGGESTED"),
    ("Furniture", "Rugs, plants, lamps, paintings", "30-500", "start", "comfort; pure style", "SUGGESTED"),
    ("Office", "T1 Patched Office", 2000, "Act 2", "liveable office, phone Crew app full, 3 staff", "MUST (office upgrades)"),
    ("Office", "T2 Harbour Office", 8000, "Act 3", "8 staff, ship orders on the phone, ledger", "MUST"),
    ("Office", "T3 Port Authority Building", 40000, "Act 4", "20 staff, consignments, insurance desk", "MUST"),
    ("Office", "T4 Hale Tower", 150000, "Act 5", "40 staff, Charter campaign room, top-floor view", "SUGGESTED"),
    ("Security", "Watchman hut (for Old Lowe)", 2500, "Act 3", "lets you hire a night watchman", "SUGGESTED"),
    ("Security", "Gate + fence", 3000, "Act 3", "goons take 2x longer on the dock", "SUGGESTED"),
    ("Security", "CCTV", 6000, "Act 4", "auto-evidence photo of night sabotage", "SUGGESTED"),
]
WORKER_JOBS = [  # job, unlock, wage formula / day, what they do, tag
    ("Deckhand (fisher)", "Act 2 (Mooring Line job board)", "70 + 22.5 x (Fishing + Seamanship)", "fishes from an assigned boat (phone)", "MUST (first automation)"),
    ("Pim (unique)", "Act 2 day ~10", "free week 1, then 115", "skill-1 deckhand; warns of R11 sabotage", "SUGGESTED (gdd/world)"),
    ("Dockhand", "Act 2", "120 flat", "sells the warehouse catch at the best open buyer; fills simple orders", "SUGGESTED"),
    ("Mechanic", "Act 3", "180 flat", "repairs docked boats overnight at 50% parts cost", "SUGGESTED"),
    ("Night watchman (Old Lowe)", "Act 3 + watchman hut", "150 flat", "blocks R5 and R11 except 03:00-05:00 (gdd/world)", "SUGGESTED"),
    ("Crane operator", "Act 4", "300 flat", "unloads ship orders at 70% of player speed", "SUGGESTED"),
    ("Tamsin Wade (unique)", "Act 4", "400", "ex-Voss crane operator: crane works at 100% of player speed", "SUGGESTED (gdd/world)"),
    ("Nils Berg (unique)", "Act 2", "160", "Fishing 2 / Seamanship 3, never no-shows", "SUGGESTED (gdd/world)"),
    ("\"Hook\" Hennessy (unique)", "side quest", "200", "immune to goon intimidation; doubles as a guard", "SUGGESTED (gdd/world)"),
    ("Tug skipper", "Act 4 + Bulldog", "300 flat", "berths ships with the Bulldog", "SUGGESTED"),
    ("Ship captain", "Act 4", "1,200 (Tern) / 3,000 (Cormorant)", "runs shipping consignments", "MUST (shipping)"),
]
VENDORS = [  # who, boat, fee/day, needs, unlock, perk, tag  (cast: gdd/world 6.3)
    ("Mari Duarte", "Mari's Tacos", 40, "berth B1 (usable day 1)", "Day 3 07:00, or when total earned reaches $250, whichever is first (MUST, Manan 31 Oct); ~Day 2 15:00 in the sim",
     "free tacos; buys up to 10 fish/day at 100%; small fish orders; fee +30% with a power post", "MUST (first vendor)"),
    ("Hugo & Fifi Laurent", "houseboat Petit Pois", 60, "berth B2", "Act 2", "+1 day of warning on rival actions", "SUGGESTED (gdd/world)"),
    ("Ola Lindqvist", "Ola's Ice (ice barge)", 50, "berth B3", "Act 2", "fish on the warehouse racks rot 50% slower; ice packs $5", "SUGGESTED (gdd/world)"),
    ("Sven Moss", "salvage diver boat", 80, "P2 berth", "Act 3", "salvage contracts at the Margaret wreck", "SUGGESTED (gdd/world)"),
    ("The Pickle Sisters", "floating veg market", 90, "P2 berth + water tap", "Act 3", "cheap stamina food $6", "SUGGESTED (gdd/world)"),
    ("Captain Mags", "ferry stop", 150, "P2 berth", "Act 4", "letters direct; tourist charter contracts $300-1,200", "SUGGESTED (gdd/world)"),
    ("Dr Imani Cole", "floating clinic", 40, "P2 berth + power post", "Act 4", "stamina regenerates faster at the Landing (pays little: volunteer)", "SUGGESTED (gdd/world)"),
    ("Visiting superyachts", "random owners", "600/night", "P3 berth free that night", "Act 4", "tips + cosmetic unlocks", "SUGGESTED"),
    ("Transient boats (phone requests)", "random", "30-400/night", "any free berth", "Act 1 day 3+", "accept or decline by phone", "SUGGESTED"),
]
SHIP_ORDERS = [  # ship, stage, cargo, berth fee, per unit, units (solo), window (game h), needs
    ("MV Hilda (small coaster)", "S3", "containers", 1000, 200, 12, 4, "P2 pier + hired mobile crane (gdd/world Act 3)"),
    ("Sunrise Reefer (fruit coaster)", "S3", "fruit pallets", 1500, 150, 20, 4, "jib crane + cold room"),
    ("Feeder ship (visiting)", "S4", "containers", 5000, 500, 40, 6, "P3 berth + harbour crane + tug or Dot's pilot"),
    ("Mainline ship (visiting)", "S5", "containers", 10000, 400, 120, 10, "P3 + harbour crane; gantry in endless"),
]
CONSIGNMENTS = [  # boat, stage, TEU, $/TEU, trip game-days, captain wage/day, fuel/trip
    ("Tern", "S4", 12, 1300, 1.5, 1200, 1800),
    ("Tern (Charter season)", "S5", 12, 2600, 1.5, 1200, 1800),
    ("Cormorant", "Endless", 80, 1500, 2, 3000, 8000),
    ("Halcyon", "Endless", 300, 1400, 3, 6000, 25000),
]
ENDLESS = [  # purchase, price
    ("Albatross stern trawler", 150000), ("Cormorant feeder", 600000), ("Ship-to-shore gantry", 400000),
    ("P4 second deep-water berth", 150000), ("Halcyon flagship", 1800000),
    ("Ending 2 only: buy out Voss's debt", 3000000),
]


def money(v):
    return f"${v:,.0f}" if isinstance(v, (int, float)) else str(v)


def md_table(head, rows):
    out = ["| " + " | ".join(head) + " |", "|" + "---|" * len(head)]
    out += ["| " + " | ".join(str(c) for c in r) + " |" for r in rows]
    return "\n".join(out)


def worker_day(skill, cap, base, bait_cost_per_fish=1.2, fuel=12):
    wage = 70 + 45 * skill
    catch = min(10 + 5 * skill, cap)
    gross = catch * base * (1 + 0.15 * skill) * A["blended_sell_factor"][0]
    fail = (14 - 2 * skill) / 100
    net = gross * (1 - fail * 0.5) - wage - catch * bait_cost_per_fish - fuel * A["fuel_price"][0]
    return wage, catch, gross, fail, net


# ---------------------------------------------------------------- tables for economy.md
def tables():
    T = {}
    T["bait"] = md_table(["Bait", "Price", "Casts", "Unlock", "Tag", "Note"],
                         [(b[0], money(b[1]), b[2], b[3], b[4], b[5]) for b in BAIT])
    T["tackle"] = md_table(["Item", "Price", "Unlock", "Tag", "Effect"], [(t[0], money(t[1]), t[2], t[3], t[4]) for t in TACKLE])
    T["clothes"] = md_table(["Slot", "Price $", "Examples", "Tag"], CLOTHES)
    T["surf"] = md_table(["Item", "Price", "Unlock", "Tag", "Effect"], [(t[0], money(t[1]), t[2], t[3], t[4]) for t in SURF])
    rows = []
    for b in BOATS:
        rng = f"{b[7] / b[8]:.0f} min" if b[8] else "-"
        rows.append((b[0], f"**{b[1]}**", b[2], b[3], b[4], b[5], b[6], f"{b[7]:,} L", f"{b[8]} L/min", rng, b[9], b[10], b[11],
                     money(b[12]) if b[12] else "inherited", b[13]))
    T["boats"] = md_table(["Id", "Name", "Tier", "Role", "Length", "Walkable deck", "Speed m/s", "Tank", "Burn", "Range",
                           "Hold", "Worker slots", "Players aboard", "Price", "Unlock"], rows)
    names = ["Biscuit", "Kestrel", "Puffin", "Swordfish", "Albatross"]
    T["boat_upgrades"] = md_table(["Upgrade", "Effect", "Tag"] + names,
                                  [(u[0], u[2], u[3], *[money(u[1] * CLASS_FACTOR[n]) for n in names]) for u in BOAT_UPGRADES])
    T["cosmetics"] = md_table(["Cosmetic", "Price", "Tag"], [(c[0], money(c[1]), c[2]) for c in COSMETICS])
    T["dock"] = md_table(["Category", "Item", "Price", "Unlock", "Effect", "Tag"],
                         [(d[0], d[1], money(d[2]), d[3], d[4], d[5]) for d in DOCK])
    T["jobs"] = md_table(["Job", "Unlock", "Wage $/day", "What they do", "Tag"], WORKER_JOBS)
    wrows = []
    for s in range(1, 6):
        w, c, g, f, n = worker_day(s, 20, 18)
        w2, c2, g2, f2, n2 = worker_day(s, 30, 32)
        wrows.append((s, money(w), money(2 * w), 10 + 5 * s, f"x{1 + 0.15 * s:.2f}", f"{f:.0%}", money(n),
                      money(n * A["days_per_real_hour"][0]), money(n2)))
    T["workers"] = md_table(["Skill", "Wage/day", "Hire fee", "Catch/shift", "Grade mult", "Fail chance",
                             "Net/day: Biscuit + shrimp", "= net per real hour", "Net/day: Kestrel + squid"], wrows)
    T["vendors"] = md_table(["Vendor", "Boat", "Fee/day", "Needs", "Arrives", "Perk", "Tag"],
                            [(v[0], v[1], money(v[2]), v[3], v[4], v[5], v[6]) for v in VENDORS])
    T["ship_orders"] = md_table(["Ship", "Stage", "Cargo", "Berth fee", "Per unit", "Units (solo)", "Window (game h)",
                                 "Needs", "Full pay (solo)"],
                                [(o[0], o[1], o[2], money(o[3]), money(o[4]), o[5], o[6], o[7], money(o[3] + o[4] * o[5]))
                                 for o in SHIP_ORDERS])
    T["consign"] = md_table(["Boat", "Stage", "Capacity", "$/TEU", "Trip (game days)", "Captain/day", "Fuel/trip",
                             "Net/trip (NPC captain)", "Net/trip (player captain +30%)"],
                            [(c[0], c[1], f"{c[2]} TEU", money(c[3]), c[4], money(c[5]), money(c[6]),
                              money(c[2] * c[3] - c[5] * c[4] - c[6]), money(c[2] * c[3] * 1.3 - c[6])) for c in CONSIGNMENTS])
    srows = []
    b1, b2, b4 = stage_bounds(1), stage_bounds(2), stage_bounds(4)
    for i, st in enumerate(STAGES):
        srows.append((f"{st['key']} {st['name']}", st["act"], st["act2p"], st["gate"], money(stage_spend(st)), money(stage_rate(st, 1)),
                      f"{b1[i][0]:.1f}-{b1[i][1]:.1f} h", f"{(b1[i][1] - b1[i][0]) * A['days_per_real_hour'][0]:.0f}",
                      f"{b2[i][0]:.1f}-{b2[i][1]:.1f} h", f"{b4[i][0]:.1f}-{b4[i][1]:.1f} h",
                      money(earned_at(b1[i][1], b1))))
    T["stages"] = md_table(["Stage", "gdd/world act", "World's 2P hours", "Goal to leave it", "Key spend", "Solo net $/h", "Solo hours", "Solo in-game days",
                            "2 players", "4 players", "Total earned by stage end"], srows)
    irows = []
    for st in STAGES:
        tot = stage_rate(st, 1)
        irows.append((f"{st['key']} {st['name']}", *[f"{money(r)} ({r / tot:.0%})" if r else "-" for r in st["rate"]], money(tot)))
    T["income"] = md_table(["Stage"] + SOURCES + ["Total solo $/h"], irows)
    sp = []
    for st in STAGES:
        sp.append((f"{st['key']}", "; ".join(f"{n} {money(c)}" for n, c in st["spend"])))
    T["spend"] = md_table(["Stage", "Key purchases (the gate spend the hours are computed from)"], sp)
    T["endless"] = md_table(["Endless purchase", "Price"], [(n, money(c)) for n, c in ENDLESS])
    T["first_days"], _ = first_days_table()
    T["assumptions"] = md_table(["Assumption", "Value", "Source / why"], [(k, v[0], v[1]) for k, v in A.items()])
    return T


def fill_md(T):
    p = HERE / "economy.md"
    if not p.exists():
        return 0
    s = p.read_text(encoding="utf-8")
    n = 0
    for k, v in T.items():
        pat = re.compile(rf"<!--T:{k}-->.*?<!--/T-->", re.S)
        s, c = pat.subn(lambda m: f"<!--T:{k}-->\n{v}\n<!--/T-->", s)
        n += c
    p.write_text(s, encoding="utf-8")
    return n


# ---------------------------------------------------------------- xlsx
BLUE = Font(name="Arial", color="0000FF")
BOLD = Font(name="Arial", bold=True)
NORM = Font(name="Arial")
HEAD_FILL = PatternFill("solid", fgColor="DDE6F0")
KEY_FILL = PatternFill("solid", fgColor="FFFF00")
CUR = '$#,##0;($#,##0);-'


def header(ws, row, cols):
    for i, c in enumerate(cols, 1):
        cell = ws.cell(row=row, column=i, value=c)
        cell.font, cell.fill = BOLD, HEAD_FILL
        cell.alignment = Alignment(wrap_text=True, vertical="top")


def autosize(ws, width=18):
    for col in ws.columns:
        ws.column_dimensions[col[0].column_letter].width = width
    for row in ws.iter_rows():
        for c in row:
            if c.font != BOLD and c.font.color is None or (c.font.color is not None and c.font.color.rgb not in ("000000FF",)):
                if c.font != BOLD and c.font != BLUE:
                    c.font = NORM


def plain_sheet(wb, title, cols, rows, money_cols=()):
    ws = wb.create_sheet(title)
    header(ws, 1, cols)
    for r, row in enumerate(rows, 2):
        for c, v in enumerate(row, 1):
            cell = ws.cell(row=r, column=c, value=v)
            cell.font = BLUE if isinstance(v, (int, float)) else NORM
            if c in money_cols and isinstance(v, (int, float)):
                cell.number_format = CUR
    autosize(ws)
    ws.freeze_panes = "A2"
    return ws


def build_xlsx():
    wb = Workbook()
    ws = wb.active
    ws.title = "README"
    lines = [
        "Port Boss economy model (gdd/economy). Generated by economy_model.py; edit the script or the blue cells.",
        "Blue text = input you can change. Black = formula. Yellow = key tuning lever.",
        "Stages sheet: change a solo $/h input or a co-op multiplier and every hour/total recalculates.",
        "Curve sheet: cumulative money earned per real hour for 1, 2 and 4 players (piecewise from Stages).",
        "Workers sheet: change skill, hold cap or fish value and the daily net recalculates.",
        "Fish prices, grades, freshness, bait: gdd/fishing (w-gdd-fishing). Clock + places: gdd/world, gdd/systems.",
        "Everything else is SUGGESTED (ours) unless the Tag column says MUST (Manan) or OPEN QUESTION.",
    ]
    for i, l in enumerate(lines, 1):
        ws.cell(row=i, column=1, value=l).font = BOLD if i == 1 else NORM
    ws.column_dimensions["A"].width = 120

    # Assumptions
    wa = wb.create_sheet("Assumptions")
    header(wa, 1, ["Assumption", "Value", "Source / why"])
    for r, (k, (v, note)) in enumerate(A.items(), 2):
        wa.cell(row=r, column=1, value=k).font = NORM
        c = wa.cell(row=r, column=2, value=v)
        c.font = BLUE
        wa.cell(row=r, column=3, value=note).font = NORM
    wa.column_dimensions["A"].width = 28
    wa.column_dimensions["B"].width = 10
    wa.column_dimensions["C"].width = 90
    ref = lambda k: f"Assumptions!$B${list(A).index(k) + 2}"  # noqa: E731
    dph_ref = ref("days_per_real_hour")

    # Stages
    wsS = wb.create_sheet("Stages")
    wsS["A1"] = "Co-op multipliers per income source (players 1/2/3/4)"
    wsS["A1"].font = BOLD
    header(wsS, 2, ["Source", "1p", "2p", "3p", "4p"])
    for i, s in enumerate(SOURCES):
        wsS.cell(row=3 + i, column=1, value=s).font = NORM
        for j, m in enumerate(COOP[s]):
            c = wsS.cell(row=3 + i, column=2 + j, value=m)
            c.font = BLUE
    mrow0 = 3  # multipliers rows 3..9, cols B..E
    top = 12
    wsS.cell(row=top - 1, column=1, value="Solo net $ per real hour by source (blue inputs), gate spend, hours").font = BOLD
    cols = ["Stage", "Name"] + SOURCES + ["Solo $/h", "2p $/h", "4p $/h", "Gate spend $", "Solo hours", "2p hours",
                                          "4p hours", "Solo start h", "Solo end h", "2p start h", "2p end h",
                                          "4p start h", "4p end h", "Solo in-game days"]
    header(wsS, top, cols)
    L = lambda n: wsS.cell(row=1, column=n).column_letter  # noqa: E731
    src_first, src_last = 3, 2 + len(SOURCES)  # C..I
    for i, st in enumerate(STAGES):
        r = top + 1 + i
        wsS.cell(row=r, column=1, value=st["key"])
        wsS.cell(row=r, column=2, value=st["name"])
        for j, v in enumerate(st["rate"]):
            c = wsS.cell(row=r, column=src_first + j, value=v)
            c.font, c.number_format = BLUE, CUR
        rng = f"{L(src_first)}{r}:{L(src_last)}{r}"
        base = src_last + 1  # J
        wsS.cell(row=r, column=base, value=f"=SUM({rng})")
        wsS.cell(row=r, column=base + 1, value=f"=SUMPRODUCT({rng},TRANSPOSE($C${mrow0}:$C${mrow0 + 6}))")
        wsS.cell(row=r, column=base + 2, value=f"=SUMPRODUCT({rng},TRANSPOSE($E${mrow0}:$E${mrow0 + 6}))")
        c = wsS.cell(row=r, column=base + 3, value=stage_spend(st))
        c.font, c.fill = BLUE, KEY_FILL
        c.comment = Comment("; ".join(f"{n} {c_}" for n, c_ in st["spend"]), "economy")
        sp = f"{L(base + 3)}{r}"
        for k, rc in enumerate((base, base + 1, base + 2)):
            wsS.cell(row=r, column=base + 4 + k, value=f"={sp}/{L(rc)}{r}")
        for k in range(3):  # start/end per player count
            hrs = f"{L(base + 4 + k)}{r}"
            sc, ec = base + 7 + 2 * k, base + 8 + 2 * k
            wsS.cell(row=r, column=sc, value=0 if i == 0 else f"={L(ec)}{r - 1}")
            wsS.cell(row=r, column=ec, value=f"={L(sc)}{r}+{hrs}")
        wsS.cell(row=r, column=base + 13, value=f"={L(base + 4)}{r}*{dph_ref}")
        for cc in range(base, base + 4):
            wsS.cell(row=r, column=cc).number_format = CUR
        for cc in range(base + 4, base + 14):
            wsS.cell(row=r, column=cc).number_format = "0.0"
    # SUMPRODUCT with TRANSPOSE needs array eval; use explicit products instead (LibreOffice-safe)
    for i in range(len(STAGES)):
        r = top + 1 + i
        for k, mcol in ((1, "C"), (2, "E")):
            terms = "+".join(f"{L(src_first + j)}{r}*${mcol}${mrow0 + j}" for j in range(len(SOURCES)))
            wsS.cell(row=r, column=src_last + 1 + k, value=f"={terms}")
    tr = top + 1 + len(STAGES)
    wsS.cell(row=tr, column=1, value="TOTAL").font = BOLD
    for k in range(3):
        col = L(src_last + 5 + k)
        wsS.cell(row=tr, column=src_last + 5 + k, value=f"=SUM({col}{top + 1}:{col}{tr - 1})").number_format = "0.0"
    wsS.cell(row=tr, column=src_last + 4, value=f"=SUM({L(src_last + 4)}{top + 1}:{L(src_last + 4)}{tr - 1})").number_format = CUR
    for col in wsS.columns:
        wsS.column_dimensions[col[0].column_letter].width = 13
    wsS.column_dimensions["A"].width = 28

    # Curve
    wc = wb.create_sheet("Curve")
    header(wc, 1, ["Real hours", "Solo: earned $", "2 players: earned $", "4 players: earned $"])
    rs, re_ = top + 1, top + len(STAGES)
    base = src_last + 1
    pairs = [(base, base + 7, base + 8), (base + 1, base + 9, base + 10), (base + 2, base + 11, base + 12)]
    for i in range(0, 81):
        r = 2 + i
        wc.cell(row=r, column=1, value=i * 0.25)
        h = f"$A{r}"
        for k, (rate_c, s_c, e_c) in enumerate(pairs):
            R = f"Stages!${L(rate_c)}${rs}:${L(rate_c)}${re_}"
            S = f"Stages!${L(s_c)}${rs}:${L(s_c)}${re_}"
            E = f"Stages!${L(e_c)}${rs}:${L(e_c)}${re_}"
            f = f"=SUMPRODUCT({R},({h}>{S})*(({h}>={E})*({E}-{S})+({h}<{E})*({h}-{S})))"
            wc.cell(row=r, column=2 + k, value=f).number_format = CUR
    for col in "ABCD":
        wc.column_dimensions[col].width = 20

    # Workers (formula-driven)
    ww = wb.create_sheet("Workers")
    header(ww, 1, ["Skill (1-5)", "Hold cap (fish)", "Pool avg base $", "Wage/day", "Hire fee", "Catch/shift",
                   "Grade mult", "Fail chance", "Gross $/day", "Bait $/day", "Fuel $/day", "Net $/day", "Net per real hour"])
    examples = [(s, 20, 18) for s in range(1, 6)] + [(s, 30, 32) for s in range(1, 6)] + [(3, 100, 32), (5, 100, 70)]
    for i, (s, cap, base_v) in enumerate(examples):
        r = 2 + i
        for c, v in zip((1, 2, 3), (s, cap, base_v)):
            ww.cell(row=r, column=c, value=v).font = BLUE
        ww.cell(row=r, column=4, value=f"=70+45*A{r}")
        ww.cell(row=r, column=5, value=f"=2*D{r}")
        ww.cell(row=r, column=6, value=f"=MIN(10+5*A{r},B{r})")
        ww.cell(row=r, column=7, value=f"=1+0.15*A{r}")
        ww.cell(row=r, column=8, value=f"=(14-2*A{r})/100")
        ww.cell(row=r, column=9, value=f"=F{r}*C{r}*G{r}*{ref('blended_sell_factor')}")
        ww.cell(row=r, column=10, value=f"=F{r}*1.2")
        ww.cell(row=r, column=11, value=f"=12*{ref('fuel_price')}")
        ww.cell(row=r, column=12, value=f"=I{r}*(1-H{r}*0.5)-D{r}-J{r}-K{r}")
        ww.cell(row=r, column=13, value=f"=L{r}*{dph_ref}")
        for c in (4, 5, 9, 10, 11, 12, 13):
            ww.cell(row=r, column=c).number_format = CUR
        ww.cell(row=r, column=8).number_format = "0%"
    ww.cell(row=2 + len(examples) + 1, column=1,
            value="Rows 1-5: Biscuit + shrimp (pool ~$18). 6-10: Kestrel + squid (~$32). Last two: Puffin hold 100 "
                  "(rod only; the trawl net drum adds ~40 fish/shift on top). Pool averages are provisional until gdd/fishing's CSV.").font = NORM
    for col in ww.columns:
        ww.column_dimensions[col[0].column_letter].width = 14

    # catalogue sheets
    plain_sheet(wb, "Bait & Tackle", ["Item", "Price $", "Casts", "Unlock", "Tag", "Note"],
                [list(b) for b in BAIT] + [[t[0], t[1], "", t[2], t[3], t[4]] for t in TACKLE], (2,))
    plain_sheet(wb, "Clothes", ["Slot", "Price $", "Examples", "Tag"], CLOTHES)
    plain_sheet(wb, "Surf shop", ["Item", "Price $", "Unlock", "Tag", "Effect"], SURF, (2,))
    plain_sheet(wb, "Boats", ["Id", "Name", "Tier", "Role", "Length", "Walkable deck", "Speed m/s", "Tank L", "Burn L/min",
                              "Hold", "Worker slots", "Players aboard", "Price $", "Unlock"], BOATS, (13,))
    wu = plain_sheet(wb, "Boat upgrades", ["Upgrade", "Base $ (x class factor)", "Effect", "Tag"] + list(CLASS_FACTOR),
                     [list(u) for u in BOAT_UPGRADES], (2,))
    for j, n in enumerate(CLASS_FACTOR):
        wu.cell(row=len(BOAT_UPGRADES) + 3, column=5 + j, value=CLASS_FACTOR[n]).font = BLUE
        for i in range(len(BOAT_UPGRADES)):
            col = wu.cell(row=1, column=5 + j).column_letter
            wu.cell(row=2 + i, column=5 + j, value=f"=$B{2 + i}*{col}${len(BOAT_UPGRADES) + 3}").number_format = CUR
    wu.cell(row=len(BOAT_UPGRADES) + 3, column=4, value="class factor →").font = BOLD
    plain_sheet(wb, "Boat cosmetics", ["Cosmetic", "Price $", "Tag"], COSMETICS, (2,))
    plain_sheet(wb, "Dock shop", ["Category", "Item", "Price $", "Unlock", "Effect", "Tag"], DOCK, (3,))
    plain_sheet(wb, "Staff jobs", ["Job", "Unlock", "Wage $/day", "What they do", "Tag"], WORKER_JOBS)
    plain_sheet(wb, "Vendors", ["Vendor", "Boat", "Fee $/day", "Needs", "Arrives", "Perk", "Tag"], VENDORS, (3,))
    wo = plain_sheet(wb, "Ship orders", ["Ship", "Stage", "Cargo", "Berth fee $", "Per unit $", "Units (solo)",
                                         "Window game h", "Needs", "Full pay $"], [list(o) for o in SHIP_ORDERS], (4, 5))
    for i in range(len(SHIP_ORDERS)):
        wo.cell(row=2 + i, column=9, value=f"=D{2 + i}+E{2 + i}*F{2 + i}").number_format = CUR
    plain_sheet(wb, "Endless", ["Purchase", "Price $"], ENDLESS, (2,))
    wk = plain_sheet(wb, "Consignments", ["Boat", "Stage", "TEU", "$/TEU", "Trip game days", "Captain $/day",
                                          "Fuel $/trip", "Net/trip NPC captain", "Net/trip player captain"],
                     [list(c) for c in CONSIGNMENTS], (4, 6, 7))
    for i in range(len(CONSIGNMENTS)):
        r = 2 + i
        wk.cell(row=r, column=8, value=f"=C{r}*D{r}-F{r}*E{r}-G{r}").number_format = CUR
        wk.cell(row=r, column=9, value=f"=C{r}*D{r}*1.3-G{r}").number_format = CUR
    out = HERE / "economy.xlsx"
    wb.save(out)
    return out


# ---------------------------------------------------------------- charts
INK, INK2, GRID, SURF_BG = "#0b0b0b", "#52514e", "#e4e3df", "#fcfcfb"
SERIES = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#6250d6"]


def charts():
    plt.rcParams.update({"font.family": "Arial", "font.size": 10, "text.color": INK, "axes.labelcolor": INK2,
                         "xtick.color": INK2, "ytick.color": INK2, "axes.edgecolor": GRID})
    fig, ax = plt.subplots(figsize=(10, 5.6), dpi=150)
    fig.patch.set_facecolor(SURF_BG)
    ax.set_facecolor(SURF_BG)
    hours = [i / 20 for i in range(0, 421)]
    b1 = stage_bounds(1)
    for i, (s, e, _) in enumerate(b1):
        if i % 2 == 0:
            ax.axvspan(s, e, color="#f1f0ec", lw=0)
        ax.text((s + e) / 2, 130, f"{STAGES[i]['key']}\n{STAGES[i]['name']}", ha="center", va="bottom", fontsize=8.5, color=INK2)
    for k, (p, lab) in enumerate(((1, "Solo"), (2, "2 players"), (4, "4 players"))):
        b = stage_bounds(p)
        end = b[-1][1]
        hs = [h for h in hours if h <= end] + [end]
        ax.plot(hs, [max(earned_at(h, b), 1) for h in hs], color=SERIES[k], lw=2, label=lab)
        ax.plot([end], [earned_at(end, b)], "o", ms=8, color=SERIES[k], mec=SURF_BG, mew=2)
    ends = [stage_bounds(p)[-1][1] for p in (1, 2, 4)]
    ax.text(0.4, 1.2e6, f"Charter vote reached at: solo {ends[0]:.1f} h  ·  2 players {ends[1]:.1f} h  ·  4 players {ends[2]:.1f} h",
            fontsize=9, color=INK)
    ax.set_yscale("log")
    ax.set_ylim(100, 2e6)
    ax.set_xlim(0, 21)
    ax.yaxis.set_major_formatter(matplotlib.ticker.FuncFormatter(lambda v, _: f"${v:,.0f}"))
    ax.grid(axis="y", color=GRID, lw=0.8)
    for sp in ("top", "right"):
        ax.spines[sp].set_visible(False)
    ax.set_xlabel("Real hours played (stage bands = solo)")
    ax.set_ylabel("Total money earned (log scale)")
    ax.set_title("Port Boss: money earned over time, to the Charter vote (target curve)", loc="left", fontsize=12, fontweight="bold")
    ax.legend(frameon=False, loc="center right")
    fig.tight_layout()
    fig.savefig(HERE / "money-over-time.png", facecolor=SURF_BG)
    plt.close(fig)

    fig, ax = plt.subplots(figsize=(10, 3.8), dpi=150)
    fig.patch.set_facecolor(SURF_BG)
    ax.set_facecolor(SURF_BG)
    labels = [f"{st['key']} {st['name']}" for st in STAGES][::-1]
    lefts = [0.0] * len(STAGES)
    for j, src in enumerate(SOURCES):
        vals = [st["rate"][j] / stage_rate(st, 1) for st in STAGES][::-1]
        ax.barh(labels, vals, left=lefts, color=SERIES[j], edgecolor=SURF_BG, linewidth=2, label=src, height=0.6)
        lefts = [a + b for a, b in zip(lefts, vals)]
    ax.set_xlim(0, 1)
    ax.xaxis.set_major_formatter(matplotlib.ticker.PercentFormatter(1.0))
    for sp in ("top", "right", "left"):
        ax.spines[sp].set_visible(False)
    ax.set_title("Where solo income comes from, by stage", loc="left", fontsize=12, fontweight="bold")
    ax.legend(frameon=False, ncol=4, fontsize=8, loc="upper center", bbox_to_anchor=(0.5, -0.15))
    fig.tight_layout()
    fig.savefig(HERE / "income-mix.png", facecolor=SURF_BG)
    plt.close(fig)


# ---------------------------------------------------------------- self-check
def check(T, filled):
    totals = {}
    for p in (1, 2, 4):
        b = stage_bounds(p)
        totals[p] = b[-1][1]
        lo, hi = TOTAL_TARGET[p]
        assert lo <= totals[p] <= hi, (p, totals[p])
    assert totals[4] < totals[2] < totals[1]
    for st, (s, e, _) in zip(STAGES, stage_bounds(1)):
        lo, hi = st["target"]
        assert lo <= e - s <= hi, (st["key"], round(e - s, 2))
    # each stage earns more per hour than the one before, and rates grow by < 6x (no cliff)
    rates = [stage_rate(st, 1) for st in STAGES]
    assert all(1.5 < b / a < 6 for a, b in zip(rates, rates[1:])), rates
    # first-30-minute promise: S1 earns the first upgrade (engine rebuild $90, Manan 31 Oct) within ~15 min of fishing
    assert 250 / STAGES[0]["rate"][0] * 60 <= 15
    # Manan 31 Oct: Mari arrives by Day 3 07:00 at the latest; the first upgrade ($90 engine) lands by real minute 10
    for k, v in FIRST_FISH.items():
        rows, mari = first_days(v)
        assert mari <= (MARI_DAY, "07:00"), (k, mari)
        eng = next(r for r in rows if "engine" in r[3])
        assert eng[0] <= 10, (k, "first upgrade later than minute 10", eng[0])
    # the first worker pays: skill-1 deckhand on Biscuit + shrimp nets positive
    assert worker_day(1, 20, 18)[4] > 0
    # worker stays below the player's own hand rate in S2 (automation helps, never replaces play)
    assert worker_day(2, 20, 18)[4] * A["days_per_real_hour"][0] < STAGES[1]["rate"][0]
    if (HERE / "economy.md").exists():
        md = (HERE / "economy.md").read_text(encoding="utf-8")
        missing = [k for k in T if f"<!--T:{k}-->" not in md]
        assert not missing, missing
        assert filled == len(T)
    print("checks OK:", {p: round(t, 1) for p, t in totals.items()}, "hours; stage solo $/h", rates)


if __name__ == "__main__":
    T = tables()
    filled = fill_md(T)
    print("xlsx:", build_xlsx())
    charts()
    check(T, filled)
