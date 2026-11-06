# Port Boss M5 art, modelled from code in Blender 5.1 (headless):
#   furniture  -> Assets/Game/Art/M5/PB_Prop_<id>.fbx   (Walt's office: beds, desk, lamps, paintings ...)
#   dock       -> Assets/Game/Art/M5/PB_Prop_<id>.fbx   (planks, berths, dock lamp, power post, tap)
#   wh         -> Assets/Game/Art/M5/PB_Bld_wh0/1/2.fbx (warehouse tiers)
#   office     -> Assets/Game/Art/M5/PB_Bld_office0/1/2.fbx (office tiers, every set spot as an empty)
#   hats       -> Assets/Game/Art/M5/PB_Hat_<id>.fbx
#   spots      -> art-src/m5/spots.json (dock / yard spots in Walt's Landing space)
# Run:  blender -b -P build_props.py -- [furniture dock wh office hats spots | all] [norender]
# 1 unit = 1 m, Z up, every item's FRONT faces -Y, origin at the floor centre of its footprint (wall pieces: the centre
# of the back face; hats: the head's centre). Vertex alpha: 0 = glow faces, 239 = dye channel 1, else 255.
import sys, os, json, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from m5lib import *
import m5lib, layout

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
RENDER = 'norender' not in ARGV
WHAT = {a for a in ARGV if a != 'norender'} or {'all'}
def want(k): return 'all' in WHAT or k in WHAT

REPORT = {}          # fbx -> tris
FOOTS = {}           # prop id -> (w, d, h) measured
OBJS = {}            # id -> visual object (for renders)


_TK = [0]
def tick(mb, label):
    """'debugtris' on the command line: print the tris each part of a build adds."""
    if 'debugtris' in ARGV: t = mb.tris(); print('  TRIS %-22s +%d' % (label, t - _TK[0])); _TK[0] = t


def make(name, fn, fname, kind='prop', off=None):
    """Build one asset into its own collection, export it, remember its tris and footprint. off: shift everything (x, y)."""
    seed(sum(ord(c) * (i + 1) for i, c in enumerate(name)))
    coll = new_coll(name); mb = MB(); _TK[0] = 0
    fn(mb, coll); tick(mb, 'rest')
    ob = finish(mb, name, coll)
    if off and (off[0] or off[1]):
        T = Matrix.Translation((off[0], off[1], 0))
        for o in coll.objects:
            if o.type == 'MESH': o.data.transform(T)
            else: o.location = Vector(o.location) + Vector((off[0], off[1], 0))
    if kind == 'floor':          # a simple body collider from the bounds (game: *_colBox -> BoxCollider)
        bb = [Vector(c) for c in ob.bound_box]
        lo = [min(v[k] for v in bb) for k in range(3)]; hi = [max(v[k] for v in bb) for k in range(3)]
        col_box(name.split('_')[-1].capitalize() + '_colBox', (lo[0], lo[1], max(lo[2], 0)), hi, coll)
    REPORT[fname] = export(coll, fname)
    for o in list(coll.objects):
        o['nm'] = o.name
        if o is not ob: o.name = name + '|' + o.name
    bb = [Vector(c) for c in ob.bound_box]
    lo = [min(v[k] for v in bb) for k in range(3)]; hi = [max(v[k] for v in bb) for k in range(3)]
    al = alpha_counts(ob)
    print('  %s bounds x %.2f..%.2f y %.2f..%.2f z %.2f..%.2f alpha %s' % (name, lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], al))
    OBJS[name] = (ob, coll, lo, hi)
    for o in sorted([o for o in coll.objects if o.type == 'EMPTY'], key=lambda o: o['nm']):
        print('    EMPTY %-18s (%.3f, %.3f, %.3f) rotz %d' % (o['nm'], o.location.x, o.location.y, o.location.z, round(math.degrees(o.rotation_euler.z))))
    return ob, coll, lo, hi


