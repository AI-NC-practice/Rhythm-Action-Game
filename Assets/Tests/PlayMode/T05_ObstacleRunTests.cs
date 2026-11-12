using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>장애물 통과 (GDD 4.4). Game 씬에서 시간을 주입해 실제 진행 흐름으로 검사한다.</summary>
    public class T05_ObstacleRunTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        IEnumerator Load(double startRunBeat)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;   // GameBootstrap.Start(자동 시작)이 먼저 끝나게 한다
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(startRunBeat);
            yield return null;
        }

        /// <summary>시간을 step초 간격으로 t까지 진행 (매 단계 한 프레임).</summary>
        IEnumerator AdvanceTo(double t, double step = 0.02)
        {
            while (now < t - 1e-9)
            {
                now = System.Math.Min(t, now + step);
                yield return null;
            }
        }

        /// <summary>
        /// 8박 위치에서 시작 → 12박 수레(↑). 목표 시각 2.0초. inputDelay(초) 후 ↑ 입력, null이면 무입력.
        /// 통과 상태가 유지된 시계 박자 길이와 중간 카메라 오프셋을 돌려준다.
        /// </summary>
        IEnumerator RunCart(double? inputDelay, System.Action<double, string, Vector3> report)
        {
            yield return Load(8);
            yield return AdvanceTo(1.96);
            Assert.AreEqual(1, boot.judge.Cues.Count, "수레 판정이 예약됨");
            Assert.AreEqual(ArrowKey.Up, boot.judge.Cues[0].Key);

            double startBeat = -1, endBeat = -1;
            string state = null;
            Vector3 mid = Vector3.zero;
            bool pressed = false;
            while (now < 8.0)
            {
                now += 0.01;
                if (inputDelay.HasValue && !pressed && now >= 2.0 + inputDelay.Value)
                {
                    boot.judge.Submit(ArrowKey.Up, now);
                    pressed = true;
                }
                yield return null;
                bool active = dir.TraversalState != "None";
                if (active && startBeat < 0) startBeat = boot.clock.CurrentBeat;
                if (active && dir.TraversalState != "Pending") state = dir.TraversalState;
                if (active && dir.TraversalProgress > 0.45f && dir.TraversalProgress < 0.55f) mid = dir.motion.localPosition + dir.motion.localEulerAngles * 0.01f;
                if (!active && startBeat >= 0) { endBeat = boot.clock.CurrentBeat; break; }
            }
            report(endBeat - startBeat, state, mid);
        }

        [UnityTest]
        public IEnumerator T05_AllResults_Take4Beats_WithDifferentMotion()
        {
            double dP = 0, dN = 0, dF = 0;
            string sP = null, sN = null, sF = null;
            Vector3 mP = default, mN = default, mF = default;

            yield return RunCart(0.0, (d, s, m) => { dP = d; sP = s; mP = m; });
            yield return RunCart(0.1, (d, s, m) => { dN = d; sN = s; mN = m; });
            yield return RunCart(null, (d, s, m) => { dF = d; sF = s; mF = m; });

            Assert.AreEqual(4.0, dP, 0.1, "PERFECT 4박");
            Assert.AreEqual(4.0, dN, 0.1, "NORMAL 4박");
            Assert.AreEqual(4.0, dF, 0.1, "FAIL 4박");
            Assert.AreEqual("Vault_Perfect", sP);
            Assert.AreEqual("Vault_Normal", sN);
            Assert.AreEqual("Vault_Fail", sF);
            Assert.Greater(Vector3.Distance(mP, mN), 0.1f, "PERFECT/NORMAL 연출 다름");
            Assert.Greater(Vector3.Distance(mP, mF), 0.1f, "PERFECT/FAIL 연출 다름");
            Assert.Greater(Vector3.Distance(mN, mF), 0.1f, "NORMAL/FAIL 연출 다름");
        }

        [UnityTest]
        public IEnumerator T05_Fail_LosesHpAndSpeed_MusicKeepsGoing()
        {
            yield return Load(8);
            for (int i = 0; i < 6; i++) boot.clock.ApplyJudgement(Judgement.Perfect);
            Assert.AreEqual(1.30f, boot.clock.SpeedMultiplier, 1e-4f);
            double target = boot.clock.TimeOfBeat(4);
            yield return AdvanceTo(target - 0.05);
            double beatBefore = boot.clock.CurrentBeat;
            float distBefore = boot.player.Distance;
            yield return AdvanceTo(target + 0.2);          // 무입력 → FAIL
            Assert.AreEqual(2, dir.Health.Hp, "체력 −1");
            Assert.AreEqual(1.04f, boot.clock.SpeedMultiplier, 1e-4f, "1.30 × 0.8");
            Assert.IsTrue(boot.clock.IsRunning && !boot.clock.IsPaused, "박자 시계(음악) 계속");
            Assert.Greater(boot.clock.CurrentBeat, beatBefore, "되감지 않음");
            Assert.Greater(boot.player.Distance, distBefore, "위치도 되감지 않음");
            Assert.AreEqual(StageState.Running, dir.State);
        }

        [UnityTest]
        public IEnumerator T05_Fail_SpeedNotBelowMin()
        {
            yield return Load(8);
            yield return AdvanceTo(2.3);
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(1.00f, boot.clock.SpeedMultiplier, 1e-4f, "1.00 아래로 내려가지 않음");
        }

        [UnityTest]
        public IEnumerator T05_ThreeFails_GameOver()
        {
            yield return Load(8);    // 12 수레, 20 빨랫줄, 24 항아리 → 모두 무입력
            dir.AllowDeathRewind = false;   // beta1-1: GDD-beta B9.3은 챕터 되감기가 기본. 게임 오버 경로(GDD 4.7)를 검사
            yield return AdvanceTo(12.0, 0.05);
            Assert.AreEqual(0, dir.Health.Hp);
            Assert.AreEqual(StageState.GameOver, dir.State);
            Assert.IsFalse(boot.player.Running, "게임 오버면 멈춤");
            float d = boot.player.Distance;
            yield return AdvanceTo(13.0, 0.05);
            Assert.AreEqual(d, boot.player.Distance, 1e-4f);
        }

        [UnityTest]
        public IEnumerator T05_CuePoint_IsAtObstacle()
        {
            yield return Load(8);
            yield return AdvanceTo(1.2);
            var cue = boot.judge.Cues[0];
            Assert.IsTrue(cue.WorldPoint.HasValue);
            var expected = MapLayout.PointAt(14 * GameConfig.MetersPerBeat);
            Assert.Less(Vector3.Distance(new Vector3(expected.x, 0, expected.z), new Vector3(cue.WorldPoint.Value.x, 0, cue.WorldPoint.Value.z)), 0.01f, "판정 원이 장애물 위치에");
        }
    }
}
