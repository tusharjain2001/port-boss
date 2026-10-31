# Port Boss M1 art, modelled from code in Blender 5.1 (headless, never opens a window):
#   "Biscuit" (grandpa Walt's clinker skiff) -> Assets/Game/Art/PB_Boat_Biscuit_T1.fbx
#   Walt's Landing (pier, shack, rocky cove)  -> Assets/Game/Art/PB_Landing_Walts.fbx
# Run:  blender -b -P build_boat_landing.py -- [boat|landing|all] [norender]
# Art direction "Ponyo Coast": flat per-face vertex colours ("Col", sRGB bytes) from the palette only, every edge
# bevelled, seeded hand-made wobble. 1 unit = 1 m, Z up, the boat's bow points to -Y (Unity +Z).
# .blend files and self-check renders go to art-src/m1/out/ (gitignored).
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, Matrix, Euler, noise

try:
    HERE = os.path.dirname(os.path.abspath(__file__))
except NameError:
    HERE = r'C:\Users\asus\AppData\Roaming\Panoptes\worktrees\w-pb-m1\art-src\m1'
REPO = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out'); REN = os.path.join(OUT, 'renders'); ART = os.path.join(REPO, 'Assets', 'Game', 'Art')
for _d in (OUT, REN, ART): os.makedirs(_d, exist_ok=True)
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
WHAT = ARGV[0] if ARGV else 'all'
RENDER = 'norender' not in ARGV

PAL = dict(dark='#805C52', ink='#3F2C27', paper='#FFFBF2', cream2='#F8EFDD', cream3='#EEDFC4', red='#D9483B',
           poppy='#E2553F', sun='#F7C548', teal='#46D6C8', blue='#2160B8', sky='#3A86E0', leaf='#7DBE6A',
           peach='#F6A27A', wood='#B9825A', midwood='#9A6A48', weath='#C9A882', greyw='#A89A8A', rope='#D8C08A',
           iron='#5E5A66', rust='#A2583A', rock='#9A93A6', rock2='#7C7690', sand='#EAD7A8', grass='#8CC46A',
           grass2='#6FAF5A')
NAMES = list(PAL); CI = {n: i for i, n in enumerate(NAMES)}


def srgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def lerp(a, b, t): return a + (b - a) * t
def sstep(a, b, x): t = clamp((x - a) / (b - a)); return t * t * (3 - 2 * t)
def D(deg): return math.radians(deg)
def M(loc=(0, 0, 0), rot=(0, 0, 0)): return Matrix.Translation(Vector(loc)) @ Euler(rot, 'XYZ').to_matrix().to_4x4()
def frange(a, b, s): n = int(round((b - a) / s)); return [a + s * i for i in range(n + 1)]


R = random.Random(7)          # the hand-made wobble; reseeded per asset so each build is identical
def J(a): return R.uniform(-a, a)


# ---------------------------------------------------------------- mesh building ------------------------------------------------
class MB:
    """Collects primitive bmeshes into one mesh; every face carries a palette index (written as the Col attribute)."""
    def __init__(s): s.bm = bmesh.new(); s.lay = s.bm.faces.layers.int.new('ci')

    def add(s, src, col=None, mat=None):
        if mat is not None: bmesh.ops.transform(src, matrix=mat, verts=src.verts)
        src.normal_update()
        sl = src.faces.layers.int.get('ci')
        vm = {v: s.bm.verts.new(v.co) for v in src.verts}
        for f in src.faces:
            try: nf = s.bm.faces.new([vm[v] for v in f.verts])
            except ValueError: continue
            nf[s.lay] = f[sl] if col is None else CI[col(f.calc_center_median(), f.normal) if callable(col) else col]
        src.free(); return s


def put(mb, bm, col, loc=(0, 0, 0), rot=(0, 0, 0)): mb.add(bm, col, M(loc, rot))


_MAT = {}
def toon_mat():
    """PB_Toon: Unity swaps it for its own toon shader; here it is a 2-tone cel ramp x vertex colour for the renders."""
    if 'm' in _MAT: return _MAT['m']
    m = bpy.data.materials.new('PB_Toon'); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); dif = nt.nodes.new('ShaderNodeBsdfDiffuse')
    s2r = nt.nodes.new('ShaderNodeShaderToRGB'); ramp = nt.nodes.new('ShaderNodeValToRGB')
    vc = nt.nodes.new('ShaderNodeVertexColor'); vc.layer_name = 'Col'
    mul = nt.nodes.new('ShaderNodeMix'); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs['Factor'].default_value = 1
    em = nt.nodes.new('ShaderNodeEmission')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[0].color = (0.62, 0.66, 0.82, 1)
    ramp.color_ramp.elements[1].position = 0.55; ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs['Color'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], mul.inputs['A']); nt.links.new(vc.outputs['Color'], mul.inputs['B'])
    nt.links.new(mul.outputs['Result'], em.inputs['Color']); nt.links.new(em.outputs[0], out.inputs['Surface'])
    _MAT['m'] = m; return m


def finish(mb, name, coll, angle=50):
    """bmesh -> object: Col corner colours (sRGB bytes), smooth + sharp-by-angle, weighted normals (flat faces stay flat)."""
    me = bpy.data.meshes.new(name); mb.bm.to_mesh(me); mb.bm.free()
    vals = [0] * len(me.polygons); me.attributes['ci'].data.foreach_get('value', vals)
    rgb = [srgb(PAL[n]) for n in NAMES]; cols = [0.0] * (len(me.loops) * 4)
    for p in me.polygons:
        c = rgb[vals[p.index]] + (1.0,)
        for li in range(p.loop_start, p.loop_start + p.loop_total): cols[li * 4:li * 4 + 4] = c
    me.attributes.remove(me.attributes['ci'])
    ca = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER'); ca.data.foreach_set('color_srgb', cols)
    me.color_attributes.active_color = ca
    me.polygons.foreach_set('use_smooth', [True] * len(me.polygons))
    me.set_sharp_from_angle(angle=D(angle))
    me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    wn = ob.modifiers.new('WeightedNormal', 'WEIGHTED_NORMAL'); wn.keep_sharp = True; wn.weight = 50; wn.mode = 'FACE_AREA'
    return ob


def col_box(name, lo, hi, coll):
    """A collider: an axis-aligned 8-vertex box in the asset's local space (object at the origin, identity transform)."""
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector([hi[k] if v.co[k] > 0 else lo[k] for k in range(3)])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob); ob.hide_render = True; ob.display_type = 'WIRE'
    return ob


def empty(name, loc, coll, parent=None):
    ob = bpy.data.objects.new(name, None); ob.empty_display_type = 'PLAIN_AXES'; ob.empty_display_size = 0.15
    coll.objects.link(ob)
    if parent: ob.parent = parent
    ob.location = loc; return ob


def bev_edges(bm, edges, w, seg):
    edges = [e for e in edges if e.is_valid]
    if not edges or w <= 0: return
    bmesh.ops.bevel(bm, geom=list({v for e in edges for v in e.verts}) + edges, offset=w, offset_type='OFFSET',
                    segments=seg, profile=0.5, affect='EDGES', clamp_overlap=True)


def bev_sharp(bm, w, seg, ang=50):
    bm.normal_update()
    bev_edges(bm, [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > D(ang)], w, seg)


def box(sx, sy, sz, bevel=0.02, seg=2):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts)
    if bevel: bev_edges(bm, bm.edges[:], min(bevel, 0.3 * min(sx, sy, sz)), seg)
    return bm


def board(x0, x1, y0, y1, z0, ztop, bevel=0.012, seg=1, lean=(0, 0)):
    """A box from bounds; ztop may be a function (x, y) for sloped tops; lean shifts the top (hand-made wobble)."""
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        X = x1 if v.co.x > 0 else x0; Y = y1 if v.co.y > 0 else y0
        if v.co.z > 0: v.co = Vector((X + lean[0], Y + lean[1], ztop(X, Y) if callable(ztop) else ztop))
        else: v.co = Vector((X, Y, z0))
    bev_edges(bm, bm.edges[:], min(bevel, 0.3 * min(x1 - x0, y1 - y0)), seg)
    return bm


def cyl(r, h, n=8, r2=None, bevel=0.015, seg=1):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=n, radius1=r, radius2=r if r2 is None else r2, depth=h)
    if bevel: bev_sharp(bm, bevel, seg, 60)
    return bm


def blob(sub, rad, seed, amp=0.18, freq=1.2):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    o = Vector((seed * 7.13, seed * 3.71, seed * 1.93))
    for v in bm.verts:
        v.co *= 1 + amp * noise.noise(v.co * freq + o)
        v.co = Vector((v.co.x * rad[0], v.co.y * rad[1], v.co.z * rad[2]))
    return bm


def circle(r, n=8, ph=0.0): return [(r * math.cos(ph + 2 * math.pi * i / n), r * math.sin(ph + 2 * math.pi * i / n)) for i in range(n)]


def rrect(a0, a1, b0, b1, r, segs=(2, 2, 2, 2)):
    """Rounded rectangle profile, CCW; segs per corner (top-right, top-left, bottom-left, bottom-right)."""
    pts = []
    for (ca, cb, st), sg in zip(((a1 - r, b1 - r, 0), (a0 + r, b1 - r, 90), (a0 + r, b0 + r, 180), (a1 - r, b0 + r, 270)), segs):
        for k in range(sg + 1):
            ang = D(st + 90 * k / max(sg, 1)) if sg else D(st + 45)
            pts.append((ca + r * math.cos(ang), cb + r * math.sin(ang)))
    return pts


