using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>타이틀 화면 (GDD 4.8): "시작", "설정", "종료" + 설정 화면("타이밍 보정", "음량", "돌아가기").</summary>
    public class TitleMenu : MonoBehaviour
    {
        public RectTransform MainPanel { get; private set; }
        public RectTransform SettingsPanel { get; private set; }
        public Button StartButton { get; private set; }
        public Button SettingsButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Button BackButton { get; private set; }
        public Slider OffsetSlider { get; private set; }
        public Slider VolumeSlider { get; private set; }
        public Text OffsetValue { get; private set; }
        public Text VolumeValue { get; private set; }
        /// <summary>예고음 켜기/끄기 (GDD-beta B17.2).</summary>
        public Button CueTicksButton { get; private set; }
        public Text CueTicksValue { get; private set; }
        /// <summary>음악 켜기/끄기 (효과음만 듣기, 테스트용).</summary>
        public Button MusicButton { get; private set; }
        public Text MusicValue { get; private set; }

        /// <summary>테스트에서 종료 대신 기록만 한다.</summary>
        public bool QuitRequested { get; private set; }
        public bool allowQuit = true;

        void Awake() => Build();

        void Start()
        {
            // 화면 없는 빌드 검사용: 실행 인자 -startGame 이면 바로 게임 씬으로
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-startGame") >= 0) StartGame();
        }

        void Build()
        {
            if (MainPanel != null) return;
            MainPanel = new GameObject("MainMenu", typeof(RectTransform)).GetComponent<RectTransform>();
            MainPanel.SetParent(transform, false);
            UiWidgets.Stretch(MainPanel);
            StartButton = UiWidgets.Button(MainPanel, "StartButton", UiText.Start, new Vector2(0, -20), StartGame);
            SettingsButton = UiWidgets.Button(MainPanel, "SettingsButton", UiText.Settings, new Vector2(0, -120), OpenSettings);
            QuitButton = UiWidgets.Button(MainPanel, "QuitButton", UiText.Quit, new Vector2(0, -220), Quit);

            SettingsPanel = UiWidgets.Panel(transform, "SettingsPanel", new Color(0.05f, 0.05f, 0.07f, 0.92f));
            UiWidgets.Label(SettingsPanel, "SettingsTitle", UiText.Settings, 64, new Vector2(0, 300));
            UiWidgets.Label(SettingsPanel, "OffsetLabel", UiText.TimingOffset, 40, new Vector2(-360, 120), TextAnchor.MiddleRight, new Vector2(320, 60));
            OffsetSlider = UiWidgets.Slider(SettingsPanel, "OffsetSlider",
                GameConfig.TimingOffsetMinMs / GameConfig.TimingOffsetStepMs, GameConfig.TimingOffsetMaxMs / GameConfig.TimingOffsetStepMs,
                GameSettings.TimingOffsetMs / GameConfig.TimingOffsetStepMs, new Vector2(80, 120), v => SetOffset(Mathf.RoundToInt(v) * GameConfig.TimingOffsetStepMs));
            OffsetValue = UiWidgets.Label(SettingsPanel, "OffsetValue", GameSettings.FormatOffset(GameSettings.TimingOffsetMs), 40, new Vector2(470, 120), TextAnchor.MiddleLeft, new Vector2(200, 60));
            UiWidgets.Label(SettingsPanel, "VolumeLabel", UiText.Volume, 40, new Vector2(-360, 0), TextAnchor.MiddleRight, new Vector2(320, 60));
            VolumeSlider = UiWidgets.Slider(SettingsPanel, "VolumeSlider", GameConfig.VolumeMin, GameConfig.VolumeMax, GameSettings.Volume, new Vector2(80, 0), v => SetVolume(Mathf.RoundToInt(v)));
            VolumeValue = UiWidgets.Label(SettingsPanel, "VolumeValue", GameSettings.Volume.ToString(), 40, new Vector2(470, 0), TextAnchor.MiddleLeft, new Vector2(200, 60));
            UiWidgets.Label(SettingsPanel, "CueTicksLabel", UiText.CueTicks, 40, new Vector2(-360, -110), TextAnchor.MiddleRight, new Vector2(320, 60));
            CueTicksButton = UiWidgets.Button(SettingsPanel, "CueTicksButton", OnOff(GameSettings.CueTicks), new Vector2(80, -110), ToggleCueTicks);
            CueTicksValue = CueTicksButton.GetComponentInChildren<Text>();
            UiWidgets.Label(SettingsPanel, "MusicLabel", UiText.Music, 40, new Vector2(-360, -220), TextAnchor.MiddleRight, new Vector2(320, 60));
            MusicButton = UiWidgets.Button(SettingsPanel, "MusicButton", OnOff(GameSettings.MusicOn), new Vector2(80, -220), ToggleMusic);
            MusicValue = MusicButton.GetComponentInChildren<Text>();
            BackButton = UiWidgets.Button(SettingsPanel, "BackButton", UiText.Back, new Vector2(0, -340), CloseSettings);
            SettingsPanel.gameObject.SetActive(false);
        }

        public void StartGame() => SceneManager.LoadScene("Game");

        public void OpenSettings()
        {
            OffsetSlider.SetValueWithoutNotify(GameSettings.TimingOffsetMs / GameConfig.TimingOffsetStepMs);
            VolumeSlider.SetValueWithoutNotify(GameSettings.Volume);
            OffsetValue.text = GameSettings.FormatOffset(GameSettings.TimingOffsetMs);
            VolumeValue.text = GameSettings.Volume.ToString();
            CueTicksValue.text = OnOff(GameSettings.CueTicks);
            MusicValue.text = OnOff(GameSettings.MusicOn);
            MainPanel.gameObject.SetActive(false);
            SettingsPanel.gameObject.SetActive(true);
        }

        public void CloseSettings()
        {
            SettingsPanel.gameObject.SetActive(false);
            MainPanel.gameObject.SetActive(true);
        }

        public void SetOffset(int ms)
        {
            GameSettings.TimingOffsetMs = ms;
            OffsetValue.text = GameSettings.FormatOffset(GameSettings.TimingOffsetMs);
        }

        public void SetVolume(int v)
        {
            GameSettings.Volume = v;
            VolumeValue.text = GameSettings.Volume.ToString();
            AudioListener.volume = GameSettings.Volume / 100f;
        }

        static string OnOff(bool on) => on ? UiText.On : UiText.Off;

        /// <summary>예고음 켜기/끄기를 바꾸고 저장한다.</summary>
        /// <summary>음악 켜기/끄기를 바꾸고 저장한다 (효과음은 그대로).</summary>
        public void ToggleMusic()
        {
            GameSettings.MusicOn = !GameSettings.MusicOn;
            MusicValue.text = OnOff(GameSettings.MusicOn);
        }

        public void ToggleCueTicks()
        {
            GameSettings.CueTicks = !GameSettings.CueTicks;
            CueTicksValue.text = OnOff(GameSettings.CueTicks);
        }

        public void Quit()
        {
            QuitRequested = true;
            if (!allowQuit) return;
#if UNITY_EDITOR
            if (!Application.isBatchMode) UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
