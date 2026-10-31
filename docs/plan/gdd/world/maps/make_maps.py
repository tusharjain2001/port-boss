"""Generates the 3 Port Boss world-layout diagrams (SVG) + prints travel times.
World units are metres (5000 x 5000 m sea). Run: python make_maps.py  -> map-A/B/C.svg
Render PNG: chrome --headless --screenshot (see render.sh)."""
import math, os

HERE = os.path.dirname(os.path.abspath(__file__))
MAP_PX = 1200          # map square in px
S = MAP_PX / 5000.0    # px per metre
PANEL = 440            # right legend panel
W, H = MAP_PX + PANEL, MAP_PX
SPEEDS = [("T1 Biscuit (skiff)", 6), ("T2 launch", 9), ("T3 trawler", 11), ("T4 fast boat", 16)]

C = dict(deep="#0B3D5C", mid="#14597A", shelf="#1F7A8F", shallow="#3FD1C2", sand="#E9DCC8",
         land="#A9BF7E", land2="#8FA866", ridge="#6B5B45", ink="#1B2B34", paper="#F6EFE3",
         ours="#F2994A", voss="#C0392B", town="#D9C3A0", route="#FFFFFF", gold="#F2C38E")

def p(x, y):
    return f"{x*S:.1f},{y*S:.1f}"

def smooth(pts, closed=True):
    """Catmull-Rom through pts -> cubic bezier path (metres in, px out)."""
    n = len(pts)
    d = f"M{p(*pts[0])}"
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        p0 = pts[(i - 1) % n] if closed or i > 0 else pts[i]
        p1, p2 = pts[i], pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if closed or i + 2 < n else p2
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C{p(*c1)} {p(*c2)} {p(*p2)}"
    return d + (" Z" if closed else "")

def plen(pts):
    return sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1))

def mins(m, v):
    return m / v / 60

