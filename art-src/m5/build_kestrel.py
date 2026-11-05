# Port Boss M5 art, modelled from code in Blender 5.1 (headless, never opens a window):
#   the Kestrel (T2 fishing launch, 7.5 m: centre console with a ship's wheel, cuddy cabin with one bunk, aft fishing
#   deck with 3 rod holders, swim ladder, outboard) -> Assets/Game/Art/PB_Boat_Kestrel_T2.fbx
# Run:  blender -b -P art-src/m5/build_kestrel.py -- [norender]
# Same look and conventions as Biscuit (art-src/m1/build_boat_landing.py, whose helpers this imports): flat per-face
# palette colours ("Col", sRGB bytes), every edge bevelled, a little seeded wobble; 1 unit = 1 m, Z up, bow to -Y,
# origin on the waterline. Paint channel per face in Col alpha = 255 - 16*ch (1 Hull, 2 Stripe, 3 Trim, 4 Deck).
# .blend + self-check renders go to art-src/m5/out/ (gitignored).
import sys, os, math, random, functools
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'm1'))
_argv = sys.argv; sys.argv = [_argv[0], '--', 'lib']          # import the m1 helpers without building Biscuit
import build_boat_landing as L
sys.argv = _argv
from build_boat_landing import (bpy, bmesh, Vector, Matrix, MB, put, finish, col_box, empty, box, board, cyl, blob, circle,
                                rrect, sweep, slab, clip, zsplit, torus, coil, new_scene, export, setup_render, shoot,
                                bev_sharp, clamp, lerp, D, M, frange, CI, PAL, NAMES)
ARGV = _argv[_argv.index('--') + 1:] if '--' in _argv else []
RENDER = 'norender' not in ARGV
OUT = os.path.join(HERE, 'out'); L.REN = os.path.join(OUT, 'renders')
for _d in (OUT, L.REN): os.makedirs(_d, exist_ok=True)
for n, h in (('teak', '#C8915F'), ('glass', '#A8DCF0')):        # two new swatches: the fresh teak deck, pale glass
    PAL[n] = h; NAMES.append(n); CI[n] = len(NAMES) - 1
def J(a): return L.R.uniform(-a, a)

# ---------------------------------------------------------------- hull lines ---------------------------------------------------
NS = 18
TST = [(i / NS) ** 1.3 for i in range(NS + 1)]               # stations bow (t=0) -> transom (t=1), denser at the bow
DECK, SKIN = 0.35, 0.085                                      # deck top above the waterline, topsides/bulwark thickness
WL, BOOT = 0.05, 0.12                                         # antifouling line, boot-top stripe top
ST0, ST1 = 0.30, 0.12                                         # sheer stripe: from zs-0.30 up to zs-0.12
def fore(t, tm, p): return 1 - (1 - min(t / tm, 1)) ** p
def zs(t): return 1.06 + 0.04 * (1 - t) + 0.50 * max(0, (0.42 - t) / 0.42) ** 2           # sheer
def zk(t): return -0.40 + 0.64 * max(0, 1 - t / 0.34) ** 2                                # keel
def zc(t): return -0.06 + 0.52 * max(0, 1 - t / 0.55) ** 2                                # chine
def xc(t): return 1.10 * fore(t, 0.36, 3.0) - (0.07 * ((t - 0.7) / 0.3) ** 2 if t > 0.7 else 0)
def Bs(t): return 1.37 * fore(t, 0.28, 2.4) - (0.10 * ((t - 0.62) / 0.38) ** 2 if t > 0.62 else 0)
def ybow(z):
    z0, z1 = zk(0), zs(0); f = clamp((z - z0) / (z1 - z0)); return -3.28 - 0.47 * f ** 0.75
def ystern(z): return 3.62 + 0.07 * clamp((z - zk(1)) / (zs(1) - zk(1)))
def yst(t, z): return ybow(z) + t * (ystern(z) - ybow(z))


def qbez(a, b, c, u): return ((1 - u) ** 2 * a[0] + 2 * u * (1 - u) * b[0] + u * u * c[0], (1 - u) ** 2 * a[1] + 2 * u * (1 - u) * b[1] + u * u * c[1])


class Sec:
    """Half section (+x) at station t: keel, a slightly convex V bottom, the chine with a spray-rail lip, then
    topsides that flare out and stand up near-vertical to the sheer. cols = the mesh columns (stripe edges exact)."""
    def __init__(s, t):
        s.t = t; k, c, x, S, top = zk(t), zc(t), xc(t), Bs(t), zs(t)
        lip = 0.05 * min(1, t / 0.15)
        bot = [(x * u, k + (c - k) * u - 0.04 * math.sin(math.pi * u) * min(1, t / 0.2)) for u in (0, 0.35, 0.7, 1.0)]
        a, cp, b = (x + lip, c + 0.05), (S - 0.02, lerp(c, top, 0.10)), (S, top)
        s.top = [qbez(a, cp, b, i / 30) for i in range(31)]
        s.dense = bot[:-1] + [bot[-1], (x + lip, c + 0.015)] + s.top
        zt = lambda zz: s.at_z(zz, s.top)
        s.cols = bot + [(x + lip, c + 0.015), a, (zt(lerp(a[1], top - ST0, 0.5)), lerp(a[1], top - ST0, 0.5)),
                        (zt(top - ST0), top - ST0), (zt(top - ST1), top - ST1), b]

    @staticmethod
    def at_z(z, pts):
        if z <= pts[0][1]: return pts[0][0] if z >= pts[0][1] - 1e-9 else 0.0
        for (x0, z0), (x1, z1) in zip(pts, pts[1:]):
            if z0 <= z <= z1: return x0 + (x1 - x0) * (z - z0) / max(1e-9, z1 - z0)
        return pts[-1][0]

    def hw(s, z): return 0.0 if z < s.dense[0][1] else s.at_z(z, s.dense)


