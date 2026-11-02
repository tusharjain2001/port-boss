# Port Boss, the final map: map B "Crescent Bay" as a Blender BLOCKOUT at true scale (1 unit = 1 m, Z up, X east, Y north,
# world (0,0) = the island's bounding-box centre). Built headless, never opens a window:
#   blender -b -P art-src/world/build_island.py -- [norender] [proof=<dir>]
# The shape comes from island_shape.py (map-B.png's own control points, sampled exactly); this file turns it into meshes:
# the terrain, Old Saltmouth's houses and the 7 shops, Walt's Landing (M1's real pier + shack FBX, with Biscuit), Dot's
# lighthouse, the wreck, rocks, trees, Voss Harbour (blocks), its breakwater and buoy line, the P1-P3 marker buoys, and
# Ponyo cumulus. The sea is one plane coloured by the seabed texture this run writes (art-src/world/seabed/).
# Ponyo Coast: flat per-face palette colours ("Col"), the M1 2-tone cel ramp, no textures except the seabed depth.
# .blend + renders -> art-src/world/out/ (gitignored); renders are copied to the proof folder.
import bpy, bmesh, math, random, sys, os, shutil, time
import numpy as np
from mathutils import Vector, Matrix, Euler, noise

HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import island_shape as S
REPO = os.path.normpath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out'); REN = os.path.join(OUT, 'renders'); SEABED = os.path.join(HERE, 'seabed')
ART = os.path.join(REPO, 'Assets', 'Game', 'Art')
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
RENDER = 'norender' not in ARGV
PROOF = next((a[6:] for a in ARGV if a.startswith('proof=')), os.path.expanduser(r'~\Desktop\Port Boss\proof\map-b'))
ONLY = next((a[5:].split(',') for a in ARGV if a.startswith('only=')), None)
for _d in (OUT, REN, SEABED): os.makedirs(_d, exist_ok=True)

PAL = dict(dark='#805C52', ink='#3F2C27', paper='#FFFBF2', cream2='#F8EFDD', cream3='#EEDFC4', red='#D9483B',
           poppy='#E2553F', sun='#F7C548', teal='#46D6C8', blue='#2160B8', sky='#3A86E0', leaf='#7DBE6A',
           peach='#F6A27A', wood='#B9825A', midwood='#9A6A48', weath='#C9A882', greyw='#A89A8A', rope='#D8C08A',
           iron='#5E5A66', rust='#A2583A', rock='#9A93A6', rock2='#7C7690', sand='#EAD7A8', grass='#8CC46A',
           grass2='#6FAF5A', tree3='#4F8F55', heath='#B5C978', conc='#9AA3A8', conc2='#B9BEC2', vred='#C0392B',
           cloud='#FFFBF2', orange='#F2994A', path='#E3CFA6')
NAMES = list(PAL); CI = {n: i for i, n in enumerate(NAMES)}
def srgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
def lin(h): return tuple(c ** 2.2 for c in srgb(h)) + (1,)
def D(deg): return math.radians(deg)
def M(loc=(0, 0, 0), rot=(0, 0, 0), sc=1.0):
    return Matrix.Translation(Vector(loc)) @ Euler(rot, 'XYZ').to_matrix().to_4x4() @ Matrix.Scale(sc, 4)
R = random.Random(42)
T0 = time.time()
def log(*a): print('[%6.1fs]' % (time.time() - T0), *a, flush=True)


# ---------------------------------------------------------------- the shape (one source of truth)
F = S.heightfield()
log('heightfield; pier root', S.PIER_ROOT)
log('seabed', S.write_seabed(F, SEABED))
CS = S.SIZE / S.N


def samp(A, x, y):
    """Bilinear sample of a field at world points (numpy arrays or floats)."""
    x = np.asarray(x, np.float64); y = np.asarray(y, np.float64)
    px = np.clip((x - S.XW) / CS - 0.5, 0, S.N - 1.001); py = np.clip((S.YN - y) / CS - 0.5, 0, S.N - 1.001)
    i, j = np.floor(px).astype(int), np.floor(py).astype(int); fx, fy = px - i, py - j
    return (A[j, i] * (1 - fx) * (1 - fy) + A[j, i + 1] * fx * (1 - fy) + A[j + 1, i] * (1 - fx) * fy + A[j + 1, i + 1] * fx * fy)


def H(x, y): return float(samp(F['h'], x, y))
def SDF(x, y): return float(samp(F['sdf'], x, y))
def KIND(x, y): return int(F['kind'][int(np.clip((S.YN - y) / CS, 0, S.N - 1)), int(np.clip((x - S.XW) / CS, 0, S.N - 1))])
def slope(x, y, e=4.0): return math.hypot(H(x + e, y) - H(x - e, y), H(x, y + e) - H(x, y - e)) / (2 * e)


def inland(mx, my, need=12.0):
    """A map point moved up the distance field until it is `need` metres inland (pins drawn at the shore)."""
    x, y = S.W(mx, my)
    for _ in range(80):
        d = SDF(x, y); low = H(x, y) < 1.6
        if d >= need and not low: break
        f = H if d >= need else SDF                                  # inland first, then up off the Cut's low banks
        gx = f(x + 3, y) - f(x - 3, y); gy = f(x, y + 3) - f(x, y - 3); g = math.hypot(gx, gy) or 1
        x += gx / g * 2.5; y += gy / g * 2.5
    return x, y


# ---------------------------------------------------------------- materials
def IN(node, ident): return next(s for s in node.inputs if s.identifier == ident)
def OUTC(node): return next(s for s in node.outputs if s.identifier == 'Result_Color')