class Map:
    def __init__(self, key, title, subtitle):
        self.key, self.title, self.subtitle = key, title, subtitle
        self.el, self.routes, self.legend = [], [], []

    def add(self, s):
        self.el.append(s)

    def land(self, pts, cls="land"):
        d = smooth(pts)
        self.add(f'<path d="{d}" fill="none" stroke="{C["shallow"]}" stroke-opacity=".55" stroke-width="{90*S:.1f}" stroke-linejoin="round"/>')
        self.add(f'<path d="{d}" fill="none" stroke="{C["sand"]}" stroke-width="{26*S:.1f}" stroke-linejoin="round"/>')
        self.add(f'<path d="{d}" fill="{C["land"]}"/>')

    def islet(self, x, y, rx, ry, label, sub=None, dy=0):
        pts = [(x + rx * math.cos(a) * (1 + .12 * math.sin(3 * a)), y + ry * math.sin(a) * (1 + .1 * math.cos(2 * a)))
               for a in [i * math.pi / 6 for i in range(12)]]
        self.land(pts)
        self.label(x, y + ry + 120 + dy, label, 15, bold=True)
        if sub:
            self.label(x, y + ry + 210 + dy, sub, 12)

    def label(self, x, y, text, size=13, bold=False, color=None, anchor="middle", italic=False, halo=True):
        col = color or C["paper"]
        w = "700" if bold else "500"
        st = "italic" if italic else "normal"
        fam = "Fraunces, Georgia, serif" if bold else "Barlow, 'Segoe UI', sans-serif"
        h = f'stroke="{C["deep"]}" stroke-width="3.5" stroke-linejoin="round" paint-order="stroke"' if halo else ""
        self.add(f'<text x="{x*S:.1f}" y="{y*S:.1f}" font-family="{fam}" font-size="{size}" font-weight="{w}" '
                 f'font-style="{st}" fill="{col}" text-anchor="{anchor}" {h}>{text}</text>')

    def pin(self, x, y, n, color=None):
        col = color or C["gold"]
        self.add(f'<circle cx="{x*S:.1f}" cy="{y*S:.1f}" r="9" fill="{col}" stroke="{C["ink"]}" stroke-width="1.5"/>')
        self.add(f'<text x="{x*S:.1f}" y="{y*S+4:.1f}" font-family="Barlow, sans-serif" font-size="11" font-weight="700" fill="{C["ink"]}" text-anchor="middle">{n}</text>')

    def zone(self, x, y, r, n, label, color="#7FE3D6", dash="6 5"):
        self.add(f'<circle cx="{x*S:.1f}" cy="{y*S:.1f}" r="{r*S:.1f}" fill="{color}" fill-opacity=".14" stroke="{color}" stroke-width="1.6" stroke-dasharray="{dash}"/>')
        self.add(f'<text x="{x*S:.1f}" y="{y*S+5:.1f}" font-family="Barlow, sans-serif" font-size="14" font-weight="700" fill="{color}" text-anchor="middle" stroke="{C["deep"]}" stroke-width="3" paint-order="stroke">F{n}</text>')
        self.legend.append(("zone", f"F{n}", label, color))

    def surf(self, x, y, label):
        for k in range(3):
            yy = y + k * 45
            self.add(f'<path d="M{p(x-110, yy)} q{30*S:.1f},{-35*S:.1f} {60*S:.1f},0 t{60*S:.1f},0 t{60*S:.1f},0" fill="none" stroke="#FFFFFF" stroke-width="2" stroke-linecap="round"/>')
        self.legend.append(("surf", "~", label, "#FFFFFF"))

    def route(self, pts, name, color="#FFFFFF", show=True):
        L = plen(pts)
        self.routes.append((name, L))
        if show:
            d = "M" + " L".join(p(*q) for q in pts)
            self.add(f'<path d="{smooth(pts, closed=False)}" fill="none" stroke="{color}" stroke-width="2.2" stroke-dasharray="2 7" stroke-linecap="round" opacity=".9"/>')
        return L

    def svg(self):
        out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
               '<style>@import url("https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,600;9..144,700&amp;family=Barlow:wght@500;700&amp;display=block");</style>',
               '<defs><radialGradient id="sea" cx="48%" cy="50%" r="60%">'
               f'<stop offset="0" stop-color="{C["shelf"]}"/><stop offset=".55" stop-color="{C["mid"]}"/>'
               f'<stop offset="1" stop-color="{C["deep"]}"/></radialGradient>'
               '<pattern id="hatch" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
               f'<line x1="0" y1="0" x2="0" y2="8" stroke="{C["ours"]}" stroke-width="3"/></pattern>'
               '<pattern id="hatchv" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
               f'<line x1="0" y1="0" x2="0" y2="8" stroke="{C["voss"]}" stroke-width="3"/></pattern></defs>',
               f'<rect width="{MAP_PX}" height="{MAP_PX}" fill="url(#sea)"/>']
        # 500 m grid
        for i in range(1, 10):
            v = i * 500 * S
            out.append(f'<line x1="{v:.1f}" y1="0" x2="{v:.1f}" y2="{MAP_PX}" stroke="#FFFFFF" stroke-opacity=".06"/>')
            out.append(f'<line x1="0" y1="{v:.1f}" x2="{MAP_PX}" y2="{v:.1f}" stroke="#FFFFFF" stroke-opacity=".06"/>')
        out += self.el
        # scale bar + compass
        x0, y0 = 60, MAP_PX - 50
        out.append(f'<rect x="{x0}" y="{y0}" width="{1000*S:.1f}" height="8" fill="{C["paper"]}"/>'
                   f'<rect x="{x0}" y="{y0}" width="{500*S:.1f}" height="8" fill="{C["ink"]}"/>'
                   f'<text x="{x0}" y="{y0-8}" font-family="Barlow, sans-serif" font-size="13" fill="{C["paper"]}">0</text>'
                   f'<text x="{x0+1000*S:.1f}" y="{y0-8}" font-family="Barlow, sans-serif" font-size="13" fill="{C["paper"]}" text-anchor="middle">1 km</text>'
                   f'<text x="{x0}" y="{y0+28}" font-family="Barlow, sans-serif" font-size="12" fill="{C["paper"]}" opacity=".8">grid = 500 m · sea area 5 × 5 km</text>')
        cx, cy = MAP_PX - 60, 70
        out.append(f'<path d="M{cx},{cy-34} L{cx+11},{cy} L{cx},{cy-8} L{cx-11},{cy} Z" fill="{C["paper"]}"/>'
                   f'<text x="{cx}" y="{cy+22}" font-family="Fraunces, Georgia, serif" font-size="16" font-weight="700" fill="{C["paper"]}" text-anchor="middle">N</text>')
        # panel
        px = MAP_PX
        out.append(f'<rect x="{px}" y="0" width="{PANEL}" height="{H}" fill="{C["paper"]}"/>')
        y = 52
        out.append(f'<text x="{px+28}" y="{y}" font-family="Fraunces, Georgia, serif" font-size="28" font-weight="700" fill="{C["ink"]}">{self.title}</text>')
        y += 26
        for line in self.subtitle:
            out.append(f'<text x="{px+28}" y="{y}" font-family="Barlow, sans-serif" font-size="14" fill="{C["ink"]}" opacity=".8">{line}</text>')
            y += 19
        y += 14
        out.append(f'<text x="{px+28}" y="{y}" font-family="Fraunces, Georgia, serif" font-size="17" font-weight="700" fill="{C["ink"]}">Travel time from our dock (min)</text>')
        y += 24
        hdr = ["T1 6", "T2 9", "T3 11", "T4 16"]
        out.append(f'<text x="{px+28}" y="{y}" font-family="Barlow, sans-serif" font-size="12" font-weight="700" fill="{C["ink"]}">to ... (boat tier, m/s)</text>')
        for j, h in enumerate(hdr):
            out.append(f'<text x="{px+250+j*46}" y="{y}" font-family="Barlow, sans-serif" font-size="12" font-weight="700" fill="{C["ink"]}" text-anchor="middle">{h}</text>')
        y += 6
        for name, L in self.routes:
            y += 19
            out.append(f'<text x="{px+28}" y="{y}" font-family="Barlow, sans-serif" font-size="13" fill="{C["ink"]}">{name} <tspan opacity=".55">{L/1000:.1f} km</tspan></text>')
            for j, (_, v) in enumerate(SPEEDS):
                out.append(f'<text x="{px+250+j*46}" y="{y}" font-family="Barlow, sans-serif" font-size="13" fill="{C["ink"]}" text-anchor="middle">{mins(L, v):.1f}</text>')
        y += 30
        out.append(f'<text x="{px+28}" y="{y}" font-family="Fraunces, Georgia, serif" font-size="17" font-weight="700" fill="{C["ink"]}">Key</text>')
        for kind, mark, text, col in self.legend:
            y += 21
            if kind == "pin":
                out.append(f'<circle cx="{px+38}" cy="{y-4}" r="8" fill="{col}" stroke="{C["ink"]}" stroke-width="1.2"/>'
                           f'<text x="{px+38}" y="{y}" font-family="Barlow, sans-serif" font-size="10" font-weight="700" fill="{C["ink"]}" text-anchor="middle">{mark}</text>')
            elif kind == "zone":
                out.append(f'<circle cx="{px+38}" cy="{y-4}" r="8" fill="#14597A" stroke="{col}" stroke-width="1.5" stroke-dasharray="3 2"/>'
                           f'<text x="{px+38}" y="{y}" font-family="Barlow, sans-serif" font-size="9" font-weight="700" fill="#FFFFFF" text-anchor="middle">{mark}</text>')
            elif kind == "swatch":
                out.append(f'<rect x="{px+30}" y="{y-12}" width="16" height="12" fill="{col}" stroke="{C["ink"]}" stroke-width=".8"/>')
            else:
                out.append(f'<text x="{px+38}" y="{y}" font-family="Barlow, sans-serif" font-size="15" font-weight="700" fill="#1F7A8F" text-anchor="middle">{mark}</text>')
            out.append(f'<text x="{px+56}" y="{y}" font-family="Barlow, sans-serif" font-size="13" fill="{C["ink"]}">{text}</text>')
        out.append(f'<text x="{px+28}" y="{H-24}" font-family="Barlow, sans-serif" font-size="11" fill="{C["ink"]}" opacity=".6">Port Boss GDD · world · {self.key} · 2026-10-31 · diagram, not art</text>')
        out.append('</svg>')
        return "\n".join(out)

    def save(self):
        path = os.path.join(HERE, f"map-{self.key}.svg")
        open(path, "w", encoding="utf-8").write(self.svg())
        print(f"\n== {self.key} {self.title} -> {path}")
        for name, L in self.routes:
            print(f"  {name:34s} {L:6.0f} m  " + "  ".join(f"{mins(L, v):4.1f}" for _, v in SPEEDS))


