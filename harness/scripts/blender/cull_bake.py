"""고폴리 → 안 보이는 바닥 면 삭제 → 로우폴리 → 새 UV에 색·노멀 굽기 → GLB (작업 T11R, 정적 에셋).
Usage (via blender-run.ps1): --in <high.glb> --out <low.glb> --max-tris 5000 --tex 2048 [--cull-bottom 1]
- 바닥 면: 아래를 보고(normal.z < -0.7) 모델 높이의 아래 3% 안에 있는 면 = 땅에 닿아 보이지 않는 면. 처마 밑처럼 높이 있는 아랫면은 남긴다.
- 굽기: Cycles "선택한 것에서 활성으로" — 고폴리의 기본 색(조명 없이)과 탄젠트 노멀을 로우폴리 새 UV로.
"""
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bmesh
import bpy
from _common import args, clear_scene, import_model, triangle_count, ensure_dir, ok, info, fail

a = args()
src, dst = a.get("in"), a.get("out")
max_tris = int(a.get("max-tris", "5000"))
tex = int(a.get("tex", "2048"))
cull = a.get("cull-bottom", "1") == "1"
if not src or not dst:
    fail("need --in, --out")

clear_scene()
meshes = import_model(os.path.abspath(src))
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
if len(meshes) > 1:
    bpy.ops.object.join()
high = bpy.context.view_layer.objects.active
high.name = "high"
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

# 로우폴리 = 고폴리 복제 (기하만)
low = high.copy()
low.data = high.data.copy()
low.name = "low"
bpy.context.collection.objects.link(low)

bm = bmesh.new()
bm.from_mesh(low.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
bm.normal_update()
zs = [v.co.z for v in bm.verts]
zmin, zmax = min(zs), max(zs)
height = max(zmax - zmin, 1e-6)
culled = 0
if cull:
    bottom = [f for f in bm.faces if f.normal.z < -0.7 and f.calc_center_median().z < zmin + 0.03 * height]
    culled = len(bottom)
    bmesh.ops.delete(bm, geom=bottom, context="FACES")
bm.to_mesh(low.data)
bm.free()
info(f"bottom faces culled: {culled}")

# 줄이기
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.context.view_layer.objects.active = low
before = triangle_count([low])
if before > max_tris:
    m = low.modifiers.new("dec", "DECIMATE")
    m.ratio = max_tris / before * 0.97
    bpy.ops.object.modifier_apply(modifier=m.name)
after = triangle_count([low])
info(f"triangles {before} -> {after}")

# 새 UV
low.data.materials.clear()
while low.data.uv_layers:
    low.data.uv_layers.remove(low.data.uv_layers[0])
low.data.uv_layers.new(name="UVMap")
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=0.004)
bpy.ops.object.mode_set(mode="OBJECT")

# 굽기 대상 이미지와 머티리얼
name = os.path.splitext(os.path.basename(dst))[0]
img_col = bpy.data.images.new(name + "_base_color", tex, tex, alpha=False)
img_nrm = bpy.data.images.new(name + "_normal", tex, tex, alpha=False)
img_nrm.colorspace_settings.name = "Non-Color"
mat = bpy.data.materials.new(name + "_mat")
mat.use_nodes = True
nt = mat.node_tree
bsdf = nt.nodes.get("Principled BSDF")
t_col = nt.nodes.new("ShaderNodeTexImage"); t_col.image = img_col
t_nrm = nt.nodes.new("ShaderNodeTexImage"); t_nrm.image = img_nrm
nmap = nt.nodes.new("ShaderNodeNormalMap")
low.data.materials.append(mat)

scn = bpy.context.scene
scn.render.engine = "CYCLES"
scn.cycles.device = "CPU"
scn.cycles.samples = 4
bake = scn.render.bake
bake.use_selected_to_active = True
bake.use_cage = False
bake.cage_extrusion = 0.01 * height
bake.max_ray_distance = 0.04 * height
bake.margin = 8

def bake_into(image_node, kind):
    nt.nodes.active = image_node
    bpy.ops.object.select_all(action="DESELECT")
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    if kind == "DIFFUSE":
        bake.use_pass_direct = False
        bake.use_pass_indirect = False
        bake.use_pass_color = True
        bpy.ops.object.bake(type="DIFFUSE")
    else:
        bake.normal_space = "TANGENT"
        bpy.ops.object.bake(type="NORMAL")

bake_into(t_col, "DIFFUSE")
bake_into(t_nrm, "NORMAL")
info("baked base color + normal")

# 구운 텍스처를 머티리얼에 연결
nt.links.new(t_col.outputs["Color"], bsdf.inputs["Base Color"])
nt.links.new(t_nrm.outputs["Color"], nmap.inputs["Color"])
nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
bsdf.inputs["Roughness"].default_value = 0.85

# 이미지를 파일로 저장해 두고(내보내기에서 넣기 위해) 고폴리 삭제
ensure_dir(os.path.dirname(os.path.abspath(dst)))
for img in (img_col, img_nrm):
    img.filepath_raw = os.path.join(os.path.dirname(os.path.abspath(dst)), img.name + ".png")
    img.file_format = "PNG"
    img.save()
bpy.data.objects.remove(high, do_unlink=True)
bpy.ops.object.select_all(action="DESELECT")
low.select_set(True)
bpy.ops.export_scene.gltf(filepath=os.path.abspath(dst), export_format="GLB", use_selection=True, export_image_format="AUTO")
for img in (img_col, img_nrm):
    try:
        os.remove(img.filepath_raw)
    except OSError:
        pass
ok(f"{dst} tris={after} culled={culled} ({os.path.getsize(dst) / 1e6:.2f} MB)")
