using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 박에 맞춘 적 칼 휘두름 (GDD-beta B17.5): 적 공격 판정(CombatStep.Kind == Attack, 일반 적·포위·무사)의 목표 반 박 전에
    /// 실제 녹음 "쉭"(SfxKind.SwordWhoosh)을 dsp 시각으로 예약한다. 판정 박에는 결과 연출이 "챙"(막기)·"퍽"(맞음)을 낸다.
    /// 소리만으로 "쉭-챙"이 박에 들린다. 연출 끄기(F1)면 내지 않는다. 동시 공격은 판정 하나라 한 번만.
    /// </summary>
    public class CombatSwingSounds : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        public SfxPlayer sfx;
        public PresentationDirector presentation;

        AudioSource[] sources;
        int next;
        readonly HashSet<JudgeCue> done = new HashSet<JudgeCue>();
        double lastBeat = double.NegativeInfinity;

        /// <summary>예약한 휘두름 수와 최근 예약 박 (테스트용).</summary>
        public int ScheduledCount { get; private set; }
        public readonly List<double> RecentBeats = new List<double>();
        public AudioSource LastSource { get; private set; }

        void Awake() => EnsureInit();

        void EnsureInit()
        {
            if (sources != null) return;
            sources = new AudioSource[4];
            for (int i = 0; i < sources.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                sources[i] = s;
            }
        }

        /// <summary>휘두름 소리를 낼 판정인지: 적 공격(막기·피하기)만. 내 공격·마무리·윈드밀·사건·장단 노드는 아님.</summary>
        public static bool IsEnemySwing(JudgeCue cue) => cue != null && cue.Tag is CombatStep s && s.Kind == CombatStepKind.Attack;

        void Update() => Tick();

        public void Tick()
        {
            EnsureInit();
            if (clock == null || judge == null || !clock.IsRunning || clock.IsPaused) return;
            double beat = clock.CurrentBeat;
            if (beat < lastBeat - 0.5) done.Clear();   // 되감기·다시 시작
            lastBeat = beat;
            if (presentation != null && !presentation.Enabled) return;
            double now = clock.Now;
            foreach (var c in judge.Cues)
            {
                if (c.Judged || done.Contains(c) || !IsEnemySwing(c)) continue;
                double b = c.TargetBeat - GameConfig.CombatSound.SwingLeadBeats;
                if (b < beat - 0.05) { done.Add(c); continue; }   // 이미 지난 자리 (늦게 예약된 판정)
                double at = clock.TimeOfBeat(b);
                if (at - now > GameConfig.CombatSound.LookAheadSeconds) continue;
                done.Add(c);
                Play(System.Math.Max(0.0, at - now), b);
            }
            if (done.Count > 256) done.RemoveWhere(x => x.Judged);
        }

        void Play(double delay, double beat)
        {
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.clip = sfx != null ? sfx.Clip(SfxKind.SwordWhoosh) : SfxSynth.CreateClip(SfxKind.SwordWhoosh);
            src.volume = GameConfig.CombatSound.SwingVolume * (sfx != null ? sfx.sfxGroupVolume : 1f);
            src.PlayScheduled(AudioSettings.dspTime + delay);
            LastSource = src;
            ScheduledCount++;
            RecentBeats.Add(beat);
            if (RecentBeats.Count > 32) RecentBeats.RemoveAt(0);
        }
    }
}