def sweep(path, prof, closed=False, cap=True, up=(0, 0, 1), side=None):
    """Sweep a 2D profile (a along 'side', b along 'normal') along a path; prof may be one profile or one per point."""
    P = [Vector(p) for p in path]; n = len(P)
    profs = prof if isinstance(prof[0][0], (tuple, list)) else [prof] * n
    bm = bmesh.new(); rings = []; upv = Vector(up)
    for i, p in enumerate(P):
        t = (P[(i + 1) % n] - P[i - 1]) if closed else (P[min(i + 1, n - 1)] - P[max(i - 1, 0)])
        t.normalize()
        if side is not None: sv = Vector(side); sv = sv - t * sv.dot(t)
        else: sv = t.cross(upv)
        if sv.length < 1e-6: sv = t.cross(Vector((1, 0, 0)))
        sv.normalize(); nv = sv.cross(t)
        rings.append([bm.verts.new(p + sv * a + nv * b) for a, b in profs[i]])
    m = len(rings[0])
    for i in range(n if closed else n - 1):
        A, B = rings[i], rings[(i + 1) % n]
        for j in range(m): bm.faces.new((A[j], A[(j + 1) % m], B[(j + 1) % m], B[j]))
    if cap and not closed: bm.faces.new(rings[0][::-1]); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def slab(poly, z0, z1, bevel=0.012, seg=2):
    bm = bmesh.new(); n = len(poly)
    lo = [bm.verts.new((x, y, z0)) for x, y in poly]; hi = [bm.verts.new((x, y, z1)) for x, y in poly]
    bm.faces.new(lo[::-1]); bm.faces.new(hi)
    for i in range(n): bm.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bev_sharp(bm, bevel, seg, 30)
    return bm


def clip(poly, clipper):
    """Sutherland-Hodgman: poly clipped by a convex CCW polygon."""
    out = list(poly)
    for i in range(len(clipper)):
        a, b = clipper[i], clipper[(i + 1) % len(clipper)]
        inp, out = out, []
        if not inp: break
        ins = lambda p: (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]) >= 0
        def inter(p, q):
            dx, dy = q[0] - p[0], q[1] - p[1]; ex, ey = b[0] - a[0], b[1] - a[1]
            den = dx * ey - dy * ex
            u = ((a[0] - p[0]) * ey - (a[1] - p[1]) * ex) / den if den else 0
            return (p[0] + u * dx, p[1] + u * dy)
        s = inp[-1]
        for e in inp:
            if ins(e):
                if not ins(s): out.append(inter(s, e))
                out.append(e)
            elif ins(s): out.append(inter(s, e))
            s = e
    return out


def zsplit(bm, *zs):
    for z in zs:
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-5, plane_co=(0, 0, z), plane_no=(0, 0, 1))


def torus(c, R_, r, axis='Z', n=10, m=6):
    c = Vector(c)
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n; ca, sa = R_ * math.cos(a), R_ * math.sin(a)
        pts.append(c + (Vector((ca, sa, 0)) if axis == 'Z' else Vector((ca, 0, sa)) if axis == 'Y' else Vector((0, ca, sa))))
    upv = {'Z': (0, 0, 1), 'Y': (0, 1, 0), 'X': (1, 0, 0)}[axis]
    return sweep(pts, circle(r, m), closed=True, up=upv)


def coil(mb, c, r, turns=3, tube=0.022, col='rope'):
    for k in range(turns):
        mb.add(torus((c[0] + J(0.01), c[1] + J(0.01), c[2] + tube + k * tube * 1.7), r - k * 0.012, tube, 'Z', 9, 5), col)


def new_scene(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _MAT.clear()
    coll = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(coll)
    return coll


def tris_of(obs):
    dg = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in obs:
        if o.type != 'MESH': continue
        me = o.evaluated_get(dg).to_mesh(); n += sum(len(p.vertices) - 2 for p in me.polygons); o.evaluated_get(dg).to_mesh_clear()
    return n


def export(coll, fname):
    for o in bpy.context.scene.objects: o.select_set(False)
    obs = list(coll.all_objects)
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    path = os.path.join(ART, fname)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, apply_unit_scale=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, colors_type='SRGB')
    vis = [o for o in obs if o.type == 'MESH' and '_col' not in o.name]
    print('EXPORTED', path, 'tris visual', tris_of(vis), 'total', tris_of(obs))
    print('OBJECTS', sorted(o.name for o in obs))
    print('PER-OBJECT TRIS', {o.name: tris_of([o]) for o in vis})
    return path


# ---------------------------------------------------------------- render ------------------------------------------------------
def setup_render(sea_size=60):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = 1280; sc.render.resolution_y = 720
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True
    bg = wd.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.45, 0.68, 0.93, 1); bg.inputs[1].default_value = 0.8
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3.2; sun.data.angle = D(3)
    sun.rotation_euler = (D(50), 0, D(-40)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_end = 2000
    # the sea: a flat teal plane at the waterline (render only)
    bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=sea_size)
    me = bpy.data.meshes.new('sea'); bm.to_mesh(me); bm.free()
    sm = bpy.data.materials.new('sea'); sm.use_nodes = True; nt = sm.node_tree; nt.nodes.clear()
    o = nt.nodes.new('ShaderNodeOutputMaterial'); e = nt.nodes.new('ShaderNodeEmission')
    e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL['teal'])) + (1,)
    nt.links.new(e.outputs[0], o.inputs['Surface']); me.materials.append(sm)
    sea = bpy.data.objects.new('sea', me); sc.collection.objects.link(sea)
    return cam


def shoot(cam, name, loc, target, lens=35):
    cam.location = loc; cam.data.lens = lens
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = os.path.join(REN, name); bpy.ops.render.render(write_still=True)
    print('RENDER', os.path.join(REN, name))


# ================================================================ ASSET 1: BISCUIT =============================================
NS = 16                                           # stations bow (t=0) -> transom (t=1), denser at the bow
TST = [(i / NS) ** 1.25 for i in range(NS + 1)]
KE = [0, 0.25, 0.42, 0.56, 0.69, 0.82, 1.0]        # strake edges as girth fractions, keel -> sheer (6 strakes)
K = len(KE) - 1
LAP, SKIN = 0.05, 0.035                            # clinker lap step, hull skin thickness


def zs(t): return 0.555 + 0.19 * max(0, (0.45 - t) / 0.45) ** 2 + 0.04 * max(0, (t - 0.55) / 0.45) ** 2   # sheer (skin top)
def zk(t): return -0.245 + 0.165 * max(0, 1 - t / 0.28) ** 2 + 0.065 * max(0, (t - 0.7) / 0.3) ** 2        # skin bottom at the keel
def fore(t, tm, p): return 1 - (1 - min(t / tm, 1)) ** p
def Bs(t): return 0.925 * fore(t, 0.5, 3.6) - (0.09 * ((t - 0.55) / 0.45) ** 2 if t > 0.55 else 0)          # half-breadth at the sheer
def Bb(t): return 0.87 * fore(t, 0.5, 5.8) - (0.04 * ((t - 0.6) / 0.4) ** 2 if t > 0.6 else 0)             # half-breadth at the bilge


def bez(P, u):
    a, b, c, d = (1 - u) ** 3, 3 * u * (1 - u) ** 2, 3 * u * u * (1 - u), u ** 3
    return (a * P[0][0] + b * P[1][0] + c * P[2][0] + d * P[3][0], a * P[0][1] + b * P[1][1] + c * P[2][1] + d * P[3][1])


class Sec:
    """Hull cross-section at station t (half, +x side) as a cubic Bezier from keel to sheer, sampled by girth fraction."""
    def __init__(s, t):
        k, z1, bb, bs = zk(t), zs(t), Bb(t), Bs(t)
        v = max(0, 1 - t / 0.35) ** 2                      # V-ness toward the bow
        P = [(0, k), (bb * (0.68 - 0.3 * v), k + 0.035 + 0.1 * v * (z1 - k)), (bb, k + 0.12 + 0.25 * v * (z1 - k)), (bs, z1)]
        s.p = [bez(P, i / 40) for i in range(41)]
        L = [0.0]
        for i in range(40): L.append(L[-1] + math.hypot(s.p[i + 1][0] - s.p[i][0], s.p[i + 1][1] - s.p[i][1]))
        s.L = [l / L[-1] for l in L]

    def at(s, a):
        i = 0
        while i < 39 and s.L[i + 1] < a: i += 1
        f = clamp((a - s.L[i]) / max(1e-9, s.L[i + 1] - s.L[i]))
        (x0, z0), (x1, z1) = s.p[i], s.p[i + 1]
        tx, tz = x1 - x0, z1 - z0; ln = math.hypot(tx, tz) or 1
        return x0 + f * tx, z0 + f * tz, tz / ln, -tx / ln


SEC = [Sec(t) for t in TST]


def ybow(z):
    z0, z1 = zk(0), zs(0); f = clamp((z1 - z) / (z1 - z0)); return -2.445 + 0.20 * f ** 1.6
def ystern(z):
    z0, z1 = zk(1), zs(1); return 2.27 + 0.175 * (z - z0) / (z1 - z0)
def yst(t, z): return ybow(z) + t * (ystern(z) - ybow(z))


def dent(i, k, s, sd):         # a dent in the starboard topsides, aft of amidships
    if sd != -1: return 0.0
    g = k + 1 - s
    return 0.035 * max(0, 1 - abs(i - 11) / 1.6) * max(0, 1 - abs(g - 3.6) / 1.1)


def hull_pt(t, k, s, sd, S=None, i=None):
    S = S or Sec(t)
    a = KE[k + 1] + (KE[k] - KE[k + 1]) * s
    lap = LAP * min(1, t / 0.12)
    off = lap * s * (0.5 if k == 0 else 1.0) - (dent(i, k, s, sd) if i is not None else 0)
    x, z, nx, nz = S.at(a); x += nx * off; z += nz * off
    return Vector((sd * x, yst(t, z), z))