def shops_legend(m):
    for n, t in [("1", "Hook, Line &amp; Sinker (bait/tackle)"), ("2", "Doyle Boatworks (boats, paint)"),
                 ("3", "Plank &amp; Post (dock upgrades)"), ("4", "The Mooring Line pub (hiring)"),
                 ("5", "Fish Market (Voss buyer)"), ("6", "Swell Surf Co."), ("7", "Post office + Nell's Curios")]:
        m.legend.append(("pin", n, t, C["gold"]))


# ---------------------------------------------------------------- MAP A (recommended)
def map_a():
    # Kufu review 31 Oct: Voss Harbour pushed to the far east tip of a long east arm (~2 km from our dock
    # over the ridge, 3+ km by sea round the north) so it reads as a separate bustling world; F7 moved off land
    # into the north bay; overlapping labels moved.
    m = Map("A", "A · Twin Coasts", ["Our dock + town on the west coast; Voss Harbour",
                                     "on the far east tip, behind Saltback Ridge.",
                                     "Walt's Cut (silted) opens to the north bay.", "RECOMMENDED · Kufu review 31 Oct"])
    island = [(2290, 2560), (2160, 2380), (2080, 2100), (2180, 1920), (2380, 1820), (2520, 1790), (2700, 1840),
              (2860, 1960), (2900, 2120), (2990, 2270), (3150, 2200), (3400, 2120), (3650, 2110), (3880, 2170),
              (4060, 2300), (4140, 2480), (4080, 2680), (3900, 2800), (3650, 2850), (3400, 2900), (3150, 2980),
              (3000, 3080), (2980, 3250), (2900, 3380), (2700, 3520), (2500, 3600), (2300, 3540), (2080, 3360),
              (1990, 3100), (2020, 2850), (2150, 2720)]
    m.add(f'<circle cx="{2700*S}" cy="{2650*S}" r="{1850*S}" fill="none" stroke="#7FE3D6" stroke-opacity=".25" stroke-width="2" stroke-dasharray="1 9"/>')
    m.land(island)
    # ridge across the root of the east arm: it hides Voss from our side
    m.add(f'<path d="{smooth([(3120,2230),(3170,2450),(3150,2650),(3200,2930)], closed=False)}" fill="none" stroke="{C["ridge"]}" stroke-width="3" stroke-dasharray="10 6" opacity=".7"/>')
    m.add(f'<path d="M{p(3165,2420)} l-9,16 l18,0 z" fill="{C["ridge"]}"/>')
    m.label(3230, 2520, "Saltback Ridge 140 m", 11, color=C["ink"], halo=False, italic=True, anchor="start")
    m.label(3230, 2580, "hides Voss from our side", 10, color=C["ink"], halo=False, italic=True, anchor="start")
    # Walt's Cut: west waist -> north bay, under the Voss causeway
    m.add(f'<path d="M{p(2290,2560)} L{p(2960,2260)}" stroke="#7FB8C9" stroke-width="7" stroke-dasharray="14 6" opacity=".9"/>')
    m.add(f'<rect x="{2700*S:.1f}" y="{2340*S:.1f}" width="{60*S:.1f}" height="{70*S:.1f}" fill="{C["voss"]}"/>')
    m.label(2640, 2230, "Walt's Cut · silted", 11, color=C["ink"], halo=False, italic=True)
    m.label(2800, 2480, "Voss causeway", 10, color=C["voss"], halo=False)
    # town + shops
    m.add(f'<path d="{smooth([(2240,2420),(2350,2400),(2440,2460),(2450,2560),(2400,2680),(2330,2700),(2300,2600)])}" fill="{C["town"]}" opacity=".95"/>')
    m.label(2380, 2350, "OLD SALTMOUTH", 12, bold=True, color=C["ink"], halo=False)
    for n, (x, y) in {"1": (2270, 2470), "2": (2330, 2660), "3": (2400, 2610), "4": (2340, 2490), "5": (2410, 2540),
                      "6": (2340, 2575), "7": (2390, 2440)}.items():
        m.pin(x, y, n)
    shops_legend(m)
    # our dock + expansion
    m.add(f'<rect x="{2130*S:.1f}" y="{2550*S:.1f}" width="{160*S:.1f}" height="{22*S:.1f}" fill="{C["ours"]}"/>')
    m.label(1960, 2590, "HALE'S LANDING", 14, bold=True, color=C["ours"], anchor="end")
    m.label(1960, 2670, "our dock (start)", 12, anchor="end")
    for i, (y0, h) in enumerate([(2420, 100), (2260, 140), (2080, 160)]):
        m.add(f'<rect x="{1880*S:.1f}" y="{y0*S:.1f}" width="{240*S:.1f}" height="{h*S:.1f}" fill="url(#hatch)" opacity=".75" stroke="{C["ours"]}" stroke-dasharray="4 3"/>')
        m.label(1850, y0 + h / 2 + 40, f"P{i+1}", 12, bold=True, color=C["ours"], anchor="end")
    m.label(2420, 2060, "Cannery Flats", 11, bold=True, color="#B5651D", halo=False)
    m.label(2420, 2120, "(our expansion)", 10, color="#B5651D", halo=False)
    m.legend.append(("swatch", "", "Our port expansion P1 → P3 (piers, berth, cranes)", C["ours"]))
    # Voss Harbour: the far east tip, reclaimed land, cranes, breakwater
    m.add(f'<path d="{smooth([(3800,2230),(4150,2230),(4330,2400),(4320,2700),(4120,2830),(3850,2780),(3760,2500)])}" fill="#9AA3A8"/>')
    for k in range(6):
        m.add(f'<rect x="{(3930+k*55)*S:.1f}" y="{(2380+(k%2)*40)*S:.1f}" width="6" height="24" fill="{C["voss"]}"/>')
    m.add(f'<rect x="{3880*S:.1f}" y="{2560*S:.1f}" width="10" height="34" fill="#5E676C"/>')
    m.add(f'<path d="{smooth([(4430,2150),(4560,2450),(4500,2850)], closed=False)}" fill="none" stroke="#C7CCD0" stroke-width="5"/>')
    m.add(f'<path d="{smooth([(4400,2050),(4680,2450),(4600,2980)], closed=False)}" fill="none" stroke="{C["voss"]}" stroke-width="2" stroke-dasharray="3 6"/>')
    m.label(4080, 3000, "VOSS HARBOUR", 15, bold=True, color="#FFB4A8")
    m.label(4080, 3080, "cranes · freighters · the VOSS tower", 11)
    m.label(4080, 3150, "no-dock buoy line · patrol", 11)
    m.legend.append(("swatch", "", "Voss Harbour (visit by boat, cannot dock)", "#9AA3A8"))
    # shipping lane + ferry
    m.add(f'<path d="M{p(4800,0)} C{p(4650,1500)} {p(4750,3500)} {p(4900,5000)}" fill="none" stroke="#FFFFFF" stroke-opacity=".12" stroke-width="14"/>')
    m.label(4700, 600, "Voss Lanes (freighters)", 12, anchor="end")
    m.add(f'<path d="M{p(4330,2400)} L{p(4980,1700)}" stroke="#FFFFFF" stroke-width="1.5" stroke-dasharray="8 6" opacity=".5"/>')
    m.label(4950, 1620, "ferry → Port Ardent", 11, anchor="end")
    # lighthouse
    m.pin(2520, 1760, "L", "#FFFFFF")
    m.label(2470, 1730, "Dot's lighthouse", 12, anchor="end")
    m.legend.append(("pin", "L", "Lighthouse (harbour master Dot)", "#FFFFFF"))
    # islets
    m.islet(1300, 1880, 110, 80, "Gullrock")
    m.islet(900, 3380, 210, 120, "Lantern Key", "The Pelican (premium buyer)")
    m.islet(4200, 4150, 150, 110, "Mango Cay", "late game · cartel")
    m.islet(4000, 900, 120, 90, "Sorrel Isle", "cannery buyer (Act 3)")
    # fishing zones
    m.add(f'<path d="{smooth([(2560,2850),(2400,2900),(2250,2870),(2100,2960),(2010,2990)], closed=False)}" fill="none" stroke="#7FB8C9" stroke-width="4" stroke-linecap="round"/>')
    m.label(2250, 2960, "Salt River", 10, color=C["ink"], halo=False, italic=True)
    m.zone(1880, 3020, 150, 8, "Saltmouth Estuary (brackish, muddy)", color="#E8C77A")
    m.zone(1760, 2760, 200, 1, "The Shallows (start, 0–8 m)")
    m.zone(1650, 3500, 260, 2, "Kelp Shelf (5–15 m)")
    m.zone(1300, 1880, 330, 3, "Gullrock Reef")
    m.zone(1150, 1450, 170, 5, "Wreck of the Margaret (Walt's boat)", color=C["gold"])
    m.zone(4300, 3550, 300, 6, "Voss Lanes (wakes, patrols)", color="#FFB4A8")
    m.zone(3150, 1830, 170, 7, "Lantern Glow, the north bay (night only)", color="#C9A7FF")
    m.label(500, 4300, "THE DROP-OFF · 30 m+ · F4", 15, bold=True, color="#7FE3D6", anchor="start")
    m.label(500, 4380, "deep blue beyond the ring: big fish, rough seas", 12, anchor="start")
    m.legend.append(("zone", "F4", "The Drop-off (beyond dotted ring)", "#7FE3D6"))
    # surf
    m.surf(2500, 3720, "Breaker Point (beginner surf)")
    m.label(2500, 3920, "Breaker Point", 13, bold=True)
    m.surf(980, 2250, "Gullrock 'Widowmaker' reef break (advanced)")
    m.surf(4560, 2000, "Voss Wall (breakwater wave, chased off)")
    D = (2130, 2560)
    m.route([D, (1800, 2720)], "Shallows (F1)")
    m.route([D, (1880, 3020)], "Saltmouth Estuary (F8)")
    m.route([D, (1420, 1960)], "Gullrock (F3)")
    m.route([D, (1960, 2900), (2000, 3300), (2250, 3650), (2450, 3700)], "Breaker Point surf")
    m.route([D, (1800, 2850), (1120, 3330)], "Lantern Key (buyer)")
    m.route([D, (1150, 1450)], "Wreck of the Margaret", show=False)
    m.route([D, (1980, 2250), (1980, 1950), (2200, 1680), (2550, 1640), (2950, 1700), (3350, 1950), (3750, 1980), (4250, 2080), (4450, 2350)],
            "Voss Harbour (round north)")
    m.route([D, (2290, 2560), (2960, 2260), (3350, 1990), (3750, 1980), (4250, 2080), (4450, 2350)], "Voss Harbour via Walt's Cut", show=False)
    m.route([D, (1800, 2800), (1950, 3800), (3000, 3900), (4050, 4100)], "Mango Cay", show=False)
    m.route([D, (1980, 2250), (1980, 1950), (2200, 1680), (3000, 1300), (3880, 980)], "Sorrel Isle", show=False)
    m.save()


