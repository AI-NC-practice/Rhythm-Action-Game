using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>Game 씬: 장단 줄(위쪽 한 줄 판정 원) + 장단 노드 판정 규칙 (GDD-beta B18.2).</summary>
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
            Assert.Greater(dir.RhythmNoteList.Count, 200, "스테이지 장단 노드");

            // 첫 장단 노드 2박 전까지 진행 → 레인에 보임, 판정 화면은 켜지지 않음
            var first = dir.RhythmNoteList[0];
            while (boot.clock.CurrentBeat < first.RunBeat - 1.5) { now += 0.02; yield return null; }
            var cue = boot.judge.Cues.FirstOrDefault(c => c.Tag == (object)first);
            Assert.IsNotNull(cue, "장단 노드가 예약됨");
            Assert.IsFalse(cue.AffectsSpeed, "속도 배율에 반영하지 않음");
            yield return null;
            var ring = boot.ringView.Rings.FirstOrDefault(r => r.Cue == cue);
            Assert.IsNotNull(ring, "장단 노드도 판정 원으로 뜸");
            Assert.AreEqual(JudgeRingView.RowSlot(first.Index), ring.Root.anchoredPosition, "화면 위쪽 장단 줄의 정해진 칸");
            Assert.AreEqual(GameConfig.Jangdan.RowYPx, ring.Root.anchoredPosition.y, 1e-3f);
            // 떠 있는 장단 원끼리 겹치지 않고, 사건·전투 원은 장단 줄 아래
            var rowRings = boot.ringView.Rings.Where(r => r.Cue.Tag is RhythmNote && !r.Cue.Judged).ToList();
            for (int i = 0; i < rowRings.Count; i++)
                for (int j = i + 1; j < rowRings.Count; j++)
                    Assert.GreaterOrEqual(UnityEngine.Vector2.Distance(rowRings[i].Root.anchoredPosition, rowRings[j].Root.anchoredPosition), 140f, "장단 원끼리 겹치지 않음");
            foreach (var r in boot.ringView.Rings.Where(r => !(r.Cue.Tag is RhythmNote)))
                Assert.LessOrEqual(r.Root.anchoredPosition.y, GameConfig.Jangdan.EventMaxYPx + 1e-3f, "사건·전투 원은 장단 줄 아래");
            if (boot.judge.Cues.All(c => c.Tag is RhythmNote || c.Judged || !c.Appeared))
            {
                Assert.IsFalse(boot.judge.HasActiveCue, "장단 노드만 있을 때 판정 화면(회색·줌) 꺼짐");
                Assert.IsNull(boot.judge.FirstActiveStageCue);
            }

            // 맞추면 손에 따라 장구 소리, 속도는 그대로
            now = boot.clock.TimeOfBeat(cue.TargetBeat);
            boot.judge.Submit(cue.Key, now);
            Assert.AreEqual(Judgement.Perfect, cue.Result);
            Assert.AreEqual(cue.Key == ArrowKey.Right ? "Hit_Normal" : "Hit_Perfect", boot.hitSound.LastSource.clip.name, "← 덩 / → 덕");
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