def outer_ring(i, sd, tags=False):
    ks = [(k, s) for k in range(K - 1, -1, -1) for s in ((0, 0.5, 1) if k < 2 else (0, 1))]   # mid verts only where the bilge curves
    return [k for k, s in ks] if tags else [hull_pt(TST[i], k, s, sd, SEC[i], i) for k, s in ks]


def inner_ring(i, sd, n=7):
    out = []
    for m in range(n):
        x, z, nx, nz = SEC[i].at(1 - m / (n - 1)); x = max(0.0, x - nx * SKIN); z -= nz * SKIN
        out.append(Vector((sd * x, yst(TST[i], z), z)))
    return out


_HW = {}
def hw_line(z):
    """[(y, x)] the inner skin's half-width at height z, station by station, bow first."""
    key = round(z, 4)
    if key in _HW: return _HW[key]
    pts = []
    for i in range(NS + 1):
        prev = None
        for m in range(61):
            x, zz, nx, nz = SEC[i].at(m / 60); xi, zi = max(0.0, x - nx * SKIN), zz - nz * SKIN
            if prev and (prev[1] - z) * (zi - z) <= 0 and prev[1] != zi:
                f = (z - prev[1]) / (zi - prev[1]); pts.append((yst(TST[i], z), prev[0] + f * (xi - prev[0]))); break
            prev = (xi, zi)
    pts.sort(); _HW[key] = pts; return pts


def inner_hw(y, z):
    L = hw_line(z)
    if not L or y < L[0][0] or y > L[-1][0]: return 0.0
    for (y0, x0), (y1, x1) in zip(L, L[1:]):
        if y0 <= y <= y1: return x0 + (x1 - x0) * (y - y0) / max(1e-9, y1 - y0)
    return L[-1][1]


PATCH = {(-1, 4): (6, 9, 'greyw'), (1, 3): (10, 13, 'weath')}
def strake_col(sd, k, i):
    for (psd, pk), (i0, i1, c) in PATCH.items():
        if psd == sd and pk == k and i0 <= i < i1: return c
    return 'red' if k == K - 1 else 'paper' if k % 2 == 0 else 'cream2'


WL = 0.08                     # anti-fouling line, just above the waterline


def build_hull(mb):
    # --- outer clinker skin (both sides, stitched at keel + stem) ---
    bm = bmesh.new(); lay = bm.faces.layers.int.new('ci'); tg = outer_ring(0, 1, True)
    for sd in (1, -1):
        Rg = [[bm.verts.new(p) for p in outer_ring(i, sd)] for i in range(NS + 1)]
        for i in range(NS):
            for j in range(len(Rg[0]) - 1):
                q = [Rg[i][j], Rg[i][j + 1], Rg[i + 1][j + 1], Rg[i + 1][j]]
                if sd < 0: q.reverse()
                f = bm.faces.new(q); f[lay] = CI[strake_col(sd, tg[j], i)]
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
    zsplit(bm, WL)
    for f in bm.faces:
        if f.calc_center_median().z < WL: f[lay] = CI['rust']
    mb.add(bm)
    # --- inner skin (painted cream inside) ---
    bm = bmesh.new()
    for sd in (1, -1):
        Rg = [[bm.verts.new(p) for p in inner_ring(i, sd)] for i in range(NS + 1)]
        for i in range(NS):
            for j in range(len(Rg[0]) - 1):
                q = [Rg[i][j], Rg[i][j + 1], Rg[i + 1][j + 1], Rg[i + 1][j]]
                if sd > 0: q.reverse()
                bm.faces.new(q)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0006)
    mb.add(bm, 'cream3')
    # --- transom: a raked panel, red band on top, cream, anti-fouling below ---
    Lp, Ls = outer_ring(NS, 1), outer_ring(NS, -1)
    z1 = zs(1)
    loop = Lp + Ls[::-1][1:] + [Vector((0, ystern(z1 + 0.03), z1 + 0.03))]
    nt = Vector((0, 1, -0.175 / (zs(1) - zk(1)))).normalized()
    cf = Vector((0, ystern(0.25), 0.25)); cb = cf + nt * 0.05
    bm = bmesh.new(); lay = bm.faces.layers.int.new('ci')
    F = [bm.verts.new(p) for p in loop]; B = [bm.verts.new(p + nt * 0.05 + (cb - (p + nt * 0.05)).normalized() * 0.012) for p in loop]
    vf, vb = bm.verts.new(cf), bm.verts.new(cb); n = len(loop)
    for j in range(n):
        a, b = j, (j + 1) % n
        bm.faces.new((vf, F[b], F[a]))[lay] = CI['cream3']
        bm.faces.new((vb, B[a], B[b]))[lay] = CI['paper']
        bm.faces.new((F[a], F[b], B[b], B[a]))[lay] = CI['paper']
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    zsplit(bm, WL, z1 - 0.22)
    for f in bm.faces:
        if f[lay] == CI['cream3']: continue
        c = f.calc_center_median(); f[lay] = CI['rust' if c.z < WL else 'red' if c.z > z1 - 0.22 else 'paper']
    mb.add(bm)
    # --- gunwale + rubrail (warm wood), both sides ---
    prof = rrect(-0.075, 0.045, -0.08, 0.045, 0.02, (1, 1, 0, 0))
    for sd in (1, -1):
        path = [hull_pt(TST[i], K - 1, 0, sd, SEC[i]) for i in range(NS + 1)]
        path.append(path[-1] + Vector((0, 0.05, 0)))
        mb.add(sweep(path, [(a * sd, b) for a, b in prof]), 'wood')
    # transom cap
    ytop = lambda z: ystern(z) + 0.025
    path = [Vector((-Bs(1) - 0.05, ytop(z1), z1)), Vector((-Bs(1) * 0.5, ytop(z1 + 0.022), z1 + 0.022)), Vector((0, ytop(z1 + 0.03), z1 + 0.03)),
            Vector((Bs(1) * 0.5, ytop(z1 + 0.022), z1 + 0.022)), Vector((Bs(1) + 0.05, ytop(z1), z1))]
    mb.add(sweep(path, rrect(-0.055, 0.055, -0.035, 0.045, 0.018, (1, 1, 0, 0))), 'wood')
    # --- keel + stem post (one swept backbone) ---
    path = [Vector((0, ystern(zk(1)) + 0.05, zk(1)))] + [Vector((0, yst(TST[i], zk(TST[i])), zk(TST[i]))) for i in range(NS, 0, -1)]
    z0, ztop = zk(0), 0.80
    for m in range(0, 6):
        z = z0 + (ztop - z0) * m / 5; path.append(Vector((0, ybow(min(z, zs(0))) - (0.012 if m else 0), z)))
    bm = sweep(path, rrect(-0.045, 0.045, -0.03, 0.05, 0.02, (1, 0, 0, 1)), side=(1, 0, 0))
    zsplit(bm, WL)
    mb.add(bm, lambda c, n: 'rust' if c.z < WL else 'wood')


def build_interior(mb):
    # floorboards: lengthwise duckboard slats, top at z=0.12
    n, W, gap = 8, 1.50, 0.025; w = (W - (n - 1) * gap) / n
    for k in range(n):
        x0 = -W / 2 + k * (w + gap); x1 = x0 + w; xo = max(abs(x0), abs(x1)) + 0.015
        yf = -1.75
        while inner_hw(yf, 0.075) < xo and yf < 0: yf += 0.02
        yr = 2.075
        L = yr - yf; cy = (yf + yr) / 2
        put(mb, box(w, L - abs(J(0.02)), 0.045, 0.014, 1), 'wood' if k != 5 else 'weath', ((x0 + x1) / 2, cy, 0.0975), (0, D(J(1.2)), D(J(0.15))))
    # floor battens peeking out at the ends
    # ribs (bent frames) inside
    for y in (-1.5, -0.55, 0.35, 1.15, 1.8):
        zt = zs(clamp((y - ybow(0.3)) / (ystern(0.3) - ybow(0.3)))) - 0.08
        for sd in (1, -1):
            path = [Vector((sd * (inner_hw(y, z) - 0.016), y, z)) for z in [0.05 + (zt - 0.05) * m / 4 for m in range(5)]]
            mb.add(sweep(path, rrect(-0.017, 0.017, -0.026, 0.026, 0.008, (0, 0, 0, 0)), up=(0, 1, 0)), 'midwood')
    # risers (seat stringers) along both sides at z~0.36
    ys = frange(-1.6, 2.36, 0.44)
    for sd in (1, -1):
        mb.add(sweep([Vector((sd * (inner_hw(y, 0.37) - 0.02), y, 0.365)) for y in ys], rrect(-0.019, 0.019, -0.035, 0.035, 0.01, (1, 1, 0, 0))), 'wood')
    # centre thwart, near the bow third (top z=0.45)
    ty = -1.0; hw = min(inner_hw(ty - 0.13, 0.42), inner_hw(ty + 0.13, 0.42)) - 0.005
    put(mb, box(2 * hw, 0.26, 0.05, 0.02, 2), 'wood', (0, ty, 0.425), (0, 0, D(0.8)))
    put(mb, box(0.07, 0.07, 0.30, 0.015, 1), 'midwood', (0, ty, 0.25))          # centre post
    # stern bench (helm seat) with a locker front
    y0, y1 = 2.08, ystern(0.45) - 0.005
    hw = min(inner_hw(y0, 0.42), inner_hw(y1 - 0.04, 0.42)) - 0.005
    for k, (a, b) in enumerate(((y0, y0 + 0.175), (y0 + 0.19, y1))):
        put(mb, box(2 * hw, b - a, 0.05, 0.016, 2), 'wood' if k == 0 else 'weath', (0, (a + b) / 2, 0.425), (0, D(J(0.8)), 0))
    hwf = inner_hw(y0 + 0.02, 0.13) - 0.01
    put(mb, box(2 * hwf, 0.035, 0.30, 0.012, 1), 'midwood', (0, y0 + 0.02, 0.25))
    # foredeck over the front 0.8 m: 4 planks clipped to the hull, deck beam under its edge, a cleat
    zd, ye = 0.58, -1.70
    L = [(y, x) for y, x in hw_line(zd) if y < ye]
    right = [(max(0.0, x - 0.012), y) for y, x in L] + [(inner_hw(ye, zd) - 0.012, ye)]
    right = [p for k, p in enumerate(right) if k == 0 or math.dist(p, right[k - 1]) > 0.03]
    poly = right + [(-x, y) for x, y in reversed(right) if x > 0.001]
    for k in range(4):
        x0 = -0.86 + k * 0.435; x1 = x0 + 0.42
        p = clip([(x0, -3.0), (x1, -3.0), (x1, ye), (x0, ye)], poly)
        if len(p) >= 3: mb.add(slab(p, 0.555, 0.60 + J(0.004), 0.014, 1), 'wood' if k != 2 else 'weath')
    put(mb, box(2 * inner_hw(ye - 0.04, 0.53) - 0.01, 0.08, 0.06, 0.015, 1), 'midwood', (0, ye - 0.035, 0.525))
    # horn cleat on the foredeck
    cx, cyy = 0.0, -2.0
    put(mb, box(0.07, 0.16, 0.05, 0.015, 1), 'dark', (cx, cyy, 0.625))
    put(mb, box(0.06, 0.32, 0.05, 0.02, 2), 'dark', (cx, cyy, 0.665))


