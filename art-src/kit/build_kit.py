# Port Boss character kit v1: Pip (docs/plan/replan/chars/cast.md §3, editor-kit.md §2b), modelled from code in Blender
# 5.2, headless:   blender -b -P build_kit.py -- [path/to/X Bot.fbx] [norender]
# Output: Assets/Game/Art/PB_Char_Pip.fbx  +  art-src/kit/out/{pip.blend, pip-*.png} (gitignored)
# Skeleton: X Bot's joints, read from X Bot.fbx at build time (the Mixamo file itself is never shipped), scaled uniformly
# so the Head joint sits at 82% of her 1.47 m (base 1.75 m x Pip's root 0.84); Mixamo bone names without the prefix;
# mitten hands keep only the thumb and index chains. Body = smooth lofted shapes, one mesh, colour only from the
# face-corner byte colour "Col" (sRGB) like the M1 art, one material "PB_Toon". Faces -Y, Z up, 1 unit = 1 m.
# The comedy lives in the mesh: big head, egg body, big mitts and boots around Mixamo-shaped joints.
import bpy, bmesh, math, os, sys
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
OUT = os.path.join(HERE, 'out')
FBX = os.path.join(REPO, 'Assets', 'Game', 'Art', 'PB_Char_Pip.fbx')
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
XBOT = next((a for a in ARGV if a.lower().endswith('.fbx')), os.path.join(REPO, 'Assets', 'ThirdParty', 'Mixamo', 'X Bot.fbx'))
RENDER = 'norender' not in ARGV
os.makedirs(OUT, exist_ok=True)

HEIGHT = 1.47
HEAD_AT = 0.82            # Head joint height / full height (editor-kit §2b)
PAL = dict(skin='#A86E4B', skin_dk='#93603F', cheek='#D9796A', hair='#2B2220', ink='#3F2C27', white='#FFFBF2',
           iris='#6B3F26', yellow='#E9A634', yellow_dk='#C98A2C', red='#D9483B', glove='#F0883A', glove_dk='#D8702A',
           boot='#2E3F66', sole='#1F2638', metal='#B4B8BD', mouth='#6E2A2A', tongue='#D9645A',
           player='#E8752A')  # slot 0 orange (the HUD's); armband + back patch


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


def read_xbot():
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


def build_rig(xb):
    s = HEAD_AT * HEIGHT / xb['Head'][0].z
    arm = bpy.data.armatures.new('PipRig'); rig = bpy.data.objects.new('Pip', arm)
    bpy.context.scene.collection.objects.link(rig); activate(rig)
    bpy.ops.object.mode_set(mode='EDIT')
    E = arm.edit_bones
    for n in KEEP:   # parents come before children in KEEP's order within each chain; set parents after
        h, t, _ = xb[n]; b = E.new(n); b.head = h * s; b.tail = t * s
    for n in KEEP:
        p = xb[n][2]
        if p: E[n].parent = E[p]; E[n].use_connect = False
    for axis, vertical in (('GLOBAL_NEG_Y', True), ('GLOBAL_POS_Z', False)):
        for b in E: b.select = b.select_head = b.select_tail = (abs((b.tail - b.head).normalized().z) > 0.7) == vertical
        bpy.ops.armature.calculate_roll(type=axis)
    bpy.ops.object.mode_set(mode='OBJECT')
    for b in arm.bones:
        if b.name.endswith('_End') or b.name.endswith(('Thumb4', 'Index4')): b.use_deform = False
    J = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in arm.bones}
    return rig, J, s


