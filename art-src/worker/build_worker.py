# Port Boss dock worker: our own stylised, stocky character, generated from code in Blender 5.1.
#   blender --background --python build_worker.py -- <outdir> concept <A|B|C>   concept renders of one body type
#   blender --background --python build_worker.py -- <outdir> build [A|B|C]     the game model: Worker.blend + Worker.fbx + previews
# One base body + modular parts on one Mixamo-named rig. Every part is a separate skinned mesh; the game shows the parts
# the player picked. One material: each face's UVs sit in the middle of a cell of a 4x4 palette texture, so the game
# recolours a whole character by rewriting 16 pixels (see Worker.cs). Faces -Y, Z up, 1 unit = 1 m, ~1.8 m tall.
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix, Euler
from mathutils.bvhtree import BVHTree

ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
OUT = os.path.abspath(ARGV[0] if ARGV else '.')
MODE = ARGV[1] if len(ARGV) > 1 else 'build'
TYPE = ARGV[2] if len(ARGV) > 2 else 'C'
os.makedirs(OUT, exist_ok=True)

# Palette cells (index = cell in the 4x4 texture, row-major from the bottom-left). Worker.cs uses the same numbers.
SLOTS = ['skin', 'skin_dark', 'hair', 'eye', 'top', 'top_trim', 'accent', 'reflect',
         'legs', 'legs_trim', 'boots', 'sole', 'gloves', 'hat', 'hat_trim', 'metal']
PREVIEW = {'skin': '#d9a37e', 'skin_dark': '#b27a5a', 'hair': '#4a3020', 'eye': '#1d1712', 'top': '#3f6d9e', 'top_trim': '#33577f',
           'accent': '#f2c230', 'reflect': '#d9dde0', 'legs': '#3c4654', 'legs_trim': '#2c333d', 'boots': '#7a4a2a', 'sole': '#2a2422',
           'gloves': '#c9a35a', 'hat': '#e8b020', 'hat_trim': '#c48d14', 'metal': '#a8adb2'}

# ---------------------------------------------------------------- body types (concepts) ----------------------------------------
# A: bulldog (barrel chest + belly, short legs), B: strongman (V-taper, huge arms), C: balanced stocky (the pick, see README).
TYPES = {
    'A': dict(chest=(0.25, 0.17, 0.18), belly=(0.215, 0.19, 0.17), belly_y=-0.035, arm=1.0, fore=1.05, leg=0.95, delt=0.105, neck=0.09, hip_w=0.115),
    'B': dict(chest=(0.265, 0.16, 0.18), belly=(0.175, 0.145, 0.14), belly_y=-0.0, arm=1.15, fore=1.15, leg=1.0, delt=0.115, neck=0.092, hip_w=0.11),
    'C': dict(chest=(0.255, 0.165, 0.18), belly=(0.2, 0.17, 0.155), belly_y=-0.02, arm=1.08, fore=1.12, leg=1.0, delt=0.11, neck=0.09, hip_w=0.112),
}
P = TYPES[TYPE]

# Joint positions (left side, +X; the right side mirrors). The rig and the cuts below use these.
SH = Vector((0.25, 0.0, 1.38))      # shoulder joint
EL = Vector((0.52, 0.01, 1.38))     # elbow
WR = Vector((0.76, 0.0, 1.38))      # wrist
HIP = Vector((P['hip_w'], 0.0, 0.88))
KN = Vector((0.125, -0.01, 0.48))
AN = Vector((0.13, 0.02, 0.1))
COLLAR, WAIST, BOOT_TOP = 1.435, 1.0, 0.25
X_SLEEVE, X_ROLL, X_ARMPIT = 0.40, 0.575, 0.215
NECK_X, STRAP_X, BIB_X, BACK_BIB_X = 0.1, 0.14, 0.14, 0.17      # neckline sides, overall straps 0.1..0.14, bib widths
V_Z, BACK_NECK_Z = 1.33, 1.40                                    # vest neckline front / back
UPPER = lambda c: c.z > WAIST - 0.07 and abs(c.x) < X_ARMPIT + 0.02   # the torso, not the arms or legs: limits torso-only cuts


def Z(z, where=None): return ((0, 0, z), (0, 0, 1)) + ((where,) if where else ())
def X(x, where=None): return [((s * x, 0, 0), (1, 0, 0)) + ((where,) if where else ()) for s in (1, -1)]
KNEES = [Z(0.42), Z(0.52), ((0, 0, 0), (0, 1, 0), lambda c: 0.4 < c.z < 0.54)]


# ---------------------------------------------------------------- geometry helpers -------------------------------------------
class Geo:
    """Closed primitive shells collected into one mesh; voxel remesh then unions them."""
    def __init__(self): self.v, self.f = [], []

    def add_bm(self, bm, M):
        bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
        o = len(self.v)
        self.v += [tuple(v.co) for v in bm.verts]
        self.f += [[o + v.index for v in f.verts] for f in bm.faces]
        bm.free()

    def ellip(self, c, r, rot=(0, 0, 0), mirror=False, seg=28):
        for s in ([1, -1] if mirror else [1]):
            bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=max(6, seg // 2 + 2), radius=1.0)
            rr = Euler((rot[0], rot[1] * s, rot[2] * s)).to_matrix().to_4x4()
            self.add_bm(bm, Matrix.Translation((c[0] * s, c[1], c[2])) @ rr @ Matrix.Diagonal((r[0], r[1], r[2], 1)))
        return self

    def tube(self, a, b, r1, r2, mirror=False, flat=1.0):
        """Tapered capsule from a (radius r1) to b (r2); flat squashes it across (y) for flatter limbs."""
        for s in ([1, -1] if mirror else [1]):
            A, B = Vector((a[0] * s, a[1], a[2])), Vector((b[0] * s, b[1], b[2]))
            d = B - A
            q = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix().to_4x4()
            bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=24, radius1=r1, radius2=r2, depth=d.length)
            self.add_bm(bm, Matrix.Translation((A + B) / 2) @ q @ Matrix.Diagonal((1, flat, 1, 1)))
            self.ellip(A, (r1, r1 * flat, r1)); self.ellip(B, (r2, r2 * flat, r2))
        return self

    def box(self, c, half, mirror=False):
        for s in ([1, -1] if mirror else [1]):
            bm = bmesh.new(); bmesh.ops.create_cube(bm, size=2.0)
            self.add_bm(bm, Matrix.Translation((c[0] * s, c[1], c[2])) @ Matrix.Diagonal((half[0], half[1], half[2], 1)))
        return self

    def obj(self, name):
        me = bpy.data.meshes.new(name); me.from_pydata(self.v, [], self.f); me.update()
        ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
        return ob


def activate(ob):
    for o in bpy.context.scene.objects: o.select_set(False)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)


def bm_of(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data); return bm


def bm_put(bm, ob):
    bm.normal_update(); bm.to_mesh(ob.data); ob.data.update(); bm.free()