def build_details(mb):
    # chipped paint: little cream-3 flakes on the red sheer strake (and the transom band)
    rr = random.Random(5)
    for sd, i, s in ((1, 6.3, 0.4), (1, 10.6, 0.65), (1, 15.2, 0.35), (-1, 4.7, 0.5), (-1, 9.4, 0.3), (-1, 12.8, 0.6), (-1, 17.6, 0.45)):
        i = i * NS / 20; t = lerp(TST[int(i)], TST[int(i) + 1], i - int(i)); S = Sec(t)
        p = hull_pt(t, K - 1, s, sd, S); pa = hull_pt(t, K - 1, s + 0.08, sd, S)
        t2 = t + 0.01; pb = hull_pt(t2, K - 1, s, sd)
        nrm = (pb - p).cross(pa - p).normalized()
        if nrm.dot(Vector((sd, 0, 0))) < 0: nrm = -nrm
        u = (pb - p).normalized(); v = nrm.cross(u)
        rad = rr.uniform(0.04, 0.065)
        poly = [p + nrm * 0.007 + u * math.cos(a) * rad * rr.uniform(0.6, 1.3) + v * math.sin(a) * rad * rr.uniform(0.6, 1.0)
                for a in [2 * math.pi * q / 6 for q in range(6)]]
        bm = bmesh.new(); vs = [bm.verts.new(c) for c in poly]; f = bm.faces.new(vs); bm.normal_update()
        if f.normal.dot(nrm) < 0: f.normal_flip()
        mb.add(bm, 'cream3')
    # oars lashed under the thwart, lying along the port side
    for k, (x, z) in enumerate(((0.64, 0.155), (0.58, 0.21))):
        ya, yb = -1.55 + k * 0.08, 0.55 + k * 0.08
        mb.add(sweep([Vector((x, ya + 0.45, z)), Vector((x, yb, z))], circle(0.024, 6)), 'weath')
        put(mb, box(0.13, 0.46, 0.04, 0.015, 1), 'weath', (x, ya + 0.25, z), (0, D(8 + k * 6), 0))
        put(mb, box(0.135, 0.09, 0.042, 0.015, 1), 'poppy', (x, ya + 0.04, z), (0, D(8 + k * 6), 0))
    for y in (-1.0, -0.2):
        mb.add(torus((0.61, y, 0.18), 0.075, 0.02, 'X', 8, 4), 'rope')
    # bait bucket (stern bench, port) with bait showing
    bm = cyl(0.13, 0.26, 8, r2=0.15, bevel=0.012); bm.normal_update()
    mb.add(bm, lambda c, n: 'peach' if n.z > 0.9 else 'blue', M((0.58, 2.18, 0.58)))
    mb.add(sweep([Vector((0.58 + 0.15 * math.cos(a), 2.18, 0.71 + 0.12 * math.sin(a))) for a in [D(10 + 32 * q) for q in range(6)]],
                 circle(0.02, 5), up=(0, 1, 0)), 'iron')
    # rope coil on the stern bench, starboard
    coil(mb, (-0.55, 2.2, 0.45), 0.15, 2)
    # lantern on a crook at the starboard stern quarter
    path = [(-0.70, 2.28, 0.55), (-0.70, 2.28, 1.05), (-0.68, 2.28, 1.16), (-0.60, 2.28, 1.21), (-0.52, 2.28, 1.17), (-0.51, 2.28, 1.11)]
    mb.add(sweep(path, circle(0.021, 6), up=(0, 1, 0)), 'iron')
    lx, ly = -0.51, 2.28
    mb.add(torus((lx, ly, 1.07), 0.045, 0.02, 'Y', 8, 5), 'iron')
    put(mb, cyl(0.095, 0.07, 8, r2=0.03, bevel=0.01), 'iron', (lx, ly, 1.005))
    put(mb, box(0.13, 0.13, 0.15, 0.025, 2), 'sun', (lx, ly, 0.90))
    put(mb, box(0.16, 0.16, 0.045, 0.015, 1), 'iron', (lx, ly, 0.815))
    # anchor on a rope coil on the foredeck, rope to the cleat
    coil(mb, (0.36, -2.02, 0.60), 0.13, 2)
    ax, ay, az = -0.2, -1.86, 0.625
    put(mb, box(0.05, 0.40, 0.045, 0.015, 1), 'iron', (ax, ay, az), (0, 0, D(-12)))
    put(mb, cyl(0.022, 0.30, 6, bevel=0.008), 'iron', (ax + 0.035, ay + 0.15, az), (0, D(90), D(-12)))
    arm = [Vector((ax - 0.15 * math.sin(a), ay - 0.16 - 0.05 * math.cos(a), az)) for a in [D(-80 + 32 * q) for q in range(6)]]
    mb.add(sweep(arm, circle(0.024, 6)), 'iron')
    for e in (arm[0], arm[-1]): put(mb, box(0.09, 0.07, 0.04, 0.015, 1), 'iron', (e.x, e.y + 0.02, az))
    mb.add(torus((ax - 0.045, ay + 0.22, az), 0.04, 0.018, 'Z', 8, 5), 'iron')
    mb.add(sweep([(ax - 0.04, ay + 0.26, az), (-0.15, -1.95, 0.64), (-0.04, -2.0, 0.68), (0.2, -2.03, 0.63), (0.3, -2.02, 0.65)], circle(0.02, 6)), 'rope')
    # rope tied round the starboard gunwale near the bow, with a fender hanging over the side
    yfw = -1.75; tf = clamp((yfw - ybow(0.5)) / (ystern(0.5) - ybow(0.5)))
    gp = hull_pt(tf, K - 1, 0, -1)
    for q in range(2): mb.add(torus((gp.x + 0.015, yfw + q * 0.045, gp.z - 0.015), 0.085, 0.02, 'Y', 9, 5), 'rope')
    xo = -(inner_hw(yfw, 0.25) + SKIN + 0.02)
    mb.add(sweep([(gp.x - 0.07, yfw, gp.z + 0.02), (gp.x - 0.1, yfw, gp.z - 0.05), (xo - 0.07, yfw, 0.45), (xo - 0.07, yfw, 0.36)], circle(0.02, 6), up=(0, 1, 0)), 'rope')
    put(mb, cyl(0.08, 0.30, 8, bevel=0.035, seg=2), 'blue', (xo - 0.07, yfw, 0.20))
    # swim ladder over the transom, port side of the motor: from the gunwale to 0.6 m under the waterline
    for x in (0.34, 0.56):
        path = [(x, 2.30, 0.56), (x, 2.33, 0.69), (x, 2.43, 0.735), (x, 2.53, 0.71), (x, 2.575, 0.62), (x, 2.585, 0.4), (x, 2.59, -0.62)]
        mb.add(sweep(path, circle(0.022, 6), up=(0, 1, 0)), 'iron')
    for z in (0.25, -0.15, -0.55):
        put(mb, box(0.27, 0.065, 0.045, 0.015, 1), 'wood', (0.45, 2.59, z))


