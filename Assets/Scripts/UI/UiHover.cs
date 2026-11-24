using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R38 (사용자 피드백 2026-11-24 3번 "마우스가 올라가면 반응이 있어야"): 메뉴 버튼에 마우스를 올리면 커지고 글자가 금빛으로,
    /// 누르는 순간 살짝 눌린다. 버튼 배경색은 Button의 ColorBlock(UiWidgets.Button)이 바꾼다. 일시정지(timeScale 0)에서도 움직이게 실제 시간.
    /// </summary>
    public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Text label;
        public bool Hovered { get; private set; }
        public bool Pressed { get; private set; }

        Color labelNormal = Color.white;
        Selectable sel;

        void Awake()
        {
            sel = GetComponent<Selectable>();
            if (label != null) labelNormal = label.color;
        }

        void OnDisable()
        {
            Hovered = Pressed = false;
            transform.localScale = Vector3.one;
            if (label != null) label.color = labelNormal;
        }

        public void OnPointerEnter(PointerEventData e) => Hovered = true;
        public void OnPointerExit(PointerEventData e) { Hovered = false; Pressed = false; }
        public void OnPointerDown(PointerEventData e) => Pressed = true;
        public void OnPointerUp(PointerEventData e) => Pressed = false;

        /// <summary>지금 목표 크기 (테스트용).</summary>
        public float TargetScale => sel != null && !sel.interactable ? 1f
            : Pressed ? GameConfig.MenuButton.PressedScale : Hovered ? GameConfig.MenuButton.HoverScale : 1f;

        void Update()
        {
            float k = 1f - Mathf.Exp(-GameConfig.MenuButton.Sharpness * Time.unscaledDeltaTime);
            float s = Mathf.Lerp(transform.localScale.x, TargetScale, k);
            transform.localScale = new Vector3(s, s, 1f);
            if (label != null) label.color = Color.Lerp(label.color, Hovered ? GameConfig.MenuButton.LabelHover : labelNormal, k);
        }
    }
}
