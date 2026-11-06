# Port Boss M5: the M1 art helpers (copied from art-src/m1/build_boat_landing.py so that file can change freely),
# plus a per-face vertex ALPHA byte: 255 = plain, 0 = glow (lamp shades/bulbs), 239 = dye channel 1 (hat fabric).
# Same look rules: flat per-face palette colours in "Col" (sRGB bytes), every edge bevelled, seeded wobble.
import bpy, bmesh, math, random, os
from mathutils import Vector, Matrix, Euler, noise

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out'); REN = os.path.join(OUT, 'renders'); ART = os.path.join(REPO, 'Assets', 'Game', 'Art', 'M5')
for _d in (OUT, REN, ART): os.makedirs(_d, exist_ok=True)

PAL = dict(dark='#805C52', ink='#3F2C27', paper='#FFFBF2', cream2='#F8EFDD', cream3='#EEDFC4', red='#D9483B',
           poppy='#E2553F', sun='#F7C548', teal='#46D6C8', blue='#2160B8', sky='#3A86E0', leaf='#7DBE6A',
           peach='#F6A27A', wood='#B9825A', midwood='#9A6A48', weath='#C9A882', greyw='#A89A8A', rope='#D8C08A',
           iron='#5E5A66', rust='#A2583A', rock='#9A93A6', rock2='#7C7690', sand='#EAD7A8', grass='#8CC46A',
           grass2='#6FAF5A',
           # M5 additions (same warm, soft family)
           fresh='#E6C493', navy='#3B4C7A', navy2='#5A6A94', gold='#E3A83C', brass='#CFA04E', black='#33303A',
           straw='#EBCB80', straw2='#D2AC63', frost='#E2F0F4', ice='#9FD2E6', cblue='#5AB4F0', terra='#CF7650',
           fern='#5C9A4E', canvas='#EADCBB', teal2='#2FA79C', plum='#8E5C8A', grey2='#C9C3CF')
NAMES = list(PAL); CI = {n: i for i, n in enumerate(NAMES)}
GLOW, DYE1 = 0, 239


def srgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def lerp(a, b, t): return a + (b - a) * t
def sstep(a, b, x): t = clamp((x - a) / (b - a)); return t * t * (3 - 2 * t)
def D(deg): return math.radians(deg)
def M(loc=(0, 0, 0), rot=(0, 0, 0)): return Matrix.Translation(Vector(loc)) @ Euler(rot, 'XYZ').to_matrix().to_4x4()
def frange(a, b, s): n = int(round((b - a) / s)); return [a + s * i for i in range(n + 1)]


R = random.Random(7)
def seed(n):
    global R
    R = random.Random(n)
def J(a): return R.uniform(-a, a)


class MB:
    """Collects primitive bmeshes into one mesh; every face carries a palette index and an alpha byte."""
    def __init__(s): s.bm = bmesh.new(); s.lay = s.bm.faces.layers.int.new('ci'); s.al = s.bm.faces.layers.int.new('al')

    def add(s, src, col=None, mat=None, a=255):
        if mat is not None: bmesh.ops.transform(src, matrix=mat, verts=src.verts)
        src.normal_update()
        sl = src.faces.layers.int.get('ci')
        vm = {v: s.bm.verts.new(v.co) for v in src.verts}
        for f in src.faces:
            try: nf = s.bm.faces.new([vm[v] for v in f.verts])
            except ValueError: continue
            nf[s.lay] = f[sl] if col is None else CI[col(f.calc_center_median(), f.normal) if callable(col) else col]
            nf[s.al] = a(f.calc_center_median(), f.normal) if callable(a) else a
        src.free(); return s

    def tris(s): return sum(len(f.verts) - 2 for f in s.bm.faces)


def put(mb, bm, col, loc=(0, 0, 0), rot=(0, 0, 0), a=255): mb.add(bm, col, M(loc, rot), a)


_MAT = {}
def toon_mat():
    """PB_Toon: Unity swaps it for its own toon shader; here a 2-tone cel ramp x vertex colour (glow faces brighter)."""
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
    # alpha 0 (glow) -> lit tone always: mix(ramp, white, 1 - alpha) so the self-check renders show the glow faces
    inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1.0
    lit = nt.nodes.new('ShaderNodeMix'); lit.data_type = 'RGBA'; lit.inputs['B'].default_value = (1.25, 1.2, 1.1, 1)
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs['Color'], ramp.inputs['Fac'])
    nt.links.new(vc.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], lit.inputs['Factor'])
    nt.links.new(ramp.outputs['Color'], lit.inputs['A'])
    nt.links.new(lit.outputs['Result'], mul.inputs['A']); nt.links.new(vc.outputs['Color'], mul.inputs['B'])
    nt.links.new(mul.outputs['Result'], em.inputs['Color']); nt.links.new(em.outputs[0], out.inputs['Surface'])
    _MAT['m'] = m; return m


