"""Shared helpers for harness Blender scripts (run via blender-run.ps1)."""
import sys
import os
import bpy


def args():
    """Parse '--key value' pairs after the '--' separator into a dict."""
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = {}
    i = 0
    while i < len(argv):
        tok = argv[i]
        if not tok.startswith("--"):
            i += 1
            continue
        has_value = i + 1 < len(argv) and not argv[i + 1].startswith("--")
        out[tok[2:]] = argv[i + 1] if has_value else "1"
        i += 2 if has_value else 1
    return out


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_model(path):
    ext = os.path.splitext(path)[1].lower()
    if ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=path)
    else:
        fail(f"unsupported input: {path}")
    # 뼈 표시용 도형(glTF 임포터가 만드는 Icosphere 등)은 모델이 아니므로 뺀다
    shapes = {pb.custom_shape for o in bpy.context.scene.objects if o.type == "ARMATURE"
              for pb in o.pose.bones if pb.custom_shape is not None}
    return [o for o in bpy.context.scene.objects if o.type == "MESH" and o not in shapes]


def triangle_count(meshes):
    total = 0
    for o in meshes:
        o.data.calc_loop_triangles()
        total += len(o.data.loop_triangles)
    return total


def ensure_dir(path):
    d = os.path.dirname(os.path.abspath(path))
    os.makedirs(d, exist_ok=True)


def ok(msg):
    print(f"[OK] {msg}", flush=True)


def info(msg):
    print(f"[INFO] {msg}", flush=True)


def fail(msg):
    print(f"[FAIL] {msg}", flush=True)
    sys.exit(1)
