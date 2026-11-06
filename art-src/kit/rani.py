# Rani (cast.md §4): broad, the V / anvil. Short black undercut under a mustard bandana (a bow on top, tails behind),
# strong brows, a smirk, gold studs; a sleeveless faded-denim shirt (V neck, two chest pockets), bare strong arms, a
# leather tool belt with pouches, a walkie-talkie and spanners, olive cargo trousers, black lace-up boots, yellow
# gauntlets. Modelled in Pip space, built at 1.82 m (base 1.75 x root 1.04).
from kitlib import *

HEIGHT = 1.82
ARM_DOWN = -40
HAT = ((0, -0.006, 1.34), (0, -0.006, 1.46))
FACE_Z = 1.27
PALETTE = dict(skin='#9A6544', skin_dk='#85543A', shave='#5E4A42', hair='#1E1A1A', iris='#3B2618', lips='#B8323F',
               denim='#8FA9C4', denim_d='#7892AE', fray='#C9D6E2', olive='#6B6F3E', olive_d='#5A5E33', belt='#6E4428',
               pouch='#7E4E2C', pouch_d='#663E22', radio='#26262A', spanner='#B4B8BD', bandana='#D9A23A', bandana_d='#BE8A2C',
               gold='#E3B94C', glove='#E2B43C', glove_d='#C99A2C', boot='#26262A', sole='#161618')
# clothes channels by part (kitlib.CH): 1 Hat, 2 Top, 3 Over, 4 Legs, 5 Boots, 6 Gloves; belt, tool belt, buttons 0
CLOTHES = dict(Bandana=1, Torso=dict(denim=2, olive=4), Shoulder=2, Collar=2, Pockets=2, Flaps=2, Leg=4, Cargo=4, Boot=5,
               Laces=5, Glove=6)

HEAD_C = Vector((0, -0.006, 1.272)); HEAD_R = Vector((0.145, 0.138, 0.156))
LEG_X = 0.115
ARM_Y, ARM_Z = 0.042, 1.086


# ---------------------------------------------------------------- torso
def torso_key(z):
    # the V: broad shoulders and chest over a narrower waist, hips a little out again
    keys = [(0.468, (0.065, 0.055, 0.055, 0.0)), (0.49, (0.16, 0.115, 0.11, 0.0)), (0.54, (0.19, 0.13, 0.13, -0.004)),
            (0.62, (0.186, 0.125, 0.128, -0.006)), (0.7, (0.172, 0.118, 0.115, -0.006)), (0.8, (0.19, 0.125, 0.116, -0.004)),
            (0.9, (0.218, 0.138, 0.122, 0.0)), (0.97, (0.232, 0.14, 0.124, 0.004)), (1.03, (0.235, 0.128, 0.12, 0.008)),
            (1.075, (0.2, 0.105, 0.1, 0.012)), (1.115, (0.12, 0.075, 0.072, 0.016)), (1.14, (0.05, 0.05, 0.05, 0.02))]
    return spline(keys, z)


