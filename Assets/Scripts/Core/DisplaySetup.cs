using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 빌드 실행 시작 때 FullHD(GameConfig.Display) 전체 화면 창으로 맞춘다 — 예전에 실행한 해상도가 레지스트리에 남아 있어도.
    /// 에디터와 배치 모드(화면 없음)에서는 하지 않는다.
    /// </summary>
    public static class DisplaySetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            Screen.SetResolution(GameConfig.Display.Width, GameConfig.Display.Height, FullScreenMode.FullScreenWindow);
        }
    }
}
