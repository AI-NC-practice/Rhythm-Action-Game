using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 연출 이름별 화면 효과·이펙트 조합 (제안서 2장 표의 "화면", "효과" 열). 수치는 GameConfig.Fx.
    /// PresentationDirector.Play가 카메라·시간과 함께 한 번에 건다.
    /// </summary>
    public static class FxLooks
    {
        public struct Vfx
        {
            public VfxKind Kind;
            public int Count;     // −1이면 기본
            public Vfx(VfxKind k, int count = -1) { Kind = k; Count = count; }
        }

        public static void For(string preset, List<ScreenPulse> screen, List<Vfx> vfx)
        {
            switch (preset)
            {
                case "ParryPerfect":
                case "ParryPerfectFinale":
                    // B: 노출 +0.6 → 0 (0.1초), 색수차 0.4 → 0 (0.2초), 붓획 방사 섬광 + 불씨 30
                    screen.Add(new ScreenPulse(ScreenChannel.Exposure, GameConfig.Fx.ParryExposure, GameConfig.Fx.ParryExposureSeconds));
                    screen.Add(new ScreenPulse(ScreenChannel.Chromatic, GameConfig.Fx.ParryChromatic, GameConfig.Fx.ParryChromaticSeconds));
                    vfx.Add(new Vfx(VfxKind.BrushBurst));
                    vfx.Add(new Vfx(VfxKind.Embers, GameConfig.Fx.EmbersPerfect));
                    break;
                case "DefendNormal":
                    // C: 스파크 10, 섬광 없음
                    vfx.Add(new Vfx(VfxKind.Embers, GameConfig.Fx.EmbersNormal));
                    break;
                case "Hit":
                    // D: 주홍 비네트 0.45 → 0 (0.4초), 채도 −80 순간, 렌즈 왜곡 −0.2 펄스
                    screen.Add(new ScreenPulse(ScreenChannel.Vignette, GameConfig.Fx.HitVignette, GameConfig.Fx.HitVignetteSeconds, GameConfig.Fx.Vermilion));
                    screen.Add(new ScreenPulse(ScreenChannel.Saturation, GameConfig.Fx.HitSaturation, GameConfig.Fx.HitSaturationSeconds));
                    screen.Add(new ScreenPulse(ScreenChannel.LensDistortion, GameConfig.Fx.HitLens, GameConfig.Fx.HitLensSeconds));
                    break;
                case "Execution":
                case "ExecutionKick":
                    // E: 화면을 가르는 먹 획 + 충격파 링 + 흙먼지
                    vfx.Add(new Vfx(VfxKind.InkSlash));
                    vfx.Add(new Vfx(VfxKind.Shockwave));
                    vfx.Add(new Vfx(VfxKind.Dust));
                    break;
                case "SimultaneousPerfect":
                    vfx.Add(new Vfx(VfxKind.Embers, GameConfig.Fx.EmbersPerfect));
                    break;
                case "WindmillArrow":
                    // F3: 화살표마다 붓획 방사 섬광
                    vfx.Add(new Vfx(VfxKind.BrushBurst));
                    break;
                case "WindmillFinish":
                    // F3: 화면 먹 X + 지면 충격파(6 m) + 흙먼지 + 흰 플래시(노출) + 색수차 0.6 → 0
                    screen.Add(new ScreenPulse(ScreenChannel.Chromatic, GameConfig.Fx.WindmillChromatic, GameConfig.Fx.WindmillChromaticSeconds));
                    screen.Add(new ScreenPulse(ScreenChannel.Exposure, GameConfig.Fx.WhiteFlashExposure, GameConfig.Fx.WhiteFlashSeconds));
                    vfx.Add(new Vfx(VfxKind.InkX));
                    vfx.Add(new Vfx(VfxKind.Shockwave));
                    vfx.Add(new Vfx(VfxKind.Dust));
                    break;
                case "ParkourFail":
                    // G: 흔들림 0.5 + 붉은 테두리 0.3
                    screen.Add(new ScreenPulse(ScreenChannel.Vignette, GameConfig.Fx.ParkourFailVignette, GameConfig.Fx.HitVignetteSeconds, GameConfig.Fx.Vermilion));
                    vfx.Add(new Vfx(VfxKind.Dust));
                    break;
                case "LandPerfect":
                    vfx.Add(new Vfx(VfxKind.Dust, GameConfig.Fx.DustCount / 2));
                    break;
                case "LandNormal":
                case "LandFail":
                    vfx.Add(new Vfx(VfxKind.Dust));
                    break;
                case "EventPerfectSlow":
                    vfx.Add(new Vfx(VfxKind.BrushBurst));
                    vfx.Add(new Vfx(VfxKind.HanjiDebris));
                    break;
            }
        }

        /// <summary>처형 원 예고 (제안서 2장 E 예고): 노출 −0.6, 비네트 0.45 (원이 떠 있는 동안 유지되도록 길이를 준다).</summary>
        public static void FinisherWarn(List<ScreenPulse> screen, float seconds)
        {
            screen.Add(new ScreenPulse(ScreenChannel.Exposure, GameConfig.Fx.FinisherWarnExposure, seconds));
            screen.Add(new ScreenPulse(ScreenChannel.Vignette, GameConfig.Fx.FinisherWarnVignette, seconds));
        }
    }
}
