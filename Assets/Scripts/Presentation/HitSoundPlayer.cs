using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 히트사운드 (GDD-beta B14): 방향키 입력마다 그 순간 장구 소리를 낸다. 판정 결과로 소리를 고른다(HitSoundRules).
    /// 효과음 풀(SfxPlayer)과 따로 자기 AudioSource를 돌려 써서 판정 효과음을 끊지 않는다. 음량은 효과음 그룹 음량을 따른다.
    /// </summary>
    public class HitSoundPlayer : MonoBehaviour
    {
        public JudgeSystem judge;
        public SfxPlayer sfx;

        AudioSource[] sources;
        int next;
        JudgeSystem subscribed;

        /// <summary>낸 히트사운드 수와 마지막 소리 (테스트용).</summary>
        public int PlayedCount { get; private set; }
        public SfxKind LastKind { get; private set; }
        public float LastVolume { get; private set; }
        public AudioSource LastSource { get; private set; }
        /// <summary>마지막 히트사운드를 몇 초 뒤에 울리도록 예약했는지 (0 = 바로, B15.3).</summary>
        public double LastDelay { get; private set; }
        public AudioSource[] Sources { get { EnsureInit(); return sources; } }

        void Awake() => EnsureInit();

        void OnEnable() => Subscribe();
        void Start() => Subscribe();   // judge를 Awake 뒤에 연결해도 받도록
        void OnDisable()
        {
            if (subscribed != null) subscribed.InputReceived -= OnInput;
            subscribed = null;
        }

        void Subscribe()
        {
            if (judge == subscribed) return;
            if (subscribed != null) subscribed.InputReceived -= OnInput;
            subscribed = judge;
            if (judge != null) judge.InputReceived += OnInput;
        }

        void EnsureInit()
        {
            if (sources != null) return;
            sources = new AudioSource[GameConfig.HitSound.Voices];
            for (int i = 0; i < sources.Length; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.priority = 0;   // 가장 높은 우선순위: 소리가 많아도 끊기지 않게
                sources[i] = s;
            }
        }

        void OnInput(ArrowKey key, JudgeCue cue, InputOutcome outcome)
        {
            var result = cue != null ? cue.Result : Judgement.Fail;
            float volume = HitSoundRules.Pick(outcome, result, out var kind);
            // 박 맞춤 (GDD-beta B15.3): 이른 PERFECT는 목표 박 시각에 울린다
            var clock = judge != null ? judge.clock : null;
            double until = cue != null && clock != null ? clock.TimeOfBeat(cue.TargetBeat) - clock.Now : double.NaN;
            Play(kind, volume, HitSoundRules.SnapDelay(outcome, result, until));
        }

        /// <summary>히트사운드 하나를 낸다 (돌려 쓰는 AudioSource 중 다음 것). delay초 뒤로 예약할 수 있다(dsp 시각 기준).</summary>
        public void Play(SfxKind kind, float volume, double delay = 0)
        {
            EnsureInit();
            var src = sources[next];
            next = (next + 1) % sources.Length;
            src.Stop();
            src.clip = sfx != null ? sfx.Clip(kind) : SfxSynth.CreateClip(kind);
            src.volume = volume * (sfx != null ? sfx.sfxGroupVolume : 1f);
            if (delay > 0) src.PlayScheduled(AudioSettings.dspTime + delay);
            else src.Play();
            PlayedCount++;
            LastDelay = delay > 0 ? delay : 0;
            LastKind = kind;
            LastVolume = volume;
            LastSource = src;
        }
    }
}
