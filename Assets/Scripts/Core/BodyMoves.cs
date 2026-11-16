namespace RhythmActionGame
{
    /// <summary>
    /// 1인칭 전신 플레이어의 Animator 상태 고르기 (작업 T13). 장애물(사건) 판정 결과마다 상태 하나.
    /// 상태 이름 = "{동작}_{결과}" (예: Vault_Perfect). 기본은 Run(반복).
    /// 동작은 장애물 분류(GDD 4.4)로 정하고, 분류가 없는 새 사건·안 멈추는 적은 판정 키로 정한다(↑ Climb, ↓ Slide, ←/→ Dodge).
    /// </summary>
    public static class BodyMoves
    {
        public const string Run = "Run";

        /// <summary>상태 이름에 쓰는 동작 이름.</summary>
        public static readonly string[] Actions = { "Vault", "Climb", "Slide", "Drop", "Dodge", "WallRun" };
        public static readonly Judgement[] Results = { Judgement.Perfect, Judgement.Normal, Judgement.Fail };

        public static string ActionOf(ObstacleCategory c)
        {
            switch (c)
            {
                case ObstacleCategory.Vault: return "Vault";
                case ObstacleCategory.Climb: return "Climb";
                case ObstacleCategory.Slide: return "Slide";
                case ObstacleCategory.Drop: return "Drop";
                case ObstacleCategory.Dodge: return "Dodge";
                default: return "WallRun";
            }
        }

        public static string ActionOfKey(ArrowKey k) =>
            k == ArrowKey.Up ? "Climb" : k == ArrowKey.Down ? "Slide" : "Dodge";

        public static string State(string action, Judgement r) => $"{action}_{r}";

        /// <summary>사건 하나의 판정 결과 → 상태 이름. category가 없으면 key로.</summary>
        public static string StateFor(ObstacleCategory? category, ArrowKey key, Judgement r) =>
            State(category.HasValue ? ActionOf(category.Value) : ActionOfKey(key), r);

        /// <summary>왼쪽으로 피하는/달리는 동작은 오른쪽 클립을 좌우 반전한다.</summary>
        public static bool Mirror(string action, ArrowKey key) =>
            key == ArrowKey.Left && (action == "Dodge" || action == "WallRun");

        /// <summary>
        /// 상태별 클립 (작업 T12 Mixamo + T11 VARCO player). 앞은 Mixamo 파일 이름(Assets/Art/Animations/Mixamo/),
        /// "player_"로 시작하면 VARCO player 동작(Assets/Art/Models/player/).
        /// </summary>
        public static string ClipFor(string action, Judgement r)
        {
            switch (action)
            {
                case "Vault": return r == Judgement.Perfect ? "vault" : r == Judgement.Normal ? "player_climb_box" : "hit_obstacle";
                case "Climb": return r == Judgement.Perfect ? "player_run_jump" : r == Judgement.Normal ? "player_climb_box" : "hit_obstacle";
                case "Slide": return r == Judgement.Perfect ? "slide" : r == Judgement.Normal ? "duck" : "hit_obstacle";   // R06: NORMAL은 팔이 화면을 가리던 player_box_slide 대신 숙이기
                case "Drop": return r == Judgement.Perfect ? "player_forward_roll" : r == Judgement.Normal ? "player_heavy_landing_1" : "player_forward_fall_twist";
                case "Dodge": return r == Judgement.Perfect ? "dodge_right" : r == Judgement.Normal ? "player_spin_jump_dodge" : "player_heavy_hit_back";
                default: return r == Judgement.Fail ? "stumble" : RunClip;   // R06: 벽타기는 몸을 45° 눕혀(PlayerBody) 달리기 클립으로 벽을 밟음
            }
        }

        public const string RunClip = "player_forward_run_1";

        /// <summary>작업 R16: 멈춰 싸우는 동안(전투 상태, 이동 거의 없음) 대기 자세. 플레이어 전용 대기 클립이 없어 같은 Humanoid 적(검)의 대기 클립을 옮겨 쓴다.</summary>
        public const string CombatIdle = "CombatIdle";
        public const string CombatIdleClip = "great_sword_idle";   // 작업 R28: Mixamo 칼 든 대기 (예전 적 VARCO 대기는 팔을 벌림)
        /// <summary>작업 R27: 전투에서 내 칼로 막기(적 공격 ↑ 성공)·베기(내 공격·칼 겨루기 성공). Mixamo Great Sword Blocked Impact / Power Slash.</summary>
        public const string SwordBlock = "SwordBlock", SwordBlockClip = "great_sword_block";
        public const string SwordSlash = "SwordSlash", SwordSlashClip = "great_sword_slash";

        /// <summary>내가 맞음 (작업 T14).</summary>
        public const string Hit = "Hit";
        public const string HitClip = "player_light_hit_back";

        /// <summary>처형 상태 (작업 T14): 적의 쓰러짐 동작과 같은 박에 시작한다.</summary>
        public static string ExecState(ExecutionKind how) => "Exec_" + how;

        public static readonly ExecutionKind[] Executions = EnemyMoves.Defeats;

        public static string ExecClip(ExecutionKind how)
        {
            switch (how)
            {
                case ExecutionKind.KickOffRoof: return "kick";
                case ExecutionKind.DisarmSlash: return "disarm";
                case ExecutionKind.StabWithOwnSword: return "stab";
                case ExecutionKind.Windmill: return "windmill";
                default: return "player_cross_block";        // 밀쳐 쓰러뜨리기
            }
        }

        /// <summary>클립을 beats 박 길이에 맞추는 재생 속도.</summary>
        public static float SpeedToFit(float clipSeconds, float beats, float secondsPerBeat) =>
            beats <= 0f || secondsPerBeat <= 0f ? 1f : clipSeconds / (beats * secondsPerBeat);
    }
}
