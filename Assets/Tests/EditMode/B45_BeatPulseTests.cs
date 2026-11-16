using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>화면이 박에 맞춰 숨 쉼 (GDD-beta B17.2): 박 펄스, 예고음 박, 걸음 출렁임, 예고음 설정.</summary>
    public class B45_BeatPulseTests
    {
        [Test]
        public void B45_Pulse_PeaksOnBeat_BarStartBigger()
        {
            Assert.AreEqual(1f, BeatPulse.Envelope(5.0), 1e-5f, "박 순간 가장 큼");
            Assert.AreEqual(0f, BeatPulse.Envelope(5.0 + GameConfig.BeatFx.PulseBeats), 1e-5f, "PulseBeats 뒤 0");
            Assert.AreEqual(0f, BeatPulse.Envelope(5.6), 1e-5f, "다음 박 전까지 0");
            Assert.Greater(BeatPulse.Envelope(5.05), BeatPulse.Envelope(5.15), "박 뒤로 줄어듦");
            Assert.Greater(BeatPulse.Envelope(5.15), BeatPulse.Envelope(5.25));

            Assert.IsTrue(BeatPulse.IsBarStart(0) && BeatPulse.IsBarStart(4.2) && BeatPulse.IsBarStart(-4));
            Assert.IsFalse(BeatPulse.IsBarStart(5) || BeatPulse.IsBarStart(3.9));
            Assert.AreEqual(GameConfig.BeatFx.BarAccent, BeatPulse.Amount(8.0), 1e-5f, "마디 첫 박은 더 크게");
            Assert.AreEqual(1f, BeatPulse.Amount(9.0), 1e-5f, "보통 박");
            Assert.Greater(GameConfig.BeatFx.BarAccent, 1f);
            Assert.AreEqual(1f + GameConfig.BeatFx.BarAccent * GameConfig.BeatFx.RingScale, BeatPulse.Scale(12.0, GameConfig.BeatFx.RingScale), 1e-5f);
            Assert.AreEqual(1f, BeatPulse.Scale(12.5, GameConfig.BeatFx.RingScale), 1e-5f, "박 사이엔 원래 크기");
            Assert.AreEqual(1f, BeatPulse.Envelope(-3.0), 1e-5f, "음수 박자도 같은 규칙");
        }

        [Test]
        public void B45_TickBeats_TwoAndOneBeatBefore_WithinApproach()
        {
            CollectionAssert.AreEqual(new[] { 8.0, 9.0 }, BeatPulse.TickBeats(10).ToArray(), "2박 전·1박 전");
            CollectionAssert.AreEqual(new[] { 8.5, 9.5 }, BeatPulse.TickBeats(10.5).ToArray(), "반 박 판정은 반 박 자리에서 셈");
            Assert.AreEqual(2f, GameConfig.ApproachBeats, "판정 원이 떠 있는 2박 동안 남은 박마다");
            foreach (var b in BeatPulse.TickBeats(20))
                Assert.GreaterOrEqual(b, 20 - GameConfig.ApproachBeats - 1e-9, "원이 뜨기 전 박은 없음");
        }

        [Test]
        public void B45_Step_OneBeatOneStep_LowestOnBeat_FeetAlternate()
        {
            Assert.AreEqual(-GameConfig.BeatFx.StepDropMeters, BeatPulse.StepHeight(6.0), 1e-5f, "박 순간 가장 낮음 (발이 닿음)");
            Assert.AreEqual(0f, BeatPulse.StepHeight(6.5), 1e-5f, "반 박에서 가장 높음");
            Assert.AreEqual(BeatPulse.StepHeight(6.25), BeatPulse.StepHeight(7.25), 1e-5f, "1박 = 한 걸음 (주기 1박)");
            Assert.AreEqual(-1f, BeatPulse.Foot(6.3));
            Assert.AreEqual(1f, BeatPulse.Foot(7.3), "왼발·오른발 번갈아");
            Assert.AreEqual(-GameConfig.BeatFx.StepSwayMeters, BeatPulse.StepSway(6.5), 1e-5f);
            Assert.AreEqual(GameConfig.BeatFx.StepSwayMeters, BeatPulse.StepSway(7.5), 1e-5f);
            Assert.AreEqual(0f, BeatPulse.StepSway(7.0), 1e-5f, "박 순간 가운데");

            var run = BeatPulse.Bob(6.0, 1f, out float rollRun);
            Assert.AreEqual(-GameConfig.BeatFx.StepDropMeters, run.y, 1e-5f);
            var stop = BeatPulse.Bob(8.0, 0f, out float rollStop);
            Assert.AreEqual(0f, stop.x, 1e-6f, "멈춤: 좌우 없음");
            Assert.AreEqual(0f, rollStop, 1e-6f);
            Assert.AreEqual(-GameConfig.BeatFx.IdleDipMeters * GameConfig.BeatFx.BarAccent, stop.y, 1e-5f, "멈춤: 박마다 살짝 내려앉음 (마디 첫 박 더 크게)");
            Assert.AreEqual(0f, BeatPulse.Bob(8.5, 0f, out _).y, 1e-6f, "박 사이엔 제자리");
        }

        [Test]
        public void B45_TickClip_ShortHigh_LastIsHigher()
        {
            var a = ClickSynth.CreateTick(false);
            var b = ClickSynth.CreateTick(true);
            Assert.AreEqual(Mathf.RoundToInt(ClickSynth.SampleRate * GameConfig.BeatFx.TickSeconds), a.Length);
            Assert.Greater(a.Max(Mathf.Abs), 0.1f);
            Assert.Greater(GameConfig.BeatFx.TickLastFrequency, GameConfig.BeatFx.TickFrequency, "1박 전 틱은 높은 음");
            Assert.Greater(Crossings(b), Crossings(a));
            Assert.Less(GameConfig.BeatFx.TickVolume, 0.5f, "작은 틱");
        }

        static int Crossings(float[] d)
        {
            int n = 0;
            for (int i = 1; i < d.Length; i++) if ((d[i - 1] < 0) != (d[i] < 0)) n++;
            return n;
        }

        [Test]
        public void B45_CueTicksSetting_DefaultOn_Saved()
        {
            bool had = PlayerPrefs.HasKey(GameSettings.CueTicksKey);
            int saved = PlayerPrefs.GetInt(GameSettings.CueTicksKey, 1);
            try
            {
                PlayerPrefs.DeleteKey(GameSettings.CueTicksKey);
                Assert.IsTrue(GameSettings.CueTicks, "기본 켜짐");
                GameSettings.CueTicks = false;
                Assert.AreEqual(0, PlayerPrefs.GetInt(GameSettings.CueTicksKey));
                Assert.IsFalse(GameSettings.CueTicks);
                StringAssert.Contains(UiText.CueTicks, UiText.AllGlyphs);
                StringAssert.Contains(UiText.Off, UiText.AllGlyphs);
            }
            finally
            {
                if (had) PlayerPrefs.SetInt(GameSettings.CueTicksKey, saved); else PlayerPrefs.DeleteKey(GameSettings.CueTicksKey);
            }
        }
    }
}
