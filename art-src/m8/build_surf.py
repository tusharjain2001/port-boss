# Port Boss M8 step 4: Swell Surf Co.'s jetty and Kai's four surfboards (Blender 5.x headless).
#   -> Assets/Game/Art/M5/PB_Bld_surfjetty.fbx and PB_Prop_board_{foam,body,long,short}.fbx (M1Build registers every PB_*.fbx
#      there; the game places the jetty from SurfShop.cs and the boards from the surfing code)
# Run:  blender -b --factory-startup -P build_surf.py -- [norender]
# 1 unit = 1 m, Z up, front -Y (the M1 axes: Blender -Y = the game's +Z).
# The jetty: the origin is the shore point at sea level; it runs out to sea along -Y from steps on the beach, over the
#   Cut's silted shallows, to a T-head in ~1.3 m of water. Ponyo Coast timber, sun-yellow and teal trim (Kai's colours), an
#   arch with a name board over the steps (read from the sea), two boards planted in the sand, a lamp and a ladder on the
#   head. Colliders: *_colDeck (walk on), *_colBox (rails, posts). Empties: socket_berth + socket_berth_fwd (a boat
#   alongside the stem, bow to sea), socket_sign (the arch board's sea face), socket_lamp.
# The boards: a width profile (tail to nose) slabbed and bevelled round, rocker bent in, a stringer, fins (thruster on
#   the shortboard, a single on the longboard, two soft ones on the foamie, none on the bodyboard). The origin is the
#   middle of the bottom; the nose is -Y.
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'm5'))
from m5lib import *
import m5lib

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
RENDER = 'norender' not in ARGV
m5lib.REN = os.path.join(HERE, 'out', 'renders'); os.makedirs(m5lib.REN, exist_ok=True)

DECK = 1.25                    # the deck's top above the sea (as The Pelican's: Biscuit's gunwale height)
JW = 1.2                       # the stem's half-width
Y0, Y1 = 2.5, -38.0            # the stem: from over the beach out to the head's sea edge
HY0, HX = -34.0, 3.4           # the T-head: from here to Y1, half-width
PILE_Y = [2.0 - 3.4 * i for i in range(12)]


def pile(mb, x, y, top):
    put(mb, cyl(0.12, top + 2.4, 6), 'midwood', (x, y, (top - 2.4) / 2))


def rails(mb, coll, x, ya, yb, tag):
    """posts every ~1.7 m and a top rail along x from ya to yb (ya > yb)"""
    n = max(1, int(round((ya - yb) / 1.7)))
    for i in range(n + 1):
        y = ya - (ya - yb) * i / n
        put(mb, bx(x - 0.05, x + 0.05, y - 0.05, y + 0.05, DECK, DECK + 0.95), 'weath')
    put(mb, bx(x - 0.06, x + 0.06, yb, ya, DECK + 0.9, DECK + 1.0), 'sun')
    put(mb, bx(x - 0.035, x + 0.035, yb, ya, DECK + 0.5, DECK + 0.56), 'weath')
    col_box('Rail%s_colBox' % tag, (x - 0.08, yb, DECK), (x + 0.08, ya, DECK + 1.0), coll)


def planted_board(mb, x, y, col, stripe, lean, yaw):
    """a surfboard stuck nose-up in the sand beside the steps"""
    L, W, T = 2.1, 0.5, 0.07
    m = M((x, y, 0.95), (D(90 - lean), 0, D(yaw)))
    bm = slab(outline(L, W, 0.5, 0.6, 0.12, 14), 0, T, bevel=0.025, seg=2); bmesh.ops.transform(bm, matrix=m, verts=bm.verts); put(mb, bm, col)
    s = slab([(-0.03, -L / 2 + 0.25), (0.03, -L / 2 + 0.25), (0.03, L / 2 - 0.15), (-0.03, L / 2 - 0.15)], T, T + 0.004, bevel=0.0)
    bmesh.ops.transform(s, matrix=m, verts=s.verts); put(mb, s, stripe)