_MAT = {}
def haze_group():
    """Aerial perspective: mix toward the horizon colour by view distance. k/strength are set per shot."""
    if 'haze' in _MAT: return _MAT['haze']
    ng = bpy.data.node_groups.new('Haze', 'ShaderNodeTree')
    ng.interface.new_socket('Color', in_out='INPUT', socket_type='NodeSocketColor')
    ng.interface.new_socket('Color', in_out='OUTPUT', socket_type='NodeSocketColor')
    n = ng.nodes; gi = n.new('NodeGroupInput'); go = n.new('NodeGroupOutput')
    cam = n.new('ShaderNodeCameraData')
    k = n.new('ShaderNodeValue'); k.name = 'k'; k.outputs[0].default_value = 2600
    st = n.new('ShaderNodeValue'); st.name = 'strength'; st.outputs[0].default_value = 0.75
    div = n.new('ShaderNodeMath'); div.operation = 'DIVIDE'
    neg = n.new('ShaderNodeMath'); neg.operation = 'MULTIPLY'; neg.inputs[1].default_value = -1
    ex = n.new('ShaderNodeMath'); ex.operation = 'EXPONENT'
    om = n.new('ShaderNodeMath'); om.operation = 'SUBTRACT'; om.inputs[0].default_value = 1
    mul = n.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    mix = n.new('ShaderNodeMix'); mix.data_type = 'RGBA'; IN(mix, 'B_Color').default_value = lin('#CFE6F5')
    L = ng.links.new
    L(cam.outputs['View Distance'], div.inputs[0]); L(k.outputs[0], div.inputs[1]); L(div.outputs[0], neg.inputs[0])
    L(neg.outputs[0], ex.inputs[0]); L(ex.outputs[0], om.inputs[1]); L(om.outputs[0], mul.inputs[0]); L(st.outputs[0], mul.inputs[1])
    L(mul.outputs[0], mix.inputs['Factor']); L(gi.outputs[0], IN(mix, 'A_Color')); L(OUTC(mix), go.inputs[0])
    _MAT['haze'] = ng; return ng


def set_haze(k, strength):
    ng = haze_group(); ng.nodes['k'].outputs[0].default_value = k; ng.nodes['strength'].outputs[0].default_value = strength


def toon_ramp(nt, dark=(0.62, 0.66, 0.82, 1), edge=0.55):
    dif = nt.nodes.new('ShaderNodeBsdfDiffuse'); s2r = nt.nodes.new('ShaderNodeShaderToRGB'); ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].color = dark; ramp.color_ramp.elements[1].position = edge; ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    nt.links.new(dif.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs['Color'], ramp.inputs['Fac'])
    return ramp


def toon_mat():
    """PB_Toon as in M1 (2-tone cel ramp with the blue-violet shade x vertex colour) + the haze."""
    if 'toon' in _MAT: return _MAT['toon']
    m = bpy.data.materials.new('PB_Toon'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); ramp = toon_ramp(nt)
    vc = nt.nodes.new('ShaderNodeVertexColor'); vc.layer_name = 'Col'
    mul = nt.nodes.new('ShaderNodeMix'); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs['Factor'].default_value = 1
    hz = nt.nodes.new('ShaderNodeGroup'); hz.node_tree = haze_group(); em = nt.nodes.new('ShaderNodeEmission')
    nt.links.new(ramp.outputs['Color'], IN(mul, 'A_Color')); nt.links.new(vc.outputs['Color'], IN(mul, 'B_Color'))
    nt.links.new(OUTC(mul), hz.inputs[0]); nt.links.new(hz.outputs[0], em.inputs['Color'])
    nt.links.new(em.outputs[0], out.inputs['Surface'])
    _MAT['toon'] = m; return m


def cloud_mat():
    m = bpy.data.materials.new('PB_Cloud'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); ramp = toon_ramp(nt, lin('#C4CCEF'), 0.4)
    ramp.color_ramp.elements[1].color = lin('#FFFBF2')
    mix = nt.nodes.new('ShaderNodeMix'); mix.data_type = 'RGBA'; mix.inputs['Factor'].default_value = 0.18
    IN(mix, 'B_Color').default_value = lin('#CFE6F5')
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs['Strength'].default_value = 1.05
    nt.links.new(ramp.outputs['Color'], IN(mix, 'A_Color')); nt.links.new(OUTC(mix), em.inputs['Color'])
    nt.links.new(em.outputs[0], out.inputs['Surface']); return m


