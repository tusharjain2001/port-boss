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
           cliff='#B9A391',
           # the finished town (4 Nov): window glass, stone plinths/steps/kerbs, cobbles, soft wall pastels, shutters
           glass='#3D6E9E', glass2='#5A8DB8', stone='#A8998A', stone2='#8C7F72', cob='#B8A07C', cob2='#A58C6A', cob3='#C6B08C',
           wmint='#CFEBDB', wsky='#CCE1F2', wbutter='#F8E5AE', wrose='#F5CFC3', wlav='#DDD6EE', sage='#86B59A', navy='#2D4F7C',
           flower='#E8577A', flower2='#F4B63F', hedge='#5E9E58',
           earth='#D2B48A', earth2='#C4A57A', scrub='#64A35A', quay='#9A8B7C')
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
    return terrain_mesh(name, GX, GY, coll)


def terrain_mesh(name, GX, GY, coll, floor=-6.0, keep_z=-1.0, skirt=0.0):
    """The terrain on a (jittered) grid: tris that reach above keep_z, the floor clamped, palette colour per face.
    skirt > 0: a curtain that far down along the grid's 4 edges (both windings), hiding cracks between cells at different LODs."""
    Z = samp(F['h'], GX, GY)
    Z = np.maximum(Z, floor)
    ny, nx = GX.shape; idx = np.arange(nx * ny).reshape(ny, nx)
    a, b, c, d = idx[:-1, :-1], idx[:-1, 1:], idx[1:, 1:], idx[1:, :-1]
    zf = Z.ravel(); XS, YS = GX.ravel(), GY.ravel()
    keep = np.maximum.reduce([zf[a], zf[b], zf[c], zf[d]]) > keep_z     # only what meets the waterline (or the shallows)
    alt = ((np.add.outer(np.arange(ny - 1), np.arange(nx - 1))) % 2 == 0)
    t1 = np.where(alt[..., None], np.stack([a, b, c], -1), np.stack([a, b, d], -1))
    t2 = np.where(alt[..., None], np.stack([a, c, d], -1), np.stack([b, c, d], -1))
    tris = np.concatenate([t1[keep], t2[keep]])
    if not len(tris): return None
    if skirt > 0:
        on = np.zeros(nx * ny, bool); on[tris.ravel()] = True; ev, et = [], []
        for side in (idx[0, :], idx[:, -1], idx[-1, ::-1], idx[::-1, 0]):
            for p, q in zip(side[:-1], side[1:]):
                if on[p] and on[q]:              # one winding on the grid's own edge verts, the other on copies (validate() merges twins)
                    k = nx * ny + len(ev); ev += [(p, skirt), (q, skirt), (p, 0), (q, 0), (p, skirt), (q, skirt)]
                    et += [(p, q, k + 1), (p, k + 1, k), (k + 2, k + 5, k + 3), (k + 2, k + 4, k + 5)]
        if ev:
            src = np.array([e[0] for e in ev]); dz = np.array([e[1] for e in ev])
            XS = np.concatenate([XS, XS[src]]); YS = np.concatenate([YS, YS[src]]); zf = np.concatenate([zf, zf[src] - dz])
            tris = np.concatenate([tris, np.array(et)])
    used, inv = np.unique(tris, return_inverse=True); tris = inv.reshape(-1, 3)
    V = np.stack([XS[used], YS[used], zf[used]], -1)
    me = bpy.data.meshes.new(name); me.vertices.add(len(V)); me.vertices.foreach_set('co', V.astype(np.float32).ravel())
    me.loops.add(len(tris) * 3); me.loops.foreach_set('vertex_index', tris.astype(np.int32).ravel())
    me.polygons.add(len(tris)); me.polygons.foreach_set('loop_start', np.arange(0, len(tris) * 3, 3, dtype=np.int32))
    me.update(); me.validate()
    if len(me.polygons) != len(tris):        # validate() dropped degenerate faces: colour what is left
        vi = np.zeros(len(me.loops), np.int32); me.loops.foreach_get('vertex_index', vi); tris = vi.reshape(-1, 3)
    # per-face palette colour from the centroid: height, slope, sand/rock, town, Voss, the Cut
    P = V[tris]; cen = P.mean(1); nrm = np.cross(P[:, 1] - P[:, 0], P[:, 2] - P[:, 0])
    nz = np.abs(nrm[:, 2]) / (np.linalg.norm(nrm, axis=1) + 1e-9)
    cx, cy, cz = cen[:, 0], cen[:, 1], cen[:, 2]
    be = samp(F['beach'], cx, cy)
    ki = F['kind'][np.clip(((S.YN - cy) / CS).astype(int), 0, S.N - 1), np.clip(((cx - S.XW) / CS).astype(int), 0, S.N - 1)]
    nn = S.waves(cx, cy, 11, [(140, 1), (90, 0.7), (55, 0.5)]); nn2 = S.waves(cx, cy, 5, [(230, 1), (120, 0.6)])
    nf = S.waves(cx, cy, 17, [(52, 1), (37, 0.8), (26, 0.6), (19, 0.45), (13, 0.3)])
    ci = np.full(len(tris), CI['grass'])
    ci[nf > 0.9] = CI['grass2']; ci[nn2 > 0.9] = CI['leaf']
    ci[(cz > 122 + 10 * nn) & (nn2 > -0.2)] = CI['heath']
    ci[(ki == 1) & (nz > 0.9) & (nn < -0.25)] = CI['grass2']                               # town gardens (the streets are their own mesh)
    ci[(be > 0.45) & (cz > 3.0) & (cz < 6.5) & (nz > 0.8) & (nn > -0.3)] = CI['dune']        # dune grass behind the sand
    ci[(be > 0.45) & (cz < 3.4) & (nz > 0.85)] = CI['sand']
    ci[(be > 0.45) & (cz < 0.5) & (nz > 0.85)] = CI['sand2']                                 # wet sand at the waterline
    ci[(be <= 0.45) & (be > 0.08) & (cz < 2.6) & (nz > 0.8) & (ki == 0)] = CI['shingle']     # pebble coves where sand meets rock
    coast = samp(F['sdf'], cx, cy) < 70
    steep = (nz < 0.78) & coast
    strata = np.floor((cz + 4 * nn) / 7).astype(int) % 3                                      # banded sea cliffs, not one grey
    ci[steep] = np.array([CI['rock'], CI['rock2'], CI['cliff']])[strata[steep]]
    sc_ = (nz < 0.78) & ~coast                                                               # steep hillside: mottled scrub
    ci[sc_] = np.where(nf[sc_] > 0.7, CI['scrub'], np.where(nf[sc_] < -1.2, CI['leaf'], CI['grass2']))
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
    # Walt's Landing stays clear (INTEGRATED-3): a rock poked through the pier root and sat by the cot. The footprint: the
    # 32 m pier out west from the root, the shack and the yard behind it. R is still drawn for a skipped rock, so every
    # other rock keeps its exact shape and place.
    px, py = S.PIER_ROOT; bx0, bx1, by0, by1 = px - 36, px + 18, py - 10, py + 16
    def on_landing(x, y, s): return math.hypot(max(bx0 - x, 0, x - bx1), max(by0 - y, 0, y - by1)) < s + 1.0
    for k, (x, y, s) in enumerate(rocks):
        rz = s * R.uniform(0.55, 0.95); z = H(x, y)
        if on_landing(x, y, s):
            R.uniform(0.7, 1.0); R.uniform(-8, 8); R.uniform(-8, 8); R.uniform(0, 360); log('rock off the landing', round(x, 1), round(y, 1), s); continue
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


WIN = None                             # the panes that light up at night (one MB, exported per cell as "_Windows")


def window(mb, m, ox, oy, nx, ny, z, front, lit, shut):
    """A window on the wall whose outward normal is (nx, ny), its centre (ox, oy) on the wall plane: a white frame plate,
    the glass proud of it, a sill; at the front a cross bar, maybe a flower box; maybe shutters; maybe a lit pane."""
    yaw = math.atan2(nx, -ny)                                   # local -Y (out of the window) -> (nx, ny)
    def at(off, dx=0.0, dz=0.0): return m @ M((ox + nx * off - ny * dx, oy + ny * off + nx * dx, z + dz), (0, 0, yaw))
    mb.add(box(1.3, 0.1, 1.75), 'paper', at(0.0))
    mb.add(box(1.0, 0.12, 1.45), 'glass', at(0.02))
    mb.add(box(1.5, 0.3, 0.12), 'paper', at(0.1, 0, -0.93))
    if front:
        mb.add(box(0.08, 0.06, 1.45), 'paper', at(0.1)); mb.add(box(1.0, 0.06, 0.08), 'paper', at(0.1, 0, 0.15))
    if shut: 
        for sx in (-0.95, 0.95): mb.add(box(0.55, 0.08, 1.55), shut, at(0.03, sx))
    if front and lit is not None and lit.random() < 0.3:          # a flower box
        mb.add(box(1.2, 0.3, 0.25), 'midwood', at(0.22, 0, -0.77))
        mb.add(blob(1, (0.55, 0.16, 0.18), int(ox * 13 + z * 7) % 97, 0.25, 2.0), lit.choice(('flower', 'flower2', 'poppy', 'paper')), at(0.22, 0, -0.57))
    if lit is not None and lit.random() < 0.5 and WIN is not None:   # lit after dusk (Map.cs: Sky's lamp weight)
        WIN.add(box(0.96, 0.13, 1.4), 'paper', at(0.025))