def sheet_mesh(fn, us, vs, th):
    """A two-sided thin sheet: top surface fn(u, v) -> point, bottom = top - (0, 0, th); closed edges."""
    bm = bmesh.new()
    T = [[bm.verts.new(fn(u, v)) for u in us] for v in vs]
    B = [[bm.verts.new(Vector(fn(u, v)) - Vector((0, 0, th))) for u in us] for v in vs]
    nu, nv = len(us), len(vs)
    for j in range(nv - 1):
        for i in range(nu - 1):
            bm.faces.new((T[j][i], T[j][i + 1], T[j + 1][i + 1], T[j + 1][i])); bm.faces.new((B[j][i], B[j + 1][i], B[j + 1][i + 1], B[j][i + 1]))
    for i in range(nu - 1):
        bm.faces.new((T[0][i], B[0][i], B[0][i + 1], T[0][i + 1])); bm.faces.new((T[-1][i], T[-1][i + 1], B[-1][i + 1], B[-1][i]))
    for j in range(nv - 1):
        bm.faces.new((T[j][0], T[j + 1][0], B[j + 1][0], B[j][0])); bm.faces.new((T[j][-1], B[j][-1], B[j + 1][-1], T[j + 1][-1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def rod(mb, a, b, r, col, n=6):
    mb.add(sweep([a, b], circle(r, n)), col)


# ================================================================ FURNITURE ===================================================
def p_cot(mb, coll):
    L = 1.95
    for sx in (-1, 1): put(mb, box(0.07, L, 0.08, 0.025), 'wood', (sx * 0.36, 0, 0.37))
    for sy in (-1, 1): put(mb, box(0.74, 0.07, 0.07, 0.025), 'midwood', (0, sy * (L / 2 - 0.035), 0.37))
    for y in (-0.78, 0.0, 0.78):                     # crossed camp-bed legs
        for s in (-1, 1):
            put(mb, box(0.74, 0.055, 0.055, 0.02), 'midwood', (0, y + s * 0.035, 0.19), (0, s * math.atan2(0.34, 0.66), 0))
        for sx in (-1, 1): put(mb, box(0.09, 0.13, 0.04, 0.0), 'dark', (sx * 0.33, y, 0.02))
    # fresh canvas, a little sag, and new rope lashings at the corners
    mb.add(sheet_mesh(lambda u, v: (u, v, 0.405 - 0.035 * (1 - (u / 0.34) ** 2) * (1 - (v / 0.92) ** 2)), frange(-0.34, 0.34, 0.17), frange(-0.92, 0.92, 0.23), 0.025), 'canvas')
    for sx in (-1, 1):
        for sy in (-1, 1): mb.add(torus((sx * 0.36, sy * 0.86, 0.37), 0.06, 0.016, 'Y', 8, 4), 'rope')
    put(mb, blob(1, (0.27, 0.16, 0.07), 3, 0.08), 'paper', (0, 0.68, 0.45), (D(4), 0, D(J(4))))
    # blanket: poppy with cream stripes, folded back at the top, draping over the rails
    put(mb, box(0.80, 1.18, 0.05, 0.022, 2), 'poppy', (0, -0.30, 0.43), (0, 0, D(J(1.5))))
    for sx in (-1, 1): put(mb, box(0.035, 1.16, 0.15, 0.0), 'poppy', (sx * 0.405, -0.30, 0.37))
    for y in (-0.62, -0.50): put(mb, box(0.815, 0.05, 0.055, 0.015), 'cream2', (0, y, 0.432))
    put(mb, box(0.81, 0.17, 0.065, 0.025, 2), 'cream2', (0, 0.33, 0.44), (D(-6), 0, 0))
    for sx in (-1, 1): put(mb, box(0.035, 0.16, 0.13, 0.0), 'cream2', (sx * 0.41, 0.33, 0.38))


def p_bunk(mb, coll):
    X, Y, H = 1.0, 0.475, 1.75
    for sx in (-1, 1):
        for sy in (-1, 1): put(mb, box(0.09, 0.09, H, 0.025), 'midwood', (sx * (X - 0.045), sy * (Y - 0.045), H / 2), (D(J(0.6)), D(J(0.6)), 0))
    for z, blanket in ((0.34, 'teal'), (1.24, 'sun')):
        for sy in (-1, 1): put(mb, box(2 * X - 0.1, 0.06, 0.15, 0.02), 'wood', (0, sy * (Y - 0.05), z))
        for sx in (-1, 1): put(mb, box(0.06, 2 * Y - 0.1, 0.15, 0.02), 'wood', (sx * (X - 0.05), 0, z))
        put(mb, box(2 * X - 0.16, 2 * Y - 0.16, 0.035, 0.0), 'weath', (0, 0, z - 0.03))
        put(mb, box(1.86, 0.80, 0.13, 0.045, 2), 'cream2', (0, 0, z + 0.05))
        put(mb, box(1.30, 0.84, 0.05, 0.02, 2), blanket, (0.26, 0, z + 0.13), (0, 0, D(J(1.5))))
        put(mb, box(0.10, 0.85, 0.055, 0.0), 'cream2', (-0.37, 0, z + 0.132))
        put(mb, blob(1, (0.17, 0.27, 0.07), int(z * 10), 0.08), 'paper', (-0.72, 0, z + 0.17))
        for sx in (-1, 1):                               # end boards
            put(mb, box(0.05, 2 * Y - 0.12, 0.26, 0.0), 'weath', (sx * (X - 0.05), 0, z + 0.25))
    # guard rail on the top bunk (back full, front stops short of the ladder) + the ladder at the front, +X end
    put(mb, box(2 * X - 0.1, 0.05, 0.08, 0.02), 'wood', (0, Y - 0.05, 1.58))
    put(mb, box(1.25, 0.05, 0.08, 0.02), 'wood', (-0.30, -(Y - 0.05), 1.58))
    put(mb, box(0.06, 0.06, 0.27, 0.015), 'midwood', (0.32, -(Y - 0.05), 1.47))
    for x in (0.52, 0.86): put(mb, box(0.055, 0.055, 1.62, 0.018), 'wood', (x, -(Y + 0.04), 0.81), (D(-2), 0, 0))
    for z in (0.32, 0.62, 0.92, 1.22, 1.50): put(mb, box(0.34, 0.05, 0.045, 0.0), 'midwood', (0.69, -(Y + 0.04), z))


def p_double(mb, coll):
    X, Y = 0.725, 1.025
    for sx in (-1, 1):
        put(mb, box(0.11, 0.11, 1.08, 0.03), 'midwood', (sx * (X - 0.055), Y - 0.055, 0.54))
        put(mb, box(0.11, 0.11, 0.62, 0.03), 'midwood', (sx * (X - 0.055), -(Y - 0.055), 0.31))
        put(mb, cyl(0.07, 0.07, 8, bevel=0.02), 'wood', (sx * (X - 0.055), Y - 0.055, 1.11))
        put(mb, cyl(0.07, 0.07, 8, bevel=0.0), 'wood', (sx * (X - 0.055), -(Y - 0.055), 0.65))
        put(mb, box(0.06, 2 * Y - 0.2, 0.2, 0.0), 'wood', (sx * (X - 0.06), 0, 0.27))
    # headboard: teal painted planks with an arched top (like Walt's door), cream cap rail
    n = 6; w = (2 * X - 0.22) / n
    for i in range(n):
        x0 = -X + 0.11 + i * w + 0.006; x1 = x0 + w - 0.012
        mb.add(board(x0, x1, Y - 0.08, Y - 0.04, 0.42, lambda x, y: 0.93 + 0.1 * math.cos(x / X * math.pi / 2) + J(0.01)), 'teal')
    mb.add(sweep([(x, Y - 0.06, 0.96 + 0.1 * math.cos(x / X * math.pi / 2)) for x in frange(-X + 0.1, X - 0.1, (2 * X - 0.2) / 6)], rrect(-0.035, 0.035, -0.03, 0.03, 0.012, (1, 1, 1, 1))), 'cream2')
    put(mb, box(2 * X - 0.2, 0.05, 0.22, 0.02), 'teal', (0, -(Y - 0.06), 0.47))
    put(mb, box(2 * X - 0.2, 0.06, 0.05, 0.02), 'cream2', (0, -(Y - 0.06), 0.6))
    put(mb, box(2 * X - 0.16, 2 * Y - 0.2, 0.05, 0.0), 'weath', (0, 0, 0.31))
    put(mb, box(1.36, 1.86, 0.2, 0.07, 2), 'cream2', (0, -0.02, 0.44))
    for sx in (-0.33, 0.33): put(mb, blob(1, (0.25, 0.15, 0.08), int(sx * 30) + 50, 0.08), 'paper', (sx, 0.68, 0.6), (D(8), 0, D(J(5))))
    # patchwork quilt over the lower 2/3 with a turned-down poppy fold and drapes
    qy0, qy1 = -0.97, 0.42
    put(mb, box(1.46, qy1 - qy0, 0.05, 0.022), 'cream3', (0, (qy0 + qy1) / 2, 0.56))
    pc = ['poppy', 'sun', 'teal', 'peach', 'sky', 'cream2', 'leaf']
    nx, ny = 4, 4; pw = 1.36 / nx; ph = (qy1 - qy0 - 0.1) / ny
    for i in range(nx):
        for j in range(ny):
            xa = -0.68 + i * pw; ya = qy0 + 0.05 + j * ph
            mb.add(slab([(xa + 0.012, ya + 0.012), (xa + pw - 0.012, ya + 0.012), (xa + pw - 0.012, ya + ph - 0.012), (xa + 0.012, ya + ph - 0.012)], 0.585, 0.593, 0), pc[(i * 3 + j * 2) % len(pc)])
    put(mb, box(1.48, 0.17, 0.065, 0.025), 'poppy', (0, qy1 + 0.02, 0.575))
    for sx in (-1, 1): put(mb, box(0.035, qy1 - qy0, 0.2, 0.0), 'cream3', (sx * 0.735, (qy0 + qy1) / 2, 0.47))
    put(mb, box(1.46, 0.035, 0.2, 0.0), 'cream3', (0, qy0 - 0.005, 0.47))


def hammock_bed(mb, hx=1.2, hz=1.42):
    """The striped hammock between two hooks at (+-hx, 0, hz): spreader bars, sagging cloth, rope fans, a pillow."""
    ex, ez, sag = 0.82, 1.0, 0.42
    def surf(u, v): return (u, v, ez - sag * (1 - (u / ex) ** 2) + 0.09 * (v / 0.42) ** 2 * (1 - 0.5 * (1 - (u / ex) ** 2)))
    stripes = ['teal', 'cream2', 'poppy', 'cream2', 'sun', 'cream2', 'teal']
    mb.add(sheet_mesh(surf, frange(-ex, ex, ex / 5), frange(-0.42, 0.42, 0.12), 0.02),
           lambda c, n: stripes[min(6, max(0, int((c.y + 0.42) / 0.12)))])
    for s in (-1, 1):
        put(mb, box(0.05, 0.92, 0.045, 0.018), 'wood', (s * ex, 0, ez + 0.1))
        for v in (-0.40, -0.13, 0.13, 0.40):
            rod(mb, (s * ex, v, ez + 0.1), (s * (hx - 0.06), 0, hz - 0.02), 0.012, 'rope', 4)
        mb.add(torus((s * (hx - 0.05), 0, hz), 0.035, 0.013, 'Y', 8, 4), 'iron')
    put(mb, blob(1, (0.16, 0.26, 0.07), 9, 0.1), 'sun', (-0.55, 0, ez - 0.2), (0, D(-25), 0))


def p_hammock(mb, coll):
    for s in (-1, 1):
        put(mb, box(0.12, 0.12, 1.62, 0.03, 2), 'midwood', (s * 1.26, 0, 0.81), (0, D(s * 2), 0))
        put(mb, box(0.14, 0.86, 0.1, 0.03), 'midwood', (s * 1.26, 0, 0.05))
        for sy in (-1, 1): put(mb, box(0.06, 0.5, 0.06, 0.018), 'wood', (s * 1.26, sy * 0.2, 0.3), (D(sy * -48), 0, 0))
        put(mb, cyl(0.075, 0.05, 8, bevel=0.015), 'dark', (s * 1.26 + s * 0.03, 0, 1.64))
    hammock_bed(mb)


def p_hammock_kestrel(mb, coll):
    hammock_bed(mb)
    for s in (-1, 1): put(mb, box(0.03, 0.12, 0.16, 0.01), 'iron', (s * 1.25, 0, 1.42))   # hook plates (bolt to the cabin)


def p_chair(mb, coll):
    put(mb, slab(rrect(-0.27, 0.27, -0.25, 0.22, 0.11, (2, 2, 2, 2)), 0.41, 0.46, 0.015), 'wood')
    put(mb, slab(rrect(-0.23, 0.23, -0.22, 0.17, 0.09, (2, 2, 2, 2)), 0.46, 0.51, 0.02), 'poppy')
    for sx in (-1, 1):
        for sy in (-1, 1):
            put(mb, cyl(0.026, 0.44, 6, r2=0.022, bevel=0.008), 'midwood', (sx * 0.21, sy * 0.17, 0.205), (D(-sy * 5), D(sx * 6), 0))
        put(mb, box(0.03, 0.36, 0.03, 0.01), 'midwood', (sx * 0.215, 0, 0.14))
    put(mb, box(0.42, 0.03, 0.03, 0.01), 'midwood', (0, 0, 0.16))
    # the bent arm-and-back rail of a captain's chair, spindles, a crest board
    arc = [D(a) for a in range(-25, 206, 23)]
    rail = [(0.27 * math.cos(a), -0.03 + 0.26 * math.sin(a), 0.66 + 0.1 * max(0, math.sin(a))) for a in arc]
    mb.add(sweep(rail, rrect(-0.04, 0.04, -0.02, 0.02, 0.012, (1, 1, 1, 1))), 'wood')
    for k, a in enumerate(arc):
        if k in (0, len(arc) - 1): rod(mb, (0.24 * math.cos(a), -0.03 + 0.22 * math.sin(a), 0.46), rail[k], 0.022, 'midwood', 6)
        elif k % 2 == 0 or 2 < k < 8: rod(mb, (0.22 * math.cos(a), -0.03 + 0.2 * math.sin(a), 0.46), rail[k], 0.013, 'midwood', 5)
    mb.add(sweep([(0.17 * math.cos(D(a)), -0.03 + 0.27 * math.sin(D(a)), 0.80) for a in range(40, 141, 25)], rrect(-0.035, 0.035, -0.05, 0.05, 0.015, (1, 1, 1, 1))), 'midwood')


def p_desk(mb, coll):
    put(mb, box(1.3, 0.65, 0.05, 0.02, 2), 'midwood', (0, 0, 0.735))
    put(mb, box(1.31, 0.17, 0.052, 0.018), 'fresh', (0, 0.10, 0.737))                 # the mended plank
    for x in (-0.6, -0.3, 0.3, 0.6):
        for y in (0.04, 0.16): put(mb, box(0.018, 0.018, 0.01, 0.0), 'iron', (x, y, 0.764))
    put(mb, box(0.44, 0.60, 0.71, 0.02), 'wood', (0.40, 0, 0.355))                    # drawer pedestal
    for k, z in enumerate((0.58, 0.37, 0.15)):
        put(mb, box(0.38, 0.03, 0.18, 0.012), 'weath' if k != 1 else 'fresh', (0.40, -0.31, z))
        put(mb, cyl(0.022, 0.03, 6, bevel=0.006), 'brass', (0.40, -0.335, z), (D(90), 0, 0))
    for y in (-0.26, 0.26): put(mb, box(0.07, 0.07, 0.71, 0.02), 'wood', (-0.58, y, 0.355))
    put(mb, box(0.06, 0.5, 0.06, 0.015), 'midwood', (-0.58, 0, 0.12))
    put(mb, box(0.9, 0.03, 0.36, 0.012), 'midwood', (-0.17, 0.29, 0.52))
    # splinted leg: two fresh battens + rope wraps
    for dx in (-0.045, 0.045): put(mb, box(0.02, 0.06, 0.28, 0.006), 'fresh', (-0.58 + dx, -0.26, 0.3))
    for z in (0.22, 0.38): mb.add(torus((-0.58, -0.26, z), 0.062, 0.012, 'Z', 8, 4), 'rope')
    # papers, a ledger, a mug, an ink pot, a pencil
    for k, (x, y, a, c) in enumerate(((-0.32, -0.08, 8, 'paper'), (-0.22, -0.02, -12, 'cream3'), (-0.36, 0.02, 25, 'paper'))):
        put(mb, box(0.21, 0.29, 0.004, 0.0), c, (x, y, 0.763 + 0.004 * k), (0, 0, D(a)))
    put(mb, box(0.26, 0.19, 0.04, 0.012), 'poppy', (0.18, 0.12, 0.783), (0, 0, D(-7)))
    put(mb, box(0.25, 0.18, 0.012, 0.004), 'cream2', (0.18, 0.12, 0.79), (0, 0, D(-7)))
    put(mb, cyl(0.045, 0.1, 8, bevel=0.01), 'teal', (0.45, -0.12, 0.81))
    put(mb, cyl(0.038, 0.012, 8, bevel=0.0), 'dark', (0.45, -0.12, 0.857))
    mb.add(torus((0.497, -0.12, 0.81), 0.03, 0.01, 'Y', 8, 4), 'teal')
    put(mb, cyl(0.03, 0.05, 6, r2=0.022, bevel=0.008), 'ink', (-0.05, 0.2, 0.786))
    put(mb, box(0.17, 0.014, 0.014, 0.004), 'sun', (-0.08, -0.15, 0.768), (0, 0, D(30)))


def p_shelves(mb, coll):
    for sx in (-1, 1): put(mb, box(0.045, 0.35, 1.8, 0.015), 'wood', (sx * 0.478, 0, 0.9), (0, D(J(0.5)), 0))
    for z in (0.06, 0.48, 0.90, 1.32, 1.76): put(mb, box(0.93, 0.33, 0.035, 0.012), 'wood', (0, 0, z))
    for k, x in enumerate((-0.31, 0.0, 0.31)): put(mb, box(0.3, 0.025, 1.76, 0.0), 'midwood' if k != 1 else 'dark', (x, 0.162, 0.9))
    put(mb, box(0.95, 0.03, 0.06, 0.01), 'midwood', (0, -0.16, 0.03))
    # bottom: floats + a rope coil
    for k, (x, c) in enumerate(((-0.3, 'poppy'), (-0.12, 'sun'))): put(mb, blob(1, (0.085, 0.085, 0.085), 20 + k, 0.05), c, (x, 0.0, 0.165))
    coil(mb, (0.22, 0.0, 0.08), 0.13, 2, 0.024)
    # books (leaning)
    x = -0.42
    for k, (w, h, c) in enumerate(((0.05, 0.26, 'poppy'), (0.06, 0.22, 'blue'), (0.045, 0.25, 'sun'), (0.07, 0.2, 'teal'), (0.05, 0.24, 'plum'), (0.06, 0.21, 'cream2'))):
        put(mb, box(w, 0.2, h, 0.0), c, (x + w / 2, -0.02, 0.5 + h / 2), (0, D(-6 if k == 5 else J(2)), 0)); x += w + 0.008
    put(mb, box(0.22, 0.17, 0.05, 0.0), 'navy', (0.28, -0.02, 0.523)); put(mb, box(0.2, 0.16, 0.04, 0.0), 'peach', (0.29, -0.02, 0.568), (0, 0, D(8)))
    # jars
    for k, (x, c, cl) in enumerate(((-0.33, 'ice', 'iron'), (-0.12, 'sun', 'rust'), (0.1, 'ice', 'poppy'), (0.32, 'leaf', 'iron'))):
        h = (0.17, 0.13, 0.19, 0.14)[k]
        put(mb, cyl(0.065, h, 8, bevel=0.0), c, (x, 0.0, 0.92 + h / 2))
        put(mb, cyl(0.06, 0.03, 8, bevel=0.0), cl, (x, 0.0, 0.93 + h))
    # glass floats in rope nets + a ship's bell
    for k, (x, c) in enumerate(((-0.28, 'teal'), (0.02, 'sky'))):
        put(mb, blob(1, (0.1, 0.1, 0.1), 30 + k, 0.03), c, (x, 0.0, 1.44))
        mb.add(torus((x, 0.0, 1.44), 0.1, 0.012, 'X', 8, 3), 'rope'); mb.add(torus((x, 0.0, 1.44), 0.1, 0.012, 'Y', 8, 3), 'rope')
    mb.add(lathe([(0.0, 1.35), (0.08, 1.35), (0.075, 1.38), (0.06, 1.46), (0.04, 1.5), (0.0, 1.51)], 10), 'brass', M((0.3, 0, 0)))
    put(mb, box(0.03, 0.03, 0.06, 0.01), 'brass', (0.3, 0, 1.53))
    # on top: a big float in a net
    put(mb, blob(1, (0.13, 0.13, 0.13), 40, 0.04), 'teal', (-0.2, 0.0, 1.91))
    mb.add(torus((-0.2, 0.0, 1.91), 0.13, 0.014, 'X', 10, 4), 'rope')


def p_wardrobe(mb, coll):
    W, Dp, H = 1.0, 0.6, 1.95
    for sx in (-1, 1): put(mb, box(0.045, Dp - 0.02, H - 0.12, 0.012), 'midwood', (sx * (W / 2 - 0.022), 0.01, 0.1 + (H - 0.12) / 2))
    put(mb, box(W - 0.04, 0.03, H - 0.12, 0.01), 'dark', (0, Dp / 2 - 0.02, 0.1 + (H - 0.12) / 2))
    put(mb, box(W + 0.07, Dp + 0.05, 0.08, 0.025, 2), 'midwood', (0, 0.0, H - 0.04))
    put(mb, box(W + 0.03, Dp + 0.02, 0.1, 0.02), 'midwood', (0, 0.0, 0.05))
    put(mb, box(W - 0.06, Dp - 0.06, 0.03, 0.008), 'dark', (0, 0.0, 0.11))
    for sx in (-1, 1): put(mb, box(0.06, 0.06, 0.05, 0.015), 'dark', (sx * 0.42, -0.22, 0.0 + 0.025))
    # inside: a rail, a poppy oilskin and a sky shirt on hangers
    rod(mb, (-0.46, 0.05, 1.62), (0.46, 0.05, 1.62), 0.014, 'brass')
    put(mb, box(0.34, 0.1, 0.78, 0.04, 2), 'poppy', (0.18, 0.05, 1.18), (0, D(3), 0))
    put(mb, box(0.32, 0.08, 0.6, 0.03, 2), 'sky', (-0.16, 0.08, 1.28))
    def door(x_hinge, sgn, ang):
        m = M((x_hinge, -Dp / 2 + 0.005, 0), (0, 0, D(ang)))
        w = W / 2 - 0.02; cx = -sgn * w / 2
        mb.add(box(w, 0.035, H - 0.24, 0.012), 'weath', m @ M((cx, 0, 0.12 + (H - 0.24) / 2)))
        for zc, hh in ((0.6, 0.55), (1.3, 0.62)):
            mb.add(box(w - 0.12, 0.02, hh, 0.01), 'teal', m @ M((cx, -0.02, zc)))
        mb.add(cyl(0.024, 0.04, 6, bevel=0.008), 'brass', m @ M((cx - sgn * (w / 2 - 0.06), -0.04, 1.0), (D(90), 0, 0)))
    door(-W / 2 + 0.02, -1, 0)                  # left door closed (hinged at the left edge)
    door(W / 2 - 0.02, 1, 28)                   # right door ajar


def p_rug(mb, coll):
    cols = ['poppy', 'cream2', 'teal', 'sun', 'cream2', 'peach']
    for k, c in enumerate(cols):
        a, b = 0.8 - k * 0.085, 0.5 - k * 0.085
        put(mb, slab(rrect(-a, a, -b, b, b * 0.92, (3, 3, 3, 3)), 0, 0.012 + 0.0025 * k, 0.004 if k == 0 else 0), c)


def p_plant(mb, coll):
    put(mb, cyl(0.17, 0.03, 10, bevel=0.01), 'terra', (0, 0, 0.015))
    put(mb, cyl(0.12, 0.25, 10, r2=0.15, bevel=0.015), 'terra', (0, 0, 0.155))
    put(mb, cyl(0.172, 0.07, 10, bevel=0.02), 'terra', (0, 0, 0.29))
    put(mb, cyl(0.175, 0.03, 10, bevel=0.008), 'teal', (0, 0, 0.24))
    put(mb, cyl(0.15, 0.02, 10, bevel=0.0), 'ink', (0, 0, 0.315))
    for k in range(11):
        a = D(k * 360 / 11 + J(10)); r = 0.20 + 0.04 * (k % 3); h = 0.38 + 0.18 * ((k * 7) % 5) / 4
        path = [(math.cos(a) * r * t, math.sin(a) * r * t, 0.31 + h * math.sin(math.pi * min(t, 0.95) * 0.82) - 0.05 * t * t) for t in frange(0, 1, 0.2)]
        prof = [[(-w, -0.006), (w, -0.006), (w, 0.006), (-w, 0.006)] for w in [0.012 + 0.05 * math.sin(math.pi * (0.12 + 0.88 * t)) for t in frange(0, 1, 0.2)]]
        mb.add(sweep(path, prof, side=(-math.sin(a), math.cos(a), 0)), ('fern', 'leaf', 'grass2')[k % 3])


def p_lamp(mb, coll):
    mb.add(lathe([(0.0, 0.0), (0.10, 0.0), (0.105, 0.025), (0.085, 0.05), (0.075, 0.12), (0.09, 0.17), (0.05, 0.22), (0.025, 0.24), (0.0, 0.245)], 10), 'teal')
    put(mb, cyl(0.012, 0.11, 6, bevel=0.0), 'brass', (0, 0, 0.29))
    put(mb, blob(1, (0.045, 0.045, 0.05), 5, 0.0), 'sun', (0, 0, 0.34), a=GLOW)
    put(mb, cyl(0.15, 0.17, 10, r2=0.09, bevel=0.012), 'cream2', (0, 0, 0.38), a=GLOW)
    mb.add(torus((0, 0, 0.297), 0.15, 0.012, 'Z', 12, 4), 'poppy')


def p_floorlamp(mb, coll):
    for k in range(3):
        a = D(90 + k * 120); ft = (0.2 * math.cos(a), 0.2 * math.sin(a), 0.02)
        mb.add(sweep([ft, (0.1 * math.cos(a), 0.1 * math.sin(a), 0.25), (0.02 * math.cos(a), 0.02 * math.sin(a), 0.45)], circle(0.022, 6)), 'midwood')
        put(mb, cyl(0.03, 0.03, 6, bevel=0.008), 'dark', ft)
    put(mb, cyl(0.03, 0.06, 8, bevel=0.01), 'brass', (0, 0, 0.45))
    put(mb, cyl(0.016, 1.04, 6, bevel=0.0), 'brass', (0, 0, 0.98))
    put(mb, blob(1, (0.05, 0.05, 0.055), 6, 0.0), 'sun', (0, 0, 1.40), a=GLOW)
    put(mb, cyl(0.22, 0.27, 12, r2=0.16, bevel=0.015), 'cream2', (0, 0, 1.47), a=GLOW)
    mb.add(torus((0, 0, 1.335), 0.22, 0.014, 'Z', 12, 4), 'poppy')
    mb.add(torus((0, 0, 1.605), 0.16, 0.012, 'Z', 12, 4), 'poppy')


def frame(mb, w, h, d, col, inner_col):
    """Picture frame on a wall: back face at y=0, front to -y; centred at the origin in x/z."""
    t = 0.06
    for sz in (-1, 1): put(mb, box(w, d, t, 0.012), col, (0, -d / 2, sz * (h - t) / 2))
    for sx in (-1, 1): put(mb, box(t, d, h - 2 * t + 0.01, 0.012), col, (sx * (w - t) / 2, -d / 2, 0))
    put(mb, box(w - 2 * t + 0.02, 0.012, h - 2 * t + 0.02, 0.0), inner_col, (0, -0.012, 0))
    return -0.02           # y of the picture plane


def poly_circle(cx, cz, r, n=10, a0=0, a1=360):
    return [(cx + r * math.cos(D(a0 + (a1 - a0) * i / n)), cz + r * math.sin(D(a0 + (a1 - a0) * i / n))) for i in range(n + (0 if a1 - a0 >= 360 else 1))]


def p_painting(mb, coll):
    w, h = 0.72, 0.56; y = frame(mb, w, h, 0.05, 'wood', 'peach')
    L = [0]
    def F(poly, c): L[0] += 1; mb.add(flat(poly, y - 0.002 * L[0], 0.002), c)
    a, b = 0.30, 0.22                                      # picture half-extents
    F([(-a, 0.06), (a, 0.06), (a, b), (-a, b)], 'ice')         # sky: pale blue top
    F([(-a, 0.0), (a, 0.0), (a, 0.08), (-a, 0.1)], 'peach')    # dawn band
    F(poly_circle(0.12, -0.035, 0.085, 10, 0, 180), 'sun')     # the sun rising
    F([(-a, -0.04), (a, -0.04), (a, 0.0), (-a, 0.0)], 'sun')
    F([(-a, -b), (a, -b), (a, -0.04), (-a, -0.04)], 'teal')    # the sea
    for zz, x0, x1 in ((-0.10, -0.25, -0.05), (-0.16, 0.0, 0.22), (-0.07, 0.15, 0.27)): F([(x0, zz), (x1, zz), (x1, zz + 0.012), (x0, zz + 0.012)], 'ice')
    for zz, ww in ((-0.06, 0.07), (-0.09, 0.05), (-0.12, 0.035)): F([(0.12 - ww, zz), (0.12 + ww, zz), (0.12 + ww, zz + 0.012), (0.12 - ww, zz + 0.012)], 'sun')
    # clouds
    for cx, cz, s in ((-0.17, 0.16, 1.0), (0.18, 0.18, 0.7)):
        F(poly_circle(cx, cz, 0.04 * s, 8) , 'cream2'); F(poly_circle(cx + 0.045 * s, cz - 0.01, 0.03 * s, 8), 'cream2'); F(poly_circle(cx - 0.045 * s, cz - 0.012, 0.028 * s, 8), 'cream2')
    # the little red-and-cream skiff (Biscuit) with her lantern and yellow motor
    F([(-0.20, -0.085), (-0.02, -0.085), (0.02, -0.052), (-0.23, -0.052)], 'red')
    F([(-0.235, -0.052), (0.02, -0.052), (0.025, -0.04), (-0.24, -0.04)], 'cream2')
    F([(-0.03, -0.04), (0.0, -0.04), (0.0, -0.015), (-0.03, -0.015)], 'sun')
    F([(-0.215, -0.04), (-0.208, -0.04), (-0.208, 0.03), (-0.215, 0.03)], 'ink')
    F(poly_circle(-0.198, 0.02, 0.012, 6), 'sun')
    F([(-0.21, -0.1), (0.01, -0.1), (0.0, -0.09), (-0.2, -0.09)], 'blue')     # her shadow on the water
    # gulls
    for gx, gz in ((-0.05, 0.13), (0.03, 0.15)):
        F([(gx - 0.03, gz), (gx, gz - 0.012), (gx + 0.03, gz), (gx, gz - 0.005)], 'ink')


def p_painting2(mb, coll):
    w, h = 0.52, 0.72; y = frame(mb, w, h, 0.05, 'midwood', 'cream2')
    L = [0]
    def F(poly, c): L[0] += 1; mb.add(flat(poly, y - 0.002 * L[0], 0.002), c)
    a, b = 0.20, 0.30
    F([(-a, 0.0), (a, 0.0), (a, b), (-a, b)], 'sky')
    F([(-a, -0.02), (a, -0.02), (a, 0.12), (-a, 0.08)], 'peach')
    F([(-a, -b), (a, -b), (a, -0.06), (-a, -0.06)], 'teal')
    for zz, x0, x1 in ((-0.14, -0.15, 0.02), (-0.22, 0.03, 0.17)): F([(x0, zz), (x1, zz), (x1, zz + 0.012), (x0, zz + 0.012)], 'ice')
    F([(-a, -0.12), (-0.12, -0.05), (0.02, -0.03), (0.12, -0.07), (a, -0.10), (a, -0.16), (-a, -0.18)], 'rock2')
    F([(-0.15, -0.06), (-0.10, -0.035), (0.02, -0.02), (0.1, -0.05), (0.06, -0.07), (-0.12, -0.08)], 'grass')
    # the lighthouse: cream tower with poppy bands, dark gallery, glowing lantern, poppy cap, beams
    F([(-0.18, 0.17), (-0.045, 0.14), (-0.045, 0.16)], 'cream2'); F([(0.18, 0.2), (0.045, 0.14), (0.045, 0.16)], 'cream2')
    F([(-0.045, -0.03), (0.045, -0.03), (0.03, 0.13), (-0.03, 0.13)], 'paper')
    for z0 in (0.0, 0.07): F([(-0.043 + 0.0015 * z0 * 50, z0), (0.043 - 0.0015 * z0 * 50, z0), (0.04 - 0.0015 * z0 * 50, z0 + 0.025), (-0.04 + 0.0015 * z0 * 50, z0 + 0.025)], 'poppy')
    F([(-0.045, 0.13), (0.045, 0.13), (0.045, 0.14), (-0.045, 0.14)], 'ink')
    F([(-0.025, 0.14), (0.025, 0.14), (0.025, 0.175), (-0.025, 0.175)], 'sun')
    F([(-0.035, 0.175), (0.035, 0.175), (0.0, 0.215)], 'poppy')
    F(poly_circle(0.12, 0.22, 0.03, 8), 'paper')                               # moon-ish sun
    for gx, gz in ((-0.12, 0.24), (-0.07, 0.21)): F([(gx - 0.025, gz), (gx, gz - 0.01), (gx + 0.025, gz), (gx, gz - 0.004)], 'ink')


def p_stove(mb, coll):
    put(mb, box(0.6, 0.6, 0.03, 0.01), 'rock2', (0, 0, 0.015))
    for k in range(3):
        a = D(90 + 120 * k); put(mb, box(0.05, 0.05, 0.13, 0.012), 'black', (0.15 * math.cos(a), 0.15 * math.sin(a), 0.095))
    mb.add(lathe([(0.0, 0.14), (0.16, 0.14), (0.2, 0.2), (0.235, 0.32), (0.24, 0.42), (0.215, 0.56), (0.16, 0.64), (0.0, 0.645)], 12), 'iron')
    mb.add(torus((0, 0, 0.42), 0.243, 0.016, 'Z', 12, 4), 'black')
    put(mb, cyl(0.18, 0.04, 12, bevel=0.012), 'black', (0, 0, 0.665))
    put(mb, cyl(0.2, 0.025, 12, bevel=0.008), 'black', (0, 0, 0.7))
    put(mb, cyl(0.06, 1.6, 8, bevel=0.0), 'black', (0, 0.06, 1.5))
    put(mb, cyl(0.075, 0.04, 8, bevel=0.01), 'iron', (0, 0.06, 1.0))
    # fire door with a warm window, a brass handle, an ash lip
    put(mb, box(0.2, 0.06, 0.17, 0.02), 'black', (0, -0.215, 0.37))
    put(mb, box(0.13, 0.03, 0.08, 0.01), 'sun', (0, -0.24, 0.385))
    for x in (-0.03, 0.03): put(mb, box(0.012, 0.035, 0.08, 0.003), 'black', (x, -0.25, 0.385))
    put(mb, box(0.06, 0.04, 0.02, 0.008), 'brass', (0.1, -0.26, 0.33))
    put(mb, box(0.22, 0.09, 0.025, 0.008), 'black', (0, -0.2, 0.2))
    # the kettle on top
    mb.add(lathe([(0.0, 0.0), (0.09, 0.0), (0.11, 0.05), (0.1, 0.1), (0.06, 0.13), (0.02, 0.14), (0.0, 0.145)], 10), 'teal', M((0.02, 0.02, 0.712)))
    mb.add(sweep([(0.1, 0.02, 0.76), (0.16, 0.02, 0.8), (0.19, 0.02, 0.84)], [circle(r, 6) for r in (0.025, 0.017, 0.012)]), 'teal')
    mb.add(sweep([(-0.05, 0.02, 0.85), (-0.03, 0.02, 0.9), (0.06, 0.02, 0.9), (0.08, 0.02, 0.85)], circle(0.01, 5), up=(0, 1, 0)), 'black')


def p_maptable(mb, coll):
    X, Y = 0.7, 0.45
    for sx in (-1, 1):
        for sy in (-1, 1):
            mb.add(lathe([(0.0, 0.0), (0.05, 0.0), (0.045, 0.06), (0.03, 0.15), (0.045, 0.35), (0.03, 0.55), (0.04, 0.72), (0.045, 0.78), (0.0, 0.78)], 8), 'midwood', M((sx * (X - 0.08), sy * (Y - 0.08), 0)))
        put(mb, box(0.05, 2 * Y - 0.2, 0.05, 0.0), 'midwood', (sx * (X - 0.08), 0, 0.2))
    put(mb, box(2 * X - 0.2, 0.05, 0.05, 0.0), 'midwood', (0, 0, 0.2))
    for sy in (-1, 1): put(mb, box(2 * X - 0.1, 0.04, 0.1, 0.0), 'wood', (0, sy * (Y - 0.06), 0.73))
    for sx in (-1, 1): put(mb, box(0.04, 2 * Y - 0.1, 0.1, 0.0), 'wood', (sx * (X - 0.06), 0, 0.73))
    put(mb, box(2 * X, 2 * Y, 0.05, 0.02, 2), 'wood', (0, 0, 0.805))
    for sy in (-1, 1): put(mb, box(2 * X, 0.05, 0.04, 0.015), 'midwood', (0, sy * (Y - 0.025), 0.85))
    for sx in (-1, 1): put(mb, box(0.05, 2 * Y - 0.1, 0.04, 0.015), 'midwood', (sx * (X - 0.025), 0, 0.85))
    # the chart of the bay: cream sheet, turquoise bay, the crescent island, the town, a dashed route, a compass rose
    z = 0.832; rot = Matrix.Rotation(D(-3), 4, 'Z')
    def C(poly, c, dz):
        bm = slab(poly, z + dz, z + dz + 0.003, 0); mb.add(bm, c, rot)
    C([(-0.56, -0.33), (0.56, -0.33), (0.56, 0.33), (-0.56, 0.33)], 'cream3', 0)
    C([(-0.52, -0.29), (0.52, -0.29), (0.52, 0.29), (-0.52, 0.29)], 'ice', 0.003)
    arc = [D(a) for a in frange(-140, 140, 20)]
    path = [Vector((0.06 + 0.24 * math.cos(a), 0.235 * math.sin(a), z + 0.006)) for a in arc]
    wid = [0.012 + 0.07 * math.sin(math.pi * k / (len(arc) - 1)) ** 0.7 for k in range(len(arc))]
    mb.add(sweep(path, [[(-w, 0), (w, 0), (w, 0.003), (-w, 0.003)] for w in wid]), 'sand', rot)
    mb.add(sweep([p + Vector((0, 0, 0.003)) for p in path[2:-2]], [[(-w * 0.55, 0), (w * 0.55, 0), (w * 0.55, 0.003), (-w * 0.55, 0.003)] for w in wid[2:-2]]), 'leaf', rot)
    for x, y in ((0.31, 0.0), (0.27, 0.07), (0.28, -0.06)): C([(x - 0.012, y - 0.012), (x + 0.012, y - 0.012), (x + 0.012, y + 0.012), (x - 0.012, y + 0.012)], 'poppy', 0.012)
    for k in range(6):
        x = 0.22 - k * 0.075; y = -0.02 + 0.04 * math.sin(k * 0.9)
        C([(x - 0.02, y - 0.005), (x + 0.02, y - 0.005), (x + 0.02, y + 0.005), (x - 0.02, y + 0.005)], 'poppy', 0.006)
    cx, cy = -0.4, 0.2; star = []
    for k in range(8):
        r = 0.07 if k % 2 == 0 else 0.022; a = D(90 + 45 * k); star.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    C(star, 'ink', 0.006)
    # brass dividers lying open on the chart, a pencil, a stone paperweight
    hx, hy = 0.05, -0.18
    for a in (D(200), D(232)):
        L = 0.24; ex, ey = hx + L * math.cos(a), hy + L * math.sin(a)
        mb.add(sweep([(hx, hy, z + 0.02), (ex, ey, z + 0.01)], [circle(r, 5) for r in (0.008, 0.004)]), 'brass')
    put(mb, cyl(0.016, 0.012, 8, bevel=0.003), 'brass', (hx, hy, z + 0.022))
    put(mb, box(0.16, 0.012, 0.012, 0.003), 'sun', (0.38, -0.2, z + 0.012), (0, 0, D(-20)))
    put(mb, blob(1, (0.05, 0.04, 0.025), 77, 0.15), 'rock2', (-0.45, -0.22, z + 0.02))


def p_trophy(mb, coll):
    mb.add(flat(rrect(-0.45, 0.45, -0.21, 0.21, 0.15, (3, 3, 3, 3)), -0.0, 0.03, 0.01), 'midwood')
    mb.add(flat(rrect(-0.40, 0.40, -0.165, 0.165, 0.11, (3, 3, 3, 3)), -0.03, 0.02, 0.008), 'wood')
    for x in (-0.22, 0.22):
        put(mb, cyl(0.016, 0.07, 6, bevel=0.004), 'brass', (x, -0.085, 0.03), (D(90), 0, 0))
        put(mb, cyl(0.024, 0.012, 6, bevel=0.003), 'brass', (x, -0.12, 0.03), (D(90), 0, 0))
    put(mb, box(0.2, 0.01, 0.055, 0.004), 'brass', (0, -0.055, -0.115))
    mb.add(sweep([(-0.12, -0.01, 0.2), (0, -0.01, 0.29), (0.12, -0.01, 0.2)], circle(0.01, 5), up=(0, 1, 0)), 'rope')


def p_balcony(mb, coll):
    W, Dp = 2.5, 1.2
    for k in range(6):
        y = -0.1 - k * 0.2; put(mb, box(W, 0.18, 0.045, 0.012), 'fresh' if k % 2 else 'wood', (J(0.01), y, -0.0225), (0, 0, D(J(0.4))))
    put(mb, box(W, 0.07, 0.16, 0.02), 'midwood', (0, -0.035, -0.12))
    for x in (-1.18, 0.0, 1.18):
        put(mb, box(0.07, Dp, 0.14, 0.02), 'midwood', (x, -Dp / 2, -0.115))
        L = math.hypot(0.95, 0.7); put(mb, box(0.07, L, 0.08, 0.0), 'midwood', (x, -0.48, -0.47), (math.atan2(0.7, 0.95), 0, 0))
        put(mb, box(0.12, 0.04, 0.16, 0.0), 'iron', (x, -0.02, -0.82))
    # the -X side rail stops short: a walk-in gap by the wall (local y -0.05..-0.85) to step on from a porch / landing
    for x, y in ((-1.2, -1.15), (0.0, -1.15), (1.2, -1.15), (-1.2, -0.9), (1.2, -0.06)):
        put(mb, box(0.09, 0.09, 1.05, 0.02), 'midwood', (x, y, 0.525))
    put(mb, box(W + 0.06, 0.13, 0.06, 0.02), 'cream2', (0, -1.15, 1.06))
    put(mb, box(0.13, Dp - 0.05, 0.06, 0.02), 'cream2', (1.2, -0.6, 1.06))
    put(mb, box(0.13, 0.3, 0.06, 0.02), 'cream2', (-1.2, -1.02, 1.06))
    put(mb, box(W, 0.05, 0.06, 0.015), 'wood', (0, -1.15, 0.12))
    put(mb, box(0.05, Dp - 0.1, 0.06, 0.015), 'wood', (1.2, -0.6, 0.12))
    put(mb, box(0.05, 0.25, 0.06, 0.015), 'wood', (-1.2, -1.02, 0.12))
    for x in frange(-1.05, 1.05, 0.21): put(mb, box(0.045, 0.03, 0.88, 0.0), 'wood', (x, -1.15, 0.59))
    for y in frange(-0.95, -0.25, 0.233): put(mb, box(0.03, 0.045, 0.88, 0.0), 'wood', (1.2, y, 0.59))
    put(mb, box(0.03, 0.045, 0.88, 0.0), 'wood', (-1.2, -1.03, 0.59))
    # a teal flower box hooked over the front rail
    put(mb, box(0.8, 0.2, 0.18, 0.025), 'teal', (0.55, -1.3, 0.98))
    for k in range(5): put(mb, blob(1, (0.07, 0.07, 0.06), 90 + k, 0.15), ('poppy', 'leaf', 'sun', 'leaf', 'poppy')[k], (0.25 + 0.15 * k, -1.3, 1.1))
    col_box('Deck_colDeck', (-W / 2, -Dp, -0.06), (W / 2, 0, 0), coll)
    col_box('RailF_colBox', (-W / 2, -Dp, 0), (W / 2, -Dp + 0.12, 1.1), coll)
    col_box('RailL_colBox', (-W / 2, -Dp, 0), (-W / 2 + 0.12, -0.86, 1.1), coll)
    col_box('RailR_colBox', (W / 2 - 0.12, -Dp, 0), (W / 2, 0, 1.1), coll)


def p_locker(mb, coll):
    """The office's fixed clothes locker (modelled into every office tier, not sold)."""
    put(mb, box(0.45, 0.43, 1.78, 0.03, 2), 'teal2', (0, 0.01, 0.89))
    put(mb, box(0.40, 0.03, 1.62, 0.015), 'teal', (0, -0.205, 0.89))
    for z in (1.5, 1.42, 1.34): put(mb, box(0.24, 0.02, 0.025, 0.006), 'teal2', (0, -0.225, z))
    put(mb, box(0.03, 0.04, 0.16, 0.01), 'brass', (0.14, -0.235, 0.95))
    put(mb, box(0.14, 0.012, 0.06, 0.004), 'cream2', (0, -0.225, 1.62))
    put(mb, box(0.47, 0.45, 0.04, 0.015), 'iron', (0, 0.01, 1.8))


def p_crate(mb, coll):
    """Walt's upturned fish box, used as a bedside table (fixed in every office tier)."""
    put(mb, box(0.40, 0.30, 0.04, 0.012), 'weath', (0, 0, 0.40))
    for z in (0.08, 0.2, 0.32):
        for sy in (-1, 1): put(mb, box(0.40, 0.03, 0.1, 0.01), 'weath' if z != 0.2 else 'greyw', (0, sy * 0.135, z))
        for sx in (-1, 1): put(mb, box(0.03, 0.27, 0.1, 0.01), 'weath', (sx * 0.185, 0, z))
    for sx in (-1, 1):
        for sy in (-1, 1): put(mb, box(0.05, 0.05, 0.4, 0.01), 'midwood', (sx * 0.175, sy * 0.125, 0.2))
    put(mb, box(0.2, 0.012, 0.07, 0.004), 'poppy', (0, -0.153, 0.2))


FURN = [('cot_fixed', p_cot, 'floor'), ('bunk', p_bunk, 'floor'), ('double', p_double, 'floor'), ('hammock', p_hammock, 'floor'),
        ('hammock_kestrel', p_hammock_kestrel, 'prop'), ('chair', p_chair, 'floor'), ('desk', p_desk, 'floor'),
        ('shelves', p_shelves, 'floor'), ('wardrobe', p_wardrobe, 'floor'), ('rug', p_rug, 'prop'), ('plant', p_plant, 'floor'),
        ('lamp', p_lamp, 'prop'), ('floorlamp', p_floorlamp, 'floor'), ('painting', p_painting, 'prop'),
        ('painting2', p_painting2, 'prop'), ('stove', p_stove, 'floor'), ('maptable', p_maptable, 'floor'),
        ('trophy', p_trophy, 'prop'), ('balcony', p_balcony, 'prop')]
FIXED = dict(locker=p_locker, crate=p_crate)       # modelled into the office tiers (and kept here for the layout check)


def build_furniture():
    for pid, fn, kind in FURN:
        ob, coll, lo, hi = make('PB_Prop_' + pid, fn, 'PB_Prop_%s.fbx' % pid, kind)
        FOOTS[pid] = tuple(round(v, 3) for v in (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]))
    for pid, fn in FIXED.items():          # measured for the layout only
        seed(5); m = MB(); fn(m, None); bm = m.bm; vs = [v.co for v in bm.verts]
        FOOTS[pid] = tuple(round(f(v[k] for v in vs), 3) for k in range(3) for f in (min, max)); bm.free()
    json.dump(FOOTS, open(os.path.join(OUT, 'props.json'), 'w'), indent=1)
    layout.FOOT.update({k: tuple(v) for k, v in FOOTS.items() if k in layout.FOOT})
    for t in layout.LAYOUT: layout.snap(t)


# ================================================================ DOCK ========================================================
# Dock props sit ON the pier deck: origin = deck top (z 0 = DECK 1.30 in the landing), front -Y.
def plank(mb, x0, x1, y, w, col, z=0.0, th=0.07, ragged=(0, 0), rot=(0, 0, 0)):
    bm = box(x1 - x0, w, th, 0.018)
    for v in bm.verts:                                  # splintered ends
        if ragged[0] and v.co.x < 0: v.co.x += J(ragged[0])
        if ragged[1] and v.co.x > 0: v.co.x += J(ragged[1])
    put(mb, bm, col, ((x0 + x1) / 2, y, z - th / 2), rot)


def sphere_cap(r):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=1, radius=r); return bm


def p_planks_broken(mb, coll):
    """A 2.9 m section of rotten deck: a dark gap where a board fell through, two cracked loose boards, a warning rag."""
    put(mb, box(2.2, 0.36, 0.012, 0.0), 'ink', (-0.1, 0.0, 0.004))                      # the hole (dark water below)
    put(mb, box(2.0, 0.26, 0.008, 0.0), 'navy', (-0.1, 0.0, 0.009))
    for y, x0, x1 in ((-0.36, -1.40, -0.25), (0.36, 0.3, 1.45)):                       # cracked boards either side
        plank(mb, x0, x1, y, 0.3, 'greyw', 0.012, 0.04, (0.0, 0.12))
    plank(mb, -1.45, -0.95, 0.0, 0.3, 'greyw', 0.012, 0.05, (0, 0.15))                    # stubs left in the gap
    plank(mb, 0.95, 1.45, 0.0, 0.3, 'weath', 0.012, 0.05, (0.15, 0))
    # two cracked loose boards: one propped on the stub, one lying askew
    plank(mb, -0.9, 0.55, 0.0, 0.3, 'weath', 0.07, 0.06, (0.1, 0.1), (0, D(-6), D(8)))
    put(mb, box(0.9, 0.29, 0.06, 0.018), 'greyw', (0.55, 0.62, 0.075), (D(4), 0, D(-14)))
    put(mb, box(0.55, 0.12, 0.06, 0.016), 'weath', (-0.5, 0.66, 0.045), (0, 0, D(-20)))
    for x, y in ((-0.6, 0.02), (0.05, 0.05), (0.3, 0.6)): put(mb, cyl(0.012, 0.07, 5, bevel=0), 'iron', (x, y, 0.16), (D(J(30)), D(J(30)), 0))
    # a poppy rag tied to a stick wedged in the gap: reads as "broken" from the game camera
    put(mb, box(0.05, 0.05, 1.0, 0.012), 'midwood', (-1.15, -0.05, 0.42), (D(6), D(-8), 0))
    mb.add(sheet_mesh(lambda u, v: (-1.1 + u, -0.06 + 0.04 * u, 0.86 - v - 0.06 * u * u), frange(0, 0.3, 0.1), frange(0, 0.2, 0.1), 0.012), 'poppy')
    col_box('Loose_colBox', (-0.95, -0.2, 0), (1.05, 0.85, 0.22), coll)


def p_planks_fixed(mb, coll):
    """Three fresh pale planks laid over the gap + the free bench (its seat faces +X on the deck's -X edge)."""
    for k, y in enumerate((-0.36, 0.0, 0.36)):
        plank(mb, -1.3 + J(0.02), 1.3 + J(0.02), y, 0.31, ('fresh', 'cream3', 'fresh')[k], 0.018, 0.05)
        for x in (-1.0, 0.0, 1.0):
            for dy in (-0.08, 0.08): put(mb, cyl(0.014, 0.012, 6, bevel=0), 'iron', (x, y + dy, 0.024))
    # the bench: fresh seat, chunky legs, a teal back rail; local (-0.96, 0) is spot_bench_planks (seat faces +X)
    bx_ = -0.96
    put(mb, box(0.38, 1.5, 0.07, 0.025, 2), 'fresh', (bx_, 0, 0.46))
    for dy in (-0.6, 0.6):
        for dx in (-0.12, 0.12): put(mb, box(0.08, 0.08, 0.43, 0.02), 'wood', (bx_ + dx, dy, 0.215))
        put(mb, box(0.32, 0.06, 0.05, 0.015), 'wood', (bx_, dy, 0.12))
        put(mb, box(0.07, 0.07, 0.5, 0.02), 'wood', (bx_ - 0.17, dy, 0.72), (0, D(-8), 0))
    put(mb, box(0.07, 1.58, 0.14, 0.025, 2), 'teal', (bx_ - 0.21, 0, 0.93), (0, D(-8), 0))
    put(mb, box(0.05, 1.5, 0.05, 0.015), 'cream2', (bx_ - 0.19, 0, 0.8), (0, D(-8), 0))
    empty('socket_bench', (bx_, 0, 0.5), coll, 90)
    col_box('Bench_colBox', (bx_ - 0.25, -0.78, 0), (bx_ + 0.19, 0.78, 0.5), coll)


def berth_pile(mb, fresh):
    """A berth pile just outside the deck edge at local (0, -0.35): sea bed (z -5.3) up past the deck."""
    if fresh:
        bm = cyl(0.17, 5.95, 8, bevel=0.035); zc = (-5.3 + 0.65) / 2; zsplit(bm, -0.85 - zc)
        mb.add(bm, lambda c, n: 'dark' if c.z < -0.85 else 'wood', M((0, -0.35, zc)))
        put(mb, cyl(0.19, 0.07, 8, bevel=0.025), 'midwood', (0, -0.35, 0.67))
        for q in range(2): mb.add(torus((0, -0.35, 0.36 + q * 0.07), 0.18, 0.026, 'Z', 10, 6), 'rope')
    else:                                                    # snapped below the deck, leaning out, jagged top
        L = 4.85; bm = cyl(0.15, L, 8, bevel=0.03); zsplit(bm, -L / 2 + 4.0)
        for v in bm.verts:
            if v.co.z > L / 2 - 0.01: v.co.z += 0.18 * math.sin(3.1 * math.atan2(v.co.y, v.co.x)) + J(0.05)
        mb.add(bm, lambda c, n: 'dark' if c.z < -L / 2 + 4.0 else 'greyw', M((0, -0.45, -5.3 + L / 2), (D(9), D(-4), 0)))
        put(mb, box(0.08, 0.06, 0.5, 0.02), 'greyw', (0.2, -0.9, -0.95), (D(40), D(10), 0))   # a split-off sliver


def p_berth_broken(mb, coll):
    berth_pile(mb, False)
    # the old mooring rope: tied to the deck edge, hanging into the water
    mb.add(sweep([(0.35, 0.02, -0.06), (0.4, -0.08, -0.4), (0.33, -0.12, -0.9), (0.42, -0.1, -1.4)], circle(0.024, 6)), 'rope')
    put(mb, box(0.3, 0.16, 0.05, 0.012), 'greyw', (0.6, 0.18, 0.025), (0, 0, D(14)))      # the cleat's empty plate
    for dx in (-0.08, 0.08): put(mb, cyl(0.014, 0.03, 5, bevel=0), 'rust', (0.6 + dx, 0.18, 0.06))
    col_box('Pile_colBox', (-0.25, -0.8, -1.3), (0.25, -0.15, -0.3), coll)
    empty('socket_moor', (0, -1.6, -1.30), coll)


def p_berth_fixed(mb, coll):
    berth_pile(mb, True)
    put(mb, box(0.1, 0.22, 0.06, 0.02), 'dark', (0.6, 0.22, 0.03), (0, 0, D(90)))            # cleat on the deck
    put(mb, box(0.09, 0.46, 0.065, 0.025, 2), 'iron', (0.6, 0.22, 0.09), (0, 0, D(90)))
    mb.add(torus((-0.62, -0.08, -0.55), 0.2, 0.075, 'Y', 12, 6), 'black')                    # a tyre fender on a rope
    rod(mb, (-0.62, -0.02, 0.0), (-0.62, -0.08, -0.33), 0.016, 'rope', 5)
    # the berth sign: a small double-sided board on a post off the pile top, text-free (the game writes "B2" / "B3")
    put(mb, box(0.07, 0.07, 0.62, 0.02), 'midwood', (0, -0.35, 0.95))
    put(mb, box(0.46, 0.06, 0.3, 0.025, 2), 'teal', (0, -0.35, 1.37))
    for s in (-1, 1): put(mb, box(0.38, 0.012, 0.22, 0.008), 'paper', (0, -0.35 + s * 0.031, 1.37))
    empty('socket_sign', (0, -0.35 - 0.038, 1.37), coll, 0)          # faces the water (-Y)
    empty('socket_sign_back', (0, -0.35 + 0.038, 1.37), coll, 180)   # faces the deck
    col_box('Pile_colBox', (-0.19, -0.54, -1.3), (0.19, -0.16, 1.6), coll)
    empty('socket_moor', (0, -1.6, -1.30), coll)


def p_docklamp(mb, coll):
    """2.6 m dock lamp: a teal post with a curly arm out over local -Y and a warm hanging lantern (panes = glow)."""
    put(mb, box(0.32, 0.32, 0.06, 0.02), 'iron', (0, 0, 0.03))
    for x in (-0.11, 0.11):
        for y in (-0.11, 0.11): put(mb, cyl(0.018, 0.02, 6, bevel=0), 'dark', (x, y, 0.07))
    mb.add(lathe([(0.0, 0.0), (0.095, 0.0), (0.09, 0.25), (0.06, 0.32), (0.055, 2.42), (0.075, 2.47), (0.0, 2.5)], 8), 'teal', M((0, 0, 0.06)))
    mb.add(torus((0, 0, 0.36), 0.07, 0.016, 'Z', 8, 4), 'cream2')
    mb.add(sweep([(0, 0.0, 2.38), (0, -0.25, 2.5), (0, -0.5, 2.48), (0, -0.62, 2.38)], circle(0.03, 6), up=(1, 0, 0)), 'teal')
    rod(mb, (0, 0.0, 2.1), (0, -0.32, 2.47), 0.018, 'teal')
    mb.add(sweep([(0, -0.1 + 0.09 * math.cos(D(a)), 2.2 + 0.09 * math.sin(D(a))) for a in range(-90, 181, 30)], circle(0.013, 5), up=(1, 0, 0)), 'teal')
    put(mb, sphere_cap(0.04), 'sun', (0, 0, 2.6))
    ly = -0.62
    rod(mb, (0, ly, 2.38), (0, ly, 2.28), 0.012, 'iron', 5)
    put(mb, cyl(0.17, 0.1, 8, r2=0.04, bevel=0.015), 'iron', (0, ly, 2.22))
    put(mb, cyl(0.03, 0.03, 6, bevel=0.008), 'iron', (0, ly, 2.285))
    put(mb, box(0.22, 0.22, 0.28, 0.04, 2), 'sun', (0, ly, 2.03), a=GLOW)
    for sx in (-1, 1):
        for sy in (-1, 1): put(mb, box(0.035, 0.035, 0.3, 0.008), 'iron', (sx * 0.11, ly + sy * 0.11, 2.03))
    put(mb, box(0.26, 0.26, 0.04, 0.012), 'iron', (0, ly, 1.88))
    put(mb, cyl(0.045, 0.05, 6, r2=0.015, bevel=0.008), 'iron', (0, ly, 1.835))
    col_box('Post_colBox', (-0.16, -0.16, 0), (0.16, 0.16, 2.5), coll)


def p_powerpost(mb, coll):
    """B1's shore-power post: teal box post, cream panel with two sockets + a meter, a coiled lead on a hook."""
    put(mb, box(0.36, 0.3, 0.06, 0.02), 'iron', (0, 0, 0.03))
    put(mb, box(0.3, 0.24, 1.15, 0.04, 2), 'teal', (0, 0, 0.635))
    put(mb, box(0.36, 0.3, 0.08, 0.03, 2), 'teal2', (0, 0, 1.25))
    put(mb, box(0.4, 0.34, 0.06, 0.02), 'cream2', (0, 0, 1.31))
    put(mb, box(0.24, 0.03, 0.44, 0.02), 'cream2', (0, -0.125, 0.86))
    for z in (0.95, 0.76):
        put(mb, cyl(0.045, 0.03, 8, bevel=0.008), 'iron', (0, -0.145, z), (D(90), 0, 0))
        put(mb, box(0.05, 0.012, 0.02, 0.004), 'sun', (0, -0.16, z + 0.065))
    put(mb, box(0.12, 0.02, 0.07, 0.01), 'ink', (0, -0.142, 1.04))
    put(mb, box(0.07, 0.012, 0.03, 0.004), 'leaf', (0, -0.153, 1.04), a=GLOW)              # the little "on" light
    put(mb, box(0.08, 0.03, 0.12, 0.015), 'sun', (0, -0.13, 0.48))
    put(mb, box(0.12, 0.03, 0.03, 0.01), 'iron', (0.19, 0, 0.9))
    for k in range(3): mb.add(torus((0.21 + k * 0.012, 0, 0.72 - k * 0.01), 0.15, 0.018, 'X', 12, 5), 'sun' if k != 1 else 'poppy')
    col_box('Post_colBox', (-0.18, -0.16, 0), (0.25, 0.16, 1.34), coll)


def p_tap(mb, coll):
    """B1's water tap: a wooden post, a brass tap with a blue wheel, a teal hose coiled on a hook, a drip bucket."""
    put(mb, box(0.32, 0.32, 0.05, 0.015), 'iron', (0, 0, 0.025))
    put(mb, box(0.16, 0.16, 0.98, 0.03, 2), 'wood', (0, 0, 0.54))
    put(mb, cyl(0.12, 0.06, 4, bevel=0.02), 'midwood', (0, 0, 1.06), (0, 0, D(45)))
    rod(mb, (0, -0.06, 0.82), (0, -0.16, 0.82), 0.025, 'brass')
    mb.add(sweep([(0, -0.16, 0.82), (0, -0.2, 0.8), (0, -0.21, 0.74)], circle(0.022, 6), up=(1, 0, 0)), 'brass')
    rod(mb, (0, -0.15, 0.82), (0, -0.15, 0.9), 0.012, 'brass', 5)
    mb.add(torus((0, -0.15, 0.91), 0.05, 0.011, 'Z', 8, 4), 'blue')
    for a in (0, 90): put(mb, box(0.1, 0.014, 0.014, 0.0), 'blue', (0, -0.15, 0.91), (0, 0, D(a)))
    put(mb, box(0.13, 0.03, 0.03, 0.01), 'iron', (0.1, 0, 0.66))
    for k in range(3): mb.add(torus((0.13 + k * 0.012, 0, 0.5 - k * 0.012), 0.16, 0.022, 'X', 12, 5), 'teal' if k != 1 else 'teal2')
    bm = cyl(0.11, 0.2, 10, r2=0.13, bevel=0.012); bm.normal_update()
    mb.add(bm, lambda c, n: 'ice' if n.z > 0.9 else 'blue', M((0.0, -0.24, 0.1)))
    col_box('Post_colBox', (-0.12, -0.38, 0), (0.33, 0.12, 1.1), coll)


DOCK = [('planks_broken', p_planks_broken), ('planks_fixed', p_planks_fixed), ('berth_broken', p_berth_broken),
        ('berth_fixed', p_berth_fixed), ('docklamp', p_docklamp), ('powerpost', p_powerpost), ('tap', p_tap)]


def build_dock():
    for pid, fn in DOCK:
        ob, coll, lo, hi = make('PB_Prop_' + pid, fn, 'PB_Prop_%s.fbx' % pid)
        FOOTS[pid] = tuple(round(v, 3) for v in (lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]))


