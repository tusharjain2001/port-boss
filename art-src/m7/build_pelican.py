# Port Boss M7 step 3: The Pelican, Amara Bell's restaurant on Lantern Key (gdd world §2.3, §6.4; Blender 5.x headless).
#   -> Assets/Game/Art/M5/PB_Bld_pelican.fbx (M1Build picks up every PB_*.fbx there; the game places it: Pelican.cs)
# Run:  blender -b --factory-startup -P build_pelican.py -- [norender]
# 1 unit = 1 m, Z up. The origin is the shore point at sea level; the jetty runs out to sea along -Y (the "front"), the
# restaurant stands inland along +Y on a plinth over the beach. Ponyo Coast: cream timber, terracotta roof, sea-blue
# windows, striped awning and parasols, and a pelican on the sign.
# Colliders: *_colDeck (decks you walk on), *_colBox (walls, rails, tables). Empties (positions only; the game turns them):
#   socket_counter (the customer's spot), socket_counter_mid (the counter), socket_amara (behind it), socket_berth +
#   socket_berth_fwd (the boat alongside, bow to sea), socket_sign (the name board's face), socket_lamp (the jetty lamp).
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'm5'))
from m5lib import *
import m5lib

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
RENDER = 'norender' not in ARGV
m5lib.REN = os.path.join(HERE, 'out', 'renders'); os.makedirs(m5lib.REN, exist_ok=True)

DECK = 1.25                    # the decks' top above the sea
JY0, JY1, JW = -4.0, -27.0, 1.3  # the jetty: from the terrace's edge out to sea, half-width
TX, TY0, TY1 = 6.0, -4.0, 1.0    # the terrace: half-width, sea edge, house front
HX, HY0, HY1, WALL = 5.0, 1.0, 9.0, 3.2   # the house


def sph(r, sx=1.0, sy=1.0, sz=1.0, u=10, v=6):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=r)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts); return bm


def pile(mb, x, y, top):
    put(mb, cyl(0.13, top + 2.6, 6), 'midwood', (x, y, (top - 2.6) / 2))


def jetty(mb, coll):
    y = JY0; k = 0
    while y > JY1 + 0.01:                                   # planks across, a little ragged in colour
        y1 = max(JY1, y - 0.27)
        put(mb, bx(-JW, JW, y1, y - 0.03, DECK - 0.07, DECK, 0.0), 'wood' if k % 3 else 'weath'); y -= 0.3; k += 1
    for sx in (-1, 1): put(mb, bx(sx * JW - 0.12, sx * JW + 0.12 if sx < 0 else sx * JW + 0.12, JY1, JY0, DECK - 0.3, DECK - 0.07, 0.0), 'midwood')
    for yy in (-4.2, -7.6, -11.0, -14.4, -17.8, -21.2, -24.6, -26.9):
        for sx in (-1, 1): pile(mb, sx * (JW - 0.05), yy, DECK - 0.07)
    for yy in (-16.0, -23.5):                               # bollards on the berth side (+X)
        put(mb, cyl(0.12, 0.38, 8), 'iron', (JW - 0.25, yy, DECK + 0.19)); put(mb, cyl(0.16, 0.06, 8), 'iron', (JW - 0.25, yy, DECK + 0.4))
        col_box('Bollard%d_colBox' % int(-yy), (JW - 0.42, yy - 0.17, DECK), (JW - 0.08, yy + 0.17, DECK + 0.45), coll)
    coil(mb, Vector((JW - 0.6, -18.6, DECK + 0.03)), 0.22)
    # the lamp at the end (it is Lantern Key): a post, an arm, a lantern whose panes glow
    lx, ly = -JW + 0.25, JY1 + 0.35
    put(mb, bx(lx - 0.08, lx + 0.08, ly - 0.08, ly + 0.08, DECK, DECK + 3.0), 'midwood')
    put(mb, bx(lx - 0.05, lx + 0.65, ly - 0.04, ly + 0.04, DECK + 2.85, DECK + 2.93), 'midwood')
    put(mb, bx(lx + 0.42, lx + 0.72, ly - 0.15, ly + 0.15, DECK + 2.3, DECK + 2.62), 'sun', a=0)
    put(mb, bx(lx + 0.38, lx + 0.76, ly - 0.19, ly + 0.19, DECK + 2.62, DECK + 2.7), 'iron')
    put(mb, cyl(0.12, 0.14, 6, r2=0.02), 'iron', (lx + 0.57, ly, DECK + 2.77))
    col_box('Lamp_colBox', (lx - 0.12, ly - 0.12, DECK), (lx + 0.12, ly + 0.12, DECK + 3.0), coll)
    empty('socket_lamp', (lx + 0.57, ly, DECK + 2.46), coll)
    col_box('Jetty_colDeck', (-JW, JY1 - 0.1, DECK - 0.3), (JW, JY0, DECK), coll)
    empty('socket_berth', (JW + 1.45, -20.0, 0.0), coll); empty('socket_berth_fwd', (JW + 1.45, -24.0, 0.0), coll)


