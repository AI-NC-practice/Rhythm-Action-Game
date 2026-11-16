using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>무사 결투 사건과 각도 (GDD-beta B13.4): 자리 바꾸기, 옆차기 다운(바닥 시점), 컷 각도.</summary>
    public class B34_DuelEventsTests
    {
        const float Dt = 0.02f;
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        DuelCamera duel;
        PresentationHooks hooks;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            duel = Object.FindFirstObjectByType<DuelCamera>();
            hooks = Object.FindFirstObjectByType<PresentationHooks>();
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.slowMotion.RealTime = () => real;
            dir.MotionDeltaTime = () => Dt;
            duel.RealTime = () => real;
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;   // 일정한 빠르기로 입력 시각을 미리 계산하는 테스트 (회피 슬로모션은 B47 테스트)
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
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue c)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.02);
            now = boot.clock.TimeOfBeat(c.TargetBeat);
            boot.judge.Submit(c.Key, now);
            yield return null;
        }

        IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
        double Rel(StageDirector.Encounter e) => boot.clock.CurrentBeat - e.StartBeat;

        [UnityTest]
        public IEnumerator B34_Duel_Angles_Swap_KickDown_TopDown()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            double O = enc.StartBeat;
            var cues = enc.Cues.ToList();
            Assert.AreEqual(45, cues.Count, "결투 판정 45 (B13.4)");

            // 컷 A 로우 앵글: 낮은 카메라가 위를 올려다봄
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + 1.0));
            yield return null;
            Assert.AreEqual("A", duel.Cut);
            var ca = duel.CutCams["A"].transform;
            Assert.Less(ca.position.y, duel.CutLookAt.y - 0.8f, "카메라가 바라보는 점보다 낮음");
            Assert.Greater(ca.forward.y, 0.15f, "위를 올려다봄");

            for (int i = 0; i < 13; i++) yield return Hit(cues[i]);          // 1단계
            // 컷 B 칼날 클로즈업 (1.2 m)
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + 24.3));
            yield return null;
            Assert.AreEqual("B", duel.Cut);
            Assert.AreEqual(1.2f, Flat(duel.CutCams["B"].transform.position - duel.CutLookAt).magnitude, 0.05f, "칼날 옆 1.2 m");

            // 자리 바꾸기: 받아치기 A37.5 성공 → 두 사람 자리가 180° 바뀜
            int swapIdx = cues.FindIndex(c => ((CombatStep)c.Tag).OffsetBeats == BossDuel.SwapBeat);
            for (int i = 13; i < swapIdx; i++) yield return Hit(cues[i]);
            var pBefore = dir.MotionPlayer; var eBefore = dir.MotionEnemy;
            yield return Hit(cues[swapIdx]);
            Assert.IsTrue(enc.Swapped, "자리 바꾸기");
            yield return Frames(Mathf.CeilToInt(GameConfig.DuelSwapSeconds / Dt) + 3);
            Assert.Less(Flat(dir.MotionPlayer - eBefore).magnitude, 0.3f, "플레이어가 무사가 있던 자리로");
            Assert.Less(Flat(dir.MotionEnemy - pBefore).magnitude, 0.6f, "무사가 플레이어가 있던 자리 쪽으로");
            Assert.IsNull(duel.Cut, "원이 떠 있으면 어깨 너머 컷 없이 1인칭 그대로 바뀜");

            // 옆차기 다운: 2단계 마지막(A52) 뒤 판정 없이 맞고 쓰러짐
            int kickIdx = cues.FindIndex(c => ((CombatStep)c.Tag).OffsetBeats == BossDuel.KickAfterBeat);
            for (int i = swapIdx + 1; i < kickIdx; i++) yield return Hit(cues[i]);
            yield return Hit(cues[kickIdx]);
            Assert.IsTrue(enc.Kicked, "옆차기에 맞음");
            Assert.AreEqual(3, dir.Health.Hp, "실패가 아님: 체력 그대로");
            Assert.IsTrue(hooks.Log.Contains("KickHit"), "맞는 연출");
            Assert.AreEqual("Kick", dir.boss.State);
            yield return null;
            Assert.AreEqual("K", duel.Cut, "옆차기 측면 컷");
            Assert.IsFalse(duel.Brain.IsBlending, "바로 자름");
            Assert.AreSame(duel.CutCams["K"], duel.Brain.ActiveVirtualCamera as CinemachineCamera);
            yield return AdvanceTo(now + GameConfig.DuelKickCutSeconds + 0.01);   // 컷 길이는 박자 시계 기준
            yield return null;
            Assert.AreNotEqual("K", duel.Cut, "0.25초 뒤 1인칭");
            Assert.IsFalse(duel.Brain.IsBlending, "돌아올 때도 바로 자름");
            Assert.AreEqual(1f, duel.DownAmount, 1e-3f, "바닥에 누움");
            yield return new WaitForSecondsRealtime(0.4f);              // 1인칭 카메라 사이 블렌드가 끝나게
            var cam = boot.playerCamera.transform;
            float ground = boot.player.transform.position.y;
            Assert.AreEqual(GameConfig.DuelDownEyeHeight, cam.position.y - ground, 0.12f, "바닥 0.3 m 높이 시점");
            Assert.Greater(cam.forward.y, 0.3f, "위를 올려다봄");

            // 다운 구간 판정 3개: 56 ← 57 → 58 ↑
            var down = cues.Where(c => ((CombatStep)c.Tag).Phase == BossDuel.PhaseDown).ToList();
            CollectionAssert.AreEqual(new[] { ArrowKey.Left, ArrowKey.Right, ArrowKey.Up }, down.Select(c => c.Key));
            CollectionAssert.AreEqual(new[] { 56.0, 57.0, 58.0 }, down.Select(c => c.TargetBeat - O));
            foreach (var c in down)
            {
                yield return Hit(c);
                Assert.AreEqual(Judgement.Perfect, c.Result);
                Assert.Greater(duel.DownAmount, 0.9f, $"{c.TargetBeat - O}: 누운 채 판정");
            }
            // 59까지 일어남
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + BossDuel.StandUpTo + 0.05));
            yield return null;
            Assert.AreEqual(0f, duel.DownAmount, 1e-3f, "59박까지 일어남");
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(GameConfig.EyeHeight + GameConfig.DuelGuardCamHeight, boot.playerCamera.transform.position.y - boot.player.transform.position.y, 0.15f, "눈높이로 돌아옴");

            // 나머지 → 마무리 → 처형 컷 D 마지막 0.5초는 위에서 내려다봄
            var rest = cues.Where(c => !c.Judged).ToList();
            foreach (var c in rest) yield return Hit(c);
            Assert.IsTrue(enc.Done);
            yield return AdvanceTo(boot.clock.TimeOfBeat(O + BossDuel.LengthBeats) - 0.2);
            yield return null;
            Assert.AreEqual("D", duel.Cut);
            Assert.IsTrue(duel.CutTopDown, "마지막 0.5초: 위에서");
            Assert.Less(duel.CutCams["D"].transform.forward.y, -0.9f, "아래를 내려다봄");
            Assert.Greater(duel.CutCams["D"].transform.position.y - boot.player.transform.position.y, 5f, "높은 곳");
        }

        [UnityTest]
        public IEnumerator B34_Swap_ShoulderCut_WhenNoRing()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            int swapIdx = cues.FindIndex(c => ((CombatStep)c.Tag).OffsetBeats == BossDuel.SwapBeat);
            for (int i = 0; i < swapIdx; i++) yield return Hit(cues[i]);
            // 원이 없는 틈을 만든다: 받아치기 뒤 판정을 모두 지움
            yield return AdvanceTo(boot.clock.TimeOfBeat(cues[swapIdx].TargetBeat) - 0.02);
            foreach (var c in cues.Skip(swapIdx + 1)) boot.judge.Cancel(c);
            now = boot.clock.TimeOfBeat(cues[swapIdx].TargetBeat);
            boot.judge.Submit(cues[swapIdx].Key, now);
            yield return null;
            yield return null;
            Assert.AreEqual("S", duel.Cut, "원이 없는 틈: 어깨 너머 컷");
            Assert.IsFalse(duel.Brain.IsBlending, "바로 자름");
            var s = duel.CutCams["S"].transform;
            var f = Flat(dir.boss.transform.position - boot.player.transform.position).normalized;
            Assert.Less(Vector3.Dot(Flat(s.position - boot.player.transform.position), f), -0.5f, "플레이어 뒤");
            Assert.Greater(Vector3.Dot(s.forward, (dir.boss.transform.position + Vector3.up * 1.2f - s.position).normalized), 0.95f, "무사(가슴 높이)를 바라봄");
            Assert.IsTrue(duel.ThirdPerson);
            yield return AdvanceTo(now + GameConfig.DuelSwapCutSeconds + 0.01);
            yield return null;
            Assert.AreNotEqual("S", duel.Cut, "0.6초 뒤 1인칭");
        }
    }
}
