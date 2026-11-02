# Stretch (cast.md §2): tall-lanky, the lamppost. White hard hat sitting too high over spiky black hair, big round
# glasses, big ears, a gap-toothed grin; red ear defenders round his neck; a rust plaid flannel shirt with the sleeves
# rolled to the elbow, a belt, khaki shorts with turned-up hems, rainbow socks, oversized black boots. Modelled in Pip
# space (thin, a smaller head), built at 2.0 m (base 1.75 x root 1.14).
from kitlib import *

HEIGHT = 1.995
ARM_DOWN = -55
HAT = ((0, -0.004, 1.385), (0, -0.004, 1.5))
FACE_Z = 1.27
PALETTE = dict(skin='#E9B892', skin_dk='#D69C77', cheek='#E79A86', hair='#1F1C1E', iris='#2E2420', lens='#DDE6EA',
               flannel='#8C4A2F', flannel_m='#7A3E27', flannel_d='#5E2D1C', roll='#B9A48A', khaki='#B39A6E', khaki_d='#9C8459',
               belt='#4A3326', buckle='#B4B8BD', hat='#F1EFE8', hat_d='#D9D6CC', muff='#D9483B', muff_d='#A8322A', band='#2A2A2E',
               boot='#2A2A2E', sole='#18181A', sock_r='#D9483B', sock_o='#EB8A2E', sock_y='#E9C93A', sock_g='#5AA34A', sock_b='#3D7BD9')

HEAD_C = Vector((0, -0.006, 1.27)); HEAD_R = Vector((0.132, 0.128, 0.152))
LEG_X = 0.1
ARM_Y, ARM_Z = 0.042, 1.086
CHECK = 0.04


def plaid(h, v): return ('flannel', 'flannel_m', 'flannel_m', 'flannel_d')[(h % 2) + 2 * (v % 2)]


# ---------------------------------------------------------------- torso
def torso_key(z):
    keys = [(0.468, (0.06, 0.05, 0.05, 0.0)), (0.49, (0.13, 0.1, 0.09, 0.0)), (0.54, (0.162, 0.118, 0.105, -0.004)),
            (0.62, (0.165, 0.12, 0.108, -0.006)), (0.7, (0.158, 0.118, 0.106, -0.006)), (0.8, (0.162, 0.122, 0.11, -0.004)),
            (0.9, (0.172, 0.128, 0.114, 0.0)), (0.98, (0.178, 0.128, 0.116, 0.004)), (1.04, (0.172, 0.122, 0.112, 0.008)),
            (1.09, (0.14, 0.1, 0.095, 0.014)), (1.125, (0.08, 0.065, 0.065, 0.018)), (1.14, (0.045, 0.045, 0.045, 0.02))]
    return spline(keys, z)


N_T = 28


def build_torso():
    def col(c, n):
        if c.z < 0.655: return 'khaki'
        if c.z < 0.69: return 'belt'
        a = (math.atan2(c.y, c.x) + 2 * math.pi) % (2 * math.pi)
        return plaid(int(c.z / CHECK), int(a / (2 * math.pi) * N_T / 2))
    P = Part('Torso', col, W_torso)
    zs = sorted(set([0.468, 0.476, 0.49, 0.52, 0.56, 0.6, 0.655, 0.672, 0.69] + [round(k * CHECK / 2, 4) for k in range(35, 56)] +
                    [1.1, 1.115, 1.128, 1.136, 1.14]))
    rings = []
    for z in zs:
        rx, ryf, ryb, yc = torso_key(z); pts = []
        for j in range(N_T):
            a = 2 * math.pi * j / N_T; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * ca, yc + sa * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_shirt_bits(torso_ob):
    """Two chest pockets with flaps, the belt buckle, the back patch."""
    S = Surface(torso_ob); parts = []
    Pk = Part('Pockets', 'flannel_d', W_torso, (0, 0, 0.85))
    for sx in (1, -1):
        decal(Pk, S, [(sx * 0.04, 0.975), (sx * 0.112, 0.975), (sx * 0.112, 0.915), (sx * 0.04, 0.915)], 0.003, 2)
    parts.append(Pk)
    Pf = Part('Flaps', 'flannel_m', W_torso, (0, 0, 0.85))
    for sx in (1, -1):
        decal(Pf, S, [(sx * 0.037, 0.985), (sx * 0.115, 0.985), (sx * 0.115, 0.962), (sx * 0.076, 0.952), (sx * 0.037, 0.962)], 0.0045, 2)
    parts.append(Pf)
    Pb = Part('Buckle', 'buckle', W_torso, (0, 0, 0.85))
    decal(Pb, S, [(-0.028, 0.687), (0.028, 0.687), (0.028, 0.658), (-0.028, 0.658)], 0.004, 2); parts.append(Pb)
    Pp = Part('BackPatch', 'player', W_torso, (0, 0, 0.85))
    decal(Pp, Surface(torso_ob, back=True), ellipse(0, 0.93, 0.075, 0.075, 20), 0.003, 3); parts.append(Pp)
    return parts


