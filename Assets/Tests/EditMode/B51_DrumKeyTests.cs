using NUnit.Framework;
using UnityEngine;

namespace RhythmActionGame.Tests
{
    /// <summary>장구 키 Q·W 분리 (GDD-beta B18.5): 두 판정 줄이 서로 방해하지 않음.</summary>
    public class B51_DrumKeyTests
    {
        [Test]
        public void B51_DrumKeys_JudgeOnlyJangdan_ArrowsOnlyStage()
        {
            Assert.IsTrue(JudgeRules.IsDrum(ArrowKey.Kung) && JudgeRules.IsDrum(ArrowKey.Deok));
            Assert.IsFalse(JudgeRules.IsDrum(ArrowKey.Left));
            Assert.AreEqual("Q", JudgeRules.Arrow(ArrowKey.Kung));
            Assert.AreEqual("W", JudgeRules.Arrow(ArrowKey.Deok));
            Assert.AreEqual(1.0, GameConfig.Jangdan.EventClearBeats, 1e-9, "사건·전투 판정과 1박");

            var go = new GameObject("B51");
            try
            {
                double now = 0;
                var clock = go.AddComponent<BeatClock>();
                clock.TimeSource = () => now;
                var judge = go.AddComponent<JudgeSystem>();
                judge.clock = clock;
                judge.readKeyboard = false;
                judge.OffsetSource = () => 0f;
                clock.StartClock();
                var drum = judge.Schedule(4, ArrowKey.Kung);        // 2.0초
                var stage = judge.Schedule(4.5, ArrowKey.Up);       // 2.25초
                var drum2 = judge.Schedule(5, ArrowKey.Deok);       // 2.5초
                now = 1.9; judge.Tick();

                // 장단 노드가 더 이르지만 방향키는 사건 노드만 본다 → 장단 노드는 그대로
                now = 2.25; judge.Submit(ArrowKey.Up, now);
                Assert.IsTrue(stage.Judged); Assert.AreEqual(Judgement.Perfect, stage.Result);
                Assert.IsFalse(drum.Judged, "방향키가 장단 노드를 FAIL로 만들지 않음");

                // Q는 장단 노드만
                now = 2.0; judge.Submit(ArrowKey.Kung, now);
                Assert.IsTrue(drum.Judged); Assert.AreEqual(Judgement.Perfect, drum.Result);

                // 같은 줄 안에서 틀린 장구 키는 FAIL (W 노드에 Q)
                now = 2.5; judge.Submit(ArrowKey.Kung, now);
                Assert.IsTrue(drum2.Judged); Assert.AreEqual(Judgement.Fail, drum2.Result, "Q·W를 바꿔 치면 FAIL");

                // 장단 노드가 없을 때 Q는 아무것도 판정하지 않음
                var stage2 = judge.Schedule(6, ArrowKey.Down);
                now = 3.0; judge.Tick();
                judge.Submit(ArrowKey.Deok, now);
                Assert.IsFalse(stage2.Judged, "장구 키가 사건 노드를 건드리지 않음");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