SEC = [Sec(t) for t in TST]
COLC = ['paper'] * 7 + ['blue', 'paper']                      # face colour per column strip (strip 7 = sheer stripe)
HULL_CH = {'paper': 1, 'blue': 2}


def hw_in(t, z):                                              # inner face of the topsides/bulwark at height z
    return max(0.0, Sec(t).hw(z) - SKIN)


@functools.lru_cache(None)
def plan_in(z, n=60):
    """[(y, x)] inner half-width at height z, bow -> stern (y from the stem curve)."""
    out = []
    for i in range(n + 1):
        t = (i / n) ** 1.3; x = hw_in(t, z)
        if x > 0.002: out.append((yst(t, z), x))
    return out


def inner_x(y, z):
    P = plan_in(z)
    if y < P[0][0] or y > P[-1][0]: return 0.0
    for (y0, x0), (y1, x1) in zip(P, P[1:]):
        if y0 <= y <= y1: return x0 + (x1 - x0) * (y - y0) / max(1e-9, y1 - y0)
    return P[-1][1]


def build_hull(mb):
    # --- outer skin, both sides, stitched at keel and stem ---
    bm = bmesh.new(); lay = bm.faces.layers.int.new('ci')
    for sd in (1, -1):
        Rg = [[bm.verts.new((sd * x, yst(TST[i], z), z)) for x, z in SEC[i].cols] for i in range(NS + 1)]
        for i in range(NS):
            for j in range(len(Rg[0]) - 1):
                q = [Rg[i][j], Rg[i][j + 1], Rg[i + 1][j + 1], Rg[i + 1][j]]
                if sd < 0: q.reverse()
                bm.faces.new(q)[lay] = CI[COLC[j]]
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
    zsplit(bm, WL, BOOT)
    for f in bm.faces:
        z = f.calc_center_median().z
        if z < WL: f[lay] = CI['rust']
        elif z < BOOT: f[lay] = CI['blue']
    mb.add(bm, ch=HULL_CH)
    # --- inner face of the topsides (the bulwark seen from the deck), cream, deck level up to the sheer ---
    bm = bmesh.new()
    for sd in (1, -1):
        Rg = []
        for i in range(NS + 1):
            t = TST[i]; S = SEC[i]; top = zs(t)
            Rg.append([bm.verts.new((sd * max(0.0, S.hw(z) - SKIN), yst(t, z), z)) for z in (DECK - 0.06, lerp(DECK, top, 0.5), top - 0.01)])
        for i in range(NS):
            for j in range(2):
                q = [Rg[i][j], Rg[i][j + 1], Rg[i + 1][j + 1], Rg[i + 1][j]]
                if sd > 0: q.reverse()
                bm.faces.new(q)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
    mb.add(bm, 'cream2')
    # --- transom: a flat raked panel with the same bands as the sides, cream inside ---
    S = SEC[NS]; ring = [Vector((x, ystern(z), z)) for x, z in S.cols]
    loop = ring + [Vector((-p.x, p.y, p.z)) for p in reversed(ring[1:])]
    loop = [p for k, p in enumerate(loop) if k == 0 or (p - loop[k - 1]).length > 1e-4]
    if (loop[0] - loop[-1]).length < 1e-4: loop.pop()
    cen = Vector((0, ystern(0.4), 0.4)); inward = Vector((0, -0.10, 0))
    bm = bmesh.new(); lay = bm.faces.layers.int.new('ci')
    F = [bm.verts.new(p) for p in loop]
    B = [bm.verts.new(p + inward + (Vector((0, p.y, cen.z)) - p).normalized() * SKIN) for p in loop]
    vf, vb = bm.verts.new(cen), bm.verts.new(cen + inward); n = len(loop)
    for j in range(n):
        a, b = j, (j + 1) % n
        bm.faces.new((vf, F[a], F[b]))[lay] = CI['paper']
        bm.faces.new((vb, B[b], B[a]))[lay] = CI['cream2']
        bm.faces.new((F[b], F[a], B[a], B[b]))[lay] = CI['paper']
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    z1 = zs(1); zsplit(bm, WL, BOOT, z1 - ST0, z1 - ST1)
    for f in bm.faces:
        if f[lay] == CI['cream2']: continue
        z = f.calc_center_median().z
        f[lay] = CI['rust' if z < WL else 'blue' if z < BOOT else 'blue' if z1 - ST0 < z < z1 - ST1 else 'paper']
    mb.add(bm, ch=HULL_CH)
    # --- gunwale cap + fat rubrail in one warm-wood sweep, both sides ---
    prof = rrect(-0.115, 0.055, -0.10, 0.045, 0.03, (1, 1, 0, 0))
    for sd in (1, -1):
        path = [Vector((sd * Bs(t), yst(t, zs(t)), zs(t))) for t in TST]
        path[0] = path[0] + Vector((0, 0.02, 0)); path.append(path[-1] + Vector((0, 0.06, 0)))
        mb.add(sweep(path, [(a * sd, b) for a, b in prof]), 'wood', ch=3)
    # transom cap (wood), a gentle crown
    w = Bs(1) + 0.05; yc = lambda z: ystern(z) - 0.035
    path = [Vector((x, yc(z1), z1 + 0.025 * (1 - (x / w) ** 2))) for x in (-w, -w * 0.5, 0, w * 0.5, w)]
    mb.add(sweep(path, rrect(-0.085, 0.07, -0.05, 0.045, 0.025, (2, 2, 1, 1))), 'wood', ch=3)
    # --- keel strip + stem guard (one swept backbone), antifouling below the waterline ---
    path = [Vector((0, ystern(zk(1)) + 0.02, zk(1) - 0.01))] + [Vector((0, yst(TST[i], zk(TST[i])), zk(TST[i]) - 0.01)) for i in range(NS, 0, -1)]
    z0 = zk(0)
    for m in range(0, 7):
        z = z0 + (zs(0) + 0.03 - z0) * m / 6; path.append(Vector((0, ybow(min(z, zs(0))) - 0.012, z)))
    bm = sweep(path, rrect(-0.04, 0.04, -0.03, 0.04, 0.018, (1, 0, 0, 1)), side=(1, 0, 0))
    zsplit(bm, WL)
    mb.add(bm, lambda c, n: 'rust' if c.z < WL else 'greyw')


