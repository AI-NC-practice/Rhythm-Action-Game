using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 작업 R06: 1인칭 카메라·동작 정리 (사용자 피드백 2·6·7번).
    /// 실제 PlayerCamera 자세(프레임 끝, 모든 LateUpdate 뒤)로 "내 몸만" 그려 화면 가림 비율을 재고,
    /// 머리를 뺀 목 단면(목 뼈)이 화면에 들어오는지, 카메라가 머리에서 얼마나 떨어지는지 잰다.
    /// 결과별 미리보기 PNG와 측정표: harness/reports/R06/.
    /// </summary>
    public class R06_FirstPersonViewPlayTests
    {
        const string OutDir = "harness/reports/R06";
        const int BodyLayer = 31;

        /// <summary>프레임 끝 측정기: 화면 카메라와 같은 자세로 몸 레이어만 작은 텍스처에 그린다.</summary>
        [DefaultExecutionOrder(32000)]
        class ViewProbe : MonoBehaviour
        {
            public Camera view;
            public PlayerBody body;
            public Camera probe;
            public RenderTexture rt;
            public Texture2D tex;
            public bool measuring;
            public string shotPath;
            public float lastCover;
            public bool lastNeckInView;
            public float lastHeadGap;

            void LateUpdate()
            {
                if (!measuring || view == null) return;
                probe.transform.SetPositionAndRotation(view.transform.position, view.transform.rotation);
                probe.fieldOfView = view.fieldOfView;
                probe.nearClipPlane = view.nearClipPlane;
                probe.aspect = (float)rt.width / rt.height;
                probe.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                RenderTexture.active = prev;
                var px = tex.GetPixels32();
                int hit = 0;
                foreach (var c in px) if (!(c.r > 240 && c.g < 15 && c.b > 240)) hit++;   // 지운 색(자홍)이 아닌 곳 = 내 몸
                lastCover = hit / (float)px.Length;

                var a = body.animator;
                var neck = a.GetBoneTransform(HumanBodyBones.Neck) ?? a.GetBoneTransform(HumanBodyBones.Head);
                var vp = view.WorldToViewportPoint(neck.position);
                lastNeckInView = body.VisibleBody.enabled && vp.z > view.nearClipPlane && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
                lastHeadGap = Vector3.Distance(view.transform.position, a.GetBoneTransform(HumanBodyBones.Head).position);

                if (shotPath != null)
                {
                    var big = new RenderTexture(640, 360, 24);
                    var old = view.targetTexture;
                    view.targetTexture = big;
                    view.Render();
                    view.targetTexture = old;
                    RenderTexture.active = big;
                    var t = new Texture2D(640, 360, TextureFormat.RGB24, false);
                    t.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                    RenderTexture.active = prev;
                    File.WriteAllBytes(shotPath, t.EncodeToPNG());
                    Object.Destroy(t);
                    big.Release();
                    shotPath = null;
                }
            }
        }

        class CaseResult
        {
            public string Name;
            public float MaxCover, MeanCover;
            public int NeckFrames, Frames;
            public float MaxHeadGap, MaxLean;
        }

        double now;
        GameBootstrap boot;
        StageDirector dir;
        PlayerBody body;
        ViewProbe probe;
        static readonly List<CaseResult> Results = new List<CaseResult>();

        [TearDown]
        public void TearDown()
        {
            if (probe != null) { probe.rt.Release(); Object.Destroy(probe.gameObject); }
            if (dir != null && dir.slowMotion != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        static ChartData Chart(params ChartEvent[] events) =>
            new ChartData { name = "test", bpm = 120, runBeats = 400, events = events };

        IEnumerator Load(ChartData chart, double startBeat = 0)
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
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            if (chart != null) dir.UseChart(chart);   // null = 실제 stage1 차트
            dir.AllowEventSlowMotion = false;
            boot.StartRun(startBeat);
            yield return null;

            var view = GameObject.Find("PlayerCamera").GetComponent<Camera>();
            var go = new GameObject("R06Probe");
            probe = go.AddComponent<ViewProbe>();
            probe.view = view;
            probe.body = body;
            probe.rt = new RenderTexture(192, 108, 24);
            probe.tex = new Texture2D(192, 108, TextureFormat.RGBA32, false);
            var pcGo = new GameObject("R06ProbeCam");
            pcGo.transform.SetParent(go.transform, false);
            probe.probe = pcGo.AddComponent<Camera>();
            probe.probe.enabled = false;
            probe.probe.clearFlags = CameraClearFlags.SolidColor;
            probe.probe.backgroundColor = new Color(1f, 0f, 1f, 1f);
            probe.probe.cullingMask = 1 << BodyLayer;
            probe.probe.targetTexture = probe.rt;
            body.VisibleBody.gameObject.layer = BodyLayer;
        }

        /// <summary>사건 하나를 지나며 측정. delay: 0 → PERFECT, 0.15 → NORMAL, null → FAIL.</summary>
        IEnumerator Run(string name, double eventBeat, double? delay)
        {
            double t0 = boot.clock.TimeOfBeat(eventBeat);
            double t1 = t0 + GameConfig.Body.ActionBeats * GameConfig.SecondsPerBeat;
            var res = new CaseResult { Name = name };
            var covers = new List<float>();
            bool shot30 = false, shot55 = false;
            while (now < t1 + 0.3)
            {
                now += 0.01;
                if (delay.HasValue)
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay.Value - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                bool inAction = now > t0 + (delay ?? 0.2) + 0.02 && now < t1 && body.ActionBlend > 0f;   // 판정이 몸에 전달된 뒤부터 (놓침 판정은 LateUpdate에서 나서 한 프레임 늦음)
                probe.measuring = inAction;
                double p = (now - t0) / (t1 - t0);
                if (inAction && !shot30 && p >= 0.3) { shot30 = true; probe.shotPath = $"{OutDir}/{name}_a.png"; }
                if (inAction && !shot55 && p >= 0.55) { shot55 = true; probe.shotPath = $"{OutDir}/{name}_b.png"; }
                yield return null;
                if (inAction)
                {
                    covers.Add(probe.lastCover);
                    res.Frames++;
                    if (probe.lastNeckInView) res.NeckFrames++;
                    res.MaxHeadGap = Mathf.Max(res.MaxHeadGap, probe.lastHeadGap);
                    res.MaxLean = Mathf.Max(res.MaxLean, Mathf.Abs(body.LeanDegrees));
                }
            }
            probe.measuring = false;
            res.MaxCover = covers.Count > 0 ? covers.Max() : 0f;
            res.MeanCover = covers.Count > 0 ? covers.Average() : 0f;
            Results.RemoveAll(x => x.Name == name);
            Results.Add(res);
            dir.Health.Reset();
            Debug.Log($"[R06] {name}: cover max {res.MaxCover:0.000} mean {res.MeanCover:0.000}, neck {res.NeckFrames}/{res.Frames}, headGap {res.MaxHeadGap:0.00}, lean {res.MaxLean:0.0}");
        }

        static readonly (string action, string kind, string key)[] Moves =
        {
            ("Vault", "low_wall", "up"), ("Climb", "roof_climb", "up"), ("Slide", "eave", "down"),
            ("Drop", "drop_down", "down"), ("Dodge", "jars", "right"), ("WallRun", "wall_run", "right"), ("WallRunL", "wall_run", "left"),
        };

        IEnumerator RunAll(string tag, double? delay)
        {
            var events = Moves.Select((m, i) => new ChartEvent { runBeat = 8 + i * 8, type = "obstacle", kind = m.kind, key = m.key }).ToArray();
            yield return Load(Chart(events));
            Directory.CreateDirectory(OutDir);
            for (int i = 0; i < Moves.Length; i++)
                yield return Run($"{Moves[i].action}_{tag}", 8 + i * 8, delay);
            WriteTable();
        }

        static void WriteTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# R06 1인칭 화면 측정");
            sb.AppendLine();
            sb.AppendLine("내 몸만 그린 화면 가림 비율(최대·평균), 목 단면(목 뼈)이 화면 안에 있던 프레임, 카메라–머리 뼈 최대 거리, 몸 기울기 최대.");
            sb.AppendLine();
            sb.AppendLine("| 경우 | 가림 최대 | 가림 평균 | 목 보임 | 카메라–머리 (m) | 기울기 (°) |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var r in Results.OrderBy(x => x.Name))
                sb.AppendLine($"| {r.Name} | {r.MaxCover:0.000} | {r.MeanCover:0.000} | {r.NeckFrames}/{r.Frames} | {r.MaxHeadGap:0.00} | {r.MaxLean:0.0} |");
            Directory.CreateDirectory(OutDir);
            File.WriteAllText($"{OutDir}/measure.md", sb.ToString(), new UTF8Encoding(false));
        }

        CaseResult Get(string name) => Results.First(x => x.Name == name);

        [UnityTest]
        public IEnumerator R06_Fail_HeadlessBodyNeverInFront()
        {
            yield return RunAll("Fail", null);
            foreach (var m in Moves)
            {
                var r = Get($"{m.action}_Fail");
                Assert.Greater(r.Frames, 50, r.Name);
                Assert.AreEqual(0, r.NeckFrames, $"{r.Name}: 머리 뺀 목 단면이 화면에 보임 ({r.NeckFrames}/{r.Frames})");
                Assert.LessOrEqual(r.MaxCover, 0.02f, $"{r.Name}: 실패 중 내 몸이 화면에 보임 (가림 {r.MaxCover:0.000})");
            }
            // 전투에서 맞음 (적 공격 FAIL)
            body.PlayOneShot(BodyMoves.Hit);
            var res = new CaseResult { Name = "Hit" };
            probe.measuring = true;
            double end = now + 1.2;
            while (now < end)
            {
                now += 0.01;
                if (res.Frames == 40) probe.shotPath = $"{OutDir}/Hit_a.png";
                yield return null;
                res.Frames++;
                if (probe.lastNeckInView) res.NeckFrames++;
                res.MaxCover = Mathf.Max(res.MaxCover, probe.lastCover);
                res.MaxHeadGap = Mathf.Max(res.MaxHeadGap, probe.lastHeadGap);
            }
            probe.measuring = false;
            Results.RemoveAll(x => x.Name == "Hit");
            Results.Add(res);
            WriteTable();
            Assert.AreEqual(0, res.NeckFrames, $"맞음: 목 단면이 화면에 보임 ({res.NeckFrames}/{res.Frames})");
            Assert.LessOrEqual(res.MaxCover, 0.02f, $"맞음: 내 몸이 화면에 보임 ({res.MaxCover:0.000})");
        }

        [UnityTest]
        public IEnumerator R06_Perfect_WallRunLeans45_SlideCoverCapped()
        {
            yield return RunAll("Perfect", 0.0);
            var w = Get("WallRun_Perfect");
            Assert.AreEqual(GameConfig.Body.WallRunLeanDegrees, w.MaxLean, 3f, "벽타기 몸 기울기 약 45°");
            Assert.AreEqual(GameConfig.Body.WallRunLeanDegrees, Get("WallRunL_Perfect").MaxLean, 3f, "왼쪽 벽도");
            Assert.Less(Get("Drop_Perfect").MaxCover, 0.3f, "앞구르기: 굴러 넘어오는 내 몸이 화면을 덮지 않음");
            var s = Get("Slide_Perfect");
            Assert.LessOrEqual(s.MaxCover, GameConfig.Body.SlideMaxScreenCover, $"미끄러지기 화면 가림 {s.MaxCover:0.000}");
            foreach (var m in Moves)
                Assert.AreEqual(0, Get($"{m.action}_Perfect").NeckFrames, $"{m.action}: 목 단면");
        }

        [UnityTest]
        public IEnumerator R06_Normal_WallRunLeans_SlideCoverCapped()
        {
            yield return RunAll("Normal", 0.15);
            Assert.AreEqual(GameConfig.Body.WallRunNormalLeanDegrees, Get("WallRun_Normal").MaxLean, 3f, "벽타기 NORMAL 기울기");
            var s = Get("Slide_Normal");
            Assert.LessOrEqual(s.MaxCover, GameConfig.Body.SlideMaxScreenCover, $"미끄러지기 NORMAL 화면 가림 {s.MaxCover:0.000}");
            foreach (var m in Moves)
                Assert.AreEqual(0, Get($"{m.action}_Normal").NeckFrames, $"{m.action}: 목 단면");
        }

        [UnityTest]
        public IEnumerator R06_WallRun_CameraTiltsWithBody_AwayFromWall()
        {
            yield return Load(Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "wall_run", key = "right" }));
            double mid = boot.clock.TimeOfBeat(8) + GameConfig.Body.ActionBeats * GameConfig.SecondsPerBeat * 0.5;
            while (now < mid)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
            }
            Assert.AreEqual(BodyMoves.State("WallRun", Judgement.Perfect), body.LastState);
            // 오른쪽 벽: 몸은 왼쪽(+z 회전)으로 눕고 발은 벽 쪽(+x), 카메라도 같은 쪽으로 기울어짐
            Assert.Greater(body.LeanDegrees, 40f, "몸이 벽 반대쪽으로 눕힘");
            Assert.Greater(body.bodyOffset.localPosition.x, 0.4f, "발이 벽 쪽으로");
            float roll = Mathf.DeltaAngle(0f, dir.motion.localEulerAngles.z);
            Assert.Greater(roll, GameConfig.Body.WallRunCameraRoll * 0.9f, $"카메라 기울기 {roll:0.0}°");
            var foot = body.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            var head = body.animator.GetBoneTransform(HumanBodyBones.Head).position;
            var right = body.transform.right;
            Assert.Greater(Vector3.Dot(foot - head, right), 0.8f, "발이 머리보다 벽 쪽 (몸이 벽을 밟음)");
            Assert.AreEqual(BodyMoves.RunClip, BodyMoves.ClipFor("WallRun", Judgement.Perfect), "옆걸음 대신 달리기 클립");

        }

        /// <summary>실제 stage1 벽타기(60박, 오른쪽 벽)에서 PERFECT: 발이 벽 쪽, 몸 약 45°, 3인칭·1인칭 미리보기.</summary>
        [UnityTest]
        public IEnumerator R06_WallRun_OnStageWall_Preview()
        {
            yield return Load(null, 54);
            while (boot.player.RunBeat < 60 + GameConfig.Body.ActionBeats * 0.5)   // 달리기 박 (곡 박과 다름)
            {
                now += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                if (dir.Health.Hp < GameConfig.MaxHp) dir.Health.Reset();
                yield return null;
            }
            Assert.AreEqual(BodyMoves.State("WallRun", Judgement.Perfect), body.LastState, "60박 벽타기 PERFECT");
            Assert.Greater(body.LeanDegrees, 40f);
            var foot = body.animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
            float footSide = Vector3.Dot(foot - body.transform.position, body.transform.right);
            Debug.Log($"[R06] stage wall run: lean {body.LeanDegrees:0.0}, foot side {footSide:0.00} m, wall foot offset {body.WallFootOffset:0.00} m");
            Assert.AreEqual(1.7f, body.WallFootOffset, 0.3f, "광선으로 잰 벽 안쪽 면 (벽 중심 2.0 m − 두께 절반)");
            Assert.Greater(footSide, 1.2f, "발이 벽 쪽 (벽 안쪽 면)");
            probe.shotPath = $"{OutDir}/WallRun_stage_fp.png";
            probe.measuring = true;
            yield return null;
            probe.measuring = false;
            // 3인칭 미리보기: 몸 전체(원래 메시)를 뒤·옆에서
            var shotGo = new GameObject("R06Third");
            var third = shotGo.AddComponent<Camera>();
            third.enabled = false;
            var pt = body.transform;
            third.transform.position = pt.position + pt.forward * 3.0f - pt.right * 2.2f + Vector3.up * 1.8f;   // 앞쪽·벽 반대편에서 벽을 봄
            third.transform.LookAt(pt.position + pt.right * 1.0f + Vector3.up * 1.6f);
            var oldMode = body.body.shadowCastingMode;
            body.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var rtx = new RenderTexture(640, 360, 24);
            third.targetTexture = rtx;
            third.Render();
            var prevRt = RenderTexture.active;
            RenderTexture.active = rtx;
            var tx = new Texture2D(640, 360, TextureFormat.RGB24, false);
            tx.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
            RenderTexture.active = prevRt;
            Directory.CreateDirectory(OutDir);
            File.WriteAllBytes($"{OutDir}/WallRun_stage_third.png", tx.EncodeToPNG());
            body.body.shadowCastingMode = oldMode;
            rtx.Release();
            Object.Destroy(shotGo);
        }
    }
}
