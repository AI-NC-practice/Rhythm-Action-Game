using UnityEngine;
using UnityEngine.UI;
namespace RhythmActionGame
{

    /// <summary>플레이 화면 (GDD 4.8): 왼쪽 위 하트 3개, 오른쪽 위 "x1.00", 그 아래 점수, 가운데 아래 "콤보 N".</summary>
    public class GameHud : MonoBehaviour
    {
        public StageDirector director;
        public BeatClock clock;
        public ScoreKeeper score;

        public Image[] Hearts { get; private set; }
        public Text SpeedText { get; private set; }
        public Text ScoreText { get; private set; }
        public Text ComboText { get; private set; }

        static readonly Color HeartFull = new Color(0.9f, 0.15f, 0.2f, 1f);
        static readonly Color HeartEmpty = new Color(0.4f, 0.4f, 0.4f, 0.35f);

        public int FullHearts
        {
            get { int n = 0; foreach (var h in Hearts) if (h.color.a > 0.9f) n++; return n; }
        }

        void Awake() => Build();

        void Build()
        {
            if (Hearts != null) return;
            Hearts = new Image[GameConfig.MaxHp];
            for (int i = 0; i < Hearts.Length; i++)
            {
                var h = UiFactory.CreateImage(transform, $"Heart_{i}", UiFactory.Disc, HeartFull, 56);
                var rt = h.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(60 + i * 70, -60);
                Hearts[i] = h;
            }
            SpeedText = Corner("Speed", new Vector2(1, 1), new Vector2(-40, -60), TextAnchor.MiddleRight, 52);
            ScoreText = Corner("Score", new Vector2(1, 1), new Vector2(-40, -130), TextAnchor.MiddleRight, 40);
            ComboText = Corner("Combo", new Vector2(0.5f, 0), new Vector2(0, 120), TextAnchor.MiddleCenter, 56);
            Refresh();
        }

        Text Corner(string name, Vector2 anchor, Vector2 pos, TextAnchor align, int size)
        {
            var t = UiFactory.CreateText(transform, name, "", size, Color.white, new Vector2(500, size * 1.5f));
            t.alignment = align;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x == 0.5f ? 0.5f : anchor.x, 0.5f);
            rt.anchoredPosition = pos;
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);
            return t;
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (Hearts == null) return;
            int hp = director != null ? director.Health.Hp : GameConfig.MaxHp;
            for (int i = 0; i < Hearts.Length; i++) Hearts[i].color = i < hp ? HeartFull : HeartEmpty;
            SpeedText.text = UiText.Speed(clock != null ? clock.TargetSpeed : GameConfig.SpeedStart);   // 판정 즉시 새 값 (GDD-beta B15.2)
            var b = score != null ? score.Board : null;
            ScoreText.text = (b != null ? b.Score : 0).ToString();
            string combo = UiText.Combo(b != null ? b.Combo : 0);
            ComboText.text = combo;
            ComboText.enabled = combo.Length > 0;
        }
    }
}
