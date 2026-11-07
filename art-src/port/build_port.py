# Port Boss: Walt's Landing port (Tushar 7 Nov, plan A), modelled from code in Blender (headless):
#   -> Assets/Game/Art/Port/PB_Port.fbx: the timber quay on piles along the shore (either side of Walt's pier), the
#      level port yard behind it (packed earth, a sleeper kerb), the fence round its three land sides with the gate onto
#      the town lane, two pegged-out building lots, crates, barrels and a net rack.
# Run:  blender -b --factory-startup -P art-src/port/build_port.py
# Coordinates: Walt's Landing's own Blender metres, like M5's spots (art-src/m5/spots.json): Z up, the pier runs -Y from
# its root at y ~ +10, deck top z 1.30. The game puts the model under the landing with no offset (World.cs). Map B's
# terrain is level at 1.25 m over the yard (art-src/world/island_shape.py YARD; world dx = y - 10, dy = -x from the pier
# root), so the yard sits 5 cm over it. Look rules as M5: flat per-face palette colours, every edge bevelled.
import sys, os, math, random
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'm5'))
import bpy, bmesh
from mathutils import Vector, Matrix
from m5lib import *
import m5lib

ART = os.path.join(m5lib.REPO, 'Assets', 'Game', 'Art', 'Port'); os.makedirs(ART, exist_ok=True); m5lib.ART = ART

# the yard and the quay in landing metres (from island_shape.YARD / QUAY: x = -dy, y = dx + 10)
X0, X1 = -34.0, 34.0                 # along the shore (north end -X, south end +X)
QY0, QY1 = -1.4, 8.6                 # quay: water edge .. the yard's edge
YY0, YY1 = 8.6, 43.6                 # yard: from the quay inland to the fence
ZT = 1.30                            # deck / yard top (= Walt's pier deck)
PIER = 1.38                          # Walt's pier runs up the middle (|x| < 1.38): the quay leaves it its own deck
GATE_X, GATE_W = -19.0, 4.2          # the gate in the inland fence (world dy +19)
GAP_N, GAP_S = (27.8, 5.0), (2.6, 4.0)  # the openings in the north / south fences: centre (m along from the quay end), width
                                     # (north: onto the town's lane up to June's, world x 46.5-51.6; south: onto the waterfront path to Doyle's)
MARKET_X = 17.0                      # the Market gate in the inland fence (world dy -17): the lane to Pratt's (Town.cs)
LOTS = dict(lot1=(-33.0, -23.0, 18.0, 41.0), lot2=(23.0, 33.0, 18.0, 41.0))   # x0, x1, y0, y1 (clear of both gates' ways in)
R = random.Random(711)


def J(a): return R.uniform(-a, a)


_NAMES = {}
def col_box(name, lo, hi, coll):
    """m5lib.col_box with a unique name: Blender would make a repeat "X_colBox.001", and the game reads the suffix."""
    base, suf = name.rsplit('_', 1); k = _NAMES[name] = _NAMES.get(name, 0) + 1
    return m5lib.col_box('%s%d_%s' % (base, k, suf), lo, hi, coll)