def sea_mat(img):
    """The game's plan (systems 4.3): colour bands by depth from the seabed texture; here flat toon bands + haze + a
    lighter sky sheen at grazing angles."""
    m = bpy.data.materials.new('PB_Sea'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear(); N_ = nt.nodes; L = nt.links.new
    out = N_.new('ShaderNodeOutputMaterial'); tc = N_.new('ShaderNodeTexCoord')
    mp = N_.new('ShaderNodeMapping'); mp.inputs['Location'].default_value = (-S.XW / S.SIZE, -(S.YN - S.SIZE) / S.SIZE, 0)
    mp.inputs['Scale'].default_value = (1 / S.SIZE, 1 / S.SIZE, 1)
    tex = N_.new('ShaderNodeTexImage'); tex.image = img; tex.extension = 'EXTEND'; tex.interpolation = 'Linear'
    # height = v * 320 - 160 -> depth = -height -> band fac = sqrt(depth / 160)
    a = N_.new('ShaderNodeMath'); a.operation = 'MULTIPLY_ADD'; a.inputs[1].default_value = -(S.HMAX - S.HMIN); a.inputs[2].default_value = -S.HMIN
    b = N_.new('ShaderNodeMath'); b.operation = 'DIVIDE'; b.inputs[1].default_value = 160; b.use_clamp = True
    c = N_.new('ShaderNodeMath'); c.operation = 'SQRT'
    ramp = N_.new('ShaderNodeValToRGB'); cr = ramp.color_ramp; cr.interpolation = 'CONSTANT'
    bands = S.BANDS
    cr.elements[0].position = 0; cr.elements[0].color = lin(bands[0][1])
    cr.elements[1].position = math.sqrt(bands[1][0] / 160); cr.elements[1].color = lin(bands[1][1])
    for lim, col in bands[2:]:
        e = cr.elements.new(math.sqrt(lim / 160)); e.color = lin(col)
    lw = N_.new('ShaderNodeLayerWeight'); lw.inputs['Blend'].default_value = 0.35
    sh = N_.new('ShaderNodeMix'); sh.data_type = 'RGBA'; IN(sh, 'B_Color').default_value = lin('#BFE3F4')
    pw = N_.new('ShaderNodeMath'); pw.operation = 'MULTIPLY'; pw.inputs[1].default_value = 0.55
    hz = N_.new('ShaderNodeGroup'); hz.node_tree = haze_group(); em = N_.new('ShaderNodeEmission')
    L(tc.outputs['Object'], mp.inputs['Vector']); L(mp.outputs['Vector'], tex.inputs['Vector'])
    L(tex.outputs['Color'], a.inputs[0]); L(a.outputs[0], b.inputs[0]); L(b.outputs[0], c.inputs[0]); L(c.outputs[0], ramp.inputs['Fac'])
    L(ramp.outputs['Color'], IN(sh, 'A_Color')); L(lw.outputs['Facing'], pw.inputs[0]); L(pw.outputs[0], sh.inputs['Factor'])
    L(OUTC(sh), hz.inputs[0]); L(hz.outputs[0], em.inputs['Color']); L(em.outputs[0], out.inputs['Surface'])
    _MAT['sea_sheen'] = pw
    return m


# ---------------------------------------------------------------- mesh building (M1's MB idea: bmesh pieces, a palette index per face)
class MB:
    def __init__(s): s.bm = bmesh.new(); s.lay = s.bm.faces.layers.int.new('ci')
    def add(s, src, col, mat=None):
        if mat is not None: bmesh.ops.transform(src, matrix=mat, verts=src.verts)
        vm = {v: s.bm.verts.new(v.co) for v in src.verts}
        for f in src.faces:
            try: nf = s.bm.faces.new([vm[v] for v in f.verts])
            except ValueError: continue
            nf[s.lay] = CI[col(f.calc_center_median(), f.normal) if callable(col) else col]
        src.free(); return s


def colourize(me, ci_per_poly):
    rgb = np.array([srgb(PAL[n]) + (1.0,) for n in NAMES], np.float32)
    counts = np.zeros(len(me.polygons), np.int32); me.polygons.foreach_get('loop_total', counts)
    cols = np.repeat(rgb[np.asarray(ci_per_poly)], counts, axis=0).ravel()
    ca = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER'); ca.data.foreach_set('color_srgb', cols)
    me.color_attributes.active_color = ca


def finish(mb, name, coll, angle=40, shadow=True):
    me = bpy.data.meshes.new(name); mb.bm.to_mesh(me); mb.bm.free()
    vals = np.zeros(len(me.polygons), np.int32); me.attributes['ci'].data.foreach_get('value', vals)
    me.attributes.remove(me.attributes['ci']); colourize(me, vals)
    me.polygons.foreach_set('use_smooth', [True] * len(me.polygons)); me.set_sharp_from_angle(angle=D(angle))
    me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob); ob.visible_shadow = shadow
    return ob


def box(sx, sy, sz):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0); bmesh.ops.scale(bm, vec=(sx, sy, sz), verts=bm.verts); return bm


def cyl(r, h, n=8, r2=None):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=n, radius1=r, radius2=r if r2 is None else r2, depth=h)
    return bm


def blob(sub, rad, seed, amp=0.18, freq=1.2):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    o = Vector((seed * 7.13, seed * 3.71, seed * 1.93))
    for v in bm.verts:
        v.co *= 1 + amp * noise.noise(v.co * freq + o)
        v.co = Vector((v.co.x * rad[0], v.co.y * rad[1], v.co.z * rad[2]))
    return bm


def gable(w, d, h):
    """A roof prism: ridge along X, width w (X), depth d (Y), height h, base at z=0."""
    bm = bmesh.new(); vs = [bm.verts.new(p) for p in ((-w / 2, -d / 2, 0), (w / 2, -d / 2, 0), (w / 2, d / 2, 0), (-w / 2, d / 2, 0),
                                                       (-w / 2, 0, h), (w / 2, 0, h))]
    for f in ((0, 1, 5, 4), (2, 3, 4, 5), (0, 4, 3), (1, 2, 5), (0, 3, 2, 1)): bm.faces.new([vs[i] for i in f])
    return bm


