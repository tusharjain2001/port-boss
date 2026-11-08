# Port Boss M7 step 4: Efficiency, Adrian Voss's motor yacht (gdd world §4.2 Day 4: "Voss's yacht Efficiency motors past
# the Landing at a distance... Voss on the flybridge doesn't look over"). Blender 5.x headless.
#   -> Assets/Game/Art/M5/PB_Boat_efficiency.fbx (M1Build picks up every PB_*.fbx there; the game sails it: Yacht.cs)
# Run:  blender -b --factory-startup -P build_yacht.py -- [norender]
# 1 unit = 1 m, Z up, the bow points -Y (the front), the waterline at z = 0. Stylised, not a real boat: a long white hull
# with a navy boot stripe, a low cabin with a dark wraparound window band, a flybridge with a white rail and a radar arch,
# and Voss on it in a navy coat, looking ahead. Clean, cold and expensive, against the Landing's patched timber.
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'm5'))
from m5lib import *
import m5lib

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
RENDER = 'norender' not in ARGV
L, B = 20.0, 5.2               # length, beam


def hull_section(y):
    """half-beam and deck height at y (-L/2 bow .. +L/2 stern): a fine bow, a flat full stern"""
    t = (y + L / 2) / L                                     # 0 bow .. 1 stern
    hb = B / 2 * (math.sin(min(1.0, t * 1.9) * math.pi / 2) ** 0.85 if t < 0.55 else 1.0)
    return max(0.05, hb), 1.25 + 0.55 * (1 - t) ** 2      # the sheer rises to the bow


def hull(mb):
    n = 22; ys = [-L / 2 + L * i / n for i in range(n + 1)]
    bm = bmesh.new(); rows = []
    for y in ys:
        hb, top = hull_section(y)
        pts = [(-hb, top), (-hb * 0.98, 0.35), (-hb * 0.7, -0.55), (0, -0.85), (hb * 0.7, -0.55), (hb * 0.98, 0.35), (hb, top)]
        rows.append([bm.verts.new((x, y, z)) for x, z in pts])
    for a, b_ in zip(rows, rows[1:]):
        for k in range(len(a) - 1): bm.faces.new((a[k], a[k + 1], b_[k + 1], b_[k]))
    bm.faces.new(list(reversed(rows[-1])))                  # the transom
    bm.normal_update()
    def col(p, nrm):
        if p.z < -0.05: return 'navy'                       # below the waterline: the antifouling (navy)
        if p.z < 0.42: return 'navy2'                       # the boot stripe
        return 'paper'
    mb.add(bm, col)
    # the deck
    pts = []
    for y in ys:
        hb, top = hull_section(y); pts.append((hb - 0.05, y, top))
    for y in reversed(ys):
        hb, top = hull_section(y); pts.append((-hb + 0.05, y, top))
    bm = bmesh.new(); vs = [bm.verts.new(p) for p in pts]; bm.faces.new(vs); mb.add(bm, 'fresh')
    # a thin rub rail (chrome-grey) along the sheer
    for s in (-1, 1):
        for i in range(n):
            y0, y1 = ys[i], ys[i + 1]; h0, t0 = hull_section(y0); h1, t1 = hull_section(y1)
            a = Vector((s * h0, y0, t0 - 0.12)); b_ = Vector((s * h1, y1, t1 - 0.12))
            mb.add(sweep([a, b_], circle(0.06, 5), cap=True), 'grey2')