# ---------------------------------------------------------------- MAP B (CHOSEN, Manan 31 Oct)
def map_b():
    # Manan 31 Oct: "option B was okay" -> map B, the original Crescent Bay, is the chosen map.
    # Kept from the 31 Oct pass: no fishing zones, dock + expansion, lighthouse, wreck, islets, surf.
    # Walt's Cut: a silted channel through the narrow south horn, from the bay to the outer south coast (toward Voss).
    m = Map("B", "B · Crescent Bay (CHOSEN)", ["Crescent island hugging a calm bay that opens",
                                               "west. Our dock deep inside the bay; Voss on the",
                                               "outer east coast. Walt's Cut (silted) crosses the",
                                               "south horn. Any fish, anywhere: no zones."])
    island = [(1900, 1700), (2300, 1450), (2900, 1400), (3400, 1650), (3700, 2100), (3780, 2600), (3650, 3150),
              (3300, 3550), (2800, 3700), (2250, 3600), (1900, 3350), (2150, 3200), (2550, 3250), (2850, 3050),
              (3000, 2650), (2950, 2200), (2700, 1900), (2300, 1880), (2050, 1950)]
    m.land(island)
    # Saltback Ridge along the outer arc: hides Voss from the bay
    m.add(f'<path d="{smooth([(2950,1550),(3400,2100),(3500,2700),(3200,3350)], closed=False)}" fill="none" stroke="{C["ridge"]}" stroke-width="3" stroke-dasharray="10 6" opacity=".7"/>')
    m.label(3430, 1950, "Saltback Ridge 140 m", 11, color=C["ink"], halo=False, italic=True, anchor="end")
    m.label(3430, 2010, "hides Voss from the bay", 10, color=C["ink"], halo=False, italic=True, anchor="end")
    # Walt's Cut: through the narrow south horn, bay -> outer south coast
    m.add(f'<path d="M{p(2580,3230)} L{p(2700,3680)}" stroke="#7FB8C9" stroke-width="7" stroke-dasharray="14 6" opacity=".9"/>')
    m.label(2520, 3470, "Walt's Cut · silted", 11, color=C["ink"], halo=False, italic=True, anchor="end")
    # town + shops on the bay shore
    m.add(f'<path d="{smooth([(2880,2450),(2990,2400),(3030,2560),(2960,2700),(2870,2640)])}" fill="{C["town"]}"/>')
    m.label(3250, 2230, "OLD SALTMOUTH", 12, bold=True, color=C["ink"], halo=False)
    for n, (x, y) in {"1": (2900, 2480), "2": (2880, 2620), "3": (2960, 2650), "4": (2950, 2510), "5": (3000, 2570),
                      "6": (2600, 3260), "7": (2990, 2440)}.items():
        m.pin(x, y, n)
    shops_legend(m)
    # our dock, deep inside the bay, + expansion along the bay shore
    m.add(f'<rect x="{2760*S:.1f}" y="{2540*S:.1f}" width="{130*S:.1f}" height="{22*S:.1f}" fill="{C["ours"]}"/>')
    m.label(3060, 2800, "↖ HALE'S LANDING", 14, bold=True, color=C["ours"], anchor="start")
    m.label(3110, 2870, "our dock (start), inside the bay", 11, anchor="start")
    for k, (x0, y0, w, h) in enumerate([(2700, 2300, 180, 110), (2650, 2080, 200, 140), (2600, 1980, 120, 0)][:2]):
        m.add(f'<rect x="{x0*S:.1f}" y="{y0*S:.1f}" width="{w*S:.1f}" height="{h*S:.1f}" fill="url(#hatch)" opacity=".75" stroke="{C["ours"]}" stroke-dasharray="4 3"/>')
        m.label(x0 - 30, y0 + h / 2 + 40, f"P{k+1}", 12, bold=True, color=C["ours"], anchor="end")
    m.add(f'<rect x="{2620*S:.1f}" y="{2700*S:.1f}" width="{180*S:.1f}" height="{180*S:.1f}" fill="url(#hatch)" opacity=".75" stroke="{C["ours"]}" stroke-dasharray="4 3"/>')
    m.label(2590, 2830, "P3", 12, bold=True, color=C["ours"], anchor="end")
    m.legend.append(("swatch", "", "Our port expansion P1 → P3 (bay shore)", C["ours"]))
    # Voss Harbour: outer east coast
    m.add(f'<path d="{smooth([(3700,2350),(3980,2300),(4080,2600),(3980,2900),(3720,2850)])}" fill="#9AA3A8"/>')
    for k in range(6):
        m.add(f'<rect x="{(3800+k*45)*S:.1f}" y="{(2450+(k%2)*40)*S:.1f}" width="6" height="24" fill="{C["voss"]}"/>')
    m.add(f'<path d="{smooth([(4250,2150),(4380,2600),(4250,3050)], closed=False)}" fill="none" stroke="#C7CCD0" stroke-width="5"/>')
    m.add(f'<path d="{smooth([(4300,2050),(4520,2600),(4320,3200)], closed=False)}" fill="none" stroke="{C["voss"]}" stroke-width="2" stroke-dasharray="3 6"/>')
    m.label(4150, 3300, "VOSS HARBOUR", 15, bold=True, color="#FFB4A8")
    m.label(4150, 3380, "cranes · freighters · the VOSS tower", 11)
    m.label(4150, 3450, "no-dock buoy line · patrol", 11)
    m.legend.append(("swatch", "", "Voss Harbour (outer coast; visit by boat, cannot dock)", "#9AA3A8"))
    m.add(f'<path d="M{p(4800,0)} C{p(4650,1500)} {p(4750,3500)} {p(4900,5000)}" fill="none" stroke="#FFFFFF" stroke-opacity=".12" stroke-width="14"/>')
    m.label(4700, 420, "Voss Lanes (freighters)", 12, anchor="end")
    m.add(f'<path d="M{p(4080,2600)} L{p(4980,2000)}" stroke="#FFFFFF" stroke-width="1.5" stroke-dasharray="8 6" opacity=".5"/>')
    m.label(4950, 1930, "ferry → Port Ardent", 11, anchor="end")
    # lighthouse on the north horn tip, the wreck, islets
    m.pin(1880, 1640, "L", "#FFFFFF")
    m.label(1820, 1600, "Dot's lighthouse", 12, anchor="end")
    m.legend.append(("pin", "L", "Lighthouse (harbour master Dot)", "#FFFFFF"))
    m.pin(1000, 1700, "W", C["gold"])
    m.label(1000, 1610, "Wreck of the Margaret", 12)
    m.legend.append(("pin", "W", "Wreck of the Margaret (Walt's boat; story)", C["gold"]))
    m.islet(2250, 2550, 120, 90, "Gullrock", "reef in the bay")
    m.islet(1100, 2600, 220, 120, "Lantern Key", "The Pelican (premium buyer)")
    m.islet(4100, 900, 120, 90, "Sorrel Isle", "cannery buyer (Act 3)")
    m.islet(4300, 4300, 150, 110, "Mango Cay", "late game · cartel")
    # surf
    m.surf(2800, 3820, "South horn beach break (beginner)")
    m.label(2800, 4040, "Breaker Point", 13, bold=True)
    m.surf(1650, 1330, "North horn point break (advanced)")
    m.surf(4450, 2400, "Voss Wall (breakwater wave, chased off)")
    # no zones
    m.label(300, 4520, "ANY FISH, ANYWHERE", 15, bold=True, color="#7FE3D6", anchor="start")
    m.label(300, 4600, "no fishing zones (Manan 31 Oct): your rod and bait pick the fish", 12, anchor="start")
    m.legend.append(("text", "≈", "No fishing zones: the rod and bait pick the fish", "#7FE3D6"))
    D = (2750, 2550)
    m.route([D, (2500, 2700)], "Bay Shallows (home water)")
    m.route([D, (2380, 2560)], "Gullrock (in the bay)")
    m.route([D, (2560, 2870), (1800, 2860), (1320, 2660)], "Lantern Key (buyer)")
    m.route([D, (2450, 2350), (1800, 2250), (1100, 1780)], "Wreck of the Margaret")
    m.route([D, (1800, 2650), (1750, 3400), (2600, 3900)], "South beach surf", show=False)
    m.route([D, (1800, 2650), (1750, 3400), (2200, 3800), (3000, 3900), (3700, 3500), (3950, 3000)], "Voss Harbour (round south)")
    m.route([D, (2560, 3150), (2700, 3720), (3300, 3800), (3750, 3400), (3950, 3000)], "Voss Harbour via Walt's Cut", show=False)
    m.route([D, (2450, 2350), (1800, 2250), (1700, 1500), (2300, 1150), (3400, 1250), (4000, 1000)], "Sorrel Isle", show=False)
    m.route([D, (1800, 2650), (1750, 3400), (2200, 3800), (3000, 3950), (4150, 4250)], "Mango Cay", show=False)
    m.save()


