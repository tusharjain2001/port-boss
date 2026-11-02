# Sully (cast.md §6): the diver, a gumball on legs. A big polished brass diving helmet (front porthole with his face
# inside: wide eyes, a small moustache; barred side portholes; a handle on top) on a bolted brass breastplate, a faded
# tan canvas suit, black rubber gloves, brass-and-lead boots. Modelled in Pip space, built at 1.75 m (base, root 1.0).
# The helmet is his head (no hat bone: it never wobbles off his face).
from kitlib import *

HEIGHT = 1.75
ARM_DOWN = -35
HAT = None
FACE_Z = 1.3
PALETTE = dict(skin='#E2A784', skin_dk='#CF9070', hair='#4A3428', iris='#3A4A5A', suit='#B8A47E', suit_d='#A08C66',
               brass='#C99A3A', brass_d='#A67C2A', brass_l='#E0B85A', glove='#26262A', glove_d='#1A1A1C', sole='#2A2622')

HELM_C = Vector((0, -0.01, 1.3)); HELM_R = 0.2
FACE_C = Vector((0, HELM_C.y - 0.082, HELM_C.z))     # his face's front sits 1 cm behind the porthole's opening
LEG_X = 0.125
ARM_Y, ARM_Z = 0.042, 1.086


def torso_key(z):
    keys = [(0.468, (0.07, 0.06, 0.06, 0.0)), (0.49, (0.18, 0.14, 0.13, 0.0)), (0.54, (0.235, 0.18, 0.165, -0.004)),
            (0.62, (0.255, 0.2, 0.175, -0.01)), (0.72, (0.262, 0.212, 0.178, -0.014)), (0.82, (0.262, 0.21, 0.178, -0.012)),
            (0.92, (0.255, 0.2, 0.175, -0.006)), (1.0, (0.24, 0.185, 0.168, 0.0)), (1.06, (0.21, 0.16, 0.15, 0.006)),
            (1.1, (0.16, 0.125, 0.12, 0.012)), (1.13, (0.1, 0.08, 0.08, 0.016)), (1.145, (0.05, 0.05, 0.05, 0.02))]
    return spline(keys, z)


def build_torso():
    P = Part('Torso', lambda c, n: 'suit_d' if 0.655 < c.z < 0.675 else 'suit', W_torso)   # the fold at the waist
    zs = [0.468, 0.476, 0.49, 0.52, 0.56, 0.6, 0.64, 0.655, 0.665, 0.675, 0.7, 0.76, 0.84, 0.92, 0.98, 1.03, 1.07, 1.1, 1.122, 1.138, 1.145]
    rings = []
    for z in zs:
        rx, ryf, ryb, yc = torso_key(z); pts = []
        for j in range(28):
            a = 2 * math.pi * j / 28; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * ca, yc + sa * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_corselet():
    """The bolted brass breastplate over the shoulders, round the helmet's neck ring."""
    P = Part('Corselet', lambda c, n: 'brass_d' if c.z < 0.985 else 'brass', W_torso)
    N = 28; rings = []
    for z, f in [(0.975, 1.02), (0.99, 1.05), (1.03, 1.0), (1.08, 0.84), (1.12, 0.66), (1.14, 0.6)]:
        rx, ryf, ryb, yc = torso_key(min(z, 1.0)); pts = []
        for j in range(N):
            a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * f * 1.04 * ca, yc + sa * (ryb if sa > 0 else ryf) * f * 1.06, z)))
        rings.append(pts)
    P.loft(rings, True, False)
    B = Part('Bolts', 'brass_l', W_torso); B.closed = True
    for j in range(12):
        a = 2 * math.pi * (j + 0.5) / 12; ca, sa = math.cos(a), math.sin(a)
        rx, ryf, ryb, yc = torso_key(1.0)
        c = Vector((rx * 0.98 * 1.04 * ca, yc + sa * (ryb if sa > 0 else ryf) * 0.98 * 1.06, 1.035))
        ball(B, c + (c - Vector((0, yc, c.z))).normalized() * 0.006, 0.013, 8, 5)
    # the neck ring the helmet screws onto
    R = Part('NeckRing', 'brass_d', W_head)
    tube(R, [(0, HELM_C.y, 1.12), (0, HELM_C.y, 1.15)], [0.14, 0.14], n=24)
    return [P, B, R]


