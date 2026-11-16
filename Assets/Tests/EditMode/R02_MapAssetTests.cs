using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R02: 맵·장애물·사건 소품 새 에셋 35개 + 먼 배경 6장.
    /// 정적 에셋은 T11R과 같은 방식(고폴리 → 바닥 면 삭제 → 로우폴리 + 색·노멀 굽기)이고 Assets/Art/Models/&lt;id&gt;/&lt;id&gt;.glb에 있다.
    /// 목록과 에셋별 삼각형 상한은 harness/asset_specs.json(group "R02")에서 읽는다.
    /// </summary>
    public class R02_MapAssetTests
    {
        [Serializable] class Spec { public string id; public string group; public string kind; public int max_triangles; }
        [Serializable] class Defaults { public double max_file_mb; }
        [Serializable] class Specs { public Defaults defaults; public Spec[] assets; }

        [Serializable] class Accessor { public int count; }
        [Serializable] class Primitive { public int indices = -1; public int mode = 4; }
        [Serializable] class MeshDef { public Primitive[] primitives; }
        [Serializable] class TexRef { public int index = -1; }
        [Serializable] class Pbr { public TexRef baseColorTexture; }
        [Serializable] class Mat { public Pbr pbrMetallicRoughness; public TexRef normalTexture; }
        [Serializable] class Img { public int bufferView = -1; }
        [Serializable] class Gltf { public Accessor[] accessors; public MeshDef[] meshes; public Mat[] materials; public Img[] images; }

        static Specs Load() => JsonUtility.FromJson<Specs>(File.ReadAllText("harness/asset_specs.json"));

        static Gltf ReadGlb(string path, out long bytes)
        {
            var b = File.ReadAllBytes(path);
            bytes = b.Length;
            Assert.AreEqual("glTF", Encoding.ASCII.GetString(b, 0, 4), $"{path}: GLB 헤더");
            int jsonLen = BitConverter.ToInt32(b, 12);
            return JsonUtility.FromJson<Gltf>(Encoding.UTF8.GetString(b, 20, jsonLen));
        }

        [Test]
        public void R02_SpecList_Has35Models_And6Backdrops()
        {
            var r02 = Load().assets.Where(a => a.group == "R02").ToList();
            Assert.AreEqual(35, r02.Count(a => a.kind != "backdrop"), "새 에셋 35개 (R01 승인 목록)");
            Assert.AreEqual(6, r02.Count(a => a.kind == "backdrop"), "배경 그림 6장");
            CollectionAssert.AreEquivalent(r02.Where(a => a.kind != "backdrop").Select(a => a.id).ToArray(),
                RhythmActionGame.EditorTools.ToonMaterials.MapAssets, "툰 머티리얼 목록 = 명세 목록");
        }

        [Test]
        public void R02_Glb_LowPoly_UnderLimit_WithBakedColorAndNormal()
        {
            var specs = Load();
            long maxBytes = (long)(specs.defaults.max_file_mb * 1024 * 1024);
            foreach (var a in specs.assets.Where(a => a.group == "R02" && a.kind != "backdrop"))
            {
                string path = $"Assets/Art/Models/{a.id}/{a.id}.glb";
                Assert.IsTrue(File.Exists(path), path);
                var g = ReadGlb(path, out long bytes);
                int tris = 0;
                foreach (var m in g.meshes)
                    foreach (var p in m.primitives)
                    {
                        Assert.AreEqual(4, p.mode, $"{a.id}: 삼각형 프리미티브");
                        tris += g.accessors[p.indices].count / 3;
                    }
                Assert.Greater(tris, 0, a.id);
                Assert.LessOrEqual(tris, a.max_triangles, $"{a.id} 삼각형 {a.max_triangles} 이하 ({tris})");
                Assert.LessOrEqual(bytes, maxBytes, $"{a.id} {specs.defaults.max_file_mb} MB 이하 ({bytes / 1e6:0.00} MB)");
                Assert.AreEqual(1, g.materials.Length, $"{a.id}: 구운 머티리얼 하나 (드로우콜 1)");
                var mat = g.materials[0];
                Assert.IsTrue(mat.pbrMetallicRoughness?.baseColorTexture != null && mat.pbrMetallicRoughness.baseColorTexture.index >= 0, $"{a.id}: 구운 색 텍스처");
                Assert.IsTrue(mat.normalTexture != null && mat.normalTexture.index >= 0, $"{a.id}: 구운 노멀 텍스처");
            }
        }

        [Test]
        public void R02_ToonMaterial_HasBakedTextures()
        {
            foreach (var id in RhythmActionGame.EditorTools.ToonMaterials.MapAssets)
            {
                var m = RhythmActionGame.EditorTools.ToonMaterials.Static(id);
                Assert.AreEqual(RhythmActionGame.EditorTools.ToonMaterials.ShaderName, m.shader.name, id);
                Assert.IsNotNull(m.GetTexture("_BaseMap"), $"{id}: _BaseMap");
                Assert.IsNotNull(m.GetTexture("_BumpMap"), $"{id}: _BumpMap");
                Assert.AreEqual($"{RhythmActionGame.EditorTools.ToonMaterials.Dir}/{id}.mat", AssetDatabase.GetAssetPath(m), id);
            }
        }

        [Test]
        public void R02_Backdrops_InAssets()
        {
            foreach (var a in Load().assets.Where(a => a.kind == "backdrop"))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Backdrops/{a.id}.png");
                Assert.IsNotNull(tex, a.id);
                Assert.Greater(tex.width, tex.height, $"{a.id}: 가로로 긴 파노라마");
            }
        }
    }
}
