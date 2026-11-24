using NUnit.Framework;

namespace RhythmActionGame.Tests
{
    /// <summary>작업 R39 (GDD-beta B22): 체력 0 챕터 되감기는 장구 콤보 0, 전투 되감기는 콤보 유지(B20). 점수 패널티는 둘 다 −1000.</summary>
    public class R39_ComboResetTests
    {
        [Test]
        public void R39_ChapterRewind_ResetsCombo_BattleRewindKeeps()
        {
            var b = new ScoreBoard();
            for (int i = 0; i < 25; i++) b.Add(Judgement.Perfect, true);
            var cp = b.Save();
            int entry = b.Score;
            for (int i = 0; i < 5; i++) b.Add(Judgement.Perfect, true);
            Assert.AreEqual(30, b.Combo);

            b.RewindTo(cp);   // 전투 되감기 (체력 그대로)
            Assert.AreEqual(30, b.Combo, "전투 되감기: 콤보 유지 (B20)");
            Assert.AreEqual(entry - GameConfig.RewindScorePenalty, b.Score);

            int max = b.MaxCombo;
            b.RewindTo(cp, resetCombo: true);   // 체력 0 챕터 되감기
            Assert.AreEqual(0, b.Combo, "챕터 되감기: 콤보 0");
            Assert.AreEqual(entry - GameConfig.RewindScorePenalty, b.Score, "점수 패널티는 같음");
            Assert.AreEqual(max, b.MaxCombo, "최대 콤보 기록은 그대로");
            Assert.AreEqual("", UiText.Combo(b.Combo), "HUD 콤보 글자 없음");
            b.Add(Judgement.Perfect, true);
            Assert.AreEqual(1, b.Combo, "되감은 뒤 다시 1부터");
        }
    }
}
