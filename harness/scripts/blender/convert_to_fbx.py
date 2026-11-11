"""Convert a model (e.g. VARCO GLB with skin + animation) to FBX with textures embedded, for Unity Humanoid import.
Usage (via blender-run.ps1): --in <model.glb> --out <model.fbx>
- Keeps armature, skin weights and animations.
- Embeds textures (VARCO's own FBX export only references base_color.png / normal.png by name).
"""
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bpy
from _common import args, clear_scene, import_model, triangle_count, ensure_dir, ok, info, fail

a = args()
src, dst = a.get("in"), a.get("out")
if not src or not dst:
    fail("need --in and --out")

clear_scene()
meshes = import_model(os.path.abspath(src))
if not meshes:
    fail("no mesh in model")
arms = [o for o in bpy.context.scene.objects if o.type == "ARMATURE"]
actions = list(bpy.data.actions)
info(f"meshes={len(meshes)} triangles={triangle_count(meshes)} armatures={len(arms)} "
     f"bones={sum(len(x.data.bones) for x in arms)} actions={[x.name for x in actions]}")

ensure_dir(dst)
bpy.ops.export_scene.fbx(
    filepath=os.path.abspath(dst),
    use_selection=False,
    apply_scale_options="FBX_SCALE_UNITS",
    add_leaf_bones=False,
    bake_anim=bool(actions),
    path_mode="COPY",
    embed_textures=True,
)
ok(f"fbx -> {dst} ({os.path.getsize(dst) / 1024 / 1024:.2f} MB)")
