using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>음악 끄기 설정 (작업 B53, 2026-11-16 사용자 요청: 효과음만 듣기).</summary>
    public class B53_MusicMuteTests
    {
        bool saved;

        [SetUp] public void SetUp() { saved = GameSettings.MusicOn; GameSettings.MusicOn = true; }
        [TearDown] public void TearDown() { GameSettings.MusicOn = saved; Time.timeScale = 1f; }

        [Test]
        public void B53_Setting_DefaultOn_Saved()
        {
            PlayerPrefs.DeleteKey(GameSettings.MusicOnKey);
            GameSettings.MusicOn = true;
            Assert.IsTrue(GameSettings.MusicOn, "기본 켜짐");
            GameSettings.MusicOn = false;
            Assert.AreEqual(0, PlayerPrefs.GetInt(GameSettings.MusicOnKey), "저장");
            Assert.IsFalse(GameSettings.MusicOn);
            StringAssert.Contains(UiText.Music, UiText.AllGlyphs, "폰트 글자");
        }

        [UnityTest]
        public IEnumerator B53_Title_MusicToggle()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var menu = Object.FindAnyObjectByType<TitleMenu>();
            menu.OpenSettings();
            var labels = menu.SettingsPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Select(t => t.text).ToList();
            CollectionAssert.Contains(labels, UiText.Music, "설정 화면에 음악");
            Assert.AreEqual(UiText.On, menu.MusicValue.text);
            menu.MusicButton.onClick.Invoke();
            Assert.AreEqual(UiText.Off, menu.MusicValue.text);
            Assert.IsFalse(GameSettings.MusicOn, "저장");
            menu.MusicButton.onClick.Invoke();
            Assert.IsTrue(GameSettings.MusicOn);
        }

        [UnityTest]
        public IEnumerator B53_GameScene_PauseToggle_MutesOnlyMusic_ClockAndSfxKeepGoing()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.StartRun(0);
            yield return null;
            var sfx = boot.presentation.sfx;
            var music = boot.clock.music;
            Assert.IsNotNull(music);
            sfx.Tick();
            float on = music.volume;
            Assert.Greater(on, 0f, "켜짐: 곡 소리 있음");

            // 일시정지 메뉴에서 끄기
            boot.pause.Pause();
            StringAssert.Contains(UiText.On, boot.pause.MusicText.text);
            boot.pause.MusicButton.onClick.Invoke();
            StringAssert.Contains(UiText.Off, boot.pause.MusicText.text);
            Assert.IsFalse(GameSettings.MusicOn);
            boot.pause.Resume();
            sfx.Tick();
            Assert.AreEqual(0f, music.volume, 1e-6f, "꺼짐: 곡 소리 0");

            // 박자 시계는 계속, 효과음은 그대로 남
            double before = boot.clock.CurrentBeat;
            now += 0.5;
            yield return null;
            Assert.Greater(boot.clock.CurrentBeat, before + 0.9, "곡·박자 시계는 계속");
            sfx.Play(SfxKind.ParryClang, 1f);
            sfx.Tick();
            Assert.Greater(sfx.ActiveCount(SfxKind.ParryClang), 0, "효과음은 그대로 남");
            Assert.AreEqual(0f, music.volume, 1e-6f);

            // 다시 켜기
            GameSettings.MusicOn = true;
            sfx.Tick();
            Assert.AreEqual(on, music.volume, 0.05f, "다시 켜면 곡 소리");
            boot.clock.StopClock();
        }
    }
}
