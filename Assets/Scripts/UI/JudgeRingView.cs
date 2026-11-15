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
        /// <summary>UI 가산 머티리얼 (GDD-beta B8.5). SceneBuilder가 에셋을 넘기고, 없으면 실행 중 셰이더로 만든다.</summary>
        public Material additiveMaterial;
        Material Additive => VfxFactory.UiAdditive(additiveMaterial);

        Image AddImage(Transform parent, string name, Sprite s, Color c, float d, bool additive)
        {
            var img = UiFactory.CreateImage(parent, name, s, c, d);
            if (additive && Additive != null) img.material = Additive;
            return img;
        }

        /// <summary>원 하나의 화면 요소.</summary>
        public class Ring
        {
            public JudgeCue Cue;
            public RectTransform Root;
            public Image Outer, Target, Button, Glow, Chevron;
            /// <summary>가산 빛 (B17): 목표에 가까울수록 밝아짐, 바깥 원 빛, 둘레를 도는 빛 점.</summary>
            public Image ChargeGlow, OuterGlow;
            public Image[] Orbit;
            /// <summary>내 공격 원의 칼 아이콘 (GDD-beta B11.2), 그 밖에는 null.</summary>
            public Image StrikeIcon;
            public float Charge;
            public Text Arrow;
            public float AppearedAt;      // 실제 시간
            public float PulseAt = -999f; // 키를 눌러 반응한 시각 (B13)
            public bool Locked;
            /// <summary>다음에 누를 원은 선명, 기다리는 원은 흐리게 (GDD-beta B10.1).</summary>
            public CanvasGroup Group;
            public bool Clamped;          // 투영점이 안전 타원 밖이었음
            public Chain Chain;
            public int ChainIndex;
        }

        /// <summary>1박 미만 간격으로 이어지는 원 묶음.</summary>
        public class Chain
        {
            public List<JudgeCue> Members = new List<JudgeCue>();
            public Vector2 Anchor;
            /// <summary>돌려 쓰는 자리 수 (GDD-beta B11.6). index번째 원은 index % Slots 자리에 선다.</summary>
            public int Slots = 1;
            public int SlotOf(int index) => Slots > 0 ? index % Slots : index;
            /// <summary>뒤 원이 자리를 잡으면 묶음 기준점을 고정한다 (첫 원이 움직여 자리가 어긋나지 않게).</summary>
            public bool Fixed;
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
        /// <summary>내 공격 원 빛 (주홍, GDD-beta B11.2).</summary>
        public static readonly Color StrikeColor = new Color(GameConfig.StrikeRingColor.r, GameConfig.StrikeRingColor.g, GameConfig.StrikeRingColor.b, GameConfig.RingGlowAlpha);

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

        /// <summary>내 공격(칼 겨루기 포함) 원인지 (GDD-beta B11.2).</summary>
        public static bool IsStrike(JudgeCue cue) =>
            cue.Tag is CombatStep s && (s.Kind == CombatStepKind.Strike || s.Kind == CombatStepKind.Lock);

        /// <summary>원 빛 색: 마무리 금색, 내 공격 주홍, 그 밖(막기·피하기·사건) 청록.</summary>
        public static Color RingGlowColor(JudgeCue cue)
        {
            if (cue.Tag is RhythmNote n) return n.Key == ArrowKey.Left ? GameConfig.Jangdan.KungColor : GameConfig.Jangdan.DeokColor;   // B18.2 손 색
            if (IsFinisher(cue)) return FinisherGlowColor;
            if (IsStrike(cue)) return StrikeColor;
            return GlowColor;
        }

        /// <summary>마무리(처형·윈드밀) 원인지.</summary>
        public static bool IsFinisher(JudgeCue cue) =>
            cue.Tag is CombatStep s && (s.Kind == CombatStepKind.Finisher || s.Attack == "windmill");

        void OnAppeared(JudgeCue cue)
        {
            var root = new GameObject("Ring", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            var ring = new Ring { Cue = cue, Root = root, AppearedAt = RealTime() };
            ring.Group = root.gameObject.AddComponent<CanvasGroup>();
            ring.Group.blocksRaycasts = false;
            var glowColor = RingGlowColor(cue);
            ring.Glow = UiFactory.CreateImage(root, "Glow", UiFactory.Glow, glowColor, GameConfig.RingGlowDiameterPx);
            ring.ChargeGlow = AddImage(root, "ChargeGlow", UiFactory.Glow, glowColor, GameConfig.RingGlowDiameterPx * 1.15f, true);
            ring.OuterGlow = AddImage(root, "OuterGlow", UiFactory.Glow, glowColor, GameConfig.RingStartDiameterPx * 1.15f, true);
            ring.Button = UiFactory.CreateImage(root, "Button", UiFactory.Disc, ButtonColor, GameConfig.RingTargetDiameterPx);
            ring.Target = UiFactory.CreateImage(root, "TargetRing", UiFactory.Ring, TargetColor, GameConfig.RingTargetDiameterPx);
            ring.Outer = UiFactory.CreateImage(root, "OuterRing", UiFactory.Ring, OuterColor, GameConfig.RingStartDiameterPx);
            ring.Arrow = UiFactory.CreateText(root, "Arrow", JudgeRules.Arrow(cue.Key), 64, Color.white, new Vector2(120, 120));
            ring.Chevron = UiFactory.CreateImage(root, "Chevron", UiFactory.Chevron, GlowColor, GameConfig.RingChevronSizePx);
            ring.Chevron.enabled = false;
            ring.Orbit = new Image[GameConfig.RingOrbitDots];
            for (int i = 0; i < ring.Orbit.Length; i++) ring.Orbit[i] = AddImage(root, "Orbit" + i, VfxFactory.Dot, Color.white, 18f, true);
            if (IsStrike(cue))
            {
                // 어두운 원 위에 칼 아이콘 (주홍 빛 위에서도 보이게)
                var back = UiFactory.CreateImage(root, "StrikeIconBack", UiFactory.Disc, new Color(0.08f, 0.05f, 0.05f, 0.85f), GameConfig.StrikeIconPx * 1.15f);
                back.rectTransform.anchoredPosition = GameConfig.StrikeIconOffsetPx;
                ring.StrikeIcon = UiFactory.CreateImage(root, "StrikeIcon", VfxFactory.SwordIcon, GameConfig.StrikeRingColor, GameConfig.StrikeIconPx);
                ring.StrikeIcon.rectTransform.anchoredPosition = GameConfig.StrikeIconOffsetPx;
            }
            if (cue.Tag is RhythmNote) ring.Button.color = RingGlowColor(cue) * new Color(0.6f, 0.6f, 0.6f, 0.9f);   // 손 색 버튼
            else AssignChain(ring);                                   // 장단 노드는 묶음 대신 장단 줄의 고정 칸
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
                var targets = new List<double>();
                foreach (var m in chain.Members) targets.Add(m.TargetBeat);
                chain.Slots = RingLayout.ChainSlots(targets);
                chains.Add(chain);
                ring.Chain = chain; ring.ChainIndex = 0;
            }
        }

        /// <summary>판정 없이 취소된 원은 문구 없이 지운다 (B11: 전투·윈드밀 재도전, 게임 오버, 다시 시작).</summary>
        void OnCancelled(JudgeCue cue) => RemoveRing(cue);

        /// <summary>원을 지운다. Destroy는 프레임 끝에 일어나므로 먼저 숨겨 같은 프레임에서도 화면에 남지 않게 한다.</summary>
        static void KillRing(Ring r)
        {
            if (r.Root == null) return;
            r.Root.gameObject.SetActive(false);
            Destroy(r.Root.gameObject);
        }

        void RemoveRing(JudgeCue cue)
        {
            for (int i = rings.Count - 1; i >= 0; i--)
            {
                if (rings[i].Cue != cue) continue;
                KillRing(rings[i]);
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
                KillRing(rings[i]);
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
                if (rings[i].Cue.Judged) { KillRing(rings[i]); rings.RemoveAt(i); }
            foreach (var r in rings) UpdateRing(r);
            // 다음에 누를 원(가장 이른 원)은 선명, 기다리는 원은 흐리게 (GDD-beta B10.1)
            Ring next = null;
            foreach (var r in rings) if (!r.Cue.Judged && (next == null || r.Cue.TargetBeat < next.Cue.TargetBeat)) next = r;
            foreach (var r in rings) if (r.Group != null) r.Group.alpha = r == next ? GameConfig.RingNextAlpha : GameConfig.RingWaitingAlpha;
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
            /// <summary>화려한 층 (B17).</summary>
            public Image Lightning, Flare, Shock, Core, Crack, Smoke;
            public List<Spark> Sparks = new List<Spark>();
            public float ShownAt, Seconds;
            public Vector2 BasePos;
        }
        readonly List<RingFx> ringFx = new List<RingFx>();
        public IReadOnlyList<RingFx> RingEffects => ringFx;
        public static readonly Color PerfectFxColor = new Color(1f, 0.82f, 0.25f);
        public static readonly Color NormalFxColor = new Color(0.15f, 0.85f, 0.75f);
        public static readonly Color FailFxColor = new Color(0.9f, 0.2f, 0.12f);

        /// <summary>바깥으로 흩어지는 빛 입자 하나.</summary>
        public class Spark
        {
            public Image Img;
            public Vector2 Dir;
            public float Speed;
        }

        /// <summary>
        /// 화려한 판정 이펙트 층 (GDD-beta B8.5, 대격돌 수준의 화려함을 기준점으로).
        /// PERFECT: 방사형 번개선 + 십자·X 섬광 + 충격 고리(3배) + 빛 입자 18 + 금빛 중심 섬광.
        /// NORMAL: 작은 번개선 + 빛 입자 8 (+ 기존 청록 고리). FAIL: 붉은 금 + 어두운 연기 (+ 기존 흔들림·갈라짐).
        /// </summary>
        void AddBurst(RingFx fx)
        {
            var root = fx.Root;
            float d = GameConfig.RingTargetDiameterPx;
            int sparks = 0;
            Color sparkColor = Color.white;
            switch (fx.Result)
            {
                case Judgement.Perfect:
                    fx.Shock = AddImage(root, "Shock", UiFactory.Ring, Color.white, d, true);
                    fx.Lightning = AddImage(root, "Lightning", VfxFactory.Lightning, new Color(0.55f, 0.85f, 1f), GameConfig.FxLightningDiameterPx, true);
                    fx.Lightning.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    fx.Flare = AddImage(root, "Flare", VfxFactory.Flare, new Color(0.85f, 0.93f, 1f), GameConfig.FxFlareDiameterPx, true);
                    fx.Flare.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-12f, 12f));
                    fx.Core = AddImage(root, "Core", UiFactory.Glow, PerfectFxColor, d * 1.4f, true);
                    sparks = GameConfig.FxSparksPerfect;
                    sparkColor = new Color(1f, 0.9f, 0.55f);
                    break;
                case Judgement.Normal:
                    fx.Lightning = AddImage(root, "Lightning", VfxFactory.Lightning, NormalFxColor, GameConfig.FxLightningDiameterPx * 0.6f, true);
                    fx.Lightning.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                    sparks = GameConfig.FxSparksNormal;
                    sparkColor = NormalFxColor;
                    break;
                default:
                    fx.Smoke = UiFactory.CreateImage(root, "Smoke", VfxFactory.Smoke, new Color(0.12f, 0.08f, 0.08f, 0.6f), d * 1.6f);
                    fx.Smoke.transform.SetAsFirstSibling();
                    fx.Crack = UiFactory.CreateImage(root, "Crack", VfxFactory.Crack, FailFxColor, d * 1.1f);
                    break;
            }
            for (int i = 0; i < sparks; i++)
            {
                float ang = (i + Random.Range(-0.3f, 0.3f)) / sparks * Mathf.PI * 2f;
                var img = AddImage(root, "Spark" + i, VfxFactory.Dot, sparkColor, Random.Range(10f, 22f), true);
                fx.Sparks.Add(new Spark { Img = img, Dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)), Speed = GameConfig.FxSparkSpeedPx * Random.Range(0.6f, 1.1f) });
            }
        }

        /// <summary>화려한 층의 모양 (u: 0~1 진행도, age: 초).</summary>
        void PoseBurst(RingFx fx, float u, float age)
        {
            float fade = 1f - u;
            float ease = 1f - (1f - u) * (1f - u);
            if (fx.Shock != null)
            {
                fx.Shock.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, GameConfig.FxShockScale, ease);
                SetA(fx.Shock, 0.9f * fade);
            }
            if (fx.Lightning != null)
            {
                fx.Lightning.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.4f, Mathf.Sqrt(u));
                SetA(fx.Lightning, fade * fade);
            }
            if (fx.Flare != null)
            {
                // 처음에 확 퍼졌다가 가늘어지며 사라짐
                float s = u < 0.25f ? Mathf.Lerp(0.6f, 1.15f, u / 0.25f) : Mathf.Lerp(1.15f, 0.9f, (u - 0.25f) / 0.75f);
                fx.Flare.rectTransform.localScale = new Vector3(s, s * Mathf.Lerp(1f, 0.6f, u), 1f);
                SetA(fx.Flare, u < 0.15f ? 1f : fade);
            }
            if (fx.Core != null)
            {
                fx.Core.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, ease);
                SetA(fx.Core, fade);
            }
            if (fx.Smoke != null)
            {
                fx.Smoke.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.4f, ease);
                SetA(fx.Smoke, 0.6f * fade);
            }
            if (fx.Crack != null) SetA(fx.Crack, fade);
            foreach (var sp in fx.Sparks)
            {
                // 감속하며 바깥으로 (거리 = v·(t − t²/2T))
                float dist = sp.Speed * (age - age * age / (2f * fx.Seconds));
                sp.Img.rectTransform.anchoredPosition = sp.Dir * dist;
                SetA(sp.Img, fade);
            }
        }

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
            AddBurst(fx);
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
            PoseBurst(fx, u, now - fx.ShownAt);
            return true;
        }

        static void SetA(Image i, float a) { var c = i.color; c.a = a; i.color = c; }

        /// <summary>키 반응 펄스 배율 (1 → 1.15 → 1, 0.12초).</summary>
        public static float PulseScale(float age)
        {
            if (age < 0f || age >= GameConfig.InputPulseSeconds) return 1f;
            return 1f + (GameConfig.InputPulseScale - 1f) * Mathf.Sin(age / GameConfig.InputPulseSeconds * Mathf.PI);
        }

        /// <summary>
        /// 다가오는 원의 빛 (GDD-beta B8.5): 목표에 가까울수록 가산 빛이 0.4 → 1.0으로 밝아지고,
        /// 바깥 원에도 빛이 붙으며, 빛 점 8개가 원 둘레를 돈다.
        /// </summary>
        void AnimateCharge(Ring r, double untilBeats, float outerDiameter)
        {
            float p = Mathf.Clamp01(1f - (float)(untilBeats / GameConfig.ApproachBeats));
            r.Charge = Mathf.Lerp(GameConfig.RingChargeMin, 1f, p);
            float now = RealTime();
            if (r.ChargeGlow != null)
            {
                var c = r.ChargeGlow.color; c.a = r.Charge; r.ChargeGlow.color = c;
                float breathe = 1f + 0.08f * Mathf.Sin(now * Mathf.PI * 2f * 3f) * p;
                r.ChargeGlow.rectTransform.localScale = Vector3.one * breathe;
            }
            if (r.OuterGlow != null)
            {
                r.OuterGlow.rectTransform.sizeDelta = Vector2.one * outerDiameter * 1.15f;
                var c = r.OuterGlow.color; c.a = 0.5f * r.Charge; r.OuterGlow.color = c;
            }
            if (r.Orbit != null)
            {
                for (int i = 0; i < r.Orbit.Length; i++)
                {
                    float ang = (now * GameConfig.RingOrbitDegPerSec + i * 360f / r.Orbit.Length) * Mathf.Deg2Rad;
                    r.Orbit[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * GameConfig.RingOrbitRadiusPx;
                    var c = r.Orbit[i].color; c.a = 0.35f + 0.65f * r.Charge; r.Orbit[i].color = c;
                }
            }
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
                AnimateCharge(r, until, d);
            }
            if (r.Locked) return;
            if (r.Cue.Tag is RhythmNote note)
            {
                // 장단 줄 (GDD-beta B18.2): 화면 위쪽 한 줄, 노드 순서대로 왼쪽부터 칸을 돌려 씀
                r.Root.anchoredPosition = RowSlot(note.Index);
                r.Locked = true;
                return;
            }
            var radii = RingLayout.SafeRadii(Size());
            Vector2 pos;
            bool clamped;
            if (r.Chain != null && r.ChainIndex == 0 && r.Chain.Fixed)
            {
                pos = RingLayout.ChainSlot(r.Chain.Anchor, 0, r.Chain.Slots, radii);   // 뒤 원이 이미 자리를 잡음: 첫 원도 그 기준점에 고정
                clamped = false;
                r.Locked = true;
            }
            else if (r.Chain != null && r.ChainIndex > 0)
            {
                pos = RingLayout.ChainSlot(r.Chain.Anchor, r.Chain.SlotOf(r.ChainIndex), r.Chain.Slots, radii);
                r.Chain.Fixed = true;
                clamped = false;
                r.Locked = true;                                  // 묶음의 자리는 첫 원이 정한다
            }
            else
            {
                pos = SafePosition(r.Cue, radii, out clamped, out var outward);
                pos = AvoidOverlap(r, pos, radii);
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
                    pos = RingLayout.ChainSlot(pos, 0, r.Chain.Slots, radii);
                }
                if (RealTime() - r.AppearedAt >= GameConfig.RingLockSeconds) r.Locked = true;
            }
            if (pos.y > GameConfig.Jangdan.EventMaxYPx) pos.y = GameConfig.Jangdan.EventMaxYPx;   // 장단 줄을 피해 그 아래로 (B18.2)
            r.Root.anchoredPosition = pos;
        }

        /// <summary>장단 줄의 index번째 노드 자리 (순서대로 왼쪽부터 RowSlots칸을 돌려 씀).</summary>
        public static Vector2 RowSlot(int index)
        {
            int n = GameConfig.Jangdan.RowSlots;
            int slot = ((index % n) + n) % n;
            return new Vector2((slot - (n - 1) * 0.5f) * GameConfig.Jangdan.RowSpacingPx, GameConfig.Jangdan.RowYPx);
        }

        /// <summary>
        /// 이미 떠 있는 다른 원과 140 px 안이면 겹치지 않을 때까지 가로 160 px씩 옮긴다 (GDD-beta B10.1).
        /// 오른쪽 먼저, 안전 타원 가로 범위 안에서만. 둘 곳이 없으면 그대로.
        /// </summary>
        Vector2 AvoidOverlap(Ring r, Vector2 pos, Vector2 radii)
        {
            for (int k = 0; k < 7; k++)
            {
                int step = (k + 1) / 2 * (k % 2 == 1 ? 1 : -1);          // 0, +1, −1, +2, −2, ...
                var cand = new Vector2(pos.x + step * GameConfig.RingChainSpacingPx, pos.y);
                if (Mathf.Abs(cand.x) > radii.x + 0.5f) continue;
                bool free = true;
                // 묶음 첫 원이면 묶음이 쓸 자리 전부를 검사한다 (GDD-beta B11.6: 자리를 돌려 쓰므로 Slots개)
                int slots = r.Chain != null ? r.Chain.Slots : 1;
                for (int j = 0; j < slots && free; j++)
                {
                    var at = r.Chain != null ? RingLayout.ChainSlot(cand, j, slots, radii) : cand;
                    foreach (var o in rings)
                    {
                        if (o == r) break;                                     // 먼저 뜬 원만 피한다 (나중 원이 비켜 감)
                        if ((r.Chain != null && o.Chain == r.Chain) || o.Root == null) continue;
                        if (Vector2.Distance(o.Root.anchoredPosition, at) < GameConfig.RingOverlapMinPx) { free = false; break; }
                    }
                }
                if (free) return cand;
            }
            return pos;
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
