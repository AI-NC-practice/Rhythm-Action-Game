using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>작업 R25 (GDD-beta B20, 2026-11-17 사용자 결정): 장구·방향키 점수, 장구 콤보 보너스, 되감기 패널티, 랭크 8단계.</summary>
    public class R25_ScoreRulesTests
    {
        [Test]
        public void R25_Arrow_100_50_NoCombo()
        {
            var b = new ScoreBoard();
            b.Add(Judgement.Perfect, true); b.Add(Judgement.Perfect, true);
            b.Add(Judgement.Perfect, false);
            b.Add(Judgement.Normal, false);
            Assert.AreEqual(100 + 100 + 50, b.Score, "장구 50×2 + 방향키 100 + 50");
            Assert.AreEqual(2, b.Combo, "방향키는 콤보를 세지 않음");
            b.Add(Judgement.Fail, false);
            Assert.AreEqual(2, b.Combo, "방향키 FAIL은 장구 콤보를 끊지 않음");
            Assert.AreEqual(250, b.Score);
        }

        [Test]
        public void R25_JangdanComboBonus_Thresholds()
        {
            Assert.AreEqual(0, ScoreBoard.JangdanBonus(4));
            Assert.AreEqual(10, ScoreBoard.JangdanBonus(5));
            Assert.AreEqual(10, ScoreBoard.JangdanBonus(9));
            Assert.AreEqual(20, ScoreBoard.JangdanBonus(10));
            Assert.AreEqual(30, ScoreBoard.JangdanBonus(20));
            Assert.AreEqual(30, ScoreBoard.JangdanBonus(49));
            Assert.AreEqual(50, ScoreBoard.JangdanBonus(50));
            var b = new ScoreBoard();
            for (int i = 0; i < 4; i++) b.Add(Judgement.Perfect, true);
            Assert.AreEqual(200, b.Score);
            b.Add(Judgement.Normal, true);   // 콤보 5: NORMAL 20 + 보너스 10
            Assert.AreEqual(230, b.Score, "NORMAL도 콤보가 이어지고 보너스");
            Assert.AreEqual(5, b.Combo);
        }

        [Test]
        public void R25_RewindPenalty_BackToCheckpointMinus1000_ComboKept()
        {
            var b = new ScoreBoard();
            for (int i = 0; i < 30; i++) b.Add(Judgement.Perfect, true);
            var cp = b.Save();
            int atEntry = b.Score;
            for (int i = 0; i < 10; i++) b.Add(Judgement.Perfect, false);
            int combo = b.Combo;
            b.RewindTo(cp);
            Assert.AreEqual(atEntry - 1000, b.Score, "초입 점수 − 1000");
            Assert.AreEqual(30, b.Perfect, "판정 수도 초입으로 (다시 친 판정을 두 번 세지 않음)");
            Assert.AreEqual(combo, b.Combo, "장구 콤보 유지");
            b.RewindTo(cp);
            Assert.AreEqual(atEntry - 1000, b.Score, "또 죽어도 같은 초입 점수에서 − 1000 (쌓이지 않음)");
            var small = new ScoreBoard();
            small.Add(Judgement.Perfect, false);
            small.RewindTo(small.Save());
            Assert.AreEqual(0, small.Score, "0 아래로 안 감");
        }

        [Test]
        public void R25_MaxScore_Stage1()
        {
            var c = StageChart.Load();
            int jangdan = RhythmNotes.Generate(c).Count;
            int arrows = ChartCheck.RunTargets(c).Count + ChartCheck.CombatJudgements(c);
            int max = ScoreKeeper.MaxScoreOf(c);
            Assert.AreEqual(ScoreBoard.MaxScoreFor(jangdan, arrows), max);
            Assert.Greater(jangdan, 100); Assert.Greater(arrows, 100);
            UnityEngine.Debug.Log($"[R25] stage1 장구 {jangdan} · 방향키 {arrows} → 만점 {max}");
            var b = new ScoreBoard { MaxScore = max };
            for (int i = 0; i < jangdan; i++) b.Add(Judgement.Perfect, true);
            for (int i = 0; i < arrows; i++) b.Add(Judgement.Perfect, false);
            Assert.AreEqual(max, b.Score, "모두 PERFECT = 만점");
            Assert.AreEqual("SSS", b.Rank(true));
        }
    }
}