def finish(mb, name, coll, angle=50):
    """bmesh -> object: Col corner colours (sRGB bytes + alpha byte), smooth + sharp-by-angle, weighted normals."""
    me = bpy.data.meshes.new(name); mb.bm.to_mesh(me); mb.bm.free()
    n = len(me.polygons); vals = [0] * n; als = [0] * n
    me.attributes['ci'].data.foreach_get('value', vals); me.attributes['al'].data.foreach_get('value', als)
    rgb = [srgb(PAL[k]) for k in NAMES]; cols = [0.0] * (len(me.loops) * 4)
    for p in me.polygons:
        c = rgb[vals[p.index]] + (als[p.index] / 255.0,)
        for li in range(p.loop_start, p.loop_start + p.loop_total): cols[li * 4:li * 4 + 4] = c
    me.attributes.remove(me.attributes['ci']); me.attributes.remove(me.attributes['al'])
    ca = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER'); ca.data.foreach_set('color_srgb', cols)
    me.color_attributes.active_color = ca
    me.polygons.foreach_set('use_smooth', [True] * n)
    me.set_sharp_from_angle(angle=D(angle))
    me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    wn = ob.modifiers.new('WeightedNormal', 'WEIGHTED_NORMAL'); wn.keep_sharp = True; wn.weight = 50; wn.mode = 'FACE_AREA'
    return ob


def alpha_counts(ob):
    """{alpha byte: faces} read back from the Col attribute (proves the glow / dye marks survived)."""
    me = ob.data; ca = me.color_attributes['Col']; out = {}
    for p in me.polygons:
        a = int(round(ca.data[p.loop_start].color[3] * 255)); out[a] = out.get(a, 0) + 1
    return out


def col_box(name, lo, hi, coll):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector([hi[k] if v.co[k] > 0 else lo[k] for k in range(3)])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob); ob.hide_render = True; ob.display_type = 'WIRE'
    return ob


def empty(name, loc, coll, yaw=0.0):
    ob = bpy.data.objects.new(name, None); ob.empty_display_type = 'PLAIN_AXES'; ob.empty_display_size = 0.15
    coll.objects.link(ob); ob.location = loc; ob.rotation_euler = (0, 0, D(yaw)); return ob


def bev_edges(bm, edges, w, seg):
    edges = [e for e in edges if e.is_valid]
    if not edges or w <= 0: return
    bmesh.ops.bevel(bm, geom=list({v for e in edges for v in e.verts}) + edges, offset=w, offset_type='OFFSET',
                    segments=seg, profile=0.5, affect='EDGES', clamp_overlap=True)


def bev_sharp(bm, w, seg, ang=50):
    bm.normal_update()
    bev_edges(bm, [e for e in bm.edges if len(e.link_faces) == 2 and e.calc_face_angle(0) > D(ang)], w, seg)


def box(sx, sy, sz, bevel=0.02, seg=1):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts)
    if bevel: bev_edges(bm, bm.edges[:], min(bevel, 0.3 * min(sx, sy, sz)), seg)
    return bm


def board(x0, x1, y0, y1, z0, ztop, bevel=0.012, seg=1, lean=(0, 0)):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        X = x1 if v.co.x > 0 else x0; Y = y1 if v.co.y > 0 else y0
        if v.co.z > 0: v.co = Vector((X + lean[0], Y + lean[1], ztop(X, Y) if callable(ztop) else ztop))
        else: v.co = Vector((X, Y, z0))
    bev_edges(bm, bm.edges[:], min(bevel, 0.3 * min(x1 - x0, y1 - y0)), seg)
    return bm


def bx(x0, x1, y0, y1, z0, z1, bevel=0.015): return board(x0, x1, y0, y1, z0, z1, bevel)


def cyl(r, h, n=8, r2=None, bevel=0.015, seg=1):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=n, radius1=r, radius2=r if r2 is None else r2, depth=h)
    if bevel: bev_sharp(bm, bevel, seg, 60)
    return bm


