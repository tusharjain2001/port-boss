# Port Boss character kit: the shared tools (docs/plan/replan/chars/editor-kit.md §2b). Every crew member is modelled in
# "Pip space": on Pip's joints (X Bot scaled so the Head joint is at 82% of 1.47 m), then the finished mesh is scaled
# uniformly to their own height (the rig is built at that height, so the joints still sit exactly on the spec).
# Colour = the face-corner byte colour "Col" (sRGB) from a per-character palette, one material "PB_Toon". Faces -Y, Z up.
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out')
os.makedirs(OUT, exist_ok=True)

PIP_H = 1.47              # Pip space: every body is modelled at this height
HEAD_AT = 0.82            # Head joint height / full height (editor-kit §2b)
PAL = {}                  # the palette of the character being built (set by build_kit.py)
COMMON = dict(white='#FFFBF2', ink='#3F2C27', metal='#B4B8BD', mouth='#6E2A2A', tongue='#D9645A',
              player='#E8752A')  # slot 0 orange (the HUD's): armband + back patch, never on the outfit


def srgb(h): h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4))
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def lerp(a, b, t): return a + (b - a) * t
def sstep(a, b, x): t = clamp((x - a) / (b - a)); return t * t * (3 - 2 * t)


def spline(keys, t):
    """Catmull-Rom through (t, value-tuple) keys."""
    if t <= keys[0][0]: return keys[0][1]
    if t >= keys[-1][0]: return keys[-1][1]
    i = max(k for k in range(len(keys) - 1) if keys[k][0] <= t)
    t0, t1 = keys[i][0], keys[i + 1][0]; u = (t - t0) / (t1 - t0)
    p0 = keys[max(i - 1, 0)][1]; p1 = keys[i][1]; p2 = keys[i + 1][1]; p3 = keys[min(i + 2, len(keys) - 1)][1]
    return tuple(0.5 * (2 * b + (-a + c) * u + (2 * a - 5 * b + 4 * c - d) * u * u + (-a + 3 * b - 3 * c + d) * u ** 3)
                 for a, b, c, d in zip(p0, p1, p2, p3))


# ---------------------------------------------------------------- skeleton (X Bot's joints, scaled) ------------------------------
KEEP = ['Hips', 'Spine', 'Spine1', 'Spine2', 'Neck', 'Head', 'HeadTop_End']
for side in ('Left', 'Right'):
    KEEP += [side + n for n in ('Shoulder', 'Arm', 'ForeArm', 'Hand', 'UpLeg', 'Leg', 'Foot', 'ToeBase', 'Toe_End')]
    KEEP += [f'{side}Hand{f}{j}' for f in ('Thumb', 'Index') for j in (1, 2, 3, 4)]


def read_xbot(XBOT):
    """Bone name -> (head, tail, parent) in metres, Z up, facing -Y, from Mixamo's X Bot (T-pose)."""
    if not os.path.exists(XBOT): raise SystemExit('X Bot.fbx not found: ' + XBOT + ' (HANDOVER §5: Mixamo X Bot, With Skin, T-pose)')
    bpy.ops.import_scene.fbx(filepath=XBOT)
    arm = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    mw = arm.matrix_world; out = {}
    for b in arm.data.bones:
        n = b.name.split(':')[-1]
        out[n] = (mw @ b.head_local, mw @ b.tail_local, b.parent.name.split(':')[-1] if b.parent else None)
    for o in list(bpy.context.scene.objects): bpy.data.objects.remove(o, do_unlink=True)
    return out