def build_motor(coll):
    mb = MB()
    # clamp bracket hugging the transom top (pivot = origin = mount point)
    put(mb, box(0.20, 0.20, 0.07, 0.02, 1), 'iron', (0, -0.02, 0.0))
    put(mb, box(0.16, 0.045, 0.17, 0.015, 1), 'iron', (0, -0.10, -0.08))
    put(mb, box(0.18, 0.06, 0.24, 0.02, 1), 'iron', (0, 0.06, -0.11))
    for x in (-0.05, 0.05): put(mb, cyl(0.025, 0.12, 6, bevel=0.008), 'ink', (x, -0.155, -0.12), (D(90), 0, 0))
    put(mb, box(0.16, 0.14, 0.34, 0.03, 1), 'iron', (0, 0.15, -0.08))           # swivel housing
    # power head: chunky sunflower cowling with a red belt, darker lower cowl
    put(mb, box(0.32, 0.42, 0.14, 0.035, 1), 'iron', (0, 0.25, 0.10))
    put(mb, box(0.36, 0.48, 0.38, 0.09, 3), 'sun', (0, 0.26, 0.33))
    put(mb, box(0.375, 0.495, 0.075, 0.035, 2), 'red', (0, 0.26, 0.255))
    put(mb, box(0.20, 0.10, 0.05, 0.02, 1), 'cream2', (0, 0.12, 0.49))           # top grab handle
    put(mb, box(0.08, 0.06, 0.05, 0.02, 1), 'red', (0.12, 0.03, 0.36))           # pull-start knob
    # leg, anti-cavitation plate, gearcase torpedo and skeg
    put(mb, box(0.11, 0.20, 1.02, 0.04, 2), 'iron', (0, 0.25, -0.49), (D(-3), 0, 0))
    put(mb, box(0.26, 0.34, 0.04, 0.015, 1), 'iron', (0, 0.30, -0.82))
    rad = [0.035, 0.068, 0.078, 0.072, 0.05]
    tp = [Vector((0, y, -1.04)) for y in (0.06, 0.11, 0.24, 0.36, 0.42)]
    mb.add(sweep(tp, [circle(r, 8) for r in rad], side=(1, 0, 0)), 'iron')
    put(mb, box(0.045, 0.16, 0.16, 0.018, 1), 'iron', (0, 0.32, -1.14))
    # tiller arm into the boat, rubber grip
    mb.add(sweep([(0, 0.06, 0.20), (0, -0.20, 0.215), (0, -0.40, 0.235)], circle(0.03, 8)), 'iron')
    mb.add(sweep([(0, -0.40, 0.235), (0, -0.52, 0.248), (0, -0.66, 0.262)], circle(0.042, 8)), 'ink')
    print('TRIS motor', sum(len(f.verts) - 2 for f in mb.bm.faces))
    ob = finish(mb, 'Motor', coll); ob.location = (0, 2.50, 0.66)
    # propeller: own object, spins about its local Y (shaft) axis
    pm = MB()
    pm.add(sweep([(0, -0.06, 0), (0, 0.03, 0), (0, 0.075, 0), (0, 0.10, 0)], [circle(r, 8) for r in (0.045, 0.05, 0.04, 0.02)], side=(1, 0, 0)), 'iron')
    for k in range(3):
        pm.add(box(0.11, 0.04, 0.13, 0.018, 2), 'iron', Matrix.Rotation(D(120 * k), 4, 'Y') @ Matrix.Translation((0, 0, 0.10)) @ Matrix.Rotation(D(32), 4, 'Z'))
    pr = finish(pm, 'Prop', coll); pr.parent = ob; pr.location = (0, 0.46, -1.04)
    empty('socket_tiller_grip', (0, -0.64, 0.26), coll, ob)
    return ob


def build_boat():
    global R
    R = random.Random(7)
    coll = new_scene('Biscuit')
    mb = MB(); nt = lambda: sum(len(f.verts) - 2 for f in mb.bm.faces)
    build_hull(mb); a = nt(); build_interior(mb); b = nt(); build_details(mb); c = nt()
    print('TRIS hull %d interior %d details %d' % (a, b - a, c - b))
    hull = finish(mb, 'Hull', coll)
    build_motor(coll)
    # ---- colliders (boat local space) ----
    col_box('Deck_colDeck', (-0.79, -1.72, 0.02), (0.79, 2.08, 0.12), coll)
    segs = [(-2.10, -1.70, 0.60, 0.80), (-1.70, -1.32, 0.12, 0.72), (-1.32, -0.80, 0.12, 0.68), (-0.80, 1.40, 0.12, 0.64), (1.40, 2.45, 0.12, 0.68)]
    for sd, nm in ((1, 'L'), (-1, 'R')):
        for k, (ya, yb, za, zb) in enumerate(segs):
            xin = min(inner_hw(y, max(za, 0.15)) for y in frange(ya, yb, (yb - ya) / 6) if inner_hw(y, max(za, 0.15)) > 0.05)
            xout = 0.975
            lo, hi = (xin, ya, za), (xout, yb, zb)
            if sd < 0: lo, hi = (-xout, ya, za), (-xin, yb, zb)
            col_box(('Side_%s_colBox' % nm) if k == 3 else ('Side%s%d_colBox' % (nm, k)), lo, hi, coll)
    col_box('Bow_colBox', (-0.45, -2.50, 0.60), (0.45, -2.10, 0.80), coll)
    col_box('Stern_colBox', (-0.82, 2.40, 0.12), (0.82, 2.52, 0.68), coll)
    col_box('Thwart_colBox', (-0.86, -1.13, 0.12), (0.86, -0.87, 0.45), coll)
    col_box('SternBench_colBox', (-0.84, 2.08, 0.12), (0.84, 2.41, 0.45), coll)
    col_box('Foredeck_colBox', (-0.74, -2.30, 0.12), (0.74, -1.70, 0.60), coll)
    col_box('HullBottom_colBox', (-0.92, -2.25, -0.30), (0.92, 2.45, 0.05), coll)
    # ---- sockets ----
    empty('socket_helm', (-0.32, 1.80, 0.12), coll)
    empty('socket_ladder_top', (0.45, 2.45, 0.68), coll)
    empty('socket_ladder_bottom', (0.45, 2.59, -0.55), coll)
    empty('socket_anchor', (0.0, -2.0, 0.69), coll)
    for k, (x, y) in enumerate(((0.55, -1.3), (0.62, 0.0), (0.58, 1.4), (-0.55, -1.3), (-0.62, 0.0), (-0.58, 1.4), (0, -2.0), (0, 2.15))):
        empty('buoy_%d' % k, (x, y, -0.15), coll)
    hwr = inner_hw(0.0, 0.55)
    empty('rail_L', (hwr - 0.5, 0.0, 0.12), coll); empty('rail_R', (-(hwr - 0.5), 0.0, 0.12), coll)
    empty('rail_bow', (0.0, -1.55, 0.12), coll); empty('rail_stern', (0.0, 1.88, 0.12), coll)
    empty('cam_boat', (0.0, 0.2, 1.12), coll)
    # ---- checks ----
    bpy.context.view_layer.update()
    bb = [hull.matrix_world @ Vector(c) for c in hull.bound_box]
    print('HULL BOUNDS', [round(min(v[k] for v in bb), 3) for k in range(3)], [round(max(v[k] for v in bb), 3) for k in range(3)])
    clear = min(inner_hw(y, 0.13) for y in frange(-1.32, 2.08, 0.04))
    print('CLEAR FLOOR half-width min over y -1.32..2.08 at z .13: %.3f (need >= 0.70)' % clear)
    print('inner hw at z=.13:', [(round(y, 2), round(inner_hw(y, 0.13), 2)) for y in frange(-2.0, 2.4, 0.4)])
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'boat_biscuit.blend'))
    export(coll, 'PB_Boat_Biscuit_T1.fbx')
    if RENDER:
        cam = setup_render()
        shoot(cam, 'boat_34front.png', (-4.6, -6.2, 3.6), (0, -0.2, 0.25), 40)
        shoot(cam, 'boat_side.png', (-9.5, -0.3, 1.0), (0, 0.1, 0.25), 45)
        shoot(cam, 'boat_34rear.png', (4.4, 6.6, 3.7), (0, 0.4, 0.3), 40)
        shoot(cam, 'boat_top.png', (0.01, 0.2, 9.5), (0, 0.2, 0), 40)
        shoot(cam, 'boat_bow.png', (1.2, -3.6, 2.4), (0, -1.9, 0.45), 40)


# ================================================================ ASSET 2: WALT'S LANDING =======================================
def coast_rho(x, y):
    cy, rx = 33.0, 35.0; ry = 25.5 if y < cy else 21.0
    dx, dy = x / rx, (y - cy) / ry
    r = math.hypot(dx, dy); th = math.atan2(dy, dx)
    wob = 0.05 * math.sin(3 * th + 1.3) + 0.035 * math.sin(5 * th + 0.4) + 0.02 * math.sin(11 * th + 2.0)
    cove = 0.035 * math.exp(-((x + 1) / 10) ** 2) if y < cy else 0.0
    return r * (1 + wob + cove)


def dist_in(x, y): return (1 - coast_rho(x, y)) * 24.0        # ~metres inland from the waterline


def terrain_h(x, y):
    d = dist_in(x, y)
    if d < 0: return -4 + 4 * sstep(-9, 0, d)
    tw = 1 - sstep(10, 17, math.hypot(x + 3, y - 10))            # Walt's flat landing by the pier root
    bw = lerp(3.0, 1.7, tw)
    if d <= bw: return 1.25 * d / bw
    d2 = d - bw
    top = 7.2 + 1.2 * noise.noise(Vector((x / 17, y / 17, 0.3)))
    rs = lerp(1.0, 10.0, tw)
    zf = 1.25 + 0.08 * min(d2 / 2, 1)
    z = zf + (top - zf) * sstep(rs, rs + 11.0, d2)
    return z + 0.6 * noise.noise(Vector((x / 5, y / 5, 1.7))) * sstep(rs + 1, rs + 5, d2)


def tcol(c, n):
    if c.z < -1.6: return 'rock2'
    if c.z < 1.05 and n.z > 0.45: return 'sand'
    if n.z < 0.64 and c.z > 1.2: return 'rock' if noise.noise(c * 0.21) > -0.15 else 'rock2'
    return 'grass' if noise.noise(c * 0.13 + Vector((3, 1, 0))) > -0.2 else 'grass2'


def grid(xs, ys, fn):
    bm = bmesh.new(); V = [[bm.verts.new((x, y, fn(x, y))) for x in xs] for y in ys]
    for j in range(len(ys) - 1):
        for i in range(len(xs) - 1): bm.faces.new((V[j][i], V[j][i + 1], V[j + 1][i + 1], V[j + 1][i]))
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method='BEAUTY')
    return bm


