using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 1인칭 전신 플레이어 (작업 T13). Game 씬의 player 모델: 머리 뺀 복사본만 화면에, 원래 메시는 그림자만,
    /// 카메라는 눈높이에서 머리 뼈를 따라가고, 장애물 결과(PERFECT/NORMAL/FAIL)마다 Animator 상태가 바뀐다.
    /// </summary>
    public class T13_PlayerBodyPlayTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        static ChartData Chart(params ChartEvent[] events) =>
            new ChartData { name = "test", bpm = 120, runBeats = 264, events = events };

        IEnumerator Load(ChartData chart)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            body = Object.FindAnyObjectByType<PlayerBody>();
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.UseChart(chart);
            dir.AllowEventSlowMotion = false;
            boot.StartRun(0);
            yield return null;
        }

        IEnumerator AdvanceTo(double t, double? delay)
        {
            while (now < t - 1e-9)
            {
                now = System.Math.Min(t, now + 0.01);
                if (delay.HasValue)
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay.Value - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator T13_PlayerModel_HeadlessVisible_ShadowOnlyOriginal_CameraAtEye()
        {
            yield return Load(Chart());
            Assert.IsNotNull(body, "PlayerBody");
            Assert.IsTrue(body.animator != null && body.animator.isHuman, "Humanoid Animator");
            Assert.AreEqual("RhythmActionGame/Toon", body.body.sharedMaterial.shader.name, "툰 머티리얼");
            Assert.AreEqual(ShadowCastingMode.ShadowsOnly, body.body.shadowCastingMode, "원래 메시(머리 포함)는 그림자만");
            Assert.IsNotNull(body.VisibleBody, "머리 뺀 복사본");
            Assert.AreNotEqual(ShadowCastingMode.ShadowsOnly, body.VisibleBody.shadowCastingMode, "복사본은 화면에 그림");
            Assert.Greater(body.RemovedHeadTriangles, 100, "머리·목 삼각형을 뺐음");
            Assert.Less(body.VisibleBody.sharedMesh.triangles.Length / 3, body.body.sharedMesh.triangles.Length / 3);
            yield return AdvanceTo(1.0, null);
            var rig = body.cameraRig;
            Assert.AreEqual(GameConfig.EyeHeight, rig.localPosition.y, 0.15f, "카메라 눈높이 1.65 m 근처 (머리 뼈를 따라감)");
            var head = body.animator.GetBoneTransform(HumanBodyBones.Head);
            float headY = body.transform.InverseTransformPoint(head.position).y;
            Assert.AreEqual(GameConfig.EyeHeight, headY, 0.25f, $"모델 머리 높이가 눈높이 근처 ({headY:0.00})");
            // 몸이 진행 방향(플레이어 앞 +Z)을 본다: 오른 어깨 − 왼 어깨가 플레이어 오른쪽(+X)
            var t = body.transform;
            var right = t.InverseTransformDirection(body.animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position - body.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position);
            var hips = t.InverseTransformPoint(body.animator.GetBoneTransform(HumanBodyBones.Hips).position);
            Debug.Log($"[T13] shoulders right={right} hips={hips} headY={headY:0.00}");
            right.y = 0f;
            Assert.Greater(Vector3.Angle(Vector3.right, right), -1f);
            Assert.Less(Vector3.Angle(Vector3.right, right), 30f, $"몸이 앞을 봄 (어깨 방향 {right})");
            Assert.Less(new Vector2(hips.x, hips.z).magnitude, 0.25f, $"엉덩이가 플레이어 자리 ({hips})");
            Assert.AreEqual(BodyMoves.Run, body.LastState);
            Assert.IsTrue(body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.Run), "기본은 달리기");
        }

        [UnityTest]
        public IEnumerator T13_ObstacleResults_DriveAnimatorStates_FitFourBeats()
        {
            foreach (var delay in new double?[] { 0.0, 0.15, null })
            {
                yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "low_wall", key = "up" }));
                yield return AdvanceTo(5.3, delay);                       // 판정 박 8(4초) 지나 FAIL까지
                var expect = delay == 0.0 ? Judgement.Perfect : delay == 0.15 ? Judgement.Normal : Judgement.Fail;
                string state = BodyMoves.State("Vault", expect);
                Assert.AreEqual(state, body.LastState, $"delay {delay}");
                yield return null; yield return null;
                var a = body.animator;
                bool inState = a.GetCurrentAnimatorStateInfo(0).IsName(state) || a.GetNextAnimatorStateInfo(0).IsName(state);
                Assert.IsTrue(inState, $"Animator가 {state}로 전환");
                float len = body.stateSeconds[System.Array.IndexOf(body.stateNames, state)];
                Assert.AreEqual(len / (GameConfig.Body.ActionBeats * GameConfig.SecondsPerBeat), a.GetFloat(PlayerBody.ActionSpeedParam), 1e-4, "클립을 4박 길이에 맞춤");
            }
        }

        [UnityTest]
        public IEnumerator T13_DodgeLeft_Mirrored_ThenBackToRun()
        {
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "jars", key = "left" }));
            yield return AdvanceTo(4.2, 0.0);
            Assert.AreEqual(BodyMoves.State("Dodge", Judgement.Perfect), body.LastState);
            Assert.IsTrue(body.LastMirror, "왼쪽 피하기는 좌우 반전");
            Assert.IsTrue(body.animator.GetBool(PlayerBody.MirrorParam));
            yield return AdvanceTo(4.2 + GameConfig.Body.ActionBeats * GameConfig.SecondsPerBeat + 0.6, null);
            Assert.IsTrue(body.animator.GetCurrentAnimatorStateInfo(0).IsName(BodyMoves.Run), "동작이 끝나면 달리기로");
        }
    }
}