# ================================================================ HATS ========================================================
# Head radius 0.13, origin = the head's centre, inner rim at z +0.03..+0.06, front -Y. Fabric faces alpha 239 (dye ch 1).
def shell(loop, n=14):
    """Revolve a CLOSED cross-section loop [(r, z)] about Z: a hat shell with thickness (no caps)."""
    bm = bmesh.new()
    rings = [[bm.verts.new((max(r, 1e-4) * math.cos(2 * math.pi * i / n), max(r, 1e-4) * math.sin(2 * math.pi * i / n), z)) for i in range(n)] for r, z in loop]
    for k in range(len(rings)):
        A, B = rings[k], rings[(k + 1) % len(rings)]
        for j in range(n): bm.faces.new((A[j], A[(j + 1) % n], B[(j + 1) % n], B[j]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def brim(r0, rfn, z0, slope, th=0.014, n=20, droop=None):
    """An annulus brim from radius r0 out to rfn(angle); z falls by slope per metre outward (+droop(angle, t))."""
    bm = bmesh.new(); m = 3
    def P(i, k, top):
        a = 2 * math.pi * i / n; r = r0 + (rfn(a) - r0) * k / (m - 1); t = k / (m - 1)
        z = z0 - slope * (r - r0) + (droop(a, t) if droop else 0) + (0 if top else -th)
        return (r * math.cos(a), r * math.sin(a), z)
    T = [[bm.verts.new(P(i, k, True)) for k in range(m)] for i in range(n)]
    B = [[bm.verts.new(P(i, k, False)) for k in range(m)] for i in range(n)]
    for i in range(n):
        j = (i + 1) % n
        for k in range(m - 1):
            bm.faces.new((T[i][k], T[i][k + 1], T[j][k + 1], T[j][k])); bm.faces.new((B[i][k], B[j][k], B[j][k + 1], B[i][k + 1]))
        bm.faces.new((T[i][-1], B[i][-1], B[j][-1], T[j][-1])); bm.faces.new((T[i][0], T[j][0], B[j][0], B[i][0]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def peak(r_in, depth, z, th=0.016, tilt=10, a0=-155, a1=-25, n=8):
    """A cap peak in front (-Y): inner edge on the band circle, outer edge an ellipse reaching depth in front."""
    inner = [(r_in * math.cos(D(a)), r_in * math.sin(D(a))) for a in frange(a1, a0, (a0 - a1) / n)]
    outer = [((r_in + 0.012) * math.cos(D(a)), -(r_in + depth) * abs(math.sin(D(a))) ** 1.2) for a in frange(a0, a1, (a1 - a0) / n)]
    bm = slab(inner + outer, -th / 2, th / 2, 0.005)
    bmesh.ops.transform(bm, matrix=M((0, 0, z), (D(tilt), 0, 0)) @ M((0, r_in * 0.0, 0)), verts=bm.verts)
    return bm


def head_loop(z0, z1, gap=0.008, nz=5):
    """Inner surface following the head sphere (r 0.13) from z1 down to z0 (for a shell loop's inside)."""
    return [(math.sqrt(max(0.13 ** 2 - z * z, 0)) + gap, z) for z in frange(z1, z0, (z0 - z1) / nz)]


def dome_loop(rim, z0, top, bulge=1.0, nz=7):
    """Outer dome from (rim, z0) up to (0, top)."""
    out = []
    for k in range(nz + 1):
        t = k / nz; a = t * math.pi / 2
        out.append((rim * math.cos(a) ** (0.8 / bulge), z0 + (top - z0) * math.sin(a)))
    return out


def h_beanie(mb, coll, col='poppy', bobble=False):
    put(mb, shell(dome_loop(0.148, 0.09, 0.232, 1.15) + [(0.0, 0.145)] + head_loop(0.09, 0.12), 14), col, a=DYE1)
    put(mb, shell([(0.150, 0.033), (0.158, 0.06), (0.155, 0.098), (0.136, 0.1), (0.132, 0.033)], 14), col, a=DYE1)
    if bobble:
        for z, r in ((0.13, 0.147), (0.165, 0.13)): mb.add(torus((0, 0, z), r, 0.009, 'Z', 14, 4), 'cream2')
        put(mb, blob(1, (0.058, 0.058, 0.052), 11, 0.18, 3.0), 'cream2', (0, 0, 0.262))
    else:
        put(mb, box(0.05, 0.012, 0.03, 0.004), 'cream2', (0.06, -0.153, 0.066), (0, 0, D(-22)))


def h_bobble(mb, coll): h_beanie(mb, coll, 'teal', True)


def h_flatcap(mb, coll):
    m = M((0, -0.012, 0), (D(-5), 0, 0))
    crown = [(0.150, 0.035), (0.158, 0.075), (0.162, 0.105), (0.152, 0.13), (0.11, 0.146), (0.05, 0.152), (0.0, 0.154), (0.0, 0.128)] + head_loop(0.035, 0.11, 0.01)[1:]
    mb.add(shell(crown, 14), 'midwood', m, DYE1)
    mb.add(peak(0.150, 0.075, 0.045, 0.016, 12, -150, -30), 'midwood', m, DYE1)
    mb.add(cyl(0.016, 0.012, 6, bevel=0.003), 'dark', m @ M((0, 0.0, 0.156)))
    for a in (-60, -20, 20, 60, 100, 140, 180, 220):                       # stitched panel seams
        mb.add(sweep([(0.153 * math.cos(D(a)), 0.153 * math.sin(D(a)), 0.13), (0.03 * math.cos(D(a)), 0.03 * math.sin(D(a)), 0.152)], circle(0.003, 3)), 'dark', m)


def h_souwester(mb, coll):
    crown = dome_loop(0.150, 0.045, 0.222, 0.9) + [(0.0, 0.14)] + head_loop(0.045, 0.11)[1:]
    put(mb, shell(crown, 14), 'gold', a=DYE1)
    put(mb, brim(0.146, lambda a: 0.215 + 0.075 * max(0, math.sin(a)) ** 1.5, 0.05, 0.42, 0.015, 22,
                 lambda a, t: -0.02 * t * max(0, math.sin(a))), 'gold', a=DYE1)
    mb.add(torus((0, 0, 0.05), 0.15, 0.008, 'Z', 14, 4), 'straw2')
    for s in (-1, 1):                                                      # chin ties
        rod(mb, (s * 0.145, -0.01, 0.04), (s * 0.12, -0.03, -0.12), 0.006, 'ink', 4)


def h_bucket(mb, coll):
    crown = [(0.150, 0.05), (0.146, 0.13), (0.134, 0.178), (0.07, 0.19), (0.0, 0.192), (0.0, 0.16), (0.12, 0.155)] + head_loop(0.05, 0.1)[1:]
    put(mb, shell(crown, 14), 'sky', a=DYE1)
    put(mb, brim(0.146, lambda a: 0.235, 0.058, 0.55, 0.013, 20), 'sky', a=DYE1)
    put(mb, cyl(0.153, 0.03, 14, bevel=0.006), 'cream2', (0, 0, 0.072))
    for r in (0.19, 0.218): mb.add(torus((0, 0, 0.058 - 0.55 * (r - 0.146) + 0.002), r, 0.003, 'Z', 18, 3), 'cream2')


def h_hardhat(mb, coll):
    crown = dome_loop(0.155, 0.045, 0.232, 1.0) + [(0.0, 0.205)] + head_loop(0.045, 0.12, 0.018)[1:]
    put(mb, shell(crown, 16), 'sun', a=DYE1)
    put(mb, brim(0.15, lambda a: 0.185 + 0.06 * max(0, -math.sin(a)) ** 2, 0.052, 0.12, 0.016, 22), 'sun', a=DYE1)
    mb.add(sweep([(0, 0.15 * math.cos(D(a)), 0.045 + 0.188 * math.sin(D(a))) for a in range(20, 161, 20)], rrect(-0.02, 0.02, -0.01, 0.012, 0.006, (1, 1, 1, 1)), up=(1, 0, 0)), 'sun', None, DYE1)
    mb.add(torus((0, 0, 0.06), 0.135, 0.008, 'Z', 14, 4), 'ink')            # the inner head strap
    put(mb, slab(rrect(-0.035, 0.035, -0.022, 0.022, 0.018, (2, 2, 2, 2)), 0, 0.006, 0), 'cream2', (0, -0.142, 0.13), (D(-70), 0, 0))


def anchor(mb, y, cx, cz, s, col):
    """A little flat anchor badge facing -Y at (cx, y, cz)."""
    def F(poly): mb.add(flat([(cx + a * s, cz + b * s) for a, b in poly], y, 0.006, 0.0), col)
    F([(-0.006, -0.03), (0.006, -0.03), (0.006, 0.026), (-0.006, 0.026)])
    F([(-0.022, 0.012), (0.022, 0.012), (0.022, 0.02), (-0.022, 0.02)])
    F(poly_circle(0, 0.034, 0.01, 8))
    F([(-0.032, -0.012), (-0.024, -0.016), (-0.012, -0.03), (0.012, -0.03), (0.024, -0.016), (0.032, -0.012), (0.022, -0.036), (-0.022, -0.036)])


def cap_top(top_col, band_col, peak_col, badge_col, cord_col, tilt=4, sag=0.0):
    def fn(mb, coll):
        put(mb, shell([(0.148, 0.035), (0.150, 0.092), (0.132, 0.092), (0.132, 0.035)], 16), band_col)
        top = [(0.142, 0.09), (0.158, 0.12), (0.176, 0.152), (0.178 - sag * 0.2, 0.172 - sag), (0.14, 0.184 - sag), (0.07, 0.19 - sag), (0.0, 0.192 - sag),
               (0.0, 0.17 - sag), (0.12, 0.166 - sag), (0.135, 0.095)]
        mb.add(shell(top, 16), top_col, M((0, 0.008, 0), (D(tilt), 0, 0)), DYE1)
        put(mb, peak(0.148, 0.06, 0.045, 0.014, 4, -145, -35), peak_col)
        mb.add(sweep([(0.153 * math.cos(D(a)), 0.153 * math.sin(D(a)), 0.052) for a in range(-150, -29, 15)], circle(0.007, 5)), cord_col)
        for s in (-1, 1): put(mb, cyl(0.011, 0.008, 6, bevel=0.002), cord_col, (s * 0.132, -0.077, 0.052), (0, D(s * 60), D(s * 30)))
        anchor(mb, -0.162, 0, 0.068, 0.72, badge_col)
        if sag:                                                            # Walt's: a cream patch, a frayed cord end
            put(mb, slab(rrect(-0.03, 0.03, -0.022, 0.022, 0.006, (1, 1, 1, 1)), 0, 0.006, 0), 'cream3', (0.12, 0.06, 0.168), (D(-12), D(38), 0))
            rod(mb, (0.132, -0.077, 0.052), (0.15, -0.08, 0.015), 0.005, cord_col, 4)
    return fn


def h_straw(mb, coll):
    crown = [(0.147, 0.04), (0.149, 0.125), (0.143, 0.148), (0.10, 0.152), (0.05, 0.145), (0.0, 0.148), (0.0, 0.125)] + head_loop(0.04, 0.11)[1:]
    put(mb, shell(crown, 14), 'straw')
    put(mb, brim(0.145, lambda a: 0.29, 0.045, 0.1, 0.012, 22, lambda a, t: -0.025 * t * t * (1 + 0.5 * math.sin(2 * a))), 'straw')
    for r in (0.19, 0.24, 0.285): mb.add(torus((0, 0, 0.046 - 0.1 * (r - 0.145) - 0.025 * ((r - 0.145) / 0.145) ** 2 + 0.003), r, 0.004, 'Z', 20, 3), 'straw2')
    put(mb, shell([(0.152, 0.045), (0.153, 0.085), (0.146, 0.085), (0.146, 0.045)], 14), 'poppy', a=DYE1)     # the dyeable ribbon
    put(mb, blob(1, (0.03, 0.02, 0.025), 7, 0.1), 'poppy', (0.13, 0.08, 0.065), (0, 0, D(60)), a=DYE1)
    put(mb, box(0.012, 0.03, 0.07, 0.004), 'poppy', (0.15, 0.1, 0.03), (0, D(20), D(60)), a=DYE1)


HATS = [('beanie', h_beanie), ('flatcap', h_flatcap), ('souwester', h_souwester), ('bucket', h_bucket), ('hardhat', h_hardhat),
        ('captain', cap_top('paper', 'black', 'black', 'gold', 'gold')), ('straw', h_straw), ('bobble', h_bobble),
        ('walt', cap_top('navy2', 'navy', 'ink', 'brass', 'rope', -3, 0.012))]


def build_hats():
    for hid, fn in HATS: make('PB_Hat_' + hid, fn, 'PB_Hat_%s.fbx' % hid)


# ================================================================ BUILDING KIT ================================================
def vb(x0, x1, y0, y1, z0, ztop, lean=(0, 0), w=0.012):
    """A wall board: only its vertical edges are bevelled (the seams read; half the tris of a fully bevelled board)."""
    bm = board(x0, x1, y0, y1, z0, ztop, 0.0, 1, lean)
    bev_edges(bm, [e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 0.05], min(w, 0.3 * min(x1 - x0, y1 - y0)), 1)
    return bm


def hb(sx, sy, sz, loc, w=0.012):
    """A floor plank: only its top long edges are bevelled."""
    bm = box(sx, sy, sz, 0)
    bev_edges(bm, [e for e in bm.edges if all(v.co.z > 0 for v in e.verts) and abs(e.verts[0].co.x - e.verts[1].co.x) > 0.05], w, 1)
    bmesh.ops.translate(bm, vec=loc, verts=bm.verts); return bm


def bwall(mb, side, c0, c1, t0, t1, z0, ztop, holes=(), bw=0.31, cols=(('weath', 0.65), ('greyw', 1.0)), skip=(), lean=0.015, bev=0.012):
    """A wall of vertical boards. side 'F'/'B': boards along X (c0..c1), thickness y t0..t1; 'L'/'R': along Y, x t0..t1.
    ztop: a number or fn(u). holes [(a0, a1, h0, h1)] are cut out (door / window openings). skip: board indices left out."""
    n = max(1, int(round((c1 - c0) / bw))); w = (c1 - c0) / n
    for i in range(n):
        if i in skip: continue
        u0, u1 = c0 + i * w + 0.006, c0 + (i + 1) * w - 0.006
        r = R.random(); col = next(c for c, p in cols if r <= p)
        cuts = sorted({u0, u1} | {a for h in holes for a in h[:2] if u0 < a < u1})
        ln = J(lean)
        for a, b in zip(cuts, cuts[1:]):
            m = (a + b) / 2; zt = ztop(m) if callable(ztop) else ztop
            pieces = [(z0, zt)]
            for h in holes:
                if h[0] <= m <= h[1]:
                    pieces = [q for p in pieces for q in ((p[0], min(p[1], h[2])), (max(p[0], h[3]), p[1])) if q[1] - q[0] > 0.02]
            for pz0, pz1 in pieces:
                top = (lambda X, Y, pz1=pz1: (ztop(X if side in 'FB' else Y) if callable(ztop) and pz1 == zt else pz1) + J(0.012))
                if side in 'FB': mb.add(vb(a, b, t0, t1, pz0, top, (ln, 0), bev), col)
                else: mb.add(vb(t0, t1, a, b, pz0, top, (0, ln), bev), col)


def wall_mat(side, u, wall):
    """Frame for a wall feature: local u along the wall, -Y local = outward. wall = outer face coordinate."""
    if side == 'F': return M((u, wall, 0), (0, 0, 0))
    if side == 'B': return M((u, wall, 0), (0, 0, D(180)))
    if side == 'L': return M((wall, u, 0), (0, 0, D(-90)))
    return M((wall, u, 0), (0, 0, D(90)))


def window(mb, side, a0, a1, z0, z1, wall, th=0.06, frame='cream2', glass='sky', shutters=None, bev=0.02):
    """Cream-framed window in a board wall: glass in the middle of the wall, frames both faces, sill outside."""
    uc = (a0 + a1) / 2; w = a1 - a0; m = wall_mat(side, uc, wall)
    zc = (z0 + z1) / 2; h = z1 - z0
    mb.add(box(w, 0.025, h, 0.0), glass, m @ M((0, th / 2, zc)))
    for z in (z0, z1): mb.add(box(w + 0.12, th + 0.1, 0.07, bev), frame, m @ M((0, th / 2, z)))            # one frame through the wall
    for s in (-1, 1): mb.add(box(0.07, th + 0.1, h, bev), frame, m @ M((s * w / 2, th / 2, zc)))
    mb.add(box(0.045, 0.05, h, bev * 0.6), frame, m @ M((0, th / 2, zc)))
    mb.add(box(w, 0.05, 0.045, bev * 0.6), frame, m @ M((0, th / 2, zc)))
    mb.add(box(w + 0.2, 0.16, 0.05, bev), 'wood', m @ M((0, -0.07, z0 - 0.05)))
    if shutters:
        for s in (-1, 1):
            mb.add(box(w / 2 - 0.01, 0.035, h + 0.06, bev * 0.6), shutters, m @ M((s * (w * 0.75 + 0.08), -0.03, zc)))


def door_frame(mb, side, a0, a1, z1, wall, z0=0.0, col='wood'):
    m = wall_mat(side, (a0 + a1) / 2, wall); w = a1 - a0
    for s in (-1, 1): mb.add(box(0.08, 0.1, z1 - z0 + 0.06, 0.02), col, m @ M((s * (w / 2 + 0.04), 0.02, (z0 + z1) / 2 + 0.03)))
    mb.add(box(w + 0.16, 0.1, 0.08, 0.02), col, m @ M((0, 0.02, z1 + 0.04)))


def door_leaf(mb, hinge, w, h, ang, z0=0.0, col='teal', brace='cream2', side_sign=1, knob='iron'):
    """Plank door hinged at `hinge` (x, y), closed position runs from the hinge toward -X*side_sign; ang opens it (deg)."""
    m = M((hinge[0], hinge[1], z0), (0, 0, D(ang)))
    for q in range(3):
        mb.add(box(w / 3 - 0.01, 0.05, h - 0.04, 0.012), col, m @ M((-side_sign * (q + 0.5) * w / 3, 0, h / 2)))
    for z in (0.35, h - 0.35): mb.add(box(w - 0.08, 0.04, 0.12, 0.015), brace, m @ M((-side_sign * w / 2, -0.04, z)))
    dl = math.hypot(w - 0.2, h - 0.7)
    mb.add(box(dl, 0.04, 0.11, 0.015), brace, m @ M((-side_sign * w / 2, -0.04, h / 2), (0, side_sign * math.atan2(h - 0.7, w - 0.2), 0)))
    mb.add(cyl(0.035, 0.06, 6, bevel=0.01), knob, m @ M((-side_sign * (w - 0.12), -0.06, h / 2), (D(90), 0, 0)))
    c = [m @ Vector((0, -0.03, 0)), m @ Vector((-side_sign * w, -0.03, 0)), m @ Vector((0, 0.03, 0)), m @ Vector((-side_sign * w, 0.03, 0))]
    return (min(v.x for v in c), min(v.y for v in c)), (max(v.x for v in c), max(v.y for v in c))


def gable_roof(mb, x0, x1, HD, EAVE, RIDGE, OV=0.4, cols=('poppy', 'cream2', 'peach'), sheet_w=0.98, sag=None, skip=(), patch=None, xov=0.35, yc=0.0, waves=5):
    """Corrugated gable roof, ridge along X at y = yc. sag(x, y) -> dz bends the sheets (an old roof). skip: sheet indices."""
    th = math.atan2(RIDGE - EAVE, HD); l = (HD + OV) / math.cos(th) + 0.06
    n = max(2, int(round((x1 - x0 + 2 * xov) / sheet_w))); sw = (x1 - x0 + 2 * xov) / n; k = 0
    for side in (-1, 1):
        ye = yc + side * (HD + OV); ze = EAVE - OV * math.tan(th) + 0.05
        for q in range(n):
            k += 1
            if k - 1 in skip: continue
            xc = x0 - xov + (q + 0.5) * sw
            rot = Matrix.Rotation(D(180), 4, 'Z') if side > 0 else Matrix.Identity(4)
            m = Matrix.Translation((xc + J(0.03), ye, ze + 0.012 * (q % 3))) @ rot @ Matrix.Rotation(th, 4, 'X') @ Matrix.Rotation(D(J(1.5)), 4, 'Z')
            bm = corr_sheet(sw + 0.02, l, waves); bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
            if sag:
                for v in bm.verts: v.co.z += sag(v.co.x, v.co.y)
            mb.add(bm, cols[(k - 1) % len(cols)])
    mb.add(sweep([(x0 - xov - 0.05, yc, RIDGE + 0.06), (x1 + xov + 0.05, yc, RIDGE + 0.06)] if not sag else
                 [(x, yc, RIDGE + 0.06 + sag(x, yc)) for x in frange(x0 - xov - 0.05, x1 + xov + 0.05, (x1 - x0 + 2 * xov + 0.1) / 6)],
                 [(-0.2, -0.08), (0.0, 0.05), (0.2, -0.08), (0.2, -0.11), (0.0, 0.01), (-0.2, -0.11)], up=(0, 1, 0)), 'iron')
    return lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y - yc) / HD)


def floor_planks(mb, x0, x1, y0, y1, z=0.0, cols=(('weath', 0.6), ('wood', 0.85), ('greyw', 1.0)), w=0.3, th=0.05):
    n = max(1, int(round((y1 - y0) / w))); pw = (y1 - y0) / n
    for i in range(n):
        r = R.random(); col = next(c for c, p in cols if r <= p)
        mb.add(hb(x1 - x0 - 0.01, pw - 0.012, th, ((x0 + x1) / 2, y0 + (i + 0.5) * pw, z - th / 2 + J(0.003))), col)


def footings(mb, x0, x1, y0, y1, ztop, g, nx=3, ny=2):
    for i in range(nx + 1):
        for j in range(ny + 1):
            if 0 < i < nx and 0 < j < ny: continue
            x = x0 + (x1 - x0) * i / nx; y = y0 + (y1 - y0) * j / ny
            put(mb, blob(0, (0.26, 0.22, 0.3), int(x * 13 + y * 7) + 300, 0.1), 'rock2', (x, y, g + 0.05))
    put(mb, box(x1 - x0 + 0.2, y1 - y0 + 0.2, ztop - g + 0.06, 0.03, 2), 'greyw', ((x0 + x1) / 2, (y0 + y1) / 2, (ztop + g) / 2 - 0.03))


def corner_posts(mb, x0, x1, y0, y1, z0, z1, col='dark'):
    for x in (x0 + 0.045, x1 - 0.045):
        for y in (y0 + 0.045, y1 - 0.045): put(mb, box(0.13, 0.13, z1 - z0, 0.025, 2), col, (x, y, (z0 + z1) / 2))


def gull(mb, loc, yaw, sit=True):
    m = M(loc, (0, 0, D(yaw)))
    mb.add(blob(1, (0.16, 0.09, 0.09), 400, 0.06), 'paper', m @ M((0, 0, 0.1)))
    mb.add(blob(1, (0.065, 0.06, 0.065), 401, 0.04), 'paper', m @ M((0.13, 0, 0.19)))
    mb.add(cyl(0.018, 0.07, 5, r2=0.006, bevel=0), 'sun', m @ M((0.21, 0, 0.18), (0, D(95), 0)))
    for s in (-1, 1):
        mb.add(box(0.2, 0.025, 0.07, 0.02), 'grey2', m @ M((-0.03, s * 0.085, 0.13), (D(s * 12), 0, 0)))
        mb.add(box(0.02, 0.012, 0.012, 0.0), 'ink', m @ M((0.17, s * 0.045, 0.21)))
    mb.add(box(0.1, 0.06, 0.03, 0.01), 'grey2', m @ M((-0.17, 0, 0.12), (0, D(-15), 0)))
    if not sit:
        for s in (-1, 1): mb.add(cyl(0.008, 0.06, 4, bevel=0), 'sun', m @ M((0.0, s * 0.03, 0.02)))


def lamp_box(mb, loc, glow=True, broken=False, col='sun'):
    """A wall bracket lamp hung off a wall face (loc = the lamp's centre); broken = cocked, dark, no glow."""
    x, y, z = loc
    rod(mb, (x, y + 0.25, z + 0.22), (x, y, z + 0.22), 0.014, 'iron', 5)
    rod(mb, (x, y, z + 0.22), (x, y, z + 0.12), 0.01, 'iron', 5)
    tilt = (D(28), 0, D(15)) if broken else (0, 0, 0)
    put(mb, cyl(0.12, 0.07, 8, r2=0.03, bevel=0.012), 'iron', (x, y, z + 0.09), tilt)
    put(mb, box(0.14, 0.14, 0.17, 0.03, 2), 'greyw' if broken else col, (x, y + (0.03 if broken else 0), z - 0.02), tilt, a=GLOW if glow and not broken else 255)
    put(mb, box(0.16, 0.16, 0.03, 0.01), 'iron', (x, y + (0.05 if broken else 0), z - 0.115), tilt)


def add_fixed(mb, fn, loc, yaw):
    t = MB(); fn(t, None); mb.add(t.bm, None, M(loc, (0, 0, D(yaw))))


# ================================================================ WAREHOUSES ==================================================
# ~6 x 4.5 m, origin = floor centre (floor top z 0, ground ~ -0.15), the big door opening faces -Y, walkable inside.
WX, WY, WT = 3.0, 2.25, 0.06


def wh_shell(mb, coll, tier):
    old = tier == 0
    EAVE, RIDGE = 2.7, 3.8
    footings(mb, -WX, WX, -WY, WY, 0.0, -0.15, 3, 2)
    floor_planks(mb, -WX + 0.02, WX - 0.02, -WY + 0.02, WY - 0.02, 0.0,
                 (('greyw', 0.5), ('weath', 0.85), ('dark', 1.0)) if old else (('fresh', 0.5), ('wood', 0.8), ('weath', 1.0)))
    put(mb, board(-1.3, 1.3, -WY - 0.65, -WY, -0.17, lambda x, y: -0.15 + 0.15 * (y + WY + 0.65) / 0.65, 0.02), 'weath' if old else 'wood')
    cols = (('weath', 0.45), ('wood', 0.65), ('greyw', 0.88), ('dark', 1.0)) if old else (('fresh', 0.45), ('wood', 0.75), ('weath', 1.0))
    sag = (lambda x, y: -0.22 * math.cos(math.pi * x / (2 * WX + 0.8)) * (1 - 0.6 * abs(y) / (WY + 0.4))) if old else None
    roof_z = lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y) / WY)
    door = (-1.2, 1.2, 0.0, 2.4)
    win = [] if old else [('L', -0.5, 0.5, 1.95, 2.45), ('R', -0.5, 0.5, 1.95, 2.45)]
    bev = 0.012 if old else 0.0
    bwall(mb, 'F', -WX, WX, -WY, -WY + WT, 0.0, EAVE, [door], 0.32, cols, skip={14, 16} if old else (), bev=bev)
    bwall(mb, 'B', -WX, WX, WY - WT, WY, 0.0, EAVE, [], 0.32, cols, skip={4, 11} if old else (), bev=bev)
    for side, xs in (('L', -WX), ('R', WX - WT)):
        holes = [(w[1], w[2], w[3], w[4]) for w in win if w[0] == side]
        bwall(mb, side, -WY, WY, xs, xs + WT, 0.0, lambda u: roof_z(u) - 0.06 + (sag(xs, u) if sag else 0), holes, 0.32, cols,
              skip=({3, 9} if side == 'L' else {6}) if old else (), bev=bev)
        for w in win:
            if w[0] == side: window(mb, side, w[1], w[2], w[3], w[4], xs if side == 'L' else xs + WT, WT)
    corner_posts(mb, -WX, WX, -WY, WY, 0.0, EAVE)
    door_frame(mb, 'F', -1.2, 1.2, 2.4, -WY, 0.0, 'dark' if old else 'midwood')
    # tie beams + a ridge beam inside (also what you see through the missing boards)
    for x in (-1.5, 0.0, 1.5):
        put(mb, box(0.1, 2 * WY - 0.1, 0.14, 0.02), 'midwood', (x, 0, EAVE - 0.07 + (sag(x, 0) * 0.5 if sag else 0)))
    roof_cols = ('greyw', 'rust', 'terra', 'greyw', 'rust') if old else ('poppy', 'cream2', 'poppy', 'cream2', 'peach') if tier == 1 else ('cblue', 'cream2', 'sky', 'cream2', 'cblue')
    gable_roof(mb, -WX, WX, WY, EAVE, RIDGE, 0.35, roof_cols, 1.0, sag, skip={2} if old else (), xov=0.3)
    if old:
        for x in (-0.9, -0.3):                              # rafters showing through the missing sheet
            put(mb, box(0.08, WY + 0.2, 0.1, 0.02), 'midwood', (x - 0.6, -WY / 2 - 0.1, (EAVE + RIDGE) / 2 - 0.08), (math.atan2(RIDGE - EAVE, WY), 0, 0))
    # colliders + sockets (shared by every tier)
    col_box('Floor_colDeck', (-WX, -WY, -0.3), (WX, WY, 0.0), coll)
    col_box('Back_colBox', (-WX, WY - WT, 0), (WX, WY, RIDGE), coll)
    col_box('Left_colBox', (-WX, -WY, 0), (-WX + WT, WY, RIDGE), coll)
    col_box('Right_colBox', (WX - WT, -WY, 0), (WX, WY, RIDGE), coll)
    col_box('FrontL_colBox', (-WX, -WY, 0), (-1.25, -WY + WT, RIDGE), coll)
    col_box('FrontR_colBox', (1.25, -WY, 0), (WX, -WY + WT, RIDGE), coll)
    col_box('FrontTop_colBox', (-1.25, -WY, 2.4), (1.25, -WY + WT, RIDGE), coll)
    empty('socket_door', (0, -WY - 0.6, -0.15), coll, 0)
    empty('socket_store', (0, -0.1, 0), coll, 180)
    return EAVE, RIDGE, roof_z, sag