HOUSES = []                            # (x, y, yaw, w, d, z0) of every house built (the gardens go round them)


def house(mb, x, y, yaw, w, d, storeys, wall, roof, z0, hipped=False, chimney=False):
    ht = 2.9 * storeys; HOUSES.append((x, y, yaw, w, d, z0))
    m = M((x, y, 0), (0, 0, yaw)); rk = random.Random(int(x * 7.3 + y * 13.1))   # (its own dice: the layout's stay as they were)
    mb.add(box(w, d, ht + 4), wall, m @ M((0, 0, z0 + ht / 2 - 2)))                 # walls (sunk 4 m into the slope)
    mb.add(box(w + 0.14, d + 0.14, 0.9), 'stone', m @ M((0, 0, z0 + 0.0)))            # a stone plinth
    mb.add(box(w + 0.12, d + 0.12, 0.22), 'paper' if wall not in ('paper', 'cream2') else 'stone', m @ M((0, 0, z0 + ht - 0.11)))   # the eaves band
    rh = 0.42 * d
    mb.add((hip(w + 0.8, d + 0.9, rh) if hipped else gable(w + 0.8, d + 0.9, rh)), roof, m @ M((0, 0, z0 + ht)))
    if chimney: 
        mb.add(box(0.8, 0.8, 2.4), 'rust', m @ M((w * 0.28, d * 0.18, z0 + ht + rh * 0.6)))
        mb.add(box(1.0, 1.0, 0.2), 'stone2', m @ M((w * 0.28, d * 0.18, z0 + ht + rh * 0.6 + 1.25)))
    shut = rk.choice((None, None, 'teal', 'sage', 'blue', 'navy', 'red', 'sun')) if wall not in ('teal', 'sky', 'blue') else None
    door = shut or rk.choice(('navy', 'red', 'teal', 'midwood', 'sage'))
    for sy in (-1, 1):                                                                # the doors, front and back: frame, door, step
        mb.add(box(1.45, 0.1, 2.45), 'paper', m @ M((0, sy * d / 2, z0 + 1.2)))
        mb.add(box(1.1, 0.16, 2.1), door, m @ M((0, sy * (d / 2 + 0.02), z0 + 1.05)))
        mb.add(box(1.9, 0.9, 0.3), 'stone2', m @ M((0, sy * (d / 2 + 0.45), z0 + 0.1)))
    band = 'paper' if wall not in ('paper', 'cream2') else 'stone'
    for k in range(1, storeys): mb.add(box(w + 0.1, d + 0.1, 0.16), band, m @ M((0, 0, z0 + 2.9 * k + 0.1)))   # a band at each floor
    ncol = max(2, int(w / 2.4)); xs = np.linspace(-w / 2 + w / (2 * ncol), w / 2 - w / (2 * ncol), ncol) if ncol > 1 else [0.0]
    nside = max(1, int(d / 3.2)); ys = np.linspace(-d / 2 + d / (2 * nside), d / 2 - d / (2 * nside), nside) if nside > 1 else [0.0]
    for k in range(storeys):
        z = z0 + 1.6 + 2.9 * k
        for fx in xs:
            if k == 0 and abs(fx) < 1.1: continue                                      # (the doors)
            window(mb, m, fx, -d / 2, 0, -1, z, True, rk, shut)
            window(mb, m, fx, d / 2, 0, 1, z, True, rk, shut)
        for fy in ys:
            for sx in (-1, 1): window(mb, m, sx * w / 2, fy, sx, 0, z, False, rk, shut)
    return z0 + ht + rh                                                                 # the ridge height


def hull(W, Lg, Hh):
    """A boat hull: flat deck outline with a pointed bow (+Y), tapering in to a narrow keel Hh below; top at z=0."""
    bm = bmesh.new(); top = [(-W / 2, -Lg / 2), (W / 2, -Lg / 2), (W / 2, Lg * 0.18), (W * 0.28, Lg * 0.4), (0, Lg / 2), (-W * 0.28, Lg * 0.4), (-W / 2, Lg * 0.18)]
    R = [[bm.verts.new((x * k, y * (1 - 0.18 * (1 - k) / 0.55) - 0.2 * (1 - k) / 0.55, z)) for x, y in top] for k, z in ((1, 0), (0.94, -0.35), (0.45, -Hh))]
    for a, b in zip(R, R[1:]):                                                           # (a sheer band, then the hull down to the keel)
        for i in range(len(top)): j = (i + 1) % len(top); bm.faces.new((a[i], a[j], b[j], b[i]))
    bm.faces.new(list(reversed(R[0]))); bm.faces.new(R[-1]); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); return bm


def fish_sign(mb, m, loc, L, col, fin='peach'):
    """A big cartoon fish: body, tail, eye (shop signs, the market's weathervane)."""
    x, y, z = loc
    mb.add(blob(1, (L / 2, L * 0.16, L * 0.2), 31, 0.05, 0.6), col, m @ M((x, y, z)))
    mb.add(blob(1, (L * 0.12, L * 0.05, L * 0.2), 32, 0.05, 0.6), fin, m @ M((x + L * 0.55, y, z)))
    mb.add(blob(1, (L * 0.05,) * 3, 33, 0.0, 0.6), 'ink', m @ M((x - L * 0.32, y - L * 0.14, z + L * 0.05)))


def umbrella(mb, m, x, y, z, col):
    mb.add(cyl(0.05, 2.4, 6), 'ink', m @ M((x, y, z + 1.2))); mb.add(cyl(1.5, 0.6, 8, 0.05), col, m @ M((x, y, z + 2.5)))
    mb.add(cyl(0.55, 0.08, 8), 'weath', m @ M((x, y, z + 0.75)))


def bulbs(mb, a, b, n=9, sag=0.8, r=0.16):
    for k in range(n + 1):
        t = k / n; p = a.lerp(b, t); p.z -= sag * 4 * t * (1 - t)
        mb.add(blob(1, (r,) * 3, 70 + k, 0.0), 'sun', Matrix.Translation(p))


# The landmarks: each a different silhouette, front (-Y local) to the bay. Kufu's list (3 Nov) and map B's pins:
# 1 Hook, Line & Sinker, 2 Doyle Boatworks, 3 Plank & Post, 4 The Mooring Line, 5 Pratt's Fish Market (the Voss-owned
# buyer), 6 Swell Surf Co., 7 Post office + Nell's; + Hartley's Outfitters (no pin on map B: placed in town).
def lm_tackle(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); top = house(mb, x, y, yaw, 7.5, 7, 2, 'teal', 'red', z)
    for s in (-1.4, 1.4): mb.add(cyl(0.1, 1.6, 6), 'ink', m @ M((s, 0, top + 0.6)))
    fish_sign(mb, m, (0, 0, top + 2.0), 6.0, 'sun')
    fy = -3.5 - 0.12                                                                     # the shop front: a sign over the door
    mb.add(box(3.4, 0.14, 0.75), 'paper', m @ M((0, fy, z + 2.75))); mb.add(box(3.1, 0.18, 0.55), 'navy', m @ M((0, fy, z + 2.75)))
    fish_sign(mb, m, (0, fy - 0.18, z + 2.75), 1.5, 'sun')
    rx_ = -2.7                                                                           # a rod rack against the wall, left of the door
    for k in (-0.6, 0.6): mb.add(cyl(0.05, 1.3, 6), 'midwood', m @ M((rx_ + k, fy - 0.35, z + 0.65)))
    mb.add(box(1.4, 0.12, 0.1), 'midwood', m @ M((rx_, fy - 0.35, z + 1.2))); mb.add(box(1.4, 0.3, 0.12), 'midwood', m @ M((rx_, fy - 0.35, z + 0.15)))
    for k, col in enumerate(('ink', 'poppy', 'sky', 'ink', 'sun')):
        mb.add(cyl(0.025, 2.9, 5, 0.012), col, m @ M((rx_ - 0.5 + k * 0.25, fy - 0.3, z + 1.55), (D(-6), D(-4 + k * 2), 0)))
    cx_ = 2.7                                                                            # the bait cooler, right of the door
    mb.add(box(1.1, 0.65, 0.6), 'sky', m @ M((cx_, fy - 0.45, z + 0.32))); mb.add(box(1.16, 0.7, 0.1), 'paper', m @ M((cx_, fy - 0.45, z + 0.66)))
    mb.add(box(0.5, 0.06, 0.2), 'paper', m @ M((cx_, fy - 0.79, z + 0.4)))
    return top + 3.2


