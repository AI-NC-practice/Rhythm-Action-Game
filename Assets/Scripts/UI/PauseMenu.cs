using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace RhythmActionGame
{
    /// <summary>일시정지 (GDD 4.8): Esc → "일시정지", "계속", "타이틀로". 음악과 박자 시계도 멈춘다.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public BeatClock clock;
        public StageDirector director;
        public SlowMotion slowMotion;

        public RectTransform Panel { get; private set; }
        public Button ContinueButton { get; private set; }
        public Button TitleButton { get; private set; }
        /// <summary>음악 켜기/끄기 (효과음만 듣기, 테스트용).</summary>
        public Button MusicButton { get; private set; }
        public Text MusicText { get; private set; }
        public bool IsPaused { get; private set; }

        void Awake() => Build();

        void Build()
        {
            if (Panel != null) return;
            Panel = UiWidgets.Panel(transform, "PausePanel", new Color(0, 0, 0, 0.6f));
            UiWidgets.Label(Panel, "PauseTitle", UiText.Paused, 80, new Vector2(0, 180));
            ContinueButton = UiWidgets.Button(Panel, "ContinueButton", UiText.Continue, new Vector2(0, 0), Resume);
            TitleButton = UiWidgets.Button(Panel, "TitleButton", UiText.ToTitle, new Vector2(0, -110), ToTitle);
            MusicButton = UiWidgets.Button(Panel, "MusicButton", MusicLabel(), new Vector2(0, -220), ToggleMusic);
            MusicText = MusicButton.GetComponentInChildren<Text>();
            Panel.gameObject.SetActive(false);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Toggle();
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            if (director != null && (director.State == StageState.GameOver || director.State == StageState.Cleared)) return;
            IsPaused = true;
            if (clock != null) clock.Pause();
            Time.timeScale = 0f;
            MusicText.text = MusicLabel();
            Panel.gameObject.SetActive(true);
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = slowMotion != null && slowMotion.IsActive ? GameConfig.SlowMotionTimeScale : 1f;
            if (clock != null) clock.Resume();
            Panel.gameObject.SetActive(false);
        }

        static string MusicLabel() => $"{UiText.Music} {(GameSettings.MusicOn ? UiText.On : UiText.Off)}";

        /// <summary>음악 켜기/끄기 (효과음·히트사운드는 그대로, 곡과 박자는 계속).</summary>
        public void ToggleMusic()
        {
            GameSettings.MusicOn = !GameSettings.MusicOn;
            MusicText.text = MusicLabel();
        }

        public void ToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }
    }
}
