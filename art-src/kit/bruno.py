# Bruno (cast.md §1): stocky, the fridge. Rolled charcoal beanie, thick black beard, heavy brow, small eyes, big round
# nose; lime hi-vis vest (silver stripes, open at the front) over a faded navy tee, bare forearms, tan leather gloves, grey
# cargo trousers with knee pads, brown steel-toe boots. Modelled in Pip space, built at 1.61 m (base 1.75 x root 0.92).
from kitlib import *

HEIGHT = 1.61
ARM_DOWN = -30            # the Humanoid's lowest upper arm: a wide chest
HAT = ((0, -0.005, 1.33), (0, -0.005, 1.46))
FACE_Z = 1.27
PALETTE = dict(skin='#B98A62', skin_dk='#A47552', nose='#C47B5E', beard='#2A2422', iris='#3B2A20',
               tee='#3E4A66', tee_dk='#343E57', vest='#C9E23A', silver='#C9CDD2', trouser='#5B5F66', trouser_dk='#4A4E55',
               belt='#3A3B40', glove='#B58E5E', glove_dk='#9C774C', boot='#6B4A33', sole='#3A2A20', toe='#7C5940',
               beanie='#4A4A50', beanie_dk='#3C3C42')
# clothes channels by part (kitlib.CH): 1 Hat, 2 Top, 3 Over, 4 Legs, 5 Boots, 6 Gloves; the belt stays 0
CLOTHES = dict(Beanie=1, Torso=dict(tee=2, trouser=4), Shoulder=2, Arm=dict(tee=2, tee_dk=2), Vest=3, Leg=4, KneePad=4, Cargo=4,
               Boot=5, Glove=6)

HEAD_C = Vector((0, -0.006, 1.275)); HEAD_R = Vector((0.158, 0.15, 0.162))
LEG_X = 0.13
ARM_Y, ARM_Z = 0.042, 1.086


# ---------------------------------------------------------------- torso + vest
def torso_key(z):
    # z: half-width, depth to the front, depth to the back, centre y: a fridge (boxy, flat front, broad all the way down)
    keys = [(0.468, (0.07, 0.06, 0.06, 0.0)), (0.49, (0.19, 0.15, 0.13, 0.0)), (0.54, (0.245, 0.19, 0.16, -0.004)),
            (0.62, (0.258, 0.205, 0.172, -0.01)), (0.72, (0.262, 0.212, 0.175, -0.014)), (0.82, (0.264, 0.205, 0.178, -0.012)),
            (0.92, (0.25, 0.198, 0.18, -0.006)), (1.0, (0.232, 0.19, 0.176, 0.0)), (1.05, (0.218, 0.175, 0.165, 0.006)),
            (1.095, (0.19, 0.14, 0.13, 0.012)), (1.13, (0.1, 0.085, 0.08, 0.018)), (1.145, (0.05, 0.05, 0.05, 0.02))]
    return spline(keys, z)


def torso_pt(z, a, out=0.0):
    rx, ryf, ryb, yc = torso_key(z); ca, sa = math.cos(a), math.sin(a)
    p = 2.6; x = math.copysign(abs(ca) ** (2 / p), ca); y = math.copysign(abs(sa) ** (2 / p), sa)
    v = Vector(((rx + out) * x, yc + y * ((ryb if sa > 0 else ryf) + out), z))
    return v


def build_torso():
    def col(c, n):
        if c.z < 0.672: return 'trouser'
        if c.z < 0.705: return 'belt'
        return 'tee'
    P = Part('Torso', col, W_torso)
    zs = [0.468, 0.476, 0.49, 0.52, 0.56, 0.62, 0.672, 0.69, 0.705, 0.76, 0.84, 0.92, 0.98, 1.03, 1.07, 1.1, 1.125, 1.14, 1.145]
    P.loft([[torso_pt(z, 2 * math.pi * j / 28) for j in range(28)] for z in zs])
    return P


def shell(P, grid, th):
    """A thin solid from a grid of outer points (rows x cols, open both ways): inner = outer pulled in by th toward the
    body's axis, the edges closed."""
    rows, cols = len(grid), len(grid[0])
    def inward(p): d = Vector((p.x, p.y - 0.0, 0)); return p - d.normalized() * th if d.length > 1e-6 else p
    O = [[P.vert(p) for p in r] for r in grid]; I = [[P.vert(inward(p)) for p in r] for r in grid]
    for i in range(rows - 1):
        for j in range(cols - 1):
            P.f.append([O[i][j], O[i][j + 1], O[i + 1][j + 1], O[i + 1][j]])
            P.f.append([I[i][j], I[i + 1][j], I[i + 1][j + 1], I[i][j + 1]])
    for i in range(rows - 1):
        for j in (0, cols - 1):
            P.f.append([O[i][j], O[i + 1][j], I[i + 1][j], I[i][j]])
    for j in range(cols - 1):
        for i in (0, rows - 1):
            P.f.append([O[i][j], I[i][j], I[i][j + 1], O[i][j + 1]])


