using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>전투 소리 실제 녹음 + 박 맞춤 (GDD-beta B17.5, 작업 B48).</summary>
    public class B48_CombatSoundPlayTests
    {
        [UnityTest]
        public IEnumerator B48_Swing_ScheduledHalfBeatBefore_EnemyAttack_Once()
        {
            double now = 0;
            var root = new GameObject("B48");
            try
            {
                var clock = root.AddComponent<BeatClock>();
                clock.TimeSource = () => now;
                var judge = root.AddComponent<JudgeSystem>();
                judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
                judge.enabled = false;
                var go = new GameObject("Swing"); go.transform.SetParent(root.transform);
                var swing = go.AddComponent<CombatSwingSounds>();
                swing.clock = clock; swing.judge = judge;
                swing.enabled = false;
                clock.StartClock();
                yield return null;

                judge.Schedule(10, ArrowKey.Left, null, new CombatStep { Kind = CombatStepKind.Attack, Attackers = new[] { 0, 1, 2 } });   // 동시 공격도 판정 하나
                judge.Schedule(12, ArrowKey.Up, null, new CombatStep { Kind = CombatStepKind.Strike });
                judge.Schedule(13, ArrowKey.Kung, null, new RhythmNote());
                now = clock.TimeOfBeat(9.5) - 0.2; swing.Tick();
                Assert.AreEqual(0, swing.ScheduledCount, "0.1초보다 멀면 아직");
                now = clock.TimeOfBeat(9.5) - 0.05; swing.Tick();
                Assert.AreEqual(1, swing.ScheduledCount, "판정 반 박 전 \"쉭\" 예약");
                Assert.AreEqual(9.5, swing.RecentBeats.Last(), 1e-9);
                StringAssert.Contains("SwordWhoosh", swing.LastSource.clip.name);
                swing.Tick();
                Assert.AreEqual(1, swing.ScheduledCount, "한 번만");
                for (double b = 9.5; b < 13.5; b += 0.05) { now = clock.TimeOfBeat(b); swing.Tick(); }
                Assert.AreEqual(1, swing.ScheduledCount, "내 공격·장단 노드는 휘두름 예약 없음");
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator B48_GameScene_RecordedClipsLoaded_AndSwingBeforeEnemyAttack()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            Assert.IsNotNull(boot.combatSwing, "휘두름 예약 연결");
            var sfx = boot.presentation.sfx;
            foreach (var k in new[] { SfxKind.SwordWhoosh, SfxKind.ParryClang, SfxKind.BlockClang, SfxKind.Slash, SfxKind.Punch, SfxKind.Kick, SfxKind.BodyFall, SfxKind.SwordSwish })
                Assert.AreEqual("Rec_" + k, sfx.Clip(k).name, $"{k}: 실제 녹음");

            // 92박 일반 적 앞에서 시작해 첫 공격 반 박 전에 "쉭"
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;
            boot.StartRun(88);
            yield return null;
            JudgeCue attack = null;
            for (int i = 0; i < 2000 && attack == null; i++)
            {
                now += 0.02;
                yield return null;
                attack = boot.judge.Cues.FirstOrDefault(c => !c.Judged && CombatSwingSounds.IsEnemySwing(c));
                foreach (var c in boot.judge.Cues.ToList())
                    if (!c.Judged && boot.clock.CurrentBeat >= c.TargetBeat) boot.judge.Submit(c.Key, boot.clock.TimeOfBeat(c.TargetBeat));
            }
            Assert.IsNotNull(attack, "일반 적 공격");
            while (boot.clock.CurrentBeat < attack.TargetBeat - 0.3) { now += 0.02; yield return null; }
            Assert.IsTrue(boot.combatSwing.RecentBeats.Any(b => System.Math.Abs(b - (attack.TargetBeat - GameConfig.CombatSound.SwingLeadBeats)) < 1e-6), "판정 반 박 전 휘두름");
            boot.clock.StopClock();
        }
    }
}