def rack(mb, coll, name, x0, x1, y0, y1, h=1.8, items=True):
    """Wall rack: posts, three shelves, baskets / buckets / floats / rope on it."""
    for x in (x0 + 0.03, x1 - 0.03):
        for y in (y0 + 0.03, y1 - 0.03): put(mb, box(0.06, 0.06, h, 0.015), 'midwood', (x, y, h / 2))
    along_x = (x1 - x0) > (y1 - y0)
    for z in (0.12, 0.72, 1.32, h - 0.03):
        put(mb, box(x1 - x0, y1 - y0, 0.04, 0.012), 'wood', ((x0 + x1) / 2, (y0 + y1) / 2, z))
    if items:
        L = (x1 - x0) if along_x else (y1 - y0); n = int(L / 0.42)
        for zi, z in enumerate((0.14, 0.74, 1.34)):
            for k in range(n):
                if (k + zi) % 2: continue                   # every other slot is left free (room for your catch)
                t = (k + 0.5) / n; px = x0 + t * (x1 - x0) if along_x else (x0 + x1) / 2; py = (y0 + y1) / 2 if along_x else y0 + t * (y1 - y0)
                kind = (k * 3 + zi * 5) % 6
                if kind in (0, 3):    # a wicker basket
                    put(mb, cyl(0.15, 0.2, 8, r2=0.17, bevel=0.02), 'straw2', (px, py, z + 0.1))
                    mb.add(torus((px, py, z + 0.2), 0.17, 0.015, 'Z', 8, 4), 'straw')
                elif kind == 1:       # a bucket
                    bm = cyl(0.12, 0.22, 8, r2=0.14, bevel=0.012); bm.normal_update()
                    mb.add(bm, lambda c, nn: 'ice' if nn.z > 0.9 else ('teal', 'poppy', 'sun')[(k + zi) % 3], M((px, py, z + 0.11)))
                elif kind == 2:       # a rope coil
                    coil(mb, (px, py, z), 0.13, 2, 0.022)
                elif kind == 4:       # floats
                    for q in range(2): put(mb, blob(1, (0.08, 0.08, 0.08), 70 + k + q, 0.05), ('poppy', 'sun')[q], (px + (q - 0.5) * 0.15 * along_x, py + (q - 0.5) * 0.15 * (not along_x), z + 0.08))
                # kind 5: left empty (room to store your catch)
    col_box(name + '_colBox', (x0, y0, 0), (x1, y1, h), coll)


