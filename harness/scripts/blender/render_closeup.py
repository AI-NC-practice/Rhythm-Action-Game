"""Close-up render of a model region (T자 자세 손 확인용, 작업 T11R).
Usage: --in <model> --out <png> --part hand|head
hand: 가장 오른쪽(+X) 끝 부근, head: 가장 위 부근. 정면(-Y 쪽)에서 본다.
"""
import os, sys, math
sys.path.append(os.path.dirname(__file__))
import bpy
from mathutils import Vector
from _common import args, clear_scene, import_model, ensure_dir, ok, fail

a = args()
src, dst, part = a.get("in"), a.get("out"), a.get("part", "hand")
clear_scene()
meshes = import_model(os.path.abspath(src))
pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
size = mx - mn
verts = [o.matrix_world @ v.co for o in meshes for v in o.data.vertices]
if part == "hand":
    edge = [v for v in verts if v.x > mx.x - 0.12 * size.x]
else:
    edge = [v for v in verts if v.z > mx.z - 0.18 * size.z]
c = sum(edge, Vector()) / len(edge)
span = max(0.12 * size.x, 0.18 * size.z) * 1.2
cam_data = bpy.data.cameras.new("cam")
cam = bpy.data.objects.new("cam", cam_data)
bpy.context.collection.objects.link(cam)
cam.location = c + Vector((0.25 * span, -2.2 * span, 0.25 * span))
d = c - cam.location
cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
cam_data.lens = 50
bpy.context.scene.camera = cam
for loc, e in ((Vector((2, -3, 4)), 3.0), (Vector((-3, -2, 2)), 1.5)):
    ld = bpy.data.lights.new("l", "SUN"); ld.energy = e
    lo = bpy.data.objects.new("l", ld); bpy.context.collection.objects.link(lo)
    lo.rotation_euler = (-loc).to_track_quat("-Z", "Y").to_euler()
scn = bpy.context.scene
scn.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
scn.render.resolution_x = scn.render.resolution_y = 640
scn.world = bpy.data.worlds.new("w"); scn.world.color = (0.2, 0.2, 0.2)
ensure_dir(os.path.dirname(os.path.abspath(dst)))
scn.render.filepath = os.path.abspath(dst)
bpy.ops.render.render(write_still=True)
ok(f"closeup {part} -> {dst}")
