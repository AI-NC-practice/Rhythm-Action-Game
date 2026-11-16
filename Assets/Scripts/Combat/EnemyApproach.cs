using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 작업 R18b (GDD-beta B19.3 앞부분): 땅 위 멈추는 전투의 적은 처음부터 전투 자리에 서 있지 않다.
    /// 플레이어 시야에서 가려진 자리(빌드 때 보이는 메시로 고름: 담·집 모퉁이 너머)에 있다가, 플레이어가 멈추기 noticeLead박 전에
    /// 고개를 돌려 알아채고(NoticeBeats 동안 플레이어 쪽으로 돌아섬, 소리 없음) 전투 자리(EnemyActor의 처음 자리)로 달려와 멈춤 박 전에 도착한다.
    /// 도착한 뒤에는 아무것도 하지 않는다 (전투 연출은 StageDirector가 그대로).
    /// </summary>
    [DefaultExecutionOrder(-60)]   // PlayerBody(−50)·StageDirector(0)보다 먼저 자리를 잡는다
    public class EnemyApproach : MonoBehaviour
    {
        public EnemyActor actor;
        /// <summary>플레이어가 멈추는 달리기 박 (전투 사건의 runBeat).</summary>
        public float stopBeat;
        /// <summary>숨은 자리 (월드)와 숨은 자리 → 전투 자리 길의 발 높이 (고른 간격).</summary>
        public Vector3 hidePos;
        public float[] pathY = new float[0];
        /// <summary>전투 자리 (월드, EnemyActor 처음 자리)와 방향.</summary>
        public Vector3 homePos;
        public Quaternion homeRot = Quaternion.identity;
        /// <summary>멈춤 몇 박 전에 알아채는지 (달리는 거리에 따라 빌드 때 정함).</summary>
        public float noticeLead = GameConfig.Map.ApproachNoticeLead;

        public float NoticeBeat => stopBeat - noticeLead;
        public float RunFromBeat => NoticeBeat + GameConfig.Map.ApproachNoticeBeats;
        public float ArriveBeat => stopBeat - GameConfig.Map.ApproachArriveBefore;

        /// <summary>지금 단계 (테스트용): Hidden, Notice, Run, Arrived.</summary>
        public string Phase { get; private set; } = "Hidden";

        PathFollower player;
        StageDirector director;

        void Start()
        {
            var boot = GameBootstrap.Instance;
            if (boot != null) { player = boot.player; director = boot.director; }
        }

        void LateUpdate()
        {
            if (actor == null || player == null || actor.Defeated) return;
            if (director != null && director.CurrentEncounter != null && System.Array.IndexOf(director.CurrentEncounter.Actors, actor) >= 0) { Phase = "Arrived"; return; }
            double v = player.VisualRunBeat;
            if (v >= ArriveBeat) { if (Phase != "Arrived") { Phase = "Arrived"; actor.transform.SetPositionAndRotation(homePos, homeRot); } return; }
            if (v < NoticeBeat - 40) return;   // 멀리 있을 때는 그대로 (숨은 자리에 있음)
            var toPlayer = player.transform.position - actor.transform.position; toPlayer.y = 0f;
            if (v < NoticeBeat)
            {
                Phase = "Hidden";
                // 숨어서 길이 아니라 옆(전투 자리 반대쪽)을 보고 있음
                var away = hidePos - homePos; away.y = 0f;
                actor.transform.SetPositionAndRotation(hidePos, away.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(away.normalized) : homeRot);
                return;
            }
            if (v < RunFromBeat)
            {
                Phase = "Notice";
                float k = Mathf.SmoothStep(0f, 1f, (float)((v - NoticeBeat) / GameConfig.Map.ApproachNoticeBeats));
                var away = hidePos - homePos; away.y = 0f;
                var from = away.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(away.normalized) : homeRot;
                var to = toPlayer.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(toPlayer.normalized) : homeRot;
                actor.transform.SetPositionAndRotation(hidePos, Quaternion.Slerp(from, to, k));
                return;
            }
            Phase = "Run";
            float u = Mathf.Clamp01((float)((v - RunFromBeat) / (ArriveBeat - RunFromBeat)));
            float s = u * u * (3f - 2f * u) * 0.35f + u * 0.65f;   // 처음에 조금 느리게 출발
            var p = Vector3.Lerp(hidePos, homePos, s);
            p.y = PathY(s);
            var dir = homePos - hidePos; dir.y = 0f;
            var look = u < 0.85f ? dir : Vector3.Lerp(dir.normalized, toPlayer.normalized, (u - 0.85f) / 0.15f);
            actor.transform.SetPositionAndRotation(p, look.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(look.normalized) : homeRot);
        }

        float PathY(float s)
        {
            if (pathY == null || pathY.Length < 2) return Mathf.Lerp(hidePos.y, homePos.y, s);
            float f = s * (pathY.Length - 1);
            int i = Mathf.Min((int)f, pathY.Length - 2);
            return Mathf.Lerp(pathY[i], pathY[i + 1], f - i);
        }
    }
}