# ---------------------------------------------------------------- layout -------------------------------------------------------
CA, CF, CW = -0.30, -1.95, 0.43            # cuddy: aft wall y, end of the straight sides, outer half-width
CN = CF - 0.40                             # the rounded nose tip
CROOF = 1.62                               # cabin wall top (roof underside)
DW, DH = 0.56, 1.05                        # door opening
KY0, KY1, KW, KH = 0.79, 1.45, 0.48, 1.30  # console y range, half-width, body top
SEATY = (0.45, 0.77)                       # seat across the console front
TR_IN = None                               # transom inner y at deck height (set in build)


def cabin_plan(inset=0.0, n=7):
    """CCW outline of the cabin (outer face) shrunk by inset: straight sides, an elliptical nose."""
    w = CW - inset; a = CA - inset; f = CF; r = (CF - CN) - inset
    pts = [(w, a)]
    for k in range(n + 1):
        th = math.pi * k / n
        pts.append((w * math.cos(th), f - r * math.sin(th)))
    pts.append((-w, a))
    return pts[::-1]                                          # (-w,a) .. nose .. (w,a) is clockwise -> reverse is CCW


def build_deck(mb):
    """Teak planks lengthwise over the whole deck (bow well, side decks, cockpit, under the cabin), clipped to the hull."""
    global TR_IN
    TR_IN = ystern(DECK) - 0.10
    P = [(y, x - 0.008) for y, x in plan_in(DECK - 0.02) if y < TR_IN]
    right = [(x, y) for y, x in P] + [(inner_x(TR_IN, DECK - 0.02) - 0.008, TR_IN)]
    keep = [right[0]]
    for p in right[1:-1]:
        if math.dist(p, keep[-1]) > 0.14: keep.append(p)
    right = keep + [right[-1]]                                # a coarse outline: fewer verts on every clipped plank
    poly = right[::-1] + [(-x, y) for x, y in right if x > 0.001]
    poly = poly[::-1] if sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly))) < 0 else poly
    pw, gap = 0.20, 0.014
    nP = int(2.7 / (pw + gap)) + 1; x0 = -nP * (pw + gap) / 2
    for k in range(nP):
        a = x0 + k * (pw + gap); b = a + pw
        p = clip(poly, [(a, -4.0), (b, -4.0), (b, 4.0), (a, 4.0)])     # the hull plan clipped by a convex strip
        if len(p) >= 3: mb.add(slab(p, DECK - 0.045, DECK + J(0.003), 0.012, 1), 'teak', ch=4)
    # the sub-sole under the seams (dark)
    mb.add(slab(poly, DECK - 0.07, DECK - 0.04, 0.0, 1), 'midwood', ch=4)
    return poly


def wall_col(cen):
    def f(c, n):
        if n.z > 0.6: return 'paper'
        d = Vector((c.x - cen[0], c.y - cen[1], 0))
        return 'paper' if n.dot(d) > 0 else 'cream3'
    return f