def blob(sub, rad, sd, amp=0.18, freq=1.2):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    o = Vector((sd * 7.13, sd * 3.71, sd * 1.93))
    for v in bm.verts:
        v.co *= 1 + amp * noise.noise(v.co * freq + o)
        v.co = Vector((v.co.x * rad[0], v.co.y * rad[1], v.co.z * rad[2]))
    return bm


def circle(r, n=8, ph=0.0): return [(r * math.cos(ph + 2 * math.pi * i / n), r * math.sin(ph + 2 * math.pi * i / n)) for i in range(n)]


def rrect(a0, a1, b0, b1, r, segs=(2, 2, 2, 2)):
    pts = []
    for (ca, cb, st), sg in zip(((a1 - r, b1 - r, 0), (a0 + r, b1 - r, 90), (a0 + r, b0 + r, 180), (a1 - r, b0 + r, 270)), segs):
        for k in range(sg + 1):
            ang = D(st + 90 * k / max(sg, 1)) if sg else D(st + 45)
            pts.append((ca + r * math.cos(ang), cb + r * math.sin(ang)))
    return pts


def sweep(path, prof, closed=False, cap=True, up=(0, 0, 1), side=None):
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


def lathe(prof, n=10, cap=True):
    """Revolve [(r, z)] about Z (bottom to top)."""
    bm = bmesh.new(); rings = []
    for r, z in prof:
        rings.append([bm.verts.new((max(r, 1e-4) * math.cos(2 * math.pi * i / n), max(r, 1e-4) * math.sin(2 * math.pi * i / n), z)) for i in range(n)])
    for A, B in zip(rings, rings[1:]):
        for j in range(n): bm.faces.new((A[j], A[(j + 1) % n], B[(j + 1) % n], B[j]))
    if cap: bm.faces.new(rings[0][::-1]); bm.faces.new(rings[-1])
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm


def slab(poly, z0, z1, bevel=0.012, seg=1):
    bm = bmesh.new(); n = len(poly)
    lo = [bm.verts.new((x, y, z0)) for x, y in poly]; hi = [bm.verts.new((x, y, z1)) for x, y in poly]
    bm.faces.new(lo[::-1]); bm.faces.new(hi)
    for i in range(n): bm.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bevel: bev_sharp(bm, bevel, seg, 30)
    return bm


def flat(poly, y, th=0.006, bevel=0.0):
    """A flat shape in the XZ plane (poly = [(x, z)]) facing -Y, from y-th to y: painting / chart shapes."""
    bm = slab(poly, 0, th, bevel)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((0, y, 0)) @ Matrix.Rotation(D(90), 4, 'X'), verts=bm.verts)
    return bm


def zsplit(bm, *zs):
    for z in zs:
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], dist=1e-5, plane_co=(0, 0, z), plane_no=(0, 0, 1))


def torus(c, R_, r, axis='Z', n=10, m=6):
    c = Vector(c); pts = []
    for i in range(n):
        a = 2 * math.pi * i / n; ca, sa = R_ * math.cos(a), R_ * math.sin(a)
        pts.append(c + (Vector((ca, sa, 0)) if axis == 'Z' else Vector((ca, 0, sa)) if axis == 'Y' else Vector((0, ca, sa))))
    upv = {'Z': (0, 0, 1), 'Y': (0, 1, 0), 'X': (1, 0, 0)}[axis]
    return sweep(pts, circle(r, m), closed=True, up=upv)


def coil(mb, c, r, turns=3, tube=0.022, col='rope'):
    for k in range(turns):
        mb.add(torus((c[0] + J(0.01), c[1] + J(0.01), c[2] + tube + k * tube * 1.7), r - k * 0.012, tube, 'Z', 9, 5), col)


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


def new_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True); _MAT.clear()


def new_coll(name):
    coll = bpy.data.collections.new(name); bpy.context.scene.collection.children.link(coll); return coll


def tris_of(obs):
    dg = bpy.context.evaluated_depsgraph_get(); n = 0
    for o in obs:
        if o.type != 'MESH': continue
        me = o.evaluated_get(dg).to_mesh(); n += sum(len(p.vertices) - 2 for p in me.polygons); o.evaluated_get(dg).to_mesh_clear()
    return n


def export(coll, fname):
    """Same FBX settings/axes as M1: 1 unit = 1 m, Z up, front -Y in Blender."""
    for o in bpy.context.scene.objects: o.select_set(False)
    obs = list(coll.all_objects)
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    path = os.path.join(ART, fname)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, apply_unit_scale=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, colors_type='SRGB')
    vis = [o for o in obs if o.type == 'MESH' and '_col' not in o.name]
    t = tris_of(vis)
    print('EXPORTED %s tris %d' % (fname, t))
    return t