def quay(mb, coll):
    # deck boards run out from the shore (along Y), ~0.3 m wide, three tones, each a few mm proud or sunk
    for xa, xb in ((X0, -PIER), (PIER, X1)):
        n = int(round((xb - xa) / 0.3)); w = (xb - xa) / n
        for i in range(n):
            x = xa + (i + 0.5) * w; r = R.random(); col = 'weath' if r < 0.5 else 'wood' if r < 0.94 else 'greyw'   # (few grey boards: calmer from the sea)
            put(mb, box(w - 0.014, QY1 - QY0 - 0.02, 0.08, 0.012), col, (x, (QY0 + QY1) / 2 + J(0.03), ZT - 0.04 + J(0.004)))
        col_box('Quay_colDeck', (xa, QY0, ZT - 0.3), (xb, QY1, ZT), coll)
    # the water edge: a fender beam, the ends' beams, piles every 3 m in two rows, bollards every ~8.5 m
    for xa, xb in ((X0, -PIER), (PIER, X1)):
        put(mb, box(xb - xa, 0.22, 0.3, 0.03), 'midwood', ((xa + xb) / 2, QY0 - 0.09, ZT - 0.2))
    for x in (X0, X1): put(mb, box(0.22, QY1 - QY0, 0.3, 0.03), 'midwood', (x + (0.09 if x < 0 else -0.09), (QY0 + QY1) / 2, ZT - 0.2))
    for x in [X0 + 0.3 + 3.0 * k for k in range(int((X1 - X0) / 3.0) + 1)]:
        if abs(x) < PIER + 0.4: continue
        for y in (QY0 + 0.05, (QY0 + QY1) / 2):
            put(mb, cyl(0.17, 4.6, 8, bevel=0.02), 'midwood', (x, y, ZT - 0.08 - 2.3))
            put(mb, cyl(0.18, 0.5, 8, bevel=0.015), 'greyw', (x, y, -0.15))                      # the wet band at the tide line
    for x in (-30.0, -21.5, -13.0, -4.5, 4.5, 13.0, 21.5, 30.0):
        put(mb, cyl(0.17, 0.42, 10, r2=0.13, bevel=0.03), 'navy', (x, QY0 + 0.32, ZT + 0.21))
        put(mb, cyl(0.21, 0.07, 10, bevel=0.02), 'navy', (x, QY0 + 0.32, ZT + 0.45))
        col_box('Bollard_colBox', (x - 0.2, QY0 + 0.12, ZT), (x + 0.2, QY0 + 0.52, ZT + 0.5), coll)
    # under the deck at the yard's edge: a timber sheet wall down to the seabed (no terrain bank shows under the quay)
    for xa, xb in ((X0, -PIER), (PIER, X1)):
        put(mb, box(xb - xa, 0.25, 4.6, 0.02), 'dark', ((xa + xb) / 2, QY1 - 0.15, ZT - 0.1 - 2.3))
    # two iron ladders down the fender to the water (looks only: swimmers climb the pier's ladder or a boat's)
    for x in (-24.0, 24.0):
        for s in (-0.22, 0.22): put(mb, box(0.05, 0.05, 2.2, 0.01), 'iron', (x + s, QY0 - 0.25, ZT - 1.05))
        for k in range(6): put(mb, box(0.44, 0.04, 0.04, 0.008), 'iron', (x, QY0 - 0.25, ZT - 2.0 + k * 0.36))


def yard(mb, coll):
    # packed earth: one warm tone, with soft irregular patches over it (dust lighter, gravel greyer), never a grid
    # (look check 7 Nov: 2 m tiles of four tones read as a chequered floor from the sea)
    put(mb, box(X1 - X0, YY1 - YY0, 0.06, 0.0), 'weath', ((X0 + X1) / 2, (YY0 + YY1) / 2, ZT - 0.03))
    for k in range(70):
        x, y = R.uniform(X0 + 2, X1 - 2), R.uniform(YY0 + 2, YY1 - 2); r = R.uniform(1.2, 4.2)
        col = 'fresh' if k % 3 else 'greyw'
        put(mb, blob(1, (r, r * R.uniform(0.5, 0.9), 0.02), 900 + k, 0.3, 0.7), col, (x, y, ZT + 0.004 + 0.0015 * (k % 5)), (0, 0, R.uniform(0, 6.3)))
    for x in (GATE_X - 0.7, GATE_X + 0.7, MARKET_X - 0.7, MARKET_X + 0.7):               # the cart tracks from the gates to the quay
        for y in [YY0 + 0.6 + 1.2 * k for k in range(int((YY1 - YY0 - 1.2) / 1.2))]:
            put(mb, box(0.32, 1.1, 0.012, 0.0), 'midwood', (x + 0.15 * math.sin(y / 5), y, ZT + 0.012))
    col_box('Yard_colDeck', (X0, YY0, ZT - 0.4), (X1, YY1, ZT), coll)
    # the sleeper kerb round the yard's land sides
    for x0, x1, y0, y1 in ((X0, X1, YY1 - 0.2, YY1 + 0.1), (X0 - 0.1, X0 + 0.2, YY0, YY1), (X1 - 0.2, X1 + 0.1, YY0, YY1)):
        put(mb, box(x1 - x0, y1 - y0, 0.18, 0.03), 'midwood', ((x0 + x1) / 2, (y0 + y1) / 2, ZT + 0.03))


