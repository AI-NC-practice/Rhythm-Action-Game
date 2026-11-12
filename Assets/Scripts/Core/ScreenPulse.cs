using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>화면 효과 항목 (GDD-beta B5, 제안서 1.2 ScreenFx).</summary>
    public enum ScreenChannel
    {
        Exposure,        // ColorAdjustments.postExposure (EV)
        Saturation,      // ColorAdjustments.saturation (−100~100) — 항목 규칙: 최솟값
        Vignette,        // Vignette.intensity (색은 Color)
        Chromatic,       // ChromaticAberration.intensity
        LensDistortion,  // LensDistortion.intensity (절댓값이 큰 쪽)
        Bloom,           // Bloom.intensity
    }

    /// <summary>
    /// 화면 효과 한 번: 시작할 때 Value였다가 Seconds 동안 0으로 줄어든다 (제안서 2장 "A → 0 (t초)").
    /// </summary>
    public struct ScreenPulse
    {
        public ScreenChannel Channel;
        public float Value;
        public float Seconds;
        public Color Color;

        public ScreenPulse(ScreenChannel ch, float value, float seconds, Color? color = null)
        {
            Channel = ch; Value = value; Seconds = seconds; Color = color ?? Color.black;
        }

        /// <summary>시작 후 a초에서의 값 (선형으로 0까지).</summary>
        public float At(float a) => a < 0 || a >= Seconds ? 0f : Value * (1f - a / Seconds);
    }

    /// <summary>VFX 종류 (GDD-beta B5 이펙트 언어).</summary>
    public enum VfxKind
    {
        BrushBurst,    // 붓획 방사 섬광 (화면, 판정 원 자리)
        InkSlash,      // 화면을 가르는 먹 획
        InkX,          // 먹 X 획 (윈드밀·무사 마무리)
        WhiteFlash,    // 흰 화면 플래시
        Embers,        // 불씨 스파크 (월드 파티클)
        Shockwave,     // 충격파 링 (월드)
        Dust,          // 흙먼지 (월드 파티클)
        TileDebris,    // 기왓장 파편 (월드 파티클)
        HanjiDebris,   // 한지 조각 (월드 파티클)
        RewindStreak,  // 되감기: 화면을 가로지르며 거꾸로 흐르는 먹 줄기 (GDD-beta B9.4)
    }
}
