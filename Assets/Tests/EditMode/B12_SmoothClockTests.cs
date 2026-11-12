using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>부드러운 박자 시계 (GDD-beta B8.1). dsp·실제 시간을 주입해 검사한다.</summary>
    public class B12_SmoothClockTests
    {
        const double Buffer = 1024.0 / 48000.0;   // 약 21.3 ms

        [Test]
        public void B12_Smooth_AdvancesEveryFrame_WithinOneBuffer_NeverBackwards()
        {
            double real = 100.0, dsp = 5.0;
            var c = new SmoothClock(() => dsp, () => real, Buffer);
            double prev = c.Now();
            int strictlyUp = 0, calls = 0;
            // 프레임 5 ms, dsp는 버퍼 단위(21.3 ms)로만 갱신 (조금 늦게 따라옴)
            double nextDspUpdate = real + Buffer;
            for (int i = 0; i < 400; i++)
            {
                real += 0.005;
                if (real >= nextDspUpdate) { dsp += Buffer; nextDspUpdate += Buffer; }
                double t = c.Now();
                calls++;
                Assert.GreaterOrEqual(t, prev, "뒤로 가지 않음");
                Assert.GreaterOrEqual(t, dsp - 1e-12, "dsp보다 뒤가 아님");
                Assert.LessOrEqual(t - dsp, Buffer + 1e-9, "dsp와의 차이 ≤ 버퍼 한 개");
                if (t > prev + 1e-9) strictlyUp++;
                prev = t;
            }
            Assert.Greater(strictlyUp, calls * 0.95, "dsp가 멈춰 있는 사이에도 매 프레임 진행 (끊김 없음)");
        }

        [Test]
        public void B12_Smooth_StallCapped_DspBackwardsResets()
        {
            double real = 0, dsp = 1.0;
            var c = new SmoothClock(() => dsp, () => real, Buffer);
            c.Now();
            real += 1.0;                                    // 오디오가 멈춤 (dsp 그대로)
            Assert.AreEqual(1.0 + Buffer, c.Now(), 1e-9, "버퍼 한 개 이상 앞서가지 않음");
            dsp = 0.5;                                      // 장치 재시작으로 dsp가 뒤로
            Assert.AreEqual(0.5, c.Now(), 1e-9, "새 기준");
        }

        [Test]
        public void B12_FromRealtime_MapsInputEventTime_NotInFuture()
        {
            double real = 10.0, dsp = 3.0;
            var c = new SmoothClock(() => dsp, () => real, Buffer);
            c.Now();
            real = 10.015;                                  // 같은 dsp 구간 안
            double now = c.Now();
            Assert.AreEqual(3.015, now, 1e-9);
            Assert.AreEqual(3.004, c.FromRealtime(10.004), 1e-9, "이벤트 시각 → 시계 시각");
            Assert.AreEqual(c.Now(), c.FromRealtime(11.0), 1e-9, "미래 시각은 지금으로 자름");
        }

        [Test]
        public void B12_Config_JudgeWindowsUnchanged_LatencyDefaults()
        {
            Assert.AreEqual(60f, GameConfig.PerfectWindowMs, "판정 창 그대로 (GDD 4.2)");
            Assert.AreEqual(150f, GameConfig.NormalWindowMs);
            Assert.AreEqual(16f, GameConfig.DisplayLatencyMs, "화면 표시 지연 보정 기본값");
            Assert.AreEqual(0.1, GameConfig.InputMaxAgeSeconds, 1e-9);
        }
    }
}
