using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 판정 원 UI (GDD 4.2). 판정이 등장하면 원을 띄우고, 바깥 원을 360→120 px로 2박자 동안 줄인다.
    /// 판정되면 원을 지우고 그 자리에 "PERFECT" / "NORMAL" / "FAIL"을 0.6초 동안 보여 준다.
    /// </summary>
    public class JudgeRingView : MonoBehaviour
    {
        public JudgeSystem judge;
        public BeatClock clock;
        public Camera worldCamera;

        /// <summary>원 하나의 화면 요소.</summary>
        public class Ring
        {
            public JudgeCue Cue;
            public RectTransform Root;
            public Image Outer, Target, Button;
            public Text Arrow;
        }

        /// <summary>판정 문구 하나.</summary>
        public class Label
        {
            public Text Text;
            public float ShownAt;   // unscaled time
        }

        readonly List<Ring> rings = new List<Ring>();
        readonly List<Label> labels = new List<Label>();
        RectTransform canvasRect;

        public IReadOnlyList<Ring> Rings => rings;
        public IReadOnlyList<Label> Labels => labels;

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public System.Func<float> RealTime = () => Time.unscaledTime;

        static readonly Color OuterColor = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color TargetColor = new Color(1f, 0.85f, 0.3f, 1f);
        static readonly Color ButtonColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);

        public void Bind(JudgeSystem j, BeatClock c, Camera cam)
        {
            Unbind();
            judge = j; clock = c; worldCamera = cam;
            if (judge != null)
            {
                judge.CueAppeared += OnAppeared;
                judge.CueJudged += OnJudged;
            }
        }

        void Unbind()
        {
            if (judge == null) return;
            judge.CueAppeared -= OnAppeared;
            judge.CueJudged -= OnJudged;
        }

        void OnEnable() { if (judge != null) Bind(judge, clock, worldCamera); }
        void OnDisable() => Unbind();

        void Awake() => canvasRect = transform as RectTransform;

        void OnAppeared(JudgeCue cue)
        {
            var root = new GameObject("Ring", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(transform, false);
            var ring = new Ring { Cue = cue, Root = root };
            ring.Button = UiFactory.CreateImage(root, "Button", UiFactory.Disc, ButtonColor, GameConfig.RingTargetDiameterPx);
            ring.Target = UiFactory.CreateImage(root, "TargetRing", UiFactory.Ring, TargetColor, GameConfig.RingTargetDiameterPx);
            ring.Outer = UiFactory.CreateImage(root, "OuterRing", UiFactory.Ring, OuterColor, GameConfig.RingStartDiameterPx);
            ring.Arrow = UiFactory.CreateText(root, "Arrow", JudgeRules.Arrow(cue.Key), 64, Color.white, new Vector2(120, 120));
            rings.Add(ring);
            UpdateRing(ring);
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
            var t = UiFactory.CreateText(transform, "JudgeLabel", JudgeRules.Label(r), 56, color, new Vector2(400, 90));
            t.rectTransform.anchoredPosition = pos;
            labels.Add(new Label { Text = t, ShownAt = RealTime() });
        }

        void Update() => Refresh();

        /// <summary>원 크기·위치와 문구 수명을 갱신한다.</summary>
        public void Refresh()
        {
            foreach (var r in rings) UpdateRing(r);
            float now = RealTime();
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                if (now - labels[i].ShownAt < GameConfig.JudgeTextSeconds) continue;
                if (labels[i].Text != null) Destroy(labels[i].Text.gameObject);
                labels.RemoveAt(i);
            }
        }

        void UpdateRing(Ring r)
        {
            if (clock != null)
            {
                double until = r.Cue.TargetBeat - clock.CurrentBeat;
                float d = JudgeRules.OuterRingDiameter(until);
                r.Outer.rectTransform.sizeDelta = new Vector2(d, d);
            }
            r.Root.anchoredPosition = ScreenPosition(r.Cue);
        }

        /// <summary>판정 지점을 화면(캔버스 좌표)으로 투영한다. 없거나 카메라 뒤면 화면 가운데.</summary>
        Vector2 ScreenPosition(JudgeCue cue)
        {
            if (!cue.WorldPoint.HasValue || worldCamera == null || canvasRect == null) return Vector2.zero;
            var sp = worldCamera.WorldToScreenPoint(cue.WorldPoint.Value);
            if (sp.z <= 0) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local);
            return local;
        }
    }
}
