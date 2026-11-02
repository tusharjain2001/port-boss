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
TAG = next((a[4:] for a in ARGV if a.startswith('tag=')), '')            # a variant: renders get _<tag>, its seabed stays in out/
S.RIDGE_H = float(next((a[6:] for a in ARGV if a.startswith('ridge=')), S.RIDGE_H))
S.HORN = float(next((a[5:] for a in ARGV if a.startswith('horn=')), S.HORN))
if TAG: SEABED = os.path.join(OUT, 'seabed_' + TAG)
for _d in (OUT, REN, SEABED): os.makedirs(_d, exist_ok=True)

PAL = dict(dark='#805C52', ink='#3F2C27', paper='#FFFBF2', cream2='#F8EFDD', cream3='#EEDFC4', red='#D9483B',
           poppy='#E2553F', sun='#F7C548', teal='#46D6C8', blue='#2160B8', sky='#3A86E0', leaf='#7DBE6A',
           peach='#F6A27A', wood='#B9825A', midwood='#9A6A48', weath='#C9A882', greyw='#A89A8A', rope='#D8C08A',
           iron='#5E5A66', rust='#A2583A', rock='#9A93A6', rock2='#7C7690', sand='#EAD7A8', grass='#8CC46A',
           grass2='#6FAF5A', tree3='#4F8F55', heath='#B5C978', conc='#9AA3A8', conc2='#B9BEC2', vred='#C0392B',
           cloud='#FFFBF2', orange='#F2994A', path='#E3CFA6', sand2='#D9BF8C', dune='#C6CF86', shingle='#BBB0A6',
           cliff='#B9A391')
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
    ci[(ki == 1) & (nz > 0.9) & (nn < -0.25)] = CI['path']                                 # lanes and yards, not a dirt field
    ci[(be > 0.45) & (cz > 3.0) & (cz < 6.5) & (nz > 0.8) & (nn > -0.3)] = CI['dune']        # dune grass behind the sand
    ci[(be > 0.45) & (cz < 3.4) & (nz > 0.85)] = CI['sand']
    ci[(be > 0.45) & (cz < 0.5) & (nz > 0.85)] = CI['sand2']                                 # wet sand at the waterline
    ci[(be <= 0.45) & (be > 0.08) & (cz < 2.6) & (nz > 0.8) & (ki == 0)] = CI['shingle']     # pebble coves where sand meets rock
    coast = samp(F['sdf'], cx, cy) < 70
    steep = (nz < 0.78) & coast
    strata = np.floor((cz + 4 * nn) / 7).astype(int) % 3                                      # banded sea cliffs, not one grey
    ci[steep] = np.array([CI['rock'], CI['rock2'], CI['cliff']])[strata[steep]]
    sc_ = (nz < 0.78) & ~coast                                                               # steep hillside: mottled scrub
    ci[sc_] = np.where(nn[sc_] > 0.15, CI['tree3'], np.where(nn2[sc_] > 0, CI['grass2'], CI['leaf']))
    ci[(nz < 0.55) & ~coast] = CI['rock2']                                                    # ...rock only where it's very steep
    ci[(cz < 0.9) & (be <= 0.08) & (ki == 0)] = CI['rock2']
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


ALL = (-1150 - S.HORN, 1700, -1300, 1300)


def build_rocks(coll):
    mb = MB()
    def t(x, y, rr):
        d = SDF(x, y)
        if not (-14 < d < 7) or KIND(x, y) != 0: return None
        if float(samp(F['beach'], x, y)) > 0.3:                                 # a few boulders break up the beaches
            return rr.uniform(1.2, 3.0) if rr.random() < 0.08 else None
        return rr.uniform(2.0, 6.5)
    rocks = scatter(560, ALL, t, 9, 5)
    gx, gy = S.W(2250, 2550); rr = random.Random(8)                             # Gullrock's reef: tips awash on the flat
    while sum(1 for r in rocks if math.hypot(r[0] - gx, r[1] - gy) < 240) < 70:
        a = rr.uniform(0, 6.3); q = rr.uniform(1.05, 1.75)
        rocks.append((gx + 120 * q * math.cos(a), gy + 90 * q * math.sin(a), rr.uniform(1.0, 2.6)))
    # Voss Harbour's breakwater armour + a few sea stacks off the north horn's point and the lighthouse
    bwp = S.Wl(S.smooth(S.BREAKWATER, 30, closed=False))
    rocks += [(x + R.uniform(-6, 6), y + R.uniform(-6, 6), R.uniform(3, 5)) for x, y in bwp[::2]]
    for mx, my, s in ((1780, 1560, 9), (1700, 1440, 7), (1610, 1370, 6), (1840, 1500, 5), (2190, 2470, 6), (2320, 2640, 5)):
        x, y = S.W(mx, my); rocks.append((x, y, s))
    for k, (x, y, s) in enumerate(rocks):
        rz = s * R.uniform(0.55, 0.95); z = H(x, y)
        mb.add(blob(1, (s, s * R.uniform(0.7, 1.0), rz), k + 3, 0.2, 0.9), 'rock' if k % 3 else 'rock2',
               M((x, y, min(z, 0.6) - 0.2 * rz + (0.4 * rz if z > 0 else 0)), (D(R.uniform(-8, 8)), D(R.uniform(-8, 8)), D(R.uniform(0, 360)))))
    log('rocks', len(rocks)); finish(mb, 'Rocks', coll, 45); return rocks


