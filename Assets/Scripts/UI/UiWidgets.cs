using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>버튼·슬라이더·패널을 코드로 만든다.</summary>
    public static class UiWidgets
    {
        static Sprite white, speedLines;

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var t = new Texture2D(4, 4) { name = "White" };
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(px); t.Apply();
                    white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                }
                return white;
            }
        }

        /// <summary>가장자리에서 가운데로 모이는 집중선 (가운데는 투명).</summary>
        public static Sprite SpeedLines
        {
            get
            {
                if (speedLines != null) return speedLines;
                const int W = 512, H = 288;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "SpeedLines", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[W * H];
                var rnd = new System.Random(7);
                const int Lines = 90;
                var angles = new float[Lines];
                var widths = new float[Lines];
                for (int i = 0; i < Lines; i++) { angles[i] = (float)(i * Mathf.PI * 2 / Lines + rnd.NextDouble() * 0.05); widths[i] = 0.004f + (float)rnd.NextDouble() * 0.008f; }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float u = (x + 0.5f) / W * 2f - 1f, v = ((y + 0.5f) / H * 2f - 1f) * H / W;
                        float r = Mathf.Sqrt(u * u + v * v);
                        float ang = Mathf.Atan2(v, u);
                        if (ang < 0) ang += Mathf.PI * 2;
                        float best = 1f;
                        int idx = Mathf.RoundToInt(ang / (Mathf.PI * 2) * Lines) % Lines;
                        for (int k = -1; k <= 1; k++)
                        {
                            int j = (idx + k + Lines) % Lines;
                            float d = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, angles[j] * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                            best = Mathf.Min(best, d / widths[j]);
                        }
                        float line = Mathf.Clamp01(1f - best);
                        float edge = Mathf.Clamp01((r - 0.55f) / 0.45f);   // 가운데 55%는 비움
                        px[y * W + x] = new Color32(255, 255, 255, (byte)(255 * line * edge));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                speedLines = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
                return speedLines;
            }
        }

        public static RectTransform Panel(Transform parent, string name, Color color, bool fullScreen = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = White;
            img.color = color;
            var rt = (RectTransform)go.transform;
            if (fullScreen) Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        public static Text Label(Transform parent, string name, string text, int size, Vector2 pos, TextAnchor align = TextAnchor.MiddleCenter, Vector2? box = null)
        {
            var t = UiFactory.CreateText(parent, name, text, size, Color.white, box ?? new Vector2(900, size * 1.6f));
            t.alignment = align;
            t.rectTransform.anchoredPosition = pos;
            return t;
        }

        public static Button Button(Transform parent, string name, string label, Vector2 pos, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(360, 84);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = White;
            // 작업 R38: 그림 색은 흰색, 상태별 색은 ColorBlock에 (예전에는 그림 색 0.15가 ColorBlock에 곱해져 마우스를 올려도 거의 안 바뀌었다)
            img.color = Color.white;
            var b = go.GetComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = b.colors;
            colors.normalColor = GameConfig.MenuButton.ButtonNormal;
            colors.highlightedColor = GameConfig.MenuButton.ButtonHover;
            colors.pressedColor = GameConfig.MenuButton.ButtonPressed;
            colors.selectedColor = GameConfig.MenuButton.ButtonNormal;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = GameConfig.MenuButton.FadeSeconds;
            b.colors = colors;
            var t = UiFactory.CreateText(rt, "Label", label, 40, Color.white, rt.sizeDelta);
            go.AddComponent<UiHover>().label = t;
            if (onClick != null) b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>가로 슬라이더 (배경, 채움, 손잡이).</summary>
        public static Slider Slider(Transform parent, string name, float min, float max, float value, Vector2 pos, Action<float> onChanged)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(520, 36);
            rt.anchoredPosition = pos;

            var bg = Panel(rt, "Background", new Color(1, 1, 1, 0.2f));
            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(rt, false); Stretch(fillArea);
            var fill = Panel(fillArea, "Fill", new Color(0.95f, 0.75f, 0.35f));
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(rt, false); Stretch(handleArea);
            var handle = Panel(handleArea, "Handle", Color.white, false);
            handle.sizeDelta = new Vector2(28, 48);

            var s = go.GetComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handle.GetComponent<Image>();
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.navigation = new Navigation { mode = Navigation.Mode.None };
            s.minValue = min; s.maxValue = max; s.wholeNumbers = true;
            s.SetValueWithoutNotify(value);
            if (onChanged != null) s.onValueChanged.AddListener(v => onChanged(v));
            return s;
        }
    }
}