# ---------------------------------------------------------------- terrain
def terrain(name, x0, x1, y0, y1, step, coll, seed):
    xs = np.arange(x0, x1 + step / 2, step); ys = np.arange(y0, y1 + step / 2, step)
    GX, GY = np.meshgrid(xs, ys); rng = np.random.RandomState(seed); j = step * 0.3
    GX = GX + rng.uniform(-j, j, GX.shape); GY = GY + rng.uniform(-j, j, GY.shape)   # break the grid: hand-made, not CG-regular
    Z = samp(F['h'], GX, GY)
    Z = np.maximum(Z, -6.0)
    ny, nx = GX.shape; idx = np.arange(nx * ny).reshape(ny, nx)
    a, b, c, d = idx[:-1, :-1], idx[:-1, 1:], idx[1:, 1:], idx[1:, :-1]
    zf = Z.ravel()
    keep = np.maximum.reduce([zf[a], zf[b], zf[c], zf[d]]) > -1.0     # only what meets the waterline
    alt = ((np.add.outer(np.arange(ny - 1), np.arange(nx - 1))) % 2 == 0)
    t1 = np.where(alt[..., None], np.stack([a, b, c], -1), np.stack([a, b, d], -1))
    t2 = np.where(alt[..., None], np.stack([a, c, d], -1), np.stack([b, c, d], -1))
    tris = np.concatenate([t1[keep], t2[keep]])
    used, inv = np.unique(tris, return_inverse=True); tris = inv.reshape(-1, 3)
    V = np.stack([GX.ravel()[used], GY.ravel()[used], zf[used]], -1)
    me = bpy.data.meshes.new(name); me.vertices.add(len(V)); me.vertices.foreach_set('co', V.astype(np.float32).ravel())
    me.loops.add(len(tris) * 3); me.loops.foreach_set('vertex_index', tris.astype(np.int32).ravel())
    me.polygons.add(len(tris)); me.polygons.foreach_set('loop_start', np.arange(0, len(tris) * 3, 3, dtype=np.int32))
    me.update(); me.validate()
    # per-face palette colour from the centroid: height, slope, sand/rock, town, Voss, the Cut
    P = V[tris]; cen = P.mean(1); nrm = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    nz = np.abs(nrm[:, 2]) / (np.linalg.norm(nrm, axis=1) + 1e-9)
    cx, cy, cz = cen[:, 0], cen[:, 1], cen[:, 2]
    be = samp(F['beach'], cx, cy)
    ki = F['kind'][np.clip(((S.YN - cy) / CS).astype(int), 0, S.N - 1), np.clip(((cx - S.XW) / CS).astype(int), 0, S.N - 1)]
    nn = S.waves(cx, cy, 11, [(140, 1), (90, 0.7), (55, 0.5)]); nn2 = S.waves(cx, cy, 5, [(230, 1), (120, 0.6)])
    ci = np.full(len(tris), CI['grass'])
    ci[nn > 0.35] = CI['grass2']; ci[nn2 > 0.9] = CI['leaf']
    ci[(cz > 122 + 10 * nn) & (nn2 > -0.2)] = CI['heath']
    ci[(ki == 1) & (nz > 0.9)] = CI['path']
    ci[(ki == 1) & (nz > 0.9) & (nn > 0.2)] = CI['grass']
    ci[(be > 0.45) & (cz < 3.4) & (nz > 0.85)] = CI['sand']
    steep = nz < 0.78
    ci[steep] = np.where(nn[steep] > 0, CI['rock'], CI['rock2'])
    ci[(cz < 0.9) & (be <= 0.45) & (ki == 0)] = CI['rock2']
    ci[ki == 3] = CI['sand']
    ci[ki == 4] = np.where(nn[ki == 4] > 0, CI['rock'], CI['rock2'])
    ci[(ki == 2) & (cz > 2.0)] = CI['conc']
    ci[(ki == 2) & (cz <= 2.0)] = CI['conc2']
    colourize(me, ci)
    me.polygons.foreach_set('use_smooth', [True] * len(me.polygons)); me.set_sharp_from_angle(angle=D(32))
    me.materials.append(toon_mat())
    ob = bpy.data.objects.new(name, me); coll.objects.link(ob)
    log(name, 'tris', len(tris))
    return ob


