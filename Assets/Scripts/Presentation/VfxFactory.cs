using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 이펙트 재료를 코드로 만든다 (GDD-beta B5 이펙트 언어: 먹·붓획, 단청 색, 불씨, 한지·기왓장 파편, 흙먼지).
    /// 텍스처는 노이즈로 가장자리를 거칠게 해 붓 질감을 낸다.
    /// </summary>
    public static class VfxFactory
    {
        /// <summary>단청 색.</summary>
        public static readonly Color DancheongTeal = new Color(0.15f, 0.85f, 0.75f);
        public static readonly Color DancheongVermilion = new Color(0.9f, 0.3f, 0.15f);
        public static readonly Color DancheongYellow = new Color(1f, 0.82f, 0.25f);
        public static readonly Color Ink = new Color(0.06f, 0.05f, 0.07f);

        static Sprite brushBurst, inkSlash, inkX, ring;

        static Sprite lightning, flare, dot, smoke, crack, scanlines;

        /// <summary>가로 줄무늬 (되감기 겹화면, 4 px마다 어두운 줄). 타일로 깐다.</summary>
        public static Sprite Scanlines
        {
            get
            {
                if (scanlines != null) return scanlines;
                const int s = 8;
                var tex = NewTex(s, "Scanlines");
                tex.wrapMode = TextureWrapMode.Repeat;
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(y % 4 == 0 ? 255 : 0));
                tex.SetPixels32(px); tex.Apply();
                return scanlines = ToSprite(tex);
            }
        }
        static Material uiAdditive;

        /// <summary>방사형 번개선: 가운데에서 바깥으로 꺾이며 뻗는 가는 빛줄기 10개 (GDD-beta B8.5).</summary>
        public static Sprite Lightning
        {
            get
            {
                if (lightning != null) return lightning;
                const int s = 256;
                var tex = NewTex(s, "Lightning");
                var a = new float[s * s];
                var rnd = new System.Random(7);
                var c = new Vector2(s * 0.5f, s * 0.5f);
                for (int b = 0; b < 10; b++)
                {
                    float ang = (b + (float)rnd.NextDouble() * 0.6f) / 10f * Mathf.PI * 2f;
                    var p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * s * 0.08f;
                    const int segs = 6;
                    float len = s * (0.36f + 0.1f * (float)rnd.NextDouble());
                    for (int k = 0; k < segs; k++)
                    {
                        float jit = ((float)rnd.NextDouble() - 0.5f) * 0.9f;
                        var dir = new Vector2(Mathf.Cos(ang + jit), Mathf.Sin(ang + jit));
                        var q = p + dir * (len / segs);
                        DrawLine(a, s, p, q, Mathf.Lerp(2.6f, 0.8f, k / (float)segs));
                        p = q;
                    }
                }
                return lightning = FromAlpha(tex, a, s);
            }
        }

        static Sprite FromAlpha(Texture2D tex, float[] a, int s)
        {
            var px = new Color32[s * s];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a[i]) * 255));
            tex.SetPixels32(px); tex.Apply();
            return ToSprite(tex);
        }

        static void DrawLine(float[] a, int s, Vector2 p, Vector2 q, float w)
        {
            int x0 = Mathf.Max(0, (int)(Mathf.Min(p.x, q.x) - w * 3)), x1 = Mathf.Min(s - 1, (int)(Mathf.Max(p.x, q.x) + w * 3));
            int y0 = Mathf.Max(0, (int)(Mathf.Min(p.y, q.y) - w * 3)), y1 = Mathf.Min(s - 1, (int)(Mathf.Max(p.y, q.y) + w * 3));
            var pq = q - p;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    var v = new Vector2(x + 0.5f, y + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(v - p, pq) / Mathf.Max(1e-4f, pq.sqrMagnitude));
                    float d = Vector2.Distance(v, p + pq * t);
                    float core = Mathf.Clamp01(w - d + 0.5f);                 // 밝은 심
                    float halo = Mathf.Exp(-(d * d) / (w * w * 4f)) * 0.45f;  // 빛 번짐
                    a[y * s + x] = Mathf.Max(a[y * s + x], Mathf.Max(core, halo));
                }
        }

        /// <summary>십자 + X 섬광: 가늘고 긴 빛줄기 8개와 부드러운 중심 (GDD-beta B8.5).</summary>
        public static Sprite Flare
        {
            get
            {
                if (flare != null) return flare;
                const int s = 256;
                var tex = NewTex(s, "Flare");
                var a = new float[s * s];
                float r = s * 0.5f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float dx = (x + 0.5f - r) / r, dy = (y + 0.5f - r) / r;
                        float d2 = dx * dx + dy * dy;
                        float core = Mathf.Exp(-d2 * 30f);
                        float cross = Mathf.Exp(-Mathf.Abs(dy) * 90f) * Mathf.Clamp01(1f - Mathf.Abs(dx))
                                    + Mathf.Exp(-Mathf.Abs(dx) * 90f) * Mathf.Clamp01(1f - Mathf.Abs(dy));
                        float u = (dx + dy) * 0.7071f, v = (dx - dy) * 0.7071f;
                        float xs = (Mathf.Exp(-Mathf.Abs(v) * 140f) * Mathf.Clamp01(1f - Mathf.Abs(u) * 1.25f)
                                  + Mathf.Exp(-Mathf.Abs(u) * 140f) * Mathf.Clamp01(1f - Mathf.Abs(v) * 1.25f)) * 0.7f;
                        a[y * s + x] = core + cross + xs;
                    }
                return flare = FromAlpha(tex, a, s);
            }
        }

        /// <summary>부드러운 빛 점 (빛 입자·궤도 점).</summary>
        public static Sprite Dot
        {
            get
            {
                if (dot != null) return dot;
                const int s = 32;
                var tex = NewTex(s, "Dot");
                var a = new float[s * s];
                float r = s * 0.5f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                        a[y * s + x] = Mathf.Exp(-d * d * 4f) * 1.2f - 0.05f;
                    }
                return dot = FromAlpha(tex, a, s);
            }
        }

        static Sprite swordIcon;
        /// <summary>
        /// 내 공격 원의 칼 아이콘 (GDD-beta B11.2): 왼쪽 아래 → 오른쪽 위로 기운 칼 (칼날·코등이·손잡이), 흰색 (색은 Image.color로).
        /// </summary>
        public static Sprite SwordIcon
        {
            get
            {
                if (swordIcon != null) return swordIcon;
                const int s = 64;
                var tex = NewTex(s, "SwordIcon");
                var a = new float[s * s];
                var dir = new Vector2(1f, 1f).normalized;
                var nrm = new Vector2(-dir.y, dir.x);
                var c = new Vector2(s * 0.5f, s * 0.5f);
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f) - c;
                        float u = Vector2.Dot(p, dir) / (s * 0.5f);    // 칼 방향 −1(손잡이 끝) ~ 1(칼끝)
                        float v = Mathf.Abs(Vector2.Dot(p, nrm)) / (s * 0.5f);
                        float alpha = 0f;
                        if (u > -0.3f && u < 0.92f)                                  // 칼날: 끝으로 갈수록 가늘어짐
                        {
                            float w = Mathf.Lerp(0.16f, 0.04f, (u + 0.3f) / 1.22f);
                            alpha = Mathf.Clamp01((w - v) * s * 0.5f);
                        }
                        if (u > -0.42f && u <= -0.3f) alpha = Mathf.Max(alpha, Mathf.Clamp01((0.32f - v) * s * 0.5f));   // 코등이
                        if (u > -0.9f && u <= -0.42f) alpha = Mathf.Max(alpha, Mathf.Clamp01((0.1f - v) * s * 0.5f));   // 손잡이
                        a[y * s + x] = alpha;
                    }
                return swordIcon = FromAlpha(tex, a, s);
            }
        }

        static Sprite bloodSplatter;
        /// <summary>
        /// 피 튐 (GDD-beta B12.2): 화면 가장자리에 몰린 핏자국 덩어리와 흩어진 방울, 아래로 흘러내린 줄기. 흰색 (색은 Image.color로).
        /// </summary>
        public static Sprite BloodSplatter
        {
            get
            {
                if (bloodSplatter != null) return bloodSplatter;
                const int s = 256;
                var tex = NewTex(s, "BloodSplatter");
                var a = new float[s * s];
                var rng = new System.Random(7);
                float R() => (float)rng.NextDouble();
                void Blob(Vector2 c, float r, float rough)
                {
                    int x0 = Mathf.Max(0, (int)(c.x - r * 1.6f)), x1 = Mathf.Min(s - 1, (int)(c.x + r * 1.6f));
                    int y0 = Mathf.Max(0, (int)(c.y - r * 1.6f)), y1 = Mathf.Min(s - 1, (int)(c.y + r * 1.6f));
                    for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                        {
                            var d = new Vector2(x + 0.5f, y + 0.5f) - c;
                            float ang = Mathf.Atan2(d.y, d.x);
                            float edge = r * (1f + rough * (Mathf.PerlinNoise(ang * 1.7f + c.x, c.y * 0.05f) - 0.5f));
                            float v = Mathf.Clamp01((edge - d.magnitude) / 2f);
                            int i = y * s + x;
                            a[i] = Mathf.Max(a[i], v);
                        }
                }
                // 가장자리 큰 덩어리 + 흩어진 방울 + 흘러내린 줄기
                for (int k = 0; k < 14; k++)
                {
                    float ang = R() * Mathf.PI * 2f;
                    var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                    float dist = 0.62f + 0.3f * R();
                    var c = new Vector2(s * 0.5f, s * 0.5f) + dir * (s * 0.5f * dist);
                    float r = s * (0.05f + 0.07f * R());
                    Blob(c, r, 0.9f);
                    for (int j = 0; j < 6; j++)
                        Blob(c - dir * (r * (1.2f + 2.5f * R())) + new Vector2(R() - 0.5f, R() - 0.5f) * r * 2f, r * (0.08f + 0.18f * R()), 0.4f);
                    if (dir.y > -0.2f)   // 위·옆 덩어리는 아래로 흘러내림
                        for (float t = 0; t < 1f; t += 0.02f)
                            Blob(c + new Vector2(0f, -r * (0.5f + 3f * t)), r * 0.18f * (1f - t * 0.7f), 0.2f);
                }
                return bloodSplatter = FromAlpha(tex, a, s);
            }
        }

        /// <summary>연기 덩어리 (노이즈로 가장자리를 불규칙하게).</summary>
        public static Sprite Smoke
        {
            get
            {
                if (smoke != null) return smoke;
                const int s = 128;
                var tex = NewTex(s, "Smoke");
                var a = new float[s * s];
                float r = s * 0.5f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                        float n = Mathf.PerlinNoise(x * 0.06f + 3f, y * 0.06f + 9f);
                        a[y * s + x] = Mathf.Clamp01((1f - d) * 1.6f) * Mathf.Lerp(0.4f, 1f, n);
                    }
                return smoke = FromAlpha(tex, a, s);
            }
        }

        /// <summary>금 간 원: 가운데에서 갈라지는 들쭉날쭉한 금 6줄 (FAIL).</summary>
        public static Sprite Crack
        {
            get
            {
                if (crack != null) return crack;
                const int s = 128;
                var tex = NewTex(s, "Crack");
                var a = new float[s * s];
                var rnd = new System.Random(11);
                var c = new Vector2(s * 0.5f, s * 0.5f);
                for (int b = 0; b < 6; b++)
                {
                    float ang = (b + (float)rnd.NextDouble() * 0.5f) / 6f * Mathf.PI * 2f;
                    var p = c;
                    for (int k = 0; k < 4; k++)
                    {
                        float jit = ((float)rnd.NextDouble() - 0.5f) * 1.1f;
                        var q = p + new Vector2(Mathf.Cos(ang + jit), Mathf.Sin(ang + jit)) * s * 0.11f;
                        DrawLine(a, s, p, q, 1.6f);
                        p = q;
                    }
                }
                return crack = FromAlpha(tex, a, s);
            }
        }

        /// <summary>UI 가산 머티리얼 (SceneBuilder가 에셋을 넘김, 없으면 실행 중 셰이더로 생성).</summary>
        public static Material UiAdditive(Material asset)
        {
            if (asset != null) return asset;
            if (uiAdditive != null) return uiAdditive;
            var sh = Shader.Find("RhythmActionGame/UIAdditive");
            if (sh == null) return null;
            return uiAdditive = new Material(sh) { name = "UIAdditive_Runtime" };
        }


        static Texture2D NewTex(int size, string name) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };

        static Sprite ToSprite(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);

        /// <summary>붓 한 획의 알파: 선분 a→b에서의 거리 d, 굵기 w(끝으로 갈수록 가늘어짐), 노이즈로 거친 가장자리.</summary>
        static float Stroke(Vector2 p, Vector2 a, Vector2 b, float w, float seed)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            float d = Vector2.Distance(p, a + ab * t);
            float taper = w * Mathf.Sin(Mathf.Lerp(0.15f, 1f, t) * Mathf.PI * 0.5f) * (1f - 0.6f * t);
            float rough = (Mathf.PerlinNoise(t * 12f + seed, d * 0.05f + seed) - 0.5f) * w * 0.6f;
            return Mathf.Clamp01((taper + rough - d) / 2f);
        }

        /// <summary>붓획 방사 섬광: 가운데에서 바깥으로 뻗는 획 14개 (흰색, 색은 Image.color로).</summary>
        public static Sprite BrushBurst
        {
            get
            {
                if (brushBurst != null) return brushBurst;
                const int s = 256;
                var tex = NewTex(s, "BrushBurst");
                var px = new Color32[s * s];
                var c = new Vector2(s * 0.5f, s * 0.5f);
                int n = GameConfig.Fx.BrushStrokes;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float a = 0f;
                        for (int i = 0; i < n; i++)
                        {
                            float ang = (i + 0.3f * Mathf.Sin(i * 7.1f)) / n * Mathf.PI * 2f;
                            var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                            float len = s * (0.32f + 0.14f * Mathf.Abs(Mathf.Sin(i * 3.7f)));
                            a = Mathf.Max(a, Stroke(p, c + dir * s * 0.12f, c + dir * len, 7f, i * 1.3f));
                        }
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                return brushBurst = ToSprite(tex);
            }
        }

        /// <summary>먹 획 하나 (왼쪽 아래 → 오른쪽 위).</summary>
        public static Sprite InkSlash
        {
            get
            {
                if (inkSlash != null) return inkSlash;
                const int s = 256;
                var tex = NewTex(s, "InkSlash");
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float a = Stroke(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s * 0.05f, s * 0.2f), new Vector2(s * 0.95f, s * 0.8f), 22f, 4.2f);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                return inkSlash = ToSprite(tex);
            }
        }

        /// <summary>먹 X 획 두 개.</summary>
        public static Sprite InkX
        {
            get
            {
                if (inkX != null) return inkX;
                const int s = 256;
                var tex = NewTex(s, "InkX");
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        float a = Mathf.Max(Stroke(p, new Vector2(s * 0.08f, s * 0.12f), new Vector2(s * 0.92f, s * 0.88f), 20f, 1.1f),
                                            Stroke(p, new Vector2(s * 0.9f, s * 0.1f), new Vector2(s * 0.1f, s * 0.9f), 18f, 8.7f));
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                return inkX = ToSprite(tex);
            }
        }

        /// <summary>충격파용 고리 (가장자리가 거친 흰 고리).</summary>
        public static Sprite ShockRing
        {
            get
            {
                if (ring != null) return ring;
                const int s = 256;
                var tex = NewTex(s, "ShockRing");
                var px = new Color32[s * s];
                float r = s * 0.5f;
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                        float ang = Mathf.Atan2(y - r, x - r);
                        float edge = 0.9f + (Mathf.PerlinNoise(ang * 3f + 5f, 0.5f) - 0.5f) * 0.08f;
                        float k = (d - edge) / 0.06f;
                        float a = d > 1f ? 0f : Mathf.Exp(-k * k);
                        px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                return ring = ToSprite(tex);
            }
        }

        /// <summary>
        /// 파티클용 머티리얼 (URP Particles/Unlit, 가산). 빌드에서도 셰이더가 들어가도록 SceneBuilder가 에셋으로 만들어 넘기는 것이 기본이고,
        /// 없으면 실행 중에 만든다.
        /// </summary>
        public static Material ParticleMaterial(Material asset)
        {
            Material m;
            if (asset != null) m = new Material(asset) { name = asset.name + "_Soft" };   // 에셋은 그대로 두고 복사본에 텍스처
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
                m = new Material(shader) { name = "VfxParticle_Runtime" };
                SetupAdditive(m);
            }
            // 묶음 A 제안(2026-11-17, 회색 네모): 텍스처가 없으면 파티클이 네모 판으로 그려져 지붕 위에 회색·흰 네모가 보였다 → 가장자리가 흐린 둥근 점
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") == null) m.SetTexture("_BaseMap", SoftDot());
            if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == null) m.SetTexture("_MainTex", SoftDot());
            return m;
        }

        static Texture2D softDot;

        /// <summary>가운데가 밝고 가장자리로 갈수록 투명해지는 둥근 점 (64×64, 알파).</summary>
        public static Texture2D SoftDot()
        {
            if (softDot != null) return softDot;
            const int n = 64;
            softDot = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "VfxSoftDot", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            softDot.SetPixels32(px);
            softDot.Apply(false, true);
            return softDot;
        }

        /// <summary>URP Particles/Unlit을 가산 투명으로 설정한다.</summary>
        public static void SetupAdditive(Material m)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 2f);          // Additive
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>한 번에 뿜는 파티클 시스템 하나 (재사용). 종류별 크기·속도·색.</summary>
        public static ParticleSystem Particles(Transform parent, VfxKind kind, Material mat)
        {
            var go = new GameObject("Vfx_" + kind);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;
            var em = ps.emission; em.enabled = false;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.1f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            Color c;
            switch (kind)
            {
                case VfxKind.Embers:
                    main.startLifetime = GameConfig.Fx.EmberLifetime;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(GameConfig.Fx.EmberSpeedMin, GameConfig.Fx.EmberSpeedMax);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
                    main.gravityModifier = 0.6f;
                    c = new Color(2.2f, 1.1f, 0.35f);            // HDR 주황 (Bloom으로 빛남)
                    break;
                case VfxKind.Dust:
                    main.startLifetime = 0.6f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                    main.gravityModifier = -0.05f;
                    shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.6f;
                    c = new Color(0.62f, 0.55f, 0.45f, 0.5f);
                    break;
                case VfxKind.TileDebris:
                    main.startLifetime = 0.9f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
                    main.gravityModifier = 1.5f;
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    c = new Color(0.22f, 0.23f, 0.27f);
                    break;
                default: // HanjiDebris
                    main.startLifetime = 1.2f;
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                    main.gravityModifier = 0.25f;
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    c = new Color(0.95f, 0.92f, 0.82f);
                    break;
            }
            main.startColor = c;
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }
    }
}