# ---------------------------------------------------------------- arms
def arm_col(c, n):
    x = abs(c.x)
    if x < 0.3: return plaid(int(x / CHECK), int(((math.atan2(c.z - ARM_Z, c.y - ARM_Y) + 2 * math.pi) % (2 * math.pi)) / (2 * math.pi) * 6))
    if x < 0.345: return 'roll'
    return 'skin'


def build_arm(sx):
    P = Part('Arm', arm_col, W_arm)
    xs = sorted(set([round(k * CHECK / 2, 4) for k in range(5, 16)] + [0.3, 0.305, 0.34, 0.345, 0.352, 0.39, 0.44, 0.49, 0.52]))
    def r(x):
        if x < 0.3: return 0.062
        if x < 0.345: return 0.07                            # the rolled sleeve
        return lerp(0.045, 0.04, (x - 0.345) / 0.18)          # thin bare forearm
    rings = []
    for x in xs:
        rr = r(x); rings.append(ring(Vector((sx * x, ARM_Y, ARM_Z)), Vector((0, 1, 0)), Vector((0, 0, 1)), rr, rr, 12, phase=0))
    P.loft(rings)
    return P


def build_shoulder(sx):
    P = Part('Shoulder', lambda c, n: plaid(int(abs(c.x) / CHECK), int(c.z / CHECK)), W_arm)
    ball(P, (sx * 0.135, ARM_Y + 0.004, ARM_Z - 0.008), 0.068, 12, 8)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    tube(P, [(sx * 0.205, ARM_Y, ARM_Z), (sx * 0.245, ARM_Y, ARM_Z)], [0.07, 0.069], n=14)
    return P


def build_hand(sx):
    P = Part('Hand', 'skin', W_hand)
    y, z = ARM_Y, ARM_Z - 0.004
    tube(P, [(sx * 0.51, y, z), (sx * 0.55, y, z)], [0.04, 0.046], n=10)                                     # wrist
    ball(P, (sx * 0.615, y + 0.002, z - 0.006), 1.0, 12, 8, squash=(0.078, 0.056, 0.036))                    # the mitt
    tube(P, [(sx * 0.57, y - 0.03, z - 0.006), (sx * 0.6, y - 0.066, z - 0.004), (sx * 0.622, y - 0.08, z)], [0.022, 0.02, 0.017], n=8)
    ball(P, (sx * 0.622, y - 0.08, z), 0.017, 8, 5)
    return P


# ---------------------------------------------------------------- legs, socks, boots
def build_shorts(sx):
    x = sx * LEG_X
    P = Part('Shorts', lambda c, n: 'khaki_d' if c.z < 0.455 else 'khaki', W_leg)
    tube(P, [(x * 0.85, 0.0, 0.62), (x, 0.0, 0.53), (x, 0.0, 0.47), (x, 0.0, 0.455), (x, 0.0, 0.43), (x, 0.0, 0.425)],
         [0.1, 0.098, 0.096, 0.1, 0.101, 0.09], n=14)
    return P


def build_shin(sx):
    x = sx * LEG_X
    def col(c, n):
        if c.z > 0.27: return 'skin'
        return ('sock_r', 'sock_o', 'sock_y', 'sock_g', 'sock_b')[min(4, int((0.27 - c.z) / 0.014))]
    P = Part('Shin', col, W_leg)
    zs = [0.44, 0.38, 0.33, 0.29, 0.272, 0.27, 0.256, 0.242, 0.228, 0.214, 0.2, 0.19]
    rs = [0.046, 0.046, 0.044, 0.042, 0.042, 0.048, 0.048, 0.048, 0.048, 0.048, 0.048, 0.045]
    tube(P, [(x, 0.006, z) for z in zs], rs, n=12)
    return P


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.03 else 'boot', W_boot)
    boot_shape(P, cx, [(0.1, (0.06, 0.11)), (0.08, (0.078, 0.13)), (0.0, (0.088, 0.14)), (-0.09, (0.088, 0.13)),
                       (-0.17, (0.082, 0.112)), (-0.215, (0.06, 0.08)), (-0.23, (0.026, 0.045))],
               [(0.09, 0.09), (0.16, 0.088), (0.2, 0.084), (0.205, 0.078)], p=2.6)
    # laces: a few dark ridges up the front
    lc = Part('Laces', 'sole', W_boot); lc.closed = True
    for k, z in enumerate([0.1, 0.13, 0.16]):
        ball(lc, (cx, -0.075 + 0.02 * k, z), 1.0, 8, 4, squash=(0.035, 0.01, 0.008))
    return [P, lc]


