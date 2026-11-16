using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R04: 회색 상자 장애물·사건 소품을 R02 모델로. Game 씬에 그리는 회색 도형(기본 상자·공·캡슐·원통)이 남지 않음.
    /// 충돌 상자는 그대로 두고 그리기만 끈다. 땅과 길 띠는 지형이라 뺀다. 사건 소품 미리보기 PNG는 harness/reports/R04/.
    /// </summary>
    public class R04_NoGrayShapesPlayTests
    {
        static readonly string[] Primitives = { "Cube", "Sphere", "Capsule", "Cylinder" };

        static bool IsTerrain(Transform t) => t.name == "Ground" || t.name.StartsWith("Path_");

        [UnityTest]
        public IEnumerator R04_GameScene_NoVisibleGrayPrimitive()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var gray = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.enabled)   // 그림자만 그리는 것도 포함 (회색 막대 그림자도 보인다)
                .Where(r => { var mf = r.GetComponent<MeshFilter>(); return mf != null && mf.sharedMesh != null && Primitives.Contains(mf.sharedMesh.name); })
                .Where(r => !IsTerrain(r.transform))
                .Select(r => $"{r.name} < {(r.transform.parent ? r.transform.parent.name : "")}").ToList();
            Assert.IsEmpty(gray, "회색 도형이 그려짐: " + string.Join(", ", gray.Distinct()));

            // 장애물 충돌 상자는 그대로 (그리기만 끔), 같은 자리에 모델
            foreach (var name in new[] { "Prop_clothesline", "Prop_eave" })
            {
                var boxes = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(c => c.name == name).ToList();
                Assert.Greater(boxes.Count, 0, name);
                foreach (var b in boxes)
                {
                    Assert.IsTrue(b.enabled, $"{name}: 충돌 유지");
                    Assert.IsFalse(b.GetComponent<MeshRenderer>().enabled, $"{name}: 상자는 그리지 않음");
                    Assert.IsNotNull(b.transform.parent.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Model_")), $"{name}: 모델이 대신 보임");
                }
            }
            // 사건 소품 부품은 모델 (움직이는 부품 이름·개수 그대로)
            foreach (var prop in Object.FindObjectsByType<EventProp>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.parts.Length > 0))
                foreach (var part in prop.parts)
                    Assert.IsNotNull(part.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("Model_")), $"{prop.kind}: {part.name}는 모델");
            // 칼: 환도 모델 (1인칭 그림자만)
            var sword = GameObject.Find("Player").GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Sword");   // 작업 R27: 오른손 뼈 아래
            Assert.IsNotNull(sword);
            Assert.IsTrue(sword.GetComponentsInChildren<MeshRenderer>(true).All(r => r.GetComponent<MeshFilter>().sharedMesh.name != "Cube"), "칼은 모델");
        }

        static readonly (string kind, float back, float lateral, float up)[] Shots =
        {
            ("clothesline", 6f, 0f, 0f), ("eave", 6f, 0f, 0f), ("rolling_jars", 4f, 1.5f, 0f), ("awning_bounce", 6f, 1.5f, 0f), ("rope_swing", 7f, 0f, 0f),
            ("closing_gate", 8f, 0f, 0f), ("tile_slip", 5f, 0f, 0.5f), ("long_jump", 5f, 0f, 0.5f), ("banner_slide", 6f, -2f, 0f), ("arrow_volley", 5f, 0f, 0f),
        };

        [UnityTest]
        public IEnumerator R04_EventPropPreviews_Png()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            if (boot != null) boot.judge.readKeyboard = false;
            var cam = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            cam.transform.SetParent(null, true);
            foreach (var b in Object.FindObjectsByType<PlayerBody>(FindObjectsSortMode.None)) b.enabled = false;
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt;
            Directory.CreateDirectory("harness/reports/R04");
            var inst = Object.FindFirstObjectByType<DressingInstancer>();
            var chart = StageChart.Load();
            foreach (var (kind, back, lateral, up) in Shots)
            {
                var e = chart.events.FirstOrDefault(x => x.IsObstacle && x.kind == kind);
                if (e == null) continue;
                // 화살은 날아가는 중이어야 보인다: 부품을 켜서 찍는다
                if (kind == "arrow_volley")
                    foreach (var prop in Object.FindObjectsByType<EventProp>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.kind == kind))
                        for (int i = 0; i < prop.parts.Length; i++) { prop.parts[i].gameObject.SetActive(true); prop.parts[i].localPosition = new Vector3(-prop.slotSides[i] * 1.5f, 1.6f, prop.parts[i].localPosition.z); }
                float d = (e.runBeat + 2) * GameConfig.MetersPerBeat;
                var target = MapLayout.PointAt(d, out var fwd);
                var eye = MapLayout.PointAt(d - back) + MapLayout.RightOf(fwd) * lateral + Vector3.up * (GameConfig.EyeHeight + up);
                yield return null;
                cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target + Vector3.up * 1.2f - eye));
                if (inst != null) inst.Draw();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"harness/reports/R04/{kind}.png", tex.EncodeToPNG());
                Object.Destroy(tex);
            }
            cam.targetTexture = null;
            Object.Destroy(rt);
            Assert.IsTrue(File.Exists("harness/reports/R04/closing_gate.png"));
            Assert.IsTrue(File.Exists("harness/reports/R04/clothesline.png"));
        }
    }
}