def build_cabin(mb):
    # walls: a U swept around the sides and nose, leaning in a touch; the aft bulkhead with the door is separate
    U = [(CW, CA)] + [(CW * math.cos(math.pi * k / 8), CF - (CF - CN) * math.sin(math.pi * k / 8)) for k in range(9)] + [(-CW, CA)]
    U = [U[0], (CW, lerp(CA, CF, 0.5))] + U[1:-1] + [(-CW, lerp(CA, CF, 0.5)), U[-1]]
    path = [Vector((x, y, DECK - 0.04)) for x, y in U]
    th, lean, h = 0.06, 0.035, CROOF - DECK + 0.04
    bm = sweep(path, [(0.0, 0.0), (th, 0.0), (th + lean, h), (lean, h)], up=(0, 0, 1))
    bev_sharp(bm, 0.014, 1, 50)
    mb.add(bm, wall_col((0, (CA + CN) / 2)), ch=0)
    # aft bulkhead: two side panels and a lintel round the door opening, white outside, cream inside
    zt = CROOF + 0.02
    for a, b in ((DW / 2, CW - 0.01), (-(CW - 0.01), -DW / 2)):
        mb.add(board(a, b, CA - 0.06, CA, DECK - 0.03, zt, 0.012, 1), wall_col((0, CA - 1)))
    mb.add(board(-DW / 2, DW / 2, CA - 0.06, CA, DECK + DH, zt, 0.012, 1), wall_col((0, CA - 1)))
    # door frame (wood trim), proud of the bulkhead
    fr = [(DW / 2 + 0.03, CA + 0.005, DECK), (DW / 2 + 0.03, CA + 0.005, DECK + DH - 0.08), (DW / 2 - 0.04, CA + 0.005, DECK + DH + 0.03),
          (-DW / 2 + 0.04, CA + 0.005, DECK + DH + 0.03), (-DW / 2 - 0.03, CA + 0.005, DECK + DH - 0.08), (-DW / 2 - 0.03, CA + 0.005, DECK)]
    mb.add(sweep(fr, rrect(-0.04, 0.04, -0.03, 0.03, 0.012, (1, 1, 1, 1)), side=(0, 1, 0)), 'wood', ch=3)
    # rolled canvas door curtain over the opening
    put(mb, cyl(0.07, DW + 0.04, 8, bevel=0.02), 'sun', (0, CA + 0.06, DECK + DH + 0.10), (0, D(90), 0))
    for x in (-0.18, 0.18): put(mb, box(0.03, 0.15, 0.03, 0.01, 1), 'dark', (x, CA + 0.06, DECK + DH + 0.10))
    # threshold step (wood trim)
    put(mb, box(DW + 0.06, 0.10, 0.05, 0.015, 1), 'wood', (0, CA - 0.03, DECK + 0.02), ch=3)
    # roof: a slab with a little overhang, cream non-skid top, a wood trim band round its edge
    rp = cabin_plan(-0.05)
    mb.add(slab(rp, CROOF, CROOF + 0.08, 0.025, 1), lambda c, n: 'cream2' if n.z > 0.6 else 'paper', ch=0)
    band = [Vector((x, y, CROOF - 0.015)) for x, y in cabin_plan(0.012, 10)]
    mb.add(sweep(band, rrect(-0.025, 0.025, -0.035, 0.03, 0.012, (1, 1, 1, 1)), up=(0, 0, 1)), 'wood', ch=3)
    # roof: a forward hatch and two teak grab rails
    put(mb, box(0.50, 0.50, 0.08, 0.03, 2), 'paper', (0, CF + 0.05, CROOF + 0.12))
    put(mb, box(0.40, 0.40, 0.03, 0.012, 1), 'glass', (0, CF + 0.05, CROOF + 0.165))
    for sd in (1, -1):
        x = sd * (CW - 0.12); y0, y1 = CA - 0.15, CF + 0.15
        rail = [Vector((x, y0, CROOF + 0.08)), Vector((x, y0 - 0.06, CROOF + 0.18)), Vector((x, y1 + 0.06, CROOF + 0.18)), Vector((x, y1, CROOF + 0.08))]
        mb.add(sweep(rail, rrect(-0.02, 0.02, -0.022, 0.022, 0.01, (1, 1, 1, 1)), side=(1, 0, 0)), 'wood', ch=3)
    # portholes: wood rims (trim) with sky-blue glass, 2 a side + one in the nose
    def port(c, nrm, r=0.10):
        nrm = Vector(nrm).normalized(); q = nrm.to_track_quat('Z', 'Y')
        m = Matrix.Translation(Vector(c)) @ q.to_matrix().to_4x4()
        mb.add(torus((0, 0, 0), r, 0.03, 'Z', 8, 4), 'wood', m, ch=3)
        mb.add(cyl(r - 0.005, 0.03, 8, bevel=0.0), 'glass', m)
        mb.add(cyl(r - 0.01, 0.012, 8, bevel=0.0), 'glass', m @ Matrix.Translation((0, 0, -0.085)))   # seen from inside
    zpo = (DECK + CROOF) / 2 + 0.12
    for sd in (1, -1):
        for y in (-0.85, -1.55):
            port((sd * (CW + 0.02 - 0.035 * 0.62), y, zpo), (sd, 0, 0.035 / 1.2))
    port((0, CN - 0.02, zpo), (0, -1, 0.03), 0.09)
    # --- inside: the bunk (a cream box, a teal mattress, a pillow at the bow end, a folded sun-yellow blanket) ---
    by0, by1 = CA - 0.10, CN + 0.08
    strip = lambda p: clip(p, [(-1, by1), (1, by1), (1, by0), (-1, by0)])
    mz = DECK + 0.27; mt = 0.13
    mb.add(slab(strip(cabin_plan(0.075, 8)), DECK - 0.02, mz, 0.02, 1), 'cream2')
    mb.add(slab(strip(cabin_plan(0.095, 8)), mz - 0.01, mz + mt, 0.045, 1), 'teal')
    put(mb, blob(1, (0.25, 0.16, 0.08), 31, 0.06), 'paper', (0, by1 + 0.26, mz + mt + 0.06))
    # warm wood inside: ceiling battens and a shelf along each side above the bunk (trim)
    for y in (CA - 0.35, -1.05, -1.65):
        put(mb, box(2 * CW - 0.16, 0.06, 0.05, 0.015, 1), 'midwood', (0, y, CROOF - 0.03), ch=3)
    for sd in (1, -1):
        put(mb, box(0.10, 1.10, 0.035, 0.012, 1), 'wood', (sd * (CW - 0.13), -1.20, mz + 0.50), ch=3)
    put(mb, box(2 * (CW - 0.08) - 0.02, 0.36, 0.06, 0.025, 2), 'sun', (0, by0 - 0.24, mz + mt + 0.02), (D(1.5), 0, 0))
    # a little brass lamp over the pillow
    put(mb, cyl(0.05, 0.08, 8, bevel=0.012), 'sun', (0, CN + 0.08, CROOF - 0.12), (D(90), 0, 0))
    return (0, (by0 + by1) / 2, mz + mt)


