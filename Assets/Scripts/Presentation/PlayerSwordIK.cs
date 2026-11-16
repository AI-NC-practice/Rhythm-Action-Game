using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R27 (평가 1회차 지적 "막을 때 1인칭 화면에 내 칼이 안 보임"): 막기 동안 오른손을 1인칭 카메라 앞 대각선 자리로 옮긴다 (손 IK).
    /// 칼날 방향은 PlayerBody가 맞춘다. Animator(모델)와 같은 오브젝트에 붙어 OnAnimatorIK를 받는다 (컨트롤러 IK Pass 켬).
    /// </summary>
    public class PlayerSwordIK : MonoBehaviour
    {
        public PlayerBody body;
        Animator animator;

        void Awake() => animator = GetComponent<Animator>();

        void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || body == null) return;
            float w = body.SwordGuardWeight;
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            if (w <= 0f || body.viewCamera == null) return;
            var c = body.viewCamera.transform;
            var g = GameConfig.Body.SwordGuardHand;
            var right = c.position + c.right * g.x + c.up * g.y + c.forward * g.z;
            animator.SetIKPosition(AvatarIKGoal.RightHand, right);
            // 왼손은 옮기지 않는다 — 위치만 옮기면 손 방향이 칼자루와 안 맞아 손가락이 칼자루를 뚫고 삐져나왔다 (사용자 피드백)
        }
    }
}