def lm_boatworks(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); w, d, h = 16, 20, 6.5
    mb.add(box(w, d, h + 4), 'weath', m @ M((0, 0, z + h / 2 - 2)))
    mb.add(gable(d + 1.4, w + 1.4, 5.2), 'red', m @ M((0, 0, z + h), (0, 0, D(90))))     # a pitched roof, gable end to the bay
    mb.add(gable(d + 0.04, w, 5.0), 'weath', m @ M((0, 0, z + h), (0, 0, D(90))))        # (the gable walls under it)
    mb.add(box(w + 0.3, d + 0.3, 0.3), 'paper', m @ M((0, 0, z + h)))                    # the eaves band
    mb.add(box(10.8, 0.25, 6.0), 'paper', m @ M((0, -d / 2 - 0.05, z + 3.0)))             # the big doors: frame, the dark shed, leaves
    # (INTEGRATED-3: the doorway was 'ink', a black slab behind Doyle's counter: now the shed's warm inside, planked)
    mb.add(box(10, 0.3, 5.6), 'dark', m @ M((0, -d / 2 - 0.1, z + 2.8)))
    for k in range(6): mb.add(box(10, 0.04, 0.07), 'midwood', m @ M((0, -d / 2 - 0.27, z + 0.5 + 0.9 * k)))
    for sx in (-1, 1): mb.add(box(2.6, 0.22, 5.4), 'blue', m @ M((sx * 6.6, -d / 2 - 0.12, z + 2.75)))
    mb.add(box(8, 0.35, 1.5), 'blue', m @ M((0, -d / 2 - 0.3, z + h + 1.3)))             # the sign board, a boat painted on it
    mb.add(blob(1, (2.2, 0.12, 0.42), 36, 0.03, 0.6), 'red', m @ M((0, -d / 2 - 0.5, z + h + 1.15)))
    mb.add(box(3.2, 0.12, 0.18), 'paper', m @ M((0, -d / 2 - 0.52, z + h + 1.45)))
    mb.add(cyl(0.07, 1.2, 6), 'paper', m @ M((0.3, -d / 2 - 0.52, z + h + 1.7)))
    rw = random.Random(9)
    for fy in (-6, 0, 6):
        for sx in (-1, 1): window(mb, m, sx * w / 2, fy, sx, 0, z + 3.4, False, rw, None)
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


DOOR5 = [6.8]                          # Pratt's door distance from its centre (past its front steps)


def lm_market(mb, x, y, z, yaw):
    m = M((x, y, 0), (0, 0, yaw)); w, d = 24, 11
    def g(a, b): v = m @ Vector((a, b, 0)); return H(v.x, v.y)
    z0, z = z, max(g(a, b) for a in np.linspace(-w / 2, w / 2, 7) for b in np.linspace(-d / 2, d / 2, 4)) + 0.1   # the floor clears the slope
    ht = z - z0 + 4.4; mb.add(box(w + 1, d + 1, ht), 'stone', m @ M((0, 0, z + 0.35 - ht / 2)))   # a stone plinth down into the hill
    mb.add(box(w + 1.2, d + 1.2, 0.2), 'conc2', m @ M((0, 0, z + 0.35)))
    gf = g(0, -d / 2 - 4); ns = max(0, math.ceil((z + 0.35 - gf) / 0.3))                  # steps down to the street in front
    for i in range(ns):
        top = gf + (i + 1) * (z + 0.35 - gf) / ns; yy = -d / 2 - 0.5 - (ns - 1 - i) * 0.45 - 0.225
        mb.add(box(9, 0.45, top - gf + 1), 'stone' if i % 2 else 'conc2', m @ M((0, yy, (top + gf - 1) / 2)))
    DOOR5[0] = d / 2 + 0.5 + ns * 0.45 + 1.2; log('market floor %.1f (ground %.1f, front %.1f), %d steps, door %.1f' % (z, z0, gf, ns, DOOR5[0]))
    zf, ph, rh = z + 0.45, 4.1, 4.2                                                      # floor top, post height, roof height
    for sx in np.linspace(-w / 2 + 0.5, w / 2 - 0.5, 5):                                 # timber posts on stone footings
        for sy in (-d / 2 + 0.5, d / 2 - 0.5):
            mb.add(box(0.8, 0.8, 0.45), 'stone', m @ M((sx, sy, zf + 0.22))); mb.add(box(0.42, 0.42, ph), 'midwood', m @ M((sx, sy, zf + ph / 2)))
    for sy in (-d / 2 + 0.5, d / 2 - 0.5): mb.add(box(w + 0.4, 0.5, 0.5), 'midwood', m @ M((0, sy, zf + ph - 0.05)))   # the eaves beams
    for sx in np.linspace(-w / 2 + 0.5, w / 2 - 0.5, 5): mb.add(box(0.35, d - 0.6, 0.35), 'midwood', m @ M((sx, 0, zf + ph - 0.1)))
    mb.add(box(w - 0.4, 0.3, ph), 'weath', m @ M((0, d / 2 - 0.4, zf + ph / 2)))          # the back wall, the low side walls
    for sx in (-1, 1): mb.add(box(0.3, d - 1.2, 1.1), 'weath', m @ M((sx * (w / 2 - 0.5), 0, zf + 0.55)))
    for k in range(6):                                                                   # the striped pitched roof, gable ends, eaves boards
        mb.add(gable(w / 6 + 0.02, d + 2.2, rh), 'blue' if k % 2 else 'paper', m @ M((-w / 2 - 0.3 + (w + 0.6) / 12 + k * (w + 0.6) / 6, 0, zf + ph + 0.2)))
    for sx in (-1, 1): mb.add(gable(0.2, d + 1.0, rh - 0.4), 'paper', m @ M((sx * (w / 2 + 0.1), 0, zf + ph + 0.2)))
    for sy in (-1, 1): mb.add(box(w + 0.7, 0.12, 0.4), 'paper', m @ M((0, sy * (d / 2 + 1.1), zf + ph + 0.25)))
    for cy in (-2.2, 1.6):                                                               # two fish counters: ice trays, the catch laid out
        mb.add(box(w * 0.7, 1.4, 0.95), 'midwood', m @ M((0, cy, zf + 0.48))); mb.add(box(w * 0.7 - 0.3, 1.2, 0.14), 'wsky', m @ M((0, cy, zf + 1.0)))
        for k in range(14):
            fx_ = -w * 0.35 + 0.9 + k * (w * 0.7 - 1.8) / 13
            mb.add(blob(1, (0.38, 0.13, 0.1), 80 + k, 0.05, 0.6), ('conc2', 'sky', 'peach', 'iron')[k % 4], m @ M((fx_, cy + 0.25 * ((k % 2) * 2 - 1), zf + 1.12), (0, 0, 0.3 * ((k % 3) - 1))))
    rk = random.Random(5)
    for sx in (-1, 1):                                                                   # fish crates on ice at the ends
        for k in range(3):
            cx_, cy_ = sx * (w / 2 - 2.0), -d / 2 + 1.6 + k * 1.0
            for j in range(rk.randint(1, 3)): mb.add(box(0.85, 0.6, 0.38), 'wood', m @ M((cx_ + rk.uniform(-0.1, 0.1), cy_, zf + 0.2 + j * 0.4)))
            mb.add(box(0.75, 0.5, 0.06), 'wsky', m @ M((cx_, cy_, zf + 0.42 + (j) * 0.4)))
    for sx in (-4.5, 4.5):                                                               # hanging scales over the front counter
        mb.add(cyl(0.02, 1.2, 4), 'ink', m @ M((sx, -2.2, zf + ph - 0.7))); mb.add(cyl(0.2, 0.25, 10), 'paper', m @ M((sx, -2.2, zf + ph - 1.4), (D(90), 0, 0)))
        mb.add(cyl(0.35, 0.05, 10), 'iron', m @ M((sx, -2.2, zf + ph - 1.9)))
    sy_ = -d / 2 - 0.1                                                                   # the sign hangs under the front eave
    for sx in (-2.2, 2.2): mb.add(cyl(0.025, 0.5, 4), 'ink', m @ M((sx, sy_, zf + ph - 0.55)))
    mb.add(box(5.6, 0.16, 1.2), 'paper', m @ M((0, sy_, zf + ph - 1.4))); mb.add(box(5.2, 0.2, 0.9), 'blue', m @ M((0, sy_, zf + ph - 1.4)))
    fish_sign(mb, m, (0, sy_ - 0.2, zf + ph - 1.4), 2.6, 'paper', 'sun')
    return zf + ph + rh


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
SHOP_SPOTS = []                        # (n, name, x, y, ground z, yaw) of each landmark as built (the game's anchors)
LIGHTHOUSE = []                        # (x, y, ground z)


STREETS = []                           # (polyline [(x, y)], width): the quay and the lanes (houses keep off them)
TERRACES = (9, 17, 26, 36)             # the heights of the streets along the hill (houses line them)
TERR = []


def street_d(x, y):
    best = 1e9
    for P, wd in STREETS:
        A = np.array(P); a, b = A[:-1], A[1:]; ab = b - a; L2 = (ab ** 2).sum(1) + 1e-9
        t = np.clip(((np.array([x, y]) - a) * ab).sum(1) / L2, 0, 1); q = a + ab * t[:, None]
        best = min(best, float(np.hypot(*(q - (x, y)).T).min()) - wd / 2)
    return best


def resample(P, step=2.0):
    P = np.asarray(P, float); seg = np.hypot(*np.diff(P, axis=0).T); s_ = np.concatenate([[0], np.cumsum(seg)])
    if s_[-1] < step: return P
    t = np.linspace(0, s_[-1], int(s_[-1] / step) + 1)
    return np.stack([np.interp(t, s_, P[:, 0]), np.interp(t, s_, P[:, 1])], -1)