def build_console(mb):
    # chunky warm-wood console (trim), a cream top with an ink dash and dials, a short windscreen
    cy = (KY0 + KY1) / 2
    put(mb, box(2 * KW, KY1 - KY0, KH - DECK + 0.02, 0.07, 2), 'wood', (0, cy, (KH + DECK) / 2 - 0.01), ch=3)
    put(mb, box(2 * KW + 0.06, 0.08, 0.10, 0.03, 1), 'midwood', (0, KY1 - 0.02, DECK + 0.05), ch=3)        # kick plinth
    put(mb, box(2 * KW - 0.18, 0.03, KH - DECK - 0.30, 0.012, 1), 'cream2', (0, KY1 + 0.005, (KH + DECK) / 2 + 0.02))   # face panel
    # sloped dash, rising to the front
    dz = lambda x, y: KH + 0.04 + 0.16 * (KY1 - y) / (KY1 - KY0)
    mb.add(board(-KW + 0.02, KW - 0.02, KY0 + 0.02, KY1 - 0.02, KH - 0.04, dz, 0.025, 2), lambda c, n: 'ink' if n.z > 0.5 else 'paper')
    for x, r, cl in ((-0.18, 0.075, 'paper'), (0.0, 0.09, 'paper'), (0.20, 0.06, 'sun')):
        y = KY1 - 0.24; z = dz(x, y) + 0.012
        put(mb, cyl(r, 0.03, 8, bevel=0.01), 'iron', (x, y, z), (D(-13.5), 0, 0))
        put(mb, cyl(r - 0.018, 0.02, 8, bevel=0.0), cl, (x, y - 0.002, z + 0.012), (D(-13.5), 0, 0))
    # windscreen: wood frame (trim) round a sky-blue pane, leaning back
    wz0 = dz(0, KY0 + 0.05); wy = KY0 + 0.07
    fr = [(-KW + 0.03, wy, wz0), (-KW + 0.07, wy + 0.08, wz0 + 0.30), (KW - 0.07, wy + 0.08, wz0 + 0.30), (KW - 0.03, wy, wz0)]
    mb.add(sweep(fr, rrect(-0.03, 0.03, -0.028, 0.028, 0.012, (1, 1, 1, 1)), side=(0, 1, 0)), 'wood', ch=3)
    mb.add(board(-KW + 0.06, KW - 0.06, wy - 0.012, wy + 0.012, wz0, wz0 + 0.27, 0.008, 1, lean=(0, 0.07)), 'glass')
    # front seat across the console (white box, teal cushion)
    a, b = SEATY
    put(mb, box(2 * KW - 0.04, b - a, 0.34, 0.04, 1), 'paper', (0, (a + b) / 2, DECK + 0.17))
    put(mb, box(2 * KW, b - a + 0.04, 0.10, 0.045, 1), 'teal', (0, (a + b) / 2 - 0.01, DECK + 0.38))
    # grab rail round the console top (stainless)
    gr = [(KW + 0.05, KY1 - 0.06, KH - 0.15), (KW + 0.05, KY1 - 0.06, KH + 0.05), (KW + 0.05, KY0 + 0.10, KH + 0.18), (KW + 0.05, KY0 + 0.10, KH - 0.05)]
    for sd in (1, -1):
        mb.add(sweep([Vector((sd * p[0], p[1], p[2])) for p in gr], circle(0.018, 6), side=(1, 0, 0)), 'greyw')
    # life ring on the console's port side
    m = M((KW + 0.07, cy - 0.02, (KH + DECK) / 2 + 0.05), (0, D(90), 0))
    for k in range(6):
        a0 = 2 * math.pi * k / 6; a1 = 2 * math.pi * (k + 1) / 6
        arc = [Vector((0.20 * math.cos(a0 + (a1 - a0) * q / 2), 0.20 * math.sin(a0 + (a1 - a0) * q / 2), 0)) for q in range(3)]
        mb.add(sweep(arc, circle(0.05, 5), up=(0, 0, 1), cap=True), 'poppy' if k % 2 else 'paper', m)


def build_wheel(coll):
    """The ship's wheel: own object (pivot = hub = socket_wheel), turns about its local Z (the shaft, tilted aft-up)."""
    wb = MB(); r = 0.235
    wb.add(torus((0, 0, 0), r, 0.032, 'Z', 12, 6), 'midwood', ch=3)
    wb.add(cyl(0.06, 0.08, 10, bevel=0.02), 'sun')
    wb.add(cyl(0.035, 0.05, 8, bevel=0.012), 'wood', M((0, 0, 0.06)), ch=3)
    for k in range(6):
        a = 2 * math.pi * k / 6 + D(15)
        d = Vector((math.cos(a), math.sin(a), 0))
        wb.add(sweep([d * 0.05, d * (r + 0.01)], circle(0.016, 5), side=(0, 0, 1)), 'midwood', ch=3)
        wb.add(sweep([d * (r + 0.02), d * (r + 0.07), d * (r + 0.11)], [circle(q, 6) for q in (0.022, 0.03, 0.022)], side=(0, 0, 1)), 'midwood', ch=3)
    wh = finish(wb, 'Wheel', coll)
    wh.location = (0, KY1 + 0.12, KH - 0.03); wh.rotation_euler = (D(-70), 0, 0)          # faces aft, 20 deg off vertical
    return wh


def build_throttle(coll):
    tb = MB()
    tb.add(sweep([(0, 0, 0), (0, 0, 0.12), (0, -0.02, 0.22)], circle(0.016, 6), up=(0, 1, 0)), 'greyw')
    tb.add(blob(1, (0.035, 0.035, 0.035), 9, 0.04), 'red', M((0, -0.02, 0.24)))
    th = finish(tb, 'Throttle', coll); th.location = (-KW + 0.12, KY1 - 0.10, KH + 0.06); th.rotation_euler = (D(-20), 0, 0)
    return th