# ---------------------------------------------------------------- scatter: rocks, trees
def scatter(n_want, box_, test, spacing, seed, tries=60000):
    rr = random.Random(seed); pts = []; grid_ = {}
    cell = spacing
    for _ in range(tries):
        if len(pts) >= n_want: break
        x, y = rr.uniform(box_[0], box_[1]), rr.uniform(box_[2], box_[3])
        r = test(x, y, rr)
        if r is None: continue
        gx, gy = int(x // cell), int(y // cell)
        if any(math.hypot(x - p[0], y - p[1]) < spacing * max(1, p[2] / 6) for i in (-1, 0, 1) for j in (-1, 0, 1) for p in grid_.get((gx + i, gy + j), [])):
            continue
        p = (x, y, r); pts.append(p); grid_.setdefault((gx, gy), []).append(p)
    return pts


ALL = (-1150, 1700, -1300, 1300)


def build_rocks(coll):
    mb = MB()
    def t(x, y, rr):
        d = SDF(x, y)
        if not (-14 < d < 7) or KIND(x, y) != 0 or float(samp(F['beach'], x, y)) > 0.3: return None
        return rr.uniform(2.0, 6.5)
    rocks = scatter(520, ALL, t, 9, 5)
    # Voss Harbour's breakwater armour + a few sea stacks off the north horn's point and the lighthouse
    bwp = S.Wl(S.smooth(S.BREAKWATER, 30, closed=False))
    rocks += [(x + R.uniform(-6, 6), y + R.uniform(-6, 6), R.uniform(3, 5)) for x, y in bwp[::2]]
    for mx, my, s in ((1780, 1560, 9), (1700, 1440, 7), (1610, 1370, 6), (1840, 1500, 5), (2190, 2470, 6), (2320, 2640, 5)):
        x, y = S.W(mx, my); rocks.append((x, y, s))
    for k, (x, y, s) in enumerate(rocks):
        rz = s * R.uniform(0.55, 0.95); z = H(x, y)
        mb.add(blob(1, (s, s * R.uniform(0.7, 1.0), rz), k + 3, 0.2, 0.9), 'rock' if k % 3 else 'rock2',
               M((x, y, min(z, 0.6) - 0.2 * rz + (0.4 * rz if z > 0 else 0)), (D(R.uniform(-8, 8)), D(R.uniform(-8, 8)), D(R.uniform(0, 360)))))
    log('rocks', len(rocks)); return finish(mb, 'Rocks', coll, 45)


def build_trees(coll):
    mb = MB()
    def t(x, y, rr):
        z = H(x, y)
        if not (5 < z < 128) or SDF(x, y) < 22 or KIND(x, y) != 0 or slope(x, y) > 0.55: return None
        clump = float(S.waves(np.array(x), np.array(y), 21, [(380, 1), (210, 0.8), (120, 0.5)]))
        if clump < 0.15 + 0.3 * (z / 140): return None
        return rr.uniform(3.2, 6.0)
    trees = scatter(1500, ALL, t, 8.5, 9, 120000)
    for k, (x, y, s) in enumerate(trees):
        g = H(x, y); h = s * R.uniform(1.0, 1.5)
        mb.add(cyl(0.35 * s / 4, h, 6, 0.22 * s / 4), 'midwood', M((x, y, g + h / 2 - 0.4)))
        col = ('leaf', 'grass2', 'tree3')[R.randrange(3) if g < 60 else R.choice((1, 2, 2))]
        mb.add(blob(1, (s, s * R.uniform(0.85, 1.0), s * R.uniform(0.8, 1.0)), 100 + k, 0.14, 0.8), col, M((x, y, g + h + s * 0.45)))
        if s > 4.6:
            a = R.uniform(0, 6.3)
            mb.add(blob(1, (s * 0.62,) * 3, 200 + k, 0.14, 0.9), 'grass2' if col == 'leaf' else 'leaf',
                   M((x + math.cos(a) * s * 0.7, y + math.sin(a) * s * 0.7, g + h + s * 0.1)))
    log('trees', len(trees)); return finish(mb, 'Trees', coll, 60)


# ---------------------------------------------------------------- Old Saltmouth
def house(mb, x, y, yaw, w, d, storeys, wall, roof, z0):
    ht = 2.9 * storeys
    m = M((x, y, 0), (0, 0, yaw))
    mb.add(box(w, d, ht + 4), wall, m @ M((0, 0, z0 + ht / 2 - 2)))                 # walls (sunk 4 m into the slope)
    mb.add(gable(w + 0.8, d + 0.9, 0.42 * d), roof, m @ M((0, 0, z0 + ht)))
    mb.add(box(1.1, 0.25, 2.1), 'ink', m @ M((0, -d / 2 - 0.05, z0 + 1.05)))           # door
    for k in range(storeys):
        for fx in (-0.3, 0.3):
            mb.add(box(1.0, 0.2, 1.15), 'sky', m @ M((w * fx, -d / 2 - 0.04, z0 + 1.6 + 2.9 * k)))


SHOP_STYLE = {1: ('cream2', 'teal', 'teal'), 2: ('weath', 'red', 'blue'), 3: ('peach', 'red', 'sun'), 4: ('paper', 'poppy', 'red'),
              5: ('conc2', 'blue', 'blue'), 6: ('sun', 'teal', 'poppy'), 7: ('cream2', 'red', 'red')}


def build_town(coll):
    mb = MB(); px, py = S.PIER_ROOT; placed = []
    # the 7 shops at their map pins (moved inland where a pin sits on the shoreline), fronts facing the bay (west)
    for n, (name, (mx, my)) in S.SHOPS.items():
        x, y = inland(mx - (45 if n == 6 else 0), my, 10)          # Swell Surf: 45 m west, off the Cut's bank
        z = min(H(x + a, y + b) for a in (-6, 6) for b in (-5, 5))
        wall, roof, awn = SHOP_STYLE[n]; yaw = D(-90 + R.uniform(-6, 6))
        w, d = (16, 9) if n == 5 else (12, 9)
        house(mb, x, y, yaw, w, d, 1 if n in (5, 6) else 2, wall, roof, z)
        m = M((x, y, 0), (0, 0, yaw))
        mb.add(box(w * 0.8, 2.2, 0.25), awn, m @ M((0, -d / 2 - 1.1, z + 2.7), (D(-14), 0, 0)))     # awning
        mb.add(cyl(0.12, 7, 6), 'paper', m @ M((w / 2 + 0.8, -d / 2 - 1.5, z + 3.5)))              # flag pole + flag
        mb.add(box(0.06, 2.2, 1.3), awn, m @ M((w / 2 + 0.8, -d / 2 - 2.6, z + 6.2)))
        placed.append((x, y, 9)); log('shop', n, name, 'at world (%.0f, %.0f) z %.1f' % (x, y, z))
    # houses: dense along the waterfront, thinning up the hillside; each faces downhill toward the bay
    tx, ty = S.W(2965, 2560)
    def t(x, y, rr):
        r = math.hypot((x - tx) * 0.85, y - ty)
        if r > 380 or rr.random() > 1.15 - r / 380: return None
        z = H(x, y)
        if not (1.2 < z < 48) or SDF(x, y) < 9 or slope(x, y) > 0.42 or KIND(x, y) in (2, 3): return None
        if math.hypot(x - px - 12, y - py - 3) < 34: return None                         # Walt's Landing yard
        if any(math.hypot(x - a, y - b) < c + 7 for a, b, c in placed): return None
        return 7.0
    hs = scatter(95, (tx - 420, tx + 420, ty - 380, ty + 380), t, 13.5, 13)
    walls = ['paper', 'cream2', 'cream2', 'peach', 'paper', 'cream3', 'sun', 'teal']; roofs = ['red', 'poppy', 'red', 'rust', 'poppy', 'blue', 'teal']
    for k, (x, y, _) in enumerate(hs):
        gx = H(x + 3, y) - H(x - 3, y); gy = H(x, y + 3) - H(x, y - 3)
        yaw = math.atan2(gy, gx) - math.pi / 2 if math.hypot(gx, gy) > 0.05 else D(-90)
        yaw = D(round(math.degrees(yaw) / 15) * 15 + R.uniform(-5, 5))
        w, d = R.uniform(6.5, 10), R.uniform(6, 8)
        z = min(H(x + a, y + b) for a in (-w / 2, w / 2) for b in (-d / 2, d / 2))
        house(mb, x, y, yaw, w, d, R.choice((1, 2, 2, 3)) if k < 50 else R.choice((1, 2)), R.choice(walls), R.choice(roofs), z)
    log('houses', len(hs))
    # the seawall along the waterfront: a low stone kerb on the town shore
    return finish(mb, 'OldSaltmouth', coll, 30)


def build_lighthouse(coll):
    mb = MB(); x, y = inland(*S.PLACES['lighthouse'], 14); g = H(x, y)
    for k in range(5):
        h0 = g + 3.6 * k; r0, r1 = 3.2 - 0.18 * k, 3.2 - 0.18 * (k + 1)
        mb.add(cyl(r0, 3.6, 12, r1), 'red' if k % 2 else 'paper', M((x, y, h0 + 1.8)))
    top = g + 18
    mb.add(cyl(3.0, 0.5, 12), 'ink', M((x, y, top + 0.25)))
    mb.add(cyl(1.6, 2.4, 10), 'sun', M((x, y, top + 1.7)))
    mb.add(cyl(2.0, 1.8, 10, 0.2), 'red', M((x, y, top + 3.8)))
    mb.add(box(7, 5, 3.4), 'paper', M((x + 6, y - 5, g + 1.2))); mb.add(gable(7.6, 5.8, 2.2), 'red', M((x + 6, y - 5, g + 2.9)))
    log('lighthouse at', round(x), round(y), 'ground', round(g, 1)); return finish(mb, 'DotsLighthouse', coll, 30)


def build_wreck(coll):
    """Walt's Margaret: a 20 m trawler on a 5 m shoal, bow and wheelhouse awash, the mast leaning out (story + photo spot)."""
    mb = MB(); x, y = S.W(*S.PLACES['wreck']); m = M((x, y, -3.2), (D(18), D(-24), D(35)))
    mb.add(blob(1, (3.4, 10.5, 2.6), 77, 0.05, 0.5), lambda c, n: 'dark' if c.z < -3.4 else 'red', m)
    mb.add(box(4.0, 4.5, 3.2), 'weath', m @ M((0, 2.5, 2.8)))
    mb.add(cyl(0.22, 13, 6), 'midwood', m @ M((0, -3, 7.5)))
    return finish(mb, 'WreckOfTheMargaret', coll)


def build_cut(coll):
    """The low Voss Causeway: a road over the silted Cut (what killed the Landing). One culvert, a rail each side."""
    mb = MB(); cx, cy = S.W(2641, 3452)
    a = np.subtract(S.W(2700, 3680), S.W(2580, 3230)); ang = math.atan2(a[1], a[0])
    m = M((cx, cy, 0), (0, 0, ang))
    mb.add(box(11, 64, 0.8), 'conc', m @ M((0, 0, 1.4))); mb.add(box(1.2, 6, 1.2), 'ink', m @ M((0, 0, 0.6)))
    for s in (-1, 1):
        mb.add(box(0.3, 64, 0.9), 'greyw', m @ M((s * 5.2, 0, 2.2)))
    return finish(mb, 'VossCauseway', coll)


def build_voss(coll):
    """Blocks only (the real harbour comes later): 6 gantry cranes in Voss red, 2 freighters, container stacks, sheds, the
    VOSS tower; the no-dock buoy line 150 m outside the breakwater."""
    mb = MB()
    qx = lambda my: S.W(4060 - 0.0004 * (my - 2600) ** 2 * 1.0, my)
    for k, my in enumerate(np.linspace(2420, 2780, 6)):
        x, y = qx(my); x -= 18
        for dx in (-10, 10):
            for dy in (-6, 6): mb.add(box(1.6, 1.6, 34), 'vred', M((x + dx, y + dy, 3 + 17)))
        mb.add(box(24, 3, 3), 'vred', M((x, y - 6, 36))); mb.add(box(24, 3, 3), 'vred', M((x, y + 6, 36)))
        mb.add(box(70, 3.2, 3.2), 'vred', M((x + 22, y, 41))); mb.add(box(8, 8, 6), 'conc2', M((x - 6, y, 40)))
    cols = ['vred', 'blue', 'sun', 'teal', 'conc2', 'rust', 'iron']
    for k in range(40):
        x, y = S.W(R.uniform(3780, 3990), R.uniform(2380, 2820))
        if SDF(x, y) < 25: continue
        hc = 2.6 * R.randint(1, 4); mb.add(box(12.2, 2.5 * R.randint(3, 6), hc), R.choice(cols), M((x, y, 3 + hc / 2), (0, 0, D(90))))
    for mx, my, w, d, h in ((3790, 2470, 60, 30, 14), (3800, 2730, 70, 34, 16), (3880, 2850, 50, 28, 12)):
        x, y = S.W(mx, my); mb.add(box(w, d, h), 'conc2', M((x, y, 3 + h / 2))); mb.add(box(w + 2, d + 2, 1.2), 'iron', M((x, y, 3 + h)))
    x, y = S.W(3760, 2600); mb.add(box(16, 16, 58), 'iron', M((x, y, 32))); mb.add(box(18, 18, 4), 'vred', M((x, y, 63)))
    for my, ln in ((2500, 170), (2730, 150)):                       # freighters at the quay, bows north
        x, y = qx(my); x += 30
        mb.add(box(24, ln, 10), 'iron', M((x, y, 1))); mb.add(box(23, ln * 0.18, 12), 'paper', M((x, y - ln * 0.38, 11)))
        for q in range(6):
            mb.add(box(20, ln * 0.1, 7.8), R.choice(cols), M((x, y - ln * 0.22 + q * ln * 0.11, 9.9)))
    bl = S.Wl(S.smooth(S.BUOYLINE, 40, closed=False)); acc = 0
    for i in range(1, len(bl)):
        acc += math.dist(bl[i], bl[i - 1])
        if acc < 40: continue
        acc = 0; x, y = bl[i]
        mb.add(cyl(0.9, 1.4, 10), 'vred', M((x, y, 0.4))); mb.add(cyl(0.5, 0.7, 8), 'paper', M((x, y, 1.45)))
        mb.add(cyl(0.12, 1.2, 6), 'vred', M((x, y, 2.3)))
    return finish(mb, 'VossHarbour_blocks', coll, 30)


def build_expansion(coll):
    """P1-P3 (our future piers along the bay shore): orange marker buoys at each site's corners, nothing built."""
    mb = MB()
    for k, (x0, y0, w, h) in S.EXPANSION.items():
        for mx, my in ((x0, y0), (x0 + w, y0), (x0 + w, y0 + h), (x0, y0 + h)):
            x, y = S.W(mx, my); mb.add(cyl(0.7, 1.3, 10), 'orange', M((x, y, 0.35))); mb.add(cyl(0.08, 1.4, 6), 'ink', M((x, y, 1.6)))
    return finish(mb, 'Expansion_markers', coll)


def build_clouds(coll):
    """Ponyo cumulus: towering lumpy banks on the horizon, 8-15 km out (render only, no shadows)."""
    mb = MB(); rr = random.Random(3)
    for k in range(16):
        a = D(k * 22.5 + rr.uniform(-8, 8)); r = rr.uniform(9000, 15000)
        cx, cy = r * math.cos(a), r * math.sin(a); base = rr.uniform(500, 800); hgt = rr.uniform(1.2, 2.4)
        for q in range(rr.randint(9, 15)):
            t = q / 14; s = rr.uniform(380, 900) * (1.15 - 0.55 * t)
            o = (rr.uniform(-1, 1) * 900 * (1 - t), rr.uniform(-1, 1) * 900 * (1 - t), base + t * 1500 * hgt)
            mb.add(blob(2, (s, s, s * 0.8), 500 + k * 20 + q, 0.12, 1.1), 'cloud', M((cx + o[0], cy + o[1], o[2])))
    ob = finish(mb, 'Cumulus', coll, 80, shadow=False); ob.data.materials[0] = cloud_mat(); return ob


# ---------------------------------------------------------------- M1 assets: Walt's Landing (pier + shack) and Biscuit
def import_fbx(path, drop=()):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path, axis_forward='-Z', axis_up='Y')
    new = [o for o in bpy.data.objects if o not in before]
    for o in list(new):
        if o.name.split('.')[0] in drop or '_col' in o.name:
            new.remove(o); bpy.data.objects.remove(o, do_unlink=True)
    for o in new:
        if o.type == 'MESH':
            if o.data.color_attributes: o.data.color_attributes[0].name = 'Col'
            o.data.materials.clear(); o.data.materials.append(toon_mat())
        if o.type == 'EMPTY': o.hide_render = True
    root = bpy.data.objects.new(os.path.basename(path)[:-4], None); bpy.context.scene.collection.objects.link(root)
    for o in new:
        if o.parent is None: o.parent = root
    return root