def parasol(mb, x, y):
    put(mb, cyl(0.03, 2.3, 6), 'dark', (x, y, DECK + 1.15))
    c = Vector((x, y, 0))
    def stripe(p, n):
        a = math.atan2(p.y - c.y, p.x - c.x); return 'red' if int((a + math.pi) / (math.pi / 4)) % 2 else 'paper'
    put(mb, cyl(1.15, 0.42, 16, r2=0.04, bevel=0.0), stripe, (x, y, DECK + 2.25))
    put(mb, sph(0.06), 'dark', (x, y, DECK + 2.5))


def table(mb, coll, x, y, n):
    put(mb, cyl(0.5, 0.05, 12), 'cream2', (x, y, DECK + 0.74)); put(mb, cyl(0.05, 0.72, 6), 'dark', (x, y, DECK + 0.37))
    put(mb, cyl(0.26, 0.04, 8), 'dark', (x, y, DECK + 0.02))
    for k in range(2):                                       # two chairs, painted
        a = math.radians(90 + 180 * k + 25); cx, cy = x + 0.82 * math.cos(a), y + 0.82 * math.sin(a); yaw = a + math.pi / 2
        put(mb, box(0.42, 0.42, 0.05), 'cblue' if (n + k) % 2 else 'teal2', (cx, cy, DECK + 0.45), (0, 0, yaw))
        for lx_, ly_ in ((-0.17, -0.17), (0.17, -0.17), (-0.17, 0.17), (0.17, 0.17)):
            p = Vector((lx_, ly_, 0)); p.rotate(Euler((0, 0, yaw))); put(mb, box(0.04, 0.04, 0.44), 'dark', (cx + p.x, cy + p.y, DECK + 0.22))
        b = Vector((0, 0.2, 0)); b.rotate(Euler((0, 0, yaw)))
        put(mb, box(0.42, 0.05, 0.42), 'cblue' if (n + k) % 2 else 'teal2', (cx + b.x, cy + b.y, DECK + 0.68), (0, 0, yaw))
    col_box('Table%d_colBox' % n, (x - 0.55, y - 0.55, DECK), (x + 0.55, y + 0.55, DECK + 0.8), coll)


def terrace(mb, coll):
    y = TY0; k = 0
    while y < TY1 - 0.01:                                   # boards along X
        put(mb, bx(-TX, TX, y + 0.02, min(TY1, y + 0.29), DECK - 0.1, DECK, 0.0), 'wood' if k % 4 else 'weath'); y += 0.3; k += 1
    put(mb, bx(-TX - 0.05, TX + 0.05, TY0 - 0.08, TY1, DECK - 0.32, DECK - 0.1, 0.0), 'midwood')
    for xx in (-6, -3, 0, 3, 6):
        for yy in (TY0 + 0.1, -1.5): pile(mb, xx * 0.97, yy, DECK - 0.1)
    # the rail: white posts and two rails along the sea edge (open to the jetty) and both sides
    def rail(x0, y0, x1, y1, name):
        L = math.hypot(x1 - x0, y1 - y0); n = max(1, int(L / 1.45))
        for i in range(n + 1):
            t = i / n; put(mb, box(0.09, 0.09, 1.0), 'paper', (x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, DECK + 0.5))
        ang = math.atan2(y1 - y0, x1 - x0)
        for h in (0.97, 0.55): put(mb, box(L + 0.09, 0.07, 0.07), 'paper', ((x0 + x1) / 2, (y0 + y1) / 2, DECK + h), (0, 0, ang))
        col_box(name, (min(x0, x1) - 0.06, min(y0, y1) - 0.06, DECK), (max(x0, x1) + 0.06, max(y0, y1) + 0.06, DECK + 1.05), coll)
    rail(-TX, TY0, -JW, TY0, 'RailL_colBox'); rail(JW, TY0, TX, TY0, 'RailR_colBox')
    rail(-TX, TY0, -TX, TY1, 'RailSL_colBox'); rail(TX, TY0, TX, TY1, 'RailSR_colBox')
    # a lifebuoy on the rail, planters at the corners
    put(mb, torus(Vector((3.6, TY0 - 0.08, DECK + 0.62)), 0.26, 0.07, axis='Y', n=12, m=6), lambda p, n: 'poppy' if int((math.atan2(p.z - DECK - 0.62, p.x - 3.6) + math.pi) / (math.pi / 2)) % 2 else 'paper')
    for px_ in (-TX + 0.45, TX - 0.45):
        put(mb, cyl(0.3, 0.5, 8, r2=0.36), 'terra', (px_, TY1 - 0.45, DECK + 0.25))
        put(mb, blob(2, (0.42, 0.42, 0.34), int(px_ * 10) & 0xff), 'grass2', (px_, TY1 - 0.45, DECK + 0.78))
    table(mb, coll, -3.7, -2.3, 0); table(mb, coll, 3.7, -2.3, 1); table(mb, coll, -3.4, -0.2, 2)
    parasol(mb, -3.7, -2.3); parasol(mb, 3.7, -2.3)
    col_box('Terrace_colDeck', (-TX, TY0 - 0.08, DECK - 0.32), (TX, TY1 + 0.05, DECK), coll)


