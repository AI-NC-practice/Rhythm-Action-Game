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
            UpdateComboTier(b != null ? b.Combo : 0);
        }

        // ---------------- 작업 R26: 장구 콤보 단계별 표시 (보너스 단계 5·10·20·50) ----------------
        /// <summary>지금 콤보 단계 (0: 5 미만, 1: 5↑, 2: 10↑, 3: 20↑, 4: 50↑)와 단계가 오른 횟수 (테스트용).</summary>
        public int ComboTier { get; private set; }
        public int ComboTierUps { get; private set; }
        /// <summary>단계가 오를 때 튀는 정도 (1 → 0으로 줄어듦).</summary>
        public float ComboPop { get; private set; }
        Outline comboGlow;

        public static int TierOf(int combo)
        {
            int t = 0;
            for (int i = 0; i < GameConfig.JangdanComboBonusAt.Length; i++) if (combo >= GameConfig.JangdanComboBonusAt[i]) t = i + 1;
            return t;
        }

        void UpdateComboTier(int combo)
        {
            int tier = TierOf(combo);
            if (tier > ComboTier) { ComboTierUps++; ComboPop = 1f; }
            ComboTier = tier;
            ComboPop = Mathf.Max(0f, ComboPop - Time.unscaledDeltaTime / GameConfig.ComboPopSeconds);
            if (ComboText == null) return;
            var col = GameConfig.ComboTierColors[Mathf.Clamp(tier, 0, GameConfig.ComboTierColors.Length - 1)];
            ComboText.color = Color.Lerp(col, Color.white, ComboPop * 0.6f);
            float sc = GameConfig.ComboTierScales[Mathf.Clamp(tier, 0, GameConfig.ComboTierScales.Length - 1)] * (1f + GameConfig.ComboPopScale * ComboPop * ComboPop);
            ComboText.rectTransform.localScale = Vector3.one * sc;
            if (comboGlow == null) { comboGlow = ComboText.gameObject.AddComponent<Outline>(); comboGlow.effectDistance = new Vector2(3, -3); }
            comboGlow.enabled = tier >= 2;
            comboGlow.effectColor = new Color(col.r, col.g, col.b, tier >= 4 ? 0.9f : 0.55f);
            comboGlow.effectDistance = Vector2.one * (2f + tier) * (1f + ComboPop);
        }
    }
}
