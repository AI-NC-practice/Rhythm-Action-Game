using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// Mixamo 동작 (작업 T12): Without Skin FBX 11개가 Assets/Art/Animations/Mixamo/에 있고, 모두 Humanoid(아바타 valid·human,
    /// 클립 humanMotion)로 들어오며, player(VARCO) 모델에 샘플링하면 실제로 뼈가 움직인다(근육 공간 리타기팅).
    /// </summary>
    public class T12_MixamoImportTests
    {
        static readonly string[] Clips = { "vault", "slide", "wall_run", "stumble", "hit_obstacle", "duck", "dodge_right", "stab", "disarm", "kick", "windmill" };

        [Test]
        public void T12_EachMixamoClip_IsHumanoid_NoMesh()
        {
            foreach (var name in Clips)
            {
                string path = $"{EditorTools.HumanoidImport.MixamoRoot}/{name}.fbx";
                Assert.IsTrue(File.Exists(path), path);
                var imp = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, $"{name}: Humanoid");
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                var avatar = assets.OfType<Avatar>().FirstOrDefault();
                Assert.IsTrue(avatar != null && avatar.isValid && avatar.isHuman, $"{name}: 아바타 valid·human");
                Assert.IsFalse(assets.OfType<Mesh>().Any(), $"{name}: Without Skin (메시 없음)");
                var clip = assets.OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                Assert.IsNotNull(clip, $"{name}: 클립");
                Assert.IsTrue(clip.humanMotion, $"{name}: Humanoid 클립");
                Assert.Greater(clip.length, 0.2f, $"{name}: 길이");
            }
        }

        [Test]
        public void T12_MixamoClips_RetargetOntoPlayer()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/player/player_forward_run_1.fbx");
            Assert.IsNotNull(model, "player 모델");
            var go = Object.Instantiate(model);
            try
            {
                var animator = go.GetComponent<Animator>() ?? go.AddComponent<Animator>();
                Assert.IsTrue(animator.avatar != null && animator.avatar.isHuman, "player 아바타 human");
                var arm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var leg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                Assert.IsNotNull(arm); Assert.IsNotNull(leg);
                foreach (var name in Clips)
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath($"{EditorTools.HumanoidImport.MixamoRoot}/{name}.fbx")
                        .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
                    float moved = 0f;
                    clip.SampleAnimation(go, 0f);
                    var a0 = arm.localRotation; var l0 = leg.localRotation;
                    for (int i = 1; i <= 8; i++)
                    {
                        clip.SampleAnimation(go, clip.length * i / 8f);
                        moved = Mathf.Max(moved, Quaternion.Angle(a0, arm.localRotation), Quaternion.Angle(l0, leg.localRotation));
                    }
                    Assert.Greater(moved, 5f, $"{name}: player 뼈가 움직임 (리타기팅, 최대 {moved:0.0}°)");
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
