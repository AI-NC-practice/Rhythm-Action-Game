using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmActionGame
{
    /// <summary>
    /// 화면 효과 (GDD-beta B5, 제안서 1.2·4장). URP 전역 Volume 하나(우선순위 20)에 Vignette, ChromaticAberration,
    /// LensDistortion, Bloom, ColorAdjustments(postExposure, saturation)를 둔다.
    /// 판정 화면(JudgeFx: 채도 −60, 노출 −0.4, 비네트 0.3)과 화면 효과 펄스들을 **항목별 최댓값 하나**로 합친다
    /// (채도는 최솟값, 렌즈 왜곡은 절댓값이 큰 쪽, 노출은 밝히는 섬광이 있으면 그 최댓값·없으면 가장 어두운 값). 더하지 않는다.
    /// 판정 원 UI는 Screen Space Overlay라 이 효과를 받지 않는다.
    /// </summary>
    public class ScreenFx : MonoBehaviour
    {
        public Volume volume;
        /// <summary>판정 화면 값을 읽어 합친다.</summary>
        public JudgeFx judgeFx;
        /// <summary>새 판정 원이 뜨면 붉은 테두리를 절반으로 (제안서 2장 D).</summary>
        public JudgeSystem judge;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public System.Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        ColorAdjustments color;
        Vignette vignette;
        ChromaticAberration chroma;
        LensDistortion lens;
        Bloom bloom;

        class Active { public ScreenPulse P; public float Age; public float Scale = 1f; }
        readonly List<Active> pulses = new List<Active>();

        public float Exposure { get; private set; }
        public float Saturation { get; private set; }
        public float VignetteIntensity { get; private set; }
        public Color VignetteColor { get; private set; } = Color.black;
        public float Chromatic { get; private set; }
        public float Lens { get; private set; }
        public float BloomIntensity { get; private set; }
        public int ActivePulses => pulses.Count;
        /// <summary>박 펄스 화면 가장자리 (GDD-beta B17.2, BeatFx가 매 프레임 넣음). 다른 비네트와 최댓값으로 합친다.</summary>
        public float BeatVignette { get; set; }
        /// <summary>슬로모션 회색 화면 (GDD-beta B17.4 2차, BulletTimeFx가 매 프레임 넣음, 음수). 다른 채도와 최솟값으로 합친다.</summary>
        public float SlowSaturation { get; set; }
        public VolumeProfile Profile => volume != null ? volume.profile : null;

        void Awake() => EnsureSetup();

        void OnEnable() { if (judge != null) judge.CueAppeared += OnCueAppeared; }
        void OnDisable() { if (judge != null) judge.CueAppeared -= OnCueAppeared; }

        public void EnsureSetup()
        {
            if (volume == null)
            {
                volume = GetComponent<Volume>();
                if (volume == null) volume = gameObject.AddComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 20;      // 판정 화면 Volume(10)보다 위 — 합친 값을 여기서 최종 적용
            if (volume.profile == null) volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var p = volume.profile;
            if (!p.TryGet(out color)) color = p.Add<ColorAdjustments>(true);
            if (!p.TryGet(out vignette)) vignette = p.Add<Vignette>(true);
            if (!p.TryGet(out chroma)) chroma = p.Add<ChromaticAberration>(true);
            if (!p.TryGet(out lens)) lens = p.Add<LensDistortion>(true);
            if (!p.TryGet(out bloom)) bloom = p.Add<Bloom>(true);
            color.postExposure.overrideState = true;
            color.saturation.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.color.overrideState = true;
            chroma.intensity.overrideState = true;
            lens.intensity.overrideState = true;
            bloom.intensity.overrideState = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = GameConfig.Fx.BloomThreshold;
            Apply();
        }

        /// <summary>화면 효과 하나를 건다.</summary>
        public void Add(ScreenPulse p) => pulses.Add(new Active { P = p });

        public void Add(IEnumerable<ScreenPulse> ps) { foreach (var p in ps) Add(p); }

        public void ClearAll() { pulses.Clear(); Apply(); }

        void OnCueAppeared(JudgeCue cue)
        {
            // 붉은 테두리가 다음 판정 원을 방해하지 않게 (가독성)
            foreach (var a in pulses)
                if (a.P.Channel == ScreenChannel.Vignette && a.P.Color != Color.black) a.Scale = Mathf.Min(a.Scale, GameConfig.Fx.HitVignetteOnNextRing);
        }

        void LateUpdate() => Step(DeltaTime());

        public void Step(float dt)
        {
            for (int i = pulses.Count - 1; i >= 0; i--)
            {
                pulses[i].Age += dt;
                if (pulses[i].Age >= pulses[i].P.Seconds) pulses.RemoveAt(i);
            }
            Apply();
        }

        void Apply()
        {
            // 판정 화면 값에서 출발
            float exp = judgeFx != null ? judgeFx.Exposure : 0f;
            float sat = judgeFx != null ? judgeFx.Saturation : 0f;
            float vig = judgeFx != null ? judgeFx.VignetteIntensity : 0f;
            Color vigColor = Color.black;
            float ca = 0f, ld = 0f, bl = GameConfig.Fx.BloomBase;
            if (BeatVignette > vig) vig = BeatVignette;
            if (SlowSaturation < sat) sat = SlowSaturation;
            // 노출: 밝히는 섬광이 있으면 그중 최댓값, 없으면 가장 어두운 값 (판정 화면 포함)
            float brightest = 0f, darkest = Mathf.Min(0f, exp);
            foreach (var a in pulses)
            {
                float v = a.P.At(a.Age) * a.Scale;
                switch (a.P.Channel)
                {
                    case ScreenChannel.Exposure: if (v > 0f) brightest = Mathf.Max(brightest, v); else darkest = Mathf.Min(darkest, v); break;
                    case ScreenChannel.Saturation: sat = Mathf.Min(sat, v); break;
                    case ScreenChannel.Vignette: if (v > 0f && v >= vig) { vig = v; vigColor = a.P.Color; } break;   // 같으면 색 있는 효과(붉은 테두리)가 보이게
                    case ScreenChannel.Chromatic: ca = Mathf.Max(ca, v); break;
                    case ScreenChannel.LensDistortion: if (Mathf.Abs(v) > Mathf.Abs(ld)) ld = v; break;
                    case ScreenChannel.Bloom: bl = Mathf.Max(bl, v); break;
                }
            }
            exp = brightest > 0f ? brightest : darkest;
            Exposure = exp; Saturation = sat; VignetteIntensity = vig; VignetteColor = vigColor; Chromatic = ca; Lens = ld; BloomIntensity = bl;
            if (color == null) return;
            color.postExposure.value = exp;
            color.saturation.value = sat;
            vignette.intensity.value = vig;
            vignette.color.value = vigColor;
            chroma.intensity.value = ca;
            lens.intensity.value = ld;
            bloom.intensity.value = bl;
        }
    }
}
