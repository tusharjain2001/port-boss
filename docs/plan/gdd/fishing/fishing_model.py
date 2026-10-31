"""Port Boss fishing numbers check. MUST (Manan 31 Oct): 15 fish, 3 per rod (5 rods), no zones; legend needs Old Faithful
AND Walt's Mix ($40, sold once you own Old Faithful); Old Brass can bite again rarely after the story catch; time/weather
are a x1.5 nudge, never a lock. Run: python -I fishing_model.py
Reads fish-roster.csv next to this file; prints the timing windows per rod, grade mix, who bites per rod x bait,
$/fish and $/hour. Asserts the MUST rules. Planning model only, not game code."""
import csv, math, os, random

HERE = os.path.dirname(os.path.abspath(__file__))
# class: (green zone as fraction of bar, pointer speed in bar-widths/s), fishing.md §3.7 (Kufu review 31 Oct)
CLASS_BAR = {"Everyday": (0.19, 0.90), "Uncommon": (0.18, 1.00), "Scarce": (0.17, 1.10),
             "Trophy": (0.15, 1.25), "Legend": (0.13, 1.40)}
# rod: (name, price, green x, pointer speed x, bite min s, bite max s), fishing.md §6.2
RODS = [("Walt's Bamboo", 0, 1.00, 1.00, 4, 10), ("Fibreglass", 450, 1.15, 0.95, 4, 9),
        ("Carbon", 1600, 1.30, 0.90, 4, 8), ("Deep-sea", 6000, 1.40, 0.85, 4, 7),
        ("Old Faithful", 15000, 1.50, 0.80, 4, 6)]
STOP_SPEEDUP = [1.00, 1.08, 1.16]          # stop 1, 2, 3
GRADE_MULT = [1.0, 1.3, 1.7, 2.5]          # 0..3 hits = Common, Fine, Rare, Prized
BAITS = {"Worm": 0.5, "Shrimp": 1.2, "Sardine": 1.8, "Squid": 2.5, "WaltsMix": 40.0}  # $ per cast
FAV, LIKED, OTHER = 4.0, 2.0, 0.5   # bait weights (fish-roster.csv: first bait = favourite)
TIER_WEIGHT = {1: 1.0, 2: 0.7, 3: 0.45, 4: 0.3, 5: 0.2}  # newer rod tiers are rarer bites
NUDGE = 1.5                                 # at a fish's favourite time or weather; never a lock
LEGEND_CHANCE = 0.05                        # per cast, only with Old Faithful + Walt's Mix (x NUDGE at dusk/rain)
LEGEND_AGAIN = 0.01                         # MUST (Manan 31 Oct): after the first (story) catch it can bite again, rarely
FRESH = [(12, 1.00), (24, 0.85), (36, 0.60), (48, 0.35)]  # (until in-game hour, price factor); >=48 h rotten
TIMES, WEATHERS = ["Dawn", "Day", "Dusk", "Night"], ["Clear", "Fair", "Breezy", "Fog", "Rain", "Storm"]


def window_ms(cls, stop=0, rod=0):
    zone, speed = CLASS_BAR[cls]
    _, _, green, slow, _, _ = RODS[rod]
    return 1000 * zone * green / (speed * slow * STOP_SPEEDUP[stop])


def p_hit(win_ms, sigma_ms):
    return math.erf((win_ms / 2) / (sigma_ms * math.sqrt(2)))


def grade_mix(cls, sigma, rod=0):
    ps = [p_hit(window_ms(cls, s, rod), sigma) for s in range(3)]
    dist = [1.0]
    for p in ps:  # convolve: number of hits after each stop
        dist = [(dist[k] if k < len(dist) else 0) * (1 - p) + (dist[k - 1] * p if k else 0) for k in range(len(dist) + 1)]
    return dist


def bite_wait(rng, rod=0):
    lo, hi = RODS[rod][4:]
    return lo + (hi - lo) * rng.random()  # MUST: inside 4-10 s for every rod


def freshness(hours):
    for until, f in FRESH:
        if hours < until:
            return f
    return 0.0


def bite_odds(fish, rod, bait, time=None, weather=None, legend_caught=False):
    """Chance of each species on one bite. rod is 1..5. Legend handled as a separate per-cast check."""
    w = {}
    for f in fish:
        if f["class"] == "Legend" or int(f["rod_tier"]) > rod:
            continue  # the rod gate is hard
        baits = f["baits"].split("|")
        bf = FAV if baits[0] == bait else LIKED if bait in baits else OTHER
        n = (NUDGE if f["fav_time"] in ("Any", time) else 1) * (NUDGE if f["fav_weather"] in ("Any", weather) else 1)
        w[f["id"]] = bf * TIER_WEIGHT[int(f["rod_tier"])] * n
    legend = 0.0
    if rod == 5 and bait == "WaltsMix":  # MUST: best rod AND best bait, both
        legend = min(1.0, (LEGEND_AGAIN if legend_caught else LEGEND_CHANCE) * (NUDGE if time == "Dusk" else 1) * (NUDGE if weather == "Rain" else 1))
    tot = sum(w.values())
    out = {k: (1 - legend) * v / tot for k, v in w.items()}
    if legend:
        out["oldbrass"] = legend
    return out


