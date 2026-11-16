using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 캐릭터 에셋 (작업 T11): VARCO T자 자세 → Rig → Animate 결과 FBX가 Humanoid 아바타(valid·human)로 들어오고,
    /// asset_specs의 동작마다 클립이 하나씩 있으며, 메시 FBX 옆에 텍스처(base_color, normal)가 있다.
    /// </summary>
    public class T11_CharacterImportTests
    {
        [System.Serializable] class Spec { public Asset[] assets; }
        [System.Serializable] class Asset { public string id; public string rig; public string[] animations; }

        static Asset[] Characters()
        {
            var s = JsonUtility.FromJson<Spec>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "harness/asset_specs.json")));
            return s.assets.Where(a => !string.IsNullOrEmpty(a.rig)).ToArray();
        }

        [Test]
        public void T11_EachAnimation_HasHumanoidFbx_ValidAvatar_AndClip()
        {
            var chars = Characters();
            CollectionAssert.AreEquivalent(new[] { "player", "enemy_sword", "enemy_spear", "boss" }, chars.Select(c => c.id).ToArray());
            foreach (var c in chars)
            {
                // 작업 R27 보강: 적은 손가락 리그(<id>_rig_fingers.fbx, 동작 없음)가 하나 더 있다
                int rig = File.Exists($"Assets/Art/Models/{c.id}/{c.id}_rig_fingers.fbx") ? 1 : 0;
                Assert.AreEqual(c.animations.Length + rig, EditorTools.HumanoidImport.FbxOf(c.id).Count, $"{c.id}: 동작 수만큼 FBX (+ 손가락 리그)");
                foreach (var anim in c.animations)
                {
                    string path = $"Assets/Art/Models/{c.id}/{c.id}_{anim}.fbx";
                    var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                    Assert.IsNotNull(imp, path);
                    Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, $"{path} Humanoid");
                    var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                    Assert.IsNotNull(avatar, $"{path} 아바타");
                    Assert.IsTrue(avatar.isValid, $"{path} avatar valid");
                    Assert.IsTrue(avatar.isHuman, $"{path} avatar human");
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(x => !x.name.StartsWith("__preview__")).ToList();
                    Assert.AreEqual(1, clips.Count, $"{path} 클립 하나");
                    Assert.Greater(clips[0].length, 0.1f, $"{path} 클립 길이");
                    Assert.AreEqual(EditorTools.HumanoidImport.IsLoop(anim), imp.clipAnimations[0].loopTime, $"{path} 반복 여부");
                }
            }
        }

        [Test]
        public void T11_MeshFbx_HasTexturesNextToIt_AndFingersOnPlayer()
        {
            foreach (var c in Characters())
            {
                string dir = $"Assets/Art/Models/{c.id}";
                Assert.IsTrue(File.Exists($"{dir}/base_color.png"), $"{c.id} base_color.png");
                Assert.IsTrue(File.Exists($"{dir}/normal.png"), $"{c.id} normal.png");
                // 메시가 든 FBX는 캐릭터당 하나 (첫 동작) — 나머지는 뼈+동작만 (저장소 용량)
                var withMesh = EditorTools.HumanoidImport.FbxOf(c.id)
                    .Where(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().Any()).ToList();
                string rigPath = $"{dir}/{c.id}_rig_fingers.fbx";
                bool hasRig = File.Exists(rigPath);
                // 작업 R27 보강: 적은 손가락 리그 FBX(메시 포함)가 모델 바탕 — 첫 동작 FBX의 메시와 함께 둘
                Assert.AreEqual(hasRig ? 2 : 1, withMesh.Count, $"{c.id}: 메시 FBX (첫 동작{(hasRig ? " + 손가락 리그" : "")})");
                Assert.AreEqual($"{dir}/{c.id}_{c.animations[0]}.fbx", withMesh.First(p => p != rigPath));
                if (hasRig)
                {
                    var ra = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Avatar>().First();
                    Assert.IsTrue(ra.isValid && ra.isHuman, $"{c.id} 손가락 리그 아바타");
                    Assert.IsTrue(ra.humanDescription.human.Any(h => h.humanName.Contains("Index")), $"{c.id} 손가락 리그에 손가락 뼈");
                    int rt = AssetDatabase.LoadAllAssetsAtPath(rigPath).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                    Assert.LessOrEqual(rt, 15000, $"{c.id} 손가락 리그 삼각형 ({rt})");
                }
                int tris = AssetDatabase.LoadAllAssetsAtPath(withMesh[0]).OfType<Mesh>().Sum(m => m.triangles.Length / 3);
                // T11R(2026-11-16 사용자 결정): 손가락·모자 챙이 무너지지 않게 캐릭터 상한 15,000
                Assert.LessOrEqual(tris, 15000, $"{c.id} 삼각형 15,000 이하 ({tris})");
            }
            var player = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/player/player_forward_run_1.fbx").OfType<Avatar>().First();
            Assert.IsTrue(player.humanDescription.human.Any(h => h.humanName.Contains("Index")), "player는 손가락 뼈 (humanoid-fingers)");
        }
    }
}
