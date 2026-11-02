# Pip (cast.md §3): short-round, the egg. Yellow sou'wester over two curly hair puffs, a red-and-white striped top under
# yellow bib overalls, orange rubber mitts, navy boots. Everything in Pip space (her own).
from kitlib import *

HEIGHT = 1.47
ARM_DOWN = -25            # the Humanoid's lowest upper arm (default -60): her belly is round
HAT = ((0, -0.004, 1.372), (0, -0.004, 1.53))
PALETTE = dict(skin='#A86E4B', skin_dk='#93603F', cheek='#D9796A', hair='#2B2220', iris='#6B3F26', yellow='#E9A634',
               yellow_dk='#C98A2C', red='#D9483B', glove='#F0883A', glove_dk='#D8702A', boot='#2E3F66', sole='#1F2638')

# ---------------------------------------------------------------- Pip ----------------------------------------------------------
HEAD_C = Vector((0, -0.005, 1.29)); HEAD_R = Vector((0.168, 0.158, 0.172))
STRIPE = 0.052


def stripes_z(z): return 'red' if (z / STRIPE) % 1.0 < 0.5 else 'white'
def stripes_x(x): return 'red' if ((abs(x) - 0.02) / STRIPE) % 1.0 < 0.5 else 'white'


def torso_colour(c, n):
    ax = abs(c.x); front = c.y < 0
    if ax < 0.12 and front and c.z < 0.995: return 'yellow'                     # the bib
    if ax < 0.15 and not front and c.z < 0.93: return 'yellow'                  # the back bib
    if c.z < 0.82: return 'yellow'                                              # the overalls all round
    if abs(ax - 0.105) < 0.022 and c.z > 0.9: return 'yellow'                   # straps over the shoulders
    return stripes_z(c.z)


def build_torso():
    # egg: z, half-width, depth to the front, depth to the back, centre y. Wide all the way down to a low crotch.
    keys = [(0.468, (0.07, 0.06, 0.06, 0.0)), (0.49, (0.175, 0.145, 0.13, 0.0)), (0.53, (0.238, 0.2, 0.17, -0.005)),
            (0.6, (0.264, 0.236, 0.186, -0.012)), (0.7, (0.273, 0.25, 0.192, -0.018)), (0.8, (0.266, 0.24, 0.187, -0.014)),
            (0.9, (0.248, 0.212, 0.176, -0.006)), (0.98, (0.222, 0.182, 0.163, 0.003)), (1.04, (0.19, 0.155, 0.147, 0.009)),
            (1.092, (0.14, 0.118, 0.112, 0.014)), (1.13, (0.08, 0.07, 0.07, 0.018)), (1.144, (0.04, 0.04, 0.04, 0.018))]
    P = Part('Torso', torso_colour, W_torso)
    # rings on the stripe edges (every half stripe), so each stripe is a clean band of faces; a few more at the ends
    zs = sorted(set([round(k * STRIPE / 2, 4) for k in range(18, 45)] + [0.474, 0.481, 1.132, 1.139]))
    rings = []
    N = 28
    for z in zs:
        rx, ryf, ryb, yc = spline(keys, z)
        pts = []
        for j in range(N):
            a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * ca, yc + sa * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_arm(sx):
    P = Part('Arm', lambda c, n: stripes_x(c.x), W_arm)
    y, z = 0.0418, 1.0858
    keys = [(0.12, (0.072,)), (0.2, (0.072,)), (0.3, (0.066,)), (0.42, (0.062,)), (0.54, (0.058,))]
    xs = [0.02 + k * STRIPE / 2 for k in range(4, 21)]          # rings on the stripe edges
    tube(P, [(sx * x, y, z) for x in xs], [spline(keys, x)[0] for x in xs], n=12)
    return P


def build_shoulder(sx):
    """The round shoulder: a ball lofted along the arm, its rings on the sleeve's stripe edges so the stripes run on."""
    P = Part('Shoulder', lambda c, n: stripes_x(c.x), W_arm)
    c, r = 0.15, 0.084
    xs = [c - r + 0.004] + [0.02 + k * STRIPE / 2 for k in range(2, 9) if abs(0.02 + k * STRIPE / 2 - c) < r - 0.004] + [c + r - 0.004]
    rings = [ring(Vector((sx * x, 0.045, 1.082)), Vector((0, 1, 0)), Vector((0, 0, 1)), math.sqrt(max(r * r - (x - c) ** 2, 1e-6)),
                  math.sqrt(max(r * r - (x - c) ** 2, 1e-6)), 12) for x in xs]
    P.loft(rings)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    y, z = 0.0418, 1.0858
    tube(P, [(sx * 0.212, y, z), (sx * 0.258, y, z)], [0.076, 0.075], n=14)
    return P