def rail_run(mb, coll, a, b, name, gaps=()):
    """A post-and-two-rail fence from a to b (x, y), weathered, posts every ~2.4 m; gaps: (centre along, width)."""
    a, b = Vector((a[0], a[1], 0)), Vector((b[0], b[1], 0)); L = (b - a).length; d = (b - a) / L
    ang = math.atan2(d.y, d.x)
    cuts = sorted([(c - w / 2, c + w / 2) for c, w in gaps])
    segs, s = [], 0.0
    for c0, c1 in cuts: segs.append((s, c0)); s = c1
    segs.append((s, L))
    for k, (s0, s1) in enumerate(segs):
        if s1 - s0 < 0.5: continue
        n = max(1, int(round((s1 - s0) / 2.4)))
        for i in range(n + 1):
            p = a + d * (s0 + (s1 - s0) * i / n)
            put(mb, box(0.14, 0.14, 1.3, 0.025), 'midwood', (p.x, p.y, ZT + 0.55), (0, 0, ang + J(0.03)))
        mid = a + d * ((s0 + s1) / 2)
        for z in (0.45, 0.95):
            put(mb, box(s1 - s0, 0.07, 0.12, 0.02), 'weath', (mid.x, mid.y, ZT + z + J(0.01)), (J(0.006), 0, ang))
        lo = a + d * s0; hi = a + d * s1; nrm = Vector((-d.y, d.x, 0)) * 0.12
        c = [lo - nrm, hi - nrm, hi + nrm, lo + nrm]
        col_box('%s%d_colBox' % (name, k), (min(v.x for v in c), min(v.y for v in c), ZT), (max(v.x for v in c), max(v.y for v in c), ZT + 1.3), coll)


def gate(coll, name, gx, sign=True):
    """A gate in the inland fence at x = gx: two tall posts, a cross-beam with a blank sign board (socket_<name>_sign: its
    face, for a name), two teal leaves swung open inward against the fence (Walt's door colours)."""
    gm = MB()
    for s in (-1, 1):
        x = gx + s * (GATE_W / 2 + 0.12)
        put(gm, box(0.26, 0.26, 3.0, 0.03), 'midwood', (x, YY1, ZT + 1.25))
        put(gm, box(0.36, 0.36, 0.12, 0.02), 'dark', (x, YY1, ZT + 2.8))
    put(gm, box(GATE_W + 0.9, 0.22, 0.24, 0.03), 'midwood', (gx, YY1, ZT + 2.62))
    if sign:
        put(gm, box(GATE_W - 0.6, 0.08, 0.62, 0.03), 'cream2', (gx, YY1 + 0.16, ZT + 2.22))
        put(gm, box(GATE_W - 0.4, 0.06, 0.74, 0.02), 'teal2', (gx, YY1 + 0.12, ZT + 2.22))
    for s in (-1, 1):                                                                        # the leaves, open 100° inward
        hx = gx + s * (GATE_W / 2); lw = GATE_W / 2 - 0.08
        cx, cy = hx - s * lw / 2 * math.cos(math.radians(80)), YY1 - lw / 2 * math.sin(math.radians(80))
        rot = (0, 0, s * math.radians(80))
        for k in range(4): put(gm, box(lw / 4 - 0.02, 0.06, 1.15, 0.015), 'teal', (cx + (k - 1.5) * (lw / 4) * math.cos(math.radians(s * 80)), cy + (k - 1.5) * (lw / 4) * math.sin(math.radians(s * 80)), ZT + 0.68), rot)
        put(gm, box(lw, 0.07, 0.12, 0.015), 'cream2', (cx, cy, ZT + 0.42), rot); put(gm, box(lw, 0.07, 0.12, 0.015), 'cream2', (cx, cy, ZT + 1.0), rot)
    finish(gm, name, coll)
    for s in (-1, 1): col_box('%sPost%d_colBox' % (name, s + 1), (gx + s * (GATE_W / 2 + 0.12) - 0.15, YY1 - 0.15, ZT), (gx + s * (GATE_W / 2 + 0.12) + 0.15, YY1 + 0.15, ZT + 3.0), coll)
    empty('socket_%s' % name, (gx, YY1 + 1.2, ZT), coll, 180)                                 # outside the gate, facing in
    if sign: empty('socket_%s_sign' % name, (gx, YY1 + 0.2, ZT + 2.22), coll, 180)


