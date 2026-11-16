using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R18c (GDD-beta B19.3 뒷부분): 지붕 위 전투의 적은 처음부터 전투 자리에 서 있지 않다.
    /// 플레이어 뒤쪽 옆 지붕 줄에서 나타나 쫓아오며 앞지르고(빌드 때 보이는 메시로 잰 발 높이), 지붕을 박차고 뛰어(Running Jump)
    /// 포물선으로 날아(Falling Idle) 플레이어가 멈추기 직전 전투 자리에 착지한다(Hard Landing). 클립 시각은 포물선에 맞춰 직접 정한다.
    /// 착지 뒤에는 아무것도 하지 않는다 (전투 연출은 StageDirector가 그대로).
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class EnemyLeapIn : MonoBehaviour
    {
        public EnemyActor actor;
        /// <summary>플레이어가 멈추는 달리기 박 (전투 사건 runBeat).</summary>
        public float stopBeat;
        /// <summary>나타나 달리기 시작·발이 지붕을 떠남·발이 전투 자리에 닿음 (달리기 박).</summary>
        public float runFromBeat, takeoffBeat, contactBeat;
        /// <summary>옆 지붕 줄 발 자리 (월드 XZ·발 높이, runFromBeat→takeoffBeat 고른 간격)와 뛰어넘는 칸.</summary>
        public float[] runX = new float[0], runZ = new float[0], runY = new float[0];
        public bool[] runGap = new bool[0];
        /// <summary>전투 자리(착지 자리, 발 높이 = 보이는 지붕 면)와 방향, 날아가는 포물선 높이 (m).</summary>
        public Vector3 homePos;
        public Quaternion homeRot = Quaternion.identity;
        public float arc = 2f;

        public float EndBeat => stopBeat - GameConfig.Map.LeapIdleBefore;

        /// <summary>지금 단계 (테스트용): Waiting, Run, Air, Land, Done.</summary>
        public string Phase { get; private set; } = "Waiting";
        /// <summary>발이 면에서 떨어진 중 (뛰어넘기·날기, 발밑 검사에서 뺌).</summary>
        public bool Hopping { get; private set; }

        PathFollower player;
        StageDirector director;
        Renderer[] renderers;
        bool shown = true;

        void Start()
        {
            var boot = GameBootstrap.Instance;
            if (boot != null) { player = boot.player; director = boot.director; }
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        void Show(bool on)
        {
            if (shown == on || renderers == null) return;
            shown = on;
            foreach (var r in renderers) if (r != null) r.enabled = on;
        }

        float SecondsPerRunBeat => GameConfig.SecondsPerBeat;

        void LateUpdate()
        {
            if (actor == null || player == null || actor.Defeated) { Show(true); return; }
            var anim = actor.anim;
            if (director != null && director.CurrentEncounter != null && System.Array.IndexOf(director.CurrentEncounter.Actors, actor) >= 0)
            {
                if (Phase != "Done") { Phase = "Done"; Hopping = false; if (anim != null) anim.EndLeap(); }
                Show(true);
                return;
            }
            double v = player.VisualRunBeat;
            Hopping = false;
            if (v >= EndBeat)
            {
                Show(true);
                if (Phase != "Done") { Phase = "Done"; actor.transform.SetPositionAndRotation(homePos, homeRot); if (anim != null) anim.EndLeap(); }
                return;
            }
            float jumpKey = anim != null ? anim.HitSecondsOf(EnemyMoves.LeapJump) : 0.3f;
            float landKey = anim != null ? anim.HitSecondsOf(EnemyMoves.LeapLand) : 0.3f;
            int n = runY != null ? runY.Length : 0;
            if (v < runFromBeat || n < 2)
            {
                // 아직 나타나지 않음 (앞쪽 지붕에 미리 서 있지 않게 숨김)
                Phase = "Waiting";
                Show(false);
                if (n > 0) actor.transform.position = new Vector3(runX[0], runY[0], runZ[0]);
                if (anim != null) anim.EndLeap();
                return;
            }
            Show(true);
            if (v < takeoffBeat)
            {
                Phase = "Run";
                float f = (float)((v - runFromBeat) / (takeoffBeat - runFromBeat)) * (n - 1);
                int i = Mathf.Min((int)f, n - 2);
                float k = f - i;
                var p = new Vector3(Mathf.Lerp(runX[i], runX[i + 1], k), Mathf.Lerp(runY[i], runY[i + 1], k), Mathf.Lerp(runZ[i], runZ[i + 1], k));
                Hopping = runGap != null && runGap.Length == n && (runGap[i] || runGap[i + 1]);
                var dir = new Vector3(runX[i + 1] - runX[i], 0f, runZ[i + 1] - runZ[i]);
                actor.transform.SetPositionAndRotation(p, dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir.normalized) : actor.transform.rotation);
                double jumpStart = takeoffBeat - jumpKey / SecondsPerRunBeat;
                if (anim != null) { if (v >= jumpStart) anim.PlayLeap(EnemyMoves.LeapJump, (float)((v - jumpStart) * SecondsPerRunBeat)); else anim.EndLeap(); }
                return;
            }
            var from = new Vector3(runX[n - 1], runY[n - 1], runZ[n - 1]);
            if (v < contactBeat)
            {
                Phase = "Air";
                Hopping = true;
                float u = (float)((v - takeoffBeat) / (contactBeat - takeoffBeat));
                var p = Vector3.Lerp(from, homePos, u);
                p.y += arc * 4f * u * (1f - u);
                var dir = homePos - from; dir.y = 0f;
                var face = Quaternion.Slerp(dir.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(dir.normalized) : homeRot, homeRot, Mathf.SmoothStep(0f, 1f, u));
                actor.transform.SetPositionAndRotation(p, face);
                if (anim != null)
                {
                    float sinceTake = (float)((v - takeoffBeat) * SecondsPerRunBeat), toContact = (float)((contactBeat - v) * SecondsPerRunBeat);
                    if (toContact <= landKey) anim.PlayLeap(EnemyMoves.LeapLand, landKey - toContact);
                    else if (sinceTake < GameConfig.Map.LeapJumpAirSeconds) anim.PlayLeap(EnemyMoves.LeapJump, jumpKey + sinceTake);
                    else anim.PlayLeap(EnemyMoves.LeapFall, sinceTake);
                }
                return;
            }
            Phase = "Land";
            actor.transform.SetPositionAndRotation(homePos, homeRot);
            if (anim != null) anim.PlayLeap(EnemyMoves.LeapLand, landKey + (float)((v - contactBeat) * SecondsPerRunBeat));
        }
    }
}
