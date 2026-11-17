using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RhythmActionGame.Tests
{
    /// <summary>타이틀·설정·HUD·일시정지·결과·속도감 (GDD 4.8, 4.3).</summary>
    public class T08_UiTests
    {
        double now;
        GameBootstrap boot;
        int savedOffset, savedVolume;

        [SetUp]
        public void SetUp() { savedOffset = GameSettings.TimingOffsetMs; savedVolume = GameSettings.Volume; }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameSettings.TimingOffsetMs = savedOffset;
            GameSettings.Volume = savedVolume;
        }

        static string[] Texts(Transform root) => root.GetComponentsInChildren<Text>(true).Select(t => t.text).ToArray();

        IEnumerator LoadGame(double startRunBeat = 0, bool emptyChart = false)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            if (emptyChart) boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [UnityTest]
        public IEnumerator T08_Title_Texts_AndSettingsSaved()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            var menu = Object.FindAnyObjectByType<TitleMenu>();
            Assert.IsNotNull(menu);
            var texts = Texts(menu.transform);
            CollectionAssert.Contains(texts, "Rhythm-Action-Game");
            CollectionAssert.Contains(texts, "시작");
            CollectionAssert.Contains(texts, "설정");
            CollectionAssert.Contains(texts, "종료");
            Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>());
            Assert.AreEqual(Navigation.Mode.None, menu.StartButton.navigation.mode);

            menu.SettingsButton.onClick.Invoke();
            Assert.IsTrue(menu.SettingsPanel.gameObject.activeSelf);
            Assert.IsFalse(menu.MainPanel.gameObject.activeSelf);
            var st = Texts(menu.SettingsPanel);
            CollectionAssert.Contains(st, "타이밍 보정");
            CollectionAssert.Contains(st, "음량");
            CollectionAssert.Contains(st, "돌아가기");

            // 슬라이더 범위: −200~+200 ms(5 ms 단위), 음량 0~100
            Assert.AreEqual(-200, Mathf.RoundToInt(menu.OffsetSlider.minValue) * 5);
            Assert.AreEqual(200, Mathf.RoundToInt(menu.OffsetSlider.maxValue) * 5);
            Assert.AreEqual(0, menu.VolumeSlider.minValue);
            Assert.AreEqual(100, menu.VolumeSlider.maxValue);

            menu.OffsetSlider.value = 3;                  // 3 × 5 = 15 ms
            Assert.AreEqual("+15ms", menu.OffsetValue.text);
            menu.VolumeSlider.value = 42;
            Assert.AreEqual(15, PlayerPrefs.GetInt(GameSettings.TimingOffsetKey), "PlayerPrefs 저장");
            Assert.AreEqual(42, PlayerPrefs.GetInt(GameSettings.VolumeKey));
            menu.OffsetSlider.value = -4;
            Assert.AreEqual("-20ms", menu.OffsetValue.text);

            menu.BackButton.onClick.Invoke();
            Assert.IsTrue(menu.MainPanel.gameObject.activeSelf);

            // 다시 불러오면 저장값이 보임
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            menu = Object.FindAnyObjectByType<TitleMenu>();
            menu.OpenSettings();
            Assert.AreEqual("-20ms", menu.OffsetValue.text);
            Assert.AreEqual(42, Mathf.RoundToInt(menu.VolumeSlider.value));
        }

        [UnityTest]
        public IEnumerator T08_Title_StartLoadsGame()
        {
            yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            Object.FindAnyObjectByType<TitleMenu>().StartButton.onClick.Invoke();
            // 작업 R35: 지붕에서 떨어지는 연출(FallSeconds) 뒤에 게임 씬
            float until = Time.realtimeSinceStartup + GameConfig.Title.FallSeconds + 5f;
            while (SceneManager.GetActiveScene().name != "Game" && Time.realtimeSinceStartup < until) yield return null;
            yield return null;
            Assert.AreEqual("Game", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator T08_Font_HasAllUiGlyphs()
        {
            var font = UiFactory.Font;
            font.RequestCharactersInTexture(UiText.AllGlyphs, 40);
            foreach (var ch in UiText.AllGlyphs)
                if (ch != ' ') Assert.IsTrue(font.GetCharacterInfo(ch, out _, 40), $"글자 없음: {ch}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator T08_Hud_HeartsSpeedScoreCombo()
        {
            yield return LoadGame(emptyChart: true);   // beta1-1: B20 차트는 4박에 수레가 있어 직접 예약한 판정과 겹친다
            var hud = boot.hud;
            yield return null;
            Assert.AreEqual(3, hud.Hearts.Length);
            Assert.AreEqual(3, hud.FullHearts);
            Assert.AreEqual("x1.00", hud.SpeedText.text);
            Assert.AreEqual("0", hud.ScoreText.text);
            Assert.IsFalse(hud.ComboText.enabled, "콤보 0은 숨김");

            boot.judge.Schedule(4, ArrowKey.Kung);   // 작업 R25: 콤보는 장구만
            boot.judge.Schedule(6, ArrowKey.Kung);
            now = 2.0; boot.judge.Tick(); boot.judge.Submit(ArrowKey.Kung, now);
            yield return null;
            Assert.IsFalse(hud.ComboText.enabled, "콤보 1은 숨김");
            Assert.AreEqual("x1.05", hud.SpeedText.text);
            now = boot.clock.TimeOfBeat(6); boot.judge.Submit(ArrowKey.Kung, now);
            yield return null;
            Assert.IsTrue(hud.ComboText.enabled);
            Assert.AreEqual("콤보 2", hud.ComboText.text);
            Assert.AreEqual("100", hud.ScoreText.text, "장구 PERFECT 50 × 2 (R25)");
            Assert.AreEqual("x1.10", hud.SpeedText.text);

            boot.director.Health.Damage();
            yield return null;
            Assert.AreEqual(2, hud.FullHearts);
            // 위치: 하트 왼쪽 위, 속도 오른쪽 위, 콤보 가운데 아래
            Assert.AreEqual(new Vector2(0, 1), hud.Hearts[0].rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(1, 1), hud.SpeedText.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0), hud.ComboText.rectTransform.anchorMin);
        }

        [UnityTest]
        public IEnumerator T08_Pause_StopsClockAndMusic_ThenResumes()
        {
            yield return LoadGame();
            now = 1.0;
            yield return null;
            var pause = boot.pause;
            pause.Toggle();
            Assert.IsTrue(pause.IsPaused);
            Assert.IsTrue(pause.Panel.gameObject.activeSelf);
            var texts = Texts(pause.Panel);
            CollectionAssert.Contains(texts, "일시정지");
            CollectionAssert.Contains(texts, "계속");
            CollectionAssert.Contains(texts, "타이틀로");
            Assert.IsTrue(boot.clock.IsPaused, "박자 시계 정지");
            Assert.AreEqual(0f, Time.timeScale);
            double b = boot.clock.CurrentBeat;
            float d = boot.player.Distance;
            now = 5.0;
            yield return null;
            Assert.AreEqual(b, boot.clock.CurrentBeat, 1e-9, "일시정지 중 박자 멈춤");
            Assert.AreEqual(d, boot.player.Distance, 1e-4f);
            pause.ContinueButton.onClick.Invoke();
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
            now = 5.5;
            yield return null;
            Assert.AreEqual(b + 1.0, boot.clock.CurrentBeat, 1e-6, "재개 후 이어짐");
        }

        [UnityTest]
        public IEnumerator T08_Result_Cleared_ShowsCountsAndRank()
        {
            yield return LoadGame();
            var board = boot.score.Board;
            for (int i = 0; i < 18; i++) board.Add(Judgement.Perfect);
            for (int i = 0; i < 5; i++) board.Add(Judgement.Normal);
            for (int i = 0; i < 2; i++) board.Add(Judgement.Fail);
            board.ObserveSpeed(1.3f);
            boot.director.Clear();
            yield return null;
            var r = boot.result;
            Assert.IsTrue(r.Shown);
            Assert.AreEqual("스테이지 클리어", r.TitleText.text);
            Assert.AreEqual("PERFECT 18", r.PerfectText.text);
            Assert.AreEqual("NORMAL 5", r.NormalText.text);
            Assert.AreEqual("FAIL 2", r.FailText.text);
            Assert.AreEqual("최고 속도 x1.30", r.MaxSpeedText.text);
            Assert.AreEqual("점수 2050", r.ScoreText.text, "방향키 100×18 + 50×5 (R25)");
            Assert.AreEqual("랭크 " + ScoreBoard.Rank(true, 2050, board.MaxScore), r.RankText.text, "만점 대비 비율 (R25)");
            Assert.AreEqual("랭크 F", r.RankText.text, "2050 / 만점 33100 → F");
            var texts = Texts(r.Panel);
            CollectionAssert.Contains(texts, "다시 하기");
            CollectionAssert.Contains(texts, "타이틀로");
        }

        [UnityTest]
        public IEnumerator T08_Result_GameOver_RankDash()
        {
            yield return LoadGame();
            boot.director.AllowDeathRewind = false;   // beta1-1: GDD-beta B9.3 기본은 챕터 되감기. 게임 오버 결과 화면(GDD 4.8)을 검사
            boot.director.Health.Damage();
            boot.director.Health.Damage();
            boot.director.Health.Damage();
            yield return null;
            var r = boot.result;
            Assert.IsTrue(r.Shown);
            Assert.AreEqual("게임 오버", r.TitleText.text);
            Assert.AreEqual("랭크 -", r.RankText.text);
        }

        [UnityTest]
        public IEnumerator T08_SpeedLines_AndFovBonus()
        {
            yield return LoadGame();
            yield return null;
            Assert.AreEqual(0f, boot.speedFx.Intensity, 1e-5f);
            Assert.IsFalse(boot.speedFx.lines.enabled);
            Assert.AreEqual(70f, boot.playerCamera.fieldOfView, 0.01f);

            boot.clock.ApplyJudgement(Judgement.Perfect);          // 1.05
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            yield return null; yield return null;
            Assert.AreEqual(0f, boot.speedFx.Intensity, 1e-5f, "1.10 미만");

            for (int i = 0; i < 2; i++) boot.clock.ApplyJudgement(Judgement.Perfect);   // 1.15
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            yield return null; yield return null;
            Assert.AreEqual(0.5f, boot.speedFx.Intensity, 1e-3f);
            Assert.IsTrue(boot.speedFx.lines.enabled, "집중선 표시");
            Assert.AreEqual(0.5f, boot.speedFx.lines.color.a, 1e-3f);
            Assert.IsNotNull(boot.speedFx.lines.sprite);
            Assert.AreEqual(72.5f, boot.playerCamera.fieldOfView, 0.05f, "FOV +2.5°");

            for (int i = 0; i < 3; i++) boot.clock.ApplyJudgement(Judgement.Perfect);   // 1.30
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            yield return null; yield return null;
            Assert.AreEqual(1f, boot.speedFx.Intensity, 1e-3f);
            Assert.AreEqual(75f, boot.playerCamera.fieldOfView, 0.05f, "FOV 최대 +5°");
        }
    }
}
