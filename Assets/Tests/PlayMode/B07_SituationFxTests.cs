using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>상황별 연출 연결 (제안서 2장 A~G, GDD-beta B4·B5). Game 씬에서 시간을 주입해 실제 흐름으로 검사한다.</summary>
    public class B07_SituationFxTests
    {
        double now;
        float realTime;
        GameBootstrap boot;
        StageDirector dir;
        PresentationHooks hooks;
        PresentationDirector pd;
        readonly List<FxPreset> played = new List<FxPreset>();

        IEnumerator Load(double startRunBeat, ChartData chart = null)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            pd = boot.presentation;
            hooks = Object.FindAnyObjectByType<PresentationHooks>();
            now = 0; realTime = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.slowMotion.RealTime = () => realTime;
            pd.timeFx.RealTime = () => realTime;
            played.Clear();
            pd.Played += e => played.Add(e.Preset);
            if (chart != null) dir.UseChart(chart);
            if (boot.bulletTime != null) boot.bulletTime.enabled = false;   // 일정한 빠르기로 입력 시각을 미리 계산하는 테스트 (회피 슬로모션은 B47 테스트)
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            if (dir != null) dir.slowMotion.Cancel();
            Time.timeScale = 1f;
        }

        IEnumerator AdvanceTo(double t, double step = 0.02)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03, 0.01);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.26, 0.01);   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1)가 지나야 무입력 FAIL
        }

        [UnityTest]
        public IEnumerator B07_A_B_E_Duel_WarnDodgeExecution()
        {
            yield return Load(89);   // beta1-1: 88 매복 사건(B20 차트)을 건너뛴다. 92 칼 포졸                       // 92: 칼 든 포졸 (땅)
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var enemy = enc.Actors[0];
            // A: 첫 공격 0.5박 전 예고
            var c0 = enc.Cues[0];
            yield return AdvanceTo(boot.clock.TimeOfBeat(c0.TargetBeat - 0.6), 0.01);
            Assert.IsFalse(hooks.Log.Contains("AttackWarn"), "0.5박 전보다 일찍 예고하지 않음");
            yield return AdvanceTo(boot.clock.TimeOfBeat(c0.TargetBeat - 0.45), 0.01);
            Assert.IsTrue(hooks.Log.Contains("AttackWarn"), "A: 휘두르기 직전 예고");
            // B: 회피 PERFECT (가로베기 ↓)
            yield return Hit(c0, 0.0);
            Assert.AreEqual("DodgePerfect", LastFx());
            Assert.IsTrue(enemy.Frozen, "Hit Stop: 적만 멈춤");
            Assert.AreEqual(1f, Time.timeScale, "전역 시간 그대로");
            yield return Hit(enc.Cues[1], 0.0);
            Assert.AreEqual(2, hooks.Log.Count(n => n == "AttackWarn"), "공격마다 예고");
            realTime += 1f; pd.timeFx.Tick();
            Assert.IsFalse(enemy.Frozen, "Hit Stop 끝나면 풀림");
            // E: 마무리 원 예고(화면 더 가라앉음) → 처형
            yield return Hit(enc.Cues[2], 0.0);
            yield return Hit(enc.Cues[3], 0.0);                  // GDD-beta B18.3 연속기 4회
            yield return Hit(enc.Cues[4], 0.0);
            Assert.IsTrue(hooks.Log.Contains("FinisherWarn"), "마무리 원 예고");
            Assert.AreEqual("Execution", LastFx(), "땅 위 PERFECT 처형");
            Assert.IsTrue(dir.slowMotion.IsActive, "처형 슬로모션");
            Assert.AreEqual(VfxKind.InkSlash, pd.vfxPool.ActiveFlash, "먹 획");
            pd.cameraFx.Step(0.1f);
            Assert.Less(pd.cameraFx.Fov, -4f, "FOV가 50° 쪽으로");
            Assert.Greater(Mathf.Abs(pd.cameraFx.Roll), 3f, "처형 롤");
        }

        [UnityTest]
        public IEnumerator B07_C_D_NormalAndFail()
        {
            yield return Load(89);   // beta1-1: 88 매복 사건(B20 차트)을 건너뛴다. 92 칼 포졸
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            yield return Hit(enc.Cues[0], 0.1);                 // NORMAL
            Assert.AreEqual("DefendNormal", LastFx(), "C: NORMAL 방어");
            yield return Hit(enc.Cues[1], null);                 // FAIL (무입력)
            Assert.AreEqual("Hit", LastFx(), "D: 피격");
            pd.screenFx.Step(0f);
            Assert.AreEqual(GameConfig.Fx.Vermilion, pd.screenFx.VignetteColor, "붉은 테두리");
            Assert.Greater(pd.cameraFx.Trauma, 0.4f, "흔들림");
        }

        [UnityTest]
        public IEnumerator B07_B_E_RoofParryAndKick()
        {
            yield return Load(126);                              // 128: 창 든 포졸 (지붕): 찌르기 →, 올려베기 ↑
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            yield return Hit(enc.Cues[0], 0.0);
            Assert.AreEqual("DodgePerfect", LastFx());
            yield return Hit(enc.Cues[1], 0.0);
            Assert.AreEqual("ParryPerfect", LastFx(), "↑ 받아 막기 = 패링");
            Assert.AreEqual(VfxKind.BrushBurst, pd.vfxPool.ActiveFlash, "붓획 방사 섬광");
            pd.screenFx.Step(0f);
            Assert.Greater(pd.screenFx.Exposure, 0.3f, "노출 순간 밝아짐");
            yield return Hit(enc.Cues[2], 0.0);
            yield return Hit(enc.Cues[3], 0.0);                  // GDD-beta B18.3 연속기 4회
            yield return Hit(enc.Cues[4], 0.0);
            Assert.AreEqual("ExecutionKick", LastFx(), "지붕 위: 발차기");
            pd.cameraFx.Step(0.1f);
            Assert.Greater(pd.cameraFx.Pitch, 5f, "아래로 10°쪽");
        }

        [UnityTest]
        public IEnumerator B07_F_Surround_TurnLowWide_WindmillFovShockwave_SlowMoOnlyDesignated()
        {
            yield return Load(181);
            pd.vfxPool.DeltaTime = () => 0f;
            pd.cameraFx.DeltaTime = () => 0f;
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            int slowStart = pd.timeFx.SlowMotionCount;
            // 1단계: 5번 순차 공격 — 등장할 때마다 회전 롤 3°
            foreach (var c in cues.Where(c => ((CombatStep)c.Tag).Phase == 1)) yield return Hit(c, 0.0);
            Assert.AreEqual(5, hooks.Log.Count(n => n == "SurroundTurn"));
            Assert.AreEqual(pd.timeFx.SlowMotionCount, slowStart, "1단계 PERFECT는 슬로모션 없음");
            Assert.AreEqual(5, played.Count(p => p.Name == "SurroundTurn" && p.Roll == 3f), "1단계 회전마다 롤 3°");
            // 2단계: 동시 공격 — 낮게(위로 8°)·넓게(FOV +6°), PERFECT는 슬로모션 후보(16박 제한)
            var p2 = cues.Where(c => ((CombatStep)c.Tag).Phase == 2).ToList();
            foreach (var c in p2) yield return Hit(c, 0.0);
            Assert.AreEqual(3, played.Count(p => p.Name == "Simultaneous" && p.Fov == 6f && p.Pitch == -8f), "동시 공격마다 FOV +6°, 위로 8°");
            Assert.AreEqual(3, hooks.Log.Count(n => n == "SimultaneousPerfect"));
            Assert.AreEqual(slowStart, pd.timeFx.SlowMotionCount, "동시 공격 PERFECT 뒤 슬로모션 없음 (GDD-beta B17.4: 회피 슬로모션으로 바꿈)");
            dir.slowMotion.Cancel(); Time.timeScale = 1f;
            // 3단계: 윈드밀 화살표마다 FOV +2° (최대 +8°)
            pd.cameraFx.ClearAll();
            var p3 = cues.Where(c => ((CombatStep)c.Tag).Phase == 3).ToList();
            for (int i = 0; i < p3.Count; i++)
            {
                yield return Hit(p3[i], 0.0);
                if (i < p3.Count - 1)
                {
                    pd.cameraFx.Step(0f);
                    float fov = 0; // 화살표 i+1개 쌓임 (각 0.05초 상승 후 유지)
                    pd.cameraFx.Step(0.06f); fov = pd.cameraFx.Fov;
                    Assert.AreEqual(2f * (i + 1), fov, 0.05f, $"화살표 {i + 1}개: FOV +{2 * (i + 1)}°");
                }
            }
            Assert.AreEqual(4, hooks.Log.Count(n => n == "WindmillArrow"));
            Assert.IsTrue(hooks.Log.Contains("WindmillFinish"), "윈드밀 마무리");
            Assert.AreEqual(slowStart + 1, pd.timeFx.SlowMotionCount, "윈드밀 슬로모션 (항상)");
            Assert.AreEqual(VfxKind.InkX, pd.vfxPool.ActiveFlash, "먹 X");
            var ring = pd.vfxPool.LastShockwave;
            pd.vfxPool.Step(0.2f);
            Assert.AreEqual(3f, VfxPool.ShockRadius(ring), 0.01f);
            pd.vfxPool.Step(0.199f);
            Assert.AreEqual(6f, VfxPool.ShockRadius(ring), 0.02f, "충격파 반지름 6 m");
            pd.cameraFx.Step(0.02f);
            Assert.LessOrEqual(pd.cameraFx.Fov, 8f + 1e-3f, "FOV는 +8°를 넘지 않음 (화살표 몫)");
        }

        [UnityTest]
        public IEnumerator B07_Boss_Duel_StrikeClash_SlowMoOnlyOnExecution()
        {
            // beta1-1: GDD-beta B11 — 무사 결투는 42판정. 내 공격 PERFECT는 충돌 연출(StrikePerfect),
            // 3단계 난타 중에는 슬로모션을 걸지 않고(예전 "마지막 패링 슬로모션" 대신) 마무리 처형만 슬로모션
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            int s0 = pd.timeFx.SlowMotionCount;
            var cues = enc.Cues.ToList();
            Assert.AreEqual(CombatStepKind.Strike, ((CombatStep)cues[2].Tag).Kind);
            for (int i = 0; i < 3; i++) yield return Hit(cues[i], 0.0);
            Assert.AreEqual("StrikePerfect", LastFx(), "내 공격 PERFECT: 칼끼리 충돌");
            for (int i = 3; i < cues.Count - 1; i++) { yield return Hit(cues[i], 0.0); dir.slowMotion.Cancel(); Time.timeScale = 1f; }
            Assert.AreEqual(s0, pd.timeFx.SlowMotionCount, "결투 중 슬로모션 없음");
            Assert.IsTrue(hooks.Log.Contains("PostureBreak"), "3단계 끝: 자세 무너짐");
            yield return Hit(cues[cues.Count - 1], 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(dir.CurrentEncounter.StartBeat + BossDuel.LengthBeats) + 0.05);   // beta1-1: GDD-beta B11.5 — 처형 컷 D가 끝난 74박에 클리어
            Assert.AreEqual(StageState.Cleared, dir.State);
            Assert.AreEqual(s0 + 1, pd.timeFx.SlowMotionCount, "마무리 처형 슬로모션");
        }

        /// <summary>예고(AttackWarn·FinisherWarn)를 뺀 마지막 연출.</summary>
        string LastFx() => hooks.Log.Last(n => n != "AttackWarn" && n != "FinisherWarn" && n != "SurroundTurn" && n != "Simultaneous");

        static ChartData Chart(params ChartEvent[] e) => new ChartData { name = "t", bpm = 120, runBeats = 264, events = e };

        IEnumerator RunAll(double? delay, double untilRun)
        {
            while (boot.player.RunBeat < untilRun)
            {
                now += 0.01;
                if (delay.HasValue)
                    foreach (var c in boot.judge.Cues.ToArray())
                        if (!c.Judged && c.Appeared && now >= boot.clock.TimeOfBeat(c.TargetBeat) + delay.Value - 1e-6) { boot.judge.Submit(c.Key, now); break; }
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator B07_G_Parkour_JumpLand_FailRedBorder_EventSlowMo()
        {
            yield return Load(0, Chart(
                new ChartEvent { runBeat = 8, type = "obstacle", kind = "cart", key = "Up" },
                new ChartEvent { runBeat = 14, type = "obstacle", kind = "rope_swing", key = "Up" },
                new ChartEvent { runBeat = 20, type = "obstacle", kind = "low_wall", key = "Up" },
                new ChartEvent { runBeat = 26, type = "obstacle", kind = "tile_slip" }));
            int s0 = pd.timeFx.SlowMotionCount;
            yield return RunAll(0.0, 13);
            Assert.IsTrue(hooks.Log.Contains("JumpPerfect"), "G: 도약 PERFECT");
            Assert.IsTrue(hooks.Log.Contains("LandPerfect"), "G: 착지 PERFECT");
            Assert.AreEqual(s0, pd.timeFx.SlowMotionCount, "일반 장애물은 슬로모션 없음");
            yield return RunAll(0.0, 19);
            Assert.IsFalse(hooks.Log.Contains("EventPerfectSlow"), "밧줄 넘기 PERFECT 뒤 슬로모션 없음 (GDD-beta B17.4)");
            Assert.AreEqual(s0, pd.timeFx.SlowMotionCount);
            dir.slowMotion.Cancel(); Time.timeScale = 1f;
            yield return RunAll(null, 25);                        // 낮은 담 FAIL
            Assert.IsTrue(hooks.Log.Contains("ParkourFail"), "G: FAIL");
            pd.screenFx.Step(0f);
            yield return RunAll(0.0, 33);                         // 기와 미끄러짐: 첫 판정에 기왓장 파편
            Assert.IsTrue(hooks.Log.Contains("TileDebris"), "기왓장이 빠짐");
            Assert.AreEqual(s0, pd.timeFx.SlowMotionCount, "기와 미끄러짐 PERFECT 뒤 슬로모션 없음 (B17.4)");
        }

        [UnityTest]
        public IEnumerator B07_G_ParkourFail_Vignette03()
        {
            yield return Load(0, Chart(new ChartEvent { runBeat = 8, type = "obstacle", kind = "low_wall", key = "Up" }));
            yield return RunAll(null, 8.5);
            Assert.AreEqual("ParkourFail", LastFx());
            pd.screenFx.Step(0f);
            Assert.AreEqual(0.3f, pd.screenFx.VignetteIntensity, 0.02f, "붉은 테두리 0.3");
            Assert.AreEqual(GameConfig.Fx.Vermilion, pd.screenFx.VignetteColor);
            Assert.Greater(pd.cameraFx.Trauma, 0.3f, "흔들림 0.5");
        }
    }
}