def smooth(ob, iters, factor=0.5):
    bm = bm_of(ob)
    for _ in range(iters):
        bmesh.ops.smooth_laplacian_vert(bm, verts=bm.verts, lambda_factor=factor, lambda_border=0.0,
                                        use_x=True, use_y=True, use_z=True, preserve_volume=True)
    bm_put(bm, ob)


def solidify(ob, voxel, faces, pre_smooth=6, post_smooth=2):
    """Union the shells (voxel remesh), soften the creases where they meet, then rebuild clean quads (Quadriflow)."""
    activate(ob)
    ob.data.remesh_voxel_size = voxel; ob.data.remesh_voxel_adaptivity = 0.0
    bpy.ops.object.voxel_remesh()
    smooth(ob, pre_smooth, 0.6)
    if faces:
        # The voxel mesh has zero-area slivers that make QuadriFlow refuse it; dissolve them first.
        bm = bm_of(ob); bmesh.ops.dissolve_degenerate(bm, dist=0.0005, edges=bm.edges); bm_put(bm, ob)
        for attempt in range(3):
            try:
                r = bpy.ops.object.quadriflow_remesh(mode='FACES', target_faces=faces, use_mesh_symmetry=True, use_preserve_sharp=False,
                                                     use_preserve_boundary=False, smooth_normals=False, seed=1 + attempt)
            except RuntimeError: r = {'CANCELLED'}
            if 'FINISHED' in r: break
            md = ob.modifiers.new('d', 'DECIMATE'); md.ratio = 0.5; bpy.ops.object.modifier_apply(modifier='d')
        else:
            print('WARN quadriflow gave up on', ob.name, '- decimating instead')
            md = ob.modifiers.new('d', 'DECIMATE'); md.ratio = min(1.0, faces * 2.0 / max(1, tris(ob))); bpy.ops.object.modifier_apply(modifier='d')
        smooth(ob, post_smooth, 0.4)
    for p in ob.data.polygons: p.use_smooth = True
    return ob


def cut(ob, planes):
    """Bisect along planes (co, normal[, where]) so seams between parts run along clean edge loops; where(c) limits a cut
    to the faces it matters for (a cut through the whole body costs a ring of extra triangles on every limb it crosses)."""
    bm = bm_of(ob); cut_bm(bm, planes); bm_put(bm, ob)


def cut_bm(bm, planes):
    for co, no, *where in planes:
        fs = [f for f in bm.faces if not where or where[0](f.calc_center_median())]
        geom = list({v for f in fs for v in f.verts}) + list({e for f in fs for e in f.edges}) + fs
        bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-5, plane_co=co, plane_no=no)


def subset(src, name, keep, offset=0.0, rim=0.0, slot='skin', paint=None, cuts=()):
    """New mesh from the faces of src whose centre passes keep(c, n); pushed out along the normals by offset;
    open borders folded back towards the body by rim (gives cloth an edge thickness); paint(c, n) -> slot per face."""
    bm = bm_of(src); bm.normal_update()
    nrm = {v: v.normal.copy() for v in bm.verts}
    if cuts:
        cut_bm(bm, cuts); bm.normal_update()
        for v in bm.verts:
            if v not in nrm: nrm[v] = v.normal.copy()
    gone = [f for f in bm.faces if not keep(f.calc_center_median(), f.normal)]
    bmesh.ops.delete(bm, geom=gone, context='FACES')
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
    for v in bm.verts: v.co += nrm[v] * offset
    bm.normal_update()
    if rim > 0: fold_rim(bm, rim, nrm)
    me = bpy.data.meshes.new(name); ob = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(ob)
    bm_put(bm, ob)
    set_slots(ob, slot, paint)
    return ob


def fold_rim(bm, depth, body_nrm=None):
    """Turn the open edges in towards the body (along the body's own normals when known: the edge normals of a cut
    shell lean sideways, and a hem folded along them comes out ragged)."""
    edges = [e for e in bm.edges if e.is_boundary]
    if not edges: return
    nrm = {v: (body_nrm[v] if body_nrm and v in body_nrm else v.normal).copy() for e in edges for v in e.verts}
    res = bmesh.ops.extrude_edge_only(bm, edges=edges)
    for g in res['geom']:
        if isinstance(g, bmesh.types.BMVert):
            old = next((e.other_vert(g) for e in g.link_edges if e.other_vert(g) in nrm), None)
            if old is not None: g.co = old.co - nrm[old] * depth
    bm.normal_update()