def build_land(coll):
    mb = MB()
    mb.add(grid(frange(-46, 46, 2.0), [-30, -20, -12, -6] + frange(-2, 64, 2.0), terrain_h), tcol)
    rr = random.Random(11); rocks = []
    hero = [(-3.4, 8.9, 1.5), (3.6, 9.2, 1.25), (-2.2, 7.7, 0.8), (5.2, 8.3, 0.9)]
    for x, y, s in hero: rocks.append((x, y, s))
    tries = 0
    while len(rocks) < 40 and tries < 20000:
        tries += 1
        x, y = rr.uniform(-40, 40), rr.uniform(4, 60); d = dist_in(x, y)
        if abs(x) < 4.5 and y < 17: continue
        land = math.hypot(x + 3, y - 10) < 15 and 1.2 < d < 13
        if land: continue
        if len(rocks) < 18 and -1.5 < d < 2.2: s = rr.uniform(0.9, 2.1)
        elif len(rocks) >= 18 and 4.5 < d < 14: s = rr.uniform(1.8, 3.4)
        else: continue
        if any(math.hypot(x - a, y - b) < (s + c) * 0.9 for a, b, c in rocks): continue
        rocks.append((x, y, s))
    rcols = []
    for k, (x, y, s) in enumerate(rocks):
        rz = s * rr.uniform(0.55, 0.8); z = terrain_h(x, y) - 0.3 * rz
        bm = blob(2 if k < 4 or s > 3.0 else 1, (s, s * rr.uniform(0.75, 1.0), rz), k + 3, 0.16, 0.9)
        mb.add(bm, 'rock' if k % 3 else 'rock2', M((x, y, z), (D(J(8)), D(J(8)), D(rr.uniform(0, 360)))))
        if math.hypot(x + 3, y - 10) < 17 and z + rz > 1.0:
            rcols.append(((x - s * 0.8, y - s * 0.8, z - rz * 0.5), (x + s * 0.8, y + s * 0.8, z + rz * 0.85)))
    # trees: round blobby canopies on the grassy top
    for k, (x, y, h) in enumerate(((-15, 31, 3.6), (-7, 37, 4.2), (9, 33, 3.4), (17, 42, 4.0))):
        g = terrain_h(x, y)
        lean = (J(0.5), J(0.5))
        path = [Vector((x, y, g - 0.3)), Vector((x + lean[0] * 0.4, y + lean[1] * 0.4, g + h * 0.5)), Vector((x + lean[0], y + lean[1], g + h))]
        mb.add(sweep(path, [circle(r, 7) for r in (0.34, 0.25, 0.18)]), 'midwood')
        top = Vector((x + lean[0], y + lean[1], g + h + 0.9))
        mb.add(blob(2, (2.5, 2.4, 2.0), 40 + k, 0.12, 0.8), 'leaf', M(top))
        for q in range(3):
            a = D(120 * q + k * 40); o = Vector((math.cos(a) * 1.8, math.sin(a) * 1.8, -0.6 + 0.3 * q))
            mb.add(blob(1, (1.45, 1.45, 1.25), 60 + k * 3 + q, 0.12, 0.9), 'grass2' if q == 1 else 'leaf', M(top + o))
    # low chunky bushes
    for k, (x, y) in enumerate(((-20, 24), (-11, 26), (2, 27), (13, 25), (24, 31), (-26, 36), (5, 44), (-2, 20), (-12, 18.5))):
        g = terrain_h(x, y)
        for q in range(2):
            o = Vector((q * 0.9 + J(0.2), J(0.3), 0)); r = 1.0 - 0.3 * q
            mb.add(blob(1, (r, r * 0.9, r * 0.7), 80 + k * 2 + q, 0.15, 1.0), 'leaf' if (k + q) % 2 else 'grass2', M(Vector((x, y, g + 0.15)) + o))
    finish(mb, 'Land', coll, 45)
    # ground collider: the same height field, coarser
    bm = grid(frange(-45, 45, 3.0), [-30, -12] + frange(-3, 63, 3.0), terrain_h)
    me = bpy.data.meshes.new('Ground_col'); bm.to_mesh(me); bm.free(); me.materials.append(toon_mat())
    g = bpy.data.objects.new('Ground_col', me); coll.objects.link(g); g.hide_render = True
    for k, (lo, hi) in enumerate(rcols): col_box('Rock%d_colBox' % (k + 1), lo, hi, coll)


DECK, PY0, PY1, PW = 1.30, 10.0, -22.0, 1.30      # deck top, pier start (shore) and end, half-width


