using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>대치 공백 줄이기 (GDD-beta B10.2).</summary>
    public class B23_StandoffTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        [Test]
        public void B23_EncounterStartBeat_NearestBeatPlus2()
        {
            Assert.AreEqual(94.0, CombatRules.EncounterStartBeat(92.0), 1e-9, "박 위에 도착: 그 박 + 2");
            Assert.AreEqual(95.0, CombatRules.EncounterStartBeat(92.3), 1e-9, "박 사이: 다음 박 + 2");
            Assert.AreEqual(94.0, CombatRules.EncounterStartBeat(92.03), 1e-9, "프레임 오차는 그 박으로");
            Assert.AreEqual(-4f, GameConfig.Fx.Standoff.Fov, 1e-5f, "대치 FOV −4°");
            Assert.AreEqual(0.5f, GameConfig.Fx.StandoffApproachMeters, 1e-5f, "적이 0.5 m 다가옴");
        }

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(start);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B23_FirstRing_Within05s_OfArrival([Values(89.0, 181.0, 256.0)] double start)
        {
            yield return Load(start);
            double arrivedAt = -1, appearedAt = -1;
            JudgeCue firstCombat = null;
            boot.judge.CueAppeared += c => { if (firstCombat == null && c.Tag is CombatStep) { firstCombat = c; appearedAt = now; } };
            for (int i = 0; i < 4000 && appearedAt < 0; i++)
            {
                now += 0.005;
                yield return null;
                if (arrivedAt < 0 && dir.State == StageState.Combat) arrivedAt = now;
            }
            Assert.Greater(arrivedAt, 0, "전투 시작");
            Assert.Greater(appearedAt, 0, "첫 원 등장");
            var enc = dir.CurrentEncounter;
            // beta1-1: GDD-beta B11.1 — 무사 결투는 O(도착 후 가장 가까운 박) 뒤 등장 컷 4박이 지나야 첫 원이 뜬다
            double cut = enc.IsBossDuel ? BossDuel.PhaseStart(BossDuel.PhaseExplore) * GameConfig.SecondsPerBeat / boot.clock.SpeedMultiplier : 0;
            Assert.LessOrEqual(appearedAt - arrivedAt, 0.5 + cut + 0.02, $"{start}: 도착 후 0.5초(+등장 컷) 안에 첫 원 (공백 {appearedAt - arrivedAt:0.000}초)");
            if (enc.IsBossDuel) Assert.GreaterOrEqual(appearedAt - arrivedAt, cut - 0.02, "등장 컷 동안에는 원 없음");
            double appearBeat = enc.IsBossDuel ? enc.StartBeat + BossDuel.PhaseStart(BossDuel.PhaseExplore) : enc.StartBeat - GameConfig.ApproachBeats;
            Assert.AreEqual(appearBeat, System.Math.Round(appearBeat), 1e-9, "첫 원은 박 위에서 등장");
        }

        [UnityTest]
        public IEnumerator B23_Standoff_CameraAndEnemyStepForward()
        {
            yield return Load(89);
            var hooks = Object.FindAnyObjectByType<PresentationHooks>();
            var pd = boot.presentation;
            pd.cameraFx.DeltaTime = () => 0f;
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.01; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
            Assert.IsTrue(hooks.Log.Contains("Standoff"), "대치 연출");
            Assert.AreEqual("Standoff", pd.LastPlayed);
            pd.cameraFx.Step(0.25f);
            Assert.AreEqual(-4f, pd.cameraFx.Fov, 0.05f, "카메라가 적 쪽으로 (FOV −4°)");
            var enc = dir.CurrentEncounter;
            var enemy = enc.Actors[0];
            float d0 = Vector3.Distance(enemy.transform.position, boot.player.transform.position);
            while (boot.clock.CurrentBeat < enc.StartBeat - 0.02) { now += 0.01; yield return null; }
            Assert.AreEqual(0.5f, enemy.StandoffOffset, 0.03f, "첫 공격 직전 0.5 m 다가옴");
            Assert.Less(Vector3.Distance(enemy.transform.position, boot.player.transform.position), d0 - 0.3f, "플레이어 쪽으로");
        }
    }
}
