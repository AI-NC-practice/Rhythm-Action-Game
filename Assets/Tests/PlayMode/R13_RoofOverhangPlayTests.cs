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
    /// 작업 R13 (사용자 피드백 2026-11-16 2번, 2026-11-24 8번): 지붕 길 위 아래로 피하기(136·232박)는 지붕에 올려놓은 낮은 처마(대문처럼 보임)가 아니라
    /// 길 옆 땅에 선 높은 건물(누각·궁궐 전각)의 처마 밑. 건물은 땅에 서 있고(공중 아님) 기둥·난간이 길을 막지 않으며, 땅 위 처마(80박)는 낮은 처마 그대로.
    /// 처마 밑 여유는 R41 측정(eave_136·eave_232). 미리보기(다가가는 1인칭) → harness/reports/R13/.
    /// </summary>
    public class R13_RoofOverhangPlayTests
    {
        const string Out = "harness/reports/R13";
        double now;
        GameBootstrap boot;
        StageDirector dir;

        [TearDown]
        public void TearDown() { if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        static Transform Obstacle(double beat)
        {
            var parent = GameObject.Find("Obstacles").transform;
            float d = (float)((beat + GameConfig.ObstacleBeats * 0.5) * GameConfig.MetersPerBeat);
            var p = MapLayout.PointAt(d);
            return parent.Cast<Transform>().Where(t => t.name.EndsWith("_eave")).OrderBy(t => (t.position - p).sqrMagnitude).First();
        }

        [UnityTest]
        public IEnumerator R13_RoofEaves_AreTallerBuildingOverhang_GroundEaveUnchanged()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            foreach (var beat in new[] { 136.0, 232.0 })
            {
                var o = Obstacle(beat);
                Assert.IsNull(o.Find("ModelPivot_eave_low"), $"{beat}박: 지붕 위에 낮은 처마 모델이 없음");
                var b = o.Cast<Transform>().FirstOrDefault(t => t.name.StartsWith("Overhang_"));
                Assert.IsNotNull(b, $"{beat}박: 옆 높은 건물");
                string id = b.name.Substring("Overhang_".Length);
                var rs = b.GetComponentsInChildren<Renderer>();
                var bnd = rs[0].bounds; foreach (var r in rs) bnd.Encapsulate(r.bounds);
                float groundY = 0f;
                Debug.Log($"[R13] {beat}박 {id}: 바닥 {bnd.min.y:0.00} m, 꼭대기 {bnd.max.y:0.00} m, 길 높이 {o.position.y:0.00} m, 옆 {o.InverseTransformPoint(b.position).x:0.0} m, 크기 {b.localScale.x:0.00}");
                Assert.Less(Mathf.Abs(bnd.min.y - groundY), 0.3f, $"{beat}박: 건물이 땅에 서 있음 (공중 아님)");
                Assert.Greater(bnd.max.y, o.position.y + GameConfig.Map.EaveUnderside, $"{beat}박: 지붕 길보다 높은 건물");
                var box = o.Find("Prop_eave");
                Assert.IsNotNull(box, "충돌 상자는 그대로");
                Assert.IsFalse(box.GetComponent<MeshRenderer>().enabled, "충돌 상자는 안 그림");
            }
            var ground = Obstacle(80);
            Assert.IsNotNull(ground.Find("ModelPivot_eave_low"), "80박(땅 위 장터)은 낮은 처마 그대로");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator R13_Preview_ApproachAndUnder()
        {
            Directory.CreateDirectory(Out);
            foreach (var beat in new[] { 136.0, 232.0 })
            {
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null;
                boot = GameBootstrap.Instance;
                dir = boot.director;
                now = 0;
                boot.clock.TimeSource = () => now;
                float realTime = Time.realtimeSinceStartup;
                dir.slowMotion.RealTime = () => realTime + (float)now;
                foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
                boot.judge.readKeyboard = false;
                boot.judge.OffsetSource = () => 0f;
                dir.AllowEventSlowMotion = false;
                boot.StartRun(beat - 4);
                yield return null;
                var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
                int shot = 0;
                while (boot.player.RunBeat < beat + 2.2)
                {
                    now += 0.01;
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                    if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                    yield return null;
                    double rb = boot.player.RunBeat;
                    string name = shot == 0 && rb >= beat - 1.5 ? "approach" : shot == 1 && rb >= beat + 1.0 ? "under" : null;
                    if (name == null) continue;
                    shot++;
                    var rt = new RenderTexture(960, 540, 24);
                    view.targetTexture = rt; view.Render(); view.targetTexture = null;
                    var prev = RenderTexture.active; RenderTexture.active = rt;
                    var t = new Texture2D(960, 540, TextureFormat.RGB24, false);
                    t.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                    RenderTexture.active = prev;
                    File.WriteAllBytes($"{Out}/eave_{beat}_{name}.png", t.EncodeToPNG());
                    Object.Destroy(t); rt.Release();
                }
                Assert.AreEqual(2, shot, $"{beat}박 미리보기 2장");
            }
        }
    }
}
