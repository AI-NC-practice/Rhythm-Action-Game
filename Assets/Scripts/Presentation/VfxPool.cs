using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 이펙트 풀 (GDD-beta B5, 제안서 1.2·4장). 코드로 만든 이펙트를 미리 만들어 두고 다시 쓴다.
    /// - 화면 섬광(붓획 방사, 먹 획, 먹 X, 흰 플래시): 동시에 하나, 등급이 높은(같으면 나중) 쪽이 이김, 불투명도 ≤ 0.6.
    ///   섬광 Canvas는 판정 원 Canvas(20)보다 아래(10)라 원을 가리지 않는다.
    /// - 월드 이펙트(불씨, 흙먼지, 기왓장·한지 파편, 충격파): 종류별 풀에서 돌려 쓴다.
    /// </summary>
    public class VfxPool : MonoBehaviour
    {
        /// <summary>섬광을 올릴 Canvas (sortingOrder 10).</summary>
        public RectTransform flashLayer;
        /// <summary>월드 위치를 화면으로 바꿀 카메라.</summary>
        public Camera worldCamera;
        /// <summary>파티클 머티리얼 에셋 (SceneBuilder가 넘김, 없으면 실행 중 생성).</summary>
        public Material particleMaterial;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        class Flash
        {
            public VfxKind Kind;
            public int Grade;
            public float Age, Seconds;
            public Color Color;
        }

        Image flashImage;
        Flash flash;
        readonly Dictionary<VfxKind, List<ParticleSystem>> particles = new Dictionary<VfxKind, List<ParticleSystem>>();
        readonly Dictionary<VfxKind, int> nextIndex = new Dictionary<VfxKind, int>();
        readonly List<(Transform t, float age)> shocks = new List<(Transform, float)>();
        readonly List<Transform> shockPool = new List<Transform>();
        bool built;

        /// <summary>지금 보이는 섬광 (없으면 null).</summary>
        public VfxKind? ActiveFlash => flash != null ? flash.Kind : (VfxKind?)null;
        public int ActiveFlashGrade => flash != null ? flash.Grade : -1;
        public Image FlashImage => flashImage;
        public int PlayCount { get; private set; }
        /// <summary>종류별로 미리 만든 오브젝트 수 (풀 크기 확인용).</summary>
        public int PooledCount(VfxKind k) => k == VfxKind.Shockwave ? shockPool.Count : particles.TryGetValue(k, out var l) ? l.Count : 0;
        public ParticleSystem LastParticles { get; private set; }
        RewindOverlay rewindOverlay;
        /// <summary>테이프 되감기 겹화면 (GDD-beta B10.3).</summary>
        public RewindOverlay Rewind { get { Build(); return rewindOverlay; } }
        public Transform LastShockwave { get; private set; }

        void Awake() => Build();

        public void Build()
        {
            if (built) return;
            built = true;
            // 텍스처를 미리 만든다 (첫 재생 때 만들면 수십 ms 멈칫해 Hit Stop보다 길어진다)
            _ = VfxFactory.BrushBurst; _ = VfxFactory.InkSlash; _ = VfxFactory.InkX; _ = VfxFactory.ShockRing; _ = VfxFactory.BloodSplatter;
            _ = VfxFactory.Lightning; _ = VfxFactory.Flare; _ = VfxFactory.Dot; _ = VfxFactory.Smoke; _ = VfxFactory.Crack; _ = VfxFactory.Scanlines;
            if (flashLayer == null)
            {
                var c = UiFactory.CreateCanvas("ScreenFlashCanvas", GameConfig.JudgeFxCanvasOrder);
                c.transform.SetParent(transform, false);
                flashLayer = (RectTransform)c.transform;
            }
            rewindOverlay = gameObject.AddComponent<RewindOverlay>();
            rewindOverlay.Build(flashLayer);
            var go = new GameObject("ScreenFlash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(flashLayer, false);
            flashImage = go.GetComponent<Image>();
            flashImage.raycastTarget = false;
            flashImage.enabled = false;

            var mat = VfxFactory.ParticleMaterial(particleMaterial);
            foreach (var k in new[] { VfxKind.Embers, VfxKind.Dust, VfxKind.TileDebris, VfxKind.HanjiDebris })
            {
                var list = new List<ParticleSystem>();
                for (int i = 0; i < GameConfig.Fx.PoolSize; i++) list.Add(VfxFactory.Particles(transform, k, mat));
                particles[k] = list;
                nextIndex[k] = 0;
            }
            for (int i = 0; i < GameConfig.Fx.PoolSize; i++)
            {
                var ring = new GameObject("Vfx_Shockwave", typeof(SpriteRenderer)).transform;
                ring.SetParent(transform, false);
                var sr = ring.GetComponent<SpriteRenderer>();
                sr.sprite = VfxFactory.ShockRing;
                sr.color = new Color(1f, 0.95f, 0.85f, 0.8f);
                ring.gameObject.SetActive(false);
                shockPool.Add(ring);
            }
        }

        static bool IsFlash(VfxKind k) => k == VfxKind.BrushBurst || k == VfxKind.InkSlash || k == VfxKind.InkX || k == VfxKind.WhiteFlash || k == VfxKind.RewindStreak || k == VfxKind.BloodSplatter;

        /// <summary>
        /// 이펙트 하나를 재생한다. 섬광은 등급이 낮으면 거절될 수 있다(false).
        /// world: 월드 위치 (섬광은 화면 위치로 바꿔 쓴다, 없으면 화면 가운데). count: 파티클 개수 (−1이면 기본).
        /// </summary>
        public bool Play(VfxKind kind, Vector3? world = null, int grade = 3, int count = -1, Color? tint = null)
        {
            Build();
            PlayCount++;
            if (IsFlash(kind)) return PlayFlash(kind, world, grade, tint);
            if (kind == VfxKind.Shockwave) { PlayShock(world ?? transform.position); return true; }
            var list = particles[kind];
            int i = nextIndex[kind];
            nextIndex[kind] = (i + 1) % list.Count;
            var ps = list[i];
            ps.transform.position = world ?? transform.position;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (tint.HasValue) { var m = ps.main; m.startColor = tint.Value; }
            int n = count >= 0 ? count : kind == VfxKind.Embers ? GameConfig.Fx.EmbersPerfect
                : kind == VfxKind.Dust ? GameConfig.Fx.DustCount : GameConfig.Fx.DebrisCount;
            ps.Emit(n);
            LastParticles = ps;
            return true;
        }

        bool PlayFlash(VfxKind kind, Vector3? world, int grade, Color? tint)
        {
            if (flash != null && grade < flash.Grade) return false;      // 동시에 하나, 등급 높은 쪽
            float secs = kind == VfxKind.BrushBurst ? GameConfig.Fx.BrushBurstSeconds
                       : kind == VfxKind.WhiteFlash ? GameConfig.Fx.WhiteFlashSeconds
                       : kind == VfxKind.RewindStreak ? GameConfig.RewindSeconds
                       : kind == VfxKind.BloodSplatter ? GameConfig.FailHitSeconds : GameConfig.Fx.InkSlashSeconds;
            Color col = tint ?? (kind == VfxKind.BrushBurst ? VfxFactory.DancheongTeal : kind == VfxKind.WhiteFlash ? Color.white
                : kind == VfxKind.BloodSplatter ? GameConfig.BloodColor : VfxFactory.Ink);
            flash = new Flash { Kind = kind, Grade = grade, Seconds = secs, Color = col };
            var rt = flashImage.rectTransform;
            float h = GameConfig.UiReferenceHeight, w = h * 16f / 9f;
            switch (kind)
            {
                case VfxKind.BrushBurst:
                    flashImage.sprite = VfxFactory.BrushBurst;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(520, 520);
                    rt.anchoredPosition = ScreenPos(world);
                    rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    break;
                case VfxKind.InkSlash:
                    flashImage.sprite = VfxFactory.InkSlash;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(w * GameConfig.Fx.InkSlashWidth, w * GameConfig.Fx.InkSlashWidth * 0.6f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    break;
                case VfxKind.RewindStreak:
                    // 화면을 가로지르는 먹 줄기 (오른쪽 → 왼쪽으로 거꾸로 흐름)
                    flashImage.sprite = VfxFactory.InkSlash;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(w * 1.2f, h * 0.5f);
                    rt.anchoredPosition = new Vector2(w * 0.5f, 0f);
                    rt.localRotation = Quaternion.Euler(0, 180f, 0);
                    break;
                case VfxKind.BloodSplatter:
                    // 화면 전체에 핏자국 (가장자리 위주)
                    flashImage.sprite = VfxFactory.BloodSplatter;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(w, h);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.Euler(0, 0, Random.value < 0.5f ? 0f : 180f);
                    break;
                case VfxKind.InkX:
                    flashImage.sprite = VfxFactory.InkX;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(h * 1.1f, h * 1.1f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localRotation = Quaternion.identity;
                    break;
                default:
                    flashImage.sprite = UiWidgets.White;
                    UiWidgets.Stretch(rt);
                    rt.localRotation = Quaternion.identity;
                    break;
            }
            flashImage.enabled = true;
            Pose(0f);
            return true;
        }

        Vector2 ScreenPos(Vector3? world)
        {
            if (!world.HasValue || worldCamera == null) return Vector2.zero;
            var sp = worldCamera.WorldToScreenPoint(world.Value);
            if (sp.z <= 0 || worldCamera.pixelWidth <= 0) return Vector2.zero;
            float h = GameConfig.UiReferenceHeight;
            float s = h / worldCamera.pixelHeight;
            var local = new Vector2((sp.x - worldCamera.pixelWidth * 0.5f) * s, (sp.y - worldCamera.pixelHeight * 0.5f) * s);
            return RingLayout.Clamp(local, RingLayout.SafeRadii(new Vector2(h * worldCamera.aspect, h)), out _);
        }

        void PlayShock(Vector3 pos)
        {
            Transform t = null;
            foreach (var s in shockPool) if (!s.gameObject.activeSelf) { t = s; break; }
            if (t == null) { t = shocks[0].t; shocks.RemoveAt(0); }
            t.position = pos + Vector3.up * 0.05f;
            t.rotation = Quaternion.Euler(90f, 0, 0);       // 바닥에 눕힘
            t.localScale = Vector3.zero;
            t.gameObject.SetActive(true);
            shocks.Add((t, 0f));
            LastShockwave = t;
        }

        /// <summary>섬광 모양: 처음 커졌다가 알파가 0까지 (불투명도 상한 0.6).</summary>
        void Pose(float age)
        {
            if (flash == null) return;
            float u = Mathf.Clamp01(age / flash.Seconds);
            float alpha = GameConfig.Fx.FlashMaxAlpha * (flash.Kind == VfxKind.BloodSplatter ? 1f - u * u : 1f - u);   // 핏자국은 오래 남았다가 사라짐
            var c = flash.Color; c.a = Mathf.Min(alpha, GameConfig.Fx.FlashMaxAlpha);
            flashImage.color = c;
            if (flash.Kind == VfxKind.RewindStreak)
            {
                float w = GameConfig.UiReferenceHeight * 16f / 9f;
                flashImage.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(w * 0.5f, -w * 0.5f, u), 0f);
                flashImage.rectTransform.localScale = Vector3.one;
            }
            else if (flash.Kind == VfxKind.BrushBurst) flashImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, Mathf.Sqrt(u));
            else flashImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.05f, 1f, u);
        }

        void LateUpdate() => Step(DeltaTime());

        public void Step(float dt)
        {
            if (flash != null)
            {
                flash.Age += dt;
                if (flash.Age >= flash.Seconds) { flash = null; flashImage.enabled = false; }
                else Pose(flash.Age);
            }
            for (int i = shocks.Count - 1; i >= 0; i--)
            {
                var (t, age) = shocks[i];
                age += dt;
                if (age >= GameConfig.Fx.ShockwaveSeconds) { t.gameObject.SetActive(false); shocks.RemoveAt(i); continue; }
                float u = age / GameConfig.Fx.ShockwaveSeconds;
                float d = GameConfig.Fx.ShockwaveRadius * 2f * u / 2.56f;    // 스프라이트 지름 2.56 m(256 px / 100) 기준
                t.localScale = new Vector3(d, d, 1f);
                var sr = t.GetComponent<SpriteRenderer>();
                var c = sr.color; c.a = 0.8f * (1f - u); sr.color = c;
                shocks[i] = (t, age);
            }
        }

        /// <summary>충격파 링의 지금 반지름 (m).</summary>
        public static float ShockRadius(Transform t) => t.localScale.x * 2.56f * 0.5f;
    }
}
