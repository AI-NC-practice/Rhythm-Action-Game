using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R19: 204박 닫히는 대문. 원인 = 회피 슬로모션의 대문 3인칭 컷(BulletTimeFx, GDD-beta "문 건너편 앞에서 미끄러져 들어오는 주인공")에서
    /// 슬로(0.3배) 동안 미끄러지기 클립 앞 10% 남짓(선 자세)만 재생돼 "문 안에 서 있는 내 몸"으로 보였다.
    /// - 컷 동안 몸이 미끄러지는 낮은 자세(머리가 몸 받침 기준 1.15 m 아래, 선 자세 약 1.6 m).
    /// - 1인칭 프레임(컷 밖)에서 머리 없는 몸 전체(머리·엉덩이·두 발)가 카메라 1.2 m 앞 화면 안에 보이는 프레임 0 (R06 방식: 내 몸만 그려 가림 비율도 기록).
    /// - PERFECT·NORMAL·FAIL 1인칭(컷이면 컷 화면) 미리보기 PNG → harness/reports/R19/.
    /// 작업 R20·R23 미리보기: 평지·지붕 위·궁궐 1인칭 PNG → harness/reports/R20/ (전 = harness/reports/review/fp_*.jpg).
    /// </summary>
    public class R19_GateBodyPlayTests
    {
        const string OutDir = "harness/reports/R19";
        const int BodyLayer = 31;
        double now;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;
        Camera view;
        Camera probe;
        RenderTexture rt;
        Texture2D tex;

        [TearDown]
        public void TearDown()
        {
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            if (rt != null) rt.Release();
            Time.timeScale = 1f;
        }

        IEnumerator Load(double startBeat)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            body = Object.FindAnyObjectByType<PlayerBody>();
            now = 0;
            boot.clock.TimeSource = () => now;
            float realTime = Time.realtimeSinceStartup;
            dir.slowMotion.RealTime = () => realTime + (float)now;
            foreach (var fx in Object.FindObjectsByType<TimeFx>(FindObjectsSortMode.None)) fx.RealTime = () => realTime + (float)now;
            foreach (var cf in Object.FindObjectsByType<CameraFx>(FindObjectsSortMode.None)) cf.DeltaTime = () => 0.01f;
            if (boot.bulletTime != null) boot.bulletTime.DeltaTime = () => 0.01f;   // 회피 슬로모션(대문 컷)은 켠 채로, 실제 시간 대신 프레임마다 0.01초
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;
            view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            var pcGo = new GameObject("R19ProbeCam");
            probe = pcGo.AddComponent<Camera>();
            probe.enabled = false;
            probe.clearFlags = CameraClearFlags.SolidColor;
            probe.backgroundColor = new Color(1f, 0f, 1f, 1f);
            probe.cullingMask = 1 << BodyLayer;
            rt = new RenderTexture(192, 108, 24);
            tex = new Texture2D(192, 108, TextureFormat.RGBA32, false);
            probe.targetTexture = rt;
            body.VisibleBody.gameObject.layer = BodyLayer;
        }

        /// <summary>자동 연주 한 프레임. delay: 0 → PERFECT, 0.15 → NORMAL, null → 입력 없음(FAIL). 대문 판정에만 delay를 쓰고 나머지는 PERFECT.</summary>
        IEnumerator Step(double gateBeat, double? delay)
        {
            now += 0.01;
            foreach (var c in boot.judge.Cues.ToArray())
            {
                if (c.Judged || !c.Appeared) continue;
                bool gate = c.Tag is StageDirector.ObstacleRun r && r.Event != null && r.Event.kind == "closing_gate";   // 원 박은 시계 박이라 사건 종류로 찾는다
                if (gate && !delay.HasValue) continue;
                double d = gate ? delay.Value : 0.0;
                if (now >= boot.clock.TimeOfBeat(c.TargetBeat) + d - 1e-6) { boot.judge.Submit(c.Key, now); break; }
            }
            if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
            yield return null;
        }

        float BodyCover()
        {
            probe.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
            probe.fieldOfView = view.fieldOfView;
            probe.nearClipPlane = view.nearClipPlane;
            probe.aspect = (float)rt.width / rt.height;
            probe.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = prev;
            int hit = 0;
            var px = tex.GetPixels32();
            foreach (var c in px) if (!(c.r > 240 && c.g < 15 && c.b > 240)) hit++;
            return hit / (float)px.Length;
        }

        /// <summary>몸 전체(머리·엉덩이·두 발)가 화면 안이고 엉덩이가 카메라에서 1.2 m 넘게 앞 = "내 몸이 화면 앞에 서 있음".</summary>
        bool WholeBodyInFront()
        {
            if (!body.VisibleBody.enabled) return false;
            var a = body.animator;
            foreach (var b in new[] { HumanBodyBones.Head, HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                var t = a.GetBoneTransform(b);
                if (t == null) return false;
                var vp = view.WorldToViewportPoint(t.position);
                if (vp.z <= view.nearClipPlane || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) return false;
            }
            var hips = a.GetBoneTransform(HumanBodyBones.Hips).position - view.transform.position;
            return Vector3.Dot(hips, view.transform.forward) > 1.2f;
        }

        static void Shot(Camera cam, string path)
        {
            var big = new RenderTexture(640, 360, 24);
            var old = cam.targetTexture;
            cam.targetTexture = big;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = big;
            var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            RenderTexture.active = prev;
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.Destroy(t);
            big.Release();
        }

        [UnityTest]
        public IEnumerator R19_ClosingGate_NoStandingBody_LowSlideInCut_Previews()
        {
            Directory.CreateDirectory(OutDir);
            var ev = dirChart().events.First(e => e.kind == "closing_gate");
            double gateBeat = ev.runBeat + EventCatalog.Get("closing_gate").Slots[0].Offset;   // 달리기 박 (204)
            var lines = new List<string>();
            foreach (var (tag, delay) in new (string, double?)[] { ("PERFECT", 0.0), ("NORMAL", 0.15), ("FAIL", null) })
            {
                yield return Load(gateBeat - 8);
                int frames = 0, frontFrames = 0, cutFrames = 0, lowCutFrames = 0;
                float maxCover = 0f, maxCutHead = 0f;
                bool shotA = false, shotB = false;
                while (boot.player.VisualRunBeat < gateBeat + 4 && frames < 4000)
                {
                    yield return Step(gateBeat, delay);
                    frames++;
                    double v = boot.player.VisualRunBeat;
                    if (v < gateBeat - 2) continue;
                    bool cut = boot.bulletTime != null && boot.bulletTime.GateCut;
                    if (cut)
                    {
                        cutFrames++;
                        var head = body.animator.GetBoneTransform(HumanBodyBones.Head).position;
                        float h = body.bodyOffset.InverseTransformPoint(head).y;
                        maxCutHead = Mathf.Max(maxCutHead, h);
                        if (h < 1.15f) lowCutFrames++;
                    }
                    else
                    {
                        maxCover = Mathf.Max(maxCover, BodyCover());
                        if (WholeBodyInFront()) frontFrames++;
                    }
                    if (!shotA && v >= gateBeat + 0.05) { shotA = true; Shot(view, $"{OutDir}/{tag}_a.png"); }
                    if (!shotB && v >= gateBeat + 1.0) { shotB = true; Shot(view, $"{OutDir}/{tag}_b.png"); }
                }
                string line = $"{tag}: frames {frames}, 1인칭 몸 전체 앞 {frontFrames}, 가림 최대 {maxCover:0.000}, 컷 {cutFrames}(낮은 자세 {lowCutFrames}, 머리 최대 {maxCutHead:0.00} m), 낮은 자세 시각 {body.LowPoseNormalizedTime:0.00}";
                lines.Add(line);
                Debug.Log("[R19] " + line);
                Assert.AreEqual(0, frontFrames, $"{tag}: 1인칭 화면 앞에 내 몸 전체가 보이는 프레임");
                if (tag != "FAIL")
                {
                    Assert.Greater(cutFrames, 0, $"{tag}: 대문 3인칭 컷 (회피 슬로모션)");
                    Assert.GreaterOrEqual(body.LowPoseNormalizedTime, 0f, $"{tag}: 컷 시작에 낮은 자세로 넘김");
                    Assert.AreEqual(cutFrames, lowCutFrames, $"{tag}: 컷 동안 머리가 몸 받침 기준 1.15 m 아래 (선 자세 약 1.6 m) (미끄러짐·숙임) — 최대 {maxCutHead:0.00}");
                }
                else Assert.AreEqual(0, cutFrames, "FAIL은 컷 없음");
                Assert.IsTrue(File.Exists($"{OutDir}/{tag}_a.png") && File.Exists($"{OutDir}/{tag}_b.png"), "미리보기");
            }
            File.WriteAllLines($"{OutDir}/summary.txt", lines);
        }

        ChartData dirChart() => dir != null ? dir.Chart : StageChart.Load();

        /// <summary>작업 R20·R23: 평지·지붕 위·궁궐 1인칭 미리보기 (하늘 끝 띠·길 가장자리 검은 선 확인용).</summary>
        [UnityTest]
        public IEnumerator R20_R23_FirstPersonPreviews_FlatRoofPalace()
        {
            string outDir = "harness/reports/R20";
            Directory.CreateDirectory(outDir);
            var spots = new (string name, double beat)[] { ("flat_012", 12), ("flat_196", 196), ("flat_200", 200), ("roof_116", 116), ("roof_140", 140), ("palace_216", 216), ("palace_236", 236) };
            foreach (var (name, beat) in spots)
            {
                yield return Load(beat - 3);
                int frames = 0;
                while (boot.player.VisualRunBeat < beat && frames < 2000) { yield return Step(-999, 0.0); frames++; }
                Shot(view, $"{outDir}/{name}.png");
                Assert.IsTrue(File.Exists($"{outDir}/{name}.png"), name);
            }
            // 배경 원통: 위쪽은 투명하게 녹아 하늘 위에 겹침 (R20), 길·땅은 외곽선 패스 끔 (R23)
            var bd = GameObject.Find("Backdrop").GetComponent<MeshRenderer>().sharedMaterial;
            Assert.Greater(bd.renderQueue, 2500, "배경은 하늘(스카이박스) 뒤에 그림 (투명 큐)");
            var pieces = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r => r.name == "Ground" || r.name.StartsWith("Path_")).ToList();
            Assert.Greater(pieces.Count, 10, "땅 + 길 조각");
            foreach (var r in pieces)
                Assert.IsFalse(r.sharedMaterial.GetShaderPassEnabled("SRPDefaultUnlit"), $"{r.name}: 툰 외곽선 패스 끔");
        }

        /// <summary>
        /// 작업 R23 전후 비교: 같은 프레임을 땅·길 외곽선 패스를 켜고(전) 끄고(후) 두 번 찍는다 → harness/reports/R23/{자리}_before|after.png.
        /// 화면 아래 절반의 거의 검은 픽셀 수(검은 사선)가 줄고, 길 가운데와 옆 바닥의 밝기 차이(길 구분)는 남는다.
        /// </summary>
        [UnityTest]
        public IEnumerator R23_PathEdgeLines_BeforeAfter_SameFrame()
        {
            string outDir = "harness/reports/R23";
            Directory.CreateDirectory(outDir);
            var lines = new List<string>();
            foreach (var (name, beat) in new (string, double)[] { ("flat_012", 12), ("flat_020", 20), ("flat_200", 200), ("palace_216", 216) })
            {
                yield return Load(beat - 3);
                int frames = 0;
                while (boot.player.VisualRunBeat < beat && frames < 2000) { yield return Step(-999, 0.0); frames++; }
                var mats = new[] { GameObject.Find("Ground"), GameObject.Find("Path_0") }.Select(g => g.GetComponent<MeshRenderer>().sharedMaterial).Distinct().ToList();
                foreach (var m in mats) m.SetShaderPassEnabled("SRPDefaultUnlit", true);
                var before = Capture(view);
                foreach (var m in mats) m.SetShaderPassEnabled("SRPDefaultUnlit", false);
                var after = Capture(view);
                File.WriteAllBytes($"{outDir}/{name}_before.png", before.EncodeToPNG());
                File.WriteAllBytes($"{outDir}/{name}_after.png", after.EncodeToPNG());
                LineDiff(before, after, out int removed, out int added);
                float contrast = PathContrast(after);
                string line = $"{name}: 외곽선을 끄며 사라진 검은 픽셀 {removed}, 새로 생긴 검은 픽셀 {added}, 길-바닥 밝기 차이(후, 6 m 앞) {contrast:0.000}";
                lines.Add(line);
                Debug.Log("[R23] " + line);
                Assert.Greater(removed, 100, $"{name}: 전 사진에 길 가장자리 검은 선이 있었고 후에는 사라짐");
                Assert.Less(added, 20, $"{name}: 끈 뒤 새 검은 선 없음");
                Assert.Greater(contrast, 0.04f, $"{name}: 길이 바닥과 구분됨");
                Object.Destroy(before); Object.Destroy(after);
            }
            File.WriteAllLines($"{outDir}/summary.txt", lines);
        }

        /// <summary>
        /// 작업 R20 전후 비교: 같은 프레임을 배경 원통 위쪽이 안개 색(예전, _TopToFog 1)과 투명(지금, 0)으로 두 번 찍는다 → harness/reports/R20/{자리}_before|after.png.
        /// 화면 위쪽 절반에서 안개 색과 거의 같은 픽셀(휜 띠)이 줄어든다.
        /// </summary>
        [UnityTest]
        public IEnumerator R20_SkyBand_BeforeAfter_SameFrame()
        {
            string outDir = "harness/reports/R20";
            Directory.CreateDirectory(outDir);
            var lines = new List<string>();
            foreach (var (name, beat) in new (string, double)[] { ("flat_196", 196), ("roof_116", 116), ("roof_140", 140), ("palace_236", 236) })
            {
                yield return Load(beat - 3);
                int frames = 0;
                while (boot.player.VisualRunBeat < beat && frames < 2000) { yield return Step(-999, 0.0); frames++; }
                var bd = GameObject.Find("Backdrop").GetComponent<MeshRenderer>().sharedMaterial;
                var fog = bd.GetColor("_FogColor");
                bd.SetFloat("_TopToFog", 1f);
                var before = Capture(view);
                bd.SetFloat("_TopToFog", 0f);
                var after = Capture(view);
                File.WriteAllBytes($"{outDir}/{name}_before.png", before.EncodeToPNG());
                File.WriteAllBytes($"{outDir}/{name}_after.png", after.EncodeToPNG());
                int fb = FogPixels(before, fog), fa = FogPixels(after, fog);
                int changed = Changed(before, after);
                string line = $"{name}: 위쪽 절반에서 바뀐 픽셀(예전 띠 자리) {changed}, 안개 색에 가까운 픽셀 전 {fb} → 후 {fa}";
                lines.Add(line);
                Debug.Log("[R20] " + line);
                Assert.Greater(changed, 500, $"{name}: 예전 모습에는 위쪽에 안개 색 띠가 있었고 지금은 하늘로 바뀜");
                Assert.LessOrEqual(fa, fb, $"{name}: 안개 색 띠 픽셀이 늘지 않음");
                Object.Destroy(before); Object.Destroy(after);
            }
            File.WriteAllLines($"{outDir}/summary.txt", lines);
        }

        /// <summary>위쪽 절반에서 RGB 차이 합이 0.1 넘게 바뀐 픽셀 수.</summary>
        static int Changed(Texture2D a, Texture2D b)
        {
            var pa = a.GetPixels(); var pb = b.GetPixels();
            int n = 0;
            for (int i = pa.Length / 2; i < pa.Length; i++)
                if (Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b) > 0.1f) n++;
            return n;
        }

        /// <summary>화면 위쪽 절반에서 안개 색(감마)과 RGB 각 0.03 안인 픽셀 수.</summary>
        static int FogPixels(Texture2D t, Color fogLinearOrGamma)
        {
            var f = fogLinearOrGamma;
            int n = 0;
            var px = t.GetPixels();
            for (int y = t.height / 2; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                {
                    var c = px[y * t.width + x];
                    if (Mathf.Abs(c.r - f.r) < 0.03f && Mathf.Abs(c.g - f.g) < 0.03f && Mathf.Abs(c.b - f.b) < 0.03f) n++;
                }
            return n;
        }

        static Texture2D Capture(Camera cam)
        {
            var big = new RenderTexture(640, 360, 24);
            var old = cam.targetTexture;
            cam.targetTexture = big;
            cam.Render();
            cam.targetTexture = old;
            var prev = RenderTexture.active;
            RenderTexture.active = big;
            var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            t.Apply();
            RenderTexture.active = prev;
            big.Release();
            return t;
        }

        static bool Dark(Color32 c) => c.r < 45 && c.g < 45 && c.b < 45;

        /// <summary>같은 프레임 전후: 전에만 검은 픽셀(사라진 선)과 후에만 검은 픽셀 수.</summary>
        static void LineDiff(Texture2D before, Texture2D after, out int removed, out int added)
        {
            removed = 0; added = 0;
            var a = before.GetPixels32(); var b = after.GetPixels32();
            for (int i = 0; i < a.Length; i++)
            {
                if (Dark(a[i]) && !Dark(b[i])) removed++;
                if (!Dark(a[i]) && Dark(b[i])) added++;
            }
        }

        /// <summary>6 m 앞 길 가운데와 길 옆 바닥(가장자리 + 1.2 m)의 화면 밝기 차이 (3×3 평균).</summary>
        float PathContrast(Texture2D t)
        {
            float d = boot.player.VisualDistance + 6f;
            var c = MapLayout.PointAt(d, out var fwd);
            var right = MapLayout.RightOf(fwd);
            float L(Vector3 w)
            {
                var vp = view.WorldToViewportPoint(w);
                int x0 = Mathf.Clamp((int)(vp.x * t.width), 1, t.width - 2), y0 = Mathf.Clamp((int)(vp.y * t.height), 1, t.height - 2);
                float sum = 0f;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) { var p = t.GetPixel(x0 + dx, y0 + dy); sum += 0.299f * p.r + 0.587f * p.g + 0.114f * p.b; }
                return sum / 9f;
            }
            float side = GameConfig.PathWidth * 0.5f + 1.2f;
            return Mathf.Abs(L(c) - 0.5f * (L(c + right * side) + L(c - right * side)));
        }
    }
}
