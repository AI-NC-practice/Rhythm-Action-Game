using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    public class T02_BeatAndSpeedTests
    {
        const float Eps = 1e-4f;

        // ---------- SpeedRules (GDD 4.1 표) ----------
        [Test]
        public void T02_Perfect_AddsFivePercent()
        {
            Assert.AreEqual(1.05f, SpeedRules.Apply(1.00f, Judgement.Perfect), Eps);
            Assert.AreEqual(1.20f, SpeedRules.Apply(1.15f, Judgement.Perfect), Eps);
        }

        [Test]
        public void T02_Perfect_CapsAtMax()
        {
            Assert.AreEqual(1.30f, SpeedRules.Apply(1.25f, Judgement.Perfect), Eps);
            Assert.AreEqual(1.30f, SpeedRules.Apply(1.28f, Judgement.Perfect), Eps);
            Assert.AreEqual(1.30f, SpeedRules.Apply(1.30f, Judgement.Perfect), Eps);
        }

        [Test]
        public void T02_Normal_KeepsSpeed()
        {
            Assert.AreEqual(1.00f, SpeedRules.Apply(1.00f, Judgement.Normal), Eps);
            Assert.AreEqual(1.15f, SpeedRules.Apply(1.15f, Judgement.Normal), Eps);
            Assert.AreEqual(1.30f, SpeedRules.Apply(1.30f, Judgement.Normal), Eps);
        }

        [Test]
        public void T02_Fail_MultipliesByPoint8_NotBelowMin()
        {
            Assert.AreEqual(1.04f, SpeedRules.Apply(1.30f, Judgement.Fail), Eps); // 1.30 × 0.8
            Assert.AreEqual(1.00f, SpeedRules.Apply(1.20f, Judgement.Fail), Eps); // 0.96 → 1.00
            Assert.AreEqual(1.00f, SpeedRules.Apply(1.00f, Judgement.Fail), Eps);
            Assert.AreEqual(1.00f, SpeedRules.Apply(1.25f, Judgement.Fail), Eps); // 1.00 경계
        }

        [Test]
        public void T02_SixPerfects_ReachExactlyMax()
        {
            var s = new SpeedState();
            for (int i = 0; i < 6; i++) s.Apply(Judgement.Perfect);
            Assert.AreEqual(1.30f, s.Speed, Eps);
            Assert.AreEqual(1.30f, s.MaxSpeed, Eps);
            s.Apply(Judgement.Fail);
            Assert.AreEqual(1.04f, s.Speed, Eps);
            Assert.AreEqual(1.30f, s.MaxSpeed, Eps, "최고 속도는 유지");
        }

        // ---------- 1박자 거리 ----------
        [Test]
        public void T02_DistancePerBeat_Is2_5m_ForAnySpeed()
        {
            foreach (var s in new[] { 1.00f, 1.05f, 1.10f, 1.17f, 1.25f, 1.30f })
            {
                Assert.AreEqual(5f * s, SpeedRules.MoveSpeed(s), Eps, $"s={s} 이동 속도");
                Assert.AreEqual(2.5f, SpeedRules.DistanceForBeats(1f, s), Eps, $"s={s} 1박자 거리");
                Assert.AreEqual(10f, SpeedRules.DistanceForBeats(4f, s), Eps, $"s={s} 4박자 거리");
            }
        }

        // ---------- BeatTimeline ----------
        [Test]
        public void T02_Timeline_CountsBeatsAt120Bpm()
        {
            var t = new BeatTimeline();
            t.Start(10.0);
            Assert.AreEqual(0.0, t.BeatAt(10.0), 1e-9);
            Assert.AreEqual(1.0, t.BeatAt(10.5), 1e-9);
            Assert.AreEqual(4.0, t.BeatAt(12.0), 1e-9);
        }

        [Test]
        public void T02_Timeline_SpeedChange_ContinuesBeat()
        {
            var t = new BeatTimeline();
            t.Start(0.0);
            t.SetSpeed(1.0f, 0.0);
            Assert.AreEqual(2.0, t.BeatAt(1.0), 1e-6);
            t.SetSpeed(1.25f, 1.0);                       // 1초 지점에서 1.25배
            Assert.AreEqual(2.0, t.BeatAt(1.0), 1e-6, "속도 바꾼 순간 박자 끊김 없음");
            Assert.AreEqual(4.5, t.BeatAt(2.0), 1e-6);    // 1초 동안 2.5박
            Assert.AreEqual(2.2, t.TimeOfBeat(5.0, 2.0), 1e-6); // 0.5박 남음 × 0.4초/박 = 0.2초
        }

        [Test]
        public void T02_Timeline_TimeOfBeat()
        {
            var t = new BeatTimeline();
            t.Start(0.0);
            t.SetSpeed(1.25f, 0.0);
            // 1.25배에서 1박 = 0.4초
            Assert.AreEqual(0.4, t.TimeOfBeat(1.0, 0.0), 1e-6);
            Assert.AreEqual(1.6, t.TimeOfBeat(4.0, 0.0), 1e-6);
        }

        [Test]
        public void T02_Timeline_PauseResume_BeatContinues()
        {
            var t = new BeatTimeline();
            t.Start(0.0);
            Assert.AreEqual(3.0, t.BeatAt(1.5), 1e-6);
            t.Pause(1.5);
            Assert.AreEqual(3.0, t.BeatAt(5.0), 1e-6, "일시정지 중 박자 멈춤");
            t.Resume(10.0);
            Assert.AreEqual(3.0, t.BeatAt(10.0), 1e-6, "재개 순간 멈춘 박자에서 이어짐");
            Assert.AreEqual(4.0, t.BeatAt(10.5), 1e-6);
        }

        // ---------- 메트로놈 ----------
        [Test]
        public void T02_Click_AccentEvery4Beats()
        {
            Assert.IsTrue(ClickSynth.IsAccent(0));
            Assert.IsFalse(ClickSynth.IsAccent(1));
            Assert.IsFalse(ClickSynth.IsAccent(2));
            Assert.IsFalse(ClickSynth.IsAccent(3));
            Assert.IsTrue(ClickSynth.IsAccent(4));
            Assert.IsTrue(ClickSynth.IsAccent(8));
        }

        [Test]
        public void T02_Click_WaveformsAreAudibleAndAccentLouder()
        {
            var normal = ClickSynth.Create(false);
            var accent = ClickSynth.Create(true);
            Assert.Greater(normal.Length, 0);
            float peakN = 0, peakA = 0;
            foreach (var v in normal) peakN = Mathf.Max(peakN, Mathf.Abs(v));
            foreach (var v in accent) peakA = Mathf.Max(peakA, Mathf.Abs(v));
            Assert.Greater(peakN, 0.1f);
            Assert.Greater(peakA, peakN);
            var clip = ClickSynth.CreateClip(true);
            Assert.AreEqual(accent.Length, clip.samples);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void T02_Metronome_SchedulesEveryBeat_AccentOnBar()
        {
            var go = new GameObject("Metro");
            go.AddComponent<AudioSource>();
            var clock = go.AddComponent<BeatClock>();
            var metro = go.AddComponent<Metronome>();
            metro.clock = clock;
            double now = 0;
            clock.TimeSource = () => now;
            try
            {
                clock.StartClock();
                var accents = new System.Collections.Generic.List<bool>();
                // 0.02초 간격으로 4초(8박) 진행
                for (int i = 0; i <= 200; i++)
                {
                    now = i * 0.02;
                    int before = metro.ScheduledCount;
                    metro.Tick();
                    if (metro.ScheduledCount > before) accents.Add(metro.LastWasAccent);
                }
                Assert.AreEqual(8, metro.ScheduledCount, "4초 동안 박 1~8 예약");
                Assert.AreEqual(8, metro.LastScheduledBeat);
                // 박 1,2,3 보통 / 4 강세 / 5,6,7 보통 / 8 강세
                CollectionAssert.AreEqual(new[] { false, false, false, true, false, false, false, true }, accents);
            }
            finally { Object.DestroyImmediate(go); }
        }

        // ---------- BeatClock: 음악 속도와 이동 속도에 함께 적용 ----------
        [Test]
        public void T02_BeatClock_AppliesSpeedToMusicAndMovement()
        {
            var go = new GameObject("Clock");
            var music = go.AddComponent<AudioSource>();
            var clock = go.AddComponent<BeatClock>();
            clock.music = music;
            double now = 0;
            clock.TimeSource = () => now;
            try
            {
                // Awake는 EditMode AddComponent에서 호출되지 않을 수 있으므로 기본 상태를 직접 확인
                clock.ApplyJudgement(Judgement.Perfect);
                clock.ApplyJudgement(Judgement.Perfect);
                Assert.AreEqual(1.10f, clock.TargetSpeed, Eps);
                Assert.AreEqual(1.00f, clock.SpeedMultiplier, Eps, "GDD-beta B15.2: 다음 마디 첫 박까지 기다림");
                clock.ApplyPendingSpeedNow();
                Assert.AreEqual(1.10f, clock.SpeedMultiplier, Eps);
                Assert.AreEqual(1.10f, music.pitch, Eps, "음악 재생 속도 = s");
                Assert.AreEqual(5.5f, clock.MoveSpeed, Eps, "이동 속도 = 5 × s");

                clock.ApplyJudgement(Judgement.Fail);
                clock.ApplyPendingSpeedNow();
                Assert.AreEqual(1.00f, clock.SpeedMultiplier, Eps);
                Assert.AreEqual(1.00f, music.pitch, Eps);
                Assert.AreEqual(5.0f, clock.MoveSpeed, Eps);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void T02_BeatClock_PauseResume_UsesInjectedTime()
        {
            var go = new GameObject("Clock");
            var clock = go.AddComponent<BeatClock>();
            double now = 100;
            clock.TimeSource = () => now;
            try
            {
                clock.StartClock();
                now = 101;
                Assert.AreEqual(2.0, clock.CurrentBeat, 1e-6);
                clock.Pause();
                now = 200;
                Assert.AreEqual(2.0, clock.CurrentBeat, 1e-6);
                clock.Resume();
                now = 200.5;
                Assert.AreEqual(3.0, clock.CurrentBeat, 1e-6);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