def ribbon(mb, P, wd, rs, quay=False, earth=False):
    """A cobbled street draped on the ground: 2 rows of setts in 3 tones, a stone kerb each side with a lip down to the
    grass (no gap shows on a cross slope). It is part of the town mesh, so it collides (you walk on it)."""
    # quay: level across, at least 1.2 m up, its water side a stone wall down under the sea. earth: a packed-earth path,
    # no kerb. Returns the resampled line, its normals and the street's height along it (the furniture stands on it).
    P = resample(P, 1.5 if earth else 2.0); n = len(P)
    if n < 2: return P, None, None
    T = np.gradient(P, axis=0); T /= np.hypot(*T.T)[:, None] + 1e-9; N_ = np.stack([-T[:, 1], T[:, 0]], -1)
    lift = 0.05 if earth else 0.07
    def Z(x, y): return max(H(x, y), H(x + 0.5, y), H(x - 0.5, y), H(x, y + 0.5), H(x, y - 0.5)) + lift
    bm = bmesh.new(); cols = []; rows = []; Zr = []; walls = []
    kb = 0.15 if earth else 0.4
    offs = (-wd / 2 - kb, -wd / 2, 0.0, wd / 2, wd / 2 + kb)
    for k in range(n):
        pts = [P[k] + N_[k] * o for o in offs]
        zs = [max(max(Z(*q) for q in pts), 1.2)] * 5 if quay else [Z(*q) for q in pts]
        wet = (0 if SDF(*pts[0]) < SDF(*pts[-1]) else 4) if quay else -1             # (the quay's water side)
        r = [bm.verts.new((q[0], q[1], z + (0.08 if abs(o) > wd / 2 - 0.01 and not earth else 0.0))) for q, z, o in zip(pts, zs, offs)]
        for j in (0, 4):                                                    # the lip down to the grass (the quay wall)
            q = pts[j]; g = H(*q); r.append(bm.verts.new((q[0], q[1], min(g - 0.3, -1.8) if j == wet and g < 1.0 else g - 0.3)))
        rows.append(r); Zr.append(zs[2]); walls.append(wet)
    if earth: top = lambda c: rs.choice(('earth', 'earth2', 'earth'))
    else: top = lambda c: 'stone' if c in (0, 3) else rs.choice(('cob', 'cob2', 'cob3', 'cob'))
    for k in range(n - 1):
        a, b = rows[k], rows[k + 1]
        for c in range(4):
            bm.faces.new((a[c], b[c], b[c + 1], a[c + 1])); cols.append(top(c))
        bm.faces.new((a[5], b[5], b[0], a[0])); cols.append('quay' if walls[k] == 0 else 'earth2' if earth else 'stone2')
        bm.faces.new((a[4], b[4], b[6], a[6])); cols.append('quay' if walls[k] == 4 else 'earth2' if earth else 'stone2')
    it = iter(cols); mb.add(bm, lambda c_, n_: next(it))
    return P, N_, np.array(Zr)


def nearest_pt(x, y, streets):
    best, at = 1e9, None
    for P, _ in streets:
        A = np.asarray(P); d = np.hypot(A[:, 0] - x, A[:, 1] - y); i = int(d.argmin())
        if d[i] < best: best, at = float(d[i]), tuple(A[i])
    return at, best


def before_shop(x, y, depth=28, half=10):
    # in the open ground in front of a shop's door (no house may stand there: the counters face the street)
    for _, _, sx, sy, _, yaw in SHOP_SPOTS:
        dx, dy = x - sx, y - sy; d = dx * math.sin(yaw) - dy * math.cos(yaw); l = dx * math.cos(yaw) + dy * math.sin(yaw)
        if 0 < d < depth and abs(l) < half: return True
    return False


def in_house(x, y, pad=0.4):
    for hx, hy, yaw, hw, hd, _ in HOUSES:
        if abs(x - hx) > 12 or abs(y - hy) > 12: continue
        c, s_ = math.cos(yaw), math.sin(yaw); lx = (x - hx) * c + (y - hy) * s_; ly = -(x - hx) * s_ + (y - hy) * c
        if abs(lx) < hw / 2 + pad and abs(ly) < hd / 2 + pad: return True
    return False


def bollard(mb, x, y, z):
    mb.add(cyl(0.2, 0.75, 8), 'iron', M((x, y, z + 0.37))); mb.add(cyl(0.27, 0.12, 8), 'iron', M((x, y, z + 0.78)))


def bench(mb, x, y, z, yaw):
    m = M((x, y, z), (0, 0, yaw))
    for k in range(3): mb.add(box(1.8, 0.14, 0.06), 'wood', m @ M((0, -0.2 + k * 0.18, 0.48)))
    mb.add(box(1.8, 0.06, 0.4), 'wood', m @ M((0, 0.25, 0.78)))
    for sx in (-0.75, 0.75): mb.add(box(0.08, 0.5, 0.48), 'iron', m @ M((sx, 0, 0.24)))


def crates(mb, x, y, z, yaw, rk):
    m = M((x, y, z), (0, 0, yaw))
    for k in range(rk.randint(2, 4)):
        c = rk.choice(('wood', 'weath', 'midwood')); s_ = rk.uniform(0.6, 0.9)
        mb.add(box(s_, s_, s_), c, m @ M((k * 0.95 - 1, rk.uniform(-0.2, 0.2), s_ / 2), (0, 0, rk.uniform(-0.3, 0.3))))
    if rk.random() < 0.6: mb.add(box(0.7, 0.7, 0.7), 'weath', m @ M((-0.5, 0, 1.0), (0, 0, 0.4)))
    for k in range(rk.randint(1, 3)):                                                   # barrels
        mb.add(cyl(0.34, 0.95, 10), rk.choice(('midwood', 'rust', 'teal')), m @ M((1.6 + k * 0.75, 0.6, 0.48)))


def quay_lamp(mb, x, y, z):
    mb.add(cyl(0.09, 3.8, 6), 'navy', M((x, y, z + 1.9))); mb.add(cyl(0.18, 0.4, 8), 'navy', M((x, y, z + 0.2)))
    mb.add(cyl(0.3, 0.12, 8), 'navy', M((x, y, z + 4.35))); mb.add(cyl(0.22, 0.45, 8, 0.28), 'sun', M((x, y, z + 4.05)))
    if WIN is not None: WIN.add(cyl(0.25, 0.5, 8, 0.31), 'paper', M((x, y, z + 4.05)))     # glows at night