def mat(slot):
    m = bpy.data.materials.get('pal_' + slot)
    if m: return m
    m = bpy.data.materials.new('pal_' + slot)
    c = PREVIEW[slot]; rgb = [int(c[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    m.diffuse_color = (*[x ** 2.2 for x in rgb], 1)
    nt = m.node_tree; nt.nodes.clear()
    # Toon preview: two light bands like the pack's ramp (the game uses the pack's own toon shader).
    d = nt.nodes.new('ShaderNodeBsdfDiffuse'); s2r = nt.nodes.new('ShaderNodeShaderToRGB')
    ramp = nt.nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].position = 0.0; ramp.color_ramp.elements[0].color = (0.5, 0.52, 0.62, 1)
    ramp.color_ramp.elements[1].position = 0.22; ramp.color_ramp.elements[1].color = (0.8, 0.8, 0.84, 1)
    e = ramp.color_ramp.elements.new(0.5); e.color = (1, 1, 1, 1)
    mix = nt.nodes.new('ShaderNodeMix'); mix.data_type = 'RGBA'; mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1
    em = nt.nodes.new('ShaderNodeEmission'); out = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(d.outputs[0], s2r.inputs[0]); nt.links.new(s2r.outputs[0], ramp.inputs[0])
    mix.inputs[6].default_value = (*[x ** 2.2 for x in rgb], 1)
    nt.links.new(ramp.outputs[0], mix.inputs[7]); nt.links.new(mix.outputs[2], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
    return m


def set_slots(ob, slot, paint=None):
    ob.data.materials.clear()
    used = [slot]
    if paint:
        for p in ob.data.polygons:
            s = paint(Vector(p.center), Vector(p.normal)) or slot
            if s not in used: used.append(s)
    for s in used: ob.data.materials.append(mat(s))
    for p in ob.data.polygons:
        s = (paint(Vector(p.center), Vector(p.normal)) if paint else None) or slot
        p.material_index = used.index(s)
        p.use_smooth = True


def tris(ob): return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def mirror_x(ob, name):
    me = ob.data.copy(); o = bpy.data.objects.new(name, me); bpy.context.scene.collection.objects.link(o)
    bm = bm_of(o)
    for v in bm.verts: v.co.x = -v.co.x
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    bm_put(bm, o)
    return o


# ---------------------------------------------------------------- body -------------------------------------------------------
def build_body():
    g = Geo()
    ch, be = P['chest'], P['belly']
    g.ellip((0, 0.0, 1.24), ch)                                            # chest / ribcage
    g.ellip((0, P['belly_y'], 1.05), be)                                   # belly
    g.ellip((0, 0.01, 0.92), (0.2, 0.145, 0.13))                           # pelvis
    g.ellip((0.085, 0.075, 0.87), (0.095, 0.075, 0.095), mirror=True)      # glutes
    g.ellip((0.1, -0.115, 1.29), (0.115, 0.06, 0.085), mirror=True)        # pecs
    g.ellip((0.12, 0.03, 1.405), (0.11, 0.08, 0.06), (0, 0.35, 0), mirror=True)   # traps
    g.ellip((0.12, 0.09, 1.26), (0.12, 0.07, 0.14), mirror=True)           # lats / upper back
    g.tube((0, 0.01, 1.36), (0, 0.0, 1.53), P['neck'], P['neck'] * 0.9)     # neck (thick)
    a, f = P['arm'], P['fore']
    g.ellip(SH + Vector((0.01, 0, 0.005)), (P['delt'], P['delt'] * 0.92, P['delt'] * 0.95), mirror=True)  # deltoid
    g.tube(SH, EL, 0.082 * a, 0.066 * a, mirror=True, flat=0.92)           # upper arm
    g.ellip(((SH.x + EL.x) / 2 + 0.01, -0.015, 1.385), (0.1, 0.07 * a, 0.072 * a), mirror=True)  # biceps
    g.ellip(((SH.x + EL.x) / 2, 0.03, 1.375), (0.1, 0.06 * a, 0.065 * a), mirror=True)          # triceps
    g.tube(EL, WR, 0.07 * f, 0.05 * f, mirror=True, flat=0.85)             # forearm
    g.ellip((EL.x + 0.075, -0.005, 1.383), (0.1, 0.066 * f, 0.074 * f), mirror=True)  # forearm bulge (Popeye)
    l = P['leg']
    g.tube(HIP, KN, 0.115 * l, 0.08 * l, mirror=True)                      # thigh
    g.ellip((0.125, -0.02, 0.7), (0.095 * l, 0.1 * l, 0.15), mirror=True)  # quads
    g.tube(KN, AN, 0.075 * l, 0.055 * l, mirror=True)                      # shin
    g.ellip((0.128, 0.03, 0.33), (0.078 * l, 0.08 * l, 0.11), mirror=True) # calf
    body = g.obj('Body_Base')
    solidify(body, 0.006, 1900, pre_smooth=24, post_smooth=6)
    # Seams the skin pieces share with the clothes; each garment adds its own colour cuts (subset(cuts=...)).
    planes = [((0, 0, COLLAR), (0, 0, 1), UPPER)] + [((s * NECK_X, 0, 0), (1, 0, 0), UPPER) for s in (1, -1)]
    planes += [((s * x, 0, 0), (1, 0, 0)) for x in (X_SLEEVE, X_ROLL) for s in (1, -1)]
    planes += [((0, 0, z), (0, 0, 1)) for z in (WAIST - 0.035, WAIST + 0.04, BOOT_TOP - 0.06)]
    print('BODY quads', len(body.data.polygons), 'tris', tris(body)); cut(body, planes); print('BODY after cuts tris', tris(body))
    return body


# ---------------------------------------------------------------- head + face ---------------------------------------------------
HEAD_C = Vector((0, 0.0, 1.645))
FACES = {
    # 0 "Bruno": broad square jaw, big nose, heavy brow
    0: dict(jaw=(0.112, 0.1, 0.088), jaw_z=1.555, corners=0.052, nose=(0.03, 0.03, 0.04), nose_y=-0.118, cheek=0.044, brow=(0.095, 0.034, 0.026), chin=(0.05, 0.04, 0.035)),
    # 1 "Sunny": round cheeks, button nose
    1: dict(jaw=(0.105, 0.098, 0.09), jaw_z=1.56, corners=0.0, nose=(0.026, 0.028, 0.028), nose_y=-0.116, cheek=0.056, brow=(0.085, 0.028, 0.02), chin=(0.04, 0.035, 0.03)),
    # 2 "Hawk": long face, strong chin, long nose
    2: dict(jaw=(0.098, 0.095, 0.1), jaw_z=1.545, corners=0.04, nose=(0.024, 0.034, 0.046), nose_y=-0.12, cheek=0.036, brow=(0.09, 0.03, 0.022), chin=(0.045, 0.045, 0.045)),
    # 3 "Old Salt": heavy jowls, bulbous nose, bushy brow
    3: dict(jaw=(0.115, 0.1, 0.085), jaw_z=1.55, corners=0.058, nose=(0.035, 0.03, 0.035), nose_y=-0.118, cheek=0.05, brow=(0.1, 0.036, 0.028), chin=(0.045, 0.035, 0.03)),
}
CRANIUM = ((0, 0.012, 1.665), (0.118, 0.132, 0.128))


def build_head(i):
    F = FACES[i]
    g = Geo()
    g.ellip(*CRANIUM)
    g.ellip((0, -0.03, F['jaw_z']), F['jaw'])
    if F['corners']: g.ellip((0.068, -0.02, 1.525), (F['corners'],) * 3, mirror=True)
    g.ellip((0, -0.085, 1.505), F['chin'])
    g.ellip((0.062, -0.075, 1.575), (F['cheek'],) * 3, mirror=True)
    g.ellip((0, F['nose_y'], 1.6), F['nose'])
    g.ellip((0, -0.098, 1.665), F['brow'])
    g.ellip((0.117, 0.012, 1.615), (0.022, 0.034, 0.044), (0, 0, -0.25), mirror=True)   # ears
    g.tube((0, 0.01, 1.44), (0, 0.005, 1.56), 0.078, 0.078)              # neck stub (sits inside the body's neck)
    head = solidify(g.obj('Head_%d' % i), 0.0028, 650, pre_smooth=14, post_smooth=4)
    set_slots(head, 'skin')
    return head


def surf(ob, x, z, y0=-0.5):
    """Point on the front surface of ob at (x, z), and its normal."""
    bvh = BVHTree.FromObject(ob, bpy.context.evaluated_depsgraph_get())
    hit, n, _, _ = bvh.ray_cast(Vector((x, y0, z)), Vector((0, 1, 0)))
    return (hit, n) if hit else (Vector((x, -0.12, z)), Vector((0, -1, 0)))


def build_face_bits(head, i):
    """Eyes, brows and mouth for head i, sat on its surface."""
    F = FACES[i]
    whites, pupils = Geo(), Geo()
    for s in (1, -1):
        p, n = surf(head, s * 0.042, 1.63)
        whites.ellip(p - n * 0.002, (0.019, 0.01, 0.024), seg=10)
        pupils.ellip(p + n * 0.006 + Vector((s * -0.002, 0, -0.002)), (0.0105, 0.006, 0.0145), seg=8)
    eo = whites.obj('Eyes_%d' % i); set_slots(eo, 'reflect')
    po = pupils.obj('Pupils_%d' % i); set_slots(po, 'eye')
    activate(eo); po.select_set(True); bpy.ops.object.join()
    brows = Geo()
    bz, inner = 1.668, {0: 0.0, 1: 0.006, 2: 0.004, 3: -0.002}[i]
    for s in (1, -1):
        a, na = surf(head, s * 0.02, bz + inner); b, nb = surf(head, s * 0.068, bz - 0.002)
        brows.tube(a + na * 0.005, b + nb * 0.004, 0.0105 + 0.003 * (i in (0, 3)), 0.008 + 0.004 * (i == 3), flat=0.7)
    bo = brows.obj('Brows_%d' % i); solidify(bo, 0.0018, 160, 3, 1); set_slots(bo, 'hair')
    mouth = Geo()
    m0, mn = surf(head, 0.0, 1.528); ml, _ = surf(head, 0.028, 1.534)
    mouth.tube(Vector((-ml.x, ml.y, ml.z)) + mn * 0.002, m0 + mn * 0.004, 0.006, 0.0065).tube(m0 + mn * 0.004, ml + mn * 0.002, 0.0065, 0.006)
    mo = mouth.obj('Mouth_%d' % i); solidify(mo, 0.0015, 40, 2, 1); set_slots(mo, 'skin_dark')
    return eo, bo, mo


# ---------------------------------------------------------------- hair, beards, hats -------------------------------------------
def hairline(c):
    """True above the hairline: high at the forehead, down over the temples, low at the nape."""
    front = math.cos(math.atan2(c.x, -(c.y - 0.01)))   # 1 = straight ahead, -1 = the back of the head
    z = 1.635 + 0.075 * front
    if abs(c.x) > 0.095 and -0.04 < c.y < 0.06: z = max(z, 1.665)   # clear of the ears
    return c.z > z


def build_hair(kind):
    g = Geo()
    (cx, cy, cz), (rx, ry, rz) = CRANIUM
    g.ellip((cx, cy, cz), (rx + 0.012, ry + 0.012, rz + 0.012))
    if kind == 'quiff':
        g.ellip((0, -0.085, 1.765), (0.075, 0.055, 0.04), (0.35, 0, 0)).ellip((0.02, -0.03, 1.79), (0.08, 0.07, 0.035))
    if kind == 'curly':
        for i in range(70):
            u, v = (i * 0.618034) % 1.0, (i + 0.5) / 70
            th, ph = 2 * math.pi * u, math.acos(1 - v * 1.15)
            d = Vector((math.sin(ph) * math.cos(th), math.sin(ph) * math.sin(th), math.cos(ph)))
            p = Vector((cx + d.x * (rx + 0.016), cy + d.y * (ry + 0.016), cz + d.z * (rz + 0.016)))
            if hairline(p): g.ellip(p, (0.026,) * 3)
    ob = solidify(g.obj('Hair_' + kind), 0.0035, {'buzz': 420, 'quiff': 520, 'curly': 700}[kind], 4, 1)
    bm = bm_of(ob)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if not hairline(f.calc_center_median()) or f.normal.z < -0.5], context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.normal_update(); fold_rim(bm, 0.012 if kind != 'curly' else 0.02)
    bm_put(bm, ob); set_slots(ob, 'hair')
    return ob


def build_beard(kind, head, face):
    g = Geo()
    if kind in ('full', 'goatee', 'moustache'):
        p, n = surf(head, 0.0, 1.565)
        g.tube(p + n * 0.006 + Vector((-0.004, 0, 0.004)), p + n * 0.006 + Vector((-0.045, 0.012, -0.02)), 0.017, 0.012)
        g.tube(p + n * 0.006 + Vector((0.004, 0, 0.004)), p + n * 0.006 + Vector((0.045, 0.012, -0.02)), 0.017, 0.012)
    if kind == 'full':
        for x, z, r in [(0, 1.49, 0.06), (0.05, 1.5, 0.05), (0.085, 1.535, 0.045), (0.1, 1.58, 0.035), (0.03, 1.5, 0.055)]:
            for s in ((1, -1) if x else (1,)):
                pp, nn = surf(head, s * x, z)
                g.ellip(pp + nn * (r * 0.35), (r, r * 0.8, r))
    if kind == 'goatee':
        pp, nn = surf(head, 0, 1.495); g.ellip(pp + nn * 0.015, (0.035, 0.028, 0.045))
    ob = solidify(g.obj('Beard_%s_%d' % (kind, face)), 0.0025, {'full': 500, 'goatee': 240, 'moustache': 160}[kind], 4, 1)
    set_slots(ob, 'hair')
    return ob


def build_hat(kind):
    g = Geo()
    (cx, cy, cz), (rx, ry, rz) = CRANIUM
    if kind == 'cap':
        g.ellip((cx, cy + 0.005, cz + 0.012), (rx + 0.014, ry + 0.014, rz + 0.004))
        g.ellip((0, -0.16, 1.705), (0.1, 0.085, 0.013), (-0.12, 0, 0))                # brim
        g.ellip((0, 0.01, 1.8), (0.014, 0.014, 0.009))                                 # button
    elif kind == 'beanie':
        g.ellip((cx, cy, cz + 0.02), (rx + 0.016, ry + 0.016, rz + 0.012))
        g.tube((0, cy, 1.69), (0, cy, 1.725), rx + 0.024, rx + 0.022)                  # folded cuff
    elif kind == 'hardhat':
        g.ellip((cx, cy, cz + 0.022), (rx + 0.022, ry + 0.022, rz + 0.016))
        g.ellip((0, cy - 0.012, 1.7), (rx + 0.055, ry + 0.07, 0.012))                 # rim, wider at the front
        g.tube((0, -0.12, 1.75), (0, 0.13, 1.76), 0.016, 0.016)                      # ridge front..back
        g.tube((0, -0.06, 1.8), (0, 0.08, 1.805), 0.02, 0.02)
    ob = solidify(g.obj('Hat_' + kind), 0.003, {'cap': 450, 'beanie': 420, 'hardhat': 500}[kind], 3, 1)
    bm = bm_of(ob)
    lo = {'cap': 1.685, 'beanie': 1.675, 'hardhat': 1.68}[kind]
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().z < lo and f.normal.z < -0.3 and abs(f.calc_center_median().y + 0.16) > 0.09], context='FACES')
    bm_put(bm, ob)
    if kind == 'cap':
        cut(ob, [((0, -0.12, 0), (0, 1, 0)), ((0, 0, 1.72), (0, 0, 1)), ((0, 0, 1.795), (0, 0, 1))])
        paint = lambda c, n: 'hat_trim' if c.y < -0.12 and c.z < 1.72 or c.z > 1.795 else None
    elif kind == 'beanie':
        cut(ob, [((0, 0, 1.735), (0, 0, 1))]); paint = lambda c, n: 'hat_trim' if c.z < 1.735 else None
    else:
        cut(ob, [((0, 0, 1.715), (0, 0, 1))]); paint = lambda c, n: 'hat_trim' if c.z < 1.715 else None
    set_slots(ob, 'hat', paint)
    return ob