def joint_report(J):
    """editor-kit §2b: joints in % of full height, against the table (+-5% of the bone length rule, checked loosely)."""
    H = HEIGHT
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
    def __init__(self, name, colour, weights):
        self.name, self.colour, self.weights = name, colour, weights
        self.v, self.f = [], []; self.closed = False

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
def W_neck(co): return chain(co.z, [(1.08, 'Spine2'), (1.14, 'Neck'), (1.2, 'Head')])
def W_arm(co): s = side(co); return chain(abs(co.x), [(0.27, s + 'Arm'), (0.35, s + 'ForeArm'), (0.5, s + 'ForeArm'), (0.56, s + 'Hand')])
def W_hand(co): return {side(co) + 'Hand': 1.0}
def W_leg(co): s = side(co); return chain(co.z, [(0.36, s + 'Leg'), (0.44, s + 'UpLeg')])
def W_boot(co):
    s = side(co)
    if co.z > 0.13: return chain(co.z, [(0.13, s + 'Foot'), (0.17, s + 'Leg')])
    return chain(-co.y, [(0.02, s + 'Foot'), (0.08, s + 'ToeBase')])


# ---------------------------------------------------------------- Pip ----------------------------------------------------------
HEAD_C = Vector((0, -0.005, 1.29)); HEAD_R = Vector((0.168, 0.158, 0.172))
STRIPE = 0.052


def stripes_z(z): return 'red' if (z / STRIPE) % 1.0 < 0.5 else 'white'
def stripes_x(x): return 'red' if ((abs(x) - 0.02) / STRIPE) % 1.0 < 0.5 else 'white'


def torso_colour(c, n):
    ax = abs(c.x); front = c.y < 0
    if ax < 0.12 and front and c.z < 0.995: return 'yellow'                     # the bib
    if ax < 0.15 and not front and c.z < 0.93: return 'yellow'                  # the back bib
    if c.z < 0.82: return 'yellow'                                              # the overalls all round
    if abs(ax - 0.105) < 0.022 and c.z > 0.9: return 'yellow'                   # straps over the shoulders
    return stripes_z(c.z)


def build_torso():
    # egg: z, half-width, depth to the front, depth to the back, centre y. Wide all the way down to a low crotch.
    keys = [(0.468, (0.07, 0.06, 0.06, 0.0)), (0.49, (0.175, 0.145, 0.13, 0.0)), (0.53, (0.238, 0.2, 0.17, -0.005)),
            (0.6, (0.264, 0.236, 0.186, -0.012)), (0.7, (0.273, 0.25, 0.192, -0.018)), (0.8, (0.266, 0.24, 0.187, -0.014)),
            (0.9, (0.248, 0.212, 0.176, -0.006)), (0.98, (0.222, 0.182, 0.163, 0.003)), (1.04, (0.19, 0.155, 0.147, 0.009)),
            (1.092, (0.14, 0.118, 0.112, 0.014)), (1.13, (0.08, 0.07, 0.07, 0.018)), (1.144, (0.04, 0.04, 0.04, 0.018))]
    P = Part('Torso', torso_colour, W_torso)
    # rings on the stripe edges (every half stripe), so each stripe is a clean band of faces; a few more at the ends
    zs = sorted(set([round(k * STRIPE / 2, 4) for k in range(18, 45)] + [0.474, 0.481, 1.132, 1.139]))
    rings = []
    N = 28
    for z in zs:
        rx, ryf, ryb, yc = spline(keys, z)
        pts = []
        for j in range(N):
            a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)
            pts.append(Vector((rx * ca, yc + sa * (ryb if sa > 0 else ryf), z)))
        rings.append(pts)
    P.loft(rings)
    return P


def build_arm(sx):
    P = Part('Arm', lambda c, n: stripes_x(c.x), W_arm)
    y, z = 0.0418, 1.0858
    keys = [(0.12, (0.072,)), (0.2, (0.072,)), (0.3, (0.066,)), (0.42, (0.062,)), (0.54, (0.058,))]
    xs = [0.02 + k * STRIPE / 2 for k in range(4, 21)]          # rings on the stripe edges
    tube(P, [(sx * x, y, z) for x in xs], [spline(keys, x)[0] for x in xs], n=12)
    return P


