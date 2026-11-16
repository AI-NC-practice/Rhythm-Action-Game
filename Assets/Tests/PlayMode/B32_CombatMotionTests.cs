using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>움직이며 싸우는 전투 (GDD-beta B13.1·B13.2). 게임 씬에서 시계와 이동 시간을 주입해 검사한다.</summary>
    public class B32_CombatMotionTests
    {
        const float Dt = 0.02f;
        double now;
        GameBootstrap boot;
        StageDirector dir;

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
            dir.MotionDeltaTime = () => Dt;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(start);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator AdvanceTo(double t, double step = 0.01)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) { now += 0.01; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        [UnityTest]
        public IEnumerator B32_NormalEnemy_Charge_StepPerJudgement_Arena_Return()
        {
            yield return Load(89);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var enemy = enc.Actors[0];
            Assert.IsTrue(dir.MotionActive, "움직이는 전투");
            var C = dir.MotionCenter;
            float start = Flat(enemy.transform.position - boot.player.transform.position).magnitude;
            Assert.Greater(start, 4f, "적은 5 m 앞에 서 있음");
            // 돌진: 첫 공격 목표 박 직전에 싸움 거리
            while (boot.clock.CurrentBeat < enc.StartBeat - 0.2) { now += 0.01; yield return null; }
            Assert.AreEqual(dir.FightDistance, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, "싸움 거리까지 돌진");

            var cues = enc.Cues.ToList();
            for (int i = 0; i < cues.Count - 1; i++)                   // 마무리 전 공격들: PERFECT
            {
                var c = cues[i];
                var step = (CombatStep)c.Tag;
                var f = Flat(dir.MotionEnemy - dir.MotionPlayer).normalized;
                var right = Vector3.Cross(Vector3.up, f);
                StageDirector.StepMotion(step, Judgement.Perfect, f, right, 1, out var pm, out _);
                var p0 = dir.MotionPlayer;
                yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
                now = boot.clock.TimeOfBeat(c.TargetBeat);
                boot.judge.Submit(c.Key, now);
                yield return null;
                Assert.AreEqual(Judgement.Perfect, c.Result);
                if (step.Key != ArrowKey.Down)
                    Assert.Less(Vector3.Distance(Flat(p0 + pm), Flat(dir.MotionPlayerTarget)), 0.05f, $"{i}: {step.Key} PERFECT 이동 목표 (표 B13.1)");
                // 0.35초 뒤 도착, 플레이어 몸(카메라)이 그 자리
                yield return Frames(Mathf.CeilToInt(GameConfig.CombatMoveSeconds / Dt) + 2);
                Assert.Less(Vector3.Distance(dir.MotionPlayer, dir.MotionPlayerTarget), 1e-3f, "이동 끝");
                Assert.Less(Flat(boot.player.transform.position - dir.MotionPlayer).magnitude, 1e-3f, "플레이어가 실제로 그 자리");
                Assert.AreEqual(dir.FightDistance, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, "싸움 거리 다시 맞춤");
                Assert.LessOrEqual(Flat(dir.MotionPlayer - C).magnitude, GameConfig.CombatArenaRadius + 1e-3f, "결투 원 안");
                Assert.LessOrEqual(Flat(dir.MotionEnemy - C).magnitude, GameConfig.CombatArenaRadius + 1e-3f);
                Assert.Less(Flat(enemy.transform.position - dir.MotionEnemy).magnitude, 0.4f, "적도 그 자리 (밀림 제외)");
            }
            Assert.Greater(Flat(boot.player.transform.position - C).magnitude, 0.3f, "제자리가 아니라 움직였음");

            // 마무리 → 끝나면 경로 위 자리·방향으로 돌아옴
            var fin = cues[cues.Count - 1];
            yield return AdvanceTo(boot.clock.TimeOfBeat(fin.TargetBeat) - 0.02);
            now = boot.clock.TimeOfBeat(fin.TargetBeat);
            boot.judge.Submit(fin.Key, now);
            yield return null;
            Assert.IsTrue(enc.Done);
            yield return null;
            Assert.IsTrue(dir.MotionReturning, "돌아오는 중");
            yield return Frames(Mathf.CeilToInt(GameConfig.CombatReturnSeconds / Dt) + 2);
            Assert.IsFalse(dir.MotionReturning);
            Assert.AreEqual(Vector3.zero, boot.player.CombatOffset, "경로 위 자리");
            Assert.IsNull(boot.player.CombatFacing, "경로 방향");
            yield return AdvanceTo(boot.clock.TimeOfBeat(enc.ResumeBeat + 0.3));
            Assert.AreEqual(StageState.Running, dir.State, "다시 달림");
        }

        [UnityTest]
        public IEnumerator B32_Gap_EnemyCircles20dps_FacingMax240dps()
        {
            yield return Load(89);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            while (boot.clock.CurrentBeat < enc.StartBeat - 0.2) { now += 0.01; yield return null; }
            var c = enc.Cues[0];
            // 피하기 PERFECT처럼 큰 각도 변화 뒤 플레이어 회전 속도 검사
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
            now = boot.clock.TimeOfBeat(c.TargetBeat);
            boot.judge.Submit(c.Key, now);
            var prev = boot.player.transform.rotation;
            float maxStep = 0f;
            for (int i = 0; i < 40; i++)
            {
                yield return null;
                maxStep = Mathf.Max(maxStep, Quaternion.Angle(prev, boot.player.transform.rotation));
                prev = boot.player.transform.rotation;
            }
            Assert.LessOrEqual(maxStep, GameConfig.CombatFaceMaxDegPerSec * Dt + 0.05f, "초당 240° 이하로 돎");
            var look = Flat(dir.MotionEnemy - dir.MotionPlayer).normalized;
            Assert.Greater(Vector3.Dot(Flat(boot.player.transform.forward).normalized, look), 0.97f, "적을 바라봄");

            // 원이 없는 틈: 남은 원을 지우면 적이 둘레를 초당 20°로 돈다
            foreach (var q in enc.Cues) boot.judge.Cancel(q);
            yield return null;
            var d0 = Flat(dir.MotionEnemy - dir.MotionPlayer);
            yield return Frames(50);                                   // 1초
            var d1 = Flat(dir.MotionEnemy - dir.MotionPlayer);
            Assert.AreEqual(GameConfig.CombatCircleDegPerSec, Vector3.Angle(d0, d1), 1.5f, "1초에 20°");
            Assert.AreEqual(dir.FightDistance, d1.magnitude, 0.05f, "거리 유지");
        }

        [UnityTest]
        public IEnumerator B32_Spear_FightDistance26()
        {
            yield return Load(126);                                     // 128 창 든 포졸 (지붕)
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.AreEqual("spear", enc.Actors[0].kind);
            Assert.AreEqual(GameConfig.FightDistanceSpear, dir.FightDistance, 1e-5f, "창 싸움 거리 2.6 m");
            while (boot.clock.CurrentBeat < enc.StartBeat - 0.2) { now += 0.01; yield return null; }
            Assert.AreEqual(2.6f, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, "2.6 m까지 돌진");
            var c = enc.Cues[0];
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
            now = boot.clock.TimeOfBeat(c.TargetBeat);
            boot.judge.Submit(c.Key, now);
            yield return Frames(Mathf.CeilToInt(GameConfig.CombatMoveSeconds / Dt) + 2);
            Assert.AreEqual(2.6f, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, "이동 뒤에도 2.6 m");
        }

        [UnityTest]
        public IEnumerator B32_BossDuel_StepPerJudgement_Distance22_Arena()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.IsBossDuel);
            Assert.IsTrue(dir.MotionActive, "무사 결투도 움직이는 전투");
            Assert.AreEqual(GameConfig.FightDistanceSword, dir.FightDistance, 1e-5f, "무사 2.2 m");
            var C = dir.MotionCenter;
            // 등장 컷이 끝나는 O+4까지 싸움 거리로 다가옴
            while (boot.clock.CurrentBeat < enc.StandoffEndBeat) { now += 0.01; yield return null; }
            yield return null;
            Assert.AreEqual(2.2f, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, "O+4에 싸움 거리");
            var cues = enc.Cues.ToList();
            var kinds = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 9; i++)                                // A6← A8↓ S10 S11 A13↑ A14→ A15↓ S17 S18
            {
                var c = cues[i];
                var step = (CombatStep)c.Tag;
                var f = Flat(dir.MotionEnemy - dir.MotionPlayer).normalized;
                StageDirector.StepMotion(step, Judgement.Perfect, f, Vector3.Cross(Vector3.up, f), 1, out var pm, out _);
                var p0 = dir.MotionPlayer;
                yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
                now = boot.clock.TimeOfBeat(c.TargetBeat);
                boot.judge.Submit(c.Key, now);
                yield return null;
                Assert.AreEqual(Judgement.Perfect, c.Result);
                bool wall = Flat(p0 + pm - C).magnitude > GameConfig.CombatArenaRadius - 0.1f || Flat(p0 + pm + f * dir.FightDistance - C).magnitude > GameConfig.CombatArenaRadius - 0.1f;   // 원 가장자리에 몰리면 물러남 (B13.1)
                // 작업 R07 후속: 지붕 위(성문 위 무사)에서는 옆으로 지붕 안쪽까지만 — 지붕 끝에 몰린 이동도 가장자리처럼 본다
                if (!float.IsPositiveInfinity(dir.ArenaLateral))
                    wall |= Mathf.Abs(Vector3.Dot(p0 + pm - C, dir.ArenaRight)) > dir.ArenaLateral - 0.05f || Mathf.Abs(Vector3.Dot(p0 + pm + f * dir.FightDistance - C, dir.ArenaRight)) > dir.ArenaLateral - 0.05f;
                if (step.Key != ArrowKey.Down && !wall)
                    Assert.Less(Vector3.Distance(Flat(p0 + pm), Flat(dir.MotionPlayerTarget)), 0.05f, $"{i}: {step.Kind} {step.Key} 이동 목표 (표 B13.1)");
                yield return Frames(Mathf.CeilToInt(GameConfig.CombatMoveSeconds / Dt) + 2);
                Assert.AreEqual(2.2f, Flat(dir.MotionEnemy - dir.MotionPlayer).magnitude, 0.05f, $"{i}: 싸움 거리 2.2 m");
                Assert.LessOrEqual(Flat(dir.MotionPlayer - C).magnitude, GameConfig.CombatArenaRadius + 1e-3f, $"{i}: 결투 원 안");
                Assert.LessOrEqual(Flat(dir.MotionEnemy - C).magnitude, GameConfig.CombatArenaRadius + 1e-3f);
                Assert.Less(Flat(boot.player.transform.position - dir.MotionPlayer).magnitude, 1e-3f, "플레이어가 실제로 그 자리");
                kinds.Add($"{step.Kind}{step.Key}");
            }
            Assert.GreaterOrEqual(kinds.Count, 6, "막기·피하기·숙이기·내 공격을 모두 거침");
            Assert.Greater(Flat(dir.MotionPlayer - C).magnitude, 0.5f, "무사와 싸우며 실제로 움직였음");
        }
    }
}
