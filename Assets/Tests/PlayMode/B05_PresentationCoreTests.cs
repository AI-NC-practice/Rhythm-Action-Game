using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>연출 핵심 (GDD-beta B4·B5, 제안서 1·4·6장). 시간을 주입해 결정적으로 검사한다.</summary>
    public class B05_PresentationCoreTests
    {
        class Dummy : IHitStoppable
        {
            public bool Frozen;
            public int Calls;
            public void SetFrozen(bool f) { Frozen = f; Calls++; }
        }

        double now;
        float real;
        GameObject root;
        BeatClock clock;
        CameraFx cam;
        TimeFx time;
        SlowMotion slow;
        PresentationDirector pd;
        Transform offset;

        [SetUp]
        public void SetUp()
        {
            now = 0; real = 100f;
            root = new GameObject("FxTest");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            clock.StartClock();
            offset = new GameObject("FxOffset").transform;
            offset.SetParent(root.transform);
            cam = root.AddComponent<CameraFx>();
            cam.fxOffset = offset;
            cam.DeltaTime = () => 0f;                 // Step으로만 진행
            slow = root.AddComponent<SlowMotion>();
            slow.RealTime = () => real;
            time = root.AddComponent<TimeFx>();
            time.slowMotion = slow;
            time.clock = clock;
            time.RealTime = () => real;
            pd = root.AddComponent<PresentationDirector>();
            pd.cameraFx = cam;
            pd.timeFx = time;
            pd.Enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            slow.Cancel();
            Time.timeScale = 1f;
            PresentationDirector.DefaultEnabled = true;
            Object.Destroy(root);
        }

        void Run(float seconds, float dt = 0.01f)
        {
            for (float t = 0; t < seconds - 1e-6f; t += dt) { cam.Step(dt); real += dt; time.Tick(); slow.Tick(); }
        }

        [UnityTest]
        public IEnumerator B05_Layers_ClampedPerSection4()
        {
            for (int i = 0; i < 5; i++) pd.Play(GameConfig.Fx.Execution, 1f);         // 롤 8° × 5
            for (int i = 0; i < 5; i++) pd.Play(GameConfig.Fx.ExecutionKick, 1f);     // 고개 10° × 5
            for (int i = 0; i < 4; i++) pd.Play(GameConfig.Fx.Hit, 1f);               // 흔들림 0.6 × 4
            Assert.AreEqual(1f, cam.Trauma, 1e-5f, "흔들림 세기 합 ≤ 1");
            cam.Step(0.2f);
            Assert.LessOrEqual(Mathf.Abs(cam.Roll), 10f + 1e-4f, "롤 ±10°");
            Assert.LessOrEqual(Mathf.Abs(cam.Pitch), 35f + 1e-4f, "고개 ±35°");
            Assert.AreEqual(1f - 1.5f * 0.2f, cam.Trauma, 1e-4f, "초당 1.5 감소");
            var e = offset.localEulerAngles;
            Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(0, e.z)), 10f + 1e-3f);

            // FOV: JudgeFx가 45~85°로 자른다
            var camGo = new GameObject("Cam", typeof(Camera));
            camGo.transform.SetParent(offset, false);
            var jfx = root.AddComponent<JudgeFx>();
            jfx.cam = camGo.GetComponent<Camera>();
            jfx.DeltaTime = () => 0f;
            jfx.EnsureSetup();
            cam.judgeFx = jfx;
            cam.ClearAll();
            for (int i = 0; i < 10; i++) pd.Play(GameConfig.Fx.Simultaneous);          // +6° × 10
            cam.Step(0.2f); jfx.Step(0f);
            Assert.AreEqual(85f, jfx.Fov, 1e-3f, "FOV 최대 85°");
            cam.ClearAll();
            for (int i = 0; i < 10; i++) pd.Play(GameConfig.Fx.Execution);              // −8° × 10
            cam.Step(0.2f); jfx.Step(0f);
            Assert.AreEqual(45f, jfx.Fov, 1e-3f, "FOV 최소 45°");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_YawOwner_Priority()
        {
            Assert.IsTrue(cam.RequestYaw(CameraOwner.JudgeTurn, 20f, 1f));
            Assert.IsTrue(cam.RequestYaw(CameraOwner.Windmill, 180f, 0.8f), "높은 주인이 가져감");
            Assert.IsFalse(cam.RequestYaw(CameraOwner.Execution, 30f, 1f), "윈드밀 > 처형");
            Assert.IsFalse(cam.RequestYaw(CameraOwner.SurroundTurn, 30f, 1f));
            Assert.IsFalse(cam.RequestYaw(CameraOwner.JudgeTurn, 10f, 1f));
            Assert.IsFalse(cam.RequestYaw(CameraOwner.Parkour, 10f, 1f));
            Assert.AreEqual(CameraOwner.Windmill, cam.YawOwner);
            Assert.AreEqual(180f, cam.Yaw, 1e-4f);
            Run(0.81f);
            Assert.IsNull(cam.YawOwner, "시간이 끝나면 주인 없음");
            Assert.IsTrue(cam.RequestYaw(CameraOwner.Parkour, 5f, 0.2f), "끝난 뒤엔 낮은 주인도 받음");
            Assert.IsTrue(cam.RequestYaw(CameraOwner.EventGaze, 8f, 0.2f), "같거나 높으면 바꿈");
            // 순서: Windmill > Execution > SurroundTurn > EventGaze > JudgeTurn > Parkour
            Assert.Greater(CameraOwner.Windmill, CameraOwner.Execution);
            Assert.Greater(CameraOwner.Execution, CameraOwner.SurroundTurn);
            Assert.Greater(CameraOwner.SurroundTurn, CameraOwner.EventGaze);
            Assert.Greater(CameraOwner.EventGaze, CameraOwner.JudgeTurn);
            Assert.Greater(CameraOwner.JudgeTurn, CameraOwner.Parkour);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_HitStop_LocalOnly_LongerWins_IgnoredDuringSlowMo()
        {
            var a = new Dummy(); var b = new Dummy();
            double beat0 = clock.CurrentBeat;
            Assert.IsTrue(time.HitStop(0.07f, new IHitStoppable[] { a }));
            Assert.IsTrue(a.Frozen);
            Assert.AreEqual(1f, Time.timeScale, "전역 시간 그대로");
            now += 0.05;
            Assert.AreEqual(beat0 + 0.1, clock.CurrentBeat, 1e-9, "박자 시계 그대로");
            // 겹침: 0.02초 뒤 0.03초짜리 → 끝은 그대로(0.07), 더 긴 0.1초 → 늦은 쪽
            real += 0.02f;
            time.HitStop(0.03f, new IHitStoppable[] { b });
            Assert.AreEqual(100.07f, time.HitStopUntil, 1e-4f, "늦게 끝나는 쪽 하나");
            time.HitStop(0.1f, null);
            Assert.AreEqual(100.12f, time.HitStopUntil, 1e-4f);
            real = 100.119f; time.Tick();
            Assert.IsTrue(a.Frozen && b.Frozen);
            real = 100.121f; time.Tick();
            Assert.IsFalse(a.Frozen || b.Frozen, "끝나면 풀림");

            // 슬로모션 중에는 무시
            Assert.IsTrue(time.RequestSlowMotion(SlowMoMode.Always));
            Assert.IsFalse(time.HitStop(0.05f, new IHitStoppable[] { a }));
            Assert.IsFalse(a.Frozen);

            // 실제 적: 얼어 있는 동안 무기 자세가 바뀌지 않음
            var enemyGo = new GameObject("Enemy");
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(enemyGo.transform);
            var enemy = enemyGo.AddComponent<EnemyActor>();
            enemy.weaponPivot = pivot;
            enemy.Attack("overhead", 0.5f);
            var pose = pivot.localRotation;
            enemy.SetFrozen(true);
            enemy.Attack("overhead", 0.9f);
            Assert.AreEqual(pose, pivot.localRotation, "Hit Stop 중 무기 멈춤");
            enemy.SetFrozen(false);
            enemy.Attack("overhead", 0.9f);
            Assert.AreNotEqual(pose, pivot.localRotation);
            Object.Destroy(enemyGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_SlowMotion_FrequencyLimit16Beats()
        {
            Assert.IsTrue(pd.RequestSlowMotion(SlowMoMode.Candidate), "처음은 받음");
            slow.Cancel();
            now = 7.9 * 0.5;                                     // 7.9박
            Assert.IsFalse(pd.RequestSlowMotion(SlowMoMode.Candidate), "16박 안 → 거절");
            Assert.IsTrue(pd.RequestSlowMotion(SlowMoMode.Always), "처형·윈드밀은 항상");
            slow.Cancel();
            now = (7.9 + 15.9) * 0.5;
            Assert.IsFalse(pd.RequestSlowMotion(SlowMoMode.Candidate), "Always도 직전 슬로모션으로 센다");
            now = (7.9 + 16.01) * 0.5;
            Assert.IsTrue(pd.RequestSlowMotion(SlowMoMode.Candidate), "16박 뒤 → 받음");
            Assert.AreEqual(3, time.SlowMotionCount);

            // 일반 PERFECT 연출은 남는다
            slow.Cancel();
            now += 1.0;
            bool accepted = pd.Play(GameConfig.Fx.ParryPerfectFinale);
            Assert.IsFalse(accepted, "빈도 제한으로 슬로모션 거절");
            Assert.AreEqual("ParryPerfectFinale", pd.LastPlayed, "대신 일반 PERFECT 연출");
            Assert.IsTrue(time.HitStopActive, "Hit Stop은 걸림");
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_AllPresets_RecoverWithin05s()
        {
            foreach (var p in GameConfig.Fx.All)
            {
                cam.ClearAll();
                slow.Cancel();
                time.ResetHistory();
                pd.Play(p, -1f);
                Run(p.Total);
                Run(GameConfig.Fx.RecoverSeconds);
                Assert.IsTrue(cam.IsIdle, p.Name + ": 끝나고 0.5초 안에 모든 층 0");
                Assert.Less(offset.localPosition.magnitude, 1e-4f, p.Name);
                Assert.Less(Quaternion.Angle(Quaternion.identity, offset.localRotation), 1e-3f, p.Name + ": 원래 카메라");
                Assert.AreEqual(0f, cam.Fov, 1e-4f, p.Name);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_Disable_ClearsAndBlocks_F1Toggle()
        {
            pd.Play(GameConfig.Fx.Hit, 1f);
            cam.Step(0.05f);
            Assert.Greater(offset.localPosition.magnitude + cam.Trauma, 0f);
            pd.Enabled = false;
            Assert.IsTrue(cam.IsIdle, "끄면 층을 모두 지움");
            Assert.AreEqual(Vector3.zero, offset.localPosition);
            pd.Play(GameConfig.Fx.Hit, 1f);
            Assert.IsTrue(cam.IsIdle, "꺼져 있으면 재생 안 함");
            Assert.IsFalse(pd.RequestSlowMotion(SlowMoMode.Candidate), "후보 슬로모션도 없음");
            Assert.IsTrue(pd.RequestSlowMotion(SlowMoMode.Always), "처형·윈드밀 슬로모션은 GDD 규칙이라 유지");
            slow.Cancel();
            pd.Enabled = true;
            bool before = PresentationDirector.DefaultEnabled;
            bool after = DevMode.ToggleFx();
            Assert.AreEqual(!before, after, "F1 토글");
            Assert.AreEqual(after, PresentationDirector.Instance.Enabled);
            DevMode.ToggleFx();
            yield return null;
        }

        [UnityTest]
        public IEnumerator B05_JudgeStaysAccurate_DuringHitStopAndSlowMo()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double t = 0;
            boot.clock.TimeSource = () => t;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.director.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new ChartEvent[0] });
            boot.StartRun(0);
            yield return null;
            var results = new List<Judgement>();
            boot.judge.CueJudged += (c, r) => results.Add(r);
            boot.judge.Schedule(4, ArrowKey.Up);
            boot.judge.Schedule(8, ArrowKey.Up);
            boot.judge.Schedule(12, ArrowKey.Up);
            t = 1.5; boot.judge.Tick();
            boot.presentation.Play(GameConfig.Fx.ParryPerfect, 1f, new IHitStoppable[] { new Dummy() });
            Assert.IsTrue(boot.presentation.timeFx.HitStopActive);
            boot.judge.Submit(ArrowKey.Up, 2.0 + 0.055);         // +55 ms (Hit Stop 중)
            boot.presentation.RequestSlowMotion(SlowMoMode.Always);
            Assert.AreEqual(0.3f, Time.timeScale, 1e-4f);
            t = 3.5; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, boot.clock.TimeOfBeat(8) + 0.059);   // +59 ms (슬로모션 중)
            t = 5.0; boot.judge.Tick();
            boot.judge.Submit(ArrowKey.Up, boot.clock.TimeOfBeat(12) + 0.095);  // +95 ms → NORMAL (GDD-beta B8.4: PERFECT ±90)
            CollectionAssert.AreEqual(new[] { Judgement.Perfect, Judgement.Perfect, Judgement.Normal }, results, "PERFECT 창(GDD-beta B8.4 ±90 ms) 그대로");
            boot.director.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator B05_GameScene_EventSlowMotion_RespectsLimit()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var dir = boot.director;
            double t = 0;
            boot.clock.TimeSource = () => t;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.UseChart(new ChartData { name = "t", bpm = 120, runBeats = 264, events = new[] {
                new ChartEvent { runBeat = 8, type = "obstacle", kind = "rope_swing", key = "Up" },
                new ChartEvent { runBeat = 14, type = "obstacle", kind = "closing_gate", key = "Down" },
                new ChartEvent { runBeat = 30, type = "obstacle", kind = "rope_swing", key = "Up" } } });
            boot.StartRun(0);
            yield return null;
            Assert.IsNotNull(dir.presentation, "SceneBuilder가 연결");
            Assert.IsNotNull(boot.presentation.cameraFx.fxOffset, "FxOffset 층");
            var remaps = new List<double>();
            while (boot.player.RunBeat < 36)
            {
                t += 0.01;
                foreach (var c in boot.judge.Cues.ToArray())
                    if (!c.Judged && c.Appeared && t >= boot.clock.TimeOfBeat(c.TargetBeat) - 1e-6) { boot.judge.Submit(c.Key, t); break; }
                yield return null;
                if (boot.player.Remap.HasValue && (remaps.Count == 0 || remaps[remaps.Count - 1] != boot.player.Remap.Value.Start)) remaps.Add(boot.player.Remap.Value.Start);
            }
            Assert.AreEqual(2, remaps.Count, "10박 밧줄: 받음 / 16박 대문: 16박 안이라 거절 / 32박 밧줄: 받음");
            Assert.AreEqual(2, boot.presentation.timeFx.SlowMotionCount);
            dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }
    }
}
