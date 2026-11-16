using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>아슬아슬 회피 슬로모션 (GDD-beta B17.4): 박자 시계·음악·화면이 함께 느려졌다가 판정 뒤 돌아옴.</summary>
    public class B47_BulletTimePlayTests
    {
        [TearDown]
        public void TearDown() => Time.timeScale = 1f;

        [UnityTest]
        public IEnumerator B47_SlowsClockMusicScreen_FromOneBeatBefore_ReturnsAfterJudge()
        {
            double now = 0;
            var root = new GameObject("B47");
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
                var bt = root.AddComponent<BulletTimeFx>();
                bt.clock = clock; bt.judge = judge; bt.slowMotion = slow; bt.sfx = sfx; bt.lowPass = lowPass;
                bt.enabled = false;
                float dt = 0.02f;
                clock.StartClock();
                yield return null;

                void Step(double secs) { now += secs; judge.Tick(); bt.Tick((float)secs); }

                // 회피 슬로모션 판정 (목표 10박)
                var cue = judge.Schedule(10, ArrowKey.Left, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                judge.Schedule(5, ArrowKey.Up, null, new CombatStep { Kind = CombatStepKind.Attack });   // 보통 판정은 느려지지 않음
                while (clock.CurrentBeat < 8.9) { Step(dt); if (clock.CurrentBeat > 4.99 && clock.CurrentBeat < 5.2) Assert.AreEqual(1f, clock.SlowFactor, "보통 판정은 그대로"); }
                Assert.AreEqual(1f, clock.SlowFactor);
                Assert.IsNull(bt.Active);
                while (clock.CurrentBeat < 9.0) Step(0.005);
                Step(0.001);
                Assert.AreSame(cue, bt.Active, "1박 전부터");
                Assert.AreEqual(1, bt.StartCount);
                while (clock.CurrentBeat < 9.3) Step(dt);
                Assert.AreEqual(GameConfig.BulletTime.Factor, clock.SlowFactor, 1e-4f, "0.25박에 걸쳐 0.4배");
                Assert.AreEqual(GameConfig.BulletTime.Factor, clock.SpeedMultiplier, 1e-4f, "박자 시계 0.4배");
                Assert.AreEqual(GameConfig.BulletTime.Factor, Time.timeScale, 1e-4f, "화면 0.4배");
                Assert.AreEqual(GameConfig.BulletTime.Factor, music.pitch, 1e-4f, "음악 재생도 0.4배 (곡 위치와 박자 관계 그대로, 음이 낮아짐)");
                Assert.AreEqual(1f, bt.Muffle, 1e-4f);
                Assert.AreEqual(GameConfig.BulletTime.LowPassMuffledHz, lowPass.cutoffFrequency, 1f, "물속처럼 먹먹하게");
                Assert.AreEqual(GameConfig.BulletTime.MusicGain, sfx.MusicFxGain, 1e-4f, "작게");
                double b0 = clock.CurrentBeat;
                Step(0.1);
                Assert.AreEqual(0.1 * GameConfig.BulletTime.Factor / GameConfig.SecondsPerBeat, clock.CurrentBeat - b0, 1e-6, "0.1초에 0.08박");
                Assert.IsFalse(cue.Judged, "판정 시간 폭은 실제 시간 그대로라 아직 판정 전");

                // 판정(PERFECT) → 0.15초에 원래대로
                while (clock.CurrentBeat < 10) Step(0.01);
                judge.Submit(ArrowKey.Left, clock.TimeOfBeat(10));
                Assert.AreEqual(Judgement.Perfect, cue.Result);
                double before = clock.CurrentBeat;
                Step(0.0);
                Assert.IsTrue(bt.Releasing, "판정 뒤 돌아가기 시작");
                Assert.AreEqual(1, bt.ReleaseCount);
                Assert.AreEqual(before, clock.CurrentBeat, 1e-9, "박자는 끊기지 않고 이어짐");
                Step(0.075);
                Assert.That(clock.SlowFactor, Is.InRange(0.6f, 0.8f), "돌아오는 중");
                Step(0.08);
                Assert.AreEqual(1f, clock.SlowFactor, 1e-6f, "0.15초 뒤 원래 속도");
                Assert.AreEqual(1f, Time.timeScale, 1e-6f);
                Assert.AreEqual(1f, music.pitch, 1e-6f);
                Assert.AreEqual(GameConfig.BulletTime.LowPassOpenHz, lowPass.cutoffFrequency, 1f);
                Assert.AreEqual(1f, sfx.MusicFxGain, 1e-6f);
                Assert.IsNull(bt.Active);

                // 놓쳐도(FAIL) 돌아옴
                var miss = judge.Schedule(20, ArrowKey.Down, null, new CombatStep { Kind = CombatStepKind.Attack, BulletTime = true });
                while (!miss.Judged) Step(dt);
                Assert.AreEqual(Judgement.Fail, miss.Result);
                Assert.AreEqual(2, bt.StartCount);
                for (int i = 0; i < 10; i++) Step(dt);
                Assert.AreEqual(2, bt.ReleaseCount, "FAIL도 돌아옴");
                Assert.AreEqual(1f, clock.SlowFactor, 1e-6f);

                // 처형·윈드밀 슬로모션(GDD 4.2)은 그대로 두고 음악만 먹먹하게
                slow.Trigger();
                bt.Tick(0f);
                Assert.AreEqual(1f, clock.SlowFactor, "처형 슬로모션은 박자 시계를 바꾸지 않음");
                Assert.AreEqual(1f, bt.Muffle, 1e-4f, "처형 슬로모션 동안 음악 먹먹하게");
                slow.Cancel();
                bt.Tick(0f);
                Assert.AreEqual(0f, bt.Muffle, 1e-4f);
            }
            finally { Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator B47_GameScene_ClosingGate_BulletTime_NoPerfectSlowMo()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var dir = boot.director;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(MapLayout.Sections[4].StartBeat);   // 5챕터 궁궐 담
            yield return null;
            var bt = boot.bulletTime;
            Assert.IsNotNull(bt, "회피 슬로모션 연결");
            int slowBefore = dir.slowMotion.TriggerCount;
            JudgeCue gate = null;
            float minFactor = 1f, minScale = 1f;
            double limit = 30;
            while (now < limit && (gate == null || !gate.Judged || bt.Active != null))
            {
                now += 0.02;
                yield return null;
                foreach (var c in boot.judge.Cues.ToList())
                    if (!c.Judged && boot.clock.CurrentBeat >= c.TargetBeat) boot.judge.Submit(c.Key, boot.clock.TimeOfBeat(c.TargetBeat));
                if (bt.Active != null && gate == null) gate = bt.Active;
                minFactor = Mathf.Min(minFactor, boot.clock.SlowFactor);
                minScale = Mathf.Min(minScale, Time.timeScale);
            }
            Assert.IsNotNull(gate, "대문에서 회피 슬로모션");
            var run = gate.Tag as StageDirector.ObstacleRun;
            Assert.IsNotNull(run);
            Assert.AreEqual("closing_gate", run.Def.Kind);
            Assert.AreEqual(Judgement.Perfect, gate.Result);
            Assert.AreEqual(GameConfig.BulletTime.Factor, minFactor, 1e-3f, "박자 시계·음악 0.4배까지");
            Assert.AreEqual(GameConfig.BulletTime.Factor, minScale, 1e-3f, "화면 0.4배까지");
            Assert.AreEqual(1f, boot.clock.SlowFactor, 1e-6f, "피한 뒤 원래 속도");
            Assert.AreEqual(1f, Time.timeScale, 1e-6f);
            Assert.AreEqual(slowBefore, dir.slowMotion.TriggerCount, "PERFECT 뒤 0.4초 슬로모션은 없음 (B4 2·3번 없앰)");
            boot.clock.StopClock();
        }
    }
}
