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
            if (asset != null) return asset;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            var m = new Material(shader) { name = "VfxParticle_Runtime" };
            SetupAdditive(m);
            return m;
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
