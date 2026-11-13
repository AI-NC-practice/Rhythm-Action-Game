using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RhythmActionGame.EditorTools
{
    /// <summary>타이틀 메뉴, HUD, 일시정지, 결과, 속도감 효과 (T08).</summary>
    public static partial class SceneBuilder
    {
        static void EventSystemObject()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static partial void BuildTitleUiExtra(Canvas canvas)
        {
            canvas.gameObject.AddComponent<TitleMenu>();
            EventSystemObject();
        }

        static partial void BuildGameExtra(GameBootstrap boot, ChartData chart)
        {
            var director = boot.director;
            var clock = boot.clock;

            var hudCanvas = UiFactory.CreateCanvas("HudCanvas", GameConfig.HudCanvasOrder);

            // 집중선 (화면 전체, HUD 뒤)
            var linesGo = new GameObject("SpeedLines", typeof(RectTransform), typeof(Image));
            linesGo.transform.SetParent(hudCanvas.transform, false);
            UiWidgets.Stretch((RectTransform)linesGo.transform);
            var lines = linesGo.GetComponent<Image>();
            lines.raycastTarget = false;
            lines.color = new Color(1, 1, 1, 0);
            var speedFx = hudCanvas.gameObject.AddComponent<SpeedFx>();
            speedFx.clock = clock;
            speedFx.judgeFx = boot.fx;
            speedFx.lines = lines;

            var keeper = hudCanvas.gameObject.AddComponent<ScoreKeeper>();
            keeper.director = director;
            keeper.clock = clock;
            var hud = hudCanvas.gameObject.AddComponent<GameHud>();
            hud.director = director;
            hud.clock = clock;
            hud.score = keeper;

            // 무사 자세 게이지 (GDD-beta B11.3)
            var posture = hudCanvas.gameObject.AddComponent<PostureBar>();
            posture.director = director;

            var menuCanvas = UiFactory.CreateCanvas("MenuCanvas", GameConfig.MenuCanvasOrder);
            var pause = menuCanvas.gameObject.AddComponent<PauseMenu>();
            pause.clock = clock;
            pause.director = director;
            pause.slowMotion = director.slowMotion;
            var result = menuCanvas.gameObject.AddComponent<ResultScreen>();
            result.director = director;
            result.score = keeper;
            result.clock = clock;

            boot.score = keeper;
            boot.hud = hud;
            boot.pause = pause;
            boot.result = result;
            boot.speedFx = speedFx;
            EventSystemObject();
        }
    }
}
