using System;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 무사 자세 게이지 (GDD-beta B11.3). 무사 결투 동안에만 화면 위 가운데에 보인다.
    /// 3단계가 끝나 자세가 무너지면 게이지가 붉게 번쩍인다.
    /// </summary>
    public class PostureBar : MonoBehaviour
    {
        public StageDirector director;

        public RectTransform Root { get; private set; }
        public Image Back { get; private set; }
        public Image Fill { get; private set; }
        public Image Flash { get; private set; }
        public Text Title { get; private set; }

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public Func<float> RealTime = () => Time.unscaledTime;

        static readonly Color FillColor = new Color(1f, 0.78f, 0.25f, 1f);
        static readonly Color BackColor = new Color(0f, 0f, 0f, 0.55f);

        float brokenAt = -999f;
        bool subscribed;

        /// <summary>보이는 중인지.</summary>
        public bool Visible => Root != null && Root.gameObject.activeSelf;
        /// <summary>채워진 비율 (0~1).</summary>
        public float Ratio { get; private set; }
        /// <summary>무너짐 번쩍임 세기 (0~1).</summary>
        public float FlashAlpha => Flash != null ? Flash.color.a : 0f;

        void Awake() => Build();

        void Build()
        {
            if (Root != null) return;
            Root = new GameObject("PostureBar", typeof(RectTransform)).GetComponent<RectTransform>();
            Root.SetParent(transform, false);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 1f);
            Root.pivot = new Vector2(0.5f, 1f);
            Root.anchoredPosition = new Vector2(0f, -GameConfig.PostureBarTopPx);
            Root.sizeDelta = new Vector2(GameConfig.PostureBarWidthPx, GameConfig.PostureBarHeightPx);
            Back = Bar("Back", BackColor, GameConfig.PostureBarWidthPx + 6f, GameConfig.PostureBarHeightPx + 6f, 0.5f);
            Fill = Bar("Fill", FillColor, 0f, GameConfig.PostureBarHeightPx, 0f);
            Flash = Bar("Flash", new Color(GameConfig.Fx.Vermilion.r, GameConfig.Fx.Vermilion.g, GameConfig.Fx.Vermilion.b, 0f),
                GameConfig.PostureBarWidthPx + 24f, GameConfig.PostureBarHeightPx + 24f, 0.5f);
            Title = UiFactory.CreateText(Root, "Title", UiText.PostureTitle, 26, Color.white, new Vector2(300, 40));
            Title.rectTransform.anchoredPosition = new Vector2(0f, GameConfig.PostureBarHeightPx * 0.5f + 22f);
            Root.gameObject.SetActive(false);
        }

        Image Bar(string name, Color c, float w, float h, float pivotX)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(Root, false);
            img.raycastTarget = false;
            img.color = c;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(pivotX, 0.5f);
            rt.pivot = new Vector2(pivotX, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            return img;
        }

        void OnEnable() => Subscribe();
        void OnDisable()
        {
            if (director != null && subscribed) director.PostureBroken -= OnBroken;
            subscribed = false;
        }

        void Subscribe()
        {
            if (subscribed || director == null) return;
            director.PostureBroken += OnBroken;
            subscribed = true;
        }

        void OnBroken(StageDirector.Encounter e) => brokenAt = RealTime();

        void Update()
        {
            Build();
            Subscribe();
            var e = director != null ? director.CurrentEncounter : null;
            bool show = e != null && e.IsBossDuel && !e.Done;
            if (Root.gameObject.activeSelf != show) Root.gameObject.SetActive(show);
            if (!show) { Ratio = 0f; brokenAt = -999f; return; }
            Ratio = Mathf.Clamp01(e.Posture / GameConfig.PostureFull);
            Fill.rectTransform.sizeDelta = new Vector2(GameConfig.PostureBarWidthPx * Ratio, GameConfig.PostureBarHeightPx);
            // 무너짐: 0.6초 동안 붉게 번쩍이며 사라짐, 그 뒤에도 게이지는 붉은빛
            float age = RealTime() - brokenAt;
            float k = age >= 0f && age < GameConfig.PostureBreakFlashSeconds ? 1f - age / GameConfig.PostureBreakFlashSeconds : 0f;
            var fc = Flash.color; fc.a = 0.9f * k; Flash.color = fc;
            Fill.color = e.PostureBroken ? Color.Lerp(GameConfig.Fx.Vermilion, Color.white, k) : FillColor;
        }
    }
}
