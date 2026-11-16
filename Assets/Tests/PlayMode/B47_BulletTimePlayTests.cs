using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>
    /// 아슬아슬 회피 슬로모션 (GDD-beta B17.4 2차, 작업 B52): 노드는 평소 빠르기로 누르고, 피한 직후 박자 시계·음악·화면이 함께 느려지며
    /// 회색·먹먹해졌다가 돌아옴. 대문은 문 건너편 카메라 컷.
    /// </summary>
    public class B47_BulletTimePlayTests
    {
        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator B52_NoSlowBeforePress_SlowAfterDodge_GrayMuffled_ThenBack_SkipIfNextClose_NoneOnFail()
        {
            double now = 0;
            var root = new GameObject("B52");
            try
            {
                var music = root.AddComponent<AudioSource>();
                var lowPass = root.AddComponent<AudioLowPassFilter>();
                var clock = root.AddComponent<BeatClock>();
                clock.TimeSource = () => now;
                clock.music = music;
                var judge = root.AddComponent<JudgeSystem>();
                judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
                judge.enabled = false;
                var slow = root.AddComponent<SlowMotion>();
                var sfxGo = new GameObject("Sfx"); sfxGo.transform.SetParent(root.transform);
                var sfx = sfxGo.AddComponent<SfxPlayer>();
                var screenGo = new GameObject("Screen"); screenGo.transform.SetParent(root.transform);
                var screen = screenGo.AddComponent<ScreenFx>();
                screen.DeltaTime = () => 0f;
                var camFx = root.AddComponent<CameraFx>();
                camFx.DeltaTime = () => 0f;
                var bt = root.AddComponent<BulletTimeFx>();
                bt.clock = clock; bt.judge = judge; bt.slowMotion = slow; bt.sfx = sfx; bt.lowPass = lowPass; bt.screenFx = screen; bt.cameraFx = camFx;
                bt.DeltaTime = () => 0f;
                bt.enabled = false; bt.enabled = true;   // 연결 뒤 구독
                clock.StartClock();
                yield return null;

                void Step(double secs) { now += secs; judge.Tick(); bt.Tick((float)secs); }

                // 1) 노드가 다가오는 동안은 느려지지 않음
                var cue = judge.Schedule(10, ArrowKey.Left, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                while (clock.CurrentBeat < 9.99) { Step(0.02); Assert.AreEqual(1f, clock.SlowFactor, "누르기 전에는 평소 빠르기"); }
                Assert.AreEqual(1f, Time.timeScale);

                // 2) 피한 직후 0.06초에 0.3배
                while (clock.CurrentBeat < 10) Step(0.005);
                judge.Submit(ArrowKey.Left, clock.TimeOfBeat(10));
                Assert.AreEqual(Judgement.Perfect, cue.Result);
                Assert.AreSame(cue, bt.Active, "피한 직후 시작");
                Assert.AreEqual(1, bt.StartCount);
                Assert.Greater(camFx.ActiveCount, 0, "칼 회피 카메라 (NearMiss: 숙이며 올려다봄)");
                Step(0.03);
                Assert.That(clock.SlowFactor, Is.InRange(0.3f, 1f), "느려지는 중");
                Step(0.04);
                Assert.AreEqual(GameConfig.BulletTime.Factor, clock.SlowFactor, 1e-4f, "0.3배");
                Assert.AreEqual(GameConfig.BulletTime.Factor, clock.SpeedMultiplier, 1e-4f, "박자 시계");
                Assert.AreEqual(GameConfig.BulletTime.Factor, Time.timeScale, 1e-4f, "화면");
                Assert.AreEqual(GameConfig.BulletTime.Factor, music.pitch, 1e-4f, "음악도 같은 배율 (음이 낮아짐, 곡 위치와 박자 관계 그대로)");
                Assert.AreEqual(1f, bt.Muffle, 1e-4f);
                Assert.AreEqual(GameConfig.BulletTime.LowPassMuffledHz, lowPass.cutoffFrequency, 1f, "먹먹하게");
                Assert.AreEqual(GameConfig.BulletTime.MusicGain, sfx.MusicFxGain, 1e-4f, "작게");
                Assert.AreEqual(GameConfig.BulletTime.Saturation, screen.SlowSaturation, 1e-3f, "회색 화면");
                screen.Step(0f);
                Assert.LessOrEqual(screen.Saturation, GameConfig.BulletTime.Saturation + 1e-3f, "화면 채도에 합쳐짐");

                // 3) 실제 0.6초 머문 뒤 0.15초에 돌아옴
                Step(0.45);
                Assert.AreEqual(GameConfig.BulletTime.Factor, clock.SlowFactor, 1e-4f, "0.55초: 아직 느림");
                Step(0.09);
                Assert.IsTrue(bt.Releasing, "0.6초 뒤 돌아가기 시작");
                Step(0.16);
                Assert.AreEqual(1f, clock.SlowFactor, 1e-6f, "원래 속도");
                Assert.AreEqual(1f, Time.timeScale, 1e-6f);
                Assert.AreEqual(0f, screen.SlowSaturation, 1e-4f, "색이 돌아옴");
                Assert.AreEqual(GameConfig.BulletTime.LowPassOpenHz, lowPass.cutoffFrequency, 1f);
                Assert.IsNull(bt.Active);

                // 4) 다음 판정이 가까우면 그 0.6박 전에 돌아오기 시작
                var c2 = judge.Schedule(20, ArrowKey.Up, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                var next = judge.Schedule(21, ArrowKey.Down, null, new CombatStep { Kind = CombatStepKind.Attack });
                while (clock.CurrentBeat < 20) Step(0.005);
                judge.Submit(ArrowKey.Up, clock.TimeOfBeat(20));
                Assert.AreEqual(2, bt.StartCount);
                double releaseBeat = double.NaN;
                while (clock.CurrentBeat < 20.9)
                {
                    Step(0.01);
                    if (bt.Releasing && double.IsNaN(releaseBeat)) releaseBeat = clock.CurrentBeat;
                }
                Assert.LessOrEqual(releaseBeat, 21 - GameConfig.BulletTime.ClearBeats + 0.02, "다음 판정 0.6박 전에 돌아오기 시작");
                Assert.AreEqual(1f, clock.SlowFactor, 1e-6f, "다음 노드는 평소 빠르기로 누름");
                judge.Submit(ArrowKey.Down, clock.TimeOfBeat(21));
                Step(0.02);

                // 5) 다음 판정이 너무 가까우면 건너뜀
                var c3 = judge.Schedule(30, ArrowKey.Left, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                judge.Schedule(30.5, ArrowKey.Right, null, new CombatStep { Kind = CombatStepKind.Attack });
                while (clock.CurrentBeat < 30) Step(0.005);
                judge.Submit(ArrowKey.Left, clock.TimeOfBeat(30));
                Assert.AreEqual(1, bt.SkipCount, "0.5박 뒤 노드: 슬로 없이 노드 우선");
                Assert.AreEqual(2, bt.StartCount);
                judge.Submit(ArrowKey.Right, clock.TimeOfBeat(30.5) + 0.0);
                Step(0.3);

                // 6) FAIL이면 슬로모션 없음
                var miss = judge.Schedule(40, ArrowKey.Down, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                while (!miss.Judged) Step(0.02);
                Assert.AreEqual(Judgement.Fail, miss.Result);
                Assert.AreEqual(2, bt.StartCount, "FAIL은 슬로 없음");
                Assert.AreEqual(1f, clock.SlowFactor);

                // 7) 처형·윈드밀 슬로모션(GDD 4.2)도 회색·먹먹하게, 박자 시계는 그대로
                slow.Trigger();
                bt.Tick(0f);
                Assert.AreEqual(1f, clock.SlowFactor);
                Assert.AreEqual(1f, bt.Muffle, 1e-4f);
                Assert.AreEqual(GameConfig.BulletTime.Saturation, screen.SlowSaturation, 1e-3f, "처형 슬로모션도 회색");
                slow.Cancel();
                bt.Tick(0f);
                Assert.AreEqual(0f, bt.Muffle, 1e-4f);
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator B52_GameScene_ClosingGate_PressAtNormalSpeed_ThenGateCameraCut()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var dir = boot.director;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            var bt = boot.bulletTime;
            Assert.IsNotNull(bt, "회피 슬로모션 연결");
            bt.DeltaTime = () => 0.02f;
            boot.StartRun(MapLayout.Sections[4].StartBeat);   // 5챕터 궁궐 담
            yield return null;
            int slowBefore = dir.slowMotion.TriggerCount;
            JudgeCue gate = null;
            float factorBeforeGate = 1f, minFactor = 1f;
            bool sawCut = false, camAtGate = false, bodyShown = false;
            for (int i = 0; i < 1500 && (gate == null || bt.Active != null); i++)
            {
                now += 0.02;
                yield return null;
                if (gate == null && bt.Active != null) gate = bt.Active;
                if (gate == null) factorBeforeGate = Mathf.Min(factorBeforeGate, boot.clock.SlowFactor);
                minFactor = Mathf.Min(minFactor, boot.clock.SlowFactor);
                if (bt.GateCut)
                {
                    sawCut = true;
                    if (Vector3.Distance(boot.playerCamera.transform.position, bt.GateCamPosition) < 0.5f) camAtGate = true;   // 컷 카메라는 LateUpdate에서 놓이고, 그 사이 플레이어(부모)가 몇 cm 움직일 수 있음
                    if (boot.player.GetComponentsInChildren<Renderer>().Any(r => r.shadowCastingMode == ShadowCastingMode.On)) bodyShown = true;
                }
                foreach (var c in boot.judge.Cues.ToList())
                    if (!c.Judged && boot.clock.CurrentBeat >= c.TargetBeat) boot.judge.Submit(c.Key, boot.clock.TimeOfBeat(c.TargetBeat));
            }
            Assert.IsNotNull(gate, "대문에서 회피 슬로모션");
            Assert.AreEqual("closing_gate", ((StageDirector.ObstacleRun)gate.Tag).Def.Kind);
            Assert.AreEqual(Judgement.Perfect, gate.Result);
            Assert.AreEqual(1f, factorBeforeGate, 1e-6f, "대문 노드는 평소 빠르기로 누름");
            Assert.AreEqual(GameConfig.BulletTime.Factor, minFactor, 1e-3f, "피한 뒤 0.3배");
            Assert.IsTrue(sawCut, "대문 3인칭 컷");
            Assert.IsTrue(camAtGate, "카메라가 문 건너편 앞");
            Assert.IsTrue(bodyShown, "주인공 몸이 보임");
            yield return null;
            Assert.IsFalse(bt.GateCut);
            Assert.AreEqual(Vector3.zero, boot.playerCamera.transform.localPosition, "1인칭으로 돌아옴");
            Assert.IsFalse(boot.player.GetComponentsInChildren<Renderer>().Any(r => r.shadowCastingMode == ShadowCastingMode.On), "몸은 다시 그림자만");
            Assert.AreEqual(1f, boot.clock.SlowFactor, 1e-6f);
            Assert.AreEqual(1f, Time.timeScale, 1e-6f);
            Assert.AreEqual(slowBefore, dir.slowMotion.TriggerCount, "PERFECT 뒤 0.4초 슬로모션은 없음");
            boot.clock.StopClock();
        }
    }
}
