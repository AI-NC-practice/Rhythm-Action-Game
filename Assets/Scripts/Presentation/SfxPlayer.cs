using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 효과음 재생 (GDD-beta B7, 제안서 5장 "소리").
    /// - AudioSource 풀 8개, 같은 종류는 동시 3개까지 (넘으면 가장 오래된 같은 종류를 끊고 새로 냄)
    /// - 그룹 두 개: Music(곡·반주), Sfx(효과음). 그룹 음량은 각 AudioSource.volume에 곱한다.
    ///   (AudioMixer 에셋은 코드로 만들 수 있는 공개 API가 없어 이 컴포넌트가 그룹 역할을 한다.)
    /// - 덕킹: Music 그룹 음량만 잠깐 줄인다. 음악의 재생 위치·속도(time, pitch)는 건드리지 않는다.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        /// <summary>미리 만든 효과음 (SceneBuilder가 Assets/Audio/Sfx/*.wav를 넣음). 비어 있으면 실행 중 합성.</summary>
        public List<AudioClip> clips = new List<AudioClip>();
        /// <summary>Music 그룹에 속한 소리 (곡 AudioSource, 반주 AudioSource).</summary>
        public List<AudioSource> musicSources = new List<AudioSource>();

        [Range(0, 1)] public float musicGroupVolume = 1f;
        [Range(0, 1)] public float sfxGroupVolume = 1f;

        /// <summary>테스트에서 바꿔 끼우는 실제 시간 (초).</summary>
        public System.Func<float> RealTime = () => Time.unscaledTime;

        class Voice
        {
            public AudioSource Source;
            public SfxKind Kind;
            public float StartedAt = -999f, Length;
        }

        readonly List<Voice> voices = new List<Voice>();
        readonly Dictionary<SfxKind, AudioClip> byKind = new Dictionary<SfxKind, AudioClip>();
        readonly Dictionary<AudioSource, float> musicBase = new Dictionary<AudioSource, float>();
        float duckDb, duckUntil, duckStart, duckSeconds;
        bool built;

        /// <summary>마지막으로 낸 효과음들 (테스트용, 최근 32개).</summary>
        public readonly List<SfxKind> History = new List<SfxKind>();
        public int PoolSize => voices.Count;
        /// <summary>지금 덕킹 배율 (1 = 그대로).</summary>
        public float DuckFactor { get; private set; } = 1f;

        void Awake() => Build();

        public void Build()
        {
            if (built) return;
            built = true;
            foreach (var c in clips)
                if (c != null && System.Enum.TryParse(c.name.Replace("Sfx_", ""), out SfxKind k)) byKind[k] = c;
            for (int i = 0; i < GameConfig.Fx.SfxPoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                voices.Add(new Voice { Source = src });
            }
        }

        public AudioClip Clip(SfxKind k)
        {
            Build();
            if (!byKind.TryGetValue(k, out var c) || c == null) byKind[k] = c = SfxSynth.CreateClip(k);
            return c;
        }

        /// <summary>Music 그룹에 소리를 넣는다 (원래 음량을 기억해 두고 그룹 음량·덕킹을 곱한다).</summary>
        public void RegisterMusic(AudioSource s)
        {
            if (s == null || musicSources.Contains(s)) return;
            musicSources.Add(s);
        }

        /// <summary>같은 종류가 지금 몇 개 울리는지 (길이로 계산, 오디오 장치가 없어도 정확).</summary>
        public int ActiveCount(SfxKind k)
        {
            float now = RealTime();
            int n = 0;
            foreach (var v in voices) if (v.Kind == k && now - v.StartedAt < v.Length) n++;
            return n;
        }

        /// <summary>효과음 하나를 낸다.</summary>
        public void Play(SfxKind k, float volume = 1f)
        {
            Build();
            float now = RealTime();
            Voice pick = null;
            // 같은 종류가 3개면 가장 오래된 같은 종류를 끊는다
            if (ActiveCount(k) >= GameConfig.Fx.SfxSameKindMax)
            {
                foreach (var v in voices) if (v.Kind == k && now - v.StartedAt < v.Length && (pick == null || v.StartedAt < pick.StartedAt)) pick = v;
            }
            if (pick == null) foreach (var v in voices) if (now - v.StartedAt >= v.Length) { pick = v; break; }
            if (pick == null) foreach (var v in voices) if (pick == null || v.StartedAt < pick.StartedAt) pick = v;   // 다 차면 가장 오래된 것
            var clip = Clip(k);
            pick.Kind = k;
            pick.StartedAt = now;
            pick.Length = clip.length;
            pick.Source.Stop();
            pick.Source.clip = clip;
            pick.Source.volume = volume * sfxGroupVolume;
            pick.Source.Play();
            History.Add(k);
            if (History.Count > 32) History.RemoveAt(0);
        }

        /// <summary>음악을 dB만큼 seconds 동안 줄였다가 되돌린다. 겹치면 가장 깊은 값 하나 (제안서 4장).</summary>
        public void Duck(float db, float seconds)
        {
            float now = RealTime();
            bool active = now < duckUntil;
            if (active && db >= duckDb) { duckUntil = Mathf.Max(duckUntil, now + seconds); return; }
            duckDb = db; duckStart = now; duckSeconds = seconds; duckUntil = now + seconds;
        }

        void LateUpdate() => Tick();

        public void Tick()
        {
            float now = RealTime();
            float db = 0f;
            if (now < duckUntil)
            {
                // 빠르게 내려갔다가 끝에서 되돌아옴 (마지막 30%)
                float u = (now - duckStart) / Mathf.Max(1e-4f, duckUntil - duckStart);
                db = duckDb * (u < 0.7f ? 1f : 1f - (u - 0.7f) / 0.3f);
            }
            DuckFactor = Mathf.Pow(10f, db / 20f);
            foreach (var s in musicSources)
            {
                if (s == null) continue;
                if (!musicBase.ContainsKey(s)) musicBase[s] = s.volume;
                s.volume = musicBase[s] * musicGroupVolume * DuckFactor;   // 음량만 (time·pitch는 그대로)
            }
        }

        /// <summary>반주처럼 Music 그룹 소리의 기준 음량을 다시 정할 때.</summary>
        public void SetMusicBase(AudioSource s, float v) { musicBase[s] = v; }
    }
}
