using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace RhythmActionGame
{

    /// <summary>속도감 효과 (GDD 4.3): s ≥ 1.10이면 화면 가장자리 집중선, FOV 최대 +5°.</summary>
    public class SpeedFx : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeFx judgeFx;
        public Image lines;

        public float Intensity { get; private set; }

        void Awake()
        {
            // 집중선 그림은 실행할 때 코드로 만든다 (씬에 저장되지 않는 텍스처)
            if (lines != null && lines.sprite == null) lines.sprite = UiWidgets.SpeedLines;
        }

        void LateUpdate()
        {
            float s = clock != null ? clock.SpeedMultiplier : GameConfig.SpeedStart;
            Intensity = SpeedFxMath.Intensity(s);
            if (judgeFx != null) judgeFx.FovBonus = SpeedFxMath.FovBonus(s);
            if (lines != null)
            {
                var c = lines.color;
                c.a = Intensity;
                lines.color = c;
                lines.enabled = Intensity > 0f;
            }
        }
    }
}