VEST_GAP = 0.075


def vest_angle(x, rx, base):
    """The ring angle (near the front, base -pi/2, or the back, +pi/2) whose point on the superellipse has this x."""
    c = clamp(abs(x) / (rx + 0.016), 0, 0.999) ** (2.6 / 2)      # |cos a|^(2/p) = x / rx
    d = math.acos(c)                                           # angle from the side, 0..pi/2
    if base < 0: return -d if x > 0 else math.pi + d           # front: right side -d, left side pi + d (the vest runs past pi)
    return d if x > 0 else math.pi - d


def vest_colour(c, n):
    if 0.765 < c.z < 0.8 or 0.845 < c.z < 0.88: return 'silver'                  # two bands all round
    if c.z > 0.88 and abs(abs(c.x) - 0.155) < 0.02: return 'silver'                # braces up front and back
    return 'vest'


def build_vest():
    P = Part('Vest', vest_colour, W_torso)
    zs = [0.705, 0.72, 0.765, 0.8, 0.845, 0.88, 0.92, 0.96, 1.0, 1.03, 1.05]
    grid = []
    for z in zs:
        rx = torso_key(z)[0]
        g = math.asin(min(0.95, VEST_GAP / rx))
        a0, a1 = -math.pi / 2 + g, 3 * math.pi / 2 - g          # front-right edge, round the back, to the front-left edge
        # columns: even steps, plus one on each edge of the four silver braces (front and back), so they stay straight
        angles = [lerp(a0, a1, j / 22) for j in range(23)]
        for bx in (0.135, 0.175):
            for sgn in (1, -1):
                for base in (-math.pi / 2, math.pi / 2):     # front, back
                    a = vest_angle(sgn * bx, rx, base)
                    if a0 < a < a1: angles.append(a)
        grid.append([torso_pt(z, a, 0.016) for a in sorted(angles)])
    shell(P, grid, 0.012)
    # over the shoulders: a strap each side from the front panel to the back
    for sx in (1, -1):
        x = sx * 0.155
        pts = [(x, -0.19, 1.03), (x, -0.15, 1.1), (x, -0.06, 1.13), (x, 0.04, 1.135), (x, 0.12, 1.11), (x, 0.17, 1.04)]
        tube(P, pts, [0.042] * len(pts), n=10, flat=0.3)
    return P


# ---------------------------------------------------------------- arms
def build_shoulder(sx):
    P = Part('Shoulder', 'tee', W_arm)
    ball(P, (sx * 0.16, ARM_Y + 0.004, ARM_Z - 0.004), 0.094, 14, 9)
    return P


def build_arm(sx):
    def col(c, n):
        x = abs(c.x)
        if x < 0.292: return 'tee'
        if x < 0.31: return 'tee_dk'                        # the rolled sleeve edge
        return 'skin'
    P = Part('Arm', col, W_arm)
    xs = [0.13, 0.2, 0.26, 0.292, 0.3, 0.31, 0.318, 0.36, 0.41, 0.46, 0.5, 0.52]
    rs = [0.09, 0.09, 0.088, 0.087, 0.092, 0.089, 0.07, 0.073, 0.07, 0.066, 0.062, 0.06]
    tube(P, [(sx * x, ARM_Y, ARM_Z) for x in xs], rs, n=14)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    tube(P, [(sx * 0.2, ARM_Y, ARM_Z), (sx * 0.245, ARM_Y, ARM_Z)], [0.096, 0.095], n=16)
    return P


def build_glove(sx):
    P = Part('Glove', lambda c, n: 'glove_dk' if 0.555 < abs(c.x) < 0.572 else 'glove', W_hand)
    y, z = ARM_Y, ARM_Z - 0.004
    tube(P, [(sx * 0.5, y, z), (sx * 0.54, y, z), (sx * 0.572, y, z)], [0.066, 0.078, 0.084], n=12)          # cuff
    ball(P, (sx * 0.64, y + 0.004, z - 0.006), 1.0, 12, 8, squash=(0.094, 0.068, 0.05))                     # the mitt
    tube(P, [(sx * 0.585, y - 0.038, z - 0.006), (sx * 0.617, y - 0.08, z - 0.004), (sx * 0.643, y - 0.096, z)],
         [0.03, 0.028, 0.023], n=8)                                                                           # thumb
    ball(P, (sx * 0.643, y - 0.096, z), 0.023, 8, 5)
    return P


