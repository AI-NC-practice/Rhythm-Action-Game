using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>일반 적 전투 (GDD 4.5). Game 씬에서 시간을 주입해 검사한다.</summary>
    public class T06_CombatTests
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
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        IEnumerator AdvanceTo(double t, double step = 0.02)
        {
            while (now < t - 1e-9)
            {
                now = System.Math.Min(t, now + step);
                yield return null;
            }
        }

        /// <summary>전투가 시작될 때까지 진행.</summary>
        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 2000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State, "적 앞에서 전투 시작");
        }

        /// <summary>현재 전투의 i번째 판정을 목표 시각 + delay에 누른다 (null이면 무입력으로 지나감).</summary>
        IEnumerator Hit(int i, double? delay)
        {
            var cue = dir.CurrentEncounter.Cues[i];
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else
            {
                yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.2);
            }
        }

        [UnityTest]
        public IEnumerator T06_StopsAtEnemy_AllPerfect_DisarmOnGround_ResumesNextBar()
        {
            yield return Load(88);   // 92: 칼 든 포졸 (땅)
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsFalse(boot.player.Running, "자동 이동 정지");
            float stopDist = boot.player.Distance;
            Assert.AreEqual(92 * 2.5f, stopDist, 0.3f);
            Assert.AreEqual(3, enc.Cues.Count, "공격 2 + 마무리 1");
            Assert.AreEqual(ArrowKey.Down, enc.Cues[0].Key, "가로베기 ↓");
            Assert.AreEqual(ArrowKey.Left, enc.Cues[1].Key, "내려베기 ←");
            Assert.AreEqual(ArrowKey.Up, enc.Cues[2].Key, "마무리 ↑");
            Assert.AreEqual(2.0, enc.Cues[1].TargetBeat - enc.Cues[0].TargetBeat, 1e-6, "공격 간격 2박");
            Assert.AreEqual(0.0, (enc.Cues[0].TargetBeat - 2.0) % 4.0, 1e-6, "첫 원은 마디 첫 박에 등장");

            yield return Hit(0, 0.0);
            yield return Hit(1, 0.0);
            Assert.AreEqual(stopDist, boot.player.Distance, 1e-3f, "전투 중 위치 고정");
            yield return Hit(2, 0.0);
            Assert.AreEqual(ExecutionKind.DisarmSlash, enc.Execution, "땅 위 PERFECT 처형");
            StringAssert.StartsWith("Defeated_DisarmSlash", dir.normalEnemies[0].State);
            Assert.AreEqual(StageState.Combat, dir.State, "다음 마디까지는 정지");
            double resume = enc.ResumeBeat;
            Assert.AreEqual(0.0, resume % 4.0, 1e-6, "다시 달리는 박은 마디 첫 박");
            yield return AdvanceTo(boot.clock.TimeOfBeat(resume - 0.1));
            Assert.AreEqual(StageState.Combat, dir.State);
            yield return AdvanceTo(boot.clock.TimeOfBeat(resume + 0.2));
            Assert.AreEqual(StageState.Running, dir.State);
            Assert.IsTrue(boot.player.Running);
            yield return AdvanceTo(now + 0.5);
            Assert.Greater(boot.player.Distance, stopDist + 0.5f, "다시 달림");
            Assert.AreEqual(3, dir.Health.Hp);
        }

        [UnityTest]
        public IEnumerator T06_DefenseFail_LosesHp_RetriesFromFirstAttackNextBar()
        {
            yield return Load(88);
            yield return UntilCombat();
            for (int i = 0; i < 4; i++) boot.clock.ApplyJudgement(Judgement.Perfect);   // 1.20배
            var enc = dir.CurrentEncounter;
            double firstTarget = enc.Cues[0].TargetBeat;
            yield return Hit(0, null);                       // 무입력 → FAIL
            Assert.AreEqual(2, dir.Health.Hp, "체력 −1");
            Assert.AreEqual(1.0f, boot.clock.SpeedMultiplier, 1e-4f, "1.2 × 0.8 = 0.96 → 1.00");
            Assert.AreEqual(2, enc.Attempts, "재도전");
            Assert.AreEqual(3, enc.Cues.Count);
            Assert.AreEqual(ArrowKey.Down, enc.Cues[0].Key, "첫 공격부터 다시");
            Assert.Greater(enc.Cues[0].TargetBeat, firstTarget);
            Assert.AreEqual(0.0, (enc.Cues[0].TargetBeat - 2.0) % 4.0, 1e-6, "다음 마디에 원 등장");
            Assert.AreEqual(CombatRules.FirstTargetBeat(firstTarget + 0.15 * 2), enc.Cues[0].TargetBeat, 1e-6);
            yield return Hit(0, 0.0);
            yield return Hit(1, 0.1);
            yield return Hit(2, 0.1);                        // NORMAL 마무리
            Assert.AreEqual(ExecutionKind.PushDown, enc.Execution);
        }

        [UnityTest]
        public IEnumerator T06_FinisherFail_RetriesWholeEnemy()
        {
            yield return Load(88);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            yield return Hit(0, 0.0);
            yield return Hit(1, 0.0);
            yield return Hit(2, null);                       // 마무리 무입력
            Assert.AreEqual(2, dir.Health.Hp);
            Assert.AreEqual(2, enc.Attempts);
            Assert.IsFalse(enc.Done);
            Assert.AreEqual(ArrowKey.Down, enc.Cues[0].Key);
        }

        [UnityTest]
        public IEnumerator T06_RoofPerfect_KickExecution_SlowMotionDoesNotAffectBeat()
        {
            yield return Load(124);  // 128: 창 든 포졸 (지붕)
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.AreEqual(ArrowKey.Right, enc.Cues[0].Key, "찌르기 →");
            Assert.AreEqual(ArrowKey.Up, enc.Cues[1].Key, "올려베기 ↑");
            yield return Hit(0, 0.0);
            yield return Hit(1, 0.0);
            yield return Hit(2, 0.0);
            Assert.AreEqual(ExecutionKind.KickOffRoof, enc.Execution, "지붕 위 PERFECT: 발차기");
            StringAssert.StartsWith("Defeated_KickOffRoof", dir.normalEnemies[1].State);

            Assert.IsTrue(dir.slowMotion.IsActive);
            Assert.AreEqual(0.3f, Time.timeScale, 1e-4f, "슬로모션 0.3");
            double b0 = boot.clock.CurrentBeat;
            float s = boot.clock.SpeedMultiplier;
            now += 0.2;
            Assert.AreEqual(b0 + 0.2 * s / 0.5, boot.clock.CurrentBeat, 1e-6, "박자 시계는 그대로");
            realTime += 0.39f; dir.slowMotion.Tick();
            Assert.AreEqual(0.3f, Time.timeScale, 1e-4f, "0.4초 전에는 유지");
            realTime += 0.02f; dir.slowMotion.Tick();
            Assert.AreEqual(1f, Time.timeScale, 1e-4f, "0.4초 뒤 원래대로");
        }

        [UnityTest]
        public IEnumerator T06_Attack_TrailAfterimages_CameraFollowsWeapon()
        {
            yield return Load(88);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var actor = dir.normalEnemies[0];
            var cue = enc.Cues[0];
            // 원이 뜨고 공격 중간쯤에서 멈춘 채 실제 시간 0.35초 기다림
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat - 0.8));
            Assert.IsTrue(cue.Appeared);
            Assert.IsTrue(actor.TrailEmitting, "무기 트레일");
            StringAssert.StartsWith("Attack_horizontal", actor.State);
            yield return new WaitForSecondsRealtime(0.35f);
            Assert.GreaterOrEqual(actor.LiveGhosts, 2, "잔상 2개 이상");
            Assert.LessOrEqual(actor.LiveGhosts, EnemyActor.GhostMax);
            Assert.Less(Vector3.Distance(cue.WorldPoint.Value, actor.weaponTip.position), 0.01f, "판정 원이 무기 끝을 따라감");
            var cam = boot.playerCamera.transform;
            Assert.Greater(Quaternion.Angle(Quaternion.identity, cam.localRotation), 1f, "카메라가 공격 지점 쪽으로 돎");
            var toTip = (actor.weaponTip.position - cam.position).normalized;
            var rigFwd = cam.parent.forward;
            Assert.Greater(Vector3.Dot(cam.forward, toTip), Vector3.Dot(rigFwd, toTip) - 1e-4f, "공격 지점을 향함");

            // 무기 끝이 움직이면 원 위치도 따라 움직임
            var p1 = cue.WorldPoint.Value;
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat - 0.1));
            Assert.Greater(Vector3.Distance(p1, cue.WorldPoint.Value), 0.05f, "휘두르는 동안 지점 이동");
        }
    }
}
