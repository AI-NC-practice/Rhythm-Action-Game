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
        /// <summary>
        /// 연출 이름별 효과음과 음악 덕킹 (제안서 2장 소리 열). duckDb 0이면 덕킹 없음.
        /// </summary>
        public static void Sounds(string preset, List<SfxKind> sfx, out float duckDb, out float duckSeconds)
        {
            duckDb = 0f; duckSeconds = 0f;
            switch (preset)
            {
                case "AttackWarn": sfx.Add(SfxKind.Glint); sfx.Add(SfxKind.Whoosh); break;                     // A: "칭" + 바람
                case "ParryPerfect":
                case "ParryPerfectFinale":                                                                      // B: 높은 "챙" + 저음 "쿵", −4 dB 0.15초
                    sfx.Add(SfxKind.MetalClang); sfx.Add(SfxKind.LowBoom);
                    duckDb = GameConfig.Fx.DuckParryDb; duckSeconds = GameConfig.Fx.DuckParrySeconds; break;
                case "DodgePerfect": sfx.Add(SfxKind.Whoosh); break;                                            // B 회피: 바람
                case "DefendNormal": sfx.Add(SfxKind.DullClank); break;                                         // C: 둔한 "탕"
                case "Hit":                                                                                     // D: 타격 + 저음 + 귀 울림, −8 dB 0.3초
                    sfx.Add(SfxKind.BodyHit); sfx.Add(SfxKind.LowBoom); sfx.Add(SfxKind.EarRing);
                    duckDb = GameConfig.Fx.DuckHitDb; duckSeconds = GameConfig.Fx.DuckHitSeconds; break;
                case "Execution":
                case "ExecutionKick":                                                                           // E: 무거운 타격 + 깊은 저음 + 잔향(징), −6 dB 0.5초
                    sfx.Add(SfxKind.BodyHit); sfx.Add(SfxKind.LowBoom); sfx.Add(SfxKind.Jing);
                    duckDb = GameConfig.Fx.DuckExecutionDb; duckSeconds = GameConfig.Fx.DuckExecutionSeconds; break;
                case "SurroundTurn": case "Simultaneous": sfx.Add(SfxKind.Whoosh); break;                       // F: 칼이 지나가는 바람
                case "SimultaneousPerfect": sfx.Add(SfxKind.Whoosh); sfx.Add(SfxKind.MetalClang); break;
                case "WindmillArrow": sfx.Add(SfxKind.Buk); break;                                              // F3: 화살표마다 북
                case "WindmillFinish":                                                                          // F3: 바람 상승 → 초저음 "쿵" + 넓은 잔향
                    sfx.Add(SfxKind.Whoosh); sfx.Add(SfxKind.LowBoom); sfx.Add(SfxKind.Jing);
                    duckDb = GameConfig.Fx.DuckExecutionDb; duckSeconds = GameConfig.Fx.DuckExecutionSeconds; break;
                case "JumpPerfect": sfx.Add(SfxKind.Whoosh); break;                                             // G: 도약 바람
                case "LandPerfect": case "LandNormal": sfx.Add(SfxKind.DustPuff); break;                        // G: 착지 먼지
                case "LandFail": case "ParkourFail": sfx.Add(SfxKind.BodyHit); sfx.Add(SfxKind.DustPuff); break;
                case "EventPerfectSlow": sfx.Add(SfxKind.Whoosh); sfx.Add(SfxKind.Buk); break;
            }
        }
    }
}