def build_streets(mb, shore, px, py, tx, ty, rr):
    """The quay along the town's waterline and a lane from every shop door to it (or to a lane already built)."""
    def clear_of_shops(x, y): return math.hypot(x - px - 12, y - py - 3) > 24 and all(math.hypot(x - sx, y - sy) > {2: 21}.get(n, 14) for n, _, sx, sy, _, _ in SHOP_SPOTS)
    def along(off, wd, ok, tag):
        q = []
        for p, (nx, ny) in shore:
            if math.hypot((p[0] - tx) * 0.85, p[1] - ty) > 300: q.append(None); continue
            x, y = p[0] + nx * off, p[1] + ny * off
            q.append((x, y) if KIND(x, y) == 1 and clear_of_shops(x, y) and ok(x, y) else None)
        runs, cur = [], []
        for v in q + [None]:
            if v is not None and (not cur or math.dist(cur[-1], v) < 8): cur.append(v); continue
            if len(cur) > 6: runs.append(cur)
            cur = [v] if v is not None else []
        out = []
        for r in runs:
            A = np.array(r); A = np.stack([np.convolve(np.pad(A[:, i], 4, mode='edge'), np.ones(9) / 9, 'valid') for i in (0, 1)], -1)
            out.append((list(map(tuple, resample(A, 2.0))), wd))
        log(tag, len(runs), [len(r) for r in runs]); return out
    quays = along(3.0, 5.0, lambda x, y: H(x, y) > -0.3, 'quay runs')           # its water side right at the waterline
    STREETS.extend(quays)
    STREETS.extend(along(22.5, 4.0, lambda x, y: H(x, y) > 0.8 and slope(x, y) < 0.45 and street_d(x, y) > 6, 'back street runs'))
    def gS(x, y):
        gx = SDF(x + 2, y) - SDF(x - 2, y); gy = SDF(x, y + 2) - SDF(x, y - 2); g = math.hypot(gx, gy) or 1; return gx / g, gy / g
    nl = 0
    for P, _ in quays:                                                                    # lanes up from the quay, every ~70 m
        for i in range(12, len(P) - 6, 35):
            x, y = P[i]; L_ = [(x, y)]
            for _ in range(60):
                gx, gy = gS(x, y); x, y = x + gx * 2, y + gy * 2
                if KIND(x, y) != 1 or slope(x, y) > 0.45 or H(x, y) > 38 or not clear_of_shops(x, y): break
                L_.append((x, y))
            if len(L_) > 12: STREETS.append((L_, 3.2)); nl += 1
    log('cross lanes', nl)
    lm = [(x, y) for _, _, x, y, _, _ in SHOP_SPOTS]
    for hL in TERRACES:                                                                   # streets along the hill's contours
        q = []
        for p, (nx, ny) in shore:
            v = None
            if math.hypot((p[0] - tx) * 0.85, p[1] - ty) < 330:
                for s_ in np.arange(6, 200, 1.5):
                    x, y = p[0] + nx * s_, p[1] + ny * s_
                    if H(x, y) >= hL:
                        if (KIND(x, y) == 1 and slope(x, y) < 0.5 and math.hypot((x - tx) * 0.85, y - ty) < 340
                                and math.hypot(x - px - 12, y - py - 3) > 34 and all(math.dist((x, y), c) > 17 for c in lm)): v = (x, y)
                        break
            q.append(v)
        runs, cur = [], []
        for v in q + [None]:
            if v is not None and (not cur or math.dist(cur[-1], v) < 16):
                if math.dist(cur[-1], v) > 0.8 if cur else True: cur.append(v)
                continue
            if len(cur) > 12: runs.append(cur)
            cur = [v] if v is not None else []
        for r in runs:
            A = np.array(r); A = np.stack([np.convolve(np.pad(A[:, i], 6, mode='edge'), np.ones(13) / 13, 'valid') for i in (0, 1)], -1)
            A = resample(A, 2.0)
            keep = [tuple(a) for a in A if street_d(*a) > 8]
            if len(keep) > 10 and math.dist(keep[0], keep[-1]) > 30: STREETS.append((keep, 4.0)); TERR.append(keep)
        log('terrace', hL, 'runs', [len(r) for r in runs], 'q', sum(v is not None for v in q), 'kept', [len(P) for P in TERR])
    def nearest(x, y):
        best, at = 1e9, None
        for P, _ in STREETS:
            for pt in P:
                d = math.dist(pt, (x, y))
                if d < best: best, at = d, pt
        return at, best
    doors = []
    for n, name, x, y, z, yaw in SHOP_SPOTS:
        dd = {2: 11.0, 3: 5.6, 5: DOOR5[0]}.get(n, 5.2) + 2.0
        doors.append((x + math.sin(yaw) * dd, y - math.cos(yaw) * dd))
    doors.append((px + 12 + 8, py + 3))                                                   # Walt's yard
    for d in sorted(doors, key=lambda d: nearest(*d)[1]):
        at, dist = nearest(*d)
        if at is None or dist < 3 or dist > 260: continue
        STREETS.append(([d, at], 3.6))
    for n, name, x, y, z, yaw in SHOP_SPOTS:                                            # a cobbled forecourt before each door (M4's counter stands on it)
        fx, fy = math.sin(yaw), -math.cos(yaw); d0 = {2: 11.0, 3: 5.6, 5: DOOR5[0]}.get(n, 5.2) - 1.0
        STREETS.append(([(x + fx * d0, y + fy * d0), (x + fx * (d0 + 9), y + fy * (d0 + 9))], 4.9))
    RZ = [ribbon(mb, P, wd, rr, quay=(wd >= 5)) for P, wd in STREETS]
    # the quay's furniture: bollards on its edge, lamps on the land side, benches looking out, crates behind
    k = 0
    for (P, wd), (R_, N_, Zr) in zip(STREETS, RZ):
        if wd < 5 or N_ is None: continue
        for i in range(2, len(R_) - 2):
            nx, ny = N_[i]
            if SDF(*(R_[i] + (nx, ny))) > SDF(*R_[i]): nx, ny = -nx, -ny                 # (n points to the water)
            n2 = np.array((nx, ny)); z = Zr[i] + 0.08; k += 1
            if k % 5 == 0: bollard(mb, *(R_[i] + n2 * (wd / 2 + 0.2)), z)
            if k % 9 == 2: quay_lamp(mb, *(R_[i] - n2 * (wd / 2 + 0.2)), z)
            if k % 17 == 7: bench(mb, *(R_[i] + n2 * (wd / 2 - 0.6)), Zr[i], math.atan2(nx, -ny) + math.pi)
            if k % 23 == 11:
                cx, cy = R_[i] - n2 * (wd / 2 + 1.6)
                crates(mb, cx, cy, H(cx, cy), math.atan2(-nx, ny), rr)
    # by each shop's counter: barrels and crates on one side of the door, a bench and potted plants on the other
    for n, name, x, y, z, yaw in SHOP_SPOTS:
        if n == 1: continue                                                              # (June's has her rod rack and bait cooler)
        fx, fy = math.sin(yaw), -math.cos(yaw); sx_, sy_ = math.cos(yaw), math.sin(yaw)
        dd = {2: 11.0, 3: 5.6, 5: DOOR5[0]}.get(n, 5.2) - 0.2
        for side in (-1, 1):
            cx, cy = x + fx * dd + sx_ * side * 4.4, y + fy * dd + sy_ * side * 4.4             # (all of it >= 2.5 m from the anchor: M4's counter stands there)
            if SDF(cx, cy) < 6 or street_d(cx, cy) < 0.5: continue
            if side < 0: crates(mb, cx, cy, H(cx, cy), yaw + math.pi, rr)                      # (the barrels on the far side)
            else:
                bench(mb, cx, cy, H(cx, cy), yaw + math.pi)
                for k2 in (-1, 1):
                    qx, qy = cx + sx_ * k2 * 1.5, cy + sy_ * k2 * 1.5; g = H(qx, qy)
                    mb.add(cyl(0.32, 0.5, 8, 0.4), 'rust', M((qx, qy, g + 0.25)))
                    mb.add(blob(1, (0.45,) * 3, 50 + n * 3 + k2, 0.2, 1.0), rr.choice(('hedge', 'flower', 'flower2')), M((qx, qy, g + 0.75)))
    # Walt's yard: a packed-earth path from the pier root past the shack, his crates and a net rack behind it
    yard = [(px + 1, py), (px + 8, py + 1.5), (px + 14, py + 2.5), (px + 20, py + 3)]
    ribbon(mb, yard, 3.0, rr, earth=True); STREETS.append((yard, 3.0))
    pad = [(px + 1, py + 5), (px + 16, py + 7)]                                         # the packed-earth yard round the shack
    ribbon(mb, pad, 7.0, rr, earth=True); STREETS.append((pad, 7.0))
    for cx, cy, a in ((px + 9.5, py + 13.5, 0.2), (px - 0.5, py + 10.5, -0.3)): crates(mb, cx, cy, H(cx, cy), a, rr)
    for k2 in range(3):
        cx, cy = px + 12 + k2 * 1.6, py + 9; g = H(cx, cy)
        mb.add(cyl(0.07, 1.9, 6), 'midwood', M((cx, cy, g + 0.95)))
    mb.add(box(3.6, 0.06, 1.3), 'rope', M((px + 13.6, py + 9, H(px + 13.6, py + 9) + 1.2)))