# ---------------------------------------------------------------- hands, gloves, boots ------------------------------------------
FINGERS = [(-0.034, 0.072, 0.013), (-0.0115, 0.078, 0.0135), (0.0115, 0.073, 0.013), (0.033, 0.06, 0.012)]  # y, length, radius
KNUCK_X = 0.865


def build_hand():
    g = Geo()
    g.tube((0.735, 0, 1.38), (0.79, 0, 1.38), 0.046, 0.047, flat=0.8)                  # wrist (inside the forearm end)
    g.ellip((0.83, 0.0, 1.379), (0.055, 0.052, 0.026))                                 # palm
    g.box((0.835, 0.0, 1.379), (0.035, 0.042, 0.021))
    for y, L, r in FINGERS:
        a = Vector((KNUCK_X - 0.01, y, 1.381)); b = Vector((KNUCK_X + L * 0.55, y * 1.05, 1.376)); c = Vector((KNUCK_X + L, y * 1.08, 1.366))
        g.tube(a, b, r, r * 0.95).tube(b, c, r * 0.95, r * 0.85)
    g.tube((0.79, -0.035, 1.373), (0.83, -0.075, 1.366), 0.018, 0.016).tube((0.83, -0.075, 1.366), (0.86, -0.095, 1.362), 0.016, 0.0135)  # thumb
    hand = solidify(g.obj('Hand_L'), 0.0018, 360, 3, 1); set_slots(hand, 'skin')
    return hand


