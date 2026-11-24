using UnityEngine;

namespace RhythmActionGame.EditorTools
{
    public static partial class SceneBuilder
    {
        /// <summary>
        /// 작업 R36: 1챕터 시작선 뒤 진입로 — 길(경로 처음 방향을 뒤로 곧게 늘림)과 양옆 집 줄. 시작화면(AttractMode)에서 주인공이 여기를 달린다.
        /// 땅은 맵 전체 바닥(BuildGround)이 이미 덮는다.
        /// </summary>
        static void BuildApproach(Transform map)
        {
            var root = new GameObject("Approach").transform;
            root.SetParent(map, false);
            var p0 = AttractMode.ApproachPoint(0f, out var fwd);
            var right = MapLayout.RightOf(fwd);
            var pathMat = Mat("Path", new Color(0.8f, 0.74f, 0.63f));
            float L = GameConfig.Attract.BuildLength;
            var road = Box(root, "ApproachPath", p0 - fwd * (L * 0.5f) + Vector3.up * 0.02f, new Vector3(GameConfig.PathWidth, 0.04f, L), Quaternion.LookRotation(fwd, Vector3.up), pathMat);
            road.isStatic = true;
            // 길가 집: 긴 축을 길 방향으로, 양옆에 번갈아 기와집·초가.
            // 작업 R37: 집 두 채(TilePeriod = 집 간격 × 2)마다 똑같이 되풀이 — 시작화면이 TilePeriod만큼 뒤로 옮겨도 화면이 같다
            string[][] mods = { new[] { "giwa_s", "choga" }, new[] { "choga_L", "giwa_long" } };
            var rot = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
            int k = 0;
            foreach (float side in new[] { -1f, 1f })
                for (int j = 0; -GameConfig.Attract.HouseSpacing * (j + 0.5f) > -L; j++, k++)
                {
                    float z = -GameConfig.Attract.HouseSpacing * (j + 0.5f);
                    string id = mods[side > 0 ? 1 : 0][j % 2];
                    var w = Wrap(id, root, $"ApproachHouse_{k}");
                    var sz = FitModule(w, id);
                    if (sz.x > GameConfig.Attract.HouseSpacing - 0.6f)
                    {
                        float s = (GameConfig.Attract.HouseSpacing - 0.6f) / sz.x;
                        var ls = w.Go.transform.localScale; ls.x *= s; w.Go.transform.localScale = ls;
                    }
                    w.Go.transform.SetPositionAndRotation(p0 + fwd * z + right * side * (GameConfig.Attract.RoadHalf + sz.z * 0.5f), rot);
                }
        }

        /// <summary>작업 R36: 게임 씬 안 시작화면 — 메뉴 캔버스(제목·시작·설정·종료, 평소 꺼 둠)와 AttractMode.</summary>
        static void BuildAttract(GameBootstrap boot)
        {
            var canvas = UiFactory.CreateCanvas("TitleCanvas", GameConfig.MenuCanvasOrder + 10);
            var title = UiFactory.CreateText(canvas.transform, "Title", "Rhythm-Action-Game", 96, Color.white, new Vector2(1600, 160));
            title.rectTransform.anchoredPosition = new Vector2(0, 220);
            var outline = title.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            outline.effectDistance = new Vector2(3f, -3f);
            var credit = UiFactory.CreateText(canvas.transform, "MusicCredit", GameConfig.Music.Credit, 15, new Color(1f, 1f, 1f, 0.6f), new Vector2(1600, 28));
            credit.rectTransform.anchorMin = credit.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            credit.rectTransform.anchoredPosition = new Vector2(0, 24);
            canvas.gameObject.AddComponent<TitleMenu>();
            canvas.gameObject.SetActive(false);

            var attract = boot.gameObject.AddComponent<AttractMode>();
            attract.boot = boot;
            attract.body = boot.player.GetComponentInChildren<PlayerBody>(true) ?? Object.FindAnyObjectByType<PlayerBody>();
            attract.menu = canvas.gameObject;
            var hud = GameObject.Find("HudCanvas");
            attract.hideDuringAttract = hud != null ? new[] { hud } : new GameObject[0];
            var bd = GameObject.Find("Backdrop");
            if (bd != null) attract.backdrop = bd.GetComponent<Renderer>();
            boot.attract = attract;
        }
    }
}
