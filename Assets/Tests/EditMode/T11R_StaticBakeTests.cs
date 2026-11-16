using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 T11R: 정적 에셋 7개는 고폴리 → 안 보이는 바닥 면 삭제 → 로우폴리 + 색·노멀 굽기(Blender cull_bake.py) 결과.
    /// GLB 파일을 직접 읽어(임포터와 무관) 삼각형 5,000 이하, 15 MB 이하, 구운 색·노멀 텍스처가 머티리얼에 붙어 있는지 본다.
    /// </summary>
    public class T11R_StaticBakeTests
    {
        static readonly string[] Statics = { "house_giwa", "wall_stone", "gate", "cart", "jars", "weapon_sword", "weapon_spear" };

        [Serializable] class Accessor { public int count; }
        [Serializable] class Primitive { public int indices = -1; public int mode = 4; }
        [Serializable] class MeshDef { public Primitive[] primitives; }
        [Serializable] class TexRef { public int index = -1; }
        [Serializable] class Pbr { public TexRef baseColorTexture; }
        [Serializable] class Material { public Pbr pbrMetallicRoughness; public TexRef normalTexture; }
        [Serializable] class Image { public int bufferView = -1; }
        [Serializable] class Gltf { public Accessor[] accessors; public MeshDef[] meshes; public Material[] materials; public Image[] images; }

        static Gltf ReadGlb(string path, out long bytes)
        {
            var b = File.ReadAllBytes(path);
            bytes = b.Length;
            Assert.AreEqual("glTF", Encoding.ASCII.GetString(b, 0, 4), $"{path}: GLB 헤더");
            int jsonLen = BitConverter.ToInt32(b, 12);
            return JsonUtility.FromJson<Gltf>(Encoding.UTF8.GetString(b, 20, jsonLen));
        }

        [Test]
        public void T11R_StaticGlb_LowPoly_Under15MB_WithBakedColorAndNormal()
        {
            foreach (var id in Statics)
            {
                string path = $"Assets/Art/Models/{id}.glb";
                Assert.IsTrue(File.Exists(path), path);
                var g = ReadGlb(path, out long bytes);
                int tris = 0;
                foreach (var m in g.meshes)
                    foreach (var p in m.primitives)
                    {
                        Assert.AreEqual(4, p.mode, $"{id}: 삼각형 프리미티브");
                        Assert.GreaterOrEqual(p.indices, 0, $"{id}: 인덱스 있음");
                        tris += g.accessors[p.indices].count / 3;
                    }
                Assert.Greater(tris, 0, id);
                Assert.LessOrEqual(tris, 5000, $"{id} 삼각형 5,000 이하 ({tris})");
                Assert.LessOrEqual(bytes, 15L * 1024 * 1024, $"{id} 15 MB 이하 ({bytes / 1e6:0.00} MB)");
                Assert.AreEqual(1, g.materials.Length, $"{id}: 구운 머티리얼 하나 (드로우콜 1)");
                var mat = g.materials[0];
                Assert.IsTrue(mat.pbrMetallicRoughness != null && mat.pbrMetallicRoughness.baseColorTexture != null && mat.pbrMetallicRoughness.baseColorTexture.index >= 0, $"{id}: 구운 색 텍스처");
                Assert.IsTrue(mat.normalTexture != null && mat.normalTexture.index >= 0, $"{id}: 구운 노멀 텍스처");
                Assert.GreaterOrEqual(g.images.Length, 2, $"{id}: 색·노멀 이미지가 GLB 안에 들어 있음");
            }
        }
    }
}