def build_town(coll):
    global WIN; WIN = MB()
    mb = MB(); px, py = S.PIER_ROOT; placed = []; rr = random.Random(17)
    for n, (name, fn) in LANDMARKS.items():
        mx, my = S.SHOPS[n][1] if n in S.SHOPS else PIN8
        x, y = inland(mx - (45 if n == 6 else 0), my, 10)          # Swell Surf: 45 m west, off the Cut's bank
        z = min(H(x + a, y + b) for a in (-6, 6) for b in (-5, 5))
        yaw = D(-90 + rr.uniform(-5, 5)); top = fn(mb, x, y, z, yaw)
        SHOP_SPOTS.append((n, name, x, y, z, yaw))
        placed.append((x, y, 13)); LABELS.append((name, (x, y, top))); log('landmark', n, name, 'at (%.0f, %.0f) z %.1f' % (x, y, z))
    LABELS.append(("Walt's Landing (our dock)", (px - 16, py, 4)))
    # the waterfront: terrace rows of narrow, bright houses along the town shore, lamps, dinghies at their moorings
    tx, ty = S.W(2965, 2560); shore = []
    for curve in (S.smooth(S.TOWN, 60), S.smooth(S.island_pts(), 40)):
        P = np.array(S.Wl(curve))
        for a, b in zip(P, np.roll(P, -1, 0)):
            for t in np.arange(0, 1, 1.5 / max(np.linalg.norm(b - a), 1.5)): shore.append(a + (b - a) * t)
    sh = []                                                                              # the real shore, with its normals
    for p in shore:
        if abs(SDF(p[0], p[1])) > 4: continue
        gx = SDF(p[0] + 3, p[1]) - SDF(p[0] - 3, p[1]); gy = SDF(p[0], p[1] + 3) - SDF(p[0], p[1] - 3); g = math.hypot(gx, gy) or 1
        sh.append((p, (gx / g, gy / g)))
    build_streets(mb, sh, px, py, tx, ty, random.Random(23))
    walls = ['paper', 'wbutter', 'peach', 'wmint', 'cream2', 'wsky', 'wrose', 'paper', 'sun', 'wlav']; roofs = ['red', 'poppy', 'rust', 'blue', 'red', 'teal']
    rows = []; k = 0
    for p in shore:
        if math.hypot((p[0] - tx) * 0.85, p[1] - ty) > 330 or abs(SDF(p[0], p[1])) > 4: continue      # the real waterline only
        gx = SDF(p[0] + 3, p[1]) - SDF(p[0] - 3, p[1]); gy = SDF(p[0], p[1] + 3) - SDF(p[0], p[1] - 3); g = math.hypot(gx, gy) or 1
        nx, ny = gx / g, gy / g; qx, qy = p[0] + nx * 15, p[1] + ny * 15
        if KIND(qx, qy) != 1 or not (9 < SDF(qx, qy) < 24) or not (1.2 < H(qx, qy) < 14): continue
        if math.hypot(qx - px - 12, qy - py - 3) < 36 or any(math.hypot(qx - a, qy - b) < 14 for a, b, _ in placed): continue
        if any(math.hypot(qx - a, qy - b) < 5.6 for a, b in rows) or street_d(qx, qy) < 4.5 or before_shop(qx, qy): continue
        rows.append((qx, qy)); k += 1
        yaw = math.atan2(-ny, -nx) + math.pi / 2
        house(mb, qx, qy, yaw, rr.uniform(4.8, 5.6), 7, rr.choice((2, 2, 2, 3)), walls[k % len(walls)], roofs[(k * 5) % len(roofs)],
              H(qx, qy) - 0.2, hipped=False, chimney=rr.random() < 0.4)
        if k % 3 == 0:                                                                   # a dinghy off the quay
            dx, dy = p[0] - nx * rr.uniform(12, 28), p[1] - ny * rr.uniform(12, 28)
            if H(dx, dy) < -1:
                mb.add(blob(1, (0.95, 2.4, 0.45), 120 + k, 0.03, 0.6), lambda c, n_: 'paper' if c.z > 0.25 else rr.choice(('red', 'teal', 'sun', 'sky')),
                       M((dx, dy, 0.1), (0, 0, rr.uniform(0, 6.3))))
    placed += [(a, b, 6) for a, b in rows]
    log('waterfront rows', len(rows))
    # houses lining both sides of each terrace street, fronts to the street (uphill side most, downhill side some)
    th = []
    for P in TERR:
        R_ = resample(P, 1.0); T = np.gradient(R_, axis=0); acc = rr.uniform(0, 6)
        for i in range(len(R_)):
            acc += 1.0
            if acc < 8.2: continue
            t = T[i] / (np.hypot(*T[i]) + 1e-9); nx, ny = -t[1], t[0]
            if H(*(R_[i] + (nx * 3, ny * 3))) < H(*R_[i]): nx, ny = -nx, -ny             # (n points uphill)
            for side in ((1, -1) if rr.random() < 0.55 else (1,)):
                w, d = rr.uniform(5.2, 7.2), rr.uniform(6, 7.4)
                x, y = R_[i] + np.array((nx, ny)) * side * (2.0 + 0.6 + d / 2)
                fx, fy = -nx * side, -ny * side                                            # its front faces the street
                yaw = math.atan2(fx, -fy)
                cz = [H(x + a * math.cos(yaw) - b * math.sin(yaw), y + a * math.sin(yaw) + b * math.cos(yaw)) for a in (-w / 2, w / 2) for b in (-d / 2, d / 2)]
                if max(cz) - min(cz) > 4.5 or min(cz) < 1.2 or SDF(x, y) < 9: continue
                if any(math.hypot(x - a, y - b) < c + 5 for a, b, c in placed) or any(math.hypot(x - a, y - b) < 7.0 for a, b in th): continue
                if street_d(x, y) < 3.4 or before_shop(x, y): continue
                fz = H(x + fx * (d / 2 + 0.6), y + fy * (d / 2 + 0.6))                        # (the door's floor at the street's level)
                house(mb, x, y, yaw, w, d, rr.choice((2, 2, 3, 1)), rr.choice(walls), rr.choice(roofs), min(min(cz), fz),
                      hipped=rr.random() < 0.2, chimney=rr.random() < 0.5)
                th.append((x, y)); acc = 0
    placed += [(a, b, 6) for a, b in th]
    log('terrace houses', len(th))
    # houses up the hillside: each faces downhill toward the bay; some hipped roofs and chimneys break the rhythm
    def t(x, y, r_):
        r = math.hypot((x - tx) * 0.85, y - ty)
        if r > 380 or r_.random() > 1.15 - r / 380: return None
        z = H(x, y)
        if not (1.2 < z < 48) or SDF(x, y) < 9 or slope(x, y) > 0.42 or KIND(x, y) in (2, 3): return None
        if math.hypot(x - px - 12, y - py - 3) < 34: return None                         # Walt's Landing yard
        if any(math.hypot(x - a, y - b) < c + 7 for a, b, c in placed) or street_d(x, y) < 6.5 or before_shop(x, y): return None
        return 7.0
    hs = scatter(60, (tx - 420, tx + 420, ty - 380, ty + 380), t, 12.0, 13, 120000)
    walls = ['paper', 'cream2', 'wbutter', 'peach', 'paper', 'wrose', 'wmint', 'wsky', 'cream2', 'wlav']; roofs = ['red', 'poppy', 'red', 'rust', 'poppy', 'blue', 'teal']
    for k, (x, y, _) in enumerate(hs):
        gx = H(x + 3, y) - H(x - 3, y); gy = H(x, y + 3) - H(x, y - 3)
        yaw = math.atan2(gy, gx) - math.pi / 2 if math.hypot(gx, gy) > 0.05 else D(-90)
        yaw = D(round(math.degrees(yaw) / 15) * 15 + R.uniform(-5, 5))
        w, d = R.uniform(6.5, 10), R.uniform(6, 8)
        z = min(H(x + a, y + b) for a in (-w / 2, w / 2) for b in (-d / 2, d / 2))
        house(mb, x, y, yaw, w, d, R.choice((1, 2, 2, 3)) if k < 70 else R.choice((1, 2)), R.choice(walls), R.choice(roofs), z,
              hipped=rr.random() < 0.3, chimney=rr.random() < 0.5)
    log('houses', len(hs))
    paths = []
    for hx, hy, yaw, hw, hd, z0 in HOUSES:
        fx, fy = math.sin(yaw), -math.cos(yaw); F = (hx + fx * (hd / 2 + 1.0), hy + fy * (hd / 2 + 1.0))
        at, dist = nearest_pt(*F, STREETS + paths)
        if at is None or dist < 2.0 or dist > 28: continue
        pts = [(F[0] + (at[0] - F[0]) * t, F[1] + (at[1] - F[1]) * t) for t in np.linspace(0, 1, max(2, int(dist)))]
        if any(in_house(*q, 0.3) for q in pts[1:]) or any(math.hypot(q[0] - a, q[1] - b) < 9 for q in pts for _, _, a, b, _, _ in SHOP_SPOTS): continue
        paths.append(([F, at], 1.5))
    for P, wd in paths: ribbon(mb, P, wd, rr, earth=True)
    STREETS.extend(paths); log('door paths', len(paths))
    gm = MB(); rg = random.Random(31); nt = 0
    def clear(x, y, r):
        if street_d(x, y) < r + 0.6 or H(x, y) < 1.2 or SDF(x, y) < 6: return False
        return all(math.hypot(x - hx, y - hy) > max(hw, hd) / 2 + r + 0.8 for hx, hy, _, hw, hd, _ in HOUSES if abs(x - hx) < 20 and abs(y - hy) < 20) and \
               all(math.hypot(x - a, y - b) > c for a, b, c in placed[:len(LANDMARKS)])
    def tree(x, y):
        s_ = rg.uniform(1.6, 2.6); g = H(x, y); h = rg.uniform(2.6, 3.4)
        gm.add(cyl(0.16, h, 6, 0.11), 'midwood', M((x, y, g + h / 2 - 0.3)))
        gm.add(blob(1, (s_, s_ * rg.uniform(0.85, 1), s_ * rg.uniform(0.8, 1)), 300 + int(x * 3 + y) % 500, 0.14, 0.8), rg.choice(('leaf', 'grass2', 'tree3', 'leaf')), M((x, y, g + h + s_ * 0.5)))
        if rg.random() < 0.25:                                                            # a blossom tree
            gm.add(blob(1, (s_ * 0.7,) * 3, 800 + int(x) % 300, 0.18, 1.0), rg.choice(('wrose', 'flower', 'paper')), M((x + 0.6, y, g + h + s_ * 0.9)))
    for hx, hy, yaw, hw, hd, z0 in HOUSES:
        c, s_ = math.cos(yaw), math.sin(yaw)
        def L(a, b): return (hx + a * c - b * s_, hy + a * s_ + b * c)
        if rg.random() < 0.55:                                                            # a hedge along the back garden
            bx, by = L(0, hd / 2 + 2.2)
            if clear(bx, by, hw / 2 - 0.5):
                gm.add(box(hw + 0.6, 0.8, 1.0), 'hedge', M((bx, by, H(bx, by) + 0.3), (0, 0, yaw)))
        for a, b in ((hw / 2 + 2.8, hd / 2 + 1.5), (-hw / 2 - 2.8, -0.5), (hw / 2 + 2.4, -hd / 2 + 1.0)):
            if rg.random() < 0.4:
                x, y = L(a, b)
                if clear(x, y, 1.2): tree(x, y); nt += 1
    for x, y, _ in scatter(140, (tx - 330, tx + 330, ty - 320, ty + 320), lambda x, y, r_: 2.0 if KIND(x, y) == 1 and math.hypot((x - tx) * 0.85, y - ty) < 330 and clear(x, y, 2.0) else None, 9.0, 41, 40000):
        tree(x, y); nt += 1
    FL = ('flower', 'flower2', 'wrose', 'wlav', 'paper', 'sun')
    def clump(x, y, s_, col):
        g = H(x, y); gm.add(blob(1, (s_, s_, s_ * 0.6), 40 + int(x * 5 + y * 3) % 900, 0.2, 1.0), 'hedge', M((x, y, g + s_ * 0.25)))
        if col: gm.add(blob(1, (s_ * 0.55,) * 3, 70 + int(x * 3 + y * 7) % 900, 0.2, 1.0), col, M((x + 0.1, y, g + s_ * 0.75)))
    nb = 0
    for hx, hy, yaw, hw, hd, z0 in HOUSES:                                               # beds either side of the door
        c, s_ = math.cos(yaw), math.sin(yaw); col = rg.choice(FL)
        for a in (-hw / 2 + 0.9, hw / 2 - 0.9):
            x, y = hx + a * c + (hd / 2 + 0.55) * s_, hy + a * s_ - (hd / 2 + 0.55) * c
            if street_d(x, y) < 0.3 or H(x, y) < 0.8 or rg.random() < 0.3: continue
            gm.add(box(1.5, 0.55, 0.35), 'hedge', M((x, y, H(x, y) + 0.12), (0, 0, yaw)))
            for k in (-0.45, 0, 0.45): gm.add(blob(1, (0.26,) * 3, 11 + nb * 3 + int(k * 10), 0.2, 1.0), col, M((x + k * c, y + k * s_, H(x, y) + 0.42)))
            nb += 1
    lawn = scatter(520, (tx - 330, tx + 330, ty - 320, ty + 320), lambda x, y, r_: 2.0 if KIND(x, y) == 1 and math.hypot((x - tx) * 0.85, y - ty) < 330 and clear(x, y, 0.6) else None, 3.2, 43, 60000)
    for x, y, _ in lawn: clump(x, y, rg.uniform(0.45, 0.8), rg.choice(FL) if rg.random() < 0.6 else None)
    finish(gm, 'TownGardens', coll, 60); log('garden trees', nt, 'beds', nb, 'lawn clumps', len(lawn))
    finish(WIN, 'TownWindows', coll, 30, shadow=False); WIN = None
    return finish(mb, 'OldSaltmouth', coll, 30)


