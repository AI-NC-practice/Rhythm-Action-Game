using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>포위 전투와 최종 무사 (GDD 4.6, 4.5).</summary>
    public class T07_SurroundBossTests
    {
        double now;
        float realTime;
        GameBootstrap boot;
        StageDirector dir;

        IEnumerator Load(double startRunBeat)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0; realTime = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.slowMotion.RealTime = () => realTime;
            dir.SpinDeltaTime = () => 0.05f;     // 카메라 연출 1프레임 = 0.05초
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        IEnumerator AdvanceTo(double t, double step = 0.02)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03, 0.01);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.2, 0.01);
        }

        static int PhaseOf(JudgeCue c) => ((CombatStep)c.Tag).Phase;

        [UnityTest]
        public IEnumerator T07_Surround_Layout_FiveEnemiesRadius4()
        {
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            var stop = MapLayout.PointAt(184 * GameConfig.MetersPerBeat);
            Assert.AreEqual(5, dir.surroundEnemies.Length);
            Assert.AreEqual(3, dir.surroundEnemies.Count(e => e.kind == "sword"), "칼 3");
            Assert.AreEqual(2, dir.surroundEnemies.Count(e => e.kind == "spear"), "창 2");
            foreach (var e in dir.surroundEnemies)
            {
                var d = e.transform.position - stop; d.y = 0;
                Assert.AreEqual(4f, d.magnitude, 0.01f, "반지름 4 m");
            }
        }

        [UnityTest]
        public IEnumerator T07_Surround_AllSuccess_Windmill_DefeatsAll_SlowMo_Spin360()
        {
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.AreEqual(12, enc.Cues.Count, "순차 5 + 동시 3 + 윈드밀 4");
            Assert.AreEqual(0.0, (enc.Cues[0].TargetBeat - 2.0) % 4.0, 1e-6);

            var cues = enc.Cues.ToList();
            foreach (var c in cues.Where(c => PhaseOf(c) == 1)) yield return Hit(c, 0.0);
            foreach (var c in cues.Where(c => PhaseOf(c) == 2)) yield return Hit(c, 0.0);
            var wind = cues.Where(c => PhaseOf(c) == 3).ToList();
            yield return Hit(wind[0], 0.0);
            yield return Hit(wind[1], 0.1);      // NORMAL도 성공
            yield return Hit(wind[2], 0.0);
            Assert.IsFalse(enc.Done, "4개를 다 눌러야 함");
            yield return Hit(wind[3], 0.0);

            Assert.IsTrue(enc.Done);
            Assert.AreEqual(ExecutionKind.Windmill, enc.Execution);
            Assert.IsTrue(dir.surroundEnemies.All(e => e.Defeated && e.State == "Defeated_Windmill"), "5명 모두 날아감");
            Assert.IsTrue(dir.slowMotion.IsActive, "슬로모션");
            Assert.AreEqual(0.3f, Time.timeScale, 1e-4f);
            Assert.IsTrue(dir.WindmillSpinning);
            float maxSeen = 0;
            for (int i = 0; i < 30 && dir.WindmillSpinning; i++) { yield return null; maxSeen = Mathf.Max(maxSeen, dir.WindmillSpinDegrees); }
            Assert.AreEqual(360f, maxSeen, 0.01f, "카메라 360° 회전");
            Assert.AreEqual(3, dir.Health.Hp);

            yield return AdvanceTo(boot.clock.TimeOfBeat(enc.ResumeBeat + 0.3));
            Assert.AreEqual(StageState.Running, dir.State, "다음 마디에 다시 달림");
        }

        [UnityTest]
        public IEnumerator T07_Surround_Phase1And2Fail_LoseHp_ButContinue()
        {
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            var p1 = cues.Where(c => PhaseOf(c) == 1).ToList();
            yield return Hit(p1[0], null);                   // 1단계 FAIL
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(1, enc.Attempts, "전투는 다시 하지 않음");
            Assert.IsFalse(p1[1].Cancelled, "다음 공격 그대로");
            for (int i = 1; i < p1.Count; i++) yield return Hit(p1[i], 0.0);
            var p2 = cues.Where(c => PhaseOf(c) == 2).ToList();
            yield return Hit(p2[0], 0.0);
            yield return Hit(p2[1], null);                   // 2단계 FAIL
            Assert.AreEqual(1, dir.Health.Hp);
            Assert.IsFalse(p2[2].Cancelled);
            Assert.AreEqual(1, enc.Attempts);
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        [UnityTest]
        public IEnumerator T07_Windmill_Fail_RetriesOnlyPhase3NextBar()
        {
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            foreach (var c in cues.Where(c => PhaseOf(c) != 3)) yield return Hit(c, 0.0);
            var wind = cues.Where(c => PhaseOf(c) == 3).ToList();
            yield return Hit(wind[0], 0.0);
            yield return Hit(wind[1], null);                 // 윈드밀 FAIL
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(2, dir.WindmillAttempts);
            Assert.IsFalse(enc.Done);
            var retry = enc.Cues.Where(c => !c.Judged && PhaseOf(c) == 3).ToList();
            Assert.AreEqual(4, retry.Count, "윈드밀 4개만 다시");
            Assert.AreEqual(0, enc.Cues.Count(c => !c.Judged && PhaseOf(c) != 3), "1·2단계는 다시 하지 않음");
            Assert.AreEqual(0.0, (retry[0].TargetBeat - 2.0) % 4.0, 1e-6, "다음 마디에 등장");
            foreach (var c in retry) yield return Hit(c, 0.0);
            Assert.IsTrue(enc.Done);
            Assert.IsTrue(dir.surroundEnemies.All(e => e.Defeated));
        }

        [UnityTest]
        public IEnumerator T07_Surround_CameraTurnsToAttacker_In0_2s_AndDropsForSimultaneous()
        {
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            var p1 = cues.Where(c => PhaseOf(c) == 1).ToList();
            // 3번째 적(뒤쪽) 공격: 앞의 두 개를 처리하고 원이 첫 활성 원이 될 때
            yield return Hit(p1[0], 0.0);
            yield return Hit(p1[1], 0.0);
            yield return null;
            for (int i = 0; i < 4; i++) yield return null;   // 4 × 0.05초 = 0.2초
            var enemy = dir.surroundEnemies[2];
            var d = enemy.transform.position - boot.player.transform.position; d.y = 0;
            float want = Vector3.SignedAngle(boot.player.transform.forward, d, Vector3.up);
            Assert.AreEqual(0f, Mathf.DeltaAngle(want, dir.SurroundYaw), 1f, "0.2초 안에 공격하는 적 쪽으로");
            Assert.Greater(Mathf.Abs(want), 60f, "뒤쪽 적이라 크게 돎");

            for (int i = 2; i < p1.Count; i++) yield return Hit(p1[i], 0.0);
            var p2 = cues.Where(c => PhaseOf(c) == 2).ToList();
            yield return AdvanceTo(boot.clock.TimeOfBeat(p2[0].TargetBeat - 0.5), 0.01);
            Assert.AreEqual(StageDirector.SimultaneousCameraDrop, dir.SurroundCameraDrop, 0.01f, "동시 공격 때 카메라 낮게");
            Assert.AreEqual(StageDirector.SimultaneousCameraDrop, dir.motion.localPosition.y, 0.01f);
        }

        [UnityTest]
        public IEnumerator T07_Boss_FiveAttacks_ThenFinisher_StageClear()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.Event.IsBoss);
            Assert.AreEqual(6, enc.Cues.Count, "공격 5 + 마무리");
            foreach (var c in enc.Cues.ToList()) yield return Hit(c, 0.0);
            Assert.AreEqual(StageState.Cleared, dir.State, "무사를 쓰러뜨리면 스테이지 클리어");
            Assert.IsTrue(dir.boss.Defeated);
        }

        [UnityTest]
        public IEnumerator T07_Boss_Fail_RetriesFromFirstAttack()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            yield return Hit(cues[0], 0.0);
            yield return Hit(cues[1], 0.0);
            yield return Hit(cues[2], null);
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(2, enc.Attempts);
            Assert.AreEqual(6, enc.Cues.Count(c => !c.Judged));
            Assert.AreEqual(StageState.Combat, dir.State);
        }
    }
}
