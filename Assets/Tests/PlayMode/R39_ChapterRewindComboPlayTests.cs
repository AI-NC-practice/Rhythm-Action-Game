using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R39 (사용자 피드백 2026-11-24 4번 "죽어서 처음으로 되돌아갔는데 콤보가 유지돼요, 0으로"): 실제 게임에서 장구를 쳐 콤보를 쌓고
    /// 방향키 판정을 놓쳐 체력 0 → 챕터 되감기가 시작되면 장구 콤보 0, HUD 콤보 글자도 사라진다. (결투 되감기의 콤보 유지는 R25 테스트)
    /// </summary>
    public class R39_ChapterRewindComboPlayTests
    {
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator R39_HpZero_ChapterRewind_ComboResetToZero()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            now = 0; float real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;
            dir.slowMotion.RealTime = () => real;
            boot.presentation.timeFx.RealTime = () => Time.realtimeSinceStartup + (float)now;
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(0);
            yield return null;
            var b = boot.score.Board;
            var hud = Object.FindAnyObjectByType<GameHud>();
            int comboBefore = 0;
            // 장구 노드만 PERFECT, 방향키 판정은 놓침 → 체력 3번 깎여 0 → 챕터 되감기
            for (int i = 0; i < 20000 && !dir.Rewinding; i++)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && ScoreBoard.IsJangdan(c.Key) && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                comboBefore = b.Combo;
                yield return null;
            }
            Assert.IsTrue(dir.Rewinding, "체력 0 → 되감기");
            Assert.AreEqual(RewindKind.Chapter, dir.LastRewind, "챕터 되감기");
            Assert.Greater(comboBefore, 2, $"되감기 전 장구 콤보가 쌓여 있었음 ({comboBefore})");
            Assert.AreEqual(0, b.Combo, "체력 0 챕터 되감기 → 장구 콤보 0");
            yield return null;
            Assert.IsFalse(hud.ComboText.enabled && hud.ComboText.text.Length > 0, $"HUD 콤보 글자 사라짐 ('{hud.ComboText.text}')");
            Debug.Log($"[R39] 되감기 전 콤보 {comboBefore} → {b.Combo}, 점수 {b.Score}");
        }
    }
}
