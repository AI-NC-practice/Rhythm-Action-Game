using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>Game 씬: 장구 레인 + 장단 노드 판정 규칙 (GDD-beta B18.2).</summary>
    public class B50_JangdanPlayTests
    {
        [UnityTest]
        public IEnumerator B50_GameScene_LaneShowsNotes_NoJudgeScreen_FailKeepsHpAndSpeed()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var boot = GameBootstrap.Instance;
            var dir = boot.director;
            double now = 0;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            boot.StartRun(0);
            yield return null;
            Assert.IsNotNull(boot.jangdanLane, "장구 레인");
            Assert.Greater(dir.RhythmNoteList.Count, 200, "스테이지 장단 노드");

            // 첫 장단 노드 2박 전까지 진행 → 레인에 보임, 판정 화면은 켜지지 않음
            var first = dir.RhythmNoteList[0];
            while (boot.clock.CurrentBeat < first.RunBeat - 1.5) { now += 0.02; yield return null; }
            var cue = boot.judge.Cues.FirstOrDefault(c => c.Tag == (object)first);
            Assert.IsNotNull(cue, "장단 노드가 예약됨");
            Assert.IsFalse(cue.AffectsSpeed, "속도 배율에 반영하지 않음");
            boot.jangdanLane.Refresh();
            Assert.Greater(boot.jangdanLane.VisibleCount, 0, "레인에 노드가 흘러옴");
            if (boot.judge.Cues.All(c => c.Tag is RhythmNote || c.Judged || !c.Appeared))
            {
                Assert.IsFalse(boot.judge.HasActiveCue, "장단 노드만 있을 때 판정 화면(회색·줌) 꺼짐");
                Assert.IsNull(boot.judge.FirstActiveStageCue);
            }

            // 맞추면 손에 따라 장구 소리, 속도는 그대로
            now = boot.clock.TimeOfBeat(cue.TargetBeat);
            boot.judge.Submit(cue.Key, now);
            Assert.AreEqual(Judgement.Perfect, cue.Result);
            Assert.AreEqual(cue.Key == ArrowKey.Deok ? "Hit_Normal" : "Hit_Perfect", boot.hitSound.LastSource.clip.name, "Q 덩 / W 덕 (B18.5)");
            Assert.AreEqual(GameConfig.SpeedStart, boot.clock.TargetSpeed, 1e-5f, "장단 PERFECT는 속도에 반영 안 함");

            // 놓쳐도 체력·속도 그대로
            var next = boot.judge.Cues.First(c => c.Tag is RhythmNote && !c.Judged);
            int hp = dir.Health.Hp;
            while (!next.Judged) { now += 0.02; yield return null; }
            Assert.AreEqual(Judgement.Fail, next.Result);
            Assert.AreEqual(hp, dir.Health.Hp, "장단 FAIL은 체력 그대로");
            Assert.AreEqual(GameConfig.SpeedStart, boot.clock.TargetSpeed, 1e-5f, "장단 FAIL은 속도 그대로");
            boot.clock.StopClock();
        }
    }
}
