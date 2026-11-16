using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 맵 모델·분위기와 드로우콜 줄이기 전후 (작업 T15).
    /// 드로우콜: 같은 시점 6곳(챕터마다 1인칭 눈높이)에서 렌더 통계를 잰다.
    /// 전: Game_NoBatch(정적 플래그 없음) + SRP Batcher 끔 + GPU 인스턴싱 끔 / 뒤: Game(정적 배칭) + SRP Batcher + 인스턴싱.
    /// 결과를 harness/reports/drawcalls.md에 쓴다.
    /// </summary>
    public class T15_DrawCallTests
    {
        static readonly int[] Beats = { 24, 84, 140, 186, 224, 258 };
        const string NoBatchScene = "Assets/Scenes/Test/Game_NoBatch.unity";

        /// <summary>Unity 6의 그리기 호출 카운터 (종류별). 합계 = 그리기 호출.</summary>
        static readonly string[] DrawCounters = { "Standard Draw Calls Count", "Standard Instanced Draw Calls Count", "SRP Batcher Draw Calls Count", "BRG Draw Calls Count" };

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            SetInstancing(true);
        }

        static void SetInstancing(bool on)
        {
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.shader != null && m.shader.name == "RhythmActionGame/Toon") m.enableInstancing = on;
        }

        /// <summary>시점마다 [그리기 호출 합, SetPass, 정적 배치 수, SRP Batcher 그리기 호출].</summary>
        IEnumerator Measure(long[,] into)
        {
            yield return null;
            var boot = GameBootstrap.Instance;
            if (boot != null) boot.judge.readKeyboard = false;
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            cam.transform.SetParent(null, true);
            foreach (var b in Object.FindObjectsByType<PlayerBody>(FindObjectsSortMode.None)) b.enabled = false;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;                                   // 배치 모드에서도 그리도록 화면 밖 텍스처에
            var draws = DrawCounters.Select(n => ProfilerRecorder.StartNew(ProfilerCategory.Render, n)).ToList();
            var setp = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var stat = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Static Batches Count");
            try
            {
                for (int i = 0; i < Beats.Length; i++)
                {
                    var p = MapLayout.PointAt(Beats[i] * GameConfig.MetersPerBeat, out var fwd);
                    cam.transform.SetPositionAndRotation(p + Vector3.up * GameConfig.EyeHeight, Quaternion.LookRotation(fwd + Vector3.down * 0.05f));
                    for (int f = 0; f < 3; f++) { cam.Render(); yield return null; }
                    into[i, 0] = draws.Where(r => r.Valid).Sum(r => r.LastValue);
                    into[i, 1] = setp.Valid ? setp.LastValue : -1;
                    into[i, 2] = stat.Valid ? stat.LastValue : -1;
                    into[i, 3] = draws[2].Valid ? draws[2].LastValue : -1;
                    Debug.Log($"[T15] {Beats[i]}박: " + string.Join(", ", DrawCounters.Select((n, k) => $"{n}={(draws[k].Valid ? draws[k].LastValue : -1)}")) + $", SetPass={into[i, 1]}, Static Batches={into[i, 2]}");
                }
            }
            finally
            {
                foreach (var r in draws) r.Dispose();
                setp.Dispose(); stat.Dispose();
                cam.targetTexture = null;
                Object.Destroy(rt);
            }
        }

        [UnityTest]
        public IEnumerator T15_DrawCalls_BeforeAfter_Recorded_AndReduced()
        {
            var before = new long[Beats.Length, 4];
            var after = new long[Beats.Length, 4];
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(NoBatchScene, new LoadSceneParameters(LoadSceneMode.Single));
            GraphicsSettings.useScriptableRenderPipelineBatching = false;
            SetInstancing(false);
            yield return Measure(before);
            GraphicsSettings.useScriptableRenderPipelineBatching = true;
            SetInstancing(true);
#endif
            yield return SceneManager.LoadSceneAsync("Game");
            yield return Measure(after);

            var sb = new StringBuilder("# 드로우콜 (작업 T15)\n\n");
            sb.Append("같은 시점 6곳(챕터마다 1인칭 눈높이, 1280×720 화면 밖 렌더)의 렌더 통계. PlayMode 테스트 T15_DrawCallTests가 잼 (에디터).\n");
            sb.Append("Unity 6 카운터: 그리기 호출 = Standard + Standard Instanced + SRP Batcher + BRG Draw Calls (그림자 포함 한 프레임).\n");
            sb.Append("- 전: 정적 플래그 없는 같은 씬(Game_NoBatch) + SRP Batcher 끔 + GPU 인스턴싱 끔\n");
            sb.Append("- 뒤: Game 씬 — 맵 정적 배칭 + SRP Batcher(툰 셰이더 UnityPerMaterial) + GPU 인스턴싱, 같은 에셋은 머티리얼 하나 공유\n\n");
            sb.Append("| 시점(박) | 챕터 | 전 그리기 호출 | 전 SetPass | 뒤 그리기 호출 | 뒤 SetPass | 뒤 정적 배치 | 뒤 SRP Batcher 호출 |\n|---|---|---|---|---|---|---|---|\n");
            var sum = new long[8];
            for (int i = 0; i < Beats.Length; i++)
            {
                sb.Append($"| {Beats[i]} | {MapLayout.SectionOf(Beats[i]).Number} | {before[i, 0]} | {before[i, 1]} | {after[i, 0]} | {after[i, 1]} | {after[i, 2]} | {after[i, 3]} |\n");
                for (int k = 0; k < 4; k++) { sum[k] += before[i, k]; sum[k + 4] += after[i, k]; }
            }
            int n = Beats.Length;
            sb.Append($"| 평균 | | {sum[0] / n} | {sum[1] / n} | {sum[4] / n} | {sum[5] / n} | {sum[6] / n} | {sum[7] / n} |\n");
            Directory.CreateDirectory("harness/reports");
            File.WriteAllText("harness/reports/drawcalls.md", sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[T15] " + sb.ToString().Replace("\n", " / "));

            Assert.Greater(sum[0], 0, "전 그리기 호출을 잼");
            Assert.Greater(sum[4], 0, "뒤 그리기 호출을 잼");
            Assert.Less(sum[5], sum[1], "SetPass가 줄어듦");
            Assert.LessOrEqual(sum[4], sum[0], "그리기 호출이 늘지 않음");
        }

        [UnityTest]
        public IEnumerator T15_Map_ModelsReplaceGrayBoxes_Atmosphere()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            // 회색 상자(지붕 길·담·장애물)는 충돌만 남기고 그리지 않는다. 같은 자리에 모델.
            var boxes = all.Where(t => (t.name.StartsWith("Prop_house_giwa") || t.name.StartsWith("Prop_wall_stone") || t.name.StartsWith("Prop_gate_")
                || t.name == "Prop_cart" || t.name == "Prop_jars" || t.name.StartsWith("AlleyWall")) && t.GetComponent<BoxCollider>() != null).ToList();
            Assert.Greater(boxes.Count, 10);
            foreach (var b in boxes)
            {
                Assert.IsFalse(b.GetComponent<MeshRenderer>().enabled, $"{b.name}: 상자는 그리지 않음");
                Assert.IsTrue(b.GetComponent<BoxCollider>().enabled, $"{b.name}: 충돌은 유지");
                Assert.IsTrue(all.Any(t => t.name.StartsWith("Model_") && t.name.Contains("_" + b.name + "_")), $"{b.name}: 모델이 대신 보임");
            }
            // 챕터마다 꾸미기
            foreach (var s in MapLayout.Sections)
                Assert.Greater(all.Count(t => t.name.StartsWith($"Dress_{s.Number}_")), 2, $"{s.Number}구간 장식");
            // 외곽 그림·하늘·해 질 녘·안개
            Assert.IsNotNull(GameObject.Find("Backdrop"), "먼 배경 그림 원통");
            Assert.IsTrue(RenderSettings.fog, "안개");
            Assert.IsNotNull(RenderSettings.skybox, "하늘");
            var sun = RenderSettings.sun;
            Assert.IsNotNull(sun);
            Assert.Less(sun.transform.eulerAngles.x, 25f, "해 질 녘: 낮은 해");
            Assert.Greater(sun.color.r, sun.color.b + 0.3f, "해 질 녘: 따뜻한 빛");
        }
    }
}
