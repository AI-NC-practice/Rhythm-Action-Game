using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>마디 첫 박에 속도 적용 + 음악 음 높이 보정 연결 (GDD-beta B15.1, B15.2).</summary>
    public class B37_SpeedOnBarTests
    {
        [UnityTest]
        public IEnumerator B37_SpeedAppliesOnNextBar_HudAtOnce_PitchCorrected_RewindAtOnce()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var clock = boot.clock;
            double now = 0;
            clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });
            boot.StartRun(0);
            yield return null;

            Assert.IsNotNull(clock.pitchCorrector, "음 높이 보정 연결");
            Assert.AreSame(clock.music.gameObject, clock.pitchCorrector.gameObject, "음악 AudioSource와 같은 GameObject");
            Assert.AreEqual(1f, clock.pitchCorrector.Ratio, 1e-6f);

            // 1박에서 PERFECT → 목표 1.05, 실제는 다음 마디 첫 박(4박)부터
            now = 0.5; yield return null;
            clock.ApplyJudgement(Judgement.Perfect);
            Assert.AreEqual(1.05f, clock.TargetSpeed, 1e-5f);
            Assert.AreEqual(1.00f, clock.SpeedMultiplier, 1e-5f, "바로 바뀌지 않음");
            Assert.AreEqual(4.0, clock.PendingSpeedBeat, 1e-9, "다음 마디 첫 박");
            yield return null;
            Assert.AreEqual("x1.05", boot.hud.SpeedText.text, "HUD는 바로 새 값");
            Assert.AreEqual(1.00f, clock.music.pitch, 1e-5f, "음악 빠르기는 아직 그대로");
            Assert.AreEqual(GameConfig.SpeedStart * GameConfig.BaseMoveSpeed, clock.MoveSpeed, 1e-4f, "이동도 아직 그대로");

            now = 1.99; yield return null;                                    // 3.98박
            Assert.AreEqual(1.00f, clock.SpeedMultiplier, 1e-5f, "마디 첫 박 전");
            now = 2.10; yield return null;                                    // 4박(2.0초) 지남
            Assert.AreEqual(1.05f, clock.SpeedMultiplier, 1e-5f, "마디 첫 박에 적용");
            Assert.IsTrue(double.IsNaN(clock.PendingSpeedBeat));
            Assert.AreEqual(4.0 + 0.10 * 1.05 / GameConfig.SecondsPerBeat, clock.CurrentBeat, 1e-6, "4박 시각(2.0초)부터 새 속도 — 박자가 끊기지 않음");
            Assert.AreEqual(1.05f, clock.music.pitch, 1e-5f, "음악 재생 속도 = s");
            Assert.AreEqual(1f / 1.05f, clock.pitchCorrector.Ratio, 1e-5f, "음 높이는 1/s로 보정");

            // 마디 안에서 두 번 바뀌면 마지막 값, 되감기는 바로 새 속도
            clock.ApplyJudgement(Judgement.Perfect);
            clock.ApplyJudgement(Judgement.Perfect);
            Assert.AreEqual(1.15f, clock.TargetSpeed, 1e-5f);
            Assert.AreEqual(8.0, clock.PendingSpeedBeat, 1e-9);
            clock.RestartAt(20);
            Assert.AreEqual(1.15f, clock.SpeedMultiplier, 1e-5f, "되감기는 바로 새 속도");
            Assert.IsTrue(double.IsNaN(clock.PendingSpeedBeat));
            if (clock.music.clip != null)
            {
                double lead = MusicPitchCorrector.LatencySeconds * 1.15 / GameConfig.SecondsPerBeat;
                Assert.AreEqual(BeatClock.MusicTimeAt(20 + lead, clock.music.clip.length), clock.music.time, 0.05f, "음악 위치를 보정 지연만큼 앞당김");
            }

            // FAIL도 다음 마디
            now += 0.25; yield return null;
            clock.ApplyJudgement(Judgement.Fail);
            Assert.AreEqual(1.15f, clock.SpeedMultiplier, 1e-5f);
            Assert.AreEqual(Mathf.Max(1.15f * 0.8f, GameConfig.SpeedMin), clock.TargetSpeed, 1e-5f);
            clock.StopClock();
        }
    }
}