def build_boot():
    g = Geo()
    x = AN.x
    g.tube((x, 0.01, 0.06), (x, 0.015, BOOT_TOP), 0.074, 0.077, flat=1.05)            # shaft
    g.ellip((x, -0.05, 0.07), (0.07, 0.12, 0.065))                                     # foot
    g.ellip((x, -0.14, 0.058), (0.068, 0.07, 0.056))                                   # toe cap
    g.ellip((x, 0.045, 0.065), (0.064, 0.06, 0.06))                                    # heel
    g.box((x, -0.045, 0.018), (0.074, 0.15, 0.018))                                    # sole
    g.ellip((x, -0.04, 0.018), (0.076, 0.17, 0.02))
    ob = solidify(g.obj('Boot_L'), 0.0035, 380, 4, 1)
    cut(ob, [((0, 0, 0.036), (0, 0, 1)), ((0, 0, BOOT_TOP - 0.025), (0, 0, 1))])
    set_slots(ob, 'boots', lambda c, n: 'sole' if c.z < 0.036 else 'legs_trim' if c.z > BOOT_TOP - 0.025 else None)
    return ob


# ---------------------------------------------------------------- outfits ---------------------------------------------------------
def build_outfits(body):
    parts = {}
    zl = WAIST - 0.035
    neck = lambda c: c.z > COLLAR and abs(c.x) < NECK_X
    torso = lambda c: c.z > zl and abs(c.x) < X_SLEEVE and not neck(c)
    collar = lambda c: abs(c.x) < 0.125 and c.z > COLLAR - 0.025
    # Skin pieces that show around the clothes.
    parts['Skin_Neck'] = subset(body, 'Skin_Neck', lambda c, n: neck(c))
    parts['Skin_Arm_Upper'] = subset(body, 'Skin_Arm_Upper', lambda c, n: X_SLEEVE < abs(c.x) < X_ROLL)
    parts['Skin_Arm_Lower'] = subset(body, 'Skin_Arm_Lower', lambda c, n: abs(c.x) > X_ROLL)
    # T-shirt: torso + short sleeves, ribbed crew collar and sleeve hems.
    trim_cuts = [Z(COLLAR - 0.025, UPPER)] + X(0.125, UPPER)
    parts['Top_Tee'] = subset(body, 'Top_Tee', lambda c, n: torso(c), 0.004, 0.012, 'top',
                              lambda c, n: 'top_trim' if collar(c) or abs(c.x) > X_SLEEVE - 0.03 else None, trim_cuts + X(X_SLEEVE - 0.03))
    # Work shirt, sleeves rolled to the elbow, button placket.
    parts['Top_Shirt'] = subset(body, 'Top_Shirt', lambda c, n: torso(c) or (c.z > zl and abs(c.x) < X_ROLL), 0.008, 0.014, 'top',
                                lambda c, n: 'top_trim' if (abs(c.x) < 0.012 and c.y < 0 and WAIST + 0.04 < c.z < COLLAR - 0.025) else None,
                                [Z(COLLAR - 0.025, UPPER)] + X(0.012, UPPER))
    roll = Geo()
    for s in (1, -1): roll.tube((s * (X_ROLL - 0.055), 0.005, 1.381), (s * (X_ROLL - 0.005), 0.005, 1.381), 0.083 * P['fore'], 0.08 * P['fore'])
    roll.tube((0, 0.012, COLLAR - 0.02), (0, 0.008, COLLAR + 0.025), P['neck'] + 0.022, P['neck'] + 0.012)
    ro = solidify(roll.obj('Top_Shirt_Roll'), 0.003, 320, 3, 1)
    bm = bm_of(ro); bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.normal.z > 0.85 and abs(f.calc_center_median().x) < 0.15 or f.normal.z < -0.85 and abs(f.calc_center_median().x) < 0.15], context='FACES'); bm_put(bm, ro)
    set_slots(ro, 'top_trim'); parts['Top_Shirt_Roll'] = ro
    # Hi-vis vest over the tee: V neck, open arm holes, two reflective bands, zip.
    def vest(c):
        if c.z < zl or abs(c.x) > X_ARMPIT: return False
        return c.z < (V_Z if c.y < 0 else BACK_NECK_Z) if abs(c.x) < NECK_X else True
    parts['Vest'] = subset(body, 'Vest', lambda c, n: vest(c), 0.024, 0.02, 'accent',
                           lambda c, n: 'reflect' if (1.06 < c.z < 1.1 or 1.2 < c.z < 1.24) else 'metal' if abs(c.x) < 0.012 and c.y < 0 else None,
                           [Z(z, UPPER) for z in (V_Z, BACK_NECK_Z, 1.06, 1.1, 1.2, 1.24)] + X(X_ARMPIT, UPPER) + X(0.012, UPPER) + [((0, 0, 0), (0, 1, 0), UPPER)])
    # Trousers: belt, knee pads, cargo pockets; the cuffs fall over the boots.
    legs = lambda c: BOOT_TOP - 0.06 < c.z < WAIST + 0.04
    parts['Legs_Trousers'] = subset(body, 'Legs_Trousers', lambda c, n: legs(c), 0.008, 0.012, 'legs',
                                    lambda c, n: 'sole' if c.z > WAIST else 'legs_trim' if (c.y < 0 and 0.42 < c.z < 0.52) or (abs(c.x) > BACK_BIB_X and 0.6 < c.z < 0.72) else None,
                                    [Z(WAIST), Z(0.6), Z(0.72)] + X(BACK_BIB_X, lambda c: 0.58 < c.z < 0.74) + KNEES)
    # Overalls (dungarees): baggy legs + bib front and back + straps over the shoulders.
    def bib(c):
        if c.z <= WAIST + 0.04: return legs(c)
        if abs(c.x) >= X_ARMPIT or neck(c): return False
        strap = NECK_X < abs(c.x) < STRAP_X
        return strap or (abs(c.x) < BIB_X and c.z < 1.27) if c.y < 0 else strap or (abs(c.x) < BACK_BIB_X and c.z < 1.12)
    parts['Legs_Overalls'] = subset(body, 'Legs_Overalls', lambda c, n: bib(c), 0.03, 0.018, 'accent',
                                    lambda c, n: 'legs_trim' if (c.y < 0 and abs(c.x) < 0.07 and 1.14 < c.z < 1.2) or (c.y < 0 and 0.42 < c.z < 0.52) else None,
                                    [Z(z, UPPER) for z in (1.27, 1.12, 1.14, 1.2)] + X(X_ARMPIT, UPPER) + X(STRAP_X, UPPER) + X(BACK_BIB_X, UPPER)
                                    + X(0.07, UPPER) + [((0, 0, 0), (0, 1, 0), UPPER)] + KNEES)
    buck = Geo()
    for s in (1, -1):
        p = Vector((s * (NECK_X + STRAP_X) / 2, 0, 1.275)); bvh = BVHTree.FromObject(parts['Legs_Overalls'], bpy.context.evaluated_depsgraph_get())
        hit, nn, _, _ = bvh.ray_cast(Vector((p.x, -0.5, p.z)), Vector((0, 1, 0)))
        if hit: buck.box(hit + nn * 0.004, (0.022, 0.006, 0.016))
    bo = buck.obj('Legs_Overalls_Buckles'); set_slots(bo, 'metal'); parts['Legs_Overalls_Buckles'] = bo
    # Belt buckle for the trousers.
    bvh = BVHTree.FromObject(parts['Legs_Trousers'], bpy.context.evaluated_depsgraph_get())
    hit, nn, _, _ = bvh.ray_cast(Vector((0, -0.5, WAIST + 0.02)), Vector((0, 1, 0)))
    bk = Geo().box(hit + nn * 0.004, (0.026, 0.006, 0.019)).obj('Legs_Trousers_Buckle'); set_slots(bk, 'metal'); parts['Legs_Trousers_Buckle'] = bk
    return parts


