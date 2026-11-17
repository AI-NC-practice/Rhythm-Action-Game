using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// PlayerBody의 목 단면 숨김 규칙을 모든 LateUpdate(카메라 연출) 뒤에 한 번 더 확인한다. PlayerBody.Start가 붙인다.
    /// </summary>
    [DefaultExecutionOrder(30000)]
    public class PlayerBodyLateVisibility : MonoBehaviour
    {
        public PlayerBody body;

        void LateUpdate()
        {
            if (body != null) body.LateVisibilityCheck();
        }
    }
}
