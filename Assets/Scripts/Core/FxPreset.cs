namespace RhythmActionGame
{
    /// <summary>슬로모션 요청 방식 (GDD-beta B4).</summary>
    public enum SlowMoMode
    {
        None,
        /// <summary>처형·윈드밀 (GDD 4.2) — 빈도 제한 없이 항상.</summary>
        Always,
        /// <summary>사건·전투 PERFECT 후보 — 직전 슬로모션 시작 후 16박 안이면 거절.</summary>
        Candidate,
    }

    /// <summary>
    /// 연출 한 번의 카메라·시간 수치 (GDD-beta B5, 제안서 2~3장). 값은 GameConfig.Fx에만 둔다.
    /// 각 층은 Rise초 동안 올라가 Hold초 유지한 뒤 Release초 동안 0으로 돌아온다 (Release ≤ 0.5초).
    /// 방향이 있는 값(Roll, SideShift)은 연출을 부를 때 준 방향(side: 왼쪽 −1, 오른쪽 +1)을 곱한다.
    /// </summary>
    public struct FxPreset
    {
        public string Name;
        /// <summary>강도 등급 0~5 (제안서 3장).</summary>
        public int Grade;
        /// <summary>대상 오브젝트만 멈추는 시간 (초).</summary>
        public float HitStop;
        public SlowMoMode SlowMo;
        /// <summary>흔들림 세기(트라우마)에 더할 값 (0~1).</summary>
        public float Trauma;
        /// <summary>흔들림 주파수 (Hz).</summary>
        public float ShakeHz;
        /// <summary>뒤로 밀림 (m, 음수면 앞으로).</summary>
        public float KickBack;
        /// <summary>옆으로 이동 (m, side 방향).</summary>
        public float SideShift;
        /// <summary>높이 변화 (m).</summary>
        public float Height;
        /// <summary>고개 (도, + 아래 / − 위).</summary>
        public float Pitch;
        /// <summary>기울기 (도, side 방향).</summary>
        public float Roll;
        /// <summary>좌우 흔들림 (도, 저주파).</summary>
        public float YawWobble;
        /// <summary>FOV 변화 (도).</summary>
        public float Fov;
        public float Rise, Hold, Release;

        public float Total => Rise + Hold + Release;
    }
}