def build_trees(coll):
    mb = MB()
    def t(x, y, rr):
        z = H(x, y)
        if not (5 < z < S.RIDGE_H * 0.92) or SDF(x, y) < 22 or KIND(x, y) != 0 or slope(x, y) > 0.85: return None
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
def hip(w, d, h):
    """A hipped roof: a 4-sided cone over a w x d base, base at z=0."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=4, radius1=0.7071, radius2=0.04, depth=1)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(D(45), 3, 'Z'))
    bmesh.ops.translate(bm, verts=bm.verts, vec=(0, 0, 0.5)); bmesh.ops.scale(bm, vec=(w, d, h), verts=bm.verts)
    return bm


def house(mb, x, y, yaw, w, d, storeys, wall, roof, z0, hipped=False, chimney=False):
    ht = 2.9 * storeys
    m = M((x, y, 0), (0, 0, yaw))
    mb.add(box(w, d, ht + 4), wall, m @ M((0, 0, z0 + ht / 2 - 2)))                 # walls (sunk 4 m into the slope)
    rh = 0.42 * d
    mb.add((hip(w + 0.8, d + 0.9, rh) if hipped else gable(w + 0.8, d + 0.9, rh)), roof, m @ M((0, 0, z0 + ht)))
    if chimney: mb.add(box(0.8, 0.8, 2.4), 'rust', m @ M((w * 0.28, d * 0.18, z0 + ht + rh * 0.6)))
    mb.add(box(1.1, 0.25, 2.1), 'ink', m @ M((0, -d / 2 - 0.05, z0 + 1.05)))           # door
    for k in range(storeys):
        for fx in (-0.3, 0.3):
            mb.add(box(1.0, 0.2, 1.15), 'sky', m @ M((w * fx, -d / 2 - 0.04, z0 + 1.6 + 2.9 * k)))
    return z0 + ht + rh                                                                 # the ridge height


def fish_sign(mb, m, loc, L, col, fin='peach'):
    """A big cartoon fish: body, tail, eye (shop signs, the market's weathervane)."""
    x, y, z = loc
    mb.add(blob(1, (L / 2, L * 0.16, L * 0.2), 31, 0.05, 0.6), col, m @ M((x, y, z)))
    mb.add(blob(1, (L * 0.12, L * 0.05, L * 0.2), 32, 0.05, 0.6), fin, m @ M((x + L * 0.55, y, z)))
    mb.add(blob(1, (L * 0.05,) * 3, 33, 0.0, 0.6), 'ink', m @ M((x - L * 0.32, y - L * 0.14, z + L * 0.05)))


def umbrella(mb, m, x, y, z, col):
    mb.add(cyl(0.05, 2.4, 6), 'ink', m @ M((x, y, z + 1.2))); mb.add(cyl(1.5, 0.6, 8, 0.05), col, m @ M((x, y, z + 2.5)))
    mb.add(cyl(0.55, 0.08, 8), 'weath', m @ M((x, y, z + 0.75)))


def bulbs(mb, a, b, n=9, sag=0.8):
    for k in range(n + 1):
        t = k / n; p = a.lerp(b, t); p.z -= sag * 4 * t * (1 - t)
        mb.add(blob(1, (0.16,) * 3, 70 + k, 0.0), 'sun', Matrix.Translation(p))


# The landmarks: each a different silhouette, front (-Y local) to the bay. Kufu's list (3 Nov) and map B's pins:
# 1 Hook, Line & Sinker, 2 Doyle Boatworks, 3 Plank & Post, 4 The Mooring Line, 5 Pratt's Fish Market (the Voss-owned
# buyer), 6 Swell Surf Co., 7 Post office + Nell's; + Hartley's Outfitters (no pin on map B: placed in town).
def lm_tackle(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); top = house(mb, x, y, yaw, 7.5, 7, 2, 'teal', 'red', z)
    for s in (-1.4, 1.4): mb.add(cyl(0.1, 1.6, 6), 'ink', m @ M((s, 0, top + 0.6)))
    fish_sign(mb, m, (0, 0, top + 2.0), 6.0, 'sun')
    mb.add(cyl(0.16, 2.6, 6), 'iron', m @ M((2.6, -4.3, z + 4.4)))                     # the giant hook on the front
    mb.add(blob(1, (0.7, 0.16, 0.5), 34, 0.0), 'iron', m @ M((2.25, -4.3, z + 3.05)))
    return top + 3.2


def lm_boatworks(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); w, d, h = 16, 20, 6.5
    mb.add(box(w, d, h + 4), 'weath', m @ M((0, 0, z + h / 2 - 2)))
    bm = cyl(w / 2 + 0.6, d + 1.0, 14)
    mb.add(bm, 'red', m @ M((0, 0, z + h), (D(90), 0, 0)) @ Matrix.Diagonal((1, 0.55, 1, 1)))   # red barrel roof
    mb.add(box(10, 0.3, 5.6), 'ink', m @ M((0, -d / 2 - 0.1, z + 2.8)))               # the open shed doors
    mb.add(box(9, 0.4, 1.7), 'blue', m @ M((0, -d / 2 - 0.3, z + h + 1.2)))           # the sign board
    run = 24; drop = z + 1.8; ang = math.atan2(drop, run)
    mb.add(box(6, run, 0.6), 'midwood', m @ M((0, -d / 2 - run / 2, z - drop / 2 - 0.2), (-ang, 0, 0)))   # slipway
    zb = z - drop * 8 / run + 1.0
    mb.add(blob(1, (2.1, 5.2, 1.2), 35, 0.04, 0.6), lambda c, n: 'red' if c.z > zb - 0.1 else 'paper', m @ M((0, -d / 2 - 8, zb), (-ang, 0, 0)))
    for s in (-1, 1): mb.add(cyl(0.18, 4, 6), 'iron', m @ M((s * 2.6, -d / 2 - 8, zb - 1.2)))
    return z + h + 3


def lm_timber(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw))
    for sx in (-5, 5):
        for sy in (-3.6, 3.6): mb.add(cyl(0.25, 5.2, 6), 'midwood', m @ M((sx, sy, z + 2.6)))
    mb.add(gable(11.5, 8.8, 2.4), 'red', m @ M((0, 0, z + 5.2)))
    for k in range(6):                                                                   # plank stacks
        mb.add(box(7.0, 1.3, 0.7 + 0.35 * (k % 3)), ('wood', 'weath', 'midwood')[k % 3], m @ M((-1.5 + (k % 2) * 3, -2.4 + (k // 2) * 2.4, z + 0.5)))
    for k in range(5): mb.add(cyl(0.35, 6, 8), 'midwood', m @ M((8.5, -2 + k * 0.72, z + 0.35 + (k % 2) * 0.55), (0, D(90), 0)))
    for s in (-1, 1): mb.add(cyl(0.18, 9.5, 6), 'sun', m @ M((-8 + s * 1.2, 2, z + 4.5), (0, D(s * 8), 0)))   # derrick
    mb.add(cyl(0.14, 8, 6), 'sun', m @ M((-8, -1.5, z + 8), (D(60), 0, 0)))
    o = m @ Vector((0, 8, 0)); house(mb, o.x, o.y, yaw, 6, 4.5, 1, 'peach', 'teal', z)    # the yard office
    return z + 9


def lm_pub(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); w, d = 12, 9
    top = house(mb, x, y, yaw, w, d, 2, 'cream2', 'dark', z, chimney=True)
    for fx in (-0.48, -0.16, 0.16, 0.48): mb.add(box(0.3, 0.16, 5.8), 'dark', m @ M((w * fx, -d / 2 - 0.12, z + 2.9)))
    mb.add(box(w, 0.18, 0.3), 'dark', m @ M((0, -d / 2 - 0.12, z + 2.9)))
    mb.add(box(0.15, 1.6, 0.15), 'ink', m @ M((-w / 2 - 0.8, -d / 2 + 0.2, z + 4.6), (0, 0, D(90))))
    mb.add(box(0.15, 1.3, 1.1), 'red', m @ M((-w / 2 - 1.3, -d / 2 + 0.2, z + 3.9), (0, 0, D(90))))   # hanging sign
    for k, col in enumerate(('poppy', 'paper', 'poppy')): umbrella(mb, m, -3.5 + k * 3.5, -d / 2 - 4, z, col)
    for s in (-1, 1): mb.add(cyl(0.08, 4.6, 6), 'ink', m @ M((s * 6, -d / 2 - 7, z + 2.3)))
    a = m @ Vector((-6, -d / 2 - 7, z + 4.5)); b = m @ Vector((6, -d / 2 - 7, z + 4.5)); c = m @ Vector((0, -d / 2, z + 5.5))
    bulbs(mb, a, b); bulbs(mb, a, c, 7, 0.5); bulbs(mb, b, c, 7, 0.5)
    return top + 1


def lm_market(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); w, d = 24, 11
    mb.add(box(w + 1, d + 1, 0.5), 'conc2', m @ M((0, 0, z + 0.1)))
    for sx in np.linspace(-w / 2 + 0.6, w / 2 - 0.6, 6):
        for sy in (-d / 2 + 0.5, d / 2 - 0.5): mb.add(cyl(0.2, 4.2, 6), 'paper', m @ M((sx, sy, z + 2.2)))
    for k in range(6):                                                                   # the striped roof
        mb.add(gable(w / 6 + 0.02, d + 1.6, 2.6), 'blue' if k % 2 else 'paper', m @ M((-w / 2 + w / 12 + k * w / 6, 0, z + 4.3)))
    for k in range(3): mb.add(box(w * 0.8, 1.1, 1.0), 'weath', m @ M((0, -3 + k * 3, z + 0.85)))   # fish counters
    mb.add(cyl(0.1, 2.2, 6), 'ink', m @ M((0, 0, z + 7.8))); fish_sign(mb, m, (0, 0, z + 9.0), 4.2, 'conc2', 'blue')
    return z + 10


def lm_surf(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); top = house(mb, x, y, yaw, 7, 5, 1, 'sun', 'teal', z)
    for k, col in enumerate(('poppy', 'teal', 'sky', 'paper', 'peach', 'blue')):
        mb.add(blob(1, (0.32, 0.08, 1.25), 40 + k, 0.0), col, m @ M((-3 + k * 1.15, -5.2 - 0.4 * abs(k - 2.5), z + 1.0), (D(-8), D(-15 + k * 6), 0)))
    umbrella(mb, m, 4.5, -6, z, 'poppy'); mb.add(box(3.4, 0.2, 1.0), 'sky', m @ M((0, -2.6, top + 0.4)))
    return top + 1.5


def lm_post(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); top = house(mb, x, y, yaw, 10, 8, 2, 'red', 'dark', z)
    tx, ty = 6.2, -2.0
    mb.add(box(3.6, 3.6, 16 + 4), 'cream2', m @ M((tx, ty, z + 8 - 2)))
    mb.add(hip(4.2, 4.2, 3.4), 'teal', m @ M((tx, ty, z + 16)))
    mb.add(cyl(1.3, 0.2, 14), 'ink', m @ M((tx, ty - 1.85, z + 13.6), (D(90), 0, 0)))
    mb.add(cyl(1.12, 0.24, 14), 'paper', m @ M((tx, ty - 1.9, z + 13.6), (D(90), 0, 0)))
    mb.add(box(1.2, 0.5, 1.4), 'red', m @ M((-6.5, -4.8, z + 0.7)))                     # the pillar box
    return z + 20


def lm_outfitters(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); top = house(mb, x, y, yaw, 9, 8, 2, 'peach', 'blue', z)
    mb.add(cyl(3.0, 0.3, 16), 'sun', m @ M((0, 0, top + 0.9)))                         # the giant sou'wester sign
    mb.add(blob(1, (1.7, 1.7, 1.4), 36, 0.04), 'sun', m @ M((0, 0, top + 1.6)))
    mb.add(box(7, 1.8, 0.2), 'sky', m @ M((0, -4.9, z + 2.6), (D(-14), 0, 0)))
    return top + 3.5


LANDMARKS = {1: ("Hook, Line & Sinker", lm_tackle), 2: ("Doyle Boatworks", lm_boatworks), 3: ("Plank & Post", lm_timber),
             4: ("The Mooring Line", lm_pub), 5: ("Pratt's Fish Market", lm_market), 6: ("Swell Surf Co.", lm_surf),
             7: ("Post office + Nell's", lm_post), 8: ("Hartley's Outfitters", lm_outfitters)}
PIN8 = (3045, 2505)                    # Hartley's: no pin on map B; on the high street between the pub and the market
LABELS = []                            # (name, world point) for the labelled overhead


def build_town(coll):
    mb = MB(); px, py = S.PIER_ROOT; placed = []; rr = random.Random(17)
    for n, (name, fn) in LANDMARKS.items():
        mx, my = S.SHOPS[n][1] if n in S.SHOPS else PIN8
        x, y = inland(mx - (45 if n == 6 else 0), my, 10)          # Swell Surf: 45 m west, off the Cut's bank
        z = min(H(x + a, y + b) for a in (-6, 6) for b in (-5, 5))
        top = fn(mb, x, y, z, D(-90 + rr.uniform(-5, 5)))
        placed.append((x, y, 13)); LABELS.append((name, (x, y, top))); log('landmark', n, name, 'at (%.0f, %.0f) z %.1f' % (x, y, z))
    LABELS.append(("Walt's Landing (our dock)", (px - 16, py, 4)))
    # the waterfront: terrace rows of narrow, bright houses along the town shore, lamps, dinghies at their moorings
    tx, ty = S.W(2965, 2560); shore = []
    for curve in (S.smooth(S.TOWN, 60), S.smooth(S.island_pts(), 40)):
        P = np.array(S.Wl(curve))
        for a, b in zip(P, np.roll(P, -1, 0)):
            for t in np.arange(0, 1, 1.5 / max(np.linalg.norm(b - a), 1.5)): shore.append(a + (b - a) * t)
    walls = ['paper', 'sun', 'peach', 'teal', 'cream2', 'sky', 'paper', 'poppy']; roofs = ['red', 'poppy', 'rust', 'blue', 'red', 'teal']
    rows = []; k = 0
    for p in shore:
        if math.hypot((p[0] - tx) * 0.85, p[1] - ty) > 330 or abs(SDF(p[0], p[1])) > 4: continue      # the real waterline only
        gx = SDF(p[0] + 3, p[1]) - SDF(p[0] - 3, p[1]); gy = SDF(p[0], p[1] + 3) - SDF(p[0], p[1] - 3); g = math.hypot(gx, gy) or 1
        nx, ny = gx / g, gy / g; qx, qy = p[0] + nx * 15, p[1] + ny * 15
        if KIND(qx, qy) != 1 or not (9 < SDF(qx, qy) < 24) or not (1.2 < H(qx, qy) < 14): continue
        if math.hypot(qx - px - 12, qy - py - 3) < 36 or any(math.hypot(qx - a, qy - b) < 14 for a, b, _ in placed): continue
        if any(math.hypot(qx - a, qy - b) < 5.6 for a, b in rows): continue
        rows.append((qx, qy)); k += 1
        yaw = math.atan2(-ny, -nx) + math.pi / 2
        house(mb, qx, qy, yaw, rr.uniform(4.8, 5.6), 7, rr.choice((2, 2, 3)), walls[k % len(walls)], roofs[(k * 5) % len(roofs)],
              H(qx, qy) - 0.2, hipped=False, chimney=rr.random() < 0.4)
        if k % 4 == 0:                                                                   # a lamp on the quay
            lx, ly = p[0] + nx * 7, p[1] + ny * 7; g0 = H(lx, ly)
            if g0 > 0.5: mb.add(cyl(0.1, 4.2, 6), 'ink', M((lx, ly, g0 + 2.1))); mb.add(blob(1, (0.35,) * 3, 90 + k, 0.0), 'sun', M((lx, ly, g0 + 4.3)))
        if k % 3 == 0:                                                                   # a dinghy off the quay
            dx, dy = p[0] - nx * rr.uniform(12, 28), p[1] - ny * rr.uniform(12, 28)
            if H(dx, dy) < -1:
                mb.add(blob(1, (0.95, 2.4, 0.45), 120 + k, 0.03, 0.6), lambda c, n_: 'paper' if c.z > 0.25 else rr.choice(('red', 'teal', 'sun', 'sky')),
                       M((dx, dy, 0.1), (0, 0, rr.uniform(0, 6.3))))
    placed += [(a, b, 6) for a, b in rows]
    log('waterfront rows', len(rows))
    # houses up the hillside: each faces downhill toward the bay; some hipped roofs and chimneys break the rhythm
    def t(x, y, r_):
        r = math.hypot((x - tx) * 0.85, y - ty)
        if r > 380 or r_.random() > 1.15 - r / 380: return None
        z = H(x, y)
        if not (1.2 < z < 48) or SDF(x, y) < 9 or slope(x, y) > 0.42 or KIND(x, y) in (2, 3): return None
        if math.hypot(x - px - 12, y - py - 3) < 34: return None                         # Walt's Landing yard
        if any(math.hypot(x - a, y - b) < c + 7 for a, b, c in placed): return None
        return 7.0
    hs = scatter(130, (tx - 420, tx + 420, ty - 380, ty + 380), t, 12.0, 13, 120000)
    walls = ['paper', 'cream2', 'cream2', 'peach', 'paper', 'cream3', 'sun', 'teal']; roofs = ['red', 'poppy', 'red', 'rust', 'poppy', 'blue', 'teal']
    for k, (x, y, _) in enumerate(hs):
        gx = H(x + 3, y) - H(x - 3, y); gy = H(x, y + 3) - H(x, y - 3)
        yaw = math.atan2(gy, gx) - math.pi / 2 if math.hypot(gx, gy) > 0.05 else D(-90)
        yaw = D(round(math.degrees(yaw) / 15) * 15 + R.uniform(-5, 5))
        w, d = R.uniform(6.5, 10), R.uniform(6, 8)
        z = min(H(x + a, y + b) for a in (-w / 2, w / 2) for b in (-d / 2, d / 2))
        house(mb, x, y, yaw, w, d, R.choice((1, 2, 2, 3)) if k < 70 else R.choice((1, 2)), R.choice(walls), R.choice(roofs), z,
              hipped=rr.random() < 0.3, chimney=rr.random() < 0.5)
    log('houses', len(hs))
    return finish(mb, 'OldSaltmouth', coll, 30)


def build_maris(coll, land):
    """Mari Duarte's taco boat, moored across the pier from Biscuit (her Day-3+ state: she pays $40/day to moor here)."""
    mb = MB(); L = land.matrix_world @ M((-3.3, -14.5, 0))
    mb.add(blob(1, (1.6, 4.0, 0.95), 37, 0.03, 0.6), lambda c, n: 'poppy' if c.z < 0.35 else 'sun', L @ M((0, 0, 0.15)))
    mb.add(box(2.4, 6.0, 0.25), 'weath', L @ M((0, 0, 0.85)))
    mb.add(box(2.2, 2.6, 1.9), 'teal', L @ M((0, 0.8, 1.9))); mb.add(box(0.1, 1.6, 0.8), 'ink', L @ M((1.12, 0.8, 2.2)))
    mb.add(box(1.2, 3.0, 0.12), 'poppy', L @ M((1.6, 0.8, 2.8), (0, D(18), 0)))
    mb.add(cyl(0.06, 3.4, 6), 'ink', L @ M((0, -2.4, 2.6)))
    bulbs(mb, L @ Vector((0, -2.4, 4.2)), L @ Vector((0, 2.0, 2.9)), 6, 0.3)
    LABELS.append(("Mari's taco boat (Day 3+)", tuple(L @ Vector((0, 0, 3)))))
    return finish(mb, 'MarisTacoBoat', coll, 30)


def build_lighthouse(coll):
    """Dot's lighthouse: 26 m of red and white bands on the north horn's knoll, so it reads from the bay mouth."""
    mb = MB(); x, y = inland(*S.PLACES['lighthouse'], 16); g = H(x, y)
    mb.add(blob(1, (9, 8, 3), 41, 0.15), 'rock2', M((x, y, g - 1.0)))
    for k in range(6):
        h0 = g + 4.0 * k; r0, r1 = 4.0 - 0.2 * k, 4.0 - 0.2 * (k + 1)
        mb.add(cyl(r0, 4.0, 14, r1), 'red' if k % 2 else 'paper', M((x, y, h0 + 2.0)))
    top = g + 24
    mb.add(cyl(3.6, 0.5, 14), 'ink', M((x, y, top + 0.25)))
    for a in range(12): mb.add(cyl(0.06, 1.0, 4), 'ink', M((x + 3.4 * math.cos(a * 0.524), y + 3.4 * math.sin(a * 0.524), top + 1.0)))
    mb.add(cyl(1.9, 3.0, 12), 'sun', M((x, y, top + 2.0)))
    mb.add(cyl(2.3, 2.4, 12, 0.25), 'red', M((x, y, top + 4.7)))
    mb.add(box(8, 6, 3.6), 'paper', M((x + 8, y - 6, g + 1.3))); mb.add(gable(8.6, 6.8, 2.4), 'red', M((x + 8, y - 6, g + 3.1)))
    LABELS.append(("Dot's lighthouse", (x, y, top + 6)))
    log('lighthouse at', round(x), round(y), 'ground', round(g, 1)); return finish(mb, 'DotsLighthouse', coll, 30)


def build_wreck(coll):
    """Walt's Margaret: a 26 m trawler on a 5 m shoal, bow and wheelhouse out of the water, masts leaning (story + photo spot)."""
    mb = MB(); x, y = S.W(*S.PLACES['wreck']); m = M((x, y, -1.6), (D(14), D(-22), D(35)))
    mb.add(blob(1, (4.2, 13.0, 3.2), 77, 0.05, 0.5), lambda c, n: 'dark' if c.z < -2.2 else 'red', m)
    mb.add(box(7.2, 2.0, 0.5), 'paper', m @ M((0, -9.5, 2.2)))
    mb.add(box(4.4, 5.0, 3.6), 'paper', m @ M((0, 3.2, 4.1))); mb.add(box(4.6, 5.2, 0.4), 'red', m @ M((0, 3.2, 6.0)))
    for q, (yy, h) in enumerate(((-4.5, 17), (8, 9))):
        mb.add(cyl(0.26, h, 6), 'midwood', m @ M((0, yy, 3 + h / 2), (D(-6 + q * 18), 0, 0)))
    mb.add(cyl(0.12, 7, 6), 'midwood', m @ M((0, -4.5, 13), (0, D(90), 0)))
    LABELS.append(("Wreck of the Margaret", (x, y, 18)))
    return finish(mb, 'WreckOfTheMargaret', coll)


def dash(mb, x, y, L, w, ang, seed):
    mb.add(blob(1, (L / 2, w / 2, 0.06), seed, 0.08, 0.7), 'paper', M((x, y, 0.05), (0, 0, ang)))


def build_foam(coll, rocks):
    """Ponyo's crisp white foam: collars round wet rocks, breaking surf lines along the beaches, the Widowmaker on
    Gullrock's reef edge, the north-horn point reef and the wreck (render stand-in for the game's foam shader)."""
    mb = MB(); rr = random.Random(23); k = 0
    for x, y, s in rocks:
        if -8 < H(x, y) < 0.8:
            k += 1; mb.add(cyl(1.05 * s + 0.6, 0.08, 10), 'paper', M((x, y, 0.04)))
    def surf(x, y, r_):
        if float(samp(F['beach'], x, y)) < 0.6 or KIND(x, y) == 3: return None
        d = SDF(x, y); return 1.0 if (-15 < d < -9 or -34 < d < -27) else None
    for x, y, _ in scatter(900, ALL, surf, 15, 31, 200000):
        gx = SDF(x + 3, y) - SDF(x - 3, y); gy = SDF(x, y + 3) - SDF(x, y - 3)
        dash(mb, x, y, rr.uniform(10, 22), rr.uniform(0.9, 1.6), math.atan2(gx, -gy), 300 + k); k += 1
    for (mx, my, rx, ry), a0, a1 in (((2250, 2550, 200, 170), 95, 265), ((2250, 2550, 235, 205), 110, 250), ((1650, 1330, 120, 110), 0, 360),
                                      ((1000, 1700, 15, 13), 0, 360)):
        cx, cy = S.W(mx, my)
        for a in np.arange(a0, a1, 4 if rx > 100 else 30):
            t = math.radians(a); j = rr.uniform(-12, 12) if rx > 100 else rr.uniform(-2, 2)
            x, y = cx + (rx + j) * math.cos(t), cy + (ry + j) * math.sin(t)
            dash(mb, x, y, rr.uniform(10, 20) if rx > 100 else 5, rr.uniform(2.5, 4.5) if rx > 100 else 1.6, t + math.pi / 2, 500 + k); k += 1
    log('foam pieces', k)
    return finish(mb, 'Foam', coll, 80, shadow=False)


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
    if TAG: name = name.replace('.png', '_' + TAG + '.png')
    sc.render.filepath = os.path.join(REN, name); bpy.ops.render.render(write_still=True)
    log('RENDER', os.path.join(REN, name))


def W3(mx, my, z): x, y = S.W(mx, my); return (x, y, z)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    coll = bpy.data.collections.new('CrescentBay'); bpy.context.scene.collection.children.link(coll)
    terrain('Island', -1150 - S.HORN, 1700, -1300, 1300, 6.0, coll, 1)
    for k, (mx, my, rx, ry) in S.ISLETS.items():
        x, y = S.W(mx, my); terrain('Islet_' + k, x - rx - 120, x + rx + 120, y - ry - 120, y + ry + 120, 4.0, coll, 2)
    rocks = build_rocks(coll); build_trees(coll); build_town(coll); build_lighthouse(coll); build_wreck(coll); build_cut(coll)
    build_voss(coll); build_expansion(coll); build_foam(coll, rocks)
    LABELS.append(("Gullrock reef", S.W(2250, 2550) + (14,)))
    clouds = build_clouds(coll)
    land, boat, moor = place_landing(); build_maris(coll, land)
    img = bpy.data.images.load(os.path.join(SEABED, 'seabed_2048.png')); img.colorspace_settings.name = 'Non-Color'
    cam = setup_render(img)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'crescent_bay%s.blend' % ('_' + TAG if TAG else '')))
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
    bx, by = S.W(2450, 2640); yaw = math.atan2(*(np.subtract(S.W(3350, 2520), (bx, by))[::-1])) + math.pi / 2
    boat.matrix_world = M((bx, by, 0), (0, 0, yaw))                                   # ...and heading home: town + ridge
    eye = boat.matrix_world @ Vector((0, 1.5, 1.55)); look = boat.matrix_world @ Vector((0, -60, 3.0))
    shoot(cam, 'deck_home.png', tuple(eye), tuple(look), 24, near=0.15)
    boat.matrix_world = moor
    # the sea landmarks: Gullrock's reef break with Dot's lighthouse beyond; the Margaret's wreck with the north horn behind
    shoot(cam, 'landmarks_reef_lighthouse.png', W3(2180, 2850, 2.2), W3(1930, 1640, 18), 28)
    shoot(cam, 'landmarks_wreck.png', W3(860, 1790, 2.0), W3(1500, 1640, 8), 32)
    # extras for judging the 3D shape: an aerial three-quarter and Voss Harbour from outside its buoy line
    shoot(cam, 'aerial_overview.png', W3(1250, 4100, 1100), W3(2900, 2550, 0), 32, haze=(9000, 0.45), near=20)
    shoot(cam, 'voss_from_sea.png', W3(4720, 2900, 2.5), W3(3950, 2580, 22), 32)
    # the town from above the bay, for the labelled landmark sheet (labels: pixel positions in town_labels.json)
    tl = ((S.PIER_ROOT[0] - 150, S.PIER_ROOT[1] - 190, 105), (S.PIER_ROOT[0] + 95, S.PIER_ROOT[1] + 5, 4))
    shoot(cam, 'town_overhead.png', tl[0], tl[1], 30, haze=(6000, 0.4))
    if not ONLY or 'town_overhead' in ONLY:
        from bpy_extras.object_utils import world_to_camera_view
        sc = bpy.context.scene; out = []
        for name, p in LABELS:
            u, v, dep = world_to_camera_view(sc, cam, Vector(p))
            if dep > 0 and 0 <= u <= 1 and 0 <= v <= 1: out.append(dict(name=name, x=u * sc.render.resolution_x, y=(1 - v) * sc.render.resolution_y))
        import json; json.dump(out, open(os.path.join(REN, 'town_labels%s.json' % ('_' + TAG if TAG else '')), 'w'), indent=1)
    os.makedirs(PROOF, exist_ok=True)
    for f in sorted(os.listdir(REN)):
        if f.endswith('.png') and (not TAG or f.endswith('_' + TAG + '.png')): shutil.copy2(os.path.join(REN, f), os.path.join(PROOF, f))
    if not TAG: shutil.copy2(os.path.join(SEABED, 'seabed_2048_preview.png'), os.path.join(PROOF, 'seabed_2048_preview.png'))
    log('copied to', PROOF)


main()