# ---------------------------------------------------------------- legs + boots
def build_leg(sx):
    P = Part('Leg', 'trouser', W_leg)
    x = sx * LEG_X
    tube(P, [(x * 0.92, 0.0, 0.62), (x, -0.002, 0.5), (x, 0.0, 0.4), (x, 0.006, 0.3), (x, 0.01, 0.215), (x, 0.01, 0.2)],
         [0.118, 0.117, 0.112, 0.112, 0.116, 0.1], n=16)
    # knee pad (a rounded block on the front of the knee) and a cargo pocket on the outer thigh
    k = Part('KneePad', 'trouser_dk', W_leg); k.closed = True
    ball(k, (x, -0.103, 0.415), 1.0, 12, 7, squash=(0.078, 0.032, 0.07))
    pk = Part('Cargo', 'trouser_dk', W_leg); pk.closed = True
    ball(pk, (sx * (LEG_X + 0.105), 0.0, 0.53), 1.0, 10, 7, squash=(0.03, 0.078, 0.074))
    return [P, k, pk]


def build_boot(sx):
    cx = sx * LEG_X
    def col(c, n):
        if c.z < 0.026: return 'sole'
        if c.y < -0.1 and c.z < 0.11: return 'toe'          # the steel toe cap
        return 'boot'
    P = Part('Boot', col, W_boot)
    keys = [(0.09, (0.055, 0.1)), (0.075, (0.07, 0.12)), (0.0, (0.08, 0.13)), (-0.08, (0.08, 0.115)),
            (-0.15, (0.074, 0.098)), (-0.19, (0.054, 0.07)), (-0.205, (0.024, 0.04))]
    rings = []
    for k in range(11):
        y = lerp(0.09, -0.205, k / 10)
        rx, top = spline([(-t, v) for t, v in keys], -y)
        rings.append(ring(Vector((cx, y, top / 2)), Vector((1, 0, 0)), Vector((0, 0, 1)), rx, top / 2, 14, p=2.8))
    P.loft(rings)
    tube(P, [(cx, 0.02, 0.08), (cx, 0.02, 0.2), (cx, 0.02, 0.25)], [0.084, 0.086, 0.088], n=14)
    return P


# ---------------------------------------------------------------- head
def head_point(th, ph):
    """A broad head: wide cheeks and a square jaw (the beard sits on it), flat on top under the beanie."""
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)
    wide = 1 + 0.12 * math.sin(low * math.pi) + 0.05 * low
    x = HEAD_R.x * cp * cx * wide
    y = HEAD_R.y * cp * sx * (1 + 0.05 * low)
    z = HEAD_R.z * sp * (0.92 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def around(c):
    """0 at the front of the face, pi at the back."""
    return abs(math.atan2(c.x, -(c.y - HEAD_C.y)))


def beard_line(a):
    """How high the beard comes, by the angle round the head: low across the cheeks, up the sideburns to the beanie."""
    if a < 0.55: return 1.226 + 0.03 * (a / 0.55) ** 2
    if a < 1.75: return 1.256 + 0.09 * sstep(0.55, 1.4, a)
    return 1.12                                              # (none at the back)


def build_head():
    def col(c, n):
        a = around(c)
        return 'beard' if a < 1.75 and c.z < beard_line(a) - 0.001 else 'skin'
    P = Part('Head', col, W_head)
    rings, HK, NR = [], 8, 18
    for k in range(1, NR):
        pts = []
        for j in range(32):
            th = 2 * math.pi * j / 32 - math.pi / 2
            a = abs(math.atan2(math.cos(th), -math.sin(th)))
            zb = beard_line(min(a, 1.749))
            sh = clamp((zb - HEAD_C.z) / (HEAD_R.z * (0.92 if zb > HEAD_C.z else 1.0)), -0.99, 0.99)
            phb = math.asin(sh)
            ph = lerp(-math.pi / 2, phb, k / HK) if k <= HK else lerp(phb, math.pi / 2, (k - HK) / (NR - HK))
            pts.append(head_point(th, ph))
        rings.append(pts)
    ids = P.loft(rings, False, False)
    b, t = P.vert(head_point(0, -math.pi / 2)), P.vert(head_point(0, math.pi / 2)); n = len(rings[0])
    for j in range(n):
        P.f.append([ids[0][(j + 1) % n], ids[0][j], b]); P.f.append([ids[-1][j], ids[-1][(j + 1) % n], t])
    return P


def build_beard_bulk():
    """The beard's volume under the chin and along the jaw, so it reads as a beard and not paint."""
    P = Part('BeardBulk', 'beard', W_head); P.closed = True
    ball(P, (0, HEAD_C.y - 0.085, 1.155), 1.0, 14, 8, squash=(0.12, 0.075, 0.075))
    for sx in (1, -1):
        ball(P, (sx * 0.1, HEAD_C.y - 0.035, 1.17), 1.0, 10, 7, squash=(0.07, 0.07, 0.07))
    return P


def build_ears():
    P = Part('Ears', 'skin_dk', W_head); P.closed = True
    for sx in (1, -1): ball(P, (sx * 0.168, HEAD_C.y + 0.015, 1.27), 1.0, 10, 6, squash=(0.018, 0.027, 0.036))
    return P


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, 0.02, 1.08), (0, 0.016, 1.19)], [0.085, 0.082], n=12)
    return P


