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
        public static readonly string[] Attacks = { "horizontal", "overhead", "upward", "thrust" };
        public static readonly ExecutionKind[] Defeats =
            { ExecutionKind.KickOffRoof, ExecutionKind.DisarmSlash, ExecutionKind.StabWithOwnSword, ExecutionKind.PushDown, ExecutionKind.Windmill };

        public static string ModelOf(string kind) => kind == "spear" ? "enemy_spear" : kind == "boss" ? "boss" : "enemy_sword";
        public static string WeaponOf(string kind) => kind == "spear" ? "weapon_spear" : "weapon_sword";

        public static string AttackState(string attack) => "Attack_" + attack;
        public static string DefeatState(ExecutionKind how) => "Defeat_" + how;

        public static string IdleClip(string model) => "standing_idle_1_looping";

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

        /// <summary>쓰러지는 동작: 지붕에서 떨어짐·칼 뺏김·찔림·밀려 쓰러짐·윈드밀에 날아감.</summary>
        public static string DefeatClip(string model, ExecutionKind how)
        {
            bool boss = model == "boss";
            switch (how)
            {
                case ExecutionKind.KickOffRoof: return "hit_fall";
                case ExecutionKind.DisarmSlash: return boss ? "hit_fall" : "struggle_fall";
                case ExecutionKind.Windmill: return boss ? "heavy_hit_back" : "light_hit_back";
                default: return "hit_fall";
            }
        }
    }
}