def build_aft(mb):
    # corner seats in the aft quarters (white boxes, teal cushions); the centre stays free for the motor and ladder
    out = []
    for sd in (1, -1):
        x0, x1 = 0.62, inner_x(TR_IN - 0.2, DECK + 0.3) - 0.01
        y0, y1 = TR_IN - 0.46, TR_IN - 0.005
        hwx = (x0 + x1) / 2
        put(mb, box(x1 - x0, y1 - y0, 0.34, 0.035, 1), 'paper', (sd * hwx, (y0 + y1) / 2, DECK + 0.17))
        put(mb, box(x1 - x0 + 0.02, y1 - y0 + 0.03, 0.10, 0.045, 1), 'teal', (sd * hwx, (y0 + y1) / 2 - 0.01, DECK + 0.38))
        out.append((sd * hwx, (y0 + y1) / 2, DECK + 0.43, x0, x1, y0, y1))
    # 3 flush rod holders: port + starboard aft quarters on the gunwale, one on the transom cap
    holders = []
    for sd in (1, -1):
        y = 2.35; t = clamp((y - ybow(zs(0.7))) / (ystern(zs(0.7)) - ybow(zs(0.7))))
        holders.append((sd * (Bs(t) - 0.03), y, zs(t) + 0.04))
    holders.append((-0.62, ystern(zs(1)) - 0.035, zs(1) + 0.06))
    for (x, y, z) in holders:
        mb.add(cyl(0.035, 0.05, 8, bevel=0.012), 'greyw', M((x, y, z)))
        mb.add(cyl(0.022, 0.012, 8, bevel=0.0), 'ink', M((x, y, z + 0.026)))
    # stern cleats on the cap corners, a coiled stern line
    for sd in (1, -1):
        cx = sd * (Bs(1) - 0.12); cyy = ystern(zs(1)) - 0.03
        put(mb, box(0.05, 0.05, 0.05, 0.015, 1), 'greyw', (cx, cyy, zs(1) + 0.075))
        put(mb, box(0.22, 0.05, 0.04, 0.015, 1), 'greyw', (cx, cyy, zs(1) + 0.105))
    # swim ladder over the transom (port of the motor), down to 0.6 m under the waterline
    lx = (0.42, 0.66); zt = zs(1) + 0.06
    yi = ystern(zt) - 0.09
    for x in lx:
        path = [(x, yi, zt - 0.10), (x, yi + 0.02, zt + 0.06), (x, yi + 0.12, zt + 0.12), (x, yi + 0.22, zt + 0.08), (x, ystern(zt) + 0.17, zt - 0.05),
                (x, ystern(0.6) + 0.17, 0.6), (x, ystern(-0.6) + 0.17, -0.68)]
        mb.add(sweep(path, circle(0.022, 6), up=(0, 1, 0)), 'greyw')
    for z in (0.62, 0.25, -0.12, -0.50):
        put(mb, box(lx[1] - lx[0] + 0.03, 0.08, 0.045, 0.015, 1), 'teak', ((lx[0] + lx[1]) / 2, ystern(z) + 0.18, z))
    # outboard bracket on the transom (iron), the motor clamps to it
    put(mb, box(0.40, 0.12, 0.42, 0.03, 1), 'iron', (0, ystern(0.55) + 0.06, 0.52))
    put(mb, box(0.34, 0.26, 0.06, 0.02, 1), 'iron', (0, ystern(0.55) + 0.18, 0.66))
    return out, holders, (sum(lx) / 2, ystern(zt), zt), ((sum(lx) / 2), ystern(-0.6) + 0.17, -0.6)


def build_bow(mb):
    # bow roller + anchor on the stem head, a cleat
    yb = ybow(zs(0)); zt = zs(0) + 0.04
    put(mb, box(0.16, 0.42, 0.06, 0.02, 1), 'greyw', (0, yb + 0.12, zt + 0.02))
    put(mb, cyl(0.045, 0.12, 8, bevel=0.012), 'ink', (0, yb - 0.04, zt + 0.06), (0, D(90), 0))
    # a classic cartoon anchor hanging off the roller in front of the stem: ring, stock, shank, curved arms, flukes
    ax, ay, az = 0.0, yb - 0.11, zt + 0.02
    mb.add(torus((ax, ay, az), 0.05, 0.018, 'X', 8, 5), 'iron')
    sh = 0.62
    put(mb, box(0.055, 0.055, sh, 0.018, 1), 'iron', (ax, ay, az - 0.05 - sh / 2))
    put(mb, cyl(0.022, 0.40, 6, bevel=0.008), 'iron', (ax, ay, az - 0.14), (0, D(90), 0))
    for e in (-1, 1): put(mb, blob(0, (0.035, 0.035, 0.035), 40 + e, 0.02), 'iron', (ax + e * 0.21, ay, az - 0.14))
    zb = az - 0.05 - sh
    arm = [Vector((ax + 0.22 * math.sin(a), ay, zb + 0.22 * (1 - math.cos(a)))) for a in [D(-70 + 28 * q) for q in range(6)]]
    mb.add(sweep(arm, circle(0.03, 6), up=(0, 1, 0)), 'iron')
    for e in (arm[0], arm[-1]):
        d = 1 if e.x > 0 else -1
        put(mb, box(0.13, 0.04, 0.09, 0.015, 1), 'iron', (e.x + d * 0.01, ay, e.z + 0.05), (0, D(-d * 35), 0))
    # rope from the ring back over the roller into the locker
    mb.add(sweep([(ax, ay + 0.02, az + 0.05), (ax, yb + 0.02, zt + 0.12), (ax, yb + 0.25, zt + 0.06), (ax + 0.03, yb + 0.42, zt - 0.03)], circle(0.02, 6)), 'rope')
    # bow cleat on the stem head, aft of the roller
    t = 0.07; cy0 = yst(t, zs(t)) + 0.02
    put(mb, box(0.07, 0.07, 0.05, 0.015, 1), 'greyw', (0, cy0 + 0.05, zs(t) + 0.06))
    put(mb, box(0.07, 0.26, 0.045, 0.015, 1), 'greyw', (0, cy0 + 0.05, zs(t) + 0.095))
    # fenders hanging over each side amidships (blue), tied to the gunwale
    for sd in (1, -1):
        y = 0.9; t = clamp((y - ybow(zs(0.5))) / (ystern(zs(0.5)) - ybow(zs(0.5))))
        x = sd * (Bs(t) + 0.12); z = zs(t)
        mb.add(sweep([(sd * (Bs(t) - 0.04), y, z + 0.04), (x, y, z), (x, y, z - 0.30)], circle(0.015, 5), up=(0, 1, 0)), 'rope')
        put(mb, cyl(0.10, 0.36, 8, bevel=0.045, seg=1), 'blue', (x, y, z - 0.50))
    return (0.0, yb + 0.05, zt + 0.06)


