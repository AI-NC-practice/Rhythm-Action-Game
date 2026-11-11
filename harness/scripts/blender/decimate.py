"""Reduce a model's triangle count and export GLB (free alternative to regenerating in VARCO).
Usage (via blender-run.ps1): --in <model.glb> --out <model_small.glb> --max-tris 5000
Static meshes only (skinned/animated models: use VARCO Remesh before rigging instead).
"""
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bpy
from _common import args, clear_scene, import_model, triangle_count, ensure_dir, ok, info, fail

a = args()
src, dst = a.get("in"), a.get("out")
max_tris = int(a.get("max-tris", "5000"))
if not src or not dst:
    fail("need --in, --out, --max-tris")

clear_scene()
meshes = import_model(os.path.abspath(src))
before = triangle_count(meshes)
if before > max_tris:
    ratio = max_tris / before * 0.98
    for o in meshes:
        m = o.modifiers.new("decimate", "DECIMATE")
        m.ratio = ratio
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=m.name)
after = triangle_count(meshes)
info(f"triangles {before} -> {after} (max {max_tris})")
if after > max_tris:
    fail("still over the limit")

ensure_dir(dst)
bpy.ops.export_scene.gltf(filepath=os.path.abspath(dst), export_format="GLB")
ok(f"glb -> {dst} ({os.path.getsize(dst) / 1024 / 1024:.2f} MB)")
