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
        public const int NormalEnemyAttacks = 4;   // GDD-beta B18.3: 연속기 4회 (GDD 4.5의 2회에서 늘림)
        /// <summary>일반 적의 공격 간격 (박자).</summary>
        public const int NormalEnemyAttackIntervalBeats = 2;
        /// <summary>창 든 포졸의 첫 공격 간격 (박, GDD-beta B16.1). B18.3 연속기의 첫 간격.</summary>
        public const float SpearAttackIntervalBeats = 1.5f;
        /// <summary>일반 적 연속기 공격 간격 (박, GDD-beta B18.3). 칼 0·1·2·3 → 마무리 5, 창 0·1.5·2.5·4 → 마무리 6.</summary>
        public static readonly float[] SwordComboGaps = { 1f, 1f, 1f };
        public static readonly float[] SpearComboGaps = { SpearAttackIntervalBeats, 1f, 1.5f };
        /// <summary>마지막 공격 뒤 마무리까지 (박).</summary>
        public const float FinisherGapBeats = 2f;
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
        /// <summary>작업 R07 후속: 지붕 위 전투에서 길 가운데로부터 옆으로 가는 최대 거리 (m)와 지붕 끝 여유 (m). 지붕 밖 공중으로 나가지 않게.</summary>
        public const float CombatRoofMaxLateral = 2.5f;
        public const float CombatRoofEdgeMargin = 0.8f;
        /// <summary>작업 R07 후속: 지붕 위 전투에서 가운데 면 높이와 이만큼(m) 넘게 차이 나는 비탈·틈으로는 옆으로 가지 않는다.</summary>
        public const float CombatRoofSlopeDrop = 0.6f;
        /// <summary>작업 R07 후속: 지붕 위 전투 자리 높이 격자 크기 (칸 수 × 0.5 m, 적 자리 중심).</summary>
        public const int CombatArenaGridCells = 44;
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
            /// <summary>칼이 머리 위를 스쳐 감 (GDD-beta B17.4 2차): 몸을 숙이고 위를 올려다봄. 회피 슬로모션 동안 (실제 시간으로 흐름).</summary>
            public static readonly FxPreset NearMiss = P("NearMiss", 3, height: -0.35f, pitch: -14f, roll: 5f, fov: -6f, rise: 0.08f, hold: 0.45f, release: 0.25f);
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
            /// <summary>실제 장구·꽹과리 녹음으로 만든 히트사운드 폴더 (GDD-beta B17.1, make-hitsounds.py). 파일 이름 Hit_Perfect/Normal/Fail/Empty.wav.</summary>
            public const string RecordedDir = "Assets/Audio/Hit";
            /// <summary>누르는 순간 음악을 줄이는 양과 길이 (GDD-beta B17.1): 히트사운드가 튀어나오게.</summary>
            public const float DuckDb = -4.5f;   // GDD-beta B18.1 (B17.1의 −3에서 키움)
            public const float DuckSeconds = 0.08f;
            /// <summary>이른 PERFECT 입력의 히트사운드를 목표 박까지 기다려 울리는 최대 시간 (초, GDD-beta B15.3) = PERFECT 창.</summary>
            public const float SnapMaxWaitSeconds = PerfectWindowMs / 1000f;
            /// <summary>오디오 DSP 버퍼 (샘플). 1024 → 512로 줄여 출력 지연을 약 21 ms → 11 ms(48 kHz)로.</summary>
            public const int DspBufferSize = 512;
        }

        // ---------------- GDD-beta B18.2 달리기 장단 노드 ----------------
        /// <summary>
        /// 달리기 장단 노드: Q = 궁편("덩"), W = 채편("덕") (GDD-beta B18.5). 챕터(1~6)마다 4마디 장단 표를 돌려 쓴다.
        /// 표 한 칸 = "마디 안 박위치+손" (L = Q 궁편, R = W 채편). 뒤 챕터일수록 반 박이 많다. GDD에 없음, 사용자 결정(약 3배)에 맞춰 Claude가 정함.
        /// </summary>
        public static class Jangdan
        {
            public static readonly string[][] Bars =
            {
                new[] { "0L 2L 3R", "0L 1R 2L 3R", "0L 2L 2.5R 3R", "0L 1R 2L 3R" },
                new[] { "0L 1R 2L 3R", "0L 1R 1.5R 2L 3R", "0L 1R 2L 2.5R 3R", "0L 0.5R 1R 2L 3R" },
                new[] { "0L 0.5R 1R 2L 2.5R 3R", "0L 1R 1.5R 2L 3R 3.5R", "0L 0.5R 1R 1.5R 2L 3R", "0L 1R 2L 2.5R 3R 3.5R" },
                new[] { "0L 1R 2L 3R", "0L 1R 2L 3R", "0L 1R 2L 3R", "0L 1R 2L 3R" },
                new[] { "0L 0.5R 1R 1.5L 2R 2.5R 3L", "0L 1R 1.5R 2L 3R 3.5R", "0L 0.5R 1.5L 2R 3L 3.5R", "0L 1R 2L 2.5R 3R 3.5R" },
                new[] { "0L 1R 2L 3R", "0L 1R 2L 3R", "0L 1R 2L 3R", "0L 1R 2L 3R" },
            };
            /// <summary>장단 노드끼리의 최소 간격 (박).</summary>
            public const double MinGapBeats = 0.5;
            /// <summary>사건·전투 판정과의 최소 간격 (박, GDD-beta B18.5: 장애물과 장구가 거의 같은 박에 나오지 않게).</summary>
            public const double EventClearBeats = 1.0;
            /// <summary>멈추는 전투(대치·포위·무사) 자리 앞뒤로 비우는 박.</summary>
            public const double CombatClearBeats = 2;
            /// <summary>챕터 시작 뒤 비우는 박 (첫 판정은 챕터 시작 + 2박 이후, B04).</summary>
            public const double ChapterLeadBeats = 2;
            /// <summary>슬로모션 후보 판정 뒤 비우는 박 (B04).</summary>
            public const double SlowMoClearBeats = 2;
            /// <summary>장단 노드를 몇 박 앞까지 예약하는지 (레인에 미리 보이게, 다음 멈추는 전투 앞까지만).</summary>
            public const double ScheduleAheadBeats = 5;
            /// <summary>장구 레인 (1080p 기준 px): 화면 아래 가로 레인, 노드가 1박에 LanePxPerBeat씩 왼쪽 판정 표시로 흘러옴.</summary>
            public const float LaneWidthPx = 1100f;
            public const float LaneHeightPx = 96f;
            public const float LaneBottomPx = 40f;
            public const float HitMarkerInsetPx = 110f;
            public const float LanePxPerBeat = 220f;
            public const double LaneLookAheadBeats = 4;
            public const float NoteSizePx = 70f;
            public static readonly UnityEngine.Color KungColor = new UnityEngine.Color(0.25f, 0.55f, 0.95f, 1f);   // Q 궁편
            public static readonly UnityEngine.Color DeokColor = new UnityEngine.Color(0.95f, 0.3f, 0.25f, 1f);    // W 채편
        }

        // ---------------- B45 화면이 박에 맞춰 숨 쉼 (GDD-beta B17.2) ----------------
        /// <summary>
        /// 박 펄스·예고음·걸음 출렁임 (GDD-beta B17.2, 제안서 rhythm-feel 2.1-2·2.2-4·5). 모든 값은 GDD에 없음, Claude가 정함.
        /// 박 펄스 세기 = 마디 첫 박이면 BarAccent, 아니면 1 × (1 − 박 안 위치/PulseBeats)² (박 순간 가장 크고 빠르게 줄어듦).
        /// </summary>
        public static class BeatFx
        {
            /// <summary>펄스가 이어지는 길이 (박). 0.35박 = 0.175초(1.00배).</summary>
            public const float PulseBeats = 0.35f;
            /// <summary>마디 첫 박의 세기 배율 (보통 박 = 1).</summary>
            public const float BarAccent = 1.8f;
            /// <summary>보통 박 세기 1일 때 판정 원 테두리(목표 원) 크기 증가 (0.08 = 8%).</summary>
            public const float RingScale = 0.08f;
            /// <summary>보통 박 세기 1일 때 HUD(하트·속도·점수·콤보) 크기 증가.</summary>
            public const float HudScale = 0.05f;
            /// <summary>보통 박 세기 1일 때 장구 레인 판정 표시 크기 증가.</summary>
            public const float LaneScale = 0.12f;
            /// <summary>보통 박 세기 1일 때 화면 가장자리(비네트) 세기. 다른 화면 효과와 항목별 최댓값으로 합친다.</summary>
            public const float Vignette = 0.12f;

            /// <summary>예고음: 판정 원의 목표 박 1박 전·2박 전에 "틱" (판정 원이 떠 있는 동안 남은 박마다, ApproachBeats = 2).</summary>
            public const int TickCount = 2;
            /// <summary>예고음 음량 (작게). 마지막(1박 전) 틱은 TickLastBoost 배 높은 음.</summary>
            public const float TickVolume = 0.35f;
            /// <summary>예고음 주파수 (Hz)와 길이 (초). 1박 전 틱은 한 음 높여 "곧"을 알린다.</summary>
            public const float TickFrequency = 2200f;
            public const float TickLastFrequency = 2900f;
            public const float TickSeconds = 0.03f;
            /// <summary>다른 판정의 목표 박과 이만큼(박) 안이면 예고음을 내지 않는다 (히트사운드와 겹치지 않게).</summary>
            public const float TickClearBeats = 0.25f;
            /// <summary>예고음을 이만큼(초) 미리 예약한다 (메트로놈과 같음).</summary>
            public const double TickLookAheadSeconds = 0.1;

            /// <summary>걸음: 1박 = 한 걸음. 발이 닿는 박 순간 카메라가 가장 낮다 (m).</summary>
            public const float StepDropMeters = 0.05f;
            /// <summary>걸음마다 좌우로 옮기는 폭 (m, 왼발·오른발 번갈아).</summary>
            public const float StepSwayMeters = 0.025f;
            /// <summary>걸음마다 기우는 각 (도).</summary>
            public const float StepRollDegrees = 0.6f;
            /// <summary>멈춰 있을 때(전투) 박마다 살짝 출렁이는 높이 (m, 박 펄스 세기 × 이 값).</summary>
            public const float IdleDipMeters = 0.015f;
            /// <summary>달리기 ↔ 멈춤 전환을 부드럽게 (초당 변화량).</summary>
            public const float RunBlendPerSecond = 4f;
        }

        // ---------------- B47·B52 아슬아슬 회피 슬로모션 (GDD-beta B17.4 2차) ----------------
        /// <summary>
        /// 회피 슬로모션 (GDD-beta B17.4 2차, 2026-11-16 사용자 승인). 노드가 다가오는 동안에는 느려지지 않는다.
        /// 회피 판정을 PERFECT·NORMAL로 피한 직후 박자 시계·음악·화면이 함께 느려져 "간신히 피하는" 장면을 보여 주고 돌아온다.
        /// 곡도 같은 배율로 느려지므로 곡과 판정 박의 관계는 그대로다. 모든 값은 GDD에 없음, Claude가 정함.
        /// </summary>
        public static class BulletTime
        {
            /// <summary>느려지는 배율 (박자 시계·음악·Time.timeScale).</summary>
            public const float Factor = 0.3f;
            /// <summary>피한 직후 느려지는 데 걸리는 시간 (실제 초).</summary>
            public const float InSeconds = 0.06f;
            /// <summary>느린 채로 머무는 시간 (실제 초, 들어가는 시간 포함).</summary>
            public const float HoldSeconds = 0.6f;
            /// <summary>원래 속도로 돌아오는 시간 (실제 초).</summary>
            public const float ReleaseSeconds = 0.15f;
            /// <summary>다음 판정 목표 이만큼(박) 전에는 돌아오기 시작한다 (노드는 누르는 느낌 그대로).</summary>
            public const float ClearBeats = 0.6f;
            /// <summary>먹먹함: 저역 통과 주파수 (Hz) — 평소 / 가장 먹먹할 때.</summary>
            public const float LowPassOpenHz = 22000f;
            public const float LowPassMuffledHz = 700f;
            /// <summary>가장 먹먹할 때 음악 음량 배율.</summary>
            public const float MusicGain = 0.6f;
            /// <summary>슬로 동안 화면 채도 (회색, ColorAdjustments.saturation).</summary>
            public const float Saturation = -70f;
            /// <summary>회피 슬로모션 판정: 무사 결투 받아치기, 3단계 난타 마지막 무사 공격 (O 기준 박).</summary>
            public static readonly double[] BossBeats = { 37.5, 66 };
            /// <summary>대문 컷: 카메라를 문 건너편 앞 이 거리(m)·높이(m)에 두고, 문 쪽 이 높이(m)를 본다.</summary>
            public const float GateCamAheadMeters = 4.0f;
            public const float GateCamHeight = 1.3f;
            public const float GateCamSideMeters = 1.2f;
            public const float GateLookHeight = 0.8f;
            /// <summary>
            /// B4 2·3번(사건·전투 PERFECT 뒤 0.4초 슬로모션)을 쓰는지. GDD-beta B17.4로 없앰(false).
            /// 처형·윈드밀 마무리(SlowMoMode.Always)는 그대로.
            /// </summary>
            public const bool PerfectCandidateSlowMo = false;
        }

        // ---------------- B48 전투 소리 실제 녹음 (GDD-beta B17.5) ----------------
        /// <summary>전투 소리를 박에 맞춤 (GDD-beta B17.5): 적 공격은 판정 반 박 전에 칼 휘두름 "쉭", 판정 박에 "챙"/"퍽". 음량은 GDD에 없음, Claude가 정함.</summary>
        public static class CombatSound
        {
            /// <summary>적 칼 휘두름 "쉭"을 판정 목표 몇 박 전에 내는지.</summary>
            public const double SwingLeadBeats = 0.5;
            /// <summary>휘두름 음량.</summary>
            public const float SwingVolume = 0.8f;
            /// <summary>휘두름을 이만큼(초) 미리 예약한다.</summary>
            public const double LookAheadSeconds = 0.1;
        }

        // ---------------- B10 배경 음악 (GDD 1장 사운드: 국악 퓨전, BPM 고정, 보컬 없음) ----------------
        /// <summary>
        /// 배경 음악 "스테이지 1" — 퓨전 국악 액션, 120 BPM, 101마디(404박, 3분 22초). harness/scripts/music/make-song.py가
        /// 판정 타임라인(harness/music/stage1_timeline.json, GDD-beta B16.2)을 따라 공유마당 국악기 음원(태평소·꽹과리·장구)과
        /// 코드로 만든 드럼·베이스·현악을 조립했다: 판정 박마다 장구·꽹과리 강세, 전투는 절정, 전투 앞은 빌드업 (docs/CREDITS.md).
        /// 저장소 용량 때문에 mp3로 넣었고(wav-to-mp3.ps1), mp3 변환이 앞에 둔 지연만큼 첫 박이 뒤로 밀려 있다.
        /// </summary>
        public static class Music
        {
            /// <summary>음악 파일 (Assets 기준 경로).</summary>
            public const string ClipPath = "Assets/Audio/Music/stage1_song.mp3";
            /// <summary>곡 길이 (박). 음악 시각 = 첫 박 + (곡 박 × 0.5초) mod 202초. 클리어(390박) 뒤 마무리 3마디, 더 길어지면 처음으로.</summary>
            public const int LoopBeats = 404;
            /// <summary>판정 타임라인 (모두 PERFECT 진행의 판정 시계 박자, 오토플레이로 뽑음, B16.2).</summary>
            public const string TimelinePath = "harness/music/stage1_timeline.json";
            /// <summary>
            /// 챕터 1~6이 시작되는 곡 박 (판정 타임라인의 챕터 시작 시계 박자, GDD-beta B16.3). 챕터 되감기·챕터 이동은 시계가 0박부터
            /// 다시 시작하므로 곡은 이 위치부터 재생한다.
            /// </summary>
            public static readonly int[] ChapterSongBeats = { 0, 56, 116, 204, 240, 308 };   // B18: 연속기로 전투가 길어져 다시 잼
            /// <summary>
            /// 음악 첫 박의 파일 안 시각 (초) = mp3 인코더 지연이 Unity가 가져온 클립에 남긴 만큼(25 ms).
            /// 테스트 B10_Downbeat가 실제 가져온 클립에서 잰다 (Windows 디코더로는 14 ms였지만 Unity 디코더는 25 ms).
            /// </summary>
            public const float FirstBeatSeconds = 0.025f;
            /// <summary>음악이 박자 시계와 이만큼(초) 넘게 어긋나면 재생 위치를 다시 맞춘다 (반복 이음매, mp3 꼬리 등). GDD에 없음, Claude가 정함.</summary>
            public const float ResyncThresholdSeconds = 0.05f;
            /// <summary>음악 기준 음량. GDD에 없음, Claude가 정함 (곡이 크게 만들어져 히트사운드·효과음이 묻히지 않게 낮춤).</summary>
            public const float Volume = 0.45f;   // GDD-beta B18.1: 장구 히트사운드가 묻히지 않게 (0.6에서 낮춤)
            /// <summary>곡이 있으면 합성 장단 반주(GDD-beta B7)는 끈다. GDD에 없음, Claude가 정함 (곡에 장구가 이미 있음).</summary>
            public const bool KeepJangguBacking = false;
            /// <summary>음 높이 보정 피치 시프터의 창 길이와 최소 지연 (초, GDD-beta B15.1). 평균 지연 = 최소 + 창/2 = 22 ms.</summary>
            public const float PitchShiftWindowSeconds = 0.04f;
            public const float PitchShiftMinDelaySeconds = 0.002f;
            /// <summary>게임 안 출처 표기 (CC BY·공공누리 1유형).</summary>
            public const string Credit = "음악 재료(공유마당): 한국저작권위원회 「태평소독주 시그널음악」「국악기_꽹과리」 CC BY · (재)전주정보문화산업진흥원 「전통악기 장구」 공공누리 1유형";
        }

        /// <summary>1인칭 전신 플레이어와 툰 셰이딩 (작업 T13, GDD 4.3·5·6).</summary>
        public static class Body
        {
            /// <summary>플레이어 모델 키 (m, GDD 5 에셋 표).</summary>
            public const float PlayerHeight = 1.75f;
            /// <summary>카메라가 머리 뼈 움직임을 따라가는 비율 (0~1). 장애물 카메라 연출(StageDirector)과 겹쳐 두 번 움직이지 않게 일부만.</summary>
            public const float HeadFollowWeight = 0.35f;
            /// <summary>머리 뼈 추적 부드럽게 (1/초, 지수 감쇠).</summary>
            public const float HeadFollowSharpness = 14f;
            /// <summary>눈 위치를 머리 뼈보다 앞으로 (m). 아래를 볼 때 머리를 뺀 목 단면이 화면에 걸리지 않게.</summary>
            public const float EyeForward = 0.12f;
            /// <summary>달리기 한 주기(두 걸음)의 박자 수 — 달리기 재생 속도를 박에 맞춘다.</summary>
            public const float RunCycleBeats = 2f;
            /// <summary>장애물 동작 한 번의 길이 (박자). 클립 길이를 이 길이에 맞게 재생 속도를 바꾼다.</summary>
            public const float ActionBeats = ObstacleBeats;
            /// <summary>동작 사이 크로스페이드 (초, GDD 6 "0.1~0.15초").</summary>
            public const float CrossFadeSeconds = 0.12f;
            /// <summary>머리 메시로 볼 뼈 가중치 (머리·목 합이 이 값 이상인 삼각형은 1인칭 카메라에 그리지 않음).</summary>
            public const float HeadWeightThreshold = 0.5f;

            // ----- 작업 R06: 1인칭 카메라·동작 정리 (사용자 피드백 2·6·7번, GDD에 없음) -----
            /// <summary>장애물 동작·피격 중 카메라가 머리 뼈를 따라가는 비율. 1 = 카메라가 머리에 붙음 → 머리 없는 몸이 화면 앞에 보이지 않는다.</summary>
            public const float ActionHeadFollowWeight = 1f;
            /// <summary>달리기 ↔ 동작 사이 따라가기 비율을 바꾸는 빠르기 (1/초, 지수 감쇠).</summary>
            public const float ActionBlendSharpness = 10f;
            /// <summary>벽타기: 몸(모델 루트)을 발 기준으로 벽 반대쪽으로 눕히는 각도 (도). 발은 벽을 밟고 머리는 길 쪽.</summary>
            public const float WallRunLeanDegrees = 45f;
            /// <summary>벽타기 NORMAL(휘청임)의 기울기 (도).</summary>
            public const float WallRunNormalLeanDegrees = 35f;
            /// <summary>벽타기 때 발이 가는 옆 거리 (m): 벽을 못 찾았을 때 기본값 = 벽 안쪽 면 (벽 중심 2.0 m − 두께 절반 0.25 m) 조금 안.</summary>
            public const float WallRunFootOffset = 1.7f;
            /// <summary>벽 찾기 광선 길이 (m)와 발바닥–벽 면 틈 (m).</summary>
            public const float WallRunMaxSearch = 4f;
            public const float WallRunFootGap = 0.05f;
            /// <summary>벽타기 카메라 기울기 (도, 몸과 같은 방향 = 벽 반대쪽으로). NORMAL은 절반에 흔들림.</summary>
            public const float WallRunCameraRoll = 20f;
            /// <summary>아래로 피하기(미끄러지기) 중 내 몸(팔·다리)이 화면을 가리는 비율 상한 (0~1, PlayMode 측정).</summary>
            public const float SlideMaxScreenCover = 0.2f;
            /// <summary>목 단면이 화면 가장자리 밖 이 비율(뷰포트) 안에만 있어도 몸을 숨긴다 (카메라 흔들림 여유).</summary>
            public const float NeckViewMargin = 0.15f;
            /// <summary>동작 중 카메라가 달리는 방향에서 이 각도(도) 넘게 돌아가면 몸을 숨긴다 (앞구르기 한 바퀴).</summary>
            public const float HideBodyViewAngle = 60f;
            /// <summary>머리가 엉덩이보다 이 높이(m) 안쪽으로 낮아지면 몸이 뒤집힌 것으로 보고 숨긴다 (앞구르기).</summary>
            public const float InvertedHeadAboveHips = 0.15f;
            /// <summary>뒤집혔다가 머리가 엉덩이보다 이 높이(m) 넘게 올라오면 다시 보인다 (미끄러지기 최저 약 0.31 m라 거기선 안 숨김).</summary>
            public const float UprightHeadAboveHips = 0.45f;

            /// <summary>툰 명암 단계 수 (GDD 6 "2~3단계").</summary>
            public const int ToonSteps = 3;
            /// <summary>가장 어두운 단계의 밝기 (0~1).</summary>
            public const float ToonShadowLevel = 0.55f;
            /// <summary>외곽선 두께 (화면 높이 대비, 뒤집은 메시).</summary>
            public const float OutlineWidth = 0.0025f;
        }

        /// <summary>적 모델·전투 애니메이션 (작업 T14).</summary>
        public static class EnemyBody
        {
            // ----- 작업 R07: 적 달리기 (사용자 피드백 4·7번, GDD에 없음) -----
            /// <summary>이 속도(m/s, 수평) 넘게 움직이면 달리기, RunStopSpeed 아래로 떨어지면 대기 (되돌이 여유).</summary>
            public const float RunStartSpeed = 0.8f;
            public const float RunStopSpeed = 0.4f;
            /// <summary>달리기 클립 원래 속도로 보이는 이동 속도 (m/s). 이동 속도 / 이 값 = 재생 배율.</summary>
            public const float RunClipSpeed = 4.5f;
            /// <summary>달리기 재생 배율 범위.</summary>
            public const float RunRateMin = 0.6f;
            public const float RunRateMax = 1.8f;
            /// <summary>이동 속도 부드럽게 (1/초).</summary>
            public const float SpeedSmoothing = 12f;
            /// <summary>한 프레임 이동이 이 속도(m/s)를 넘으면 순간이동(되감기·배치)으로 보고 무시.</summary>
            public const float TeleportSpeed = 25f;
            // ----- 작업 R18 후속: 걷기 (사건 소품 적, 돌진 적이 다가오기 시작할 때) -----
            /// <summary>이 속도(m/s)보다 느리게 움직이면 걷기, WalkToRunSpeed를 넘으면 달리기 (되돌이 여유 RunToWalkSpeed).</summary>
            public const float WalkToRunSpeed = 2.6f;
            public const float RunToWalkSpeed = 2.1f;
            /// <summary>걷기 클립 원래 속도로 보이는 이동 속도 (m/s, Mixamo Standard Walk 약 1.5). 이동 속도 / 이 값 = 재생 배율.</summary>
            public const float WalkClipSpeed = 1.5f;
            public const float WalkRateMin = 0.7f;
            public const float WalkRateMax = 1.5f;

            // ----- 작업 R07: 지붕 발차기 낙하 -----
            /// <summary>차여 날아가는 방향: 플레이어 반대쪽에서 지붕 가장자리 쪽으로 이 각도(도)만큼 돌림 (길 따라 날아가면 지붕에 남으므로).</summary>
            public const float KickOffRoofSideAngle = 80f;
            /// <summary>날아가는 수평 거리 (m, 지붕 반폭 4 m를 넘김)와 그 시간 (초).</summary>
            public const float KickOffRoofDistance = 7.5f;
            public const float KickOffRoofFlightSeconds = 1.0f;
            /// <summary>처음 위로 튀는 속도 (m/s)와 중력 (m/s²).</summary>
            public const float KickOffRoofLift = 2.5f;
            public const float Gravity = 9.8f;
            /// <summary>떨어지는 것을 카메라가 따라보는 시간 (초)과 최대 고개 각도 (도, CameraFx 한도 35°).</summary>
            public const float KickFallLookSeconds = 1.4f;
            public const float KickFallLookMaxYaw = 35f;
            /// <summary>칼·창 길이 (m, 회색 상자와 같은 값).</summary>
            public const float SwordLength = 1.0f;
            public const float SpearLength = 2.2f;
            /// <summary>동작 사이 크로스페이드 (초, GDD 6 "0.1~0.15초").</summary>
            public const float CrossFadeSeconds = 0.12f;
            /// <summary>타격 순간 뒤 휘두르기를 마저 보여 주는 시간 (초) → 그다음 대기 자세로.</summary>
            public const float FollowThroughSeconds = 0.35f;
            /// <summary>타격 순간 찾기: 클립 앞에서 이 시간 안의 오른손 속도 최고점 (긴 클립은 첫 공격만).</summary>
            public const float HitSearchSeconds = 8f;
            /// <summary>타격 순간 찾기에서 빼는 클립 앞부분 비율 (준비 동작).</summary>
            public const float HitSearchSkip = 0.12f;
            /// <summary>처형 시작 때 적이 서는 거리 (m, 플레이어 발 기준). 발차기·칼 뺏기·찌르기.</summary>
            public const float ExecutionDistance = 1.2f;
        }

        /// <summary>맵 꾸미기·분위기 (작업 T15, GDD 5·6, 2026-11-16 사용자 맵 디자인 요청).</summary>
        public static class Map
        {
            // ----- 작업 R03: R01 맵 재설계 안(docs/proposals/map-redesign.html, 사용자 승인 2026-11-16). GDD에 없음, R01에서 정함 -----

            /// <summary>배치 무작위 씨앗 (같은 결과).</summary>
            public const int DressSeed = 16;
            /// <summary>모퉁이 곡선: 모퉁이 앞뒤 이 거리(m)만 곡선으로 잇는다. 경로 거리·박자 관계는 그대로.</summary>
            public const float CornerRadius = 8f;
            /// <summary>달리는 길 반폭 (m): 폭 3.6 m는 항상 비운다.</summary>
            public const float Corridor = 1.8f;
            /// <summary>사건 지점 앞뒤 이 거리(m) 안에서는 길가 이 반폭(m) 안쪽에 소품을 두지 않는다.</summary>
            public const float EventClearAlong = 3f;
            public const float EventClearHalf = 2.4f;
            /// <summary>1줄 건물 사이 틈 (m)과 샛골목 확률·폭 (m).</summary>
            public const float GapMax = 1.2f;
            public const float AlleyChance = 0.18f;
            public const float AlleyMin = 3f, AlleyMax = 4.5f;
            /// <summary>1줄 건물 앞면 들쭉날쭉 (m), 회전 (±도), 크기 배율 범위.</summary>
            public const float FrontJitter = 0.6f;
            public const float YawJitter = 4f;
            public const float ScaleMin = 0.92f, ScaleMax = 1.08f;
            /// <summary>뒤 줄: 길가(반폭)에서 2줄 8.5 m, 3줄 20 m 뒤 (±1.5 m).</summary>
            public const float Row2Back = 8.5f, Row3Back = 20f, RowJitter = 1.5f;
            /// <summary>길 위 지붕(기와지붕 바다·궁궐 담 위)이 있는 곳의 옆 건물 최소 반폭 = 지붕 반폭 + 이 값 (m).</summary>
            public const float RoofSideGap = 0.2f;
            /// <summary>길(중심선)에서 건물 발자국까지 최소 거리 (m) — 모퉁이에서 길을 막지 않게.</summary>
            public const float BuildingClearance = 2.6f;
            /// <summary>계획 단계 건물 발자국 깊이 = 명세 깊이 × 이 값 (실제 모델은 빌드 때 다시 검사).</summary>
            public const float FootprintMargin = 1.3f;

            /// <summary>건물·담 모듈: 너비 범위, 깊이, 높이 (m). 높이는 R02 명세 height_m.</summary>
            public struct Module
            {
                public string Id; public float WidthMin, WidthMax, Depth, Height;
                public Module(string id, float w0, float w1, float d, float h) { Id = id; WidthMin = w0; WidthMax = w1; Depth = d; Height = h; }
            }
            public static readonly Module[] Modules =
            {
                new Module("choga", 7, 9, 6, 4.0f), new Module("choga_L", 9, 11, 8, 4.0f), new Module("house_giwa", 10, 13, 8, 5.5f),
                new Module("giwa_s", 7, 9, 6, 4.8f), new Module("giwa_long", 14, 18, 6, 5.0f), new Module("giwa_gate", 8, 10, 6, 6.0f),
                new Module("sijeon", 12, 15, 7, 7.0f), new Module("palace_hall", 24, 30, 14, 14f), new Module("pavilion", 8, 9, 8, 9.0f),
                new Module("wall_earth", 6, 8, 0.7f, 2.2f), new Module("wall_stone", 6, 8, 1.0f, 2.2f), new Module("wall_palace", 8, 10, 1.2f, 5.0f),
                new Module("fence", 5, 7, 0.3f, 1.4f),
            };

            /// <summary>소품: 발자국(지름, m)과 높이 (m).</summary>
            public struct PropDef
            {
                public string Id; public float Footprint, Height;
                public PropDef(string id, float fp, float h) { Id = id; Footprint = fp; Height = h; }
            }
            public static readonly PropDef[] Props =
            {
                new PropDef("jangdok", 2.2f, 1.3f), new PropDef("well", 2.0f, 2.2f), new PropDef("pine", 3.5f, 7f), new PropDef("zelkova", 6f, 11f),
                new PropDef("jangseung", 0.8f, 3f), new PropDef("firewood", 1.6f, 1.2f), new PropDef("pyeongsang", 2f, 0.6f), new PropDef("jige", 0.9f, 1.6f),
                new PropDef("jars", 1.4f, 1.0f), new PropDef("cart", 1.6f, 1.2f), new PropDef("stall_a", 2.2f, 2.6f), new PropDef("stall_b", 2.2f, 2.6f),
                new PropDef("stall_pot", 2.4f, 1.3f), new PropDef("cloth_rack", 2f, 2.2f), new PropDef("straw", 1.8f, 1.2f), new PropDef("lantern", 0.5f, 3f),
                new PropDef("basket", 1.2f, 1.0f), new PropDef("banner_pole", 0.5f, 5f),
            };

            /// <summary>길 구역 (경로 거리 m): 이름, 반폭(길 중심 → 1줄 건물 앞면), 왼쪽·오른쪽 1줄 모듈, 소품과 10 m당 개수, 2·3줄 모듈.</summary>
            public struct Zone
            {
                public float Start, End, Half; public string Name;
                public string[] Left, Right, Props, Row2, Row3; public float PropsPer10m;
            }
            static Zone Z(float s, float e, string name, float half, string[] l, string[] r, string[] props, float dens, string[] r2, string[] r3)
                => new Zone { Start = s, End = e, Name = name, Half = half, Left = l, Right = r, Props = props, PropsPer10m = dens, Row2 = r2, Row3 = r3 };
            static readonly string[] None = new string[0];
            public static readonly Zone[] Zones =
            {
                Z(0, 15, "마을 어귀", 3.2f, new[] { "fence" }, new[] { "fence" }, new[] { "jangseung", "zelkova", "straw", "jige" }, 3, new[] { "choga" }, None),
                Z(15, 85, "초가 골목", 3.3f, new[] { "choga", "choga_L", "wall_earth", "fence" }, new[] { "choga", "wall_earth", "choga_L", "fence" },
                  new[] { "jangdok", "firewood", "jige", "straw", "jars", "basket" }, 3, new[] { "choga", "choga_L", "house_giwa" }, new[] { "house_giwa", "choga" }),
                Z(85, 140, "장터 큰길", 5.0f, new[] { "sijeon", "giwa_long", "giwa_s" }, new[] { "sijeon", "giwa_long", "house_giwa" },
                  new[] { "stall_a", "stall_b", "stall_pot", "cloth_rack", "straw", "lantern", "basket", "cart", "jars" }, 10, new[] { "house_giwa", "sijeon" }, new[] { "house_giwa" }),
                Z(140, 215, "장터 뒷골목", 4.6f, new[] { "sijeon", "giwa_s", "choga", "giwa_long" }, new[] { "house_giwa", "sijeon", "wall_earth", "giwa_s" },
                  new[] { "stall_a", "stall_b", "basket", "straw", "lantern", "jars", "jige", "stall_pot" }, 6, new[] { "house_giwa", "choga" }, new[] { "house_giwa" }),
                Z(215, 270, "양반 돌담길", 3.4f, new[] { "wall_stone", "giwa_gate", "giwa_s", "house_giwa" }, new[] { "wall_stone", "house_giwa", "giwa_gate", "giwa_long" },
                  new[] { "pine", "jars", "jangdok", "well" }, 3, new[] { "house_giwa" }, new[] { "house_giwa", "pavilion" }),
                Z(270, 410, "기와지붕 바다", 2.6f, new[] { "house_giwa", "giwa_s", "giwa_long" }, new[] { "giwa_long", "house_giwa", "giwa_s" }, None, 0,
                  new[] { "house_giwa", "giwa_s", "giwa_long" }, new[] { "house_giwa", "giwa_long" }),
                Z(410, 440, "뒷마당", 3.4f, new[] { "wall_stone", "choga" }, new[] { "house_giwa", "wall_stone" }, new[] { "jangdok", "firewood", "jars", "pyeongsang" }, 4, new[] { "house_giwa" }, None),
                Z(440, 490, "관아 마당", 15f, new[] { "wall_stone" }, new[] { "wall_stone" }, new[] { "zelkova", "pyeongsang", "jangdok", "banner_pole", "cart" }, 2, new[] { "house_giwa" }, new[] { "house_giwa" }),
                Z(490, 560, "궁궐 담 길", 4.2f, new[] { "wall_palace" }, new[] { "house_giwa", "wall_stone", "giwa_gate" }, new[] { "pine", "lantern", "banner_pole" }, 2, new[] { "palace_hall" }, new[] { "palace_hall" }),
                Z(560, 625, "궁궐 담 위", 0f, None, None, None, 0, new[] { "palace_hall" }, new[] { "house_giwa" }),
                Z(625, 660, "성문 누각", 0f, None, None, None, 0, new[] { "house_giwa", "giwa_long" }, new[] { "house_giwa" }),   // 성문 위에서 내려다보는 도성 지붕
            };

            /// <summary>복선 적이 서는 자리: 종류, 뿌리(사건 위치)에서 앞으로 (m), 옆 (m, +오른쪽). 높이는 그 아래 실제 지붕·담에 맞춘다.</summary>
            public struct Stand
            {
                public string Kind; public float Ahead, Lateral, Spacing;
                public Stand(string k, float a, float l, float s) { Kind = k; Ahead = a; Lateral = l; Spacing = s; }
            }
            public static readonly Stand[] ForeshadowStands =
            {
                new Stand("roof_watchers", 8f, 8.5f, 1.6f),      // 장터 오른쪽 2층 시전 용마루 위 (모퉁이 140 m 전)
                new Stand("wall_climbers", 25f, -15.5f, 1.6f),   // 관아 마당 왼쪽 담 위
                new Stand("gate_warrior", 35f, 1.6f, 1.2f),      // 성문 위 (모퉁이 곡선 지나 곧은 성문 길)
            };
            /// <summary>길가 장식 인스턴싱을 묶는 칸 크기 (m). 칸마다 경계로 카메라·그림자 컬링 (드로우콜).</summary>
            public const float DressCell = 320f;
            /// <summary>작은 소품(그림자 없는 것)은 카메라에서 이 거리(m)보다 먼 칸이면 그리지 않는다 — 안개 속이라 거의 안 보임.</summary>
            public const float PropDrawDistance = 70f;
            /// <summary>작은 소품을 묶는 칸 크기 (m) — 거리 컬링이 듣도록 건물 칸보다 잘게.</summary>
            public const float PropCell = 40f;
            /// <summary>적·복선 발밑 지지면 검사: 이 높이 차(m) 안에 실제 보이는 지붕·담·땅이 있어야 한다.</summary>
            public const float SupportTolerance = 1.0f;
            /// <summary>길 위 지붕(기와집 이어 붙임)의 이음매를 지붕 위에 서는 적 자리에서 이만큼(m) 비킨다.</summary>
            public const float SeamClearance = 1.5f;
            /// <summary>추격 적이 나란히 달리는 옆 거리 (m). 길 위 지붕에서는 지붕 반폭 − RoofRunnerInset 이하 (지붕 가장자리 밖으로 나가지 않게).</summary>
            public const float ChaseLateral = 3.5f;
            public const float RoofRunnerInset = 1.5f;
            /// <summary>작업 R07: 추격 적 옆 거리 하한 (m) — 면이 97% 넘게 이어지는 옆 거리를 찾을 때 여기까지 줄인다.</summary>
            public const float ChaseMinLateral = 1.25f;
            /// <summary>작업 R07: 추격 적이 집과 집 사이 틈을 뛰어넘을 때 최대 높이 (m).</summary>
            public const float ChaseHopMax = 0.8f;

            // ----- 작업 R18 (GDD-beta B19.2, B19.4): 옆 건물에서 쫓아오는 추격 적, 걷다가 질주하는 돌진 적 -----
            /// <summary>추격 적이 공격 전까지 달리는 옆 거리 범위 (m, 옆 지붕 줄·골목). 작은 값부터 찾는다.</summary>
            public const float ChaseFarLateralMin = 8f;
            public const float ChaseFarLateralMax = 20f;
            /// <summary>옆 거리 후보 간격 (m).</summary>
            public const float ChaseFarLateralStep = 1f;
            /// <summary>먼 줄 경로 길이 / 플레이어 경로 길이 상한 (바깥 모퉁이에서 너무 빨리 달리지 않게).</summary>
            public const float ChaseFarMaxStretch = 1.6f;
            /// <summary>공격 몇 박 전에 플레이어 쪽으로 뛰어드는지 (박).</summary>
            public const float ChaseLeapBeats = 2f;
            /// <summary>뛰어들 때 지나는 지붕·담 위 여유 (m)와 최소 포물선 높이 (m).</summary>
            public const float ChaseLeapClearance = 0.6f;
            public const float ChaseLeapMinArc = 1.0f;
            /// <summary>뛰어들기 포물선 높이 상한 (m, 성문·높은 지붕을 넘을 때).</summary>
            public const float ChaseLeapMaxArc = 8f;
            /// <summary>공격 뒤(PERFECT 아님) 플레이어 속도에 대한 추격 적 속도 비율 — 점점 뒤처진다.</summary>
            public const float ChaseFallBehindRate = 0.35f;
            /// <summary>돌진 적: 판정 몇 박 전부터 움직이는지, 걷는 속도·질주 속도 (m/박), 걷기에서 질주로 올라가는 박 수, 질주 박 수.</summary>
            public const float ChargeStartBeats = 12f;
            public const float ChargeWalkSpeed = 0.8f;
            public const float ChargeSprintSpeed = 4f;
            public const float ChargeRampBeats = 4f;
            public const float ChargeSprintBeats = 2f;

            // ----- 작업 R18b (GDD-beta B19.3·B19.4): 숨어 있다가 나오는 적 -----
            /// <summary>매복 적이 판정 몇 박 전에 숨은 자리에서 뛰쳐나오는지 (박).</summary>
            public const float AmbushOutBeats = 2f;
            /// <summary>매복 적 숨은 자리 ↔ 끝자리 거리 범위 (m).</summary>
            public const float AmbushHideMin = 2.5f;
            public const float AmbushHideMax = 7.5f;
            /// <summary>뛰어오르는 매복(지붕·담 밑에서)의 숨은 자리 거리 상한 (m).</summary>
            public const float AmbushHideMaxJump = 11f;
            /// <summary>궁궐 담 위 매복 포졸이 숨는 집의 앞면 거리 (m, 길 가운데에서). 담 반폭 + 여유.</summary>
            public const float HideHouseOffset = 3.0f;
            /// <summary>땅 위 전투 적: 플레이어가 멈추기 몇 박 전에 알아채는지 (기본, 멀면 늘림), 알아채는 동작 길이(박), 멈춤 몇 박 전에 전투 자리에 도착하는지.</summary>
            public const float ApproachNoticeLead = 4f;
            public const float ApproachNoticeBeats = 0.5f;
            public const float ApproachArriveBefore = 0.25f;
            /// <summary>전투 적이 숨은 자리에서 달려오는 속도 상한 (m/박)과 알아채기 박 상한 (먼 숨은 자리).</summary>
            public const float ApproachRunSpeed = 3.6f;
            public const float ApproachNoticeLeadMax = 7f;
            /// <summary>숨은 자리를 찾을 때 플레이어 눈 높이 (m)와 시야를 확인하는 범위 (멈춤·판정 몇 박 전부터).</summary>
            public const float HideEyeHeight = 1.6f;
            public const float HideCheckBeats = 16f;

            // ----- 작업 R18c (GDD-beta B19.3 뒷부분): 앞질러 지붕을 넘어 착지하는 지붕 위 전투 적 -----
            /// <summary>멈춤 몇 박 전에 옆 지붕 줄에 나타나 달리기 시작하는지, 지붕을 박차는 박, 전투 자리에 발이 닿는 박, 대기로 돌아오는 박 (멈춤 기준).</summary>
            public const float LeapRunBeats = 12f;
            public const float LeapTakeoffBefore = 2.75f;
            public const float LeapContactBefore = 1.25f;
            public const float LeapIdleBefore = 0.4f;
            /// <summary>옆 지붕 줄의 옆 거리 범위 (m)와 간격, 나타나는 자리(플레이어 뒤 m), 박차는 자리(전투 자리보다 앞 m).</summary>
            public const float LeapLateralMin = 6f;
            public const float LeapLateralMax = 12f;
            public const float LeapStartBehind = 5f;
            public const float LeapTakeoffAhead = 1f;
            /// <summary>박찬 뒤 도약 클립을 이어 보여 주는 시간 (초), 그다음 공중 자세. 날아가는 포물선 높이 상한 (m).</summary>
            public const float LeapJumpAirSeconds = 0.25f;
            public const float LeapMaxArc = 7f;

            /// <summary>먼 배경 파노라마 (R02 그림 6장을 이어 붙인 한 장, 둘레를 한 번만 돈다).</summary>
            public const float BackdropTopFade = 0.55f;

            /// <summary>먼 배경(산·지붕 실루엣)을 그린 원통 반지름·높이 (m). 맵 중심 기준.</summary>
            public const float BackdropRadius = 300f;
            public const float BackdropHeight = 150f;
            /// <summary>원통 밑을 땅 아래로 내리는 깊이 (m).</summary>
            public const float BackdropSink = 6f;

            /// <summary>해 질 녘 해 (도): 높이·방향.</summary>
            public const float SunPitch = 17f;
            public const float SunYaw = -62f;
            public const float SunIntensity = 1.25f;
            /// <summary>그림자 단계 수 (URP 주광 캐스케이드). 4 → 2로 그림자 그리기 호출을 줄인다.</summary>
            public const int ShadowCascades = 2;
            /// <summary>안개 (선형, m).</summary>
            public const float FogStart = 70f;
            public const float FogEnd = 330f;
        }
    }
}