def build_rig(xb, height, name, hat=None):
    s = HEAD_AT * height / xb['Head'][0].z
    arm = bpy.data.armatures.new(name + 'Rig'); rig = bpy.data.objects.new(name, arm)
    bpy.context.scene.collection.objects.link(rig); activate(rig)
    bpy.ops.object.mode_set(mode='EDIT')
    E = arm.edit_bones
    for n in KEEP:   # parents come before children in KEEP's order within each chain; set parents after
        h, t, _ = xb[n]; b = E.new(n); b.head = h * s; b.tail = t * s
    for n in KEEP:
        p = xb[n][2]
        if p: E[n].parent = E[p]; E[n].use_connect = False
    # one of our own: the hat on its own bone (pivot on the hatband, given in Pip space), so the game can wobble it
    if hat:
        k = height / PIP_H
        b = E.new('Hat'); b.head = Vector(hat[0]) * k; b.tail = Vector(hat[1]) * k; b.parent = E['Head']; b.use_connect = False
    for axis, vertical in (('GLOBAL_NEG_Y', True), ('GLOBAL_POS_Z', False)):
        for b in E: b.select = b.select_head = b.select_tail = (abs((b.tail - b.head).normalized().z) > 0.7) == vertical
        bpy.ops.armature.calculate_roll(type=axis)
    bpy.ops.object.mode_set(mode='OBJECT')
    for b in arm.bones:
        if b.name.endswith('_End') or b.name.endswith(('Thumb4', 'Index4')): b.use_deform = False
    J = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in arm.bones}
    return rig, J, s

def joint_report(J, height):
    """editor-kit §2b: joints in % of full height, against the table (+-5% of the bone length rule, checked loosely)."""
    H = height
    rows = [('Head joint', J['Head'][0].z / H, 0.820), ('Shoulders apart', (J['LeftArm'][0].x - J['RightArm'][0].x) / H, 0.155),
            ('Shoulder height', J['LeftArm'][0].z / H, 0.739), ('Hips height', J['Hips'][0].z / H, 0.535),
            ('Hip joints apart', (J['LeftUpLeg'][0].x - J['RightUpLeg'][0].x) / H, 0.084), ('Ankle height', J['LeftFoot'][0].z / H, 0.045)]
    bad = 0
    for n, got, want in rows:
        ok = abs(got - want) <= 0.05 * want + 0.004; bad += not ok
        print('JOINT %-17s %.3f (spec %.3f) %s' % (n, got, want, 'ok' if ok else 'OFF'))
    assert bad == 0, 'joints off the editor-kit table'

# ---------------------------------------------------------------- mesh parts ---------------------------------------------------
class Part:
    """A closed mesh in world space + how to colour its faces + how to weight its vertices."""
    def __init__(self, name, colour, weights, centre=None):
        self.name, self.colour, self.weights = name, colour, weights
        self.v, self.f = [], []; self.closed = False
        self.centre = centre      # decals: which way is out (faces turned away from this point)

    def vert(self, p): self.v.append(Vector(p)); return len(self.v) - 1

    def loft(self, rings, cap0=True, cap1=True):
        n = len(rings[0]); ids = [[self.vert(p) for p in r] for r in rings]
        for a, b in zip(ids, ids[1:]):
            for j in range(n): self.f.append([a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]])
        if cap0:
            c = self.vert(sum(rings[0], Vector()) / n)
            for j in range(n): self.f.append([ids[0][(j + 1) % n], ids[0][j], c])
        if cap1:
            c = self.vert(sum(rings[-1], Vector()) / n)
            for j in range(n): self.f.append([ids[-1][j], ids[-1][(j + 1) % n], c])
        return ids

    def obj(self):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata([tuple(v) for v in self.v], [], self.f); me.update()
        bm = bmesh.new(); bm.from_mesh(me)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new(self.name, me); bpy.context.scene.collection.objects.link(ob)
        for p in me.polygons: p.use_smooth = True
        col = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
        for p in me.polygons:
            key = self.colour if isinstance(self.colour, str) else self.colour(p.center, p.normal)
            c = srgb(PAL[key]) + (1.0,)
            for li in p.loop_indices: col.data[li].color_srgb = c
        groups = {}
        for v in me.vertices:
            for bone, w in self.weights(v.co).items():
                if w <= 0.001: continue
                if bone not in groups: groups[bone] = ob.vertex_groups.new(name=bone)
                groups[bone].add([v.index], w, 'REPLACE')
        return ob