# ---------------------------------------------------------------- the helmet (it is his head)
PORT_R = 0.085            # the front porthole's opening


def build_helmet():
    """The brass sphere with a real opening at the front porthole (his face is inside it, never poking out): rings
    round the forward axis from the opening's edge back to the rear pole."""
    P = Part('Helmet', lambda c, n: 'brass_l' if c.z > HELM_C.z + 0.15 and c.y < HELM_C.y else 'brass', W_head)
    R = Vector((HELM_R, HELM_R * 0.98, HELM_R * 0.96)); t0 = math.asin(PORT_R / R.x); rings = []
    for k in range(16):
        t = lerp(t0, math.pi * 0.985, k / 15)                                   # angle from straight ahead (-Y)
        rings.append([HELM_C + Vector((math.sin(t) * math.cos(a) * R.x, -math.cos(t) * R.y, math.sin(t) * math.sin(a) * R.z))
                      for a in [2 * math.pi * j / 28 for j in range(28)]])
    P.loft(rings, False, True)
    # inside, a dark lining (only seen round his face through the glass)
    L = Part('Lining', 'brass_d', W_head); L.closed = True
    ball(L, HELM_C + Vector((0, 0.035, 0)), 1.0, 16, 10, squash=(0.17, 0.17, 0.17))
    # the front porthole's thick rim, the side portholes with bars, a handle on top
    F = Part('Portholes', 'brass_d', W_head); F.closed = True
    fc = Vector((0, HELM_C.y - math.cos(math.asin(PORT_R / HELM_R)) * HELM_R * 0.98, HELM_C.z))
    hoop(F, [fc + Vector((math.cos(a) * 0.088, -0.004 * math.cos(a) ** 2, math.sin(a) * 0.088)) for a in [2 * math.pi * k / 20 for k in range(20)]], 0.016, 8)
    for sx in (1, -1):
        sc = Vector((sx * 0.192, HELM_C.y, HELM_C.z + 0.01))
        hoop(F, [sc + Vector((0, math.cos(a) * 0.05, math.sin(a) * 0.05)) for a in [2 * math.pi * k / 14 for k in range(14)]], 0.011, 6)
        ball(F, sc - Vector((sx * 0.008, 0, 0)), 1.0, 10, 6, squash=(0.008, 0.045, 0.045))          # dark glass
        for d in (-0.018, 0.018):                                                                 # the bars
            tube(F, [sc + Vector((sx * 0.006, d, -0.045)), sc + Vector((sx * 0.006, d, 0.045))], [0.005, 0.005], n=5)
            tube(F, [sc + Vector((sx * 0.006, -0.045, d)), sc + Vector((sx * 0.006, 0.045, d))], [0.005, 0.005], n=5)
    H = Part('Handle', 'brass_d', W_head); H.closed = True
    top = HELM_C + Vector((0, 0, HELM_R * 0.96))
    hoop(H, [top + Vector((math.cos(a) * 0.035, 0, 0.012 + math.sin(a) * 0.03)) for a in [math.pi * k / 10 for k in range(11)]] +
         [top + Vector((-0.035 + 0.07 * k / 4, 0, 0.012)) for k in range(4, 0, -1)], 0.008, 6)
    return [P, L, F, H]


def build_face():
    """His face inside the helmet, just behind the porthole's opening (filling it), never past it."""
    P = Part('Face', 'skin', W_head)
    ball(P, FACE_C, 0.092, 16, 10)
    return P


