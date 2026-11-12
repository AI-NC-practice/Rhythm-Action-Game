using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>개발자 모드: 숫자 키 챕터 이동 (사용자 요청, 본 게임에서는 빠짐).</summary>
    public class DevModeTests
    {
        [TearDown]
        public void TearDown() { GameBootstrap.PendingStartBeat = 0; Time.timeScale = 1f; }

        [Test]
        public void DevMode_ChapterStartBeats_MatchSections()
        {
            int[] expected = { 0, 56, 108, 176, 196, 252 };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], DevMode.ChapterStartBeat(i + 1));
            Assert.AreEqual(-1, DevMode.ChapterStartBeat(0));
            Assert.AreEqual(-1, DevMode.ChapterStartBeat(7));
        }

        [UnityTest]
        public IEnumerator DevMode_JumpTo_StartsGameAtChapter([Values(1, 4, 6)] int chapter)
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            Assert.IsNotNull(DevMode.Instance, "개발자 모드 자동 설치");
            Time.timeScale = 0f;   // 일시정지 상태에서 눌러도 풀린다
            Assert.IsTrue(DevMode.JumpTo(chapter));
            yield return null;
            yield return null;
            Assert.AreEqual("Game", SceneManager.GetActiveScene().name);
            var boot = GameBootstrap.Instance;
            Assert.IsNotNull(boot);
            Assert.AreEqual(1f, Time.timeScale);
            int start = MapLayout.Sections[chapter - 1].StartBeat;
            Assert.AreEqual(start, boot.player.RunBeat, 0.5, "챕터 시작 지점");
            Assert.AreEqual(0, GameBootstrap.PendingStartBeat, "한 번 쓰면 초기화");
            Assert.AreEqual(StageState.Running, boot.director.State);
            Assert.AreEqual(3, boot.director.Health.Hp);
            yield return null;
            StringAssert.Contains($"현재 {chapter}.", DevMode.Instance.LabelText);
        }
    }
}