def place_landing():
    """M1's pier runs local -Y out to sea from its root at local (0, 10): turn it -90 deg so it points west into the bay."""
    px, py = S.PIER_ROOT
    land = import_fbx(os.path.join(ART, 'PB_Landing_Walts.fbx'), drop=('Land',))
    land.matrix_world = M((px - 10, py, 0), (0, 0, D(-90)))
    boat = import_fbx(os.path.join(ART, 'PB_Boat_Biscuit_T1.fbx'))
    moor = land.matrix_world @ M((2.9, -19.0, 0))
    boat.matrix_world = moor
    return land, boat, moor


# ---------------------------------------------------------------- render
def setup_render(img):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.view_settings.view_transform = 'Standard'
    try: sc.eevee.shadow_pool_size = '1024'
    except Exception as e: log('shadow pool', e)
    try: sc.eevee.taa_render_samples = 32
    except Exception: pass
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; nt = wd.node_tree
    bg = nt.nodes['Background']; bg.inputs[1].default_value = 1.0
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ'); ramp = nt.nodes.new('ShaderNodeValToRGB')
    cr = ramp.color_ramp; cr.elements[0].position = 0.0; cr.elements[0].color = lin('#CFE6F5')
    cr.elements[1].position = 0.55; cr.elements[1].color = lin('#3A86E0'); e = cr.elements.new(0.12); e.color = lin('#8FC2F0')
    nt.links.new(tc.outputs['Generated'], sep.inputs[0]); nt.links.new(sep.outputs['Z'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], bg.inputs[0])
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3.4; sun.data.angle = D(2)
    sun.rotation_euler = (D(50), 0, D(-45)); sc.collection.objects.link(sun)          # 40 deg high from the south-west
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.clip_start = 0.1; cam.data.clip_end = 40000
    bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=40000)
    me = bpy.data.meshes.new('Sea'); bm.to_mesh(me); bm.free(); me.materials.append(sea_mat(img))
    sea = bpy.data.objects.new('Sea', me); sc.collection.objects.link(sea); sea.visible_shadow = False
    return cam


