using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 장구 레인 (GDD-beta B18.2): 달리기 장단 노드를 화면 아래 가로 레인에 보여 준다(태고의 달인 방식).
    /// 노드는 오른쪽에서 왼쪽 판정 표시로 박자에 맞춰 흘러온다. Q 궁편(푸른 원), W 채편(붉은 원) (B18.5).
    /// 판정 원(JudgeRingView)은 장단 노드를 그리지 않는다.
    /// </summary>
    public class JangdanLaneView : MonoBehaviour
    {
        public JudgeSystem judge;
        public BeatClock clock;

        RectTransform lane;
        Image hitMarker, flash;
        Text flashText;
        readonly List<Image> pool = new List<Image>();
        float flashUntil;

        /// <summary>지금 레인에 그려진 노드 수 (테스트용).</summary>
        public int VisibleCount { get; private set; }
        /// <summary>노드의 레인 위 x (판정 표시 기준, px). 테스트용.</summary>
        public static float NoteX(double beatsAhead) => (float)(beatsAhead * GameConfig.Jangdan.LanePxPerBeat);
        public RectTransform Lane { get { Build(); return lane; } }
        public Text FlashText { get { Build(); return flashText; } }

        void Awake() => Build();
        void OnEnable() { if (judge != null) judge.CueJudged += OnJudged; }
        void OnDisable() { if (judge != null) judge.CueJudged -= OnJudged; }

        void Build()
        {
            if (lane != null) return;
            lane = new GameObject("JangdanLane", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            lane.SetParent(transform, false);
            lane.anchorMin = lane.anchorMax = new Vector2(0.5f, 0f);
            lane.sizeDelta = new Vector2(GameConfig.Jangdan.LaneWidthPx, GameConfig.Jangdan.LaneHeightPx);
            lane.anchoredPosition = new Vector2(0, GameConfig.Jangdan.LaneBottomPx + GameConfig.Jangdan.LaneHeightPx * 0.5f);
            var bg = lane.GetComponent<Image>(); bg.color = new Color(0f, 0f, 0f, 0.35f); bg.raycastTarget = false;
            hitMarker = UiFactory.CreateImage(lane, "HitMarker", UiFactory.Ring, new Color(1f, 0.92f, 0.7f, 0.9f), GameConfig.Jangdan.NoteSizePx + 10);
            hitMarker.rectTransform.anchoredPosition = new Vector2(HitX, 0);
            flash = UiFactory.CreateImage(lane, "HitFlash", UiFactory.Glow, new Color(1, 1, 1, 0), GameConfig.Jangdan.NoteSizePx * 2.2f);
            flash.rectTransform.anchoredPosition = new Vector2(HitX, 0);
            flashText = UiFactory.CreateText(lane, "HitText", "", 22, Color.white, new Vector2(200, 30));
            flashText.rectTransform.anchoredPosition = new Vector2(HitX, GameConfig.Jangdan.LaneHeightPx * 0.5f + 18);
        }

        static float HitX => -GameConfig.Jangdan.LaneWidthPx * 0.5f + GameConfig.Jangdan.HitMarkerInsetPx;

        Image Note(int i)
        {
            while (pool.Count <= i)
            {
                var img = UiFactory.CreateImage(lane, "Note", UiFactory.Disc, Color.white, GameConfig.Jangdan.NoteSizePx);
                var t = UiFactory.CreateText(img.transform, "Arrow", "", 30, Color.white, new Vector2(60, 60));
                pool.Add(img);
            }
            return pool[i];
        }

        void LateUpdate() => Refresh();

        /// <summary>레인을 다시 그린다 (LateUpdate, 테스트에서 직접 부름).</summary>
        public void Refresh()
        {
            Build();
            int n = 0;
            if (judge != null && clock != null && clock.IsRunning)
            {
                double now = clock.CurrentBeat;
                foreach (var cue in judge.Cues)
                {
                    if (!(cue.Tag is RhythmNote) || cue.Judged) continue;
                    double ahead = cue.TargetBeat - now;
                    if (ahead > GameConfig.Jangdan.LaneLookAheadBeats || ahead < -0.3) continue;
                    var img = Note(n++);
                    img.gameObject.SetActive(true);
                    bool left = cue.Key == ArrowKey.Kung;
                    img.color = left ? GameConfig.Jangdan.KungColor : GameConfig.Jangdan.DeokColor;
                    img.rectTransform.anchoredPosition = new Vector2(HitX + NoteX(ahead), 0);
                    var t = img.GetComponentInChildren<Text>();
                    t.text = left ? "Q" : "W";   // 장구 키 (B18.5)
                }
            }
            for (int i = n; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
            VisibleCount = n;
            float u = Mathf.Clamp01((flashUntil - Time.unscaledTime) / 0.25f);
            var c = flash.color; c.a = u * 0.8f; flash.color = c;
            if (u <= 0f) flashText.text = "";
        }

        void OnJudged(JudgeCue cue, Judgement r)
        {
            if (!(cue.Tag is RhythmNote)) return;
            Build();
            flash.color = r == Judgement.Perfect ? new Color(1f, 0.85f, 0.3f, 0.8f) : r == Judgement.Normal ? new Color(0.7f, 0.9f, 1f, 0.8f) : new Color(1f, 0.25f, 0.2f, 0.8f);
            flashText.text = r == Judgement.Perfect ? "PERFECT" : r == Judgement.Normal ? "NORMAL" : "FAIL";
            flashUntil = Time.unscaledTime + 0.25f;
        }
    }
}
