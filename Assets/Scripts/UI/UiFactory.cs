using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>코드로 UI를 만들 때 쓰는 공통 도구. 한글은 Windows 맑은 고딕 동적 폰트.</summary>
    public static class UiFactory
    {
        static Font font;
        static Sprite ringSprite, discSprite, glowSprite, chevronSprite;

        /// <summary>부드러운 빛 고리 스프라이트 (흰색, 반지름 75% 부근이 가장 밝음). 판정 원 바깥 빛 (GDD-beta B1.3).</summary>
        public static Sprite Glow
        {
            get
            {
                if (glowSprite != null) return glowSprite;
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Glow", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                        float k = (d - 0.75f) / 0.16f;
                        float a = d > 1f ? 0f : Mathf.Exp(-k * k);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return glowSprite;
            }
        }

        /// <summary>오른쪽을 가리키는 꺾쇠(▶) 스프라이트 (흰색). 방향 꺾쇠 (GDD-beta B1.3).</summary>
        public static Sprite Chevron
        {
            get
            {
                if (chevronSprite != null) return chevronSprite;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Chevron", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        // 왼쪽 변(x=8)에서 오른쪽 꼭짓점(x=56, 가운데)으로 좁아지는 삼각형
                        float fx = x + 0.5f, fy = Mathf.Abs(y + 0.5f - size * 0.5f);
                        float halfH = (56f - fx) / 48f * 26f;
                        float a = fx < 8f || fx > 56f ? 0f : Mathf.Clamp01(halfH - fy + 0.5f);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px);
                tex.Apply();
                chevronSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return chevronSprite;
            }
        }

        public static Font Font
        {
            get
            {
                if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Arial" }, 32);
                return font;
            }
        }

        /// <summary>테두리만 있는 원 스프라이트 (흰색, 두께는 반지름의 8%).</summary>
        public static Sprite Ring
        {
            get
            {
                if (ringSprite == null) ringSprite = MakeCircle(256, 0.08f, "Ring");
                return ringSprite;
            }
        }

        /// <summary>속이 찬 원 스프라이트 (흰색).</summary>
        public static Sprite Disc
        {
            get
            {
                if (discSprite == null) discSprite = MakeCircle(128, 1f, "Disc");
                return discSprite;
            }
        }

        static Sprite MakeCircle(int size, float thickness, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            float inner = r * (1f - thickness);
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    // 바깥과 안쪽 경계를 1px 부드럽게
                    float a = Mathf.Clamp01(r - d) * Mathf.Clamp01(d - inner + 1f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, GameConfig.UiReferenceHeight);
            scaler.matchWidthOrHeight = 1f; // 높이 기준 → 1080p 기준 px가 그대로 맞는다
            return canvas;
        }

        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, float diameter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            return img;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.rectTransform.sizeDelta = box;
            return t;
        }
    }
}