def shoot(cam, name, loc, target, lens=35, res=(1920, 1080), ortho=None, haze=(2600, 0.72), near=2.0):
    if ONLY and name.split('.')[0] not in ONLY: return
    sc = bpy.context.scene; sc.render.resolution_x, sc.render.resolution_y = res
    cam.data.type = 'ORTHO' if ortho else 'PERSP'
    if ortho: cam.data.ortho_scale = ortho
    cam.location = loc; cam.data.lens = lens; cam.data.clip_start = near   # depth precision vs the sea plane
    cam.rotation_euler = (0, 0, 0) if ortho else (Vector(target) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    set_haze(*haze)
    sc.render.filepath = os.path.join(REN, name); bpy.ops.render.render(write_still=True)
    log('RENDER', os.path.join(REN, name))


def W3(mx, my, z): x, y = S.W(mx, my); return (x, y, z)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    coll = bpy.data.collections.new('CrescentBay'); bpy.context.scene.collection.children.link(coll)
    terrain('Island', -1150, 1700, -1300, 1300, 6.0, coll, 1)
    for k, (mx, my, rx, ry) in S.ISLETS.items():
        x, y = S.W(mx, my); terrain('Islet_' + k, x - rx - 120, x + rx + 120, y - ry - 120, y + ry + 120, 4.0, coll, 2)
    build_rocks(coll); build_trees(coll); build_town(coll); build_lighthouse(coll); build_wreck(coll); build_cut(coll)
    build_voss(coll); build_expansion(coll)
    clouds = build_clouds(coll)
    land, boat, moor = place_landing()
    img = bpy.data.images.load(os.path.join(SEABED, 'seabed_2048.png')); img.colorspace_settings.name = 'Non-Color'
    cam = setup_render(img)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'crescent_bay.blend'))
    log('saved', os.path.join(OUT, 'crescent_bay.blend'))
    if not RENDER: return
    px, py = S.PIER_ROOT
    # 1 top-down over exactly map-B.png's 5 x 5 km sheet (map 0..5000), north up, no haze, no clouds
    sun = bpy.data.objects['sun']; sun.rotation_euler = (D(35), 0, D(-135))       # map convention: light from the NW
    clouds.hide_render = True; _MAT['sea_sheen'].inputs[1].default_value = 0.0
    shoot(cam, 'topdown.png', W3(2500, 2500, 4000), W3(2500, 2500.001, 0), res=(2400, 2400), ortho=5000, haze=(1e9, 0), near=100)
    clouds.hide_render = False; _MAT['sea_sheen'].inputs[1].default_value = 0.55; sun.rotation_euler = (D(50), 0, D(-45))
    # 2 hero views at eye level from the sea (2 m)
    shoot(cam, 'hero_bay_entrance.png', W3(1520, 2470, 2.0), W3(2950, 2570, 40), 26)
    shoot(cam, 'hero_town.png', W3(2560, 2470, 2.0), W3(3050, 2570, 34), 30)
    shoot(cam, 'hero_walts_landing.png', (px - 78, py - 30, 1.9), (px + 2, py + 4, 4.5), 30)
    # 3 from Biscuit's deck (eye 1.55 m over the waterline), out in the bay heading for the bay mouth
    bx, by = S.W(2580, 2560); yaw = math.atan2(*(np.subtract(S.W(1880, 1640), (bx, by))[::-1])) + math.pi / 2
    boat.matrix_world = M((bx, by, 0), (0, 0, yaw))
    eye = boat.matrix_world @ Vector((0, 1.5, 1.55)); look = boat.matrix_world @ Vector((0, -60, 1.2))
    shoot(cam, 'deck_biscuit.png', tuple(eye), tuple(look), 24, near=0.15)
    boat.matrix_world = moor
    # extras for judging the 3D shape: an aerial three-quarter and Voss Harbour from outside its buoy line
    shoot(cam, 'aerial_overview.png', W3(1250, 4100, 1100), W3(2900, 2550, 0), 32, haze=(9000, 0.45), near=20)
    shoot(cam, 'voss_from_sea.png', W3(4720, 2900, 2.5), W3(3950, 2580, 22), 32)
    os.makedirs(PROOF, exist_ok=True)
    for f in sorted(os.listdir(REN)):
        shutil.copy2(os.path.join(REN, f), os.path.join(PROOF, f))
    shutil.copy2(os.path.join(SEABED, 'seabed_2048_preview.png'), os.path.join(PROOF, 'seabed_2048_preview.png'))
    log('copied to', PROOF)


main()
