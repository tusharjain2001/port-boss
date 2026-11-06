# Old Gus (cast.md §5): wiry, the question mark (hunched). Brown tweed flat cap over a white fringe, bushy white brows, a
# squint, a big white walrus moustache, a red nose, big ears; a cream cable-knit sweater with elbow patches, dark green
# chest waders on braces, brown rubber boots, bare bony hands. Modelled in Pip space (the hunch is in the mesh: the upper
# back rounds and the head sits forward of the joints), built at 1.66 m (base 1.75 x root 0.95).
from kitlib import *

HEIGHT = 1.6625
ARM_DOWN = -55
HAT = ((0, -0.07, 1.345), (0, -0.07, 1.43))
FACE_Z = 1.26
PALETTE = dict(skin='#E8B9A0', skin_dk='#D69C84', nose='#E0675A', cheek='#E8907E', white_h='#F2EFEA', white_d='#D9D5CE',
               iris='#4A5A6A', knit='#E6DCC4', knit_d='#D3C6A8', patch='#7A5236', wader='#4E5A3A', wader_d='#414B30',
               buckle='#B4B8BD', cap='#7A6448', cap_d='#68543B', boot='#6E4A30', sole='#3A2618')
# clothes channels by part (kitlib.CH): 1 Hat, 2 Top, 3 Over, 4 Legs, 5 Boots, 6 Gloves; the chest waders are his Legs
CLOTHES = dict(Cap=1, Torso=dict(knit=2, knit_d=2, wader=4), Arm=2, Shoulder=2, Braces=4, Leg=4, Boot=5)

HEAD_C = Vector((0, -0.07, 1.25)); HEAD_R = Vector((0.138, 0.134, 0.15))
LEG_X = 0.095
ARM_Y, ARM_Z = 0.042, 1.086
WADER = 0.87                 # the waders come up to here (chest waders)


def hunch(z): return -0.09 * sstep(0.86, 1.14, z)             # how far forward the body leans at this height


def torso_key(z):
    keys = [(0.468, (0.055, 0.05, 0.05, 0.0)), (0.49, (0.12, 0.095, 0.09, 0.0)), (0.54, (0.15, 0.11, 0.105, -0.004)),
            (0.62, (0.152, 0.112, 0.108, -0.006)), (0.72, (0.148, 0.108, 0.106, -0.004)), (0.82, (0.152, 0.11, 0.115, 0.0)),
            (0.92, (0.168, 0.112, 0.13, 0.004)), (1.0, (0.175, 0.11, 0.138, 0.006)), (1.05, (0.168, 0.1, 0.134, 0.008)),
            (1.09, (0.135, 0.085, 0.11, 0.01)), (1.125, (0.075, 0.06, 0.07, 0.012)), (1.14, (0.04, 0.04, 0.04, 0.012))]
    rx, ryf, ryb, yc = spline(keys, z)
    return rx, ryf, ryb, yc + hunch(z)


def torso_col(c, n):
    if c.z < WADER + (0.01 if c.y < 0 else -0.015): return 'wader'
    a = (math.atan2(c.y - torso_key(c.z)[3], c.x) + 2 * math.pi) % (2 * math.pi)   # round the leaning centre
    return 'knit_d' if int(a / (2 * math.pi) * 28) % 3 == 0 else 'knit'     # cable ribs


def build_torso():
    P = Part('Torso', torso_col, W_torso)
    zs = [0.468, 0.476, 0.49, 0.52, 0.56, 0.62, 0.7, 0.78, 0.84, 0.855, 0.87, 0.885, 0.92, 0.96, 1.0, 1.04, 1.07, 1.095, 1.115, 1.13, 1.14]
    rings = []
    for z in zs:
        rx, ryf, ryb, yc = torso_key(z); pts = []
        for j in range(28):
            a = 2 * math.pi * j / 28; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * ca, yc + sa * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_braces():
    """Braces over the shoulders, from the waders' bib in front to the back, a buckle at each front end."""
    P = Part('Braces', 'wader_d', W_torso)
    B = Part('BraceBuckles', 'buckle', W_torso); B.closed = True
    for sx in (1, -1):
        x = sx * 0.085
        pts = [(x, -0.112 + hunch(0.87), 0.87), (x, -0.112 + hunch(0.98), 0.98), (x * 1.05, -0.09 + hunch(1.07), 1.07), (x * 1.1, -0.02 + hunch(1.12), 1.12),
               (x * 1.1, 0.06 + hunch(1.11), 1.11), (x * 1.05, 0.13 + hunch(1.03), 1.03), (x, 0.142 + hunch(0.94), 0.94), (x, 0.135, 0.85)]
        tube(P, pts, [0.017] * len(pts), n=6, flat=0.35)
        ball(B, (x, -0.118 + hunch(0.89), 0.89), 1.0, 8, 4, squash=(0.024, 0.008, 0.018))
    return [P, B]


