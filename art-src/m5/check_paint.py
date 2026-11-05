# Paint-channel check of an exported boat FBX (Blender, headless):
#   blender -b -P check_paint.py -- <boat.fbx> [reference.fbx] [render-prefix]
# Prints, per mesh: verts, tris, the set of "Col" alpha bytes and faces per channel (alpha = 255 - 16*ch).
# With a reference FBX it asserts the same objects, vertex positions and RGB colours (only alpha may differ).
# With a render prefix it renders one top + one 3/4 view per channel 1..4 with that channel's faces in magenta.
import bpy, sys, os, math
from mathutils import Vector
sys.dont_write_bytecode = True
A = sys.argv[sys.argv.index('--') + 1:]
FBX, REF = A[0], (A[1] if len(A) > 1 and A[1].lower().endswith('.fbx') else None)
PRE = A[-1] if len(A) > 1 and not A[-1].lower().endswith('.fbx') else None


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    return {o.name.split('.')[0]: o for o in bpy.context.scene.objects if o.type == 'MESH'}


def data(o):
    me = o.data; ca = me.color_attributes['Col']
    rgba = [tuple(round(c * 255) for c in d.color_srgb) for d in ca.data]
    pos = [tuple(round(x, 5) for x in v.co) for v in me.vertices]
    face_a = [rgba[p.loop_start][3] for p in me.polygons] if ca.domain == 'CORNER' else None
    return me, ca, rgba, pos, face_a


def stats(obs):
    out = {}
    for n, o in sorted(obs.items()):
        if '_col' in n: continue
        me, ca, rgba, pos, fa = data(o)
        tris = sum(len(p.vertices) - 2 for p in me.polygons)
        alphas = sorted({c[3] for c in rgba})
        per = {}
        for a in fa: per[(255 - a) // 16 if (255 - a) % 16 == 0 else 'bad%d' % a] = per.get((255 - a) // 16 if (255 - a) % 16 == 0 else 'bad%d' % a, 0) + 1
        mixed = sum(1 for p in me.polygons if len({rgba[l][3] for l in range(p.loop_start, p.loop_start + p.loop_total)}) > 1)
        print('MESH %-8s verts %d tris %d domain %s alphas %s faces/channel %s mixed-alpha faces %d' % (n, len(me.vertices), tris, ca.domain, alphas, dict(sorted(per.items(), key=str)), mixed))
        out[n] = (pos, [c[:3] for c in rgba], fa)
    return out


cur = stats(load(FBX))
allA = set(a for p, c, fa in cur.values() for a in fa)
print('ALL ALPHAS', sorted(allA))
if REF:
    ref = stats(load(REF))
    assert sorted(ref) == sorted(cur), (sorted(ref), sorted(cur))
    for n in ref:
        assert ref[n][0] == cur[n][0], n + ': vertex positions differ'
        assert ref[n][1] == cur[n][1], n + ': RGB colours differ'
    print('REFERENCE MATCH: same objects, vertex positions and RGB; only alpha may differ')

if PRE:
    obs = load(FBX)
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_EEVEE'; sc.render.resolution_x = 960; sc.render.resolution_y = 640
    sc.view_settings.view_transform = 'Standard'
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; wd.node_tree.nodes['Background'].inputs[0].default_value = (0.45, 0.68, 0.93, 1)
    m = bpy.data.materials.new('vc'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    o_ = nt.nodes.new('ShaderNodeOutputMaterial'); e = nt.nodes.new('ShaderNodeEmission'); vc = nt.nodes.new('ShaderNodeVertexColor'); vc.layer_name = 'Col'
    nt.links.new(vc.outputs['Color'], e.inputs['Color']); nt.links.new(e.outputs[0], o_.inputs['Surface'])
    vis = [o for n, o in obs.items() if '_col' not in n]
    for o in obs.values():
        if '_col' in o.name: o.hide_render = True
    for o in vis: o.data.materials.clear(); o.data.materials.append(m)
    orig = {o.name: [tuple(d.color) for d in o.data.color_attributes['Col'].data] for o in vis}
    bb = [o.matrix_world @ Vector(c) for o in vis for c in o.bound_box]
    lo = Vector([min(v[k] for v in bb) for k in range(3)]); hi = Vector([max(v[k] for v in bb) for k in range(3)])
    c = (lo + hi) / 2; L = (hi - lo).length
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    def shoot(name, loc):
        cam.location = loc; cam.data.lens = 40
        cam.rotation_euler = (c - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = name; bpy.ops.render.render(write_still=True); print('RENDER', name)
    for ch in range(1, 5):
        a = (255 - 16 * ch) / 255
        for o in vis:
            ca = o.data.color_attributes['Col']; src = orig[o.name]
            for i, d in enumerate(ca.data):
                d.color = (1, 0, 1, 1) if abs(src[i][3] - a) < 0.5 / 255 else (src[i][0] * 0.55 + 0.25, src[i][1] * 0.55 + 0.25, src[i][2] * 0.55 + 0.25, 1)
        shoot('%s_ch%d_34.png' % (PRE, ch), tuple(c + Vector((-0.55 * L, -0.75 * L, 0.55 * L))))
        shoot('%s_ch%d_top.png' % (PRE, ch), tuple(c + Vector((0.001, 0.0, 1.5 * L))))