def window(mb, x, y, z, face):                              # a sea-blue frame, dark glass, teal shutters; face: +1/-1 = Y faces, 'x+'/'x-' sides
    def P(u, v, d): return (x + u, y + d * face, z + v) if face in (1, -1) else (x + d * (1 if face == 'x+' else -1), y + u, z + v)
    rot = (0, 0, 0) if face in (1, -1) else (0, 0, math.pi / 2)
    put(mb, box(1.0, 0.07, 1.15), 'cblue', P(0, 0, 0.03), rot)
    put(mb, box(0.84, 0.05, 0.98), 'navy2', P(0, 0, 0.06), rot)
    put(mb, box(0.86, 0.05, 0.05), 'cblue', P(0, 0, 0.08), rot); put(mb, box(0.05, 0.05, 1.0), 'cblue', P(0, 0, 0.08), rot)
    for s in (-1, 1): put(mb, box(0.42, 0.05, 1.12), 'teal2', P(s * 0.74, 0, 0.04), rot)
    put(mb, box(1.2, 0.16, 0.07), 'cream2', P(0, -0.6, 0.06), rot)


def house(mb, coll):
    # the plinth over the beach (hides the stilts) and the floor
    put(mb, bx(-HX - 0.15, HX + 0.15, HY0, HY1 + 0.2, -0.6, DECK - 0.12, 0.02), 'midwood')
    for xx in frange(-HX, HX, 0.5):                        # vertical board lines on the plinth
        put(mb, bx(xx - 0.015, xx + 0.015, HY0 - 0.02, HY0, -0.4, DECK - 0.14, 0.0), 'dark')
    put(mb, bx(-HX - 0.1, HX + 0.1, HY0 - 0.05, HY1 + 0.15, DECK - 0.12, DECK, 0.0), 'wood')
    # walls: cream, a weathered band at the foot, wood corner posts
    t = 0.18
    put(mb, bx(-HX, HX, HY0, HY0 + t, DECK, DECK + WALL), 'paper'); put(mb, bx(-HX, HX, HY1 - t, HY1, DECK, DECK + WALL), 'paper')
    put(mb, bx(-HX, -HX + t, HY0, HY1, DECK, DECK + WALL), 'paper'); put(mb, bx(HX - t, HX, HY0, HY1, DECK, DECK + WALL), 'paper')
    for (x0, x1, y0, y1) in ((-HX - 0.01, HX + 0.01, HY0 - 0.01, HY0 + 0.02), (-HX - 0.01, HX + 0.01, HY1 - 0.02, HY1 + 0.01),
                             (-HX - 0.01, -HX + 0.02, HY0, HY1), (HX - 0.02, HX + 0.01, HY0, HY1)):
        put(mb, bx(x0, x1, y0, y1, DECK, DECK + 0.55, 0.0), 'cream3')
    for cx_ in (-HX, HX):
        for cy_ in (HY0, HY1): put(mb, bx(cx_ - 0.13, cx_ + 0.13, cy_ - 0.13, cy_ + 0.13, DECK, DECK + WALL + 0.05), 'midwood')
    # gable ends (triangles above the side walls) and the roof: terracotta, ridge along X, eaves over the front
    rise = 2.3; mid = (HY0 + HY1) / 2
    for sx in (-1, 1):
        put(mb, slab([(HY0, 0), (HY1, 0), (mid, rise)], -0.09, 0.09, 0.0), 'paper', (sx * (HX - 0.09), 0, DECK + WALL), (math.pi / 2, 0, math.pi / 2))
    half = (HY1 - HY0) / 2 + 0.7; ang = math.atan2(rise, (HY1 - HY0) / 2); L = half / math.cos(ang)
    for s in (-1, 1):
        cy_ = mid - s * half / 2; cz = DECK + WALL + rise - (half / 2) * math.tan(ang) + 0.08
        put(mb, box(2 * HX + 0.9, L, 0.16), 'terra', (0, cy_, cz), (s * ang, 0, 0))
        for i in range(1, 7):                              # tile lines
            yy = mid - s * (half * i / 7); zz = DECK + WALL + rise - (half * i / 7) * math.tan(ang) + 0.17
            put(mb, box(2 * HX + 0.92, 0.05, 0.04), 'rust', (0, yy, zz), (s * ang, 0, 0))
    put(mb, box(2 * HX + 1.0, 0.3, 0.2), 'rust', (0, mid, DECK + WALL + rise + 0.1))
    put(mb, bx(2.6, 3.3, 6.2, 6.9, DECK + WALL + 0.5, DECK + WALL + rise + 0.9), 'rock')   # chimney
    put(mb, bx(2.5, 3.4, 6.1, 7.0, DECK + WALL + rise + 0.9, DECK + WALL + rise + 1.02), 'rock2')
    # windows: two each side and back, one by the door
    for xx in (-3.4, 3.9): window(mb, xx, HY0, DECK + 1.6, -1)
    for xx in (-2.5, 2.5): window(mb, xx, HY1, DECK + 1.6, 1)
    for yy in (3.2, 6.8): window(mb, -HX, yy, DECK + 1.6, 'x-'); window(mb, HX, yy, DECK + 1.6, 'x+')
    # the door (teal, a brass knob) and the serving hatch with its counter and a striped awning
    put(mb, bx(1.55, 2.65, HY0 - 0.07, HY0, DECK, DECK + 2.25), 'midwood'); put(mb, bx(1.65, 2.55, HY0 - 0.1, HY0 - 0.03, DECK, DECK + 2.15), 'teal2')
    put(mb, sph(0.05), 'brass', (2.42, HY0 - 0.13, DECK + 1.05))
    put(mb, bx(-1.5, 1.1, HY0 - 0.06, HY0, DECK + 0.95, DECK + 2.3), 'midwood')
    put(mb, bx(-1.38, 0.98, HY0 - 0.08, HY0 - 0.02, DECK + 1.05, DECK + 2.2), 'black')      # the hatch: dark inside
    put(mb, bx(-1.36, 0.96, HY0 - 0.2, HY0 - 0.08, DECK + 1.72, DECK + 1.76, 0.0), 'midwood')                 # a shelf of jars in the hatch
    for k, cc in enumerate(('peach', 'sun', 'ice', 'leaf', 'peach', 'cream2', 'sun')):
        put(mb, cyl(0.07, 0.18 + 0.04 * (k % 2), 8), cc, (-1.2 + k * 0.34, HY0 - 0.14, DECK + 1.85 + 0.02 * (k % 2)))
    for k in range(8):                                     # the awning: 8 stripes leaning out over the counter
        x0 = -1.6 + k * 0.35
        put(mb, box(0.35, 1.5, 0.04, 0.0), 'red' if k % 2 == 0 else 'paper', (x0 + 0.175, HY0 - 0.68, DECK + 2.32), (math.radians(-17), 0, 0))
    put(mb, box(2.85, 0.05, 0.16, 0.0), 'red', (-0.2, HY0 - 1.4, DECK + 2.04))
    # the counter in front of the hatch (Amara stands between it and the wall)
    put(mb, bx(-1.45, 1.05, -0.3, 0.18, DECK, DECK + 1.0), 'wood'); put(mb, bx(-1.55, 1.15, -0.38, 0.24, DECK + 1.0, DECK + 1.07), 'cream2')
    put(mb, bx(-1.35, 0.95, -0.34, -0.3, DECK + 0.15, DECK + 0.9, 0.0), 'midwood')
    for k, cc in enumerate(('frost', 'ice', 'frost')): put(mb, bx(-1.2 + k * 0.75, -0.62 + k * 0.75, -0.2, 0.1, DECK + 1.07, DECK + 1.12, 0.0), cc)   # ice trays
    col_box('Counter_colBox', (-1.55, -0.38, DECK), (1.15, 0.24, DECK + 1.07), coll)
    # the name: an arch over the jetty, 5.5 m out from the terrace (so at the counter it's behind the camera), facing the
    # sea where the boats come in (the words: Firm.Sign in the game), the pelican perched on it; its posts go down into
    # the sea like the piles
    AY = TY0 - 5.5
    for sx in (-1, 1):
        put(mb, bx(sx * (JW + 0.1) - 0.1, sx * (JW + 0.1) + 0.1, AY - 0.1, AY + 0.1, -1.6, DECK + 3.25), 'midwood')
        col_box('Arch%s_colBox' % ('L' if sx < 0 else 'R'), (sx * (JW + 0.1) - 0.12, AY - 0.12, DECK), (sx * (JW + 0.1) + 0.12, AY + 0.12, DECK + 3.25), coll)
    put(mb, bx(-JW - 0.45, JW + 0.45, AY - 0.08, AY + 0.04, DECK + 2.45, DECK + 3.15), 'navy')
    put(mb, bx(-JW - 0.35, JW + 0.35, AY - 0.11, AY - 0.06, DECK + 2.53, DECK + 3.07, 0.0), 'cream2')
    put(mb, box(2 * JW + 1.1, 0.3, 0.08, 0.0), 'terra', (0, AY - 0.02, DECK + 3.2))
    empty('socket_sign', (0.0, AY - 0.12, DECK + 2.8), coll)
    pel(mb, Vector((0.95, AY + 0.02, DECK + 3.24)))
    col_box('House_colBox', (-HX - 0.15, HY0 - 0.1, DECK - 1.9), (HX + 0.15, HY1 + 0.2, DECK + WALL), coll)
    empty('socket_counter', (-0.2, -1.25, DECK), coll); empty('socket_counter_mid', (-0.2, -0.06, DECK), coll); empty('socket_amara', (-0.2, 0.62, DECK), coll)