# ---------------------------------------------------------------- rig -------------------------------------------------------------
def build_rig():
    """Mixamo bone names and hierarchy, T-pose, so Unity's Humanoid maps it at once and Mixamo clips retarget cleanly."""
    arm = bpy.data.armatures.new('WorkerRig'); rig = bpy.data.objects.new('Worker', arm)
    bpy.context.scene.collection.objects.link(rig); activate(rig)
    bpy.ops.object.mode_set(mode='EDIT')
    E = arm.edit_bones

    def bone(name, head, tail, parent=None, roll=0.0):
        b = E.new(name); b.head = Vector(head); b.tail = Vector(tail); b.roll = roll
        if parent: b.parent = E[parent]; b.use_connect = False
        return b
    bone('Hips', (0, 0.01, 0.93), (0, 0.01, 1.03))
    bone('Spine', (0, 0.01, 1.03), (0, 0.01, 1.13), 'Hips')
    bone('Spine1', (0, 0.01, 1.13), (0, 0.01, 1.25), 'Spine')
    bone('Spine2', (0, 0.01, 1.25), (0, 0.01, 1.4), 'Spine1')
    bone('Neck', (0, 0.01, 1.43), (0, 0.005, 1.53), 'Spine2')
    bone('Head', (0, 0.005, 1.53), (0, 0.0, 1.72), 'Neck')
    bone('HeadTop_End', (0, 0.0, 1.72), (0, 0.0, 1.8), 'Head')
    for side, s in (('Left', 1), ('Right', -1)):
        def m(v): return (v[0] * s, v[1], v[2])
        bone(side + 'Shoulder', m((0.04, 0.01, 1.4)), m((SH.x - 0.02, 0.0, SH.z)), 'Spine2')
        bone(side + 'Arm', m(SH), m(EL), side + 'Shoulder')
        bone(side + 'ForeArm', m(EL), m(WR), side + 'Arm')
        bone(side + 'Hand', m(WR), m((KNUCK_X, 0.0, 1.38)), side + 'ForeArm')
        names = ['Index', 'Middle', 'Ring', 'Pinky']
        for (y, L, r), n in zip(FINGERS, names):
            k = [Vector((KNUCK_X - 0.005, y, 1.38)), Vector((KNUCK_X + L * 0.38, y * 1.04, 1.378)), Vector((KNUCK_X + L * 0.7, y * 1.07, 1.372)), Vector((KNUCK_X + L, y * 1.08, 1.366))]
            par = side + 'Hand'
            for j in range(3):
                bone(f'{side}Hand{n}{j + 1}', m(k[j]), m(k[j + 1]), par); par = f'{side}Hand{n}{j + 1}'
            bone(f'{side}Hand{n}4', m(k[3]), m(k[3] + Vector((0.012, 0, 0))), par)
        tk = [Vector((0.8, -0.035, 1.373)), Vector((0.83, -0.075, 1.366)), Vector((0.848, -0.088, 1.364)), Vector((0.865, -0.098, 1.362))]
        par = side + 'Hand'
        for j in range(3):
            bone(f'{side}HandThumb{j + 1}', m(tk[j]), m(tk[j + 1]), par); par = f'{side}HandThumb{j + 1}'
        bone(f'{side}HandThumb4', m(tk[3]), m(tk[3] + Vector((0.01, -0.006, 0))), par)
        bone(side + 'UpLeg', m(HIP), m(KN), 'Hips')
        bone(side + 'Leg', m(KN), m(AN), side + 'UpLeg')
        bone(side + 'Foot', m(AN), m((AN.x, -0.11, 0.025)), side + 'Leg')
        bone(side + 'ToeBase', m((AN.x, -0.11, 0.025)), m((AN.x, -0.19, 0.025)), side + 'Foot')
        bone(side + 'Toe_End', m((AN.x, -0.19, 0.025)), m((AN.x, -0.22, 0.025)), side + 'ToeBase')
    # Roll every bone so its local Z points the same way (helps Mixamo/Unity agree on twist axes).
    for axis, vertical in (('GLOBAL_NEG_Y', True), ('GLOBAL_POS_Z', False)):
        for b in E: b.select = b.select_head = b.select_tail = (abs((b.tail - b.head).normalized().z) > 0.7) == vertical
        bpy.ops.armature.calculate_roll(type=axis)
    bpy.ops.object.mode_set(mode='OBJECT')
    for b in arm.bones:
        if b.name.endswith(('_End', '4')) and ('Hand' in b.name or 'Toe' in b.name or 'HeadTop' in b.name): b.use_deform = False
    return rig