def build_back(torso_ob):
    Pb = Part('BackPatch', 'player', W_torso, (0, 0, 0.9))
    decal(Pb, Surface(torso_ob, back=True), ellipse(0, 0.98, 0.07, 0.07, 20), 0.003, 3)
    return [Pb]


# ---------------------------------------------------------------- arms
def arm_col(c, n):
    x = abs(c.x)
    if x > 0.49: return 'knit_d'                                   # the ribbed cuff
    if 0.29 < x < 0.36 and c.y > ARM_Y + 0.018: return 'patch'      # the elbow patch, on the back of the elbow
    a = (math.atan2(c.z - ARM_Z, c.y - ARM_Y) + 2 * math.pi) % (2 * math.pi)
    return 'knit_d' if int(a / (2 * math.pi) * 12) % 3 == 0 else 'knit'


def build_arm(sx):
    P = Part('Arm', arm_col, W_arm)
    xs = [0.12, 0.17, 0.22, 0.27, 0.29, 0.325, 0.36, 0.4, 0.45, 0.49, 0.5, 0.53]
    rs = [0.064, 0.064, 0.06, 0.056, 0.056, 0.057, 0.055, 0.052, 0.05, 0.05, 0.056, 0.054]
    rings = []
    for x, r in zip(xs, rs):
        rings.append(ring(Vector((sx * x, ARM_Y, ARM_Z)), Vector((0, 1, 0)), Vector((0, 0, 1)), r, r, 12))
    P.loft(rings)
    return P


def build_shoulder(sx):
    P = Part('Shoulder', 'knit', W_arm)
    ball(P, (sx * 0.13, ARM_Y + 0.004 + hunch(1.05) * 0.5, ARM_Z - 0.01), 0.066, 12, 8)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    tube(P, [(sx * 0.2, ARM_Y, ARM_Z), (sx * 0.24, ARM_Y, ARM_Z)], [0.068, 0.067], n=14)
    return P


def build_hand(sx):
    """A bare, bony hand: knuckly mitt, a long thumb."""
    P = Part('Hand', 'skin', W_hand)
    y, z = ARM_Y, ARM_Z - 0.004
    tube(P, [(sx * 0.52, y, z), (sx * 0.56, y, z)], [0.038, 0.042], n=10)
    ball(P, (sx * 0.62, y + 0.002, z - 0.006), 1.0, 12, 8, squash=(0.078, 0.052, 0.032))
    for k in range(3):                                              # knuckles
        ball(P, (sx * 0.665, y - 0.03 + 0.03 * k, z + 0.012), 0.012, 6, 4)
    tube(P, [(sx * 0.57, y - 0.03, z - 0.006), (sx * 0.6, y - 0.068, z - 0.004), (sx * 0.627, y - 0.085, z)], [0.02, 0.018, 0.015], n=8)
    ball(P, (sx * 0.627, y - 0.085, z), 0.015, 8, 5)
    return P


# ---------------------------------------------------------------- legs + boots
def build_leg(sx):
    x = sx * LEG_X
    P = Part('Leg', 'wader', W_leg)
    tube(P, [(x * 0.85, 0.0, 0.62), (x, 0.0, 0.52), (x, 0.0, 0.42), (x, 0.004, 0.33), (x, 0.01, 0.28), (x, 0.012, 0.265)],
         [0.1, 0.096, 0.09, 0.092, 0.094, 0.086], n=14)
    return P


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.028 else 'boot', W_boot)
    boot_shape(P, cx, [(0.09, (0.054, 0.1)), (0.075, (0.068, 0.12)), (0.0, (0.075, 0.125)), (-0.08, (0.074, 0.11)),
                       (-0.15, (0.066, 0.09)), (-0.185, (0.046, 0.062)), (-0.198, (0.02, 0.035))],
               [(0.08, 0.084), (0.2, 0.09), (0.28, 0.098), (0.305, 0.098), (0.31, 0.09)], p=2.6)
    return P


# ---------------------------------------------------------------- head
def head_point(th, ph):
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)
    x = HEAD_R.x * cp * cx * (1 - 0.08 * low)
    y = HEAD_R.y * cp * sx
    z = HEAD_R.z * sp * (0.94 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def fringe_line(a):
    """The white fringe: from the cap down to here round the sides and back (bald on top, under the cap); none in front."""
    if a < 1.0: return 1.4
    return 1.4 - 0.155 * sstep(1.0, 1.5, a) + 0.03 * sstep(2.4, math.pi, a)


def build_head():
    def col(c, n):
        a = around_of(c, HEAD_C)
        return 'white_h' if a >= 1.0 and c.z > fringe_line(a) + 0.001 else 'skin'
    P = Part('Head', col, W_head)
    return warped_head(P, head_point, fringe_line, HK=11, NR=19)


def build_fringe():
    """The fringe's fluff: tufts sticking out over the ears and round the back, under the cap."""
    P = Part('Fringe', 'white_h', W_head); P.closed = True
    for j in range(14):
        th = math.pi * (0.25 + 0.5 * j / 13) * 2 - math.pi / 2       # the sides and back
        a = abs(math.atan2(math.cos(th), -math.sin(th)))
        if a < 1.15: continue
        c = Vector((HEAD_C.x + HEAD_R.x * 0.98 * math.cos(th), HEAD_C.y + HEAD_R.y * 0.98 * math.sin(th), 1.31))
        ball(P, c, 1.0, 8, 5, squash=(0.034, 0.034, 0.04))
    return P


def build_ears():
    P = Part('Ears', 'skin_dk', W_head); P.closed = True
    for sx in (1, -1): ball(P, (sx * 0.14, HEAD_C.y + 0.015, 1.25), 1.0, 10, 6, squash=(0.026, 0.04, 0.052))
    return P


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, -0.01, 1.08), (0, -0.06, 1.19)], [0.045, 0.042], n=12)
    return P