def ring(c, u, w, ru, rw, n, p=2.0, phase=0.0):
    """Superellipse section around c in the plane (u, w); p=2 is an ellipse, higher is boxier."""
    pts = []
    for j in range(n):
        a = 2 * math.pi * j / n + phase; ca, sa = math.cos(a), math.sin(a)
        x = math.copysign(abs(ca) ** (2 / p), ca); y = math.copysign(abs(sa) ** (2 / p), sa)
        pts.append(c + u * x * ru + w * y * rw)
    return pts


def frame(d):
    d = Vector(d).normalized(); a = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((0, 1, 0))
    u = a.cross(d).normalized(); return u, d.cross(u)


def tube(P, pts, radii, n=14, flat=1.0, cap0=True, cap1=True):
    pts = [Vector(p) for p in pts]; rings = []
    for i, p in enumerate(pts):
        d = pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]
        u, w = frame(d); rings.append(ring(p, u, w * flat, radii[i], radii[i], n))
    return P.loft(rings, cap0, cap1)


def ball(P, c, r, n=12, rows=8, squash=(1, 1, 1)):
    c = Vector(c); rings = []
    for k in range(1, rows):
        a = math.pi * k / rows - math.pi / 2
        rings.append([c + Vector((math.cos(a) * math.cos(2 * math.pi * j / n) * r * squash[0],
                                  math.cos(a) * math.sin(2 * math.pi * j / n) * r * squash[1], math.sin(a) * r * squash[2])) for j in range(n)])
    ids = P.loft(rings, False, False)
    b, t = P.vert(c - Vector((0, 0, r * squash[2]))), P.vert(c + Vector((0, 0, r * squash[2])))
    for j in range(n):
        P.f.append([ids[0][(j + 1) % n], ids[0][j], b]); P.f.append([ids[-1][j], ids[-1][(j + 1) % n], t])


# ---------------------------------------------------------------- weights ------------------------------------------------------
def chain(pos, keys):
    """Blend between bones placed at increasing positions along one axis: [(pos, bone), ...]."""
    if pos <= keys[0][0]: return {keys[0][1]: 1.0}
    for (a, ba), (b, bb) in zip(keys, keys[1:]):
        if pos <= b:
            t = sstep(a, b, pos) if b > a else 1.0
            out = {}; out[ba] = out.get(ba, 0) + 1 - t; out[bb] = out.get(bb, 0) + t; return out
    return {keys[-1][1]: 1.0}


def side(co): return 'Left' if co.x > 0 else 'Right'
def W_torso(co): return chain(co.z, [(0.74, 'Hips'), (0.84, 'Spine'), (0.92, 'Spine1'), (1.0, 'Spine2')])
def W_head(co): return {'Head': 1.0}
def W_hat(co): return {'Hat': 1.0}
def W_neck(co): return chain(co.z, [(1.08, 'Spine2'), (1.14, 'Neck'), (1.2, 'Head')])
def W_arm(co): s = side(co); return chain(abs(co.x), [(0.27, s + 'Arm'), (0.35, s + 'ForeArm'), (0.5, s + 'ForeArm'), (0.56, s + 'Hand')])
def W_hand(co): return {side(co) + 'Hand': 1.0}
def W_leg(co): s = side(co); return chain(co.z, [(0.36, s + 'Leg'), (0.44, s + 'UpLeg')])
def W_boot(co):
    s = side(co)
    if co.z > 0.13: return chain(co.z, [(0.13, s + 'Foot'), (0.17, s + 'Leg')])
    return chain(-co.y, [(0.02, s + 'Foot'), (0.08, s + 'ToeBase')])