# ---------------------------------------------------------------- render ------------------------------------------------------
def setup_render(w=1280, h=720, sea=None, bgc=(0.45, 0.68, 0.93)):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = w; sc.render.resolution_y = h
    try: sc.eevee.taa_render_samples = 16
    except Exception: pass
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True
    bg = wd.node_tree.nodes['Background']; bg.inputs[0].default_value = bgc + (1,); bg.inputs[1].default_value = 0.8
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3.2; sun.data.angle = D(3)
    sun.rotation_euler = (D(50), 0, D(-40)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_end = 2000
    if sea is not None:
        z, size, col = sea
        bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=size)
        me = bpy.data.meshes.new('ground'); bm.to_mesh(me); bm.free()
        sm = bpy.data.materials.new('ground'); sm.use_nodes = True; nt = sm.node_tree; nt.nodes.clear()
        o = nt.nodes.new('ShaderNodeOutputMaterial'); e = nt.nodes.new('ShaderNodeEmission')
        e.inputs['Color'].default_value = tuple(c ** 2.2 for c in srgb(PAL[col])) + (1,)
        nt.links.new(e.outputs[0], o.inputs['Surface']); me.materials.append(sm)
        g = bpy.data.objects.new('ground', me); sc.collection.objects.link(g); g.location.z = z
    return cam


def shoot(cam, name, loc, target, lens=35, ortho=None):
    cam.location = loc
    if ortho: cam.data.type = 'ORTHO'; cam.data.ortho_scale = ortho
    else: cam.data.type = 'PERSP'; cam.data.lens = lens
    cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    path = os.path.join(REN, name); bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)
    print('RENDER', path); return path


def sheet(paths, cols, out):
    """Tile same-size PNG renders into one contact sheet (row-major, top-left first)."""
    import numpy as np
    ims = [bpy.data.images.load(p) for p in paths]
    w, h = ims[0].size; rows = (len(ims) + cols - 1) // cols
    big = np.ones((rows * h, cols * w, 4), dtype=np.float32)
    for k, im in enumerate(ims):
        px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
        r, c = divmod(k, cols); y0 = (rows - 1 - r) * h
        big[y0:y0 + h, c * w:(c + 1) * w] = px
    img = bpy.data.images.new('sheet', cols * w, rows * h, alpha=True)
    img.pixels.foreach_set(big.ravel()); img.filepath_raw = out; img.file_format = 'PNG'; img.save()
    print('SHEET', out)


# ---------------------------------------------------------------- Walt's Landing ground (from M1, read-only copy) ----------
def coast_rho(x, y):
    cy, rx = 33.0, 35.0; ry = 25.5 if y < cy else 21.0
    dx, dy = x / rx, (y - cy) / ry
    r = math.hypot(dx, dy); th = math.atan2(dy, dx)
    wob = 0.05 * math.sin(3 * th + 1.3) + 0.035 * math.sin(5 * th + 0.4) + 0.02 * math.sin(11 * th + 2.0)
    cove = 0.035 * math.exp(-((x + 1) / 10) ** 2) if y < cy else 0.0
    return r * (1 + wob + cove)


def dist_in(x, y): return (1 - coast_rho(x, y)) * 24.0


def terrain_h(x, y):
    d = dist_in(x, y)
    if d < 0: return -4 + 4 * sstep(-9, 0, d)
    tw = 1 - sstep(10, 17, math.hypot(x + 3, y - 10))
    bw = lerp(3.0, 1.7, tw)
    if d <= bw: return 1.25 * d / bw
    d2 = d - bw
    top = 7.2 + 1.2 * noise.noise(Vector((x / 17, y / 17, 0.3)))
    rs = lerp(1.0, 10.0, tw)
    zf = 1.25 + 0.08 * min(d2 / 2, 1)
    z = zf + (top - zf) * sstep(rs, rs + 11.0, d2)
    return z + 0.6 * noise.noise(Vector((x / 5, y / 5, 1.7))) * sstep(rs + 1, rs + 5, d2)


DECK, PY0, PY1, PW = 1.30, 10.0, -22.0, 1.30
SHACK = (-6.0, 14.0, 2.0, 1.75)          # sx, sy, half-width, half-depth (build_shack)
def shack_floor():
    sx, sy, HW, HD = SHACK
    return max(terrain_h(sx + a, sy + b) for a in (-HW, 0, HW) for b in (-HD, 0, HD)) + 0.28
