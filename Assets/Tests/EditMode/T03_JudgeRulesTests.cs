using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    public class T03_JudgeRulesTests
    {
        const double Target = 10.0;

        static bool Judge(double deltaMs, ArrowKey pressed, out Judgement r, float offset = 0f, ArrowKey expected = ArrowKey.Up) =>
            JudgeRules.TryJudge(Target + offset / 1000.0 + deltaMs / 1000.0, Target, offset, pressed, expected, out r);

        [TestCase(0.0)]
        // beta1-1: GDD-beta B8.4 판정 창 PERFECT ±90, NORMAL ±180 ms (GDD 4.2의 ±60/±150 대신)
        [TestCase(89.9)]
        [TestCase(-89.9)]
        public void T03_WithinPerfectWindow_IsPerfect(double ms)
        {
            Assert.IsTrue(Judge(ms, ArrowKey.Up, out var r));
            Assert.AreEqual(Judgement.Perfect, r);
        }

        [TestCase(90.1)]
        [TestCase(-90.1)]
        [TestCase(179.9)]
        [TestCase(-179.9)]
        public void T03_WithinNormalWindow_IsNormal(double ms)
        {
            Assert.IsTrue(Judge(ms, ArrowKey.Up, out var r));
            Assert.AreEqual(Judgement.Normal, r);
        }

        [TestCase(-180.1)]
        [TestCase(-200.0)]
        [TestCase(-299.9)]
        public void T03_EarlyBetween150And300_IsFail(double ms)
        {
            Assert.IsTrue(Judge(ms, ArrowKey.Up, out var r));
            Assert.AreEqual(Judgement.Fail, r);
        }

        [TestCase(-300.1)]
        [TestCase(-1000.0)]
        public void T03_EarlierThan300_IsIgnored(double ms)
        {
            Assert.IsFalse(Judge(ms, ArrowKey.Up, out _));
            Assert.IsFalse(Judge(ms, ArrowKey.Down, out _), "다른 키라도 너무 이르면 무시");
        }

        [TestCase(0.0)]
        [TestCase(100.0)]
        [TestCase(-250.0)]
        public void T03_WrongKey_IsFail(double ms)
        {
            Assert.IsTrue(Judge(ms, ArrowKey.Left, out var r));
            Assert.AreEqual(Judgement.Fail, r);
        }

        [Test]
        public void T03_NoInputUntilPlus150_IsMissed()
        {
            Assert.IsFalse(JudgeRules.IsMissed(Target + 0.1799, Target, 0f));
            Assert.IsTrue(JudgeRules.IsMissed(Target + 0.1801, Target, 0f));
            Assert.IsFalse(Judge(180.1, ArrowKey.Up, out _), "창이 지난 입력은 판정 대상이 아님(이미 FAIL)");
        }

        [Test]
        public void T03_TimingOffset_IsAddedToTarget()
        {
            // 보정 +50 ms: 목표가 10.050초로 밀린다
            Assert.AreEqual(0.0, JudgeRules.DeltaMs(10.050, Target, 50f), 1e-6);
            Assert.IsTrue(JudgeRules.TryJudge(10.050, Target, 50f, ArrowKey.Up, ArrowKey.Up, out var r1));
            Assert.AreEqual(Judgement.Perfect, r1);
            // 보정 없이 10.050초면 +50 ms → PERFECT 이지만, 10.120초면 보정 없을 때 NORMAL, +50 보정이면 PERFECT(+70 → NORMAL 아님 확인)
            Assert.IsTrue(JudgeRules.TryJudge(10.120, Target, 0f, ArrowKey.Up, ArrowKey.Up, out var r2));
            Assert.AreEqual(Judgement.Normal, r2);
            Assert.IsTrue(JudgeRules.TryJudge(10.100, Target, 50f, ArrowKey.Up, ArrowKey.Up, out var r3));
            Assert.AreEqual(Judgement.Perfect, r3);
            // 보정 −100 ms: 9.900초가 정확한 목표
            Assert.IsTrue(JudgeRules.TryJudge(9.900, Target, -100f, ArrowKey.Up, ArrowKey.Up, out var r4));
            Assert.AreEqual(Judgement.Perfect, r4);
            Assert.IsTrue(JudgeRules.IsMissed(10.081, Target, -100f));
        }

        [Test]
        public void T03_SettingsOffset_ClampedAndSnappedTo5ms()
        {
            Assert.AreEqual(-200, GameSettings.ClampOffset(-500));
            Assert.AreEqual(200, GameSettings.ClampOffset(999));
            Assert.AreEqual(15, GameSettings.ClampOffset(14));
            Assert.AreEqual(10, GameSettings.ClampOffset(12));
            Assert.AreEqual("+15ms", GameSettings.FormatOffset(15));
            Assert.AreEqual("-20ms", GameSettings.FormatOffset(-20));
            Assert.AreEqual("0ms", GameSettings.FormatOffset(0));
        }

        [Test]
        public void T03_OuterRing_Shrinks360To120OverTwoBeats()
        {
            Assert.AreEqual(360f, JudgeRules.OuterRingDiameter(2.0), 1e-3f);
            Assert.AreEqual(300f, JudgeRules.OuterRingDiameter(1.5), 1e-3f);
            Assert.AreEqual(240f, JudgeRules.OuterRingDiameter(1.0), 1e-3f);
            Assert.AreEqual(120f, JudgeRules.OuterRingDiameter(0.0), 1e-3f);
            Assert.AreEqual(360f, JudgeRules.OuterRingDiameter(3.0), 1e-3f);
            Assert.AreEqual(120f, JudgeRules.OuterRingDiameter(-1.0), 1e-3f);
        }

        [Test]
        public void T03_Labels()
        {
            Assert.AreEqual("PERFECT", JudgeRules.Label(Judgement.Perfect));
            Assert.AreEqual("NORMAL", JudgeRules.Label(Judgement.Normal));
            Assert.AreEqual("FAIL", JudgeRules.Label(Judgement.Fail));
        }

        // ---------- JudgeSystem (시간 주입) ----------
        static (GameObject go, BeatClock clock, JudgeSystem judge) MakeSystem(System.Func<double> time)
        {
            var go = new GameObject("Judge");
            var clock = go.AddComponent<BeatClock>();
            clock.TimeSource = time;
            var judge = go.AddComponent<JudgeSystem>();
            judge.clock = clock;
            judge.readKeyboard = false;
            judge.OffsetSource = () => 0f;
            clock.StartClock();
            return (go, clock, judge);
        }

        [Test]
        public void T03_JudgeSystem_AppearsTwoBeatsBefore_PerfectRaisesSpeed()
        {
            double now = 0;
            var (go, clock, judge) = MakeSystem(() => now);
            try
            {
                var cue = judge.Schedule(4, ArrowKey.Up);
                now = 0.99; judge.Tick();          // 1.98박
                Assert.IsFalse(cue.Appeared);
                now = 1.0; judge.Tick();           // 2박 = 목표 2박 전
                Assert.IsTrue(cue.Appeared);
                Assert.IsTrue(judge.HasActiveCue);
                now = 2.0; judge.Submit(ArrowKey.Up, now);
                Assert.IsTrue(cue.Judged);
                Assert.AreEqual(Judgement.Perfect, cue.Result);
                Assert.AreEqual(1.05f, clock.SpeedMultiplier, 1e-4f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void T03_JudgeSystem_NoInput_FailsAfter150ms()
        {
            double now = 0;
            var (go, clock, judge) = MakeSystem(() => now);
            int judged = 0;
            judge.CueJudged += (c, r) => { judged++; Assert.AreEqual(Judgement.Fail, r); };
            try
            {
                var cue = judge.Schedule(4, ArrowKey.Left);
                now = 2.179; judge.Tick();   // beta1-1: GDD-beta B8.4 +180 ms
                Assert.IsFalse(cue.Judged);
                now = 2.181; judge.Tick();
                Assert.IsTrue(cue.Judged);
                Assert.AreEqual(1, judged);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void T03_JudgeSystem_EarlyPressIgnored_ThenJudged()
        {
            double now = 0;
            var (go, clock, judge) = MakeSystem(() => now);
            try
            {
                var cue = judge.Schedule(4, ArrowKey.Down);
                now = 1.5; judge.Tick();
                judge.Submit(ArrowKey.Down, 1.5);   // −500 ms: 무시
                Assert.IsFalse(cue.Judged);
                judge.Submit(ArrowKey.Down, 2.1);   // +100 ms: NORMAL
                Assert.AreEqual(Judgement.Normal, cue.Result);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void T03_NoLegacyInputManager_InScripts()
        {
            var legacy = new Regex(@"\bInput\.(GetKey|GetKeyDown|GetKeyUp|GetAxis|GetAxisRaw|GetButton|GetButtonDown|GetMouseButton|mousePosition)\b");
            var offenders = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories)
                .Where(f => legacy.IsMatch(File.ReadAllText(f))).ToArray();
            CollectionAssert.IsEmpty(offenders, "구 Input Manager 사용: " + string.Join(", ", offenders));
            StringAssert.Contains("Keyboard.current", File.ReadAllText("Assets/Scripts/Rhythm/JudgeSystem.cs"));
        }
    }
}