def pel(mb, at):                                           # a pelican, sitting on the board, looking out to sea (-Y)
    put(mb, sph(0.24, 1.0, 1.45, 0.95), 'paper', at + Vector((0, 0.05, 0.24)))                 # body
    for s in (-1, 1): put(mb, sph(0.16, 0.45, 1.3, 0.75), 'cream3', at + Vector((s * 0.2, 0.1, 0.28)))   # wings
    put(mb, sph(0.07, 1, 1, 1.9), 'paper', at + Vector((0, -0.26, 0.48)))                       # neck
    put(mb, sph(0.12), 'paper', at + Vector((0, -0.27, 0.64)))                                  # head
    for s in (-1, 1): put(mb, sph(0.025), 'black', at + Vector((s * 0.085, -0.34, 0.68)))       # eyes
    put(mb, cyl(0.045, 0.5, 6, r2=0.02, bevel=0.0), 'peach', at + Vector((0, -0.6, 0.6)), (math.radians(95), 0, 0))   # beak
    put(mb, sph(0.08, 0.7, 2.2, 0.9), 'peach', at + Vector((0, -0.5, 0.52)))                    # the pouch
    for s in (-1, 1): put(mb, box(0.07, 0.12, 0.02, 0.0), 'peach', at + Vector((s * 0.08, -0.08, 0.01)))   # feet


