using System;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 아슬아슬 회피 슬로모션 (GDD-beta B17.4). 회피 슬로모션 판정(대문 ↓, 포위 마지막 동시 공격, 무사 A37.5·A66)의 목표 1박 전부터
    /// 0.25박에 걸쳐 박자 시계·음악·화면(Time.timeScale)을 함께 0.4배로 늦추고, 그 판정이 나면 0.15초에 걸쳐 원래대로 돌린다.
    /// 음악은 음 높이가 낮아지고(BeatClock.SetSlowFactor) 저역 통과로 먹먹해지며 작아진다.
    /// 처형·윈드밀 마무리 슬로모션(SlowMotion, GDD 4.2) 동안에도 음악을 먹먹하게 한다.
    /// </summary>
    public class BulletTimeFx : MonoBehaviour
    {
        public BeatClock clock;
        public JudgeSystem judge;
        public StageDirector director;
        /// <summary>처형·윈드밀 슬로모션 (먹먹함만 함께).</summary>
        public SlowMotion slowMotion;
        /// <summary>음악 음량 배율을 넣는 곳.</summary>
        public SfxPlayer sfx;
        /// <summary>음악 AudioSource에 붙은 저역 통과 필터.</summary>
        public AudioLowPassFilter lowPass;

        /// <summary>테스트에서 바꿔 끼우는 프레임 시간 (실제 초).</summary>
        public Func<float> DeltaTime = () => Time.unscaledDeltaTime;

        JudgeCue active;
        double startBeat;
        bool releasing;
        float releaseFrom, releaseT;
        bool ownsTimeScale;

        /// <summary>지금 회피 슬로모션 중인 판정 (돌아오는 중 포함).</summary>
        public JudgeCue Active => active;
        public bool Releasing => releasing;
        /// <summary>회피 슬로모션이 시작된 횟수·돌아온 횟수 (테스트용).</summary>
        public int StartCount { get; private set; }
        public int ReleaseCount { get; private set; }
        /// <summary>먹먹함 0~1 (저역 통과·음량).</summary>
        public float Muffle { get; private set; }

        public event Action<JudgeCue> Started;
        /// <summary>판정이 나서 원래 속도로 돌아가기 시작할 때 (판정 결과는 cue.Result).</summary>
        public event Action<JudgeCue> Released;

        void OnEnable()
        {
            if (clock != null) clock.Restarted += OnRestarted;
            if (director != null) director.StageReset += ResetState;
        }

        void OnDisable()
        {
            if (clock != null) clock.Restarted -= OnRestarted;
            if (director != null) director.StageReset -= ResetState;
        }

        void OnRestarted(double _) => ResetState();

        /// <summary>되감기·다시 시작: 바로 평소 속도로 (시계는 StartClock·RestartAt이 이미 1로 돌림).</summary>
        public void ResetState()
        {
            active = null;
            releasing = false;
            if (clock != null && clock.IsRunning) clock.SetSlowFactor(1f, clock.Now);
            if (ownsTimeScale && Time.timeScale > 0f && (slowMotion == null || !slowMotion.IsActive)) Time.timeScale = 1f;
            ownsTimeScale = false;
            ApplyAudio();
        }

        /// <summary>회피 슬로모션 판정인지: 전투 판정의 BulletTime, 사건의 BulletSlot.</summary>
        public static bool IsBulletCue(JudgeCue cue)
        {
            if (cue == null) return false;
            if (cue.Tag is CombatStep s) return s.BulletTime;
            if (cue.Tag is StageDirector.ObstacleRun r) return r.Def != null && r.Def.BulletSlot >= 0 && r.Cues.IndexOf(cue) == r.Def.BulletSlot;
            return false;
        }

        void Update() => Tick(DeltaTime());

        public void Tick(float dt)
        {
            if (clock == null || !clock.IsRunning || clock.IsPaused) { ApplyAudio(); return; }
            double beat = clock.CurrentBeat;
            double now = clock.Now;
            if (active == null && judge != null)
            {
                foreach (var c in judge.Cues)
                {
                    if (c.Judged || !IsBulletCue(c)) continue;
                    if (beat >= c.TargetBeat - GameConfig.BulletTime.LeadBeats - 1e-6 && beat < c.TargetBeat + 0.5)
                    {
                        active = c;
                        startBeat = c.TargetBeat - GameConfig.BulletTime.LeadBeats;
                        releasing = false;
                        StartCount++;
                        Started?.Invoke(c);
                        break;
                    }
                }
            }
            if (active != null && !releasing)
            {
                if (active.Judged)
                {
                    releasing = true;
                    releaseFrom = clock.SlowFactor;
                    releaseT = 0f;
                    ReleaseCount++;
                    Released?.Invoke(active);
                }
                else
                {
                    float u = Mathf.Clamp01((float)((beat - startBeat) / GameConfig.BulletTime.RampInBeats));
                    clock.SetSlowFactor(Mathf.Lerp(1f, GameConfig.BulletTime.Factor, u), now);
                }
            }
            if (releasing)
            {
                releaseT += Mathf.Max(0f, dt);
                float u = Mathf.Clamp01(releaseT / GameConfig.BulletTime.ReleaseSeconds);
                clock.SetSlowFactor(Mathf.Lerp(releaseFrom, 1f, u), now);
                if (u >= 1f) { active = null; releasing = false; }
            }
            ApplyTimeScale();
            ApplyAudio();
        }

        void ApplyTimeScale()
        {
            if (Time.timeScale <= 0f || (slowMotion != null && slowMotion.IsActive)) return;   // 일시정지·처형 슬로모션이 우선
            float f = clock.SlowFactor;
            if (f < 1f - 1e-4f) { Time.timeScale = f; ownsTimeScale = true; }
            else if (ownsTimeScale) { Time.timeScale = 1f; ownsTimeScale = false; }
        }

        void ApplyAudio()
        {
            float bullet = clock != null ? Mathf.Clamp01((1f - clock.SlowFactor) / (1f - GameConfig.BulletTime.Factor)) : 0f;
            float story = slowMotion != null && slowMotion.IsActive ? 1f : 0f;
            Muffle = Mathf.Max(bullet, story);
            if (lowPass != null)
                lowPass.cutoffFrequency = GameConfig.BulletTime.LowPassOpenHz * Mathf.Pow(GameConfig.BulletTime.LowPassMuffledHz / GameConfig.BulletTime.LowPassOpenHz, Muffle);
            if (sfx != null) sfx.MusicFxGain = Mathf.Lerp(1f, GameConfig.BulletTime.MusicGain, Muffle);
        }
    }
}