def build_pier(coll):
    mb = MB()
    bents = [PY0 - 2.5 * k for k in range(13)] + [PY1 + 0.1]
    # piles (posts) every 2.5 m each side, down to z=-4, leaning a little
    for y in bents:
        for x in (-1.22, 1.22):
            zc = (-4 + 0.94) / 2; bm = cyl(0.15, 4.94, 8, bevel=0.03, seg=1); zsplit(bm, 0.45 - zc)
            mb.add(bm, lambda c, n: 'dark' if c.z < 0.45 else 'midwood', M((x + J(0.03), y + J(0.03), zc), (D(J(1.5)), D(J(1.5)), D(R.uniform(0, 45)))))
        put(mb, box(2.95 + J(0.08), 0.17, 0.18, 0.025, 1), 'midwood', (J(0.04), y, 1.03), (D(J(1)), D(J(1.5)), D(J(1))))
    # bollard piles at the end (+X side, where the boat moors) with rope wraps
    for y in (PY1 + 0.15, -16.6):
        zc = (1.85 - 4) / 2; bm = cyl(0.16, 5.85, 8, bevel=0.035, seg=2); zsplit(bm, 0.45 - zc)
        mb.add(bm, lambda c, n: 'dark' if c.z < 0.45 else 'midwood', M((1.48, y, zc), (D(J(1)), D(J(1.5)), 0)))
        put(mb, cyl(0.175, 0.07, 8, bevel=0.025, seg=2), 'dark', (1.48, y, 1.86))
        for q in range(2): mb.add(torus((1.48, y, 1.52 + q * 0.06), 0.175, 0.025, 'Z', 10, 6), 'rope')
    # cross bracing under the deck
    for k in (2, 4, 6, 8, 10, 12):
        y = bents[k] + 0.12
        L = math.hypot(2.44, 1.6); ang = math.atan2(1.6, 2.44) * (1 if k % 4 else -1)
        put(mb, box(L, 0.07, 0.16, 0.02, 1), 'weath', (0, y, 0.2), (0, ang, 0))
    # stringers
    for x in (-1.0, 0.0, 1.0):
        for a, b in ((PY0, -0.6), (-0.6, -11.4), (-11.4, PY1 + 0.05)):
            put(mb, box(0.14, abs(a - b) + 0.05, 0.10, 0.02, 1), 'midwood', (x + J(0.03), (a + b) / 2, 1.17), (0, 0, D(J(0.1))))
    # deck planks: weathered, hand-laid, a few broken ends at the edges (the middle 1.8 m is always solid)
    y = PY0 - 0.17; k = 0; broken = {7: 1, 19: -1, 33: 1, 34: 1, 52: -1, 61: 2, 70: -1, 83: 2}
    while y > PY1 + 0.1:
        w = 0.32 * (1 + J(0.03)); L = 2.6 * (1 + J(0.04))
        x0, x1 = -L / 2 + J(0.03), L / 2 + J(0.03)
        b = broken.get(k, 0)
        if b == 1: x1 = R.uniform(0.92, 1.1)
        elif b == -1: x0 = -R.uniform(0.92, 1.1)
        elif b == 2: x0, x1 = -R.uniform(0.92, 1.0), R.uniform(0.92, 1.0)
        bm = box(x1 - x0, w, 0.08, 0.02, 1)
        if b:
            for v in bm.verts:
                if (b in (1, 2) and v.co.x > 0) or (b in (-1, 2) and v.co.x < 0): v.co.x += J(0.07)
        c = R.random()
        put(mb, bm, 'weath' if c < 0.62 else 'greyw' if c < 0.9 else 'wood', ((x0 + x1) / 2, y, DECK - 0.04 + J(0.006)), (D(J(0.6)), D(J(0.8)), D(J(1.8))))
        y -= w + 0.04; k += 1
    # low rope rail on the -X side: posts + sagging rope
    posts = [9.0 - 2.5 * k for k in range(13)]
    for py in posts:
        put(mb, box(0.11, 0.11, 1.08, 0.025, 1), 'midwood', (-1.37, py, 1.53), (D(J(2)), D(J(2.5)), D(J(3))))
        put(mb, box(0.13, 0.13, 0.05, 0.02, 1), 'dark', (-1.37, py, 2.08))
    path = []
    for a, b in zip(posts, posts[1:]):
        for q in range(4):
            s = q / 4; path.append(Vector((-1.37, a + (b - a) * s, 1.98 - 0.6 * s * (1 - s))))
    path.append(Vector((-1.37, posts[-1], 1.98)))
    mb.add(sweep(path, circle(0.026, 6)), 'rope')
    # cleats on the +X side
    for cy in (-19.2, -13.2):
        put(mb, box(0.09, 0.2, 0.06, 0.02, 1), 'dark', (1.02, cy, DECK + 0.03))
        put(mb, box(0.08, 0.42, 0.06, 0.025, 2), 'dark', (1.02, cy, DECK + 0.085))
    # ladder down to the water at the end, +X side
    ly = -20.6
    for dy in (-0.25, 0.25):
        put(mb, box(0.07, 0.08, 2.75, 0.02, 2), 'midwood', (1.40, ly + dy, 0.47), (0, D(-2), 0))
    for z in (1.0, 0.7, 0.4, 0.1, -0.2, -0.5):
        put(mb, box(0.06, 0.52, 0.05, 0.018, 1), 'wood', (1.40 + 0.035 * (0.47 - z) / 1.375, ly, z))
    # old lamp post at the end
    lx, lyy = -1.0, PY1 + 0.35
    put(mb, box(0.15, 0.15, 2.9, 0.03, 2), 'dark', (lx, lyy, DECK + 1.45), (D(1.2), D(-0.8), 0))
    put(mb, box(0.75, 0.09, 0.09, 0.025, 1), 'dark', (lx + 0.32, lyy, DECK + 2.75))
    put(mb, box(0.38, 0.06, 0.06, 0.02, 1), 'dark', (lx + 0.14, lyy, DECK + 2.6), (0, D(-40), 0))
    hx = lx + 0.6
    mb.add(sweep([(hx, lyy, DECK + 2.7), (hx, lyy, DECK + 2.6)], circle(0.02, 6)), 'iron')
    put(mb, cyl(0.15, 0.10, 8, r2=0.04, bevel=0.015), 'iron', (hx, lyy, DECK + 2.53))
    put(mb, box(0.2, 0.2, 0.25, 0.035, 2), 'sun', (hx, lyy, DECK + 2.35))
    put(mb, box(0.24, 0.24, 0.05, 0.02, 1), 'iron', (hx, lyy, DECK + 2.21))
    # crab pots (two side by side, one stacked)
    def crab_pot(c, yaw):
        m = M(c, (0, 0, yaw))
        mb.add(box(0.66, 0.46, 0.06, 0.02, 1), 'midwood', m @ M((0, 0, 0.03)))
        mb.add(box(0.60, 0.40, 0.30, 0.13, 2), 'rope', m @ M((0, 0, 0.19)))
        for xx in (-0.27, 0, 0.27):
            arc = [Vector((xx, 0.23 * math.cos(a), 0.06 + 0.29 * math.sin(a))) for a in [D(180 * q / 6) for q in range(7)]]
            mb.add(sweep(arc, circle(0.02, 5), up=(1, 0, 0)), 'rust', m)
        mb.add(box(0.16, 0.05, 0.12, 0.02, 1), 'sun', m @ M((0.18, -0.235, 0.16)))
    crab_pot((-0.7, -15.4, DECK), D(4)); crab_pot((-0.72, -16.15, DECK), D(-6)); crab_pot((-0.7, -15.8, DECK + 0.36), D(12))
    # fish crate with a few fish
    fx, fy = -0.72, -18.4
    put(mb, box(0.72, 0.48, 0.05, 0.015, 1), 'wood', (fx, fy, DECK + 0.025))
    for zz in (0.11, 0.26):
        for sy in (-1, 1): put(mb, box(0.72, 0.035, 0.11, 0.012, 1), 'wood', (fx, fy + sy * 0.225, DECK + zz))
        for sx in (-1, 1): put(mb, box(0.035, 0.48, 0.11, 0.012, 1), 'wood', (fx + sx * 0.34, fy, DECK + zz))
    for q, (dx, dy, c) in enumerate(((-0.12, -0.05, 'sky'), (0.12, 0.08, 'teal'), (0.0, 0.0, 'peach'))):
        mb.add(blob(1, (0.22, 0.07, 0.07), 120 + q, 0.08), c, M((fx + dx, fy + dy, DECK + 0.27 + 0.03 * q), (D(J(15)), 0, D(J(25)))))
    coil(mb, (0.78, -18.2, DECK), 0.2, 3, 0.025)
    bm = cyl(0.14, 0.28, 10, r2=0.16, bevel=0.012); bm.normal_update()
    mb.add(bm, lambda c, n: 'teal' if n.z > 0.9 else 'poppy', M((-0.95, -20.7, DECK + 0.14)))
    finish(mb, 'Pier', coll)
    col_box('PierDeck_colBox', (-PW, PY1 - 0.1, 1.0), (PW, PY0, DECK), coll)
    col_box('RopeRail_colBox', (-1.45, PY1 + 0.6, DECK), (-1.18, 9.1, 2.2), coll)
    col_box('PilesL_colBox', (-1.40, PY1 - 0.1, -4.0), (-1.05, PY0, 1.0), coll)
    col_box('PilesR_colBox', (1.05, PY1 - 0.1, -4.0), (1.66, PY0, 1.0), coll)
    col_box('PierEnd_colBox', (-1.40, PY1 - 0.15, -4.0), (1.66, PY1 + 0.3, 1.0), coll)
    col_box('LampPost_colBox', (lx - 0.1, lyy - 0.1, DECK), (lx + 0.1, lyy + 0.1, DECK + 2.9), coll)
    col_box('CrabPots_colBox', (-1.08, -16.45, DECK), (-0.33, -15.1, DECK + 0.7), coll)
    col_box('FishCrate_colBox', (-1.08, -18.64, DECK), (-0.36, -18.16, DECK + 0.33), coll)
    empty('socket_moor', (2.9, -19.0, 0.0), coll)
    empty('socket_spawn', (0.15, -19.6, DECK), coll)
    empty('socket_pier_ladder_top', (1.2, ly, DECK), coll)
    empty('socket_pier_ladder_bottom', (1.42, ly, -0.5), coll)