def wh_build(tier):
    def fn(mb, coll):
        EAVE, RIDGE, roof_z, sag = wh_shell(mb, coll, tier)
        if tier == 0:
            # one door leaf left, leaning against the wall beside the opening; one working lamp, one broken
            put(mb, box(1.15, 0.05, 2.3, 0.015), 'greyw', (-1.95, -WY - 0.16, 1.12), (D(-4), 0, D(2)))
            for x in (-2.3, -1.95, -1.6): put(mb, box(0.3, 0.04, 2.25, 0.012), 'teal2' if x != -1.95 else 'greyw', (x, -WY - 0.2, 1.12), (D(-4), 0, D(2)))
            put(mb, box(1.0, 0.04, 0.12, 0.015), 'weath', (-1.95, -WY - 0.24, 1.7), (D(-4), 0, D(2)))
            lamp_box(mb, (0.0, -WY - 0.28, 2.6), True)
            lamp_box(mb, (WX - 0.6, -WY - 0.28, 2.3), False, True)
            for x, y, a in ((-2.6, -1.0, 75), (-2.65, 0.5, -5)): put(mb, box(0.3, 0.04, 1.8, 0.012), 'greyw', (x, y, 0.9), (0, D(14), D(a)))   # fallen boards inside
            put(mb, cyl(0.28, 0.85, 10, bevel=0.03), 'rust', (2.3, 1.6, 0.425))                                         # an old barrel
            for z in (0.15, 0.7): mb.add(torus((2.3, 1.6, z), 0.285, 0.02, 'Z', 10, 4), 'iron')
            # gulls nesting on the ridge
            rz = RIDGE + 0.12
            put(mb, torus((0.9, 0, rz + sag(0.9, 0)), 0.17, 0.06, 'Z', 9, 5), 'straw2')
            for q in range(2): put(mb, blob(1, (0.045, 0.035, 0.035), 500 + q, 0.05), 'cream3', (0.86 + q * 0.07, 0.02, rz + 0.04 + sag(0.9, 0)))
            gull(mb, (0.9, 0.0, rz + 0.02 + sag(0.9, 0)), 160)
            gull(mb, (-1.6, 0.03, rz - 0.03 + sag(-1.6, 0)), -20, False)
            gull(mb, (2.4, 0.0, rz - 0.06 + sag(2.4, 0)), 200, False)
            for x, y in ((1.4, -0.6), (-0.4, 0.8), (2.0, 0.6)): put(mb, blob(0, (0.05, 0.04, 0.01), 600 + int(x * 10), 0.1), 'paper', (x, y, roof_z(y) + 0.06 + sag(x, y)))
            return
        # tier 1/2: teal sliding doors slid open, a rail, lamps, racks
        put(mb, box(5.6, 0.08, 0.1, 0.02), 'iron', (0, -WY - 0.12, 2.5))
        for s in (-1, 1):
            cx = s * 1.95
            for q in range(3): put(mb, box(0.4, 0.05, 2.36, 0.012), 'teal', (cx + (q - 1) * 0.41, -WY - 0.12, 1.2))
            for z in (0.45, 1.95): put(mb, box(1.2, 0.04, 0.12, 0.015), 'cream2', (cx, -WY - 0.16, z))
            dl = math.hypot(1.0, 1.4); put(mb, box(dl, 0.04, 0.11, 0.015), 'cream2', (cx, -WY - 0.16, 1.2), (0, s * math.atan2(1.4, 1.0), 0))
            for x in (cx - 0.4, cx + 0.4): put(mb, cyl(0.04, 0.05, 6, bevel=0.01), 'iron', (x, -WY - 0.12, 2.43), (D(90), 0, 0))
            put(mb, box(0.05, 0.06, 0.3, 0.015), 'iron', (cx - s * 0.55, -WY - 0.19, 1.1))
        lamp_box(mb, (0.0, -WY - 0.28, 2.62), True)
        put(mb, cyl(0.01, 0.4, 4, bevel=0), 'iron', (0, 0.0 if tier == 1 else -0.9, EAVE - 0.34))
        put(mb, cyl(0.16, 0.1, 8, r2=0.05, bevel=0.012), 'iron', (0, 0.0 if tier == 1 else -0.9, EAVE - 0.6))
        put(mb, blob(1, (0.06, 0.06, 0.06), 9, 0), 'sun', (0, 0.0 if tier == 1 else -0.9, EAVE - 0.68), a=GLOW)
        if tier == 1:
            rack(mb, coll, 'RackB', -2.4, 2.4, 1.75, WY - WT - 0.01)
            rack(mb, coll, 'RackL', -WX + WT + 0.01, -WX + 0.48, -1.5, 1.45)
            rack(mb, coll, 'RackR', WX - 0.48, WX - WT - 0.01, -1.5, 1.45)
            return
        # tier 2: the insulated cold room at the back (pale panels, frosty heavy door, a little blue light, a chiller)
        y0, y1 = 0.6, 0.72; zt = roof_z(y1) - 0.06
        n = 8; pw = (2 * (WX - WT)) / n
        for i in range(n):
            xa = -WX + WT + i * pw
            if -0.7 < xa + pw / 2 < 0.7: continue
            put(mb, board(xa + 0.01, xa + pw - 0.01, y0, y1, 0.0, lambda X, Y: roof_z(Y) - 0.07, 0.02), 'frost' if i % 2 else 'cream2')
        put(mb, box(1.5, y1 - y0, zt - 2.05, 0.02), 'frost', (0, (y0 + y1) / 2, (2.05 + zt) / 2 - 0.05))
        for s in (-1, 1): put(mb, box(0.12, 0.16, 2.1, 0.03), 'ice', (s * 0.68, (y0 + y1) / 2 - 0.02, 1.05))
        put(mb, box(1.5, 0.16, 0.12, 0.03), 'ice', (0, (y0 + y1) / 2 - 0.02, 2.08))
        put(mb, box(1.24, 0.16, 2.0, 0.06, 2), 'frost', (0, y0 - 0.04, 1.0))                      # the heavy door, shut
        put(mb, box(1.08, 0.04, 1.84, 0.03), 'ice', (0, y0 - 0.13, 1.0))
        for z in (0.45, 1.55):
            put(mb, box(0.2, 0.08, 0.14, 0.03), 'iron', (-0.5, y0 - 0.14, z))
        put(mb, box(0.07, 0.1, 0.6, 0.02), 'iron', (0.38, y0 - 0.18, 1.05))                       # the big lever handle
        put(mb, box(0.2, 0.06, 0.08, 0.02), 'iron', (0.38, y0 - 0.15, 1.05))
        put(mb, cyl(0.05, 0.08, 8, bevel=0.015), 'brass', (0.38, y0 - 0.25, 1.33), (D(90), 0, 0))
        for k in range(7): put(mb, blob(0, (0.06, 0.02, 0.03), 700 + k, 0.2), 'paper', (-0.5 + k * 0.16, y0 - 0.15, 1.98 - 0.02 * (k % 2)))
        put(mb, box(0.16, 0.08, 0.12, 0.02), 'iron', (0, y0 - 0.06, 2.3))                         # the little blue light
        put(mb, box(0.11, 0.07, 0.08, 0.02), 'cblue', (0, y0 - 0.1, 2.3), a=GLOW)
        for s in (-1, 1): put(mb, box(0.012, 0.012, 0.11, 0.0), 'iron', (s * 0.04, y0 - 0.14, 2.3))
        # the chiller unit outside on the +X wall
        cx = WX + 0.25
        put(mb, box(0.42, 1.0, 0.75, 0.04, 2), 'grey2', (cx, 1.35, 0.55))
        mb.add(torus((cx + 0.21, 1.35, 0.58), 0.25, 0.03, 'X', 12, 4), 'iron')
        for a in (0, 60, 120): put(mb, box(0.02, 0.44, 0.06, 0.01), 'iron', (cx + 0.22, 1.35, 0.58), (D(a), 0, 0))
        for y in (0.95, 1.75): put(mb, box(0.32, 0.06, 0.2, 0.02), 'iron', (cx, y, 0.08))
        rod(mb, (cx - 0.1, 1.0, 0.85), (cx - 0.1, 1.0, 1.6), 0.025, 'iron')
        rack(mb, coll, 'RackL', -WX + WT + 0.01, -WX + 0.48, -1.5, 0.45)
        rack(mb, coll, 'RackR', WX - 0.48, WX - WT - 0.01, -1.5, 0.45)
        for x in (-2.0, 2.0):                                                                  # ice-blue crates inside the cold room
            for q in range(2): put(mb, box(0.6, 0.45, 0.35, 0.03), 'ice' if q else 'frost', (x, 1.5, 0.175 + q * 0.36))
        col_box('Cold_colBox', (-WX, y0 - 0.2, 0), (WX, y1, roof_z(y1)), coll)
        col_box('Chiller_colBox', (WX, 0.85, 0), (WX + 0.47, 1.85, 0.95), coll)
    return fn


