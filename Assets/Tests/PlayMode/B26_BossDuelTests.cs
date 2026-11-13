using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RhythmActionGame.Tests
{
    /// <summary>무사 결투 진행 (GDD-beta B11.2~B11.4). 게임 씬에서 시간을 주입해 검사한다.</summary>
    public class B26_BossDuelTests
    {
        double now;
        float real;
        GameBootstrap boot;
        StageDirector dir;
        PresentationDirector pd;
        PresentationHooks hooks;
        PostureBar bar;

        IEnumerator Load(double start)
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            boot = GameBootstrap.Instance;
            dir = boot.director;
            pd = boot.presentation;
            hooks = Object.FindFirstObjectByType<PresentationHooks>();
            bar = Object.FindFirstObjectByType<PostureBar>();
            now = 0; real = 100f;
            boot.clock.TimeSource = () => now;
            boot.judge.readKeyboard = false;
            boot.judge.OffsetSource = () => 0f;
            dir.RewindRealTime = () => real;
            dir.slowMotion.RealTime = () => real;
            bar.RealTime = () => real;
            boot.StartRun(start);
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

        IEnumerator Hit(JudgeCue cue, double? delay)
        {
            yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) - 0.03);
            if (delay.HasValue)
            {
                now = boot.clock.TimeOfBeat(cue.TargetBeat) + delay.Value;
                boot.judge.Submit(cue.Key, now);
                yield return null;
            }
            else yield return AdvanceTo(boot.clock.TimeOfBeat(cue.TargetBeat) + 0.2);
        }

        IEnumerator FinishRewind()
        {
            real += GameConfig.RewindSeconds + 0.01f;
            yield return null;
            Assert.IsFalse(dir.Rewinding);
        }

        static CombatStep Step(JudgeCue c) => (CombatStep)c.Tag;
        static float Units(float u) => u * GameConfig.PostureFull / BossDuel.PostureSteps;

        [UnityTest]
        public IEnumerator B26_Duel_Strike_Posture_PhaseRewind_Clear()
        {
            yield return Load(256);
            Assert.IsNotNull(bar, "자세 게이지");
            Assert.IsFalse(bar.Visible, "결투 전에는 안 보임");
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            double O = enc.StartBeat;
            Assert.IsTrue(enc.IsBossDuel);
            Assert.AreEqual(System.Math.Ceiling(enc.ArriveBeat - 0.05), O, 1e-9, "O = 도착 후 가장 가까운 박");
            var cues = enc.Cues.ToList();
            Assert.AreEqual(42, cues.Count);
            for (int i = 0; i < cues.Count; i++) Assert.AreEqual(O + BossDuel.Steps()[i].OffsetBeats, cues[i].TargetBeat, 1e-9);
            yield return null;
            Assert.IsTrue(bar.Visible, "결투 중 표시");
            Assert.AreEqual(new Vector2(0.5f, 1f), bar.Root.anchorMin, "화면 위 가운데");
            Assert.AreEqual(0f, bar.Ratio);

            // 1단계: A6 A8 PERFECT, S10 PERFECT(무사 밀림), S11 NORMAL(내가 밀림)
            yield return Hit(cues[0], 0.0);
            yield return Hit(cues[1], 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(cues[2].TargetBeat - 0.5));
            Assert.AreEqual("Guard", dir.boss.State, "내 공격 원이 뜨면 무사가 칼을 들어 막음");
            yield return Hit(cues[2], 0.0);
            Assert.AreEqual("StrikePerfect", hooks.Log.Last(n => n != "AttackWarn"), "칼끼리 충돌 연출");
            Assert.AreEqual("Pushed", dir.boss.State, "무사가 뒤로 밀림");
            var home = dir.boss.transform.position;
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.Greater(dir.boss.PushOffset, 0.01f, "밀려난 거리");
            Assert.Greater((dir.boss.transform.position - boot.player.transform.position).magnitude,
                (home - boot.player.transform.position).magnitude, "플레이어에게서 멀어짐");
            yield return new WaitForSecondsRealtime(0.3f);                   // 밀림이 끝나 제자리로
            Assert.AreEqual(0f, dir.boss.PushOffset, 1e-4f, "제자리로 돌아옴");
            yield return Hit(cues[3], 0.13);
            Assert.AreEqual(Judgement.Normal, cues[3].Result);
            Assert.AreEqual("StrikeNormal", hooks.Log.Last(n => n != "AttackWarn"), "내가 밀림");
            Assert.AreNotEqual("Pushed", dir.boss.State, "NORMAL은 무사가 밀리지 않음");
            pd.cameraFx.Step(0.05f);
            Assert.Less(pd.cameraFx.Position.z, -0.08f, "카메라(나)가 뒤로 밀림");
            Assert.AreEqual(Units(3.5f), enc.Posture, 1e-3f, "PERFECT 3 + NORMAL 1 = 3.5 × 100/N");
            yield return null;
            Assert.AreEqual(enc.Posture / 100f, bar.Ratio, 1e-4f, "게이지 표시");

            for (int i = 4; i < 13; i++) yield return Hit(cues[i], 0.0);
            yield return Hit(cues[13], 0.0);                                 // 칼 겨루기 L30
            Assert.AreEqual(CombatStepKind.Lock, Step(cues[13]).Kind);
            float atClash = enc.Posture;
            Assert.AreEqual(Units(13.5f), atClash, 1e-3f);
            yield return Hit(cues[14], 0.0);                                 // 2단계 A30
            Assert.Greater(enc.Posture, atClash);
            yield return Hit(cues[15], null);                                // A30.5 FAIL → 2단계 처음으로
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(RewindKind.Boss, dir.LastRewind);
            Assert.AreEqual(3, dir.Health.Hp, "체력 그대로");
            Assert.AreEqual(atClash, enc.Posture, 1e-4f, "게이지는 단계 시작 값으로");
            yield return FinishRewind();
            Assert.AreEqual(O + 30, boot.clock.CurrentBeat, 0.05, "2단계 처음 (O+30)");
            var again = enc.Cues.ToList();
            Assert.AreEqual(19 + 8 + 1, again.Count, "2단계부터 다시 예약");
            Assert.AreEqual(O + 32, again[0].TargetBeat, 1e-9, "같은 박자에");

            for (int i = 0; i < 27; i++) yield return Hit(again[i], 0.0);  // 2단계 + 3단계
            Assert.AreEqual(GameConfig.PostureFull, enc.Posture, 1e-4f, "3단계 끝: 가득");
            Assert.IsTrue(enc.PostureBroken);
            Assert.IsTrue(hooks.Log.Contains("PostureBreak"), "자세 무너짐 연출");
            yield return null;
            Assert.AreEqual(1f, bar.Ratio, 1e-4f);
            Assert.Greater(bar.FlashAlpha, 0.3f, "게이지가 붉게 번쩍임");
            real += GameConfig.PostureBreakFlashSeconds + 0.01f;
            yield return null;
            Assert.AreEqual(0f, bar.FlashAlpha, 1e-4f);

            yield return Hit(again[27], 0.0);                                // 마무리 F70
            yield return AdvanceTo(boot.clock.TimeOfBeat(dir.CurrentEncounter.StartBeat + BossDuel.LengthBeats) + 0.05);   // beta1-1: GDD-beta B11.5 — 처형 컷 D가 끝난 74박에 클리어
            Assert.AreEqual(StageState.Cleared, dir.State, "클리어");
            Assert.AreEqual(O + BossDuel.LengthBeats, dir.LastEncounter.ResumeBeat, 1e-6, "74박에 끝남 (마무리 70 + 처형 컷 D)");
            yield return null;
            Assert.IsFalse(bar.Visible, "결투가 끝나면 숨김");
        }

        [UnityTest]
        public IEnumerator B26_Duel_FlurryAndFinisherFail_RewindToPhaseStart()
        {
            yield return Load(256);
            yield return UntilCombat();
            var enc = dir.CurrentEncounter;
            double O = enc.StartBeat;
            var cues = enc.Cues.ToList();
            for (int i = 0; i < 33; i++) yield return Hit(cues[i], 0.0);   // 1단계 13 + 칼 겨루기 1 + 2단계 19
            Assert.AreEqual(Units(33f), enc.Posture, 1e-3f);
            yield return Hit(cues[33], null);                                // 3단계 첫 판정 FAIL
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(Units(33f), enc.Posture, 1e-3f, "3단계 시작 값");
            yield return FinishRewind();
            Assert.AreEqual(O + 58, boot.clock.CurrentBeat, 0.05, "3단계 처음 (O+58)");
            var p3 = enc.Cues.ToList();
            Assert.AreEqual(9, p3.Count, "3단계 8 + 마무리");
            for (int i = 0; i < 8; i++) yield return Hit(p3[i], 0.0);
            Assert.IsTrue(enc.PostureBroken);
            yield return Hit(p3[8], null);                                   // 마무리 FAIL
            Assert.IsTrue(dir.Rewinding);
            Assert.AreEqual(3, dir.Health.Hp);
            Assert.AreEqual(GameConfig.PostureFull, enc.Posture, 1e-4f, "마무리 단계는 가득 찬 채로");
            yield return FinishRewind();
            Assert.AreEqual(O + 68, boot.clock.CurrentBeat, 0.05, "마무리 처음 (O+68)");
            Assert.AreEqual(1, enc.Cues.Count);
            yield return Hit(enc.Cues[0], 0.0);
            yield return AdvanceTo(boot.clock.TimeOfBeat(dir.CurrentEncounter.StartBeat + BossDuel.LengthBeats) + 0.05);   // beta1-1: GDD-beta B11.5 — 처형 컷 D가 끝난 74박에 클리어
            Assert.AreEqual(StageState.Cleared, dir.State);
        }
    }

    /// <summary>내 공격 원 모양과 긴 묶음 자리 돌려쓰기 (GDD-beta B11.2, B11.6). 판정 원 화면만 따로 띄워 검사한다.</summary>
    public class B26_DuelRingViewTests
    {
        double now;
        float real;
        GameObject root;
        BeatClock clock;
        JudgeSystem judge;
        JudgeRingView view;
        Camera cam;
        static readonly Vector2 Hd = new Vector2(1920, 1080);

        [SetUp]
        public void SetUp()
        {
            now = 0; real = 0;
            root = new GameObject("B26");
            clock = root.AddComponent<BeatClock>();
            clock.TimeSource = () => now;
            judge = root.AddComponent<JudgeSystem>();
            judge.clock = clock; judge.readKeyboard = false; judge.OffsetSource = () => 0f;
            var camGo = new GameObject("Cam", typeof(Camera)); camGo.transform.SetParent(root.transform);
            cam = camGo.GetComponent<Camera>();
            var canvas = UiFactory.CreateCanvas("JudgeCanvas", GameConfig.RingCanvasOrder);
            canvas.transform.SetParent(root.transform);
            view = canvas.gameObject.AddComponent<JudgeRingView>();
            view.RealTime = () => real;
            view.CanvasSize = () => Hd;
            view.displayLatencyMs = 0f;
            view.Bind(judge, clock, cam);
            clock.StartClock();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
            if (view != null && view.labelLayer != null) Object.Destroy(view.labelLayer.gameObject);
        }

        void At(double beat) { now = beat * GameConfig.SecondsPerBeat; judge.Tick(); real += 0.2f; view.Refresh(); }

        static CombatStep S(CombatStepKind k, ArrowKey key) => new CombatStep { Kind = k, Key = key, Attack = k == CombatStepKind.Strike ? "strike" : "overhead" };

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        [UnityTest]
        public IEnumerator B26_StrikeRing_VermilionGlow_SwordIcon()
        {
            var guard = judge.Schedule(4, ArrowKey.Left, null, S(CombatStepKind.Attack, ArrowKey.Left));
            var strike = judge.Schedule(8, ArrowKey.Right, null, S(CombatStepKind.Strike, ArrowKey.Right));
            At(2.5);
            var g = view.Rings.Single(r => r.Cue == guard);
            Assert.IsNull(g.StrikeIcon, "막기 원에는 칼 아이콘 없음");
            Assert.IsTrue(Same(JudgeRingView.GlowColor, g.Glow.color), "막기 원은 청록");
            At(6.5);
            var s = view.Rings.Single(r => r.Cue == strike);
            Assert.IsNotNull(s.StrikeIcon, "칼 아이콘");
            Assert.AreEqual(VfxFactory.SwordIcon, s.StrikeIcon.sprite);
            Assert.Greater(s.StrikeIcon.rectTransform.anchoredPosition.x, 0f, "원 오른쪽");
            Assert.Greater(s.StrikeIcon.rectTransform.anchoredPosition.y, 0f, "원 위");
            Assert.IsTrue(Same(GameConfig.StrikeRingColor, s.Glow.color), "내 공격 원은 주홍");
            Assert.IsTrue(Same(GameConfig.StrikeRingColor, s.ChargeGlow.color), "빛도 주홍");
            Assert.IsFalse(Same(JudgeRingView.GlowColor, s.Glow.color));
            yield return null;
        }

        [UnityTest]
        public IEnumerator B26_LongChain_ReusesSlots_InsideSafeEllipse()
        {
            // 3단계 난타처럼 1박 간격 8개. 화면 오른쪽 끝에 있는 적을 따라가도 묶음이 타원 밖으로 밀려나지 않는다
            cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var radii = RingLayout.SafeRadii(Hd);
            var cues = Enumerable.Range(0, 8).Select(i => judge.Schedule(10 + i, (ArrowKey)(i % 4), new Vector3(30f, 0f, 10f),
                S(i % 2 == 0 ? CombatStepKind.Attack : CombatStepKind.Strike, (ArrowKey)(i % 4)))).ToList();
            Vector2? first = null;
            for (double b = 8; b <= 17.5; b += 0.25)
            {
                At(b);
                Assert.LessOrEqual(view.Rings.Count(r => cues.Contains(r.Cue) && !r.Cue.Judged), 3, $"{b}: 동시에 보이는 원 3개 이하");
                foreach (var r in view.Rings.Where(r => cues.Contains(r.Cue)))
                {
                    var p = r.Root.anchoredPosition;
                    Assert.IsTrue(RingLayout.Inside(p, radii), $"{b}: {r.Cue.TargetBeat} 원 {p} 안전 타원 안");
                    if (r.Cue == cues[0]) first = p;
                    if (r.Cue == cues[3] && first.HasValue) Assert.AreEqual(first.Value.x, p.x, 0.5f, "넷째 원은 첫째 자리를 다시 씀");
                }
                var live = view.Rings.Where(r => cues.Contains(r.Cue) && !r.Cue.Judged).Select(r => r.Root.anchoredPosition).ToList();
                for (int i = 0; i < live.Count; i++)
                    for (int j = i + 1; j < live.Count; j++)
                        Assert.GreaterOrEqual(Vector2.Distance(live[i], live[j]), GameConfig.RingChainSpacingPx - 0.5f, $"{b}: 떠 있는 원끼리 겹치지 않음");
            }
            Assert.IsTrue(first.HasValue);
            yield return null;
        }
    }
}
