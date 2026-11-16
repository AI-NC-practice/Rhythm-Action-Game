using System.Collections.Generic;
using UnityEngine;

namespace RhythmActionGame
{
    /// <summary>
    /// 적·복선 발밑 지지면 기록 (작업 R03). SceneBuilder가 맵을 지을 때 실제로 보이는 지붕·담·땅에 아래로 광선을 쏴서 남긴다.
    /// (길가 장식은 GPU 인스턴싱으로 그려 실행 중에는 메시가 없으므로, 빌드 때 잰 결과를 씬에 저장한다.)
    /// </summary>
    public class MapSupportReport : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public string figure;
            /// <summary>foreshadow(복선, 지붕·담 위에 세움) / runner(달리며 나오는 적, 발밑에 맞춤) / enemy(멈춰 싸우는 적, 확인만).</summary>
            public string kind;
            /// <summary>원래 발 높이 (m). 복선 적은 빌드 때 지지면에 맞추므로 맞추기 전 값.</summary>
            public float footY;
            /// <summary>발밑 지지면 높이 (m). 못 찾으면 NaN.</summary>
            public float supportY;
            public string support;
            public bool snapped;
        }

        public List<Entry> entries = new List<Entry>();
    }
}
