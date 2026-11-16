using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 판정 예고음 (GDD-beta B17.2): 사건·전투 판정 원이 떠 있는 동안 목표 박 2박 전·1박 전에 작은 "틱"(1박 전은 한 음 높게).
    /// 장단 노드(장구 레인)는 곡의 장구 가락이라 예고음을 내지 않는다. 다른 판정의 목표 박과 겹치면 빼고, 같은 박의 틱은 한 번만.
    /// 메트로놈처럼 dsp 시각으로 미리 예약해(PlayScheduled) 프레임과 무관하게 박에 맞춘다. 설정 "예고음"으로 끈다.
    /// </summary>
    public class CueTickPlayer : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        /// <summary>켜짐 여부 (기본: 설정값). 테스트에서 바꿔 끼운다.</summary>
        public System.Func<bool> EnabledSource = () => GameSettings.CueTicks;

        AudioClip tick, tickLast;
        AudioSource[] sources;
        int nextSource;
        readonly HashSet<long> done = new HashSet<long>();
        readonly List<long> pruneTmp = new List<long>();
        double lastBeat = double.NegativeInfinity;

        /// <summary>지금까지 예약한 틱 수 (테스트용).</summary>
        public int ScheduledCount { get; private set; }
        /// <summary>최근 예약한 틱 박자들 (테스트용, 최근 32개).</summary>
        public readonly List<double> RecentBeats = new List<double>();
        /// <summary>마지막 틱이 1박 전 틱(높은 음)이었는지.</summary>
        public bool LastWasFinal { get; private set; }
        public AudioSource LastSource { get; private set; }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (sources != null) return;
            tick = ClickSynth.CreateTickClip(false);
            tickLast = ClickSynth.CreateTickClip(true);
            sources = new AudioSource[3];
            for (int i = 0; i < sources.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.volume = GameConfig.BeatFx.TickVolume;
                sources[i] = s;
            }
            if (clock == null) clock = BeatClock.Instance;
        }

        static long Key(double beat) => (long)System.Math.Round(beat * 1000.0);

        void Update() => Tick();

        /// <summary>예약 범위(0.1초) 안에 들어온 예고음 박을 예약한다.</summary>
        public void Tick()
        {
            EnsureInit();
            if (clock == null || judge == null || !clock.IsRunning || clock.IsPaused) return;
            double now = clock.CurrentBeat;
            if (now < lastBeat - 0.5) done.Clear();   // 되감기·다시 시작: 박자가 뒤로 감
            lastBeat = now;
            Prune(now);
            if (EnabledSource != null && !EnabledSource()) return;

            double nowTime = clock.Now;
            var cues = judge.Cues;
            for (int i = 0; i < cues.Count; i++)
            {
                var cue = cues[i];
                if (cue.Judged || cue.Tag is RhythmNote) continue;
                foreach (var b in BeatPulse.TickBeats(cue.TargetBeat))
                {
                    if (b <= now) continue;
                    long k = Key(b);
                    if (done.Contains(k)) continue;
                    double at = clock.TimeOfBeat(b);
                    if (at - nowTime > GameConfig.BeatFx.TickLookAheadSeconds) continue;
                    done.Add(k);
                    if (ClashesWithJudge(b)) continue;
                    Play(at, b, b >= cue.TargetBeat - 1.0 - 1e-6);
                }
            }
        }

        /// <summary>박 b가 다른 판정(장단 노드 포함)의 목표 박과 TickClearBeats 안인지.</summary>
        bool ClashesWithJudge(double b)
        {
            foreach (var c in judge.Cues)
                if (!c.Judged && System.Math.Abs(c.TargetBeat - b) < GameConfig.BeatFx.TickClearBeats) return true;
            return false;
        }

        void Play(double at, double beat, bool final)
        {
            var src = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            src.clip = final ? tickLast : tick;
            src.PlayScheduled(at);
            LastSource = src;
            LastWasFinal = final;
            ScheduledCount++;
            RecentBeats.Add(beat);
            if (RecentBeats.Count > 32) RecentBeats.RemoveAt(0);
        }

        void Prune(double now)
        {
            if (done.Count < 64) return;
            pruneTmp.Clear();
            foreach (var k in done) if (k < Key(now - 1.0)) pruneTmp.Add(k);
            foreach (var k in pruneTmp) done.Remove(k);
        }

        /// <summary>예약 기록을 지운다 (새로 시작).</summary>
        public void ResetSchedule()
        {
            done.Clear();
            lastBeat = double.NegativeInfinity;
        }
    }
}