def build_beanie():
    """A rolled charcoal beanie pulled down to the brow (lower at the back), a fat rolled cuff, ribbed by colour."""
    rib = lambda c, n: 'beanie_dk' if (math.atan2(c.y - HEAD_C.y, c.x) * 18 / math.pi) % 1.0 < 0.5 else 'beanie'
    P = Part('Beanie', rib, W_hat)
    def low(sa): return 1.338 - 0.045 * clamp(sa)          # the cuff's bottom edge: brow height at the front, lower behind
    rings = []
    N = 28
    def at(j, z, out):
        th = 2 * math.pi * j / N; sh = clamp((z - HEAD_C.z) / (HEAD_R.z * 0.92), -0.99, 0.99)
        p = head_point(th, math.asin(sh)); d = p - HEAD_C
        return HEAD_C + d * out
    for dz, out in [(0.0, 1.07), (0.018, 1.13), (0.042, 1.14), (0.062, 1.11), (0.07, 1.07)]:   # the cuff, rolled: a fat bulge
        rings.append([at(j, low(math.sin(2 * math.pi * j / N)) + dz, out) for j in range(N)])
    top = HEAD_C.z + HEAD_R.z * 0.92
    # the crown: loose over the head and standing up a little above it (a soft knit, not a skull cap)
    for t, out, up in [(0.2, 1.08, 0.0), (0.45, 1.1, 0.008), (0.68, 1.11, 0.02), (0.85, 1.1, 0.034), (0.95, 1.08, 0.042)]:
        rings.append([at(j, lerp(low(math.sin(2 * math.pi * j / N)) + 0.07, top, t), out) + Vector((0, 0, up)) for j in range(N)])
    P.loft(rings, True, True)
    return P


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, HEAD_C)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.056, 1.298
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.022, 0.025, 14)], 0.002)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.002, ez - 0.003, 0.014, 0.016, 12)], 0.0035)
        D('Glint', 'white', [ellipse(ex + sx * 0.003 - 0.003, ez + 0.004, 0.005, 0.005, 8)], 0.005)
        Pb = Part('Brow', 'beard', W_head, HEAD_C)                       # heavy, low, angled in: grumpy
        stroke(Pb, S, [(sx * 0.098, 1.322), (sx * 0.07, 1.334), (sx * 0.04, 1.333), (sx * 0.02, 1.322)], 0.026, 0.004)
        parts.append(Pb)
    # a small mouth in the beard
    D('Mouth', 'mouth', [ellipse(0, 1.2, 0.03, 0.009, 14)], 0.0075)
    # the big round nose
    Pn = Part('Nose', 'nose', W_head); Pn.closed = True
    tip = S.at(0, 1.262, 0.014); ball(Pn, tip, 1.0, 12, 8, squash=(0.042, 0.036, 0.036)); parts.append(Pn)
    return parts


def build_back(vest_ob):
    Pb = Part('BackPatch', 'player', W_torso, (0, 0, 0.9))
    decal(Pb, Surface(vest_ob, back=True), ellipse(0, 0.93, 0.085, 0.085, 20), 0.0025, 3)
    return [Pb]


def build():
    closed = [build_torso(), build_vest(), build_head(), build_beard_bulk(), build_ears(), build_neck(), build_beanie()]
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_glove(sx), build_boot(sx)] + build_leg(sx)
    closed.append(build_armband(1))          # the player colour on the LEFT upper arm (cast.md)
    return closed, lambda obs: build_face(obs['Head'][0]) + build_back(obs['Vest'][0])
