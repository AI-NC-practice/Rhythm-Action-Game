"""Mesh health check: triangles, open boundary edges (holes), non-manifold edges, loose parts, downward-facing area.
Usage (via blender-run.ps1): --in <model.glb|fbx>
"""
import os
import sys

sys.path.append(os.path.dirname(__file__))
import bmesh
import bpy
from _common import args, clear_scene, import_model, ok, fail

a = args()
src = a.get("in")
if not src:
    fail("need --in")
clear_scene()
meshes = import_model(os.path.abspath(src))
tris = boundary = nonman = parts = 0
down_area = total_area = 0.0
for o in meshes:
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.transform(o.matrix_world)
    # glTF는 UV 경계에서 정점을 나눈다 → 같은 자리 정점을 붙여야 진짜 구멍만 남는다
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.normal_update()
    tris += len(bm.faces)
    boundary += sum(1 for e in bm.edges if e.is_boundary)
    nonman += sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    seen = set()
    for f in bm.faces:
        total_area += f.calc_area()
        if f.normal.z < -0.7:
            down_area += f.calc_area()
        if f.index in seen:
            continue
        parts += 1
        stack = [f]
        while stack:
            g = stack.pop()
            if g.index in seen:
                continue
            seen.add(g.index)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen:
                        stack.append(h)
    bm.free()
ok(f"{os.path.basename(src)} tris={tris} boundary_edges={boundary} nonmanifold_edges={nonman} parts={parts} down_facing_area={down_area / max(total_area, 1e-9) * 100:.1f}%")
