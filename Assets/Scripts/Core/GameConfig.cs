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
        /// <summary>판정 문구 표시 시간 (초).</summary>
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
    }
}
