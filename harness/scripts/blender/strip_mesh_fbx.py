"""Make an animation-only FBX: keep the armature and its action, delete meshes (작업 T11).
Usage (via blender-run.ps1): --in <clip_with_mesh.fbx> --out <clip_anim_only.fbx>
같은 캐릭터의 동작 FBX는 뼈가 같으므로 Unity에서는 메시가 든 기본 FBX 하나의 아바타를 쓰고 나머지는 동작만 둔다(저장소 용량).
"""
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bpy
from _common import args, clear_scene, ensure_dir, ok, info, fail

a = args()
src, dst = a.get("in"), a.get("out")
if not src or not dst:
    fail("need --in, --out")

clear_scene()
bpy.ops.import_scene.fbx(filepath=os.path.abspath(src), automatic_bone_orientation=False)
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
if not arms:
    fail("armature 없음: " + src)
removed = 0
for o in list(bpy.context.scene.objects):
    if o.type != "ARMATURE":
        bpy.data.objects.remove(o, do_unlink=True)
        removed += 1
for m in list(bpy.data.meshes):
    bpy.data.meshes.remove(m)
for img in list(bpy.data.images):
    bpy.data.images.remove(img)
actions = [ac for ac in bpy.data.actions]
info(f"armatures {len(arms)}, removed objects {removed}, actions {len(actions)}")
# 장면 길이를 동작 길이에 맞춘다 (그대로 두면 기본 250프레임으로 내보내 클립이 8.3초가 된다)
if actions:
    start = min(int(ac.frame_range[0]) for ac in actions)
    end = max(int(round(ac.frame_range[1])) for ac in actions)
    scn = bpy.context.scene
    scn.frame_start, scn.frame_end = start, max(end, start + 1)
    info(f"frame range {scn.frame_start}-{scn.frame_end} @ {scn.render.fps} fps")
ensure_dir(os.path.dirname(os.path.abspath(dst)))
bpy.ops.export_scene.fbx(
    filepath=os.path.abspath(dst),
    use_selection=False,
    object_types={"ARMATURE"},
    apply_scale_options="FBX_SCALE_UNITS",
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=False,
    bake_anim_use_nla_strips=False,
    path_mode="AUTO",
)
ok(f"anim-only fbx -> {dst} ({os.path.getsize(dst) / 1e6:.2f} MB)")