def build_shoulder(sx):
    """The round shoulder: a ball lofted along the arm, its rings on the sleeve's stripe edges so the stripes run on."""
    P = Part('Shoulder', lambda c, n: stripes_x(c.x), W_arm)
    c, r = 0.15, 0.084
    xs = [c - r + 0.004] + [0.02 + k * STRIPE / 2 for k in range(2, 9) if abs(0.02 + k * STRIPE / 2 - c) < r - 0.004] + [c + r - 0.004]
    rings = [ring(Vector((sx * x, 0.045, 1.082)), Vector((0, 1, 0)), Vector((0, 0, 1)), math.sqrt(max(r * r - (x - c) ** 2, 1e-6)),
                  math.sqrt(max(r * r - (x - c) ** 2, 1e-6)), 12) for x in xs]
    P.loft(rings)
    return P


def build_armband(sx):
    P = Part('Armband', 'player', W_arm)
    y, z = 0.0418, 1.0858
    tube(P, [(sx * 0.212, y, z), (sx * 0.258, y, z)], [0.076, 0.075], n=14)
    return P


def build_glove(sx):
    P = Part('Glove', lambda c, n: 'glove_dk' if abs(c.x) < 0.57 and abs(c.x) > 0.555 else 'glove', W_hand)
    y, z = 0.0418, 1.082
    tube(P, [(sx * 0.505, y, z), (sx * 0.545, y, z), (sx * 0.572, y, z)], [0.064, 0.074, 0.08], n=12)        # flared cuff
    ball(P, (sx * 0.635, y + 0.004, z - 0.006), 1.0, 12, 8, squash=(0.088, 0.062, 0.045))                   # the mitt
    tube(P, [(sx * 0.585, y - 0.035, z - 0.006), (sx * 0.615, y - 0.075, z - 0.004), (sx * 0.64, y - 0.09, z)],
         [0.027, 0.025, 0.021], n=8)                                                                           # thumb
    ball(P, (sx * 0.64, y - 0.09, z), 0.021, 8, 5)
    return P


LEG_X = 0.125


def build_leg(sx):
    P = Part('Leg', 'yellow', W_leg)
    tube(P, [(sx * (LEG_X - 0.01), 0.0, 0.6), (sx * LEG_X, -0.002, 0.45), (sx * LEG_X, 0.006, 0.3), (sx * LEG_X, 0.01, 0.21)],
         [0.104, 0.102, 0.1, 0.102], n=14)
    return P


def build_boot(sx):
    cx = sx * LEG_X
    P = Part('Boot', lambda c, n: 'sole' if c.z < 0.024 else 'boot', W_boot)
    # foot: rounded-box sections along -Y (heel -> toe), flat sole on z = 0
    keys = [(0.085, (0.05, 0.095)), (0.07, (0.064, 0.115)), (0.0, (0.073, 0.125)), (-0.08, (0.073, 0.105)),
            (-0.15, (0.066, 0.085)), (-0.185, (0.046, 0.06)), (-0.198, (0.02, 0.035))]
    rings = []
    for k in range(10):
        y = lerp(0.085, -0.198, k / 9)
        rx, top = spline([(-t, v) for t, v in keys], -y)
        rings.append(ring(Vector((cx, y, top / 2)), Vector((1, 0, 0)), Vector((0, 0, 1)), rx, top / 2, 14, p=2.6))
    P.loft(rings)
    # shaft, with a rolled rim (hidden under the trouser leg)
    tube(P, [(cx, 0.018, 0.08), (cx, 0.018, 0.2), (cx, 0.018, 0.255), (cx, 0.018, 0.27)], [0.078, 0.08, 0.084, 0.076], n=14)
    return P


def head_point(th, ph):
    """Head surface: an ellipsoid, a bit wider in the cheeks and jaw, flatter on top."""
    cx, sx = math.cos(th), math.sin(th); cp, sp = math.cos(ph), math.sin(ph)
    low = clamp(-sp)                                    # 0 above the middle, 1 at the chin
    wide = 1 + 0.07 * math.sin(low * math.pi) + 0.02 * low
    x = HEAD_R.x * cp * cx * wide
    y = HEAD_R.y * cp * sx * (1 + 0.04 * low)
    z = HEAD_R.z * sp * (0.94 if sp > 0 else 1.0)
    return HEAD_C + Vector((x, y, z))