def build_cap():
    """A tweed flat cap: a low crown that slopes down to a short peak at the front."""
    P = Part('Cap', lambda c, n: 'cap_d' if c.z < 1.37 + 0.025 * clamp((c.y - HEAD_C.y) / 0.14, -1, 1) else 'cap', W_hat)
    cy = HEAD_C.y; N = 28; rings = []
    def rim(sa): return 1.35 + 0.025 * sa                         # sits low at the front, higher behind
    for dz, f, push in [(0.0, 1.07, 0.0), (0.03, 1.1, 0.012), (0.055, 1.07, 0.03), (0.07, 0.9, 0.04), (0.08, 0.6, 0.04), (0.085, 0.25, 0.036)]:
        pts = []
        for j in range(N):
            a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((HEAD_R.x * f * ca, cy + HEAD_R.y * f * sa - push, rim(sa) + dz - 0.03 * clamp(-sa) * dz / 0.085)))
        rings.append(pts)
    P.loft(rings, False, True)
    # the peak: a short stiff tongue at the front, tipped down
    pk = []
    for j in range(9):
        t = j / 8; a = math.pi * (1.2 + 0.6 * t)                     # the front arc
        ca, sa = math.cos(a), math.sin(a)
        ri = Vector((HEAD_R.x * 1.06 * ca, cy + HEAD_R.y * 1.06 * sa, rim(sa) + 0.004))
        ro = ri + Vector((ca * 0.03, sa * 0.06, -0.022)) * math.sin(math.pi * t) ** 0.5
        pk.append([ri, ro, ro - Vector((0, 0, 0.008)), ri - Vector((0, 0, 0.008))])
    ids = [[P.vert(p) for p in pr] for pr in pk]
    for j in range(8):
        a_, b_ = ids[j], ids[j + 1]
        for k in range(4): P.f.append([a_[k], b_[k], b_[(k + 1) % 4], a_[(k + 1) % 4]])
    return P


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, HEAD_C)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.05, 1.28
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.022, 0.012, 14)], 0.002)                  # a squint
        D('Pupil', 'ink', [ellipse(ex - sx * 0.002, ez - 0.001, 0.01, 0.01, 10)], 0.0035)
        Pl = Part('Lid', 'skin_dk', W_head, HEAD_C)
        stroke(Pl, S, [(ex - 0.026, ez + 0.006), (ex, ez + 0.014), (ex + 0.026, ez + 0.006)], 0.01, 0.0035); parts.append(Pl)
        D('Cheek', 'cheek', [ellipse(sx * 0.085, 1.235, 0.026, 0.018, 12)], 0.0018)
        # bushy white brows: a tuft that sticks out
        B = Part('Brow', 'white_h', W_head); B.closed = True
        ball(B, S.at(sx * 0.052, 1.312, 0.008), 1.0, 10, 6, squash=(0.038, 0.016, 0.018)); parts.append(B)
    # the walrus moustache: two big drooping wings over the mouth
    M = Part('Moustache', lambda c, n: 'white_d' if c.z < 1.19 else 'white_h', W_head); M.closed = True
    for sx in (1, -1):
        base = S.at(sx * 0.032, 1.215, 0.012)
        tube(M, [base - Vector((sx * 0.03, 0, -0.004)), base, base + Vector((sx * 0.035, 0.0, -0.02)), base + Vector((sx * 0.06, 0.012, -0.058))],
             [0.016, 0.028, 0.024, 0.01], n=10)
    parts.append(M)
    D('Mouth', 'mouth', [ellipse(0, 1.178, 0.022, 0.006, 12)], 0.002)
    N = Part('Nose', 'nose', W_head); N.closed = True                 # a red bulb
    ball(N, S.at(0, 1.245, 0.02), 1.0, 12, 8, squash=(0.036, 0.034, 0.032)); parts.append(N)
    return parts


def build():
    closed = [build_torso(), build_head(), build_fringe(), build_ears(), build_neck(), build_cap()] + build_braces()
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_hand(sx), build_leg(sx), build_boot(sx)]
    closed.append(build_armband(1))
    return closed, lambda obs: build_face(obs['Head'][0]) + build_back(obs['Torso'][0])
