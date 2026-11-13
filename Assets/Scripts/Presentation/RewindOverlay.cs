using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 테이프 되감기 겹화면 (GDD-beta B10.3). 되감는 0.8초 동안
    /// 오른쪽 → 왼쪽으로 흐르는 먹 줄기 3개, 위아래로 일렁이는 가로 줄무늬, 왼쪽 위에서 깜빡이는 "◀◀"를 보여 준다.
    /// 판정 문구·이펙트 층(Canvas 10)에 그려 판정 원 층보다 아래다.
    /// </summary>
    public class RewindOverlay : MonoBehaviour
    {
        public const string Icon = "◀◀";

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        RectTransform root;
        readonly List<Image> streaks = new List<Image>();
        Image scanlines;
        Text icon;
        float age = -1f;

        static readonly float[] StreakY = { 260f, 0f, -240f };
        static readonly float[] StreakSpeed = { 1.0f, 1.35f, 0.8f };

        public bool Active => age >= 0f && age < GameConfig.RewindSeconds;
        public IReadOnlyList<Image> Streaks => streaks;
        public Image Scanlines => scanlines;
        public Text IconText => icon;

        static float W => GameConfig.UiReferenceHeight * 16f / 9f;

        /// <summary>layer(Canvas 10) 아래에 겹화면을 만든다 (한 번만).</summary>
        public void Build(RectTransform layer)
        {
            if (root != null) return;
            root = new GameObject("RewindOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(layer, false);
            UiWidgets.Stretch(root);
            scanlines = new GameObject("Scanlines", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            scanlines.transform.SetParent(root, false);
            UiWidgets.Stretch(scanlines.rectTransform);
            scanlines.sprite = VfxFactory.Scanlines;
            scanlines.type = Image.Type.Tiled;
            scanlines.color = new Color(0.85f, 0.9f, 1f, 0.22f);
            scanlines.raycastTarget = false;
            for (int i = 0; i < StreakY.Length; i++)
            {
                var img = UiFactory.CreateImage(root, "Streak" + i, VfxFactory.InkSlash, VfxFactory.Ink, 100f);
                img.rectTransform.sizeDelta = new Vector2(W * 0.9f, 220f);
                img.rectTransform.localRotation = Quaternion.Euler(0, 180f, 0);   // 거꾸로 흐르는 붓 방향
                streaks.Add(img);
            }
            icon = UiFactory.CreateText(root, "Icon", Icon, 96, Color.white, new Vector2(260, 120));
            icon.alignment = TextAnchor.MiddleLeft;
            var rt = icon.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(48f, -40f);
            root.gameObject.SetActive(false);
        }

        public void Play()
        {
            if (root == null) return;
            age = 0f;
            root.gameObject.SetActive(true);
            Pose();
        }

        public void Stop()
        {
            age = -1f;
            if (root != null) root.gameObject.SetActive(false);
        }

        void LateUpdate() => Step(DeltaTime());

        public void Step(float dt)
        {
            if (age < 0f) return;
            age += dt;
            if (age >= GameConfig.RewindSeconds) { Stop(); return; }
            Pose();
        }

        void Pose()
        {
            float u = Mathf.Clamp01(age / GameConfig.RewindSeconds);
            float fade = u < 0.85f ? 1f : (1f - u) / 0.15f;
            for (int i = 0; i < streaks.Count; i++)
            {
                // 오른쪽 밖 → 왼쪽 밖, 줄기마다 빠르기가 다름 (되감기 동안 여러 번 지나감)
                float k = Mathf.Repeat(u * StreakSpeed[i] * 2f + i * 0.33f, 1f);
                streaks[i].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(W, -W, k), StreakY[i]);
                var c = streaks[i].color; c.a = 0.55f * fade; streaks[i].color = c;
            }
            // 줄무늬 일렁임: 위아래로 흔들림
            scanlines.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Sin(age * 60f) * 6f);
            var sc = scanlines.color; sc.a = 0.22f * fade; scanlines.color = sc;
            // ◀◀ 깜빡임 (초당 6번)
            var ic = icon.color; ic.a = (Mathf.Repeat(age * 6f, 1f) < 0.6f ? 1f : 0.35f) * fade; icon.color = ic;
        }
    }
}