def build_torso():
    def col(c, n):
        if c.z < 0.665: return 'olive'
        if c.z < 0.702: return 'belt'
        return 'denim'
    P = Part('Torso', col, W_torso)
    zs = [0.468, 0.476, 0.49, 0.52, 0.56, 0.62, 0.665, 0.684, 0.702, 0.74, 0.8, 0.86, 0.92, 0.97, 1.01, 1.045, 1.075, 1.1, 1.12, 1.135, 1.142]
    rings = []
    for z in zs:
        rx, ryf, ryb, yc = torso_key(z); pts = []
        for j in range(28):
            a = 2 * math.pi * j / 28; ca, sa = math.cos(a), math.sin(a)
            x = math.copysign(abs(ca) ** (2 / 2.3), ca); y = math.copysign(abs(sa) ** (2 / 2.3), sa)
            pts.append(Vector((rx * x, yc + y * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_shirt_bits(torso_ob):
    S = Surface(torso_ob); parts = []
    def D(name, col, shapes, out, rings=2):
        P = Part(name, col, W_torso, (0, 0, 0.85))
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    D('Neck V', 'skin', [[(-0.05, 1.135), (0.05, 1.135), (0.0, 1.03)]], 0.002, 3)
    D('Collar', 'denim_d', [[(-0.05, 1.138), (-0.085, 1.12), (-0.03, 1.05), (-0.006, 1.04)],
                            [(0.05, 1.138), (0.006, 1.04), (0.03, 1.05), (0.085, 1.12)]], 0.0035)
    D('Pockets', 'denim_d', [[(sx * 0.045, 0.99), (sx * 0.125, 0.99), (sx * 0.125, 0.925), (sx * 0.045, 0.925)] for sx in (1, -1)], 0.0025)
    D('Flaps', 'denim', [[(sx * 0.042, 0.998), (sx * 0.128, 0.998), (sx * 0.128, 0.976), (sx * 0.085, 0.966), (sx * 0.042, 0.976)] for sx in (1, -1)], 0.004)
    D('Buttons', 'fray', [ellipse(0, z, 0.007, 0.007, 8) for z in (1.0, 0.94, 0.88, 0.82, 0.76)], 0.0035)
    D('Buckle', 'spanner', [[(-0.03, 0.698), (0.03, 0.698), (0.03, 0.668), (-0.03, 0.668)]], 0.004)
    P = Part('BackPatch', 'player', W_torso, (0, 0, 0.85))
    decal(P, Surface(torso_ob, back=True), ellipse(0, 0.95, 0.075, 0.075, 20), 0.003, 3); parts.append(P)
    return parts


def build_toolbelt():
    """Leather pouches on both hips, a walkie-talkie standing in one, spanners hanging off the other."""
    P = Part('Pouches', lambda c, n: 'pouch_d' if c.z > 0.66 else 'pouch', W_torso); P.closed = True
    R = Part('Radio', 'radio', W_torso); R.closed = True
    Sp = Part('Spanners', 'spanner', W_torso); Sp.closed = True
    for sx in (1, -1):
        c = Vector((sx * 0.19, -0.06, 0.625))
        rings = [ring(c + Vector((0, 0, dz)), Vector((1, 0, 0)), Vector((0, 1, 0)), 0.05, 0.035, 12, p=3) for dz in (-0.06, -0.045, 0.035, 0.05)]
        P.loft(rings)
        if sx > 0:
            r = Vector((sx * 0.175, -0.075, 0.7))
            R.loft([ring(r + Vector((0, 0, dz)), Vector((1, 0, 0)), Vector((0, 1, 0)), 0.018, 0.012, 8, p=4) for dz in (-0.06, 0.04)])
            tube(R, [r + Vector((0.008, 0, 0.04)), r + Vector((0.008, 0, 0.09))], [0.004, 0.003], n=5)
        else:
            for dx in (0.0, 0.025):
                top = Vector((sx * (0.2 + dx), -0.1, 0.6))
                tube(Sp, [top, top + Vector((0, -0.004, -0.09))], [0.007, 0.007], n=5)
                ball(Sp, top + Vector((0, -0.004, -0.098)), 0.014, 8, 5)
    return [P, R, Sp]


# ---------------------------------------------------------------- arms
def build_shoulder(sx):
    P = Part('Shoulder', 'denim', W_arm)                    # the shirt's shoulder: sleeveless, ends at the arm
    ball(P, (sx * 0.15, ARM_Y + 0.002, ARM_Z - 0.004), 0.076, 12, 8)
    return P


def build_arm(sx):
    P = Part('Arm', 'skin', W_arm)
    xs = [0.19, 0.22, 0.26, 0.3, 0.33, 0.37, 0.41, 0.45]                    # from inside the shoulder cap out
    rs = [0.07, 0.078, 0.074, 0.062, 0.062, 0.066, 0.06, 0.054]          # a bicep, elbow, forearm
    tube(P, [(sx * x, ARM_Y, ARM_Z) for x in xs], rs, n=14)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    tube(P, [(sx * 0.225, ARM_Y, ARM_Z), (sx * 0.265, ARM_Y, ARM_Z)], [0.085, 0.082], n=16)
    return P


def build_glove(sx):
    """Yellow gauntlets: a big flared cuff halfway up the forearm, a fat mitt."""
    P = Part('Glove', lambda c, n: 'glove_d' if 0.43 < abs(c.x) < 0.445 else 'glove', W_hand)
    y, z = ARM_Y, ARM_Z - 0.004
    tube(P, [(sx * 0.43, y, z), (sx * 0.445, y, z), (sx * 0.5, y, z), (sx * 0.56, y, z)], [0.09, 0.088, 0.072, 0.064], n=14)
    ball(P, (sx * 0.635, y + 0.004, z - 0.006), 1.0, 12, 8, squash=(0.094, 0.068, 0.05))
    tube(P, [(sx * 0.585, y - 0.038, z - 0.006), (sx * 0.617, y - 0.08, z - 0.004), (sx * 0.643, y - 0.096, z)], [0.03, 0.028, 0.023], n=8)
    ball(P, (sx * 0.643, y - 0.096, z), 0.023, 8, 5)
    return P


# ---------------------------------------------------------------- legs + boots
def build_leg(sx):
    x = sx * LEG_X
    P = Part('Leg', 'olive', W_leg)
    tube(P, [(x * 0.88, 0.0, 0.62), (x, 0.0, 0.52), (x, 0.0, 0.42), (x, 0.004, 0.32), (x, 0.01, 0.26), (x, 0.012, 0.245)],
         [0.104, 0.1, 0.094, 0.09, 0.09, 0.082], n=14)
    pk = Part('Cargo', 'olive_d', W_leg); pk.closed = True
    ball(pk, (sx * (LEG_X + 0.088), 0.0, 0.5), 1.0, 10, 7, squash=(0.026, 0.07, 0.068))
    return [P, pk]


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.028 else 'boot', W_boot)
    boot_shape(P, cx, [(0.09, (0.054, 0.1)), (0.075, (0.07, 0.12)), (0.0, (0.078, 0.13)), (-0.08, (0.077, 0.118)),
                       (-0.15, (0.07, 0.098)), (-0.19, (0.05, 0.07)), (-0.205, (0.022, 0.04))],
               [(0.08, 0.082), (0.18, 0.084), (0.26, 0.088), (0.29, 0.09)], p=2.7)
    lc = Part('Laces', 'sole', W_boot); lc.closed = True
    for k, z in enumerate([0.1, 0.13, 0.16, 0.19, 0.22]):
        ball(lc, (cx, -0.072 + 0.012 * k, z), 1.0, 8, 4, squash=(0.032, 0.009, 0.007))
    return [P, lc]


# ---------------------------------------------------------------- head
def head_point(th, ph):
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)
    x = HEAD_R.x * cp * cx * (1 - 0.06 * low)
    y = HEAD_R.y * cp * sx
    z = HEAD_R.z * sp * (0.94 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def shave_line(a):
    """The undercut: shaved up the sides and the back to here, hair above (under the bandana and the slicked top)."""
    if a < 0.9: return 1.355
    return 1.355 - 0.075 * sstep(0.9, 1.5, a)


def build_head():
    P = Part('Head', lambda c, n: 'shave' if c.z > shave_line(around_of(c, HEAD_C)) + 0.001 and around_of(c, HEAD_C) > 0.9 else
             ('hair' if c.z > 1.356 else 'skin'), W_head)
    return warped_head(P, head_point, shave_line, HK=11, NR=19)


def build_hair():
    """The slicked-back top: a long low mass from the forehead over to the crown."""
    P = Part('HairTop', 'hair', W_head); P.closed = True
    ball(P, (0, HEAD_C.y + 0.02, 1.405), 1.0, 14, 8, squash=(0.095, 0.15, 0.058))
    return P


def build_bandana():
    """A mustard bandana round the head along the hairline (lower behind), a bow on top at the front, tails behind."""
    P = Part('Bandana', lambda c, n: 'bandana_d' if abs(c.z - bandana_z(c)) < 0.008 else 'bandana', W_hat)
    N = 28; rings = []
    for dz, out in [(-0.022, 1.075), (-0.008, 1.1), (0.012, 1.1), (0.026, 1.075)]:
        pts = []
        for j in range(N):
            th = 2 * math.pi * j / N; a = abs(math.atan2(math.cos(th), -math.sin(th)))
            zc = 1.36 - 0.05 * sstep(0.6, 2.6, a)
            pts.append(Vector((HEAD_C.x + HEAD_R.x * out * math.cos(th) * 0.99, HEAD_C.y + HEAD_R.y * out * math.sin(th), zc + dz)))
        rings.append(pts)
    P.loft(rings, False, False)
    # the bow on top, a little forward: two loops and a knot
    for sx in (1, -1):
        ball(P, (sx * 0.045, HEAD_C.y - 0.06, 1.448), 1.0, 10, 6, squash=(0.04, 0.022, 0.028))
    ball(P, (0, HEAD_C.y - 0.065, 1.44), 0.022, 8, 5)
    # tails hanging behind
    for sx in (1, -1):
        tube(P, [(sx * 0.012, HEAD_C.y + 0.15, 1.315), (sx * 0.03, HEAD_C.y + 0.165, 1.27), (sx * 0.045, HEAD_C.y + 0.17, 1.225)],
             [0.024, 0.021, 0.016], n=8, flat=0.3)
    return P


def bandana_z(c):
    a = around_of(c, HEAD_C); return 1.36 - 0.05 * sstep(0.6, 2.6, a)


def build_ears():
    P = Part('Ears', 'skin_dk', W_head); P.closed = True
    G = Part('Studs', 'gold', W_head); G.closed = True
    for sx in (1, -1):
        ball(P, (sx * 0.148, HEAD_C.y + 0.012, 1.262), 1.0, 10, 6, squash=(0.018, 0.026, 0.035))
        ball(G, (sx * 0.16, HEAD_C.y + 0.004, 1.236), 0.008, 6, 4)
    return [P, G]


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, 0.018, 1.08), (0, 0.014, 1.19)], [0.06, 0.057], n=12)
    return P


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head, HEAD_C)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.052, 1.293
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.031, 0.023, 14)], 0.002)                       # almond
        D('Iris', 'iris', [ellipse(ex - sx * 0.003, ez - 0.002, 0.018, 0.019, 12)], 0.0035)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.003, ez - 0.002, 0.01, 0.011, 10)], 0.0045)
        D('Glint', 'white', [ellipse(ex - sx * 0.001, ez + 0.005, 0.004, 0.004, 8)], 0.0055)
        Pl = Part('Lid', 'ink', W_head, HEAD_C)
        stroke(Pl, S, [(ex - sx * 0.033, ez + 0.002), (ex, ez + 0.022), (ex + sx * 0.035, ez + 0.007)], 0.007, 0.004); parts.append(Pl)
        Pb = Part('Brow', 'hair', W_head, HEAD_C)                       # strong, arched: the boss
        stroke(Pb, S, [(sx * 0.024, 1.322), (sx * 0.05, 1.334), (sx * 0.08, 1.33)], 0.016, 0.003); parts.append(Pb)
    # the smirk: lips up on one side
    lips = [(-0.046, 1.206), (-0.022, 1.218), (0.0, 1.215), (0.024, 1.22), (0.05, 1.232), (0.032, 1.198), (0.0, 1.188), (-0.026, 1.192)]
    D('Lips', 'lips', [lips], 0.0025)
    Pm = Part('LipLine', 'mouth', W_head, HEAD_C)
    stroke(Pm, S, [(-0.044, 1.206), (0.0, 1.204), (0.048, 1.228)], 0.005, 0.0035); parts.append(Pm)
    Pn = Part('Nose', 'skin_dk', W_head); Pn.closed = True
    ball(Pn, S.at(0, 1.25, 0.008), 1.0, 10, 6, squash=(0.018, 0.015, 0.018)); parts.append(Pn)
    return parts


def build():
    closed = [build_torso(), build_head(), build_hair(), build_bandana(), build_neck()] + build_ears() + build_toolbelt()
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_glove(sx)] + build_leg(sx) + build_boot(sx)
    closed.append(build_armband(1))
    return closed, lambda obs: build_face(obs['Head'][0]) + build_shirt_bits(obs['Torso'][0])