def jetty(mb, coll):
    # the stem's planks, a little ragged in colour, and the head
    y = Y0; k = 0
    while y > HY0 + 0.01:
        y1 = max(HY0, y - 0.27)
        put(mb, bx(-JW, JW, y1, y - 0.03, DECK - 0.07, DECK, 0.0), 'wood' if k % 3 else 'weath'); y -= 0.3; k += 1
    y = HY0; k = 0
    while y > Y1 + 0.01:
        y1 = max(Y1, y - 0.27)
        put(mb, bx(-HX, HX, y1, y - 0.03, DECK - 0.07, DECK, 0.0), 'wood' if k % 3 else 'weath'); y -= 0.3; k += 1
    for sx in (-1, 1): put(mb, bx(sx * JW - 0.12, sx * JW + 0.12, HY0, Y0, DECK - 0.3, DECK - 0.07, 0.0), 'midwood')
    for sx in (-1, 1): put(mb, bx(sx * HX - 0.12, sx * HX + 0.12, Y1, HY0, DECK - 0.3, DECK - 0.07, 0.0), 'midwood')
    put(mb, bx(-HX, HX, Y1 - 0.12, Y1 + 0.12, DECK - 0.3, DECK - 0.07, 0.0), 'midwood')
    for yy in PILE_Y:
        if yy < HY0: break
        for sx in (-1, 1): pile(mb, sx * (JW - 0.05), yy, DECK - 0.07)
    for xx in (-HX + 0.1, -1.2, 1.2, HX - 0.1):
        for yy in (HY0 - 0.1, Y1 + 0.1): pile(mb, xx, yy, DECK - 0.07)
    col_box('Stem_colDeck', (-JW, HY0, DECK - 0.3), (JW, Y0, DECK), coll)
    col_box('Head_colDeck', (-HX, Y1, DECK - 0.3), (HX, HY0 + 0.05, DECK), coll)
    # steps down to the beach at the land end (4 x 0.25 m)
    for i in range(4):
        z = DECK - 0.25 * (i + 1); ya, yb = Y0 + 0.5 * (i + 1), Y0 + 0.5 * i
        put(mb, bx(-JW + 0.1, JW - 0.1, yb, ya, z - 0.07, z, 0.006), 'wood' if i % 2 else 'weath')
        put(mb, bx(-JW + 0.1, JW - 0.1, ya - 0.06, ya, -0.2, z - 0.07, 0.006), 'midwood')
        col_box('Step%d_colDeck' % i, (-JW + 0.1, yb, -0.2), (JW - 0.1, ya, z), coll)
    # rails: the -X side the whole stem; the +X side short of the berth (y -22..-33 is open: the boat lies there)
    rails(mb, coll, -JW + 0.06, Y0, HY0, 'W'); rails(mb, coll, JW - 0.06, Y0, -21.6, 'E1')
    rails(mb, coll, -HX + 0.06, HY0, Y1 + 0.06, 'HW'); rails(mb, coll, HX - 0.06, HY0, Y1 + 0.06, 'HE')
    put(mb, bx(-HX + 0.06, HX - 0.06, Y1 + 0.02, Y1 + 0.14, DECK + 0.9, DECK + 1.0), 'sun')
    for xx in frange(-HX + 0.06, HX - 0.06, (2 * HX - 0.12) / 4):
        put(mb, bx(xx - 0.05, xx + 0.05, Y1 + 0.03, Y1 + 0.13, DECK, DECK + 0.95), 'weath')
    col_box('RailS_colBox', (-HX, Y1, DECK), (HX, Y1 + 0.16, DECK + 1.0), coll)
    # bollards and a coil on the berth side, a ladder off the head
    for yy in (-23.0, -32.0):
        put(mb, cyl(0.12, 0.38, 8), 'iron', (JW - 0.25, yy, DECK + 0.19)); put(mb, cyl(0.16, 0.06, 8), 'iron', (JW - 0.25, yy, DECK + 0.4))
        col_box('Bollard%d_colBox' % int(-yy), (JW - 0.42, yy - 0.17, DECK), (JW - 0.08, yy + 0.17, DECK + 0.45), coll)
    coil(mb, Vector((JW - 0.6, -27.5, DECK + 0.03)), 0.22)
    lx = HX + 0.05
    for yy in (-35.4, -36.0):
        put(mb, bx(lx - 0.03, lx + 0.03, yy - 0.03, yy + 0.03, -1.0, DECK + 0.6), 'iron')
    for zz in frange(-0.8, DECK, 0.3): put(mb, bx(lx - 0.02, lx + 0.02, -36.0, -35.4, zz - 0.02, zz + 0.02), 'iron')
    # the lamp on the head's -X corner
    px, py = -HX + 0.3, Y1 + 0.35
    put(mb, bx(px - 0.08, px + 0.08, py - 0.08, py + 0.08, DECK, DECK + 3.0), 'midwood')
    put(mb, bx(px - 0.05, px + 0.65, py - 0.04, py + 0.04, DECK + 2.85, DECK + 2.93), 'midwood')
    put(mb, bx(px + 0.42, px + 0.72, py - 0.15, py + 0.15, DECK + 2.3, DECK + 2.62), 'sun', a=0)
    put(mb, bx(px + 0.38, px + 0.76, py - 0.19, py + 0.19, DECK + 2.62, DECK + 2.7), 'iron')
    col_box('Lamp_colBox', (px - 0.12, py - 0.12, DECK), (px + 0.12, py + 0.12, DECK + 3.0), coll)
    empty('socket_lamp', (px + 0.57, py, DECK + 2.46), coll)
    # the arch over the steps: two teal posts, a sun-yellow board (the name goes on in the game), a wave cut-out on top
    for sx in (-1, 1):
        put(mb, bx(sx * 1.05 - 0.09, sx * 1.05 + 0.09, Y0 - 0.09, Y0 + 0.09, DECK, DECK + 3.1), 'teal2')
        col_box('Arch%d_colBox' % (sx + 1), (sx * 1.05 - 0.12, Y0 - 0.12, DECK), (sx * 1.05 + 0.12, Y0 + 0.12, DECK + 3.1), coll)
    put(mb, bx(-1.35, 1.35, Y0 - 0.06, Y0 + 0.06, DECK + 2.45, DECK + 3.05), 'sun')
    put(mb, bx(-1.4, 1.4, Y0 - 0.08, Y0 + 0.08, DECK + 3.05, DECK + 3.13), 'teal2')
    wave = [(-1.1 + 0.2 * i, (0.12 if i % 2 else 0.0)) for i in range(12)] + [(1.1, -0.05), (-1.1, -0.05)]
    put(mb, flat(wave, Y0 - 0.085, th=0.02), 'sky', (0, 0, DECK + 3.2))
    empty('socket_sign', (0, Y0 - 0.075, DECK + 2.75), coll)
    planted_board(mb, -1.9, Y0 + 2.2, 'poppy', 'paper', 8, 12)
    planted_board(mb, 1.95, Y0 + 2.0, 'teal', 'sun', -6, -10)
    # the berth: alongside the stem's open stretch, bow out to sea
    empty('socket_berth', (JW + 1.45, -24.5, 0.0), coll); empty('socket_berth_fwd', (JW + 1.45, -29.0, 0.0), coll)


