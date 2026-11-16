using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>툰 셰이딩·외곽선과 플레이어 Animator 구성 (작업 T13).</summary>
    public class T13_ToonBodyTests
    {
        const string Toon = "RhythmActionGame/Toon";

        [Test]
        public void T13_ToonShader_HasForwardOutlineShadowPasses()
        {
            var s = Shader.Find(Toon);
            Assert.IsNotNull(s, "툰 셰이더");
            Assert.IsTrue(s.isSupported, "컴파일 됨");
            var m = new Material(s);
            try
            {
                Assert.GreaterOrEqual(m.FindPass("ToonForward"), 0);
                Assert.GreaterOrEqual(m.FindPass("ToonOutline"), 0, "뒤집은 외곽선 패스");
                Assert.GreaterOrEqual(m.FindPass("ShadowCaster"), 0);
            }
            finally { Object.DestroyImmediate(m); }
        }

        [Test]
        public void T13_ToonMaterials_ForAllElevenAssets()
        {
            var ids = EditorTools.HumanoidImport.Characters.Concat(EditorTools.ToonMaterials.Statics).ToArray();
            Assert.AreEqual(11, ids.Length);
            foreach (var id in ids)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{EditorTools.ToonMaterials.Dir}/{id}.mat");
                Assert.IsNotNull(m, id);
                Assert.AreEqual(Toon, m.shader.name, id);
                Assert.IsNotNull(m.GetTexture("_BaseMap"), $"{id}: 색 텍스처");
                Assert.IsNotNull(m.GetTexture("_BumpMap"), $"{id}: 노멀 텍스처");
                Assert.AreEqual(GameConfig.Body.ToonSteps, m.GetFloat("_Steps"), 1e-4, id);
                Assert.AreEqual(GameConfig.Body.OutlineWidth, m.GetFloat("_OutlineWidth"), 1e-6, id);
            }
        }

        [Test]
        public void T13_GameScene_AllMeshMaterialsAreToon()
        {
            var scene = EditorSceneManager.OpenScene(EditorTools.SceneBuilder.GameScenePath, OpenSceneMode.Single);
            int n = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null || m.name == "Ghost") continue;     // 잔상(반투명)은 툰 대상 아님
                        Assert.AreEqual(Toon, m.shader.name, $"{r.name}: {m.name}");
                        n++;
                    }
                }
            Assert.Greater(n, 10);
        }

        [Test]
        public void T13_PlayerController_RunPlusActionResultStates_WithClips()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(EditorTools.ToonMaterials.ControllerPath);
            Assert.IsNotNull(ctrl);
            var sm = ctrl.layers[0].stateMachine;
            var states = sm.states.Select(s => s.state).ToDictionary(s => s.name);
            Assert.AreEqual(BodyMoves.Run, sm.defaultState.name);
            Assert.IsNotNull(states[BodyMoves.Run].motion, "달리기 클립");
            foreach (var a in BodyMoves.Actions)
                foreach (var r in BodyMoves.Results)
                {
                    var name = BodyMoves.State(a, r);
                    Assert.IsTrue(states.ContainsKey(name), name);
                    var st = states[name];
                    Assert.IsNotNull(st.motion, $"{name}: 클립");
                    Assert.IsTrue(st.speedParameterActive && st.speedParameter == PlayerBody.ActionSpeedParam, $"{name}: 속도 파라미터");
                    Assert.IsTrue(st.transitions.Any(t => t.destinationState.name == BodyMoves.Run && t.hasExitTime), $"{name}: 끝나면 Run");
                }
        }

        [Test]
        public void T13_BodyMoves_Mapping()
        {
            Assert.AreEqual("Vault_Perfect", BodyMoves.StateFor(ObstacleCategory.Vault, ArrowKey.Up, Judgement.Perfect));
            Assert.AreEqual("Slide_Fail", BodyMoves.StateFor(null, ArrowKey.Down, Judgement.Fail));
            Assert.AreEqual("Dodge_Normal", BodyMoves.StateFor(null, ArrowKey.Left, Judgement.Normal));
            Assert.IsTrue(BodyMoves.Mirror("Dodge", ArrowKey.Left));
            Assert.IsFalse(BodyMoves.Mirror("Dodge", ArrowKey.Right));
            Assert.IsFalse(BodyMoves.Mirror("Vault", ArrowKey.Left));
            Assert.AreEqual(1f, BodyMoves.SpeedToFit(2f, 4f, 0.5f), 1e-6);
            Assert.AreEqual(0.5f, BodyMoves.SpeedToFit(1f, 4f, 0.5f), 1e-6);
        }
    }
}