if __name__ == "__main__":
    rng = random.Random(1)
    for r in range(5):
        waits = [bite_wait(rng, r) for _ in range(20000)]
        assert 4 <= min(waits) and max(waits) <= 10, f"{RODS[r][0]}: bite outside 4-10 s"
    assert freshness(47.9) > 0 and freshness(48) == 0, "rot must take >= 2 in-game days"

    fish = list(csv.DictReader(l for l in open(os.path.join(HERE, "fish-roster.csv"), encoding="utf-8") if not l.startswith("#")))
    by_id = {f["id"]: f for f in fish}
    assert len(fish) == 15, f"Manan: 15 fish, got {len(fish)}"
    for t in range(1, 6):
        assert sum(int(f["rod_tier"]) == t for f in fish) == 3, f"Manan: 3 fish per rod (tier {t})"
    legends = [f for f in fish if f["class"] == "Legend"]
    assert len(legends) == 1 and legends[0]["rod_tier"] == "5" and legends[0]["baits"] == "WaltsMix"
    for r in range(1, 6):
        for b in BAITS:
            if (r, b) != (5, "WaltsMix"):
                assert "oldbrass" not in bite_odds(fish, r, b, "Dusk", "Rain"), "legend must need best rod AND best bait"
    assert 0 < bite_odds(fish, 5, "WaltsMix", "Day", "Clear", True)["oldbrass"] < bite_odds(fish, 5, "WaltsMix", "Day", "Clear")["oldbrass"]
    # no zones, no locks: with the right rod, every fish can bite at every time and weather on any bait
    for f in fish:
        if f["class"] == "Legend":
            continue
        for t in TIMES:
            for w in WEATHERS:
                for b in BAITS:
                    assert bite_odds(fish, int(f["rod_tier"]), b, t, w).get(f["id"], 0) > 0, (f["id"], t, w, b)
    print("15 species, 3 per rod, 1 legend (Old Faithful + Walt's Mix only; 5%/cast first time, 1%/cast after); every fish bites anywhere, any time, any weather\n")

    print("Stop-1 timing window (ms) per rod:")
    print("  " + " " * 9 + "".join(f"{r[0]:>15}" for r in RODS))
    for c in CLASS_BAR:
        print(f"  {c:9}" + "".join(f"{window_ms(c, 0, r):15.0f}" for r in range(5)))

    print("\nChance of Prized (3 hits), practised player (sigma 70 ms), per rod:")
    for c in CLASS_BAR:
        print(f"  {c:9}" + "".join(f"{grade_mix(c, 70, r)[3]:15.0%}" for r in range(5)))

    for label, sigma in (("novice", 110), ("practised", 70)):
        print(f"\nGrade mix, {label} player (sigma {sigma} ms), starter rod:")
        for c in CLASS_BAR:
            d = grade_mix(c, sigma)
            ev = sum(m * x for m, x in zip(GRADE_MULT, d))
            print(f"  {c:9} " + "  ".join(f"{n}:{x:4.0%}" for n, x in zip("CFRP", d)) + f"   price x{ev:.2f}")

    print("\nWho bites, per rod x bait (average over times/weathers; top 3 species):")
    for r in range(1, 6):
        for b in BAITS:
            if b == "WaltsMix" and r < 5:
                continue
            avg = {}
            for t in TIMES:
                for w in WEATHERS:
                    for k, v in bite_odds(fish, r, b, t, w).items():
                        avg[k] = avg.get(k, 0) + v / (len(TIMES) * len(WEATHERS))
            top = sorted(avg.items(), key=lambda kv: -kv[1])[:3]
            print(f"  {RODS[r-1][0]:13} {b:8} " + ", ".join(f"{by_id[k]['name'].split(' (')[0]} {v:.0%}" for k, v in top))

    print("\n$/fish and $/real hour, practised player, Fresh, mid size, best bait per rod:")
    print(f"  {'rod':13} {'bait':8} {'$/fish':>8} {'net':>8} {'fish/h':>7} {'$/h':>8}")
    for r in range(1, 6):
        lo, hi = RODS[r - 1][4:]
        per_hour = 70 * 20 / (13 + (lo + hi) / 2)  # 70/h at the starter's 7 s mean wait; cycle = 13 s + wait
        best = None
        for b in BAITS:
            if b == "WaltsMix" and r < 5:
                continue
            odds = {}
            for t in TIMES:
                for w in WEATHERS:
                    for k, v in bite_odds(fish, r, b, t, w).items():
                        odds[k] = odds.get(k, 0) + v / (len(TIMES) * len(WEATHERS))
            ev = sum(p * float(by_id[k]["base_price"]) *
                     sum(m * x for m, x in zip(GRADE_MULT, grade_mix(by_id[k]["class"], 70, r - 1)))
                     for k, p in odds.items())
            net = ev - BAITS[b] * 1.15  # 15% extra casts lost to bad strikes
            print(f"  {RODS[r-1][0]:13} {b:8} {ev:8.1f} {net:8.1f} {per_hour:7.0f} {net*per_hour:8.0f}")
            best = max(best or (net, b), (net, b))
        print(f"  {'':13} best: {best[1]}")
    print("\nOK: all MUST asserts passed")
