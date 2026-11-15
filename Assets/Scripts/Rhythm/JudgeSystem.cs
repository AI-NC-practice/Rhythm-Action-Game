using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmActionGame
{
    /// <summary>판정 하나 (원 하나). 목표 박자, 눌러야 할 키, 화면에 원을 띄울 월드 위치.</summary>
    public class JudgeCue
    {
        public double TargetBeat;
        public ArrowKey Key;
        public Vector3? WorldPoint;
        public bool Appeared;
        public bool Judged;
        /// <summary>판정 없이 취소됨 (전투 재도전 등). Judged도 true가 된다.</summary>
        public bool Cancelled;
        public Judgement Result;
        /// <summary>같은 장면의 다른 시스템(장애물, 적)이 결과를 받을 때 쓰는 꼬리표.</summary>
        public object Tag;
        /// <summary>결과를 속도 배율에 반영하는지 (GDD-beta B18.2: 달리기 장단 노드는 반영하지 않음).</summary>
        public bool AffectsSpeed = true;
    }

    /// <summary>입력 하나가 어떻게 처리됐는지 (GDD-beta B8.2 입력 반응).</summary>
    public enum InputOutcome
    {
        /// <summary>판정됨 (PERFECT/NORMAL/FAIL).</summary>
        Judged,
        /// <summary>목표 −300 ms보다 일러 무시됨.</summary>
        TooEarly,
        /// <summary>판정할 원이 없음.</summary>
        NoCue,
    }

    /// <summary>
    /// 판정 창을 관리한다 (GDD 4.2). Input System의 Keyboard.current 방향키로 입력을 받고,
    /// 순수 함수 JudgeRules로 판정한 뒤 BeatClock의 속도 배율에 반영한다.
    /// </summary>
    public class JudgeSystem : MonoBehaviour
    {
        public BeatClock clock;
        /// <summary>타이밍 보정 값 (ms). 기본은 설정값.</summary>
        public Func<float> OffsetSource = () => GameSettings.TimingOffsetMs;
        /// <summary>false면 키보드를 읽지 않는다 (테스트).</summary>
        public bool readKeyboard = true;

        readonly List<JudgeCue> cues = new List<JudgeCue>();

        public event Action<JudgeCue> CueAppeared;
        public event Action<JudgeCue, Judgement> CueJudged;
        /// <summary>판정 없이 취소되거나 모두 지워질 때 (판정 원을 지우는 데 쓴다, B11).</summary>
        public event Action<JudgeCue> CueCancelled;
        /// <summary>방향키 입력마다 (키, 가장 이른 원 또는 null, 처리 결과). 입력 반응 표시용 (B13).</summary>
        public event Action<ArrowKey, JudgeCue, InputOutcome> InputReceived;

        public IReadOnlyList<JudgeCue> Cues => cues;

        /// <summary>등장했고 아직 판정되지 않은 원이 있는지 (회색 화면·줌 연출 기준).</summary>
        public bool HasActiveCue
        {
            get { foreach (var c in cues) if (c.Appeared && !c.Judged && !(c.Tag is RhythmNote)) return true; return false; }
        }

        /// <summary>
        /// 등장했고 아직 판정되지 않은 사건·전투 원 중 가장 이른 것 (장단 노드 제외, GDD-beta B18.2).
        /// 판정 화면(회색·줌)과 카메라 돌림은 이것을 본다 — 장단 노드는 장구 레인에만 보인다.
        /// </summary>
        public JudgeCue FirstActiveStageCue
        {
            get { foreach (var c in cues) if (c.Appeared && !c.Judged && !(c.Tag is RhythmNote)) return c; return null; }
        }

        public JudgeCue FirstActiveCue
        {
            get { foreach (var c in cues) if (c.Appeared && !c.Judged) return c; return null; }
        }

        void Awake()
        {
            if (clock == null) clock = BeatClock.Instance;
        }

        /// <summary>targetBeat 박자에 key를 눌러야 하는 판정을 예약한다.</summary>
        public JudgeCue Schedule(double targetBeat, ArrowKey key, Vector3? worldPoint = null, object tag = null)
        {
            var cue = new JudgeCue { TargetBeat = targetBeat, Key = key, WorldPoint = worldPoint, Tag = tag };
            int i = cues.Count;
            while (i > 0 && cues[i - 1].TargetBeat > targetBeat) i--;
            cues.Insert(i, cue);
            return cue;
        }

        /// <summary>모든 판정을 지운다 (게임 오버, 다시 시작). 떠 있던 원도 지우도록 알린다.</summary>
        public void ClearAll()
        {
            var gone = cues.ToArray();
            cues.Clear();
            foreach (var c in gone)
            {
                if (!c.Judged) { c.Judged = true; c.Cancelled = true; }
                CueCancelled?.Invoke(c);
            }
        }

        /// <summary>판정하지 않고 취소한다 (이벤트 없음). 목록에서는 다음 Tick에 빠진다.</summary>
        public void Cancel(JudgeCue cue)
        {
            if (cue == null || cue.Judged) return;
            cue.Judged = true;
            cue.Cancelled = true;
            CueCancelled?.Invoke(cue);
        }

        void Update()
        {
            if (clock == null || !clock.IsRunning || clock.IsPaused) return;
            if (readKeyboard) ReadKeyboard();
            Tick();
        }

        void ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // 프레임 시각이 아니라 Input System 이벤트 시각(실제로 누른 시각)으로 판정한다 (GDD-beta B8.1)
            double pressed = kb.lastUpdateTime;
            if (kb.upArrowKey.wasPressedThisFrame) SubmitRealtime(ArrowKey.Up, pressed);
            if (kb.downArrowKey.wasPressedThisFrame) SubmitRealtime(ArrowKey.Down, pressed);
            if (kb.leftArrowKey.wasPressedThisFrame) SubmitRealtime(ArrowKey.Left, pressed);
            if (kb.rightArrowKey.wasPressedThisFrame) SubmitRealtime(ArrowKey.Right, pressed);
        }

        /// <summary>
        /// 실제 시간(Input System 이벤트 시각)으로 입력 하나를 처리한다. 박자 시계 시각으로 바꾸고,
        /// 지금보다 늦거나 InputMaxAgeSeconds보다 오래된 값은 잘라 쓴다 (GDD-beta B8.1).
        /// </summary>
        public void SubmitRealtime(ArrowKey key, double realTime)
        {
            double now = clock.Now;
            double t = clock.RealtimeToClock(realTime);
            if (double.IsNaN(t) || t > now) t = now;
            if (t < now - GameConfig.InputMaxAgeSeconds) t = now - GameConfig.InputMaxAgeSeconds;
            Submit(key, t);
        }

        double TargetTime(JudgeCue cue) => clock.TimeOfBeat(cue.TargetBeat);

        /// <summary>방향키 입력 하나를 처리한다. time은 입력 시각(dsp 초, 보통 clock.Now).</summary>
        /// <summary>마지막 입력 시각 (시계 초, 진단 기록용).</summary>
        public double LastInputTime { get; private set; } = double.NaN;

        public void Submit(ArrowKey key, double time)
        {
            LastInputTime = time;
            float offset = OffsetSource();
            foreach (var first in cues)
            {
                if (first.Judged) continue;
                // GDD-beta B12.1: 판정 창이 넓어 반 박 간격 원끼리 창이 겹친다. 앞 원에 늦은 입력이 다음 원에 더 가까우면 다음 원으로 본다
                // (앞 원은 그대로 남아 창이 지나면 무입력 FAIL).
                var cue = CloserNext(first, time, offset, key);
                if (JudgeRules.TryJudge(time, TargetTime(cue), offset, key, cue.Key, out var r))
                {
                    Resolve(cue, r);
                    InputReceived?.Invoke(key, cue, InputOutcome.Judged);
                }
                else
                {
                    // 무시된 입력: 너무 이름(−300 ms 전) 또는 창이 이미 지남(다음 Tick에 무입력 FAIL)
                    bool early = JudgeRules.DeltaMs(time, TargetTime(cue), offset) < 0;
                    InputReceived?.Invoke(key, cue.Appeared ? cue : null, early && cue.Appeared ? InputOutcome.TooEarly : InputOutcome.NoCue);
                }
                // 가장 이른 미판정 원 하나만 본다. 무시된 입력(너무 이름)은 뒤의 원에도 너무 이르다.
                return;
            }
            InputReceived?.Invoke(key, null, InputOutcome.NoCue);
        }

        /// <summary>
        /// 앞 원에 늦은 입력을 다음 원으로 볼지 (GDD-beta B12.1). 누른 키가 한쪽 원에만 맞으면 그 원, 둘 다 맞거나 둘 다 틀리면 목표에 더 가까운 원.
        /// 다음 원은 그 판정 창 안일 때만 고른다.
        /// </summary>
        JudgeCue CloserNext(JudgeCue first, double time, float offset, ArrowKey key)
        {
            double d1 = JudgeRules.DeltaMs(time, TargetTime(first), offset);
            if (d1 <= 0) return first;
            JudgeCue next = null;
            bool after = false;
            foreach (var c in cues)
            {
                if (c == first) { after = true; continue; }
                if (after && !c.Judged) { next = c; break; }
            }
            if (next == null) return first;
            double d2 = System.Math.Abs(JudgeRules.DeltaMs(time, TargetTime(next), offset));
            if (d2 > GameConfig.NormalWindowMs) return first;
            bool m1 = first.Key == key, m2 = next.Key == key;
            if (m1 != m2) return m2 ? next : first;
            return d2 < d1 ? next : first;
        }

        /// <summary>원 등장과 무입력 FAIL을 처리한다. Update에서 매 프레임 호출된다.</summary>
        public void Tick()
        {
            if (clock == null || !clock.IsRunning) return;
            double beat = clock.CurrentBeat;
            double now = clock.Now;
            float offset = OffsetSource();
            for (int i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];
                if (cue.Judged) continue;
                if (!cue.Appeared && beat >= cue.TargetBeat - GameConfig.ApproachBeats)
                {
                    cue.Appeared = true;
                    CueAppeared?.Invoke(cue);
                }
                if (JudgeRules.IsMissed(now, TargetTime(cue), offset)) Resolve(cue, Judgement.Fail);
            }
            cues.RemoveAll(c => c.Judged);
        }

        void Resolve(JudgeCue cue, Judgement r)
        {
            if (cue.Judged) return;
            if (!cue.Appeared) { cue.Appeared = true; CueAppeared?.Invoke(cue); }
            cue.Judged = true;
            cue.Result = r;
            if (cue.AffectsSpeed) clock.ApplyJudgement(r);
            CueJudged?.Invoke(cue, r);
        }
    }
}