def build_glove(sx):
    P = Part('Glove', lambda c, n: 'glove_dk' if abs(c.x) < 0.57 and abs(c.x) > 0.555 else 'glove', W_hand)
    y, z = 0.0418, 1.082
    tube(P, [(sx * 0.505, y, z), (sx * 0.545, y, z), (sx * 0.572, y, z)], [0.064, 0.074, 0.08], n=12)        # flared cuff
    ball(P, (sx * 0.635, y + 0.004, z - 0.006), 1.0, 12, 8, squash=(0.088, 0.062, 0.045))                   # the mitt
    tube(P, [(sx * 0.585, y - 0.035, z - 0.006), (sx * 0.615, y - 0.075, z - 0.004), (sx * 0.64, y - 0.09, z)],
         [0.027, 0.025, 0.021], n=8)                                                                           # thumb
    ball(P, (sx * 0.64, y - 0.09, z), 0.021, 8, 5)
    return P


LEG_X = 0.125


def build_leg(sx):
    P = Part('Leg', 'yellow', W_leg)
    tube(P, [(sx * (LEG_X - 0.01), 0.0, 0.6), (sx * LEG_X, -0.002, 0.45), (sx * LEG_X, 0.006, 0.3), (sx * LEG_X, 0.01, 0.21)],
         [0.104, 0.102, 0.1, 0.102], n=14)
    return P


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.024 else 'boot', W_boot)
    # foot: rounded-box sections along -Y (heel -> toe), flat sole on z = 0
    keys = [(0.085, (0.05, 0.095)), (0.07, (0.064, 0.115)), (0.0, (0.073, 0.125)), (-0.08, (0.073, 0.105)),
            (-0.15, (0.066, 0.085)), (-0.185, (0.046, 0.06)), (-0.198, (0.02, 0.035))]
    rings = []
    for k in range(10):
        y = lerp(0.085, -0.198, k / 9)
        rx, top = spline([(-t, v) for t, v in keys], -y)
        rings.append(ring(Vector((cx, y, top / 2)), Vector((1, 0, 0)), Vector((0, 0, 1)), rx, top / 2, 14, p=2.6))
    P.loft(rings)
    # shaft, with a rolled rim (hidden under the trouser leg)
    tube(P, [(cx, 0.018, 0.08), (cx, 0.018, 0.2), (cx, 0.018, 0.255), (cx, 0.018, 0.27)], [0.078, 0.08, 0.084, 0.076], n=14)
    return P


def head_point(th, ph):
    """Head surface: an ellipsoid, a bit wider in the cheeks and jaw, flatter on top."""
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)                                    # 0 above the middle, 1 at the chin
    wide = 1 + 0.07 * math.sin(low * math.pi) + 0.02 * low
    x = HEAD_R.x * cp * cx * wide
    y = HEAD_R.y * cp * sx * (1 + 0.04 * low)
    z = HEAD_R.z * sp * (0.94 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def hair_line(c):
    a = abs(math.atan2(c.x, -(c.y - HEAD_C.y)))         # 0 at the front, pi at the back
    if a < math.pi / 2: return 1.352 - 0.062 * (a / (math.pi / 2)) ** 1.4
    return 1.29 - 0.11 * ((a - math.pi / 2) / (math.pi / 2))


def build_head():
    """Latitude rings warped so ring HK runs exactly along the hairline: a clean hair edge instead of a staircase."""
    P = Part('Head', lambda c, n: 'hair' if c.z > hair_line(c) + 0.001 else 'skin', W_head)
    rings, HK, NR = [], 11, 20
    for k in range(1, NR):
        ring_pts = []
        for j in range(28):
            th = 2 * math.pi * j / 28 - math.pi / 2
            zh = hair_line(head_point(th, 0.0))
            sh = clamp((zh - HEAD_C.z) / (HEAD_R.z * (0.94 if zh > HEAD_C.z else 1.0)), -0.99, 0.99)
            phh = math.asin(sh)
            ph = lerp(-math.pi / 2, phh, k / HK) if k <= HK else lerp(phh, math.pi / 2, (k - HK) / (NR - HK))
            ring_pts.append(head_point(th, ph))
        rings.append(ring_pts)
    ids = P.loft(rings, False, False)
    b, t = P.vert(head_point(0, -math.pi / 2)), P.vert(head_point(0, math.pi / 2))
    n = len(rings[0])
    for j in range(n):
        P.f.append([ids[0][(j + 1) % n], ids[0][j], b]); P.f.append([ids[-1][j], ids[-1][(j + 1) % n], t])
    return P


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, 0.02, 1.07), (0, 0.016, 1.2)], [0.068, 0.065], n=12)
    return P