def build_wh():
    for t in range(3): make('PB_Bld_wh%d' % t, wh_build(t), 'PB_Bld_wh%d.fbx' % t)


# ================================================================ OFFICES =====================================================
# Origin = floor centre of the office tier = spot_office (Walt's shack footprint centre, floor top z 0; ground ~ -0.28).
# office1/2 are modelled building-centred and shifted by OFFSET (they grow back / left from the shack's spot, see layout.py).
OFFSET = {'office0': (0.0, 0.0), 'office1': (0.0, 0.6), 'office2': (-1.0, 0.5)}
G0 = -0.28


def office_spots(coll, tier, extra):
    for k, (x, y, z, yaw) in layout.LAYOUT[tier]['spots'].items():
        if not k.startswith('fix_'): empty(k, (x, y, z), coll, yaw)
    for k, (x, y, z, yaw) in extra.items(): empty(k, (x, y, z), coll, yaw)


def walls_cols(coll, x0, x1, y0, y1, z1, door, th=0.06, z0=0.0, dz1=2.05):
    col_box('Back_colBox', (x0, y1 - th, z0), (x1, y1, z1), coll)
    col_box('Left_colBox', (x0, y0, z0), (x0 + th, y1, z1), coll)
    col_box('Right_colBox', (x1 - th, y0, z0), (x1, y1, z1), coll)
    col_box('FrontL_colBox', (x0, y0, z0), (door[0] - 0.04, y0 + th, z1), coll)
    col_box('FrontR_colBox', (door[1] + 0.04, y0, z0), (x1, y0 + th, z1), coll)
    col_box('FrontTop_colBox', (door[0] - 0.04, y0, z0 + dz1), (door[1] + 0.04, y0 + th, z1), coll)


def stove_pipe(mb, px, py, rz):
    put(mb, cyl(0.09, 1.3, 8, bevel=0.015), 'iron', (px, py, rz + 0.45), (D(-3), D(4), 0))
    put(mb, cyl(0.21, 0.13, 8, r2=0.05, bevel=0.015), 'iron', (px + 0.04, py - 0.03, rz + 1.22))
    put(mb, cyl(0.105, 0.06, 8, bevel=0.012), 'rust', (px + 0.01, py, rz + 0.35))


def window_box(mb, a0, a1, y, z, flowers=('poppy', 'sun', 'leaf', 'peach')):
    put(mb, box(a1 - a0 + 0.1, 0.2, 0.18, 0.025), 'teal', ((a0 + a1) / 2, y - 0.1, z - 0.18))
    n = max(2, int((a1 - a0) / 0.14))
    for k in range(n): put(mb, blob(1, (0.075, 0.075, 0.065), 90 + k + int(a0 * 10), 0.15), flowers[k % len(flowers)], (a0 + (k + 0.5) * (a1 - a0) / n, y - 0.1, z - 0.06))


def rail_run(mb, a, b, h=1.0, post=0.08, spind=0.21, col='wood', cap='cream2', z0=0.0):
    """A wooden railing from a to b (x, y): posts at the ends, spindles, a cream cap rail."""
    a, b = Vector((a[0], a[1], z0)), Vector((b[0], b[1], z0)); L = (b - a).length; d = (b - a).normalized()
    ang = math.atan2(d.y, d.x)
    for p in (a, b): put(mb, box(post, post, h + 0.05, 0.02), 'midwood', (p.x, p.y, z0 + (h + 0.05) / 2))
    m = (a + b) / 2
    put(mb, box(L + 0.06, 0.12, 0.06, 0.02), cap, (m.x, m.y, z0 + h), (0, 0, ang))
    put(mb, box(L, 0.05, 0.06, 0.015), col, (m.x, m.y, z0 + 0.12), (0, 0, ang))
    n = max(1, int(L / spind))
    for k in range(1, n):
        p = a + d * (L * k / n); put(mb, box(0.04, 0.03, h - 0.12, 0.0), col, (p.x, p.y, z0 + 0.12 + (h - 0.12) / 2), (0, 0, ang))


def office0(mb, coll):
    """Walt's shack, rebuilt as an enterable one-room office: the SAME outside as build_shack, door wide open."""
    HW, HD = 2.0, 1.75; EAVE, RIDGE = 2.15, 3.2
    roof_z = lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y) / HD)
    for a in (-1.8, 1.8):
        for b in (-1.55, 1.55): put(mb, blob(1, (0.28, 0.24, 0.32), int(a * 10 + b * 7) + 300, 0.1), 'rock2', (a, b, -0.25))
    put(mb, box(2 * HW + 0.2, 2 * HD + 0.2, 0.14, 0.03, 2), 'greyw', (0, 0, -0.11))
    floor_planks(mb, -HW + 0.06, HW - 0.06, -HD + 0.06, HD - 0.06, 0.0, (('weath', 0.55), ('wood', 0.85), ('greyw', 1.0)), 0.29)
    bw = 2 * HW / 13
    wx0, wx1 = -HW + 2 * bw, -HW + 5 * bw; dx0, dx1 = -HW + 8 * bw, -HW + 12 * bw      # window boards 2-4, door boards 8-11
    WZ0, WZ1, DZ1 = 0.95, 1.65, 1.95; wc = (wx0 + wx1) / 2
    put(mb, box(1.3, 0.42, 0.16, 0.03, 2), 'weath', ((dx0 + dx1) / 2, -HD - 0.28, -0.2), (0, 0, D(2)))
    cols = (('weath', 0.7), ('greyw', 1.0))
    bwall(mb, 'F', -HW, HW, -HD, -HD + 0.06, 0.0, EAVE, [(wx0, wx1, WZ0, WZ1), (dx0, dx1, 0.0, DZ1)], bw, cols)
    bwall(mb, 'B', -HW, HW, HD - 0.06, HD, 0.0, EAVE, [], bw, cols)
    for xs in (-HW, HW - 0.06): bwall(mb, 'L', -HD, HD, xs, xs + 0.06, 0.0, lambda u: roof_z(u) - 0.06, [], 2 * HD / 12, (('weath', 0.65), ('greyw', 1.0)))
    for a in (-1, 1):
        for b in (-1, 1): put(mb, box(0.13, 0.13, EAVE, 0.025, 2), 'dark', (a * (HW - 0.02), b * (HD - 0.02), EAVE / 2))
    leaf = door_leaf(mb, (dx1, -HD + 0.02), dx1 - dx0, DZ1 - 0.04, 100, 0.01)
    door_frame(mb, 'F', dx0, dx1, DZ1, -HD)
    window(mb, 'F', wx0, wx1, WZ0, WZ1, -HD)
    for x in (-0.9, 0.9): put(mb, box(0.1, 2 * HD - 0.1, 0.12, 0.02), 'midwood', (x, 0, EAVE - 0.06))           # tie beams
    gable_roof(mb, -HW, HW, HD, EAVE, RIDGE, 0.4, ['poppy', 'cream2', 'peach', 'poppy', 'cream2', 'peach', 'poppy', 'peach', 'cream2', 'poppy'],
               (2 * HW + 0.7) / 5, xov=0.35)
    th = math.atan2(RIDGE - EAVE, HD); ye = -(HD + 0.4); ze = EAVE - 0.4 * math.tan(th) + 0.05
    mb.add(corr_sheet(0.66, 0.8, 3, 0.025), 'rust', Matrix.Translation((0.9, ye + 0.7 * math.cos(th), ze + 0.7 * math.sin(th) + 0.09)) @ Matrix.Rotation(th, 4, 'X') @ Matrix.Rotation(D(6), 4, 'Z'))
    sx, sy = layout.LAYOUT['office0']['spots']['spot_stove'][:2]; stove_pipe(mb, sx, sy + 0.06, roof_z(sy + 0.06))
    # bench outside under the window, nets + floats on the -X wall (as on Walt's shack)
    put(mb, box(1.45, 0.34, 0.06, 0.02, 2), 'wood', (wc + 0.05, -HD - 0.32, 0.45), (0, D(1.5), D(-1.5)))
    for dx in (-0.55, 0.6): put(mb, box(0.09, 0.28, 0.42 + 0.28, 0.02), 'midwood', (wc + dx, -HD - 0.32, 0.21 - 0.14))
    xw = -HW - 0.04; nu, nv = 7, 5
    pts = [[Vector((xw - 0.05 - 0.08 * abs(math.sin(i * 1.3)) - 0.05 * v / nv, -1.2 + 1.9 * i / (nu - 1),
                    EAVE - 0.25 - 0.18 * abs(math.sin(math.pi * i / 3)) - (1.45 + 0.25 * math.sin(i * 1.7)) * v / (nv - 1))) for i in range(nu)] for v in range(nv)]
    bm = bmesh.new(); V = [[bm.verts.new(p) for p in row] for row in pts]; W = [[bm.verts.new(p + Vector((0.03, 0, 0))) for p in row] for row in pts]
    for v in range(nv - 1):
        for i in range(nu - 1):
            bm.faces.new((V[v][i], V[v + 1][i], V[v + 1][i + 1], V[v][i + 1])); bm.faces.new((W[v][i], W[v][i + 1], W[v + 1][i + 1], W[v + 1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces); mb.add(bm, 'rope')
    for i in (0, 3, 6): put(mb, box(0.12, 0.06, 0.06, 0.02), 'dark', (xw - 0.03, -1.2 + 1.9 * i / (nu - 1), EAVE - 0.22))
    for i in range(1, nu): p = pts[0][i]; put(mb, blob(0, (0.075, 0.075, 0.075), 200 + i, 0.05), 'sun' if i % 2 else 'poppy', (p.x - 0.06, p.y, p.z - 0.05))
    S = layout.LAYOUT['office0']['spots']
    add_fixed(mb, p_locker, S['spot_locker'][:3], S['spot_locker'][3]); add_fixed(mb, p_crate, S['fix_crate'][:3], S['fix_crate'][3])
    # colliders + sockets + every set spot
    col_box('Floor_colDeck', (-HW, -HD, -0.3), (HW, HD, 0.0), coll)
    walls_cols(coll, -HW, HW, -HD, HD, RIDGE, (dx0, dx1), 0.06, 0.0, DZ1)
    col_box('Step_colBox', ((dx0 + dx1) / 2 - 0.65, -HD - 0.49, -0.3), ((dx0 + dx1) / 2 + 0.65, -HD - 0.07, -0.12), coll)
    col_box('Bench_colBox', (wc - 0.68, -HD - 0.5, -0.3), (wc + 0.78, -HD - 0.14, 0.48), coll)
    col_box('DoorLeaf_colBox', (leaf[0][0], leaf[0][1], 0), (leaf[1][0], leaf[1][1], DZ1), coll)
    col_box('Locker_colBox', (S['spot_locker'][0] - 0.24, S['spot_locker'][1] - 0.23, 0), (S['spot_locker'][0] + 0.24, S['spot_locker'][1] + 0.23, 1.82), coll)
    col_box('Crate_colBox', (S['fix_crate'][0] - 0.2, S['fix_crate'][1] - 0.16, 0), (S['fix_crate'][0] + 0.2, S['fix_crate'][1] + 0.16, 0.42), coll)
    office_spots(coll, 'office0', dict(spot_balcony=(HW + 0.06, -0.4, 0.0, 90), socket_door=((dx0 + dx1) / 2, -HD - 0.75, G0, 0),
                                       socket_inside=((dx0 + dx1) / 2, -1.0, 0.0, 180)))
    print('OFFICE0 door gap %.2f m (x %.2f..%.2f)' % (dx1 - dx0, dx0, dx1))


def office1(mb, coll):
    """The Patched Office: 6 x 4.5, new boards among the old, a porch with a lean-to roof, a sign board (no text)."""
    X, Y, EAVE, RIDGE = 3.0, 2.25, 2.6, 3.6; T = 0.06
    roof_z = lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y) / Y)
    Lo = layout.LAYOUT['office1']; S = Lo['spots']; d0, d1 = Lo['door']
    footings(mb, -X, X, -Y, Y, -0.04, G0, 3, 2)
    floor_planks(mb, -X + T, X - T, -Y + T, Y - T, 0.0, (('wood', 0.5), ('fresh', 0.8), ('weath', 1.0)))
    cols = (('weath', 0.4), ('fresh', 0.72), ('greyw', 0.9), ('wood', 1.0))
    W = Lo['windows']
    def holes(side): return [(w[1], w[2], w[3], w[4]) for w in W if w[0] == side]
    bwall(mb, 'F', -X, X, -Y, -Y + T, 0.0, EAVE, holes('F') + [(d0, d1, 0.0, 2.05)], 0.3, cols, bev=0)
    bwall(mb, 'B', -X, X, Y - T, Y, 0.0, EAVE, holes('B'), 0.3, cols, bev=0)
    bwall(mb, 'L', -Y, Y, -X, -X + T, 0.0, lambda u: roof_z(u) - 0.06, holes('L'), 0.3, cols, bev=0)
    bwall(mb, 'R', -Y, Y, X - T, X, 0.0, lambda u: roof_z(u) - 0.06, holes('R'), 0.3, cols, bev=0)
    for w in W: window(mb, w[0], w[1], w[2], w[3], w[4], {'F': -Y, 'B': Y, 'L': -X, 'R': X}[w[0]])
    for w in W:
        if w[0] == 'F': window_box(mb, w[1], w[2], -Y - 0.02, w[3])
    corner_posts(mb, -X, X, -Y, Y, 0.0, EAVE, 'midwood')
    leaf = door_leaf(mb, (d1, -Y + 0.02), d1 - d0, 2.01, 100, 0.01)
    door_frame(mb, 'F', d0, d1, 2.05, -Y, 0.0, 'teal2')
    for x in (-1.5, 1.5): put(mb, box(0.1, 2 * Y - 0.1, 0.12, 0.02), 'midwood', (x, 0, EAVE - 0.06))
    gable_roof(mb, -X, X, Y, EAVE, RIDGE, 0.4, ['poppy', 'cream2', 'poppy', 'peach', 'cream2', 'poppy', 'sun', 'cream2', 'poppy', 'peach', 'cream2', 'poppy', 'cream2', 'peach'], 0.95, xov=0.35)
    sx, sy = S['spot_stove'][:2]; stove_pipe(mb, sx, sy + 0.06, roof_z(sy + 0.06))
    # the porch: deck, posts, a lean-to roof, steps; runs on to x 4.3 so a bought balcony (spot_balcony) joins it
    PY0, PX1 = -3.45, 4.3
    floor_planks(mb, -X - 0.05, PX1, PY0, -Y, 0.0, (('fresh', 0.6), ('wood', 1.0)), 0.3)
    put(mb, box(PX1 + X + 0.05, 0.08, 0.26, 0.02), 'midwood', ((PX1 - X) / 2, PY0 + 0.04, -0.15))
    for x in (-X + 0.1, (PX1 - X) / 2 - 0.4, PX1 - 0.1):
        put(mb, blob(1, (0.22, 0.2, 0.26), int(x * 9) + 800, 0.1), 'rock2', (x, PY0 + 0.15, G0 + 0.05))
    posts = (-2.9, d0 - 0.25, d1 + 0.25, PX1 - 0.1)
    for x in posts: put(mb, box(0.12, 0.12, 2.12, 0.025, 2), 'teal2', (x, PY0 + 0.1, 1.06))
    put(mb, box(PX1 + X + 0.2, 0.14, 0.12, 0.025), 'midwood', ((PX1 - X) / 2, PY0 + 0.1, 2.12))
    th = math.atan2(2.38 - 2.16, -Y - (PY0 - 0.15)); l = (-Y - (PY0 - 0.15)) / math.cos(th) + 0.05
    n = 8; sw = (PX1 + X + 0.4) / n
    for q in range(n):
        m = Matrix.Translation((-X - 0.2 + (q + 0.5) * sw, PY0 - 0.15, 2.16 + 0.01 * (q % 2))) @ Matrix.Rotation(th, 4, 'X')
        mb.add(corr_sheet(sw + 0.02, l, 4, 0.02), ('cream2', 'teal')[q % 2], m)
    for q in range(2):
        put(mb, box(d1 - d0 + 0.3, 0.32, 0.12, 0.025, 2), 'fresh' if q else 'wood', ((d0 + d1) / 2, PY0 - 0.17 - 0.3 * q, -0.08 - 0.12 * q))
    # the sign board (text-free; socket_sign = its face) standing on the porch roof's front edge
    sc = (d0 + d1) / 2
    put(mb, box(2.0, 0.08, 0.5, 0.03, 2), 'teal', (sc, PY0 - 0.02, 2.48))
    put(mb, box(1.84, 0.02, 0.36, 0.01), 'cream2', (sc, PY0 - 0.065, 2.48))
    for s in (-1, 1): put(mb, box(0.06, 0.06, 0.4, 0.015), 'midwood', (sc + s * 0.8, PY0 + 0.04, 2.3))
    empty('socket_sign', (sc, PY0 - 0.08, 2.48), coll, 0)
    # a porch bench under the left windows, a lamp by the door
    put(mb, box(1.3, 0.36, 0.06, 0.02, 2), 'teal', (-1.85, -Y - 0.28, 0.44))
    for dx in (-0.5, 0.5): put(mb, box(0.08, 0.3, 0.42, 0.02), 'midwood', (-1.85 + dx, -Y - 0.28, 0.21))
    lamp_box(mb, (d1 + 0.45, -Y - 0.28, 1.95), True)
    add_fixed(mb, p_locker, S['spot_locker'][:3], S['spot_locker'][3]); add_fixed(mb, p_crate, S['fix_crate'][:3], S['fix_crate'][3])
    col_box('Floor_colDeck', (-X, -Y, -0.3), (X, Y, 0.0), coll)
    col_box('Porch_colDeck', (-X, PY0, -0.3), (PX1, -Y, 0.0), coll)
    col_box('Steps_colBox', (d0 - 0.15, PY0 - 0.5, -0.3), (d1 + 0.15, PY0, -0.12), coll)
    walls_cols(coll, -X, X, -Y, Y, RIDGE, (d0, d1))
    for k, x in enumerate(posts): col_box('PorchPost%d_colBox' % k, (x - 0.07, PY0 + 0.03, 0), (x + 0.07, PY0 + 0.17, 2.1), coll)
    col_box('Bench_colBox', (-2.5, -Y - 0.47, 0), (-1.2, -Y - 0.09, 0.47), coll)
    col_box('DoorLeaf_colBox', (leaf[0][0], leaf[0][1], 0), (leaf[1][0], leaf[1][1], 2.05), coll)
    for nm, k, sz in (('Locker', 'spot_locker', (0.24, 0.23, 1.82)), ('Crate', 'fix_crate', (0.2, 0.16, 0.42))):
        x, y = S[k][:2]; col_box(nm + '_colBox', (x - sz[0], y - sz[1], 0), (x + sz[0], y + sz[1], sz[2]), coll)
    office_spots(coll, 'office1', dict(spot_balcony=(X + 0.04, -1.0, 0.0, 90), socket_door=((d0 + d1) / 2, -Y - 0.6, 0.0, 0),
                                       socket_inside=((d0 + d1) / 2, -1.6, 0.0, 180)))


