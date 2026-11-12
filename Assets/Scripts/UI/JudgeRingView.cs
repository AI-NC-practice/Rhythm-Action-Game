using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 판정 원 UI (GDD 4.2 + GDD-beta B1).
    /// - 원은 목표 2박자 전에 뜨고, 바깥 원이 360→120 px로 2박자 동안 줄어든다 (GDD 4.2).
    /// - 원은 안전 타원 안에만 뜨고, 등장 0.1초 뒤 위치를 고정한다. 타원 밖이었으면 방향 꺾쇠를 붙인다 (B1.3).
    /// - 1박 미만 간격으로 이어지는 원은 가로 160 px 간격으로 나란히 둔다 (B1.3).
    /// - 판정 문구는 원 아래 110 px, 다른 Canvas(sortingOrder 10)에 0.45초 동안 표시한다 (B1.1, B1.2).
    /// </summary>
    public class JudgeRingView : MonoBehaviour
    {
        public JudgeSystem judge;
        public BeatClock clock;
        public Camera worldCamera;
        /// <summary>판정 문구를 올릴 층 (sortingOrder 10 Canvas). 없으면 Awake에서 만든다.</summary>
        public RectTransform labelLayer;
        /// <summary>화면 표시 지연 보정 (ms, GDD-beta B8.1): 원을 이만큼 앞당겨 그려 눈에 보이는 겹침 순간이 판정 목표와 맞게 한다.</summary>
        public float displayLatencyMs = GameConfig.DisplayLatencyMs;

        /// <summary>원 하나의 화면 요소.</summary>
        public class Ring
        {
            public JudgeCue Cue;
            public RectTransform Root;
            public Image Outer, Target, Button, Glow, Chevron;
            public Text Arrow;
            public float AppearedAt;      // 실제 시간
            public float PulseAt = -999f; // 키를 눌러 반응한 시각 (B13)
            public bool Locked;
            public bool Clamped;          // 투영점이 안전 타원 밖이었음
            public Chain Chain;
            public int ChainIndex;
        }

        /// <summary>1박 미만 간격으로 이어지는 원 묶음.</summary>
        public class Chain
        {
            public List<JudgeCue> Members = new List<JudgeCue>();
            public Vector2 Anchor;
        }

        /// <summary>판정 문구 하나.</summary>
        public class Label
        {
            public Text Text;
            public float ShownAt;   // 실제 시간
            public Vector2 BasePos;
            public bool Dimmed;
        }

        readonly List<Ring> rings = new List<Ring>();
        readonly List<Label> labels = new List<Label>();
        readonly List<Chain> chains = new List<Chain>();

        /// <summary>잠깐 떠 있다 사라지는 글자 ("빠름", 누른 키 표시 — GDD-beta B8.2).</summary>
        public class Floater
        {
            public Text Text;
            public float ShownAt, Seconds;
            public Vector2 BasePos;
        }
        readonly List<Floater> earlyLabels = new List<Floater>();
        readonly List<Floater> keyEchoes = new List<Floater>();
        public IReadOnlyList<Floater> EarlyLabels => earlyLabels;
        public IReadOnlyList<Floater> KeyEchoes => keyEchoes;
        static readonly Color EarlyColor = new Color(1f, 0.6f, 0.2f);
        static readonly Color EchoColor = new Color(1f, 1f, 1f, 0.8f);
        RectTransform canvasRect;

        public IReadOnlyList<Ring> Rings => rings;
        public IReadOnlyList<Label> Labels => labels;

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public System.Func<float> RealTime = () => Time.unscaledTime;

        /// <summary>테스트에서 바꿔 끼우는 캔버스 크기 (1080p 기준 px). 기본은 캔버스 RectTransform 크기.</summary>
        public System.Func<Vector2> CanvasSize;

        static readonly Color OuterColor = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color TargetColor = new Color(1f, 0.85f, 0.3f, 1f);
        static readonly Color ButtonColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
        /// <summary>단청 청록 빛 (GDD-beta B1.3).</summary>
        public static readonly Color GlowColor = new Color(0.15f, 0.85f, 0.75f, GameConfig.RingGlowAlpha);
        /// <summary>마무리 원 금색 빛.</summary>
        public static readonly Color FinisherGlowColor = new Color(1f, 0.78f, 0.2f, GameConfig.RingGlowAlpha);

        public void Bind(JudgeSystem j, BeatClock c, Camera cam)
        {
            Unbind();
            judge = j; clock = c; worldCamera = cam;
            if (judge != null)
            {
                judge.CueAppeared += OnAppeared;
                judge.CueJudged += OnJudged;
                judge.CueCancelled += OnCancelled;
                judge.InputReceived += OnInput;
            }
        }

        void Unbind()
        {
            if (judge == null) return;
            judge.CueAppeared -= OnAppeared;
            judge.CueJudged -= OnJudged;
            judge.CueCancelled -= OnCancelled;
            judge.InputReceived -= OnInput;
        }

        void OnEnable() { if (judge != null) Bind(judge, clock, worldCamera); }
        void OnDisable() => Unbind();

        void Awake()
        {
            canvasRect = transform as RectTransform;
            EnsureLabelLayer();
        }

        void EnsureLabelLayer()
        {
            if (labelLayer != null) return;
            var c = UiFactory.CreateCanvas("JudgeLabelCanvas", GameConfig.JudgeFxCanvasOrder);
            c.transform.SetParent(transform.parent, false);
            labelLayer = (RectTransform)c.transform;
        }

        Vector2 Size()
        {
            if (CanvasSize != null) return CanvasSize();
            var s = canvasRect != null ? canvasRect.rect.size : Vector2.zero;
            if (s.x < 1f || s.y < 1f) s = new Vector2(GameConfig.UiReferenceHeight * 16f / 9f, GameConfig.UiReferenceHeight);
            return s;
        }

        /// <summary>마무리(처형·윈드밀) 원인지.</summary>
        public static bool IsFinisher(JudgeCue cue) =>
            cue.Tag is CombatStep s && (s.Kind == CombatStepKind.Finisher || s.Attack == "windmill");

        void OnAppeared(JudgeCue cue)
        {
            var root = new GameObject("Ring", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            var ring = new Ring { Cue = cue, Root = root, AppearedAt = RealTime() };
            ring.Glow = UiFactory.CreateImage(root, "Glow", UiFactory.Glow, IsFinisher(cue) ? FinisherGlowColor : GlowColor, GameConfig.RingGlowDiameterPx);
            ring.Button = UiFactory.CreateImage(root, "Button", UiFactory.Disc, ButtonColor, GameConfig.RingTargetDiameterPx);
            ring.Target = UiFactory.CreateImage(root, "TargetRing", UiFactory.Ring, TargetColor, GameConfig.RingTargetDiameterPx);
            ring.Outer = UiFactory.CreateImage(root, "OuterRing", UiFactory.Ring, OuterColor, GameConfig.RingStartDiameterPx);
            ring.Arrow = UiFactory.CreateText(root, "Arrow", JudgeRules.Arrow(cue.Key), 64, Color.white, new Vector2(120, 120));
            ring.Chevron = UiFactory.CreateImage(root, "Chevron", UiFactory.Chevron, GlowColor, GameConfig.RingChevronSizePx);
            ring.Chevron.enabled = false;
            AssignChain(ring);
            rings.Add(ring);
            UpdateRing(ring);
        }

        /// <summary>앞 원과 1박 미만 간격이면 같은 묶음에 넣는다. 새 묶음이면 예약된 판정에서 묶음 길이를 센다.</summary>
        void AssignChain(Ring ring)
        {
            chains.RemoveAll(c => c.Members.TrueForAll(m => m.Judged || m.Cancelled));
            foreach (var c in chains)
            {
                int i = c.Members.IndexOf(ring.Cue);
                if (i < 0) continue;
                ring.Chain = c; ring.ChainIndex = i;
                return;
            }
            var chain = new Chain();
            chain.Members.Add(ring.Cue);
            if (judge != null)
            {
                var pending = new List<JudgeCue>();
                foreach (var q in judge.Cues)
                    if (q != ring.Cue && !q.Judged && !q.Cancelled && !q.Appeared && q.TargetBeat > ring.Cue.TargetBeat) pending.Add(q);
                pending.Sort((a, b) => a.TargetBeat.CompareTo(b.TargetBeat));
                double last = ring.Cue.TargetBeat;
                foreach (var q in pending)
                {
                    if (q.TargetBeat - last >= GameConfig.RingChainMaxGapBeats) break;
                    chain.Members.Add(q);
                    last = q.TargetBeat;
                }
            }
            if (chain.Members.Count > 1)
            {
                chains.Add(chain);
                ring.Chain = chain; ring.ChainIndex = 0;
            }
        }

        /// <summary>판정 없이 취소된 원은 문구 없이 지운다 (B11: 전투·윈드밀 재도전, 게임 오버, 다시 시작).</summary>
        void OnCancelled(JudgeCue cue) => RemoveRing(cue);

        void RemoveRing(JudgeCue cue)
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                if (rings[i].Cue != cue) continue;
                if (rings[i].Root != null) Destroy(rings[i].Root.gameObject);
                rings.RemoveAt(i);
            }
        }

        void OnJudged(JudgeCue cue, Judgement r)
        {
            Vector2 pos = Vector2.zero;
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                if (rings[i].Cue != cue) continue;
                pos = rings[i].Root.anchoredPosition;
                Destroy(rings[i].Root.gameObject);
                rings.RemoveAt(i);
            }
            var color = r == Judgement.Perfect ? new Color(1f, 0.9f, 0.2f) : r == Judgement.Normal ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 0.3f, 0.3f);
            EnsureLabelLayer();
            var t = UiFactory.CreateText(labelLayer, "JudgeLabel", JudgeRules.Label(r), GameConfig.JudgeLabelFontSize, color, new Vector2(400, 70));
            var label = new Label { Text = t, ShownAt = RealTime(), BasePos = RingLayout.LabelPosition(pos) };
            labels.Add(label);
            SpawnRingFx(pos, r);
            PoseLabel(label, RealTime());
        }

        void Update() => Refresh();

        /// <summary>원 크기·위치와 문구 모양·수명을 갱신한다.</summary>
        public void Refresh()
        {
            // 안전망: 어떤 경로로든 끝난 판정의 원은 남기지 않는다 (B11)
            for (int i = rings.Count - 1; i >= 0; i--)
                if (rings[i].Cue.Judged) { if (rings[i].Root != null) Destroy(rings[i].Root.gameObject); rings.RemoveAt(i); }
            foreach (var r in rings) UpdateRing(r);
            float now = RealTime();
            FadeFloaters(earlyLabels, now);
            for (int i = ringFx.Count - 1; i >= 0; i--)
                if (!PoseRingFx(ringFx[i], now)) { if (ringFx[i].Root != null) Destroy(ringFx[i].Root.gameObject); ringFx.RemoveAt(i); }
            FadeFloaters(keyEchoes, now);
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                var l = labels[i];
                if (!PoseLabel(l, now))
                {
                    if (l.Text != null) Destroy(l.Text.gameObject);
                    labels.RemoveAt(i);
                }
            }
        }

        /// <summary>문구 배율·위치·알파를 정한다. 수명이 끝났으면 false.</summary>
        bool PoseLabel(Label l, float now)
        {
            bool alive = RingLayout.LabelPose(now - l.ShownAt, out float scale, out float rise, out float alpha);
            if (l.Text == null) return false;
            if (!l.Dimmed)
            {
                // 문구 근처(200 px 안)에 판정 원이 떠 있으면 흐리게 (B1.2)
                foreach (var r in rings)
                    if (Vector2.Distance(r.Root.anchoredPosition, l.BasePos) <= GameConfig.JudgeLabelDimRadiusPx) { l.Dimmed = true; break; }
            }
            if (l.Dimmed) alpha = Mathf.Min(alpha, GameConfig.JudgeLabelDimAlpha);
            var rt = l.Text.rectTransform;
            rt.anchoredPosition = l.BasePos + new Vector2(0f, rise);
            rt.localScale = Vector3.one * scale;
            var c = l.Text.color; c.a = alpha; l.Text.color = c;
            return alive;
        }

        /// <summary>방향키 입력 반응 (GDD-beta B8.2): 원 펄스, 너무 이르면 "빠름", 원이 없으면 누른 키를 아래에 작게.</summary>
        void OnInput(ArrowKey key, JudgeCue cue, InputOutcome outcome)
        {
            EnsureLabelLayer();
            float now = RealTime();
            Ring ring = null;
            if (cue != null) foreach (var r in rings) if (r.Cue == cue) ring = r;
            if (ring != null) ring.PulseAt = now;
            if (outcome == InputOutcome.TooEarly && ring != null)
            {
                var t = UiFactory.CreateText(labelLayer, "EarlyLabel", UiText.TooEarly, GameConfig.EarlyLabelFontSize, EarlyColor, new Vector2(300, 60));
                var pos = RingLayout.LabelPosition(ring.Root.anchoredPosition);
                t.rectTransform.anchoredPosition = pos;
                earlyLabels.Add(new Floater { Text = t, ShownAt = now, Seconds = GameConfig.EarlyLabelSeconds, BasePos = pos });
            }
            else if (outcome == InputOutcome.NoCue)
            {
                var t = UiFactory.CreateText(labelLayer, "KeyEcho", JudgeRules.Arrow(key), GameConfig.KeyEchoFontSize, EchoColor, new Vector2(120, 80));
                var pos = new Vector2(0f, -Size().y * 0.5f + GameConfig.KeyEchoBottomPx);
                t.rectTransform.anchoredPosition = pos;
                keyEchoes.Add(new Floater { Text = t, ShownAt = now, Seconds = GameConfig.KeyEchoSeconds, BasePos = pos });
            }
        }

        void FadeFloaters(List<Floater> list, float now)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var f = list[i];
                float u = (now - f.ShownAt) / f.Seconds;
                if (u >= 1f || f.Text == null) { if (f.Text != null) Destroy(f.Text.gameObject); list.RemoveAt(i); continue; }
                var c = f.Text.color; c.a = 1f - u * u; f.Text.color = c;
                f.Text.rectTransform.anchoredPosition = f.BasePos + new Vector2(0f, 12f * u);
            }
        }

        /// <summary>판정 원 이펙트 하나 (GDD-beta B8.3).</summary>
        public class RingFx
        {
            public Judgement Result;
            public RectTransform Root;
            public Image Main;
            public Image[] Parts;      // PERFECT: 붓획 조각, FAIL: 갈라진 두 쪽
            public float ShownAt, Seconds;
            public Vector2 BasePos;
        }
        readonly List<RingFx> ringFx = new List<RingFx>();
        public IReadOnlyList<RingFx> RingEffects => ringFx;
        public static readonly Color PerfectFxColor = new Color(1f, 0.82f, 0.25f);
        public static readonly Color NormalFxColor = new Color(0.15f, 0.85f, 0.75f);
        public static readonly Color FailFxColor = new Color(0.9f, 0.2f, 0.12f);

        void SpawnRingFx(Vector2 pos, Judgement r)
        {
            EnsureLabelLayer();
            var root = new GameObject("RingFx_" + r, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(labelLayer, false);
            root.anchoredPosition = pos;
            var fx = new RingFx { Result = r, Root = root, ShownAt = RealTime(), BasePos = pos };
            float d = GameConfig.RingTargetDiameterPx;
            switch (r)
            {
                case Judgement.Perfect:
                    fx.Seconds = GameConfig.RingFxPerfectSeconds;
                    fx.Main = UiFactory.CreateImage(root, "Burst", UiFactory.Ring, PerfectFxColor, d);
                    var glow = UiFactory.CreateImage(root, "Glow", UiFactory.Glow, PerfectFxColor, d * 1.6f);
                    var brush = UiFactory.CreateImage(root, "Brush", VfxFactory.BrushBurst, NormalFxColor, d * 2.2f);
                    fx.Parts = new[] { glow, brush };
                    break;
                case Judgement.Normal:
                    fx.Seconds = GameConfig.RingFxNormalSeconds;
                    fx.Main = UiFactory.CreateImage(root, "Ring", UiFactory.Ring, NormalFxColor, d);
                    fx.Parts = new Image[0];
                    break;
                default:
                    fx.Seconds = GameConfig.RingFxFailSeconds;
                    fx.Main = UiFactory.CreateImage(root, "Crack", UiFactory.Disc, new Color(FailFxColor.r, FailFxColor.g, FailFxColor.b, 0.35f), d);
                    var left = UiFactory.CreateImage(root, "HalfL", UiFactory.Ring, FailFxColor, d);
                    var right = UiFactory.CreateImage(root, "HalfR", UiFactory.Ring, FailFxColor, d);
                    left.type = right.type = Image.Type.Filled;
                    left.fillMethod = right.fillMethod = Image.FillMethod.Horizontal;
                    left.fillOrigin = 0; right.fillOrigin = 1;
                    left.fillAmount = right.fillAmount = 0.5f;
                    fx.Parts = new[] { left, right };
                    break;
            }
            ringFx.Add(fx);
            PoseRingFx(fx, fx.ShownAt);
        }

        /// <summary>판정 원 이펙트 모양. 끝나면 false.</summary>
        bool PoseRingFx(RingFx fx, float now)
        {
            float u = (now - fx.ShownAt) / fx.Seconds;
            if (u >= 1f || fx.Root == null) return false;
            float fade = 1f - u;
            switch (fx.Result)
            {
                case Judgement.Perfect:
                {
                    float s = Mathf.Lerp(1f, GameConfig.RingFxPerfectScale, 1f - (1f - u) * (1f - u));
                    fx.Main.rectTransform.localScale = Vector3.one * s;
                    SetA(fx.Main, fade);
                    SetA(fx.Parts[0], 0.6f * fade);
                    fx.Parts[1].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, Mathf.Sqrt(u));
                    SetA(fx.Parts[1], 0.8f * fade);
                    break;
                }
                case Judgement.Normal:
                    fx.Main.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, GameConfig.RingFxNormalScale, u);
                    SetA(fx.Main, 0.8f * fade);
                    break;
                default:
                {
                    float shake = Mathf.Sin(u * Mathf.PI * 10f) * GameConfig.RingFxFailShakePx * (1f - u);
                    fx.Root.anchoredPosition = fx.BasePos + new Vector2(shake, 0f);
                    float split = GameConfig.RingFxFailSplitPx * Mathf.Sqrt(u);
                    fx.Parts[0].rectTransform.anchoredPosition = new Vector2(-split, -split * 0.5f);
                    fx.Parts[1].rectTransform.anchoredPosition = new Vector2(split, split * 0.5f);
                    SetA(fx.Main, 0.35f * fade);
                    SetA(fx.Parts[0], fade);
                    SetA(fx.Parts[1], fade);
                    break;
                }
            }
            return true;
        }

        static void SetA(Image i, float a) { var c = i.color; c.a = a; i.color = c; }

        /// <summary>키 반응 펄스 배율 (1 → 1.15 → 1, 0.12초).</summary>
        public static float PulseScale(float age)
        {
            if (age < 0f || age >= GameConfig.InputPulseSeconds) return 1f;
            return 1f + (GameConfig.InputPulseScale - 1f) * Mathf.Sin(age / GameConfig.InputPulseSeconds * Mathf.PI);
        }

        void UpdateRing(Ring r)
        {
            r.Root.localScale = Vector3.one * PulseScale(RealTime() - r.PulseAt);
            if (clock != null)
            {
                double lead = displayLatencyMs / 1000.0 * clock.SpeedMultiplier / GameConfig.SecondsPerBeat;   // 지연 보정 (박)
                double until = r.Cue.TargetBeat - (clock.CurrentBeat + lead);
                float d = JudgeRules.OuterRingDiameter(until);
                r.Outer.rectTransform.sizeDelta = new Vector2(d, d);
            }
            if (r.Locked) return;
            var radii = RingLayout.SafeRadii(Size());
            Vector2 pos;
            bool clamped;
            if (r.Chain != null && r.ChainIndex > 0)
            {
                pos = RingLayout.ChainSlot(r.Chain.Anchor, r.ChainIndex, r.Chain.Members.Count, radii);
                clamped = false;
                r.Locked = true;                                  // 묶음의 자리는 첫 원이 정한다
            }
            else
            {
                pos = SafePosition(r.Cue, radii, out clamped, out var outward);
                r.Clamped = clamped;
                if (clamped)
                {
                    r.Chevron.enabled = true;
                    var dir = outward.normalized;
                    r.Chevron.rectTransform.anchoredPosition = dir * (GameConfig.RingGlowDiameterPx * 0.5f + GameConfig.RingChevronSizePx * 0.5f);
                    r.Chevron.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                }
                else r.Chevron.enabled = false;
                if (r.Chain != null)
                {
                    r.Chain.Anchor = pos;
                    pos = RingLayout.ChainSlot(pos, 0, r.Chain.Members.Count, radii);
                }
                if (RealTime() - r.AppearedAt >= GameConfig.RingLockSeconds) r.Locked = true;
            }
            r.Root.anchoredPosition = pos;
        }

        /// <summary>판정 지점을 화면에 투영해 안전 타원 안으로 끌어온다. 없으면 화면 가운데.</summary>
        Vector2 SafePosition(JudgeCue cue, Vector2 radii, out bool clamped, out Vector2 outward)
        {
            clamped = false; outward = Vector2.zero;
            if (!cue.WorldPoint.HasValue || worldCamera == null || canvasRect == null) return Vector2.zero;
            var sp = worldCamera.WorldToScreenPoint(cue.WorldPoint.Value);
            var size = Size();
            float sx = worldCamera.pixelWidth > 0 ? size.x / worldCamera.pixelWidth : 1f;
            float sy = worldCamera.pixelHeight > 0 ? size.y / worldCamera.pixelHeight : 1f;
            var center = new Vector2(worldCamera.pixelWidth * 0.5f, worldCamera.pixelHeight * 0.5f);
            if (sp.z <= 0f)
            {
                // 카메라 뒤: 투영이 뒤집히므로 방향을 반대로 쓴다
                var d = -(new Vector2(sp.x, sp.y) - center);
                outward = new Vector2(d.x * sx, d.y * sy);
                clamped = true;
                return RingLayout.OnBoundary(outward, radii);
            }
            var local = new Vector2((sp.x - center.x) * sx, (sp.y - center.y) * sy);
            outward = local;
            return RingLayout.Clamp(local, radii, out clamped);
        }
    }
}
