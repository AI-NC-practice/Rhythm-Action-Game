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
        /// <summary>PERFECT 판정 창 (± ms, 실제 시간). beta1-1: GDD-beta B8.4가 GDD 4.2의 60을 90으로 바꿨다.</summary>
        public const float PerfectWindowMs = 90f;
        /// <summary>NORMAL 판정 창 (± ms, 실제 시간). beta1-1: GDD-beta B8.4가 GDD 4.2의 150을 180으로, B12.1(사용자 요청)이 240으로 바꿨다.</summary>
        public const float NormalWindowMs = 240f;
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
        /// <summary>이 박자 수 미만 간격으로 이어지는 원은 나란히 둔다. beta1-1: GDD-beta B10.1이 1을 2(원이 떠 있는 2박)로 바꿨다.</summary>
        public const float RingChainMaxGapBeats = 2f;
        /// <summary>새 원이 떠 있는 원과 이 거리(px) 안이면 겹치지 않게 옆으로 옮긴다 (GDD-beta B10.1).</summary>
        public const float RingOverlapMinPx = 140f;
        /// <summary>다음에 눌러야 할 원 / 기다리는 원의 불투명도 (GDD-beta B10.1).</summary>
        public const float RingNextAlpha = 1f;
        public const float RingWaitingAlpha = 0.55f;
        /// <summary>원 바깥 빛 테두리 알파.</summary>
        public const float RingGlowAlpha = 0.5f;
        /// <summary>빛 테두리 지름 (px). GDD-beta에 없음, Claude가 정함 (목표 원 120 px의 1.6배).</summary>
        public const float RingGlowDiameterPx = 192f;

        // ---------------- GDD-beta B8.1 타이밍 정확도 ----------------
        /// <summary>화면 표시 지연 보정 (ms): 판정 원을 이만큼 앞당겨 그린다 (60 Hz 한 프레임). GDD-beta에 없음, Claude가 정함.</summary>
        public const float DisplayLatencyMs = 16f;
        /// <summary>입력 이벤트 시각을 믿는 최대 과거 (초). 이보다 오래된 시각은 잘라 쓴다. Claude가 정함.</summary>
        public const double InputMaxAgeSeconds = 0.1;

        // ---------------- GDD-beta B8.2 입력 반응 (Claude가 정함) ----------------
        /// <summary>키를 누르면 가장 이른 원이 이 배율로 커졌다 돌아옴.</summary>
        public const float InputPulseScale = 1.15f;
        public const float InputPulseSeconds = 0.12f;
        /// <summary>"빠름" 표시: 원 아래 110 px, 36 pt, 0.4초.</summary>
        public const int EarlyLabelFontSize = 36;
        public const float EarlyLabelSeconds = 0.4f;
        /// <summary>원이 없을 때 누른 키 표시: 화면 아래 가운데(아래에서 120 px), 48 pt, 0.3초.</summary>
        public const int KeyEchoFontSize = 48;
        public const float KeyEchoSeconds = 0.3f;
        public const float KeyEchoBottomPx = 120f;

        // ---------------- GDD-beta B8.3 판정 원 이펙트 (Claude가 정함, 0.35초 이하) ----------------
        /// <summary>PERFECT: 금빛 고리가 1 → 1.8배로 커지며 사라짐 + 붓획 조각, 0.3초.</summary>
        public const float RingFxPerfectSeconds = 0.3f;
        public const float RingFxPerfectScale = 1.8f;
        /// <summary>NORMAL: 청록 고리가 1 → 1.3배, 0.25초.</summary>
        public const float RingFxNormalSeconds = 0.25f;
        public const float RingFxNormalScale = 1.3f;
        /// <summary>FAIL: 붉은 원이 흔들리며(±8 px) 두 쪽으로 갈라짐(±14 px), 0.35초.</summary>
        public const float RingFxFailSeconds = 0.35f;
        public const float RingFxFailShakePx = 8f;
        public const float RingFxFailSplitPx = 14f;
        /// <summary>판정 원 이펙트 최대 길이 (GDD-beta B8.3).</summary>
        public const float RingFxMaxSeconds = 0.35f;

        // ---------------- GDD-beta B8.5 화려한 판정 원·이펙트 (Claude가 정함) ----------------
        /// <summary>다가오는 원의 가산 빛: 등장 때 세기 0.4 → 목표 시점 1.0.</summary>
        public const float RingChargeMin = 0.4f;
        /// <summary>원 둘레를 도는 빛 점 수·속도(도/초)·반지름(px).</summary>
        public const int RingOrbitDots = 8;
        public const float RingOrbitDegPerSec = 360f;
        public const float RingOrbitRadiusPx = 74f;
        /// <summary>PERFECT 십자·X 섬광 지름 (px, 700 이상).</summary>
        public const float FxFlareDiameterPx = 760f;
        /// <summary>방사형 번개선 지름 (px).</summary>
        public const float FxLightningDiameterPx = 420f;
        /// <summary>충격 고리가 퍼지는 배율.</summary>
        public const float FxShockScale = 3f;
        /// <summary>빛 입자 수 (PERFECT 18, NORMAL 8)와 처음 속도 (px/초).</summary>
        public const int FxSparksPerfect = 18;
        public const int FxSparksNormal = 8;
        public const float FxSparkSpeedPx = 900f;

        // ---------------- GDD-beta B9 실패 규칙과 되감기 ----------------
        /// <summary>되감기 연출 길이 (실제 초). 이 동안 박자 시계를 멈춘다.</summary>
        public const float RewindSeconds = 0.8f;
        /// <summary>결투·윈드밀 FAIL 뒤 되감기 전에 맞는 연출(피 튐)을 보여 주는 시간 (실제 초, GDD-beta B12.2).</summary>
        public const float FailHitSeconds = 0.7f;
        /// <summary>피 색 (피격 연출).</summary>
        public static readonly UnityEngine.Color BloodColor = new UnityEngine.Color(0.42f, 0.0f, 0.02f);
        /// <summary>피 방울 입자 색 (가산 입자라 밝은 붉은색).</summary>
        public static readonly UnityEngine.Color BloodDropColor = new UnityEngine.Color(0.9f, 0.05f, 0.05f);
        /// <summary>화면 핏자국 최대 불투명도 (섬광 상한 0.6과 별개, 빛이 아니라 가림막이므로).</summary>
        public const float BloodSplatterAlpha = 0.85f;
        /// <summary>피 방울 입자 수.</summary>
        public const int BloodDrops = 30;
        /// <summary>되감는 지점: 다시 할 첫 판정보다 이만큼 앞 (박, 1마디).</summary>
        public const double RewindLeadBeats = 4.0;
        /// <summary>되감기 화면: 채도 −90, 노출 −0.5.</summary>
        public const float RewindSaturation = -90f;
        public const float RewindExposure = -0.5f;

        // ---------------- GDD-beta B13 움직이며 싸우는 전투 ----------------
        /// <summary>결투 원 반지름 (m, 전투를 시작한 자리 중심).</summary>
        public const float CombatArenaRadius = 5f;
        /// <summary>싸움 거리 (m): 칼·무사, 창.</summary>
        public const float FightDistanceSword = 2.2f;
        public const float FightDistanceSpear = 2.6f;
        /// <summary>적이 서 있는 자리: 멈추는 자리에서 앞으로 (m). 여기서 싸움 거리까지 돌진한다 (B13.2).</summary>
        public const float EnemySpawnMeters = 5f;
        /// <summary>판정 뒤 이동 시간 (초).</summary>
        public const float CombatMoveSeconds = 0.35f;
        /// <summary>플레이어가 적을 바라보도록 도는 부드러움 (초)과 최대 속도 (도/초).</summary>
        public const float CombatFaceSmoothSeconds = 0.25f;
        public const float CombatFaceMaxDegPerSec = 240f;
        /// <summary>원이 없는 틈에 적이 둘레를 도는 속도 (도/초).</summary>
        public const float CombatCircleDegPerSec = 20f;
        /// <summary>전투가 끝난 뒤 경로 위 자리로 돌아오는 시간 (초).</summary>
        public const float CombatReturnSeconds = 0.4f;
        /// <summary>포위 전투 (B13.3): 적들이 둘레를 도는 속도 (도/초), 뛰어드는 거리 (플레이어에서 m), 플레이어가 움직일 수 있는 반지름 (m).</summary>
        public const float SurroundCircleDegPerSec = 30f;
        public const float SurroundLungeMeters = 1.5f;
        public const float SurroundPlayerRadius = 1.2f;
        /// <summary>판정별 이동 (m, B13.1 표).</summary>
        public const float MoveParryPerfect = 0.5f, MoveParryNormal = 0.5f, MoveHitBack = 0.4f;
        public const float MoveDodgePerfect = 1.2f, MoveDodgePerfectEnemy = 0.6f, MoveDodgeNormal = 0.5f, MoveDodgeNormalEnemy = 0.2f;
        public const float MoveDuckForward = 0.5f, MoveDuckSide = 0.6f;
        public const float MoveStrikePerfect = 1f, MoveStrikeNormal = 0.3f;

        // ---------------- GDD-beta B11 무사 결투 ----------------
        /// <summary>자세 게이지 가득 (B11.3).</summary>
        public const float PostureFull = 100f;
        /// <summary>NORMAL이 PERFECT에 비해 채우는 비율 (+50/N ÷ +100/N).</summary>
        public const float PostureNormalShare = 0.5f;
        /// <summary>자세 게이지 크기 (px, 화면 위 가운데), 위쪽 여백 (px). GDD-beta에 없음, Claude가 정함.</summary>
        public const float PostureBarWidthPx = 520f;
        public const float PostureBarHeightPx = 16f;
        public const float PostureBarTopPx = 70f;
        /// <summary>"자세 무너짐" 붉은 번쩍임 길이 (초). GDD-beta에 없음, Claude가 정함.</summary>
        public const float PostureBreakFlashSeconds = 0.6f;
        /// <summary>내 공격 원 빛과 칼 아이콘 색 (주홍, B11.2). 막기 원은 지금처럼 청록 (JudgeRingView.GlowColor).</summary>
        public static readonly UnityEngine.Color StrikeRingColor = new UnityEngine.Color(1f, 0.38f, 0.12f);
        /// <summary>칼 아이콘 크기 (px)와 원 중심에서의 위치 (오른쪽 위).</summary>
        public const float StrikeIconPx = 52f;
        public static readonly UnityEngine.Vector2 StrikeIconOffsetPx = new UnityEngine.Vector2(58f, 58f);
        /// <summary>내 공격 PERFECT에 무사가 밀리는 거리 (m), NORMAL에 내가 밀리는 거리 (m, 카메라 뒤로). Claude가 정함.</summary>
        public const float StrikeBossPushMeters = 0.35f;
        public const float StrikeSelfPushMeters = 0.15f;
        /// <summary>밀림이 돌아오는 시간 (초).</summary>
        public const float StrikePushSeconds = 0.35f;
        /// <summary>긴 묶음 자리 돌려쓰기 최대 수 (B11.6).</summary>
        public const int ChainSlotsMax = 5;

        // ---- B11.5 결투 1인칭 가상 카메라 (B27) ----
        /// <summary>막기 카메라: 머리 기준 높이 (m), FOV (도).</summary>
        public const float DuelGuardCamHeight = -0.1f;
        public const float DuelGuardCamFov = 62f;
        /// <summary>공격 카메라: 앞으로 (m), FOV (도), 베는 쪽 롤 (도).</summary>
        public const float DuelStrikeCamForward = 0.4f;
        public const float DuelStrikeCamFov = 54f;
        public const float DuelStrikeCamRoll = 6f;
        /// <summary>두 카메라 사이 블렌드 (초, Ease In-Out).</summary>
        public const float DuelCamBlendSeconds = 0.3f;
        /// <summary>칼이 부딪칠 때 FOV 줌 펀치 (도)와 길이 (초).</summary>
        public const float DuelClashPunchFov = -6f;
        public const float DuelClashPunchSeconds = 0.15f;

        // ---- B11.5 결투 3인칭 컷 (B28) ----
        /// <summary>1인칭 ↔ 3인칭 블렌드 (초, D 진입만 바로 자름).</summary>
        public const float DuelCutBlendSeconds = 0.4f;
        /// <summary>다음 판정 원이 나타나기 이만큼 전에는 1인칭으로 돌아와 있어야 함 (초).</summary>
        public const float DuelCutReturnMarginSeconds = 0.4f;
        /// <summary>3인칭 카메라가 바라보는 높이 (두 사람 중간점 위, m).</summary>
        public const float DuelCutLookHeight = 1.1f;
        /// <summary>
        /// 컷별 반지름 시작·끝 (m), 카메라 높이 (m), 도는 각도 (도), 바라보는 높이 (m). GDD-beta B13.4가 B11.5를 바꿈:
        /// A 등장 = 로우 앵글(낮은 카메라가 위를 올려다봄), B 칼 겨루기 = 맞물린 칼 옆 1.2 m 클로즈업, D 처형 = 낮은 각도 180° (+ 마지막 0.5초 위에서),
        /// K 옆차기 = 측면 짧은 컷, S 자리 바꾸기 = 어깨 너머 (자리는 DuelCamera가 따로 정함).
        /// </summary>
        public static readonly (string name, float r0, float r1, float height, float sweep, float look)[] DuelCuts =
        {
            ("A", 4.5f, 4.0f, 0.4f, 40f, 1.8f),
            ("B", 1.2f, 1.2f, 1.45f, 20f, 1.45f),
            ("D", 4.0f, 4.0f, 0.5f, 180f, 1.1f),
            ("K", 3.5f, 3.5f, 1.2f, 0f, 0.9f),
            ("S", 1.0f, 1.0f, 1.7f, 0f, 1.2f),
        };
        /// <summary>처형 컷 D 마지막 위에서 내려다보는 시간 (초)과 높이 (m).</summary>
        public const float DuelTopDownSeconds = 0.5f;
        public const float DuelTopDownHeight = 7f;
        /// <summary>옆차기 컷 K·자리 바꾸기 컷 S 길이 (실제 초, 바로 자름), 그동안 무사 히트 스톱 (초).</summary>
        public const float DuelKickCutSeconds = 0.25f;
        public const float DuelSwapCutSeconds = 0.6f;
        /// <summary>자리 바꾸기 이동 시간 (초).</summary>
        public const float DuelSwapSeconds = 0.5f;
        /// <summary>다운: 카메라 눈높이 (바닥에서 m), 올려다보는 각도 (도), 넘어지는 시간 (초).</summary>
        public const float DuelDownEyeHeight = 0.3f;
        public const float DuelDownPitch = 25f;   // 35°는 무사가 화면 아래로 빠져 25°로 (B34 화면 확인)
        public const float DuelDownFallSeconds = 0.25f;



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
            // G. 파쿠르 FAIL (착지·충돌): 흔들림 0.5 + 붉은 테두리 0.3
            public static readonly FxPreset ParkourFail = P("ParkourFail", 2, trauma: 0.5f, hz: 18f, height: -0.2f, rise: 0.03f, hold: 0.07f, release: 0.35f);
            public const float ParkourFailVignette = 0.3f;
            // B9.4 되감기: 카메라가 뒤로 0.3 m 물러났다 돌아옴 (0.8초)
            public static readonly FxPreset Rewind = P("Rewind", 4, kick: 0.3f, pitch: -3f, rise: 0.2f, hold: 0.3f, release: 0.3f);
            // B10.2 대치: 카메라가 적 쪽으로 살짝 다가감 (FOV −4°), 첫 원이 뜨는 2박(1초) 동안
            public static readonly FxPreset Standoff = P("Standoff", 2, fov: -4f, rise: 0.2f, hold: 0.8f, release: 0.3f);
            // B11.2 내 공격 PERFECT: 칼끼리 맞부딪쳐 무사가 밀림 (패링과 같은 충돌 연출)
            public static readonly FxPreset StrikePerfect = P("StrikePerfect", 3, hitStop: 0.07f, trauma: 0.35f, hz: 25f, kick: -0.05f, pitch: -1.5f, fov: -3f, rise: 0.02f, hold: 0.05f, release: 0.25f);
            // B11.2 내 공격 NORMAL: 부딪쳤지만 내가 밀림 (뒤로 StrikeSelfPushMeters)
            public static readonly FxPreset StrikeNormal = P("StrikeNormal", 2, hitStop: 0.03f, trauma: 0.2f, hz: 12f, kick: StrikeSelfPushMeters, height: -0.05f, rise: 0.03f, hold: 0.05f, release: 0.3f);
            // B11.3 자세 무너짐: 흔들림 + 붉은 번쩍임
            public static readonly FxPreset PostureBreak = P("PostureBreak", 3, hitStop: 0.08f, trauma: 0.4f, hz: 20f, fov: -4f, rise: 0.03f, hold: 0.1f, release: 0.4f);
            // B12.2 결투·윈드밀 FAIL: 크게 맞음 (뒤로 0.25 m, 고개 젖힘, 강한 흔들림) + 피 튐, 0.7초 뒤 되감기
            public static readonly FxPreset BloodHit = P("BloodHit", 4, hitStop: 0.1f, trauma: 0.7f, hz: 20f, kick: 0.25f, pitch: -6f, roll: 8f, rise: 0.03f, hold: 0.25f, release: 0.4f);
            // B13.4 옆차기 다운: 판정 없이 맞음 (실패 아님) — 뒤로 크게 밀리며 고개가 젖혀지고 기움, 피는 없음
            public static readonly FxPreset KickHit = P("KickHit", 4, hitStop: 0.12f, trauma: 0.6f, hz: 18f, kick: 0.4f, pitch: -10f, roll: -12f, rise: 0.03f, hold: 0.2f, release: 0.4f);
            /// <summary>대치 때 적이 다가오는 거리 (m, GDD-beta B10.2).</summary>
            public const float StandoffApproachMeters = 0.5f;
            /// <summary>적 공격 예고: 목표 박자 몇 박 전에 (휘두르기 직전, 제안서 2장 A).</summary>
            public const float AttackWarnLeadBeats = 0.5f;

            // ---------- 효과음·반주 (GDD-beta B7, 제안서 2장 소리 열) ----------
            /// <summary>효과음 AudioSource 풀 크기 (제안서 5장: 8개).</summary>
            public const int SfxPoolSize = 8;
            /// <summary>같은 종류 효과음 동시 재생 수 상한 (제안서 4장).</summary>
            public const int SfxSameKindMax = 3;
            /// <summary>음악 덕킹 (dB, 초): PERFECT 패링 −4 dB 0.15초, 피격 −8 dB 0.3초, 처형·윈드밀 −6 dB 0.5초.</summary>
            public const float DuckParryDb = -4f;
            public const float DuckParrySeconds = 0.15f;
            public const float DuckHitDb = -8f;
            public const float DuckHitSeconds = 0.3f;
            public const float DuckExecutionDb = -6f;
            public const float DuckExecutionSeconds = 0.5f;
            /// <summary>반주 타격을 얼마나 미리 예약할지 (초). 메트로놈과 같음.</summary>
            public const double BackingLookAheadSeconds = 0.1;
            /// <summary>
            /// 장단 반주 한 마디 (박, 소리). 덩(궁+채) − 쿵 − 덕 − 덩 − 쿵 − 덕. GDD-beta에 없음, Claude가 정함 (굿거리 느낌을 4/4로 줄임).
            /// </summary>
            public static readonly float[] BackingOffsets = { 0f, 1f, 1.5f, 2f, 3f, 3.5f };
            public static readonly SfxKind[][] BackingHits =
            {
                new[] { SfxKind.JangguKung, SfxKind.JangguDeok },
                new[] { SfxKind.JangguKung },
                new[] { SfxKind.JangguDeok },
                new[] { SfxKind.JangguKung, SfxKind.JangguDeok },
                new[] { SfxKind.JangguKung },
                new[] { SfxKind.JangguDeok },
            };
            /// <summary>반주 타격별 음량 (마디 첫 박 강세).</summary>
            public static readonly float[] BackingVolumes = { 1f, 0.7f, 0.6f, 0.8f, 0.7f, 0.6f };


            public static readonly FxPreset[] All =
            {
                AttackWarn, ParryPerfect, DodgePerfect, ParryPerfectFinale, DefendNormal, Hit, Execution, ExecutionKick,
                SurroundTurn, Simultaneous, SimultaneousPerfect, WindmillArrow, WindmillFinish, JumpPerfect, LandPerfect, LandNormal, LandFail, EventPerfectSlow, ParkourFail, Rewind, Standoff, StrikePerfect, StrikeNormal, PostureBreak, BloodHit, KickHit,
            };
        }

        // ---------------- GDD-beta B14 히트사운드 ----------------
        /// <summary>방향키를 누르는 순간 나는 장구 소리 (GDD-beta B14).</summary>
        public static class HitSound
        {
            /// <summary>입력 결과별 음량: PERFECT 덩, NORMAL 딱, FAIL 턱, 원 없음·너무 이름 틱.</summary>
            public const float PerfectVolume = 1.0f;
            public const float NormalVolume = 0.7f;
            public const float FailVolume = 0.5f;
            public const float EmptyVolume = 0.2f;
            /// <summary>히트사운드 전용 AudioSource 수 (효과음 풀과 별도).</summary>
            public const int Voices = 4;
            /// <summary>이른 PERFECT 입력의 히트사운드를 목표 박까지 기다려 울리는 최대 시간 (초, GDD-beta B15.3) = PERFECT 창.</summary>
            public const float SnapMaxWaitSeconds = PerfectWindowMs / 1000f;
            /// <summary>오디오 DSP 버퍼 (샘플). 1024 → 512로 줄여 출력 지연을 약 21 ms → 11 ms(48 kHz)로.</summary>
            public const int DspBufferSize = 512;
        }

        // ---------------- B10 배경 음악 (GDD 1장 사운드: 국악 퓨전, BPM 고정, 보컬 없음) ----------------
        /// <summary>
        /// 배경 음악. 공유마당 "국악 BGM #197"(주식회사 아이티앤, CC BY)을 harness/scripts/music/make-loop.py로
        /// 재생 속도 0.967배(펄스 186.07 → 180/분, 3펄스 = 2박)로 늦춰 첫 강박을 0초에 둔 64초(128박) 루프.
        /// </summary>
        public static class Music
        {
            /// <summary>음악 파일 (Assets 기준 경로).</summary>
            public const string ClipPath = "Assets/Audio/Music/gugak_bgm197_loop120.wav";
            /// <summary>루프 길이 (박). 음악 시각 = (박 × 0.5초) mod 64초.</summary>
            public const int LoopBeats = 128;
            /// <summary>음악 첫 박의 파일 안 시각 (초). 루프 파일은 첫 강박에서 시작한다.</summary>
            public const float FirstBeatSeconds = 0f;
            /// <summary>음악 기준 음량. GDD에 없음, Claude가 정함.</summary>
            public const float Volume = 0.8f;
            /// <summary>곡이 있으면 합성 장단 반주(GDD-beta B7)는 끈다. GDD에 없음, Claude가 정함 (곡이 3박 느낌이라 4박 반주와 겹치면 어지러움).</summary>
            public const bool KeepJangguBacking = false;
            /// <summary>음 높이 보정 피치 시프터의 창 길이와 최소 지연 (초, GDD-beta B15.1). 평균 지연 = 최소 + 창/2 = 22 ms.</summary>
            public const float PitchShiftWindowSeconds = 0.04f;
            public const float PitchShiftMinDelaySeconds = 0.002f;
            /// <summary>게임 안 출처 표기 (CC BY).</summary>
            public const string Credit = "음악: 국악 BGM #197 — 주식회사 아이티앤, 공유마당(한국저작권위원회), CC BY";
        }
    }
}
