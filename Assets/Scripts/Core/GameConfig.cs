namespace RhythmActionGame
{
    /// <summary>
    /// 게임의 모든 수치를 모아 둔 단일 출처. 값은 docs/GDD.md와 일치해야 한다.
    /// GDD에 없는 값을 정했으면 주석에 "GDD에 없음, Claude가 정함"이라고 적고 log.md 제안에 남긴다.
    /// </summary>
    public static class GameConfig
    {
        // ---------------- GDD 4.1 박자와 속도 ----------------
        /// <summary>기준 BPM (속도 배율 1.0일 때).</summary>
        public const float Bpm = 120f;
        /// <summary>1박자 길이 (초, 속도 배율 1.0일 때).</summary>
        public const float SecondsPerBeat = 60f / Bpm;
        /// <summary>한 마디의 박자 수.</summary>
        public const int BeatsPerBar = 4;

        /// <summary>기본 달리기 속도 (m/s, 속도 배율 1.0일 때).</summary>
        public const float BaseMoveSpeed = 5f;
        /// <summary>1박자에 이동하는 거리 (m). 속도 배율과 무관하게 일정.</summary>
        public const float MetersPerBeat = BaseMoveSpeed * SecondsPerBeat;

        /// <summary>속도 배율 시작값.</summary>
        public const float SpeedStart = 1.00f;
        /// <summary>속도 배율 최솟값.</summary>
        public const float SpeedMin = 1.00f;
        /// <summary>속도 배율 최댓값.</summary>
        public const float SpeedMax = 1.30f;
        /// <summary>PERFECT 한 번에 더하는 속도 배율.</summary>
        public const float PerfectSpeedStep = 0.05f;
        /// <summary>FAIL 때 속도 배율에 곱하는 값.</summary>
        public const float FailSpeedMultiplier = 0.8f;

        // ---------------- GDD 4.2 판정 ----------------
        /// <summary>판정 원이 목표 시점보다 몇 박자 먼저 나타나는지.</summary>
        public const float ApproachBeats = 2f;
        /// <summary>PERFECT 판정 창 (± ms, 실제 시간).</summary>
        public const float PerfectWindowMs = 60f;
        /// <summary>NORMAL 판정 창 (± ms, 실제 시간).</summary>
        public const float NormalWindowMs = 150f;
        /// <summary>이보다 이른 입력(목표 − ms)은 무시한다. 이 값과 NORMAL 창 사이 입력은 FAIL.</summary>
        public const float EarlyIgnoreMs = 300f;

        /// <summary>판정 중 화면 채도 (URP Color Adjustments saturation, −100~100).</summary>
        public const float JudgeSaturation = -60f;
        /// <summary>회색 화면·줌인 전환 시간 (초).</summary>
        public const float JudgeEnterSeconds = 0.15f;
        /// <summary>판정 후 채도·FOV 복귀 시간 (초).</summary>
        public const float JudgeExitSeconds = 0.2f;
        /// <summary>평소 카메라 FOV (도).</summary>
        public const float FovDefault = 70f;
        /// <summary>판정 중 줌인 FOV (도).</summary>
        public const float FovZoom = 58f;
        /// <summary>판정 중 카메라가 판정 지점 쪽으로 도는 최대 각도 (도).</summary>
        public const float JudgeMaxTurnDegrees = 25f;

        /// <summary>판정 원 UI 기준 해상도 높이 (px).</summary>
        public const float UiReferenceHeight = 1080f;
        /// <summary>목표 원(버튼 둘레) 지름 (px, 1080p 기준).</summary>
        public const float RingTargetDiameterPx = 120f;
        /// <summary>바깥 원 시작 지름 (px, 1080p 기준).</summary>
        public const float RingStartDiameterPx = 360f;
        /// <summary>판정 문구 표시 시간 (초, GDD 4.2 원래 값). beta1-1에서는 GDD-beta B1.2의 JudgeLabelTotalSeconds(0.45초)를 쓴다.</summary>
        public const float JudgeTextSeconds = 0.6f;

        /// <summary>타이밍 보정 최솟값 (ms).</summary>
        public const int TimingOffsetMinMs = -200;
        /// <summary>타이밍 보정 최댓값 (ms).</summary>
        public const int TimingOffsetMaxMs = 200;
        /// <summary>타이밍 보정 단위 (ms).</summary>
        public const int TimingOffsetStepMs = 5;
        /// <summary>타이밍 보정 기본값 (ms).</summary>
        public const int TimingOffsetDefaultMs = 0;

        /// <summary>처형·윈드밀 슬로모션 시간 배율.</summary>
        public const float SlowMotionTimeScale = 0.3f;
        /// <summary>처형·윈드밀 슬로모션 길이 (실제 초).</summary>
        public const float SlowMotionSeconds = 0.4f;
        /// <summary>슬로모션 앞뒤 부드럽게 들어가고 나오는 시간 (실제 초, GDD-beta B4).</summary>
        public const float SlowMotionRampSeconds = 0.08f;

        // ---------------- GDD 4.3 플레이어 ----------------
        /// <summary>카메라 눈높이 (m).</summary>
        public const float EyeHeight = 1.65f;
        /// <summary>최대 체력.</summary>
        public const int MaxHp = 3;
        /// <summary>집중선이 나타나기 시작하는 속도 배율.</summary>
        public const float SpeedLinesThreshold = 1.10f;
        /// <summary>최대 속도에서 넓어지는 FOV (도).</summary>
        public const float SpeedFovBonusMax = 5f;

        // ---------------- GDD 4.4 장애물 ----------------
        /// <summary>장애물 하나를 통과하는 데 걸리는 박자 수 (결과와 무관).</summary>
        public const int ObstacleBeats = 4;
        /// <summary>장애물 사이 최소 간격 (박자).</summary>
        public const int MinObstacleGapBeats = 4;
        /// <summary>PERFECT 벽타기로 달리는 거리 (m).</summary>
        public const float WallRunDistance = 3f;

        // ---------------- GDD 4.5 적과의 전투 ----------------
        /// <summary>일반 적(칼·창 포졸)의 공격 수 (마무리 제외).</summary>
        public const int NormalEnemyAttacks = 2;
        /// <summary>일반 적의 공격 간격 (박자).</summary>
        public const int NormalEnemyAttackIntervalBeats = 2;
        /// <summary>무사의 공격 수 (마무리 제외).</summary>
        public const int BossAttacks = 5;
        /// <summary>무사 공격 간격 최솟값 (박자).</summary>
        public const int BossAttackIntervalMinBeats = 1;
        /// <summary>무사 공격 간격 최댓값 (박자).</summary>
        public const int BossAttackIntervalMaxBeats = 2;

        // ---------------- GDD 4.6 포위 전투 ----------------
        /// <summary>포위 전투 적 수.</summary>
        public const int SurroundEnemyCount = 5;
        /// <summary>포위 전투 칼 든 적 수.</summary>
        public const int SurroundSwordCount = 3;
        /// <summary>포위 전투 창 든 적 수.</summary>
        public const int SurroundSpearCount = 2;
        /// <summary>포위 원 반지름 (m).</summary>
        public const float SurroundRadius = 4f;
        /// <summary>1단계 순차 공격 간격 (박자).</summary>
        public const float SurroundSequentialIntervalBeats = 1f;
        /// <summary>2단계 동시 공격 횟수.</summary>
        public const int SurroundSimultaneousCount = 3;
        /// <summary>3단계 윈드밀 화살표 수.</summary>
        public const int WindmillArrowCount = 4;
        /// <summary>3단계 윈드밀 화살표 간격 (박자).</summary>
        public const float WindmillIntervalBeats = 0.5f;
        /// <summary>공격하는 적 쪽으로 카메라가 도는 시간 (초).</summary>
        public const float SurroundCameraTurnSeconds = 0.2f;

        // ---------------- GDD 4.7 승리 / 패배 ----------------
        public const int ScorePerfect = 300;
        public const int ScoreNormal = 100;
        public const int ScoreFail = 0;
        /// <summary>랭크 S: PERFECT 비율 이상 (그리고 FAIL 0).</summary>
        public const float RankSRatio = 0.90f;
        /// <summary>랭크 A: PERFECT 비율 이상.</summary>
        public const float RankARatio = 0.75f;
        /// <summary>랭크 B: PERFECT 비율 이상.</summary>
        public const float RankBRatio = 0.50f;

        // ---------------- GDD 4.8 화면 ----------------
        /// <summary>콤보 표시를 시작하는 최소 콤보 수.</summary>
        public const int ComboShowMin = 2;
        public const int VolumeMin = 0;
        public const int VolumeMax = 100;
        public const int VolumeDefault = 80;

        // ---------------- GDD 4.9 맵 ----------------
        /// <summary>경로 전체 길이 목표 (m).</summary>
        public const float PathLengthTarget = 650f;
        public const float PathLengthMin = 600f;
        public const float PathLengthMax = 700f;
        /// <summary>경로 폭 (m).</summary>
        public const float PathWidth = 3f;
        public const float RoofHeightMin = 4f;
        public const float RoofHeightMax = 6f;
        public const float RoofGapMin = 3f;
        public const float RoofGapMax = 5f;
        /// <summary>스테이지 장애물 수.</summary>
        public const int StageObstacleCount = 20;
        /// <summary>스테이지 일반 적 수 (포위 전투 제외).</summary>
        public const int StageNormalEnemyCount = 4;

        // =====================================================================
        // beta1-1 (docs/GDD-beta.md). GDD-beta가 GDD보다 우선한다.
        // =====================================================================

        // ---------------- GDD-beta B1.1 그리는 순서 ----------------
        /// <summary>판정 원 Canvas sortingOrder (맨 위).</summary>
        public const int RingCanvasOrder = 20;
        /// <summary>판정 문구·화면 이펙트 Canvas sortingOrder.</summary>
        public const int JudgeFxCanvasOrder = 10;
        /// <summary>HUD Canvas sortingOrder.</summary>
        public const int HudCanvasOrder = 5;
        /// <summary>일시정지·결과 메뉴 Canvas sortingOrder. GDD-beta에 없음, Claude가 정함 (메뉴는 판정 원보다 위).</summary>
        public const int MenuCanvasOrder = 30;

        // ---------------- GDD-beta B1.2 판정 문구 ----------------
        /// <summary>판정 문구 위치: 원 위치에서 세로 오프셋 (px, 1080p 기준, 음수 = 아래).</summary>
        public const float JudgeLabelOffsetY = -110f;
        /// <summary>판정 문구 글자 크기 (pt).</summary>
        public const int JudgeLabelFontSize = 44;
        /// <summary>등장(커졌다 줄어듦) 시간 (초).</summary>
        public const float JudgeLabelPopSeconds = 0.1f;
        /// <summary>등장 시작 배율 (→ 1.0).</summary>
        public const float JudgeLabelPopScale = 1.3f;
        /// <summary>떠오르며 사라지는 시간 (초).</summary>
        public const float JudgeLabelRiseSeconds = 0.35f;
        /// <summary>떠오르는 거리 (px).</summary>
        public const float JudgeLabelRisePx = 20f;
        /// <summary>판정 문구 전체 표시 시간 (초) = 0.1 + 0.35.</summary>
        public const float JudgeLabelTotalSeconds = JudgeLabelPopSeconds + JudgeLabelRiseSeconds;
        /// <summary>새 판정 원이 문구 중심에서 이 거리(px) 안에 뜨면 문구를 흐리게.</summary>
        public const float JudgeLabelDimRadiusPx = 200f;
        /// <summary>흐리게 할 때 문구 알파 상한.</summary>
        public const float JudgeLabelDimAlpha = 0.3f;

        // ---------------- GDD-beta B1.3 판정 원 위치 ----------------
        /// <summary>안전 타원 가로 반지름 (화면 폭 비율).</summary>
        public const float RingSafeHalfWidthRatio = 0.22f;
        /// <summary>안전 타원 세로 반지름 (화면 높이 비율).</summary>
        public const float RingSafeHalfHeightRatio = 0.18f;
        /// <summary>등장 후 위치를 고정하기까지 시간 (초).</summary>
        public const float RingLockSeconds = 0.1f;
        /// <summary>방향 꺾쇠 크기 (px).</summary>
        public const float RingChevronSizePx = 40f;
        /// <summary>연속 원 가로 간격 (px).</summary>
        public const float RingChainSpacingPx = 160f;
        /// <summary>이 박자 수 미만 간격으로 이어지는 원은 나란히 둔다.</summary>
        public const float RingChainMaxGapBeats = 1f;
        /// <summary>원 바깥 빛 테두리 알파.</summary>
        public const float RingGlowAlpha = 0.5f;
        /// <summary>빛 테두리 지름 (px). GDD-beta에 없음, Claude가 정함 (목표 원 120 px의 1.6배).</summary>
        public const float RingGlowDiameterPx = 192f;

        // ---------------- GDD-beta B1.4 판정 화면 ----------------
        /// <summary>판정 중 노출 (URP Color Adjustments postExposure, EV).</summary>
        public const float JudgeExposure = -0.4f;
        /// <summary>판정 중 비네트 세기 (URP Vignette intensity).</summary>
        public const float JudgeVignette = 0.3f;

        // ---------------- GDD-beta B5 연출 시스템 (제안서 2~4장) ----------------
        /// <summary>연출 수치. 제안서 2장 표와 3장 등급표, 4장 겹침 규칙의 값.</summary>
        public static class Fx
        {
            // 4장 겹침 규칙
            /// <summary>기울기(롤) 합계 한도 (도).</summary>
            public const float RollLimit = 10f;
            /// <summary>고개(피치) 합계 한도 (도).</summary>
            public const float PitchLimit = 35f;
            /// <summary>최종 FOV 범위 (도).</summary>
            public const float FovMin = 45f;
            public const float FovMax = 85f;
            /// <summary>흔들림 세기 합계 한도.</summary>
            public const float TraumaMax = 1f;
            /// <summary>흔들림 세기가 초당 줄어드는 양.</summary>
            public const float TraumaDecayPerSecond = 1.5f;
            /// <summary>흔들림 세기 1일 때 최대 위치 흔들림 (m). 실제 = 세기² × 이 값.</summary>
            public const float ShakeMaxPosition = 0.08f;
            /// <summary>흔들림 세기 1일 때 최대 회전 흔들림 (도).</summary>
            public const float ShakeMaxRotation = 3f;
            /// <summary>연출이 끝나고 원래 카메라로 돌아오는 최대 시간 (초).</summary>
            public const float RecoverSeconds = 0.5f;
            /// <summary>사건·전투 슬로모션 빈도 제한 (박자): 직전 슬로모션 시작 후 이 박자 안이면 거절.</summary>
            public const float SlowMoMinGapBeats = 16f;

            // ---------- 화면 효과 (ScreenFx, 제안서 2장) ----------
            /// <summary>섬광 불투명도 상한 (제안서 1.3·4장).</summary>
            public const float FlashMaxAlpha = 0.6f;
            /// <summary>FAIL 피격 붉은 테두리: 주홍 비네트 0.45 → 0 (0.4초).</summary>
            public const float HitVignette = 0.45f;
            public const float HitVignetteSeconds = 0.4f;
            public static readonly UnityEngine.Color Vermilion = new UnityEngine.Color(0.85f, 0.22f, 0.12f);
            /// <summary>다음 판정 원이 뜨면 붉은 테두리를 이 배율로 줄인다 (절반).</summary>
            public const float HitVignetteOnNextRing = 0.5f;
            /// <summary>FAIL 피격: 채도 −80 순간 (0.15초에 회복), 렌즈 왜곡 −0.2 펄스 (0.15초).</summary>
            public const float HitSaturation = -80f;
            public const float HitSaturationSeconds = 0.15f;
            public const float HitLens = -0.2f;
            public const float HitLensSeconds = 0.15f;
            /// <summary>PERFECT 패링: 노출 +0.6 → 0 (0.1초), 색수차 0.4 → 0 (0.2초).</summary>
            public const float ParryExposure = 0.6f;
            public const float ParryExposureSeconds = 0.1f;
            public const float ParryChromatic = 0.4f;
            public const float ParryChromaticSeconds = 0.2f;
            /// <summary>윈드밀 마무리: 색수차 0.6 → 0, 흰 플래시 0.1초.</summary>
            public const float WindmillChromatic = 0.6f;
            public const float WindmillChromaticSeconds = 0.3f;
            public const float WhiteFlashSeconds = 0.1f;
            /// <summary>흰 플래시 노출 (EV). 제안서에 없음, Claude가 정함.</summary>
            public const float WhiteFlashExposure = 1.0f;
            /// <summary>처형 예고: 노출 −0.6, 비네트 0.45 (제안서 2장 E).</summary>
            public const float FinisherWarnExposure = -0.6f;
            public const float FinisherWarnVignette = 0.45f;
            /// <summary>Bloom 기본 세기·문턱 (HDR 불씨가 빛나게). 제안서에 없음, Claude가 정함.</summary>
            public const float BloomBase = 0.6f;
            public const float BloomThreshold = 1.0f;

            // ---------- VFX (VfxPool, 제안서 2장) ----------
            /// <summary>붓획 방사 섬광: 12~16획, 0.2초.</summary>
            public const int BrushStrokes = 14;
            public const float BrushBurstSeconds = 0.2f;
            /// <summary>먹 획: 화면 폭 80%, 0.12초.</summary>
            public const float InkSlashWidth = 0.8f;
            public const float InkSlashSeconds = 0.12f;
            /// <summary>불씨 스파크: PERFECT 30개, NORMAL 10개, 속도 6~12 m/s, 수명 0.25초.</summary>
            public const int EmbersPerfect = 30;
            public const int EmbersNormal = 10;
            public const float EmberSpeedMin = 6f;
            public const float EmberSpeedMax = 12f;
            public const float EmberLifetime = 0.25f;
            /// <summary>충격파 링: 반지름 0 → 6 m, 0.4초 (윈드밀).</summary>
            public const float ShockwaveRadius = 6f;
            public const float ShockwaveSeconds = 0.4f;
            /// <summary>흙먼지·파편 한 번에 나오는 개수. 제안서에 없음, Claude가 정함.</summary>
            public const int DustCount = 16;
            public const int DebrisCount = 10;
            /// <summary>종류별 미리 만들어 둘 개수 (재사용 풀).</summary>
            public const int PoolSize = 4;

            static FxPreset P(string name, int grade, float hitStop = 0, SlowMoMode slow = SlowMoMode.None, float trauma = 0, float hz = 20,
                float kick = 0, float side = 0, float height = 0, float pitch = 0, float roll = 0, float wobble = 0, float fov = 0,
                float rise = 0.03f, float hold = 0.05f, float release = 0.25f) =>
                new FxPreset { Name = name, Grade = grade, HitStop = hitStop, SlowMo = slow, Trauma = trauma, ShakeHz = hz, KickBack = kick, SideShift = side,
                    Height = height, Pitch = pitch, Roll = roll, YawWobble = wobble, Fov = fov, Rise = rise, Hold = hold, Release = release };

            // A. 적의 공격 예고 (등급 2): 휘두르기 직전 FOV −2°, 공격 쪽 롤 2°
            public static readonly FxPreset AttackWarn = P("AttackWarn", 2, fov: -2f, roll: 2f, rise: 0.1f, hold: 0.15f, release: 0.2f);
            // B. PERFECT 방어 (등급 3): Hit Stop 0.07, 흔들림 0.35 (25 Hz), 뒤로 0.05 m·위로 1.5°, FOV −3° 펀치 → 0.25초 복귀
            public static readonly FxPreset ParryPerfect = P("ParryPerfect", 3, hitStop: 0.07f, trauma: 0.35f, hz: 25f, kick: 0.05f, pitch: -1.5f, fov: -3f, rise: 0.02f, hold: 0.05f, release: 0.25f);
            // B. PERFECT 회피: Hit Stop 0.05, 피하는 쪽 0.1 m·롤 4°, 0.25초 복귀
            public static readonly FxPreset DodgePerfect = P("DodgePerfect", 3, hitStop: 0.05f, side: 0.1f, roll: 4f, rise: 0.05f, hold: 0.05f, release: 0.25f);
            // B. 무사 마지막 공격 PERFECT 패링 (슬로모션 후보)
            public static readonly FxPreset ParryPerfectFinale = P("ParryPerfectFinale", 3, hitStop: 0.07f, slow: SlowMoMode.Candidate, trauma: 0.35f, hz: 25f, kick: 0.05f, pitch: -1.5f, fov: -3f, rise: 0.02f, hold: 0.05f, release: 0.25f);
            // C. NORMAL 방어·회피 (등급 2): Hit Stop 0.03, 뒤로 0.12 m, yaw ±2° (12 Hz), 흔들림 0.2, 높이 −0.08 m, 0.35초
            public static readonly FxPreset DefendNormal = P("DefendNormal", 2, hitStop: 0.03f, trauma: 0.2f, hz: 12f, kick: 0.12f, height: -0.08f, wobble: 2f, rise: 0.03f, hold: 0.05f, release: 0.3f);
            // D. FAIL 피격 (등급 3): Hit Stop 0.05, 흔들림 0.6 (20 Hz), 맞은 쪽 롤 6°, 아래로 4°, FOV +4°, 0.45초
            public static readonly FxPreset Hit = P("Hit", 3, hitStop: 0.05f, trauma: 0.6f, hz: 20f, pitch: 4f, roll: 6f, fov: 4f, rise: 0.03f, hold: 0.07f, release: 0.35f);
            // E. 처형 (등급 4): Hit Stop 0.1 → 슬로모션, 적 쪽 0.3 m, FOV 58→50 (−8°), 롤 8°, 0.5초에 복귀
            public static readonly FxPreset Execution = P("Execution", 4, hitStop: 0.1f, slow: SlowMoMode.Always, trauma: 0.3f, hz: 15f, kick: -0.3f, roll: 8f, fov: -8f, rise: 0.08f, hold: 0.4f, release: 0.5f);
            // E. 발차기 처형: 아래로 10°
            public static readonly FxPreset ExecutionKick = P("ExecutionKick", 4, hitStop: 0.1f, slow: SlowMoMode.Always, trauma: 0.3f, hz: 15f, kick: -0.3f, pitch: 10f, fov: -8f, rise: 0.08f, hold: 0.4f, release: 0.5f);
            // F1. 포위 1단계 회전: 롤 3°, 도착 흔들림 0.1
            public static readonly FxPreset SurroundTurn = P("SurroundTurn", 3, trauma: 0.1f, roll: 3f, rise: 0.1f, hold: 0.1f, release: 0.2f);
            // F2. 포위 2단계 동시 공격: FOV +6°, 위로 8° (PERFECT면 슬로모션 후보)
            public static readonly FxPreset Simultaneous = P("Simultaneous", 3, fov: 6f, pitch: -8f, rise: 0.15f, hold: 0.3f, release: 0.3f);
            public static readonly FxPreset SimultaneousPerfect = P("SimultaneousPerfect", 3, hitStop: 0.05f, slow: SlowMoMode.Candidate, trauma: 0.25f, fov: 6f, pitch: -8f, rise: 0.05f, hold: 0.3f, release: 0.3f);
            // F3. 윈드밀 화살표 하나: FOV +2° (4개 쌓여 최대 +8°)
            public static readonly FxPreset WindmillArrow = P("WindmillArrow", 3, fov: 2f, rise: 0.05f, hold: 1.2f, release: 0.4f);
            // F3. 윈드밀 마무리 (등급 5): Hit Stop 0.08 → 슬로모션, 흔들림 0.6
            public static readonly FxPreset WindmillFinish = P("WindmillFinish", 5, hitStop: 0.08f, slow: SlowMoMode.Always, trauma: 0.6f, hz: 18f, rise: 0.05f, hold: 0.4f, release: 0.5f);
            // G. 도약 (PERFECT): 위로 6°, 공중 FOV +3°
            public static readonly FxPreset JumpPerfect = P("JumpPerfect", 2, pitch: -6f, fov: 3f, rise: 0.15f, hold: 0.2f, release: 0.3f);
            // G. 착지: PERFECT 흔들림 0.1·높이 −0.05, NORMAL 0.25·−0.15·롤 3°, FAIL 0.5
            public static readonly FxPreset LandPerfect = P("LandPerfect", 1, trauma: 0.1f, height: -0.05f, rise: 0.03f, hold: 0.02f, release: 0.2f);
            public static readonly FxPreset LandNormal = P("LandNormal", 1, trauma: 0.25f, height: -0.15f, roll: 3f, rise: 0.03f, hold: 0.05f, release: 0.3f);
            public static readonly FxPreset LandFail = P("LandFail", 2, trauma: 0.5f, height: -0.2f, rise: 0.03f, hold: 0.07f, release: 0.35f);
            // 사건 PERFECT 슬로모션 후보 (기와 미끄러짐, 밧줄 넘기, 긴 도약, 닫히는 대문, 돌진 적)
            public static readonly FxPreset EventPerfectSlow = P("EventPerfectSlow", 3, slow: SlowMoMode.Candidate, trauma: 0.15f, fov: 3f, rise: 0.05f, hold: 0.35f, release: 0.4f);

            public static readonly FxPreset[] All =
            {
                AttackWarn, ParryPerfect, DodgePerfect, ParryPerfectFinale, DefendNormal, Hit, Execution, ExecutionKick,
                SurroundTurn, Simultaneous, SimultaneousPerfect, WindmillArrow, WindmillFinish, JumpPerfect, LandPerfect, LandNormal, LandFail, EventPerfectSlow,
            };
        }

    }
}
