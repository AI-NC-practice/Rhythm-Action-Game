#if UNITY_EDITOR || RAG_DEV
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmActionGame
{
    /// <summary>
    /// 개발자 모드 (사용자 요청, GDD에 없음 — 본 게임에서는 사라진다).
    /// 에디터와 회색 상자 빌드(RAG_DEV 정의)에서만 컴파일된다. BuildFinal에는 들어가지 않는다.
    /// 숫자 키 1~6(키패드 포함): 해당 챕터(MapLayout.Sections) 시작 지점에서 Game 씬을 새로 시작한다.
    /// 타이틀 화면에서도 동작한다.
    /// </summary>
    public class DevMode : MonoBehaviour
    {
        public static DevMode Instance { get; private set; }
        Text label;
        int shownSection = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (Instance != null) return;
            var go = new GameObject("DevMode");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<DevMode>();
        }

        void Awake()
        {
            var canvas = UiFactory.CreateCanvas("DevCanvas", 500);
            canvas.transform.SetParent(transform, false);
            label = UiFactory.CreateText(canvas.transform, "DevLabel", "", 26, new Color(1f, 0.85f, 0.2f, 0.9f), new Vector2(1200, 40));
            label.alignment = TextAnchor.UpperCenter;
            label.raycastTarget = false;
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -8);
            Refresh();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>챕터 번호(1~6)의 시작 달리기 박자. 범위 밖이면 -1.</summary>
        public static int ChapterStartBeat(int chapter)
        {
            if (chapter < 1 || chapter > MapLayout.Sections.Length) return -1;
            return MapLayout.Sections[chapter - 1].StartBeat;
        }

        /// <summary>챕터 시작 지점에서 Game 씬을 새로 불러온다 (적·점수·체력 모두 초기화).</summary>
        public static bool JumpTo(int chapter)
        {
            int beat = ChapterStartBeat(chapter);
            if (beat < 0) return false;
            GameBootstrap.PendingStartBeat = beat;
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
            return true;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                for (int n = 1; n <= MapLayout.Sections.Length; n++)
                {
                    if (kb[Key.Digit1 + (n - 1)].wasPressedThisFrame || kb[Key.Numpad1 + (n - 1)].wasPressedThisFrame)
                    {
                        JumpTo(n);
                        break;
                    }
                }
            }
            if (kb != null && kb.f1Key.wasPressedThisFrame) ToggleFx();
            Refresh();
        }

        /// <summary>F1: 연출 전체 켜기/끄기 (GDD-beta B5).</summary>
        public static bool ToggleFx()
        {
            PresentationDirector.DefaultEnabled = !PresentationDirector.DefaultEnabled;
            if (PresentationDirector.Instance != null) PresentationDirector.Instance.Enabled = PresentationDirector.DefaultEnabled;
            if (Instance != null) Instance.shownSection = -2;   // 문구 갱신
            return PresentationDirector.DefaultEnabled;
        }

        void Refresh()
        {
            var boot = GameBootstrap.Instance;
            int section = boot != null && boot.player != null ? MapLayout.SectionOf((int)boot.player.RunBeat).Number : 0;
            if (section == shownSection && label.text.Length > 0) return;
            shownSection = section;
            string where = section > 0 ? $"  |  현재 {section}. {MapLayout.Sections[section - 1].Name}" : "";
            string fx = PresentationDirector.DefaultEnabled ? "켬" : "끔";
            label.text = $"[개발자 모드] 숫자 1~{MapLayout.Sections.Length}: 챕터 이동  |  F1: 연출 {fx}{where}";
        }

        public string LabelText => label.text;
    }
}
#endif