# ---------------------------------------------------------------- head
def head_point(th, ph):
    """A long oval head, narrow jaw."""
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)
    x = HEAD_R.x * cp * cx * (1 - 0.1 * low)
    y = HEAD_R.y * cp * sx
    z = HEAD_R.z * sp * (0.95 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def hair_line(a):
    if a < math.pi / 2: return 1.335 - 0.035 * (a / (math.pi / 2)) ** 1.5
    return 1.3 - 0.075 * math.sin(a - math.pi / 2)           # a rounded U at the nape, not a point


def build_head():
    P = Part('Head', lambda c, n: 'hair' if c.z > hair_line(around_of(c, HEAD_C)) + 0.001 else 'skin', W_head)
    return warped_head(P, head_point, hair_line, HK=11, NR=19)


def build_spikes():
    """Spiky black hair sticking out under the hard hat, all round, longer at the sides and back."""
    P = Part('Spikes', 'hair', W_head); P.closed = True
    for row, (z, n, L0, droop, phase) in enumerate([(1.372, 24, 0.075, -0.012, 0.0), (1.345, 20, 0.06, -0.03, 0.5)]):
        for j in range(n):
            th = 2 * math.pi * (j + phase) / n - math.pi / 2
            a = abs(math.atan2(math.cos(th), -math.sin(th)))
            if row == 1 and a > 2.6: continue                     # no low row at the very back (the nape)
            base = Vector((HEAD_C.x + HEAD_R.x * 0.94 * math.cos(th), HEAD_C.y + HEAD_R.y * 0.94 * math.sin(th), z))
            out = (base - Vector((HEAD_C.x, HEAD_C.y, z))).normalized()
            front = a < 0.7                                       # the fringe: shorter, falling over the forehead
            L = (0.045 if front else L0) * (0.85 + 0.3 * ((j * 7) % 5) / 4)
            tip = base + out * L + Vector((0, 0, (-0.035 if front else droop) + 0.012 * (((j * 3) % 4) / 3 - 0.5)))
            cone(P, base, tip, 0.026 if not front else 0.022, 5)
    return P


def build_ears():
    P = Part('Ears', 'skin_dk', W_head); P.closed = True
    for sx in (1, -1): ball(P, (sx * 0.137, HEAD_C.y + 0.012, 1.262), 1.0, 10, 6, squash=(0.026, 0.038, 0.05))
    return P


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, 0.016, 1.08), (0, 0.012, 1.2)], [0.048, 0.045], n=12)
    return P


def build_hardhat():
    """A white hard hat perched too high (a gap of hair under it), a ridge over the top, a short peak at the front."""
    P = Part('HardHat', lambda c, n: 'hat_d' if c.z < 1.392 else 'hat', W_hat)
    cy = -0.004; N = 28
    rings = []
    for z, f in [(1.385, 1.0), (1.42, 0.99), (1.46, 0.93), (1.495, 0.78), (1.518, 0.55), (1.528, 0.25)]:
        rings.append(ring(Vector((0, cy, z)), Vector((1, 0, 0)), Vector((0, 1, 0)), 0.15 * f, 0.155 * f, N))
    P.loft(rings, False, True)
    # the brim: a thin rim all round, wider at the front (the peak)
    prof = []
    for j in range(N):
        a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)
        reach = 0.008 + 0.045 * clamp(-sa) ** 3
        ri = Vector((0.15 * ca, cy + 0.155 * sa, 1.388)); ro = Vector((ri.x + ca * reach, ri.y + sa * reach, 1.384))
        prof.append([ri, ro, ro - Vector((0, 0, 0.01)), ri - Vector((0, 0, 0.01))])
    ids = [[P.vert(p) for p in pr] for pr in prof]
    for j in range(N):
        a, b = ids[j], ids[(j + 1) % N]
        for k in range(4): P.f.append([a[k], b[k], b[(k + 1) % 4], a[(k + 1) % 4]])
    # the ridge over the crown
    tube(P, [(0, cy - 0.15, 1.44), (0, cy - 0.11, 1.5), (0, cy, 1.535), (0, cy + 0.11, 1.5), (0, cy + 0.15, 1.44)], [0.016] * 5, n=8)
    return P


