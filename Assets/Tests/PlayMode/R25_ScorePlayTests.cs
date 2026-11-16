using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>작업 R25 (GDD-beta B20): 실제 게임에서 장구·방향키 점수 구분, 결투 FAIL 되감기 패널티(초입 점수 − 1000, 장구 콤보 유지).</summary>
    public class R25_ScorePlayTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;
            dir.slowMotion.RealTime = () => real;
            boot.presentation.timeFx.RealTime = () => Time.realtimeSinceStartup + (float)now;
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(start);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator Frame(System.Func<JudgeCue, bool> hit)
        {
            now += 0.01;
            foreach (var c in boot.judge.Cues.ToArray())
                if (!c.Judged && c.Appeared && hit(c) && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
            yield return null;
        }

        [UnityTest]
        public IEnumerator R25_RealRun_JangdanAndArrowPoints_MaxScoreSet()
        {
            yield return Load(0);
            var b = boot.score.Board;
            Assert.AreEqual(ScoreKeeper.MaxScoreOf(dir.Chart), b.MaxScore, "만점 = 차트에서 계산");
            int jP = 0, aP = 0;
            int lastScore = 0;
            System.Action<JudgeCue, Judgement> check = (c, r) =>
            {
                int gained = b.Score - lastScore;
                lastScore = b.Score;
                if (r != Judgement.Perfect) return;
                if (ScoreBoard.IsJangdan(c.Key)) { Assert.AreEqual(50 + ScoreBoard.JangdanBonus(b.Combo), gained, "장구 PERFECT"); jP++; }
                else { Assert.AreEqual(100, gained, "방향키 PERFECT"); aP++; }
            };
            dir.AnyCueJudged += check;
            for (int i = 0; i < 3000 && boot.player.RunBeat < 40; i++) yield return Frame(c => true);
            dir.AnyCueJudged -= check;
            Debug.Log($"[R25] 0~40박: 장구 PERFECT {jP}, 방향키 PERFECT {aP}, 콤보 {b.Combo}, 점수 {b.Score}");
            Assert.Greater(jP, 10); Assert.Greater(aP, 3);
            Assert.AreEqual(jP, b.Combo, "콤보 = 장구만");
        }

        [UnityTest]
        public IEnumerator R25_DuelFail_ScoreBackToEntryMinus1000_ComboKept()
        {
            yield return Load(256);
            var b = boot.score.Board;
            for (int i = 0; i < 40; i++) b.Add(Judgement.Perfect, true);   // 초입 점수가 1000 넘게 (장구 콤보 40)
            for (int i = 0; i < 4000 && dir.State != StageState.Combat; i++) yield return Frame(c => true);
            Assert.AreEqual(StageState.Combat, dir.State);
            var enc = dir.CurrentEncounter;
            Assert.IsTrue(enc.IsBossDuel);
            int entry = b.Score;
            var cues = enc.Cues.ToList();
            // 1단계 처음 몇 개 PERFECT(방향키 점수가 쌓임) 뒤 하나를 놓침 → 단계 처음으로 되감기
            int k = 0;
            for (; k < 4; k++)
            {
                while (now < boot.clock.TimeOfBeat(cues[k].TargetBeat) - 1e-6) { now += 0.01; yield return null; }
                boot.judge.Submit(cues[k].Key, now); yield return null;
            }
            Assert.Greater(b.Score, entry, "결투 중 점수가 쌓임");
            int combo = b.Combo;
            while (!dir.Rewinding && now < boot.clock.TimeOfBeat(cues[k].TargetBeat) + 0.4) { now += 0.01; yield return null; }
            Assert.IsTrue(dir.Rewinding, "FAIL → 되감기");
            Assert.AreEqual(RewindKind.Boss, dir.LastRewind);
            Assert.AreEqual(entry - GameConfig.RewindScorePenalty, b.Score, "결투 초입 점수 − 1000");
            Assert.AreEqual(combo, b.Combo, "장구 콤보 유지");
            Assert.AreEqual(GameConfig.MaxHp, dir.Health.Hp, "체력 그대로");
            Debug.Log($"[R25] 결투 FAIL: 초입 {entry} → {b.Score}, 콤보 {b.Combo}");
        }
    }
}