# ---------------------------------------------------------------- the boards ------------------------------------------------
BOARDS = {   # id: (length, width, thickness, rocker, widest at, nose roundness (<1 rounder), tail half-width, main, rail/bottom, stringer, fins)
    'foam':  (2.40, 0.58, 0.085, 0.10, 0.47, 0.55, 0.16, 'peach', 'paper', 'sky', 'soft2'),
    'body':  (1.05, 0.55, 0.060, 0.03, 0.55, 0.40, 0.21, 'sky', 'navy', 'sun', None),
    'long':  (2.75, 0.58, 0.075, 0.12, 0.50, 0.60, 0.15, 'sun', 'cream2', 'teal2', 'single'),
    'short': (1.82, 0.48, 0.060, 0.13, 0.42, 0.95, 0.12, 'paper', 'cream3', 'poppy', 'thruster'),
}


def outline(L, W, m, a, tw, n=28):
    """half-width at t (0 tail .. 1 nose): an ellipse-ish curve with its widest point at m, a nose shaped by a, a tail
    no narrower than tw near t=0 (a squash / round tail)"""
    def hw(t):
        u = (t - m) / (1 - m) if t > m else (m - t) / m
        e = max(0.0, 1 - u * u) ** (0.5 * (a if t > m else 0.5))
        if t < 0.12: e = max(e, tw / (W / 2) * (1 - t / 0.12) + e * (t / 0.12))
        return W / 2 * e
    ts = [0.0] + [0.5 - 0.5 * math.cos(math.pi * i / n) for i in range(1, n)] + [1.0]
    right = [(max(0.004, hw(t)), -(t - 0.5) * L) for t in ts]
    return right + [(-x, y) for x, y in reversed(right[1:-1])]


def rocker(bm, L, r):
    for v in bm.verts:
        t = 0.5 - v.co.y / L
        v.co.z += r * ((2 * t - 1) ** 2) * (1.6 if t > 0.5 else 0.7)