def build_muffs():
    """Red ear defenders round his neck: a cup each side, a black band behind the neck."""
    P = Part('Muffs', lambda c, n: 'muff_d' if abs(c.x) < 0.095 else 'muff', W_neck); P.closed = True
    for sx in (1, -1):
        ball(P, (sx * 0.105, -0.005, 1.12), 1.0, 12, 7, squash=(0.032, 0.05, 0.055))
    B = Part('MuffBand', 'band', W_neck)
    pts = [(math.cos(a) * 0.09, 0.02 + math.sin(a) * 0.075, 1.14) for a in [math.pi * k / 8 for k in range(9)]]
    tube(B, pts, [0.011] * len(pts), n=6)
    return [P, B]


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, HEAD_C)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.05, 1.29
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.022, 0.026, 14)], 0.002)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.002, ez - 0.002, 0.013, 0.015, 12)], 0.0035)
        D('Glint', 'white', [ellipse(ex + sx * 0.002 - 0.003, ez + 0.005, 0.005, 0.005, 8)], 0.005)
        Pb = Part('Brow', 'hair', W_head, HEAD_C)                       # raised: eager
        stroke(Pb, S, [(ex - 0.026, 1.33), (ex, 1.338), (ex + 0.026, 1.332)], 0.01, 0.003); parts.append(Pb)
        D('Cheek', 'cheek', [ellipse(sx * 0.09, 1.235, 0.022, 0.016, 12)], 0.0018)
    # the grin: wide, open, two big front teeth with a gap
    top = [(x, 1.214 + 0.01 * (x / 0.06) ** 2) for x in [-0.06 + 0.12 * k / 10 for k in range(11)]]
    bot = [(0.06 * math.cos(a), 1.214 - 0.04 * math.sin(a)) for a in [math.pi * k / 10 for k in range(1, 10)]]
    D('Mouth', 'mouth', [top[::-1] + bot[::-1]], 0.0022)
    D('Teeth', 'white', [[(-0.024, 1.214), (-0.004, 1.214), (-0.004, 1.197), (-0.024, 1.197)],
                         [(0.004, 1.214), (0.024, 1.214), (0.024, 1.197), (0.004, 1.197)]], 0.0034, 2)
    D('Tongue', 'tongue', [ellipse(0, 1.183, 0.024, 0.009, 12)], 0.0032)
    Pn = Part('Nose', 'skin_dk', W_head); Pn.closed = True
    ball(Pn, S.at(0, 1.248, 0.01), 1.0, 10, 6, squash=(0.02, 0.018, 0.022)); parts.append(Pn)
    # big round glasses: two rims standing just off the face, a bridge, arms back to the ears
    G = Part('Glasses', 'ink', W_head); G.closed = True
    rims = []
    for sx in (1, -1):
        c = S.at(sx * 0.05, 1.29, 0.016)
        pts = [c + Vector((math.cos(a) * 0.038, 0, math.sin(a) * 0.036)) for a in [2 * math.pi * k / 16 for k in range(16)]]
        hoop(G, pts, 0.0045, 6); rims.append(c)
        tube(G, [c + Vector((sx * 0.038, 0, 0.004)), Vector((sx * 0.128, HEAD_C.y + 0.0, 1.298)), Vector((sx * 0.134, HEAD_C.y + 0.06, 1.28))], [0.004] * 3, n=5)
    tube(G, [rims[0] + Vector((-0.038, 0, 0.004)), (0, rims[0].y - 0.006, 1.298), rims[1] + Vector((0.038, 0, 0.004))], [0.004] * 3, n=5)
    parts.append(G)
    return parts


def build():
    closed = [build_torso(), build_head(), build_spikes(), build_ears(), build_neck(), build_hardhat()] + build_muffs()
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_hand(sx), build_shorts(sx), build_shin(sx)] + build_boot(sx)
    closed.append(build_armband(1))
    return closed, lambda obs: build_face(obs['Head'][0]) + build_shirt_bits(obs['Torso'][0])