def skin_auto(rig, ob):
    activate(rig); ob.select_set(True); bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')


def transfer(src, ob, rig):
    """Copy skin weights from the body (nearest face, interpolated) so clothes bend exactly like the skin under them."""
    for vg in src.vertex_groups:
        if vg.name not in ob.vertex_groups: ob.vertex_groups.new(name=vg.name)
    md = ob.modifiers.new('wt', 'DATA_TRANSFER'); md.object = src
    md.use_vert_data = True; md.data_types_verts = {'VGROUP_WEIGHTS'}; md.vert_mapping = 'POLYINTERP_NEAREST'
    md.layers_vgroup_select_src = 'ALL'; md.layers_vgroup_select_dst = 'NAME'
    activate(ob); bpy.ops.object.modifier_apply(modifier=md.name)
    bind(ob, rig)


def bind(ob, rig):
    ob.parent = rig
    if not any(m.type == 'ARMATURE' for m in ob.modifiers):
        md = ob.modifiers.new('Armature', 'ARMATURE'); md.object = rig


def rigid(ob, rig, bone_name):
    vg = ob.vertex_groups.new(name=bone_name); vg.add(range(len(ob.data.vertices)), 1.0, 'REPLACE'); bind(ob, rig)


def seg_dist(p, a, b):
    ab = b - a; t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared)); return (p - (a + ab * t)).length


def by_bones(ob, rig, names, power=6.0):
    """Weights from the distance to each bone segment (for hands and boots, which the body's weights don't reach)."""
    bones = [(n, rig.data.bones[n].head_local, rig.data.bones[n].tail_local) for n in names]
    groups = {n: ob.vertex_groups.new(name=n) for n in names}
    for v in ob.data.vertices:
        ds = [(n, seg_dist(v.co, a, b)) for n, a, b in bones]
        ws = [(n, 1.0 / max(d, 0.004) ** power) for n, d in ds]
        tot = sum(w for _, w in ws)
        for n, w in ws:
            if w / tot > 0.01: groups[n].add([v.index], w / tot, 'REPLACE')
    bind(ob, rig)


# ---------------------------------------------------------------- palette UVs + export ------------------------------------------
def palette_uvs(ob):
    """Collapse each face's UVs to the centre of its slot's palette cell, then merge to the single Worker material."""
    me = ob.data
    if not me.uv_layers: me.uv_layers.new(name='UV')
    uv = me.uv_layers.active.data
    for p in me.polygons:
        slot = me.materials[p.material_index].name[4:]
        i = SLOTS.index(slot)
        u, v = (i % 4 + 0.5) / 4, (i // 4 + 0.5) / 4
        for li in p.loop_indices: uv[li].uv = (u, v)


def to_single_material(objs, img):
    m = bpy.data.materials.get('Worker') or bpy.data.materials.new('Worker')
    nt = m.node_tree; nt.nodes.clear()
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = img; tx.interpolation = 'Closest'
    bs = nt.nodes.new('ShaderNodeBsdfPrincipled'); out = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(tx.outputs[0], bs.inputs['Base Color']); nt.links.new(bs.outputs[0], out.inputs[0]); bs.inputs['Roughness'].default_value = 1
    for ob in objs:
        if not ob.data.materials: print('WARN no material on', ob.name); set_slots(ob, 'skin')
        palette_uvs(ob)
        ob.data.materials.clear(); ob.data.materials.append(m)
        for p in ob.data.polygons: p.material_index = 0


def palette_image(path):
    img = bpy.data.images.new('WorkerPalette', 4, 4)
    px = []
    for i in range(16):
        c = PREVIEW[SLOTS[i]]; px += [int(c[j:j + 2], 16) / 255 for j in (1, 3, 5)] + [1]
    img.pixels = px; img.filepath_raw = path; img.file_format = 'PNG'; img.save()
    return img


# ---------------------------------------------------------------- render ----------------------------------------------------------
def setup_render(w=1200, h=1200):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = w; sc.render.resolution_y = h
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    wd = bpy.data.worlds.new('w'); sc.world = wd
    bg = wd.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.42, 0.55, 0.66, 1); bg.inputs[1].default_value = 0.35
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 4; sun.data.angle = 0.1
    sun.rotation_euler = (math.radians(45), 0, math.radians(-35)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.lens = 85
    return cam


def shoot(cam, path, yaw_deg, dist=6.2, z=1.0, look_z=0.92, target_x=0.0):
    a = math.radians(yaw_deg)
    cam.location = (target_x + math.sin(a) * dist, -math.cos(a) * dist, z)
    d = Vector((target_x, 0, look_z)) - cam.location
    cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)


def pose_rest(rig, carry=False):
    """A relaxed pose for renders (arms down) or a carry pose (forearms forward under a crate)."""
    activate(rig); bpy.ops.object.mode_set(mode='POSE')
    for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0, 0, 0)
    for side, s in (('Left', 1), ('Right', -1)):
        rig.pose.bones[side + 'Arm'].rotation_mode = 'QUATERNION'
        b = rig.data.bones[side + 'Arm']
        down = Vector((0.18 * s, -0.05 if not carry else -0.2, -1)).normalized()
        q = (b.matrix_local.to_3x3().inverted() @ (b.tail_local - b.head_local).normalized()).rotation_difference(b.matrix_local.to_3x3().inverted() @ down)
        rig.pose.bones[side + 'Arm'].rotation_quaternion = q
    bpy.ops.object.mode_set(mode='OBJECT')


