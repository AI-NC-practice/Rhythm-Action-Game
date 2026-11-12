using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>장애물 종류 6가지 (GDD 4.4 표의 행).</summary>
    public enum ObstacleCategory
    {
        Vault,      // 낮은 담, 수레, 항아리 더미 (↑)
        Climb,      // 지붕으로 뛰어오르기, 지붕 사이 건너뛰기, 성문 오르기 (↑)
        Slide,      // 낮은 처마, 빨랫줄 (↓)
        Drop,       // 지붕에서 뛰어내리기 (↓)
        Dodge,      // 한쪽에 놓인 장애물 (← / →)
        WallRun,    // 왼쪽/오른쪽 벽 (← / →)
    }

    /// <summary>
    /// 장애물 통과 연출 (GDD 4.4). 결과와 관계없이 4박자 동안 경로를 따라 이동하고,
    /// 결과에 따라 카메라 궤적(위치 오프셋)과 흔들림(회전 오프셋)만 다르다. 회색 상자 단계의 표현.
    /// </summary>
    public static class Traversal
    {
        public static ObstacleCategory Categorize(string kind)
        {
            switch (kind)
            {
                case "cart": case "low_wall": return ObstacleCategory.Vault;
                case "roof_climb": case "roof_jump": case "gate_climb": return ObstacleCategory.Climb;
                case "clothesline": case "eave": return ObstacleCategory.Slide;
                case "drop_down": return ObstacleCategory.Drop;
                case "jars": return ObstacleCategory.Dodge;
                case "wall_run": return ObstacleCategory.WallRun;
                default: throw new System.ArgumentException("알 수 없는 장애물: " + kind);
            }
        }

        /// <summary>GDD 4.4 표의 키.</summary>
        public static bool KeyFits(ObstacleCategory c, ArrowKey k)
        {
            switch (c)
            {
                case ObstacleCategory.Vault: case ObstacleCategory.Climb: return k == ArrowKey.Up;
                case ObstacleCategory.Slide: case ObstacleCategory.Drop: return k == ArrowKey.Down;
                default: return k == ArrowKey.Left || k == ArrowKey.Right;
            }
        }

        /// <summary>연출 상태 이름. 예: "Vault_Perfect", "Slide_Fail".</summary>
        public static string StateName(ObstacleCategory c, Judgement r) => $"{c}_{r}";

        /// <summary>
        /// 진행도 p(0~1, 4박자) 에서의 카메라 오프셋. pos는 진행 방향 기준 로컬(x 오른쪽, y 위, z 앞), euler는 도(pitch, yaw, roll).
        /// side: 왼쪽 −1, 오른쪽 +1 (Dodge·WallRun). p=0과 p=1에서 오프셋은 0 (경로로 돌아옴).
        /// </summary>
        public static void Offset(ObstacleCategory c, Judgement r, float side, float p, out Vector3 pos, out Vector3 euler)
        {
            p = Mathf.Clamp01(p);
            float bell = Mathf.Sin(p * Mathf.PI);           // 0 → 1 → 0
            pos = Vector3.zero;
            euler = Vector3.zero;

            switch (r)
            {
                case Judgement.Perfect:
                    switch (c)
                    {
                        case ObstacleCategory.Vault: pos.y = 0.8f * bell; euler.x = -6f * bell; break;
                        case ObstacleCategory.Climb: pos.y = 1.0f * bell; euler.x = -10f * bell; break;
                        case ObstacleCategory.Slide: pos.y = -1.0f * bell; euler.x = 8f * bell; break;
                        case ObstacleCategory.Drop: pos.y = 0.3f * Mathf.Sin(p * Mathf.PI * 2f) * (1 - p); euler.x = 360f * Smooth(p, 0.45f, 0.8f); break; // 착지 후 앞구르기
                        case ObstacleCategory.Dodge: pos.x = side * 1.0f * bell; euler.z = -side * 8f * bell; break;
                        case ObstacleCategory.WallRun: pos.x = side * 0.9f * bell; pos.y = 1.2f * bell; euler.z = -side * 20f * bell; break;
                    }
                    break;

                case Judgement.Normal:
                    // 같은 동작이지만 휘청임: 낮고 비틀린 궤적 + 흔들림
                    float wobble = Mathf.Sin(p * Mathf.PI * 6f) * bell;
                    switch (c)
                    {
                        case ObstacleCategory.Vault: pos.y = 0.5f * bell; euler.z = 7f * wobble; euler.x = 6f * bell; break;
                        case ObstacleCategory.Climb: pos.y = 0.5f * bell - 0.4f * Smooth(p, 0.4f, 0.6f) * (1 - p) * 2f; euler.x = 12f * bell; euler.z = 5f * wobble; break; // 처마에 매달림
                        case ObstacleCategory.Slide: pos.y = -0.7f * bell; euler.z = 6f * wobble; break;
                        case ObstacleCategory.Drop: pos.y = -0.3f * bell; euler.z = 8f * wobble; break;
                        case ObstacleCategory.Dodge: pos.x = side * 0.6f * bell; euler.z = side * 10f * wobble; break;
                        case ObstacleCategory.WallRun: pos.x = side * 0.7f * bell; pos.y = 0.5f * bell; euler.z = -side * 8f * bell + 6f * wobble; break;
                    }
                    break;

                default: // FAIL: 부딪혀 뒤로 튕겨 나감 → 허둥지둥 회복 (공통 궤적 + 종류별 높이)
                    float knock = Mathf.Sin(Mathf.Clamp01(p / 0.35f) * Mathf.PI * 0.5f) * (1f - Smooth(p, 0.35f, 1f));
                    float shake = Mathf.Sin(p * Mathf.PI * 18f) * (1f - p);
                    pos.z = -1.5f * knock;                      // 뒤로 튕김
                    pos.y = -0.9f * knock;                      // 쓰러짐
                    euler.x = -18f * knock;                     // 하늘을 봄
                    euler.z = 10f * shake;                      // 흔들림
                    if (c == ObstacleCategory.Climb || c == ObstacleCategory.WallRun) pos.y -= 0.8f * knock; // 떨어졌다 기어오름
                    break;
            }
        }

        static float Smooth(float p, float a, float b) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, p));
    }

    /// <summary>체력 (GDD 4.3). 3에서 시작, FAIL마다 −1, 0이면 게임 오버.</summary>
    public class PlayerHealth
    {
        public int Hp { get; private set; } = GameConfig.MaxHp;
        public bool IsDead => Hp <= 0;
        public event System.Action<int> Changed;
        public event System.Action Died;

        public void Damage()
        {
            if (IsDead) return;
            Hp--;
            Changed?.Invoke(Hp);
            if (IsDead) Died?.Invoke();
        }

        public void Reset()
        {
            Hp = GameConfig.MaxHp;
            Changed?.Invoke(Hp);
        }
    }
}
