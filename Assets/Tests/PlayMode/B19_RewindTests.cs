using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>실패 규칙과 되감기 (GDD-beta B9). Game 씬 + 시간 주입.</summary>
    public class B19_RewindTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        PresentationDirector pd;

        IEnumerator Load(double startRunBeat, ChartData chart = null)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            pd = boot.presentation;
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.FailHitSeconds = 0f;   // beta1-1: 피격 연출(GDD-beta B12.2)은 B30 테스트에서 따로 본다. 여기서는 바로 되감기
            dir.slowMotion.RealTime = () => real;
            if (chart != null) dir.UseChart(chart);
            boot.StartRun(startRunBeat);
            yield return null;
        }

        [TearDown]
        public void TearDown() { if (dir != null) dir.slowMotion.Cancel(); Time.timeScale = 1f; }

        IEnumerator AdvanceTo(double t, double step = 0.01)
        {
            while (now < t - 1e-9) { now = System.Math.Min(t, now + step); yield return null; }
        }

        IEnumerator UntilCombat()
        {
            for (int i = 0; i < 3000 && dir.State != StageState.Combat; i++) { now += 0.02; yield return null; }
            Assert.AreEqual(StageState.Combat, dir.State);
        }

        IEnumerator Hit(JudgeCue c, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) - 0.03);
            if (delay.HasValue) { now = boot.clock.TimeOfBeat(c.TargetBeat) + delay.Value; boot.judge.Submit(c.Key, now); yield return null; }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(c.TargetBeat) + 0.26);   // beta1-1: NORMAL 창 0.24초(GDD-beta B12.1)가 지나야 무입력 FAIL
        }

        [UnityTest]
        public IEnumerator B19_Duel_AttackFailContinues_FinisherFailPushDown()
        {
            yield return Load(89);   // beta1-1: 88 매복 사건(B20 차트)을 건너뛴다. 92 칼 포졸
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            yield return Hit(cues[0], null);                       // 공격 FAIL
            Assert.AreEqual(2, dir.Health.Hp, "체력 −1");
            Assert.IsFalse(dir.Rewinding, "일반 적은 되감지 않음");
            Assert.IsFalse(cues[1].Cancelled, "다음 공격으로 넘어감");
            yield return Hit(cues[1], null);                       // 공격 FAIL
            Assert.AreEqual(1, dir.Health.Hp);
            yield return Hit(cues[2], 0.0);                        // 마무리 PERFECT
            Assert.IsTrue(enc.Done);
            Assert.AreNotEqual(ExecutionKind.PushDown, enc.Execution, "마무리 성공이면 처형");
            Assert.AreEqual(1, enc.Attempts, "재도전 없음");
        }

        [UnityTest]
        public IEnumerator B19_Duel_FinisherFail_PushDown_SpeedDown_ResumesNextBar()
        {
            yield return Load(89);   // beta1-1: 88 매복 사건(B20 차트)을 건너뛴다. 92 칼 포졸
            yield return UntilCombat();
            for (int i = 0; i < 4; i++) boot.clock.ApplyJudgement(Judgement.Perfect);    // 1.20배
            boot.clock.ApplyPendingSpeedNow();   // GDD-beta B15.2: 속도는 다음 마디 첫 박에 들어오므로 바로 적용
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            yield return Hit(cues[0], 0.0);                        // 1.25
            yield return Hit(cues[1], 0.0);                        // 1.30
            Assert.AreEqual(1.30f, boot.clock.TargetSpeed, 1e-4f);
            yield return Hit(cues[2], null);                       // 마무리 FAIL
            Assert.AreEqual(2, dir.Health.Hp, "체력 −1");
            Assert.AreEqual(1.04f, boot.clock.TargetSpeed, 1e-4f, "속도 ×0.8 (1.30 → 1.04)");
            Assert.IsFalse(dir.Rewinding, "일반 적은 되감지 않음");
            Assert.IsTrue(enc.Done, "재도전 없이 끝");
            Assert.AreEqual(ExecutionKind.PushDown, enc.Execution, "엉겁결에 밀쳐 쓰러뜨리기");
            Assert.AreEqual(1, enc.Attempts);
            Assert.AreEqual(0.0, enc.ResumeBeat % 4.0, 1e-9, "다음 마디 첫 박에 다시 달림");
            yield return AdvanceTo(boot.clock.TimeOfBeat(enc.ResumeBeat) + 0.05);
            Assert.AreEqual(StageState.Running, dir.State);
        }

        [UnityTest]
        public IEnumerator B19_Windmill_Fail_RewindsToFirstArrowMinusOneBar_NoHpLoss()
        {
            yield return Load(181);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            var cues = enc.Cues.ToList();
            foreach (var c in cues.Where(c => ((CombatStep)c.Tag).Phase < 3)) yield return Hit(c, 0.0);
            var wind = cues.Where(c => ((CombatStep)c.Tag).Phase == 3).ToList();
            double first = wind[0].TargetBeat;
            int hp = dir.Health.Hp;
            yield return Hit(wind[0], null);                       // 첫 화살표 FAIL
            Assert.AreEqual(hp, dir.Health.Hp, "체력 그대로");
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Windmill, dir.LastRewind);
            real += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            Assert.AreEqual(first - GameConfig.RewindLeadBeats, boot.clock.CurrentBeat, 0.05, "첫 화살표 1마디(4박) 앞으로 되감음");
            var again = enc.Cues.Where(c => !c.Judged && ((CombatStep)c.Tag).Phase == 3).Select(c => c.TargetBeat).ToList();
            CollectionAssert.AreEqual(wind.Select(c => c.TargetBeat).ToList(), again, "같은 박자에 3단계 다시");
        }

        [UnityTest]
        public IEnumerator B19_HpZero_RewindsToChapterStart_KeepsScore_RankDash()
        {
            // 2구간(56~108) 안에서 체력 0
            yield return Load(70);
            var board = boot.score.Board;
            board.Add(Judgement.Perfect); board.Add(Judgement.Perfect);
            int scoreBefore = board.Score;
            dir.Health.Damage(); dir.Health.Damage(); dir.Health.Damage();
            yield return null;
            Assert.AreNotEqual(StageState.GameOver, dir.State, "게임 오버 아님");
            Assert.IsTrue(dir.Rewinding, "챕터 되감기 시작");
            Assert.AreEqual(RewindKind.Chapter, dir.LastRewind);
            Assert.AreEqual(0, boot.judge.Cues.Count(c => !c.Judged), "남은 판정 없음");
            real += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
            Assert.AreEqual(56.0, boot.player.RunBeat, 0.1, "2구간 시작 지점");
            Assert.AreEqual(3, dir.Health.Hp, "체력 3");
            Assert.AreEqual(StageState.Running, dir.State);
            Assert.GreaterOrEqual(board.Score, scoreBefore, "점수 이어 감");
            Assert.IsTrue(board.Rewound);
            Assert.AreEqual("-", board.Rank(true), "되감은 적이 있으면 클리어해도 랭크 -");
        }

        [UnityTest]
        public IEnumerator B19_RewindPresentation_PausedClock_FxSoundStreak()
        {
            yield return Load(256);
            yield return UntilCombat();
            var cues = dir.CurrentEncounter.Cues.ToList();
            yield return Hit(cues[0], null);                       // 무사 FAIL → 되감기
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(3, dir.Health.Hp, "되감기는 체력을 깎지 않음");
            // 연출: 카메라 뒤로, 채도 −90·노출 −0.5, 먹 줄기, 되감기 소리
            Assert.AreEqual("Rewind", pd.LastPlayed);
            CollectionAssert.Contains(pd.sfx.History, SfxKind.TapeRewind);
            Assert.AreEqual(VfxKind.RewindStreak, pd.vfxPool.ActiveFlash);
            pd.screenFx.Step(0f);
            Assert.LessOrEqual(pd.screenFx.Saturation, -89.9f);
            Assert.AreEqual(-0.5f, pd.screenFx.Exposure, 0.02f);
            pd.cameraFx.Step(0.3f);
            Assert.Less(pd.cameraFx.Position.z, -0.25f, "카메라 뒤로 0.3 m");
            Assert.AreEqual(GameConfig.Fx.Rewind.KickBack, 0.3f, 1e-5f);
            Assert.LessOrEqual(GameConfig.Fx.Rewind.Total, GameConfig.RewindSeconds + 1e-5f, "연출 0.8초 안");
            // 되감는 동안 박자 시계 멈춤
            double b = boot.clock.CurrentBeat;
            now += 0.6; yield return null;
            Assert.AreEqual(b, boot.clock.CurrentBeat, 1e-9);
            Assert.AreEqual(0, boot.ringView.Rings.Count, "남은 원 없음");
            real += 0.79f; yield return null;
            Assert.IsTrue(dir.Rewinding, "0.8초 전에는 계속");
            real += 0.02f; yield return null;
            Assert.IsFalse(dir.Rewinding, "0.8초 뒤 다시 진행");
            // beta1-1: GDD-beta B11.4 — 무사 결투는 그 단계 처음(1단계 = O+4)으로 되감는다
            Assert.AreEqual(dir.CurrentEncounter.StartBeat + BossDuel.PhaseStart(BossDuel.PhaseExplore), boot.clock.CurrentBeat, 0.05, "되감은 지점부터");
            Assert.IsTrue(boot.clock.IsRunning && !boot.clock.IsPaused);
        }

        [UnityTest]
        public IEnumerator B19_Config_MatchGddBetaB9()
        {
            Assert.AreEqual(0.8f, GameConfig.RewindSeconds);
            Assert.AreEqual(4.0, GameConfig.RewindLeadBeats);
            Assert.AreEqual(-90f, GameConfig.RewindSaturation);
            Assert.AreEqual(-0.5f, GameConfig.RewindExposure);
            Assert.AreEqual(0.3f, GameConfig.Fx.Rewind.KickBack);
            yield return null;
        }
    }
}