def hair_line(c):
    a = abs(math.atan2(c.x, -(c.y - HEAD_C.y)))         # 0 at the front, pi at the back
    if a < math.pi / 2: return 1.352 - 0.062 * (a / (math.pi / 2)) ** 1.4
    return 1.29 - 0.11 * ((a - math.pi / 2) / (math.pi / 2))


def build_head():
    """Latitude rings warped so ring HK runs exactly along the hairline: a clean hair edge instead of a staircase."""
    P = Part('Head', lambda c, n: 'hair' if c.z > hair_line(c) + 0.001 else 'skin', W_head)
    rings, HK, NR = [], 11, 20
    for k in range(1, NR):
        ring_pts = []
        for j in range(28):
            th = 2 * math.pi * j / 28 - math.pi / 2
            zh = hair_line(head_point(th, 0.0))
            sh = clamp((zh - HEAD_C.z) / (HEAD_R.z * (0.94 if zh > HEAD_C.z else 1.0)), -0.99, 0.99)
            phh = math.asin(sh)
            ph = lerp(-math.pi / 2, phh, k / HK) if k <= HK else lerp(phh, math.pi / 2, (k - HK) / (NR - HK))
            ring_pts.append(head_point(th, ph))
        rings.append(ring_pts)
    ids = P.loft(rings, False, False)
    b, t = P.vert(head_point(0, -math.pi / 2)), P.vert(head_point(0, math.pi / 2))
    n = len(rings[0])
    for j in range(n):
        P.f.append([ids[0][(j + 1) % n], ids[0][j], b]); P.f.append([ids[-1][j], ids[-1][(j + 1) % n], t])
    return P


def build_neck():
    P = Part('Neck', 'skin', W_neck)
    tube(P, [(0, 0.02, 1.07), (0, 0.016, 1.2)], [0.068, 0.065], n=12)
    return P


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


def build_face(head_ob):
    S = Surface(head_ob); parts = []
    def D(name, col, shapes, out, rings=3):
        P = Part(name, col, W_head)
        for sh in shapes: decal(P, S, sh, out, rings)
        parts.append(P)
    for sx in (1, -1):
        ex, ez = sx * 0.062, 1.298
        D('EyeWhite', 'white', [ellipse(ex, ez, 0.034, 0.041, 18)], 0.002)
        D('Iris', 'iris', [ellipse(ex - sx * 0.003, ez - 0.004, 0.024, 0.027, 16)], 0.0035)
        D('Pupil', 'ink', [ellipse(ex - sx * 0.003, ez - 0.004, 0.014, 0.016, 12)], 0.005)
        D('Glint', 'white', [ellipse(ex + sx * 0.004 - 0.003, ez + 0.007, 0.0065, 0.0065, 8)], 0.0062)
        D('Cheek', 'cheek', [ellipse(sx * 0.105, 1.245, 0.03, 0.022, 14)], 0.0018)
        Pb = Part('Brow', 'hair', W_head)
        stroke(Pb, S, [(ex - 0.032, 1.344), (ex - 0.012, 1.353), (ex + 0.012, 1.353), (ex + 0.032, 1.344)], 0.012, 0.003)
        parts.append(Pb)
    # the grin: a wide D with a smiling top edge, teeth along the top, a tongue at the bottom
    top = [(x, 1.232 + 0.006 * (x / 0.058) ** 2) for x in [-0.058 + 0.116 * k / 10 for k in range(11)]]
    bot = [(0.058 * math.cos(a), 1.232 - 0.045 * math.sin(a)) for a in [math.pi * k / 10 for k in range(1, 10)]]
    D('Mouth', 'mouth', [top[::-1] + bot[::-1]], 0.0022)
    Pt = Part('Teeth', 'white', W_head)
    stroke(Pt, S, [(x, z - 0.0085) for x, z in top[1:-1]], 0.013, 0.0034); parts.append(Pt)
    D('Tongue', 'tongue', [ellipse(0, 1.197, 0.027, 0.011, 14)], 0.0032)
    # nose: a soft round bump
    Pn = Part('Nose', 'skin_dk', W_head); Pn.closed = True
    tip = S.at(0, 1.268, 0.012); ball(Pn, tip, 1.0, 12, 7, squash=(0.03, 0.026, 0.026)); parts.append(Pn)
    return parts


