using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R27 보강 (사용자 피드백 2026-11-17 "손을 편 채 칼만 손에 달려 있으면 안 된다, 손가락으로 쥐어야"):
    /// 무기를 쥔 오른손 손가락을 칼자루를 감아쥔 자세로 만든다. 적 리그는 VARCO "humanoid-fingers"로 다시 리깅해 손가락 뼈가 있다.
    /// Mixamo 칼 동작은 이미 쥐고 있으므로 더 많이 굽힌 값만 남긴다(값이 작을수록 굽힘). Animator 갱신 뒤(LateUpdate)에 한다.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class FingerGrip : MonoBehaviour
    {
        public Animator animator;
        /// <summary>왼손도 쥘지 (두 손으로 쥐는 창 등). 기본은 오른손만.</summary>
        public bool leftHand;

        HumanPoseHandler handler;
        HumanPose pose;
        int[] muscles;
        float[] values;
        /// <summary>덮어쓴 프레임 수 (테스트용).</summary>
        public int Applied { get; private set; }

        public static void Build(bool left, out int[] ids, out float[] vals)
        {
            var names = HumanTrait.MuscleName;
            var li = new System.Collections.Generic.List<int>(); var lv = new System.Collections.Generic.List<float>();
            for (int i = 0; i < names.Length; i++)
            {
                string n = names[i].Replace('.', ' ');
                bool right = n.StartsWith("Right ") || n.StartsWith("RightHand ");
                bool leftN = n.StartsWith("Left ") || n.StartsWith("LeftHand ");
                if (!(right || (left && leftN))) continue;
                float v = float.NaN;
                foreach (var f in new[] { "Index", "Middle", "Ring", "Little" })
                {
                    if (n.EndsWith(f + " 1 Stretched")) v = GameConfig.Body.GripCurl1;
                    else if (n.EndsWith(f + " 2 Stretched") || n.EndsWith(f + " 3 Stretched")) v = GameConfig.Body.GripCurl23;
                    else if (n.EndsWith(f + " Spread")) v = 0f;
                }
                if (n.EndsWith("Thumb 1 Stretched") || n.EndsWith("Thumb 2 Stretched") || n.EndsWith("Thumb 3 Stretched")) v = GameConfig.Body.GripThumb;
                if (!float.IsNaN(v)) { li.Add(i); lv.Add(v); }
            }
            ids = li.ToArray(); vals = lv.ToArray();
        }

        void LateUpdate()
        {
            if (animator == null || animator.avatar == null || !animator.isHuman || !animator.gameObject.activeInHierarchy) return;
            if (handler == null) { handler = new HumanPoseHandler(animator.avatar, animator.transform); Build(leftHand, out muscles, out values); }
            if (muscles.Length == 0) return;
            handler.GetHumanPose(ref pose);
            for (int k = 0; k < muscles.Length; k++) pose.muscles[muscles[k]] = Mathf.Min(pose.muscles[muscles[k]], values[k]);
            handler.SetHumanPose(ref pose);
            Applied++;
        }
    }
}
