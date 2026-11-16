using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    public class T08_ScoreAndTextTests
    {
        [Test]
        public void T08_Score_And_JangdanCombo()   // 작업 R25: 장구만 콤보
        {
            var b = new ScoreBoard();
            b.Add(Judgement.Perfect, true);
            b.Add(Judgement.Normal, true);
            b.Add(Judgement.Perfect, true);
            Assert.AreEqual(120, b.Score, "장구 50 + 20 + 50");
            Assert.AreEqual(3, b.Combo);
            b.Add(Judgement.Fail, true);
            Assert.AreEqual(120, b.Score, "FAIL 0점");
            Assert.AreEqual(0, b.Combo, "장구 FAIL이면 콤보 끊김");
            Assert.AreEqual(3, b.MaxCombo);
            Assert.AreEqual(2, b.Perfect); Assert.AreEqual(1, b.Normal); Assert.AreEqual(1, b.Fail);
            b.ObserveSpeed(1.25f); b.ObserveSpeed(1.1f);
            Assert.AreEqual(1.25f, b.MaxSpeed, 1e-5f);
        }

        [Test]
        public void T08_Rank_Thresholds()   // 작업 R25: 만점 대비 점수 비율 8단계
        {
            Assert.AreEqual("SSS", ScoreBoard.Rank(true, 970, 1000));
            Assert.AreEqual("SS", ScoreBoard.Rank(true, 969, 1000));
            Assert.AreEqual("SS", ScoreBoard.Rank(true, 930, 1000));
            Assert.AreEqual("S", ScoreBoard.Rank(true, 880, 1000));
            Assert.AreEqual("A", ScoreBoard.Rank(true, 800, 1000));
            Assert.AreEqual("B", ScoreBoard.Rank(true, 700, 1000));
            Assert.AreEqual("C", ScoreBoard.Rank(true, 600, 1000));
            Assert.AreEqual("D", ScoreBoard.Rank(true, 500, 1000));
            Assert.AreEqual("F", ScoreBoard.Rank(true, 499, 1000));
            Assert.AreEqual("-", ScoreBoard.Rank(false, 1000, 1000), "게임 오버");
        }

        [Test]
        public void T08_Texts_MatchGdd48()
        {
            Assert.AreEqual("x1.00", UiText.Speed(1f));
            Assert.AreEqual("x1.30", UiText.Speed(1.3f));
            Assert.AreEqual("x1.05", UiText.Speed(1.05f));
            Assert.AreEqual("", UiText.Combo(1), "콤보 1은 표시 안 함");
            Assert.AreEqual("콤보 2", UiText.Combo(2));
            Assert.AreEqual("콤보 12", UiText.Combo(12));
            Assert.AreEqual("PERFECT 18", UiText.PerfectCount(18));
            Assert.AreEqual("NORMAL 5", UiText.NormalCount(5));
            Assert.AreEqual("FAIL 2", UiText.FailCount(2));
            Assert.AreEqual("최고 속도 x1.30", UiText.MaxSpeed(1.3f));
            Assert.AreEqual("점수 5900", UiText.Score(5900));
            Assert.AreEqual("랭크 A", UiText.Rank("A"));
            Assert.AreEqual("스테이지 클리어", UiText.Cleared);
            Assert.AreEqual("게임 오버", UiText.GameOver);
            Assert.AreEqual("다시 하기", UiText.Retry);
            Assert.AreEqual("타이틀로", UiText.ToTitle);
            Assert.AreEqual("일시정지", UiText.Paused);
            Assert.AreEqual("계속", UiText.Continue);
            Assert.AreEqual("타이밍 보정", UiText.TimingOffset);
            Assert.AreEqual("음량", UiText.Volume);
            Assert.AreEqual("돌아가기", UiText.Back);
        }

        [Test]
        public void T08_SpeedFx_Intensity()
        {
            Assert.AreEqual(0f, SpeedFxMath.Intensity(1.00f), 1e-5f);
            Assert.AreEqual(0f, SpeedFxMath.Intensity(1.05f), 1e-5f, "1.10 미만은 없음");
            Assert.AreEqual(0.1f / 0.3f, SpeedFxMath.Intensity(1.10f), 1e-4f);
            Assert.AreEqual(0.5f, SpeedFxMath.Intensity(1.15f), 1e-4f);
            Assert.AreEqual(1f, SpeedFxMath.Intensity(1.30f), 1e-4f);
            Assert.AreEqual(5f, SpeedFxMath.FovBonus(1.30f), 1e-4f, "FOV 최대 +5°");
            Assert.AreEqual(2.5f, SpeedFxMath.FovBonus(1.15f), 1e-4f);
            Assert.AreEqual(0f, SpeedFxMath.FovBonus(1.05f), 1e-4f);
        }

        [Test]
        public void T08_Settings_SaveAndLoad()
        {
            int oldOffset = GameSettings.TimingOffsetMs, oldVol = GameSettings.Volume;
            try
            {
                PlayerPrefs.DeleteKey(GameSettings.VolumeKey);
                Assert.AreEqual(80, GameSettings.Volume, "음량 기본 80");
                GameSettings.TimingOffsetMs = 15;
                GameSettings.Volume = 55;
                Assert.AreEqual(15, PlayerPrefs.GetInt(GameSettings.TimingOffsetKey));
                Assert.AreEqual(55, PlayerPrefs.GetInt(GameSettings.VolumeKey));
                Assert.AreEqual(15, GameSettings.TimingOffsetMs);
                Assert.AreEqual(55, GameSettings.Volume);
                GameSettings.Volume = 150;
                Assert.AreEqual(100, GameSettings.Volume);
            }
            finally { GameSettings.TimingOffsetMs = oldOffset; GameSettings.Volume = oldVol; }
        }
    }
}
