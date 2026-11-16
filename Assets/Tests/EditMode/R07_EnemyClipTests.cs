using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>작업 R07: 적 Animator에 달리기 상태, 지붕 발차기 낙하는 Mixamo "Flying Back Death", 소품 적에 달리기 구성요소.</summary>
    public class R07_EnemyClipTests
    {
        static AnimatorState State(string model, string name)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(EditorTools.EnemyRig.ControllerPath(model));
            Assert.IsNotNull(ctrl, model);
            return ctrl.layers[0].stateMachine.states.Select(s => s.state).FirstOrDefault(s => s.name == name);
        }

        [Test]
        public void R07_EnemyControllers_HaveRun_AndKickFallFromMixamo()
        {
            foreach (var model in new[] { "enemy_sword", "enemy_spear", "boss" })
            {
                var run = State(model, EnemyMoves.Run);
                Assert.IsNotNull(run, $"{model}: Run 상태");
                var runPath = AssetDatabase.GetAssetPath(run.motion);
                StringAssert.EndsWith($"_{EnemyMoves.RunClip}.fbx", runPath, $"{model}: 달리기 클립");
                Assert.IsTrue(((AnimationClip)run.motion).isLooping, $"{model}: 달리기 반복");

                var kick = State(model, EnemyMoves.DefeatState(ExecutionKind.KickOffRoof));
                Assert.IsNotNull(kick, $"{model}: 지붕 낙하 상태");
                Assert.AreEqual($"{EditorTools.HumanoidImport.MixamoRoot}/{EnemyMoves.KickFallClip}.fbx", AssetDatabase.GetAssetPath(kick.motion), $"{model}: Flying Back Death");
            }
            var imp = (ModelImporter)AssetImporter.GetAtPath($"{EditorTools.HumanoidImport.MixamoRoot}/{EnemyMoves.KickFallClip}.fbx");
            Assert.AreEqual(ModelImporterAnimationType.Human, imp.animationType, "Humanoid");
        }

        [Test]
        public void R07_EventFigureEnemies_HaveRunComponent()
        {
            var scene = EditorSceneManager.OpenScene(EditorTools.SceneBuilder.GameScenePath, OpenSceneMode.Single);
            var figs = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Animator>(true))
                .Where(a => a.runtimeAnimatorController != null && a.runtimeAnimatorController.name.StartsWith("Enemy_") && a.GetComponentInParent<EnemyActor>(true) == null).ToList();
            Assert.Greater(figs.Count, 5, "사건 소품 적");
            foreach (var a in figs)
            {
                var run = a.GetComponentInParent<EnemyFigureRun>(true);
                Assert.IsNotNull(run, $"{a.transform.parent.name}: 달리기 구성요소");
                Assert.AreEqual(a, run.animator);
            }
            var chase = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventProp>(true)).Where(p => p.kind == EventCatalog.Chase || p.companion == EventCatalog.Chase).ToList();
            Assert.Greater(chase.Count, 3, "추격 사건");
            foreach (var p in chase)
                Assert.Greater(p.chaseY.Length, 100, $"{p.name}: 추격 발밑 높이 프로필");
        }
    }
}