def fence_and_gate(mb, coll):
    # the north side opens onto the town's lane up to June's, the south side onto the waterfront path to Doyle's; inland
    # the town gate (the lane into town) and the Market gate (the lane to Pratt's)
    rail_run(mb, coll, (X0, YY0 + 0.3), (X0, YY1), 'FenceN', gaps=(GAP_N,))
    rail_run(mb, coll, (X1, YY0 + 0.3), (X1, YY1), 'FenceS', gaps=(GAP_S,))
    rail_run(mb, coll, (X0, YY1), (X1, YY1), 'FenceE', gaps=((GATE_X - X0, GATE_W), (MARKET_X - X0, GATE_W)))
    gate(coll, 'YardGate', GATE_X); gate(coll, 'MarketGate', MARKET_X)


def lots(mb, coll):
    # stakes with a red flag at each corner and string between them at knee height; a blank board at the front
    for name, (x0, x1, y0, y1) in LOTS.items():
        c = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        for x, y in c:
            put(mb, box(0.07, 0.07, 0.7, 0.01), 'weath', (x, y, ZT + 0.33), (J(0.06), J(0.06), 0))
            put(mb, box(0.02, 0.18, 0.12, 0.005), 'poppy', (x, y + 0.1, ZT + 0.62))
        for (ax, ay), (bx_, by_) in zip(c, c[1:] + c[:1]):
            L = math.hypot(bx_ - ax, by_ - ay); ang = math.atan2(by_ - ay, bx_ - ax)
            put(mb, box(L, 0.015, 0.015, 0.0), 'paper', ((ax + bx_) / 2, (ay + by_) / 2, ZT + 0.45), (0, 0, ang))
        bx0 = (x0 + x1) / 2; by0 = y0 - 0.2                                                # the board faces the quay (-Y)
        put(mb, box(0.09, 0.09, 1.2, 0.01), 'midwood', (bx0 - 0.5, by0, ZT + 0.6)); put(mb, box(0.09, 0.09, 1.2, 0.01), 'midwood', (bx0 + 0.5, by0, ZT + 0.6))
        put(mb, box(1.5, 0.06, 0.7, 0.02), 'cream2', (bx0, by0 - 0.07, ZT + 1.0)); put(mb, box(1.62, 0.05, 0.08, 0.01), 'sun', (bx0, by0 - 0.08, ZT + 1.38))
        empty('socket_%s' % name, (bx0, by0 - 0.12, ZT + 1.0), coll, 0)
        empty('spot_%s' % name, ((x0 + x1) / 2, (y0 + y1) / 2, ZT), coll, 0)