def build_hair_puffs():
    P = Part('Puffs', 'hair', W_head)
    golden = math.pi * (3 - math.sqrt(5))
    for sx in (1, -1):
        c = Vector((sx * 0.215, 0.045, 1.245))
        ball(P, c, 0.078, 10, 6)
        for k in range(9):
            zz = 1 - 2 * (k + 0.5) / 9; r = math.sqrt(1 - zz * zz); a = golden * k
            d = Vector((math.cos(a) * r, math.sin(a) * r, zz))
            if d.x * sx < -0.4: continue                  # none buried in the head
            ball(P, c + d * 0.066, 0.047, 8, 5)
    return P


def build_hat():
    yel = lambda c, n: 'yellow_dk' if c.z < 1.405 and c.z > 1.372 and (Vector((c.x, c.y - 0.0)).length < 0.2) else 'yellow'
    P = Part('Hat', yel, W_head)
    cy = -0.004
    # crown: from the band up to a rounded top
    keys = [(1.372, 1.0), (1.42, 0.985), (1.47, 0.9), (1.505, 0.72), (1.522, 0.42), (1.528, 0.12)]
    rings = []
    for z, f in keys:
        rings.append(ring(Vector((0, cy, z)), Vector((1, 0, 0)), Vector((0, 1, 0)), 0.168 * f, 0.162 * f, 28))
    P.loft(rings, False, True)
    # brim: a closed rolled band all round, longer and drooping at the back (a sou'wester)
    N = 28; prof = []
    for j in range(N):
        a = 2 * math.pi * j / N; ca, sa = math.cos(a), math.sin(a)       # sa > 0 = back
        back = clamp(sa); front = clamp(-sa)
        reach = 0.088 + 0.025 * back - 0.006 * front
        drop = 0.022 + 0.04 * back * back - 0.012 * front
        ri = Vector((0.166 * ca, cy + 0.16 * sa, 1.374))
        ro = Vector((0.166 * ca * (1 + reach / 0.166), cy + (0.16 + reach) * sa, 1.374 - drop))
        th = 0.012
        prof.append([ri, ro, ro - Vector((0, 0, th)) + (ro - ri).normalized() * 0.004, ri - Vector((0, 0, th))])
    ids = [[P.vert(p) for p in pr] for pr in prof]
    for j in range(N):
        a, b = ids[j], ids[(j + 1) % N]
        for k in range(4): P.f.append([a[k], b[k], b[(k + 1) % 4], a[(k + 1) % 4]])
    ball(P, (0, cy, 1.53), 0.014, 8, 5)       # the button on top
    return P


def build_pocket(torso_ob):
    S = Surface(torso_ob)
    # buckles where the straps meet the bib
    pb = Part('Buckles', 'metal', W_torso); pb.closed = True
    for sx in (1, -1):
        c = S.at(sx * 0.105, 0.985, 0.0)
        rings = [ring(c + Vector((0, dy, 0)), Vector((1, 0, 0)), Vector((0, 0, 1)), 0.022, 0.016, 12, p=3) for dy in (0.006, -0.007)]
        pb.loft(rings)
    P = Part('Pocket', 'yellow_dk', W_torso)
    decal(P, S, [(-0.078, 0.94), (0.078, 0.94), (0.078, 0.835), (-0.078, 0.835)], 0.002, 2)
    Pi = Part('PocketIn', 'yellow', W_torso)
    decal(Pi, S, [(-0.068, 0.93), (0.068, 0.93), (0.068, 0.845), (-0.068, 0.845)], 0.0032, 2)
    Pb = Part('BackPatch', 'player', W_torso)
    decal(Pb, Surface(torso_ob, back=True), ellipse(0, 0.79, 0.075, 0.075, 20), 0.0025, 3)
    return [pb, P, Pi, Pb]


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