def corr_sheet(w, l, waves=5, amp=0.025, th=0.02, per=3):
    bm = bmesh.new(); n = waves * per
    us = [-w / 2 + w * i / n for i in range(n + 1)]; zt = [amp * math.sin(2 * math.pi * waves * i / n) for i in range(n + 1)]
    T0 = [bm.verts.new((u, 0, z)) for u, z in zip(us, zt)]; T1 = [bm.verts.new((u, l, z)) for u, z in zip(us, zt)]
    B0 = [bm.verts.new((u, 0, z - th)) for u, z in zip(us, zt)]; B1 = [bm.verts.new((u, l, z - th)) for u, z in zip(us, zt)]
    for i in range(n):
        bm.faces.new((T0[i], T0[i + 1], T1[i + 1], T1[i])); bm.faces.new((B1[i], B1[i + 1], B0[i + 1], B0[i]))
        bm.faces.new((B0[i], B0[i + 1], T0[i + 1], T0[i])); bm.faces.new((T1[i], T1[i + 1], B1[i + 1], B1[i]))
    bm.faces.new((B0[0], T0[0], T1[0], B1[0])); bm.faces.new((B1[-1], T1[-1], T0[-1], B0[-1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def build_shack(coll):
    mb = MB()
    sx, sy = -6.0, 14.0; HW, HD = 2.0, 1.75
    g = max(terrain_h(sx + a, sy + b) for a in (-HW, 0, HW) for b in (-HD, 0, HD))
    F = g + 0.28; EAVE = F + 2.15; RIDGE = F + 3.2
    roof_z = lambda y: EAVE + (RIDGE - EAVE) * (1 - abs(y - sy) / HD)
    # stone footings + floor platform + front step
    for a in (-1.8, 1.8):
        for b in (-1.55, 1.55): put(mb, blob(1, (0.28, 0.24, 0.32), int(a * 10 + b * 7) + 300, 0.1), 'rock2', (sx + a, sy + b, F - 0.25))
    put(mb, box(2 * HW + 0.2, 2 * HD + 0.2, 0.14, 0.03, 2), 'greyw', (sx, sy, F - 0.07))
    put(mb, box(1.1, 0.42, 0.16, 0.03, 2), 'weath', (sx + 0.62, sy - HD - 0.28, F - 0.2), (0, 0, D(2)))
    # front wall (faces the sea, -Y) with a window and the teal door
    bw = 2 * HW / 13; win, door = (2, 3, 4), (8, 9, 10)
    WZ0, WZ1, DZ1 = F + 0.95, F + 1.65, F + 1.95
    def wall_boards(yf, yb, skip, front):
        for i in range(13):
            x0 = sx - HW + i * bw + 0.006; x1 = x0 + bw - 0.012
            c = 'weath' if R.random() < 0.7 else 'greyw'
            ln = (J(0.015), 0)
            pieces = [(F, EAVE)]
            if front and i in win: pieces = [(F, WZ0), (WZ1, EAVE)]
            if front and i in door: pieces = [(DZ1, EAVE)]
            for z0, z1 in pieces: mb.add(board(x0, x1, yf, yb, z0, z1 + J(0.02), 0.012, 1, ln), c)
    wall_boards(sy - HD, sy - HD + 0.06, None, True)
    wall_boards(sy + HD - 0.06, sy + HD, None, False)
    # side walls (gables) with sloped board tops
    nb = 12; bd = 2 * HD / nb
    for xs_ in (sx - HW, sx + HW - 0.06):
        for i in range(nb):
            y0 = sy - HD + i * bd + 0.006; y1 = y0 + bd - 0.012
            c = 'weath' if R.random() < 0.65 else 'greyw'
            mb.add(board(xs_, xs_ + 0.06, y0, y1, F, lambda X, Y: roof_z(Y) - 0.06, 0.012, 1), c)
    for a in (-1, 1):
        for b in (-1, 1): put(mb, box(0.13, 0.13, EAVE - F, 0.025, 2), 'dark', (sx + a * (HW - 0.02), sy + b * (HD - 0.02), (F + EAVE) / 2))
    # door: teal boards, cream Z-brace, frame, knob
    dx0 = sx - HW + door[0] * bw; dx1 = sx - HW + (door[-1] + 1) * bw
    for q in range(3):
        put(mb, box((dx1 - dx0) / 3 - 0.01, 0.05, DZ1 - F - 0.04, 0.012, 1), 'teal', (dx0 + (q + 0.5) * (dx1 - dx0) / 3, sy - HD + 0.02, (F + DZ1) / 2 + 0.01))
    for z in (F + 0.35, F + 1.6): put(mb, box(dx1 - dx0 - 0.08, 0.04, 0.12, 0.015, 1), 'cream2', ((dx0 + dx1) / 2, sy - HD - 0.02, z))
    dl = math.hypot(dx1 - dx0 - 0.2, 1.25)
    put(mb, box(dl, 0.04, 0.11, 0.015, 1), 'cream2', ((dx0 + dx1) / 2, sy - HD - 0.02, F + 0.975), (0, -math.atan2(1.25, dx1 - dx0 - 0.2), 0))
    put(mb, cyl(0.035, 0.06, 6, bevel=0.01), 'iron', (dx0 + 0.12, sy - HD - 0.06, F + 1.0), (D(90), 0, 0))
    for x in (dx0 - 0.04, dx1 + 0.04): put(mb, box(0.08, 0.08, DZ1 - F + 0.06, 0.02, 1), 'wood', (x, sy - HD - 0.01, (F + DZ1) / 2 + 0.03))
    put(mb, box(dx1 - dx0 + 0.16, 0.08, 0.08, 0.02, 1), 'wood', ((dx0 + dx1) / 2, sy - HD - 0.01, DZ1 + 0.04))
    # window: cream frame, sky glass, cross mullion, little sill
    wx0 = sx - HW + win[0] * bw; wx1 = sx - HW + (win[-1] + 1) * bw; wc = (wx0 + wx1) / 2
    put(mb, box(wx1 - wx0, 0.03, WZ1 - WZ0, 0.0, 0), 'sky', (wc, sy - HD + 0.03, (WZ0 + WZ1) / 2))
    for z in (WZ0, WZ1): put(mb, box(wx1 - wx0 + 0.12, 0.09, 0.07, 0.02, 1), 'cream2', (wc, sy - HD - 0.005, z))
    for x in (wx0, wx1): put(mb, box(0.07, 0.09, WZ1 - WZ0, 0.02, 1), 'cream2', (x, sy - HD - 0.005, (WZ0 + WZ1) / 2))
    put(mb, box(0.045, 0.06, WZ1 - WZ0, 0.015, 1), 'cream2', (wc, sy - HD + 0.0, (WZ0 + WZ1) / 2))
    put(mb, box(wx1 - wx0, 0.06, 0.045, 0.015, 1), 'cream2', (wc, sy - HD + 0.0, (WZ0 + WZ1) / 2))
    put(mb, box(wx1 - wx0 + 0.2, 0.16, 0.05, 0.02, 1), 'wood', (wc, sy - HD - 0.07, WZ0 - 0.05))
    # roof: patched corrugated sheets, faded red + cream, with a ridge cap
    th = math.atan2(RIDGE - EAVE, HD); OV = 0.4
    l = (HD + OV) / math.cos(th) + 0.06
    cols = ['poppy', 'cream2', 'peach', 'poppy', 'cream2', 'peach', 'poppy', 'peach', 'cream2', 'poppy']
    k = 0
    for side in (-1, 1):
        ye = sy + side * (HD + OV); ze = EAVE - OV * math.tan(th) + 0.05
        for q in range(5):
            xc = sx - HW - 0.35 + (q + 0.5) * (2 * HW + 0.7) / 5
            rot = Matrix.Rotation(D(180), 4, 'Z') if side > 0 else Matrix.Identity(4)
            m = Matrix.Translation((xc + J(0.03), ye, ze + 0.012 * q)) @ rot @ Matrix.Rotation(th, 4, 'X') @ Matrix.Rotation(D(J(1.5)), 4, 'Z')
            mb.add(corr_sheet(0.98, l), cols[k], m); k += 1
        if side < 0:   # a rusty patch sheet nailed over a hole
            m = Matrix.Translation((sx + 0.9, ye + 0.7 * math.cos(th), ze + 0.7 * math.sin(th) + 0.09)) @ Matrix.Rotation(th, 4, 'X') @ Matrix.Rotation(D(6), 4, 'Z')
            mb.add(corr_sheet(0.66, 0.8, 3, 0.025), 'rust', m)
    mb.add(sweep([(sx - HW - 0.4, sy, RIDGE + 0.06), (sx + HW + 0.4, sy, RIDGE + 0.06)],
                 [(-0.2, -0.08), (0.0, 0.05), (0.2, -0.08), (0.2, -0.11), (0.0, 0.01), (-0.2, -0.11)], up=(0, 1, 0)), 'iron')
    # stove pipe on the back slope
    px, py = sx + 1.2, sy + 0.8
    put(mb, cyl(0.09, 1.3, 8, bevel=0.015), 'iron', (px, py, roof_z(py) + 0.45), (D(-3), D(4), 0))
    put(mb, cyl(0.21, 0.13, 8, r2=0.05, bevel=0.015), 'iron', (px + 0.04, py - 0.03, roof_z(py) + 1.22))
    put(mb, cyl(0.105, 0.06, 8, bevel=0.012), 'rust', (px + 0.01, py, roof_z(py) + 0.35))
    # bench outside under the window
    put(mb, box(1.45, 0.34, 0.06, 0.02, 2), 'wood', (wc + 0.05, sy - HD - 0.32, F + 0.45), (0, D(1.5), D(-1.5)))
    for dx in (-0.55, 0.6): put(mb, box(0.09, 0.28, 0.42, 0.02, 1), 'midwood', (wc + dx, sy - HD - 0.32, F + 0.21))
    # nets hanging on the -X wall from pegs, with floats
    xw = sx - HW - 0.04; nu, nv = 7, 5
    pts = [[Vector((xw - 0.05 - 0.08 * abs(math.sin(i * 1.3)) - 0.05 * v / nv, sy - 1.2 + 1.9 * i / (nu - 1),
                    EAVE - 0.25 - 0.18 * abs(math.sin(math.pi * i / 3)) - (1.45 + 0.25 * math.sin(i * 1.7)) * v / (nv - 1)))
            for i in range(nu)] for v in range(nv)]
    bm = bmesh.new(); V = [[bm.verts.new(p) for p in row] for row in pts]; W = [[bm.verts.new(p + Vector((0.03, 0, 0))) for p in row] for row in pts]
    for v in range(nv - 1):
        for i in range(nu - 1):
            bm.faces.new((V[v][i], V[v + 1][i], V[v + 1][i + 1], V[v][i + 1])); bm.faces.new((W[v][i], W[v][i + 1], W[v + 1][i + 1], W[v + 1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mb.add(bm, 'rope')
    for i in (0, 3, 6): put(mb, box(0.12, 0.06, 0.06, 0.02, 1), 'dark', (xw - 0.03, sy - 1.2 + 1.9 * i / (nu - 1), EAVE - 0.22))
    for i in range(1, nu, 1):
        p = pts[0][i]; put(mb, blob(0, (0.075, 0.075, 0.075), 200 + i, 0.05), 'sun' if i % 2 else 'poppy', (p.x - 0.06, p.y, p.z - 0.05))
    finish(mb, 'Shack', coll)
    col_box('Shack_colBox', (sx - HW - 0.1, sy - HD - 0.1, g - 0.2), (sx + HW + 0.1, sy + HD + 0.1, RIDGE), coll)
    print('SHACK ground', round(g, 2), 'floor', round(F, 2))


def build_landing():
    global R
    R = random.Random(21)
    coll = new_scene('WaltsLanding')
    build_land(coll); build_pier(coll); build_shack(coll)
    print('GROUND at pier root', [(y, round(terrain_h(0, y), 2)) for y in (6, 7, 8, 9, 10, 11, 12, 14)])
    print('GROUND at terrace', [(x, round(terrain_h(x, 14), 2)) for x in (-12, -8, -4, 0, 4)])
    print('TREE ground d', [round(dist_in(x, y), 1) for x, y in ((-15, 31), (-7, 37), (9, 33), (19, 40))])
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'landing_walts.blend'))
    export(coll, 'PB_Landing_Walts.fbx')
    if RENDER:
        cam = setup_render(800)
        bp = os.path.join(OUT, 'boat_biscuit.blend')          # moor the Biscuit for the look check (render only)
        if os.path.exists(bp):
            with bpy.data.libraries.load(bp) as (src, dst): dst.objects = [n for n in src.objects if n in ('Hull', 'Motor', 'Prop')]
            for o in dst.objects:
                bpy.context.scene.collection.objects.link(o)
                if o.parent is None: o.location = Vector(o.location) + Vector((2.9, -19.0, 0))
        shoot(cam, 'landing_hero.png', (30, -52, 2.6), (-2, 8, 3.5), 32)
        shoot(cam, 'landing_gameplay.png', (17, -36, 21), (-1, -8, 1), 35)
        shoot(cam, 'landing_pierend.png', (8.5, -28.5, 4.4), (0.3, -19.3, 1.4), 32)
        shoot(cam, 'landing_shack.png', (2.5, 4.0, 3.2), (-6, 14, 2.2), 32)


if WHAT == 'check':
    for z in (0.13, 0.45):
        print('hw z=%.2f' % z, [(round(y, 2), round(inner_hw(y, z), 3)) for y in frange(-2.0, 2.4, 0.2)])
    print("min clear", min(inner_hw(y, 0.13) for y in frange(-1.32, 2.08, 0.04)))
    for b in ((box(1, 1, 1, 0.02, 1), 'box1'), (box(1, 1, 1, 0.02, 2), 'box2'), (box(1, 1, 1, 0.02, 3), 'box3')):
        print(b[1], sum(len(f.verts) - 2 for f in b[0].faces))
if WHAT in ('boat', 'all'): build_boat()
if WHAT in ('landing', 'all'): build_landing()