def clutter(mb, coll):
    # crates and fish boxes by the quay, barrels, a coil of rope, a net rack and a hand cart; colliders on the solid ones
    def crate(x, y, z, s=0.62, col='wood', a=0.0):
        put(mb, box(s, s, s, 0.03), col, (x, y, z + s / 2), (0, 0, a))
        for k in (-1, 1): put(mb, box(s + 0.02, 0.06, 0.06, 0.01), 'midwood', (x + k * 0.0, y, z + s / 2 + k * s * 0.3), (0, 0, a))
    def stack(x, y, a):
        crate(x, y, ZT, 0.66, 'wood', a); crate(x + 0.7, y + J(0.05), ZT, 0.62, 'weath', a + J(0.1)); crate(x + 0.33, y + J(0.05), ZT + 0.66, 0.6, 'fresh', a + J(0.15))
        col_box('Crates_colBox', (x - 0.4, y - 0.4, ZT), (x + 1.1, y + 0.4, ZT + 1.3), coll)
    def barrel(x, y):
        put(mb, cyl(0.3, 0.86, 10, r2=0.27, bevel=0.03), 'rust' if R.random() < 0.5 else 'teal2', (x, y, ZT + 0.43))
        for z in (0.2, 0.66): put(mb, cyl(0.31, 0.05, 10, bevel=0.01), 'iron', (x, y, ZT + z))
        col_box('Barrel_colBox', (x - 0.32, y - 0.32, ZT), (x + 0.32, y + 0.32, ZT + 0.9), coll)
    stack(-12.0, 5.6, 0.1); stack(16.0, 6.0, -0.05); stack(-29.0, 11.5, 0.2); stack(27.5, 11.0, 0.0)
    for x, y in ((-10.2, 6.8), (-9.6, 7.5), (18.4, 7.0), (-27.6, 12.6)): barrel(x, y)
    for x, y in ((-6.5, 4.5), (9.0, 4.0)):                                                   # fish boxes (blue, open), two high
        for k in range(2): put(mb, box(0.7, 0.45, 0.22, 0.02), 'cblue', (x, y, ZT + 0.11 + 0.23 * k), (0, 0, J(0.1)))
    put(mb, cyl(0.42, 0.14, 14, r2=0.42, bevel=0.03), 'rope', (5.5, 6.6, ZT + 0.07)); put(mb, cyl(0.24, 0.15, 14, bevel=0.02), 'sand', (5.5, 6.6, ZT + 0.08))
    # a net rack: two A-frames and a pole with a hanging net, on the north side of the yard
    for x in (-15.0, -11.0):
        for s in (-1, 1): put(mb, box(0.09, 0.09, 2.0, 0.01), 'midwood', (x, 30.0 + s * 0.35, ZT + 0.95), (s * 0.18, 0, 0))
    put(mb, cyl(0.05, 4.4, 6), 'midwood', (-13.0, 30.0, ZT + 1.85), (0, math.radians(90), 0))
    put(mb, box(3.8, 0.04, 1.3, 0.0), 'rope', (-13.0, 30.0, ZT + 1.2)); col_box('NetRack_colBox', (-15.3, 29.5, ZT), (-10.7, 30.5, ZT + 2.0), coll)
    # a hand cart by the warehouse side
    put(mb, box(1.2, 0.8, 0.3, 0.02), 'wood', (8.5, 19.5, ZT + 0.55)); put(mb, cyl(0.3, 0.08, 12, bevel=0.01), 'iron', (8.5, 19.0, ZT + 0.3), (math.radians(90), 0, 0))
    put(mb, cyl(0.3, 0.08, 12, bevel=0.01), 'iron', (8.5, 20.0, ZT + 0.3), (math.radians(90), 0, 0)); put(mb, box(0.06, 0.06, 1.3, 0.01), 'midwood', (9.4, 19.5, ZT + 0.5), (0, math.radians(65), 0))
    col_box('Cart_colBox', (7.8, 19.0, ZT), (9.3, 20.0, ZT + 0.75), coll)


def build():
    coll = new_coll('PB_Port'); mb = MB()
    quay(mb, coll); yard(mb, coll); fence_and_gate(mb, coll); lots(mb, coll); clutter(mb, coll)
    print('PORT tris', mb.tris())
    finish(mb, 'PB_Port', coll)
    export(coll, 'PB_Port.fbx')
    print('PORT objects', sorted(o.name for o in coll.objects if not o.name.endswith('_colBox')))


build()
