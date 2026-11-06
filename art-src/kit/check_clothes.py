# Clothes-channel check of an exported crew FBX (Blender, headless):
#   blender -b -P check_clothes.py -- <new PB_Char_X.fbx> <old PB_Char_X.fbx> [render-prefix]
# Asserts the same objects, vertex positions, RGB colours, vertex weights and bones as the old export (only "Col" alpha may
# differ), prints the faces per clothes channel (alpha = 255 - 16*ch: 1 Hat, 2 Top, 3 Over, 4 Legs, 5 Boots, 6 Gloves), the
# knee / Legs / Boots heights and the boot toe (metres, Z up, facing -Y), and with a render prefix renders a 3/4 front and
# a 3/4 back view per channel 1..6 with that channel in magenta, plus <prefix>_sheet.png (all of them on one sheet).
import bpy, sys, os
import numpy as np
from mathutils import Vector
sys.dont_write_bytecode = True
A = sys.argv[sys.argv.index('--') + 1:]
FBX, REF = A[0], A[1]
PRE = A[2] if len(A) > 2 else None
NAMES = {0: 'none', 1: 'Hat', 2: 'Top', 3: 'Over', 4: 'Legs', 5: 'Boots', 6: 'Gloves'}


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    return {o.name.split('.')[0]: o for o in bpy.context.scene.objects if o.type in ('MESH', 'ARMATURE')}


def data(obs):
    out = {}
    for n, o in sorted(obs.items()):
        if o.type == 'ARMATURE':
            out[n] = sorted((b.name, tuple(round(x, 5) for x in b.head_local), tuple(round(x, 5) for x in b.tail_local),
                             b.parent.name if b.parent else '') for b in o.data.bones)
            continue
        me = o.data; ca = me.color_attributes['Col']
        rgba = [tuple(round(c * 255) for c in d.color_srgb) for d in ca.data]
        gn = {g.index: g.name for g in o.vertex_groups}
        out[n] = dict(pos=[tuple(round(x, 5) for x in v.co) for v in me.vertices], rgb=[c[:3] for c in rgba],
                      w=[sorted((gn[g.group], round(g.weight, 4)) for g in v.groups) for v in me.vertices],
                      fa=[rgba[p.loop_start][3] for p in me.polygons],
                      mixed=sum(1 for p in me.polygons if len({rgba[l][3] for l in range(p.loop_start, p.loop_start + p.loop_total)}) > 1))
    return out


cur_obs = load(FBX); cur = data(cur_obs)
ref = data(load(REF))
assert sorted(ref) == sorted(cur), (sorted(ref), sorted(cur))
for n in ref:
    if isinstance(ref[n], list): assert ref[n] == cur[n], n + ': bones differ'; continue
    for k in ('pos', 'rgb', 'w'): assert ref[n][k] == cur[n][k], '%s: %s differ' % (n, k)
    assert set(ref[n]['fa']) == {255}, n + ': the old export already had alpha'
print('REFERENCE MATCH: same objects, bones, vertex positions, RGB and weights; only alpha differs')

cur_obs = load(FBX)
rig = next(o for o in cur_obs.values() if o.type == 'ARMATURE'); body = next(o for o in cur_obs.values() if o.type == 'MESH')
d = cur[body.name.split('.')[0]]
per = {}
for a in d['fa']:
    k = (255 - a) // 16 if (255 - a) % 16 == 0 else 'bad%d' % a; per[k] = per.get(k, 0) + 1
print('FACES/CHANNEL', ', '.join('%s %s' % (NAMES.get(k, k), v) for k, v in sorted(per.items(), key=str)), '| mixed-alpha faces', d['mixed'])
assert d['mixed'] == 0 and all(isinstance(k, int) and k <= 6 for k in per), 'bad alpha'
mw = body.matrix_world; me = body.data
vs = {ch: [mw @ me.vertices[i].co for p in me.polygons if (255 - d['fa'][p.index]) // 16 == ch for i in p.vertices] for ch in (4, 5)}
knee = (rig.matrix_world @ rig.data.bones['LeftLeg'].head_local).z
print('KNEE z %.3f' % knee)
if vs[4]: print('LEGS z %.3f..%.3f' % (min(v.z for v in vs[4]), max(v.z for v in vs[4])))
if vs[5]: print('BOOTS top z %.3f, toe front -y %.3f, heel +y %.3f' % (max(v.z for v in vs[5]), -min(v.y for v in vs[5]), max(v.y for v in vs[5])))

if PRE:
    sc = bpy.context.scene; sc.render.engine = 'BLENDER_EEVEE'; W, H = 360, 480
    sc.render.resolution_x, sc.render.resolution_y = W, H; sc.view_settings.view_transform = 'Standard'
    wd = bpy.data.worlds.new('w'); sc.world = wd; wd.use_nodes = True; wd.node_tree.nodes['Background'].inputs[0].default_value = (0.45, 0.68, 0.93, 1)
    m = bpy.data.materials.new('vc'); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    o_ = nt.nodes.new('ShaderNodeOutputMaterial'); e = nt.nodes.new('ShaderNodeEmission'); vc = nt.nodes.new('ShaderNodeVertexColor'); vc.layer_name = 'Col'
    nt.links.new(vc.outputs['Color'], e.inputs['Color']); nt.links.new(e.outputs[0], o_.inputs['Surface'])
    me.materials.clear(); me.materials.append(m)
    ca = me.color_attributes['Col']; src = [tuple(x.color) for x in ca.data]
    bb = [mw @ Vector(c) for c in body.bound_box]
    lo = Vector([min(v[k] for v in bb) for k in range(3)]); hi = Vector([max(v[k] for v in bb) for k in range(3)])
    c = (lo + hi) / 2; L = (hi - lo).z
    cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = L * 1.12
    shots = []
    for view, loc in (('34', (0.55, -0.8, 0.25)), ('back', (-0.55, 0.8, 0.25))):    # front from his left (the armband), back
        for ch in range(1, 7):
            a = (255 - 16 * ch) / 255
            for i, x in enumerate(ca.data):
                s = src[i]; x.color = (1, 0, 1, 1) if abs(s[3] - a) < 0.5 / 255 else (s[0] * 0.55 + 0.25, s[1] * 0.55 + 0.25, s[2] * 0.55 + 0.25, 1)
            cam.location = c + Vector(loc) * 4; cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler()
            sc.render.filepath = path = '%s_ch%d_%s.png' % (PRE, ch, view); bpy.ops.render.render(write_still=True); shots.append(path)
    sheet = np.zeros((2 * H, 6 * W, 4), np.float32)
    for k, p in enumerate(shots):
        im = bpy.data.images.load(p); px = np.array(im.pixels[:], np.float32).reshape(H, W, 4)
        r, col = divmod(k, 6); sheet[(1 - r) * H:(2 - r) * H, col * W:(col + 1) * W] = px   # pixels run bottom-up
    out = bpy.data.images.new('sheet', 6 * W, 2 * H, alpha=True); out.pixels = sheet.ravel()
    out.filepath_raw = PRE + '_sheet.png'; out.file_format = 'PNG'; out.save(); print('SHEET', PRE + '_sheet.png')