# ---------------------------------------------------------------- assembly --------------------------------------------------------
def build_all(full=True):
    body = build_body()
    rig = build_rig()
    skin_auto(rig, body)
    parts = build_outfits(body)
    for o in parts.values(): transfer(body, o, rig)
    heads = [build_head(i) for i in (range(4) if full else [0])]
    face_bits = []
    for i, h in enumerate(heads):
        bits = build_face_bits(h, i)
        face_bits += bits
        for o in (h,) + bits:
            if o is h: by_bones(o, rig, ['Head', 'Neck'], 10)
            else: rigid(o, rig, 'Head')
    hairs = [build_hair(k) for k in (('buzz', 'quiff', 'curly') if full else ('buzz',))]
    beards = [build_beard(k, h, i) for i, h in enumerate(heads) for k in (('moustache', 'full', 'goatee') if full else ('full',))]
    hats = [build_hat(k) for k in (('cap', 'beanie', 'hardhat') if full else ('hardhat',))]
    for o in hairs + beards + hats: rigid(o, rig, 'Head')
    hand_l = build_hand(); hand_r = mirror_x(hand_l, 'Hand_R')
    glove_l = subset(hand_l, 'Gloves_L', lambda c, n: True, 0.005, 0.0, 'gloves')
    cuff = Geo().tube((0.735, 0, 1.38), (0.785, 0, 1.38), 0.06, 0.058, flat=0.85).obj('Gloves_Cuff_L'); solidify(cuff, 0.0025, 180, 2, 1); set_slots(cuff, 'gloves')
    gl = join([glove_l, cuff], 'Gloves_L'); glove_r = mirror_x(gl, 'Gloves_R')
    for o, s in ((hand_l, 'Left'), (hand_r, 'Right'), (gl, 'Left'), (glove_r, 'Right')):
        names = [s + 'ForeArm', s + 'Hand'] + [f'{s}Hand{f}{j}' for f in ('Index', 'Middle', 'Ring', 'Pinky', 'Thumb') for j in (1, 2, 3)]
        by_bones(o, rig, names)
    hands = join([hand_l, hand_r], 'Skin_Hands'); gloves = join([gl, glove_r], 'Gloves')
    boot_l = build_boot(); boot_r = mirror_x(boot_l, 'Boot_R')
    by_bones(boot_l, rig, ['LeftLeg', 'LeftFoot', 'LeftToeBase'], 8); by_bones(boot_r, rig, ['RightLeg', 'RightFoot', 'RightToeBase'], 8)
    boots = join([boot_l, boot_r], 'Boots')
    bpy.data.objects.remove(body)
    objs = list(parts.values()) + heads + face_bits + hairs + beards + hats + [hands, gloves, boots]
    for o in objs: bind(o, rig)
    return rig, {o.name: o for o in objs}


def join(objs, name):
    activate(objs[0])
    for o in objs[1:]: o.select_set(True)
    bpy.ops.object.join(); objs[0].name = name; objs[0].data.name = name
    return objs[0]


OUTFIT = {'head': 'Head_0', 'face': 0, 'hair': None, 'beard': 'Beard_full', 'hat': 'Hat_hardhat', 'top': 'tee+vest', 'legs': 'Legs_Trousers', 'gloves': True}


def show(objs, o):
    vis = {'Skin_Neck', 'Skin_Arm_Lower', 'Boots', o['head'], 'Eyes_%d' % o['face'], 'Brows_%d' % o['face'], 'Mouth_%d' % o['face'], o['legs']}
    vis |= {o['legs'] + '_Buckle', o['legs'] + '_Buckles'}
    if o['top'] in ('tee', 'tee+vest'): vis |= {'Top_Tee', 'Skin_Arm_Upper'}
    if o['top'] == 'tee+vest': vis.add('Vest')
    if o['top'] == 'shirt': vis |= {'Top_Shirt', 'Top_Shirt_Roll'}
    if o['legs'] == 'Legs_Overalls' and o['top'] == 'tee+vest': vis.discard('Vest')
    for k in ('hair', 'beard', 'hat'):
        if o[k]: vis.add(o[k] + ('_%d' % o['face'] if k == 'beard' else ''))
    if o['hat'] and o['hair']: vis.discard(o['hair'])
    vis.add('Gloves' if o['gloves'] else 'Skin_Hands')
    n = 0
    for name, ob in objs.items():
        ob.hide_render = name not in vis; ob.hide_viewport = ob.hide_render
        if not ob.hide_render: n += tris(ob)
    return n


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if MODE == 'concept':
        rig, objs = build_all(full=False)
        print('TRIS', show(objs, OUTFIT))
        for n, o in objs.items():
            if not o.hide_render: print('  PART', n, tris(o))
        pose_rest(rig)
        cam = setup_render(900, 1200)
        for name, yaw in (('front', 0), ('side', 90), ('34', 35)):
            shoot(cam, os.path.join(OUT, f'concept-{TYPE}-{name}.png'), yaw)
        if len(ARGV) > 3:   # close-ups for checking the details
            shoot(cam, os.path.join(OUT, f'close-{TYPE}-face.png'), 20, dist=1.5, z=1.65, look_z=1.62)
            shoot(cam, os.path.join(OUT, f'close-{TYPE}-torso.png'), 25, dist=2.8, z=1.3, look_z=1.15)
            shoot(cam, os.path.join(OUT, f'close-{TYPE}-legs.png'), 35, dist=2.8, z=0.5, look_z=0.35)
        return
    rig, objs = build_all(full=True)
    report = []
    for n, o in sorted(objs.items()): report.append(f'{n}: {tris(o)} tris')
    open(os.path.join(OUT, 'parts.txt'), 'w').write('\n'.join(report) + '\n')
    print('\n'.join(report))
    cam = setup_render(900, 1200)
    looks = [
        dict(OUTFIT),
        dict(OUTFIT, head='Head_1', face=1, hair='Hair_quiff', beard=None, hat=None, top='tee', legs='Legs_Overalls', gloves=False),
        dict(OUTFIT, head='Head_2', face=2, hair='Hair_buzz', beard='Beard_goatee', hat='Hat_beanie', top='shirt', gloves=True),
        dict(OUTFIT, head='Head_3', face=3, hair='Hair_curly', beard='Beard_moustache', hat='Hat_cap', top='tee+vest', legs='Legs_Overalls', gloves=False),
    ]
    pose_rest(rig)
    for i, lk in enumerate(looks):
        t = show(objs, lk); print('LOOK', i, 'tris', t)
        shoot(cam, os.path.join(OUT, f'look-{i}.png'), 30)
    lk = dict(looks[3], hat=None)
    print('LOOK curly tris', show(objs, lk)); shoot(cam, os.path.join(OUT, 'look-3-nohat.png'), 25, dist=2.2, z=1.62, look_z=1.6)
    print('LOOK max tris', show(objs, dict(OUTFIT, legs='Legs_Overalls', hair='Hair_curly', hat=None)))
    # Back to T-pose for export.
    activate(rig); bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.select_all(action='SELECT'); bpy.ops.pose.transforms_clear(); bpy.ops.object.mode_set(mode='OBJECT')
    for ob in objs.values(): ob.hide_render = False; ob.hide_viewport = False
    img = palette_image(os.path.join(OUT, 'WorkerPalette.png'))
    to_single_material(list(objs.values()), img)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'Worker.blend'))
    activate(rig)
    for o in objs.values(): o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, 'Worker.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, axis_forward='-Z', axis_up='Y',
                             add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, mesh_smooth_type='FACE',
                             path_mode='STRIP', armature_nodetype='NULL', primary_bone_axis='Y', secondary_bone_axis='X')
    print('EXPORTED', os.path.join(OUT, 'Worker.fbx'))


main()