def build_motor(coll, at):
    """A chunky white outboard (clean, cared for). Pivot = the bracket mount; steers about its local Z (up)."""
    mb = MB()
    put(mb, box(0.24, 0.20, 0.10, 0.03, 1), 'iron', (0, 0.0, 0.0))
    put(mb, box(0.20, 0.06, 0.20, 0.02, 1), 'iron', (0, -0.08, -0.10))
    put(mb, box(0.20, 0.18, 0.36, 0.035, 1), 'iron', (0, 0.12, -0.06))
    # power head: white cowling, blue band, ink lower cowl
    put(mb, box(0.40, 0.56, 0.16, 0.05, 2), 'ink', (0, 0.26, 0.16))
    put(mb, box(0.46, 0.64, 0.50, 0.14, 3), 'paper', (0, 0.27, 0.48))
    put(mb, box(0.475, 0.655, 0.08, 0.04, 2), 'blue', (0, 0.27, 0.33))
    put(mb, box(0.24, 0.12, 0.05, 0.02, 1), 'ink', (0, 0.06, 0.70))
    # leg, cavitation plate, gearcase, skeg
    LEG = 1.34
    put(mb, box(0.14, 0.24, LEG + 0.10, 0.05, 2), 'paper', (0, 0.28, -LEG / 2 + 0.08), (D(-2), 0, 0))
    put(mb, box(0.32, 0.42, 0.045, 0.018, 1), 'ink', (0, 0.32, -LEG + 0.26))
    rad = [0.04, 0.08, 0.09, 0.085, 0.055]
    tp = [Vector((0, y, -LEG)) for y in (0.06, 0.12, 0.27, 0.40, 0.47)]
    mb.add(sweep(tp, [circle(r, 8) for r in rad], side=(1, 0, 0)), 'ink')
    put(mb, box(0.05, 0.20, 0.18, 0.02, 1), 'ink', (0, 0.36, -LEG - 0.11))
    ob = finish(mb, 'Motor', coll); ob.location = at
    pm = MB()
    pm.add(sweep([(0, -0.06, 0), (0, 0.03, 0), (0, 0.08, 0), (0, 0.11, 0)], [circle(r, 8) for r in (0.05, 0.055, 0.045, 0.02)], side=(1, 0, 0)), 'iron')
    for k in range(3):
        pm.add(box(0.13, 0.045, 0.15, 0.02, 1), 'iron', Matrix.Rotation(D(120 * k), 4, 'Y') @ Matrix.Translation((0, 0, 0.115)) @ Matrix.Rotation(D(32), 4, 'Z'))
    pr = finish(pm, 'Prop', coll); pr.parent = ob; pr.location = (0, 0.52, -LEG)
    return ob


