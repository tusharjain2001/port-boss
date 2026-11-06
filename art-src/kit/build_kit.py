# Port Boss character kit (docs/plan/replan/chars/cast.md, editor-kit.md §2b): builds the crew's bodies from code in
# Blender 5.2, headless:   blender -b -P build_kit.py -- [pip bruno stretch rani gus sully | all] [path/to/X Bot.fbx] [norender]
# (no name = pip). Per character <name>.py: HEIGHT, ARM_DOWN, HAT (the hat bone, Pip space), PALETTE, CLOTHES, build().
# Output: Assets/Game/Art/PB_Char_<Name>.fbx  +  art-src/kit/out/<name>.blend, <name>-*.png (gitignored).
# Skeleton: X Bot's joints, read from X Bot.fbx at build time (the Mixamo file is never shipped), scaled uniformly so the
# Head joint sits at 82% of the character's height; Mixamo bone names without the prefix; mitten hands keep only the
# thumb and index chains. The mesh is modelled in Pip space and scaled uniformly to the height (kitlib.py).
import importlib, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import kitlib
from kitlib import *

CREW = ['pip', 'bruno', 'stretch', 'rani', 'gus', 'sully']
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
XBOT = next((a for a in ARGV if a.lower().endswith('.fbx')), os.path.join(REPO, 'Assets', 'ThirdParty', 'Mixamo', 'X Bot.fbx'))
RENDER = 'norender' not in ARGV
NAMES = CREW if 'all' in ARGV else [a for a in ARGV if a in CREW] or ['pip']


def build_one(name):
    bpy.ops.wm.read_factory_settings(use_empty=True); kitlib._MAT = None
    m = importlib.import_module(name)
    kitlib.PAL = dict(COMMON, **m.PALETTE); kitlib.CH = getattr(m, 'CLOTHES', {})
    Name = name.capitalize(); k = m.HEIGHT / PIP_H
    rig, J, s = build_rig(read_xbot(XBOT), m.HEIGHT, Name, m.HAT)
    print('SCALE X Bot -> %s %.4f (Pip space x %.4f)' % (Name, s, k))
    joint_report(J, m.HEIGHT)
    closed, decals = m.build()
    obs = {}
    for P in closed: obs.setdefault(P.name, []).append(P.obj())
    for P in decals(obs): obs.setdefault(P.name, []).append(P.obj() if P.closed else decal_obj(P))
    report = sorted(((n, sum(tris(o) for o in l)) for n, l in obs.items()), key=lambda x: -x[1])
    body = join([o for l in obs.values() for o in l], Name + 'Mesh')
    for v in body.data.vertices: v.co = v.co * k               # Pip space -> their height
    body.data.update()
    body.data.materials.append(material())
    body.parent = rig
    md = body.modifiers.new('Armature', 'ARMATURE'); md.object = rig
    for n, t in report: print('PART %-10s %5d tris' % (n, t))
    print('TRIS', tris(body))
    bb = [body.matrix_world @ v.co for v in body.data.vertices]
    print('BOUNDS x %.3f..%.3f y %.3f..%.3f z %.3f..%.3f' % (min(v.x for v in bb), max(v.x for v in bb), min(v.y for v in bb),
          max(v.y for v in bb), min(v.z for v in bb), max(v.z for v in bb)))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, name + '.blend'))
    fbx = os.path.join(REPO, 'Assets', 'Game', 'Art', 'PB_Char_%s.fbx' % Name)
    activate(rig); body.select_set(True)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', bake_space_transform=False, axis_forward='-Z', axis_up='Y',
                             add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, mesh_smooth_type='FACE',
                             path_mode='STRIP', armature_nodetype='NULL', primary_bone_axis='Y', secondary_bone_axis='X',
                             colors_type='SRGB')
    print('EXPORTED', fbx)
    if RENDER:
        cam = setup_render(700, 900)
        pose_arms(rig, 52)
        for view, yaw in (('front', 0), ('side', 90), ('back', 180), ('34', 35)):
            shoot(cam, os.path.join(OUT, '%s-%s.png' % (name, view)), yaw, scale=1.75 * k, look=(0, 0, 0.76 * k))
        bpy.context.scene.render.resolution_x = bpy.context.scene.render.resolution_y = 700
        shoot(cam, os.path.join(OUT, '%s-face.png' % name), 18, scale=0.62 * k, look=(0, 0, getattr(m, 'FACE_Z', 1.29) * k))


for n in NAMES: build_one(n)
