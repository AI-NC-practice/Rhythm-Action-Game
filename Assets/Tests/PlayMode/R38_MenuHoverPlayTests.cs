using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R38 (사용자 피드백 2026-11-24 1·3번): 시작화면에서 Esc는 무시, 메뉴 버튼에 마우스를 올리면 배경이 금빛으로 밝아지고 커지며 글자가 금빛,
    /// 누르면 살짝 눌리고, 내리면 원래대로. 시작화면·일시정지·결과 버튼 모두. 미리보기 → harness/reports/R38/.
    /// </summary>
    public class R38_MenuHoverPlayTests
    {
        const string Out = "harness/reports/R38";

        [TearDown]
        public void TearDown() { AttractMode.AttractOnLoad = false; Time.timeScale = 1f; }

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        static float Dist(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

        /// <summary>버튼 하나: 올리기 → 밝은 금빛·1.08배·금빛 글자, 누르기 → 0.96배, 내리기 → 원래대로.</summary>
        static IEnumerator CheckButton(Button btn, string name)
        {
            var hover = btn.GetComponent<UiHover>();
            Assert.IsNotNull(hover, $"{name}: 마우스 반응 컴포넌트");
            var img = (Image)btn.targetGraphic;
            var label = btn.GetComponentInChildren<Text>();
            yield return Wait(0.2f);
            Color idle = img.canvasRenderer.GetColor() * img.color;
            Assert.Less(Dist(idle, GameConfig.MenuButton.ButtonNormal), 0.06f, $"{name}: 평소 어두운 배경");
            Assert.AreEqual(1f, btn.transform.localScale.x, 0.01f);

            ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return Wait(0.35f);
            Color hot = img.canvasRenderer.GetColor() * img.color;
            Assert.Less(Dist(hot, GameConfig.MenuButton.ButtonHover), 0.06f, $"{name}: 올리면 금빛 배경 ({hot})");
            Assert.Greater(Dist(hot, idle), 0.5f, $"{name}: 눈에 띄게 바뀜");
            Assert.AreEqual(GameConfig.MenuButton.HoverScale, btn.transform.localScale.x, 0.01f, $"{name}: 올리면 커짐");
            Assert.Less(Dist(label.color, GameConfig.MenuButton.LabelHover), 0.06f, $"{name}: 글자 금빛");

            ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
            yield return Wait(0.3f);
            Assert.AreEqual(GameConfig.MenuButton.PressedScale, btn.transform.localScale.x, 0.01f, $"{name}: 누르면 눌림");
            ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(btn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return Wait(0.35f);
            Color back = img.canvasRenderer.GetColor() * img.color;
            Assert.Less(Dist(back, idle), 0.06f, $"{name}: 내리면 원래 색");
            Assert.AreEqual(1f, btn.transform.localScale.x, 0.01f, $"{name}: 내리면 원래 크기");
            Assert.Less(Dist(label.color, Color.white), 0.06f, $"{name}: 글자 원래 색");
        }

        static void ShotWithMenu(Canvas canvas, Camera cam, string path)
        {
            var mode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = null;
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(960, 540, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t); rt.Release();
            canvas.renderMode = mode;
        }

        [UnityTest]
        public IEnumerator R38_AttractMenu_EscIgnored_ButtonsReactToMouse()
        {
            Directory.CreateDirectory(Out);
            AttractMode.AttractOnLoad = true;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var attract = boot.attract;
            Assert.IsTrue(attract.Active);
            var menu = attract.menu.GetComponent<TitleMenu>();
            var pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);

            // Esc(일시정지 토글)를 눌러도 시작화면에서는 일시정지 안 함
            pause.Toggle();
            yield return null;
            Assert.IsFalse(pause.IsPaused, "시작화면 메뉴에서 Esc 무시");
            Assert.IsFalse(pause.Panel.gameObject.activeSelf, "일시정지 패널 안 뜸");
            Assert.AreEqual(1f, Time.timeScale, "시간 그대로");

            var canvas = attract.menu.GetComponent<Canvas>();
            ShotWithMenu(canvas, attract.AttractCamera, $"{Out}/menu_idle.png");
            yield return CheckButton(menu.StartButton, "시작");
            ExecuteEvents.Execute(menu.SettingsButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            yield return Wait(0.35f);
            ShotWithMenu(canvas, attract.AttractCamera, $"{Out}/menu_hover_settings.png");
            ExecuteEvents.Execute(menu.SettingsButton.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            yield return CheckButton(menu.SettingsButton, "설정");
            yield return CheckButton(menu.QuitButton, "종료");
            menu.OpenSettings();
            yield return CheckButton(menu.BackButton, "돌아가기");
            menu.CloseSettings();

            // 시작을 눌러 시작선까지 옮겨 가는 중에도 Esc 무시
            menu.StartButton.onClick.Invoke();
            Assert.IsTrue(attract.Leading);
            pause.Toggle();
            yield return null;
            Assert.IsFalse(pause.IsPaused, "시작선까지 옮겨 가는 중에도 Esc 무시");
            Assert.IsFalse(pause.PauseQueued, "기다렸다 일시정지하지도 않음");
        }

        [UnityTest]
        public IEnumerator R38_PauseAndResultButtons_ReactToMouse()
        {
            AttractMode.AttractOnLoad = false;
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            yield return null;
            var pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            pause.Pause();
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale, "일시정지 = 시간 멈춤에서도 반응해야 함");
            yield return CheckButton(pause.ContinueButton, "계속");
            yield return CheckButton(pause.TitleButton, "타이틀로");
            pause.Resume();
            var result = Object.FindAnyObjectByType<ResultScreen>(FindObjectsInactive.Include);
            var rb = result.GetComponentsInChildren<Button>(true);
            Assert.Greater(rb.Length, 1, "결과 화면 버튼");
            foreach (var b in rb) Assert.IsNotNull(b.GetComponent<UiHover>(), $"결과 버튼 {b.name}에 마우스 반응");
        }
    }
}