def build_maris(coll, land):
    """Mari Duarte's taco boat, moored across the pier from Biscuit (her Day-3+ state: she pays $40/day to moor here)."""
    mb = MB(); L = land.matrix_world @ M((-3.3, -14.5, 0))
    mb.add(hull(3.0, 8.0, 1.3), lambda c, n: 'paper' if c.z > 0.5 else 'poppy', L @ M((0, 0, 0.95)))   # a shaped hull, a white band
    mb.add(box(2.7, 6.6, 0.12), 'weath', L @ M((0, -0.3, 0.95)))                          # the deck
    mb.add(box(2.2, 3.4, 2.0), 'teal', L @ M((0, 0.6, 2.0)))                             # the galley cabin
    mb.add(box(2.6, 3.9, 0.16), 'paper', L @ M((0, 0.6, 3.08)))
    for sx in (-1, 1):                                                                   # serving hatches both sides: shelf, awning
        mb.add(box(0.06, 2.0, 0.95), 'ink', L @ M((sx * 1.11, 0.6, 2.3))); mb.add(box(0.06, 2.2, 0.08), 'paper', L @ M((sx * 1.13, 0.6, 2.82)))
        mb.add(box(0.45, 2.2, 0.08), 'midwood', L @ M((sx * 1.3, 0.6, 1.8)))
        for k in range(4): mb.add(box(1.1, 0.57, 0.06), 'poppy' if k % 2 else 'paper', L @ M((sx * 1.6, 0.6 - 0.86 + k * 0.57, 3.0), (0, D(-20 * sx), 0)))
    mb.add(blob(1, (0.95, 0.3, 0.6), 38, 0.03, 0.8), 'sun', L @ M((0, 0.6, 3.55)))      # a giant taco on the roof
    mb.add(blob(1, (0.8, 0.22, 0.25), 39, 0.2, 1.4), 'leaf', L @ M((0, 0.6, 3.95)))
    for k in (-0.4, 0.1, 0.5): mb.add(blob(1, (0.14,) * 3, 40 + int(k * 10), 0.0), 'poppy', L @ M((k, 0.6, 4.05)))
    mb.add(box(0.08, 0.95, 1.25), 'ink', L @ M((1.0, -2.2, 1.65), (0, D(-8), 0)))       # the chalk menu board on deck
    mb.add(box(0.1, 1.05, 1.35), 'midwood', L @ M((0.97, -2.2, 1.65), (0, D(-8), 0)))
    for k in range(4): mb.add(box(0.02, 0.6 - 0.08 * (k % 2), 0.06), 'paper', L @ M((1.06, -2.2, 2.0 - k * 0.22), (0, D(-8), 0)))
    mb.add(cyl(0.06, 3.4, 6), 'ink', L @ M((0, -2.9, 2.6)))                              # the light mast; two strands to the cabin
    for sx in (-1, 1): bulbs(mb, L @ Vector((0, -2.9, 4.2)), L @ Vector((sx * 1.2, -1.1, 3.1)), 8, 0.25, 0.08)
    bulbs(mb, L @ Vector((-1.2, 2.3, 3.1)), L @ Vector((1.2, 2.3, 3.1)), 5, 0.2, 0.08)
    LABELS.append(("Mari's taco boat (Day 3+)", tuple(L @ Vector((0, 0, 3)))))
    return finish(mb, 'MarisTacoBoat', coll, 30)


def build_lighthouse(coll):
    """Dot's lighthouse: 26 m of red and white bands on the north horn's knoll, so it reads from the bay mouth."""
    mb = MB(); x, y = inland(*S.PLACES['lighthouse'], 16); g = H(x, y); LIGHTHOUSE.append((x, y, g))
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


# ---------------------------------------------------------------- export to Unity (IMPORT-PLAN.md): -- export
# Assets/Game/Art/World/: PB_Island_c<i>_<j>.fbx per 512 m cell (Terrain_LOD0/1/2 re-sampled from the heightfield with
# skirts, + that cell's share of Rocks / Trees / Town), PB_World_Props.fbx (lighthouse, wreck, causeway, Voss, P1-P3
# buoys, Mari's boat, the axis check sockets), PB_World.json (the Landing's spot, the named anchors, tris per cell) and
# PB_Seabed.png (the seabed). Every mesh is in world coordinates; the FBX axes are M1's, so Unity gives the root a 180°
# yaw (Blender (x, y, z) -> Unity (x, z, y), north = +Z). The game builds the LODGroups and colliders from the names.
EXPORT = 'export' in ARGV
WORLD = os.path.join(ART, 'World'); CELL = 512.0


def U(p): return [round(float(p[0]), 3), round(float(p[2]), 3), round(float(p[1]), 3)]     # Blender world -> Unity world
def UF(dx, dy): l = math.hypot(dx, dy) or 1; return [round(dx / l, 4), round(dy / l, 4)]   # a heading -> Unity (x, z)
def ntris(o): return sum(len(p.vertices) - 2 for p in o.data.polygons) if o.type == 'MESH' else 0


def split_cells(ob, tag):
    """One object -> one per 512 m cell, by face centre (a house on a cell edge ends up in two objects: invisible)."""
    me = ob.data; cen = np.zeros(len(me.polygons) * 3, np.float32); me.polygons.foreach_get('center', cen)
    keys = np.floor(cen.reshape(-1, 3)[:, :2] / CELL).astype(int); out = {}
    for key in sorted({tuple(k) for k in keys}):
        bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
        off = np.nonzero(~((keys[:, 0] == key[0]) & (keys[:, 1] == key[1])))[0]
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in off], context='FACES')
        nm = bpy.data.meshes.new('c%d_%d_%s' % (key[0], key[1], tag)); bm.to_mesh(nm); bm.free()
        for m in me.materials: nm.materials.append(m)
        o = bpy.data.objects.new(nm.name, nm); bpy.context.scene.collection.objects.link(o); out[key] = o
    return out