class Surface:
    """Project 2D shapes (x, z) onto a mesh from the front (or the back) to lay flat decals on it."""
    def __init__(self, ob, back=False):
        me = ob.data; self.bvh = BVHTree.FromPolygons([v.co.copy() for v in me.vertices], [list(p.vertices) for p in me.polygons])
        self.back = back

    def at(self, x, z, out):
        o = Vector((x, 2.0, z)) if self.back else Vector((x, -2.0, z))
        loc, nrm, _, _ = self.bvh.ray_cast(o, Vector((0, -1 if self.back else 1, 0)))
        if loc is None: return None
        if (nrm.y > 0) != self.back: nrm = -nrm
        return loc + nrm * out


def decal(P, surf, outline, out, rings=4):
    """A flat patch whose edge is `outline` (2D, star-shaped around its centroid), laid on the surface."""
    c = sum((Vector(p) for p in outline), Vector((0, 0))) / len(outline)
    loops = [[c + (Vector(p) - c) * (k / rings) for p in outline] for k in range(1, rings + 1)]
    pts = [[surf.at(q.x, q.y, out) for q in lp] for lp in loops]
    cp = surf.at(c.x, c.y, out)
    if cp is None or any(p is None for lp in pts for p in lp): return
    ids = [[P.vert(p) for p in lp] for lp in pts]; ci = P.vert(cp); n = len(outline)
    for j in range(n): P.f.append([ci, ids[0][j], ids[0][(j + 1) % n]])
    for a, b in zip(ids, ids[1:]):
        for j in range(n): P.f.append([a[j], b[j], b[(j + 1) % n], a[(j + 1) % n]])


def stroke(P, surf, pts, width, out):
    """A flat band along a 2D polyline, laid on the surface."""
    pts = [Vector(p) for p in pts]; L, R = [], []
    for i, p in enumerate(pts):
        d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized(); nn = Vector((-d.y, d.x)) * width / 2
        L.append(surf.at(*(p + nn), out)); R.append(surf.at(*(p - nn), out))
    if any(q is None for q in L + R): return
    li = [P.vert(q) for q in L]; ri = [P.vert(q) for q in R]
    for i in range(len(pts) - 1): P.f.append([li[i], ri[i], ri[i + 1], li[i + 1]])


def ellipse(cx, cz, rx, rz, n=16, rot=0.0):
    cr, sr = math.cos(rot), math.sin(rot)
    return [(cx + rx * math.cos(a) * cr - rz * math.sin(a) * sr, cz + rx * math.cos(a) * sr + rz * math.sin(a) * cr)
            for a in [2 * math.pi * j / n for j in range(n)]]


# ---------------------------------------------------------------- assembly ---------------------------------------------------------
def activate(ob):
    for o in bpy.context.selected_objects: o.select_set(False)
    ob.select_set(True); bpy.context.view_layer.objects.active = ob


def join(objs, name):
    activate(objs[0])
    for o in objs[1:]: o.select_set(True)
    bpy.ops.object.join()
    objs[0].name = name; objs[0].data.name = name
    return objs[0]


def tris(ob): return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def decal_obj(P):
    """Like Part.obj, but decal flaps are open: keep the winding (they face away from the surface they lie on)."""
    me = bpy.data.meshes.new(P.name)
    me.from_pydata([tuple(v) for v in P.v], [], P.f); me.update()
    ob = bpy.data.objects.new(P.name, me); bpy.context.scene.collection.objects.link(ob)
    # face them outward: away from the part's centre (the head's, the torso's)
    centre = Vector(P.centre)
    bm = bmesh.new(); bm.from_mesh(me)
    for f in bm.faces:
        if f.normal.dot(f.calc_center_median() - centre) < 0: f.normal_flip()
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    col = me.color_attributes.new('Col', 'BYTE_COLOR', 'CORNER')
    c = srgb(PAL[P.colour]) + (1.0,)
    for p in me.polygons:
        for li in p.loop_indices: col.data[li].color_srgb = c
    g = {}
    for v in me.vertices:
        for bone, w in P.weights(v.co).items():
            if bone not in g: g[bone] = ob.vertex_groups.new(name=bone)
            g[bone].add([v.index], w, 'REPLACE')
    return ob