def build():
    new_scene(); coll = new_coll('Bld_pelican'); seed(77); mb = MB()
    jetty(mb, coll); terrace(mb, coll); house(mb, coll)
    ob = finish(mb, 'Bld_pelican', coll)
    t = export(coll, 'PB_Bld_pelican.fbx')
    print('PELICAN tris %d' % t)
    for o in sorted([o for o in coll.objects if o.type == 'EMPTY'], key=lambda o: o.name):
        print('    EMPTY %-20s (%.2f, %.2f, %.2f)' % (o.name, o.location.x, o.location.y, o.location.z))
    if RENDER:
        cam = setup_render(1280, 720, sea=(0.0, 200, 'cblue'))
        shots = [shoot(cam, 'pelican-from-the-sea.png', (16, -38, 9), (0, 0, 2.5), 32),
                 shoot(cam, 'pelican-terrace.png', (-7.5, -7.5, 3.6), (0.5, 1.5, 2.0), 30),
                 shoot(cam, 'pelican-counter.png', (1.8, -4.0, 2.6), (-0.2, 0.5, 2.3), 35),
                 shoot(cam, 'pelican-jetty.png', (3.5, -30, 3.0), (0, -8, 2.0), 35)]
        sheet(shots, 2, os.path.join(m5lib.REN, 'pelican-sheet.png'))


build()