def fin(mb, y, x, h, l, col, lean=0.0):
    poly = [(0.0, 0.0), (l, 0.0), (0.22 * l, -h)]
    bm = slab(poly, -0.004, 0.004, bevel=0.002)
    # slab: x = along the board (toward the nose, -Y), y = depth (down), z = thickness -> board: x, y, z
    R = Matrix(((0, 0, 1, 0), (-1, 0, 0, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
    bmesh.ops.transform(bm, matrix=M((x, y, 0.0), (0, D(lean), 0)) @ R, verts=bm.verts)
    put(mb, bm, col)


def board(bid):
    L, W, T, R_, m, a, tw, main, rail, stripe, fins = BOARDS[bid]
    new_scene(); coll = new_coll('Prop_board_' + bid); seed(5); mb = MB()
    bm = slab(outline(L, W, m, a, tw), 0, T, bevel=T * 0.42, seg=3)
    rocker(bm, L, R_)
    mb.add(bm, lambda c, n: rail if n.z < 0.35 else main)
    if bid != 'body':
        s = slab([(-0.014, -L / 2 + 0.06 * L), (0.014, -L / 2 + 0.06 * L), (0.014, L / 2 - 0.05 * L), (-0.014, L / 2 - 0.05 * L)], T - 0.001, T + 0.003, bevel=0.0)
        rocker(s, L, R_); put(mb, s, stripe)
    if bid == 'body':                                       # a bodyboard: a sun-yellow deck panel with a stripe either side
        pad = slab(rrect(-W * 0.36, W * 0.36, -L * 0.36, L * 0.38, 0.09), T, T + 0.006, bevel=0.0); rocker(pad, L, R_); put(mb, pad, 'sun')
        for sx in (-1, 1):
            st = slab(rrect(sx * W * 0.39 - 0.012, sx * W * 0.39 + 0.012, -L * 0.3, L * 0.3, 0.01), T, T + 0.006, bevel=0.0); rocker(st, L, R_); put(mb, st, 'paper')
    else:                                                   # a deck pad at the tail
        pad = slab(outline(0.36, W * 0.52, 0.5, 0.5, W * 0.26, 12), T, T + 0.008, bevel=0.002)
        bmesh.ops.transform(pad, matrix=Matrix.Translation((0, L / 2 - 0.3, 0)), verts=pad.verts); rocker(pad, L, R_); put(mb, pad, 'ink')
    yt = L / 2 - 0.12
    if fins == 'thruster':
        fin(mb, yt - 0.02, 0.0, 0.11, 0.1, 'ink'); fin(mb, yt - 0.28, 0.13, 0.1, 0.09, 'ink', -6); fin(mb, yt - 0.28, -0.13, 0.1, 0.09, 'ink', 6)
    elif fins == 'single':
        fin(mb, yt - 0.05, 0.0, 0.18, 0.16, 'ink')
    elif fins == 'soft2':
        fin(mb, yt - 0.08, 0.12, 0.09, 0.1, 'sky', -6); fin(mb, yt - 0.08, -0.12, 0.09, 0.1, 'sky', 6)
    ob = finish(mb, 'Prop_board_' + bid, coll, angle=60)
    t = export(coll, 'PB_Prop_board_%s.fbx' % bid)
    print('BOARD %s %.2f m tris %d' % (bid, L, t))
    return t


def build():
    new_scene(); coll = new_coll('Bld_surfjetty'); seed(88); mb = MB()
    jetty(mb, coll)
    finish(mb, 'Bld_surfjetty', coll)
    t = export(coll, 'PB_Bld_surfjetty.fbx')
    print('SURFJETTY tris %d' % t)
    for o in sorted([o for o in coll.objects if o.type == 'EMPTY'], key=lambda o: o.name):
        print('    EMPTY %-20s (%.2f, %.2f, %.2f)' % (o.name, o.location.x, o.location.y, o.location.z))
    shots = []
    if RENDER:
        cam = setup_render(1280, 720, sea=(0.0, 200, 'cblue'))
        shots += [shoot(cam, 'surfjetty-from-the-sea.png', (14, -52, 8), (0, -14, 1.5), 32),
                  shoot(cam, 'surfjetty-land-end.png', (-4.5, 9, 2.6), (0, -6, 2.0), 35),
                  shoot(cam, 'surfjetty-head.png', (7, -44, 3.5), (0, -33, 1.6), 35)]
    for bid in BOARDS:
        board(bid)
        if RENDER:
            cam = setup_render(1280, 720)
            L = BOARDS[bid][0]
            shots.append(shoot(cam, 'board-%s.png' % bid, (L * 0.9, L * 0.75, L * 0.75), (0, 0, 0.05), 35))
            shots.append(shoot(cam, 'board-%s-under.png' % bid, (L * 1.0, -L * 0.35, -L * 0.55), (0, 0, 0.02), 35))
    if RENDER: sheet(shots, 3, os.path.join(m5lib.REN, 'surf-sheet.png'))


build()