class Surface:
    """Project 2D shapes (x, z) onto a mesh from the front (or the back) to lay flat decals on it."""
    def __init__(self, ob, back=False):
        me = ob.data; self.bvh = BVHTree.FromPolygons([v.co.copy() for v in me.vertices], [list(p.vertices) for p in me.polygons])
        self.back = back

    def at(self, x, z, out):
        o = Vector((x, 2.0, z)) if self.back else Vector((x, -2.0, z))
        loc, nrm, _, _ = self.bvh.ray_cast(o, Vector((0, -1 if self.back else 1, 0)))
        if loc is None: return None
        if (nrm.y > 0) != self.back: nrm = -nrm
        return loc + nrm * out


def decal(P, surf, outline, out, rings=4):
    """A flat patch whose edge is `outline` (2D, star-shaped around its centroid), laid on the surface."""
    c = sum((Vector(p) for p in outline), Vector((0, 0))) / len(outline)
    loops = [[c + (Vector(p) - c) * (k / rings) for p in outline] for k in range(1, rings + 1)]
    pts = [[surf.at(q.x, q.y, out) for q in lp] for lp in loops]
    cp = surf.at(c.x, c.y, out)
    if cp is None or any(p is None for lp in pts for p in lp): return
    ids = [[P.vert(p) for p in lp] for lp in pts]; ci = P.vert(cp); n = len(outline)
    for j in range(n): P.f.append([ci, ids[0][j], ids[0][(j + 1) % n]])
    for a, b in zip(ids, ids[1:]):
        for j in range(n): P.f.append([a[j], b[j], b[(j + 1) % n], a[(j + 1) % n]])


def stroke(P, surf, pts, width, out):
    """A flat band along a 2D polyline, laid on the surface."""
    pts = [Vector(p) for p in pts]; L, R = [], []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized(); nn = Vector((-d.y, d.x)) * width / 2
        L.append(surf.at(*(p + nn), out)); R.append(surf.at(*(p - nn), out))
    if any(q is None for q in L + R): return
    li = [P.vert(q) for q in L]; ri = [P.vert(q) for q in R]
    for i in range(len(pts) - 1): P.f.append([li[i], ri[i], ri[i + 1], li[i + 1]])


def ellipse(cx, cz, rx, rz, n=16, rot=0.0):
    cr, sr = math.cos(rot), math.sin(rot)
    return [(cx + rx * math.cos(a) * cr - rz * math.sin(a) * sr, cz + rx * math.cos(a) * sr + rz * math.sin(a) * cr)
            for a in [2 * math.pi * j / n for j in range(n)]]


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, HEAD_C)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.062, 1.298
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.034, 0.041, 18)], 0.002)
        D('Iris', 'iris', [ellipse(ex - sx * 0.003, ez - 0.004, 0.024, 0.027, 16)], 0.0035)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.003, ez - 0.004, 0.014, 0.016, 12)], 0.005)
        D('Glint', 'white', [ellipse(ex + sx * 0.004 - 0.003, ez + 0.007, 0.0065, 0.0065, 8)], 0.0062)
        D('Cheek', 'cheek', [ellipse(sx * 0.105, 1.245, 0.03, 0.022, 14)], 0.0018)
        Pb = Part('Brow', 'hair', W_head, HEAD_C)
        stroke(Pb, S, [(ex - 0.032, 1.344), (ex - 0.012, 1.353), (ex + 0.012, 1.353), (ex + 0.032, 1.344)], 0.012, 0.003)
        parts.append(Pb)
    # the grin: a wide D with a smiling top edge, teeth along the top, a tongue at the bottom
    top = [(x, 1.232 + 0.006 * (x / 0.058) ** 2) for x in [-0.058 + 0.116 * k / 10 for k in range(11)]]
    bot = [(0.058 * math.cos(a), 1.232 - 0.045 * math.sin(a)) for a in [math.pi * k / 10 for k in range(1, 10)]]
    D('Mouth', 'mouth', [top[::-1] + bot[::-1]], 0.0022)
    Pt = Part('Teeth', 'white', W_head, HEAD_C)
    stroke(Pt, S, [(x, z - 0.0085) for x, z in top[1:-1]], 0.013, 0.0034); parts.append(Pt)
    D('Tongue', 'tongue', [ellipse(0, 1.197, 0.027, 0.011, 14)], 0.0032)
    # nose: a soft round bump
    Pn = Part('Nose', 'skin_dk', W_head); Pn.closed = True
    tip = S.at(0, 1.268, 0.012); ball(Pn, tip, 1.0, 12, 7, squash=(0.03, 0.026, 0.026)); parts.append(Pn)
    return parts


