namespace RhythmActionGame
{
    /// <summary>
    /// 적 Animator 상태와 클립 (작업 T14). 모델 id는 enemy_sword / enemy_spear / boss.
    /// 상태: Idle(반복), Attack_{horizontal|overhead|upward|thrust}, Guard, Hit, Defeat_{ExecutionKind}.
    /// 클립 이름은 Assets/Art/Models/&lt;id&gt;/&lt;id&gt;_&lt;동작&gt;.fbx 의 &lt;동작&gt;.
    /// </summary>
    public static class EnemyMoves
    {
        public const string Idle = "Idle";
        public const string Guard = "Guard";
        public const string Hit = "Hit";
        /// <summary>달리기 (작업 R07): 이동 속도가 있을 때. 반복.</summary>
        public const string Run = "Run";
        public const string RunClip = "forward_run_1";
        /// <summary>걷기 (작업 R18 후속, 사용자 승인 2026-11-17 Mixamo "Standard Walk", In Place): 느리게 움직일 때. 반복.</summary>
        public const string Walk = "Walk";
        public const string WalkClip = MixamoPrefix + "walk";
        /// <summary>
        /// 작업 R18c (GDD-beta B19.3): 앞질러 지붕을 넘어 착지 — 도약(Mixamo Running Jump), 공중(Falling Idle, 반복), 착지(Hard Landing).
        /// 사용자 승인 2026-11-17. 열쇠 시각(EnemyAnimator.HitSecondsOf): 도약 = 발이 지붕을 떠나는 순간, 착지 = 발이 땅에 닿는 순간.
        /// </summary>
        public const string LeapJump = "Leap_Jump", LeapFall = "Leap_Fall", LeapLand = "Leap_Land";
        public const string LeapJumpClip = MixamoPrefix + "running_jump", LeapFallClip = MixamoPrefix + "falling_idle", LeapLandClip = MixamoPrefix + "hard_landing";
        /// <summary>달리기 클립을 가진 모델 (무사는 달리기 동작이 없어 칼 포졸 것을 Humanoid로 옮겨 씀).</summary>
        public static string RunClipModel(string model) => model == "boss" ? "enemy_sword" : model;
        public static readonly string[] Attacks = { "horizontal", "overhead", "upward", "thrust" };
        public static readonly ExecutionKind[] Defeats =
            { ExecutionKind.KickOffRoof, ExecutionKind.DisarmSlash, ExecutionKind.StabWithOwnSword, ExecutionKind.PushDown, ExecutionKind.Windmill };

        public static string ModelOf(string kind) => kind == "spear" ? "enemy_spear" : kind == "boss" ? "boss" : "enemy_sword";
        public static string WeaponOf(string kind) => kind == "spear" ? "weapon_spear" : "weapon_sword";

        public static string AttackState(string attack) => "Attack_" + attack;
        public static string DefeatState(ExecutionKind how) => "Defeat_" + how;

        /// <summary>작업 R28: 대기 = 칼을 든 전투 자세 (Mixamo "Great Sword Idle", 사용자 상시 승인 2026-11-17). 예전 VARCO 대기는 팔을 벌리고 서 있었다.</summary>
        public static string IdleClip(string model) => MixamoPrefix + "great_sword_idle";

        public static string GuardClip(string model) => model == "boss" ? "retreat_block" : "cross_block";

        public static string HitClip(string model) => model == "boss" ? "heavy_hit_back" : "light_hit_back";

        public static string AttackClip(string model, string attack)
        {
            switch (model)
            {
                case "enemy_spear":
                    return attack == "overhead" ? "jump_stab_land" : "two_hand_attack";
                case "boss":
                    return attack == "horizontal" ? "spinning_slash" : attack == "overhead" ? "double_slash" : attack == "upward" ? "combo_slash" : "sword_slash";
                default:
                    return attack == "overhead" ? "double_slash" : "sword_slash";
            }
        }

        /// <summary>Mixamo 동작(Assets/Art/Animations/Mixamo/)을 쓰는 클립 이름 앞에 붙는다 (Humanoid로 옮겨 씀).</summary>
        public const string MixamoPrefix = "mixamo/";
        /// <summary>지붕 위 발차기 낙하 (작업 R07): Mixamo "Flying Back Death".</summary>
        public const string KickFallClip = "flying_back";

        /// <summary>쓰러지는 동작: 지붕에서 떨어짐·칼 뺏김·찔림·밀려 쓰러짐·윈드밀에 날아감.</summary>
        public static string DefeatClip(string model, ExecutionKind how)
        {
            bool boss = model == "boss";
            switch (how)
            {
                case ExecutionKind.KickOffRoof: return MixamoPrefix + KickFallClip;   // 작업 R07: 차여서 뒤로 날아감 (사용자 승인 2026-11-17)
                case ExecutionKind.DisarmSlash: return boss ? "hit_fall" : "struggle_fall";
                case ExecutionKind.Windmill: return MixamoPrefix + KickFallClip;   // 작업 R31: 발에 맞아 뒤로 날아가 쓰러짐 (예전 맞고 비틀거림 → 선 자세로 누움)
                default: return "hit_fall";
            }
        }
    }
}