_MAT = None
def material():
    """PB_Toon: the game swaps in its toon shader; here a cel preview (2-step ramp x vertex colour) for the renders."""
    global _MAT
    if _MAT: return _MAT
    m = bpy.data.materials.new('PB_Toon'); m.use_backface_culling = True
    m.use_nodes = True; nt = m.node_tree; nt.nodes.clear(); N = nt.nodes.new; L = nt.links.new
    out = N('ShaderNodeOutputMaterial'); attr = N('ShaderNodeVertexColor'); attr.layer_name = 'Col'
    dif = N('ShaderNodeBsdfDiffuse'); s2r = N('ShaderNodeShaderToRGB'); ramp = N('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation = 'CONSTANT'
    ramp.color_ramp.elements[0].color = (0.62, 0.66, 0.92, 1); ramp.color_ramp.elements[1].position = 0.18; ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    mul = N('ShaderNodeMix'); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'; mul.inputs['Factor'].default_value = 1
    em = N('ShaderNodeEmission')
    L(dif.outputs[0], s2r.inputs[0]); L(s2r.outputs['Color'], ramp.inputs[0])
    L(attr.outputs['Color'], mul.inputs['A']); L(ramp.outputs['Color'], mul.inputs['B'])
    L(mul.outputs['Result'], em.inputs['Color']); L(em.outputs[0], out.inputs[0])
    _MAT = m; return m


# ---------------------------------------------------------------- previews + export -------------------------------------------------
def setup_render(w, h):
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x, sc.render.resolution_y = w, h
    sc.view_settings.view_transform = 'Standard'; sc.render.film_transparent = False
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True
    bg = wd.node_tree.nodes['Background']; bg.inputs[0].default_value = (0.62, 0.66, 0.72, 1); bg.inputs[1].default_value = 1.0
    sun = bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN')); sun.data.energy = 3; sun.data.angle = 0.3
    sun.rotation_euler = (math.radians(40), math.radians(-15), math.radians(-35)); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'
    try:
        sc.render.use_freestyle = True; sc.render.line_thickness_mode = 'ABSOLUTE'; sc.render.line_thickness = 1.4
        fs = sc.view_layers[0].freestyle_settings; ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new('ink')
        ls.linestyle = ls.linestyle or bpy.data.linestyles.new('ink'); ls.linestyle.color = (0.25, 0.17, 0.15); ls.select_crease = False
    except Exception as e: print('no freestyle', e)
    return cam


def shoot(cam, path, yaw, scale=1.75, look=(0, 0, 0.76)):
    a = math.radians(yaw); look = Vector(look)
    cam.data.ortho_scale = scale
    cam.location = look + Vector((math.sin(a) * 6, -math.cos(a) * 6, 0.25))
    cam.rotation_euler = (look - cam.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path; bpy.ops.render.render(write_still=True)


def pose_arms(rig, down_deg):
    """Arms lowered from the T-pose by down_deg (render check; the game's clips do this in Unity)."""
    activate(rig); bpy.ops.object.mode_set(mode='POSE')
    for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'; pb.rotation_euler = (0, 0, 0)
    for sd, s in (('Left', 1), ('Right', -1)):
        b = rig.data.bones[sd + 'Arm']; pb = rig.pose.bones[sd + 'Arm']; pb.rotation_mode = 'QUATERNION'
        a = math.radians(down_deg); want = Vector((s * math.cos(a), 0.0, -math.sin(a)))
        cur = (b.tail_local - b.head_local).normalized()
        R = b.matrix_local.to_3x3()
        pb.rotation_quaternion = (R.inverted() @ cur).rotation_difference(R.inverted() @ want)
    bpy.ops.object.mode_set(mode='OBJECT')



# ---------------------------------------------------------------- shared shapes ---------------------------------------------------
def warped_head(P, head_point, line, HK=10, NR=18, N=28):
    """A head lofted in latitude rings, ring HK bent to run exactly along `line(around)` (a hairline, a beard line), so a
    colour edge there is clean instead of a staircase. head_point(th, ph): th round the head (-pi/2 = the front, -Y),
    ph from -pi/2 (chin) to pi/2 (crown), z rising with ph; around = 0 at the front, pi at the back."""
    def ph_at(th, z):
        lo, hi = -math.pi / 2 + 1e-3, math.pi / 2 - 1e-3
        for _ in range(30):
            m = (lo + hi) / 2
            if head_point(th, m).z < z: lo = m
            else: hi = m
        return (lo + hi) / 2
    rings = []
    for k in range(1, NR):
        pts = []
        for j in range(N):
            th = 2 * math.pi * j / N - math.pi / 2
            a = abs(math.atan2(math.cos(th), -math.sin(th)))
            phl = ph_at(th, line(a))
            ph = lerp(-math.pi / 2, phl, k / HK) if k <= HK else lerp(phl, math.pi / 2, (k - HK) / (NR - HK))
            pts.append(head_point(th, ph))
        rings.append(pts)
    ids = P.loft(rings, False, False)
    b, t = P.vert(head_point(0, -math.pi / 2)), P.vert(head_point(0, math.pi / 2))
    for j in range(N):
        P.f.append([ids[0][(j + 1) % N], ids[0][j], b]); P.f.append([ids[-1][j], ids[-1][(j + 1) % N], t])
    return P


def around_of(c, centre):
    """0 at the front of a head (-Y), pi at the back."""
    return abs(math.atan2(c.x - centre.x, -(c.y - centre.y)))


def hoop(P, pts, r, n=6):
    """A closed thin tube through a loop of points (glasses rims, rings)."""
    pts = [Vector(p) for p in pts]; m = len(pts); rings = []
    for i, p in enumerate(pts):
        d = pts[(i + 1) % m] - pts[i - 1]; u, w = frame(d); rings.append(ring(p, u, w, r, r, n))
    ids = [[P.vert(q) for q in rg] for rg in rings]
    for i in range(m):
        a, b = ids[i], ids[(i + 1) % m]
        for j in range(n): P.f.append([a[j], a[(j + 1) % n], b[(j + 1) % n], b[j]])


def cone(P, base, tip, r, n=6):
    """A spike: a ring at `base`, closed to a point at `tip` (spiky hair)."""
    base, tip = Vector(base), Vector(tip); u, w = frame(tip - base)
    rg = [P.vert(q) for q in ring(base, u, w, r, r, n)]; t = P.vert(tip); c = P.vert(base - (tip - base) * 0.1)
    for j in range(n):
        P.f.append([rg[j], rg[(j + 1) % n], t]); P.f.append([rg[(j + 1) % n], rg[j], c])


def boot_shape(P, cx, keys, shaft, p=2.7):
    """A boot: rounded-box sections from the heel (+y) to the toe (-y), flat sole on z = 0, then a shaft up the shin.
    keys: (y, (half-width, height)); shaft: [(z, r), ...]."""
    rings = []
    y0, y1 = keys[0][0], keys[-1][0]
    for k in range(11):
        y = lerp(y0, y1, k / 10)
        rx, top = spline([(-t, v) for t, v in keys], -y)
        rings.append(ring(Vector((cx, y, top / 2)), Vector((1, 0, 0)), Vector((0, 0, 1)), rx, top / 2, 14, p=p))
    P.loft(rings)
    tube(P, [(cx, 0.02, z) for z, _ in shaft], [r for _, r in shaft], n=14)