def build_hair_puffs():
    P = Part('Puffs', 'hair', W_head)
    golden = math.pi * (3 - math.sqrt(5))
    for sx in (1, -1):
        c = Vector((sx * 0.215, 0.045, 1.245))
        ball(P, c, 0.078, 10, 6)
        for k in range(9):
            zz = 1 - 2 * (k + 0.5) / 9; r = math.sqrt(1 - zz * zz); a = golden * k
            d = Vector((math.cos(a) * r, math.sin(a) * r, zz))
            if d.x * sx < -0.4: continue                  # none buried in the head
            ball(P, c + d * 0.066, 0.047, 8, 5)
    return P


def build_hat():
    yel = lambda c, n: 'yellow_dk' if c.z < 1.405 and c.z > 1.372 and (Vector((c.x, c.y - 0.0)).length < 0.2) else 'yellow'
    P = Part('Hat', yel, W_hat)
    cy = -0.004
    # crown: from the band up to a rounded top
    keys = [(1.372, 1.0), (1.42, 0.985), (1.47, 0.9), (1.505, 0.72), (1.522, 0.42), (1.528, 0.12)]
    rings = []
    for z, f in keys:
        rings.append(ring(Vector((0, cy, z)), Vector((1, 0, 0)), Vector((0, 1, 0)), 0.168 * f, 0.162 * f, 28))
    P.loft(rings, False, True)
    # brim: a closed rolled band all round, longer and drooping at the back (a sou'wester)
    N = 28; prof = []
    for j in range(N):
        a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)       # sa > 0 = back
        back = clamp(sa); front = clamp(-sa)
        reach = 0.088 + 0.025 * back - 0.006 * front
        drop = 0.022 + 0.04 * back * back - 0.012 * front
        ri = Vector((0.166 * ca, cy + 0.16 * sa, 1.374))
        ro = Vector((0.166 * ca * (1 + reach / 0.166), cy + (0.16 + reach) * sa, 1.374 - drop))
        th = 0.012
        prof.append([ri, ro, ro - Vector((0, 0, th)) + (ro - ri).normalized() * 0.004, ri - Vector((0, 0, th))])
    ids = [[P.vert(p) for p in pr] for pr in prof]
    for j in range(N):
        a, b = ids[j], ids[(j + 1) % N]
        for k in range(4): P.f.append([a[k], b[k], b[(k + 1) % 4], a[(k + 1) % 4]])
    ball(P, (0, cy, 1.53), 0.014, 8, 5)       # the button on top
    return P


def build_pocket(torso_ob):
    S = Surface(torso_ob)
    # buckles where the straps meet the bib
    pb = Part('Buckles', 'metal', W_torso); pb.closed = True
    for sx in (1, -1):
        c = S.at(sx * 0.105, 0.985, 0.0)
        rings = [ring(c + Vector((0, dy, 0)), Vector((1, 0, 0)), Vector((0, 0, 1)), 0.022, 0.016, 12, p=3) for dy in (0.006, -0.007)]
        pb.loft(rings)
    P = Part('Pocket', 'yellow_dk', W_torso, (0, 0, 0.8))
    decal(P, S, [(-0.078, 0.94), (0.078, 0.94), (0.078, 0.835), (-0.078, 0.835)], 0.002, 2)
    Pi = Part('PocketIn', 'yellow', W_torso, (0, 0, 0.8))
    decal(Pi, S, [(-0.068, 0.93), (0.068, 0.93), (0.068, 0.845), (-0.068, 0.845)], 0.0032, 2)
    Pb = Part('BackPatch', 'player', W_torso, (0, 0, 0.8))
    decal(Pb, Surface(torso_ob, back=True), ellipse(0, 0.79, 0.075, 0.075, 20), 0.0025, 3)
    return [pb, P, Pi, Pb]


def build():
    closed = [build_torso(), build_head(), build_neck(), build_hat(), build_hair_puffs()]
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_glove(sx), build_leg(sx), build_boot(sx)]
    closed.append(build_armband(1))          # the player colour on the LEFT upper arm (cast.md)
    return closed, lambda obs: build_face(obs['Head'][0]) + build_pocket(obs['Torso'][0])