def build_boat():
    L.R = random.Random(17)
    coll = new_scene('Kestrel')
    mb = MB(); nt = lambda: sum(len(f.verts) - 2 for f in mb.bm.faces)
    build_hull(mb); a = nt(); poly = build_deck(mb); b = nt(); bunk = build_cabin(mb); c = nt()
    build_console(mb); d = nt(); seats, holders, ltop, lbot = build_aft(mb); e = nt(); anchor = build_bow(mb); f = nt()
    print('TRIS hull %d deck %d cabin %d console %d aft %d bow %d' % (a, b - a, c - b, d - c, e - d, f - e))
    hull = finish(mb, 'Hull', coll)
    wheel = build_wheel(coll); build_throttle(coll)
    motor = build_motor(coll, (0, ystern(0.55) + 0.24, 0.66))
    # ---- colliders (boat local space) ----
    cuts = [-3.20, -2.75, -2.30, -1.60, -0.60, 1.00, 2.40, TR_IN + 0.06]
    xd = lambda ya, yb: min(inner_x(y, DECK) for y in frange(ya, yb, (yb - ya) / 6))
    col_box('Deck_colDeck', (-xd(-0.60, TR_IN), -0.60, DECK - 0.12), (xd(-0.60, TR_IN), TR_IN, DECK), coll)
    for k, (ya, yb) in enumerate(zip(cuts[:4], cuts[1:5])):
        col_box('DeckF%d_colDeck' % k, (-xd(ya, yb), ya, DECK - 0.12), (xd(ya, yb), yb, DECK), coll)
    for sd, nm in ((1, 'L'), (-1, 'R')):
        for k, (ya, yb) in enumerate(zip(cuts, cuts[1:])):
            ys_ = frange(ya, yb, (yb - ya) / 6)
            xin = min(inner_x(y, z) for y in ys_ for z in (DECK + 0.05, DECK + 0.4, 1.0) if inner_x(y, z) > 0.05)
            ts = [clamp((y - ybow(1.0)) / (ystern(1.0) - ybow(1.0))) for y in ys_]
            zt = max(zs(t) for t in ts) + 0.045
            lo, hi = (xin, ya, DECK), (1.45, yb, zt)
            if sd < 0: lo, hi = (-1.45, ya, DECK), (-xin, yb, zt)
            col_box('Side%s%d_colBox' % (nm, k), lo, hi, coll)
    col_box('Bow_colBox', (-0.55, ybow(zs(0)) - 0.05, DECK), (0.55, cuts[0], zs(0) + 0.05), coll)
    col_box('Stern_colBox', (-1.35, TR_IN, DECK), (1.35, ystern(zs(1)) + 0.06, zs(1) + 0.05), coll)
    col_box('Cabin_colBox', (-CW, CF, DECK), (CW, CA + 0.01, CROOF + 0.08), coll)
    col_box('CabinNose_colBox', (-CW * 0.9, CF - 0.2, DECK), (CW * 0.9, CF, CROOF + 0.08), coll)
    col_box('CabinTip_colBox', (-CW * 0.6, CN - 0.03, DECK), (CW * 0.6, CF - 0.2, CROOF + 0.08), coll)
    col_box('Console_colBox', (-KW - 0.06, KY0, DECK), (KW + 0.06, KY1 + 0.04, KH + 0.48), coll)
    col_box('ConsoleSeat_colBox', (-KW, SEATY[0] - 0.02, DECK), (KW, KY0, DECK + 0.43), coll)
    for k, s in enumerate(seats):
        sd = 1 if s[0] > 0 else -1; x0, x1, y0, y1 = s[3], s[4], s[5], s[6]
        lo, hi = ((x0, y0, DECK), (x1, y1, DECK + 0.43)) if sd > 0 else ((-x1, y0, DECK), (-x0, y1, DECK + 0.43))
        col_box('Seat%s_colBox' % ('L' if sd > 0 else 'R'), lo, hi, coll)
    col_box('HullBottom_colBox', (-1.30, ybow(0.2) + 0.1, -0.42), (1.30, ystern(0) + 0.02, DECK - 0.12), coll)
    # ---- sockets ----
    socks = {}
    def S(n, p, parent=None): socks[n] = tuple(round(v, 3) for v in p); return empty(n, p, coll, parent)
    S('socket_helm', (0.0, KY1 + 0.55, DECK))
    S('socket_wheel', tuple(wheel.location))
    S('socket_ladder_top', ltop); S('socket_ladder_bottom', lbot)
    S('socket_anchor', anchor)
    S('socket_bunk', bunk)
    S('socket_cabin_door', (0.0, CA, DECK))
    S('cam_boat', (0.0, 0.6, DECK + 1.2))
    yr = 2.25; S('rail_L', (inner_x(yr, DECK + 0.4) - 0.42, yr, DECK)); S('rail_R', (-(inner_x(yr, DECK + 0.4) - 0.42), yr, DECK))
    ybw = CN - 0.37; S('rail_bow', (0.0, ybw, DECK)); S('rail_stern', (0.0, TR_IN - 0.40, DECK))
    for k, p in enumerate(((0.0, -2.75), (0.88, -1.0), (-0.88, -1.0), (1.02, 2.4), (-1.02, 2.4), (0.0, 3.35))):
        S('buoy_%d' % k, (p[0], p[1], -0.15))
    for k, p in enumerate(((0.24, (SEATY[0] + SEATY[1]) / 2, DECK + 0.43), (-0.24, (SEATY[0] + SEATY[1]) / 2, DECK + 0.43), seats[0][:3], seats[1][:3])):
        S('socket_seat_%d' % k, p)
    for k, p in enumerate(holders): S('socket_rodholder_%d' % k, p)
    # socket_bunk keeps the identity rotation: its forward (Unity +Z = Blender -Y) points along the bunk to the pillow
    # ---- checks ----
    bpy.context.view_layer.update()
    bb = [hull.matrix_world @ Vector(c) for c in hull.bound_box]
    print('HULL BOUNDS', [round(min(v[k] for v in bb), 3) for k in range(3)], [round(max(v[k] for v in bb), 3) for k in range(3)])
    print('INNER half-width at deck+0.4:', [(round(y, 2), round(inner_x(y, DECK + 0.4), 2)) for y in frange(-3.2, 3.4, 0.4)])
    print('WALKWAY beside the cabin (inner - CW), min over y %.2f..%.2f: %.3f (capsule 0.6 needs >= 0.66)' % (CF, CA, min(inner_x(y, z) for y in frange(CF, CA, 0.05) for z in (DECK + 0.05, DECK + 0.4, 1.0)) - CW))
    print('BOW WELL: nose %.2f -> stem inner %.2f, half-width at rail_bow %.2f' % (CN, ybow(DECK), inner_x(ybw, DECK + 0.4)))
    print('SOCKETS', socks)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'boat_kestrel.blend'))
    export(coll, 'PB_Boat_Kestrel_T2.fbx')
    if RENDER: renders()


def renders():
    cam = setup_render(80)
    shoot(cam, 'kestrel_34front.png', (-6.8, -9.0, 4.6), (0, -0.3, 0.6), 40)
    shoot(cam, 'kestrel_34rear.png', (6.4, 9.6, 4.8), (0, 0.6, 0.6), 40)
    shoot(cam, 'kestrel_side.png', (-13.5, -0.1, 1.4), (0, 0.0, 0.5), 45)
    shoot(cam, 'kestrel_top.png', (0.01, 0.0, 14.0), (0, 0.0, 0), 40)
    shoot(cam, 'kestrel_game.png', (3.5, 10.5, 6.2), (0, 0.6, 1.5), 40)
    shoot(cam, 'kestrel_cabin.png', (0.0, -0.22, 1.30), (0, -2.1, 0.74), 16)
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = 3.2; cam.data.clip_start = 1.45   # doll's-house plan: the clip cuts the roof off
    shoot(cam, 'kestrel_cabin_cutaway.png', (0.0, -1.2, 3.0), (0, -1.199, 0.0))
    cam.data.type = 'PERSP'; cam.data.clip_start = 0.1
    shoot(cam, 'kestrel_helm.png', (1.6, 3.3, 2.3), (0, 0.9, 1.0), 32)
    # side by side with Biscuit (palette / scale check)
    bfbx = os.path.join(L.ART, 'PB_Boat_Biscuit_T1.fbx')
    if os.path.exists(bfbx):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=bfbx)
        for o in set(bpy.data.objects) - before:
            if '_col' in o.name: o.hide_render = True
            if o.type == 'MESH': o.data.materials.clear(); o.data.materials.append(L.toon_mat())
            if o.parent is None: o.location = Vector(o.location) + Vector((3.4, 1.0, 0))
        shoot(cam, 'kestrel_vs_biscuit.png', (-6.0, -11.5, 4.2), (1.6, 0.2, 0.5), 40)


build_boat()