def office2(mb, coll):
    """The Harbour Office: two storeys (8 x 6), the ground floor walkable, outside stairs up to a first-floor landing."""
    X, Y, T = 4.0, 3.0, 0.06; E1, F1, EAVE, RIDGE = 2.8, 2.95, 5.3, 6.9
    roof_z = lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y) / Y)
    Lo = layout.LAYOUT['office2']; S = Lo['spots']; d0, d1 = Lo['door']
    footings(mb, -X, X, -Y, Y, -0.04, G0, 4, 3)
    floor_planks(mb, -X + T, X - T, -Y + T, Y - T, 0.0, (('wood', 0.6), ('midwood', 0.85), ('fresh', 1.0)))
    low = (('cream2', 0.6), ('cream3', 1.0)); up = (('peach', 0.55), ('cream3', 0.8), ('peach', 1.0))
    W = Lo['windows']
    def holes(side): return [(w[1], w[2], w[3], w[4]) for w in W if w[0] == side]
    UW = [('F', -2.6, -1.7, F1 + 0.95, F1 + 1.75), ('F', 0.3, 1.2, F1 + 0.95, F1 + 1.75), ('F', 2.4, 3.3, F1 + 0.95, F1 + 1.75),
          ('B', -1.2, -0.3, F1 + 0.95, F1 + 1.75), ('B', 1.6, 2.5, F1 + 0.95, F1 + 1.75), ('L', -0.5, 0.5, F1 + 0.95, F1 + 1.75)]
    UD = (-2.6, -1.6)                                                  # first-floor door on the +X wall, onto the landing
    def uholes(side): return [(w[1], w[2], w[3], w[4]) for w in UW if w[0] == side] + ([(UD[0], UD[1], F1, F1 + 2.0)] if side == 'R' else [])
    bwall(mb, 'F', -X, X, -Y, -Y + T, 0.0, E1, holes('F') + [(d0, d1, 0.0, 2.05)], 0.36, low, bev=0)
    bwall(mb, 'B', -X, X, Y - T, Y, 0.0, E1, holes('B'), 0.36, low, bev=0)
    bwall(mb, 'L', -Y, Y, -X, -X + T, 0.0, E1, holes('L'), 0.36, low, bev=0)
    bwall(mb, 'R', -Y, Y, X - T, X, 0.0, E1, holes('R'), 0.36, low, bev=0); tick(mb, 'footing+floor+ground walls')
    bwall(mb, 'F', -X, X, -Y, -Y + T, F1, EAVE, uholes('F'), 0.36, up, bev=0)
    bwall(mb, 'B', -X, X, Y - T, Y, F1, EAVE, uholes('B'), 0.36, up, bev=0)
    bwall(mb, 'L', -Y, Y, -X, -X + T, F1, lambda u: roof_z(u) - 0.06, uholes('L'), 0.36, up, bev=0)
    bwall(mb, 'R', -Y, Y, X - T, X, F1, lambda u: roof_z(u) - 0.06, uholes('R'), 0.36, up, bev=0); tick(mb, 'upper walls')
    for w in W: window(mb, w[0], w[1], w[2], w[3], w[4], {'F': -Y, 'B': Y, 'L': -X, 'R': X}[w[0]], bev=0.0)
    for w in UW: window(mb, w[0], w[1], w[2], w[3], w[4], {'F': -Y, 'B': Y, 'L': -X, 'R': X}[w[0]], shutters='teal', bev=0.0)
    tick(mb, 'windows')
    # storey band, corner posts, the first floor slab + ceiling beams seen from the ground floor
    for side, a in (('F', -Y - 0.03), ('B', Y + 0.03)): put(mb, box(2 * X + 0.12, 0.08, 0.17, 0.025), 'teal', (0, a, (E1 + F1) / 2))
    for a in (-X - 0.03, X + 0.03): put(mb, box(0.08, 2 * Y + 0.12, 0.17, 0.025), 'teal', (a, 0, (E1 + F1) / 2))
    corner_posts(mb, -X, X, -Y, Y, 0.0, EAVE, 'teal2')
    put(mb, box(2 * X - 0.12, 2 * Y - 0.12, F1 - E1, 0.01), 'midwood', (0, 0, (E1 + F1) / 2))
    for x in (-2.6, 0.0 + 0.9, 2.6): put(mb, box(0.12, 2 * Y - 0.12, 0.14, 0.02), 'wood', (x, 0, E1 - 0.07))
    put(mb, cyl(0.12, 0.03, 8, bevel=0.008), 'iron', (S['spot_stove'][0], S['spot_stove'][1] + 0.06, E1 - 0.015))     # the flue plate
    leaf = door_leaf(mb, (d1, -Y + 0.02), d1 - d0, 2.01, 100, 0.01)
    door_frame(mb, 'F', d0, d1, 2.05, -Y, 0.0, 'teal2')
    put(mb, box(d1 - d0 + 0.3, 0.4, 0.14, 0.025, 2), 'rock2', ((d0 + d1) / 2, -Y - 0.2, -0.1))
    door_leaf(mb, (X + 0.02, UD[1]), UD[1] - UD[0], 1.98, 90, F1 + 0.01, 'teal', 'cream2', 1)     # the upstairs door, shut
    door_frame(mb, 'R', UD[0], UD[1], F1 + 2.0, X, F1, 'teal2')
    gable_roof(mb, -X, X, Y, EAVE, RIDGE, 0.45, ['red', 'poppy', 'cream2', 'poppy', 'red', 'cream2', 'poppy', 'red'], 1.0, xov=0.4, waves=4)
    cy = 1.9; put(mb, box(0.55, 0.55, RIDGE + 0.5 - roof_z(cy) + 0.3, 0.03, 2), 'terra', (0.0, cy, (roof_z(cy) - 0.3 + RIDGE + 0.5) / 2))
    put(mb, box(0.65, 0.65, 0.1, 0.02), 'rock2', (0.0, cy, RIDGE + 0.5)); put(mb, cyl(0.12, 0.25, 8, bevel=0.01), 'iron', (0, cy, RIDGE + 0.67))
    tick(mb, 'band/posts/slab/doors/roof')
    # the sign over the door (text-free; socket_sign = its face) + a door lamp
    sc = (d0 + d1) / 2
    put(mb, box(2.3, 0.08, 0.5, 0.03, 2), 'teal', (sc - 0.1, -Y - 0.06, 2.42))
    put(mb, box(2.14, 0.02, 0.36, 0.01), 'cream2', (sc - 0.1, -Y - 0.105, 2.42))
    empty('socket_sign', (sc - 0.1, -Y - 0.12, 2.42), coll, 0)
    lamp_box(mb, (d0 - 0.4, -Y - 0.28, 1.9), True)
    tick(mb, 'sign+lamp')
    # outside stairs along the front wall: 16 steps from x -1 (ground) up to the landing at x 4 (first floor)
    SX0, SX1, SY0, SY1 = -1.0, X, -Y - 1.0, -Y - 0.02; nst = 16; rise = (F1 - G0) / nst; run = (SX1 - SX0) / nst
    for k in range(nst):
        z = G0 + (k + 1) * rise
        put(mb, box(run + 0.03, SY1 - SY0 - 0.08, 0.05, 0.0), 'fresh' if k % 3 else 'wood', (SX0 + (k + 0.5) * run, (SY0 + SY1) / 2, z - 0.025))
    sl = math.hypot(SX1 - SX0, F1 - G0); sa = -math.atan2(F1 - G0, SX1 - SX0)
    for y in (SY0 + 0.04, SY1 - 0.04): put(mb, box(sl, 0.06, 0.24, 0.02), 'midwood', ((SX0 + SX1) / 2, y, (G0 + F1) / 2 - 0.12), (0, sa, 0))
    for x in (SX0, 1.5, SX1 - 0.04):
        zt = G0 + (x - SX0) / (SX1 - SX0) * (F1 - G0)
        put(mb, box(0.1, 0.1, zt - G0 + 1.0, 0.02), 'teal2', (x, SY0 + 0.05, (zt + G0 + 1.0) / 2))
    put(mb, box(sl + 0.1, 0.12, 0.06, 0.02), 'cream2', ((SX0 + SX1) / 2, SY0 + 0.05, (G0 + F1) / 2 + 1.0), (0, sa, 0))
    for k in range(1, 12):
        x = SX0 + (SX1 - SX0) * k / 12; zt = G0 + (x - SX0) / (SX1 - SX0) * (F1 - G0)
        put(mb, box(0.035, 0.035, 0.9, 0.0), 'wood', (x, SY0 + 0.05, zt + 0.5))
    tick(mb, 'stairs')
    # the first-floor landing round the front-right corner (balcony joins it at y -1 when bought)
    LX0, LX1, LY0, LY1 = X, X + 1.2, -Y - 1.0, -1.0
    floor_planks(mb, LX0, LX1, LY0, LY1, F1, (('fresh', 0.6), ('wood', 1.0)), 0.3, 0.08)
    put(mb, box(LX1 - LX0, LY1 - LY0, 0.14, 0.02), 'midwood', ((LX0 + LX1) / 2, (LY0 + LY1) / 2, F1 - 0.15))
    for x, y in ((LX1 - 0.06, LY0 + 0.06), (LX1 - 0.06, LY1 - 0.06), (LX0 + 0.06, LY0 + 0.06)):
        put(mb, box(0.13, 0.13, F1 - G0, 0.025, 2), 'teal2', (x, y, (F1 + G0) / 2))
    for x, y in ((LX1 - 0.06, (LY0 + LY1) / 2),):
        L = math.hypot(1.0, 1.0); put(mb, box(0.07, L, 0.07, 0.02), 'midwood', (x, y, F1 - 0.6), (D(45), 0, 0))
    rail_run(mb, (LX1 - 0.04, LY0 + 0.04), (LX1 - 0.04, LY1 - 0.04), 1.0, z0=F1)
    rail_run(mb, (LX0 + 0.04, LY0 + 0.04), (LX1 - 0.04, LY0 + 0.04), 1.0, z0=F1)
    add_fixed(mb, p_locker, S['spot_locker'][:3], S['spot_locker'][3]); add_fixed(mb, p_crate, S['fix_crate'][:3], S['fix_crate'][3])
    # the landing's end gate: its own object so the game can hide it when the balcony goes on (the balcony's gap meets it)
    gm = MB(); rail_run(gm, (LX0 + 0.06, LY1 - 0.04), (LX1 - 0.04, LY1 - 0.04), 1.0, z0=F1); finish(gm, 'LandingGate', coll)
    col_box('LandingGate_colBox', (LX0, LY1 - 0.1, F1), (LX1, LY1, F1 + 1.05), coll)
    # stairs collider: a sloped solid (MeshCollider via *_col)
    sm = MB(); sm.add(board(SX0 - 0.25, SX1, SY0, SY1, G0, lambda x, y: G0 + max(0.0, (x - SX0 + 0.25)) / (SX1 - SX0 + 0.25) * (F1 - G0), 0.0), 'iron')
    so = finish(sm, 'Stairs_col', coll); so.hide_render = True; so.display_type = 'WIRE'
    col_box('Floor_colDeck', (-X, -Y, -0.3), (X, Y, 0.0), coll)
    walls_cols(coll, -X, X, -Y, Y, E1, (d0, d1))
    col_box('Upper_colBox', (-X, -Y, E1), (X, Y, RIDGE), coll)
    col_box('Landing_colDeck', (LX0, LY0, F1 - 0.15), (LX1, LY1, F1), coll)
    col_box('LandingRailO_colBox', (LX1 - 0.1, LY0, F1), (LX1, LY1, F1 + 1.05), coll)
    col_box('LandingRailF_colBox', (LX0, LY0, F1), (LX1, LY0 + 0.1, F1 + 1.05), coll)
    col_box('StairRail_colBox', (SX0 - 0.05, SY0 - 0.02, G0), (SX1, SY0 + 0.1, F1 + 1.05), coll)
    col_box('Step_colBox', (d0 - 0.15, -Y - 0.4, -0.3), (d1 + 0.15, -Y, -0.03), coll)
    col_box('DoorLeaf_colBox', (leaf[0][0], leaf[0][1], 0), (leaf[1][0], leaf[1][1], 2.05), coll)
    for nm, k, sz in (('Locker', 'spot_locker', (0.24, 0.23, 1.82)), ('Crate', 'fix_crate', (0.2, 0.16, 0.42))):
        x, y = S[k][:2]; col_box(nm + '_colBox', (x - sz[0], y - sz[1], 0), (x + sz[0], y + sz[1], sz[2]), coll)
    office_spots(coll, 'office2', dict(spot_balcony=(X + 0.04, 0.25, F1, 90), socket_door=((d0 + d1) / 2, -Y - 0.7, G0, 0),
                                       socket_inside=((d0 + d1) / 2, -2.2, 0.0, 180), socket_landing=(LX0 + 0.6, -Y - 0.5, F1, 0)))


OFFICES = [('office0', office0), ('office1', office1), ('office2', office2)]


def build_offices():
    for t, fn in OFFICES:
        bad, _ = layout.check(t)
        if bad: print('LAYOUT PROBLEMS in', t, bad)
        make('PB_Bld_' + t, fn, 'PB_Bld_%s.fbx' % t, off=OFFSET[t])


# ================================================================ SPOTS (Walt's Landing) =====================================
# name: (x, y, z or None = on the ground, rotz deg (Blender: 0 faces -Y, 90 faces +X), prop for the footprint)
def site_spots():
    g = lambda x, y, dz=0.0: round(terrain_h(x, y) + dz, 3)
    wx, wy = 2.5, 15.0
    wz = round(max(terrain_h(wx + a, wy + b) for a in (-3, 0, 3) for b in (-2.25, 0, 2.25)) + 0.15, 3)
    return dict(
        spot_planks=(0.0, 1.0, DECK, 0, 'planks_fixed'),   # y 5.25 lay under map B's rock at the pier root
        spot_bench_planks=(-0.96, 1.0, DECK, 90, None),
        spot_b2=(1.30, -11.25, DECK, 90, 'berth_fixed'),
        spot_b3=(1.30, -3.75, DECK, 90, 'berth_fixed'),
        spot_lamp1=(-1.0, -12.25, DECK, 90, 'docklamp'),
        spot_lamp2=(-1.0, -4.75, DECK, 90, 'docklamp'),
        spot_lamp3=(-1.0, 2.75, DECK, 90, 'docklamp'),
        spot_lamp4=(6.3, 10.9, g(6.3, 10.9), -90, 'docklamp'),
        spot_power=(1.05, -17.4, DECK, -90, 'powerpost'),
        spot_tap=(1.05, -15.8, DECK, -90, 'tap'),
        spot_wh=(wx, wy, wz, 0, 'wh'),
        spot_office=(SHACK[0], SHACK[1], round(shack_floor(), 3), 0, 'office'))


def site_obstacles():
    """(name, x0, x1, y0, y1) things already on Walt's Landing (M1) that no spot may cover."""
    c = lambda n, x, y, r: (n, x - r, x + r, y - r, y + r)
    ob = [('pier ladder', 1.25, 1.5, -20.9, -20.3), c('ladder top socket', 1.2, -20.6, 0.3), c('spawn', 0.15, -19.6, 0.5),
          c('moor point', 2.9, -19.0, 0.3), ('Biscuit at her mooring', 1.95, 3.85, -21.6, -16.4),
          ('Mari boat', -4.3, -2.3, -17.0, -12.0), c('Mari mooring', -1.1, -14.5, 0.3),
          ('shack', -8.1, -3.9, 11.9, 16.1), ('rope rail', -1.45, -1.18, -21.4, 9.1),
          ('crab pots', -1.08, -0.33, -16.45, -15.1), ('fish crate', -1.08, -0.36, -18.64, -18.16),
          c('cleat -19.2', 1.02, -19.2, 0.22), c('cleat -13.2', 1.02, -13.2, 0.22), c('bollard -21.85', 1.48, -21.85, 0.2),
          c('bollard -16.6', 1.48, -16.6, 0.2), c('rope coil', 0.78, -18.2, 0.2), c('old lamp', -1.0, -21.65, 0.12), c('bucket', -0.95, -20.7, 0.17),
          ('hero rock W', -4.6, -2.2, 7.7, 10.1), c('hero rock E', 3.6, 9.2, 1.2), c('rock', -2.2, 7.7, 0.9), c('rock', 5.2, 8.3, 0.9),
          c('bush', -2.0, 20.0, 1.0), c('bush', -1.1, 20.0, 0.7), c('bush', -12.0, 18.5, 1.0), c('bush', -11.1, 18.5, 0.7)]
    for k in range(13):                                   # the pier's piles (each side)
        y = PY0 - 2.5 * k
        for x in (-1.22, 1.22): ob.append(c('pile', x, y, 0.17))
    return ob


def seg_rect_dist(p, q, r):
    """Shortest distance between segment pq and rect r (x0, x1, y0, y1), by sampling the segment."""
    best = 9e9
    for k in range(41):
        t = k / 40; x = p[0] + (q[0] - p[0]) * t; y = p[1] + (q[1] - p[1]) * t
        dx = max(r[0] - x, 0, x - r[1]); dy = max(r[2] - y, 0, y - r[3]); best = min(best, math.hypot(dx, dy))
    return best


