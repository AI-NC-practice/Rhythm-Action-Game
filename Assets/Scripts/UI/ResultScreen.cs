using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace RhythmActionGame
{

    /// <summary>결과 화면 (GDD 4.8). 스테이지 클리어나 게임 오버가 되면 나타난다.</summary>
    public class ResultScreen : MonoBehaviour
    {
        public StageDirector director;
        public ScoreKeeper score;
        public BeatClock clock;

        public RectTransform Panel { get; private set; }
        public Text TitleText { get; private set; }
        public Text PerfectText { get; private set; }
        public Text NormalText { get; private set; }
        public Text FailText { get; private set; }
        public Text MaxSpeedText { get; private set; }
        public Text ScoreText { get; private set; }
        public Text RankText { get; private set; }
        public Button RetryButton { get; private set; }
        public Button TitleButton { get; private set; }
        public bool Shown => Panel != null && Panel.gameObject.activeSelf;

        void Awake() => Build();

        void OnEnable() { if (director != null) director.StateChanged += OnState; }
        void OnDisable() { if (director != null) director.StateChanged -= OnState; }

        void Build()
        {
            if (Panel != null) return;
            Panel = UiWidgets.Panel(transform, "ResultPanel", new Color(0.03f, 0.03f, 0.05f, 0.85f));
            TitleText = UiWidgets.Label(Panel, "ResultTitle", "", 88, new Vector2(0, 360));
            PerfectText = UiWidgets.Label(Panel, "PerfectCount", "", 44, new Vector2(0, 220));
            NormalText = UiWidgets.Label(Panel, "NormalCount", "", 44, new Vector2(0, 160));
            FailText = UiWidgets.Label(Panel, "FailCount", "", 44, new Vector2(0, 100));
            MaxSpeedText = UiWidgets.Label(Panel, "MaxSpeed", "", 44, new Vector2(0, 30));
            ScoreText = UiWidgets.Label(Panel, "ResultScore", "", 44, new Vector2(0, -30));
            RankText = UiWidgets.Label(Panel, "Rank", "", 72, new Vector2(0, -120));
            RetryButton = UiWidgets.Button(Panel, "RetryButton", UiText.Retry, new Vector2(-200, -280), Retry);
            TitleButton = UiWidgets.Button(Panel, "TitleButton", UiText.ToTitle, new Vector2(200, -280), ToTitle);
            Panel.gameObject.SetActive(false);
        }

        void OnState(StageState s)
        {
            if (s == StageState.Cleared || s == StageState.GameOver) Show(s == StageState.Cleared);
        }

        public void Show(bool cleared)
        {
            var b = score.Board;
            TitleText.text = cleared ? UiText.Cleared : UiText.GameOver;
            PerfectText.text = UiText.PerfectCount(b.Perfect);
            NormalText.text = UiText.NormalCount(b.Normal);
            FailText.text = UiText.FailCount(b.Fail);
            MaxSpeedText.text = UiText.MaxSpeed(b.MaxSpeed);
            ScoreText.text = UiText.Score(b.Score);
            RankText.text = UiText.Rank(b.Rank(cleared));
            Panel.gameObject.SetActive(true);
            if (clock != null) clock.StopClock();
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        public void ToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        }
    }
}