def build_face_decals(face_ob):
    S = Surface(face_ob); parts = []
    c = FACE_C
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, c)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.027, HELM_C.z + 0.016
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.015, 0.018, 12)], 0.0015)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.002, ez - 0.002, 0.009, 0.01, 10)], 0.003)
        D('Glint', 'white', [ellipse(ex + 0.002, ez + 0.004, 0.0035, 0.0035, 6)], 0.004)
        Pb = Part('Brow', 'hair', W_head, c)
        stroke(Pb, S, [(ex - 0.016, ez + 0.026), (ex, ez + 0.031), (ex + 0.016, ez + 0.026)], 0.008, 0.002); parts.append(Pb)
    D('Moustache', 'hair', [[(-0.03, HELM_C.z - 0.03), (-0.012, HELM_C.z - 0.022), (0, HELM_C.z - 0.026), (0.012, HELM_C.z - 0.022),
                             (0.03, HELM_C.z - 0.03), (0.016, HELM_C.z - 0.036), (-0.016, HELM_C.z - 0.036)]], 0.002, 2)
    D('Smile', 'mouth', [[(-0.018, HELM_C.z - 0.045), (0.018, HELM_C.z - 0.045), (0.0, HELM_C.z - 0.054)]], 0.002, 2)
    N = Part('Nose', 'skin_dk', W_head); N.closed = True
    ball(N, S.at(0, HELM_C.z - 0.008, 0.003), 1.0, 10, 6, squash=(0.016, 0.008, 0.015)); parts.append(N)   # flat: stays inside
    return parts


# ---------------------------------------------------------------- arms
def build_shoulder(sx):
    P = Part('Shoulder', 'suit', W_arm)
    ball(P, (sx * 0.17, ARM_Y + 0.004, ARM_Z - 0.01), 0.096, 14, 9)
    return P


def build_arm(sx):
    P = Part('Arm', 'suit', W_arm)
    xs = [0.13, 0.2, 0.27, 0.33, 0.39, 0.45, 0.48]
    rs = [0.09, 0.09, 0.086, 0.082, 0.078, 0.074, 0.07]
    tube(P, [(sx * x, ARM_Y, ARM_Z) for x in xs], rs, n=14)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    tube(P, [(sx * 0.27, ARM_Y, ARM_Z), (sx * 0.315, ARM_Y, ARM_Z)], [0.092, 0.09], n=16)   # below the breastplate
    return P


def build_glove(sx):
    P = Part('Glove', lambda c, n: 'glove_d' if abs(c.x) < 0.49 else 'glove', W_hand)
    y, z = ARM_Y, ARM_Z - 0.004
    tube(P, [(sx * 0.46, y, z), (sx * 0.5, y, z), (sx * 0.56, y, z)], [0.08, 0.074, 0.066], n=14)
    ball(P, (sx * 0.635, y + 0.004, z - 0.006), 1.0, 12, 8, squash=(0.096, 0.07, 0.052))
    tube(P, [(sx * 0.585, y - 0.04, z - 0.006), (sx * 0.617, y - 0.082, z - 0.004), (sx * 0.643, y - 0.098, z)], [0.031, 0.029, 0.024], n=8)
    ball(P, (sx * 0.643, y - 0.098, z), 0.024, 8, 5)
    return P


# ---------------------------------------------------------------- legs + boots
def build_leg(sx):
    x = sx * LEG_X
    P = Part('Leg', 'suit', W_leg)
    tube(P, [(x * 0.92, 0.0, 0.62), (x, -0.002, 0.5), (x, 0.0, 0.4), (x, 0.006, 0.3), (x, 0.01, 0.24), (x, 0.01, 0.225)],
         [0.12, 0.118, 0.112, 0.112, 0.112, 0.1], n=16)
    return P


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.03 else ('brass_d' if c.z > 0.215 else 'brass'), W_boot)
    boot_shape(P, cx, [(0.095, (0.06, 0.1)), (0.08, (0.078, 0.125)), (0.0, (0.088, 0.135)), (-0.08, (0.088, 0.122)),
                       (-0.16, (0.08, 0.1)), (-0.2, (0.06, 0.07)), (-0.215, (0.026, 0.04))],
               [(0.08, 0.092), (0.18, 0.094), (0.215, 0.1), (0.24, 0.104), (0.25, 0.1)], p=2.6)
    return P


def build_back(torso_ob):
    Pb = Part('BackPatch', 'player', W_torso, (0, 0, 0.85))
    decal(Pb, Surface(torso_ob, back=True), ellipse(0, 0.85, 0.09, 0.09, 20), 0.003, 3)
    return [Pb]


def build():
    closed = [build_torso(), build_face()] + build_corselet() + build_helmet()
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_glove(sx), build_leg(sx), build_boot(sx)]
    closed.append(build_armband(1))
    return closed, lambda obs: build_face_decals(obs['Face'][0]) + build_back(obs['Torso'][0])
