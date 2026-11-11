"""Render a preview PNG of a model so Claude (and the evaluator) can look at it with the Read tool.
Usage (via blender-run.ps1): --in <model.glb|fbx> --out <preview.png> [--size 768] [--yaw 35]
Front 3/4 view, neutral studio light, model framed to fit. Uses Workbench (fast, no GPU needed) with texture colors.
"""
import math
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bpy
from mathutils import Vector
from _common import args, clear_scene, import_model, triangle_count, ensure_dir, ok, info, fail

a = args()
src, dst = a.get("in"), a.get("out")
if not src or not dst:
    fail("need --in and --out")
size = int(a.get("size", "768"))
yaw = math.radians(float(a.get("yaw", "35")))

clear_scene()
meshes = import_model(os.path.abspath(src))
if not meshes:
    fail("no mesh in model")

# 모델 경계: 스킨/애니메이션이 적용된 실제 자세의 정점으로 계산한다 (bound_box는 기본 자세 기준이라 부정확)
depsgraph = bpy.context.evaluated_depsgraph_get()
pts = []
for o in meshes:
    ev = o.evaluated_get(depsgraph)
    m = ev.to_mesh()
    pts.extend(ev.matrix_world @ v.co for v in m.vertices)
    ev.to_mesh_clear()
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
center = (mn + mx) / 2
radius = max((mx - mn).length / 2, 1e-3)

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x = size
scene.render.resolution_y = size
scene.render.film_transparent = False
scene.world = bpy.data.worlds.new("bg") if scene.world is None else scene.world

cam_data = bpy.data.cameras.new("cam")
cam_data.lens = 50
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
dist = radius / math.tan(cam_data.angle / 2) * 1.15
direction = Vector((math.sin(yaw), -math.cos(yaw), 0.35)).normalized()  # Blender: -Y가 정면
cam.location = center + direction * dist
cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
scene.camera = cam

ensure_dir(dst)
scene.render.filepath = os.path.abspath(dst)
bpy.ops.render.render(write_still=True)
size_m = mx - mn
info(f"bounds (x,y,z) = {size_m.x:.3f}, {size_m.y:.3f}, {size_m.z:.3f} m, triangles = {triangle_count(meshes)}")
ok(f"preview -> {dst}")