def build_pip(rig):
    closed = [build_torso(), build_head(), build_neck(), build_hat(), build_hair_puffs()]
    for sx in (1, -1):
        closed += [build_arm(sx), build_shoulder(sx), build_glove(sx), build_leg(sx), build_boot(sx)]
    closed.append(build_armband(1))          # the player colour on the LEFT upper arm (cast.md)
    obs = {}
    for P in closed:
        ob = P.obj(); obs.setdefault(P.name, []).append(ob)
    head_ob, torso_ob = obs['Head'][0], obs['Torso'][0]
    decals = build_face(head_ob) + build_pocket(torso_ob)
    for P in decals:
        ob = P.obj() if P.closed else decal_obj(P); obs.setdefault(P.name, []).append(ob)
    report = sorted(((n, sum(tris(o) for o in l)) for n, l in obs.items()), key=lambda x: -x[1])
    allobs = [o for l in obs.values() for o in l]
    pip = join(allobs, 'PipMesh')
    pip.data.materials.append(material())
    pip.parent = rig
    md = pip.modifiers.new('Armature', 'ARMATURE'); md.object = rig
    return pip, report


def decal_obj(P):
    """Like Part.obj, but decal flaps are open: keep the winding (they face away from the surface they lie on)."""
    me = bpy.data.meshes.new(P.name)
    me.from_pydata([tuple(v) for v in P.v], [], P.f); me.update()
    ob = bpy.data.objects.new(P.name, me); bpy.context.scene.collection.objects.link(ob)
    # face them outward: compare each normal to the direction away from the head / torso centre
    centre = HEAD_C if P.name not in ('Pocket', 'PocketIn', 'BackPatch') else Vector((0, 0, 0.8))
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


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    xb = read_xbot()
    rig, J, s = build_rig(xb)
    print('SCALE X Bot -> Pip %.4f' % s)
    joint_report(J)
    pip, report = build_pip(rig)
    total = tris(pip)
    for n, t in report: print('PART %-10s %5d tris' % (n, t))
    print('TRIS', total)
    bb = [pip.matrix_world @ Vector(v) for v in pip.bound_box]
    print('BOUNDS x %.3f..%.3f y %.3f..%.3f z %.3f..%.3f' % (min(v.x for v in bb), max(v.x for v in bb), min(v.y for v in bb),
          max(v.y for v in bb), min(v.z for v in bb), max(v.z for v in bb)))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, 'pip.blend'))
    activate(rig); pip.select_set(True)
    bpy.ops.export_scene.fbx(filepath=FBX, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, axis_forward='-Z', axis_up='Y',
                             add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, mesh_smooth_type='FACE',
                             path_mode='STRIP', armature_nodetype='NULL', primary_bone_axis='Y', secondary_bone_axis='X',
                             colors_type='SRGB')
    print('EXPORTED', FBX)
    if RENDER:
        cam = setup_render(700, 900)
        pose_arms(rig, 52)
        for name, yaw in (('front', 0), ('side', 90), ('back', 180), ('34', 35)):
            shoot(cam, os.path.join(OUT, 'pip-%s.png' % name), yaw)
        bpy.context.scene.render.resolution_x = bpy.context.scene.render.resolution_y = 700
        shoot(cam, os.path.join(OUT, 'pip-face.png'), 18, scale=0.62, look=(0, 0, 1.29))
        pose_arms(rig, 0)
        bpy.context.scene.render.resolution_x, bpy.context.scene.render.resolution_y = 1100, 700
        shoot(cam, os.path.join(OUT, 'pip-tpose.png'), 0, scale=1.6)


main()
