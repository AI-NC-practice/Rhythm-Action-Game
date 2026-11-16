using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>화면이 박에 맞춰 숨 쉼 (GDD-beta B17.2): 박 펄스(판정 원·HUD·화면 가장자리), 예고음, 걸음·카메라.</summary>
    public class B45_BeatFxPlayTests
    {
        [TearDown]
        public void TearDown()
        {
            PresentationDirector.DefaultEnabled = true;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator B45_GameScene_PulseOnBeat_RingHudVignette_StepBob()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(0);
            var fx = boot.beatFx;
            Assert.IsNotNull(fx, "BeatFx 연결");
            Assert.IsNotNull(fx.hud, "HUD 연결");
            Assert.IsNotNull(boot.cueTicks, "예고음 연결");
            fx.DeltaTime = () => 1f;   // 달리기 정도가 바로 1이 되게

            // 카메라 계층: Motion → BeatBob → FxOffset → PlayerCamera
            var camParent = boot.playerCamera.transform.parent;
            Assert.AreEqual("FxOffset", camParent.name);
            Assert.AreEqual(fx.bob, camParent.parent, "걸음 출렁임은 FxOffset 위");
            Assert.AreEqual("Motion", fx.bob.parent.name);

            // 판정 원 하나를 띄운다 (3박 뒤 목표)
            var cue = boot.judge.Schedule(10.0, ArrowKey.Up, boot.player.transform.position + boot.player.transform.forward * 8f + Vector3.up);

            // 마디 첫 박(8박) 순간 (StageDirector가 Running을 바꿀 수 있어 확인 직전에 다시 켬)
            now = boot.clock.TimeOfBeat(8.0) + 1e-6;   // 부동소수 반올림으로 바로 앞 박이 되지 않게
            yield return null;
            boot.player.Running = true;
            fx.Step(1f);
            float accent = GameConfig.BeatFx.BarAccent;
            Assert.AreEqual(accent, fx.Pulse, 1e-3f, "마디 첫 박은 더 크게");
            var ring = boot.ringView.Rings.FirstOrDefault(r => r.Cue == cue);
            Assert.IsNotNull(ring, "판정 원이 떠 있음");
            Assert.AreEqual(1f, ring.Target.rectTransform.localScale.x, 1e-4f, "판정 원 테두리에는 박 펄스 없음 (B52: 누르는 순간 원이 출렁이지 않게)");
            Assert.AreEqual(1f + accent * GameConfig.BeatFx.HudScale, fx.hud.Hearts[0].rectTransform.localScale.x, 1e-3f, "HUD 펄스");
            Assert.AreEqual(1f + accent * GameConfig.BeatFx.HudScale, fx.hud.SpeedText.rectTransform.localScale.x, 1e-3f);
            Assert.AreEqual(1f + accent * GameConfig.BeatFx.LaneScale, boot.jangdanLane.HitMarker.rectTransform.localScale.x, 1e-3f, "장구 레인 판정 표시 펄스");
            Assert.AreEqual(accent * GameConfig.BeatFx.Vignette, boot.presentation.screenFx.BeatVignette, 1e-3f, "화면 가장자리 펄스");
            boot.presentation.screenFx.Step(0f);
            Assert.GreaterOrEqual(boot.presentation.screenFx.VignetteIntensity, accent * GameConfig.BeatFx.Vignette - 1e-3f, "비네트에 합쳐짐 (최댓값)");
            Assert.AreEqual(1f, fx.Run, 1e-3f, "달리는 중");
            Assert.AreEqual(-GameConfig.BeatFx.StepDropMeters, fx.bob.localPosition.y, 1e-3f, "박 순간 걸음이 가장 낮음");

            // 보통 박(9박)은 작게
            now = boot.clock.TimeOfBeat(9.0) + 1e-6;   // 부동소수 반올림으로 바로 앞 박이 되지 않게
            yield return null;
            boot.player.Running = true;
            fx.Step(1f);
            Assert.AreEqual(1f, fx.Pulse, 1e-3f, "보통 박");
            Assert.AreEqual(1f, ring.Target.rectTransform.localScale.x, 1e-4f);

            // 박 사이: 원래 크기, 걸음은 발 쪽으로
            boot.judge.Cancel(cue);
            now = boot.clock.TimeOfBeat(10.5);
            yield return null;
            boot.player.Running = true;
            fx.Step(1f);
            Assert.AreEqual(0f, fx.Pulse, 1e-3f, "박 사이엔 펄스 없음");
            Assert.AreEqual(1f, fx.hud.Hearts[0].rectTransform.localScale.x, 1e-3f);
            Assert.AreEqual(0f, fx.bob.localPosition.y, 1e-3f, "반 박에서 가장 높음");
            Assert.AreEqual(-GameConfig.BeatFx.StepSwayMeters, fx.bob.localPosition.x, 1e-3f, "왼발 걸음");
            now = boot.clock.TimeOfBeat(11.5);
            yield return null;
            boot.player.Running = true;
            fx.Step(1f);
            Assert.AreEqual(GameConfig.BeatFx.StepSwayMeters, fx.bob.localPosition.x, 1e-3f, "오른발 걸음 (1박 = 한 걸음)");

            // 멈춰 있으면(전투) 걸음 대신 박마다 살짝 내려앉음
            now = boot.clock.TimeOfBeat(12.0) + 1e-6;   // 부동소수 반올림으로 바로 앞 박이 되지 않게
            yield return null;
            boot.player.Running = false;
            fx.Step(1f);
            Assert.AreEqual(0f, fx.Run, 1e-3f);
            Assert.AreEqual(0f, fx.bob.localPosition.x, 1e-4f);
            Assert.AreEqual(-GameConfig.BeatFx.IdleDipMeters * accent, fx.bob.localPosition.y, 1e-3f, "멈춤: 박 출렁임");
            boot.player.Running = true;

            // 연출 끄기(F1): 모두 0
            boot.presentation.Enabled = false;
            fx.Step(1f);
            Assert.IsFalse(fx.Active);
            Assert.AreEqual(0f, fx.Pulse);
            Assert.AreEqual(Vector3.zero, fx.bob.localPosition);
            Assert.AreEqual(0f, boot.presentation.screenFx.BeatVignette);
            Assert.AreEqual(1f, fx.hud.Hearts[0].rectTransform.localScale.x, 1e-4f);
            boot.presentation.Enabled = true;

            // 일시정지: 0
            boot.clock.Pause();
            fx.Step(1f);
            Assert.AreEqual(0f, fx.Pulse);
            boot.clock.Resume();
            boot.clock.StopClock();
        }

        [UnityTest]
        public IEnumerator B45_CueTicks_TwoAndOneBeatBefore_StageOnly_NoDup_NoClash_SettingOff()
        {
            double now = 0;
            var root = new GameObject("B45");
            try
            {
                var clock = root.AddComponent<BeatClock>();
                clock.TimeSource = () => now;
                var judge = root.AddComponent<JudgeSystem>();
                judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
                bool on = true;
                var ticks = root.AddComponent<CueTickPlayer>();
                ticks.clock = clock; ticks.judge = judge; ticks.EnabledSource = () => on;
                ticks.enabled = false;   // 직접 Tick
                clock.StartClock();
                yield return null;

                // 사건 판정 하나 (목표 10박): 8박·9박에 틱, 9박은 높은 음
                judge.Schedule(10, ArrowKey.Up);
                now = clock.TimeOfBeat(8) - 0.2; ticks.Tick();
                Assert.AreEqual(0, ticks.ScheduledCount, "0.1초보다 먼 박은 아직 예약 안 함");
                now = clock.TimeOfBeat(8) - 0.05; ticks.Tick();
                Assert.AreEqual(1, ticks.ScheduledCount, "2박 전 틱 예약");
                Assert.AreEqual(8.0, ticks.RecentBeats.Last(), 1e-9);
                Assert.IsFalse(ticks.LastWasFinal);
                Assert.AreEqual("CueTick", ticks.LastSource.clip.name);
                ticks.Tick();
                Assert.AreEqual(1, ticks.ScheduledCount, "같은 박은 한 번만");
                now = clock.TimeOfBeat(9) - 0.05; ticks.Tick();
                Assert.AreEqual(2, ticks.ScheduledCount, "1박 전 틱");
                Assert.IsTrue(ticks.LastWasFinal);
                Assert.AreEqual("CueTickLast", ticks.LastSource.clip.name, "1박 전은 높은 음");
                Assert.AreEqual(GameConfig.BeatFx.TickVolume, ticks.LastSource.volume, 1e-5f);

                // 장단 노드는 예고음 없음
                judge.Schedule(20, ArrowKey.Kung, null, new RhythmNote { RunBeat = 20, Key = ArrowKey.Kung });
                for (double b = 17.5; b < 20; b += 0.05) { now = clock.TimeOfBeat(b); ticks.Tick(); }
                Assert.AreEqual(2, ticks.ScheduledCount, "장단 노드(장구 레인)는 틱 없음");

                // 같은 목표 박 두 개 → 박마다 한 번
                judge.Schedule(30, ArrowKey.Left);
                judge.Schedule(30, ArrowKey.Right);
                for (double b = 27.5; b < 30; b += 0.05) { now = clock.TimeOfBeat(b); ticks.Tick(); }
                Assert.AreEqual(4, ticks.ScheduledCount, "28·29박 각 한 번");

                // 다른 판정의 목표 박과 겹치는 틱은 뺀다 (40박 판정의 39박 틱 = 39박 판정 자리)
                judge.Schedule(39, ArrowKey.Up);
                judge.Schedule(40, ArrowKey.Down);
                for (double b = 36.5; b < 39.5; b += 0.05) { now = clock.TimeOfBeat(b); judge.Tick(); ticks.Tick(); }
                var recent = ticks.RecentBeats.Skip(4).ToList();
                CollectionAssert.AreEqual(new[] { 37.0, 38.0 }, recent, "37·38박만 (39박은 다른 판정 박이라 뺌)");

                // 설정에서 끄면 소리 없음
                on = false;
                int before = ticks.ScheduledCount;
                judge.Schedule(50, ArrowKey.Up);
                for (double b = 47.5; b < 50; b += 0.05) { now = clock.TimeOfBeat(b); ticks.Tick(); }
                Assert.AreEqual(before, ticks.ScheduledCount, "예고음 꺼짐");
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator B45_Title_CueTicksToggle_Saved()
        {
            bool saved = GameSettings.CueTicks;
            try
            {
                GameSettings.CueTicks = true;
                yield return SceneManager.LoadSceneAsync("Title");
                yield return null;
                var menu = Object.FindAnyObjectByType<TitleMenu>();
                menu.OpenSettings();
                Assert.AreEqual(UiText.On, menu.CueTicksValue.text);
                var labels = menu.SettingsPanel.GetComponentsInChildren<UnityEngine.UI.Text>(true).Select(t => t.text).ToList();
                CollectionAssert.Contains(labels, UiText.CueTicks, "설정 화면에 예고음");
                menu.CueTicksButton.onClick.Invoke();
                Assert.AreEqual(UiText.Off, menu.CueTicksValue.text);
                Assert.IsFalse(GameSettings.CueTicks, "저장");
                menu.CueTicksButton.onClick.Invoke();
                Assert.IsTrue(GameSettings.CueTicks);
            }
            finally { GameSettings.CueTicks = saved; }
        }
    }
}
