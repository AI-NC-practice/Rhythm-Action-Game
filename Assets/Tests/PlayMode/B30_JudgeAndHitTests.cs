using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>B29 피드백 (GDD-beta B12): NORMAL ±240 ms, 늦은 입력은 더 가까운 다음 원으로, 실패하면 맞는 연출 뒤 되감기.</summary>
    public class B30_JudgeAndHitTests
    {
        double now;
        float real;

        // ---------------- B12.1 판정 ----------------

        [UnityTest]
        public IEnumerator B30_LateInput_GoesToCloserNextCue()
        {
            var go = new GameObject("B30");
            var clock = go.AddComponent<BeatClock>();
            now = 0;
            clock.TimeSource = () => now;
            var judge = go.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            clock.StartClock();
            var a = judge.Schedule(10, ArrowKey.Left);
            var b = judge.Schedule(10.5, ArrowKey.Right);      // 반 박 = 0.25초 뒤: 두 원의 NORMAL 창(±0.24초)이 겹친다
            now = clock.TimeOfBeat(9); judge.Tick();
            double ta = clock.TimeOfBeat(10), tb = clock.TimeOfBeat(10.5);
            Assert.AreEqual(0.25, tb - ta, 1e-6);

            // 앞 원에 0.1초 늦음 (다음 원에는 0.15초 이름) → 앞 원 NORMAL
            now = ta + 0.1; judge.Submit(ArrowKey.Left, now);
            Assert.IsTrue(a.Judged); Assert.AreEqual(Judgement.Normal, a.Result, "앞 원에 더 가까우면 앞 원");
            Assert.IsFalse(b.Judged);

            // 새 묶음: 앞 원을 놓치고 다음 원 목표 0.01초 전에 누름 (앞 원에는 0.24초 늦음) → 다음 원 PERFECT, 앞 원은 창이 지나면 FAIL
            var c = judge.Schedule(20, ArrowKey.Left);
            var d = judge.Schedule(20.5, ArrowKey.Right);
            now = clock.TimeOfBeat(19); judge.Tick();
            double tc = clock.TimeOfBeat(20), td = clock.TimeOfBeat(20.5);
            now = td - 0.01; judge.Submit(ArrowKey.Right, now);
            Assert.IsTrue(d.Judged, "다음 원에 더 가까우면 다음 원");
            Assert.AreEqual(Judgement.Perfect, d.Result);
            Assert.IsFalse(c.Judged, "앞 원은 아직 (창 안)");
            now = tc + 0.3; judge.Tick();   // 창(0.24초)이 지난 뒤
            Assert.IsTrue(c.Judged);
            Assert.AreEqual(Judgement.Fail, c.Result, "앞 원은 창이 지나 무입력 FAIL");

            // 늦은 입력이라도 다음 원이 더 멀면 앞 원: +0.2초 → NORMAL (B8.4의 ±180이면 FAIL이던 입력)
            var e = judge.Schedule(30, ArrowKey.Up);
            var f = judge.Schedule(32, ArrowKey.Down);
            now = clock.TimeOfBeat(29); judge.Tick();
            now = clock.TimeOfBeat(30) + 0.2; judge.Submit(ArrowKey.Up, now);
            Assert.AreEqual(Judgement.Normal, e.Result, "+200 ms는 NORMAL (±240)");
            Assert.IsFalse(f.Judged);
            // 키가 한쪽에만 맞으면 그 원: 앞 원(←)에 0.15초 늦었지만 다음 원(→, 0.1초 뒤)이 더 가까워도 ←를 눌렀으면 앞 원 NORMAL
            var g = judge.Schedule(40, ArrowKey.Left);
            var h = judge.Schedule(40.5, ArrowKey.Right);
            now = clock.TimeOfBeat(39); judge.Tick();
            double tg = clock.TimeOfBeat(40), th = clock.TimeOfBeat(40.5);
            now = tg + 0.15; judge.Submit(ArrowKey.Left, now);
            Assert.Less(System.Math.Abs(now - th), 0.15, "다음 원이 시간상 더 가까움");
            Assert.AreEqual(Judgement.Normal, g.Result, "누른 키가 맞는 앞 원");
            Assert.IsFalse(h.Judged);
            Object.Destroy(go);
            yield return null;
        }

        // ---------------- B12.2 맞는 연출 뒤 되감기 ----------------

        GameBootstrap boot;
        StageDirector dir;
        PresentationDirector pd;
        PresentationHooks hooks;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            pd = boot.presentation;
            hooks = Object.FindFirstObjectByType<PresentationHooks>();
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.slowMotion.RealTime = () => real;
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
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.26);   // NORMAL 창 0.24초가 지나야 FAIL
        }

        [UnityTest]
        public IEnumerator B30_DuelFail_HitPresentation_ThenRewind()
        {
            yield return Load(256);
            Assert.AreEqual(0.7f, GameConfig.FailHitSeconds);
            Assert.AreEqual(GameConfig.FailHitSeconds, dir.FailHitSeconds, "기본값");
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            double O = enc.StartBeat;
            var cues = enc.Cues.ToList();
            int rewinds = dir.RewindCount;
            yield return Hit(cues[0], 0.0);
            yield return Hit(cues[1], null);                                  // A8 무입력 FAIL
            Assert.IsTrue(dir.HitPending, "먼저 맞는 연출");
            Assert.IsFalse(dir.Rewinding, "아직 되감지 않음");
            Assert.AreEqual(rewinds, dir.RewindCount);
            Assert.AreEqual(3, dir.Health.Hp, "체력 그대로");
            Assert.IsFalse(boot.judge.Cues.Any(c => !c.Judged), "남은 판정을 바로 지움");
            yield return null;
            Assert.AreEqual(0, boot.ringView.Rings.Count, "원이 사라짐");
            // 연출: 크게 맞음 + 핏자국 + 피 방울 + 소리
            Assert.IsTrue(hooks.Log.Contains("BloodHit"), "맞는 연출");
            Assert.IsTrue(hooks.Log.Contains("BloodDrops"), "피 방울");
            Assert.AreEqual(VfxKind.BloodSplatter, pd.vfxPool.ActiveFlash, "화면 핏자국");
            CollectionAssert.Contains(pd.sfx.History, SfxKind.BodyHit);
            pd.screenFx.Step(0f);
            Assert.Greater(pd.screenFx.VignetteIntensity, 0.4f, "붉은 테두리");
            pd.cameraFx.Step(0.05f);
            Assert.Less(pd.cameraFx.Position.z, -0.15f, "카메라가 뒤로 밀림");
            // 시계는 계속 흐름
            double b0 = boot.clock.CurrentBeat;
            now += 0.2; yield return null;
            Assert.Greater(boot.clock.CurrentBeat, b0, "맞는 동안 곡은 계속");
            real += 0.69f; yield return null;
            Assert.IsTrue(dir.HitPending, "0.7초 전에는 계속 맞는 연출");
            real += 0.02f; yield return null;
            Assert.IsFalse(dir.HitPending);
            Assert.IsTrue(dir.Rewinding, "0.7초 뒤 되감기");
            Assert.AreEqual(RewindKind.Boss, dir.LastRewind);
            real += GameConfig.RewindSeconds + 0.01f; yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.AreEqual(O + BossDuel.PhaseStart(BossDuel.PhaseExplore), boot.clock.CurrentBeat, 0.05, "그 단계 처음으로");
            Assert.AreEqual(BossDuel.Steps().Count, enc.Cues.Count(c => !c.Judged), "다시 예약");
            Assert.AreEqual(3, dir.Health.Hp);
        }

        [UnityTest]
        public IEnumerator B30_WindmillFail_HitPresentation_ThenRewind()
        {
            yield return Load(181);
            yield return UntilCombat();
            var cues = dir.CurrentEncounter.Cues.ToList();
            foreach (var c in cues.Where(c => ((CombatStep)c.Tag).Phase < 3)) yield return Hit(c, 0.0);
            var p3 = cues.Where(c => ((CombatStep)c.Tag).Phase == 3).ToList();
            yield return Hit(p3[0], null);
            Assert.IsTrue(dir.HitPending, "윈드밀도 맞는 연출 먼저");
            Assert.IsFalse(dir.Rewinding);
            Assert.IsTrue(hooks.Log.Contains("BloodHit"));
            real += GameConfig.FailHitSeconds + 0.01f; yield return null;
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Windmill, dir.LastRewind);
        }
    }
}