def superstructure(mb):
    # the main cabin: white, a dark window band all round, a sloped windscreen
    put(mb, bx(-2.0, 2.0, -3.6, 5.6, 1.3, 3.05, 0.06), 'paper')
    put(mb, bx(-2.03, 2.03, -2.6, 5.3, 1.95, 2.75, 0.02), 'black')                     # the window band
    put(mb, board(-1.9, 1.9, -4.6, -3.55, 1.3, lambda X, Y: 3.0 + (Y + 3.55) * 0.95, 0.04), 'paper')
    put(mb, board(-1.75, 1.75, -4.45, -3.6, 2.0, lambda X, Y: 2.95 + (Y + 3.6) * 0.95, 0.0), 'black')   # the windscreen
    put(mb, bx(-2.15, 2.15, -3.7, 5.8, 3.05, 3.2, 0.03), 'grey2')                       # the cabin roof edge
    # the flybridge: a deck on the roof, a white rail, the helm console, the radar arch
    put(mb, bx(-1.9, 1.9, -1.5, 4.2, 3.2, 3.32, 0.02), 'fresh')
    for x0, x1, y0, y1 in ((-1.9, -1.84, -1.5, 4.2), (1.84, 1.9, -1.5, 4.2), (-1.9, 1.9, -1.56, -1.5)):
        put(mb, bx(x0, x1, y0, y1, 3.32, 4.18, 0.0), 'paper')
        put(mb, bx(x0 - 0.03, x1 + 0.03, y0 - 0.03 if y1 - y0 < 0.1 else y0, y1 + 0.03 if y1 - y0 < 0.1 else y1, 4.1, 4.2, 0.0), 'grey2')
    put(mb, bx(-0.7, 0.7, -1.2, -0.6, 3.32, 4.3, 0.03), 'paper'); put(mb, bx(-0.65, 0.65, -1.25, -1.15, 4.0, 4.28, 0.0), 'black')
    for s in (-1, 1): put(mb, board(s * 1.55 - 0.12, s * 1.55 + 0.12, 2.8, 3.3, 3.32, 5.3, 0.03, lean=(-s * 0.35, 0)), 'paper')
    put(mb, bx(-1.35, 1.35, 2.75, 3.35, 5.2, 5.42, 0.04), 'paper')
    put(mb, bx(-0.75, 0.75, 2.9, 3.2, 5.45, 5.55, 0.02), 'grey2')                      # the radar
    put(mb, cyl(0.04, 0.9, 6), 'grey2', (0, 3.05, 5.95))
    # the name on the stern quarter (navy panel; the words come from the hull stripe) and a swim platform
    put(mb, bx(-B / 2 + 0.05, B / 2 - 0.05, L / 2 - 0.02, L / 2 + 0.75, 0.15, 0.32, 0.03), 'fresh')
    for s in (-1, 1): put(mb, bx(s * B / 2 - 0.02, s * B / 2 + 0.02, 6.6, 8.6, 0.85, 1.05, 0.0), 'navy')


def voss(mb):
    """Adrian Voss on the flybridge at the helm, looking ahead (-Y): a navy coat, grey slicked hair. A silhouette."""
    at = Vector((0.0, -0.1, 3.32))
    put(mb, cyl(0.2, 0.9, 8, r2=0.17), 'navy', at + Vector((0, 0, 0.45)))                # legs + coat skirt
    put(mb, cyl(0.26, 0.62, 8, r2=0.22), 'navy', at + Vector((0, 0, 1.18)))              # the coat
    put(mb, bx(-0.08, 0.08, -0.25, -0.2, 1.05, 1.42, 0.0), 'paper')                     # the shirt front
    for s in (-1, 1): put(mb, cyl(0.075, 0.6, 6), 'navy', at + Vector((s * 0.3, -0.05, 1.12)), (math.radians(-12), s * math.radians(8), 0))
    put(mb, sph_(0.15, 1, 1.05, 1.15), 'peach', at + Vector((0, -0.02, 1.68)))           # the head
    put(mb, sph_(0.155, 1.03, 1.05, 0.7), 'grey2', at + Vector((0, 0.03, 1.77)))         # slicked grey hair


def sph_(r, sx=1.0, sy=1.0, sz=1.0):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=r)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts); return bm


def build():
    new_scene(); coll = new_coll('Boat_efficiency'); seed(31); mb = MB()
    hull(mb); superstructure(mb); voss(mb)
    finish(mb, 'Boat_efficiency', coll)
    empty('socket_name', (B / 2 + 0.03, 7.6, 0.95), coll)
    t = export(coll, 'PB_Boat_efficiency.fbx'); print('YACHT tris %d' % t)
    if RENDER:
        cam = setup_render(1280, 720, sea=(0.0, 300, 'cblue'))
        shots = [shoot(cam, 'yacht-side.png', (26, -4, 5), (0, 0, 2.2), 35), shoot(cam, 'yacht-bow.png', (14, -26, 6), (0, 0, 2.2), 35)]
        sheet(shots, 2, os.path.join(m5lib.REN, 'yacht-sheet.png'))


build()
