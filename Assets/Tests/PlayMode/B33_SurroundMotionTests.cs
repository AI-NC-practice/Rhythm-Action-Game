using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>포위 전투 움직임 (GDD-beta B13.3): 둘레를 돌다 뛰어듦, 동시 공격 덮치기, 윈드밀은 그대로.</summary>
    public class B33_SurroundMotionTests
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

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        static int Phase(JudgeCue c) => ((CombatStep)c.Tag).Phase;
        static int[] Who(JudgeCue c) => ((CombatStep)c.Tag).Attackers;

        IEnumerator HitAt(JudgeCue c)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
            now = boot.clock.TimeOfBeat(c.TargetBeat);
            boot.judge.Submit(c.Key, now);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B33_Surround_Orbit_Lunge_GangUp_Windmill()
        {
            yield return Load(181);
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) { now += 0.01; yield return null; }
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.Event.IsSurround);
            Assert.IsTrue(dir.SurroundMotionActive, "포위 전투 움직임");
            var C = dir.MotionCenter;
            var a0 = enc.Actors[0];

            // 둘레를 돈다: 원이 아직 없을 때 각도가 바뀌고 반지름은 4 m
            var d0 = Flat(a0.transform.position - C);
            for (int i = 0; i < 10; i++) yield return null;
            var d1 = Flat(a0.transform.position - C);
            Assert.Greater(Vector3.Angle(d0, d1), 1f, "둘레를 돎");
            Assert.AreEqual(GameConfig.SurroundRadius, d1.magnitude, 0.05f, "반지름 4 m");
            Assert.AreEqual(30f, GameConfig.SurroundCircleDegPerSec);

            var cues = enc.Cues.ToList();
            var p1 = cues.Where(c => Phase(c) == 1).ToList();
            // 1단계: 공격하는 적만 1.5 m까지 뛰어듦
            var first = p1[0];
            int k = Who(first)[0];
            yield return AdvanceTo(boot.clock.TimeOfBeat(first.TargetBeat) - 0.01);
            yield return null;
            var atk = enc.Actors[k];
            Assert.AreEqual(GameConfig.SurroundLungeMeters, Flat(atk.transform.position - dir.MotionPlayer).magnitude, 0.15f, "공격하는 적이 1.5 m까지 뛰어듦");
            for (int j = 0; j < enc.Actors.Length; j++)
                if (j != k && System.Array.IndexOf(Who(p1[1]), j) < 0)
                    Assert.Greater(Flat(enc.Actors[j].transform.position - C).magnitude, 3.5f, $"적 {j}는 원에 남음");
            var p0 = dir.MotionPlayer;
            now = boot.clock.TimeOfBeat(first.TargetBeat);
            boot.judge.Submit(first.Key, now);
            yield return null;
            Assert.AreEqual(Judgement.Perfect, first.Result);
            for (int i = 0; i < 25; i++) yield return null;             // 0.5초
            Assert.AreEqual(GameConfig.SurroundRadius, Flat(atk.transform.position - C).magnitude, 0.3f, "판정 뒤 원으로 돌아감");
            if (first.Key != ArrowKey.Down && first.Key != ArrowKey.Up || first.Result == Judgement.Perfect)
                Assert.Greater(Flat(dir.MotionPlayer - p0).magnitude, 0.05f, "판정마다 플레이어가 움직임 (B13.1 표)");
            Assert.LessOrEqual(Flat(dir.MotionPlayer - C).magnitude, GameConfig.SurroundPlayerRadius + 1e-3f, "중심 1.2 m 안");
            Assert.Less(Flat(boot.player.transform.position - dir.MotionPlayer).magnitude, 1e-3f, "플레이어가 실제로 그 자리");
            for (int i = 1; i < p1.Count; i++) yield return HitAt(p1[i]);

            // 2단계: 동시 공격하는 적들이 한꺼번에 덮침
            var p2 = cues.Where(c => Phase(c) == 2).ToList();
            foreach (var c in p2)
            {
                yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.01);
                yield return null;
                Assert.GreaterOrEqual(Who(c).Length, 2);
                foreach (var w in Who(c))
                    Assert.AreEqual(GameConfig.SurroundLungeMeters, Flat(enc.Actors[w].transform.position - dir.MotionPlayer).magnitude, 0.15f, $"동시 공격 적 {w} 덮침");
                now = boot.clock.TimeOfBeat(c.TargetBeat);
                boot.judge.Submit(c.Key, now);
                yield return null;
                Assert.AreEqual(Judgement.Perfect, c.Result);
            }

            // 3단계 윈드밀: 돌기를 멈추고 그대로 마무리
            var wind = cues.Where(c => Phase(c) == 3).ToList();
            yield return AdvanceTo(boot.clock.TimeOfBeat(wind[0].TargetBeat) - 0.05);
            yield return null;
            float o0 = dir.SurroundOrbitDegrees;
            for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(o0, dir.SurroundOrbitDegrees, 1e-4f, "윈드밀 화살표가 떠 있는 동안 돌기 멈춤");
            foreach (var c in wind) yield return HitAt(c);
            Assert.IsTrue(enc.Done);
            Assert.AreEqual(ExecutionKind.Windmill, enc.Execution, "윈드밀 그대로");
            yield return null;
            Assert.IsFalse(dir.SurroundMotionActive);
            for (int i = 0; i < 25; i++) yield return null;
            Assert.AreEqual(Vector3.zero, boot.player.CombatOffset, "경로 위 자리로 돌아옴");
        }
    }
}