# ---------------------------------------------------------------- MAP C
def map_c():
    m = Map("C", "C · Long Island Chain", ["Long thin island, west-east. Our dock on the",
                                           "north shore, Voss on the south shore. Buyers",
                                           "on a chain of 4 islets to the north."])
    island = [(900, 2500), (1100, 2300), (1700, 2250), (2400, 2300), (3100, 2250), (3800, 2300), (4300, 2450),
              (4350, 2700), (4000, 2900), (3300, 2950), (2600, 2900), (1900, 2950), (1300, 2900), (950, 2750)]
    m.land(island)
    m.add(f'<path d="{smooth([(1200,2600),(2000,2580),(2800,2620),(3700,2600),(4100,2620)], closed=False)}" fill="none" stroke="{C["ridge"]}" stroke-width="3" stroke-dasharray="10 6" opacity=".7"/>')
    m.add(f'<path d="{smooth([(1500,2290),(1850,2270),(1900,2420),(1700,2480),(1500,2430)])}" fill="{C["town"]}"/>')
    m.label(1700, 2210, "OLD SALTMOUTH", 12, bold=True, color=C["ink"], halo=False)
    for n, (x, y) in {"1": (1560, 2340), "2": (1650, 2300), "3": (1760, 2420), "4": (1700, 2380), "5": (1830, 2330),
                      "6": (1000, 2700), "7": (1600, 2420)}.items():
        m.pin(x, y, n)
    shops_legend(m)
    m.add(f'<rect x="{1640*S:.1f}" y="{2100*S:.1f}" width="{22*S:.1f}" height="{160*S:.1f}" fill="{C["ours"]}"/>')
    m.label(1600, 2050, "HALE'S LANDING", 14, bold=True, color=C["ours"], anchor="end")
    m.add(f'<rect x="{1900*S:.1f}" y="{2050*S:.1f}" width="{600*S:.1f}" height="{230*S:.1f}" fill="url(#hatch)" opacity=".7" stroke="{C["ours"]}" stroke-dasharray="4 3"/>')
    m.legend.append(("swatch", "", "Our port expansion (east along shore)", C["ours"]))
    m.add(f'<path d="{smooth([(3300,2930),(3800,2900),(3900,3150),(3500,3250),(3250,3100)])}" fill="#9AA3A8"/>')
    m.label(3600, 3420, "VOSS HARBOUR", 15, bold=True, color="#FFB4A8")
    m.legend.append(("swatch", "", "Voss Harbour (south shore, cannot dock)", "#9AA3A8"))
    m.islet(1300, 1500, 130, 90, "Gullrock")
    m.islet(2300, 1200, 200, 110, "Lantern Key", "premium buyer")
    m.islet(3300, 1400, 160, 100, "Sorrel Isle", "cannery buyer")
    m.islet(4200, 1100, 140, 90, "Pelican Rock", "hotel buyer")
    m.islet(4300, 4200, 150, 110, "Mango Cay", "late game")
    m.zone(1650, 1850, 220, 1, "North Shallows (start)")
    m.zone(1300, 1500, 300, 3, "Gullrock Reef")
    m.zone(2800, 1700, 260, 2, "Kelp lanes")
    m.zone(700, 900, 320, 4, "Drop-off")
    m.surf(950, 3000, "West point beach break")
    D = (1650, 2080)
    m.route([D, (1650, 1850)], "North Shallows")
    m.route([D, (1400, 1600)], "Gullrock")
    m.route([D, (2250, 1350)], "Lantern Key (buyer)")
    m.route([D, (3250, 1520)], "Sorrel Isle", show=False)
    m.route([D, (1100, 2150), (750, 2550), (1100, 3050), (2200, 3150), (3200, 3200)], "Voss Harbour (round west)")
    m.save()


if __name__ == "__main__":
    map_a(); map_b(); map_c()
