using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>적 모델 교체와 Animator 구성 (작업 T14).</summary>
    public class T14_EnemyRigTests
    {
        [Test]
        public void T14_GameScene_EveryEnemy_HasModel_WeaponOnHandBone_NoCapsule()
        {
            var scene = EditorSceneManager.OpenScene(EditorTools.SceneBuilder.GameScenePath, OpenSceneMode.Single);
            var actors = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyActor>(true)).ToList();
            Assert.GreaterOrEqual(actors.Count, 10, "일반·포위·무사");
            foreach (var a in actors)
            {
                Assert.IsNotNull(a.anim, $"{a.name}: EnemyAnimator");
                Assert.IsTrue(a.anim.animator.isHuman, $"{a.name}: Humanoid");
                Assert.IsNotNull(a.GetComponentInChildren<SkinnedMeshRenderer>(true), $"{a.name}: 모델 메시");
                Assert.IsNull(a.transform.Find("Body"), $"{a.name}: 캡슐 없음");
                var hand = a.anim.animator.GetBoneTransform(HumanBodyBones.RightHand);
                Assert.AreEqual(hand, a.weaponVisual.parent, $"{a.name}: 무기가 오른손 뼈에");
                Assert.IsNotNull(a.weaponVisual.GetComponentInChildren<MeshRenderer>(true), $"{a.name}: 무기 모델");
                Assert.IsTrue(a.weaponTip.IsChildOf(a.weaponVisual), $"{a.name}: 무기 끝");
                string expect = a.kind == "spear" ? "weapon_spear" : "weapon_sword";
                Assert.IsNotNull(a.weaponVisual.Find("Prop_" + expect), $"{a.name}: {expect}");
            }
            // 사건 소품의 적(돌진·매복·추격·복선)도 모델
            foreach (var r in scene.GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("EventProp_"))
                        Assert.IsFalse(t.GetComponentsInChildren<Transform>(true).Any(c => c.name == "Body" && c.GetComponent<MeshFilter>() != null
                            && c.GetComponent<MeshFilter>().sharedMesh != null && c.GetComponent<MeshFilter>().sharedMesh.name.Contains("Capsule")), $"{t.name}: 캡슐 적 없음");
        }

        [Test]
        public void T14_EnemyControllers_AllStates_HitMomentInsideClip()
        {
            foreach (var model in new[] { "enemy_sword", "enemy_spear", "boss" })
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(EditorTools.EnemyRig.ControllerPath(model));
                Assert.IsNotNull(ctrl, model);
                var states = ctrl.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);
                foreach (var n in new[] { EnemyMoves.Idle, EnemyMoves.Guard, EnemyMoves.Hit }
                    .Concat(EnemyMoves.Attacks.Select(EnemyMoves.AttackState))
                    .Concat(EnemyMoves.Defeats.Select(EnemyMoves.DefeatState)))
                {
                    Assert.IsTrue(states.ContainsKey(n), $"{model}: {n}");
                    Assert.IsNotNull(states[n].motion, $"{model}: {n} 클립");
                }
            }
            var scene = EditorSceneManager.OpenScene(EditorTools.SceneBuilder.GameScenePath, OpenSceneMode.Single);
            var ea = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyAnimator>(true)).First();
            for (int i = 0; i < ea.stateNames.Length; i++)
                if (ea.stateNames[i].StartsWith("Attack_"))
                {
                    Assert.Greater(ea.hitSeconds[i], 0.1f, ea.stateNames[i]);
                    Assert.Less(ea.hitSeconds[i], ea.stateSeconds[i], ea.stateNames[i]);
                }
        }

        [Test]
        public void T14_PlayerController_HasExecutionAndHitStates()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(EditorTools.ToonMaterials.ControllerPath);
            var states = ctrl.layers[0].stateMachine.states.Select(s => s.state).ToDictionary(s => s.name);
            foreach (var h in BodyMoves.Executions)
            {
                Assert.IsTrue(states.ContainsKey(BodyMoves.ExecState(h)), h.ToString());
                Assert.IsNotNull(states[BodyMoves.ExecState(h)].motion, h.ToString());
            }
            Assert.IsNotNull(states[BodyMoves.Hit].motion);
            Assert.AreEqual("kick", BodyMoves.ExecClip(ExecutionKind.KickOffRoof));
            Assert.AreEqual("disarm", BodyMoves.ExecClip(ExecutionKind.DisarmSlash));
            Assert.AreEqual("stab", BodyMoves.ExecClip(ExecutionKind.StabWithOwnSword));
        }
    }
}