def fbx(path, obs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs: o.select_set(True)
    bpy.context.view_layer.objects.active = obs[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y',
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True, apply_unit_scale=True,
                             mesh_smooth_type='FACE', use_mesh_modifiers=True, add_leaf_bones=False, colors_type='SRGB')


def export_unity(coll, land):
    os.makedirs(WORLD, exist_ok=True)
    old_cells = {f for f in os.listdir(WORLD) if f.startswith('PB_Island_c') and f.endswith('.fbx')}
    split = {t: split_cells(bpy.data.objects[n], t) for n, t in (('Rocks', 'Rocks'), ('Trees', 'Trees'), ('OldSaltmouth', 'Town'), ('TownWindows', 'Windows'), ('TownGardens', 'Garden'))}
    tx, ty = S.W(2965, 2560); cells = []
    for i in range(-6, 5):                                                       # the sheet: X -2840..2160, Y -2450..2550
        for j in range(-5, 5):
            x0, y0 = i * CELL, j * CELL
            near = math.hypot(max(x0 - tx, 0, tx - x0 - CELL), max(y0 - ty, 0, ty - y0 - CELL)) < 420   # town + landing: finer
            obs = []
            for L, n in enumerate((128 if near else 86, 43, 21)):                # LOD0 4 m (town) / 6 m, LOD1 12 m, LOD2 24 m
                step = CELL / n; GX, GY = np.meshgrid(x0 + np.arange(n + 1) * step, y0 + np.arange(n + 1) * step)
                rng = np.random.RandomState(7919 * (i + 50) + 104729 * (j + 50) + L); jit = step * 0.3
                JX, JY = rng.uniform(-jit, jit, GX.shape), rng.uniform(-jit, jit, GX.shape)
                for J_ in (JX, JY): J_[[0, -1], :] = 0; J_[:, [0, -1]] = 0          # edges stay on the grid: seams match
                # the floor only where the water is shallow enough to show it (DepthDist 6: past ~7 m the sea is opaque)
                ob = terrain_mesh('c%d_%d_Terrain_LOD%d' % (i, j, L), GX + JX, GY + JY, coll, floor=-10.0, keep_z=-9.0, skirt=2.0)
                if ob is None: break
                obs.append(ob)
            obs += [s[(i, j)] for s in split.values() if (i, j) in s]
            if not obs: continue
            fn = 'PB_Island_c%d_%d.fbx' % (i, j); fbx(os.path.join(WORLD, fn), obs)
            cells.append(dict(file=fn, i=i, j=j, tris={o.name: ntris(o) for o in obs}))
            log('cell', i, j, {o.name: ntris(o) for o in obs})
    for f in old_cells - {c['file'] for c in cells}:                              # stale cells (keep the live ones' .meta: their GUIDs)
        for g in (f, f + '.meta'):
            if os.path.exists(os.path.join(WORLD, g)): os.remove(os.path.join(WORLD, g))
    props = [bpy.data.objects[n] for n in ('DotsLighthouse', 'WreckOfTheMargaret', 'VossCauseway', 'VossHarbour_blocks', 'Expansion_markers', 'MarisTacoBoat')]
    for nm, p in (('socket_north', (0, 500, 0)), ('socket_east', (500, 0, 0))):  # the axis check (IMPORT-PLAN §1)
        e = bpy.data.objects.new(nm, None); e.location = p; coll.objects.link(e); props.append(e)
    fbx(os.path.join(WORLD, 'PB_World_Props.fbx'), props)
    # the named anchors (M4 places people and counters on them): on the ground in front of each door, fwd = out of the door
    A = []
    def anchor(id_, name, p, f, ground=True): A.append(dict(id=id_, name=name, pos=U(p), fwd=UF(*f), ground=ground))
    ids = {1: 'tackle', 2: 'doyle', 3: 'plank', 4: 'pub', 5: 'pratts', 6: 'surf', 7: 'post', 8: 'hartleys'}
    door = {1: 4.6, 2: 11.0, 3: 5.6, 4: 5.6, 5: DOOR5[0], 6: 3.6, 7: 5.2, 8: 5.2}     # half the building's depth + 1 m (Pratt's: past its steps)
    for n, name, x, y, z, yaw in SHOP_SPOTS:
        fx, fy = math.sin(yaw), -math.cos(yaw)                                   # the front is local -Y
        ax, ay = x + fx * door[n], y + fy * door[n]; anchor(ids[n], name, (ax, ay, H(ax, ay)), (fx, fy))
    Lm = land.matrix_world; R3 = Lm.to_3x3()
    def lp(x, y, z=0.0): return tuple(Lm @ Vector((x, y, z)))
    def ld(x, y): d = R3 @ Vector((x, y, 0)); return (d.x, d.y)
    anchor('office', "Walt's shack (the office)", lp(-5.38, 11.2), ld(0, -1))
    anchor('mari_mooring', "Mari's mooring (the pier beside her boat)", lp(-1.1, -14.5, 1.3), ld(-1, 0))
    anchor('mari_boat', "Mari's taco boat", lp(-3.3, -14.5, 0.0), ld(0, -1), ground=False)
    lx, ly, lg = LIGHTHOUSE[0]; anchor('lighthouse', "Dot's lighthouse (harbour master)", (lx + 8, ly - 10, lg), (0, -1))
    cx, cy = S.W(2641, 3452); anchor('cut', "Walt's Cut (the Voss Causeway)", (cx, cy, 2.0), (1, 0))
    for id_, key in (('bay_shallows', 'Bay Shallows'), ('gullrock', 'Gullrock'), ('lantern_key', 'Lantern Key'), ('wreck', 'Wreck of the Margaret'),
                     ('breaker_point', 'Breaker Point surf'), ('sorrel', 'Sorrel Isle'), ('mango_cay', 'Mango Cay'), ('voss', 'Voss Harbour (round the south horn)')):
        x, y = S.W(*S.TARGETS[key][0]); anchor(id_, key, (x, y, 0.0), (1, 0), ground=False)
    for k, (x0, y0, w, h) in S.EXPANSION.items():
        x, y = S.W(x0 + w / 2, y0 + h / 2); anchor(k.lower(), 'Expansion site ' + k, (x, y, 0.0), (1, 0), ground=False)
    px, py = S.PIER_ROOT
    j = dict(cell=CELL, landing=dict(pos=U((px - 10, py, 0)), yaw=-90.0),      # Blender -90° about Z = Unity yaw -90°
             seabed=dict(x0=S.XW, z0=S.YN - S.SIZE, size=S.SIZE, hmin=-160.0, hmax=240.0, px=S.N),
             anchors=A, cells=cells, props={o.name: ntris(o) for o in props})
    import json; json.dump(j, open(os.path.join(WORLD, 'PB_World.json'), 'w'), indent=1)
    shutil.copy2(os.path.join(SEABED, 'seabed_2048.png'), os.path.join(WORLD, 'PB_Seabed.png'))
    tot = sum(sum(c['tris'].values()) for c in cells)
    log('EXPORTED', len(cells), 'cells,', tot, 'tris in all LODs;', len(A), 'anchors ->', WORLD)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    coll = bpy.data.collections.new('CrescentBay'); bpy.context.scene.collection.children.link(coll)
    if not EXPORT:
        terrain('Island', -1150 - S.HORN, 1700, -1300, 1300, 6.0, coll, 1)
        for k, (mx, my, rx, ry) in S.ISLETS.items():
            x, y = S.W(mx, my); terrain('Islet_' + k, x - rx - 120, x + rx + 120, y - ry - 120, y + ry + 120, 4.0, coll, 2)
    rocks = build_rocks(coll); build_trees(coll); build_town(coll); build_lighthouse(coll); build_wreck(coll); build_cut(coll)
    build_voss(coll); build_expansion(coll)
    if EXPORT:                      # (no foam stand-ins or clouds: the game's water shader and sky do those)
        land, boat, moor = place_landing(); build_maris(coll, land); export_unity(coll, land); return
    build_foam(coll, rocks)
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
    # street level, as the game's -pb-mapshots sees it (Unity (x, y, z) = Blender (x, z, y) here)
    def eye(x, y): return (x, y, max(H(x, y), 1.3) + 1.7)
    shoot(cam, 'st_quay.png', (60, 62, 6), (20, 0, 2), 30, near=0.3)
    shoot(cam, 'st_yard.png', (36, 12, 3.2), (-10, 0, 1), 30, near=0.3)
    lane = next((P for P, wd in STREETS if abs(wd - 3.2) < 0.01 and len(P) > 16), None)
    if lane: log('st_lane eye', eye(*lane[4]), 'look', lane[15], H(*lane[15]) + 1.5)
    if lane: shoot(cam, 'st_lane.png', eye(*lane[4]), (lane[15][0], lane[15][1], H(*lane[15]) + 1.5), 30, near=0.3)
    Lm = land.matrix_world                                                               # Mari's boat from the pier, and close on its hatch
    shoot(cam, 'st_mari.png', tuple(Lm @ Vector((2.5, -21.5, 2.9))), tuple(Lm @ Vector((-3.3, -14.0, 2.0))), 30, near=0.3)
    shoot(cam, 'st_mari2.png', tuple(Lm @ Vector((-0.2, -11.0, 2.4))), tuple(Lm @ Vector((-3.3, -14.8, 2.2))), 30, near=0.3)
    door = {1: 4.6, 5: DOOR5[0]}
    for n, tag in ((5, 'st_pratts'), (1, 'st_june')):                                   # a clear spot in front of the door
        _, _, x, y, z, yaw = next(s_ for s_ in SHOP_SPOTS if s_[0] == n); fx, fy = math.sin(yaw), -math.cos(yaw); rx, ry = math.cos(yaw), math.sin(yaw)
        ax, ay = x + fx * door[n], y + fy * door[n]
        dg = bpy.context.evaluated_depsgraph_get(); sc = bpy.context.scene; tgt = Vector((x, y, H(ax, ay) + {1: 2.6, 5: 4.8}[n]))
        for dd, lat in ((20, 4), (20, -4), (16, 5), (16, -5), (24, 0), (14, 0), (11, 3), (11, -3), (9, 0)):
            ex, ey = ax + fx * dd + rx * lat, ay + fy * dd + ry * lat; e = Vector((ex, ey, max(H(ex, ey), z - 0.5, 1.3) + 1.7)); dv = tgt - e
            up = sc.ray_cast(dg, e, Vector((0, 0, 1)), distance=40)[0]                     # under a roof or an awning
            hit, hp, _, _, ho = sc.ray_cast(dg, e, dv.normalized(), distance=dv.length)[:5]
            if SDF(ex, ey) > 1 and not up and (not hit or (hp - tgt).length < 6): break
        shoot(cam, tag + '.png', tuple(e), tuple(tgt), 30, near=0.3)
    os.makedirs(PROOF, exist_ok=True)
    for f in sorted(os.listdir(REN)):
        if f.endswith('.png') and (not TAG or f.endswith('_' + TAG + '.png')): shutil.copy2(os.path.join(REN, f), os.path.join(PROOF, f))
    if not TAG: shutil.copy2(os.path.join(SEABED, 'seabed_2048_preview.png'), os.path.join(PROOF, 'seabed_2048_preview.png'))
    log('copied to', PROOF)


main()