def build_spots():
    S = site_spots(); foot = {}
    if os.path.exists(os.path.join(OUT, 'props.json')): foot.update(json.load(open(os.path.join(OUT, 'props.json'))))
    foot.update(FOOTS)
    foot['wh'] = (-3.1, 3.1, -2.95, 2.35, 0, 4)                        # incl. the ramp + socket_door
    foot['office'] = (-5.5, 4.3, -3.85, 3.9, 0, 7)                      # the union of office0/1/2 (porches, stairs, landing, balcony)
    # wall / deck level extents of office0/1/2 incl. porch, steps, stairs, landing and a bought balcony (roof overhangs left out)
    o1, o2 = OFFSET['office1'], OFFSET['office2']
    foot['office'] = (min(-2.0, -3.0 + o1[0], -4.0 + o2[0]), max(3.26, 4.3 + o1[0], 5.24 + o2[0]),
                      min(-2.2, -3.8 + o1[1], -4.0 + o2[1]), max(1.75, 2.25 + o1[1], 3.0 + o2[1]), 0, 7)
    print('SPOTS office union', foot['office'])
    rects = {}; base = {}
    for k, (x, y, z, rz, p) in S.items():
        if p is None or p not in foot: continue
        r = layout.rot_bb(foot[p], rz); rects[k] = (x + r[0], x + r[1], y + r[2], y + r[3])
        if p == 'docklamp': r = (-0.17, 0.17, -0.17, 0.17)                # at walking height only the post is in the way
        base[k] = (x + r[0], x + r[1], y + r[2], y + r[3]) if p == 'docklamp' else rects[k]
    bad = []
    ks = sorted(rects)
    deck_floor = {'spot_planks'}                                         # lies flat on the deck: may cross the walkway
    for i, a in enumerate(ks):                                           # spots vs spots
        for b in ks[i + 1:]:
            A, B = rects[a], rects[b]
            if a in deck_floor or b in deck_floor: continue
            if min(A[1], B[1]) - max(A[0], B[0]) > 0 and min(A[3], B[3]) - max(A[2], B[2]) > 0: bad.append('%s overlaps %s' % (a, b))
    obs = site_obstacles()
    for k, A in rects.items():                                           # spots vs the landing's things
        for o in obs:
            if k == 'spot_office' and o[0] == 'shack': continue
            if o[0] == 'pile': continue                                    # pier piles end below the deck (z 0.94); berth piles checked below
            if k == 'spot_planks' and o[0] == 'rope rail': continue          # flat boards on the deck, under the rope
            if min(A[1], o[2]) - max(A[0], o[1]) > 0 and min(A[3], o[4]) - max(A[2], o[3]) > 0: bad.append('%s overlaps %s' % (k, o[0]))
        B = base[k]
        if k != 'spot_planks' and S[k][2] == DECK and min(B[1], 0.5) - max(B[0], -0.5) > 0: bad.append('%s blocks the 1 m pier walkway' % k)
    for k in ('spot_b2', 'spot_b3'):                                     # the new berth pile vs the old piles + boats
        x, y = S[k][0] + 0.35, S[k][1]
        for o in obs:
            if o[0] == 'pile' and math.hypot((o[1] + o[2]) / 2 - x, (o[3] + o[4]) / 2 - y) < 0.6: bad.append('%s pile too close to a pier pile' % k)
        mx, my = S[k][0] + 1.6, S[k][1]
        if abs(my - (-19.0)) < 5.0: bad.append('%s mooring too close to Biscuit (B1)' % k)
        if abs(my - S['spot_b2' if k == 'spot_b3' else 'spot_b3'][1]) < 5.0: bad.append('B2/B3 moorings too close')
    # the walking lines: pier root -> office door, pier root -> warehouse door (0.8 m wide)
    od = (SHACK[0] + 1.075, SHACK[1] - 1.75 - 0.75); wd = (S['spot_wh'][0], S['spot_wh'][1] - 2.25 - 0.6)
    for nm, p, q in (('pier->office door', (0.0, 10.0), od), ('pier->warehouse door', (0.0, 10.0), wd), ('office door->warehouse door', od, wd)):
        for k, A in rects.items():
            if k in ('spot_office', 'spot_wh', 'spot_planks', 'spot_bench_planks'): continue
            d = seg_rect_dist(p, q, base[k])
            if d < 0.4: bad.append('%s is on the path %s (%.2f m)' % (k, nm, d))
        for o in obs:
            if o[0] in ('shack', 'rope rail', 'pile'): continue
            d = seg_rect_dist(p, q, (o[1], o[2], o[3], o[4]))
            if d < 0.4: bad.append('path %s hits %s (%.2f m)' % (nm, o[0], d))
    # flat ground under the warehouse + the yard lamp
    wx, wy = S['spot_wh'][:2]
    hs = [terrain_h(wx + a, wy + b) for a in frange(-3, 3, 1.0) for b in frange(-2.25, 2.25, 0.75)]
    print('SPOTS warehouse ground %.2f..%.2f (floor %.2f)' % (min(hs), max(hs), S['spot_wh'][2]))
    if max(hs) - min(hs) > 0.12: bad.append('spot_wh ground not flat (%.2f m)' % (max(hs) - min(hs)))
    out = dict(_doc='Port Boss M5 set spots on Walt\'s Landing, in the landing\'s Blender metres (x, y, z; Z up; the pier runs -Y). '
                    'yaw = the Works.cs convention (front = (sin(-yaw), -cos(-yaw)) in Blender x/y); rotz = the same turn as a Blender '
                    'Z rotation (0 faces -Y, 90 faces +X). Generated by art-src/m5/build_props.py (spots).',
               spots={k: dict(pos=[round(v[0], 3), round(v[1], 3), round(v[2], 3)], yaw=-v[3] if v[3] else 0, rotz=v[3],
                              prop=('PB_Prop_' + v[4]) if v[4] and v[4] not in ('wh', 'office') else ('PB_Bld_wh0/1/2' if v[4] == 'wh' else 'PB_Bld_office0/1/2' if v[4] else 'bench inside PB_Prop_planks_fixed'))
                      for k, v in S.items()})
    json.dump(out, open(os.path.join(HERE, 'spots.json'), 'w'), indent=1)
    print('SPOTS wrote', os.path.join(HERE, 'spots.json'))
    for k, v in S.items(): print('  %-18s (%.2f, %.2f, %.2f) rotz %d  rect %s' % (k, v[0], v[1], v[2], v[3], tuple(round(c, 2) for c in rects.get(k, ()))))
    print('SPOTS check:', 'OK' if not bad else bad)
    return S, rects, bad


# ================================================================ SELF-CHECK RENDERS ==========================================
def emis(name, col, strength=1.0):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    o = nt.nodes.new('ShaderNodeOutputMaterial'); e = nt.nodes.new('ShaderNodeEmission')
    e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(col)) + (1,); e.inputs['Strength'].default_value = strength
    nt.links.new(e.outputs[0], o.inputs['Surface']); return m


def solo(names):
    """Show only these asset collections in renders (everything else built this run is hidden)."""
    for n, (ob, coll, lo, hi) in OBJS.items(): coll.hide_render = n not in names
    for c in bpy.data.collections:
        if c.name.startswith('R_'): c.hide_render = c.name not in names


def three_q(cam, name, lo, hi, w=(-0.75, -1.35, 0.8), lens=50, fill=1.0):
    c = Vector([(lo[k] + hi[k]) / 2 for k in range(3)]); size = max(hi[0] - lo[0], hi[1] - lo[1], hi[2] - lo[2])
    d = Vector(w).normalized() * (size * 1.55 * fill * 50 / lens + 0.25)
    return shoot(cam, name, c + d, c, lens)


def label(text, loc, size, coll, col='#33303A', rot=0.0):
    cu = bpy.data.curves.new('t_' + text, 'FONT'); cu.body = text; cu.size = size; cu.align_x = 'LEFT'
    o = bpy.data.objects.new('t_' + text, cu); coll.objects.link(o); o.location = loc; o.rotation_euler = (0, 0, rot)
    o.data.materials.append(emis('lbl' + col, col)); return o


def marker(loc, yaw, col, coll, r=0.09):
    bm = cyl(r, 0.03, 10, bevel=0); me = bpy.data.meshes.new('mk'); bm.to_mesh(me); bm.free(); me.materials.append(emis('mk' + col, col, 1.2))
    o = bpy.data.objects.new('mk', me); coll.objects.link(o); o.location = loc
    bm = box(0.035, 0.32, 0.03, 0); bmesh.ops.translate(bm, vec=(0, -0.2, 0), verts=bm.verts)
    me = bpy.data.meshes.new('ar'); bm.to_mesh(me); bm.free(); me.materials.append(emis('mk' + col, col, 1.2))
    a = bpy.data.objects.new('ar', me); coll.objects.link(a); a.location = loc; a.rotation_euler = (0, 0, D(yaw))


MK = ['#E2553F', '#2160B8', '#3FA34D', '#F7A21B', '#8E5C8A', '#1FA3A3', '#D23C8A', '#6B4E2E', '#000000', '#5AB4F0', '#B5B000']


def place(prop, loc, rot, coll):
    if prop not in OBJS: return None
    src = OBJS[prop][0]; o = bpy.data.objects.new(prop + '_at', src.data); coll.objects.link(o)
    o.location = loc; o.rotation_euler = rot; return o


def render_all():
    cam = setup_render(520, 440, sea=(0.0, 400, 'cream3'), bgc=(0.62, 0.78, 0.95))
    ground = bpy.data.objects['ground']
    paths = []
    # furniture contact sheet
    furn = [p for p, _, _ in FURN if 'PB_Prop_' + p in OBJS]
    if furn:
        for p in furn:
            n = 'PB_Prop_' + p; solo({n}); ob, coll, lo, hi = OBJS[n]
            if p in ('painting', 'painting2', 'trophy'):
                ground.location.z = lo[2] - 0.02
                paths.append(three_q(cam, 'f_' + p + '.png', lo, hi, (-0.35, -1.4, 0.25), 50, 1.1))
            else:
                ground.location.z = 0.0
                paths.append(three_q(cam, 'f_' + p + '.png', lo, hi, (-0.75, -1.35, 0.85) if p != 'rug' else (-0.6, -1.0, 1.4)))
        sheet(paths, 5, os.path.join(REN, 'furniture_sheet.png')); paths = []
    # dock contact sheet (water plane under the deck)
    dock = [p for p, _ in DOCK if 'PB_Prop_' + p in OBJS]
    if dock:
        mt = bpy.data.materials['ground']; e = mt.node_tree.nodes['Emission']
        e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL['sky'])) + (1,); ground.location.z = -1.3
        for p in dock:
            n = 'PB_Prop_' + p; solo({n}); ob, coll, lo, hi = OBJS[n]
            lo2 = list(lo); lo2[2] = max(lo[2], -1.4)
            paths.append(three_q(cam, 'd_' + p + '.png', lo2, hi, (-1.2, -0.9, 0.7) if 'berth' in p else (-0.75, -1.35, 0.85), 50, 1.3 if p in ('berth_fixed', 'berth_broken', 'powerpost', 'tap', 'docklamp') else 1.0))
        sheet(paths, 4, os.path.join(REN, 'dock_sheet.png')); paths = []
        e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL['cream3'])) + (1,)
    # hats on a head sphere
    hats = [h for h, _ in HATS if 'PB_Hat_' + h in OBJS]
    if hats:
        hc = new_coll('R_head'); m = MB(); m.add(bmesh_ico(0.13), 'peach'); m.add(bmesh_ico(0.012), 'ink', M((-0.045, -0.123, 0.0))); m.add(bmesh_ico(0.012), 'ink', M((0.045, -0.123, 0.0)))
        m.add(cyl(0.05, 0.25, 8, bevel=0), 'peach', M((0, 0, -0.19))); finish(m, 'Head', hc)
        ground.location.z = -0.3
        for h in hats:
            n = 'PB_Hat_' + h; solo({n, 'R_head'}); ob, coll, lo, hi = OBJS[n]
            paths.append(three_q(cam, 'h_' + h + '.png', (min(lo[0], -0.14), min(lo[1], -0.14), -0.14), hi, (-0.8, -1.4, 0.55), 50, 1.25))
        sheet(paths, 5, os.path.join(REN, 'hats_sheet.png')); paths = []
    # buildings: 3/4 front views
    bpy.context.scene.render.resolution_x = 1280; bpy.context.scene.render.resolution_y = 860
    mt = bpy.data.materials['ground']; e = mt.node_tree.nodes['Emission']; e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL['grass'])) + (1,)
    for t in range(3):
        n = 'PB_Bld_wh%d' % t
        if n not in OBJS: continue
        solo({n}); ground.location.z = -0.15; ob, coll, lo, hi = OBJS[n]
        three_q(cam, 'wh%d_34.png' % t, lo, hi, (-0.8, -1.4, 0.7), 35, 1.0)
        three_q(cam, 'wh%d_door.png' % t, (-2, -2.5, 0), (2, 2.2, 2.6), (0.05, -1.4, 0.35), 35, 1.0)
    for t in range(3):
        n = 'PB_Bld_office%d' % t
        if n not in OBJS: continue
        solo({n}); ground.location.z = -0.28; ob, coll, lo, hi = OBJS[n]
        three_q(cam, 'office%d_34.png' % t, lo, hi, (-0.8, -1.4, 0.7), 35, 1.0)
        if t == 2: three_q(cam, 'office2_34b.png', lo, hi, (1.3, -0.9, 0.6), 35, 1.0)
    # office plans: roof cut away (camera clip), furniture at its spots, each spot as a coloured marker + arrow + name
    for t in range(3):
        n = 'PB_Bld_office%d' % t
        if n not in OBJS: continue
        ob, coll, lo, hi = OBJS[n]; pc = new_coll('R_plan%d' % t); solo({n, pc.name})
        cut = 2.0 if t < 2 else 2.55
        k = 0
        for e_ in sorted([o for o in coll.objects if o.type == 'EMPTY'], key=lambda o: o['nm']):
            nm = e_['nm']; prop = layout.SPOT2PROP.get(nm)
            if prop and prop not in ('locker',):
                place('PB_Prop_' + prop, e_.location, e_.rotation_euler, pc)
            col = MK[k % len(MK)]; k += 1
            marker((e_.location.x, e_.location.y, min(cut - 0.15, e_.location.z + 1.0 + 0.02 * k)), math.degrees(e_.rotation_euler.z), col, pc)
            label(nm.replace('spot_', '').replace('socket_', 's:'), (e_.location.x + 0.1, e_.location.y + 0.05, cut - 0.1), 0.14, pc, col)
        c = Vector(((lo[0] + hi[0]) / 2, (lo[1] + hi[1]) / 2, 30)); ground.location.z = -0.3
        cam.data.clip_start = 30 - cut; bpy.context.scene.render.resolution_x = 1400; bpy.context.scene.render.resolution_y = int(1400 * (hi[1] - lo[1] + 1) / (hi[0] - lo[0] + 1))
        shoot(cam, 'office%d_plan.png' % t, c, c - Vector((0, 0, 1)), ortho=max(hi[0] - lo[0], hi[1] - lo[1]) + 1.0)
        cam.data.clip_start = 0.1
        # and a view in through the door with the furniture in
        bpy.context.scene.render.resolution_x = 1280; bpy.context.scene.render.resolution_y = 860
        sd = next((o for o in coll.objects if o.get('nm') == 'socket_door'), None); si = next((o for o in coll.objects if o.get('nm') == 'socket_inside'), None)
        if sd and si:
            cam.data.clip_start = 0.05
            shoot(cam, 'office%d_inside.png' % t, si.location + Vector((0.3, -0.6, 1.55)), si.location + Vector((-0.6, 2.4, 0.4)), 18)
            cam.data.clip_start = 0.1
    bpy.context.scene.render.resolution_x = 1280; bpy.context.scene.render.resolution_y = 860


def bmesh_ico(r):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=3, radius=r); return bm


def render_site(S, rects):
    """Walt's Landing (the M1 FBX, read-only) with the M5 pieces dropped on their spots: a plan + two 3/4 views."""
    fbx = os.path.join(REPO, 'Assets', 'Game', 'Art', 'PB_Landing_Walts.fbx')
    if not os.path.exists(fbx): print('no landing fbx'); return
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=fbx)
    land = [o for o in bpy.data.objects if o not in before]
    sc = new_coll('R_site'); tm = toon_mat()
    for o in land:
        for c in o.users_collection: c.objects.unlink(o)
        sc.objects.link(o)
        if o.type == 'MESH':
            if '_col' in o.name: o.hide_render = True; continue
            ca = o.data.color_attributes
            if len(ca): ca[0].name = 'Col'
            o.data.materials.clear(); o.data.materials.append(tm)
    shack = next((o for o in land if o.name.startswith('Shack') and '_col' not in o.name), None)
    g = bpy.data.objects['ground']; g.location.z = -3.0
    mt = bpy.data.materials['ground']; mt.node_tree.nodes['Emission'].inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL['teal'])) + (1,)
    cam = bpy.context.scene.camera
    def drop(tier_wh, tier_off, fixed):
        pc = new_coll('R_drop%d%d' % (tier_wh, tier_off))
        for k, (x, y, z, rz, p) in S.items():
            if k == 'spot_wh': pr = 'PB_Bld_wh%d' % tier_wh
            elif k == 'spot_office': pr = 'PB_Bld_office%d' % tier_off
            elif k in ('spot_b2', 'spot_b3'): pr = 'PB_Prop_berth_' + ('fixed' if fixed else 'broken')
            elif k == 'spot_planks': pr = 'PB_Prop_planks_' + ('fixed' if fixed else 'broken')
            elif k == 'spot_bench_planks': continue
            elif not fixed: continue
            else: pr = 'PB_Prop_' + p
            if pr in OBJS:
                for o in OBJS[pr][1].objects:
                    if o.type != 'MESH' or '_col' in o.name: continue
                    c = bpy.data.objects.new(o.name + '_s', o.data); pc.objects.link(c)
                    c.matrix_world = M((x, y, z), (0, 0, D(rz))) @ o.matrix_world
        return pc
    d0 = drop(0, 0, False); d1 = drop(1, 1, True); d2 = drop(2, 2, True)
    bpy.context.scene.render.resolution_x = 1280; bpy.context.scene.render.resolution_y = 860
    for nm, d, hide_shack in (('site_t0', d0, True), ('site_t1', d1, True), ('site_t2', d2, True)):
        solo({sc.name, d.name})
        if shack: shack.hide_render = hide_shack
        shoot(cam, nm + '_yard.png', (9.0, -4.0, 9.5), (-3.0, 12.5, 1.6), 26)
        if nm == 'site_t1': shoot(cam, nm + '_gameplay.png', (17, -36, 21), (-1, -8, 1), 35)
    # plan with the spot rects + names
    pc = new_coll('R_siteplan'); solo({sc.name, d1.name, pc.name})
    for i, (k, r) in enumerate(sorted(rects.items())):
        col = MK[i % len(MK)]
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts: v.co = Vector((r[1] if v.co.x > 0 else r[0], r[3] if v.co.y > 0 else r[2], 9.0 if v.co.z > 0 else 8.98))
        me = bpy.data.meshes.new('rc'); bm.to_mesh(me); bm.free(); me.materials.append(emis('mk' + col, col))
        o = bpy.data.objects.new('rc', me); pc.objects.link(o); o.display_type = 'WIRE'
        mod = o.modifiers.new('w', 'WIREFRAME'); mod.thickness = 0.08
        label(k.replace('spot_', ''), (r[1] + 0.1, (r[2] + r[3]) / 2, 9.0), 0.45, pc, col)
    for o in site_obstacles():
        if o[0] in ('pile',): continue
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts: v.co = Vector((o[2] if v.co.x > 0 else o[1], o[4] if v.co.y > 0 else o[3], 8.9 if v.co.z > 0 else 8.88))
        me = bpy.data.meshes.new('ob'); bm.to_mesh(me); bm.free(); me.materials.append(emis('obst', '#555555'))
        ob_ = bpy.data.objects.new('ob', me); pc.objects.link(ob_); mod = ob_.modifiers.new('w', 'WIREFRAME'); mod.thickness = 0.04
    od = (SHACK[0] + 1.075, SHACK[1] - 2.5); wd = (S['spot_wh'][0], S['spot_wh'][1] - 2.85)
    for p, q in (((0, 10), od), ((0, 10), wd), (od, wd)):
        rod_o = bpy.data.objects.new('path', bpy.data.meshes.new('path')); bm = sweep([(p[0], p[1], 8.95), (q[0], q[1], 8.95)], circle(0.4, 4), up=(0, 0, 1))
        bm.to_mesh(rod_o.data); bm.free(); rod_o.data.materials.append(emis('path', '#FFFFFF', 0.6)); pc.objects.link(rod_o)
    if shack: shack.hide_render = True
    bpy.context.scene.render.resolution_x = 1000; bpy.context.scene.render.resolution_y = 1500
    c = Vector((-3.0, -1.5, 40)); cam.data.clip_start = 0.1
    shoot(cam, 'site_plan.png', c, c - Vector((0, 0, 1)), ortho=44)


# ================================================================ MAIN ========================================================
new_scene()
if want('furniture'): build_furniture()
if want('dock'): build_dock()
if want('hats'): build_hats()
if want('wh'): build_wh()
if want('office'): build_offices()
SP = None
if want('spots'): SP = build_spots()
if RENDER:
    render_all()
    if SP: render_site(SP[0], SP[1])
print('REPORT tris:')
for k in sorted(REPORT): print('  %-28s %6d' % (k, REPORT[k]))
