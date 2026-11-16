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
            dir.RewindRealTime = () => realTime;
            dir.FailHitSeconds = 0f;   // beta1-1: 피격 연출(GDD-beta B12.2)은 B30 테스트에서 따로 본다. 여기서는 바로 되감기
            dir.SpinDeltaTime = () => 0.05f;     // 카메라 연출 1프레임 = 0.05초
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;   // 일정한 빠르기로 입력 시각을 미리 계산하는 테스트 (회피 슬로모션은 B47 테스트)
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
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.26, 0.01);   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1)가 지나야 무입력 FAIL
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
            Assert.AreEqual(CombatRules.EncounterStartBeat(enc.ArriveBeat), enc.Cues[0].TargetBeat, 1e-6, "beta1-1: GDD-beta B10.2 — 도착 후 가장 가까운 박 + 2");

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
            // beta1-1: GDD-beta B9.2 — 윈드밀 FAIL은 체력을 깎지 않고, 곡·박자 시계를 첫 화살표 1마디 앞으로 되감아 3단계만 다시
            yield return Load(181);   // beta1-1: 180 낮은 담 사건(GDD-beta B3 차트)을 건너뛴다
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            foreach (var c in cues.Where(c => PhaseOf(c) != 3)) yield return Hit(c, 0.0);
            var wind = cues.Where(c => PhaseOf(c) == 3).ToList();
            double firstWind = wind[0].TargetBeat;
            int hp = dir.Health.Hp;
            yield return Hit(wind[0], 0.0);
            yield return Hit(wind[1], null);                 // 윈드밀 FAIL
            Assert.AreEqual(hp, dir.Health.Hp, "체력 그대로");
            Assert.IsTrue(dir.Rewinding, "되감기 중");
            Assert.AreEqual(RewindKind.Windmill, dir.LastRewind);
            Assert.AreEqual(2, dir.WindmillAttempts);
            Assert.AreEqual(0, boot.judge.Cues.Count(c => !c.Judged), "남은 판정 모두 지움");
            double paused = boot.clock.CurrentBeat;
            now += 0.5;
            yield return null;
            Assert.AreEqual(paused, boot.clock.CurrentBeat, 1e-9, "되감는 동안 박자 시계 멈춤");
            realTime += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.AreEqual(firstWind - 4.0, boot.clock.CurrentBeat, 0.05, "첫 화살표 1마디 앞으로 되감음");
            var retry = enc.Cues.Where(c => !c.Judged && PhaseOf(c) == 3).ToList();
            Assert.AreEqual(4, retry.Count, "윈드밀 4개만 다시");
            Assert.AreEqual(firstWind, retry[0].TargetBeat, 1e-6, "같은 박자에 다시");
            Assert.AreEqual(0, enc.Cues.Count(c => !c.Judged && PhaseOf(c) != 3), "1·2단계는 다시 하지 않음");
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
            // beta1-1: GDD-beta B11 — 무사는 72박 결투(판정 42, 마지막이 마무리)로 바뀌었다. 모두 성공하면 클리어
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.Event.IsBoss);
            Assert.AreEqual(BossDuel.Steps().Count, enc.Cues.Count, "결투 판정 42");
            Assert.AreEqual(CombatStepKind.Finisher, ((CombatStep)enc.Cues.Last().Tag).Kind, "마지막은 마무리");
            foreach (var c in enc.Cues.ToList()) yield return Hit(c, 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(dir.CurrentEncounter.StartBeat + BossDuel.LengthBeats) + 0.05);   // beta1-1: GDD-beta B11.5 — 처형 컷 D가 끝난 74박에 클리어
            Assert.AreEqual(StageState.Cleared, dir.State, "무사를 쓰러뜨리면 스테이지 클리어");
            Assert.IsTrue(dir.boss.Defeated);
        }

        [UnityTest]
        public IEnumerator T07_Boss_Fail_RetriesFromFirstAttack()
        {
            // beta1-1: GDD-beta B11.4 — 무사 FAIL은 체력을 깎지 않고 그 단계 처음(1단계 = O+4)으로 되감아 같은 박자에 다시
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            double first = cues[0].TargetBeat;
            yield return Hit(cues[0], 0.0);
            yield return Hit(cues[1], 0.0);
            yield return Hit(cues[2], null);
            Assert.AreEqual(3, dir.Health.Hp, "체력 그대로");
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Boss, dir.LastRewind);
            realTime += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            Assert.AreEqual(enc.StartBeat + 4.0, boot.clock.CurrentBeat, 0.05, "1단계 처음 (O+4)");
            Assert.AreEqual(2, enc.Attempts);
            Assert.AreEqual(BossDuel.Steps().Count, enc.Cues.Count(c => !c.Judged), "1단계부터 모두 다시");
            Assert.AreEqual(first, enc.Cues[0].TargetBeat, 1e-6, "같은 박자에 다시");
            Assert.AreEqual(StageState.Combat, dir.State);
            foreach (var c in enc.Cues.ToList()) yield return Hit(c, 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(dir.CurrentEncounter.StartBeat + BossDuel.LengthBeats) + 0.05);   // beta1-1: GDD-beta B11.5 — 처형 컷 D가 끝난 74박에 클리어
            Assert.AreEqual(StageState.Cleared, dir.State, "되감은 뒤 성공하면 클리어");
        }
    }
}
